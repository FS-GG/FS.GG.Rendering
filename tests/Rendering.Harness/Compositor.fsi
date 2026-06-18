namespace Rendering.Harness

open System

/// Feature 147 compositor proof, parity, threshold, and readiness contracts.
module Compositor =

    val featureId: string
    val feature148Id: string
    val feature149Id: string
    val feature152Id: string

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

    val artifactPath: directory: string -> name: string -> string
    val feature148ArtifactPath: directory: string -> name: string -> string
    val feature149ArtifactPath: directory: string -> name: string -> string
    val feature152ArtifactPath: directory: string -> name: string -> string
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
