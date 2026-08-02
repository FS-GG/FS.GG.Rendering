namespace FS.GG.UI.SkiaViewer.Host

open Elmish
open FS.GG.UI.SkiaViewer

/// GL resource-ownership ledger (feature 119; GL successor to the former VulkanResources).
module GlResources =
    /// Public contract type exposed by this FS.GG.UI package.
    type ResourceCategory =
        | GlContext
        | GlSurface
        | GrContext
        | Framebuffer
        | SkiaSurface
        | SkiaGpu

    /// Public contract type exposed by this FS.GG.UI package.
    type OwnershipState =
        | Acquired
        | Transferred
        | Released

    /// Public contract type exposed by this FS.GG.UI package.
    type OwnedResource =
        { Id: string
          Category: ResourceCategory
          AcquireStage: string
          Owner: string
          TransferPoint: string option
          ReleaseAction: string
          State: OwnershipState }

    /// Public contract type exposed by this FS.GG.UI package.
    type ReleaseRecord =
        { Id: string
          Category: ResourceCategory
          Stage: string
          Order: int }

    /// Public contract type exposed by this FS.GG.UI package.
    type ResourceLedger =
        { Owned: OwnedResource list
          Released: ReleaseRecord list }

    /// Public contract function exposed by this FS.GG.UI package.
    val empty: ResourceLedger

    /// Public contract function exposed by this FS.GG.UI package.
    val acquire:
        id: string ->
        category: ResourceCategory ->
        acquireStage: string ->
        owner: string ->
        releaseAction: string ->
        ledger: ResourceLedger ->
            ResourceLedger

    /// Public contract function exposed by this FS.GG.UI package.
    val transfer: id: string -> transferPoint: string -> ledger: ResourceLedger -> ResourceLedger
    /// Public contract function exposed by this FS.GG.UI package.
    val acquired: ledger: ResourceLedger -> OwnedResource list
    /// Public contract function exposed by this FS.GG.UI package.
    val releaseAll: stage: string -> ledger: ResourceLedger -> ResourceLedger * ReleaseRecord list

/// GL startup-stage ordering + cleanup model (feature 119; GL successor to the former VulkanStartup).
module GlStartup =
    /// Public contract type exposed by this FS.GG.UI package.
    type StartupStage =
        { Name: string
          Order: int
          Resource: GlResources.ResourceCategory option
          DiagnosticStage: string }

    /// Public contract type exposed by this FS.GG.UI package.
    type StartupFailureCase =
        { FailedStage: StartupStage
          AcquiredBeforeFailure: GlResources.OwnedResource list
          ExpectedReleaseOrder: GlResources.ResourceCategory list
          ObservedReleaseOrder: GlResources.ResourceCategory list
          DiagnosticStage: string
          DiagnosticCause: string
          Synthetic: bool }

    /// Public contract function exposed by this FS.GG.UI package.
    val stages: StartupStage list
    /// Public contract function exposed by this FS.GG.UI package.
    val stageByName: name: string -> StartupStage option
    /// Public contract function exposed by this FS.GG.UI package.
    val simulateFailure: failedStageName: string -> StartupFailureCase
    /// Public contract function exposed by this FS.GG.UI package.
    val simulateSuccessfulShutdown: unit -> GlResources.ReleaseRecord list

/// Issue #180: the loop-thread hand-off that keeps `GlHost.run` single-threaded. Internal to the
/// package (products never touch it); exposed here so the concurrency invariant can be tested.
type internal LoopDispatchGate<'msg>

/// Issue #180: queue a dispatch raised off the loop thread and replay it on the loop thread.
/// `Animation.tickSubscription` dispatches from a `System.Threading.Timer` threadpool thread; the
/// run's model, effect state and thread-affine GL context all assume the loop thread.
module internal LoopDispatch =
    /// Bind a gate to the calling thread — the thread that will own the loop.
    val forCurrentThread: unit -> LoopDispatchGate<'msg>

    /// True only on the thread the gate was bound to.
    val isLoopThread: gate: LoopDispatchGate<'msg> -> bool

    /// Messages queued from other threads and not yet drained.
    val pending: gate: LoopDispatchGate<'msg> -> int

    /// Wrap a loop-thread-only dispatch: inline on the loop thread, queued from anywhere else.
    val guard: gate: LoopDispatchGate<'msg> -> dispatch: Dispatch<'msg> -> Dispatch<'msg>

    /// Replay queued messages on the loop thread, returning how many ran. A no-op off the loop
    /// thread, and bounded by the depth observed on entry.
    val drain: gate: LoopDispatchGate<'msg> -> dispatch: Dispatch<'msg> -> int

