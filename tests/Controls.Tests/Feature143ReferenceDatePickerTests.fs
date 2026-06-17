module Feature143ReferenceDatePickerTests

open Expecto
open FS.GG.UI.Controls
open Feature143OverlayFixtures

[<Tests>]
let tests =
    testList "Feature143 reference date-picker flow" [
        test "date picker opens selects once dismisses and recovers focus" {
            let opened, _ = openOne (surface TransientSurfaceKind.DatePickerCalendar "date-picker" 10)
            let selected, effects = OverlayState.update (SelectionCompleted("date-picker", "date-picker:2026-06-17", Some "2026-06-17")) opened
            let duplicate, duplicateEffects = OverlayState.update (SelectionCompleted("date-picker", "date-picker:2026-06-17", Some "2026-06-17")) selected

            Expect.isEmpty selected.OpenSurfaces "calendar closes after selection"
            Expect.equal selected.FocusedControl (Some "date-picker-trigger") "focus recovers to trigger"
            Expect.exists effects (function DispatchProductMessage("date-picker", Some "2026-06-17") -> true | _ -> false) "date selected once"
            Expect.exists duplicate.Diagnostics (fun d -> d.Code = DuplicateOverlayDispatch) "duplicate dispatch disclosed"
            Expect.isFalse (duplicateEffects |> List.exists (function DispatchProductMessage _ -> true | _ -> false)) "duplicate product dispatch suppressed"
        }
    ]
