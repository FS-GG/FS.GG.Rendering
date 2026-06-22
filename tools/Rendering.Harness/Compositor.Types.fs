namespace Rendering.Harness.Compositor

open Rendering.Harness

open System
open System.IO
open System.Security.Cryptography
open System.Text

module Types =
    type HostProfile =
        { ProfileId: string
          Backend: string
          Renderer: string option
          PresentMode: string
          FramebufferSize: string
          Scale: float option
          DisplayEnvironment: string
          ProofAlgorithmVersion: string }

    type ProofVerdict =
        | ProofPassed
        | ProofFailed of cause: string
        | ProofEnvironmentLimited of reason: string

    type PresentProof =
        { ProofId: string
          HostProfile: HostProfile
          ScenarioId: string
          Verdict: ProofVerdict
          CreatedAt: DateTimeOffset
          EvidenceArtifacts: string list
          Diagnostics: string list }

    type ParityVerdict =
        | ParityPassed
        | ParityFailed of cause: string
        | ParitySkipped of reason: string
        | ParityEnvironmentLimited of reason: string

    type CompositorTier =
        | PresentProofTier
        | DamageScissorTier
        | PromotionTier
        | PlacementReuseTier
        | ReplayTier
        | SnapshotTier

    type TierVerdict =
        | Ready
        | Limited of reason: string
        | Rejected of reason: string
        | Skipped of reason: string

    type Thresholds =
        { PromotionReductionPercent: float
          SimpleSceneOverheadPercent: float
          SnapshotImprovementPercent: float }

    type SnapshotBudget =
        { MaxEntries: int
          MaxBytes: int64 }

    type ReadinessModel =
        { Proofs: PresentProof list
          Parity: Map<string, ParityVerdict>
          TierVerdicts: Map<CompositorTier, TierVerdict>
          Diagnostics: string list }

    type ReadinessMsg =
        | ProofLoaded of PresentProof
        | ParityRecorded of scenarioId: string * verdict: ParityVerdict
        | TierEvaluated of tier: CompositorTier * verdict: TierVerdict
        | DiagnosticRecorded of string

    type ReadinessEffect =
        | WriteValidationSummary of path: string
        | WriteCompatibilityLedger of path: string

    type Feature154Model =
        { ProofStatus: string
          ParityStatus: string
          TimingStatus: string
          PublishedArtifacts: string list }

    type Feature154Msg =
        | ProofEvidenceRecorded of status: string
        | ParityEvidenceRecorded of status: string
        | TimingEvidenceRecorded of status: string
        | ArtifactPublished of path: string

    type Feature154Effect =
        | WriteFeature154Artifact of path: string

    type Feature156ScenarioVerdict =
        | Feature156Positive
        | Feature156Noisy
        | Feature156NonBeneficial
        | Feature156Incomplete
        | Feature156Rejected
        | Feature156EnvironmentLimited
        | Feature156Limited

    type Feature156PathDistribution =
        { SampleCount: int
          P50Ms: float
          P95Ms: float
          P99Ms: float
          MinMs: float
          MaxMs: float
          RawSamplePath: string }

    type Feature156ScenarioReport =
        { ScenarioId: string
          FullRedraw: Feature156PathDistribution option
          DamageScoped: Feature156PathDistribution option
          WarmupCount: int
          MeasuredRepetitions: int
          NoiseBandMs: float
          Verdict: Feature156ScenarioVerdict
          ConfidenceDecision: string
          ArtifactPaths: string list
          RejectionReasons: string list
          ProofOverheadIncluded: bool }

    type Feature156TimingSummary =
        { RunId: string
          HostProfile: HostProfile
          PolicyId: string
          WarmupCount: int
          MeasuredRepetitions: int
          ScenarioReports: Feature156ScenarioReport list
          OverallVerdict: Feature156ScenarioVerdict
          ShippedPerformanceClaim: string
          Diagnostics: string list }

    type Feature156Model =
        { RunId: string
          ExpectedProfileId: string
          ActiveProfile: HostProfile option
          PolicyId: string option
          WarmupCount: int
          MeasuredRepetitions: int
          ScenarioReports: Feature156ScenarioReport list
          PublishedArtifacts: string list
          Verdict: Feature156ScenarioVerdict
          Diagnostics: string list }

    type Feature156Msg =
        | Feature156HostProfileDetected of HostProfile
        | Feature156HostProfileRejected of reason: string
        | Feature156PolicyDeclared of policyId: string
        | Feature156ScenarioEvaluated of Feature156ScenarioReport
        | Feature156RunEnvironmentLimited of reason: string
        | Feature156SummaryPublished of path: string
        | Feature156DiagnosticRecorded of string

    type Feature156Effect =
        | Feature156DetectHostProfile
        | Feature156DeclarePolicy of policyId: string
        | Feature156PrepareScenario of scenarioId: string
        | Feature156MeasurePath of scenarioId: string * path: string
        | Feature156WriteArtifact of path: string

    [<RequireQualifiedAccess>]
    type Feature157DamageStatus =
        | Accepted
        | FallbackOnly
        | Rejected
        | EnvironmentLimited

    type Feature157DamageAttempt =
        { AttemptId: string
          RunId: string
          ScenarioId: string
          HostProfile: HostProfile
          ProofGate: string
          RetainedBacking: string
          DamageValidationStatus: string
          RenderDecision: string
          FallbackReason: string option
          PreservedPixelEvidence: string
          DamagedPixelEvidence: string
          ParityStatus: string
          ArtifactPaths: string list
          Diagnostics: string list }

    type Feature157Fallback =
        { ScenarioId: string
          Reason: string
          DamageValidationStatus: string
          AcceptedPartialRedrawArtifacts: int
          ArtifactPaths: string list
          Diagnostics: string list }

    type Feature157DamageSummary =
        { RunId: string
          HostProfile: HostProfile
          Status: Feature157DamageStatus
          AcceptedAttempts: Feature157DamageAttempt list
          Fallbacks: Feature157Fallback list
          UnsupportedHostReason: string option
          ScenarioCoverage: string list
          PerformanceClaim: string
          Diagnostics: string list }

    type Feature157Model =
        { RunId: string
          ActiveProfile: HostProfile option
          Attempts: Feature157DamageAttempt list
          Fallbacks: Feature157Fallback list
          PublishedArtifacts: string list
          Status: Feature157DamageStatus
          Diagnostics: string list }

    type Feature157Msg =
        | Feature157HostProfileDetected of HostProfile
        | Feature157AttemptRecorded of Feature157DamageAttempt
        | Feature157FallbackRecorded of Feature157Fallback
        | Feature157UnsupportedHostRecorded of reason: string
        | Feature157ArtifactPublished of path: string
        | Feature157DiagnosticRecorded of string

    type Feature157Effect =
        | Feature157DetectHostProfile
        | Feature157LoadAcceptedProofGate
        | Feature157PrepareScenario of scenarioId: string
        | Feature157RenderDamageScopedFrame of scenarioId: string
        | Feature157RenderFullRedrawFrame of scenarioId: string
        | Feature157CompareParity of scenarioId: string
        | Feature157WriteArtifact of path: string

    [<RequireQualifiedAccess>]
    type Feature158ReadinessStatus =
        | Accepted
        | FallbackOnly
        | Rejected
        | EnvironmentLimited

    type Feature158TimingSample =
        { SampleId: string
          SampleIndex: int
          ScenarioId: string
          ScenarioDefinitionId: string
          Path: Perf.TimingPath
          RunId: string
          HostProfileId: string
          PackageVersion: string
          DurationMs: float
          MeasurementPolicy: Perf.MeasurementPolicy
          InclusionStatus: Perf.InclusionStatus
          ExclusionReason: Perf.ExclusionReason option
          ArtifactPath: string }

    type Feature158PathDistribution =
        { SampleCount: int
          P50Ms: float
          P95Ms: float
          P99Ms: float
          MinMs: float
          MaxMs: float
          RawSamplePath: string }

    type Feature158ScenarioReport =
        { ScenarioId: string
          ScenarioDefinitionId: string
          FullRedraw: Feature158PathDistribution option
          DamageScoped: Feature158PathDistribution option
          WarmupCount: int
          MeasuredRepetitions: int
          IncludedSamples: Feature158TimingSample list
          ExcludedSamples: Feature158TimingSample list
          ProofProbeArtifacts: string list
          Status: Feature158ReadinessStatus
          ArtifactPaths: string list
          Diagnostics: string list }

    type Feature158ProofProbeEvidence =
        { ProbeId: string
          HostProfile: HostProfile
          ScenarioIds: string list
          ReadbackArtifacts: string list
          ProbeSampleIds: string list
          ExclusionReason: Perf.ExclusionReason
          Diagnostics: string list }

    type Feature158TimingSummary =
        { RunId: string
          HostProfile: HostProfile
          PolicyId: string
          WarmupCount: int
          MeasuredRepetitions: int
          ScenarioReports: Feature158ScenarioReport list
          IncludedSamples: Feature158TimingSample list
          ExcludedSamples: Feature158TimingSample list
          ProofProbeEvidence: Feature158ProofProbeEvidence list
          UnsupportedHostReason: string option
          Feature156Comparison: string
          Status: Feature158ReadinessStatus
          PerformanceClaim: string
          Diagnostics: string list }

    type Feature158Model =
        { RunId: string
          ExpectedProfileId: string
          ActiveProfile: HostProfile option
          PolicyId: string option
          WarmupCount: int
          MeasuredRepetitions: int
          ScenarioReports: Feature158ScenarioReport list
          ProofProbeEvidence: Feature158ProofProbeEvidence list
          PublishedArtifacts: string list
          Status: Feature158ReadinessStatus
          Diagnostics: string list }

    type Feature158Msg =
        | Feature158HostProfileDetected of HostProfile
        | Feature158HostProfileRejected of reason: string
        | Feature158PolicyDeclared of policyId: string
        | Feature158ScenarioEvaluated of Feature158ScenarioReport
        | Feature158ProbeEvidenceRecorded of Feature158ProofProbeEvidence
        | Feature158RunEnvironmentLimited of reason: string
        | Feature158SummaryPublished of path: string
        | Feature158DiagnosticRecorded of string

    type Feature158Effect =
        | Feature158DetectHostProfile
        | Feature158DeclarePolicy of policyId: string
        | Feature158PrepareScenario of scenarioId: string
        | Feature158MeasurePath of scenarioId: string * path: string
        | Feature158CaptureProbeReadback of scenarioId: string
        | Feature158WriteArtifact of path: string

    [<RequireQualifiedAccess>]
    type Feature159ReadinessStatus =
        | Accepted
        | NonBeneficial
        | FallbackOnly
        | Rejected
        | EnvironmentLimited

    type Feature159Attempt =
        { AttemptId: string
          RunId: string
          ScenarioId: string
          HostProfile: HostProfile
          PolicyId: string
          PromotionDecision: string
          ReuseDecision: string
          ContentIdentity: string
          PlacementIdentity: string
          PrimaryReason: string option
          CounterNetSavedWork: int
          ParityStatus: string
          AcceptedReuseArtifacts: int
          AcceptedPromotionArtifacts: int
          ArtifactPaths: string list
          Diagnostics: string list }

    type Feature159Summary =
        { RunId: string
          HostProfile: HostProfile
          PolicyId: string
          Status: Feature159ReadinessStatus
          Attempts: Feature159Attempt list
          UnsupportedHostReason: string option
          RequiredScenarioCoverage: string list
          CounterNetSavedWork: int
          PerformanceClaim: string
          Diagnostics: string list }

    type Feature159Model =
        { RunId: string
          ActiveProfile: HostProfile option
          PolicyId: string option
          Attempts: Feature159Attempt list
          PublishedArtifacts: string list
          Status: Feature159ReadinessStatus
          Diagnostics: string list }

    type Feature159Msg =
        | Feature159HostProfileDetected of HostProfile
        | Feature159PolicyDeclared of policyId: string
        | Feature159AttemptRecorded of Feature159Attempt
        | Feature159RunEnvironmentLimited of reason: string
        | Feature159SummaryPublished of path: string
        | Feature159DiagnosticRecorded of string

    type Feature159Effect =
        | Feature159DetectHostProfile
        | Feature159DeclarePolicy of policyId: string
        | Feature159PrepareScenario of scenarioId: string
        | Feature159EvaluatePromotion of scenarioId: string
        | Feature159CompareParity of scenarioId: string
        | Feature159WriteArtifact of path: string

    [<RequireQualifiedAccess>]
    type Feature160ReadinessStatus =
        | Accepted
        | Blocked
        | Rejected
        | FallbackOnly
        | EnvironmentLimited

    type Feature160FullValidationRecord =
        { Command: string
          StartedAt: DateTimeOffset option
          CompletedAt: DateTimeOffset option
          Status: string
          ImplementationCommit: string
          PackageSurfaceBaseline: string
          ReadinessArtifactSet: string list
          ArtifactPaths: string list
          Diagnostics: string list }

    type Feature160Iteration =
        { IterationId: string
          RunId: string
          HostProfile: HostProfile
          LaneId: string
          PolicyId: string
          DeclaredBoundMinutes: int
          ActualDuration: TimeSpan
          WarmupCount: int
          MeasuredRepetitions: int
          ScenarioReports: Feature158ScenarioReport list
          ScenarioCoverage: string list
          IncludedSamples: Feature158TimingSample list
          ExcludedSamples: Feature158TimingSample list
          Status: Feature160ReadinessStatus
          ExclusionReason: Perf.ExclusionReason option
          ArtifactPaths: string list
          RestrictedScenario: string option
          Diagnostics: string list }

    type Feature160ThroughputSummary =
        { RunId: string
          HostProfile: HostProfile
          LaneId: string
          PolicyId: string
          DeclaredBoundMinutes: int
          RequiredAttempts: int
          WarmupCount: int
          MeasuredRepetitions: int
          Iterations: Feature160Iteration list
          UnsupportedHostReason: string option
          FullValidation: Feature160FullValidationRecord option
          CompatibilityImpact: string
          PackageValidationStatus: string
          RegressionValidationStatus: string
          Status: Feature160ReadinessStatus
          ReleaseReadyStatus: string
          PerformanceClaim: string
          Diagnostics: string list }

    type Feature160Model =
        { RunId: string
          ExpectedProfileId: string
          ActiveProfile: HostProfile option
          LaneId: string option
          PolicyId: string option
          DeclaredBoundMinutes: int option
          Iterations: Feature160Iteration list
          FullValidation: Feature160FullValidationRecord option
          PublishedArtifacts: string list
          Status: Feature160ReadinessStatus
          Diagnostics: string list }

    type Feature160Msg =
        | Feature160HostProfileDetected of HostProfile
        | Feature160HostProfileRejected of reason: string
        | Feature160LaneDeclared of laneId: string
        | Feature160PolicyDeclared of policyId: string
        | Feature160BoundDeclared of minutes: int
        | Feature160IterationStarted of iterationId: string
        | Feature160IterationCompleted of Feature160Iteration
        | Feature160IterationTimedOut of iterationId: string * reason: string
        | Feature160IterationCanceled of iterationId: string * reason: string
        | Feature160IterationExcluded of Feature160Iteration
        | Feature160FullValidationRecorded of Feature160FullValidationRecord
        | Feature160ArtifactPublished of path: string
        | Feature160DiagnosticRecorded of string

    type Feature160Effect =
        | Feature160DetectHostProfile
        | Feature160DeclareFocusedLane of laneId: string
        | Feature160DeclarePolicy of policyId: string
        | Feature160DeclareIterationBound of minutes: int
        | Feature160PrepareScenario of scenarioId: string
        | Feature160RunTimingWarmup of scenarioId: string
        | Feature160MeasurePath of scenarioId: string * path: string
        | Feature160EnforceIterationTimeout of iterationId: string * minutes: int
        | Feature160WriteRawSampleArtifact of path: string
        | Feature160WriteIterationArtifact of path: string
        | Feature160WriteExcludedEvidenceArtifact of path: string
        | Feature160WriteUnsupportedHostArtifact of path: string
        | Feature160WriteFullValidationRecord of path: string
        | Feature160WriteArtifact of path: string

    [<RequireQualifiedAccess>]
    type Feature161ReadinessStatus =
        | Accepted
        | Blocked
        | Rejected
        | FallbackOnly
        | EnvironmentLimited

    type Feature161HostFacts =
        { DisplayServer: string
          DisplayIdentity: string
          RendererIdentity: string
          DirectRendering: bool option
          RefreshRateHz: float option
          RefreshUnavailableReason: string option
          DriverIdentity: string
          PackageVersionSet: string
          CpuLoadNote: string
          GpuLoadNote: string
          EnvironmentLimits: string list
          HostProfile: HostProfile
          RunIdentity: string
          ScenarioIdentity: string
          TimingPolicyIdentity: string
          CollectionTime: DateTimeOffset
          ArtifactLocations: string list }

    type Feature161PriorGate =
        { Feature: string
          Status: string
          EvidencePath: string }

    type Feature161LedgerEntry =
        { EntryId: string
          LaneId: string
          HostFacts: Feature161HostFacts
          PriorGates: Feature161PriorGate list
          Status: Feature161ReadinessStatus
          PrimaryExclusionReason: Perf.ExclusionReason option
          TimingStatus: string
          AcceptedLaneScopedPerformanceArtifacts: int
          ArtifactPaths: string list
          Diagnostics: string list }

    type Feature161ClaimScope =
        { AcceptedLaneId: string option
          AppliesTo: string
          NonGeneralizedLanes: string list
          RemainingBlockers: string list
          PerformanceClaim: string }

    type Feature161Summary =
        { RunId: string
          HostProfile: HostProfile
          PolicyId: string
          Entries: Feature161LedgerEntry list
          UnsupportedHostReason: string option
          ClaimScope: Feature161ClaimScope
          FullValidationStatus: string
          CompatibilityImpact: string
          PackageValidationStatus: string
          RegressionValidationStatus: string
          Status: Feature161ReadinessStatus
          ReleaseReadyStatus: string
          PerformanceClaim: string
          Diagnostics: string list }

    type Feature161Model =
        { RunId: string
          ExpectedProfileId: string
          ActiveProfile: HostProfile option
          PolicyId: string option
          HostFacts: Feature161HostFacts option
          Entries: Feature161LedgerEntry list
          PriorGates: Feature161PriorGate list
          PublishedArtifacts: string list
          Status: Feature161ReadinessStatus
          Diagnostics: string list }

    type Feature161Msg =
        | Feature161HostProfileDetected of HostProfile
        | Feature161HostProfileRejected of reason: string
        | Feature161PolicyDeclared of policyId: string
        | Feature161HostFactsCollected of Feature161HostFacts
        | Feature161HostFactsRejected of reason: Perf.ExclusionReason * diagnostic: string
        | Feature161PriorGateLinked of Feature161PriorGate
        | Feature161LedgerEntryRecorded of Feature161LedgerEntry
        | Feature161ArtifactPublished of path: string
        | Feature161DiagnosticRecorded of string

    type Feature161Effect =
        | Feature161DetectHostProfile
        | Feature161DeclarePolicy of policyId: string
        | Feature161CollectHostFacts
        | Feature161LoadThroughputPackage of path: string
        | Feature161WriteHostFactsArtifact of path: string
        | Feature161WriteLedgerEntryArtifact of path: string
        | Feature161WriteExcludedEvidenceArtifact of path: string
        | Feature161WriteUnsupportedHostArtifact of path: string
        | Feature161WriteArtifact of path: string
