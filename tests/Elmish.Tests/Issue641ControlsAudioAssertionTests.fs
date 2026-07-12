module Issue641ControlsAudioAssertionTests

// Issue #641 — the `app` (Controls) profile could REQUEST sound (#429/#436) but could not ASSERT it.
// `audioRequests` was `GeneratedAppHost`-only, and every audio-capable Controls path
// (`runInteractiveAppWithAudio`, `Live.runScriptWithAudio`) needs a live GL window, so the one trap the
// audio skill spends the most words on — the `Started` trap: a product that flips a flag and forgets to
// emit the `PlayAudio` — was not merely untested on this family. It was structurally UNCATCHABLE.
//
// These tests drive the REAL headless fold (`Perf.runScriptToEffects` — the same `runScriptCore` the
// existing `runScript`/`runScriptToModel` use) rather than a hand-rolled re-fold of `host.Update` in
// test code. That distinction is the whole point: a test-local fold asserts what the TEST does, and the
// bug being hunted is the product loop doing something else.

open Expecto
open System
open FS.GG.Audio.Core
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg =
    | StartPressed
    | VolumeChanged of float

type private Model = { Started: bool; Volume: float }

let private size: Size = { Width = 320; Height = 200 }

let private click = SoundId "ui-click"

/// The saved volume a settings screen restores at startup — the motivating example. Restoring it into
/// the MODEL is not restoring it: the mixer only hears what a `PlayAudio` tells it.
let private savedVolume = 0.25

/// A start screen: a Button that plays a click when pressed. Authored the documented way — an `onClick`
/// binding, no `MapPointer` clauses.
let private startScreen (_: Size) (_: Model) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Button.create [ Button.text "Start"; Button.onClick StartPressed ] |> Control.withKey "start" ] ]

