namespace Rendering.Harness

open System

/// Declarative validation lane runner and fail-closed readiness summary.
module ValidationLanes =

    type LaneStatus =
        | Passed
        | Failed
        | TimedOut
        | Hung
        | Skipped
        | Canceled
        | NotRun
        | EnvironmentLimited

    type OverallReadiness =
        | Ready
        | Blocked
        | Incomplete
        | EnvironmentLimitedReadiness

    type LaneCommand =
        { FileName: string
          Arguments: string list }

    type LaneDefinition =
        { Id: string
          Description: string
          Command: LaneCommand
          WorkingDirectory: string
          Required: bool
          Timeout: TimeSpan
          NoProgressTimeout: TimeSpan option
          LogPath: string
          ResultPath: string
          DiagnosticsPath: string
          OutputRoot: string
          ConcurrencyGroup: string option }

    type LaneResult =
        { LaneId: string
          Status: LaneStatus
          Command: string
          StartedUtc: DateTime option
          CompletedUtc: DateTime option
          Elapsed: TimeSpan option
          ExitCode: int option
          LogPath: string
          ResultArtifacts: string list
          Diagnostics: string list
          Caveats: string list
          AcceptedException: string option
          Required: bool }

    type ValidationSummary =
        { PackageProofStatus: LaneStatus option
          SelectedSamples: string list
          LocalFeedPath: string
          PackageCachePath: string option
          SourceRules: string list
          LaneResults: LaneResult list
          OverallReadiness: OverallReadiness
          Caveats: string list
          ArtifactRoot: string }

    type Model =
        { LaneDefinitions: LaneDefinition list
          RunningLaneIds: string list
          CompletedResults: LaneResult list
          CanceledLaneIds: string list
          Summary: ValidationSummary option
          Diagnostics: string list }

    type Msg =
        | RunRequested of laneIds: string list
        | LaneStarted of laneId: string * startedUtc: DateTime
        | LaneOutputReceived of laneId: string * output: string * atUtc: DateTime
        | LaneCompleted of LaneResult
        | LaneTimedOut of laneId: string * reason: string
        | LaneNoProgressDetected of laneId: string * reason: string
        | LaneCanceled of laneId: string * reason: string
        | LaneDiagnosticsCaptured of laneId: string * diagnosticPath: string
        | SummaryRequested
        | SummaryWritten of path: string
        | RunnerFailed of reason: string

    type Effect =
        | StartProcess of laneId: string
        | AppendLaneLog of laneId: string * text: string
        | PollProcess of laneId: string
        | StopProcess of laneId: string
        | CaptureDiagnostics of laneId: string
        | WriteLaneResult of laneId: string
        | WriteLaneSummary
        | CreateOutputRoot of laneId: string

    val statusToken: status: LaneStatus -> string

    val readinessToken: readiness: OverallReadiness -> string

    val commandText: command: LaneCommand -> string

    val defaultLaneDefinitions: repositoryRoot: string -> outDir: string -> LaneDefinition list

    val computeOverallReadiness: results: LaneResult list -> OverallReadiness

    val renderSummaryMarkdown: summary: ValidationSummary -> string

    val renderSummaryJson: summary: ValidationSummary -> string

    val init: lanes: LaneDefinition list -> Model * Effect list

    val update: msg: Msg -> model: Model -> Model * Effect list

    val runLane: lane: LaneDefinition -> LaneResult

    val runLanes: repositoryRoot: string -> outDir: string -> selectedLaneIds: string list -> ValidationSummary

    val writeSummary: outDir: string -> summary: ValidationSummary -> string list
