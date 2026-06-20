namespace FS.GG.UI.Controls

open FS.GG.UI.Layout
open FS.GG.UI.DesignSystem

/// Framework-neutral mouse button identity (FR-013). Qualified access is required
/// because the bare case names (`Primary`/`Secondary`) otherwise collide with the
/// existing `ButtonIntent` cases in this namespace; consumers write
/// `PointerButton.Primary`.
[<RequireQualifiedAccess>]
type PointerButton =
    | Primary
    | Secondary
    | Middle

/// Where a consumer-facing interaction originated (FR-011: distinguish from keyboard).
type PointerOrigin = Pointer

/// The kind of raw pointer sample fed into coordination. Qualified access is
/// required because the bare case names (`Moved`/`Pressed`/`Released`) otherwise
/// collide with the `VisualState` cases in this namespace; consumers write
/// `PointerPhase.Moved`.
[<RequireQualifiedAccess>]
type PointerPhase =
    | Moved
    | Pressed
    | Released
    | Wheel
    | Exited

/// Host-independent input value. Consumers build this from ViewerEvent (or any
/// source), applying the same device-pixel scaling / pixel-snap policy already
/// applied to layout so X/Y share the control-bounds coordinate space.
type PointerSample =
    { Phase: PointerPhase
      X: float
      Y: float
      Button: PointerButton option
      DeltaX: float
      DeltaY: float }

/// A press in flight for a single button (the click-or-drag candidate).
type PressCandidate =
    { Control: ControlId
      StartX: float
      StartY: float
      Dragging: bool }

/// Durable coordination state (the MVU Model), owned alongside ControlRuntimeModel.
type PointerState =
    { Hover: ControlId option
      Presses: Map<PointerButton, PressCandidate>
      LastX: float
      LastY: float
      DragThreshold: float }

/// Reason a pointer event could not be resolved to a current control (FR-010).
type PointerDiagnosticCode =
    | HitTestMiss
    | StaleTarget

/// A diagnostic emitted when a pointer event could not be resolved to a control,
/// carrying the reason code, a human-readable message, the candidate control (if
/// any), and the pointer coordinates (FR-010).
type PointerDiagnostic =
    { Code: PointerDiagnosticCode
      Message: string
      Control: ControlId option
      X: float
      Y: float }

/// Consumer-facing, control-addressed interactions emitted in order by update.
type PointerInteraction =
    | HoverEnter of control: ControlId * x: float * y: float
    | HoverLeave of control: ControlId
    | PressedDown of control: ControlId * button: PointerButton * x: float * y: float
    | ReleasedUp of control: ControlId * button: PointerButton * x: float * y: float
    | Click of control: ControlId * button: PointerButton * x: float * y: float
    | DragBegin of control: ControlId * button: PointerButton * startX: float * startY: float
    | DragMove of control: ControlId * button: PointerButton * x: float * y: float
    | DragEnd of control: ControlId * button: PointerButton * x: float * y: float
    | DragCancelled of control: ControlId option
    | Scroll of control: ControlId * deltaX: float * deltaY: float * x: float * y: float
    | FocusMovedByPointer of control: ControlId
    | Diagnostic of PointerDiagnostic

/// Overlay-aware pointer routing result: the pure coordinator state after
/// routing, the topmost decision that was recorded, and the overlay effects the
/// host must interpret before lower content sees the original input.
type PointerOverlayRoutingResult =
    { State: OverlayState
      Decision: TopmostHitDecision
      Effects: OverlayEffect list
      PassThrough: bool
      Diagnostics: ControlDiagnostic list }

/// Internal transition input derived from a PointerSample.
type PointerMsg =
    | Move of x: float * y: float
    | Down of button: PointerButton * x: float * y: float
    | Up of button: PointerButton * x: float * y: float
    | WheelMsg of deltaX: float * deltaY: float * x: float * y: float
    | WindowExited
    | FocusLost

/// Public contract module exposed by this FS.GG.UI package: the pointer
/// coordination front door (pure, host-independent, replayable). FR-001/FR-009.
module Pointer =
    /// The single pointer origin tag, used to distinguish pointer-originated
    /// interactions from keyboard/text/focus ones when consumers flatten them
    /// into one message stream (FR-011).
    val origin: PointerOrigin

    /// Initial coordination state with the default 4.0 px click-vs-drag threshold
    /// (FR-006). Override per coordinator with `{ Pointer.init () with DragThreshold = … }`.
    val init: unit -> PointerState

    /// Feature 175 (FR-001): map a keyboard key (with Shift state) to a scroll delta for the focused
    /// scroll region (`ArrowUp`/`ArrowDown` line step, `PageUp`/`PageDown` viewport step, `Home`/`End`
    /// to top/bottom via a large signed delta clamped downstream, `Space`/`Shift+Space` page down/up).
    /// `None` when the key does not scroll. The host reduces the result to `ScrollControl`.
    val scrollKeyDelta: key: string -> shift: bool -> viewportHeight: float -> float option

    /// Map a neutral sample to a transition message (None when not actionable).
    val toMsg: sample: PointerSample -> PointerMsg option

    /// Pure reducer. Hit-tests via Layout.hitTestComputed against the supplied
    /// LayoutResult (an input, never fetched inside update — keeps update pure and
    /// deterministic/replayable, FR-009/SC-005). Emits ordered interactions plus
    /// the ControlRuntimeMsg list to keep ControlRuntime's hover/press/drag/focus
    /// state consistent.
    val update:
        policy: PixelSnapPolicy ->
        layout: LayoutResult ->
        msg: PointerMsg ->
        state: PointerState ->
            PointerState * PointerInteraction list * ControlRuntimeMsg list

    /// Route a resolved pointer target through the current overlay state before
    /// lower content. The decision is pure and deterministic: modal topmost
    /// surfaces block covered targets, non-modal outside targets request the
    /// active surface's outside-pointer policy, and pass-through is exposed by
    /// the returned overlay effects.
    val routeOverlay:
        overlay: OverlayState ->
        input: string ->
        candidateLayers: ControlId list ->
        chosenTarget: ControlId option ->
            PointerOverlayRoutingResult

    /// Replay a recorded message sequence to a final state + accumulated effects;
    /// identical input yields identical output (SC-005).
    val replay:
        policy: PixelSnapPolicy ->
        layout: LayoutResult ->
        messages: PointerMsg list ->
        initial: PointerState ->
            PointerState * PointerInteraction list
