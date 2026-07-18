module Issue911SceneHostDriverTests

open System
open Expecto
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// Issue #911: the scene-host analogue of #461's click-path primitives (`Perf.runScriptToModel` +
// `BoundIds`). A `game`-family product takes input through this scene-host (`GeneratedAppHost.MapKey`),
// not the Controls click path, and Rougue1 shipped "v1 complete" with 108 green tests while unplayable:
// its acceptance suite injected messages straight into `update`, so nothing ever exercised
// `mapKey -> host -> update` and the missing routing could not turn a test red. These tests drive the
// new primitives through exactly that seam.

type GameModel =
    { Started: bool
      Paused: bool
      Floor: int
      Steps: int }

type GameMsg =
    | StartRun
    | TogglePause
    | DescendFloor
    | Move
    | ClockTick

let private zero = { Started = false; Paused = false; Floor = 0; Steps = 0 }

let private white = { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }

/// A product owns a stable case-label for its `Msg` (products commonly already have one for
/// diagnostics). It is what makes the reflection-free "handled-but-unwired" subtraction possible:
/// the framework hands back the reachable messages, the product names its handled universe.
let private caseName =
    function
    | StartRun -> "StartRun"
    | TogglePause -> "TogglePause"
    | DescendFloor -> "DescendFloor"
    | Move -> "Move"
    | ClockTick -> "ClockTick"

/// Every `Msg` case this product HANDLES in `update` — its runtime-source universe must cover this
/// (minus any case a product drives from an effect the pure fold does not model; none here).
let private handledUniverse =
    [ "StartRun"; "TogglePause"; "DescendFloor"; "Move"; "ClockTick" ]

let private update msg model =
    match msg with
    | StartRun -> { model with Started = true }, [ RenderScene(Text((0.0, 0.0), "start", white)) ]
    | TogglePause -> { model with Paused = not model.Paused }, []
    | DescendFloor -> { model with Floor = model.Floor + 1 }, []
    | Move -> { model with Steps = model.Steps + 1 }, []
    | ClockTick -> { model with Steps = model.Steps + 1 }, []

/// The host wired the way a playable product must be: every key routes to the message it names, and
/// the clock drives `ClockTick`.
let private playableHost: GeneratedAppHost<GameModel, GameMsg> =
    { Init = fun () -> zero, [ RenderScene(Text((0.0, 0.0), "boot", white)) ]
      Update = update
      View = fun m -> Text((0.0, 0.0), $"floor {m.Floor}", white)
      MapKey =
        fun key isDown ->
            if not isDown then
                None
            else
                match key with
                | Enter -> Some StartRun
                | Space -> Some TogglePause
                // `normalize` upper-cases a single-char letter raw key, so "d" arrives as `Letter 'D'` —
                // the driver exercises the real normalization the live runtime does.
                | Letter 'D' -> Some DescendFloor
                | ArrowRight -> Some Move
                | _ -> None
      Tick = fun _ -> Some ClockTick
      Diagnostics = Viewer.defaultDiagnostics }

/// The Rougue1 shape: `update` handles the full `Msg` set, but `MapKey` collapses EVERY key to a
/// single snapshot message (`Move`). `StartRun`/`TogglePause`/`DescendFloor` are handled-but-unwired —
/// dispatched from no runtime source — so the product renders and "responds" yet can never start,
/// pause, or descend. A message-injecting suite stays green; driving through the host does not.
let private unplayableHost: GeneratedAppHost<GameModel, GameMsg> =
    { playableHost with
        MapKey = fun _key isDown -> if isDown then Some Move else None
        Tick = fun _ -> None }

let private down raw = { RawKey = raw; Direction = ViewerKeyDirection.KeyDown }
let private up raw = { RawKey = raw; Direction = ViewerKeyDirection.KeyUp }

