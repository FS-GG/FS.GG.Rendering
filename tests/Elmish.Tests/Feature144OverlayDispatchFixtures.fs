module Feature144OverlayDispatchFixtures

open FS.GG.UI.Scene
open FS.GG.UI.Controls

type ProductMsg =
    | OpenChanged of ControlId * bool
    | PayloadDispatched of ControlId * string option
    | FocusRequested of ControlId option

let bounds x y width height =
    { X = x
      Y = y
      Width = width
      Height = height }

let anchorFor (id: ControlId) =
    { AnchorId = id
      AnchorBounds = Some(bounds 12.0 24.0 120.0 32.0)
      SurfaceBounds = Some(bounds 12.0 60.0 240.0 180.0)
      Placement = "bottom-start"
      NoFit = None
      FrameFingerprint = Some 144UL }

let metadata kind (surfaceId: ControlId) : TransientWidgetMetadata =
    let triggerId = surfaceId + "-trigger"

    { SurfaceKind = kind
      SurfaceId = surfaceId
      ParentSurfaceId = None
      TriggerId = triggerId
      AnchorId = triggerId
      LayerPriority = 10
      DismissalPolicy = OverlayState.defaultDismissalPolicy ()
      FocusScope =
        { SurfaceId = surfaceId
          Stops = [ surfaceId + "-item-1"; surfaceId + "-item-2" ]
          InitialFocus = Some(surfaceId + "-item-1")
          RecoveryTarget = Some triggerId
          TrapMode = LocalScope }
      Modal = false
      SelectionDispatchKey = Some "selected"
      VisibilityState = true
      TriggerEnabled = true }

let surface kind surfaceId =
    let item = metadata kind surfaceId
    TransientWidget.toSurface (anchorFor (item.AnchorId)) item

let mapOpen surface isOpen = OpenChanged(surface, isOpen)
let mapDispatch surface payload = PayloadDispatched(surface, payload)
let mapFocus focus = Some(FocusRequested focus)
