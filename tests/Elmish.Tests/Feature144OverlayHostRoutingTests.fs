module Feature144OverlayHostRoutingTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open Feature144OverlayDispatchFixtures

[<Tests>]
let tests =
    testList "Feature144 overlay host routing" [
        test "open and focus effects become ordered adapter commands" {
            let opened, effects = OverlayState.update (OpenRequested(surface TransientSurfaceKind.Menu "host-menu")) (OverlayState.init ())
            let commands = ControlsElmish.interpretOverlayOutcome mapOpen mapDispatch mapFocus effects

            Expect.equal opened.ActiveSurface (Some "host-menu") "surface opened"
            Expect.equal
                (AdapterCmd.productMessages commands)
                [ OpenChanged("host-menu", true); FocusRequested(Some "host-menu-item-1") ]
                "host emits product-visible open and focus requests in effect order"
        }

        test "dismissal is product-visible and consumes lower routing" {
            let opened, _ = OverlayState.update (OpenRequested(surface TransientSurfaceKind.ContextMenu "host-context")) (OverlayState.init ())
            let _, effects = OverlayState.update (DismissRequested(None, DismissalReason.Escape)) opened
            let commands = ControlsElmish.interpretOverlayOutcome mapOpen mapDispatch mapFocus effects

            Expect.equal (AdapterCmd.productMessages commands) [ OpenChanged("host-context", false); FocusRequested(Some "host-context-trigger") ] "close and focus recover"
            Expect.isFalse (commands |> List.exists (function DispatchProductMessage(PayloadDispatched(_, _)) -> true | _ -> false)) "dismissal does not dispatch selection"
        }
    ]
