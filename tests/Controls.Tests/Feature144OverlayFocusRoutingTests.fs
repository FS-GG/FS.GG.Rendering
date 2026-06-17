module Feature144OverlayFocusRoutingTests

open Expecto
open FS.GG.UI.Controls
open Feature144OverlayFixtures

[<Tests>]
let tests =
    testList "Feature144 overlay focus routing" [
        test "opening an interactive surface requests initial focus" {
            let opened, effects = openOne (metadata TransientSurfaceKind.AutoCompleteSuggestions "focus-suggestions" 50 true true false)

            Expect.equal opened.FocusedControl (Some "focus-suggestions-item-1") "initial focus enters surface"
            Expect.exists effects (function RequestFocus(Some "focus-suggestions-item-1") -> true | _ -> false) "focus request emitted"
        }

        test "modal traversal cycles inside the active surface" {
            let opened, _ = openOne (metadata TransientSurfaceKind.DialogModal "focus-dialog" 100 true true true)
            let next, effects = OverlayState.update (KeyRouted(None, "Tab")) opened

            Expect.equal next.FocusedControl (Some "focus-dialog-item-2") "Tab stays inside modal scope"
            Expect.exists effects (function RequestFocus(Some "focus-dialog-item-2") -> true | _ -> false) "focus move emitted"
        }

        test "stale target recovers to trigger and records a decision" {
            let opened, _ = openOne (metadata TransientSurfaceKind.DatePickerCalendar "focus-calendar" 60 true true false)
            let recovered, effects, decision = Focus.recoverOverlayFocus opened "focus-calendar-item-1"

            Expect.equal recovered.FocusedControl (Some "focus-calendar-trigger") "focus recovered"
            Expect.equal decision.RecoveryTargetKind Trigger "recovery target classified"
            Expect.exists effects (function ReportOverlayDiagnostic diagnostic when diagnostic.Code = StaleOverlayFocusTarget -> true | _ -> false) "stale target diagnostic emitted"
        }
    ]
