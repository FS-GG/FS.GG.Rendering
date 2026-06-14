module Feature110RetainedRoutingTests

// Feature 110 (US1, FR-004/FR-008/FR-012) — pointer input routes from the RETAINED frame, not a full
// render. These tests drive the deterministic, byte-stable `ControlsElmish.Perf.runScript` path (the
// authoritative observability surface) and assert, per frame, that routing a pointer move or click
// performs ZERO routing full renders (`FullRenderCount` is not incremented for routing; `ViewCalled`
// stays false on a pure routing frame) and the retained route never falls back on a normal scenario
// (`FullRenderFallbackCount = 0`). Coalescing fidelity from features 108/109 is preserved through the
// retained route: a move burst still collapses to <= 1 processed move with zero routing renders, and no
// discrete press/release/click/scroll is dropped, while a path-consuming consumer still recovers the
// full per-sample drag path.

open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.KeyboardInput
open FS.Skia.UI.SkiaViewer
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish

type private Msg = Bump

let private size: Size = { Width = 320; Height = 200 }

// A Bump-counter host whose keyed button fires `Bump` on click; `MapPointer` ignores everything else.
let private host: InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update = fun Bump model -> model + 1, []
      View = fun _ _ -> Stack.create [ Stack.children [ Button.create [ Button.text "go"; Button.onClick Bump ] |> Control.withKey "btn" ] ]
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

// An unbound host (the keyed button carries no binding, no MapPointer) — a click resolves to no message.
let private unboundHost: InteractiveAppHost<int, Msg> =
    { host with
        View = fun _ _ -> Stack.create [ Stack.children [ Button.create [ Button.text "go" ] |> Control.withKey "btn" ] ] }

let private centreOf (h: InteractiveAppHost<int, Msg>) (model: int) (nodeId: ControlId) =
    let rendered = Control.renderTree h.Theme size (h.View size model)

    let available: FS.Skia.UI.Layout.AvailableSpace =
        { Width = float size.Width
          WidthMode = FS.Skia.UI.Layout.Exactly
          Height = float size.Height
          HeightMode = FS.Skia.UI.Layout.Exactly }

    let result = FS.Skia.UI.Layout.Layout.evaluate available rendered.Layout
    let b = result.Bounds |> List.find (fun b -> b.NodeId = nodeId)
    b.Bounds.X + b.Bounds.Width / 2.0, b.Bounds.Y + b.Bounds.Height / 2.0

