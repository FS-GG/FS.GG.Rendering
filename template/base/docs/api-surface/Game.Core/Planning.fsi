// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.16.0 (src/Game.Core/Planning.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Immutable authored content identity. It is intentionally distinct from simulation state.
type AuthoredState<'content> =
    {
        ContentId: string
        Revision: uint64
        Value: 'content
    }

/// Last state accepted by the real session authority.
type AcceptedState<'state> =
    {
        SessionId: string
        Revision: uint64
        StateDigest: string
        Value: 'state
    }

/// A prediction derived from one exact accepted-state identity.
type PredictedState<'state> =
    {
        ScenarioId: string
        BasisSessionId: string
        BasisRevision: uint64
        BasisDigest: string
        StepCount: uint64
        StateDigest: string
        Value: 'state
    }

[<RequireQualifiedAccess>]
type PlanningScenarioStatus =
    | Draft
    | Cancelled
    | Stale

/// One scenario branch. Intents remain product-owned semantic data.
type PlanningScenario<'state, 'intent> =
    {
        Prediction: PredictedState<'state>
        Intents: 'intent list
        Status: PlanningScenarioStatus
    }

/// Workspace planning state with authored, accepted and predicted values in separate types.
type PlanningSession<'content, 'state, 'intent> =
    {
        Authored: AuthoredState<'content>
        Accepted: AcceptedState<'state>
        Scenarios: PlanningScenario<'state, 'intent> list
    }

/// Product adapter. <c>Apply</c> should call the same transition function used by the live session.
type ScenarioAdapter<'state, 'intent> =
    {
        Apply: 'intent -> 'state -> Result<'state, SessionFailure>
        StateDigest: 'state -> string
    }

[<RequireQualifiedAccess>]
type PlanningIssue =
    | MissingContentId
    | MissingSessionId
    | MissingScenarioId
    | MissingStateDigest
    | DuplicateScenarioId of string
    | ScenarioNotFound of string
    | ScenarioNotDraft of string * PlanningScenarioStatus
    | AcceptedBasisChanged of scenarioId: string
    | ContractFailure of scenarioId: string * SessionFailure

/// Comparison data between a prediction and its exact accepted basis.
type PlanningComparison =
    {
        ScenarioId: string
        BasisRevision: uint64
        BasisDigest: string
        PredictedDigest: string
        PredictedSteps: uint64
        IsUnchanged: bool
    }

/// A request for the owning product/session layer. Planning never applies it to accepted state itself.
type PlanningCommitIntent<'intent> =
    {
        ScenarioId: string
        BasisSessionId: string
        BasisRevision: uint64
        BasisDigest: string
        Intents: 'intent list
        PredictedDigest: string
    }

[<RequireQualifiedAccess>]
module Planning =
    val create:
        authored: AuthoredState<'content> ->
        accepted: AcceptedState<'state> ->
            Result<PlanningSession<'content, 'state, 'intent>, PlanningIssue list>

    val beginScenario:
        scenarioId: string ->
        session: PlanningSession<'content, 'state, 'intent> ->
            Result<PlanningSession<'content, 'state, 'intent>, PlanningIssue>

    val apply:
        adapter: ScenarioAdapter<'state, 'intent> ->
        scenarioId: string ->
        intent: 'intent ->
        session: PlanningSession<'content, 'state, 'intent> ->
            Result<PlanningSession<'content, 'state, 'intent>, PlanningIssue>

    val compare:
        scenarioId: string ->
        session: PlanningSession<'content, 'state, 'intent> ->
            Result<PlanningComparison, PlanningIssue>

    val cancel:
        scenarioId: string ->
        session: PlanningSession<'content, 'state, 'intent> ->
            Result<PlanningSession<'content, 'state, 'intent>, PlanningIssue>

    val proposeCommit:
        scenarioId: string ->
        session: PlanningSession<'content, 'state, 'intent> ->
            Result<PlanningCommitIntent<'intent>, PlanningIssue>

    /// Replace state only after the real session authority accepts it; existing drafts become stale.
    val replaceAccepted:
        accepted: AcceptedState<'state> ->
        session: PlanningSession<'content, 'state, 'intent> ->
            Result<PlanningSession<'content, 'state, 'intent>, PlanningIssue list>
