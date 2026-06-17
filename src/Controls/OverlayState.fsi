namespace FS.GG.UI.Controls

open FS.GG.UI.Scene

[<RequireQualifiedAccess>]
type TransientSurfaceKind =
    | Menu
    | ContextMenu
    | SplitButtonMenu
    | ComboDropdown
    | AutoCompleteSuggestions
    | DatePickerCalendar
    | ColorPickerPalette
    | DialogModal

[<RequireQualifiedAccess>]
type DismissalReason =
    | Escape
    | OutsidePointer
    | SelectionCompletion
    | ExplicitClose
    | AnchorRemoved
    | ProductClosed

type OverlayActivationSource =
    | PointerActivation
    | KeyboardActivation
    | ProductOwnedOpen
    | NestedOpen

type DismissalRule =
    | AllowDismiss
    | BlockDismiss
    | IgnoreDismiss

type SelectionCompletionPolicy =
    | CloseOnSelection
    | KeepOpenOnSelection

type AnchorRemovalPolicy =
    | CloseOnAnchorRemoval
    | KeepOpenWithDiagnostic

type TrapMode =
    | ModalTrap
    | LocalScope
    | NoFocusCapture

type OverlaySurfaceId =
    { SurfaceId: ControlId
      ParentSurfaceId: ControlId option
      TriggerId: ControlId }

type OverlayTrigger =
    { ControlId: ControlId
      Enabled: bool
      ActivationSource: OverlayActivationSource
      RecoveryTarget: ControlId option }

type AnchorEvidence =
    { AnchorId: ControlId
      AnchorBounds: Rect option
      SurfaceBounds: Rect option
      Placement: string
      NoFit: string option
      FrameFingerprint: uint64 option }

type DismissalPolicy =
    { Escape: DismissalRule
      OutsidePointer: DismissalRule
      SelectionCompletion: SelectionCompletionPolicy
      ExplicitClose: DismissalRule
      AnchorRemoval: AnchorRemovalPolicy
      PassThroughAfterDismissal: bool }

type FocusScope =
    { SurfaceId: ControlId
      Stops: ControlId list
      InitialFocus: ControlId option
      RecoveryTarget: ControlId option
      TrapMode: TrapMode }

type OverlaySurface =
    { Id: OverlaySurfaceId
      Kind: TransientSurfaceKind
      Trigger: OverlayTrigger
      LayerPriority: int
      Anchor: AnchorEvidence
      DismissalPolicy: DismissalPolicy
      FocusScope: FocusScope
      Modal: bool }

type TopmostHitDecision =
    { Input: string
      CandidateLayers: ControlId list
      ChosenTarget: ControlId option
      BlockedByModal: ControlId option
      OutsideOfSurface: ControlId option }

type OverlayTransition =
    { SurfaceId: ControlId option
      Kind: string
      Reason: string
      Stack: ControlId list }

type FocusTransition =
    { From: ControlId option
      To: ControlId option
      Reason: string }

type ProductDispatch =
    { SurfaceId: ControlId
      DispatchKey: string
      Payload: string option }

type DismissalOutcome =
    { SurfaceId: ControlId
      Reason: DismissalReason
      Dismissed: bool
      PassThrough: bool
      Diagnostic: ControlDiagnostic option }

type InteractionReplayLog =
    { Inputs: string list
      OverlayTransitions: OverlayTransition list
      FocusTransitions: FocusTransition list
      ProductDispatches: ProductDispatch list
      DismissalReasons: DismissalOutcome list
      Diagnostics: ControlDiagnostic list
      HitDecisions: TopmostHitDecision list
      RenderEvidence: string list }

type OverlayState =
    { OpenSurfaces: OverlaySurface list
      ActiveSurface: ControlId option
      FocusedControl: ControlId option
      RecentTransitions: OverlayTransition list
      ReplayLog: InteractionReplayLog
      Diagnostics: ControlDiagnostic list
      DispatchedSelectionKeys: Set<string> }

type OverlayEffect =
    | DispatchProductMessage of surface: ControlId * payload: string option
    | RequestFocus of ControlId option
    | RequestOpenStateChange of surface: ControlId * isOpen: bool
    | ReportOverlayDiagnostic of ControlDiagnostic
    | ConsumeInput
    | AllowPassThrough
    | RecordTopmostHit of TopmostHitDecision

type OverlayMsg =
    | OpenRequested of OverlaySurface
    | DismissRequested of surfaceId: ControlId option * reason: DismissalReason
    | PointerRouted of TopmostHitDecision
    | KeyRouted of surfaceId: ControlId option * key: string
    | SelectionCompleted of surfaceId: ControlId * dispatchKey: string * payload: string option
    | AnchorChanged of surfaceId: ControlId * anchor: AnchorEvidence
    | AnchorRemoved of surfaceId: ControlId
    | FocusTargetRemoved of targetId: ControlId
    | Reset

module OverlayState =
    val supportedSurfaceKinds: unit -> TransientSurfaceKind list
    val defaultDismissalPolicy: unit -> DismissalPolicy
    val modalDismissalPolicy: unit -> DismissalPolicy
    val init: unit -> OverlayState
    val diagnostics: state: OverlayState -> ControlDiagnostic list
    val replayLog: state: OverlayState -> InteractionReplayLog
    val update: msg: OverlayMsg -> state: OverlayState -> OverlayState * OverlayEffect list
