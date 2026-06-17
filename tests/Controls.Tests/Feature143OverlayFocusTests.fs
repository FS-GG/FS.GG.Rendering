module Feature143OverlayFocusTests

open Expecto
open FS.GG.UI.Controls
open Feature143OverlayFixtures

[<Tests>]
let tests =
    testList "Feature143 overlay focus" [
        test "Tab traversal stays inside the active surface" {
            let opened, _ = openOne (surface TransientSurfaceKind.AutoCompleteSuggestions "suggestions" 10)
            let next, effects = OverlayState.update (KeyRouted(None, "Tab")) opened

            Expect.equal next.FocusedControl (Some "suggestions-item-2") "focus moved to second stop"
            Expect.contains effects (RequestFocus(Some "suggestions-item-2")) "focus request emitted"
            Expect.contains effects ConsumeInput "traversal consumed"
        }

        test "stale focus target recovers to the declared recovery target" {
            let opened, _ = openOne (surface TransientSurfaceKind.DatePickerCalendar "calendar" 10)
            let recovered, effects = OverlayState.update (FocusTargetRemoved "calendar-item-1") opened

            Expect.equal recovered.FocusedControl (Some "calendar-trigger") "focus recovered to trigger"
            Expect.exists recovered.Diagnostics (fun d -> d.Code = StaleOverlayFocusTarget) "stale focus diagnostic"
            Expect.contains effects (RequestFocus(Some "calendar-trigger")) "focus recovery effect"
        }
    ]
