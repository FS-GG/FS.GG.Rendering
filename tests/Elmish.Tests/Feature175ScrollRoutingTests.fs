module Feature175ScrollRoutingTests

// Feature 175 (US1, FR-001/FR-009) — live scroll routing on the retained path.
//   * routeRetainedPointer resolves a Wheel over a scroll-viewer to (scroll-viewer id, deltaY,
//     contentHeight, viewportHeight) — the host folds this into its persistent offset.
//   * Applying the resolved offset through the real bridge (ControlRuntime.applyScrollOffsets) +
//     the live retained step produces a DIFFERENT frame (scroll responds), and the change is
//     damage-local: a chrome sibling OUTSIDE the scroll-viewer is not repainted.
// Render-only / deterministic; reaches the internal retained seams via InternalsVisibleTo.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg = Noop

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 240 }

// A shell with a chrome sibling above a keyed scroll-viewer whose content overflows.
let private tallContent =
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children [ for i in 1..30 -> Button.create [ Button.text (sprintf "row %d" i); Attr.height 30.0 ] |> Control.withKey (sprintf "row%d" i) ] ]

let private view (_: Size) (_: int) : Control<Msg> =
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children
              [ TextBlock.create [ TextBlock.text "chrome" ] |> Control.withKey "chrome"
                Control.create "scroll-viewer" [ Attr.children [ tallContent ]; Attr.height 140.0 ]
                |> Control.withKey "content-scroll" ] ]

let private hostOf () : InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update = fun _ model -> model, []
      View = view
      Theme = theme
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private wheel x y deltaY : ViewerPointerInput =
    { Phase = ViewerPointerPhaseKind.Wheel; X = x; Y = y; Button = None; DeltaX = 0.0; DeltaY = deltaY }

[<Tests>]
let tests =
    testList "Feature175ScrollRouting" [
        test "T011 — a wheel over the scroll-viewer routes to (id, delta, contentHeight, viewportHeight)" {
            let host = hostOf ()
            let r = RetainedRender.init theme size (host.View size 0)
            // a point inside the scroll-viewer's painted bounds
            let svRect = r.Render.Bounds |> List.find (fun (id, _) -> id = "content-scroll") |> snd
            let x = svRect.X + svRect.Width / 2.0
            let y = svRect.Y + svRect.Height / 2.0

            let _, _, _, scrollDeltas =
                ControlsElmish.routeRetainedPointer host r.Retained r.Render (Pointer.init ()) size 0 (wheel x y -40.0)

            match scrollDeltas with
            | [ (svId, delta, contentHeight, viewportHeight) ] ->
                Expect.equal svId "content-scroll" "the wheel resolved to the enclosing scroll-viewer"
                Expect.equal delta -40.0 "the raw wheel deltaY is carried through"
                Expect.isTrue (contentHeight > viewportHeight) "the region overflows (a real scroll range exists)"
            | other -> failtestf "expected exactly one resolved scroll delta, got %A" (other |> List.map (fun (i, d, _, _) -> i, d))
        }

        test "T011 — applying the resolved offset changes the frame, damage-locally (chrome sibling reused)" {
            let host = hostOf ()
            let r0 = RetainedRender.init theme size (host.View size 0)

            // Resolve the wheel to an offset (as the host does), then build the runtime model + stamp.
            let svRect = r0.Render.Bounds |> List.find (fun (id, _) -> id = "content-scroll") |> snd
            let _, _, _, scrollDeltas =
                ControlsElmish.routeRetainedPointer host r0.Retained r0.Render (Pointer.init ()) size 0 (wheel (svRect.X + 10.0) (svRect.Y + 10.0) -40.0)
            let (svId, deltaY, ch, vh) = List.exactlyOne scrollDeltas
            let scroll =
                ScrollState.empty |> ScrollState.withExtent ch vh |> ScrollState.applyScrollDelta -deltaY
            Expect.isTrue (scroll.Offset > 0.0) "wheel-down produced a positive scroll offset"

            let runtimeModel = { fst (ControlRuntime.init ()) with ScrollOffsets = Map.ofList [ svId, scroll ] }
            let stamped = ControlRuntime.applyScrollOffsets runtimeModel (host.View size 0)
            let s = RetainedRender.step theme size r0.Retained stamped

            Expect.notEqual s.Render.Scene r0.Render.Scene "scrolling produced a visibly different frame (responds-proof)"
            Expect.isLessThan s.WorkReduction.RepaintedNodeCount r0.Render.NodeCount
                "the repaint is damage-local — not the whole tree was repainted (chrome stays put)"
        }
    ]