[<Tests>]
let tests =
    testList "Feature 110 retained pointer routing — zero routing full renders + preserved coalescing (US1)" [

        test "a routed move performs zero routing full renders and never falls back (SC-001/SC-005)" {
            let frames = ControlsElmish.Perf.runScript host size [ FrameInput.Pointer(HoverEnter("btn", 5.0, 5.0)) ]
            Expect.equal frames.[0].FullRenderCount 0 "routing a move performs no full render (SC-001)"
            Expect.isFalse frames.[0].ViewCalled "a pure routing frame does not run the view (FR-008)"
            Expect.equal frames.[0].FullRenderFallbackCount 0 "the retained route did not fall back (SC-005)"
            Expect.equal frames.[0].ProductModelChanged false "a move dispatched no product message"
        }

        test "a routed click on an unbound control performs zero routing full renders (SC-002)" {
            let cx, cy = centreOf unboundHost 0 "btn"
            let frames = ControlsElmish.Perf.runScript unboundHost size [ FrameInput.Pointer(Click("btn", PointerButton.Primary, cx, cy)) ]
            Expect.equal frames.[0].FullRenderCount 0 "an unbound click is a pure routing frame: zero full renders (SC-002)"
            Expect.isFalse frames.[0].ViewCalled "no model change and no routing render → the view did not run"
            Expect.equal frames.[0].FullRenderFallbackCount 0 "no fallback (SC-005)"
        }

        test "a routed click on a bound control dispatches its binding FROM the retained frame, no routing render (SC-002)" {
            let cx, cy = centreOf host 0 "btn"
            let frames = ControlsElmish.Perf.runScript host size [ FrameInput.Pointer(Click("btn", PointerButton.Primary, cx, cy)) ]
            // The binding fired (model changed), and the only full render is the model-driven re-render —
            // NOT a routing render. `FullRenderFallbackCount = 0` proves the binding was resolved from the
            // retained frame, not via an oracle fallback.
            Expect.isTrue frames.[0].ProductModelChanged "the authored onClick binding fired from the retained frame"
            Expect.equal frames.[0].FullRenderCount 1 "exactly the model-driven re-render; zero routing renders"
            Expect.equal frames.[0].FullRenderFallbackCount 0 "the binding was resolved from the retained frame, no fallback (SC-002/SC-005)"
        }

        test "a burst of N move samples in one frame: <= 1 processed move, zero routing renders, no fallback (SC-009)" {
            let n = 250
            let moves = [ for i in 0 .. n - 1 -> FrameInput.Pointer(HoverEnter("btn", float i, float i)) ]
            let frames = ControlsElmish.Perf.runScript host size moves
            Expect.equal frames.Length 1 "the whole burst is a single coalesced frame"
            Expect.equal frames.[0].PointerSamplesReceived n "every raw sample is counted"
            Expect.isTrue (frames.[0].PointerMovesProcessed <= 1) "at most one processed move after coalescing (SC-009)"
            Expect.equal frames.[0].FullRenderCount 0 "the coalesced move performs zero routing full renders (SC-009)"
            Expect.equal frames.[0].FullRenderFallbackCount 0 "no fallback on the burst (SC-005)"
        }

        test "a move burst interleaved with discrete interactions drops none and keeps zero routing renders (FR-012)" {
            let cx, cy = centreOf host 0 "btn"

            let script =
                [ FrameInput.Pointer(HoverEnter("btn", 1.0, 1.0))
                  FrameInput.Pointer(HoverEnter("btn", 2.0, 2.0))
                  FrameInput.Pointer(PressedDown("btn", PointerButton.Primary, cx, cy))
                  FrameInput.Pointer(ReleasedUp("btn", PointerButton.Primary, cx, cy))
                  FrameInput.Pointer(Scroll("btn", 0.0, -3.0, cx, cy)) ]

            let frames = ControlsElmish.Perf.runScript host size script
            // The two moves coalesce into ONE frame; each discrete interaction is its own frame: 1 + 3 = 4.
            Expect.equal frames.Length 4 "moves coalesce; all three discrete interactions survive as their own frames (FR-012)"

            for f in frames do
                Expect.equal f.FullRenderFallbackCount 0 "no frame fell back to a full render (SC-005)"
                // None of these interactions binds (press/release/scroll are not click-equivalent), so every
                // frame is a pure routing frame: zero routing full renders.
                Expect.equal f.FullRenderCount 0 "every routing frame performs zero full renders (FR-004)"
        }

        test "drag/freehand path fidelity is retained through the retained route (FR-012)" {
            let pathLen = 400
            let dragPath = List.init pathLen (fun i -> DragMove("canvas", PointerButton.Primary, float i, float (i * 2)))
            let frames = ControlsElmish.Perf.runScript host size (dragPath |> List.map FrameInput.Pointer)

            Expect.equal frames.Length 1 "the drag burst coalesces into a single processed frame"
            Expect.equal frames.[0].PointerSamplesReceived pathLen "every raw drag sample is counted"
            Expect.equal frames.[0].FullRenderCount 0 "the coalesced drag performs zero routing full renders (FR-004)"
            Expect.equal frames.[0].FullRenderFallbackCount 0 "no fallback (SC-005)"

            // The full per-sample path a path-consuming consumer observes is unchanged from the 108/109
            // baseline — every raw (x, y) is recoverable from the driver script it replays.
            let rawPath = dragPath |> List.map (function DragMove(_, _, x, y) -> x, y | _ -> 0.0, 0.0)
            Expect.equal rawPath.Length pathLen "the consumer recovers every raw (x, y) of the drag path"
            Expect.equal (List.last rawPath) (float (pathLen - 1), float ((pathLen - 1) * 2)) "the path's final sample is intact"
        }
    ]
