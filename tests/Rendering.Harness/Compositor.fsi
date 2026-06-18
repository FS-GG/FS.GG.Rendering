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

    val artifactPath: directory: string -> name: string -> string
    val feature148ArtifactPath: directory: string -> name: string -> string
    val feature149ArtifactPath: directory: string -> name: string -> string
    val feature152ArtifactPath: directory: string -> name: string -> string
    val feature153ArtifactPath: directory: string -> name: string -> string
    val feature154ArtifactPath: directory: string -> name: string -> string
    val feature155ArtifactPath: directory: string -> name: string -> string
    val feature156ArtifactPath: directory: string -> name: string -> string
    val feature156ScenarioFileName: scenarioId: string -> string
    val feature156VerdictToken: verdict: Feature156ScenarioVerdict -> string
    val feature156DistributionRow: distribution: Feature156PathDistribution option -> string
    val feature156OverallVerdict: reports: Feature156ScenarioReport list -> Feature156ScenarioVerdict
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
    val renderFeature156ScenarioReport: report: Feature156ScenarioReport -> string
    val renderFeature156TimingSummary: summary: Feature156TimingSummary -> string
    val renderFeature156CompatibilityLedger: unit -> string
    val renderFeature156PackageValidation: validationLines: string list -> string
    val renderFeature156RegressionValidation: validationLines: string list -> string
    val renderFeature156ValidationSummary: summary: Feature156TimingSummary -> string
    val renderFeature156UnsupportedHostReport: reason: string -> string
