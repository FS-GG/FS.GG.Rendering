module SecondAntShowcase.Core.ControlPass

open FS.GG.UI.Controls
open SecondAntShowcase.Core.Model
open SecondAntShowcase.Core.InteractionContracts

// --- verdict DUs ------------------------------------------------------------

type Classification =
    | Interactive
    | DisplayOnly

type BehaviorVerdict =
    | Pass
    | Fail
    | NeedsReview
    | EnvironmentLimited

type BehaviorOutcome =
    { BehaviorId: string
      Description: string
      Expected: string
      Observed: string
      Verdict: BehaviorVerdict }

type InteractionStateKind =
    | Hover
    | Focus
    | Active
    | Selected
    | Disabled
    | ErrorState

type InteractionStateOutcome =
    { State: InteractionStateKind
      DiffersFromRest: bool
      EvidenceRef: string
      Verdict: BehaviorVerdict }

type DamageStatus =
    | Empty
    | Localized
    | Broad
    | FullSurface
    | Unsupported

type DamageOutcome =
    { TransitionId: string
      DamageStatus: DamageStatus
      DirtyPercentage: float
      AffectedRegionIds: string list
      Verdict: BehaviorVerdict }

type Appearance =
    | AntLight
    | AntDark

type SizeRole =
    | Preferred
    | Minimum

type VisualState =
    | Rest
    | StateCell of InteractionStateKind

type CaptureStatus =
    | Complete
    | MissingCapture
    | WrongSize
    | Undecodable
    | Degraded
    | BlockedCapture

type FidelityVerdict =
    | Approved
    | FidelityNeedsReview
    | FidelityBlocked
    | FidelityEnvironmentLimited

type VisualEvidenceItem =
    { TargetId: string
      Appearance: Appearance
      Size: SizeRole
      State: VisualState
      CapturePath: string
      CaptureStatus: CaptureStatus
      FidelityVerdict: FidelityVerdict
      Reasons: string list }

type FunctionalVerdict =
    | FunctionalPass
    | FunctionalFail
    | FunctionalNeedsReview
    | FunctionalEnvironmentLimited
    | NotApplicable

type VisualVerdict =
    | VisualApproved
    | VisualNeedsReview
    | VisualBlocked
    | VisualEnvironmentLimited

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

let catalogControlIds () : string list = CoverageMap.catalogIds ()

/// Template pages carry no ControlIds (they compose catalog controls), so the
/// template-reachable set is the distinct union of every template page's assigned ids — which
/// is empty in this sample and therefore trivially a subset of the catalog (D1, T004).
let templateReachable () : string list =
    PageRegistry.templatePages
    |> List.collect (fun page -> page.ControlIds)
    |> List.distinct
    |> List.sort

let familyOf (controlId: string) : string =
    Catalog.supportedControls
    |> List.tryFind (fun d -> d.Id = controlId)
    |> Option.map (fun d -> d.Category)
    |> Option.defaultValue "unknown"

let pageContextOf (controlId: string) : string list =
    let pages =
        PageRegistry.all
        |> List.filter (fun page -> page.ControlIds |> List.contains controlId)
        |> List.map (fun page -> page.Id)

    match pages with
    | [] -> [ "unassigned" ]
    | ps -> ps

let classify (controlId: string) : Classification * string =
    match forControl controlId with
    | _ :: _ -> Interactive, ""
    | [] ->
        let reason =
            displayOnlyReasons
            |> Map.tryFind controlId
            |> Option.defaultValue "no documented interactive behavior in this sample"

        DisplayOnly, reason

let behaviorsFor (controlId: string) : InteractionContract list = forControl controlId

let recordSkeleton (controlId: string) : ControlVerdictRecord =
    let classification, reason = classify controlId

    { ControlId = controlId
      Family = familyOf controlId
      PageContext = pageContextOf controlId
      Classification = classification
      ClassificationReason = reason
      BehaviorsExercised = []
      InteractionStates = []
      VisualEvidence = []
      DamageEvidence = []
      FunctionalVerdict =
        match classification with
        | DisplayOnly -> NotApplicable
        | Interactive -> FunctionalNeedsReview
      VisualVerdict = VisualEnvironmentLimited
      Findings = []
      Diagnostics = [] }

