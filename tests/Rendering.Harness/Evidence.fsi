namespace Rendering.Harness

/// The evidence-artifact contract: `run.json` + `metrics.csv` + `summary.md`. The formatters are
/// pure (testable without I/O); `write` persists them. Every artifact carries `proofLevel` and a
/// non-empty `notAuthoritativeFor` so it cannot overclaim.
module Evidence =

    /// One run's machine-readable evidence (the `run.json` shape).
    type Evidence =
        { RunId: string
          Tier: Tier
          Subcommand: string
          Status: RunStatus
          SkipReason: string option
          ProofLevel: ProofLevel
          AuthoritativeFor: string list
          NotAuthoritativeFor: string list
          Facts: ProbeFacts
          Frames: int
          P50Ms: float option
          P95Ms: float option
          P99Ms: float option
          Artifacts: string list }

    /// Overlay parity evidence gathered by Feature 144 harness tests.
    type OverlayEvidence =
        { ReplayLog: string list
          ProductMessages: string list
          HitOrder: string list
          Diagnostics: string list }

    /// Host capability state for Feature 145 overlay visual proof.
    type HostCapabilityStatus =
        | HostCapable
        | HostUnsupported
        | HostFailed

    /// Capture availability for the selected proof path.
    type HostCaptureAvailability =
        | CaptureAvailable
        | CaptureUnavailable of reason: string

    /// Feature 145 visual-proof run status.
    type VisualProofStatus =
        | VisualProofPassed
        | VisualProofFailed
        | VisualProofEnvironmentLimited

    /// Visual artifact state within the representative overlay flow.
    type VisualArtifactState =
        | OpenOverlay
        | ClosedOverlay

    /// Decoded pixel-content validation for a visual artifact.
    type VisualPixelContentValidation =
        | VisualPixelNonBlank
        | VisualPixelBlank
        | VisualPixelUnreadable of reason: string
        | VisualPixelInvalid of reason: string

    /// Source that produced a visual artifact.
    type VisualCaptureSource =
        | VisualLiveViewerWindow
        | VisualOffscreenHost
        | VisualSynthetic
        | VisualNoCapture

    /// Expected overlay state for visual/behavior correlation.
    type ExpectedOverlayState =
        | ExpectedOpen
        | ExpectedClosed

    /// Failure classification for Feature 145 readiness.
    type VisualProofFailureCategory =
        | NoFailure
        | Environment
        | Capture
        | OverlayBehavior
        | EvidenceBookkeeping

    /// Final decision for the Feature 144 visual-proof caveat.
    type ReadinessDecision =
        | CaveatClosed
        | CaveatEnvironmentGated
        | CaveatFailed

    /// Representative overlay flow selected for Feature 145 visual proof.
    type OverlayVisualProofScenario =
        { ScenarioId: string
          InputSequence: string list
          OpenStateStep: string
          ClosedStateStep: string
          ExpectedTopmostHitTarget: string
          ExpectedFocusState: string
          ExpectedDispatchSummary: string }

    /// Outcome of probing whether the current host may claim real visual proof.
    type HostCapabilityResult =
        { EffectiveBackend: string
          Display: string option
          GlRenderer: string option
          CaptureAvailability: HostCaptureAvailability
          Status: HostCapabilityStatus
          Owner: string
          Cause: string
          NextProofPath: string
          HostFacts: string list }

    /// Human-inspectable file captured for one scenario state.
    type VisualArtifact =
        { ArtifactId: string
          Path: string
          State: VisualArtifactState
          Width: int
          Height: int
          PixelContentValidation: VisualPixelContentValidation
          CaptureSource: VisualCaptureSource
          RunId: string
          ScenarioId: string
          CreatedAt: System.DateTimeOffset
          OverlayAboveContent: bool option
          TopmostHitTarget: string option
          NoStaleOverlayPixel: bool option }

    /// Metadata connecting pixels to deterministic overlay behavior.
    type OverlayVisualCorrelation =
        { ScenarioId: string
          InputStep: string
          ExpectedOverlayState: ExpectedOverlayState
          TopmostHitTarget: string option
          FocusState: string
          ProductDispatchSummary: string
          ReplayLogReference: string
          BehavioralEvidenceReference: string
          ArtifactPath: string
          OverlayAboveContent: bool option
          NoStaleOverlayPixel: bool option }

    /// Disclosure record when real visual proof cannot be produced in the current environment.
    type UnsupportedHostLimitation =
        { Owner: string
          Cause: string
          HostFacts: string list
          NextProofPath: string
          TrustRationale: string
          NotAuthoritativeFor: string list }

    /// Final readiness statement for the Feature 144 visual-proof caveat.
    type ReadinessCaveatDecision =
        { Caveat: string
          Decision: ReadinessDecision
          ArtifactPaths: string list
          LimitationDetails: UnsupportedHostLimitation option
          FailureCategory: VisualProofFailureCategory
          NextWorkstreamGuidance: string
          ReviewedAt: System.DateTimeOffset }

    /// One validation execution for the selected scenario.
    type VisualProofRun =
        { RunId: string
          ScenarioId: string
          HostCapability: HostCapabilityResult
          Status: VisualProofStatus
          OpenArtifact: VisualArtifact option
          ClosedArtifact: VisualArtifact option
          Correlations: OverlayVisualCorrelation list
          FailureCategory: VisualProofFailureCategory
          Limitation: UnsupportedHostLimitation option
          ReadinessDecision: ReadinessCaveatDecision option }

    /// Validation result for artifact or correlation acceptance.
    type VisualProofValidationResult =
        { Accepted: bool
          FailureCategory: VisualProofFailureCategory
          Diagnostics: string list }

    /// Stable string forms used in the artifacts.
    val tierToken: tier: Tier -> string
    val proofToken: proof: ProofLevel -> string
    val statusToken: status: RunStatus -> string
    val backendToken: backend: Backend -> string
    val hostCapabilityStatusToken: status: HostCapabilityStatus -> string
    val captureAvailabilityToken: availability: HostCaptureAvailability -> string
    val visualProofStatusToken: status: VisualProofStatus -> string
    val artifactStateToken: state: VisualArtifactState -> string
    val pixelContentToken: validation: VisualPixelContentValidation -> string
    val captureSourceToken: source: VisualCaptureSource -> string
    val expectedOverlayStateToken: state: ExpectedOverlayState -> string
    val failureCategoryToken: category: VisualProofFailureCategory -> string
    val readinessDecisionToken: decision: ReadinessDecision -> string

    /// Stable Feature 145 scenario constants.
    val feature145ReadinessDirectory: string
    val feature145ArtifactsDirectory: string
    val feature144DatePickerProofScenario: OverlayVisualProofScenario
    val visualArtifactRelativePath: runId: string -> state: VisualArtifactState -> string

    /// Render the `run.json` body (pure).
    val toJson: evidence: Evidence -> string

    /// Render `metrics.csv` from per-frame durations in milliseconds (pure).
    val metricsCsv: frameMs: float list -> string

    /// Render the human `summary.md` restating what the run proves and does NOT prove (pure).
    val toSummary: evidence: Evidence -> string

    /// Stable one-line summary for overlay parity evidence.
    val overlaySummary: evidence: OverlayEvidence -> string

    /// Validate that a visual artifact is current-run, non-empty, scenario-linked, and inside readiness artifacts.
    val validateVisualArtifact:
        readinessDir: string ->
        runId: string ->
        scenario: OverlayVisualProofScenario ->
        artifact: VisualArtifact ->
            VisualProofValidationResult

    /// Validate that visual metadata agrees with deterministic overlay behavior.
    val validateOverlayVisualCorrelation:
        scenario: OverlayVisualProofScenario ->
        artifact: VisualArtifact ->
        correlation: OverlayVisualCorrelation ->
            VisualProofValidationResult

    /// Render an unsupported-host limitation.
    val unsupportedHostLimitation:
        host: HostCapabilityResult ->
            UnsupportedHostLimitation

    /// Evaluate whether the Feature 144 caveat is closed, gated, or failed.
    val evaluateReadinessCaveat:
        run: VisualProofRun ->
            ReadinessCaveatDecision

    /// Render Feature 145 readiness records.
    val renderUnsupportedHostLimitation: limitation: UnsupportedHostLimitation -> string
    val renderCorrelation: run: VisualProofRun -> string
    val renderVisualProofRun: run: VisualProofRun -> string
    val renderReadinessDecision: decision: ReadinessCaveatDecision -> string

    /// Compute p50/p95/p99 (ms) from per-frame durations; `None` when there are no frames.
    val percentiles: frameMs: float list -> (float option * float option * float option)

    /// Persist `run.json`, `metrics.csv`, and `summary.md` into `dir`; returns the `run.json` path.
    val write: dir: string -> evidence: Evidence -> frameMs: float list -> string
