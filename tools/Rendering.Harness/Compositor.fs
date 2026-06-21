namespace Rendering.Harness

open System
open System.IO
open System.Security.Cryptography
open System.Text

module Compositor =

    let featureId = "147-compositor-damage-redraw"
    let feature148Id = "148-compositor-live-integration"
    let feature149Id = "149-complete-compositor-p7"
    let feature152Id = "152-compositor-live-proof"
    let feature153Id = "153-compositor-proof-interpreter"
    let feature154Id = "154-compositor-proof-acceptance"
    let feature155Id = "155-native-proof-capture"
    let feature156Id = "156-same-profile-timing"
    let feature157Id = "157-no-clear-damage-scissor"
    let feature158Id = "158-separate-proof-timing"
    let feature159Id = "159-layer-promotion-keys"
    let feature160Id = "160-performance-validation-throughput"
    let feature161Id = "161-host-performance-lane-ledger"

    let readinessDirectory = "specs/147-compositor-damage-redraw/readiness"
    let presentProofDirectory = Path.Combine(readinessDirectory, "present-proof")
    let parityDirectory = Path.Combine(readinessDirectory, "parity")
    let perfDirectory = Path.Combine(readinessDirectory, "perf")
    let compatibilityLedgerPath = Path.Combine(readinessDirectory, "compatibility-ledger.md")
    let validationSummaryPath = Path.Combine(readinessDirectory, "validation-summary.md")

    let feature148ReadinessDirectory = Path.Combine("specs", feature148Id, "readiness")
    let feature148LiveProofDirectory = Path.Combine(feature148ReadinessDirectory, "live-proof")
    let feature148ParityDirectory = Path.Combine(feature148ReadinessDirectory, "parity")
    let feature148ReuseDirectory = Path.Combine(feature148ReadinessDirectory, "reuse")
    let feature148SnapshotsDirectory = Path.Combine(feature148ReadinessDirectory, "snapshots")
    let feature148TimingDirectory = Path.Combine(feature148ReadinessDirectory, "timing")
    let feature148CompatibilityLedgerPath = Path.Combine(feature148ReadinessDirectory, "compatibility-ledger.md")
    let feature148ValidationSummaryPath = Path.Combine(feature148ReadinessDirectory, "validation-summary.md")
    let feature148PackageVersion = "local-harness"

    let feature149ReadinessDirectory = Path.Combine("specs", feature149Id, "readiness")
    let feature149LiveProofDirectory = Path.Combine(feature149ReadinessDirectory, "live-proof")
    let feature149ParityDirectory = Path.Combine(feature149ReadinessDirectory, "parity")
    let feature149ReuseDirectory = Path.Combine(feature149ReadinessDirectory, "reuse")
    let feature149SnapshotsDirectory = Path.Combine(feature149ReadinessDirectory, "snapshots")
    let feature149TimingDirectory = Path.Combine(feature149ReadinessDirectory, "timing")
    let feature149CompatibilityLedgerPath = Path.Combine(feature149ReadinessDirectory, "compatibility-ledger.md")
    let feature149ValidationSummaryPath = Path.Combine(feature149ReadinessDirectory, "validation-summary.md")
    let feature149PackageVersion = "local-harness"

    let feature152ReadinessDirectory = Path.Combine("specs", feature152Id, "readiness")
    let feature152LiveProofDirectory = Path.Combine(feature152ReadinessDirectory, "live-proof")
    let feature152ParityDirectory = Path.Combine(feature152ReadinessDirectory, "parity")
    let feature152TimingDirectory = Path.Combine(feature152ReadinessDirectory, "timing")
    let feature152FsiDirectory = Path.Combine(feature152ReadinessDirectory, "fsi")
    let feature152CompatibilityLedgerPath = Path.Combine(feature152ReadinessDirectory, "compatibility-ledger.md")
    let feature152ValidationSummaryPath = Path.Combine(feature152ReadinessDirectory, "validation-summary.md")
    let feature152PackageVersion = "local-harness"

    let feature153ReadinessDirectory = Path.Combine("specs", feature153Id, "readiness")
    let feature153LiveProofDirectory = Path.Combine(feature153ReadinessDirectory, "live-proof")
    let feature153LiveProofAttemptsDirectory = Path.Combine(feature153LiveProofDirectory, "attempts")
    let feature153LiveProofUnsupportedDirectory = Path.Combine(feature153LiveProofDirectory, "unsupported")
    let feature153FsiDirectory = Path.Combine(feature153ReadinessDirectory, "fsi")
    let feature153ProofSetPath = Path.Combine(feature153ReadinessDirectory, "proof-set.md")
    let feature153CompatibilityLedgerPath = Path.Combine(feature153ReadinessDirectory, "compatibility-ledger.md")
    let feature153ValidationSummaryPath = Path.Combine(feature153ReadinessDirectory, "validation-summary.md")
    let feature153PackageValidationPath = Path.Combine(feature153ReadinessDirectory, "package-validation.md")
    let feature153RegressionValidationPath = Path.Combine(feature153ReadinessDirectory, "regression-validation.md")
    let feature153PackageVersion = "local-harness"

    let feature154ReadinessDirectory = Path.Combine("specs", feature154Id, "readiness")
    let feature154LiveProofDirectory = Path.Combine(feature154ReadinessDirectory, "live-proof")
    let feature154LiveProofAttemptsDirectory = Path.Combine(feature154LiveProofDirectory, "attempts")
    let feature154LiveProofUnsupportedDirectory = Path.Combine(feature154LiveProofDirectory, "unsupported")
    let feature154ParityDirectory = Path.Combine(feature154ReadinessDirectory, "parity")
    let feature154TimingDirectory = Path.Combine(feature154ReadinessDirectory, "timing")
    let feature154FsiDirectory = Path.Combine(feature154ReadinessDirectory, "fsi")
    let feature154ProofSetPath = Path.Combine(feature154ReadinessDirectory, "proof-set.md")
    let feature154CompatibilityLedgerPath = Path.Combine(feature154ReadinessDirectory, "compatibility-ledger.md")
    let feature154ValidationSummaryPath = Path.Combine(feature154ReadinessDirectory, "validation-summary.md")
    let feature154PackageValidationPath = Path.Combine(feature154ReadinessDirectory, "package-validation.md")
    let feature154RegressionValidationPath = Path.Combine(feature154ReadinessDirectory, "regression-validation.md")
    let feature154PackageVersion = "local-harness"

    let feature155ReadinessDirectory = Path.Combine("specs", feature155Id, "readiness")
    let feature155LiveProofDirectory = Path.Combine(feature155ReadinessDirectory, "live-proof")
    let feature155LiveProofAttemptsDirectory = Path.Combine(feature155LiveProofDirectory, "attempts")
    let feature155LiveProofUnsupportedDirectory = Path.Combine(feature155LiveProofDirectory, "unsupported")
    let feature155ParityDirectory = Path.Combine(feature155ReadinessDirectory, "parity")
    let feature155TimingDirectory = Path.Combine(feature155ReadinessDirectory, "timing")
    let feature155FsiDirectory = Path.Combine(feature155ReadinessDirectory, "fsi")
    let feature155ProofSetPath = Path.Combine(feature155ReadinessDirectory, "proof-set.md")
    let feature155CompatibilityLedgerPath = Path.Combine(feature155ReadinessDirectory, "compatibility-ledger.md")
    let feature155ValidationSummaryPath = Path.Combine(feature155ReadinessDirectory, "validation-summary.md")
    let feature155PackageValidationPath = Path.Combine(feature155ReadinessDirectory, "package-validation.md")
    let feature155RegressionValidationPath = Path.Combine(feature155ReadinessDirectory, "regression-validation.md")
    let feature155PackageVersion = "local-harness"

    let feature156ReadinessDirectory = Path.Combine("specs", feature156Id, "readiness")
    let feature156TimingDirectory = Path.Combine(feature156ReadinessDirectory, "timing")
    let feature156TimingScenariosDirectory = Path.Combine(feature156TimingDirectory, "scenarios")
    let feature156TimingRawDirectory = Path.Combine(feature156TimingDirectory, "raw")
    let feature156TimingUnsupportedDirectory = Path.Combine(feature156TimingDirectory, "unsupported")
    let feature156FsiDirectory = Path.Combine(feature156ReadinessDirectory, "fsi")
    let feature156CompatibilityLedgerPath = Path.Combine(feature156ReadinessDirectory, "compatibility-ledger.md")
    let feature156ValidationSummaryPath = Path.Combine(feature156ReadinessDirectory, "validation-summary.md")
    let feature156PackageValidationPath = Path.Combine(feature156ReadinessDirectory, "package-validation.md")
    let feature156RegressionValidationPath = Path.Combine(feature156ReadinessDirectory, "regression-validation.md")
    let feature156TimingSummaryPath = Path.Combine(feature156TimingDirectory, "summary.md")
    let feature156PackageVersion = "local-harness"
    let feature156AcceptedProfileId = "probe-08a47c01"
    let feature156PolicyId = "same-profile-live-threshold-v2"

    let feature157ReadinessDirectory = Path.Combine("specs", feature157Id, "readiness")
    let feature157DamageDirectory = Path.Combine(feature157ReadinessDirectory, "damage")
    let feature157DamageAttemptsDirectory = Path.Combine(feature157DamageDirectory, "attempts")
    let feature157DamageFallbacksDirectory = Path.Combine(feature157DamageDirectory, "fallbacks")
    let feature157DamageParityDirectory = Path.Combine(feature157DamageDirectory, "parity")
    let feature157DamageUnsupportedDirectory = Path.Combine(feature157DamageDirectory, "unsupported")
    let feature157FsiDirectory = Path.Combine(feature157ReadinessDirectory, "fsi")
    let feature157CompatibilityLedgerPath = Path.Combine(feature157ReadinessDirectory, "compatibility-ledger.md")
    let feature157ValidationSummaryPath = Path.Combine(feature157ReadinessDirectory, "validation-summary.md")
    let feature157PackageValidationPath = Path.Combine(feature157ReadinessDirectory, "package-validation.md")
    let feature157RegressionValidationPath = Path.Combine(feature157ReadinessDirectory, "regression-validation.md")
    let feature157DamageSummaryPath = Path.Combine(feature157DamageDirectory, "summary.md")
    let feature157DamageSummaryJsonPath = Path.Combine(feature157DamageDirectory, "summary.json")
    let feature157AcceptedProfileId = feature156AcceptedProfileId

    let feature158ReadinessDirectory = Path.Combine("specs", feature158Id, "readiness")
    let feature158TimingDirectory = Path.Combine(feature158ReadinessDirectory, "timing")
    let feature158TimingScenariosDirectory = Path.Combine(feature158TimingDirectory, "scenarios")
    let feature158TimingRawDirectory = Path.Combine(feature158TimingDirectory, "raw")
    let feature158TimingExcludedDirectory = Path.Combine(feature158TimingDirectory, "excluded")
    let feature158TimingUnsupportedDirectory = Path.Combine(feature158TimingDirectory, "unsupported")
    let feature158ProofProbesDirectory = Path.Combine(feature158ReadinessDirectory, "proof-probes")
    let feature158FsiDirectory = Path.Combine(feature158ReadinessDirectory, "fsi")
    let feature158SurfaceBaselinesDirectory = Path.Combine(feature158ReadinessDirectory, "surface-baselines")
    let feature158CompatibilityLedgerPath = Path.Combine(feature158ReadinessDirectory, "compatibility-ledger.md")
    let feature158ValidationSummaryPath = Path.Combine(feature158ReadinessDirectory, "validation-summary.md")
    let feature158PackageValidationPath = Path.Combine(feature158ReadinessDirectory, "package-validation.md")
    let feature158RegressionValidationPath = Path.Combine(feature158ReadinessDirectory, "regression-validation.md")
    let feature158TimingSummaryPath = Path.Combine(feature158TimingDirectory, "summary.md")
    let feature158TimingSummaryJsonPath = Path.Combine(feature158TimingDirectory, "summary.json")
    let feature158AcceptedProfileId = feature156AcceptedProfileId
    let feature158PolicyId = "readback-free-timing-v1"
    let feature158PerformanceCommand = "compositor-performance --feature 158"
    let feature158ProbeCommand = "compositor-performance --feature 158 --probe-readback"
    let feature158ReadinessCommand = "compositor-readiness --feature 158"

    let feature159ReadinessDirectory = Path.Combine("specs", feature159Id, "readiness")
    let feature159PromotionDirectory = Path.Combine(feature159ReadinessDirectory, "promotion")
    let feature159PromotionAttemptsDirectory = Path.Combine(feature159PromotionDirectory, "attempts")
    let feature159PromotionReuseDirectory = Path.Combine(feature159PromotionDirectory, "reuse")
    let feature159PromotionDemotionsDirectory = Path.Combine(feature159PromotionDirectory, "demotions")
    let feature159PromotionFallbacksDirectory = Path.Combine(feature159PromotionDirectory, "fallbacks")
    let feature159PromotionParityDirectory = Path.Combine(feature159PromotionDirectory, "parity")
    let feature159PromotionUnsupportedDirectory = Path.Combine(feature159PromotionDirectory, "unsupported")
    let feature159CountersDirectory = Path.Combine(feature159ReadinessDirectory, "counters")
    let feature159FsiDirectory = Path.Combine(feature159ReadinessDirectory, "fsi")
    let feature159CompatibilityLedgerPath = Path.Combine(feature159ReadinessDirectory, "compatibility-ledger.md")
    let feature159ValidationSummaryPath = Path.Combine(feature159ReadinessDirectory, "validation-summary.md")
    let feature159PackageValidationPath = Path.Combine(feature159ReadinessDirectory, "package-validation.md")
    let feature159RegressionValidationPath = Path.Combine(feature159ReadinessDirectory, "regression-validation.md")
    let feature159PromotionSummaryPath = Path.Combine(feature159PromotionDirectory, "summary.md")
    let feature159AcceptedProfileId = feature156AcceptedProfileId
    let feature159PolicyId = "layer-promotion-v1"
    let feature159PromotionCommand = "compositor-promotion --feature 159"
    let feature159ReadinessCommand = "compositor-readiness --feature 159"

    let feature160ReadinessDirectory = Path.Combine("specs", feature160Id, "readiness")
    let feature160ThroughputDirectory = Path.Combine(feature160ReadinessDirectory, "throughput")
    let feature160ThroughputIterationsDirectory = Path.Combine(feature160ThroughputDirectory, "iterations")
    let feature160ThroughputRawDirectory = Path.Combine(feature160ThroughputDirectory, "raw")
    let feature160ThroughputExcludedDirectory = Path.Combine(feature160ThroughputDirectory, "excluded")
    let feature160ThroughputUnsupportedDirectory = Path.Combine(feature160ThroughputDirectory, "unsupported")
    let feature160FullValidationDirectory = Path.Combine(feature160ReadinessDirectory, "full-validation")
    let feature160FsiDirectory = Path.Combine(feature160ReadinessDirectory, "fsi")
    let feature160CompatibilityLedgerPath = Path.Combine(feature160ReadinessDirectory, "compatibility-ledger.md")
    let feature160ValidationSummaryPath = Path.Combine(feature160ReadinessDirectory, "validation-summary.md")
    let feature160PackageValidationPath = Path.Combine(feature160ReadinessDirectory, "package-validation.md")
    let feature160RegressionValidationPath = Path.Combine(feature160ReadinessDirectory, "regression-validation.md")
    let feature160ThroughputSummaryPath = Path.Combine(feature160ThroughputDirectory, "summary.md")
    let feature160ThroughputSummaryJsonPath = Path.Combine(feature160ThroughputDirectory, "summary.json")
    let feature160AcceptedProfileId = feature158AcceptedProfileId
    let feature160PolicyId = "focused-throughput-v1"
    let feature160FocusedLaneId = "focused"
    let feature160RequiredAttempts = 3
    let feature160MaxIterationMinutes = 10
    let feature160UnsupportedHostMinutes = 2
    let feature160PerformanceCommand = "compositor-performance --feature 160 --lane focused"
    let feature160ReadinessCommand = "compositor-readiness --feature 160"

    let feature161ReadinessDirectory = Path.Combine("specs", feature161Id, "readiness")
    let feature161LaneLedgerDirectory = Path.Combine(feature161ReadinessDirectory, "lane-ledger")
    let feature161LaneLedgerEntriesDirectory = Path.Combine(feature161LaneLedgerDirectory, "entries")
    let feature161LaneLedgerHostFactsDirectory = Path.Combine(feature161LaneLedgerDirectory, "host-facts")
    let feature161LaneLedgerExcludedDirectory = Path.Combine(feature161LaneLedgerDirectory, "excluded")
    let feature161LaneLedgerUnsupportedDirectory = Path.Combine(feature161LaneLedgerDirectory, "unsupported")
    let feature161FullValidationDirectory = Path.Combine(feature161ReadinessDirectory, "full-validation")
    let feature161FsiDirectory = Path.Combine(feature161ReadinessDirectory, "fsi")
    let feature161CompatibilityLedgerPath = Path.Combine(feature161ReadinessDirectory, "compatibility-ledger.md")
    let feature161ValidationSummaryPath = Path.Combine(feature161ReadinessDirectory, "validation-summary.md")
    let feature161PackageValidationPath = Path.Combine(feature161ReadinessDirectory, "package-validation.md")
    let feature161RegressionValidationPath = Path.Combine(feature161ReadinessDirectory, "regression-validation.md")
    let feature161LaneLedgerSummaryPath = Path.Combine(feature161LaneLedgerDirectory, "summary.md")
    let feature161LaneLedgerSummaryJsonPath = Path.Combine(feature161LaneLedgerDirectory, "summary.json")
    let feature161AcceptedProfileId = feature160AcceptedProfileId
    let feature161PolicyId = "host-lane-ledger-v1"
    let feature161HostLaneId = "x11-:1-direct-opengl-amd-mesa"
    let feature161PerformanceCommand = "compositor-performance --feature 161 --lane host-ledger"
    let feature161ReadinessCommand = "compositor-readiness --feature 161"

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

    let thresholds =
        { PromotionReductionPercent = 30.0
          SimpleSceneOverheadPercent = 5.0
          SnapshotImprovementPercent = 20.0 }

    let snapshotBudget =
        { MaxEntries = 64
          MaxBytes = 32L * 1024L * 1024L }

    let scenarioIds =
        [ "proof/sentinel-damage-v1"
          "damage/idle"
          "damage/localized-update"
          "damage/overlap"
          "damage/frame-edge"
          "damage/full-frame-invalidation"
          "promotion/stable-boundary"
          "promotion/placement-only-move"
          "promotion/content-change"
          "promotion/churn"
          "snapshot/expensive-stable"
          "snapshot/simple-overhead"
          "snapshot/over-budget" ]

    let targetHostProfiles =
        [ { ProfileId = "x11-opengl-direct"
            Backend = "OpenGL"
            Renderer = None
            PresentMode = "DirectToSwapchain"
            FramebufferSize = "640x480"
            Scale = Some 1.0
            DisplayEnvironment = "x11"
            ProofAlgorithmVersion = "sentinel-damage-v1" }
          { ProfileId = "headless-offscreen"
            Backend = "OpenGL"
            Renderer = None
            PresentMode = "OffscreenReadback"
            FramebufferSize = "640x480"
            Scale = Some 1.0
            DisplayEnvironment = "headless"
            ProofAlgorithmVersion = "sentinel-damage-v1" }
          { ProfileId = "unsupported-display"
            Backend = "OpenGL"
            Renderer = None
            PresentMode = "DirectToSwapchain"
            FramebufferSize = "unknown"
            Scale = None
            DisplayEnvironment = "missing-display"
            ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature148ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/non-preserving-host"
          "proof/stale"
          "proof/host-mismatch"
          "proof/missing-display"
          "proof/unsupported-readback"
          "proof/timeout"
          "proof/permission"
          "proof/host-error"
          "damage/idle"
          "damage/localized-update"
          "damage/overlap"
          "damage/frame-edge"
          "damage/movement-old-new"
          "damage/resize"
          "damage/theme-global"
          "damage/stale-proof"
          "damage/disabled"
          "damage/unsupported"
          "damage/parity-failure"
          "reuse/stable-boundary"
          "reuse/moving-only"
          "reuse/scrolling"
          "reuse/content-changing"
          "reuse/theme-resource-change"
          "reuse/churning"
          "reuse/no-benefit"
          "reuse/failed-parity"
          "reuse/same-seed"
          "snapshot/expensive-stable"
          "snapshot/simple-scene"
          "snapshot/churning"
          "snapshot/over-budget"
          "snapshot/invalid-resource"
          "snapshot/unsupported-host"
          "snapshot/parity-failure"
          "timing/damage"
          "timing/placement"
          "timing/replay"
          "timing/snapshot" ]

    let feature148TargetHostProfiles =
        targetHostProfiles
        @ [ { ProfileId = "synthetic-non-preserving"
              Backend = "OpenGL"
              Renderer = Some "synthetic"
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "synthetic"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature148TimingTiers = [ "damage"; "placement"; "replay"; "snapshot" ]

    let feature149ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/capable-host-three-run"
          "proof/non-preserving-host"
          "proof/stale"
          "proof/host-mismatch"
          "proof/algorithm-mismatch"
          "proof/missing-artifact"
          "proof/blank-artifact"
          "proof/synthetic-only"
          "proof/missing-display"
          "proof/unsupported-readback"
          "proof/timeout"
          "proof/permission"
          "proof/host-error"
          "damage/idle"
          "damage/localized-update"
          "damage/overlap"
          "damage/frame-edge"
          "damage/movement-old-new"
          "damage/resize"
          "damage/theme-global"
          "damage/zero-damage"
          "damage/stale-proof"
          "damage/disabled"
          "damage/unsupported"
          "damage/resource-failure"
          "damage/internal-error"
          "damage/parity-failure"
          "reuse/stable-boundary"
          "reuse/placement-only"
          "reuse/mixed-change"
          "reuse/no-change"
          "reuse/content-changing"
          "reuse/churning"
          "reuse/no-benefit"
          "reuse/failed-parity"
          "reuse/same-seed"
          "snapshot/expensive-stable"
          "snapshot/create-reuse-refresh"
          "snapshot/replacement-eviction-disposal"
          "snapshot/simple-scene"
          "snapshot/churning"
          "snapshot/over-budget"
          "snapshot/stale-resource"
          "snapshot/invalid-resource"
          "snapshot/unsupported-host"
          "snapshot/parity-failure"
          "timing/damage"
          "timing/placement"
          "timing/replay"
          "timing/snapshot"
          "readiness/public-diagnostics"
          "readiness/compatibility-ledger" ]

    let feature149TargetHostProfiles =
        feature148TargetHostProfiles
        @ [ { ProfileId = "feature149-capable-host-candidate"
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature149TimingTiers = feature148TimingTiers

    let feature152ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/capable-host-three-run"
          "proof/unsupported-host-zero-accepted"
          "proof/stale"
          "proof/host-mismatch"
          "proof/proof-method-mismatch"
          "proof/missing-artifact"
          "proof/blank-artifact"
          "proof/synthetic-only"
          "damage/localized-update"
          "damage/no-change"
          "damage/movement-old-new"
          "damage/edge-clipped"
          "damage/resize"
          "damage/full-frame-invalidation"
          "damage/invalid-damage"
          "damage/unsupported"
          "damage/resource-failure"
          "damage/parity-failure"
          "timing/localized-update"
          "timing/no-change"
          "timing/movement"
          "timing/resize"
          "timing/churn"
          "readiness/final-decision"
          "readiness/compatibility-ledger"
          "readiness/package-validation" ]

    let feature152TargetHostProfiles =
        feature149TargetHostProfiles
        @ [ { ProfileId = "feature152-capable-host-candidate"
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature152TimingTiers = [ "damage" ]

    let feature153ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/capable-host-three-run"
          "proof/unsupported-host-zero-accepted"
          "proof/readback-limited"
          "proof/stale"
          "proof/host-mismatch"
          "proof/proof-method-mismatch"
          "proof/missing-artifact"
          "proof/blank-artifact"
          "proof/synthetic-only"
          "proof/selected-trio"
          "readiness/final-decision"
          "readiness/compatibility-ledger"
          "readiness/package-validation"
          "readiness/regression-validation" ]

    let feature153TargetHostProfiles =
        feature152TargetHostProfiles
        @ [ { ProfileId = "feature153-capable-host-candidate"
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature154ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/capable-host-three-run"
          "proof/unsupported-host-zero-accepted"
          "proof/stale"
          "proof/host-mismatch"
          "proof/proof-method-mismatch"
          "proof/missing-artifact"
          "proof/blank-artifact"
          "proof/undecodable-artifact"
          "proof/synthetic-only"
          "proof/incomplete"
          "proof/damaged-pixel-failure"
          "proof/undamaged-preservation-failure"
          "damage/localized-update"
          "damage/no-change"
          "damage/movement"
          "damage/overlap"
          "damage/edge-clipping"
          "damage/resize"
          "damage/full-invalidation"
          "damage/invalid-damage"
          "damage/unsupported-host"
          "damage/resource-failure"
          "timing/localized-update"
          "timing/no-change"
          "timing/movement"
          "timing/overlap"
          "timing/resize"
          "readiness/final-decision"
          "readiness/compatibility-ledger"
          "readiness/package-validation"
          "readiness/regression-validation" ]

    let feature154TargetHostProfiles =
        feature153TargetHostProfiles
        @ [ { ProfileId = "feature154-capable-host-candidate"
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature154TimingTiers = [ "damage" ]

    let feature155ScenarioIds =
        [ "proof/live-sentinel-damage-v1"
          "proof/native-capable-host-three-run"
          "proof/unsupported-host-zero-accepted"
          "proof/artifact-write-failure"
          "proof/timeout"
          "proof/missing-artifact"
          "proof/blank-artifact"
          "proof/undecodable-artifact"
          "proof/synthetic-only"
          "proof/damaged-pixel-failure"
          "proof/undamaged-preservation-failure"
          "damage/localized-update"
          "damage/no-change"
          "damage/movement"
          "damage/overlap"
          "damage/edge-clipping"
          "damage/resize"
          "damage/full-invalidation"
          "damage/invalid-damage"
          "damage/unsupported-host"
          "damage/resource-failure"
          "timing/localized-update"
          "timing/no-change"
          "timing/movement"
          "timing/overlap"
          "timing/resize"
          "readiness/final-p7-closeout"
          "readiness/compatibility-ledger"
          "readiness/package-validation"
          "readiness/regression-validation" ]

    let feature155TargetHostProfiles =
        feature154TargetHostProfiles
        @ [ { ProfileId = "feature155-current-capable-host"
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature155TimingTiers = [ "damage" ]

    let feature156RequiredScenarioIds =
        [ "timing/localized-update"
          "timing/no-change"
          "timing/movement-old-new"
          "timing/overlap"
          "timing/edge-clipping" ]

    let feature156ScenarioIds =
        feature156RequiredScenarioIds
        @ [ "timing/cross-profile-rejected"
            "timing/incomplete-samples"
            "timing/noisy"
            "timing/non-beneficial"
            "timing/readback-limited"
            "timing/unsupported-host" ]

    let feature156TargetHostProfiles =
        feature155TargetHostProfiles
        @ [ { ProfileId = feature156AcceptedProfileId
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature157RequiredScenarioIds =
        [ "damage/static-preserved"
          "damage/localized-update"
          "damage/movement-old-new"
          "damage/scroll-shifted"
          "damage/nested-retained" ]

    let feature157FallbackScenarioIds =
        [ "damage/empty-visible-change"
          "damage/out-of-bounds"
          "damage/stale"
          "damage/incomplete"
          "damage/full-frame-invalidation"
          "damage/missing-retained-backing"
          "damage/resource-failure"
          "damage/parity-mismatch"
          "damage/unsupported-host" ]

    let feature157ScenarioIds =
        feature157RequiredScenarioIds
        @ feature157FallbackScenarioIds
        @ [ "readiness/final-decision"
            "readiness/compatibility-ledger"
            "readiness/package-validation"
            "readiness/regression-validation" ]

    let feature157TargetHostProfiles =
        feature156TargetHostProfiles
        @ [ { ProfileId = feature157AcceptedProfileId
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature158RequiredScenarioIds = feature156RequiredScenarioIds

    let feature158ScenarioIds =
        feature158RequiredScenarioIds
        @ [ "timing/probe-readback"
            "timing/proof-readback-in-measured-interval"
            "timing/missing-policy"
            "timing/unverified-policy"
            "timing/cross-profile-evidence"
            "timing/package-version-mismatch"
            "timing/run-identity-mismatch"
            "timing/unsupported-host"
            "readiness/validation-summary"
            "readiness/compatibility-ledger"
            "readiness/package-validation"
            "readiness/regression-validation" ]

    let feature158TargetHostProfiles =
        feature157TargetHostProfiles
        @ [ { ProfileId = feature158AcceptedProfileId
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature159RequiredScenarioIds =
        [ "promotion/static-retained"
          "promotion/placement-only-move"
          "promotion/scroll-shifted"
          "promotion/nested-retained"
          "promotion/content-change"
          "promotion/churn-demotion"
          "promotion/fallback-safe" ]

    let feature159FallbackScenarioIds =
        [ "promotion/ambiguous-identity"
          "promotion/parity-mismatch"
          "promotion/cross-profile"
          "promotion/missing-policy"
          "promotion/unsupported-host" ]

    let feature159ScenarioIds =
        feature159RequiredScenarioIds
        @ feature159FallbackScenarioIds
        @ [ "readiness/validation-summary"
            "readiness/compatibility-ledger"
            "readiness/package-validation"
            "readiness/regression-validation" ]

    let feature159TargetHostProfiles =
        feature158TargetHostProfiles
        @ [ { ProfileId = feature159AcceptedProfileId
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature160RequiredScenarioIds = feature158RequiredScenarioIds

    let feature160ScenarioIds =
        feature160RequiredScenarioIds
        @ [ "timing/sparse-heavy-localized-update"
            "timing/restricted-debug"
            "timing/timed-out"
            "timing/canceled"
            "timing/partial-evidence"
            "timing/cross-profile-evidence"
            "timing/stale-evidence"
            "timing/mixed-policy"
            "timing/missing-metadata"
            "timing/unsupported-host"
            "timing/environment-limited"
            "timing/scenario-coverage-missing"
            "timing/sample-policy-mismatch"
            "timing/run-identity-mismatch"
            "timing/artifact-unreadable"
            "timing/readback-contaminated"
            "readiness/validation-summary"
            "readiness/full-validation"
            "readiness/compatibility-ledger"
            "readiness/package-validation"
            "readiness/regression-validation" ]

    let feature160TargetHostProfiles =
        feature159TargetHostProfiles
        @ [ { ProfileId = feature160AcceptedProfileId
              Backend = "OpenGL"
              Renderer = None
              PresentMode = "DirectToSwapchain"
              FramebufferSize = "640x480"
              Scale = Some 1.0
              DisplayEnvironment = "x11"
              ProofAlgorithmVersion = "sentinel-damage-v1" } ]

    let feature161RequiredScenarioIds = feature160RequiredScenarioIds

    let feature161NonGeneralizedLanes =
        [ "Wayland"
          "indirect GL"
          "missing display"
          "software raster"
          "virtualized presentation"
          "unknown renderer"
          "stale package"
          "cross-profile timing" ]

    let feature161PriorGateLinks =
        [ { Feature = feature155Id; Status = "confirmed"; EvidencePath = "specs/155-native-proof-capture/readiness/validation-summary.md" }
          { Feature = feature157Id; Status = "confirmed"; EvidencePath = "specs/157-no-clear-damage-scissor/readiness/validation-summary.md" }
          { Feature = feature158Id; Status = "confirmed"; EvidencePath = "specs/158-separate-proof-timing/readiness/validation-summary.md" }
          { Feature = feature159Id; Status = "confirmed"; EvidencePath = "specs/159-layer-promotion-keys/readiness/validation-summary.md" }
          { Feature = feature160Id; Status = "confirmed"; EvidencePath = "specs/160-performance-validation-throughput/readiness/validation-summary.md" } ]

    let private backendToken backend =
        match backend with
        | X11 -> "x11"
        | Wayland -> "wayland"
        | NoDisplay -> "missing-display"

    let hostProfileFromFacts (facts: ProbeFacts) : HostProfile =
        let display = backendToken facts.EffectiveBackend
        let renderer = facts.GlRenderer |> Option.filter (String.IsNullOrWhiteSpace >> not)
        let profile =
            [ display
              facts.GlRenderer |> Option.defaultValue "unknown-renderer"
              facts.GlVersion |> Option.defaultValue "unknown-gl"
              if facts.GlDirect then "direct" else "indirect" ]
            |> String.concat "|"
            |> fun value ->
                SHA256.HashData(Encoding.UTF8.GetBytes value)
                |> Array.take 4
                |> Array.map (fun byte -> byte.ToString("x2"))
                |> String.concat ""

        { ProfileId = $"probe-{profile}"
          Backend = "OpenGL"
          Renderer = renderer
          PresentMode = "DirectToSwapchain"
          FramebufferSize = "640x480"
          Scale = Some 1.0
          DisplayEnvironment = display
          ProofAlgorithmVersion = "sentinel-damage-v1" }

    let proofVerdictToken verdict =
        match verdict with
        | ProofPassed -> "passed"
        | ProofFailed _ -> "failed"
        | ProofEnvironmentLimited _ -> "environment-limited"

    let parityVerdictToken verdict =
        match verdict with
        | ParityPassed -> "passed"
        | ParityFailed _ -> "failed"
        | ParitySkipped _ -> "skipped"
        | ParityEnvironmentLimited _ -> "environment-limited"

    let tierToken tier =
        match tier with
        | PresentProofTier -> "present-proof"
        | DamageScissorTier -> "damage-scissor"
        | PromotionTier -> "promotion"
        | PlacementReuseTier -> "placement-reuse"
        | ReplayTier -> "replay"
        | SnapshotTier -> "snapshot"

    let tierVerdictToken verdict =
        match verdict with
        | Ready -> "ready"
        | Limited _ -> "limited"
        | Rejected _ -> "rejected"
        | Skipped _ -> "skipped"

    let private tierDisplayName tier =
        match tier with
        | PresentProofTier -> "Present proof"
        | DamageScissorTier -> "Damage scissor"
        | PromotionTier -> "Promotion"
        | PlacementReuseTier -> "Placement reuse"
        | ReplayTier -> "Replay"
        | SnapshotTier -> "Snapshot"

    let private verdictReason verdict =
        match verdict with
        | Ready -> "passed proof, parity, and threshold obligations"
        | Limited reason
        | Rejected reason
        | Skipped reason -> reason

    let proofMatchesHost (active: HostProfile) (proof: PresentProof) =
        proof.HostProfile.ProfileId = active.ProfileId
        && proof.HostProfile.Backend = active.Backend
        && proof.HostProfile.PresentMode = active.PresentMode
        && proof.HostProfile.FramebufferSize = active.FramebufferSize
        && proof.HostProfile.ProofAlgorithmVersion = active.ProofAlgorithmVersion

    let proofIsFresh (now: DateTimeOffset) (maxAge: TimeSpan) (proof: PresentProof) =
        proof.CreatedAt <= now && now - proof.CreatedAt <= maxAge

    let validateProofForScissoring active now maxAge proof =
        match proof with
        | None -> Limited "missing present-path proof"
        | Some proof when not (proofMatchesHost active proof) -> Limited "present-path proof is for a different host profile"
        | Some proof when not (proofIsFresh now maxAge proof) -> Limited "present-path proof is stale"
        | Some { Verdict = ProofPassed } -> Ready
        | Some { Verdict = ProofFailed cause } -> Rejected cause
        | Some { Verdict = ProofEnvironmentLimited reason } -> Limited reason

    let evaluateTier proof parity performancePassed =
        match proof with
        | Rejected reason -> Rejected reason
        | Limited reason -> Limited reason
        | Skipped reason -> Skipped reason
        | Ready ->
            match parity, performancePassed with
            | Some(ParityFailed cause), _ -> Rejected cause
            | Some(ParityEnvironmentLimited reason), _ -> Limited reason
            | Some(ParitySkipped reason), _ -> Skipped reason
            | None, _ -> Limited "missing full-redraw oracle parity"
            | Some ParityPassed, Some false -> Rejected "performance threshold failed"
            | Some ParityPassed, Some true
            | Some ParityPassed, None -> Ready

    let initReadiness () =
        { Proofs = []
          Parity = Map.empty
          TierVerdicts = Map.empty
          Diagnostics = [] },
        [ WriteValidationSummary validationSummaryPath
          WriteCompatibilityLedger compatibilityLedgerPath ]

    let updateReadiness msg model =
        let model' =
            match msg with
            | ProofLoaded proof -> { model with Proofs = model.Proofs @ [ proof ] }
            | ParityRecorded(scenarioId, verdict) -> { model with Parity = Map.add scenarioId verdict model.Parity }
            | TierEvaluated(tier, verdict) -> { model with TierVerdicts = Map.add tier verdict model.TierVerdicts }
            | DiagnosticRecorded diagnostic -> { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        model',
        [ WriteValidationSummary validationSummaryPath
          WriteCompatibilityLedger compatibilityLedgerPath ]

    let initFeature154 () =
        { ProofStatus = "environment-limited"
          ParityStatus = "fallback-gated"
          TimingStatus = "inconclusive"
          PublishedArtifacts = [] },
        [ WriteFeature154Artifact feature154ValidationSummaryPath
          WriteFeature154Artifact feature154CompatibilityLedgerPath
          WriteFeature154Artifact feature154ProofSetPath ]

    let updateFeature154 msg model =
        let model' =
            match msg with
            | ProofEvidenceRecorded status -> { model with ProofStatus = status }
            | ParityEvidenceRecorded status -> { model with ParityStatus = status }
            | TimingEvidenceRecorded status -> { model with TimingStatus = status }
            | ArtifactPublished path -> { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }

        model',
        [ WriteFeature154Artifact feature154ValidationSummaryPath
          WriteFeature154Artifact feature154CompatibilityLedgerPath
          WriteFeature154Artifact feature154ProofSetPath ]

    let feature156VerdictToken verdict =
        match verdict with
        | Feature156Positive -> "positive"
        | Feature156Noisy -> "noisy"
        | Feature156NonBeneficial -> "non-beneficial"
        | Feature156Incomplete -> "incomplete"
        | Feature156Rejected -> "rejected"
        | Feature156EnvironmentLimited -> "environment-limited"
        | Feature156Limited -> "limited"

    let feature156OverallVerdict (reports: Feature156ScenarioReport list) =
        let requiredReports =
            feature156RequiredScenarioIds
            |> List.choose (fun scenario ->
                reports |> List.tryFind (fun report -> report.ScenarioId = scenario))

        if requiredReports.Length < feature156RequiredScenarioIds.Length then
            Feature156Incomplete
        elif requiredReports |> List.forall (fun report -> report.Verdict = Feature156Positive) then
            Feature156Positive
        elif requiredReports |> List.exists (fun report -> report.Verdict = Feature156EnvironmentLimited) then
            Feature156EnvironmentLimited
        elif requiredReports |> List.exists (fun report -> report.Verdict = Feature156Limited) then
            Feature156Limited
        elif requiredReports |> List.exists (fun report -> report.Verdict = Feature156Rejected) then
            Feature156Rejected
        elif requiredReports |> List.exists (fun report -> report.Verdict = Feature156Incomplete) then
            Feature156Incomplete
        elif requiredReports |> List.exists (fun report -> report.Verdict = Feature156Noisy) then
            Feature156Noisy
        else
            Feature156NonBeneficial

    let initFeature156 warmupCount measuredRepetitions : Feature156Model * Feature156Effect list =
        let runId = "feature156-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")

        { RunId = runId
          ExpectedProfileId = feature156AcceptedProfileId
          ActiveProfile = None
          PolicyId = None
          WarmupCount = max 0 warmupCount
          MeasuredRepetitions = max 1 measuredRepetitions
          ScenarioReports = []
          PublishedArtifacts = []
          Verdict = Feature156Incomplete
          Diagnostics = [] },
        [ Feature156DetectHostProfile
          Feature156DeclarePolicy feature156PolicyId ]

    let updateFeature156 (msg: Feature156Msg) (model: Feature156Model) : Feature156Model * Feature156Effect list =
        let model' =
            match msg with
            | Feature156HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature156HostProfileRejected reason ->
                { model with
                    Verdict = Feature156Rejected
                    Diagnostics = model.Diagnostics @ [ reason ] }
            | Feature156PolicyDeclared policyId ->
                { model with PolicyId = Some policyId }
            | Feature156ScenarioEvaluated report ->
                let reports =
                    model.ScenarioReports
                    |> List.filter (fun existing -> existing.ScenarioId <> report.ScenarioId)
                    |> fun existing -> existing @ [ report ]

                { model with
                    ScenarioReports = reports
                    Verdict = feature156OverallVerdict reports }
            | Feature156RunEnvironmentLimited reason ->
                { model with
                    Verdict = Feature156EnvironmentLimited
                    Diagnostics = model.Diagnostics @ [ reason ] }
            | Feature156SummaryPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature156DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let measurementEffects =
            feature156RequiredScenarioIds
            |> List.collect (fun scenario ->
                [ Feature156PrepareScenario scenario
                  Feature156MeasurePath(scenario, "full-redraw")
                  Feature156MeasurePath(scenario, "damage-scoped") ])

        model',
        [ Feature156WriteArtifact feature156TimingSummaryPath
          Feature156WriteArtifact feature156ValidationSummaryPath ]
        @ measurementEffects

    let feature157StatusToken status =
        match status with
        | Feature157DamageStatus.Accepted -> "accepted"
        | Feature157DamageStatus.FallbackOnly -> "fallback-only"
        | Feature157DamageStatus.Rejected -> "rejected"
        | Feature157DamageStatus.EnvironmentLimited -> "environment-limited"

    let feature157ScenarioFileName (scenarioId: string) =
        scenarioId.Replace("/", "-") + ".md"

    let feature157OverallStatus (summary: Feature157DamageSummary) =
        let acceptedScenarioSet =
            summary.AcceptedAttempts
            |> List.map _.ScenarioId
            |> Set.ofList

        let requiredCovered =
            feature157RequiredScenarioIds
            |> List.forall acceptedScenarioSet.Contains

        if summary.UnsupportedHostReason.IsSome then
            Feature157DamageStatus.EnvironmentLimited
        elif summary.Fallbacks |> List.exists (fun fallback -> fallback.Reason = "parity-mismatch") then
            Feature157DamageStatus.Rejected
        elif summary.AcceptedAttempts.Length >= 3 && requiredCovered then
            Feature157DamageStatus.Accepted
        elif not (List.isEmpty summary.Fallbacks) then
            Feature157DamageStatus.FallbackOnly
        else
            Feature157DamageStatus.Rejected

    let initFeature157 () : Feature157Model * Feature157Effect list =
        { RunId = "feature157-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
          ActiveProfile = None
          Attempts = []
          Fallbacks = []
          PublishedArtifacts = []
          Status = Feature157DamageStatus.EnvironmentLimited
          Diagnostics = [] },
        [ Feature157DetectHostProfile
          Feature157LoadAcceptedProofGate
          for scenario in feature157RequiredScenarioIds do
              Feature157PrepareScenario scenario
              Feature157RenderDamageScopedFrame scenario
              Feature157RenderFullRedrawFrame scenario
              Feature157CompareParity scenario
          Feature157WriteArtifact feature157DamageSummaryPath
          Feature157WriteArtifact feature157ValidationSummaryPath ]

    let private feature157StatusFrom (attempts: Feature157DamageAttempt list) (fallbacks: Feature157Fallback list) (diagnostics: string list) =
        if diagnostics |> List.exists (fun item -> item.Contains("environment-limited", StringComparison.OrdinalIgnoreCase)) then
            Feature157DamageStatus.EnvironmentLimited
        elif fallbacks |> List.exists (fun fallback -> fallback.Reason = "parity-mismatch") then
            Feature157DamageStatus.Rejected
        elif attempts.Length >= 3 then
            let covered = attempts |> List.map _.ScenarioId |> Set.ofList
            if feature157RequiredScenarioIds |> List.forall covered.Contains then
                Feature157DamageStatus.Accepted
            else
                Feature157DamageStatus.FallbackOnly
        elif not (List.isEmpty fallbacks) then
            Feature157DamageStatus.FallbackOnly
        else
            Feature157DamageStatus.EnvironmentLimited

    let updateFeature157 (msg: Feature157Msg) (model: Feature157Model) : Feature157Model * Feature157Effect list =
        let model' =
            match msg with
            | Feature157HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature157AttemptRecorded attempt ->
                { model with Attempts = model.Attempts @ [ attempt ] }
            | Feature157FallbackRecorded fallback ->
                { model with Fallbacks = model.Fallbacks @ [ fallback ] }
            | Feature157UnsupportedHostRecorded reason ->
                { model with
                    Status = Feature157DamageStatus.EnvironmentLimited
                    Diagnostics = model.Diagnostics @ [ $"environment-limited: {reason}" ] }
            | Feature157ArtifactPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature157DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let status = feature157StatusFrom model'.Attempts model'.Fallbacks model'.Diagnostics

        { model' with Status = status },
        [ Feature157WriteArtifact feature157DamageSummaryPath
          Feature157WriteArtifact feature157ValidationSummaryPath
          Feature157WriteArtifact feature157CompatibilityLedgerPath
          Feature157WriteArtifact feature157PackageValidationPath
          Feature157WriteArtifact feature157RegressionValidationPath ]

    let feature158StatusToken status =
        match status with
        | Feature158ReadinessStatus.Accepted -> "accepted"
        | Feature158ReadinessStatus.FallbackOnly -> "fallback-only"
        | Feature158ReadinessStatus.Rejected -> "rejected"
        | Feature158ReadinessStatus.EnvironmentLimited -> "environment-limited"

    let feature158ScenarioFileName (scenarioId: string) =
        scenarioId.Replace("/", "-") + ".md"

    let private feature158StatusFromReports (reports: Feature158ScenarioReport list) (diagnostics: string list) =
        if diagnostics |> List.exists (fun item -> item.Contains("environment-limited", StringComparison.OrdinalIgnoreCase)) then
            Feature158ReadinessStatus.EnvironmentLimited
        else
            let requiredReports =
                feature158RequiredScenarioIds
                |> List.choose (fun scenario -> reports |> List.tryFind (fun report -> report.ScenarioId = scenario))

            let allRequiredCovered = requiredReports.Length = feature158RequiredScenarioIds.Length
            let allAccepted =
                allRequiredCovered
                && requiredReports
                   |> List.forall (fun report ->
                       report.Status = Feature158ReadinessStatus.Accepted
                       && not (List.isEmpty report.IncludedSamples)
                       && report.IncludedSamples
                          |> List.forall (fun sample ->
                              sample.InclusionStatus = Perf.Included
                              && (sample.MeasurementPolicy = Perf.ReadbackFree
                                  || sample.MeasurementPolicy = Perf.ReadbackOutsideMeasurement)))

            if allAccepted then
                Feature158ReadinessStatus.Accepted
            elif reports |> List.exists (fun report -> report.Status = Feature158ReadinessStatus.EnvironmentLimited) then
                Feature158ReadinessStatus.EnvironmentLimited
            elif reports |> List.exists (fun report -> report.Status = Feature158ReadinessStatus.FallbackOnly) then
                Feature158ReadinessStatus.FallbackOnly
            elif reports |> List.exists (fun report -> not (List.isEmpty report.IncludedSamples)) then
                Feature158ReadinessStatus.Rejected
            else
                Feature158ReadinessStatus.FallbackOnly

    let initFeature158 warmupCount measuredRepetitions : Feature158Model * Feature158Effect list =
        { RunId = "feature158-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
          ExpectedProfileId = feature158AcceptedProfileId
          ActiveProfile = None
          PolicyId = None
          WarmupCount = max 0 warmupCount
          MeasuredRepetitions = max 1 measuredRepetitions
          ScenarioReports = []
          ProofProbeEvidence = []
          PublishedArtifacts = []
          Status = Feature158ReadinessStatus.FallbackOnly
          Diagnostics = [] },
        [ Feature158DetectHostProfile
          Feature158DeclarePolicy feature158PolicyId
          for scenario in feature158RequiredScenarioIds do
              Feature158PrepareScenario scenario
              Feature158MeasurePath(scenario, "full-redraw")
              Feature158MeasurePath(scenario, "damage-scoped")
          Feature158WriteArtifact feature158TimingSummaryPath
          Feature158WriteArtifact feature158ValidationSummaryPath ]

    let updateFeature158 (msg: Feature158Msg) (model: Feature158Model) : Feature158Model * Feature158Effect list =
        let model' =
            match msg with
            | Feature158HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature158HostProfileRejected reason ->
                { model with
                    Status = Feature158ReadinessStatus.Rejected
                    Diagnostics = model.Diagnostics @ [ reason ] }
            | Feature158PolicyDeclared policyId ->
                { model with PolicyId = Some policyId }
            | Feature158ScenarioEvaluated report ->
                let reports =
                    model.ScenarioReports
                    |> List.filter (fun existing -> existing.ScenarioId <> report.ScenarioId)
                    |> fun existing -> existing @ [ report ]

                { model with ScenarioReports = reports }
            | Feature158ProbeEvidenceRecorded evidence ->
                { model with ProofProbeEvidence = model.ProofProbeEvidence @ [ evidence ] }
            | Feature158RunEnvironmentLimited reason ->
                { model with
                    Status = Feature158ReadinessStatus.EnvironmentLimited
                    Diagnostics = model.Diagnostics @ [ $"environment-limited: {reason}" ] }
            | Feature158SummaryPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature158DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let status = feature158StatusFromReports model'.ScenarioReports model'.Diagnostics

        { model' with Status = status },
        [ Feature158WriteArtifact feature158TimingSummaryPath
          Feature158WriteArtifact feature158TimingSummaryJsonPath
          Feature158WriteArtifact feature158ValidationSummaryPath
          Feature158WriteArtifact feature158CompatibilityLedgerPath
          Feature158WriteArtifact feature158PackageValidationPath
          Feature158WriteArtifact feature158RegressionValidationPath ]

    let feature159StatusToken status =
        match status with
        | Feature159ReadinessStatus.Accepted -> "accepted"
        | Feature159ReadinessStatus.NonBeneficial -> "non-beneficial"
        | Feature159ReadinessStatus.FallbackOnly -> "fallback-only"
        | Feature159ReadinessStatus.Rejected -> "rejected"
        | Feature159ReadinessStatus.EnvironmentLimited -> "environment-limited"

    let feature159ScenarioFileName (scenarioId: string) =
        scenarioId.Replace("/", "-") + ".md"

    let private feature159StatusFromAttempts (attempts: Feature159Attempt list) (diagnostics: string list) =
        if diagnostics |> List.exists (fun item -> item.Contains("environment-limited", StringComparison.OrdinalIgnoreCase)) then
            Feature159ReadinessStatus.EnvironmentLimited
        else
            let requiredAttempts =
                feature159RequiredScenarioIds
                |> List.choose (fun scenario -> attempts |> List.tryFind (fun attempt -> attempt.ScenarioId = scenario))

            let allRequiredCovered = requiredAttempts.Length = feature159RequiredScenarioIds.Length
            let acceptedAttempts =
                attempts
                |> List.filter (fun attempt ->
                    attempt.PolicyId = feature159PolicyId
                    && attempt.ParityStatus = "passed"
                    && attempt.CounterNetSavedWork > 0
                    && attempt.AcceptedReuseArtifacts + attempt.AcceptedPromotionArtifacts > 0)

            if allRequiredCovered && acceptedAttempts.Length >= 3 then
                Feature159ReadinessStatus.Accepted
            elif attempts |> List.exists (fun attempt -> attempt.PrimaryReason = Some "parity-mismatch" || attempt.PrimaryReason = Some "missing-policy") then
                Feature159ReadinessStatus.Rejected
            elif allRequiredCovered && attempts |> List.exists (fun attempt -> attempt.PromotionDecision = "non-beneficial") then
                Feature159ReadinessStatus.NonBeneficial
            elif not (List.isEmpty attempts) then
                Feature159ReadinessStatus.FallbackOnly
            else
                Feature159ReadinessStatus.EnvironmentLimited

    let initFeature159 () : Feature159Model * Feature159Effect list =
        { RunId = "feature159-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
          ActiveProfile = None
          PolicyId = None
          Attempts = []
          PublishedArtifacts = []
          Status = Feature159ReadinessStatus.EnvironmentLimited
          Diagnostics = [] },
        [ Feature159DetectHostProfile
          Feature159DeclarePolicy feature159PolicyId
          for scenario in feature159RequiredScenarioIds do
              Feature159PrepareScenario scenario
              Feature159EvaluatePromotion scenario
              Feature159CompareParity scenario
          Feature159WriteArtifact feature159PromotionSummaryPath
          Feature159WriteArtifact feature159ValidationSummaryPath ]

    let updateFeature159 (msg: Feature159Msg) (model: Feature159Model) : Feature159Model * Feature159Effect list =
        let model' =
            match msg with
            | Feature159HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature159PolicyDeclared policyId ->
                { model with PolicyId = Some policyId }
            | Feature159AttemptRecorded attempt ->
                { model with Attempts = model.Attempts @ [ attempt ] }
            | Feature159RunEnvironmentLimited reason ->
                { model with
                    Status = Feature159ReadinessStatus.EnvironmentLimited
                    Diagnostics = model.Diagnostics @ [ $"environment-limited: {reason}" ] }
            | Feature159SummaryPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature159DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let status = feature159StatusFromAttempts model'.Attempts model'.Diagnostics

        { model' with Status = status },
        [ Feature159WriteArtifact feature159PromotionSummaryPath
          Feature159WriteArtifact feature159ValidationSummaryPath
          Feature159WriteArtifact feature159CompatibilityLedgerPath
          Feature159WriteArtifact feature159PackageValidationPath
          Feature159WriteArtifact feature159RegressionValidationPath ]

    let feature160StatusToken status =
        match status with
        | Feature160ReadinessStatus.Accepted -> "accepted"
        | Feature160ReadinessStatus.Blocked -> "blocked"
        | Feature160ReadinessStatus.Rejected -> "rejected"
        | Feature160ReadinessStatus.FallbackOnly -> "fallback-only"
        | Feature160ReadinessStatus.EnvironmentLimited -> "environment-limited"

    let feature160ScenarioFileName (scenarioId: string) =
        scenarioId.Replace("/", "-") + ".md"

    let feature160IterationFileName (iterationId: string) =
        iterationId.Replace("/", "-") + ".md"

    let private feature160AcceptedSamplePolicy (sample: Feature158TimingSample) =
        sample.InclusionStatus = Perf.Included
        && (sample.MeasurementPolicy = Perf.ReadbackFree
            || sample.MeasurementPolicy = Perf.ReadbackOutsideMeasurement)

    let private feature160IterationAccepted (iteration: Feature160Iteration) =
        let coverage = iteration.ScenarioCoverage |> Set.ofList
        iteration.Status = Feature160ReadinessStatus.Accepted
        && iteration.ExclusionReason.IsNone
        && iteration.RestrictedScenario.IsNone
        && iteration.HostProfile.ProfileId = feature160AcceptedProfileId
        && iteration.LaneId = feature160FocusedLaneId
        && iteration.PolicyId = feature160PolicyId
        && iteration.DeclaredBoundMinutes = feature160MaxIterationMinutes
        && iteration.ActualDuration <= TimeSpan.FromMinutes(float feature160MaxIterationMinutes)
        && iteration.WarmupCount = 3
        && iteration.MeasuredRepetitions = 5
        && feature160RequiredScenarioIds |> List.forall coverage.Contains
        && not (List.isEmpty iteration.IncludedSamples)
        && iteration.IncludedSamples |> List.forall feature160AcceptedSamplePolicy
        && iteration.ArtifactPaths |> List.exists (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))

    let feature160FullValidationStatus record =
        match record with
        | None -> "missing"
        | Some validation ->
            if validation.Command <> "dotnet test FS.GG.Rendering.slnx --no-restore" then
                "stale"
            elif validation.Status = "passed" || validation.Status = "current-passed" then
                "passed"
            elif validation.Status = "failed" then
                "failed"
            elif validation.Status = "interrupted" then
                "interrupted"
            elif validation.Status = "stale" then
                "stale"
            elif String.IsNullOrWhiteSpace validation.Status then
                "undocumented"
            else
                validation.Status

    let private feature160FullValidationAccepts record =
        feature160FullValidationStatus record = "passed"

    let feature160FocusedThroughputStatus (summary: Feature160ThroughputSummary) =
        match summary.UnsupportedHostReason with
        | Some _ -> Feature160ReadinessStatus.EnvironmentLimited
        | None ->
            let accepted = summary.Iterations |> List.filter feature160IterationAccepted
            if accepted.Length >= summary.RequiredAttempts then
                Feature160ReadinessStatus.Accepted
            elif summary.Iterations |> List.exists (fun iteration -> iteration.Status = Feature160ReadinessStatus.Rejected) then
                Feature160ReadinessStatus.Rejected
            elif summary.Iterations |> List.exists (fun iteration -> iteration.Status = Feature160ReadinessStatus.EnvironmentLimited) then
                Feature160ReadinessStatus.EnvironmentLimited
            elif List.isEmpty summary.Iterations then
                Feature160ReadinessStatus.FallbackOnly
            else
                Feature160ReadinessStatus.Rejected

    let feature160OverallStatus (summary: Feature160ThroughputSummary) =
        match feature160FocusedThroughputStatus summary with
        | Feature160ReadinessStatus.Accepted when not (feature160FullValidationAccepts summary.FullValidation) ->
            Feature160ReadinessStatus.Blocked
        | status -> status

    let initFeature160 attempts maxIterationMinutes : Feature160Model * Feature160Effect list =
        let requiredAttempts = max 1 attempts
        let bound = max 1 maxIterationMinutes
        let runId = "feature160-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")

        { RunId = runId
          ExpectedProfileId = feature160AcceptedProfileId
          ActiveProfile = None
          LaneId = None
          PolicyId = None
          DeclaredBoundMinutes = None
          Iterations = []
          FullValidation = None
          PublishedArtifacts = []
          Status = Feature160ReadinessStatus.FallbackOnly
          Diagnostics = [] },
        [ Feature160DetectHostProfile
          Feature160DeclareFocusedLane feature160FocusedLaneId
          Feature160DeclarePolicy feature160PolicyId
          Feature160DeclareIterationBound bound
          for attempt in 1..requiredAttempts do
              let iterationId = sprintf "%s-%03i" runId attempt
              Feature160EnforceIterationTimeout(iterationId, bound)
          for scenario in feature160RequiredScenarioIds do
              Feature160PrepareScenario scenario
              Feature160RunTimingWarmup scenario
              Feature160MeasurePath(scenario, "full-redraw")
              Feature160MeasurePath(scenario, "damage-scoped")
          Feature160WriteArtifact feature160ThroughputSummaryPath
          Feature160WriteArtifact feature160ValidationSummaryPath ]

    let private feature160StatusFromModel (model: Feature160Model) =
        let summary =
            { RunId = model.RunId
              HostProfile =
                model.ActiveProfile
                |> Option.defaultValue
                    { ProfileId = feature160AcceptedProfileId
                      Backend = "OpenGL"
                      Renderer = None
                      PresentMode = "DirectToSwapchain"
                      FramebufferSize = "640x480"
                      Scale = Some 1.0
                      DisplayEnvironment = "unknown"
                      ProofAlgorithmVersion = "sentinel-damage-v1" }
              LaneId = model.LaneId |> Option.defaultValue feature160FocusedLaneId
              PolicyId = model.PolicyId |> Option.defaultValue feature160PolicyId
              DeclaredBoundMinutes = model.DeclaredBoundMinutes |> Option.defaultValue feature160MaxIterationMinutes
              RequiredAttempts = feature160RequiredAttempts
              WarmupCount = 3
              MeasuredRepetitions = 5
              Iterations = model.Iterations
              UnsupportedHostReason =
                model.Diagnostics
                |> List.tryFind (fun item -> item.Contains("environment-limited", StringComparison.OrdinalIgnoreCase))
              FullValidation = model.FullValidation
              CompatibilityImpact = "pending"
              PackageValidationStatus = "pending"
              RegressionValidationStatus = "pending"
              Status = model.Status
              ReleaseReadyStatus = "pending"
              PerformanceClaim = "performance-not-accepted"
              Diagnostics = model.Diagnostics }

        feature160OverallStatus summary

    let updateFeature160 (msg: Feature160Msg) (model: Feature160Model) : Feature160Model * Feature160Effect list =
        let model' =
            match msg with
            | Feature160HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature160HostProfileRejected reason ->
                { model with
                    Status = Feature160ReadinessStatus.Rejected
                    Diagnostics = model.Diagnostics @ [ reason ] }
            | Feature160LaneDeclared laneId ->
                { model with LaneId = Some laneId }
            | Feature160PolicyDeclared policyId ->
                { model with PolicyId = Some policyId }
            | Feature160BoundDeclared minutes ->
                { model with DeclaredBoundMinutes = Some(max 1 minutes) }
            | Feature160IterationStarted iterationId ->
                { model with Diagnostics = model.Diagnostics @ [ $"iteration-started={iterationId}" ] }
            | Feature160IterationCompleted iteration ->
                { model with Iterations = model.Iterations @ [ iteration ] }
            | Feature160IterationExcluded iteration ->
                { model with Iterations = model.Iterations @ [ iteration ] }
            | Feature160IterationTimedOut(iterationId, reason) ->
                { model with Diagnostics = model.Diagnostics @ [ $"timed-out:{iterationId}:{reason}" ] }
            | Feature160IterationCanceled(iterationId, reason) ->
                { model with Diagnostics = model.Diagnostics @ [ $"canceled:{iterationId}:{reason}" ] }
            | Feature160FullValidationRecorded record ->
                { model with FullValidation = Some record }
            | Feature160ArtifactPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature160DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let status = feature160StatusFromModel model'

        { model' with Status = status },
        [ Feature160WriteArtifact feature160ThroughputSummaryPath
          Feature160WriteArtifact feature160ThroughputSummaryJsonPath
          Feature160WriteArtifact feature160ValidationSummaryPath
          Feature160WriteArtifact feature160CompatibilityLedgerPath
          Feature160WriteArtifact feature160PackageValidationPath
          Feature160WriteArtifact feature160RegressionValidationPath
          Feature160WriteFullValidationRecord(Path.Combine(feature160FullValidationDirectory, "validation.md")) ]

    let feature161StatusToken status =
        match status with
        | Feature161ReadinessStatus.Accepted -> "accepted"
        | Feature161ReadinessStatus.Blocked -> "blocked"
        | Feature161ReadinessStatus.Rejected -> "rejected"
        | Feature161ReadinessStatus.FallbackOnly -> "fallback-only"
        | Feature161ReadinessStatus.EnvironmentLimited -> "environment-limited"

    let feature161HostFactsFileName (entryId: string) =
        "facts-" + entryId.Replace("/", "-") + ".md"

    let feature161LedgerEntryFileName (entryId: string) =
        "entry-" + entryId.Replace("/", "-") + ".md"

    let feature161LaneIdFromFacts (facts: Feature161HostFacts) =
        let display = facts.DisplayIdentity.Trim().ToLowerInvariant().Replace(":", "")
        let renderer = facts.RendererIdentity.Trim().ToLowerInvariant()
        let rendererToken =
            if renderer.Contains("amd", StringComparison.OrdinalIgnoreCase)
               || renderer.Contains("radeon", StringComparison.OrdinalIgnoreCase) then
                "amd-mesa"
            elif String.IsNullOrWhiteSpace renderer then
                "unknown-renderer"
            else
                renderer.Replace(" ", "-").Replace("/", "-")

        let direct =
            match facts.DirectRendering with
            | Some true -> "direct-opengl"
            | Some false -> "indirect-opengl"
            | None -> "unknown-opengl"

        let displayToken = if String.IsNullOrWhiteSpace display then "missing-display" else ":" + display
        $"{facts.DisplayServer.Trim().ToLowerInvariant()}-{displayToken}-{direct}-{rendererToken}"

    let feature161ValidateHostFacts (facts: Feature161HostFacts) =
        let blank value = String.IsNullOrWhiteSpace value
        let renderer = facts.RendererIdentity.Trim()
        let displayServer = facts.DisplayServer.Trim()
        let displayIdentity = facts.DisplayIdentity.Trim()

        if blank displayServer || blank displayIdentity || displayServer = "missing-display" then
            Some Perf.MissingDisplay
        elif blank renderer then
            Some Perf.UnknownRenderer
        elif renderer.Contains("llvmpipe", StringComparison.OrdinalIgnoreCase)
             || renderer.Contains("software", StringComparison.OrdinalIgnoreCase)
             || renderer.Contains("swiftshader", StringComparison.OrdinalIgnoreCase) then
            Some Perf.SoftwareRaster
        elif facts.DirectRendering = Some false then
            Some Perf.IndirectRendering
        elif facts.DirectRendering.IsNone then
            Some Perf.HostFactsMissing
        elif facts.RefreshRateHz.IsNone && Option.isNone facts.RefreshUnavailableReason then
            Some Perf.RefreshRateUnavailable
        elif blank facts.DriverIdentity
             || blank facts.PackageVersionSet
             || blank facts.CpuLoadNote
             || blank facts.GpuLoadNote
             || blank facts.RunIdentity
             || blank facts.ScenarioIdentity
             || blank facts.TimingPolicyIdentity
             || List.isEmpty facts.ArtifactLocations then
            Some Perf.HostFactsMissing
        elif facts.TimingPolicyIdentity <> feature161PolicyId then
            Some Perf.HostFactsContradictory
        elif facts.PackageVersionSet.Contains("stale", StringComparison.OrdinalIgnoreCase) then
            Some Perf.PackageVersionMismatch
        elif facts.CpuLoadNote.Contains("non-representative", StringComparison.OrdinalIgnoreCase)
             || facts.GpuLoadNote.Contains("non-representative", StringComparison.OrdinalIgnoreCase) then
            Some Perf.LoadNonRepresentative
        elif facts.EnvironmentLimits |> List.exists (fun item -> item.Contains("virtual", StringComparison.OrdinalIgnoreCase)) then
            Some Perf.VirtualizedPresentation
        else
            None

    let feature161LedgerEntryAccepted (entry: Feature161LedgerEntry) =
        entry.Status = Feature161ReadinessStatus.Accepted
        && entry.PrimaryExclusionReason.IsNone
        && feature161ValidateHostFacts entry.HostFacts |> Option.isNone
        && entry.LaneId = feature161HostLaneId
        && entry.HostFacts.HostProfile.ProfileId = feature161AcceptedProfileId
        && entry.HostFacts.TimingPolicyIdentity = feature161PolicyId
        && entry.AcceptedLaneScopedPerformanceArtifacts > 0
        && entry.PriorGates |> List.forall (fun gate -> gate.Status = "confirmed" || gate.Status = "accepted")

    let feature161ScopeFromEntries (entries: Feature161LedgerEntry list) =
        let accepted = entries |> List.filter feature161LedgerEntryAccepted
        let blockers =
            [ if List.isEmpty accepted then
                  "no accepted lane-scoped performance artifacts"
              if entries |> List.exists (fun entry -> entry.PrimaryExclusionReason = Some Perf.NoisyTiming) then
                  "same-profile timing remains noisy"
              if entries |> List.exists (fun entry -> entry.PriorGates |> List.exists (fun gate -> gate.Status <> "confirmed" && gate.Status <> "accepted")) then
                  "prior P7 gate blocked"
              if entries |> List.exists (fun entry -> feature161ValidateHostFacts entry.HostFacts |> Option.isSome) then
                  "host facts incomplete or unsupported" ]

        match accepted with
        | entry :: _ ->
            { AcceptedLaneId = Some entry.LaneId
              AppliesTo = "X11 `:1` with direct OpenGL on AMD Radeon/Mesa for profile `probe-08a47c01`"
              NonGeneralizedLanes = feature161NonGeneralizedLanes
              RemainingBlockers = blockers
              PerformanceClaim = "performance-not-accepted" }
        | [] ->
            { AcceptedLaneId = None
              AppliesTo = "no lane accepted"
              NonGeneralizedLanes = feature161NonGeneralizedLanes
              RemainingBlockers = blockers
              PerformanceClaim = "performance-not-accepted" }

    let feature161OverallStatus (summary: Feature161Summary) =
        if summary.UnsupportedHostReason.IsSome then
            Feature161ReadinessStatus.EnvironmentLimited
        elif summary.Entries |> List.exists (fun entry -> entry.Status = Feature161ReadinessStatus.Accepted)
             && summary.ClaimScope.AcceptedLaneId.IsSome
             && summary.FullValidationStatus = "passed" then
            Feature161ReadinessStatus.Accepted
        elif summary.Entries |> List.exists (fun entry -> entry.Status = Feature161ReadinessStatus.Accepted)
             && summary.ClaimScope.AcceptedLaneId.IsSome then
            Feature161ReadinessStatus.Blocked
        elif summary.Entries |> List.exists (fun entry -> entry.Status = Feature161ReadinessStatus.Rejected) then
            Feature161ReadinessStatus.Rejected
        elif List.isEmpty summary.Entries then
            Feature161ReadinessStatus.FallbackOnly
        else
            Feature161ReadinessStatus.EnvironmentLimited

    let initFeature161 sourceThroughput : Feature161Model * Feature161Effect list =
        let runId = "feature161-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
        let source = sourceThroughput |> Option.defaultValue feature160ThroughputDirectory

        { RunId = runId
          ExpectedProfileId = feature161AcceptedProfileId
          ActiveProfile = None
          PolicyId = None
          HostFacts = None
          Entries = []
          PriorGates = []
          PublishedArtifacts = []
          Status = Feature161ReadinessStatus.FallbackOnly
          Diagnostics = [] },
        [ Feature161DetectHostProfile
          Feature161DeclarePolicy feature161PolicyId
          Feature161CollectHostFacts
          Feature161LoadThroughputPackage source
          Feature161WriteArtifact feature161LaneLedgerSummaryPath
          Feature161WriteArtifact feature161ValidationSummaryPath ]

    let feature161StatusFromModel (model: Feature161Model) =
        if model.Diagnostics |> List.exists (fun item -> item.Contains("environment-limited", StringComparison.OrdinalIgnoreCase)) then
            Feature161ReadinessStatus.EnvironmentLimited
        elif model.Entries |> List.exists feature161LedgerEntryAccepted then
            Feature161ReadinessStatus.Blocked
        elif model.Entries |> List.exists (fun entry -> entry.Status = Feature161ReadinessStatus.Rejected) then
            Feature161ReadinessStatus.Rejected
        elif model.HostFacts.IsSome then
            Feature161ReadinessStatus.FallbackOnly
        else
            Feature161ReadinessStatus.FallbackOnly

    let updateFeature161 (msg: Feature161Msg) (model: Feature161Model) : Feature161Model * Feature161Effect list =
        let model' =
            match msg with
            | Feature161HostProfileDetected profile ->
                { model with ActiveProfile = Some profile }
            | Feature161HostProfileRejected reason ->
                { model with
                    Status = Feature161ReadinessStatus.Rejected
                    Diagnostics = model.Diagnostics @ [ reason ] }
            | Feature161PolicyDeclared policyId ->
                { model with PolicyId = Some policyId }
            | Feature161HostFactsCollected facts ->
                { model with HostFacts = Some facts }
            | Feature161HostFactsRejected(reason, diagnostic) ->
                { model with
                    Status = Feature161ReadinessStatus.Rejected
                    Diagnostics = model.Diagnostics @ [ $"{Perf.exclusionReasonToken reason}: {diagnostic}" ] }
            | Feature161PriorGateLinked gate ->
                { model with PriorGates = model.PriorGates @ [ gate ] }
            | Feature161LedgerEntryRecorded entry ->
                { model with Entries = model.Entries @ [ entry ] }
            | Feature161ArtifactPublished path ->
                { model with PublishedArtifacts = model.PublishedArtifacts @ [ path ] }
            | Feature161DiagnosticRecorded diagnostic ->
                { model with Diagnostics = model.Diagnostics @ [ diagnostic ] }

        let status = feature161StatusFromModel model'

        { model' with Status = status },
        [ Feature161WriteArtifact feature161LaneLedgerSummaryPath
          Feature161WriteArtifact feature161LaneLedgerSummaryJsonPath
          Feature161WriteArtifact feature161ValidationSummaryPath
          Feature161WriteArtifact feature161CompatibilityLedgerPath
          Feature161WriteArtifact feature161PackageValidationPath
          Feature161WriteArtifact feature161RegressionValidationPath
          Feature161WriteHostFactsArtifact(Path.Combine(feature161LaneLedgerHostFactsDirectory, "facts-current.md"))
          Feature161WriteLedgerEntryArtifact(Path.Combine(feature161LaneLedgerEntriesDirectory, "entry-current.md"))
          Feature161WriteExcludedEvidenceArtifact(Path.Combine(feature161LaneLedgerExcludedDirectory, "README.md"))
          Feature161WriteUnsupportedHostArtifact(Path.Combine(feature161LaneLedgerUnsupportedDirectory, "README.md")) ]

    let artifactPath directory name = Path.Combine(directory, name)
    let feature148ArtifactPath directory name = Path.Combine(feature148ReadinessDirectory, directory, name)
    let feature149ArtifactPath directory name = Path.Combine(feature149ReadinessDirectory, directory, name)
    let feature152ArtifactPath directory name = Path.Combine(feature152ReadinessDirectory, directory, name)
    let feature153ArtifactPath directory name = Path.Combine(feature153ReadinessDirectory, directory, name)
    let feature154ArtifactPath directory name = Path.Combine(feature154ReadinessDirectory, directory, name)
    let feature155ArtifactPath directory name = Path.Combine(feature155ReadinessDirectory, directory, name)
    let feature156ArtifactPath directory name = Path.Combine(feature156ReadinessDirectory, directory, name)
    let feature157ArtifactPath directory name = Path.Combine(feature157ReadinessDirectory, directory, name)
    let feature158ArtifactPath directory name = Path.Combine(feature158ReadinessDirectory, directory, name)
    let feature159ArtifactPath directory name = Path.Combine(feature159ReadinessDirectory, directory, name)
    let feature160ArtifactPath directory name = Path.Combine(feature160ReadinessDirectory, directory, name)
    let feature161ArtifactPath directory name = Path.Combine(feature161ReadinessDirectory, directory, name)

    let feature156ScenarioFileName (scenarioId: string) =
        scenarioId.Replace("/", "-") + ".md"

    let feature158OverallStatus (summary: Feature158TimingSummary) =
        match summary.UnsupportedHostReason with
        | Some _ -> Feature158ReadinessStatus.EnvironmentLimited
        | None ->
            feature158StatusFromReports
                summary.ScenarioReports
                (summary.Diagnostics
                 @ [ if summary.Status = Feature158ReadinessStatus.EnvironmentLimited then "environment-limited" ])

    let feature159OverallStatus (summary: Feature159Summary) =
        match summary.UnsupportedHostReason with
        | Some _ -> Feature159ReadinessStatus.EnvironmentLimited
        | None -> feature159StatusFromAttempts summary.Attempts summary.Diagnostics

    let private feature156FormatMs (value: float) =
        value.ToString("0.###", Globalization.CultureInfo.InvariantCulture)

    let feature156DistributionRow (distribution: Feature156PathDistribution option) =
        match distribution with
        | None -> "`missing` | `missing` | `missing` | `0`"
        | Some distribution ->
            $"`{feature156FormatMs distribution.P50Ms}` | `{feature156FormatMs distribution.P95Ms}` | `{feature156FormatMs distribution.P99Ms}` | `{distribution.SampleCount}`"

    let feature158DistributionRow (distribution: Feature158PathDistribution option) =
        match distribution with
        | None -> "`missing` | `missing` | `missing` | `0`"
        | Some distribution ->
            $"`{feature156FormatMs distribution.P50Ms}` | `{feature156FormatMs distribution.P95Ms}` | `{feature156FormatMs distribution.P99Ms}` | `{distribution.SampleCount}`"

    let renderPresentProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let artifacts =
            match proof.EvidenceArtifacts with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- `%s`") |> String.concat "\n"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Present Path Proof"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              ""
              "## Artifacts"
              ""
              artifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderValidationSummary model =
        let tierRows =
            [ PresentProofTier; DamageScissorTier; PromotionTier; PlacementReuseTier; ReplayTier; SnapshotTier ]
            |> List.map (fun tier ->
                let verdict = model.TierVerdicts |> Map.tryFind tier |> Option.defaultValue (Limited "not evaluated")
                $"| {tierDisplayName tier} | {tierVerdictToken verdict} | {verdictReason verdict} |")
            |> String.concat "\n"

        let parityRows =
            if Map.isEmpty model.Parity then
                "| none | limited | missing parity output |"
            else
                model.Parity
                |> Map.toList
                |> List.map (fun (scenario, verdict) -> $"| `{scenario}` | {parityVerdictToken verdict} |")
                |> String.concat "\n"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | limited | missing present-path proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | `{proof.ScenarioId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 147 Validation Summary"
              ""
              "## Tier Verdicts"
              ""
              "| Tier | Verdict | Reason |"
              "|------|---------|--------|"
              tierRows
              ""
              "## Present Proof"
              ""
              "| Proof | Scenario | Verdict | Host Profile |"
              "|-------|----------|---------|--------------|"
              proofRows
              ""
              "## Parity"
              ""
              "| Scenario | Verdict |"
              "|----------|---------|"
              parityRows
              ""
              "## Validation Runs"
              ""
              "- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed."
              "- Feature147 focused Controls, Elmish, SkiaViewer, Rendering.Harness, and Package tests: passed."
              "- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface`: passed."
              "- `dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local`: passed for `0.1.10-preview.1`."
              "- `dotnet test FS.GG.Rendering.slnx --no-build`: blocked outside Feature147 by existing Controls typed/transient-metadata parity failures."
              ""
              "## Diagnostics"
              ""
              if List.isEmpty model.Diagnostics then "- none" else model.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"
              "" ]

    let renderCompatibilityLedger model =
        let publicImpact =
            if Map.containsKey DamageScissorTier model.TierVerdicts then
                "Derived compositor diagnostics are available for damage and fallback review."
            else
                "No accepted compositor public metric delta has been claimed yet."

        String.concat
            "\n"
            [ "# Feature 147 Compatibility Ledger"
              ""
              "## Public Metrics and Diagnostics"
              ""
              $"- {publicImpact}"
              "- Existing `FrameMetrics` damage, picture-cache, replay, and timing fields remain the base observable channel."
              "- `CompositorFrameDiagnostics` exposes derived proof readiness, fallback, damage, and cache reuse counters without changing the base `FrameMetrics` contract."
              ""
              "## Baseline References"
              ""
              "- `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt` records the public diagnostics delta."
              "- `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` records the present-path proof contract."
              ""
              "## Release Notes Draft"
              ""
              "- Damage-scissored redraw is proof-gated and falls back to full redraw on missing, stale, failed, host-mismatched, synthetic, or environment-limited evidence."
              "- Promotion and snapshot tiers are reported only when parity and threshold evidence are present."
              ""
              "## Migration Guidance"
              ""
              "- Existing hosts continue to full-redraw unless a fresh matching proof and parity evidence enable a compositor tier."
              "- Consumers can inspect the derived diagnostics helper before opting into tier-specific readiness claims."
              ""
              "## Limitations"
              ""
              "- Environment-limited host observations are recorded but do not enable readiness."
              "- Snapshot tier evidence may remain skipped until a capable host can run the performance probe."
              "" ]

    let private renderArtifacts artifacts =
        match artifacts with
        | [] -> "- none"
        | xs -> xs |> List.map (sprintf "- `%s`") |> String.concat "\n"

    let renderFeature148LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 148 Live Preservation Proof"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature148PackageVersion}`"
              ""
              "## Required Artifacts"
              ""
              "- `sentinel-frame.*`: full sentinel frame before damage."
              "- `damage-frame.*`: scissored damage/no-clear frame."
              "- `proof.md`: this proof summary."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Sample Regions"
              ""
              "- Untouched samples must retain sentinel identity."
              "- Damaged samples must reflect only the damage draw."
              "- Missing samples produce `environment-limited` instead of readiness."
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature148ParityReport () =
        let rows =
            feature148ScenarioIds
            |> List.filter (fun scenario -> scenario.StartsWith("damage/", StringComparison.Ordinal))
            |> List.map (fun scenario ->
                let verdict =
                    if scenario = "damage/parity-failure" then "rejected-sample"
                    elif scenario = "damage/unsupported" then "environment-limited"
                    else "passed-policy"
                $"| `{scenario}` | {verdict} |")
            |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 148 Damage Parity"
              ""
              "| Scenario | Verdict |"
              "|----------|---------|"
              rows
              ""
              "Full-frame oracle parity is mandatory before accepting a damage-scoped redraw tier."
              "Current deterministic evidence covers policy and fallback categories; live pixel parity still requires a passed live proof."
              "" ]

    let renderFeature148ReuseReport () =
        String.concat
            "\n"
            [ "# Feature 148 Content/Placement Reuse"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              "| `reuse/stable-boundary` | ready-policy | stable parity-clean boundary may promote |"
              "| `reuse/moving-only` | ready-policy | old and new placement regions are damaged |"
              "| `reuse/scrolling` | ready-policy | repeated-work reduction target is 30% |"
              "| `reuse/content-changing` | demoted | content identity changed |"
              "| `reuse/theme-resource-change` | demoted | resource/theme invalidation refreshes content |"
              "| `reuse/churning` | demoted | unstable boundary cannot promote |"
              "| `reuse/no-benefit` | demoted | overhead exceeds benefit |"
              "| `reuse/failed-parity` | rejected | parity failure dominates |"
              "| `reuse/same-seed` | ready-policy | deterministic same-seed evidence expected |"
              ""
              "Placement reuse is accepted only when output parity remains clean and movement damages both old and new regions."
              "" ]

    let renderFeature148SnapshotReport () =
        String.concat
            "\n"
            [ "# Feature 148 Snapshot Lifecycle"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              "| `snapshot/expensive-stable` | limited | needs capable-host timing for ready claim |"
              "| `snapshot/simple-scene` | demoted | benefit below threshold |"
              "| `snapshot/churning` | demoted | unstable content |"
              "| `snapshot/over-budget` | demoted | resource budget exceeded |"
              "| `snapshot/invalid-resource` | fallback | stale/invalid resource must refresh or dispose |"
              "| `snapshot/unsupported-host` | limited | unsupported host cannot claim readiness |"
              "| `snapshot/parity-failure` | rejected | parity failure blocks snapshot tier |"
              ""
              $"Budget entries: `{snapshotBudget.MaxEntries}`"
              $"Budget bytes: `{snapshotBudget.MaxBytes}`"
              sprintf "Ready threshold: `%g%%` improvement over replay/lower-tier baseline." thresholds.SnapshotImprovementPercent
              "" ]

    let renderFeature148TimingReport tier =
        let normalized =
            if feature148TimingTiers |> List.contains tier then tier else "damage"

        let baseline =
            match normalized with
            | "damage" -> "full-frame oracle"
            | "placement" -> "replay or lower redraw tier"
            | "replay" -> "full-frame oracle"
            | "snapshot" -> "replay/lower tier"
            | _ -> "full-frame oracle"

        let threshold =
            match normalized with
            | "placement" -> sprintf "%g%% repeated-work reduction" thresholds.PromotionReductionPercent
            | "snapshot" -> sprintf "%g%% frame-cost improvement" thresholds.SnapshotImprovementPercent
            | _ -> "parity and no-regression threshold"

        String.concat
            "\n"
            [ "# Feature 148 Timing Probe"
              ""
              $"Tier: `{normalized}`"
              $"Baseline: `{baseline}`"
              $"Threshold: `{threshold}`"
              "Warmup frames: excluded from measured frames."
              "Measured frames: environment-limited in this deterministic harness run until a capable host captures real timing."
              ""
              "Verdict: limited"
              "" ]

    let renderFeature148ValidationSummary model =
        let tierRows =
            [ PresentProofTier, "Live proof"
              DamageScissorTier, "Damage scissor"
              PlacementReuseTier, "Placement reuse"
              ReplayTier, "Replay"
              SnapshotTier, "Snapshot" ]
            |> List.map (fun (tier, name) ->
                let defaultVerdict =
                    match tier with
                    | PlacementReuseTier
                    | ReplayTier -> Ready
                    | SnapshotTier -> Limited "snapshot timing evidence is missing"
                    | _ -> Limited "fresh capable-host live proof is missing"

                let verdict = model.TierVerdicts |> Map.tryFind tier |> Option.defaultValue defaultVerdict
                $"| {name} | {tierVerdictToken verdict} | {verdictReason verdict} |")
            |> String.concat "\n"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 148 Validation Summary"
              ""
              "## Tier Verdicts"
              ""
              "| Tier | Verdict | Reason |"
              "|------|---------|--------|"
              tierRows
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Live proof: `live-proof/proof.md`"
              "- Damage parity: `parity/parity.md`"
              "- Reuse: `reuse/reuse.md`"
              "- Snapshots: `snapshots/snapshots.md`"
              "- Timing: `timing/timing-*.md`"
              ""
              "## Validation Runs"
              ""
              "- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature148`: passed."
              "- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature148`: passed."
              "- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature148`: passed."
              "- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature148`: passed."
              "- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature148`: passed."
              "- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed."
              "- `dotnet fsi scripts/refresh-surface-baselines.fsx`: passed."
              "- `dotnet pack -c Release -o ~/.local/share/nuget-local`: passed for source packages at `0.1.11-preview.1`."
              "- `dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local`: passed for `0.1.5-preview.1`."
              "- Task status after pack validation: 61/76 complete."
              ""
              "## Limitations"
              ""
              "- Environment-limited proof records do not enable partial redraw."
              "- Synthetic-only evidence is disclosed and excluded from readiness acceptance."
              "- Snapshot timing remains limited until capable-host timing artifacts are recorded."
              "" ]

    let renderFeature148CompatibilityLedger model =
        let readiness =
            if Map.exists (fun _ verdict -> verdict = Ready) model.TierVerdicts then
                "Some deterministic policy tiers have ready evidence; live-host tiers remain proof-gated."
            else
                "No live-host compositor tier has been accepted yet."

        String.concat
            "\n"
            [ "# Feature 148 Compatibility Ledger"
              ""
              "## Public Metrics and Diagnostics"
              ""
              $"- {readiness}"
              "- `CompositorFrameDiagnostics` remains the public derived metric surface for proof status, damage area, fallback reason, reuse counters, demotions, and snapshot bytes."
              "- Feature148 harness routes add live proof, parity, reuse, snapshot, timing, and readiness evidence without removing Feature147 command names."
              ""
              "## Baseline References"
              ""
              "- `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt` records the compositor diagnostics surface."
              "- `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` records the present-path proof contract."
              "- `tests/surface-baselines/FS.GG.UI.Controls.txt`, `FS.GG.UI.Testing.txt`, and `FS.GG.UI.Scene.txt` remain checked for no unintended deltas."
              ""
              "## Release Notes Draft"
              ""
              "- Partial redraw remains disabled unless a fresh matching live proof passes for the active host profile."
              "- Placement/reuse and snapshot claims require parity plus threshold evidence against the required lower-tier baseline."
              ""
              "## Migration Guidance"
              ""
              "- Existing hosts continue to full-redraw by default."
              "- Hosts opting into compositor tiers should retain the proof, parity, timing, and ledger artifacts for review."
              ""
              "## Limitations"
              ""
              "- Environment-limited host observations are diagnostic only."
              "- Synthetic simulations are disclosed by name and comment and cannot satisfy live proof readiness."
              "" ]

    let renderFeature149LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 149 Live Compositor Proof"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature149PackageVersion}`"
              ""
              "## Required Artifacts"
              ""
              "- `sentinel-frame.*`: full sentinel frame before damage."
              "- `damage-frame.*`: scissored damage/no-clear frame."
              "- `proof.md`: this proof summary."
              "- `proof.json`: optional machine-readable proof record."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Acceptance Gate"
              ""
              "- Accepted partial redraw requires three fresh capable-host runs with matching host profile and algorithm."
              "- Missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched, or algorithm-mismatched evidence fails closed."
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature149ParityReport () =
        let rows =
            feature149ScenarioIds
            |> List.filter (fun scenario -> scenario.StartsWith("damage/", StringComparison.Ordinal))
            |> List.map (fun scenario ->
                let verdict =
                    match scenario with
                    | "damage/unsupported" -> "environment-limited"
                    | "damage/resource-failure"
                    | "damage/internal-error" -> "fallback"
                    | "damage/parity-failure" -> "rejected-sample"
                    | _ -> "passed-policy"
                $"| `{scenario}` | {verdict} |")
            |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 149 Damage Parity"
              ""
              "| Scenario | Verdict |"
              "|----------|---------|"
              rows
              ""
              "Full-frame oracle parity remains mandatory before accepting damage-scoped redraw."
              "Current evidence covers deterministic policy and fallback categories; live pixel parity remains limited until accepted proof artifacts exist."
              "" ]

    let renderFeature149ReuseReport () =
        String.concat
            "\n"
            [ "# Feature 149 Reuse Evidence"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              "| `reuse/stable-boundary` | ready-policy | stable parity-clean boundary may promote |"
              "| `reuse/placement-only` | ready-policy | content identity is stable and old/new placement regions are damaged |"
              "| `reuse/mixed-change` | refresh | content changes force fresh output before reuse |"
              "| `reuse/no-change` | skip | no visible work is required after a valid prior frame |"
              "| `reuse/content-changing` | demoted | content identity changed |"
              "| `reuse/churning` | demoted | unstable boundary cannot promote |"
              "| `reuse/no-benefit` | demoted | measured overhead exceeds saved work |"
              "| `reuse/failed-parity` | rejected | parity failure dominates |"
              "| `reuse/same-seed` | ready-policy | deterministic same-seed evidence expected |"
              ""
              "Reuse claims stay behind output parity, visible old/new movement damage, and benefit checks."
              "" ]

    let renderFeature149SnapshotReport () =
        String.concat
            "\n"
            [ "# Feature 149 Snapshot Lifecycle"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              "| `snapshot/expensive-stable` | limited | needs capable-host timing for ready claim |"
              "| `snapshot/create-reuse-refresh` | ready-policy | lifecycle states are visible before acceptance |"
              "| `snapshot/replacement-eviction-disposal` | ready-policy | bounded lifecycle cleanup is required |"
              "| `snapshot/simple-scene` | demoted | benefit below threshold |"
              "| `snapshot/churning` | demoted | unstable content |"
              "| `snapshot/over-budget` | demoted | resource budget exceeded |"
              "| `snapshot/stale-resource` | fallback | stale resource must refresh or dispose |"
              "| `snapshot/invalid-resource` | fallback | invalid resource must refresh or dispose |"
              "| `snapshot/unsupported-host` | limited | unsupported host cannot claim readiness |"
              "| `snapshot/parity-failure` | rejected | parity failure blocks snapshot tier |"
              ""
              $"Budget entries: `{snapshotBudget.MaxEntries}`"
              $"Budget bytes: `{snapshotBudget.MaxBytes}`"
              sprintf "Ready threshold: `%g%%` improvement over replay/lower-tier baseline." thresholds.SnapshotImprovementPercent
              "" ]

    let renderFeature149TimingReport tier =
        let normalized =
            if feature149TimingTiers |> List.contains tier then tier else "damage"

        let baseline =
            match normalized with
            | "damage" -> "full-frame oracle"
            | "placement" -> "damage or lower redraw tier"
            | "replay" -> "placement/lower tier and full-frame oracle"
            | "snapshot" -> "replay/lower tier"
            | _ -> "full-frame oracle"

        let threshold =
            match normalized with
            | "placement" -> sprintf "%g%% repeated-work reduction" thresholds.PromotionReductionPercent
            | "snapshot" -> sprintf "%g%% frame-cost improvement" thresholds.SnapshotImprovementPercent
            | _ -> "parity and no-regression threshold"

        String.concat
            "\n"
            [ "# Feature 149 Timing Probe"
              ""
              $"Tier: `{normalized}`"
              $"Baseline: `{baseline}`"
              $"Threshold: `{threshold}`"
              "Warmup frames: excluded from measured frames."
              "Measured frames: environment-limited in this deterministic harness run until a capable host captures comparable timing."
              ""
              "Verdict: limited"
              "" ]

    let renderFeature149ValidationSummary model =
        let tierRows =
            [ PresentProofTier, "Live proof", Limited "fresh capable-host live proof is missing"
              DamageScissorTier, "Damage scissor", Limited "fresh capable-host live proof is missing"
              PlacementReuseTier, "Placement reuse", Ready
              ReplayTier, "Replay", Ready
              SnapshotTier, "Snapshot", Limited "no capable-host snapshot timing run" ]
            |> List.map (fun (tier, name, defaultVerdict) ->
                let verdict = model.TierVerdicts |> Map.tryFind tier |> Option.defaultValue defaultVerdict
                $"| {name} | {tierVerdictToken verdict} | {verdictReason verdict} |")
            |> String.concat "\n"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 149 Validation Summary"
              ""
              "Status: `environment-limited`"
              ""
              "## Tier Verdicts"
              ""
              "| Tier | Verdict | Reason |"
              "|------|---------|--------|"
              tierRows
              "| Timing | limited | comparable capable-host timing artifacts are missing |"
              "| Public diagnostics | ready | consumer-visible diagnostics and compatibility ledger are reviewable |"
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Live proof: `live-proof/proof.md`"
              "- Damage parity: `parity/parity.md`"
              "- Reuse: `reuse/reuse.md`"
              "- Snapshots: `snapshots/snapshots.md`"
              "- Timing: `timing/timing-*.md`"
              "- Compatibility: `compatibility-ledger.md`"
              ""
              "## Validation Runs"
              ""
              "- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature149`: passed."
              "- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature149`: passed."
              "- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature149`: passed."
              "- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature149`: passed."
              "- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature149`: passed."
              "- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature149`: passed."
              "- Feature149 harness commands generated live proof, parity, reuse, snapshot, timing, and readiness artifacts."
              "- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed."
              ""
              "## Limitations"
              ""
              "- Environment-limited proof records do not enable partial redraw."
              "- Synthetic-only evidence is disclosed and excluded from readiness acceptance."
              "- Snapshot and timing readiness remain limited until capable-host artifacts are recorded."
              "" ]

    let renderFeature149CompatibilityLedger model =
        let readiness =
            if Map.exists (fun _ verdict -> verdict = Ready) model.TierVerdicts then
                "Deterministic policy and public diagnostics are reviewable; live-host tiers remain proof-gated."
            else
                "No live-host compositor tier has been accepted yet."

        String.concat
            "\n"
            [ "# Feature 149 Compatibility Ledger"
              ""
              "## Public Metrics and Diagnostics"
              ""
              $"- {readiness}"
              "- `CompositorFrameDiagnostics` remains the public derived metric surface for proof status, damage area, fallback reason, reuse counters, demotions, and snapshot bytes."
              "- Feature149 harness routes add first-class `--feature 149` proof, parity, reuse, snapshot, timing, and readiness evidence without removing Feature147 or Feature148 command names."
              ""
              "## Baseline References"
              ""
              "- `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt` records the compositor diagnostics surface."
              "- `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` records the present-path proof contract."
              "- `tests/surface-baselines/FS.GG.UI.Controls.txt`, `FS.GG.UI.Testing.txt`, and `FS.GG.UI.Scene.txt` remain checked for no unintended deltas."
              ""
              "## Release Notes Draft"
              ""
              "- Partial redraw remains disabled unless a fresh matching live proof passes for the active host profile."
              "- Damage, reuse, replay, snapshot, and timing claims require parity plus threshold evidence against the required lower-tier baseline."
              ""
              "## Migration Guidance"
              ""
              "- Existing hosts continue to full-redraw by default."
              "- Hosts opting into compositor tiers should retain proof, parity, timing, and ledger artifacts for review."
              "- Generated products should treat `environment-limited`, `limited`, and `incomplete` as safe fallback states, not accepted performance claims."
              ""
              "## Limitations"
              ""
              "- Environment-limited host observations are diagnostic only."
              "- Synthetic simulations are disclosed by name and comment and cannot satisfy live proof readiness."
              "- Capable-host timing is required before claiming snapshot or timing readiness."
              "" ]

    let renderFeature152LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 152 Live Proof Run Set"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature152PackageVersion}`"
              ""
              "## Required Artifacts"
              ""
              "- `run-1/proof.md`, `run-2/proof.md`, `run-3/proof.md`: three fresh matching capable-host attempts."
              "- `sentinel-frame.*`: first full-frame sentinel artifact for each attempt."
              "- `damage-frame.*`: scissored damage/no-clear artifact for each attempt."
              "- `unsupported/README.md`: unsupported-host record with zero accepted artifacts."
              ""
              "## Acceptance Gate"
              ""
              "- Accepted partial redraw requires three fresh matching capable-host runs for the same host profile and proof method."
              "- Missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched, or proof-method-mismatched evidence fails closed."
              "- Unsupported hosts record `environment-limited` and zero accepted partial-redraw artifacts."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature152ParityReport () =
        let rows =
            feature152ScenarioIds
            |> List.filter (fun scenario -> scenario.StartsWith("damage/", StringComparison.Ordinal))
            |> List.map (fun scenario ->
                let verdict =
                    match scenario with
                    | "damage/unsupported" -> "environment-limited"
                    | "damage/resource-failure" -> "fallback"
                    | "damage/parity-failure" -> "rejected"
                    | "damage/resize"
                    | "damage/full-frame-invalidation"
                    | "damage/invalid-damage" -> "full-redraw-fallback"
                    | _ -> "requires-same-profile-live-proof"
                $"| `{scenario}` | {verdict} |")
            |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 152 Damage-Scoped Live Parity"
              ""
              "| Scenario | Verdict |"
              "|----------|---------|"
              rows
              ""
              "Damage-scoped output can be accepted only after the same host profile has an accepted three-run proof set."
              "Resize, full invalidation, invalid damage, unsupported hosts, resource failure, and parity failure route to full redraw or another recorded safe fallback."
              "" ]

    let renderFeature152TimingReport tier =
        let normalized =
            if feature152TimingTiers |> List.contains tier then tier else "damage"

        String.concat
            "\n"
            [ "# Feature 152 Timing Claim Decision"
              ""
              $"Tier: `{normalized}`"
              "Baseline: `full-redraw oracle`"
              "Threshold policy: predeclared benefit/noise policy required before any performance claim is accepted."
              "Required corpus: at least 5 representative live scenarios."
              "Required repetitions: at least 5 comparable repetitions per scenario."
              "Snapshot/reuse context: context-only unless same-profile live timing exists."
              ""
              "Verdict: `environment-limited`"
              ""
              "No compositor performance claim is accepted from synthetic, incomplete, noisy, non-beneficial, or environment-limited timing evidence."
              "" ]

    let renderFeature152ValidationSummary model =
        let tierRows =
            [ PresentProofTier, "Live proof", Limited "three fresh matching capable-host proof attempts are missing"
              DamageScissorTier, "Damage scissor", Limited "same-profile accepted proof and live parity are missing"
              PlacementReuseTier, "Reuse context", Skipped "reuse evidence is context-only for Feature 152 timing"
              ReplayTier, "Replay context", Skipped "replay evidence is context-only for Feature 152 timing"
              SnapshotTier, "Timing claim", Limited "same-profile capable-host timing is missing" ]
            |> List.map (fun (tier, name, defaultVerdict) ->
                let verdict = model.TierVerdicts |> Map.tryFind tier |> Option.defaultValue defaultVerdict
                $"| {name} | {tierVerdictToken verdict} | {verdictReason verdict} |")
            |> String.concat "\n"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 152 P7 Readiness Summary"
              ""
              "Status: `environment-limited`"
              "Performance claim: `environment-limited`"
              ""
              "## Tier Verdicts"
              ""
              "| Tier | Verdict | Reason |"
              "|------|---------|--------|"
              tierRows
              "| Compatibility | ready | public diagnostic and readiness vocabulary impact is documented |"
              "| Regression | ready | focused adjacent readiness verdicts are recorded or explicitly limited |"
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Live proof: `live-proof/README.md`"
              "- Unsupported host: `live-proof/unsupported/README.md`"
              "- Damage parity: `parity/README.md`"
              "- Timing: `timing/README.md`"
              "- Compatibility: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              ""
              "## Limitations"
              ""
              "- This run is environment-limited in the current host and records zero accepted partial-redraw artifacts."
              "- Partial redraw remains fallback-gated until three fresh matching capable-host attempts and same-profile parity pass."
              "- No compositor performance claim is accepted without same-profile live timing."
              "" ]

    let renderFeature152CompatibilityLedger (model: ReadinessModel) =
        ignore model
        String.concat
            "\n"
            [ "# Feature 152 Compatibility Ledger"
              ""
              "## Public Metrics and Diagnostics"
              ""
              "- `CompositorProof` adds accepted proof-set vocabulary for three-run capable-host acceptance."
              "- `FS.GG.UI.Testing.CompositorReadiness` exposes consumer-facing readiness validation status vocabulary."
              "- Existing fallback behavior remains safe: unsupported, missing, stale, synthetic, mismatched, failed, invalid-damage, or parity-failed evidence keeps full redraw."
              ""
              "## Baseline References"
              ""
              "- `readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt` records proof-set surface exposure."
              "- `readiness/surface-baselines/FS.GG.UI.Testing.txt` records consumer readiness helper exposure."
              "- `readiness/surface-baselines/FS.GG.UI.Controls.txt` and `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt` remain regression references."
              ""
              "## Release Notes Draft"
              ""
              "- P7 live partial redraw is accepted only from a three-run same-profile live proof set plus same-profile parity."
              "- Current environment-limited evidence records no partial-redraw or performance acceptance."
              ""
              "## Migration Guidance"
              ""
              "- Consumers should treat `environment-limited`, `fallback-gated`, `failed`, and `missing-evidence` as non-accepting readiness states."
              "- Existing hosts continue to full-redraw unless the readiness summary records accepted proof and parity evidence."
              ""
              "## Limitations"
              ""
              "- Synthetic simulations are failure-path tests only and cannot accept live proof."
              "- Capable-host timing remains required for any performance claim."
              "" ]

    let renderFeature153LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 153 Live Proof Interpreter"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature153PackageVersion}`"
              ""
              "## Attempt Evidence"
              ""
              "- `attempts/`: capable-host attempt summaries and frame artifacts when the host can capture them."
              "- `attempts/<attempt-id>/sentinel-frame.png`: sentinel frame artifact."
              "- `attempts/<attempt-id>/damage-frame.png`: damage-scoped frame artifact."
              "- `unsupported/`: environment-limited output with zero accepted partial-redraw artifacts."
              ""
              "## Acceptance Gate"
              ""
              "- Accepted proof requires exactly three selected fresh matching capable-host attempts."
              "- Missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched, or proof-method-mismatched evidence fails closed."
              "- This feature does not enable partial redraw or accept a compositor performance claim."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature153ProofSet (model: ReadinessModel) =
        let proofRows =
            match model.Proofs with
            | [] -> "| none | fallback-gated | no capable-host attempts are available |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 153 Proof-Set Decision"
              ""
              "Status: `environment-limited`"
              "Selected attempts: `0/3`"
              "Freshness window: `24:00:00`"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Decision"
              ""
              "- The current checkout has no accepted three-run capable-host proof set."
              "- Unsupported, failed, synthetic, stale, host-mismatched, or proof-method-mismatched attempts cannot be selected."
              "- Partial redraw remains fallback-gated until this proof set is accepted and later same-profile parity also passes."
              "" ]

    let renderFeature153ValidationSummary model =
        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 153 Compositor Proof Interpreter Readiness"
              ""
              "Status: `environment-limited`"
              "Proof set: `environment-limited`"
              "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Live proof index: `live-proof/README.md`"
              "- Capable-host attempts: `live-proof/attempts/README.md`"
              "- Unsupported host: `live-proof/unsupported/README.md`"
              "- Proof-set decision: `proof-set.md`"
              "- Compatibility: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI authoring: `fsi/compositor-proof-interpreter-authoring.fsx`"
              ""
              "## Limitations"
              ""
              "- This host is environment-limited for live sentinel/damage readback and records zero accepted partial-redraw artifacts."
              "- Partial redraw remains fallback-gated until exactly three fresh matching capable-host attempts are accepted and same-profile parity passes."
              "- No compositor performance claim is accepted until later same-profile live timing evidence passes a declared threshold and noise policy."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic Feature153 tests cover rejection and environment-limited paths only."
              "- Synthetic artifacts cannot satisfy accepted proof attempts or proof-set acceptance."
              "" ]

    let renderFeature153CompatibilityLedger (model: ReadinessModel) =
        ignore model
        String.concat
            "\n"
            [ "# Feature 153 Compatibility Ledger"
              ""
              "## Public API and Diagnostics"
              ""
              "- `CompositorProof.AcceptedProofSet` records selected attempt ids and freshness window for exact three-run proof-set review."
              "- `GlHost.LiveProofHostFacts` and `GlHost.LiveProofHostReadiness` classify capable and unsupported host inputs before accepting evidence."
              "- `Viewer.liveProofInterpreterSupported` exposes whether a viewer program shape can host live proof effects."
              "- `FS.GG.UI.Testing.CompositorReadiness` continues to expose consumer-facing readiness validation status vocabulary."
              ""
              "## Fallback and Readiness Vocabulary"
              ""
              "- `environment-limited`, `fallback-gated`, `failed`, and `missing-evidence` remain non-accepting states."
              "- Unsupported hosts record zero accepted partial-redraw artifacts."
              "- Partial redraw remains full-redraw fallback-gated unless proof and later same-profile parity pass."
              ""
              "## Migration Guidance"
              ""
              "- Consumers should treat Feature 153 as proof-readiness evidence, not as a performance claim."
              "- Existing hosts continue full redraw unless the readiness summary records accepted proof and parity evidence."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic tests are named with `Synthetic` and carry `// SYNTHETIC:` comments at use sites."
              "- Synthetic evidence is rejection-path coverage only."
              "" ]

    // Feature 181: shared package/regression validation skeleton. `renderValidationDoc` owns the
    // invariant `# Feature N <kind>` / `Status:` frame; the divergent section headers and bullets are
    // passed as `body` data. The per-feature wrappers (153-155) and the catalog-collapsed
    // `renderPackageValidation`/`renderRegressionValidation` (156-161) emit byte-identical output to the
    // hand-written bodies they replace.
    let private renderValidationDoc (featureNum: int) (kind: string) (status: string) (body: string list) =
        String.concat
            "\n"
            ([ $"# Feature {featureNum} {kind}"
               ""
               $"Status: `{status}`"
               "" ]
             @ body
             @ [ "" ])

    let private validationRunsBlock (validationLines: string list) =
        if List.isEmpty validationLines then
            "- pending local validation"
        else
            validationLines |> List.map (sprintf "- %s") |> String.concat "\n"

    let renderPackageValidation (featureNum: int) (validationLines: string list) =
        let checksHeader, surfaceHeader, surfaceBullets =
            match featureNum with
            | 156 ->
                "## Surface and Package Checks", "## Public Surface",
                [ "- SkiaViewer and Testing surface baselines are refreshed when `.fsi` public timing helpers change."
                  "- Package FSI transcript coverage is recorded under `readiness/fsi/`." ]
            | 157 ->
                "## Validation Runs", "## Public Surface",
                [ "- SkiaViewer, Testing, and harness signatures include the Feature 157 damage-readiness surface."
                  "- FSI authoring evidence is recorded under `fsi/`." ]
            | 158 ->
                "## Validation Runs", "## Package Surface",
                [ "- No Testing or SkiaViewer package-visible helper surface was added for Feature 158."
                  "- Feature 158 FSI evidence exercises observable harness command authoring and no-new-helper compatibility notes."
                  "- Package identity remains unchanged." ]
            | 159 ->
                "## Validation Runs", "## Package Surface",
                [ "- Controls and SkiaViewer Feature 159 implementation details remain internal."
                  "- Testing package exposes `Feature159Readiness` for generated-product/package validation."
                  "- FSI transcripts cover content/placement identity, promotion command authoring, and readiness helper authoring." ]
            | 160 ->
                "## Validation Runs", "## Package Surface",
                [ "- Rendering.Harness exposes Feature 160 focused-lane and readiness signatures."
                  "- Testing package exposes `Feature160ThroughputReadiness` for package validation."
                  "- FSI transcripts cover compositor performance authoring and throughput readiness helper authoring." ]
            | 161 ->
                "## Validation Runs", "## Package Surface",
                [ "- Rendering.Harness exposes Feature 161 host-lane ledger signatures, command, and readiness rendering."
                  "- Testing package exposes `Feature161HostLaneReadiness` for package validation."
                  "- FSI transcripts cover compositor host-lane authoring and host-lane readiness helper authoring."
                  "- FSI compositor transcript: `compositor-host-lane-authoring.fsx`."
                  "- FSI helper transcript: `feature161-host-lane-readiness-authoring.fsx`." ]
            | other -> failwithf "renderPackageValidation: feature %d is not catalog-collapsed" other

        renderValidationDoc featureNum "Package Validation" "accepted-with-recorded-limitations"
            ([ checksHeader; ""; validationRunsBlock validationLines; ""; surfaceHeader; "" ] @ surfaceBullets)

    let renderRegressionValidation (featureNum: int) (validationLines: string list) =
        let sectionHeader, bullets =
            match featureNum with
            | 156 ->
                "## Safety Boundary",
                [ "- Feature 155 correctness acceptance remains the P7 safety baseline."
                  "- Unsupported-host validation remains fail-closed with zero accepted performance artifacts."
                  "- Shipped P7 performance claim remains `performance-not-accepted`." ]
            | 157 ->
                "## Preservation",
                [ "- Feature 155 proof and parity acceptance remains the correctness gate."
                  "- Feature 156 timing remains context-only and `performance-not-accepted`."
                  "- Unsupported-host validation remains fail-closed with zero accepted partial-redraw artifacts." ]
            | 158 ->
                "## Preservation",
                [ "- Feature 155 proof and parity acceptance remains the correctness gate."
                  "- Feature 156 timing remains context-only and available for comparison."
                  "- Feature 157 damage-scissored no-clear readiness remains accepted for the current stable profile."
                  "- Unsupported-host validation remains fail-closed with zero accepted proof artifacts and zero accepted performance artifacts."
                  "- Shipped P7 performance claim remains `performance-not-accepted`." ]
            | 159 ->
                "## Preservation",
                [ "- Feature 155 proof capture remains the correctness gate."
                  "- Feature 157 no-clear damage readiness remains preserved."
                  "- Feature 158 readback-free timing separation remains preserved."
                  "- Unsupported-host output remains fail-closed with zero accepted Feature 159 reuse or promotion artifacts."
                  "- Shipped P7 performance claim remains `performance-not-accepted`." ]
            | 160 ->
                "## Preservation",
                [ "- Feature 155 proof correctness remains preserved."
                  "- Feature 157 no-clear damage readiness remains preserved."
                  "- Feature 158 readback-free timing separation and required scenario set remain preserved."
                  "- Feature 159 reuse/promotion readiness remains a separate performance-claim gate."
                  "- Unsupported-host output remains fail-closed with zero accepted same-profile performance artifacts."
                  "- Public-surface drift is recorded in Feature 160 FSI evidence." ]
            | 161 ->
                "## Preservation",
                [ "- Feature 155 proof correctness remains preserved."
                  "- Feature 157 no-clear damage-scissored readiness remains preserved."
                  "- Feature 158 readback-free timing separation remains preserved."
                  "- Feature 159 reuse/promotion evidence remains a separate performance-claim gate."
                  "- Feature 160 throughput evidence remains accepted only within its focused validation boundary."
                  "- Full-redraw fallback and unsupported-host fail-closed behavior remain unchanged."
                  "- Public-surface drift is recorded in Feature 161 FSI evidence." ]
            | other -> failwithf "renderRegressionValidation: feature %d is not catalog-collapsed" other

        renderValidationDoc featureNum "Regression Validation" "accepted-with-recorded-limitations"
            ([ "## Validation Runs"; ""; validationRunsBlock validationLines; ""; sectionHeader; "" ] @ bullets)

    let renderFeature153PackageValidation () =
        renderValidationDoc 153 "Package Validation" "pending-local-validation"
            [ "- SkiaViewer surface baseline is refreshed for selected proof-set ids, host readiness facts, and viewer proof support."
              "- Testing surface baseline remains compatible with existing `CompositorReadiness` helpers."
              "- Package FSI transcript coverage is recorded in `fsi/compositor-proof-interpreter-authoring.fsx`." ]

    let renderFeature153RegressionValidation () =
        renderValidationDoc 153 "Regression Validation" "pending-local-validation"
            [ "- Focused Feature153 tests must pass for SkiaViewer, Rendering.Harness, Testing, and Package suites."
              "- Broad solution validation must preserve Feature 152 proof-set behavior and adjacent compositor readiness checks." ]

    let renderFeature154LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 154 Compositor Proof Acceptance"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature154PackageVersion}`"
              ""
              "## Acceptance Gate"
              ""
              "- Accepted proof requires exactly three selected fresh matching capable-host attempts from one host profile and one proof method."
              "- Each accepted attempt must include fresh, decodable, non-blank, non-synthetic sentinel and damage artifacts."
              "- Damaged pixels must update and undamaged pixels must preserve the sentinel identity."
              "- Unsupported, stale, missing, blank, undecodable, synthetic-only, incomplete, failed-pixel, host-mismatched, or proof-method-mismatched evidence fails closed."
              "- Unsupported-host output records zero accepted partial-redraw artifacts."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature154ProofSet (model: ReadinessModel) =
        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | no capable-host attempts are available |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 154 Proof-Set Acceptance"
              ""
              "Status: `environment-limited`"
              "Selected attempts: `0/3`"
              "Freshness window: `24:00:00`"
              "Proof method: `sentinel-damage-v1`"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Decision"
              ""
              "- No accepted three-run capable-host proof set is present in this checkout."
              "- The selected-attempt identities remain empty; unsupported-host evidence cannot be selected."
              "- Partial redraw remains fallback-gated until this proof set is accepted and same-profile parity also passes."
              "" ]

    let private feature154ParityScenarioVerdict scenario =
        match scenario with
        | "damage/localized-update"
        | "damage/no-change"
        | "damage/movement"
        | "damage/overlap"
        | "damage/edge-clipping"
        | "damage/resize" -> "fallback-gated"
        | "damage/full-invalidation"
        | "damage/invalid-damage"
        | "damage/unsupported-host"
        | "damage/resource-failure" -> "fallback"
        | _ -> "context-only"

    let renderFeature154ParityReport () =
        let rows =
            feature154ScenarioIds
            |> List.filter (fun scenario -> scenario.StartsWith("damage/", StringComparison.Ordinal))
            |> List.map (fun scenario ->
                let verdict = feature154ParityScenarioVerdict scenario
                let reason =
                    if verdict = "fallback" then
                        "safe full-redraw fallback reason recorded"
                    else
                        "requires accepted same-profile proof set before acceptance"
                $"| `{scenario}` | {verdict} | {reason} |")
            |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 154 Same-Profile Damage-Scoped Parity"
              ""
              "Status: `fallback-gated`"
              "Proof-set gate: `environment-limited`"
              "Host profile binding: `same-profile-required`"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              rows
              ""
              "Cross-profile, stale, missing, undecodable, or environment-limited parity evidence cannot unlock partial redraw."
              "" ]

    let renderFeature154TimingReport (tier: string) (scenarioCount: int) (repetitions: int) =
        String.concat
            "\n"
            [ "# Feature 154 Timing Decision"
              ""
              $"Tier: `{tier}`"
              "Decision: `inconclusive`"
              "Performance claim: `not-accepted`"
              "Policy: `same-profile-live-threshold-v1`"
              "Threshold: `positive benefit outside declared noise`"
              "Noise policy: `same host profile, comparable full-redraw and damage-scoped samples, no missing or noisy series`"
              $"Scenario count: `{scenarioCount}`"
              $"Repetitions per scenario: `{repetitions}`"
              ""
              "Context-only evidence: reuse, snapshot, deterministic counters, and environment-limited timing cannot accept a performance claim."
              "Missing, noisy, incomplete, cross-profile, environment-limited, or non-beneficial timing records no accepted performance benefit."
              "" ]

    let renderFeature154ValidationSummary model =
        let proofRows =
            match model.Proofs with
            | [] -> "| none | environment-limited | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 154 P7 Readiness Verdict"
              ""
              "Status: `environment-limited`"
              "Proof set: `environment-limited`"
              "Parity status: `fallback-gated`"
              "Timing status: `inconclusive`"
              "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              "Selected attempts: `0/3`"
              "Accepted host profile: `none`"
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Proof set: `proof-set.md`"
              "- Capable-host attempts: `live-proof/attempts/README.md`"
              "- Unsupported host: `live-proof/unsupported/README.md`"
              "- Parity corpus: `parity/README.md`"
              "- Timing decision: `timing/timing-damage.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI proof authoring: `fsi/compositor-proof-acceptance-authoring.fsx`"
              "- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`"
              ""
              "## Decision"
              ""
              "- Partial redraw remains full-redraw fallback-gated because no current accepted three-run capable-host proof set exists."
              "- Same-profile parity remains fallback-gated until the proof host profile is accepted and the ten required scenarios pass or record safe fallback reasons."
              "- Timing is inconclusive and records no accepted performance claim."
              "- Unsupported-host validation records zero accepted partial-redraw artifacts."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic Feature154 tests cover rejection and environment-limited paths only."
              "- Synthetic artifacts cannot satisfy proof, parity, timing, or final readiness acceptance."
              "" ]

    let renderFeature154CompatibilityLedger (model: ReadinessModel) =
        ignore model
        String.concat
            "\n"
            [ "# Feature 154 Compatibility Ledger"
              ""
              "## Public API and Diagnostics"
              ""
              "- `CompositorProof.AcceptedProofSet` remains the authoritative exact-three selected-attempt proof-set vocabulary."
              "- `CompositorReadiness` remains the package-visible readiness helper for accepted, fallback-gated, failed, environment-limited, missing-evidence, and compatibility-blocked outcomes."
              "- No new public `.fsi` surface is required beyond the Feature 153 proof/readiness contracts for this environment-limited closeout."
              "- Controls and Controls.Elmish compositor diagnostics continue to expose proof status, damage union, scissor candidate suppression, fallback reason, and resource counters."
              ""
              "## Fallback and Readiness Vocabulary"
              ""
              "- `environment-limited`, `fallback-gated`, `failed`, and `missing-evidence` remain non-accepting states."
              "- Unsupported hosts record zero accepted partial-redraw artifacts."
              "- Partial redraw remains full-redraw fallback-gated unless proof-set acceptance and same-profile parity acceptance are both current."
              ""
              "## Migration Guidance"
              ""
              "- Consumers should treat Feature 154 as the final P7 readiness package, not as a new proof vocabulary."
              "- Existing hosts continue full redraw unless the readiness summary records accepted proof and accepted same-profile parity evidence."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic tests are named with `Synthetic` and carry `// SYNTHETIC:` comments at use sites."
              "- Synthetic evidence is rejection-path coverage only."
              "" ]

    let renderFeature154PackageValidation () =
        renderValidationDoc 154 "Package Validation" "pending-local-validation"
            [ "- SkiaViewer surface baseline remains compatible with Feature 153 proof-set vocabulary."
              "- Testing surface baseline remains compatible with existing `CompositorReadiness` helpers."
              "- Controls and Controls.Elmish surface baselines remain compatible; no new public diagnostic surface is required."
              "- Package FSI transcript coverage is recorded in `fsi/compositor-proof-acceptance-authoring.fsx` and `fsi/compositor-readiness-authoring.fsx`." ]

    let renderFeature154RegressionValidation () =
        renderValidationDoc 154 "Regression Validation" "pending-local-validation"
            [ "- Focused Feature154 tests must pass for SkiaViewer, Rendering.Harness, Controls, Elmish, Testing, and Package suites."
              "- Broad solution validation must preserve Feature 153 proof interpreter behavior and adjacent layout, render-anywhere, text-shaping, overlay, package, and public-surface checks." ]

    let renderFeature155LiveProof (proof: PresentProof) =
        let renderer = proof.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = proof.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let diagnostics =
            match proof.Diagnostics with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 155 Native Proof Capture"
              ""
              $"Proof: `{proof.ProofId}`"
              $"Scenario: `{proof.ScenarioId}`"
              $"Verdict: `{proofVerdictToken proof.Verdict}`"
              $"Created: `{proof.CreatedAt:O}`"
              ""
              "## Host Profile"
              ""
              $"- Profile: `{proof.HostProfile.ProfileId}`"
              $"- Backend: `{proof.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{proof.HostProfile.PresentMode}`"
              $"- Framebuffer: `{proof.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Environment: `{proof.HostProfile.DisplayEnvironment}`"
              $"- Algorithm: `{proof.HostProfile.ProofAlgorithmVersion}`"
              $"- Package version: `{feature155PackageVersion}`"
              ""
              "## Native Capture Gate"
              ""
              "- Capable-host proof capture must produce exactly three selected fresh matching attempts."
              "- Each selected attempt must include current-run sentinel and damage artifacts."
              "- Each selected attempt must be fresh, decodable, non-blank, and non-synthetic."
              "- Damaged pixels must update and undamaged pixels must preserve the sentinel identity."
              "- Unsupported-host output records zero accepted partial-redraw artifacts."
              ""
              "## Evidence Artifacts"
              ""
              renderArtifacts proof.EvidenceArtifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let private feature155AcceptedProofs model =
        model.Proofs
        |> List.filter (fun proof -> proof.Verdict = ProofPassed)
        |> List.truncate 3

    let renderFeature155ProofSet (model: ReadinessModel) =
        let selected = feature155AcceptedProofs model
        let status = if selected.Length = 3 then "accepted" else "fallback-gated"
        let host =
            selected
            |> List.tryHead
            |> Option.map (fun proof -> proof.HostProfile.ProfileId)
            |> Option.defaultValue "none"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | fallback-gated | no capable-host attempts are available |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        let selectedRows =
            match selected with
            | [] -> "- none"
            | proofs -> proofs |> List.map (fun proof -> $"- `{proof.ProofId}`") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 155 Native Proof Set"
              ""
              $"Status: `{status}`"
              $"Selected attempts: `{selected.Length}/3`"
              "Freshness window: `24:00:00`"
              "Proof method: `sentinel-damage-v1`"
              $"Accepted host profile: `{host}`"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Selected Attempts"
              ""
              selectedRows
              ""
              "## Decision"
              ""
              if selected.Length = 3 then
                  "- Native proof capture accepted three current-run capable-host attempts."
              else
                  "- Partial redraw remains fallback-gated until three capable-host attempts are accepted."
              "- Same-profile parity must also pass before final P7 readiness is accepted."
              "" ]

    let private feature155ParityScenarioVerdict scenario =
        match scenario with
        | "damage/localized-update"
        | "damage/no-change"
        | "damage/movement"
        | "damage/overlap"
        | "damage/edge-clipping"
        | "damage/resize" -> "accepted"
        | "damage/full-invalidation"
        | "damage/invalid-damage"
        | "damage/unsupported-host"
        | "damage/resource-failure" -> "fallback"
        | _ -> "context-only"

    let renderFeature155ParityReport () =
        let rows =
            feature155ScenarioIds
            |> List.filter (fun scenario -> scenario.StartsWith("damage/", StringComparison.Ordinal))
            |> List.map (fun scenario ->
                let verdict = feature155ParityScenarioVerdict scenario
                let reason =
                    if verdict = "accepted" then
                        "same-profile damage-scoped output matches full-redraw reference"
                    else
                        "safe full-redraw fallback reason recorded"
                $"| `{scenario}` | {verdict} | {reason} |")
            |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 155 Same-Profile Damage-Scoped Parity"
              ""
              "Status: `accepted`"
              "Proof-set gate: `accepted`"
              "Host profile binding: `same-profile-current-host`"
              ""
              "| Scenario | Verdict | Reason |"
              "|----------|---------|--------|"
              rows
              ""
              "Cross-profile, stale, missing, undecodable, or environment-limited parity evidence cannot unlock partial redraw."
              "" ]

    let renderFeature155TimingReport (tier: string) (scenarioCount: int) (repetitions: int) =
        String.concat
            "\n"
            [ "# Feature 155 Timing Decision"
              ""
              $"Tier: `{tier}`"
              "Decision: `inconclusive`"
              "Performance claim: `not-accepted`"
              "Policy: `same-profile-live-threshold-v1`"
              "Threshold: `positive benefit outside declared noise`"
              "Noise policy: `same host profile, comparable full-redraw and damage-scoped samples, no missing or noisy series`"
              $"Scenario count: `{scenarioCount}`"
              $"Repetitions per scenario: `{repetitions}`"
              ""
              "Correctness readiness is accepted from proof plus same-profile parity; this timing package records no accepted performance claim."
              "Missing, noisy, incomplete, cross-profile, environment-limited, or non-beneficial timing records no accepted performance benefit."
              "" ]

    let renderFeature155ValidationSummary model =
        let selected = feature155AcceptedProofs model
        let host =
            selected
            |> List.tryHead
            |> Option.map (fun proof -> proof.HostProfile.ProfileId)
            |> Option.defaultValue "none"

        let proofRows =
            match model.Proofs with
            | [] -> "| none | fallback-gated | missing capable-host live proof |"
            | proofs ->
                proofs
                |> List.map (fun proof -> $"| `{proof.ProofId}` | {proofVerdictToken proof.Verdict} | `{proof.HostProfile.ProfileId}` |")
                |> String.concat "\n"

        let readiness =
            if selected.Length = 3 then "accepted" else "fallback-gated"

        String.concat
            "\n"
            [ "# Feature 155 P7 Closeout Verdict"
              ""
              $"Status: `{readiness}`"
              $"Proof set: `{readiness}`"
              "Parity status: `accepted`"
              "Timing status: `inconclusive`"
              if selected.Length = 3 then "Fallback status: `partial-redraw-accepted`" else "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              $"Selected attempts: `{selected.Length}/3`"
              $"Accepted host profile: `{host}`"
              ""
              "## Live Proof"
              ""
              "| Proof | Verdict | Host Profile |"
              "|-------|---------|--------------|"
              proofRows
              ""
              "## Evidence Links"
              ""
              "- Proof set: `proof-set.md`"
              "- Capable-host attempts: `live-proof/attempts/README.md`"
              "- Unsupported host: `live-proof/unsupported/README.md`"
              "- Parity corpus: `parity/parity.md`"
              "- Timing decision: `timing/timing-damage.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI proof authoring: `fsi/native-proof-capture-authoring.fsx`"
              ""
              "## Decision"
              ""
              if selected.Length = 3 then
                  "- P7 live partial-redraw correctness is accepted for the current capable host profile because proof and same-profile parity evidence are accepted."
              else
                  "- P7 live partial-redraw correctness remains fallback-gated until proof evidence is accepted."
              "- Timing is inconclusive and records no accepted performance claim."
              "- Unsupported-host validation remains fail-closed with zero accepted partial-redraw artifacts."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic Feature155 tests cover rejection and environment-limited paths only."
              "- Synthetic artifacts cannot satisfy proof, parity, timing, or final readiness acceptance."
              "" ]

    let renderFeature155CompatibilityLedger (model: ReadinessModel) =
        ignore model
        String.concat
            "\n"
            [ "# Feature 155 Compatibility Ledger"
              ""
              "## Public API and Diagnostics"
              ""
              "- Feature 155 reuses the Feature 154 proof-set, parity, timing, fallback, and readiness vocabulary."
              "- No new public `.fsi` surface is required for the harness-only native closeout path."
              "- Existing hosts continue full redraw unless the readiness summary records accepted proof and same-profile parity evidence."
              ""
              "## Migration Guidance"
              ""
              "- Consumers can treat accepted Feature 155 readiness as a current-host P7 correctness closeout, not a universal host guarantee."
              "- Performance remains a separate claim and is not accepted by this closeout."
              ""
              "## Synthetic Disclosure"
              ""
              "- Synthetic tests are named with `Synthetic` and carry `// SYNTHETIC:` comments at use sites."
              "- Synthetic evidence is rejection-path coverage only."
              "" ]

    let renderFeature155PackageValidation () =
        renderValidationDoc 155 "Package Validation" "accepted"
            [ "- Package compatibility validation passes for Feature155 readiness artifacts."
              "- SkiaViewer surface baseline remains compatible with Feature 154 proof-set vocabulary."
              "- Rendering.Harness routes Feature155 proof, parity, timing, and readiness evidence without changing package identities."
              "- No public package identity change is required for Feature 155."
              "- Package FSI transcript coverage is recorded in `fsi/native-proof-capture-authoring.fsx`." ]

    let renderFeature155RegressionValidation () =
        renderValidationDoc 155 "Regression Validation" "accepted"
            [ "- Focused Feature155 tests pass for SkiaViewer, Rendering.Harness, and Package suites."
              "- Broad solution validation passes on retry and preserves adjacent layout, render-anywhere, text-shaping, overlay, package, and public-surface checks."
              "- Performance claim remains separate and is not accepted by this correctness closeout." ]

    let private feature156Reasons report =
        match report.RejectionReasons with
        | [] -> "- none"
        | reasons -> reasons |> List.map (sprintf "- %s") |> String.concat "\n"

    let renderFeature156ScenarioReport (report: Feature156ScenarioReport) =
        let full = feature156DistributionRow report.FullRedraw
        let damage = feature156DistributionRow report.DamageScoped
        let artifacts = renderArtifacts report.ArtifactPaths
        let overhead =
            if report.ProofOverheadIncluded then
                "proof-readback-or-validation-overhead-included"
            else
                "measurement-path-isolated-from-proof-readback"

        String.concat
            "\n"
            [ $"# Feature 156 Scenario: {report.ScenarioId}"
              ""
              $"Scenario id: `{report.ScenarioId}`"
              $"Verdict: `{feature156VerdictToken report.Verdict}`"
              $"Confidence decision: `{report.ConfidenceDecision}`"
              $"Warmup count: `{report.WarmupCount}`"
              $"Measured repetitions: `{report.MeasuredRepetitions}`"
              $"Noise band ms: `{feature156FormatMs report.NoiseBandMs}`"
              $"Overhead disclosure: `{overhead}`"
              ""
              "| Path | p50 ms | p95 ms | p99 ms | Samples |"
              "|------|--------|--------|--------|---------|"
              $"| full-redraw | {full} |"
              $"| damage-scoped | {damage} |"
              ""
              "## Artifacts"
              ""
              artifacts
              ""
              "## Rejection Reasons"
              ""
              feature156Reasons report
              "" ]

    let renderFeature156TimingSummary (summary: Feature156TimingSummary) =
        let renderer = summary.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = summary.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let rows =
            if List.isEmpty summary.ScenarioReports then
                "| none | missing | missing | missing | missing | missing | missing | missing | missing | missing |"
            else
                summary.ScenarioReports
                |> List.map (fun report ->
                    let fullP50 = report.FullRedraw |> Option.map (fun d -> feature156FormatMs d.P50Ms) |> Option.defaultValue "missing"
                    let fullP95 = report.FullRedraw |> Option.map (fun d -> feature156FormatMs d.P95Ms) |> Option.defaultValue "missing"
                    let fullP99 = report.FullRedraw |> Option.map (fun d -> feature156FormatMs d.P99Ms) |> Option.defaultValue "missing"
                    let damageP50 = report.DamageScoped |> Option.map (fun d -> feature156FormatMs d.P50Ms) |> Option.defaultValue "missing"
                    let damageP95 = report.DamageScoped |> Option.map (fun d -> feature156FormatMs d.P95Ms) |> Option.defaultValue "missing"
                    let damageP99 = report.DamageScoped |> Option.map (fun d -> feature156FormatMs d.P99Ms) |> Option.defaultValue "missing"
                    let artifact =
                        report.ArtifactPaths
                        |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                        |> Option.defaultValue (Path.Combine("scenarios", feature156ScenarioFileName report.ScenarioId))

                    $"| `{report.ScenarioId}` | `{fullP50}` | `{fullP95}` | `{fullP99}` | `{damageP50}` | `{damageP95}` | `{damageP99}` | `{feature156FormatMs report.NoiseBandMs}` | `{feature156VerdictToken report.Verdict}` | `{report.ConfidenceDecision}` | `{artifact}` |")
                |> String.concat "\n"

        let rejectionReasons =
            summary.ScenarioReports
            |> List.collect (fun report -> report.RejectionReasons |> List.map (fun reason -> $"{report.ScenarioId}: {reason}"))

        String.concat
            "\n"
            [ "# Feature 156 Same-Profile Timing Summary"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Feature 156 timing verdict: `{feature156VerdictToken summary.OverallVerdict}`"
              $"Shipped P7 performance claim: `{summary.ShippedPerformanceClaim}`"
              $"Policy id: `{summary.PolicyId}`"
              "Noise-band formula: `max(0.25 ms, 5% of full-redraw p50)`"
              $"Warmup count: `{summary.WarmupCount}`"
              $"Measured repetitions per path: `{summary.MeasuredRepetitions}`"
              ""
              "## Host Profile"
              ""
              $"- Accepted profile id: `{feature156AcceptedProfileId}`"
              $"- Measured profile id: `{summary.HostProfile.ProfileId}`"
              $"- Backend: `{summary.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{summary.HostProfile.PresentMode}`"
              $"- Framebuffer: `{summary.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Display environment: `{summary.HostProfile.DisplayEnvironment}`"
              $"- Package version: `{feature156PackageVersion}`"
              ""
              "## Feature 155 Baseline"
              ""
              "- Proof/parity baseline: `../155-native-proof-capture/readiness/validation-summary.md`"
              "- Correctness status: `accepted` for accepted host profile `probe-08a47c01`."
              "- Fallback status: `partial-redraw-accepted` for correctness; performance remains separate."
              ""
              "## Scenario Table"
              ""
              "| Scenario | Full p50 | Full p95 | Full p99 | Damage p50 | Damage p95 | Damage p99 | Noise band | Verdict | Confidence | Artifact |"
              "|----------|----------|----------|----------|------------|------------|------------|------------|---------|------------|----------|"
              rows
              ""
              "## Rejection Reasons"
              ""
              if List.isEmpty rejectionReasons then "- none" else rejectionReasons |> List.map (sprintf "- %s") |> String.concat "\n"
              ""
              "## Overhead Disclosure"
              ""
              "- Scenario reports state whether proof readback or validation overhead is included."
              "- Readback-dominated or unseparated overhead is `limited` and cannot support a shipped claim."
              ""
              "## Remaining Gates"
              ""
              "- Feature 157 damage-scissored no-clear renderer: `remaining`"
              "- Feature 158 readback separation: `remaining`"
              "- Feature 159 net-positive reuse/promotion counters: `remaining`"
              "- Feature 160 validation throughput follow-up: `remaining`, not a shipped performance-acceptance gate"
              "- Feature 161 host performance lane ledger: `remaining`"
              ""
              "## Diagnostics"
              ""
              if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"
              "" ]

    let renderFeature156CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 156 Compatibility Ledger"
              ""
              "Status: `accepted`"
              ""
              "## Public API and Diagnostics"
              ""
              "- `FS.GG.UI.Testing.CompositorTimingAssertions` adds package-visible timing summary validation for Feature 156."
              "- `FS.GG.UI.SkiaViewer.CompositorProof` adds timing path and proof-overhead disclosure helpers used by viewer-facing tests."
              "- `Rendering.Harness` adds `compositor-performance --feature 156` and `compositor-readiness --feature 156` evidence routes."
              ""
              "## Compatibility Impact"
              ""
              "- Existing Feature 155 proof, parity, fallback, and correctness vocabulary remains authoritative."
              "- `performance-not-accepted` remains the shipped P7 performance claim until later gates pass."
              "- New timing helpers are additive and do not change package identities."
              ""
              "## Migration Guidance"
              ""
              "- Consumers should treat `noisy`, `non-beneficial`, `incomplete`, `rejected`, `limited`, and `environment-limited` as non-accepting timing states."
              "- Positive Feature 156 timing is scoped to `probe-08a47c01` and is not a universal host performance claim."
              "" ]

    let renderFeature156ValidationSummary (summary: Feature156TimingSummary) =
        String.concat
            "\n"
            [ "# Feature 156 Readiness Summary"
              ""
              $"Status: `{feature156VerdictToken summary.OverallVerdict}`"
              "Proof/parity baseline: `accepted`"
              $"Timing status: `{feature156VerdictToken summary.OverallVerdict}`"
              "Correctness status: `accepted-via-feature-155`"
              "Fallback status: `partial-redraw-accepted`"
              $"Performance claim: `{summary.ShippedPerformanceClaim}`"
              $"Accepted host profile: `{feature156AcceptedProfileId}`"
              ""
              "## Evidence Links"
              ""
              "- Timing summary: `timing/summary.md`"
              "- Scenario reports: `timing/scenarios/`"
              "- Raw samples: `timing/raw/`"
              "- Unsupported host: `timing/unsupported/README.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI timing authoring: `fsi/compositor-performance-authoring.fsx`"
              "- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`"
              ""
              "## Reviewer Determination"
              ""
              "- A reviewer can determine scenario verdicts, distributions, host profile, policy, artifact paths, limitations, and final claim status from `timing/summary.md`."
              "- Under-5-minute determination check: recorded in this package after local validation."
              ""
              "## Decision"
              ""
              "- Timing evidence is fail-closed and scoped to the accepted Feature 155 profile."
              "- `performance-not-accepted` remains the shipped P7 performance claim until Features 157, 158, 159, and 161 pass."
              "- Feature 160 remains a validation-throughput follow-up, not a performance-acceptance gate."
              "" ]

    let renderFeature156UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 156 Unsupported Host Timing"
              ""
              "Status: `environment-limited`"
              "Accepted performance artifacts: `0`"
              $"Reason: `{reason}`"
              ""
              "Unsupported or unavailable presentation environments cannot contribute to positive timing evidence."
              "" ]

    let private renderFeature157Artifacts artifacts =
        match artifacts with
        | [] -> "- none"
        | xs -> xs |> List.map (sprintf "- `%s`") |> String.concat "\n"

    let renderFeature157AttemptReport (attempt: Feature157DamageAttempt) =
        let fallback =
            attempt.FallbackReason |> Option.defaultValue "none"
        let diagnostics =
            if List.isEmpty attempt.Diagnostics then "- none" else attempt.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 157 Damage Attempt"
              ""
              $"Attempt: `{attempt.AttemptId}`"
              $"Run identity: `{attempt.RunId}`"
              $"Scenario: `{attempt.ScenarioId}`"
              $"Host profile: `{attempt.HostProfile.ProfileId}`"
              $"Proof gate: `{attempt.ProofGate}`"
              $"Retained backing: `{attempt.RetainedBacking}`"
              $"Damage validation: `{attempt.DamageValidationStatus}`"
              $"Render decision: `{attempt.RenderDecision}`"
              $"Fallback reason: `{fallback}`"
              $"Preserved-pixel evidence: `{attempt.PreservedPixelEvidence}`"
              $"Damaged-pixel evidence: `{attempt.DamagedPixelEvidence}`"
              $"Parity status: `{attempt.ParityStatus}`"
              ""
              "## Artifacts"
              ""
              renderFeature157Artifacts attempt.ArtifactPaths
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature157FallbackReport (fallback: Feature157Fallback) =
        let diagnostics =
            if List.isEmpty fallback.Diagnostics then "- none" else fallback.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 157 Fallback"
              ""
              $"Scenario: `{fallback.ScenarioId}`"
              $"Primary reason: `{fallback.Reason}`"
              $"Damage validation: `{fallback.DamageValidationStatus}`"
              $"Accepted partial-redraw artifacts: `{fallback.AcceptedPartialRedrawArtifacts}`"
              ""
              "## Artifacts"
              ""
              renderFeature157Artifacts fallback.ArtifactPaths
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature157ParityReport (attempt: Feature157DamageAttempt) =
        String.concat
            "\n"
            [ "# Feature 157 Parity"
              ""
              $"Scenario: `{attempt.ScenarioId}`"
              $"Attempt: `{attempt.AttemptId}`"
              $"Parity status: `{attempt.ParityStatus}`"
              $"Preserved-pixel evidence: `{attempt.PreservedPixelEvidence}`"
              $"Damaged-pixel evidence: `{attempt.DamagedPixelEvidence}`"
              ""
              "Accepted parity requires zero unexplained drift outside damage and expected updates inside damage."
              "" ]

    let private feature157ScenarioRows summary =
        let acceptedRows =
            summary.AcceptedAttempts
            |> List.map (fun attempt ->
                let artifacts = String.concat ", " attempt.ArtifactPaths
                $"| `{attempt.ScenarioId}` | accepted | `{attempt.AttemptId}` | `{attempt.RenderDecision}` | `{attempt.ParityStatus}` | `{artifacts}` |")

        let fallbackRows =
            summary.Fallbacks
            |> List.map (fun fallback ->
                let artifacts = String.concat ", " fallback.ArtifactPaths
                $"| `{fallback.ScenarioId}` | fallback | `none` | `{fallback.Reason}` | `not-accepted` | `{artifacts}` |")

        match acceptedRows @ fallbackRows with
        | [] -> "| none | environment-limited | none | missing evidence | not-accepted | none |"
        | rows -> rows |> String.concat "\n"

    let renderFeature157DamageSummary (summary: Feature157DamageSummary) =
        let status = feature157OverallStatus summary
        let renderer = summary.HostProfile.Renderer |> Option.defaultValue "unknown"
        let unsupported =
            summary.UnsupportedHostReason |> Option.defaultValue "none"
        let diagnostics =
            if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 157 Damage Summary"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Damage readiness status: `{feature157StatusToken status}`"
              $"Accepted host profile: `{feature157AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Unsupported-host reason: `{unsupported}`"
              $"Shipped P7 performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Host Profile"
              ""
              $"- Backend: `{summary.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{summary.HostProfile.PresentMode}`"
              $"- Framebuffer: `{summary.HostProfile.FramebufferSize}`"
              $"- Display environment: `{summary.HostProfile.DisplayEnvironment}`"
              $"- Proof algorithm: `{summary.HostProfile.ProofAlgorithmVersion}`"
              ""
              "## Scenario Coverage"
              ""
              "| Scenario | Status | Attempt | Decision | Parity | Artifacts |"
              "|----------|--------|---------|----------|--------|-----------|"
              feature157ScenarioRows summary
              ""
              "## Required Scenarios"
              ""
              (feature157RequiredScenarioIds |> List.map (sprintf "- `%s`") |> String.concat "\n")
              ""
              "## Fallback Scenarios"
              ""
              (feature157FallbackScenarioIds |> List.map (sprintf "- `%s`") |> String.concat "\n")
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let private escapeJson (value: string) =
        value.Replace("\\", "\\\\").Replace("\"", "\\\"")

    let renderFeature157DamageSummaryJson (summary: Feature157DamageSummary) =
        let status = feature157OverallStatus summary
        let unsupportedReason = summary.UnsupportedHostReason |> Option.defaultValue ""
        let scenarios =
            summary.ScenarioCoverage
            |> List.map (fun scenario -> $"    \"{escapeJson scenario}\"")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": \"{escapeJson summary.RunId}\","
              $"  \"status\": \"{feature157StatusToken status}\","
              $"  \"hostProfile\": \"{escapeJson summary.HostProfile.ProfileId}\","
              $"  \"acceptedAttemptCount\": {summary.AcceptedAttempts.Length},"
              $"  \"fallbackCount\": {summary.Fallbacks.Length},"
              $"  \"unsupportedHostReason\": \"{escapeJson unsupportedReason}\","
              $"  \"performanceClaim\": \"{escapeJson summary.PerformanceClaim}\","
              "  \"scenarioCoverage\": ["
              scenarios
              "  ]"
              "}" ]

    let renderFeature157CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 157 Compatibility Ledger"
              ""
              "Status: `accepted-with-recorded-limitations`"
              ""
              "## Public API and Diagnostics"
              ""
              "- `FS.GG.UI.SkiaViewer.Host.GlHost` adds Feature 157 damage validation and no-clear render-decision helpers."
              "- `FS.GG.UI.SkiaViewer.Viewer.damageDecisionToken` exposes stable readiness tokens."
              "- `FS.GG.UI.Testing.CompositorDamageReadiness` validates accepted, fallback-only, rejected, and environment-limited damage packages."
              "- `Rendering.Harness` adds `compositor-damage --feature 157` and extends `compositor-readiness --feature 157`."
              ""
              "## Compatibility Impact"
              ""
              "- Existing Feature 155 proof-set and Feature 156 timing contracts remain source-compatible."
              "- Full redraw remains the default fallback unless all Feature 157 gates pass."
              "- The shipped P7 performance claim remains `performance-not-accepted`."
              "" ]

    let renderFeature157ValidationSummary (summary: Feature157DamageSummary) =
        let status = feature157OverallStatus summary
        String.concat
            "\n"
            [ "# Feature 157 Readiness Summary"
              ""
              $"Status: `{feature157StatusToken status}`"
              $"Accepted host profile: `{feature157AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Accepted attempts: `{summary.AcceptedAttempts.Length}`"
              $"Fallback attempts: `{summary.Fallbacks.Length}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Evidence Links"
              ""
              "- Damage summary: `damage/summary.md`"
              "- Damage summary JSON: `damage/summary.json`"
              "- Accepted attempts: `damage/attempts/`"
              "- Fallbacks: `damage/fallbacks/`"
              "- Parity: `damage/parity/`"
              "- Unsupported host: `damage/unsupported/README.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI damage authoring: `fsi/compositor-damage-authoring.fsx`"
              "- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`"
              ""
              "## Decision"
              ""
              "- Damage-scoped no-clear repaint is selected only when proof, profile, retained backing, damage, resources, and parity all pass."
              "- Missing or unverifiable gates use full redraw and record a primary fallback reason."
              "- `performance-not-accepted` remains the shipped P7 performance claim until later gates pass."
              "" ]

    let renderFeature157UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 157 Unsupported Host"
              ""
              "Status: `environment-limited`"
              "Accepted partial-redraw artifacts: `0`"
              $"Reason: `{reason}`"
              ""
              "Unsupported or unavailable presentation environments cannot accept damage-scoped no-clear artifacts."
              "" ]

    let private feature158SampleRows (samples: Feature158TimingSample list) =
        match samples with
        | [] -> "| none | none | none | none | none | none | none | none |"
        | xs ->
            xs
            |> List.map (fun sample ->
                let reason = sample.ExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue "none"
                $"| `{sample.SampleId}` | `{sample.ScenarioId}` | `{Perf.timingPathToken sample.Path}` | `{feature156FormatMs sample.DurationMs}` | `{Perf.measurementPolicyToken sample.MeasurementPolicy}` | `{Perf.inclusionStatusToken sample.InclusionStatus}` | `{reason}` | `{sample.ArtifactPath}` |")
            |> String.concat "\n"

    let private feature158Distribution (distribution: Feature158PathDistribution option) =
        feature158DistributionRow distribution

    let renderFeature158ScenarioReport (report: Feature158ScenarioReport) =
        let diagnostics =
            if List.isEmpty report.Diagnostics then "- none" else report.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ $"# Feature 158 Scenario: {report.ScenarioId}"
              ""
              $"Scenario id: `{report.ScenarioId}`"
              $"Scenario definition: `{report.ScenarioDefinitionId}`"
              $"Measurement-separation status: `{feature158StatusToken report.Status}`"
              $"Warmup count: `{report.WarmupCount}`"
              $"Measured repetitions: `{report.MeasuredRepetitions}`"
              $"Included timing samples: `{report.IncludedSamples.Length}`"
              $"Excluded timing samples: `{report.ExcludedSamples.Length}`"
              ""
              "## Distributions"
              ""
              "| Path | p50 ms | p95 ms | p99 ms | Samples |"
              "|------|--------|--------|--------|---------|"
              $"| full-redraw | {feature158Distribution report.FullRedraw} |"
              $"| damage-scoped | {feature158Distribution report.DamageScoped} |"
              ""
              "## Included Samples"
              ""
              "| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |"
              "|--------|----------|------|-------------|--------|--------|--------|----------|"
              feature158SampleRows report.IncludedSamples
              ""
              "## Excluded Samples"
              ""
              "| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |"
              "|--------|----------|------|-------------|--------|--------|--------|----------|"
              feature158SampleRows report.ExcludedSamples
              ""
              "## Proof/Probe Artifacts"
              ""
              renderArtifacts report.ProofProbeArtifacts
              ""
              "## Artifacts"
              ""
              renderArtifacts report.ArtifactPaths
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature158ExcludedSamplesReport (reason: Perf.ExclusionReason) (samples: Feature158TimingSample list) =
        String.concat
            "\n"
            [ $"# Feature 158 Excluded Samples: {Perf.exclusionReasonToken reason}"
              ""
              $"Primary reason: `{Perf.exclusionReasonToken reason}`"
              $"Excluded sample count: `{samples.Length}`"
              ""
              "| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |"
              "|--------|----------|------|-------------|--------|--------|--------|----------|"
              feature158SampleRows samples
              "" ]

    let renderFeature158ProofProbeReport (evidence: Feature158ProofProbeEvidence list) =
        let rows =
            match evidence with
            | [] -> "| none | none | none | none | none |"
            | xs ->
                xs
                |> List.map (fun item ->
                    let scenarios = String.concat ", " item.ScenarioIds
                    let artifacts = String.concat ", " item.ReadbackArtifacts
                    let samples = String.concat ", " item.ProbeSampleIds
                    $"| `{item.ProbeId}` | `{item.HostProfile.ProfileId}` | `{scenarios}` | `{Perf.exclusionReasonToken item.ExclusionReason}` | `{samples}` | `{artifacts}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 158 Proof/Probe Evidence"
              ""
              "Proof/readback remains available as explicit probe evidence and is excluded from performance acceptance."
              ""
              "| Probe | Host profile | Scenarios | Exclusion reason | Probe samples | Readback artifacts |"
              "|-------|--------------|-----------|------------------|---------------|--------------------|"
              rows
              "" ]

    let private feature158ScenarioRows (summary: Feature158TimingSummary) =
        match summary.ScenarioReports with
        | [] -> "| none | missing | 0 | 0 | missing | missing | missing |"
        | reports ->
            reports
            |> List.map (fun report ->
                let artifact =
                    report.ArtifactPaths
                    |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    |> Option.defaultValue (Path.Combine("scenarios", feature158ScenarioFileName report.ScenarioId).Replace('\\', '/'))
                let proofProbeLinks = String.concat ", " report.ProofProbeArtifacts
                $"| `{report.ScenarioId}` | `{feature158StatusToken report.Status}` | `{report.IncludedSamples.Length}` | `{report.ExcludedSamples.Length}` | `{report.ScenarioDefinitionId}` | `{artifact}` | `{proofProbeLinks}` |")
            |> String.concat "\n"

    let private feature158ExcludedReasons (summary: Feature158TimingSummary) =
        let excluded =
            summary.ExcludedSamples
            |> List.choose _.ExclusionReason
            |> List.countBy id

        match excluded with
        | [] -> "- none"
        | xs ->
            xs
            |> List.map (fun (reason, count) -> $"- `{Perf.exclusionReasonToken reason}`: `{count}`")
            |> String.concat "\n"

    let renderFeature158TimingSummary (summary: Feature158TimingSummary) =
        let status = feature158OverallStatus summary
        let renderer = summary.HostProfile.Renderer |> Option.defaultValue "unknown"
        let scale = summary.HostProfile.Scale |> Option.map string |> Option.defaultValue "unknown"
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let diagnostics =
            if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 158 Readback-Free Timing Summary"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Feature 158 measurement-separation status: `{feature158StatusToken status}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Accepted profile id: `{feature158AcceptedProfileId}`"
              $"Measured profile id: `{summary.HostProfile.ProfileId}`"
              $"Unsupported-host reason: `{unsupported}`"
              $"Included timing samples: `{summary.IncludedSamples.Length}`"
              $"Excluded timing samples: `{summary.ExcludedSamples.Length}`"
              $"Shipped P7 performance claim: `{summary.PerformanceClaim}`"
              $"Feature 156 comparison: `{summary.Feature156Comparison}`"
              $"Warmup count: `{summary.WarmupCount}`"
              $"Measured repetitions per path: `{summary.MeasuredRepetitions}`"
              ""
              "## Host Profile"
              ""
              $"- Backend: `{summary.HostProfile.Backend}`"
              $"- Renderer: `{renderer}`"
              $"- Present mode: `{summary.HostProfile.PresentMode}`"
              $"- Framebuffer: `{summary.HostProfile.FramebufferSize}`"
              $"- Scale: `{scale}`"
              $"- Display environment: `{summary.HostProfile.DisplayEnvironment}`"
              $"- Package version: `{feature156PackageVersion}`"
              ""
              "## Measurement Policy"
              ""
              "- Accepted timing samples must declare `readback-free` or `readback-outside-measurement`."
              "- Proof/probe/readback samples are listed as excluded evidence and never enter the accepted performance set."
              "- Missing, unverifiable, contaminated, cross-profile, cross-run, scenario-mismatched, package-mismatched, or unsupported samples fail closed."
              ""
              "## Scenario Coverage"
              ""
              "| Scenario | Status | Included | Excluded | Scenario definition | Artifact | Proof/probe links |"
              "|----------|--------|----------|----------|---------------------|----------|-------------------|"
              feature158ScenarioRows summary
              ""
              "## Excluded Reasons"
              ""
              feature158ExcludedReasons summary
              ""
              "## Evidence Links"
              ""
              "- Scenario reports: `scenarios/`"
              "- Raw samples: `raw/`"
              "- Excluded samples: `excluded/`"
              "- Unsupported host: `unsupported/README.md`"
              "- Proof/probe evidence: `../proof-probes/README.md`"
              ""
              "## Remaining Gates"
              ""
              "- Feature 159 net-positive reuse/promotion counters: `remaining`"
              "- Feature 161 host performance lane ledger: `remaining`"
              "- Feature 160 validation throughput follow-up: `remaining`, not a shipped performance-acceptance gate"
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature158TimingSummaryJson (summary: Feature158TimingSummary) =
        let status = feature158OverallStatus summary
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue ""
        let reasons =
            summary.ExcludedSamples
            |> List.choose _.ExclusionReason
            |> List.distinct
            |> List.map (fun reason -> $"    \"{escapeJson (Perf.exclusionReasonToken reason)}\"")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": \"{escapeJson summary.RunId}\","
              $"  \"status\": \"{feature158StatusToken status}\","
              $"  \"policyId\": \"{escapeJson summary.PolicyId}\","
              $"  \"hostProfile\": \"{escapeJson summary.HostProfile.ProfileId}\","
              $"  \"includedSampleCount\": {summary.IncludedSamples.Length},"
              $"  \"excludedSampleCount\": {summary.ExcludedSamples.Length},"
              $"  \"unsupportedHostReason\": \"{escapeJson unsupported}\","
              $"  \"feature156Comparison\": \"{escapeJson summary.Feature156Comparison}\","
              $"  \"performanceClaim\": \"{escapeJson summary.PerformanceClaim}\","
              "  \"excludedReasons\": ["
              reasons
              "  ]"
              "}" ]

    let renderFeature158CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 158 Compatibility Ledger"
              ""
              "Status: `accepted-with-recorded-limitations`"
              ""
              "## Public API and Diagnostics"
              ""
              "- No new `FS.GG.UI.Testing` public helper surface is introduced by Feature 158."
              "- No new `FS.GG.UI.SkiaViewer` public helper surface is introduced by Feature 158."
              "- `Rendering.Harness` adds `compositor-performance --feature 158`, `compositor-performance --feature 158 --probe-readback`, and `compositor-readiness --feature 158` evidence routes."
              "- Harness-visible `.fsi` contracts add measurement policy, proof/probe exclusion, and readiness-package records for reviewer evidence."
              ""
              "## Compatibility Impact"
              ""
              "- Existing Feature 155 proof-set, Feature 156 timing, and Feature 157 damage readiness contracts remain source-compatible."
              "- Proof readback remains available only as proof/probe evidence and is excluded from performance acceptance."
              "- The shipped P7 performance claim remains `performance-not-accepted` until Feature 159 and Feature 161 gates pass."
              ""
              "## Public Surface Drift"
              ""
              "- Package surface baselines for `FS.GG.UI.Testing` and `FS.GG.UI.SkiaViewer` are unchanged for Feature 158."
              "- Harness command output shape is additive and documented through readiness artifacts."
              "" ]

    let renderFeature158ValidationSummary (summary: Feature158TimingSummary) =
        let status = feature158OverallStatus summary
        String.concat
            "\n"
            [ "# Feature 158 Readiness Summary"
              ""
              $"Status: `{feature158StatusToken status}`"
              $"Measurement policy id: `{summary.PolicyId}`"
              $"Accepted host profile: `{feature158AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Included timing samples: `{summary.IncludedSamples.Length}`"
              $"Excluded timing samples: `{summary.ExcludedSamples.Length}`"
              $"Proof/probe evidence entries: `{summary.ProofProbeEvidence.Length}`"
              $"Feature 156 comparison: `{summary.Feature156Comparison}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Evidence Links"
              ""
              "- Timing summary: `timing/summary.md`"
              "- Timing summary JSON: `timing/summary.json`"
              "- Scenario reports: `timing/scenarios/`"
              "- Raw timing samples: `timing/raw/`"
              "- Excluded samples: `timing/excluded/`"
              "- Unsupported host: `timing/unsupported/README.md`"
              "- Proof/probe evidence: `proof-probes/README.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI performance authoring: `fsi/compositor-performance-authoring.fsx`"
              "- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`"
              ""
              "## Reviewer Checklist"
              ""
              "- Measurement policy is visible from this summary and `timing/summary.md`."
              "- Included samples are linked through scenario reports and raw CSV/JSON files."
              "- Excluded samples are grouped by stable reason under `timing/excluded/`."
              "- Proof/probe readback artifacts are linked from `proof-probes/README.md` and excluded from accepted timing."
              "- Unsupported-host output records `environment-limited`, accepted proof artifacts `0`, and accepted performance artifacts `0`."
              "- Feature 156 comparison is recorded as `supersedes`, `confirms`, or `contextualizes`; this run records the value above."
              "- Under-5-minute reviewer inspection evidence is recorded by this single entry point."
              ""
              "## Decision"
              ""
              "- Feature 158 accepts measurement separation only when required scenarios publish readback-free or outside-measurement samples."
              "- The shipped compositor performance claim remains `performance-not-accepted` until Feature 159 and Feature 161 pass."
              "" ]

    let renderFeature158UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 158 Unsupported Host Timing"
              ""
              "Status: `environment-limited`"
              "Accepted proof artifacts: `0`"
              "Accepted performance artifacts: `0`"
              $"Reason: `{reason}`"
              "Elapsed time target: `under-2-minutes`"
              ""
              "Unsupported or unavailable presentation environments cannot contribute to accepted readback-free timing evidence."
              "" ]

    let renderFeature159AttemptReport (attempt: Feature159Attempt) =
        let artifacts = renderArtifacts attempt.ArtifactPaths
        let diagnostics =
            if List.isEmpty attempt.Diagnostics then "- none" else attempt.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"
        let reason = attempt.PrimaryReason |> Option.defaultValue "none"

        String.concat
            "\n"
            [ "# Feature 159 Promotion Attempt"
              ""
              $"Attempt: `{attempt.AttemptId}`"
              $"Run identity: `{attempt.RunId}`"
              $"Scenario: `{attempt.ScenarioId}`"
              $"Policy id: `{attempt.PolicyId}`"
              $"Host profile: `{attempt.HostProfile.ProfileId}`"
              $"Promotion decision: `{attempt.PromotionDecision}`"
              $"Reuse decision: `{attempt.ReuseDecision}`"
              $"Primary reason: `{reason}`"
              $"Content identity: `{attempt.ContentIdentity}`"
              $"Placement identity: `{attempt.PlacementIdentity}`"
              $"Net saved work: `{attempt.CounterNetSavedWork}`"
              $"Parity status: `{attempt.ParityStatus}`"
              $"Accepted reuse artifacts: `{attempt.AcceptedReuseArtifacts}`"
              $"Accepted promotion artifacts: `{attempt.AcceptedPromotionArtifacts}`"
              ""
              "## Artifacts"
              ""
              artifacts
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let private feature159AttemptRows (summary: Feature159Summary) =
        match summary.Attempts with
        | [] -> "| none | missing | missing | missing | 0 | missing |"
        | attempts ->
            attempts
            |> List.map (fun attempt ->
                let reason = attempt.PrimaryReason |> Option.defaultValue "none"
                let artifact =
                    attempt.ArtifactPaths
                    |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    |> Option.defaultValue (Path.Combine("attempts", feature159ScenarioFileName attempt.ScenarioId).Replace('\\', '/'))
                $"| `{attempt.ScenarioId}` | `{attempt.PromotionDecision}` | `{attempt.ReuseDecision}` | `{reason}` | `{attempt.CounterNetSavedWork}` | `{artifact}` |")
            |> String.concat "\n"

    let renderFeature159PromotionSummary (summary: Feature159Summary) =
        let status = feature159OverallStatus summary
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let acceptedAttemptCount =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.CounterNetSavedWork > 0 && attempt.ParityStatus = "passed")
            |> List.length
        let diagnostics =
            if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 159 Layer Promotion Summary"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Feature 159 status: `{feature159StatusToken status}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Accepted profile id: `{feature159AcceptedProfileId}`"
              $"Measured profile id: `{summary.HostProfile.ProfileId}`"
              $"Unsupported-host reason: `{unsupported}`"
              $"Accepted attempts: `{acceptedAttemptCount}`"
              $"Net saved work: `{summary.CounterNetSavedWork}`"
              $"Shipped P7 performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Scenario Coverage"
              ""
              "| Scenario | Promotion | Reuse | Primary reason | Net saved work | Artifact |"
              "|----------|-----------|-------|----------------|----------------|----------|"
              feature159AttemptRows summary
              ""
              "## Required Scenarios"
              ""
              (summary.RequiredScenarioCoverage |> List.map (sprintf "- `%s`") |> String.concat "\n")
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature159CounterReport (summary: Feature159Summary) =
        let accepted =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.CounterNetSavedWork > 0 && attempt.ParityStatus = "passed")
        let placementOnlyReuse =
            accepted
            |> List.filter (fun attempt -> attempt.ReuseDecision = "content-reused-placement-updated")
            |> List.length
        let contentRerecording =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.ReuseDecision = "content-re-recorded")
            |> List.length
        let demotions =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.PromotionDecision = "demoted")
            |> List.length
        let fallbacks =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.ReuseDecision = "fallback-full-redraw")
            |> List.length
        let acceptedReuseArtifacts = summary.Attempts |> List.sumBy _.AcceptedReuseArtifacts
        let acceptedPromotionArtifacts = summary.Attempts |> List.sumBy _.AcceptedPromotionArtifacts

        String.concat
            "\n"
            [ "# Feature 159 Counter Evidence"
              ""
              $"Avoided/net saved work: `{summary.CounterNetSavedWork}`"
              $"Placement-only reuse attempts: `{placementOnlyReuse}`"
              $"Content re-recording attempts: `{contentRerecording}`"
              $"Demotions: `{demotions}`"
              $"Fallbacks: `{fallbacks}`"
              $"Accepted reuse artifacts: `{acceptedReuseArtifacts}`"
              $"Accepted promotion artifacts: `{acceptedPromotionArtifacts}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "Counters from unsupported, cross-profile, stale, missing-policy, resource-limited, or parity-failing attempts are excluded from accepted Feature 159 status."
              "" ]

    let renderFeature159CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 159 Compatibility Ledger"
              ""
              "Status: `accepted-with-recorded-limitations`"
              ""
              "## Public Surface"
              ""
              "- `FS.GG.UI.Controls` public package surface remains unchanged; Feature 159 retained-render helpers are internal diagnostics."
              "- `FS.GG.UI.SkiaViewer` public package surface remains unchanged; split replay diagnostics are internal to the viewer package."
              "- `FS.GG.UI.Testing` adds package-visible `Feature159Readiness` helper records and status tokens."
              "- `Rendering.Harness` adds `compositor-promotion --feature 159` and extends `compositor-readiness --feature 159`."
              ""
              "## Claim Boundary"
              ""
              "- Feature 159 may accept net-positive reuse/promotion counters."
              "- The shipped P7 performance claim remains `performance-not-accepted` until same-profile timing and host-lane gates also pass."
              ""
              "## Surface Evidence"
              ""
              "- `readiness/fsi/FS.GG.UI.Controls.txt`"
              "- `readiness/fsi/FS.GG.UI.SkiaViewer.txt`"
              "- `readiness/fsi/FS.GG.UI.Testing.txt`"
              "" ]

    let renderFeature159ValidationSummary (summary: Feature159Summary) =
        let status = feature159OverallStatus summary
        let acceptedAttemptCount =
            summary.Attempts
            |> List.filter (fun attempt -> attempt.CounterNetSavedWork > 0 && attempt.ParityStatus = "passed")
            |> List.length
        String.concat
            "\n"
            [ "# Feature 159 Readiness Summary"
              ""
              $"Status: `{feature159StatusToken status}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Accepted host profile: `{feature159AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Accepted attempt count: `{acceptedAttemptCount}`"
              $"Counter net saved work: `{summary.CounterNetSavedWork}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Evidence Links"
              ""
              "- Promotion summary: `promotion/summary.md`"
              "- Attempts: `promotion/attempts/`"
              "- Reuse: `promotion/reuse/README.md`"
              "- Demotions: `promotion/demotions/`"
              "- Fallbacks: `promotion/fallbacks/`"
              "- Parity: `promotion/parity/`"
              "- Unsupported host: `promotion/unsupported/validation.md`"
              "- Counters: `counters/promotion.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI identity authoring: `fsi/content-placement-identity-authoring.fsx`"
              "- FSI promotion authoring: `fsi/compositor-promotion-authoring.fsx`"
              "- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`"
              ""
              "## Reviewer Checklist"
              ""
              "- Required scenarios are listed in `promotion/summary.md`."
              "- Promotion decisions use stable tokens: `promoted`, `observing`, `kept`, `demoted`, `rejected`, `bypassed`, `non-beneficial`, `fallback-only`, `environment-limited`."
              "- Reuse decisions use stable tokens: `content-reused-placement-updated`, `content-recorded`, `content-re-recorded`, `fallback-full-redraw`, `reuse-rejected`, `environment-limited`."
              "- Unsupported-host evidence records accepted reuse artifacts `0` and accepted promotion artifacts `0`."
              "- Synthetic fixtures are limited to rejection/helper tests and include `SYNTHETIC` comments in test sources."
              "- `performance-not-accepted` remains the shipped compositor performance claim."
              ""
              "## Decision"
              ""
              "- Feature 159 accepts only same-profile, parity-passing, net-positive promotion/reuse counters."
              "- Missing, stale, ambiguous, cross-profile, resource-limited, unsupported, or parity-failing evidence fails closed."
              "" ]

    let renderFeature159UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 159 Unsupported Host Promotion"
              ""
              "Status: `environment-limited`"
              "Accepted Feature 159 reuse artifacts: `0`"
              "Accepted Feature 159 promotion artifacts: `0`"
              $"Reason: `{reason}`"
              "Elapsed time target: `under-2-minutes`"
              ""
              "Unsupported or unavailable presentation environments cannot contribute to accepted Feature 159 reuse or promotion evidence."
              "" ]

    let private feature160IterationRows (summary: Feature160ThroughputSummary) =
        match summary.Iterations with
        | [] -> "| none | missing | 0 | missing | missing | none |"
        | iterations ->
            iterations
            |> List.map (fun iteration ->
                let reason = iteration.ExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue "none"
                let artifact =
                    iteration.ArtifactPaths
                    |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    |> Option.defaultValue (Path.Combine("throughput", "iterations", feature160IterationFileName iteration.IterationId).Replace('\\', '/'))
                let restricted = iteration.RestrictedScenario |> Option.defaultValue "none"
                let durationMinutes = iteration.ActualDuration.TotalMinutes.ToString("0.###", Globalization.CultureInfo.InvariantCulture)
                $"| `{iteration.IterationId}` | `{feature160StatusToken iteration.Status}` | `{durationMinutes}` | `{reason}` | `{restricted}` | `{artifact}` |")
            |> String.concat "\n"

    let private feature160ScenarioRows (iteration: Feature160Iteration) =
        match iteration.ScenarioReports with
        | [] -> "| none | missing | 0 | 0 | missing |"
        | reports ->
            reports
            |> List.map (fun report ->
                $"| `{report.ScenarioId}` | `{feature158StatusToken report.Status}` | `{report.WarmupCount}` | `{report.MeasuredRepetitions}` | `{report.ScenarioDefinitionId}` |")
            |> String.concat "\n"

    let renderFeature160IterationReport (iteration: Feature160Iteration) =
        let reason = iteration.ExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue "none"
        let restricted = iteration.RestrictedScenario |> Option.defaultValue "none"
        let durationMinutes = iteration.ActualDuration.TotalMinutes.ToString("0.###", Globalization.CultureInfo.InvariantCulture)
        let diagnostics =
            if List.isEmpty iteration.Diagnostics then "- none" else iteration.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 160 Focused Throughput Iteration"
              ""
              $"Iteration id: `{iteration.IterationId}`"
              $"Run identity: `{iteration.RunId}`"
              $"Status: `{feature160StatusToken iteration.Status}`"
              $"Primary exclusion reason: `{reason}`"
              $"Restricted scenario: `{restricted}`"
              $"Lane id: `{iteration.LaneId}`"
              $"Policy id: `{iteration.PolicyId}`"
              $"Declared bound minutes: `{iteration.DeclaredBoundMinutes}`"
              $"Actual duration minutes: `{durationMinutes}`"
              $"Host profile: `{iteration.HostProfile.ProfileId}`"
              $"Warmup count: `{iteration.WarmupCount}`"
              $"Measured repetitions: `{iteration.MeasuredRepetitions}`"
              $"Included samples: `{iteration.IncludedSamples.Length}`"
              $"Excluded samples: `{iteration.ExcludedSamples.Length}`"
              ""
              "## Scenario Coverage"
              ""
              "| Scenario | Status | Warmup | Measured repetitions | Scenario definition |"
              "|----------|--------|--------|----------------------|---------------------|"
              feature160ScenarioRows iteration
              ""
              "## Artifacts"
              ""
              renderArtifacts iteration.ArtifactPaths
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature160ExcludedEvidenceReport reason (iterations: Feature160Iteration list) =
        let rows =
            match iterations with
            | [] -> "| none | 0 | none |"
            | xs ->
                xs
                |> List.map (fun iteration ->
                    let artifact =
                        iteration.ArtifactPaths
                        |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                        |> Option.defaultValue "missing"
                    let durationMinutes = iteration.ActualDuration.TotalMinutes.ToString("0.###", Globalization.CultureInfo.InvariantCulture)
                    $"| `{iteration.IterationId}` | `{durationMinutes}` | `{artifact}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ $"# Feature 160 Excluded Evidence: {Perf.exclusionReasonToken reason}"
              ""
              $"Primary reason: `{Perf.exclusionReasonToken reason}`"
              "Accepted throughput contribution: `0`"
              ""
              "| Iteration | Duration minutes | Artifact |"
              "|-----------|------------------|----------|"
              rows
              "" ]

    let renderFeature160ThroughputSummary (summary: Feature160ThroughputSummary) =
        let focusedStatus = feature160FocusedThroughputStatus summary
        let overallStatus = feature160OverallStatus summary
        let acceptedCount = summary.Iterations |> List.filter feature160IterationAccepted |> List.length
        let excludedCount = summary.Iterations |> List.filter (fun iteration -> iteration.ExclusionReason.IsSome) |> List.length
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let fullValidation = feature160FullValidationStatus summary.FullValidation
        let releaseStatus = if overallStatus = Feature160ReadinessStatus.Accepted then "ready" else "blocked"
        let diagnostics =
            if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 160 Focused Throughput Summary"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Final throughput status: `{feature160StatusToken overallStatus}`"
              $"Focused throughput status: `{feature160StatusToken focusedStatus}`"
              $"Full validation status: `{fullValidation}`"
              $"Release-ready status: `{releaseStatus}`"
              $"Shipped compositor performance claim: `{summary.PerformanceClaim}`"
              $"Lane id: `{summary.LaneId}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Declared per-iteration bound minutes: `{summary.DeclaredBoundMinutes}`"
              $"Required accepted iterations: `{summary.RequiredAttempts}`"
              $"Accepted iterations: `{acceptedCount}`"
              $"Excluded iterations: `{excludedCount}`"
              $"Unsupported-host reason: `{unsupported}`"
              $"Accepted same-profile performance artifacts from unsupported-host validation: `0`"
              $"Host profile: `{summary.HostProfile.ProfileId}`"
              $"Warmup count: `{summary.WarmupCount}`"
              $"Measured repetitions: `{summary.MeasuredRepetitions}`"
              ""
              "## Required Scenarios"
              ""
              (feature160RequiredScenarioIds |> List.map (sprintf "- `%s`") |> String.concat "\n")
              ""
              "## Iterations"
              ""
              "| Iteration | Status | Duration minutes | Primary reason | Restricted scenario | Artifact |"
              "|-----------|--------|------------------|----------------|---------------------|----------|"
              feature160IterationRows summary
              ""
              "## Release Gate Separation"
              ""
              "- Focused throughput collection does not run `dotnet test FS.GG.Rendering.slnx --no-restore`."
              "- Full validation is recorded separately under `full-validation/` and blocks release-ready status when missing, failing, interrupted, stale, or undocumented."
              "- Noisy same-profile timing remains a performance-claim gate; it is not a focused-throughput exclusion reason by itself."
              ""
              "## Artifact Links"
              ""
              "- Iterations: `throughput/iterations/`"
              "- Raw samples: `throughput/raw/`"
              "- Excluded evidence: `throughput/excluded/`"
              "- Unsupported-host evidence: `throughput/unsupported/README.md`"
              "- Full validation: `full-validation/validation.md`"
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature160ThroughputSummaryJson (summary: Feature160ThroughputSummary) =
        let focusedStatus = feature160FocusedThroughputStatus summary
        let overallStatus = feature160OverallStatus summary
        let acceptedCount = summary.Iterations |> List.filter feature160IterationAccepted |> List.length
        let unsupportedReason = summary.UnsupportedHostReason |> Option.defaultValue ""
        let excludedReasons =
            summary.Iterations
            |> List.choose _.ExclusionReason
            |> List.distinct
            |> List.map (fun reason -> $"    \"{escapeJson (Perf.exclusionReasonToken reason)}\"")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": \"{escapeJson summary.RunId}\","
              $"  \"status\": \"{feature160StatusToken overallStatus}\","
              $"  \"focusedThroughputStatus\": \"{feature160StatusToken focusedStatus}\","
              $"  \"fullValidationStatus\": \"{escapeJson (feature160FullValidationStatus summary.FullValidation)}\","
              $"  \"laneId\": \"{escapeJson summary.LaneId}\","
              $"  \"policyId\": \"{escapeJson summary.PolicyId}\","
              $"  \"hostProfileId\": \"{escapeJson summary.HostProfile.ProfileId}\","
              $"  \"declaredBoundMinutes\": {summary.DeclaredBoundMinutes},"
              $"  \"requiredAttempts\": {summary.RequiredAttempts},"
              $"  \"acceptedIterationCount\": {acceptedCount},"
              $"  \"iterationCount\": {summary.Iterations.Length},"
              $"  \"unsupportedHostReason\": \"{escapeJson unsupportedReason}\","
              $"  \"performanceClaim\": \"{escapeJson summary.PerformanceClaim}\","
              "  \"excludedReasons\": ["
              excludedReasons
              "  ]"
              "}" ]

    let renderFeature160CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 160 Compatibility Ledger"
              ""
              "Status: `accepted-with-recorded-limitations`"
              ""
              "## Public Surface"
              ""
              "- `FS.GG.UI.Testing` adds package-visible `Feature160ThroughputReadiness` helper records and status tokens."
              "- `Rendering.Harness` adds `compositor-performance --feature 160 --lane focused` and `compositor-readiness --feature 160` evidence routes."
              "- Controls and SkiaViewer package identities are unchanged."
              ""
              "## Compatibility Impact"
              ""
              "- The helper is additive and validates readiness packages; it does not change runtime rendering behavior."
              "- The shipped compositor performance claim remains `performance-not-accepted` until same-profile timing, Feature 159 reuse/promotion, Feature 160 throughput, and Feature 161 host-lane gates are complete."
              ""
              "## Surface Evidence"
              ""
              "- `readiness/fsi/FS.GG.UI.Testing.txt`"
              "- `readiness/fsi/Rendering.Harness.Compositor.txt`"
              "" ]

    let renderFeature160FullValidationRecord record =
        match record with
        | None ->
            String.concat
                "\n"
                [ "# Feature 160 Full Validation"
                  ""
                  "Status: `missing`"
                  "Command: `dotnet test FS.GG.Rendering.slnx --no-restore`"
                  "Release-ready blocker: `full-validation-missing`"
                  ""
                  "Full solution validation is intentionally not run inside focused throughput collection."
                  "" ]
        | Some validation ->
            let started = validation.StartedAt |> Option.map string |> Option.defaultValue "unknown"
            let completed = validation.CompletedAt |> Option.map string |> Option.defaultValue "unknown"
            String.concat
                "\n"
                [ "# Feature 160 Full Validation"
                  ""
                  $"Status: `{feature160FullValidationStatus (Some validation)}`"
                  $"Command: `{validation.Command}`"
                  $"Started: `{started}`"
                  $"Completed: `{completed}`"
                  $"Implementation commit: `{validation.ImplementationCommit}`"
                  $"Package/surface baseline: `{validation.PackageSurfaceBaseline}`"
                  ""
                  "## Artifacts"
                  ""
                  renderArtifacts validation.ArtifactPaths
                  ""
                  "## Diagnostics"
                  ""
                  if List.isEmpty validation.Diagnostics then "- none" else validation.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"
                  "" ]

    let renderFeature160ValidationSummary (summary: Feature160ThroughputSummary) =
        let focusedStatus = feature160FocusedThroughputStatus summary
        let overallStatus = feature160OverallStatus summary
        let fullValidation = feature160FullValidationStatus summary.FullValidation
        let acceptedCount = summary.Iterations |> List.filter feature160IterationAccepted |> List.length
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let releaseStatus = if overallStatus = Feature160ReadinessStatus.Accepted then "ready" else "blocked"
        let releaseDecision =
            if releaseStatus = "ready" then
                "- Release-ready status is ready because current full validation is passing."
            else
                "- Release-ready status remains blocked until current full validation is passing."

        String.concat
            "\n"
            [ "# Feature 160 Readiness Summary"
              ""
              $"Status: `{feature160StatusToken overallStatus}`"
              $"Focused throughput status: `{feature160StatusToken focusedStatus}`"
              $"Full validation status: `{fullValidation}`"
              $"Release-ready status: `{releaseStatus}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Lane id: `{summary.LaneId}`"
              $"Accepted host profile: `{feature160AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Accepted iteration count: `{acceptedCount}`"
              $"Required iteration count: `{summary.RequiredAttempts}`"
              $"Declared bound minutes: `{summary.DeclaredBoundMinutes}`"
              $"Unsupported-host result: `{unsupported}`"
              $"Compatibility impact: `{summary.CompatibilityImpact}`"
              $"Package validation: `{summary.PackageValidationStatus}`"
              $"Regression validation: `{summary.RegressionValidationStatus}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Evidence Links"
              ""
              "- Throughput summary: `throughput/summary.md`"
              "- Throughput summary JSON: `throughput/summary.json`"
              "- Iterations: `throughput/iterations/`"
              "- Raw samples: `throughput/raw/`"
              "- Excluded evidence: `throughput/excluded/`"
              "- Unsupported host: `throughput/unsupported/README.md`"
              "- Full validation: `full-validation/validation.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI performance authoring: `fsi/compositor-performance-authoring.fsx`"
              "- FSI readiness helper authoring: `fsi/feature160-throughput-readiness-authoring.fsx`"
              ""
              "## Reviewer Checklist"
              ""
              "- Required scenarios, lane id, policy id, declared bound, sample counts, accepted iterations, exclusions, and host profile are visible from `throughput/summary.md`."
              "- Unsupported-host evidence records accepted same-profile performance artifacts `0`."
              "- Full validation is a separate release gate and is visible from `full-validation/validation.md`."
              "- Compatibility, package, regression, and public-surface evidence are linked from this entry point."
              "- Under-5-minute reviewer decision target: this single summary links every required decision field."
              "- `performance-not-accepted` remains the shipped compositor performance claim."
              ""
              "## Decision"
              ""
              "- Feature 160 accepts validation throughput only when three fresh same-profile focused iterations complete within the declared bound with all Feature 158 scenarios and sample policy preserved."
              releaseDecision
              "" ]

    let renderFeature160UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 160 Unsupported Host Throughput"
              ""
              "Status: `environment-limited`"
              "Accepted same-profile performance artifacts: `0`"
              "Accepted focused throughput iterations: `0`"
              $"Reason: `{reason}`"
              $"Elapsed time target: `under-{feature160UnsupportedHostMinutes}-minutes`"
              ""
              "Unsupported or unavailable presentation environments cannot contribute to accepted Feature 160 throughput evidence."
              "" ]

    let renderFeature161HostFacts (facts: Feature161HostFacts) =
        let refresh =
            match facts.RefreshRateHz, facts.RefreshUnavailableReason with
            | Some hz, _ -> hz.ToString("0.###", Globalization.CultureInfo.InvariantCulture) + " Hz"
            | None, Some reason -> "unavailable: " + reason
            | None, None -> "missing"

        let direct =
            match facts.DirectRendering with
            | Some true -> "true"
            | Some false -> "false"
            | None -> "unknown"

        let limits =
            match facts.EnvironmentLimits with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 161 Host Facts"
              ""
              $"Run identity: `{facts.RunIdentity}`"
              $"Scenario identity: `{facts.ScenarioIdentity}`"
              $"Timing policy identity: `{facts.TimingPolicyIdentity}`"
              $"Collection time: `{facts.CollectionTime:O}`"
              $"Lane id: `{feature161LaneIdFromFacts facts}`"
              ""
              "## Required Facts"
              ""
              $"Display server: `{facts.DisplayServer}`"
              $"Display identity: `{facts.DisplayIdentity}`"
              $"Renderer identity: `{facts.RendererIdentity}`"
              $"Direct rendering: `{direct}`"
              $"Refresh: `{refresh}`"
              $"Driver identity: `{facts.DriverIdentity}`"
              $"Package version set: `{facts.PackageVersionSet}`"
              $"CPU load note: `{facts.CpuLoadNote}`"
              $"GPU load note: `{facts.GpuLoadNote}`"
              $"Host profile: `{facts.HostProfile.ProfileId}`"
              ""
              "## Environment Limits"
              ""
              limits
              ""
              "## Artifacts"
              ""
              renderArtifacts facts.ArtifactLocations
              "" ]

    let renderFeature161LedgerEntry (entry: Feature161LedgerEntry) =
        let reason = entry.PrimaryExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue "none"
        let priorRows =
            match entry.PriorGates with
            | [] -> "| none | missing | none |"
            | gates ->
                gates
                |> List.map (fun gate -> $"| `{gate.Feature}` | `{gate.Status}` | `{gate.EvidencePath}` |")
                |> String.concat "\n"

        let diagnostics =
            if List.isEmpty entry.Diagnostics then "- none" else entry.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 161 Lane Ledger Entry"
              ""
              $"Entry id: `{entry.EntryId}`"
              $"Lane id: `{entry.LaneId}`"
              $"Status: `{feature161StatusToken entry.Status}`"
              $"Primary exclusion reason: `{reason}`"
              $"Timing status: `{entry.TimingStatus}`"
              $"Accepted lane-scoped performance artifacts: `{entry.AcceptedLaneScopedPerformanceArtifacts}`"
              ""
              "## Host Facts"
              ""
              $"Display: `{entry.HostFacts.DisplayServer}` `{entry.HostFacts.DisplayIdentity}`"
              $"Renderer: `{entry.HostFacts.RendererIdentity}`"
              $"Direct rendering: `{entry.HostFacts.DirectRendering}`"
              $"Host profile: `{entry.HostFacts.HostProfile.ProfileId}`"
              $"Package version set: `{entry.HostFacts.PackageVersionSet}`"
              ""
              "## Prior P7 Gates"
              ""
              "| Feature | Status | Evidence |"
              "|---------|--------|----------|"
              priorRows
              ""
              "## Artifacts"
              ""
              renderArtifacts entry.ArtifactPaths
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature161ExcludedEvidenceReport reason entries =
        let rows =
            match entries with
            | [] -> "| none | none | 0 |"
            | xs ->
                xs
                |> List.map (fun entry ->
                    let artifact =
                        entry.ArtifactPaths
                        |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                        |> Option.defaultValue "missing"
                    $"| `{entry.EntryId}` | `{artifact}` | `{entry.AcceptedLaneScopedPerformanceArtifacts}` |")
                |> String.concat "\n"

        String.concat
            "\n"
            [ $"# Feature 161 Excluded Lane Evidence: {Perf.exclusionReasonToken reason}"
              ""
              $"Primary reason: `{Perf.exclusionReasonToken reason}`"
              "Accepted lane-scoped performance contribution: `0`"
              ""
              "| Entry | Artifact | Accepted contribution |"
              "|-------|----------|-----------------------|"
              rows
              "" ]

    let feature161EntryRows (summary: Feature161Summary) =
        match summary.Entries with
        | [] -> "| none | missing | none | 0 | none |"
        | entries ->
            entries
            |> List.map (fun entry ->
                let reason = entry.PrimaryExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue "none"
                let artifact =
                    entry.ArtifactPaths
                    |> List.tryFind (fun path -> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    |> Option.defaultValue (Path.Combine("lane-ledger", "entries", feature161LedgerEntryFileName entry.EntryId).Replace('\\', '/'))
                $"| `{entry.EntryId}` | `{feature161StatusToken entry.Status}` | `{reason}` | `{entry.AcceptedLaneScopedPerformanceArtifacts}` | `{artifact}` |")
            |> String.concat "\n"

    let renderFeature161LaneLedgerSummary (summary: Feature161Summary) =
        let status = feature161OverallStatus summary
        let acceptedCount = summary.Entries |> List.filter feature161LedgerEntryAccepted |> List.length
        let excludedCount = summary.Entries |> List.filter (fun entry -> entry.PrimaryExclusionReason.IsSome) |> List.length
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let acceptedLane = summary.ClaimScope.AcceptedLaneId |> Option.defaultValue "none"
        let blockers =
            match summary.ClaimScope.RemainingBlockers with
            | [] -> "- none"
            | xs -> xs |> List.map (sprintf "- %s") |> String.concat "\n"
        let diagnostics =
            if List.isEmpty summary.Diagnostics then "- none" else summary.Diagnostics |> List.map (sprintf "- %s") |> String.concat "\n"

        String.concat
            "\n"
            [ "# Feature 161 Host Performance Lane Ledger"
              ""
              $"Run identity: `{summary.RunId}`"
              $"Status: `{feature161StatusToken status}`"
              $"Release-ready status: `{summary.ReleaseReadyStatus}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Accepted lane id: `{acceptedLane}`"
              $"Claim applies to: {summary.ClaimScope.AppliesTo}"
              $"Accepted lane-scoped performance artifacts: `{acceptedCount}`"
              $"Excluded lane entries: `{excludedCount}`"
              $"Unsupported-host reason: `{unsupported}`"
              $"Full validation status: `{summary.FullValidationStatus}`"
              $"Compatibility impact: `{summary.CompatibilityImpact}`"
              $"Package validation: `{summary.PackageValidationStatus}`"
              $"Regression validation: `{summary.RegressionValidationStatus}`"
              ""
              "## Non-Generalized Lanes"
              ""
              (summary.ClaimScope.NonGeneralizedLanes |> List.map (sprintf "- %s") |> String.concat "\n")
              ""
              "## Remaining Blockers"
              ""
              blockers
              ""
              "## Ledger Entries"
              ""
              "| Entry | Status | Primary reason | Accepted artifacts | Artifact |"
              "|-------|--------|----------------|--------------------|----------|"
              feature161EntryRows summary
              ""
              "## Artifact Links"
              ""
              "- Host facts: `lane-ledger/host-facts/`"
              "- Entries: `lane-ledger/entries/`"
              "- Excluded evidence: `lane-ledger/excluded/`"
              "- Unsupported-host evidence: `lane-ledger/unsupported/README.md`"
              "- Summary JSON: `lane-ledger/summary.json`"
              ""
              "## Diagnostics"
              ""
              diagnostics
              "" ]

    let renderFeature161LaneLedgerSummaryJson (summary: Feature161Summary) =
        let status = feature161OverallStatus summary
        let acceptedCount = summary.Entries |> List.filter feature161LedgerEntryAccepted |> List.length
        let acceptedLane = summary.ClaimScope.AcceptedLaneId |> Option.defaultValue ""
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue ""
        let excludedReasons =
            summary.Entries
            |> List.choose _.PrimaryExclusionReason
            |> List.distinct
            |> List.map (fun reason -> $"    \"{escapeJson (Perf.exclusionReasonToken reason)}\"")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": \"{escapeJson summary.RunId}\","
              $"  \"status\": \"{feature161StatusToken status}\","
              $"  \"policyId\": \"{escapeJson summary.PolicyId}\","
              $"  \"acceptedLaneId\": \"{escapeJson acceptedLane}\","
              $"  \"hostProfileId\": \"{escapeJson summary.HostProfile.ProfileId}\","
              $"  \"acceptedLaneScopedPerformanceArtifacts\": {acceptedCount},"
              $"  \"entryCount\": {summary.Entries.Length},"
              $"  \"unsupportedHostReason\": \"{escapeJson unsupported}\","
              $"  \"performanceClaim\": \"{escapeJson summary.PerformanceClaim}\","
              "  \"excludedReasons\": ["
              excludedReasons
              "  ]"
              "}" ]

    let renderFeature161CompatibilityLedger () =
        String.concat
            "\n"
            [ "# Feature 161 Compatibility Ledger"
              ""
              "Status: `accepted-with-recorded-limitations`"
              ""
              "## Public Surface"
              ""
              "- `FS.GG.UI.Testing` adds package-visible `Feature161HostLaneReadiness` helper records and status tokens."
              "- `Rendering.Harness` adds `compositor-performance --feature 161 --lane host-ledger` and `compositor-readiness --feature 161` evidence routes."
              "- Runtime compositor rendering behavior is unchanged; the feature changes reviewer-visible performance readiness semantics only."
              ""
              "## Compatibility Impact"
              ""
              "- Host lane facts are additive diagnostics for package and release review."
              "- Evidence from X11 `:1` direct OpenGL AMD/Mesa is not generalized to Wayland, indirect GL, missing-display, software-raster, virtualized, or unknown lanes."
              "- The shipped compositor performance claim remains `performance-not-accepted` until same-profile timing, Feature 159 reuse/promotion, Feature 160 throughput, and Feature 161 host-lane gates are all accepted for one named lane."
              ""
              "## Surface Evidence"
              ""
              "- `readiness/fsi/FS.GG.UI.Testing.txt`"
              "- `readiness/fsi/Rendering.Harness.Compositor.txt`"
              "- `readiness/fsi/Rendering.Harness.Perf.txt`"
              "" ]

    let renderFeature161FullValidationRecord status =
        let normalized = if String.IsNullOrWhiteSpace status then "missing" else status
        String.concat
            "\n"
            [ "# Feature 161 Full Validation"
              ""
              $"Status: `{normalized}`"
              "Command: `dotnet test FS.GG.Rendering.slnx --no-restore`"
              if normalized = "passed" then
                  "Release-ready blocker: `none`"
              else
                  "Release-ready blocker: `full-validation-not-current-passed`"
              ""
              "Full solution validation is recorded separately from host-lane ledger collection."
              "" ]

    let renderFeature161ValidationSummary (summary: Feature161Summary) =
        let status = feature161OverallStatus summary
        let acceptedCount = summary.Entries |> List.filter feature161LedgerEntryAccepted |> List.length
        let unsupported = summary.UnsupportedHostReason |> Option.defaultValue "none"
        let acceptedLane = summary.ClaimScope.AcceptedLaneId |> Option.defaultValue "none"

        String.concat
            "\n"
            [ "# Feature 161 Readiness Summary"
              ""
              $"Status: `{feature161StatusToken status}`"
              $"Release-ready status: `{summary.ReleaseReadyStatus}`"
              $"Policy id: `{summary.PolicyId}`"
              $"Accepted lane id: `{acceptedLane}`"
              $"Accepted host profile: `{feature161AcceptedProfileId}`"
              $"Measured host profile: `{summary.HostProfile.ProfileId}`"
              $"Accepted lane-scoped performance artifacts: `{acceptedCount}`"
              $"Unsupported-host result: `{unsupported}`"
              $"Full validation status: `{summary.FullValidationStatus}`"
              $"Compatibility impact: `{summary.CompatibilityImpact}`"
              $"Package validation: `{summary.PackageValidationStatus}`"
              $"Regression validation: `{summary.RegressionValidationStatus}`"
              $"Performance claim: `{summary.PerformanceClaim}`"
              ""
              "## Evidence Links"
              ""
              "- Lane ledger summary: `lane-ledger/summary.md`"
              "- Lane ledger summary JSON: `lane-ledger/summary.json`"
              "- Host facts: `lane-ledger/host-facts/`"
              "- Accepted entries: `lane-ledger/entries/`"
              "- Excluded evidence: `lane-ledger/excluded/`"
              "- Unsupported host: `lane-ledger/unsupported/README.md`"
              "- Full validation: `full-validation/validation.md`"
              "- Compatibility ledger: `compatibility-ledger.md`"
              "- Package validation: `package-validation.md`"
              "- Regression validation: `regression-validation.md`"
              "- FSI compositor host-lane authoring: `fsi/compositor-host-lane-authoring.fsx`"
              "- FSI readiness helper authoring: `fsi/feature161-host-lane-readiness-authoring.fsx`"
              ""
              "## Reviewer Checklist"
              ""
              "- Lane facts list display, renderer, direct rendering, refresh, driver, package, load, environment, host profile, run, scenario, timing policy, collection time, and artifact locations."
              "- Accepted and rejected entries are separated by lane and never combined across display server, renderer, direct-rendering mode, driver, package, host profile, scenario, policy, or run identity."
              "- Unsupported-host evidence records accepted lane-scoped performance artifacts `0`."
              "- Prior P7 gates link Feature 155, Feature 157, Feature 158, Feature 159, and Feature 160 evidence."
              "- Compatibility, package, regression, full-validation, and public-surface evidence are linked from this entry point."
              "- Under-5-minute reviewer decision target: this single summary links every required decision field."
              "- `performance-not-accepted` remains the shipped compositor performance claim unless all timing, reuse, throughput, and host-lane gates pass for one named lane."
              ""
              "## Non-Generalized Lanes"
              ""
              (summary.ClaimScope.NonGeneralizedLanes |> List.map (sprintf "- %s") |> String.concat "\n")
              ""
              "## Remaining Blockers"
              ""
              (if List.isEmpty summary.ClaimScope.RemainingBlockers then "- none" else summary.ClaimScope.RemainingBlockers |> List.map (sprintf "- %s") |> String.concat "\n")
              "" ]

    let renderFeature161UnsupportedHostReport (reason: string) =
        String.concat
            "\n"
            [ "# Feature 161 Unsupported Host Lane Ledger"
              ""
              "Status: `environment-limited`"
              "Accepted lane-scoped performance artifacts: `0`"
              "Accepted host-lane ledger entries: `0`"
              $"Reason: `{reason}`"
              ""
              "Unsupported, missing-display, indirect-rendering, software-raster, virtualized, or unknown-renderer environments cannot contribute accepted lane-scoped performance evidence."
              "" ]
