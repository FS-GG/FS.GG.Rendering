module Feature144ClosedStateCompatibilityTests

open Expecto
open FS.GG.UI.Controls
open Feature144OverlayFixtures

[<Tests>]
let tests =
    testList "Feature144 closed-state compatibility" [
        test "closed metadata does not create open overlay state" {
            let closed = metadata TransientSurfaceKind.DatePickerCalendar "closed-calendar" 60 false true false

            Expect.isEmpty (OverlayState.init ()).OpenSurfaces "coordinator starts closed"
            Expect.isFalse closed.VisibilityState "metadata observes closed product state"
        }

        test "closed metadata validates without current-frame anchor bounds" {
            let closed = metadata TransientSurfaceKind.ColorPickerPalette "closed-palette" 70 false true false

            Expect.isEmpty (TransientWidget.validate None closed) "closed state does not require placement evidence"
        }

        test "reset closes all product-owned surfaces through explicit requests" {
            let opened, _ = openOne (metadata TransientSurfaceKind.DialogModal "modal-reset" 100 true true true)
            let reset, effects = OverlayState.update Reset opened

            Expect.isEmpty reset.OpenSurfaces "reset clears coordinator state"
            Expect.exists effects (function RequestOpenStateChange("modal-reset", false) -> true | _ -> false) "close is product-visible"
        }
    ]
