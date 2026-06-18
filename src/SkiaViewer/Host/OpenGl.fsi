namespace FS.GG.UI.SkiaViewer.Host

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

/// The OpenGL/Skia presentation host body (internal helpers hidden; only `run` is reachable).
module GlHost =
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

    /// Public contract function exposed by this FS.GG.UI package. Signature shape preserved
    /// from the former VulkanHost.run so Host/Viewer.fs routes unchanged.
    val run: program: ViewerProgram<'model, 'msg> -> Result<unit, RenderDiagnostic>

    /// Feature 120 (US1, FR-001/002): the most recent present's per-phase durations — the scene→canvas
    /// paint walk and the flush + buffer-swap (compose). Live-only, non-golden; consumed by the
    /// interactive adapter's `FrameMetrics.PaintDuration`/`ComposeDuration` and the timing baseline.
    val lastPresentTiming: unit -> System.TimeSpan * System.TimeSpan

    /// Feature 120 (US2): pure present-or-skip decision (present iff first frame, scene changed, or the
    /// framebuffer size changed). Exposed for the idle-skip transition test (T016).
    val shouldPresent:
        prev: FS.GG.UI.Scene.Scene option -> next: FS.GG.UI.Scene.Scene -> sizeChanged: bool -> bool

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
        visibleChange: bool ->
        fullFrameInvalidation: bool ->
        staleDamage: bool ->
        incompleteDamage: bool ->
        ambiguousDamage: bool ->
            DamageValidationResult

    /// Feature 157: decide if the real DirectToSwapchain no-clear path may be selected.
    val decideDamageScopedRender:
        eligibility: DamageRenderEligibility ->
            DamageRenderDecision

    /// Feature 153: classify host facts without opening native resources.
    val classifyLiveProofHost: facts: LiveProofHostFacts -> LiveProofHostReadiness

    /// Feature 153: build the proof host profile used by live proof attempts.
    val liveProofHostProfile: facts: LiveProofHostFacts -> CompositorProof.HostProfile
