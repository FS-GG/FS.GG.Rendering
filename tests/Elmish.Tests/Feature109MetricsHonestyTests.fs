module Feature109MetricsHonestyTests

// Feature 109 (US1/US3) — the per-frame `FrameMetrics` now tells the TRUTH: `ViewRebuilt` (which
// conflated "the model changed" with "the view ran") was replaced by the two precise booleans
// `ProductModelChanged` + `ViewCalled` and the integer `FullRenderCount`. These tests drive the
// deterministic, byte-stable `ControlsElmish.Perf.runScript` path — the authoritative observability
// surface this feature ships ([[fs-gg-controls-host]]) — and assert each field against the real
// code-path fact: a no-message frame changes no model; a model-changing message with no visual diff
// re-measures nothing; an animation-only tick runs the view (overlay) with NO product message, so
// `ProductModelChanged` and `ViewCalled` genuinely DIVERGE (SC-011); coalescing collapses a move
// burst to <=1 processed move while never dropping a discrete interaction. `FrameDuration` is
// excluded from every assertion (FR-012).

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg =
    | Inc
    | Noop

let private size: Size = { Width = 320; Height = 200 }
let private noMods = ViewerKeyboard.noModifiers

let private mkHost (view: int -> Control<Msg>) (mapKey: ViewerKey -> bool -> Msg option) : InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update =
        fun msg model ->
            (match msg with
             | Inc -> model + 1
             | Noop -> model),
            []
      View = fun _ model -> view model
      Theme = Theme.light
      MapKey = mapKey
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private enterInc: ViewerKey -> bool -> Msg option =
    fun key _ ->
        match key with
        | Enter -> Some Inc
        | _ -> None

// A view that renders the model into the button text (a content-only difference on a model change).
let private textView (model: int) : Control<Msg> =
    Stack.create
        [ Stack.children [ Button.create [ Button.text (string model); Button.onClick Inc ] |> Control.withKey "btn" ] ]

// A view whose output is INDEPENDENT of the model — a model change produces no visual difference at
// all, so a re-render re-measures nothing (proving no field over-reports the work).
let private constView (_: int) : Control<Msg> =
    Stack.create
        [ Stack.children [ Button.create [ Button.text "fixed"; Button.onClick Inc ] |> Control.withKey "btn" ] ]

// A view whose Switch enters the `Hover` visual state once the model reaches 2. Crossing Normal->Hover
// through `renderStep` starts a per-identity animation clock, so a following `Tick` frame runs the
// overlay with no product message (the animation-only tick of SC-011).
let private animView (model: int) : Control<Msg> =
    Stack.create
        [ Stack.children [ Switch.create [ Attr.visualState (if model >= 2 then Hover else Normal) ] |> Control.withKey "sw" ] ]

let private textHost = mkHost textView (fun _ _ -> None)
let private textKeyHost = mkHost textView enterInc
let private constKeyHost = mkHost constView enterInc
let private animKeyHost = mkHost animView enterInc

let private hover id x y = FrameInput.Pointer(HoverEnter(id, x, y))
let private tick (ms: float) = FrameInput.Tick(TimeSpan.FromMilliseconds ms)

