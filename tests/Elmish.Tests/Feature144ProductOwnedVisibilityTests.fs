module Feature144ProductOwnedVisibilityTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open Feature144OverlayDispatchFixtures

[<Tests>]
let tests =
    testList "Feature144 product-owned visibility" [
        test "open request is emitted but product model owns the state transition" {
            let _, effects = OverlayState.update (OpenRequested(surface TransientSurfaceKind.ComboDropdown "owned-combo")) (OverlayState.init ())
            let commands = ControlsElmish.interpretOverlayOutcome mapOpen mapDispatch mapFocus effects

            Expect.contains (AdapterCmd.productMessages commands) (OpenChanged("owned-combo", true)) "open is product-visible"
        }

        test "close request is emitted without hidden runtime visibility state" {
            let opened, _ = OverlayState.update (OpenRequested(surface TransientSurfaceKind.DatePickerCalendar "owned-date")) (OverlayState.init ())
            let _, effects = OverlayState.update (DismissRequested(Some "owned-date", DismissalReason.ExplicitClose)) opened
            let records = ControlRuntime.overlayDispatchRecords (ControlRuntime.attachOverlayEffects opened effects)

            Expect.exists records (fun record -> record.Kind = "request-open-state-change" && record.Payload = Some "False") "close request is recorded"
            Expect.isFalse (records |> List.exists (fun record -> record.Kind = "runtime-open-state")) "runtime does not own open state"
        }
    ]
