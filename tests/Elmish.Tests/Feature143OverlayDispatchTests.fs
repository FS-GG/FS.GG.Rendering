module Feature143OverlayDispatchTests

open Expecto
open FS.GG.UI.Controls
open Feature143OverlayDispatchFixtures

[<Tests>]
let tests =
    testList "Feature143 overlay dispatch bridge" [
        test "ControlRuntime bridge carries overlay effects without owning product state" {
            let opened, _ = openOne (surface TransientSurfaceKind.DatePickerCalendar "date" 10)
            let selected, effects = OverlayState.update (SelectionCompleted("date", "date:2026-06-17", Some "2026-06-17")) opened
            let bridge = ControlRuntime.attachOverlayEffects selected effects

            Expect.equal bridge.Overlay selected "overlay state is carried unchanged"
            Expect.exists bridge.Effects (function DispatchProductMessage("date", Some "2026-06-17") -> true | _ -> false) "product dispatch effect carried"
            Expect.isEmpty bridge.Overlay.OpenSurfaces "runtime bridge does not reopen closed overlay"
        }

        test "selection completion dispatches exactly once per dispatch key" {
            let opened, _ = openOne (surface TransientSurfaceKind.AutoCompleteSuggestions "suggestions" 10)
            let first, firstEffects = OverlayState.update (SelectionCompleted("suggestions", "suggestions:item-1", Some "item-1")) opened
            let second, secondEffects = OverlayState.update (SelectionCompleted("suggestions", "suggestions:item-1", Some "item-1")) first

            Expect.exists firstEffects (function DispatchProductMessage("suggestions", Some "item-1") -> true | _ -> false) "first dispatch emitted"
            Expect.isFalse (secondEffects |> List.exists (function DispatchProductMessage _ -> true | _ -> false)) "second dispatch suppressed"
            Expect.exists second.Diagnostics (fun d -> d.Code = DuplicateOverlayDispatch) "duplicate diagnostic recorded"
        }
    ]
