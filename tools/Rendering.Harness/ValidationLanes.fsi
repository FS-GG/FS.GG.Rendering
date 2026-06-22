namespace Rendering.Harness

open System

/// Declarative validation lane runner and fail-closed readiness summary.
module ValidationLanes =

    type ReadinessRole =
        | Required
        | Optional
        | Informational

    type LaneStatus =
        | Passed
        | Failed
        | TimedOut
        | NoProgressTimedOut
        | Canceled
        | Skipped
        | InfrastructureError
        | EnvironmentLimited
        | NotRun

    type OverallReadiness =
        | Ready
        | Blocked
        | Incomplete
        | EnvironmentLimitedReadiness

    type SelectionMode =
        | RequiredSelection
        | ExplicitSelection
        | ListOnlySelection

    type LaneCommand =
        { FileName: string
          Arguments: string list }

    type LaneDefinition =
        { Id: string
          DisplayName: string
          Description: string
          ReadinessRole: ReadinessRole
          Command: LaneCommand
          WorkingDirectory: string
          Timeout: TimeSpan
          NoProgressTimeout: TimeSpan option
          ProgressInterval: TimeSpan
          EvidenceDirectory: string
          LogPath: string
          ResultPath: string
          DiagnosticsPath: string
          OutputRoot: string
          ConcurrencyGroup: string option
          OutputScope: string option
          IsAggregate: bool
          SubstitutesFor: string option }

    type RunRequest =
        { RequestedLaneIds: string list
          IncludeOptionalLaneIds: string list
          OutDir: string
          RunId: string option
          ReplaceRun: bool
          ListOnly: bool
          AllowParallel: bool }

    type PreflightDiagnostic =
        { Code: string
          Message: string
          LaneIds: string list }

    type LaneRunPlan =
        { Request: RunRequest
          RunId: string
          SelectionMode: SelectionMode
          ArtifactRoot: string
          SelectedLanes: LaneDefinition list
          Diagnostics: PreflightDiagnostic list
          ReplacementNotice: string option }

    type LaneResult =
        { LaneId: string
          ReadinessRole: ReadinessRole
          Status: LaneStatus
          Command: string
          StartedUtc: DateTime option
          CompletedUtc: DateTime option
          Elapsed: TimeSpan option
          TimeoutBudget: TimeSpan option
          LastActivityUtc: DateTime option
          LastActivityText: string option
          ExitCode: int option
          LogPath: string
          ResultPath: string
          DiagnosticsPath: string
          ResultArtifacts: string list
          RuntimeDiagnostics: FS.GG.UI.Diagnostics.DiagnosticSummary option
          Reason: string option
          Diagnostics: string list
          Caveats: string list
          AcceptedEnvironmentLimitation: string option
          Substitution: string option
          IsAggregate: bool }

    type ValidationSummary =
        { RunId: string
          PolicyVersion: string
          OverallReadiness: OverallReadiness
          ArtifactRoot: string
          StartedUtc: DateTime
          CompletedUtc: DateTime
          FirstBlockingRequiredLane: string option
          LaneResults: LaneResult list
          Caveats: string list
          ReplacementNotice: string option }

    type ResponsivenessSummaryResult =
        { SummaryPath: string
          OverallReadiness: string
          RecordCount: int
          FirstFailedBudget: string option
          EnvironmentLimitations: string list
          Diagnostics: string list }

    type Model =
        { LaneDefinitions: LaneDefinition list
          RunPlan: LaneRunPlan option
          ActiveLaneId: string option
          PendingLaneIds: string list
          CompletedResults: LaneResult list
          CanceledLaneIds: string list
          Summary: ValidationSummary option
          Diagnostics: string list }

    type Msg =
        | RunRequested of RunRequest
        | PreflightPassed of LaneRunPlan
        | PreflightFailed of PreflightDiagnostic list
        | LaneStarted of laneId: string * startedUtc: DateTime
        | LaneOutputReceived of laneId: string * output: string * atUtc: DateTime
        | LaneHeartbeatDue of laneId: string * atUtc: DateTime
        | LaneCompleted of LaneResult
        | LaneTimedOut of laneId: string * reason: string
        | LaneNoProgressTimedOut of laneId: string * reason: string
        | InfrastructureErrorRaised of laneId: string option * reason: string
        | OperatorCanceled of reason: string
        | LaneCanceled of laneId: string * reason: string
        | SummaryRequested
        | SummaryWritten of markdownPath: string * jsonPath: string

    type Effect =
        | ValidateRequest of RunRequest
        | CreateRunRoot of path: string
        | CreateLaneEvidenceRoot of laneId: string * path: string
        | StartProcess of laneId: string
        | AppendLaneLog of laneId: string * text: string
        | PublishHeartbeat of laneId: string
        | PollProcess of laneId: string
        | StopProcess of laneId: string
        | WriteLaneResult of laneId: string
        | WriteSummary
        | RegisterCancelHandler

    val roleToken: role: ReadinessRole -> string

    val statusToken: status: LaneStatus -> string

    val readinessToken: readiness: OverallReadiness -> string

    val readResponsivenessSummary: path: string -> Result<ResponsivenessSummaryResult, string>

    val responsivenessSummaryLaneStatus: summary: ResponsivenessSummaryResult -> LaneStatus

    val commandText: command: LaneCommand -> string

    val createRunId: unit -> string

    val defaultRunRequest: outDir: string -> RunRequest

    val defaultLaneDefinitions: repositoryRoot: string -> runRoot: string -> LaneDefinition list

    val validateRequest:
        repositoryRoot: string ->
        lanes: LaneDefinition list ->
        request: RunRequest ->
            Result<LaneRunPlan, PreflightDiagnostic list>

    val computeOverallReadiness: results: LaneResult list -> OverallReadiness

    val firstBlockingRequiredLane: results: LaneResult list -> string option

    val laneStatusFromDiagnosticSummary: summary: FS.GG.UI.Diagnostics.DiagnosticSummary -> LaneStatus

    val renderSummaryMarkdown: summary: ValidationSummary -> string

    val renderSummaryJson: summary: ValidationSummary -> string

    val init: lanes: LaneDefinition list -> Model * Effect list

    val update: msg: Msg -> model: Model -> Model * Effect list

    /// Thread-safe captured-output accumulation with last-activity tracking (T035).
    type OutputBuffer =
        new: started: DateTime -> OutputBuffer
        member Append: line: string -> unit
        member LastActivityUtc: DateTime
        member LastActivityText: string
        member Snapshot: unit -> string * DateTime * string

    /// Process spawn plus stdout/stderr capture and exit-code access (T033).
    /// Spawn (Start) is the MVU interpreter edge; capture is wired to the supplied OutputBuffer.
    type ProcessRunner =
        new: lane: LaneDefinition * output: OutputBuffer -> ProcessRunner
        member Start: unit -> bool
        member WaitForExit: milliseconds: int -> bool
        member WaitForExit: unit -> unit
        member ExitCode: int
        member Kill: unit -> unit
        interface IDisposable

    /// Wall-clock and no-progress timeout budgets that terminate a running lane (T034).
    /// Preserves the TimedOut vs NoProgressTimedOut distinction (contract C-4).
    /// Monitor returns the terminal (status, exitCode, reason, diagnostics).
    type TimeoutManager =
        { LaneId: string
          WallClock: TimeSpan
          NoProgress: TimeSpan option
          ProgressInterval: TimeSpan }

        member Monitor:
            runner: ProcessRunner * output: OutputBuffer * started: DateTime ->
                LaneStatus * int option * string option * string list

    val runLane: lane: LaneDefinition -> LaneResult

    val runRequest: repositoryRoot: string -> request: RunRequest -> Result<ValidationSummary, PreflightDiagnostic list>

    val runLanes: repositoryRoot: string -> outDir: string -> selectedLaneIds: string list -> ValidationSummary

    val writeSummary: runRoot: string -> summary: ValidationSummary -> string list
