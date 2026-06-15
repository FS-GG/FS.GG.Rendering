namespace FS.GG.UI.SkiaViewer.Host

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
