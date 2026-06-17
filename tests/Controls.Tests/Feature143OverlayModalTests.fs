module Feature143OverlayModalTests

open Expecto
open FS.GG.UI.Controls
open Feature143OverlayFixtures

[<Tests>]
let tests =
    testList "Feature143 modal overlay state" [
        test "modal Tab and Shift+Tab cycle inside modal scope" {
            let opened, _ = openOne (modalSurface "dialog")
            let next, _ = OverlayState.update (KeyRouted(None, "Tab")) opened
            let previous, _ = OverlayState.update (KeyRouted(None, "Shift+Tab")) next

            Expect.equal next.FocusedControl (Some "dialog-cancel") "Tab moves to next modal stop"
            Expect.equal previous.FocusedControl (Some "dialog-ok") "Shift+Tab cycles back"
        }

        test "modal outside dismissal can be blocked by policy" {
            let opened, _ = openOne (modalSurface "dialog")
            let blocked, effects = OverlayState.update (DismissRequested(None, DismissalReason.OutsidePointer)) opened

            Expect.equal blocked.ActiveSurface (Some "dialog") "modal remains open"
            Expect.exists blocked.Diagnostics (fun d -> d.Code = BlockedOverlayDismissal) "blocked dismissal recorded"
            Expect.contains effects ConsumeInput "blocked modal outside event consumed"
        }
    ]
