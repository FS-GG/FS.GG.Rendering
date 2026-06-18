namespace Rendering.Harness

/// Performance tier (T3). Drives the viewer's bounded-run path to record render-timing evidence.
/// It declares whether a mode is deterministic / live-host / timing evidence, and — critically —
/// only claims `vsync-faithful` when the probe supplies present facts (swap-control + vblank).
/// The headless bounded path yields throughput timing, NOT a faithful per-frame vsync distribution
/// (that needs the live tier); the evidence discloses this so it cannot overclaim.
module Perf =

    type PerfMode =
        | Throughput
        | Paced60
        | PacedNative
        | StressResize
        | InputLatency

    /// The evidence kind a mode produces.
    type PerfKind =
        | DeterministicKind
        | LiveHostKind
        | TimingKind

    type TimingPath =
        | FullRedraw
        | DamageScoped

    type TimingSample =
        { ScenarioId: string
          Path: TimingPath
          RunId: string
          HostProfileId: string
          DurationMs: float
          ArtifactPath: string }

    type SampleDistribution =
        { Count: int
          P50Ms: float
          P95Ms: float
          P99Ms: float
          MinMs: float
          MaxMs: float
          RawSamplePath: string }

    type TimingVerdict =
        | Positive
        | Noisy
        | NonBeneficial
        | Incomplete
        | Rejected
        | EnvironmentLimited
        | Limited

    type ScenarioTimingDecision =
        { NoiseBandMs: float
          Verdict: TimingVerdict
          ConfidenceDecision: string
          Reasons: string list }

    type MeasurementPolicy =
        | ReadbackFree
        | ReadbackOutsideMeasurement
        | ProbeReadbackIncluded
        | Unverified
        | Missing

    type InclusionStatus =
        | Included
        | Excluded
        | Probe

    type ExclusionReason =
        | TimedOut
        | Canceled
        | PartialEvidence
        | ProbeRunExcluded
        | ProofReadbackInMeasuredInterval
        | MissingMeasurementPolicy
        | MissingMetadata
        | UnverifiableMeasurementPolicy
        | CrossProfileEvidence
        | StaleEvidence
        | MixedPolicy
        | ScenarioDefinitionMismatch
        | PackageVersionMismatch
        | RunIdentityMismatch
        | UnsupportedHost
        | EnvironmentLimitedReason
        | ScenarioCoverageMissing
        | SamplePolicyMismatch
        | ArtifactUnreadable
        | ReadbackContaminated
        | FailedProofReadback

    type ClassifiedTimingSample =
        { ScenarioId: string
          ScenarioDefinitionId: string
          Path: TimingPath
          RunId: string
          HostProfileId: string
          PackageVersion: string
          DurationMs: float
          MeasurementPolicy: MeasurementPolicy
          InclusionStatus: InclusionStatus
          ExclusionReason: ExclusionReason option
          ArtifactPath: string }

    /// Parse a `--mode` token; `None` if unrecognised.
    val parseMode: token: string -> PerfMode option

    /// The evidence kind declared by a mode.
    val kindOf: mode: PerfMode -> PerfKind

    /// Run the T3 perf tier for `mode` over `frames` bounded frames; build the evidence.
    val runPerf: mode: PerfMode -> frames: int -> facts: ProbeFacts -> outDir: string -> Evidence.Evidence * float list

    val timingPathToken: path: TimingPath -> string

    val timingVerdictToken: verdict: TimingVerdict -> string

    val measurementPolicyToken: policy: MeasurementPolicy -> string

    val parseMeasurementPolicy: token: string -> MeasurementPolicy option

    val inclusionStatusToken: status: InclusionStatus -> string

    val exclusionReasonToken: reason: ExclusionReason -> string

    val classifyMeasurementPolicy:
        policy: MeasurementPolicy ->
        isExplicitProbe: bool ->
        readbackInMeasuredInterval: bool ->
            InclusionStatus * ExclusionReason option

    val classifyTimingSample:
        expectedRunId: string ->
        expectedHostProfileId: string ->
        expectedPackageVersion: string ->
        expectedScenarioDefinitions: Map<string, string> ->
        sample: ClassifiedTimingSample ->
            ClassifiedTimingSample

    val percentile: percentile: float -> samples: float list -> float option

    val summarizeSamples: rawSamplePath: string -> samples: float list -> SampleDistribution option

    val noiseBandMs: fullRedrawP50Ms: float -> float

    val evaluateScenario:
        measuredRepetitions: int ->
        fullRedraw: SampleDistribution option ->
        damageScoped: SampleDistribution option ->
            ScenarioTimingDecision
