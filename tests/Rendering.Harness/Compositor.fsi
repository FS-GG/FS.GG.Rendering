namespace Rendering.Harness

open System

/// Feature 147 compositor proof, parity, threshold, and readiness contracts.
module Compositor =

    val featureId: string

    val readinessDirectory: string
    val presentProofDirectory: string
    val parityDirectory: string
    val perfDirectory: string
    val compatibilityLedgerPath: string
    val validationSummaryPath: string

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
    val renderPresentProof: proof: PresentProof -> string
    val renderValidationSummary: model: ReadinessModel -> string
    val renderCompatibilityLedger: model: ReadinessModel -> string