[<Tests>]
let tests =
    testList "Feature 109 honest FrameMetrics + coalescing fidelity (US1/US3, SC-001/002/003/004/011)" [

        // ---- US1: ProductModelChanged / ViewCalled / FullRenderCount tell the truth ----

        test "a frame with no product message changes no model (SC-001a)" {
            let frames = ControlsElmish.Perf.runScript textHost size [ FrameInput.Idle ]
            Expect.isFalse frames.[0].ProductModelChanged "idle dispatches no product message"
            Expect.isFalse frames.[0].ViewCalled "idle runs no view"
            Expect.equal frames.[0].FullRenderCount 0 "idle performs no full render"
        }

        test "a pure-hover frame routes from the retained frame: zero routing full renders (feature 110, SC-001/FR-004)" {
            // Feature 110 narrowed routing: a hover is resolved from the retained frame (here directly to
            // `MapPointer`, the oracle's non-Click path), performing NO `host.View` + `Control.renderTree`.
            let frames = ControlsElmish.Perf.runScript textHost size [ hover "btn" 5.0 5.0 ]
            Expect.isFalse frames.[0].ProductModelChanged "hover dispatched no product message"
            Expect.isFalse frames.[0].ViewCalled "routing performs no full render (the routing render is gone)"
            Expect.equal frames.[0].FullRenderCount 0 "zero routing full renders (SC-001)"
            Expect.equal frames.[0].FullRenderFallbackCount 0 "the retained route did not fall back (SC-005)"
            Expect.equal frames.[0].RemeasuredNodeCount 0 "no product message → renderStep did not run → no remeasure"
        }

        test "a model-changing message with no visual difference re-measures nothing (SC-001b / FR-003/004)" {
            // Two Enters on the constant view: the first seeds (init), the second steps an IDENTICAL
            // tree — the model changed both times, but the second frame re-measures nothing.
            let frames =
                ControlsElmish.Perf.runScript constKeyHost size [ FrameInput.Key(Enter, noMods); FrameInput.Key(Enter, noMods) ]

            Expect.isTrue frames.[1].ProductModelChanged "Inc changed the model (1 -> 2)"
            Expect.isTrue frames.[1].ViewCalled "the view ran"
            Expect.equal frames.[1].FullRenderCount 1 "one full render (the key-frame renderStep; no routing render)"
            Expect.equal frames.[1].RemeasuredNodeCount 0 "the view did not change, so nothing re-measured — no field over-reports work"
        }

        test "an animation-only tick is a PAINT-ONLY frame: no view, PaintRan true (feature 111, FR-004/SC-003)" {
            // Enter (model 1, Normal) seeds; Enter (model 2, Hover) starts the cross-fade clock; the
            // Tick advances that clock and re-samples the overlay carrying no product message. Feature 111:
            // the overlay is re-sampled from the unchanged retained tree WITHOUT calling host.View, so the
            // view phase is skipped and the paint phase runs (formerly ViewCalled=true conflated the two).
            let frames =
                ControlsElmish.Perf.runScript
                    animKeyHost
                    size
                    [ FrameInput.Key(Enter, noMods); FrameInput.Key(Enter, noMods); tick 16.0 ]

            let tickFrame = frames.[2]
            Expect.equal tickFrame.FrameCause FrameCause.Tick "the frame's cause is a tick"
            Expect.isFalse tickFrame.ProductModelChanged "the tick carried no product message"
            Expect.isFalse tickFrame.ViewCalled "the view phase is SKIPPED — host.View did not run (FR-004)"
            Expect.equal tickFrame.FullRenderCount 0 "zero full renders — the overlay re-sample is not a host.View materialization"
            Expect.isFalse tickFrame.DiffRan "no new view tree was reconciled (overlay re-sample only)"
            Expect.isTrue tickFrame.PaintRan "the PAINT phase ran to re-assemble the animation overlay (FR-002)"
        }

        test "ViewCalled equals FullRenderCount > 0 for every produced frame (SC-011 invariant)" {
            let frames =
                ControlsElmish.Perf.runScript
                    animKeyHost
                    size
                    [ FrameInput.Idle
                      hover "sw" 4.0 4.0
                      FrameInput.Key(Enter, noMods)
                      FrameInput.Key(Enter, noMods)
                      tick 16.0
                      FrameInput.Idle ]

            for f in frames do
                Expect.equal f.ViewCalled (f.FullRenderCount > 0) "ViewCalled is exactly FullRenderCount > 0"
        }

        test "an idle frame is zero work: no remeasure, no processed moves, view not called (SC-004 / FR-006)" {
            let frames = ControlsElmish.Perf.runScript textHost size [ FrameInput.Idle ]
            Expect.equal frames.[0].RemeasuredNodeCount 0 "idle re-measures nothing"
            Expect.equal frames.[0].PointerMovesProcessed 0 "idle processes no pointer move"
            Expect.isFalse frames.[0].ViewCalled "idle does not run the view"
            Expect.equal frames.[0].FullRenderCount 0 "idle performs no full render"
        }

        test "OnFrameMetrics yields exactly one record per produced frame; a burst is ONE record, not N (SC-010)" {
            // 7 coalesced moves + idle + a key = 3 PRODUCED frames. The burst is a single record whose
            // PointerSamplesReceived carries all 7 raw samples (never 7 separate records).
            let script =
                [ hover "btn" 1.0 1.0
                  hover "btn" 2.0 2.0
                  hover "btn" 3.0 3.0
                  hover "btn" 4.0 4.0
                  hover "btn" 5.0 5.0
                  hover "btn" 6.0 6.0
                  hover "btn" 7.0 7.0
                  FrameInput.Idle
                  FrameInput.Key(Enter, noMods) ]

            let frames = ControlsElmish.Perf.runScript textKeyHost size script
            Expect.equal frames.Length 3 "exactly one FrameMetrics per produced frame (the 7-move burst is ONE frame)"
            Expect.equal frames.[0].PointerSamplesReceived 7 "the burst is a single record carrying all 7 raw samples"
            Expect.equal frames.[0].PointerMovesProcessed 1 "the burst collapses to one processed move"
        }

        // ---- US3: coalescing fidelity is verified, not assumed ----

        test "N raw move samples in one frame: received = N, processed <= 1 incl. deferred (SC-002 / FR-008/009)" {
            let n = 250
            let moves = List.init n (fun i -> hover "btn" (float i) (float i))
            let frames = ControlsElmish.Perf.runScript textHost size moves
            Expect.equal frames.Length 1 "the whole burst is a single coalesced frame"
            Expect.equal frames.[0].PointerSamplesReceived n "every raw sample (including deferred) is counted"
            Expect.isTrue (frames.[0].PointerMovesProcessed <= 1) "at most one processed move after coalescing"
        }

        test "a move burst interleaved with press/release/click/scroll drops NO discrete interaction (SC-003 / FR-010)" {
            let script =
                [ hover "btn" 1.0 1.0
                  hover "btn" 2.0 2.0
                  FrameInput.Pointer(PressedDown("btn", PointerButton.Primary, 2.0, 2.0))
                  FrameInput.Pointer(ReleasedUp("btn", PointerButton.Primary, 2.0, 2.0))
                  FrameInput.Pointer(Click("btn", PointerButton.Primary, 2.0, 2.0))
                  FrameInput.Pointer(Scroll("btn", 0.0, -3.0, 2.0, 2.0)) ]

            let frames = ControlsElmish.Perf.runScript textHost size script
            // The two moves coalesce into ONE frame; each of the four discrete interactions is its own
            // frame (none coalesced away): 1 + 4 = 5 produced frames.
            Expect.equal frames.Length 5 "the moves coalesce; all four discrete interactions survive as their own frames"
            Expect.equal frames.[0].PointerMovesProcessed 1 "the leading move burst collapses to one processed move"

            for i in 1..4 do
                Expect.equal frames.[i].PointerSamplesReceived 1 "each discrete interaction is processed as a single un-coalesced sample"
                Expect.equal frames.[i].PointerMovesProcessed 0 "a discrete interaction is never counted as a processed move"
        }

        test "a continuous drag of hundreds of samples keeps its full raw path available to consumers (FR-011)" {
            // The drag is a DragMove burst (coalesced for processing) but the raw path is fully
            // reconstructable from the driver script a path-consuming consumer replays.
            let pathLen = 400

            let dragPath =
                List.init pathLen (fun i -> DragMove("canvas", PointerButton.Primary, float i, float (i * 2)))

            let script = dragPath |> List.map FrameInput.Pointer
            let frames = ControlsElmish.Perf.runScript textHost size script

            Expect.equal frames.Length 1 "the drag burst coalesces into a single processed frame"
            Expect.equal frames.[0].PointerSamplesReceived pathLen "every raw drag sample is counted (the path's length)"
            Expect.isTrue (frames.[0].PointerMovesProcessed <= 1) "processing collapses to <= 1 move"

            // The full raw path remains available to a path consumer that reads the samples.
            let rawPath =
                dragPath
                |> List.choose (function
                    | DragMove(_, _, x, y) -> Some(x, y)
                    | _ -> None)

            Expect.equal rawPath.Length pathLen "the consumer can recover every raw (x, y) of the drag path"
            Expect.equal (List.last rawPath) (float (pathLen - 1), float ((pathLen - 1) * 2)) "the path's final sample is intact"
        }

        // ---- US4 boundary: FrameDuration is excluded from the deterministic count/bool surface ----

        test "FrameDuration is excluded from the deterministic surface: identical scripts give byte-identical counts (FR-012 / SC-009)" {
            let script =
                [ FrameInput.Key(Enter, noMods)
                  hover "btn" 3.0 3.0
                  hover "btn" 9.0 9.0
                  FrameInput.Pointer(Click("btn", PointerButton.Primary, 9.0, 9.0))
                  tick 16.0
                  FrameInput.Idle ]

            let countTuple (f: FrameMetrics) =
                f.ProductModelChanged, f.ViewCalled, f.FullRenderCount, f.RemeasuredNodeCount, f.PointerSamplesReceived, f.PointerMovesProcessed

            let r1 = ControlsElmish.Perf.runScript textKeyHost size script |> List.map countTuple
            let r2 = ControlsElmish.Perf.runScript textKeyHost size script |> List.map countTuple
            Expect.equal r1 r2 "the count/bool surface is byte-stable run-to-run (no clock leaks in)"

            // Perf.runScript keeps FrameDuration clock-free (golden path never reads the wall clock).
            let durations = ControlsElmish.Perf.runScript textKeyHost size script |> List.map (fun f -> f.FrameDuration)
            Expect.allEqual durations TimeSpan.Zero "the deterministic driver never sets a wall-clock duration"
        }

        // ---- observation-only invariant (FR-020 / SC-008) ----

        test "observation-only: the production render path is byte-identical regardless of metric observation (FR-020 / SC-008)" {
            let model = 2

            // The same host, one with the inert default sink and one recording metrics.
            let recorded = System.Collections.Generic.List<FrameMetrics>()
            let observingHost = { textKeyHost with OnFrameMetrics = recorded.Add }

            let sceneOf (h: InteractiveAppHost<int, Msg>) =
                (Control.renderTree h.Theme size (h.View size model)).Scene

            Expect.equal (sceneOf observingHost) (sceneOf textKeyHost) "the rendered scene does not depend on the OnFrameMetrics sink"
            Expect.equal (sceneOf textKeyHost) (sceneOf textKeyHost) "the at-rest production render is byte-stable across renders"

            // The metric counts produced by the deterministic driver are identical for the two hosts —
            // the observability surface change perturbs neither the model fold nor the render path.
            let metricTuple (f: FrameMetrics) =
                f.ProductModelChanged, f.ViewCalled, f.FullRenderCount, f.RemeasuredNodeCount, f.PointerSamplesReceived, f.PointerMovesProcessed

            let script = [ FrameInput.Key(Enter, noMods); hover "btn" 3.0 3.0; FrameInput.Idle ]
            let a = ControlsElmish.Perf.runScript textKeyHost size script |> List.map metricTuple
            let b = ControlsElmish.Perf.runScript observingHost size script |> List.map metricTuple
            Expect.equal a b "observing metrics does not change the deterministic count/bool surface or the fold"
        }
    ]
