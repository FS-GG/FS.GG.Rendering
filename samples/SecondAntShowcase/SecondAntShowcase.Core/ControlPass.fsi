/// Pure pass plan for the automated control pass (Feature 176). Type-only / catalog-only:
/// it composes `CoverageMap` (the completeness oracle, D1) and `InteractionContracts` (the
/// documented-behavior oracle, D2) into the verdict-record schema from `data-model.md`, plus
/// the pure `catalog -> behaviors -> record skeleton` plan and the pure functional exercise
/// (a fold over `Model.update`). All IO (visual capture, window diagnostics, file writes)
/// lives in the App-edge runner (`ControlPassRunner.fs`); nothing here touches the filesystem
/// or a window.
module SecondAntShowcase.Core.ControlPass

open SecondAntShowcase.Core.Model
open SecondAntShowcase.Core.InteractionContracts

// --- verdict DUs ------------------------------------------------------------

/// A control is `Interactive` iff it has at least one documented behavior (D2); otherwise it
/// is `DisplayOnly` and carries a reason.
type Classification =
    | Interactive
    | DisplayOnly

/// Per-behavior / per-state / per-transition verdict shared by the functional dimension.
type BehaviorVerdict =
    | Pass
    | Fail
    | NeedsReview
    | EnvironmentLimited

/// One documented behavior driven against the showcase MVU.
type BehaviorOutcome =
    { BehaviorId: string
      Description: string
      Expected: string
      Observed: string
      Verdict: BehaviorVerdict }

/// The interaction states an interactive control can be driven into (D4).
type InteractionStateKind =
    | Hover
    | Focus
    | Active
    | Selected
    | Disabled
    | ErrorState

/// One driven interaction state, verified to differ from rest (VR-4).
type InteractionStateOutcome =
    { State: InteractionStateKind
      DiffersFromRest: bool
      EvidenceRef: string
      Verdict: BehaviorVerdict }

/// Damage-locality classification, sourced from `DamageRegionInspection` (D5).
type DamageStatus =
    | Empty
    | Localized
    | Broad
    | FullSurface
    | Unsupported

/// One state-transition damage record.
type DamageOutcome =
    { TransitionId: string
      DamageStatus: DamageStatus
      DirtyPercentage: float
      AffectedRegionIds: string list
      Verdict: BehaviorVerdict }

/// The two appearances captured per control (FR-003).
type Appearance =
    | AntLight
    | AntDark

/// The two representative sizes captured per control (FR-003).
type SizeRole =
    | Preferred
    | Minimum

/// The state a visual cell was captured in: `Rest` for everything, plus the per-state cells
/// for interactive controls.
type VisualState =
    | Rest
    | StateCell of InteractionStateKind

/// Capture status from `VisualCompleteness` (data-model Visual Evidence Item).
type CaptureStatus =
    | Complete
    | MissingCapture
    | WrongSize
    | Undecodable
    | Degraded
    | BlockedCapture

/// Per-cell fidelity verdict (M-3).
type FidelityVerdict =
    | Approved
    | FidelityNeedsReview
    | FidelityBlocked
    | FidelityEnvironmentLimited

/// One appearance x size x state visual cell.
type VisualEvidenceItem =
    { TargetId: string
      Appearance: Appearance
      Size: SizeRole
      State: VisualState
      CapturePath: string
      CaptureStatus: CaptureStatus
      FidelityVerdict: FidelityVerdict
      Reasons: string list }

/// Aggregate functional verdict of a record. `NotApplicable` only for display-only (VR-5).
type FunctionalVerdict =
    | FunctionalPass
    | FunctionalFail
    | FunctionalNeedsReview
    | FunctionalEnvironmentLimited
    | NotApplicable

/// Aggregate visual fidelity verdict of a record.
type VisualVerdict =
    | VisualApproved
    | VisualNeedsReview
    | VisualBlocked
    | VisualEnvironmentLimited

/// One classified record per cataloged control — the atomic unit of "every control exercised
/// and classified" (data-model Control Verdict Record).
type ControlVerdictRecord =
    { ControlId: string
      Family: string
      PageContext: string list
      Classification: Classification
      ClassificationReason: string
      BehaviorsExercised: BehaviorOutcome list
      InteractionStates: InteractionStateOutcome list
      VisualEvidence: VisualEvidenceItem list
      DamageEvidence: DamageOutcome list
      FunctionalVerdict: FunctionalVerdict
      VisualVerdict: VisualVerdict
      Findings: string list
      Diagnostics: string list }

// --- finding ----------------------------------------------------------------

type FindingClassification =
    | SampleLocal
    | FrameworkShared

type FindingTier =
    | Tier1
    | Tier2

type FindingSeverity =
    | Critical
    | High
    | Medium
    | Low

type FindingLifecycle =
    | Found
    | FixedAndReVerified
    | Deferred

/// A discovered defect and its lifecycle (data-model Finding entity).
type Finding =
    { FindingId: string
      Description: string
      AffectedControls: string list
      Classification: FindingClassification
      Tier: FindingTier
      Severity: FindingSeverity
      Lifecycle: FindingLifecycle
      BeforeEvidence: string
      AfterEvidence: string option
      DeferralRationale: string option
      FollowUpRef: string option }

// --- pure plan --------------------------------------------------------------

/// The completeness oracle: the catalog ids the pass iterates (== `CoverageMap.catalogIds ()`).
val catalogControlIds: unit -> string list

/// Controls reachable from the enterprise template pages. Template pages compose catalog
/// controls and introduce no new ids, so this maps onto `catalogControlIds ()` (D1, T004).
val templateReachable: unit -> string list

/// The interaction family / category of a control, from the published `Catalog`.
val familyOf: controlId: string -> string

/// The page(s) the control is exercised on (catalog page plus any template page reaching it).
val pageContextOf: controlId: string -> string list

/// Classify a control: `Interactive` (>=1 documented behavior) or `DisplayOnly` + reason (D2).
val classify: controlId: string -> Classification * string

/// The documented behaviors of a control, from its interaction contracts (the SC-002 oracle).
val behaviorsFor: controlId: string -> InteractionContract list

/// The unexercised record skeleton for a control (classification set, outcomes empty).
val recordSkeleton: controlId: string -> ControlVerdictRecord

/// The full plan: one record skeleton per cataloged control, in catalog order.
val planRecords: unit -> ControlVerdictRecord list

/// Pure functional exercise of a single documented behavior: apply the contract's scripted
/// message to the model through `Model.update` and assert the model changed. Returns the
/// outcome and the post-behavior model (so a control's behaviors fold in sequence).
val exerciseBehavior:
    model: SecondAntShowcaseModel -> contract: InteractionContract -> BehaviorOutcome * SecondAntShowcaseModel

/// Drive every documented behavior of a control from a seeded model (pure fold, no IO).
val exerciseControl: seed: SecondAntShowcaseModel -> controlId: string -> BehaviorOutcome list

/// Aggregate a record's functional verdict from its classification + behavior outcomes (VR-5).
val aggregateFunctional:
    classification: Classification -> behaviors: BehaviorOutcome list -> FunctionalVerdict

/// Aggregate a record's visual verdict from its captured cells.
val aggregateVisual: cells: VisualEvidenceItem list -> VisualVerdict

/// The set of record ids equals the catalog set, with no missing/duplicate (VR-1/G-2).
val completenessHolds: records: ControlVerdictRecord list -> bool

/// Missing and duplicate ids relative to the catalog (for diagnostics on a completeness fail).
val completenessGaps: records: ControlVerdictRecord list -> string list * string list
