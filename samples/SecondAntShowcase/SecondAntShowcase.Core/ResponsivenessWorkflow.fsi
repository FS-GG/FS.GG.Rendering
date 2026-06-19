module SecondAntShowcase.Core.ResponsivenessWorkflow

type RunRequest =
    { RunId: string
      Scope: string
      Theme: string
      OutputRoot: string
      RequireLive: bool
      ActionIds: string list }

type RunStatus =
    | NotStarted
    | CheckingLiveSession
    | ExercisingActions
    | WritingArtifacts
    | Accepted
    | Rejected
    | Blocked
    | EnvironmentLimited
    | Failed

type Model =
    { Request: RunRequest
      Status: RunStatus
      MeasuredActionIds: string list
      EnvironmentLimitations: string list
      ArtifactPaths: string list
      Diagnostics: string list }

type Msg =
    | Start
    | LiveSessionAvailable
    | LiveSessionUnavailable of string
    | ActionMeasured of actionId: string
    | ActionRejected of actionId: string * reason: string
    | WriteArtifacts
    | ArtifactsWritten of paths: string list
    | ArtifactWriteFailed of reason: string

type Effect =
    | CheckLiveSession
    | ExerciseActions of actionIds: string list
    | PersistArtifacts

type Interpreter =
    { CheckLiveSession: unit -> Result<unit, string>
      ExerciseActions: string list -> Result<string list, string>
      PersistArtifacts: Model -> Result<string list, string> }

val init: request: RunRequest -> Model * Effect list
val update: msg: Msg -> model: Model -> Model * Effect list
val interpret: interpreter: Interpreter -> model: Model -> effect: Effect -> Msg
val statusToken: status: RunStatus -> string
