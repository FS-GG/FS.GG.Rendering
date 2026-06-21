namespace FS.GG.UI.SkiaViewer

open System
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene

/// Public contract module exposed by this FS.GG.UI package.
module Viewer =
    /// Public contract function exposed by this FS.GG.UI package.
    val timingPathToken: path: ViewerTimingPath -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val timingPathCanSupportClaim: path: ViewerTimingPath -> proofReadbackIncluded: bool -> validationReadbackIncluded: bool -> bool
    /// Feature 157: stable token for damage render decisions in readiness artifacts.
    val damageDecisionToken: decision: ViewerDamageDecision -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val init: options: ViewerOptions -> ViewerModel * ViewerEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val initWithWindowBehavior: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> ViewerModel * ViewerEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val update: msg: ViewerMsg -> model: ViewerModel -> ViewerModel * ViewerEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val initRun: request: ViewerRunRequest -> ViewerRunModel * ViewerRunEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val updateRun: msg: ViewerRunMsg -> model: ViewerRunModel -> ViewerRunModel * ViewerRunEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val defaultDiagnostics: ViewerDiagnosticsOptions

    /// F1 (Feature 175 general repaint signal): the single "runtime-state changed → repaint" policy
    /// shared by every viewer loop. `internal` — exposed only so the regression can assert the policy
    /// deterministically (the loops are GL/timing-bound). Returns `current` when the input produced
    /// product messages (dispatch already re-derived) and `deriveScene ()` otherwise (runtime state may
    /// have changed with no model change — focus/hover/scroll — so re-derive on THIS input).
    val internal runtimeStateRepaint: producedMessages: bool -> current: 'scene -> deriveScene: (unit -> 'scene) -> 'scene

    /// S3 (Feature 175) live-trace read-back. `traceStartCapture` begins in-memory capture of
    /// `RenderLagTrace` events (focus/hover/scroll/dispatch/timing); `traceDrainCapture` stops and
    /// returns them as `(event, fields)` tuples; `traceEmit` records one event. Lets a test or tool
    /// observe live state programmatically — without the FS_GG_RENDER_LAG_TRACE env var and without a
    /// repack-to-instrument loop. `internal` — diagnostic seam, not a product contract.
    val internal traceStartCapture: unit -> unit
    val internal traceDrainCapture: unit -> (string * (string * string) list) list
    val internal traceEmit: eventName: string -> fields: (string * string) list -> unit
    /// Default readiness budget for responsiveness diagnostics.
    val defaultResponsivenessBudget: ViewerResponsivenessBudget
    /// Default disabled responsiveness options.
    val defaultResponsivenessOptions: ViewerResponsivenessOptions
    /// Stable JSON/readiness token for input kinds.
    val responsivenessInputKindToken: kind: ViewerResponsivenessInputKind -> string
    /// Stable JSON/readiness token for visible responses.
    val responsivenessVisibleResponseToken: response: ViewerResponsivenessVisibleResponse -> string
    /// Stable JSON/readiness token for environment statuses.
    val responsivenessEnvironmentStatusToken: status: ViewerResponsivenessEnvironmentStatus -> string
    /// Stable JSON/readiness token for summary readiness.
    val responsivenessReadinessToken: readiness: ViewerResponsivenessReadiness -> string
    /// Empty scheduler queue with the next sequence id starting at 1.
    val emptyInputQueue: ViewerInputQueue
    /// Queue depth visible to a newly received input.
    val inputQueueDepth: queue: ViewerInputQueue -> int
    /// Enqueue an input, assigning sequence id, priority lane, receipt depth, and coalescing state.
    val enqueueInput:
        receivedAt: DateTimeOffset ->
        inputKind: ViewerResponsivenessInputKind ->
        payload: string ->
        queue: ViewerInputQueue ->
            ViewerInputEnvelope * ViewerInputQueue
    /// Drain pending inputs for one frame/update pass.
    val drainInputQueue: batchId: int64 -> drainReason: string -> queue: ViewerInputQueue -> ViewerFrameDrain * ViewerInputQueue
    /// Build the dirty-state decision from product/runtime/size/theme change facts.
    val dirtyState:
        productModelChanged: bool ->
        runtimeStateChanged: bool ->
        sizeChanged: bool ->
        themeChanged: bool ->
        dirtyRegion: ViewerResponsivenessDirtyRegion option ->
        reason: string list ->
            ViewerDirtyState
    /// True when the dirty-state requires retained-scene recomposition.
    val dirtyStateRequiresRecompose: dirty: ViewerDirtyState -> bool
    /// Create a stable-ish run id with the `resp-` prefix.
    val createResponsivenessRunId: unit -> string
    /// Encode one latency record as a JSONL line using stable lowercase tokens.
    val latencyRecordToJsonLine: latency: ViewerLatencyRecord -> string
    /// Summarize latency records into budget/readiness evidence.
    val summarizeResponsivenessRecords:
        runId: string ->
        scope: string ->
        recordsPath: string ->
        startedUtc: DateTimeOffset ->
        completedUtc: DateTimeOffset ->
        budget: ViewerResponsivenessBudget ->
        records: ViewerLatencyRecord list ->
            ViewerResponsivenessSummary
    /// Encode a responsiveness summary as machine-readable JSON.
    val responsivenessSummaryToJson: summary: ViewerResponsivenessSummary -> string
    /// Encode a responsiveness summary as reviewer-readable Markdown.
    val responsivenessSummaryToMarkdown: summary: ViewerResponsivenessSummary -> string
    /// Write records.jsonl, summary.json, summary.md, and environment.md under the output root/run id.
    val writeResponsivenessRun:
        outputRoot: string ->
        summary: ViewerResponsivenessSummary ->
        records: ViewerLatencyRecord list ->
            string list
    /// Public contract function exposed by this FS.GG.UI package.
    val defaultWindowBehavior: ViewerWindowBehaviorRequest
    /// Public contract function exposed by this FS.GG.UI package.
    val validateWindowBehavior: request: ViewerWindowBehaviorRequest -> ViewerWindowOptionResult list
    /// Public contract function exposed by this FS.GG.UI package.
    val validateWindowLaunchBehavior: initialSize: Size -> request: ViewerWindowBehaviorRequest -> ViewerWindowOptionResult list
    /// Public contract function exposed by this FS.GG.UI package.
    val classifyWindowState: diagnostic: ViewerWindowStateDiagnostic -> ViewerLifecycleState
    /// Public contract function exposed by this FS.GG.UI package.
    val shouldCaptureDiagnostic: options: ViewerDiagnosticsOptions -> diagnostic: ViewerDiagnosticEvent -> bool
    /// Public contract function exposed by this FS.GG.UI package.
    val captureDiagnostic: options: ViewerDiagnosticsOptions -> diagnostic: ViewerDiagnosticEvent -> ViewerDiagnosticEvent option
    /// Public contract function exposed by this FS.GG.UI package.
    val failureFromDiagnostic: diagnostic: ViewerDiagnosticEvent -> ViewerRunFailure
    /// Public contract function exposed by this FS.GG.UI package.
    val classifyWindowObservation: outcome: ViewerLaunchOutcome -> externalObservationAttempted: bool -> externalWindowMatched: bool option -> captureAttempted: bool -> captureSucceeded: bool option -> ViewerWindowObservationResult
    /// Public contract function exposed by this FS.GG.UI package.
    val desktopSessionDiagnostic: unit -> ViewerDesktopSessionDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val runtimeCapability: unit -> ViewerRuntimeCapability
    /// Public contract function exposed by this FS.GG.UI package.
    val run: options: ViewerOptions -> scene: SceneNode -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runApp: options: ViewerOptions -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runAppWithWindowBehavior: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Feature 085 — pointer-aware, size-aware durable launch. Routes native pointer events
    /// and window resizes to the host and renders the size-aware `View`; additive to
    /// `runApp`/`runAppWithWindowBehavior`, which stay intact (FR-004/FR-006/FR-009).
    val runInteractiveViewer: options: ViewerOptions -> host: InteractiveViewerHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// As `runInteractiveViewer` with an explicit window behavior.
    val runInteractiveViewerWithWindowBehavior: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> host: InteractiveViewerHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Launch `host` in the live persistent viewer, deliver a bounded script through the viewer input queue,
    /// wait for the final scripted response to present, then close.
    val runInteractiveViewerScript:
        options: ViewerOptions ->
        script: ViewerScriptInput list ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// As `runInteractiveViewerScript` with an explicit window behavior.
    val runInteractiveViewerScriptWithWindowBehavior:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        script: ViewerScriptInput list ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runAppEvidence: request: ViewerRunRequest -> options: ViewerOptions -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runBounded: request: ViewerRunRequest -> options: ViewerOptions -> scene: SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runUntilFirstFrame: options: ViewerOptions -> scene: SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runForFrames: frameCount: int -> options: ViewerOptions -> scene: SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val captureScreenshotEvidence: request: ScreenshotEvidenceRequest -> options: ViewerOptions -> scene: SceneNode -> ScreenshotEvidenceResult
    /// Public contract function exposed by this FS.GG.UI package.
    val initEvidenceWorkflow: request: ScreenshotEvidenceRequest -> EvidenceWorkflowModel * EvidenceWorkflowEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val updateEvidenceWorkflow: msg: EvidenceWorkflowMsg -> model: EvidenceWorkflowModel -> EvidenceWorkflowModel * EvidenceWorkflowEffect list

