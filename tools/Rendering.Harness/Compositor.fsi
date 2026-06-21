namespace Rendering.Harness

open System

/// Feature 147 compositor proof, parity, threshold, and readiness contracts.
module Compositor =

    val featureId: string
    val feature148Id: string
    val feature149Id: string
    val feature152Id: string
    val feature153Id: string
    val feature154Id: string
    val feature155Id: string
    val feature156Id: string
    val feature157Id: string
    val feature158Id: string
    val feature159Id: string
    val feature160Id: string
    val feature161Id: string

    val readinessDirectory: string
    val presentProofDirectory: string
    val parityDirectory: string
    val perfDirectory: string
    val compatibilityLedgerPath: string
    val validationSummaryPath: string

    val feature148ReadinessDirectory: string
    val feature148LiveProofDirectory: string
    val feature148ParityDirectory: string
    val feature148ReuseDirectory: string
    val feature148SnapshotsDirectory: string
    val feature148TimingDirectory: string
    val feature148CompatibilityLedgerPath: string
    val feature148ValidationSummaryPath: string
    val feature148PackageVersion: string

    val feature149ReadinessDirectory: string
    val feature149LiveProofDirectory: string
    val feature149ParityDirectory: string
    val feature149ReuseDirectory: string
    val feature149SnapshotsDirectory: string
    val feature149TimingDirectory: string
    val feature149CompatibilityLedgerPath: string
    val feature149ValidationSummaryPath: string
    val feature149PackageVersion: string

    val feature152ReadinessDirectory: string
    val feature152LiveProofDirectory: string
    val feature152ParityDirectory: string
    val feature152TimingDirectory: string
    val feature152FsiDirectory: string
    val feature152CompatibilityLedgerPath: string
    val feature152ValidationSummaryPath: string
    val feature152PackageVersion: string

    val feature153ReadinessDirectory: string
    val feature153LiveProofDirectory: string
    val feature153LiveProofAttemptsDirectory: string
    val feature153LiveProofUnsupportedDirectory: string
    val feature153FsiDirectory: string
    val feature153ProofSetPath: string
    val feature153CompatibilityLedgerPath: string
    val feature153ValidationSummaryPath: string
    val feature153PackageValidationPath: string
    val feature153RegressionValidationPath: string
    val feature153PackageVersion: string

    val feature154ReadinessDirectory: string
    val feature154LiveProofDirectory: string
    val feature154LiveProofAttemptsDirectory: string
    val feature154LiveProofUnsupportedDirectory: string
    val feature154ParityDirectory: string
    val feature154TimingDirectory: string
    val feature154FsiDirectory: string
    val feature154ProofSetPath: string
    val feature154CompatibilityLedgerPath: string
    val feature154ValidationSummaryPath: string
    val feature154PackageValidationPath: string
    val feature154RegressionValidationPath: string
    val feature154PackageVersion: string

    val feature155ReadinessDirectory: string
    val feature155LiveProofDirectory: string
    val feature155LiveProofAttemptsDirectory: string
    val feature155LiveProofUnsupportedDirectory: string
    val feature155ParityDirectory: string
    val feature155TimingDirectory: string
    val feature155FsiDirectory: string
    val feature155ProofSetPath: string
    val feature155CompatibilityLedgerPath: string
    val feature155ValidationSummaryPath: string
    val feature155PackageValidationPath: string
    val feature155RegressionValidationPath: string
    val feature155PackageVersion: string

    val feature156ReadinessDirectory: string
    val feature156TimingDirectory: string
    val feature156TimingScenariosDirectory: string
    val feature156TimingRawDirectory: string
    val feature156TimingUnsupportedDirectory: string
    val feature156FsiDirectory: string
    val feature156CompatibilityLedgerPath: string
    val feature156ValidationSummaryPath: string
    val feature156PackageValidationPath: string
    val feature156RegressionValidationPath: string
    val feature156TimingSummaryPath: string
    val feature156PackageVersion: string
    val feature156AcceptedProfileId: string
    val feature156PolicyId: string

    val feature157ReadinessDirectory: string
    val feature157DamageDirectory: string
    val feature157DamageAttemptsDirectory: string
    val feature157DamageFallbacksDirectory: string
    val feature157DamageParityDirectory: string
    val feature157DamageUnsupportedDirectory: string
    val feature157FsiDirectory: string
    val feature157CompatibilityLedgerPath: string
    val feature157ValidationSummaryPath: string
    val feature157PackageValidationPath: string
    val feature157RegressionValidationPath: string
    val feature157DamageSummaryPath: string
    val feature157DamageSummaryJsonPath: string
    val feature157AcceptedProfileId: string

    val feature158ReadinessDirectory: string
    val feature158TimingDirectory: string
    val feature158TimingScenariosDirectory: string
    val feature158TimingRawDirectory: string
    val feature158TimingExcludedDirectory: string
    val feature158TimingUnsupportedDirectory: string
    val feature158ProofProbesDirectory: string
    val feature158FsiDirectory: string
    val feature158SurfaceBaselinesDirectory: string
    val feature158CompatibilityLedgerPath: string
    val feature158ValidationSummaryPath: string
    val feature158PackageValidationPath: string
    val feature158RegressionValidationPath: string
    val feature158TimingSummaryPath: string
    val feature158TimingSummaryJsonPath: string
    val feature158AcceptedProfileId: string
    val feature158PolicyId: string
    val feature158PerformanceCommand: string
    val feature158ProbeCommand: string
    val feature158ReadinessCommand: string

    val feature159ReadinessDirectory: string
    val feature159PromotionDirectory: string
    val feature159PromotionAttemptsDirectory: string
    val feature159PromotionReuseDirectory: string
    val feature159PromotionDemotionsDirectory: string
    val feature159PromotionFallbacksDirectory: string
    val feature159PromotionParityDirectory: string
    val feature159PromotionUnsupportedDirectory: string
    val feature159CountersDirectory: string
    val feature159FsiDirectory: string
    val feature159CompatibilityLedgerPath: string
    val feature159ValidationSummaryPath: string
    val feature159PackageValidationPath: string
    val feature159RegressionValidationPath: string
    val feature159PromotionSummaryPath: string
    val feature159AcceptedProfileId: string
    val feature159PolicyId: string
    val feature159PromotionCommand: string
    val feature159ReadinessCommand: string

    val feature160ReadinessDirectory: string
    val feature160ThroughputDirectory: string
    val feature160ThroughputIterationsDirectory: string
    val feature160ThroughputRawDirectory: string
    val feature160ThroughputExcludedDirectory: string
    val feature160ThroughputUnsupportedDirectory: string
    val feature160FullValidationDirectory: string
    val feature160FsiDirectory: string
    val feature160CompatibilityLedgerPath: string
    val feature160ValidationSummaryPath: string
    val feature160PackageValidationPath: string
    val feature160RegressionValidationPath: string
    val feature160ThroughputSummaryPath: string
    val feature160ThroughputSummaryJsonPath: string
    val feature160AcceptedProfileId: string
    val feature160PolicyId: string
    val feature160FocusedLaneId: string
    val feature160RequiredAttempts: int
    val feature160MaxIterationMinutes: int
    val feature160UnsupportedHostMinutes: int
    val feature160PerformanceCommand: string
    val feature160ReadinessCommand: string

    val feature161ReadinessDirectory: string
    val feature161LaneLedgerDirectory: string
    val feature161LaneLedgerEntriesDirectory: string
    val feature161LaneLedgerHostFactsDirectory: string
    val feature161LaneLedgerExcludedDirectory: string
    val feature161LaneLedgerUnsupportedDirectory: string
    val feature161FullValidationDirectory: string
    val feature161FsiDirectory: string
    val feature161CompatibilityLedgerPath: string
    val feature161ValidationSummaryPath: string
    val feature161PackageValidationPath: string
    val feature161RegressionValidationPath: string
    val feature161LaneLedgerSummaryPath: string
    val feature161LaneLedgerSummaryJsonPath: string
    val feature161AcceptedProfileId: string
    val feature161PolicyId: string
    val feature161HostLaneId: string
    val feature161PerformanceCommand: string
    val feature161ReadinessCommand: string

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

    val thresholds: Thresholds
    val snapshotBudget: SnapshotBudget
    val scenarioIds: string list
    val targetHostProfiles: HostProfile list
    val feature148ScenarioIds: string list
    val feature148TargetHostProfiles: HostProfile list
    val feature148TimingTiers: string list
    val feature149ScenarioIds: string list
    val feature149TargetHostProfiles: HostProfile list
    val feature149TimingTiers: string list
    val feature152ScenarioIds: string list
    val feature152TargetHostProfiles: HostProfile list
    val feature152TimingTiers: string list
    val feature153ScenarioIds: string list
    val feature153TargetHostProfiles: HostProfile list
    val feature154ScenarioIds: string list
    val feature154TargetHostProfiles: HostProfile list
    val feature154TimingTiers: string list
    val feature155ScenarioIds: string list
    val feature155TargetHostProfiles: HostProfile list
    val feature155TimingTiers: string list
    val feature156ScenarioIds: string list
    val feature156RequiredScenarioIds: string list
    val feature156TargetHostProfiles: HostProfile list
    val feature157RequiredScenarioIds: string list
    val feature157FallbackScenarioIds: string list
    val feature157ScenarioIds: string list
    val feature157TargetHostProfiles: HostProfile list
    val feature158RequiredScenarioIds: string list
    val feature158ScenarioIds: string list
    val feature158TargetHostProfiles: HostProfile list
    val feature159RequiredScenarioIds: string list
    val feature159FallbackScenarioIds: string list
    val feature159ScenarioIds: string list
    val feature159TargetHostProfiles: HostProfile list
    val feature160RequiredScenarioIds: string list
    val feature160ScenarioIds: string list
    val feature160TargetHostProfiles: HostProfile list
    val feature161RequiredScenarioIds: string list
    val feature161NonGeneralizedLanes: string list
    val feature161PriorGateLinks: Feature161PriorGate list

    val hostProfileFromFacts: facts: ProbeFacts -> HostProfile
    val proofVerdictToken: verdict: ProofVerdict -> string
    val parityVerdictToken: verdict: ParityVerdict -> string
    val tierToken: tier: CompositorTier -> string
    val tierVerdictToken: verdict: TierVerdict -> string

    val proofMatchesHost: active: HostProfile -> proof: PresentProof -> bool
    val proofIsFresh: now: DateTimeOffset -> maxAge: TimeSpan -> proof: PresentProof -> bool
    val validateProofForScissoring: active: HostProfile -> now: DateTimeOffset -> maxAge: TimeSpan -> proof: PresentProof option -> TierVerdict
    val evaluateTier: proof: TierVerdict -> parity: ParityVerdict option -> performancePassed: bool option -> TierVerdict

    val initReadiness: unit -> ReadinessModel * ReadinessEffect list
    val updateReadiness: msg: ReadinessMsg -> model: ReadinessModel -> ReadinessModel * ReadinessEffect list
    val initFeature154: unit -> Feature154Model * Feature154Effect list
    val updateFeature154: msg: Feature154Msg -> model: Feature154Model -> Feature154Model * Feature154Effect list
    val initFeature156: warmupCount: int -> measuredRepetitions: int -> Feature156Model * Feature156Effect list
    val updateFeature156: msg: Feature156Msg -> model: Feature156Model -> Feature156Model * Feature156Effect list
    val initFeature157: unit -> Feature157Model * Feature157Effect list
    val updateFeature157: msg: Feature157Msg -> model: Feature157Model -> Feature157Model * Feature157Effect list
    val initFeature158: warmupCount: int -> measuredRepetitions: int -> Feature158Model * Feature158Effect list
    val updateFeature158: msg: Feature158Msg -> model: Feature158Model -> Feature158Model * Feature158Effect list
    val initFeature159: unit -> Feature159Model * Feature159Effect list
    val updateFeature159: msg: Feature159Msg -> model: Feature159Model -> Feature159Model * Feature159Effect list
    val initFeature160: attempts: int -> maxIterationMinutes: int -> Feature160Model * Feature160Effect list
    val updateFeature160: msg: Feature160Msg -> model: Feature160Model -> Feature160Model * Feature160Effect list
    val initFeature161: sourceThroughput: string option -> Feature161Model * Feature161Effect list
    val updateFeature161: msg: Feature161Msg -> model: Feature161Model -> Feature161Model * Feature161Effect list

    val artifactPath: directory: string -> name: string -> string
    val feature148ArtifactPath: directory: string -> name: string -> string
    val feature149ArtifactPath: directory: string -> name: string -> string
    val feature152ArtifactPath: directory: string -> name: string -> string
    val feature153ArtifactPath: directory: string -> name: string -> string
    val feature154ArtifactPath: directory: string -> name: string -> string
    val feature155ArtifactPath: directory: string -> name: string -> string
    val feature156ArtifactPath: directory: string -> name: string -> string
    val feature157ArtifactPath: directory: string -> name: string -> string
    val feature158ArtifactPath: directory: string -> name: string -> string
    val feature159ArtifactPath: directory: string -> name: string -> string
    val feature160ArtifactPath: directory: string -> name: string -> string
    val feature161ArtifactPath: directory: string -> name: string -> string
    val feature156ScenarioFileName: scenarioId: string -> string
    val feature156VerdictToken: verdict: Feature156ScenarioVerdict -> string
    val feature156DistributionRow: distribution: Feature156PathDistribution option -> string
    val feature156OverallVerdict: reports: Feature156ScenarioReport list -> Feature156ScenarioVerdict
    val feature157StatusToken: status: Feature157DamageStatus -> string
    val feature157ScenarioFileName: scenarioId: string -> string
    val feature157OverallStatus: summary: Feature157DamageSummary -> Feature157DamageStatus
    val feature158StatusToken: status: Feature158ReadinessStatus -> string
    val feature158ScenarioFileName: scenarioId: string -> string
    val feature158OverallStatus: summary: Feature158TimingSummary -> Feature158ReadinessStatus
    val feature158DistributionRow: distribution: Feature158PathDistribution option -> string
    val feature159StatusToken: status: Feature159ReadinessStatus -> string
    val feature159ScenarioFileName: scenarioId: string -> string
    val feature159OverallStatus: summary: Feature159Summary -> Feature159ReadinessStatus
    val feature160StatusToken: status: Feature160ReadinessStatus -> string
    val feature160ScenarioFileName: scenarioId: string -> string
    val feature160IterationFileName: iterationId: string -> string
    val feature160FocusedThroughputStatus: summary: Feature160ThroughputSummary -> Feature160ReadinessStatus
    val feature160OverallStatus: summary: Feature160ThroughputSummary -> Feature160ReadinessStatus
    val feature160FullValidationStatus: record: Feature160FullValidationRecord option -> string
    val feature161StatusToken: status: Feature161ReadinessStatus -> string
    val feature161HostFactsFileName: entryId: string -> string
    val feature161LedgerEntryFileName: entryId: string -> string
    val feature161LaneIdFromFacts: facts: Feature161HostFacts -> string
    val feature161ValidateHostFacts: facts: Feature161HostFacts -> Perf.ExclusionReason option
    val feature161LedgerEntryAccepted: entry: Feature161LedgerEntry -> bool
    val feature161ScopeFromEntries: entries: Feature161LedgerEntry list -> Feature161ClaimScope
    val feature161OverallStatus: summary: Feature161Summary -> Feature161ReadinessStatus
    val renderPresentProof: proof: PresentProof -> string
    val renderValidationSummary: model: ReadinessModel -> string
    val renderCompatibilityLedger: model: ReadinessModel -> string
    val renderFeature148LiveProof: proof: PresentProof -> string
    val renderFeature148ParityReport: unit -> string
    val renderFeature148ReuseReport: unit -> string
    val renderFeature148SnapshotReport: unit -> string
    val renderFeature148TimingReport: tier: string -> string
    val renderFeature148ValidationSummary: model: ReadinessModel -> string
    val renderFeature148CompatibilityLedger: model: ReadinessModel -> string
    val renderFeature149LiveProof: proof: PresentProof -> string
    val renderFeature149ParityReport: unit -> string
    val renderFeature149ReuseReport: unit -> string
    val renderFeature149SnapshotReport: unit -> string
    val renderFeature149TimingReport: tier: string -> string
    val renderFeature149ValidationSummary: model: ReadinessModel -> string
    val renderFeature149CompatibilityLedger: model: ReadinessModel -> string
    val renderFeature152LiveProof: proof: PresentProof -> string
    val renderFeature152ParityReport: unit -> string
    val renderFeature152TimingReport: tier: string -> string
    val renderFeature152ValidationSummary: model: ReadinessModel -> string
    val renderFeature152CompatibilityLedger: model: ReadinessModel -> string
    val renderFeature153LiveProof: proof: PresentProof -> string
    val renderFeature153ProofSet: model: ReadinessModel -> string
    val renderFeature153ValidationSummary: model: ReadinessModel -> string
    val renderFeature153CompatibilityLedger: model: ReadinessModel -> string
    val renderFeature153PackageValidation: unit -> string
    val renderFeature153RegressionValidation: unit -> string
    val renderFeature154LiveProof: proof: PresentProof -> string
    val renderFeature154ProofSet: model: ReadinessModel -> string
    val renderFeature154ParityReport: unit -> string
    val renderFeature154TimingReport: tier: string -> scenarioCount: int -> repetitions: int -> string
    val renderFeature154ValidationSummary: model: ReadinessModel -> string
    val renderFeature154CompatibilityLedger: model: ReadinessModel -> string
    val renderFeature154PackageValidation: unit -> string
    val renderFeature154RegressionValidation: unit -> string
    val renderFeature155LiveProof: proof: PresentProof -> string
    val renderFeature155ProofSet: model: ReadinessModel -> string
    val renderFeature155ParityReport: unit -> string
    val renderFeature155TimingReport: tier: string -> scenarioCount: int -> repetitions: int -> string
    val renderFeature155ValidationSummary: model: ReadinessModel -> string
    val renderFeature155CompatibilityLedger: model: ReadinessModel -> string
    val renderFeature155PackageValidation: unit -> string
    val renderFeature155RegressionValidation: unit -> string
    /// Feature 181: catalog-collapsed package/regression validation renderers for features 156-161
    /// (byte-identical to the per-feature bodies they replace). `featureNum` selects the per-feature
    /// section headers and bullets.
    val renderPackageValidation: featureNum: int -> validationLines: string list -> string
    val renderRegressionValidation: featureNum: int -> validationLines: string list -> string
    val renderFeature156ScenarioReport: report: Feature156ScenarioReport -> string
    val renderFeature156TimingSummary: summary: Feature156TimingSummary -> string
    val renderFeature156CompatibilityLedger: unit -> string
    val renderFeature156ValidationSummary: summary: Feature156TimingSummary -> string
    val renderFeature156UnsupportedHostReport: reason: string -> string
    val renderFeature157AttemptReport: attempt: Feature157DamageAttempt -> string
    val renderFeature157FallbackReport: fallback: Feature157Fallback -> string
    val renderFeature157ParityReport: attempt: Feature157DamageAttempt -> string
    val renderFeature157DamageSummary: summary: Feature157DamageSummary -> string
    val renderFeature157DamageSummaryJson: summary: Feature157DamageSummary -> string
    val renderFeature157CompatibilityLedger: unit -> string
    val renderFeature157ValidationSummary: summary: Feature157DamageSummary -> string
    val renderFeature157UnsupportedHostReport: reason: string -> string
    val renderFeature158ScenarioReport: report: Feature158ScenarioReport -> string
    val renderFeature158ExcludedSamplesReport: reason: Perf.ExclusionReason -> samples: Feature158TimingSample list -> string
    val renderFeature158ProofProbeReport: evidence: Feature158ProofProbeEvidence list -> string
    val renderFeature158TimingSummary: summary: Feature158TimingSummary -> string
    val renderFeature158TimingSummaryJson: summary: Feature158TimingSummary -> string
    val renderFeature158CompatibilityLedger: unit -> string
    val renderFeature158ValidationSummary: summary: Feature158TimingSummary -> string
    val renderFeature158UnsupportedHostReport: reason: string -> string
    val renderFeature159AttemptReport: attempt: Feature159Attempt -> string
    val renderFeature159PromotionSummary: summary: Feature159Summary -> string
    val renderFeature159CounterReport: summary: Feature159Summary -> string
    val renderFeature159CompatibilityLedger: unit -> string
    val renderFeature159ValidationSummary: summary: Feature159Summary -> string
    val renderFeature159UnsupportedHostReport: reason: string -> string
    val renderFeature160IterationReport: iteration: Feature160Iteration -> string
    val renderFeature160ExcludedEvidenceReport: reason: Perf.ExclusionReason -> iterations: Feature160Iteration list -> string
    val renderFeature160ThroughputSummary: summary: Feature160ThroughputSummary -> string
    val renderFeature160ThroughputSummaryJson: summary: Feature160ThroughputSummary -> string
    val renderFeature160CompatibilityLedger: unit -> string
    val renderFeature160FullValidationRecord: record: Feature160FullValidationRecord option -> string
    val renderFeature160ValidationSummary: summary: Feature160ThroughputSummary -> string
    val renderFeature160UnsupportedHostReport: reason: string -> string
    val renderFeature161HostFacts: facts: Feature161HostFacts -> string
    val renderFeature161LedgerEntry: entry: Feature161LedgerEntry -> string
    val renderFeature161ExcludedEvidenceReport: reason: Perf.ExclusionReason -> entries: Feature161LedgerEntry list -> string
    val renderFeature161LaneLedgerSummary: summary: Feature161Summary -> string
    val renderFeature161LaneLedgerSummaryJson: summary: Feature161Summary -> string
    val renderFeature161CompatibilityLedger: unit -> string
    val renderFeature161FullValidationRecord: status: string -> string
    val renderFeature161ValidationSummary: summary: Feature161Summary -> string
    val renderFeature161UnsupportedHostReport: reason: string -> string
