module Feature144TransientMetadataFailureTests

open Expecto
open FS.GG.UI.Controls
open Feature144OverlayFixtures

[<Tests>]
let tests =
    testList "Feature144 transient metadata failure modes" [
        test "disabled triggers suppress open activation requests" {
            let disabled = metadata TransientSurfaceKind.Menu "disabled-menu" 10 false false false
            let request = TransientWidget.activationRequest PointerActivation true disabled

            Expect.isFalse request.RequestedOpenState "disabled trigger does not request open state"
            Expect.isSome request.Diagnostic "disabled trigger reports a diagnostic"
            Expect.equal (request.Diagnostic |> Option.map _.Code) (Some DisabledOverlayTrigger) "diagnostic is classified"
        }

        test "open metadata without current-frame anchor evidence is a readiness failure" {
            let current = metadata TransientSurfaceKind.DatePickerCalendar "missing-anchor" 60 true true false
            let diagnostics = TransientWidget.validate (Some(missingAnchor current.AnchorId)) current

            Expect.exists diagnostics (fun diagnostic -> diagnostic.Code = MissingOverlayAnchor) "missing anchor is explicit"
        }

        test "disabled open request does not mutate overlay state" {
            let disabled = metadata TransientSurfaceKind.ComboDropdown "disabled-combo" 40 true false false
            let surface = TransientWidget.toSurface (anchorFor (disabled.AnchorId)) disabled
            let state, effects = OverlayState.update (OpenRequested surface) (OverlayState.init ())

            Expect.isEmpty state.OpenSurfaces "disabled trigger leaves overlay closed"
            Expect.exists effects (function ReportOverlayDiagnostic diagnostic when diagnostic.Code = DisabledOverlayTrigger -> true | _ -> false) "diagnostic effect emitted"
        }
    ]
