// See skill: fs-gg-elmish
namespace FS.GG.UI.Elmish

open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

/// Public contract type exposed by this FS.GG.UI package.
type ElmishAdapterModel<'model> =
    { UserModel: 'model
      Scene: SceneNode
      Viewer: ViewerModel }

/// Public contract type exposed by this FS.GG.UI package.
type ElmishAdapterMsg<'msg> =
    | UserMsg of 'msg
    | ViewerMsg of ViewerMsg

/// Public contract type exposed by this FS.GG.UI package.
type ElmishAdapterEffect<'msg> =
    | DispatchUser of 'msg
    | DispatchViewer of ViewerEffect

/// Public contract module exposed by this FS.GG.UI package.
module ElmishAdapter =
    /// Public contract function exposed by this FS.GG.UI package.
    val init:
        viewerOptions: ViewerOptions ->
        userModel: 'model ->
        scene: SceneNode ->
            ElmishAdapterModel<'model> * ElmishAdapterEffect<'msg> list

    /// Public contract function exposed by this FS.GG.UI package.
    val update:
        render: ('model -> SceneNode) ->
        msg: ElmishAdapterMsg<'msg> ->
        model: ElmishAdapterModel<'model> ->
            ElmishAdapterModel<'model> * ElmishAdapterEffect<'msg> list

/// <summary>Opaque, monotonically increasing identity for one authoritative host transition.</summary>
type TransitionGeneration

/// <summary>Reads the stable numeric value carried by a transition generation.</summary>
module TransitionGeneration =
    /// <summary>Returns the generation's monotonic 64-bit value.</summary>
    /// <param name="generation">The opaque generation to inspect.</param>
    val value: generation: TransitionGeneration -> int64

/// <summary>Whether the host can currently present transition work.</summary>
[<RequireQualifiedAccess>]
type TransitionVisibility =
    /// <summary>The host may schedule and acknowledge presentation.</summary>
    | Visible
    /// <summary>The host retains authoritative state but withholds presentation.</summary>
    | Hidden

/// <summary>Identifies the asynchronous producer whose response joins a transition.</summary>
[<RequireQualifiedAccess>]
type TransitionResponseKind =
    /// <summary>A planning worker completed.</summary>
    | PlanningWorker
    /// <summary>Client feature loading completed.</summary>
    | ClientFeatures
    /// <summary>A caller-defined producer completed.</summary>
    /// <param name="name">Stable producer name used in the authoritative ledger.</param>
    | Other of name: string

/// <summary>The single accessible focus destination and ARIA label for one presentation phase.</summary>
type TransitionFocusTarget =
    {
        /// <summary>Stable DOM/control id to focus.</summary>
        ControlId: string
        /// <summary>Accessible label announced for the destination.</summary>
        AriaLabel: string
    }

/// <summary>Caller-owned target and focus contract used to begin a transition.</summary>
type TransitionRequest<'target> =
    {
        /// <summary>The authoritative caller target, such as Editor, Plan, or Simulate.</summary>
        Target: 'target
        /// <summary>The only focus destination exposed while the target is pending.</summary>
        PendingFocus: TransitionFocusTarget
        /// <summary>The focus destination restored after the matching target commits.</summary>
        CommittedFocus: TransitionFocusTarget
    }

/// <summary>
/// Exact acknowledgement token for one presentation revision. A host must return the token
/// from <c>RequestPresentation</c>; generation-only acknowledgements are intentionally insufficient.
/// </summary>
type TransitionCommitToken<'target> =
    {
        /// <summary>The authoritative transition generation.</summary>
        Generation: TransitionGeneration
        /// <summary>The target rendered by this presentation.</summary>
        Target: 'target
        /// <summary>The response-set revision rendered by this presentation.</summary>
        Revision: int64
    }

/// <summary>A delayed asynchronous response bound to its original generation and target.</summary>
type TransitionResponse<'target, 'response> =
    {
        /// <summary>The generation captured when the asynchronous work began.</summary>
        Generation: TransitionGeneration
        /// <summary>The target captured when the asynchronous work began.</summary>
        Target: 'target
        /// <summary>The producer category used for diagnostics and ledger inspection.</summary>
        Kind: TransitionResponseKind
        /// <summary>The caller-owned response value retained in the deferred queue.</summary>
        Payload: 'response
    }

/// <summary>Input observed at the React/DOM host boundary.</summary>
[<RequireQualifiedAccess>]
type TransitionHostInput =
    /// <summary>A controlled text value that must update synchronously.</summary>
    | ControlledValueChanged of controlId: string * value: string
    /// <summary>A controlled file token that must update synchronously.</summary>
    | ControlledFileChanged of controlId: string * fileToken: string option
    /// <summary>A controlled element blurred; pending focus intent remains authoritative.</summary>
    | ControlledBlurred of controlId: string
    /// <summary>The old DOM still holds pointer capture.</summary>
    | PointerCaptureHeld of pointerId: int64
    /// <summary>A global key dispatch was attempted.</summary>
    | GlobalKeyAttempted of key: string
    /// <summary>A global click dispatch was attempted.</summary>
    | GlobalClickAttempted of targetId: string
    /// <summary>A global file dispatch was attempted from the old DOM.</summary>
    | GlobalFileAttempted of controlId: string * fileToken: string option

/// <summary>Why a response or commit acknowledgement could not affect the authoritative target.</summary>
[<RequireQualifiedAccess>]
type TransitionRejectionReason =
    /// <summary>The generation is no longer authoritative.</summary>
    | StaleGeneration
    /// <summary>The target does not match the authoritative generation's target.</summary>
    | TargetMismatch
    /// <summary>The response-set revision is no longer the requested revision.</summary>
    | RevisionMismatch
    /// <summary>Presentation acknowledgement is forbidden while hidden.</summary>
    | HiddenPresentation
    /// <summary>No matching presentation request is outstanding.</summary>
    | NoPresentationRequested

/// <summary>An exact presentation request interpreted by a React host as non-urgent work.</summary>
type TransitionPresentation<'target, 'response> =
    {
        /// <summary>The exact token a layout/commit effect must acknowledge.</summary>
        Token: TransitionCommitToken<'target>
        /// <summary>All current-generation responses in deterministic arrival order.</summary>
        Responses: TransitionResponse<'target, 'response> list
    }

/// <summary>Pure host directives produced by a transition update.</summary>
[<RequireQualifiedAccess>]
type TransitionHostEffect<'target, 'response> =
    /// <summary>Schedule this exact presentation through the host's non-urgent React lane.</summary>
    | RequestPresentation of TransitionPresentation<'target, 'response>
    /// <summary>Release pointer capture held by the obsolete DOM.</summary>
    | ReleasePointerCapture of pointerId: int64
    /// <summary>Do not dispatch this old-DOM input to the product.</summary>
    | SuppressInput of TransitionHostInput
    /// <summary>Move focus to the one accessible destination for the current phase.</summary>
    | MoveFocus of TransitionFocusTarget

/// <summary>Authoritative, append-ordered facts emitted by the pure host state machine.</summary>
[<RequireQualifiedAccess>]
type TransitionLedgerEntry<'target> =
    /// <summary>A new generation became authoritative.</summary>
    | Began of TransitionCommitToken<'target>
    /// <summary>A matching response joined the current deferred queue.</summary>
    | ResponseAccepted of TransitionCommitToken<'target> * TransitionResponseKind
    /// <summary>A stale or mismatched response was rejected.</summary>
    | ResponseRejected of TransitionGeneration * 'target * TransitionResponseKind * TransitionRejectionReason
    /// <summary>The host was asked to present the exact token.</summary>
    | PresentationRequested of TransitionCommitToken<'target>
    /// <summary>Presentation was withheld because the host was hidden.</summary>
    | PresentationWithheld of TransitionCommitToken<'target>
    /// <summary>Host visibility changed.</summary>
    | VisibilityChanged of TransitionVisibility
    /// <summary>A controlled or otherwise permitted input was applied synchronously.</summary>
    | InputApplied of TransitionHostInput
    /// <summary>An obsolete-DOM input was suppressed.</summary>
    | InputSuppressed of TransitionHostInput
    /// <summary>Pointer capture was released from the obsolete DOM.</summary>
    | PointerCaptureReleased of pointerId: int64
    /// <summary>The accessible focus destination changed.</summary>
    | FocusMoved of TransitionFocusTarget
    /// <summary>The exact requested presentation token was acknowledged.</summary>
    | PresentationAcknowledged of TransitionCommitToken<'target>
    /// <summary>A presentation acknowledgement was rejected.</summary>
    | PresentationRejected of TransitionCommitToken<'target> * TransitionRejectionReason
    /// <summary>The acknowledged token became committed.</summary>
    | Committed of TransitionCommitToken<'target>

/// <summary>Messages accepted by the pure transition host.</summary>
[<RequireQualifiedAccess>]
type TransitionHostMsg<'target, 'response> =
    /// <summary>Make a caller target authoritative and allocate its generation.</summary>
    | BeginTransition of TransitionRequest<'target>
    /// <summary>Admit or reject a delayed asynchronous response.</summary>
    | ResponseArrived of TransitionResponse<'target, 'response>
    /// <summary>Record a visible/hidden edge and request resume convergence when needed.</summary>
    | VisibilityChanged of TransitionVisibility
    /// <summary>Acknowledge the exact token after React commits its DOM.</summary>
    | Presented of TransitionCommitToken<'target>
    /// <summary>Apply or suppress input according to the current pending state.</summary>
    | InputAttempted of TransitionHostInput

/// <summary>
/// Opaque transition-host state. Construct it with <see cref="M:FS.GG.UI.Elmish.TransitionHost.init"/>
/// and advance it only through <see cref="M:FS.GG.UI.Elmish.TransitionHost.update"/>.
/// </summary>
type TransitionHostModel<'target, 'response>

/// <summary>Pure transition-aware Elmish host bridge and read-only state observers.</summary>
/// <category>Host integration</category>
module TransitionHost =
    /// <summary>Creates an empty host in the supplied visibility state.</summary>
    /// <param name="visibility">Whether initial presentation may be requested.</param>
    /// <returns>An empty host with no target, focus destination, controlled values, or ledger entries.</returns>
    val init: visibility: TransitionVisibility -> TransitionHostModel<'target, 'response>

    /// <summary>Applies one typed host message and returns pure host directives for the edge interpreter.</summary>
    /// <param name="msg">The authoritative host message to apply.</param>
    /// <param name="model">The current opaque host state.</param>
    /// <returns>The next state and ordered host effects.</returns>
    val update:
        msg: TransitionHostMsg<'target, 'response> ->
        model: TransitionHostModel<'target, 'response> ->
            TransitionHostModel<'target, 'response> * TransitionHostEffect<'target, 'response> list
            when 'target: equality

    /// <summary>Begins a transition through the same update path as <c>BeginTransition</c>.</summary>
    /// <param name="request">Target and focus/ARIA contract for the new generation.</param>
    /// <param name="model">The current host state.</param>
    /// <returns>The next state and ordered begin/presentation/focus effects.</returns>
    val beginTransition:
        request: TransitionRequest<'target> ->
        model: TransitionHostModel<'target, 'response> ->
            TransitionHostModel<'target, 'response> * TransitionHostEffect<'target, 'response> list
            when 'target: equality

    /// <summary>Returns true until the latest authoritative response-set revision commits.</summary>
    val isPending: model: TransitionHostModel<'target, 'response> -> bool when 'target: equality

    /// <summary>Returns the latest authoritative token, including unpresented hidden work.</summary>
    val authoritative: model: TransitionHostModel<'target, 'response> -> TransitionCommitToken<'target> option

    /// <summary>Returns the last exact token accepted at the commit boundary.</summary>
    val committed: model: TransitionHostModel<'target, 'response> -> TransitionCommitToken<'target> option

    /// <summary>Returns current-generation responses in deterministic arrival order.</summary>
    val responses: model: TransitionHostModel<'target, 'response> -> TransitionResponse<'target, 'response> list

    /// <summary>Returns current host visibility.</summary>
    val visibility: model: TransitionHostModel<'target, 'response> -> TransitionVisibility

    /// <summary>Returns the one accessible focus destination for the current phase, if any.</summary>
    val focusTarget: model: TransitionHostModel<'target, 'response> -> TransitionFocusTarget option

    /// <summary>Returns the latest synchronous controlled text value for a control.</summary>
    val controlledValue: controlId: string -> model: TransitionHostModel<'target, 'response> -> string option

    /// <summary>Returns the latest synchronous controlled file token for a control.</summary>
    val controlledFile: controlId: string -> model: TransitionHostModel<'target, 'response> -> string option option

    /// <summary>Returns the complete authoritative ledger in occurrence order.</summary>
    val ledger: model: TransitionHostModel<'target, 'response> -> TransitionLedgerEntry<'target> list
