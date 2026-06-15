module Feature110FallbackTests

// Feature 110 (US3, FR-007/FR-009) — the full-render path survives ONLY as a parity oracle and a counted
// diagnostic fallback, never the normal live route. Two facts are proven:
//   - SC-005: every normal scripted pointer scenario reports `FullRenderFallbackCount = 0` for every frame
//     (the retained route resolves everything from the retained frame);
//   - SC-006: a DELIBERATELY constructed unroutable case increments `FullRenderFallbackCount` by exactly
//     one and the fallback STILL dispatches identically to the oracle.
// The forced-fallback case is REAL, not synthetic: the retained frame is genuinely stale (laid out at a
// 1x1 extent, so its cached boxes cannot contain the click point), so `retainedHitTest` legitimately
// returns `None` over a point a `Click` named; the fallback then runs the PRESERVED oracle
// (`Control.renderTree` + `bindingMessagesFor` — real product code) over the real current tree.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish

type private Msg = Bump

let private size: Size = { Width = 320; Height = 200 }

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

let private centreOf (model: int) (nodeId: ControlId) =
    let rendered = Control.renderTree host.Theme size (host.View size model)

    let available: FS.GG.UI.Layout.AvailableSpace =
        { Width = float size.Width
          WidthMode = FS.GG.UI.Layout.Exactly
          Height = float size.Height
          HeightMode = FS.GG.UI.Layout.Exactly }

    let result = FS.GG.UI.Layout.Layout.evaluate available rendered.Layout
    let b = result.Bounds |> List.find (fun b -> b.NodeId = nodeId)
    b.Bounds.X + b.Bounds.Width / 2.0, b.Bounds.Y + b.Bounds.Height / 2.0

[<Tests>]
let tests =
    testList "Feature 110 counted full-render fallback (US3, FR-007/FR-009, SC-005/SC-006)" [

        test "every normal scripted pointer scenario reports FullRenderFallbackCount = 0 for every frame (SC-005)" {
            let cx, cy = centreOf 0 "btn"

            let scripts: FrameInput<Msg> list list =
                [ [ FrameInput.Pointer(HoverEnter("btn", 5.0, 5.0)) ]
                  [ for i in 0 .. 99 -> FrameInput.Pointer(HoverEnter("btn", float i, float i)) ]
                  [ for i in 0 .. 199 -> FrameInput.Pointer(DragMove("canvas", PointerButton.Primary, float i, float (i * 2))) ]
                  [ FrameInput.Pointer(Click("btn", PointerButton.Primary, cx, cy)) ]
                  [ FrameInput.Pointer(PressedDown("btn", PointerButton.Primary, cx, cy))
                    FrameInput.Pointer(ReleasedUp("btn", PointerButton.Primary, cx, cy)) ] ]

            for script in scripts do
                let frames = ControlsElmish.Perf.runScript host size script

                for f in frames do
                    Expect.equal f.FullRenderFallbackCount 0 "a normal scripted pointer scenario never falls back (SC-005)"
        }

        test "a deliberately unroutable case increments FullRenderFallbackCount by one and the fallback matches the oracle (SC-006)" {
            let model = 0
            let cx, cy = centreOf model "btn"

            // A STALE retained frame: laid out at a 1x1 extent, so its cached boxes cannot contain the real
            // click point (real product code, not a mock — `RetainedRender.init` over a genuine tiny layout).
            let stale = RetainedRender.init host.Theme { Width = 1; Height = 1 } (host.View { Width = 1; Height = 1 } model)

            // The retained route cannot resolve the bindable click from the stale frame, so it falls back to
            // the preserved oracle over the REAL current tree.
            let msgs, fallbacks = ControlsElmish.routeRetainedInteraction host size model stale.Retained stale.Render (Click("btn", PointerButton.Primary, cx, cy))

            Expect.equal fallbacks 1 "the unresolvable bindable hit incremented FullRenderFallbackCount by exactly one (FR-009/SC-006)"

            // The fallback dispatch equals the oracle's resolution over the real tree (the fallback IS the oracle).
            let rendered = Control.renderTree host.Theme size (host.View size model)

            let oracle =
                match Control.nearestAuthored rendered "btn" with
                | Some _ ->
                    rendered.EventBindings
                    |> List.filter (fun b -> b.ControlId = "btn" && b.EventKind = "click")
                    |> List.map (fun b -> b.Dispatch { Kind = "click"; ControlId = Some "btn"; Origin = ControlEventOrigin.Pointer; Payload = None; Nav = None })
                | None -> []

            Expect.equal msgs oracle "the fallback dispatched identically to the oracle (FR-007/SC-006)"
            Expect.equal msgs [ Bump ] "the authored binding still fired through the fallback"
        }

        test "a retained frame that DOES contain the hit resolves with no fallback (the fallback is an escape hatch only)" {
            let model = 0
            let cx, cy = centreOf model "btn"
            let fresh = RetainedRender.init host.Theme size (host.View size model)
            let msgs, fallbacks = ControlsElmish.routeRetainedInteraction host size model fresh.Retained fresh.Render (Click("btn", PointerButton.Primary, cx, cy))
            Expect.equal fallbacks 0 "a current retained frame resolves the hit with no fallback"
            Expect.equal msgs [ Bump ] "the binding fired directly from the retained frame"
        }
    ]
