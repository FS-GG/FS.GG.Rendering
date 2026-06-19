module SecondAntShowcase.Tests.Feature143DatePickerFlowTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private bounds x y width height =
    { X = x
      Y = y
      Width = width
      Height = height }

let private datePickerSurface =
    { Id =
        { SurfaceId = "ant-date-picker-calendar"
          ParentSurfaceId = None
          TriggerId = "date-picker" }
      Kind = TransientSurfaceKind.DatePickerCalendar
      Trigger =
        { ControlId = "date-picker"
          Enabled = true
          ActivationSource = PointerActivation
          RecoveryTarget = Some "date-picker" }
      LayerPriority = 20
      Anchor =
        { AnchorId = "date-picker"
          AnchorBounds = Some(bounds 24.0 120.0 180.0 32.0)
          SurfaceBounds = Some(bounds 24.0 156.0 280.0 240.0)
          Placement = "bottom-start"
          NoFit = None
          FrameFingerprint = Some 143UL }
      DismissalPolicy = OverlayState.defaultDismissalPolicy ()
      FocusScope =
        { SurfaceId = "ant-date-picker-calendar"
          Stops = [ "date-2026-06-17"; "date-2026-06-18" ]
          InitialFocus = Some "date-2026-06-17"
          RecoveryTarget = Some "date-picker"
          TrapMode = LocalScope }
      Modal = false }

[<Tests>]
let tests =
    testList "Feature143 SecondAntShowcase date-picker reference flow" [
        test "scripted date-picker overlay opens selects dismisses and records evidence" {
            let opened, _ = OverlayState.update (OpenRequested datePickerSurface) (OverlayState.init ())
            let selected, effects =
                OverlayState.update
                    (SelectionCompleted("ant-date-picker-calendar", "ant-date-picker-calendar:2026-06-17", Some "2026-06-17"))
                    opened

            Expect.equal opened.ActiveSurface (Some "ant-date-picker-calendar") "calendar opened"
            Expect.isEmpty selected.OpenSurfaces "calendar dismissed"
            Expect.equal selected.FocusedControl (Some "date-picker") "focus recovered to trigger"
            Expect.exists effects (function DispatchProductMessage("ant-date-picker-calendar", Some "2026-06-17") -> true | _ -> false) "exactly one date selection effect"
            Expect.equal (OverlayState.replayLog selected).ProductDispatches.Length 1 "dispatch evidence recorded"
        }
    ]
