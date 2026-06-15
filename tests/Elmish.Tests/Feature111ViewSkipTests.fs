module Feature111ViewSkipTests

// Feature 111 (US3, FR-003/FR-004) — frames run only the phases their cause requires: a frame whose
// cause did not change the product model performs NO `host.View` (the view phase is skipped) while the
// rendered output stays byte-identical. These tests drive `ControlsElmish.Perf.runScript` for the metric
// facts and exercise the internal `RetainedRender` step directly for the byte-identity of the view-skip
// mechanism (reusing the retained tree == a fresh `host.View` of the unchanged model). They also cover
// the FR-006/SC-008 frame-rate-work clause (continuous drag + continuous animation, coalescing preserved).

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg = Bump

let private size: Size = { Width = 320; Height = 200 }
let private noMods = ViewerKeyboard.noModifiers
let private tick (ms: float) = FrameInput.Tick(TimeSpan.FromMilliseconds ms)
let private key () = FrameInput.Key(Enter, noMods)

let private animView (model: int) : Control<Msg> =
    Stack.create [ Stack.children [ Switch.create [ Attr.visualState (if model >= 2 then Hover else Normal) ] |> Control.withKey "sw" ] ]

let private geomView (model: int) : Control<Msg> =
    Stack.create
        [ Stack.orientation (if model % 2 = 0 then "vertical" else "horizontal")
          Stack.children [ Button.create [ Button.text (string model) ] |> Control.withKey "b" ] ]

let private mkHost (view: int -> Control<Msg>) : InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update = fun Bump model -> model + 1, []
      View = fun _ model -> view model
      Theme = Theme.light
      MapKey = fun k _ -> (match k with | Enter -> Some Bump | _ -> None)
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

[<Tests>]
let tests =
    testList "Feature 111 view-skip + byte-identity (US3, FR-003/FR-004/FR-006/SC-003/SC-007/SC-008)" [

        test "an animation-only tick performs no host.View (ViewCalled false, FullRenderCount 0) (SC-003)" {
            let frames = ControlsElmish.Perf.runScript (mkHost animView) size [ key (); key (); tick 16.0 ]
            let t = frames.[2]
            Expect.isFalse t.ViewCalled "the view phase is skipped on an animation-only tick"
            Expect.equal t.FullRenderCount 0 "zero full renders (the overlay re-sample is not a host.View materialization)"
            Expect.isTrue t.PaintRan "the paint phase still ran"
        }

        test "a model-changing frame still runs the view (SC-004)" {
            let frames = ControlsElmish.Perf.runScript (mkHost geomView) size [ key (); key () ]
            Expect.isTrue frames.[1].ViewCalled "a model change runs host.View"
            Expect.equal frames.[1].FullRenderCount 1 "exactly one full render for the model change"
        }

        test "byte-identity: the retained tree equals a fresh host.View of the unchanged model (FR-003)" {
            // Reach the Hover state (a live clock), then advance it — exactly the state a Tick frame is in.
            let theme = Theme.light
            let r0 = RetainedRender.init theme size (animView 1)
            let s1 = RetainedRender.step theme size r0.Retained (animView 2)

            let advanced =
                { s1.Retained with
                    StateByIdentity =
                        s1.Retained.StateByIdentity
                        |> Map.map (fun _ st -> { st with Animation = st.Animation |> Option.map (RetainedRender.advance (TimeSpan.FromMilliseconds 16.0)) }) }

            // The retained tree `repaintCached` reuses == a fresh `host.View` of the unchanged model
            // (Control has no general equality, so compare structurally via %A — the established technique).
            Expect.equal
                (sprintf "%A" advanced.Root.Control)
                (sprintf "%A" (animView 2))
                "prev.Root.Control equals host.View of the unchanged model (the reuse is byte-identical)"

            // And the painted scene is byte-identical whether the view was re-run or reused (Scene has equality).
            let sceneViewSkipped = (RetainedRender.step theme size advanced advanced.Root.Control).Render.Scene
            let sceneViewReRun = (RetainedRender.step theme size advanced (animView 2)).Render.Scene
            Expect.equal sceneViewSkipped sceneViewReRun "the view-skipped overlay is byte-identical to the view-re-run overlay (SC-007)"
        }

        test "continuous drag is frame-rate work: <= 1 processed move, zero host.View, none dropped (FR-006/SC-008)" {
            let pathLen = 400
            let drag = [ for i in 0 .. pathLen - 1 -> FrameInput.Pointer(DragMove("canvas", PointerButton.Primary, float i, float (i * 2))) ]
            let frames = ControlsElmish.Perf.runScript (mkHost animView) size drag
            Expect.equal frames.Length 1 "the whole drag coalesces into a single frame"
            Expect.isTrue (frames.[0].PointerMovesProcessed <= 1) "at most one processed move (coalescing preserved)"
            Expect.equal frames.[0].PointerSamplesReceived pathLen "every raw sample counted (path fidelity)"
            Expect.equal frames.[0].FullRenderCount 0 "zero per-sample host.View rebuilds for the drag"
            Expect.isFalse frames.[0].ViewCalled "no view phase for a pure move frame"
        }

        test "continuous animation is frame-rate work: every tick is view-free (FR-006/SC-008)" {
            // Start the clock (two keys -> Hover), then a run of ticks — each is an animation-only frame.
            let ticks = [ for _ in 1 .. 8 -> tick 4.0 ]
            let frames = ControlsElmish.Perf.runScript (mkHost animView) size (key () :: key () :: ticks)
            // The 8 tick frames are frames.[2..9]; while the clock is live each is view-free.
            let tickFrames = frames |> List.skip 2 |> List.filter (fun f -> f.FrameCause = FrameCause.Tick)
            Expect.isTrue (tickFrames |> List.forall (fun f -> not f.ViewCalled)) "every animation tick skips the view phase"
            Expect.isTrue (tickFrames |> List.forall (fun f -> f.FullRenderCount = 0)) "every animation tick performs zero full renders"
        }

        test "a move burst interleaved with discrete interactions drops none (FR-006)" {
            let script =
                [ FrameInput.Pointer(HoverEnter("sw", 1.0, 1.0))
                  FrameInput.Pointer(HoverEnter("sw", 2.0, 2.0))
                  FrameInput.Pointer(PressedDown("sw", PointerButton.Primary, 2.0, 2.0))
                  FrameInput.Pointer(ReleasedUp("sw", PointerButton.Primary, 2.0, 2.0))
                  FrameInput.Pointer(Scroll("sw", 0.0, -3.0, 2.0, 2.0)) ]

            let frames = ControlsElmish.Perf.runScript (mkHost animView) size script
            Expect.equal frames.Length 4 "the moves coalesce; all three discrete interactions survive as their own frames"
            Expect.equal frames.[0].FrameCause FrameCause.PointerMove "the leading frame is the coalesced move"

            for i in 1..3 do
                Expect.equal frames.[i].FrameCause FrameCause.PointerDiscrete "each discrete interaction is its own PointerDiscrete frame"
        }
    ]
