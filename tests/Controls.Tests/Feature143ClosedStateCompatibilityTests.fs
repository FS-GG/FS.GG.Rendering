module Feature143ClosedStateCompatibilityTests

open Expecto
open FS.GG.UI.Controls
open Feature143OverlayFixtures

[<Tests>]
let tests =
    testList "Feature143 closed-state compatibility" [
        test "closed coordinator carries no visible or hit-testable overlay state" {
            let state = OverlayState.init ()

            Expect.isEmpty state.OpenSurfaces "no open overlay content"
            Expect.isNone state.ActiveSurface "no active overlay hit target"
            Expect.isEmpty state.Diagnostics "no diagnostics at rest"
        }

        test "no-focus-capture surfaces open without stealing closed product focus" {
            let tooltipLike =
                { surface TransientSurfaceKind.Menu "informational" 10 with
                    FocusScope = focus "informational" NoFocusCapture [] }

            let opened, effects = OverlayState.update (OpenRequested tooltipLike) (OverlayState.init ())

            Expect.isNone opened.FocusedControl "no focus captured"
            Expect.isFalse (effects |> List.exists (function RequestFocus _ -> true | _ -> false)) "no focus effect"
        }
    ]