/// The OpenGL/Skia presentation host body (internal helpers hidden; only `run` is reachable).
///
/// **Concurrency (issue #180).** `run` is single-threaded and single-run.
///
/// *Single-threaded:* Silk delivers input callbacks, `DoUpdate` and `DoRender` on the thread that
/// called `run`, and the GL context is affine to it. `run` therefore hands subscriptions a
/// `LoopDispatch`-guarded `Dispatch`, so a subscription that fires on a threadpool thread (as
/// `Animation.tickSubscription` does) is replayed on the loop thread rather than mutating the model
/// and painting through Skia from off it.
///
/// *Single-run:* two overlapping `run` calls are **unsupported** and corrupt each other. The host
/// keeps per-run state in module statics — the present carrier (`lastPresentedScene`,
/// `skippedPresentCount`), the idle-represent carrier (`lastGoodFrame`, `idleRepresentsRemaining`)
/// — as does the painter (`SceneRenderer.activeReplayCache`, `fallbackEvents`, and the
/// `subObjectsReleased` diagnostic counter). None is keyed by run, and `run` resets them on entry,
/// so a second concurrent run resets the first one's state underneath it. Call `run` once per
/// process at a time.
module GlHost =
    /// The single source of truth for the graphics backend this viewer host actually initializes
    /// (always `ContextAPI.OpenGL` + Skia `GRContext.CreateGl`; Vulkan/software are rejected,
    /// feature 119). Runtime self-reports name the backend from here so a label can never drift
    /// from what really initialized (#135).
    val backendLabel: string

    [<NoEquality; NoComparison>]
    /// Testable native-window mutation boundary. The live host binds these members to its `IWindow`;
    /// tests bind them to an in-memory target and therefore exercise the exact transition policy.
    type internal RuntimeWindowTarget =
        { GetState: unit -> Silk.NET.Windowing.WindowState
          SetState: Silk.NET.Windowing.WindowState -> unit
          GetBorder: unit -> Silk.NET.Windowing.WindowBorder
          SetBorder: Silk.NET.Windowing.WindowBorder -> unit
          GetPosition: unit -> Silk.NET.Maths.Vector2D<int>
          SetPosition: Silk.NET.Maths.Vector2D<int> -> unit
          GetSize: unit -> Silk.NET.Maths.Vector2D<int>
          SetSize: Silk.NET.Maths.Vector2D<int> -> unit
          GetWorkArea: unit -> (Silk.NET.Maths.Vector2D<int> * Silk.NET.Maths.Vector2D<int>) option }

    /// Per-window state that remembers normal-window geometry across fullscreen/borderless modes.
    type internal RuntimeWindowController

    val internal createRuntimeWindowController:
        initialWindowedSize: FS.GG.UI.Scene.Size -> RuntimeWindowController

    /// Apply one validated request idempotently. `Ok true` means native state changed, `Ok false`
    /// means the request already held, and `Error` is an observable Window-stage host failure.
    val internal applyRuntimeWindowBehavior:
        controller: RuntimeWindowController ->
        target: RuntimeWindowTarget ->
        behavior: RuntimeWindowBehavior ->
            Result<bool, RenderDiagnostic>

    /// #363: run a Silk window `Create`/`Initialize` with `WAYLAND_DISPLAY` nulled so GLFW picks the
    /// GLX/X11 backend on an XWayland session, restoring it immediately afterwards. Scoped to window
    /// creation only — never held across the render loop — and serialized against concurrent windows.
    /// A no-op off Linux or when only one display variable is set.
    val internal withWindowBackendOverride: action: (unit -> 'a) -> 'a

    /// Issue #400: the size-aware interactive host publishes the coordinate space it authored the live
    /// scene in (the physical framebuffer, when rendering at native resolution), so the present-time
    /// logical→physical fit collapses to the identity instead of scaling an already-native scene a
    /// second time. `None` restores the #364 default (author in the logical window, fit up). A per-run
    /// module static, reset when `run` begins.
    val internal setLiveAuthoringSizeOverride: size: FS.GG.UI.Scene.Size option -> unit

    /// Feature 147: integer framebuffer scissor rectangle used by the proof and partial-redraw
    /// decision helpers. Coordinates are clamped to the framebuffer before use.
    type ScissorRect =
        { X: int
          Y: int
          Width: int
          Height: int }

    /// Feature 147: pure decision for whether a frame may use scissored redraw or must use full redraw.
    type ScissorDecision =
        | Scissored of ScissorRect list
        | FullRedraw of reason: string

    [<RequireQualifiedAccess>]
    /// Feature 157: reviewer-visible damage validation classification before the host can skip a full clear.
    type DamageValidationStatus =
        | Valid
        | EmptyNoChange
        | EmptyVisibleChange
        | OutOfBounds
        | Stale
        | Duplicated
        | Incomplete
        | Ambiguous
        | FullFrameInvalidation

    [<RequireQualifiedAccess>]
    /// Feature 157: retained previous-frame backing state used by the no-clear path gate.
    type RetainedBackingStatus =
        | CurrentBufferPreserved
        | RetainedFrameRestored
        | Missing
        | Stale
        | CrossRun
        | CrossProfile
        | Resized
        | ResourceFailed

    /// Feature 157: damage validation result after framebuffer-coordinate clipping.
    type DamageValidationResult =
        { Status: DamageValidationStatus
          Rects: ScissorRect list
          UnionArea: int
          Reason: string option }

    /// Feature 183 (US3): the five damage-classification flags `validateDamage` takes, named so they
    /// cannot be transposed at the call site (a swap is now a compile error). Values/results unchanged.
    type DamageValidationFlags =
        { VisibleChange: bool
          FullFrameInvalidation: bool
          StaleDamage: bool
          IncompleteDamage: bool
          AmbiguousDamage: bool }

    [<RequireQualifiedAccess>]
    /// Feature 157: host render decision for the no-clear damage-scissored branch.
    type DamageRenderDecisionKind =
        | DamageScopedAccepted
        | FullRedraw
        | SkipNoChange
        | Rejected
        | EnvironmentLimited

    /// Feature 157: package-visible diagnostic summary for one render decision.
    type DamageRenderDecision =
        { Kind: DamageRenderDecisionKind
          ScissorRects: ScissorRect list
          DamageArea: int
          FallbackReason: string option
          ProofGate: string
          RetainedBacking: string
          Parity: string }

    /// Feature 157: pure eligibility inputs for deciding whether the no-clear path may run.
    type DamageRenderEligibility =
        { Proof: CompositorProof.ProofReadiness
          RetainedBacking: RetainedBackingStatus
          Damage: ScissorRect list
          FrameWidth: int
          FrameHeight: int
          VisibleChange: bool
          FullFrameInvalidation: bool
          StaleDamage: bool
          IncompleteDamage: bool
          AmbiguousDamage: bool
          ResourcesAvailable: bool
          ParityAccepted: bool }

    /// Feature 153: pure host facts used to classify whether a live sentinel/damage proof can run.
    type LiveProofHostFacts =
        { Display: string option
          WaylandDisplay: string option
          SessionType: string option
          Renderer: string option
          ReadbackAvailable: bool
          PermissionGranted: bool
          TimedOut: bool }

    /// Feature 167: receipt callback facts captured before queued processing/render work.
    type InputReceiptDiagnostic =
        { SequenceId: int64
          InputKind: string
          ReceivedAt: System.DateTimeOffset
          CallbackDuration: System.TimeSpan
          QueueDepthAtReceipt: int
          SignalRequested: bool
          RenderWorkStarted: bool }

    /// Feature 167: presentation boundary timing facts for latency records.
    type PresentationTimingDiagnostic =
        { PresentedFrameId: int64
          PaintDuration: System.TimeSpan option
          PresentDuration: System.TimeSpan option
          EnvironmentStatus: string }

    [<RequireQualifiedAccess>]
    /// Feature 153: live proof host classification before attempting to accept evidence.
    type LiveProofHostReadiness =
        | Capable
        | MissingDisplay
        | MissingRenderer
        | ReadbackUnavailable
        | PermissionDenied
        | Timeout
        | HostError of string

    /// Issue #184: the host contract's button identity for a raw Silk.NET `MouseButton` code, or
    /// `None` for a button it cannot carry — Silk's `Unknown`, and the extra `Button4`..`Button12`
    /// that back/forward and thumb buttons arrive on. The caller drops such an event and reports it;
    /// coercing a thumb-button press onto `PrimaryButton` is the one answer that cannot be right.
    /// Takes the raw code rather than `Silk.NET.Input.MouseButton` to keep the input binding out of
    /// this package's public surface, as `FrameFailureFacts.GraphicsResetStatus` does for GL.
    val mapPointerButton: buttonCode: int -> ViewerPointerButton option

    /// Issue #179: `glGetGraphicsResetStatus` on a context that has not been reset — and also what a
    /// context without `GL_KHR_robustness` always reports, so a reset is a positive signal only.
    val glNoError: uint32

    /// Issue #179: the driver/window facts a failed frame is classified from. `GraphicsResetStatus` is
    /// the raw `glGetGraphicsResetStatus` code (`glNoError`, or a guilty/innocent/unknown reset),
    /// carried as a code rather than Silk's `GLEnum` to keep the GL binding out of this package's
    /// public surface. `ContextAbandoned` is the signal Skia always maintains.
    type FrameFailureFacts =
        { GraphicsResetStatus: uint32
          ContextAbandoned: bool
          GlContextCurrent: bool
          WindowSystemPresent: bool }

    [<RequireQualifiedAccess>]
    /// Issue #179: what a failed frame means. Constitution VI requires an implementation defect to
    /// stay distinguishable from a lost device and from a missing window system.
    type FrameFailureKind =
        | DeviceLost
        | WindowSystemUnavailable
        | TransientDrawFailure

    [<RequireQualifiedAccess>]
    /// Issue #179: what the persistent loop does about a failed frame. There is no context-recreation
    /// path, so an unrecoverable frame tears the run down explicitly rather than spinning on it.
    type FrameFailureAction =
        | RetryFrame of attempt: int
        | TeardownRun of reason: string

    /// Issue #179: consecutive transient frame failures tolerated before the run is torn down.
    val transientFrameRetryBudget: int

    /// Issue #179: classify a failed frame from driver/window facts alone.
    val classifyFrameFailure: facts: FrameFailureFacts -> FrameFailureKind

    /// Issue #179: decide what a failed frame does to the run. `consecutiveFailures` counts this
    /// failure, so the first failed frame passes 1. A lost device and a vanished window system are
    /// terminal immediately; a transient draw failure is retried up to `retryBudget` times.
    val decideFrameFailure:
        kind: FrameFailureKind -> consecutiveFailures: int -> retryBudget: int -> FrameFailureAction

    /// Issue #179: the failure streak a run carries across frames.
    type FrameFailureTracker =
        { mutable ConsecutiveFailures: int }

    /// Issue #179: a fresh streak for a new run.
    val newFrameFailureTracker: unit -> FrameFailureTracker

    /// Issue #179: a presented frame clears the streak, so the retry budget bounds *consecutive*
    /// failures rather than failures over the life of the window.
    val observeFramePresented: tracker: FrameFailureTracker -> unit

    /// Issue #179: fold a failed frame into the streak and decide what the run does about it. This
    /// is the accumulation the live loop performs, exposed so it can be driven frame by frame.
    val observeFrameFailed:
        tracker: FrameFailureTracker -> facts: FrameFailureFacts -> retryBudget: int -> FrameFailureAction

    /// Issue #365: run a product `Update`/`View` step that must not kill the persistent window. On
    /// success, `Some` its result; on a product-raised exception, `None` plus exactly one `App`-staged
    /// diagnostic through `report` — never the mislabeled `frameRenderFailed`, and never a retry (a
    /// product-code fault is deterministic, so the offending step is dropped and the window kept alive).
    /// This is exactly the guard the live `dispatch` loop performs, exposed so it can be driven directly.
    val tryProductStep: report: (RenderDiagnostic -> unit) -> phase: string -> step: (unit -> 'a) -> 'a option

    /// Public contract function exposed by this FS.GG.UI package. Signature shape preserved
    /// from the former VulkanHost.run so Host/Viewer.fs routes unchanged. A run whose frame loop
    /// was abandoned (issue #179) returns the fatal diagnostic that ended it.
    val run: program: ViewerProgram<'model, 'msg> -> Result<unit, RenderDiagnostic>

    /// Feature 120 (US1, FR-001/002): the most recent present's per-phase durations — the scene→canvas
    /// paint walk and the flush + buffer-swap (compose). Live-only, non-golden; consumed by the
    /// interactive adapter's `FrameMetrics.PaintDuration`/`ComposeDuration` and the timing baseline.
    val lastPresentTiming: unit -> System.TimeSpan * System.TimeSpan

    /// Feature 120 (US2): pure present-or-skip decision (present iff first frame, scene changed, or the
    /// framebuffer size changed). Exposed for the idle-skip transition test (T016).
    val shouldPresent:
        prev: FS.GG.UI.Scene.Scene option -> next: FS.GG.UI.Scene.Scene -> sizeChanged: bool -> bool

    /// Feature 167: build a native receipt diagnostic that proves callback work stopped before rendering.
    val recordInputReceipt:
        sequenceId: int64 ->
        inputKind: string ->
        queueDepthAtReceipt: int ->
        callbackDuration: System.TimeSpan ->
        signalRequested: bool ->
        renderWorkStarted: bool ->
            InputReceiptDiagnostic

    /// Feature 167: classify whether the receipt callback stayed within both receipt budgets.
    val receiptWithinBudget:
        inputReceiptP95: System.TimeSpan ->
        inputReceiptMax: System.TimeSpan ->
        receipt: InputReceiptDiagnostic ->
            bool

    /// Feature 167: true only when the receipt callback started render/present work.
    val receiptDidRenderWork: receipt: InputReceiptDiagnostic -> bool

    /// Feature 167: build presentation boundary facts from host timings.
    val presentationTiming:
        frameId: int64 ->
        paintDuration: System.TimeSpan option ->
        presentDuration: System.TimeSpan option ->
        liveSurfaceAvailable: bool ->
            PresentationTimingDiagnostic

    [<RequireQualifiedAccess>]
    /// Feature 122 (FR-001/002): what the live DirectToSwapchain host does for one frame — paint a
    /// fresh frame and present it, re-present the cached last good frame to fill a swapchain buffer, or
    /// fully idle.
    type PresentAction =
        | PaintAndPresent
        | RepresentLastGood
        | SkipPresent

    /// Feature 122 (FR-001/002): the pure present decision. `PaintAndPresent` when `shouldPresent`;
    /// otherwise `RepresentLastGood` while `idleRepresentsRemaining > 0` (buffers may still be undrawn),
    /// else `SkipPresent` (full idle). Keeping every swapchain buffer populated stops a multi-buffer
    /// compositor (Wayland windowed-fullscreen) from rotating an undrawn black buffer into view.
    /// Exposed for the present-plan transition test (T011).
    val planPresent:
        prev: FS.GG.UI.Scene.Scene option ->
        next: FS.GG.UI.Scene.Scene ->
        sizeChanged: bool ->
        idleRepresentsRemaining: int ->
            PresentAction

    /// Feature 121 (US1, FR-002): pure frame-pacing decision — advance (update + present) iff at least
    /// `frameInterval` seconds elapsed since the last advance. Gates DoUpdate AND DoRender so the
    /// `ViewerOptions.FrameRateCap` bounds render cadence. Exposed for the pacing test (T006).
    val shouldAdvanceFrame: lastFrameTime: float -> now: float -> frameInterval: float -> bool

    /// Feature 147: clamp damage rectangles to the framebuffer and discard empty regions.
    val normalizeScissorRects:
        frameWidth: int ->
        frameHeight: int ->
        rects: ScissorRect list ->
            ScissorRect list

    /// Feature 147: deterministic area of the scissor set after clipping.
    val scissorArea: rects: ScissorRect list -> int

    /// Feature 147: decide if the host may use scissored redraw for this frame.
    val decideScissorRedraw:
        proof: CompositorProof.ProofReadiness ->
        fullFrameInvalidation: bool ->
        damage: ScissorRect list ->
        frameWidth: int ->
        frameHeight: int ->
            ScissorDecision

    /// Feature 157: classify damage before any no-clear paint is attempted.
    val validateDamage:
        damage: ScissorRect list ->
        frameWidth: int ->
        frameHeight: int ->
        flags: DamageValidationFlags ->
            DamageValidationResult

    /// Feature 157: decide if the real DirectToSwapchain no-clear path may be selected.
    val decideDamageScopedRender:
        eligibility: DamageRenderEligibility ->
            DamageRenderDecision

    /// Feature 153: classify host facts without opening native resources.
    val classifyLiveProofHost: facts: LiveProofHostFacts -> LiveProofHostReadiness

    /// Feature 153: build the proof host profile used by live proof attempts.
    val liveProofHostProfile: facts: LiveProofHostFacts -> CompositorProof.HostProfile