[<Tests>]
let tests =
    testList
        "issue-911 scene-host driver and wiring guard"
        [ test "runKeyScriptToModel folds a key script through the host's own MapKey -> Update to the final model" {
            // Enter (start), Right (move), d (descend), Right (move) — the real routing, not injected msgs.
            let model, _ =
                GeneratedAppHost.runKeyScriptToModel playableHost [ down "Enter"; down "ArrowRight"; down "d"; down "ArrowRight" ]

            Expect.isTrue model.Started "Enter routed to StartRun through the host"
            Expect.equal model.Floor 1 "'d' routed to DescendFloor"
            Expect.equal model.Steps 2 "two ArrowRight presses each routed to Move"
          }

          test "runKeyScriptToModel exposes the Rougue1 defect a message-injecting suite cannot" {
            // The SAME script, driven through the unplayable host. Injecting StartRun into `update`
            // would flip Started; driving Enter through the broken `MapKey` does not.
            let model, _ =
                GeneratedAppHost.runKeyScriptToModel unplayableHost [ down "Enter"; down "d"; down "ArrowRight" ]

            Expect.isFalse model.Started "Enter never reaches StartRun — the product cannot start"
            Expect.equal model.Floor 0 "'d' never reaches DescendFloor — the product cannot descend"
            // Every key collapsed to Move, so the model DID change — the product looks responsive.
            Expect.equal model.Steps 3 "each key still dispatched the snapshot Move: green-but-unplayable"
          }

          test "runKeyScriptToModel returns effects in order, Init's batch first" {
            let _, effects =
                GeneratedAppHost.runKeyScriptToModel playableHost [ down "Enter" ]

            let texts =
                effects
                |> List.choose (function RenderScene (Text (_, t, _)) -> Some t | _ -> None)

            Expect.equal texts [ "boot"; "start" ] "Init's boot frame precedes the StartRun frame"
          }

          test "runKeyScriptToModel from an empty script yields the Init model and Init effects only" {
            let model, effects = GeneratedAppHost.runKeyScriptToModel playableHost []

            Expect.equal model zero "no input leaves the model at Init"

            let texts =
                effects
                |> List.choose (function RenderScene (Text (_, t, _)) -> Some t | _ -> None)

            Expect.equal texts [ "boot" ] "only Init's effects are present"
          }

          test "auditKeyWiring partitions a declared surface into wired and dead bindings" {
            // A product declares the keys it binds. 'x' is declared but bound to nothing — a dead key.
            let declared = [ down "Enter"; down "Space"; down "d"; down "x" ]
            let wiring = GeneratedAppHost.auditKeyWiring playableHost declared

            Expect.equal (wiring.Wired |> List.map snd) [ StartRun; TogglePause; DescendFloor ] "each live key names its message"
            Expect.equal wiring.Dead [ down "x" ] "the declared-but-unbound key is dead — a key that is bound is a key that dispatches"
          }

          test "auditKeyWiring reports key-UP as dead when the host only routes key-down" {
            let wiring = GeneratedAppHost.auditKeyWiring playableHost [ down "Enter"; up "Enter" ]

            Expect.equal (wiring.Wired |> List.map snd) [ StartRun ] "key-down routes"
            Expect.equal wiring.Dead [ up "Enter" ] "key-up routes to nothing for this host"
          }

          test "reachableMessages over the full input surface covers the handled universe for a playable host" {
            let surface = [ down "Enter"; down "Space"; down "d"; down "ArrowRight" ]
            let reachable = GeneratedAppHost.reachableMessages playableHost surface (Some(TimeSpan.FromMilliseconds 16.0))

            let unwired = Set.difference (Set.ofList handledUniverse) (reachable |> List.map caseName |> Set.ofList)

            Expect.isEmpty unwired "every handled Msg case is dispatched by some runtime source"
          }

          test "reachableMessages exposes handled-but-unwired Msg cases on the unplayable host" {
            let surface = [ down "Enter"; down "Space"; down "d"; down "ArrowRight" ]
            let reachable = GeneratedAppHost.reachableMessages unplayableHost surface (Some(TimeSpan.FromMilliseconds 16.0))

            let unwired = Set.difference (Set.ofList handledUniverse) (reachable |> List.map caseName |> Set.ofList)

            Expect.equal
                unwired
                (Set.ofList [ "StartRun"; "TogglePause"; "DescendFloor"; "ClockTick" ])
                "only Move is reachable; the rest are handled in update but dispatched from no source"
          }

          test "reachableMessages omits a Tick-only message when no tick sample is supplied" {
            let surface = [ down "Enter"; down "Space"; down "d"; down "ArrowRight" ]
            let withoutTick = GeneratedAppHost.reachableMessages playableHost surface None

            Expect.isFalse
                (withoutTick |> List.exists (fun m -> caseName m = "ClockTick"))
                "ClockTick is reachable only through Tick, so it is absent without a sample"

            let withTick = GeneratedAppHost.reachableMessages playableHost surface (Some(TimeSpan.FromMilliseconds 16.0))

            Expect.isTrue
                (withTick |> List.exists (fun m -> caseName m = "ClockTick"))
                "supplying a tick sample includes the clock's message"
          }
        ]