let planRecords () : ControlVerdictRecord list =
    catalogControlIds () |> List.map recordSkeleton

/// Describe what changed between two models, for the `Observed` field. Kept terse and
/// deterministic (no timestamps) so same-seed runs are byte-stable.
let private describeChange (before: SecondAntShowcaseModel) (after: SecondAntShowcaseModel) : string =
    if before = after then
        "no model change"
    else
        let parts =
            [ if before.CurrentPage <> after.CurrentPage then
                  sprintf "page %s->%s" before.CurrentPage after.CurrentPage
              if before.Mode <> after.Mode then
                  "mode toggled"
              if before.PageState <> after.PageState then
                  "page state changed" ]

        match parts with
        | [] -> "model changed"
        | ps -> String.concat "; " ps

let exerciseBehavior
    (model: SecondAntShowcaseModel)
    (contract: InteractionContract)
    : BehaviorOutcome * SecondAntShowcaseModel =
    match contract.ScriptStep with
    | None ->
        // A documented behavior with no scripted message cannot be exercised deterministically.
        let outcome =
            { BehaviorId = contract.ContractId
              Description = contract.Action
              Expected = contract.ExpectedStateChange
              Observed = "no scripted message — behavior not exercisable in the pure pass"
              Verdict = NeedsReview }

        outcome, model
    | Some msg ->
        let model' = Model.update msg model
        let changed = model' <> model

        let outcome =
            { BehaviorId = contract.ContractId
              Description = contract.Action
              Expected = contract.ExpectedStateChange
              Observed = describeChange model model'
              Verdict = (if changed then Pass else Fail) }

        outcome, model'

let exerciseControl (seed: SecondAntShowcaseModel) (controlId: string) : BehaviorOutcome list =
    behaviorsFor controlId
    |> List.fold
        (fun (outcomes, model) contract ->
            // Each behavior starts from the seeded model on the control's page, so behaviors are
            // independent (an earlier toggle doesn't mask a later one) and deterministic.
            let pageId = contract.PageId
            let startModel = { seed with CurrentPage = pageId }
            let outcome, _ = exerciseBehavior startModel contract
            outcomes @ [ outcome ], model)
        ([], seed)
    |> fst

let aggregateFunctional (classification: Classification) (behaviors: BehaviorOutcome list) : FunctionalVerdict =
    match classification with
    | DisplayOnly -> NotApplicable
    | Interactive ->
        let verdicts = behaviors |> List.map (fun b -> b.Verdict)

        if List.isEmpty verdicts then FunctionalNeedsReview
        elif List.contains Fail verdicts then FunctionalFail
        elif List.contains NeedsReview verdicts then FunctionalNeedsReview
        elif List.contains EnvironmentLimited verdicts then FunctionalEnvironmentLimited
        else FunctionalPass

let aggregateVisual (cells: VisualEvidenceItem list) : VisualVerdict =
    if List.isEmpty cells then
        VisualEnvironmentLimited
    else
        let fidelities = cells |> List.map (fun c -> c.FidelityVerdict)

        if List.contains FidelityBlocked fidelities then VisualBlocked
        elif List.contains FidelityEnvironmentLimited fidelities then VisualEnvironmentLimited
        elif List.contains FidelityNeedsReview fidelities then VisualNeedsReview
        else VisualApproved

let completenessGaps (records: ControlVerdictRecord list) : string list * string list =
    let catalog = catalogControlIds ()
    let catalogSet = Set.ofList catalog
    let recordIds = records |> List.map (fun r -> r.ControlId)
    let recordSet = Set.ofList recordIds

    let missing = catalog |> List.filter (fun id -> not (recordSet.Contains id))

    let duplicate =
        recordIds
        |> List.countBy id
        |> List.choose (fun (k, n) -> if n > 1 then Some k else None)

    let foreign = recordIds |> List.filter (fun id -> not (catalogSet.Contains id)) |> List.distinct

    missing, (duplicate @ foreign |> List.distinct)

let completenessHolds (records: ControlVerdictRecord list) : bool =
    let missing, duplicate = completenessGaps records
    List.isEmpty missing && List.isEmpty duplicate
