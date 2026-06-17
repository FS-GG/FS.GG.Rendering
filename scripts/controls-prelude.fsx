#I "../src/Controls/bin/Debug/net10.0"
#r "FS.GG.UI.Controls.dll"
#r "FS.GG.UI.Scene.dll"

open FS.GG.UI.Controls
open FS.GG.UI.Scene

let anchor =
    { AnchorId = "date-trigger"
      AnchorBounds = Some { X = 12.0; Y = 24.0; Width = 140.0; Height = 32.0 }
      SurfaceBounds = Some { X = 12.0; Y = 60.0; Width = 280.0; Height = 240.0 }
      Placement = "bottom-start"
      NoFit = None
      FrameFingerprint = Some 143UL }

let trigger =
    { ControlId = "date-trigger"
      Enabled = true
      ActivationSource = KeyboardActivation
      RecoveryTarget = Some "date-trigger" }

let focus =
    { SurfaceId = "date-calendar"
      Stops = [ "day-2026-06-17"; "day-2026-06-18" ]
      InitialFocus = Some "day-2026-06-17"
      RecoveryTarget = Some "date-trigger"
      TrapMode = LocalScope }

let surface =
    { Id = { SurfaceId = "date-calendar"; ParentSurfaceId = None; TriggerId = "date-trigger" }
      Kind = TransientSurfaceKind.DatePickerCalendar
      Trigger = trigger
      LayerPriority = 10
      Anchor = anchor
      DismissalPolicy = OverlayState.defaultDismissalPolicy ()
      FocusScope = focus
      Modal = false }

let state0 = OverlayState.init ()
let state1, effects1 = OverlayState.update (OpenRequested surface) state0
let state2, effects2 = OverlayState.update (SelectionCompleted("date-calendar", "date-calendar:2026-06-17", Some "2026-06-17")) state1

printfn "open surfaces: %A" (state1.OpenSurfaces |> List.map _.Id.SurfaceId)
printfn "open effects: %A" effects1
printfn "selection effects: %A" effects2
printfn "replay entries: %d" (OverlayState.replayLog state2).OverlayTransitions.Length
