module Feature143OverlayFixtures

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

let missingAnchor id =
    { AnchorId = id
      AnchorBounds = None
      SurfaceBounds = None
      Placement = "bottom-start"
      NoFit = None
      FrameFingerprint = Some 143UL }

let trigger enabled id =
    { ControlId = id
      Enabled = enabled
      ActivationSource = PointerActivation
      RecoveryTarget = Some id }

let focus surfaceId trap stops =
    { SurfaceId = surfaceId
      Stops = stops
      InitialFocus = stops |> List.tryHead
      RecoveryTarget = Some(surfaceId + "-trigger")
      TrapMode = trap }

let surface kind surfaceId layer =
    let triggerId = surfaceId + "-trigger"

    { Id =
        { SurfaceId = surfaceId
          ParentSurfaceId = None
          TriggerId = triggerId }
      Kind = kind
      Trigger = trigger true triggerId
      LayerPriority = layer
      Anchor = anchorFor triggerId
      DismissalPolicy = OverlayState.defaultDismissalPolicy ()
      FocusScope = focus surfaceId LocalScope [ surfaceId + "-item-1"; surfaceId + "-item-2" ]
      Modal = false }

let modalSurface surfaceId =
    let triggerId = surfaceId + "-trigger"

    { Id =
        { SurfaceId = surfaceId
          ParentSurfaceId = None
          TriggerId = triggerId }
      Kind = TransientSurfaceKind.DialogModal
      Trigger = trigger true triggerId
      LayerPriority = 100
      Anchor = anchorFor triggerId
      DismissalPolicy = OverlayState.modalDismissalPolicy ()
      FocusScope = focus surfaceId ModalTrap [ surfaceId + "-ok"; surfaceId + "-cancel" ]
      Modal = true }

let openOne surface =
    OverlayState.update (OpenRequested surface) (OverlayState.init ())

let run messages =
    messages
    |> List.fold
        (fun state msg ->
            let next, _ = OverlayState.update msg state
            next)
        (OverlayState.init ())
