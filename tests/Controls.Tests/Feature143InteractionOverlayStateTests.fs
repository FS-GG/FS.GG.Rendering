module Feature143InteractionOverlayStateTests

open Expecto
open FS.GG.UI.Controls
open Feature143OverlayFixtures

[<Tests>]
let tests =
    testList "Feature143 open and dismiss overlay state" [
        test "each supported kind opens and dismisses with Escape" {
            for kind in OverlayState.supportedSurfaceKinds () do
                let id = sprintf "%A" kind
                let opened, _ = openOne (surface kind id 10)
                let closed, effects = OverlayState.update (KeyRouted(None, "Escape")) opened

                Expect.equal opened.ActiveSurface (Some id) $"opened {kind}"
                Expect.isEmpty closed.OpenSurfaces $"dismissed {kind}"
                Expect.contains effects (RequestOpenStateChange(id, false)) $"close effect for {kind}"
        }

        test "disabled triggers do not open transient surfaces" {
            let closed =
                { surface TransientSurfaceKind.ComboDropdown "combo" 10 with
                    Trigger = trigger false "combo-trigger" }

            let state, effects = OverlayState.update (OpenRequested closed) (OverlayState.init ())

            Expect.isEmpty state.OpenSurfaces "disabled trigger leaves overlay closed"
            Expect.exists state.Diagnostics (fun d -> d.Code = DisabledOverlayTrigger) "diagnostic names disabled trigger"
            Expect.contains effects AllowPassThrough "ignored activation may fall through"
        }

        test "missing anchors fail safely without stale hit targets" {
            let ghost =
                { surface TransientSurfaceKind.DatePickerCalendar "date" 10 with
                    Anchor = missingAnchor "date-trigger" }

            let state, _ = OverlayState.update (OpenRequested ghost) (OverlayState.init ())

            Expect.isEmpty state.OpenSurfaces "no surface opens without anchor"
            Expect.exists state.Diagnostics (fun d -> d.Code = MissingOverlayAnchor) "missing anchor diagnostic"
        }
    ]
