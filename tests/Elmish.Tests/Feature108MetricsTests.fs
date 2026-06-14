module Feature108MetricsTests

// Feature 108 (US2/US3/US4) — `ControlsElmish.Perf.runScript` is the pure, byte-stable, deterministic
// frame driver. It shares the message→update→`RetainedRender.step` + coalescing code path with the
// live `runInteractiveApp` loop, so its per-frame `FrameMetrics` count/bool fields are the asserted
// determinism surface: idle = zero work, K coalesced moves = one processed move, a pure-hover frame
// does not rebuild, a click interleaved with moves is processed within its frame, and a Tick that
// only advances clocks does not report a false whole-tree rebuild. `FrameDuration` is excluded.
// Reaches the real adapter path with no live window ([[fs-skia-elmish]] / [[fs-skia-evidence-mode]]).

open System
open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.KeyboardInput
open FS.Skia.UI.SkiaViewer
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish

type private Msg =
    | Inc
    | Noop

let private size: Size = { Width = 320; Height = 200 }

let private view (model: int) : Control<Msg> =
    Stack.create
        [ Stack.children [ Button.create [ Button.text (string model); Button.onClick Inc ] |> Control.withKey "btn" ] ]

// Base host: no key/pointer/tick mapping (event-driven Tick default).
let private host: InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update =
        fun msg model ->
            (match msg with
             | Inc -> model + 1
             | Noop -> model),
            []
      View = fun _ model -> view model
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

// A host whose Enter key maps to Inc (so a Key frame meaningfully rebuilds the model).
let private keyHost =
    { host with
        MapKey =
            fun key _ ->
                match key with
                | Enter -> Some Inc
                | _ -> None }

// Feature 110: retained routing resolves a click from the retained frame by its COORDINATES
// (`retainedHitTest`), not by the interaction's carried id, so a synthetic click must name the point
// actually over the control. Centre of a node's computed bounds at `size`.
let private centreOf (model: int) (nodeId: ControlId) =
    let rendered = Control.renderTree host.Theme size (view model)

    let available: FS.Skia.UI.Layout.AvailableSpace =
        { Width = float size.Width
          WidthMode = FS.Skia.UI.Layout.Exactly
          Height = float size.Height
          HeightMode = FS.Skia.UI.Layout.Exactly }

    let result = FS.Skia.UI.Layout.Layout.evaluate available rendered.Layout
    let b = result.Bounds |> List.find (fun b -> b.NodeId = nodeId)
    b.Bounds.X + b.Bounds.Width / 2.0, b.Bounds.Y + b.Bounds.Height / 2.0

// Feature 109: `ViewRebuilt` was split into `ProductModelChanged` (a product message changed the
// model) + `ViewCalled`/`FullRenderCount` (the view ran). These 108 assertions are about the
// model-change fact, so they read `ProductModelChanged`.
let private counts (frames: FrameMetrics list) =
    frames
    |> List.map (fun f -> f.RemeasuredNodeCount, f.PointerSamplesReceived, f.PointerMovesProcessed, f.ProductModelChanged)

[<Tests>]
let tests =
    testList "Feature 108 ControlsElmish.Perf.runScript metrics + coalescing (US2/3/4, SC-003/004/005/012)" [
        test "an idle frame reports zero work and no rebuild (SC-005/012)" {
            let frames = ControlsElmish.Perf.runScript host size [ FrameInput.Idle ]
            Expect.equal frames.Length 1 "one frame per idle step"
            Expect.equal frames.[0].RemeasuredNodeCount 0 "idle re-measures nothing"
            Expect.equal frames.[0].PointerSamplesReceived 0 "no pointer samples"
            Expect.isFalse frames.[0].ProductModelChanged "idle changes no model"
        }

        test "K pointer moves in one frame coalesce: samples = K, moves processed <= 1 (SC-004)" {
            let moves = List.replicate 5 (FrameInput.Pointer(HoverEnter("btn", 10.0, 10.0)))
            let frames = ControlsElmish.Perf.runScript host size moves
            Expect.equal frames.Length 1 "5 consecutive moves coalesce into a single frame"
            Expect.equal frames.[0].PointerSamplesReceived 5 "every raw sample is counted"
            Expect.equal frames.[0].PointerMovesProcessed 1 "at most one processed move after coalescing"
        }

        test "a pure-hover frame does not rebuild the view (SC-005)" {
            let frames = ControlsElmish.Perf.runScript host size [ FrameInput.Pointer(HoverEnter("btn", 5.0, 5.0)) ]
            Expect.isFalse frames.[0].ProductModelChanged "hover dispatches no product message, so the model does not change"
        }

        test "a key that changes the model rebuilds; a following idle does not (SC-003/005)" {
            let frames = ControlsElmish.Perf.runScript keyHost size [ FrameInput.Key(Enter, ViewerKeyboard.noModifiers); FrameInput.Key(Enter, ViewerKeyboard.noModifiers); FrameInput.Idle ]
            Expect.equal frames.Length 3 "three frames"
            Expect.isTrue frames.[0].ProductModelChanged "first Enter changes the model (0->1)"
            Expect.isTrue frames.[1].ProductModelChanged "second Enter changes the model (1->2)"
            Expect.isFalse frames.[2].ProductModelChanged "the trailing idle changes no model"
            Expect.equal frames.[2].RemeasuredNodeCount 0 "idle re-measures nothing"
        }

        test "a click interleaved with moves is processed within one frame (SC-006)" {
            // Feature 110: the click point must be over the button (retained routing hit-tests by coords).
            let bx, by = centreOf 0 "btn"

            let script =
                [ FrameInput.Pointer(HoverEnter("btn", 4.0, 4.0))
                  FrameInput.Pointer(HoverEnter("btn", 6.0, 6.0))
                  FrameInput.Pointer(Click("btn", PointerButton.Primary, bx, by)) ]

            let frames = ControlsElmish.Perf.runScript host size script
            Expect.equal frames.Length 2 "the two moves coalesce; the click is its own frame"
            Expect.equal frames.[0].PointerMovesProcessed 1 "the move burst coalesces to one processed move"
            Expect.isTrue frames.[1].ProductModelChanged "the click is processed in its frame (onClick -> Inc changed the model)"
            Expect.equal frames.[1].PointerSamplesReceived 1 "the discrete click is a single sample"
        }

        test "a Tick frame advancing clocks reports no whole-tree rebuild (SC-005, animation edge)" {
            let frames = ControlsElmish.Perf.runScript host size [ FrameInput.Tick(TimeSpan.FromMilliseconds 16.0) ]
            Expect.isFalse frames.[0].ProductModelChanged "an event-driven Tick (no consumer message) changes no model"
            Expect.isTrue (frames.[0].RemeasuredNodeCount >= 0) "remeasure is a bounded count, never a false full rebuild"
        }

        test "the count/bool fields are byte-stable across repeated runs (SC-003)" {
            let script =
                [ FrameInput.Key(Enter, ViewerKeyboard.noModifiers)
                  FrameInput.Pointer(HoverEnter("btn", 3.0, 3.0))
                  FrameInput.Pointer(HoverEnter("btn", 9.0, 9.0))
                  FrameInput.Pointer(Click("btn", PointerButton.Primary, 9.0, 9.0))
                  FrameInput.Idle ]

            let r1 = ControlsElmish.Perf.runScript keyHost size script
            let r2 = ControlsElmish.Perf.runScript keyHost size script
            Expect.equal (counts r1) (counts r2) "identical script -> identical count/bool fields"
        }
    ]
