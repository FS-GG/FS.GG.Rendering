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

let statusToken status =
    match status with
    | NotStarted -> "not-started"
    | CheckingLiveSession -> "checking-live-session"
    | ExercisingActions -> "exercising-actions"
    | WritingArtifacts -> "writing-artifacts"
    | Accepted -> "accepted"
    | Rejected -> "rejected"
    | Blocked -> "blocked"
    | EnvironmentLimited -> "environment-limited"
    | Failed -> "failed"

let init request =
    { Request = request
      Status = NotStarted
      MeasuredActionIds = []
      EnvironmentLimitations = []
      ArtifactPaths = []
      Diagnostics = [] },
    [ CheckLiveSession ]

let private allActionsMeasured model =
    let measured = Set.ofList model.MeasuredActionIds
    model.Request.ActionIds |> List.forall measured.Contains

let update msg model =
    match msg with
    | Start ->
        { model with Status = CheckingLiveSession }, [ CheckLiveSession ]
    | LiveSessionAvailable ->
        { model with Status = ExercisingActions }, [ ExerciseActions model.Request.ActionIds ]
    | LiveSessionUnavailable reason ->
        { model with
            Status = EnvironmentLimited
            EnvironmentLimitations = reason :: model.EnvironmentLimitations
            Diagnostics = reason :: model.Diagnostics },
        [ PersistArtifacts ]
    | ActionMeasured actionId ->
        let next =
            { model with
                MeasuredActionIds = (actionId :: model.MeasuredActionIds) |> List.distinct }

        if allActionsMeasured next then
            { next with Status = WritingArtifacts }, [ PersistArtifacts ]
        else
            next, []
    | ActionRejected(actionId, reason) ->
        { model with
            Status = Rejected
            MeasuredActionIds = (actionId :: model.MeasuredActionIds) |> List.distinct
            Diagnostics = reason :: model.Diagnostics },
        [ PersistArtifacts ]
    | WriteArtifacts ->
        { model with Status = WritingArtifacts }, [ PersistArtifacts ]
    | ArtifactsWritten paths ->
        let finalStatus =
            match model.Status with
            | EnvironmentLimited
            | Blocked
            | Rejected
            | Failed -> model.Status
            | _ when allActionsMeasured model -> Accepted
            | _ -> Blocked

        { model with
            Status = finalStatus
            ArtifactPaths = paths },
        []
    | ArtifactWriteFailed reason ->
        { model with
            Status = Failed
            Diagnostics = reason :: model.Diagnostics },
        []

let interpret interpreter model effect =
    match effect with
    | CheckLiveSession ->
        match interpreter.CheckLiveSession() with
        | Ok() -> LiveSessionAvailable
        | Error reason -> LiveSessionUnavailable reason
    | ExerciseActions actionIds ->
        match interpreter.ExerciseActions actionIds with
        | Ok measured when measured.Length = actionIds.Length ->
            measured |> List.tryLast |> Option.defaultValue "" |> ActionMeasured
        | Ok measured -> ActionRejected(measured |> List.tryLast |> Option.defaultValue "", "incomplete-action-coverage")
        | Error reason -> ActionRejected("", reason)
    | PersistArtifacts ->
        match interpreter.PersistArtifacts model with
        | Ok paths -> ArtifactsWritten paths
        | Error reason -> ArtifactWriteFailed reason
