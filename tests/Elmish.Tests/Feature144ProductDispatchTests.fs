module Feature144ProductDispatchTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open Feature144OverlayDispatchFixtures

[<Tests>]
let tests =
    testList "Feature144 product dispatch" [
        test "selection dispatches exactly once and may also close the surface" {
            let opened, _ = OverlayState.update (OpenRequested(surface TransientSurfaceKind.Menu "dispatch-menu")) (OverlayState.init ())

            let selected, effects =
                OverlayState.update
                    (SelectionCompleted("dispatch-menu", "dispatch-menu:item-1", Some "item-1"))
                    opened

            let commands = ControlsElmish.interpretOverlayOutcome mapOpen mapDispatch mapFocus effects
            let messages = AdapterCmd.productMessages commands

            Expect.equal selected.OpenSurfaces [] "selection closes by default"
            Expect.equal (messages |> List.filter (function PayloadDispatched("dispatch-menu", Some "item-1") -> true | _ -> false)).Length 1 "selection dispatched once"
            Expect.contains messages (OpenChanged("dispatch-menu", false)) "selection close request remains product-visible"
        }

        test "duplicate dispatch attempts are diagnosed and excluded" {
            let opened, _ = OverlayState.update (OpenRequested(surface TransientSurfaceKind.Menu "duplicate-menu")) (OverlayState.init ())
            let selected, _ = OverlayState.update (SelectionCompleted("duplicate-menu", "dup", Some "A")) opened
            let _, duplicateEffects = OverlayState.update (SelectionCompleted("duplicate-menu", "dup", Some "A")) selected

            Expect.exists duplicateEffects (function ReportOverlayDiagnostic diagnostic when diagnostic.Code = DuplicateOverlayDispatch -> true | _ -> false) "duplicate diagnostic emitted"
            Expect.isFalse (duplicateEffects |> List.exists (function OverlayEffect.DispatchProductMessage _ -> true | _ -> false)) "duplicate product dispatch suppressed"
        }

        test "focus requests are exposed to both runtime and product mapping" {
            let commands =
                ControlsElmish.interpretOverlayEffect
                    mapOpen
                    mapDispatch
                    mapFocus
                    (RequestFocus(Some "dispatch-trigger"))

            Expect.exists commands (function DispatchControlRuntimeMessage(FocusControl(Some "dispatch-trigger")) -> true | _ -> false) "runtime focus message emitted"
            Expect.contains (AdapterCmd.productMessages commands) (FocusRequested(Some "dispatch-trigger")) "product focus message emitted"
        }
    ]
