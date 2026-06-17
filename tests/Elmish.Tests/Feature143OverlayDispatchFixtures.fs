module Feature143OverlayDispatchFixtures

open FS.GG.UI.Scene
open FS.GG.UI.Controls

let bounds x y width height =
    { X = x
      Y = y
      Width = width
      Height = height }

let anchorFor id =
    { AnchorId = id
      AnchorBounds = Some(bounds 10.0 20.0 120.0 32.0)
      SurfaceBounds = Some(bounds 10.0 56.0 220.0 180.0)
      Placement = "bottom-start"
      NoFit = None
      FrameFingerprint = Some 143UL }

let trigger id =
    { ControlId = id
      Enabled = true
      ActivationSource = PointerActivation
      RecoveryTarget = Some id }

let focus surfaceId =
    { SurfaceId = surfaceId
      Stops = [ surfaceId + "-item-1"; surfaceId + "-item-2" ]
      InitialFocus = Some(surfaceId + "-item-1")
      RecoveryTarget = Some(surfaceId + "-trigger")
      TrapMode = LocalScope }

let surface kind surfaceId layer =
    let triggerId = surfaceId + "-trigger"

    { Id =
        { SurfaceId = surfaceId
          ParentSurfaceId = None
          TriggerId = triggerId }
      Kind = kind
      Trigger = trigger triggerId
      LayerPriority = layer
      Anchor = anchorFor triggerId
      DismissalPolicy = OverlayState.defaultDismissalPolicy ()
      FocusScope = focus surfaceId
      Modal = false }

let openOne surface =
    OverlayState.update (OpenRequested surface) (OverlayState.init ())

let run messages =
    messages
    |> List.fold
        (fun state msg ->
            let next, _ = OverlayState.update msg state
            next)
        (OverlayState.init ())