let private baseHost: InteractiveAppHost<Model, Msg> =
    { Init = fun () -> { Started = false; Volume = savedVolume }, []
      Update = fun _ model -> model, []
      View = startScreen
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

/// The CORRECT product. Startup restores the saved volume by TELLING the mixer; a press asks for the
/// click sfx.
let private correctHost: InteractiveAppHost<Model, Msg> =
    { baseHost with
        Init = fun () -> { Started = false; Volume = savedVolume }, [ PlayAudio [ Audio.setBusVolume Bus.Music savedVolume ] ]
        Update =
            fun msg model ->
                match msg with
                | StartPressed -> { model with Started = true }, [ PlayAudio [ Audio.playSfx click 1.0 ] ]
                | VolumeChanged v -> { model with Volume = v }, [ PlayAudio [ Audio.setBusVolume Bus.Music v ] ] }

/// The TRAPPED product — the `Started` trap, and its startup twin. Both transitions land the model in
/// EXACTLY the state the correct host lands it in: `Started` flips, `Volume` holds the restored value.
/// Neither ever tells the mixer. This host is silent, and its model cannot say so.
let private trappedHost: InteractiveAppHost<Model, Msg> =
    { baseHost with
        Init = fun () -> { Started = false; Volume = savedVolume }, []
        Update =
            fun msg model ->
                match msg with
                | StartPressed -> { model with Started = true }, []
                | VolumeChanged v -> { model with Volume = v }, [] }

let private centreOf (host: InteractiveAppHost<Model, Msg>) (model: Model) (nodeId: ControlId) =
    let rendered = Control.renderTree host.Theme size (host.View size model)

    let available: FS.GG.UI.Layout.AvailableSpace =
        { Width = float size.Width
          WidthMode = FS.GG.UI.Layout.Exactly
          Height = float size.Height
          HeightMode = FS.GG.UI.Layout.Exactly }

    let result = FS.GG.UI.Layout.Layout.evaluate available rendered.Layout
    let b = result.Bounds |> List.find (fun b -> b.NodeId = nodeId)
    b.Bounds.X + b.Bounds.Width / 2.0, b.Bounds.Y + b.Bounds.Height / 2.0

/// Press the Start button — a real click, resolved from the retained frame by the real router.
let private pressStart (host: InteractiveAppHost<Model, Msg>) =
    let cx, cy = centreOf host (fst (host.Init())) "start"
    [ FrameInput.Pointer(Click("start", PointerButton.Primary, cx, cy)) ]

[<Tests>]
let tests =
    testList
        "issue-641 controls headless audio assertion"
        [
          // The capability the issue asked for: a headless dispatch whose ViewerEffect list a caller can
          // read, narrowed to AudioEffect list — no window, no GL, no device.
          test "a click on an authored Button surfaces the sound it requested, headlessly" {
            let _, effects, _ = ControlsElmish.Perf.runScriptToEffects correctHost size (pressStart correctHost)

            let audio = effects |> ControlsElmish.audioRequests

            Expect.equal
                audio
                [ Audio.setBusVolume Bus.Music savedVolume; Audio.playSfx click 1.0 ]
                "the restored volume at startup, then the click the press asked for — in dispatch order"
          }

          // THE test. The issue's central claim, made executable: the model cannot catch the `Started`
          // trap, and the effect stream can. If this test ever fails, the capability has regressed.
          test "the Started trap is invisible in the model and visible at the sink" {
            let correctModel, correctEffects, _ =
                ControlsElmish.Perf.runScriptToEffects correctHost size (pressStart correctHost)

            let trappedModel, trappedEffects, _ =
                ControlsElmish.Perf.runScriptToEffects trappedHost size (pressStart trappedHost)

            // 1. A model-level assertion CANNOT tell the silent product from the sounding one. Every
            //    test written against the model passes on BOTH — which is precisely why the trap
            //    survives code review and ships.
            Expect.equal trappedModel correctModel "the two products are indistinguishable from inside the model"
            Expect.isTrue trappedModel.Started "the trapped product did flip Started — the flag is not the bug"

            // 2. Asking what the MIXER WAS TOLD separates them instantly.
            Expect.equal
                (correctEffects |> ControlsElmish.audioRequests)
                [ Audio.setBusVolume Bus.Music savedVolume; Audio.playSfx click 1.0 ]
                "the correct product told the mixer twice"

            Expect.equal
                (trappedEffects |> ControlsElmish.audioRequests)
                []
                "the trapped product is SILENT — it restored the volume into its model and played nothing"
          }

          // `Init` seeds the stream, and it is not a detail: the restored-volume-at-startup case IS an
          // Init effect, and the live loop interprets `initEffects` into its sink before frame 0
          // (Viewer.runInteractiveViewerWithWindowBehaviorCore). A recorder that started at frame 0
          // would report the trapped and correct hosts as identical at startup.
          test "Init's requests are recorded, before any frame's" {
            let _, effects, _ = ControlsElmish.Perf.runScriptToEffects correctHost size []

            Expect.equal
                (effects |> ControlsElmish.audioRequests)
                [ Audio.setBusVolume Bus.Music savedVolume ]
                "an empty script still records what startup asked for"
          }

          // Parity with the game family — the issue's actual acceptance criterion: the same effect list
          // must narrow to the same AudioEffect list on both families, so a product that changes host
          // family does not change its audio assertions. One implementation, two names.
          test "audioRequests agrees with the generated-app family, effect for effect" {
            let _, effects, _ = ControlsElmish.Perf.runScriptToEffects correctHost size (pressStart correctHost)

            Expect.equal
                (effects |> ControlsElmish.audioRequests)
                (effects |> GeneratedAppHost.audioRequests)
                "the Controls narrowing IS the generated-app narrowing — a second copy is drift waiting to happen"
          }

          // The narrowing is a narrowing: non-audio effects are dropped, not smuggled through.
          test "non-audio effects are dropped by the narrowing but kept in the raw stream" {
            let noisyHost =
                { correctHost with
                    Update =
                        fun msg model ->
                            match msg with
                            | StartPressed ->
                                { model with Started = true },
                                [ CaptureScreenshot "shot.png"
                                  PlayAudio [ Audio.playSfx click 1.0 ]
                                  CloseWindow ]
                            | VolumeChanged v -> { model with Volume = v }, [] }

            let _, effects, _ = ControlsElmish.Perf.runScriptToEffects noisyHost size (pressStart noisyHost)

            // `ViewerEffect` carries a case with no equality, so match the case rather than compare values.
            Expect.isTrue
                (effects |> List.exists (function CloseWindow -> true | _ -> false))
                "the raw stream keeps every effect the product emitted, not just the audio ones"

            Expect.equal
                (effects |> ControlsElmish.audioRequests)
                [ Audio.setBusVolume Bus.Music savedVolume; Audio.playSfx click 1.0 ]
                "…and the narrowing yields the sound requests alone"
          }

          // The recorder is an addition, not a change: threading the effect list through `runScriptCore`
          // must leave the two existing entry points folding exactly as they did.
          test "runScript and runScriptToModel are unchanged by the recording" {
            let script = pressStart correctHost
            let metrics = ControlsElmish.Perf.runScript correctHost size script
            let model, modelMetrics = ControlsElmish.Perf.runScriptToModel correctHost size script
            let effectModel, _, effectMetrics = ControlsElmish.Perf.runScriptToEffects correctHost size script

            Expect.equal modelMetrics metrics "runScriptToModel still returns runScript's metrics"
            Expect.equal effectMetrics metrics "the recording fold returns the same metrics as the fold it extends"
            Expect.equal effectModel model "…and the same final model"
          }
        ]