/// Public contract module exposed by this FS.GG.UI package.
module GeneratedAppHost =
    /// Public contract function exposed by this FS.GG.UI package.
    val dispatchKey: host: GeneratedAppHost<'model,'msg> -> raw: ViewerKeyEvent -> model: 'model -> 'model * ViewerEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val smoke: host: GeneratedAppHost<'model,'msg> -> request: ViewerRunRequest -> Result<ViewerRunEvidence, ViewerRunFailure>

/// Feature 136 (R2/FR-001/FR-002): the rendering-edge text seam — install the bundled-font
/// real-metrics measurer (so control box sizing equals draw width) and read back per-page text
/// fallback/tofu disclosure after a render.
module Text =
    /// Install the bundled-font real-metrics measurer into the `Scene` measurement seam. Idempotent;
    /// call once at host startup before laying out control scenes.
    val installMeasurer: unit -> unit
    /// Install the HarfBuzz-backed shaping provider and matching shaped measurement seam.
    val installShapingProvider: unit -> Fonts.TextShapingProviderStatus
    /// Clear the shaping provider and use explicit fallback text measurement/render evidence.
    val clearShapingProvider: unit -> Fonts.TextShapingProviderStatus
    /// Read the active shaping provider state and diagnostics.
    val shapingProviderStatus: unit -> Fonts.TextShapingProviderStatus
    /// Shape a text value through the active provider/fallback path for diagnostic readback.
    val shapeText: text: string -> font: FontSpec -> ShapedTextResult
    /// Clear the text-fallback disclosure accumulator (the screenshot path also clears it per capture).
    val resetFallbackDisclosure: unit -> unit
    /// Aggregate disclosure (substituted/tofu counts + affected code points) for the most recent render.
    val fallbackReport: unit -> Fonts.FallbackReport
    /// Structured diagnostic lines for every non-authored character in the most recent render (FR-001).
    val fallbackDiagnostics: unit -> string list
