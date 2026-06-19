namespace Rendering.Harness

open System
open System.Diagnostics
open System.Globalization
open System.IO
open System.Text
open System.Text.Json

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

    let policyVersion = "validation-lanes-v1"

    let roleToken role =
        match role with
        | Required -> "required"
        | Optional -> "optional"
        | Informational -> "informational"

    let statusToken status =
        match status with
        | Passed -> "passed"
        | Failed -> "failed"
        | TimedOut -> "timed-out"
        | NoProgressTimedOut -> "no-progress-timeout"
        | Canceled -> "canceled"
        | Skipped -> "skipped"
        | InfrastructureError -> "infrastructure-error"
        | EnvironmentLimited -> "environment-limited"
        | NotRun -> "not-run"

    let readinessToken readiness =
        match readiness with
        | Ready -> "ready"
        | Blocked -> "blocked"
        | Incomplete -> "incomplete"
        | EnvironmentLimitedReadiness -> "environment-limited"

    let private tryGetProperty (name: string) (element: JsonElement) =
        let mutable value = Unchecked.defaultof<JsonElement>

        if element.TryGetProperty(name, &value) then
            Some value
        else
            None

    let private getStringOr fallback (element: JsonElement) =
        match element.GetString() with
        | null -> fallback
        | value -> value

    let private stringArray name (root: JsonElement) =
        match tryGetProperty name root with
        | Some value when value.ValueKind = JsonValueKind.Array ->
            value.EnumerateArray()
            |> Seq.choose (fun item ->
                if item.ValueKind = JsonValueKind.String then
                    Some(getStringOr "" item)
                else
                    None)
            |> Seq.toList
        | _ -> []

    let private failedBudgetKind (root: JsonElement) =
        match tryGetProperty "firstFailedBudget" root with
        | Some value when value.ValueKind = JsonValueKind.Object ->
            match tryGetProperty "kind" value with
            | Some kind when kind.ValueKind = JsonValueKind.String -> Some(getStringOr "" kind)
            | _ -> None
        | _ -> None

    let private recordCount (root: JsonElement) =
        match tryGetProperty "groups" root with
        | Some groups when groups.ValueKind = JsonValueKind.Array ->
            groups.EnumerateArray()
            |> Seq.sumBy (fun group ->
                match tryGetProperty "count" group with
                | Some count when count.ValueKind = JsonValueKind.Number -> count.GetInt32()
                | _ -> 0)
        | _ -> 0

    let readResponsivenessSummary path =
        try
            if not (File.Exists path) then
                Result.Error $"Responsiveness summary does not exist: {path}"
            else
                use doc = JsonDocument.Parse(File.ReadAllText path)
                let root = doc.RootElement

                let readiness =
                    match tryGetProperty "overallReadiness" root with
                    | Some value when value.ValueKind = JsonValueKind.String -> getStringOr "failed" value
                    | _ -> "failed"

                Result.Ok
                    { SummaryPath = path
                      OverallReadiness = readiness
                      RecordCount = recordCount root
                      FirstFailedBudget = failedBudgetKind root
                      EnvironmentLimitations = stringArray "environmentLimitations" root
                      Diagnostics = stringArray "diagnostics" root }
        with ex ->
            Result.Error $"Could not read responsiveness summary '{path}': {ex.Message}"

    let responsivenessSummaryLaneStatus summary =
        match summary.OverallReadiness with
        | "accepted" -> Passed
        | "environment-limited" -> EnvironmentLimited
        | "incomplete" -> Skipped
        | "blocked" -> Failed
        | "failed" -> InfrastructureError
        | _ -> InfrastructureError

    let quoteArg (arg: string) =
        if arg.Contains(' ') || arg.Contains(';') then
            "\"" + arg.Replace("\"", "\\\"") + "\""
        else
            arg

    let commandText (command: LaneCommand) =
        String.concat " " (command.FileName :: (command.Arguments |> List.map quoteArg))

    let createRunId () =
        let stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
        let suffix = Guid.NewGuid().ToString("N").Substring(0, 6)
        $"validation-{stamp}-{suffix}"

    let defaultRunRequest outDir =
        { RequestedLaneIds = []
          IncludeOptionalLaneIds = []
          OutDir = outDir
          RunId = None
          ReplaceRun = false
          ListOnly = false
          AllowParallel = false }

    let laneDirectory (runRoot: string) (laneId: string) =
        Path.Combine(runRoot, laneId)

    let laneDefinition
        (repositoryRoot: string)
        (runRoot: string)
        (laneId: string)
        (displayName: string)
        (description: string)
        (role: ReadinessRole)
        (command: LaneCommand)
        (timeoutMinutes: float)
        (noProgressMinutes: float option)
        (progressSeconds: float)
        (concurrencyGroup: string option)
        (outputScope: string option)
        (isAggregate: bool)
        (substitutesFor: string option)
        : LaneDefinition =
        let laneDir = laneDirectory runRoot laneId

        { Id = laneId
          DisplayName = displayName
          Description = description
          ReadinessRole = role
          Command = command
          WorkingDirectory = repositoryRoot
          Timeout = TimeSpan.FromMinutes timeoutMinutes
          NoProgressTimeout = noProgressMinutes |> Option.map TimeSpan.FromMinutes
          ProgressInterval = TimeSpan.FromSeconds progressSeconds
          EvidenceDirectory = laneDir
          LogPath = Path.Combine(laneDir, "log.txt")
          ResultPath = Path.Combine(laneDir, "result.json")
          DiagnosticsPath = Path.Combine(laneDir, "diagnostics.md")
          OutputRoot = Path.Combine(laneDir, "out")
          ConcurrencyGroup = concurrencyGroup
          OutputScope = outputScope
          IsAggregate = isAggregate
          SubstitutesFor = substitutesFor }

    let defaultLaneDefinitions repositoryRoot runRoot =
        [ laneDefinition
              repositoryRoot
              runRoot
              "build"
              "Build"
              "Build verification for the solution."
              Required
              { FileName = "dotnet"
                Arguments = [ "build"; "FS.GG.Rendering.slnx"; "-c"; "Release"; "--no-restore" ] }
              10.0
              (Some 2.0)
              60.0
              (Some "dotnet-build")
              (Some "solution-build-release")
              false
              None
          laneDefinition
              repositoryRoot
              runRoot
              "library-tests"
              "Library Tests"
              "Fast library and package validation not tied to one sample."
              Required
              { FileName = "dotnet"
                Arguments = [ "test"; "tests/Lib.Tests/Lib.Tests.fsproj"; "-c"; "Release"; "--no-restore" ] }
              10.0
              (Some 2.0)
              60.0
              (Some "dotnet-test")
              (Some "tests/Lib.Tests/bin/Release")
              false
              None
          laneDefinition
              repositoryRoot
              runRoot
              "package-proof"
              "Package Proof"
              "Package pin and local-feed source proof for package-consuming samples."
              Required
              { FileName = "dotnet"
                Arguments =
                    [ "fsi"
                      "scripts/refresh-local-feed-and-samples.fsx"
                      "--sample"
                      "samples/AntShowcase"
                      "--mode"
                      "proof"
                      "--isolated-cache"
                      Path.Combine(runRoot, "package-proof", "nuget-cache")
                      "--out"
                      Path.Combine(runRoot, "package-proof", "package-proof") ] }
              10.0
              (Some 2.0)
              60.0
              (Some "package-feed")
              (Some "samples/AntShowcase/package-proof")
              false
              (Some "aggregate-solution")
          laneDefinition
              repositoryRoot
              runRoot
              "controls"
              "Controls"
              "Controls package and rendering-control behavior validation."
              Required
              { FileName = "dotnet"
                Arguments =
                    [ "test"
                      "tests/Controls.Tests/Controls.Tests.fsproj"
                      "-c"
                      "Release"
                      "--no-restore"
                      "--logger"
                      "trx;LogFileName=controls.trx"
                      "--results-directory"
                      Path.Combine(runRoot, "controls", "TestResults")
                      "--blame-hang"
                      "--blame-hang-timeout"
                      "2m" ] }
              15.0
              (Some 2.0)
              60.0
              (Some "dotnet-test")
              (Some "tests/Controls.Tests/bin/Release")
              false
              (Some "aggregate-solution")
          laneDefinition
              repositoryRoot
              runRoot
              "rendering-harness"
              "Rendering Harness"
              "Rendering harness contracts, package-feed helpers, and lane runner tests."
              Required
              { FileName = "dotnet"
                Arguments =
                    [ "test"
                      "tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj"
                      "-c"
                      "Release"
                      "--no-restore"
                      "--filter"
                      "Feature166"
                      "--logger"
                      "trx;LogFileName=rendering-harness.trx"
                      "--results-directory"
                      Path.Combine(runRoot, "rendering-harness", "TestResults")
                      "--blame-hang"
                      "--blame-hang-timeout"
                      "2m" ] }
              10.0
              (Some 2.0)
              60.0
              (Some "dotnet-test")
              (Some "tests/Rendering.Harness.Tests/bin/Release")
              false
              (Some "aggregate-solution")
          laneDefinition
              repositoryRoot
              runRoot
              "antshowcase-sample"
              "AntShowcase Sample"
              "Package-consuming AntShowcase sample validation."
              Required
              { FileName = "dotnet"
                Arguments =
                    [ "test"
                      "samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj"
                      "-c"
                      "Release"
                      "--no-restore"
                      "--logger"
                      "trx;LogFileName=antshowcase-sample.trx"
                      "--results-directory"
                      Path.Combine(runRoot, "antshowcase-sample", "TestResults")
                      "--blame-hang"
                      "--blame-hang-timeout"
                      "2m" ] }
              10.0
              (Some 2.0)
              60.0
              (Some "dotnet-test")
              (Some "samples/AntShowcase/AntShowcase.Tests/bin/Release")
              false
              (Some "aggregate-solution")
          laneDefinition
              repositoryRoot
              runRoot
              "aggregate-solution"
              "Aggregate Solution"
              "Full solution validation recorded separately from focused lanes."
              Optional
              { FileName = "dotnet"
                Arguments =
                    [ "test"
                      "FS.GG.Rendering.slnx"
                      "-c"
                      "Release"
                      "--no-restore"
                      "--logger"
                      "trx;LogFileName=aggregate-solution.trx"
                      "--results-directory"
                      Path.Combine(runRoot, "aggregate-solution", "TestResults")
                      "--blame-hang"
                      "--blame-hang-timeout"
                      "3m" ] }
              20.0
              (Some 3.0)
              60.0
              (Some "aggregate")
              (Some "solution-test-release")
              true
              None ]

    let duplicateItems items =
        items
        |> List.countBy id
        |> List.choose (fun (value, count) -> if count > 1 then Some value else None)

    let diagnostic code message laneIds =
        { Code = code
          Message = message
          LaneIds = laneIds }

    let pathKey (path: string) =
        path.Replace('\\', '/').TrimEnd('/')

    let selectedLaneIdsForRequest (lanes: LaneDefinition list) (request: RunRequest) : SelectionMode * string list =
        if request.ListOnly then
            ListOnlySelection, []
        elif request.RequestedLaneIds.IsEmpty then
            let required = lanes |> List.filter (fun lane -> lane.ReadinessRole = Required) |> List.map _.Id
            RequiredSelection, required @ request.IncludeOptionalLaneIds
        else
            ExplicitSelection, request.RequestedLaneIds @ request.IncludeOptionalLaneIds

    let validateRequest
        (repositoryRoot: string)
        (lanes: LaneDefinition list)
        (request: RunRequest)
        : Result<LaneRunPlan, PreflightDiagnostic list> =
        let runId = request.RunId |> Option.defaultWith createRunId
        let runRoot = Path.Combine(request.OutDir, runId)
        let mode, selectedIds = selectedLaneIdsForRequest lanes request
        let known = lanes |> List.map _.Id |> Set.ofList
        let laneById = lanes |> List.map (fun lane -> lane.Id, lane) |> Map.ofList

        let duplicateCatalogIds =
            lanes |> List.map _.Id |> duplicateItems

        let duplicateRequestedIds =
            selectedIds |> duplicateItems

        let unknown =
            selectedIds |> List.filter (fun laneId -> not (known.Contains laneId)) |> List.distinct

        let includeOptionalErrors =
            request.IncludeOptionalLaneIds
            |> List.choose (fun laneId ->
                laneById
                |> Map.tryFind laneId
                |> Option.bind (fun lane ->
                    if lane.ReadinessRole = Optional then
                        None
                    else
                        Some lane.Id))

        let selected: LaneDefinition list =
            selectedIds
            |> List.distinct
            |> List.choose (fun laneId -> laneById |> Map.tryFind laneId)

        let duplicateResultPaths =
            selected |> List.map (fun lane -> pathKey lane.ResultPath) |> duplicateItems

        let duplicateEvidencePaths =
            selected |> List.map (fun lane -> pathKey lane.EvidenceDirectory) |> duplicateItems

        let timeoutErrors =
            selected
            |> List.filter (fun lane -> lane.Timeout <= TimeSpan.Zero || lane.ProgressInterval > TimeSpan.FromSeconds 60.0)
            |> List.map _.Id

        let unsafeSchedule =
            if request.AllowParallel then
                let groupConflicts =
                    selected
                    |> List.choose (fun lane -> lane.ConcurrencyGroup |> Option.map (fun group -> group, lane.Id))
                    |> List.groupBy fst
                    |> List.choose (fun (_, rows) ->
                        let ids = rows |> List.map snd
                        if ids.Length > 1 then Some ids else None)

                let scopeConflicts =
                    selected
                    |> List.choose (fun lane -> lane.OutputScope |> Option.map (fun scope -> scope, lane.Id))
                    |> List.groupBy fst
                    |> List.choose (fun (_, rows) ->
                        let ids = rows |> List.map snd
                        if ids.Length > 1 then Some ids else None)

                groupConflicts @ scopeConflicts
            else
                []

        let outputRootDiagnostic: PreflightDiagnostic option =
            try
                Directory.CreateDirectory request.OutDir |> ignore
                None
            with ex ->
                Some(diagnostic "output-root-unwritable" $"output root `{request.OutDir}` is not writable: {ex.Message}" [])

        let runRootDiagnostic: PreflightDiagnostic option =
            if Directory.Exists runRoot && not request.ReplaceRun then
                Some(diagnostic "run-id-exists" $"run id `{runId}` already exists under `{request.OutDir}`; pass --replace-run {runId} to replace it" [])
            else
                None

        let diagnostics =
            [ if not (Directory.Exists repositoryRoot) then
                  yield diagnostic "repository-root-missing" $"repository root `{repositoryRoot}` does not exist" []
              if not duplicateCatalogIds.IsEmpty then
                  yield diagnostic "duplicate-lane-id" "lane catalog contains duplicate lane ids" duplicateCatalogIds
              if not duplicateRequestedIds.IsEmpty then
                  yield diagnostic "duplicate-requested-lane" "request contains duplicate lane ids" duplicateRequestedIds
              if not unknown.IsEmpty then
                  yield diagnostic "unknown-lane" "request contains unknown lane ids" unknown
              if not includeOptionalErrors.IsEmpty then
                  yield diagnostic "--include-optional-role" "--include-optional accepts optional lane ids only" includeOptionalErrors
              if not duplicateResultPaths.IsEmpty then
                  yield diagnostic "duplicate-result-path" "selected lanes share result paths" duplicateResultPaths
              if not duplicateEvidencePaths.IsEmpty then
                  yield diagnostic "duplicate-evidence-path" "selected lanes share evidence directories" duplicateEvidencePaths
              if not timeoutErrors.IsEmpty then
                  yield diagnostic "invalid-time-budget" "selected lanes must have positive timeout and progress interval at most 60 seconds" timeoutErrors
              for conflict in unsafeSchedule do
                  yield diagnostic "unsafe-schedule" "parallel lane request would share a concurrency group or output scope; run sequentially or isolate outputs" conflict
              match outputRootDiagnostic with
              | Some d -> yield d
              | None -> ()
              match runRootDiagnostic with
              | Some d -> yield d
              | None -> () ]

        if diagnostics.IsEmpty then
            let replacement =
                if request.ReplaceRun && Directory.Exists runRoot then
                    Some $"Run `{runId}` replaced existing evidence at `{runRoot}`."
                else
                    None

            Ok
                { Request = request
                  RunId = runId
                  SelectionMode = mode
                  ArtifactRoot = runRoot
                  SelectedLanes = selected
                  Diagnostics = []
                  ReplacementNotice = replacement }
        else
            Error diagnostics

    let requiredResult result =
        result.ReadinessRole = Required

    let computeOverallReadiness results =
        let required = results |> List.filter requiredResult

        if required.IsEmpty then
            Ready
        elif
            required
            |> List.exists (fun result ->
                match result.Status with
                | Failed
                | TimedOut
                | NoProgressTimedOut
                | Canceled
                | InfrastructureError
                | EnvironmentLimited when result.AcceptedEnvironmentLimitation.IsNone -> true
                | _ -> false)
        then
            Blocked
        elif
            required
            |> List.exists (fun result ->
                result.Status = EnvironmentLimited
                && result.AcceptedEnvironmentLimitation.IsSome)
        then
            EnvironmentLimitedReadiness
        elif
            required
            |> List.exists (fun result ->
                match result.Status with
                | Skipped
                | NotRun -> true
                | _ -> false)
        then
            Incomplete
        elif required |> List.forall (fun result -> result.Status = Passed) then
            Ready
        else
            Incomplete

    let firstBlockingRequiredLane results =
        results
        |> List.tryFind (fun result -> result.ReadinessRole = Required && result.Status <> Passed)
        |> Option.map _.LaneId

    let init lanes =
        { LaneDefinitions = lanes
          RunPlan = None
          ActiveLaneId = None
          PendingLaneIds = []
          CompletedResults = []
          CanceledLaneIds = []
          Summary = None
          Diagnostics = [] },
        [ RegisterCancelHandler ]

    let update msg model =
        match msg with
        | RunRequested request -> model, [ ValidateRequest request ]
        | PreflightPassed plan ->
            { model with
                RunPlan = Some plan
                PendingLaneIds = plan.SelectedLanes |> List.map _.Id },
            [ CreateRunRoot plan.ArtifactRoot ]
        | PreflightFailed diagnostics ->
            { model with Diagnostics = diagnostics |> List.map _.Message },
            []
        | LaneStarted (laneId, _) ->
            { model with
                ActiveLaneId = Some laneId
                PendingLaneIds = model.PendingLaneIds |> List.filter ((<>) laneId) },
            [ PollProcess laneId ]
        | LaneOutputReceived (laneId, output, _) -> model, [ AppendLaneLog(laneId, output); PollProcess laneId ]
        | LaneHeartbeatDue (laneId, _) -> model, [ PublishHeartbeat laneId; PollProcess laneId ]
        | LaneCompleted result ->
            { model with
                ActiveLaneId = None
                CompletedResults = model.CompletedResults @ [ result ] },
            [ WriteLaneResult result.LaneId ]
        | LaneTimedOut (laneId, reason) ->
            { model with Diagnostics = model.Diagnostics @ [ reason ] },
            [ StopProcess laneId; WriteLaneResult laneId ]
        | LaneNoProgressTimedOut (laneId, reason) ->
            { model with Diagnostics = model.Diagnostics @ [ reason ] },
            [ StopProcess laneId; WriteLaneResult laneId ]
        | InfrastructureErrorRaised (laneId, reason) ->
            let laneDiagnostics =
                match laneId with
                | Some id -> $"{id}: {reason}"
                | None -> reason

            { model with Diagnostics = model.Diagnostics @ [ laneDiagnostics ] },
            [ WriteSummary ]
        | OperatorCanceled reason ->
            let cancelEffects =
                match model.ActiveLaneId with
                | Some laneId -> [ StopProcess laneId ]
                | None -> []

            { model with
                Diagnostics = model.Diagnostics @ [ reason ]
                CanceledLaneIds =
                    model.CanceledLaneIds
                    @ (model.ActiveLaneId |> Option.toList)
                    @ model.PendingLaneIds
                PendingLaneIds = []
                ActiveLaneId = None },
            cancelEffects @ [ WriteSummary ]
        | LaneCanceled (laneId, reason) ->
            { model with
                CanceledLaneIds = model.CanceledLaneIds @ [ laneId ]
                Diagnostics = model.Diagnostics @ [ reason ] },
            [ StopProcess laneId; WriteLaneResult laneId ]
        | SummaryRequested -> model, [ WriteSummary ]
        | SummaryWritten (markdownPath, jsonPath) ->
            { model with Diagnostics = model.Diagnostics @ [ markdownPath; jsonPath ] },
            []

    let ensureParentDirectory (path: string) =
        match Path.GetDirectoryName path with
        | null
        | "" -> ()
        | directory -> Directory.CreateDirectory directory |> ignore

    let writeAllText (path: string) (text: string) =
        ensureParentDirectory path
        File.WriteAllText(path, text)

    let resultForLane
        (lane: LaneDefinition)
        (status: LaneStatus)
        (started: DateTime option)
        (completed: DateTime option)
        (elapsed: TimeSpan option)
        (lastActivityUtc: DateTime option)
        (lastActivityText: string option)
        (exitCode: int option)
        (reason: string option)
        (diagnostics: string list)
        (caveats: string list)
        : LaneResult =
        { LaneId = lane.Id
          ReadinessRole = lane.ReadinessRole
          Status = status
          Command = commandText lane.Command
          StartedUtc = started
          CompletedUtc = completed
          Elapsed = elapsed
          TimeoutBudget = Some lane.Timeout
          LastActivityUtc = lastActivityUtc
          LastActivityText = lastActivityText
          ExitCode = exitCode
          LogPath = lane.LogPath
          ResultPath = lane.ResultPath
          DiagnosticsPath = lane.DiagnosticsPath
          ResultArtifacts = [ lane.ResultPath; lane.LogPath; lane.DiagnosticsPath ]
          Reason = reason
          Diagnostics = diagnostics
          Caveats = caveats
          AcceptedEnvironmentLimitation = None
          Substitution = lane.SubstitutesFor
          IsAggregate = lane.IsAggregate }

    let laneResultArtifacts (lane: LaneDefinition) =
        if Directory.Exists lane.EvidenceDirectory then
            Directory.GetFiles(lane.EvidenceDirectory, "*.trx", SearchOption.AllDirectories)
            |> Array.append (Directory.GetFiles(lane.EvidenceDirectory, "*Sequence.xml", SearchOption.AllDirectories))
            |> Array.toList
        else
            []

    let withDiscoveredArtifacts (lane: LaneDefinition) (result: LaneResult) =
        let discovered = laneResultArtifacts lane
        { result with ResultArtifacts = (result.ResultArtifacts @ discovered) |> List.distinct }

    let jsonString (value: string) =
        JsonSerializer.Serialize(value)

    let jsonStringOption value =
        value |> Option.map jsonString |> Option.defaultValue "null"

    let jsonDate value =
        value
        |> Option.map (fun (date: DateTime) -> jsonString (date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)))
        |> Option.defaultValue "null"

    let jsonTimeSpan value =
        value
        |> Option.map (fun (span: TimeSpan) -> jsonString (span.ToString()))
        |> Option.defaultValue "null"

    let jsonStringArray values =
        values |> List.map jsonString |> String.concat "," |> sprintf "[%s]"

    let boolString value =
        if value then "true" else "false"

    let writeDiagnostics (result: LaneResult) =
        let reason = result.Reason |> Option.defaultValue "none"
        let lastActivity = result.LastActivityText |> Option.defaultValue "none"

        let lines =
            [ $"# Lane Diagnostics: {result.LaneId}"
              ""
              $"- Status: `{statusToken result.Status}`"
              $"- Reason: `{reason}`"
              $"- Last activity: `{lastActivity}`"
              ""
              "## Diagnostics" ]
            @ (if result.Diagnostics.IsEmpty then [ "- None." ] else result.Diagnostics |> List.map (fun d -> "- " + d))
            @ [ ""
                "## Caveats" ]
            @ (if result.Caveats.IsEmpty then [ "- None." ] else result.Caveats |> List.map (fun d -> "- " + d))

        writeAllText result.DiagnosticsPath (String.concat Environment.NewLine lines + Environment.NewLine)

    let renderLaneResultJson (result: LaneResult) =
        "{"
        + String.concat
            ","
            [ "\"laneId\":" + jsonString result.LaneId
              "\"readinessRole\":" + jsonString (roleToken result.ReadinessRole)
              "\"status\":" + jsonString (statusToken result.Status)
              "\"command\":" + jsonString result.Command
              "\"startedUtc\":" + jsonDate result.StartedUtc
              "\"completedUtc\":" + jsonDate result.CompletedUtc
              "\"elapsed\":" + jsonTimeSpan result.Elapsed
              "\"timeoutBudget\":" + jsonTimeSpan result.TimeoutBudget
              "\"lastActivityUtc\":" + jsonDate result.LastActivityUtc
              "\"lastActivityText\":" + jsonStringOption result.LastActivityText
              "\"exitCode\":" + (result.ExitCode |> Option.map string |> Option.defaultValue "null")
              "\"logPath\":" + jsonString result.LogPath
              "\"resultPath\":" + jsonString result.ResultPath
              "\"diagnosticsPath\":" + jsonString result.DiagnosticsPath
              "\"artifacts\":" + jsonStringArray result.ResultArtifacts
              "\"reason\":" + jsonStringOption result.Reason
              "\"diagnostics\":" + jsonStringArray result.Diagnostics
              "\"caveats\":" + jsonStringArray result.Caveats
              "\"acceptedEnvironmentLimitation\":" + jsonStringOption result.AcceptedEnvironmentLimitation
              "\"substitution\":" + jsonStringOption result.Substitution
              "\"isAggregate\":" + boolString result.IsAggregate
              "\"required\":" + boolString (result.ReadinessRole = Required) ]
        + "}"

    let writeLaneResult (result: LaneResult) =
        writeDiagnostics result
        writeAllText result.ResultPath (renderLaneResultJson result + Environment.NewLine)

    let runLane (lane: LaneDefinition) : LaneResult =
        let started = DateTime.UtcNow
        let output = StringBuilder()
        let gate = obj()
        let mutable lastActivityUtc = started
        let mutable lastActivityText = "process starting"

        let appendLine (line: string) =
            lock gate (fun () ->
                output.AppendLine(line) |> ignore
                lastActivityUtc <- DateTime.UtcNow
                lastActivityText <- line)

        try
            Directory.CreateDirectory lane.EvidenceDirectory |> ignore
            Directory.CreateDirectory lane.OutputRoot |> ignore
            writeAllText lane.LogPath ""

            let psi = ProcessStartInfo(lane.Command.FileName)
            psi.WorkingDirectory <- lane.WorkingDirectory
            psi.UseShellExecute <- false
            psi.RedirectStandardOutput <- true
            psi.RedirectStandardError <- true

            for argument in lane.Command.Arguments do
                psi.ArgumentList.Add argument

            use proc = new Process()
            proc.StartInfo <- psi
            proc.OutputDataReceived.Add(fun args ->
                match args.Data with
                | null -> ()
                | line -> appendLine line)
            proc.ErrorDataReceived.Add(fun args ->
                match args.Data with
                | null -> ()
                | line -> appendLine line)

            if not (proc.Start()) then
                let completed = DateTime.UtcNow
                let result =
                    resultForLane
                        lane
                        InfrastructureError
                        (Some started)
                        (Some completed)
                        (Some(completed - started))
                        (Some lastActivityUtc)
                        (Some lastActivityText)
                        None
                        (Some "process did not start")
                        [ "process did not start" ]
                        []
                    |> withDiscoveredArtifacts lane

                writeLaneResult result
                result
            else
                proc.BeginOutputReadLine()
                proc.BeginErrorReadLine()
                let mutable finalStatus = NotRun
                let mutable exitCode = None
                let mutable reason = None
                let mutable diagnostics = []
                let mutable nextHeartbeatUtc = started + lane.ProgressInterval

                while finalStatus = NotRun do
                    if proc.WaitForExit(200) then
                        proc.WaitForExit()
                        exitCode <- Some proc.ExitCode
                        finalStatus <- if proc.ExitCode = 0 then Passed else Failed
                        if proc.ExitCode <> 0 then
                            reason <- Some $"lane exited with code {proc.ExitCode}"
                    else
                        let now = DateTime.UtcNow

                        if now >= nextHeartbeatUtc then
                            let heartbeat =
                                lock gate (fun () ->
                                    $"heartbeat lane={lane.Id} elapsed={now - started} timeout={lane.Timeout} lastActivity={lastActivityText}")

                            printfn "%s" heartbeat
                            nextHeartbeatUtc <- now + lane.ProgressInterval

                        if now - started > lane.Timeout then
                            finalStatus <- TimedOut
                            reason <- Some $"lane exceeded timeout {lane.Timeout}"
                            diagnostics <- diagnostics @ [ reason.Value ]

                            try
                                proc.Kill(true)
                            with ex ->
                                diagnostics <- diagnostics @ [ $"failed to stop timed-out process: {ex.Message}" ]
                        else
                            match lane.NoProgressTimeout with
                            | Some noProgress when now - lastActivityUtc > noProgress ->
                                finalStatus <- NoProgressTimedOut
                                reason <- Some $"lane exceeded no-progress timeout {noProgress}"
                                diagnostics <- diagnostics @ [ reason.Value ]

                                try
                                    proc.Kill(true)
                                with ex ->
                                    diagnostics <- diagnostics @ [ $"failed to stop no-progress process: {ex.Message}" ]
                            | _ -> ()

                let completed = DateTime.UtcNow
                let text, activityUtc, activityText =
                    lock gate (fun () -> output.ToString(), lastActivityUtc, lastActivityText)

                writeAllText lane.LogPath text

                let result =
                    resultForLane
                        lane
                        finalStatus
                        (Some started)
                        (Some completed)
                        (Some(completed - started))
                        (Some activityUtc)
                        (Some activityText)
                        exitCode
                        reason
                        diagnostics
                        []
                    |> withDiscoveredArtifacts lane

                writeLaneResult result
                result
        with ex ->
            let completed = DateTime.UtcNow

            let result =
                resultForLane
                    lane
                    InfrastructureError
                    (Some started)
                    (Some completed)
                    (Some(completed - started))
                    (Some lastActivityUtc)
                    (Some lastActivityText)
                    None
                    (Some ex.Message)
                    [ ex.Message ]
                    []
                |> withDiscoveredArtifacts lane

            try
                writeLaneResult result
            with _ ->
                ()

            result

    let elapsedText result =
        result.Elapsed
        |> Option.map (fun elapsed -> elapsed.ToString())
        |> Option.defaultValue "n/a"

    let markdownTableRows results =
        results
        |> List.map (fun result ->
            let reason = result.Reason |> Option.defaultValue ""
            $"| `{result.LaneId}` | `{roleToken result.ReadinessRole}` | `{statusToken result.Status}` | `{elapsedText result}` | `{result.LogPath}` | {reason} |")

    let renderSummaryMarkdown (summary: ValidationSummary) =
        let required = summary.LaneResults |> List.filter (fun result -> result.ReadinessRole = Required)

        let optional =
            summary.LaneResults
            |> List.filter (fun result -> result.ReadinessRole = Optional || result.ReadinessRole = Informational)

        let aggregateStatus =
            summary.LaneResults
            |> List.tryFind _.IsAggregate
            |> Option.map (fun result -> statusToken result.Status)
            |> Option.defaultValue "not-selected"

        let substitutionRows =
            summary.LaneResults
            |> List.choose (fun result -> result.Substitution |> Option.map (fun target -> $"- `{result.LaneId}` substitutes for `{target}`"))

        let firstBlockingRequiredLane = summary.FirstBlockingRequiredLane |> Option.defaultValue "none"
        let summaryJsonPath = Path.Combine(summary.ArtifactRoot, "summary.json")

        String.concat
            Environment.NewLine
            ([ "# Validation Lanes Summary"
               ""
               $"- Run id: `{summary.RunId}`"
               $"- Overall readiness: `{readinessToken summary.OverallReadiness}`"
               $"- First blocking required lane: `{firstBlockingRequiredLane}`"
               $"- Aggregate status: `{aggregateStatus}`"
               $"- Artifact root: `{summary.ArtifactRoot}`"
               $"- Summary JSON: `{summaryJsonPath}`" ]
             @ (summary.ReplacementNotice |> Option.map (fun notice -> [ $"- Replacement notice: {notice}" ]) |> Option.defaultValue [])
             @ [ ""
                 "## Required Lanes"
                 ""
                 "| Lane | Role | Status | Elapsed | Log | Reason |"
                 "|------|------|--------|---------|-----|--------|" ]
             @ markdownTableRows required
             @ [ ""
                 "## Optional and Informational Lanes"
                 ""
                 "| Lane | Role | Status | Elapsed | Log | Reason |"
                 "|------|------|--------|---------|-----|--------|" ]
             @ (if optional.IsEmpty then [ "| none |  |  |  |  |  |" ] else markdownTableRows optional)
             @ [ ""
                 "## Substitutions"
                 "" ]
             @ (if substitutionRows.IsEmpty then [ "- None." ] else substitutionRows)
             @ [ ""
                 "## Caveats"
                 "" ]
             @ (if summary.Caveats.IsEmpty then [ "- None." ] else summary.Caveats |> List.map (fun c -> "- " + c)))
        + Environment.NewLine

    let renderSummaryJson (summary: ValidationSummary) =
        let lanes = summary.LaneResults |> List.map renderLaneResultJson |> String.concat ","

        "{"
        + String.concat
            ","
            [ "\"runId\":" + jsonString summary.RunId
              "\"policyVersion\":" + jsonString summary.PolicyVersion
              "\"overallReadiness\":" + jsonString (readinessToken summary.OverallReadiness)
              "\"artifactRoot\":" + jsonString summary.ArtifactRoot
              "\"startedUtc\":" + jsonString (summary.StartedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))
              "\"completedUtc\":" + jsonString (summary.CompletedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))
              "\"firstBlockingRequiredLane\":" + jsonStringOption summary.FirstBlockingRequiredLane
              "\"lanes\":[" + lanes + "]"
              "\"caveats\":" + jsonStringArray summary.Caveats
              "\"replacementNotice\":" + jsonStringOption summary.ReplacementNotice ]
        + "}"

    let writeSummary (runRoot: string) (summary: ValidationSummary) =
        Directory.CreateDirectory runRoot |> ignore

        for result in summary.LaneResults do
            writeLaneResult result

        match summary.ReplacementNotice with
        | Some notice -> writeAllText (Path.Combine(runRoot, "replacement-notice.md")) ("# Replacement Notice" + Environment.NewLine + Environment.NewLine + notice + Environment.NewLine)
        | None -> ()

        let markdown = Path.Combine(runRoot, "summary.md")
        let json = Path.Combine(runRoot, "summary.json")
        writeAllText markdown (renderSummaryMarkdown summary)
        writeAllText json (renderSummaryJson summary + Environment.NewLine)
        [ markdown; json ]

    let runRequest (repositoryRoot: string) (request: RunRequest) : Result<ValidationSummary, PreflightDiagnostic list> =
        let seedRunId = request.RunId |> Option.defaultWith createRunId
        let seedRoot = Path.Combine(request.OutDir, seedRunId)
        let seedCatalog = defaultLaneDefinitions repositoryRoot seedRoot
        let request = { request with RunId = Some seedRunId }

        match validateRequest repositoryRoot seedCatalog request with
        | Error diagnostics -> Error diagnostics
        | Ok plan ->
            if request.ReplaceRun && Directory.Exists plan.ArtifactRoot then
                Directory.Delete(plan.ArtifactRoot, true)

            Directory.CreateDirectory plan.ArtifactRoot |> ignore
            let started = DateTime.UtcNow
            let results: LaneResult list = plan.SelectedLanes |> List.map runLane
            let completed = DateTime.UtcNow

            let omittedAggregate =
                seedCatalog
                |> List.tryFind (fun lane -> lane.IsAggregate)
                |> Option.bind (fun aggregate ->
                    if results |> List.exists (fun result -> result.LaneId = aggregate.Id) then
                        None
                    else
                        Some $"{aggregate.Id} was not selected; required readiness is based on focused lanes.")

            let caveats =
                [ match omittedAggregate with
                  | Some caveat -> yield caveat
                  | None -> ()
                  if results |> List.exists (fun result -> result.Status <> Passed) then
                      yield "non-passing lanes are not counted as green"
                  for result in results do
                      match result.Substitution with
                      | Some target -> yield $"{result.LaneId} is a targeted substitute for {target}"
                      | None -> () ]

            let summary =
                { RunId = plan.RunId
                  PolicyVersion = policyVersion
                  OverallReadiness = computeOverallReadiness results
                  ArtifactRoot = plan.ArtifactRoot
                  StartedUtc = started
                  CompletedUtc = completed
                  FirstBlockingRequiredLane = firstBlockingRequiredLane results
                  LaneResults = results
                  Caveats = caveats
                  ReplacementNotice = plan.ReplacementNotice }

            writeSummary plan.ArtifactRoot summary |> ignore
            Ok summary

    let runLanes (repositoryRoot: string) (outDir: string) (selectedLaneIds: string list) : ValidationSummary =
        let request =
            { defaultRunRequest outDir with
                RequestedLaneIds = selectedLaneIds }

        match runRequest repositoryRoot request with
        | Ok summary -> summary
        | Error diagnostics ->
            let message = diagnostics |> List.map _.Message |> String.concat "; "
            invalidOp message
