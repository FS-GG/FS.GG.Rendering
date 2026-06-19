namespace Rendering.Harness

open System
open System.Diagnostics
open System.Globalization
open System.IO
open System.Text
open System.Text.Json

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

    let statusToken status =
        match status with
        | Passed -> "passed"
        | Failed -> "failed"
        | TimedOut -> "timed-out"
        | Hung -> "hung"
        | Skipped -> "skipped"
        | Canceled -> "canceled"
        | NotRun -> "not-run"
        | EnvironmentLimited -> "environment-limited"

    let readinessToken readiness =
        match readiness with
        | Ready -> "ready"
        | Blocked -> "blocked"
        | Incomplete -> "incomplete"
        | EnvironmentLimitedReadiness -> "environment-limited"

    let quoteArg (arg: string) =
        if arg.Contains(' ') then "\"" + arg.Replace("\"", "\\\"") + "\"" else arg

    let commandText (command: LaneCommand) =
        String.concat " " (command.FileName :: (command.Arguments |> List.map quoteArg))

    let laneDirectory (outDir: string) (laneId: string) =
        Path.Combine(outDir, laneId)

    let laneDefinition
        (repositoryRoot: string)
        (outDir: string)
        (laneId: string)
        (description: string)
        (command: LaneCommand)
        (required: bool)
        (timeoutMinutes: float)
        (noProgressMinutes: float option)
        (concurrencyGroup: string option)
        : LaneDefinition =
        let laneDir = laneDirectory outDir laneId

        { Id = laneId
          Description = description
          Command = command
          WorkingDirectory = repositoryRoot
          Required = required
          Timeout = TimeSpan.FromMinutes timeoutMinutes
          NoProgressTimeout = noProgressMinutes |> Option.map TimeSpan.FromMinutes
          LogPath = Path.Combine(laneDir, "log.txt")
          ResultPath = Path.Combine(laneDir, "result.json")
          DiagnosticsPath = Path.Combine(laneDir, "diagnostics.md")
          OutputRoot = Path.Combine(laneDir, "out")
          ConcurrencyGroup = concurrencyGroup }

    let defaultLaneDefinitions repositoryRoot outDir =
        [ laneDefinition
              repositoryRoot
              outDir
              "package-proof"
              "Package pin and local-feed source proof for AntShowcase."
              { FileName = "dotnet"
                Arguments =
                    [ "fsi"
                      "scripts/refresh-local-feed-and-samples.fsx"
                      "--sample"
                      "samples/AntShowcase"
                      "--mode"
                      "proof"
                      "--isolated-cache"
                      Path.Combine(outDir, "package-proof", "nuget-cache")
                      "--out"
                      "specs/163-package-feed-validation-lanes/readiness/package-proof" ] }
              true
              10.0
              (Some 2.0)
              (Some "package")
          laneDefinition
              repositoryRoot
              outDir
              "antshowcase-sample"
              "Build and test the selected package-consuming AntShowcase sample."
              { FileName = "dotnet"
                Arguments =
                    [ "test"
                      "samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj"
                      "--no-restore"
                      "--logger"
                      "trx;LogFileName=antshowcase-sample.trx"
                      "--results-directory"
                      Path.Combine(outDir, "antshowcase-sample", "TestResults")
                      "--blame-hang"
                      "--blame-hang-timeout"
                      "2m" ] }
              true
              10.0
              (Some 2.0)
              (Some "dotnet-test")
          laneDefinition
              repositoryRoot
              outDir
              "controls"
              "Focused package validation tests covering source-controlled package evidence."
              { FileName = "dotnet"
                Arguments =
                    [ "test"
                      "tests/Package.Tests/Package.Tests.fsproj"
                      "--no-restore"
                      "--filter"
                      "Feature163"
                      "--logger"
                      "trx;LogFileName=controls.trx"
                      "--results-directory"
                      Path.Combine(outDir, "controls", "TestResults")
                      "--blame-hang"
                      "--blame-hang-timeout"
                      "2m" ] }
              true
              5.0
              (Some 2.0)
              (Some "dotnet-test")
          laneDefinition
              repositoryRoot
              outDir
              "rendering-harness"
              "Focused rendering harness tests for package-feed and lane contracts."
              { FileName = "dotnet"
                Arguments =
                    [ "test"
                      "tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj"
                      "--no-restore"
                      "--filter"
                      "Feature163"
                      "--logger"
                      "trx;LogFileName=rendering-harness.trx"
                      "--results-directory"
                      Path.Combine(outDir, "rendering-harness", "TestResults")
                      "--blame-hang"
                      "--blame-hang-timeout"
                      "2m" ] }
              true
              5.0
              (Some 2.0)
              (Some "dotnet-test")
          laneDefinition
              repositoryRoot
              outDir
              "aggregate-solution"
              "Full solution validation recorded separately from focused lanes."
              { FileName = "dotnet"
                Arguments =
                    [ "test"
                      "FS.GG.Rendering.slnx"
                      "--no-restore"
                      "--logger"
                      "trx;LogFileName=aggregate-solution.trx"
                      "--results-directory"
                      Path.Combine(outDir, "aggregate-solution", "TestResults")
                      "--blame-hang"
                      "--blame-hang-timeout"
                      "3m" ] }
              false
              15.0
              (Some 3.0)
              (Some "aggregate") ]

    let computeOverallReadiness results =
        let required = results |> List.filter _.Required

        if required |> List.exists (fun result -> result.Status = EnvironmentLimited) then
            EnvironmentLimitedReadiness
        elif
            required
            |> List.exists (fun result ->
                match result.Status with
                | Failed
                | TimedOut
                | Hung
                | Canceled -> true
                | _ -> false)
        then
            Blocked
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

    let init lanes =
        { LaneDefinitions = lanes
          RunningLaneIds = []
          CompletedResults = []
          CanceledLaneIds = []
          Summary = None
          Diagnostics = [] },
        lanes |> List.map (fun lane -> CreateOutputRoot lane.Id)

    let update msg model =
        match msg with
        | RunRequested laneIds -> model, laneIds |> List.map StartProcess
        | LaneStarted (laneId, _) -> { model with RunningLaneIds = model.RunningLaneIds @ [ laneId ] }, [ PollProcess laneId ]
        | LaneOutputReceived (laneId, output, _) -> model, [ AppendLaneLog(laneId, output); PollProcess laneId ]
        | LaneCompleted result ->
            { model with
                RunningLaneIds = model.RunningLaneIds |> List.filter ((<>) result.LaneId)
                CompletedResults = model.CompletedResults @ [ result ] },
            [ WriteLaneResult result.LaneId ]
        | LaneTimedOut (laneId, reason) -> { model with Diagnostics = model.Diagnostics @ [ reason ] }, [ StopProcess laneId; CaptureDiagnostics laneId ]
        | LaneNoProgressDetected (laneId, reason) -> { model with Diagnostics = model.Diagnostics @ [ reason ] }, [ StopProcess laneId; CaptureDiagnostics laneId ]
        | LaneCanceled (laneId, reason) ->
            { model with
                CanceledLaneIds = model.CanceledLaneIds @ [ laneId ]
                Diagnostics = model.Diagnostics @ [ reason ] },
            [ StopProcess laneId; WriteLaneResult laneId ]
        | LaneDiagnosticsCaptured (_, path) -> { model with Diagnostics = model.Diagnostics @ [ path ] }, []
        | SummaryRequested -> model, [ WriteLaneSummary ]
        | SummaryWritten path -> { model with Diagnostics = model.Diagnostics @ [ path ] }, []
        | RunnerFailed reason -> { model with Diagnostics = model.Diagnostics @ [ reason ] }, []

    let ensureParentDirectory (path: string) =
        match Path.GetDirectoryName path with
        | null
        | "" -> ()
        | directory -> Directory.CreateDirectory directory |> ignore

    let writeAllText (path: string) (text: string) =
        ensureParentDirectory path
        File.WriteAllText(path, text)

    let emptyResult
        (lane: LaneDefinition)
        (status: LaneStatus)
        (started: DateTime option)
        (completed: DateTime option)
        (elapsed: TimeSpan option)
        (exitCode: int option)
        (diagnostics: string list)
        (caveats: string list)
        : LaneResult =
        { LaneId = lane.Id
          Status = status
          Command = commandText lane.Command
          StartedUtc = started
          CompletedUtc = completed
          Elapsed = elapsed
          ExitCode = exitCode
          LogPath = lane.LogPath
          ResultArtifacts = [ lane.ResultPath; lane.LogPath ]
          Diagnostics = diagnostics
          Caveats = caveats
          AcceptedException = None
          Required = lane.Required }

    let laneResultArtifacts (lane: LaneDefinition) =
        let laneDir =
            match Path.GetDirectoryName lane.ResultPath with
            | null -> "."
            | directory -> directory

        if Directory.Exists laneDir then
            Directory.GetFiles(laneDir, "*.trx", SearchOption.AllDirectories)
            |> Array.append (Directory.GetFiles(laneDir, "*Sequence.xml", SearchOption.AllDirectories))
            |> Array.toList
        else
            []

    let withDiscoveredArtifacts (lane: LaneDefinition) (result: LaneResult) =
        let discovered = laneResultArtifacts lane
        { result with ResultArtifacts = result.ResultArtifacts @ discovered }

    let runLane (lane: LaneDefinition) : LaneResult =
        ensureParentDirectory lane.LogPath
        Directory.CreateDirectory lane.OutputRoot |> ignore
        let output = StringBuilder()
        let started = DateTime.UtcNow

        let append (line: string) =
            lock output (fun () -> output.AppendLine(line) |> ignore)

        let psi = ProcessStartInfo(lane.Command.FileName)
        psi.WorkingDirectory <- lane.WorkingDirectory
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true

        for argument in lane.Command.Arguments do
            psi.ArgumentList.Add argument

        try
            use proc = new Process()
            proc.StartInfo <- psi
            proc.OutputDataReceived.Add(fun args ->
                match args.Data with
                | null -> ()
                | line -> append line)
            proc.ErrorDataReceived.Add(fun args ->
                match args.Data with
                | null -> ()
                | line -> append line)

            if not (proc.Start()) then
                let completed = DateTime.UtcNow
                let result =
                    emptyResult lane EnvironmentLimited (Some started) (Some completed) (Some(completed - started)) None [ "process did not start" ] [ "environment-limited" ]
                    |> withDiscoveredArtifacts lane

                writeAllText lane.LogPath (lock output (fun () -> output.ToString()))
                result
            else
                proc.BeginOutputReadLine()
                proc.BeginErrorReadLine()
                let mutable lastProgressUtc = DateTime.UtcNow
                let mutable lastLength = 0
                let mutable finalStatus = NotRun
                let mutable exitCode = None
                let mutable diagnostics = []

                while finalStatus = NotRun do
                    if proc.WaitForExit(200) then
                        proc.WaitForExit()
                        exitCode <- Some proc.ExitCode
                        finalStatus <- if proc.ExitCode = 0 then Passed else Failed
                    else
                        let now = DateTime.UtcNow
                        let length = lock output (fun () -> output.Length)
                        if length <> lastLength then
                            lastLength <- length
                            lastProgressUtc <- now

                        if now - started > lane.Timeout then
                            finalStatus <- TimedOut
                            diagnostics <- diagnostics @ [ $"lane `{lane.Id}` exceeded timeout {lane.Timeout}" ]
                            try
                                proc.Kill(true)
                            with _ ->
                                ()
                        else
                            match lane.NoProgressTimeout with
                            | Some noProgress when now - lastProgressUtc > noProgress ->
                                finalStatus <- Hung
                                diagnostics <- diagnostics @ [ $"lane `{lane.Id}` exceeded no-progress timeout {noProgress}" ]
                                try
                                    proc.Kill(true)
                                with _ ->
                                    ()
                            | _ -> ()

                let completed = DateTime.UtcNow
                let text = lock output (fun () -> output.ToString())
                writeAllText lane.LogPath text
                let result =
                    emptyResult lane finalStatus (Some started) (Some completed) (Some(completed - started)) exitCode diagnostics []
                    |> withDiscoveredArtifacts lane

                result
        with ex ->
            let completed = DateTime.UtcNow
            writeAllText lane.LogPath (lock output (fun () -> output.ToString()))
            emptyResult lane EnvironmentLimited (Some started) (Some completed) (Some(completed - started)) None [ ex.Message ] [ "environment-limited" ]
            |> withDiscoveredArtifacts lane

    let jsonString value = JsonSerializer.Serialize(value)

    let jsonDate value =
        value
        |> Option.map (fun (date: DateTime) -> jsonString (date.ToString("O", CultureInfo.InvariantCulture)))
        |> Option.defaultValue "null"

    let jsonTimeSpan value =
        value
        |> Option.map (fun (span: TimeSpan) -> jsonString (span.ToString()))
        |> Option.defaultValue "null"

    let writeLaneResult (result: LaneResult) =
        let diagnostics = result.Diagnostics |> List.map jsonString |> String.concat ","
        let artifacts = result.ResultArtifacts |> List.map jsonString |> String.concat ","
        let caveats = result.Caveats |> List.map jsonString |> String.concat ","
        let exitCode = result.ExitCode |> Option.map string |> Option.defaultValue "null"
        let accepted = result.AcceptedException |> Option.map jsonString |> Option.defaultValue "null"

        let json =
            "{"
            + String.concat
                ","
                [ "\"laneId\":" + jsonString result.LaneId
                  "\"status\":" + jsonString (statusToken result.Status)
                  "\"command\":" + jsonString result.Command
                  "\"startedUtc\":" + jsonDate result.StartedUtc
                  "\"completedUtc\":" + jsonDate result.CompletedUtc
                  "\"elapsed\":" + jsonTimeSpan result.Elapsed
                  "\"exitCode\":" + exitCode
                  "\"logPath\":" + jsonString result.LogPath
                  "\"resultArtifacts\":[" + artifacts + "]"
                  "\"diagnostics\":[" + diagnostics + "]"
                  "\"caveats\":[" + caveats + "]"
                  "\"acceptedException\":" + accepted ]
            + "}"

        writeAllText (result.ResultArtifacts |> List.head) (json + Environment.NewLine)

    let renderSummaryMarkdown (summary: ValidationSummary) =
        let laneRows =
            summary.LaneResults
            |> List.map (fun result ->
                let diagnostics = String.concat "; " result.Diagnostics
                $"| `{result.LaneId}` | `{statusToken result.Status}` | `{result.Required}` | `{result.LogPath}` | `{diagnostics}` |")

        let packageCachePath = summary.PackageCachePath |> Option.defaultValue "not-recorded"

        String.concat
            Environment.NewLine
            ([ "# Validation Lanes Summary"
               ""
               $"- Overall readiness: `{readinessToken summary.OverallReadiness}`"
               $"- Local feed: `{summary.LocalFeedPath}`"
               $"- Package cache: `{packageCachePath}`"
               $"- Artifact root: `{summary.ArtifactRoot}`"
               ""
               "## Source Rules"
               "" ]
             @ (summary.SourceRules |> List.map (fun rule -> "- " + rule))
             @ [ ""
                 "## Lane Status"
                 ""
                 "| Lane | Status | Required | Log | Diagnostics |"
                 "|------|--------|----------|-----|-------------|" ]
             @ laneRows
             @ [ ""
                 "## Caveats"
                 "" ]
             @ (if summary.Caveats.IsEmpty then [ "- None." ] else summary.Caveats |> List.map (fun c -> "- " + c)))
        + Environment.NewLine

    let renderSummaryJson (summary: ValidationSummary) =
        let lanes =
            summary.LaneResults
            |> List.map (fun result ->
                let diagnostics = result.Diagnostics |> List.map jsonString |> String.concat ","
                $"{{\"laneId\":{jsonString result.LaneId},\"status\":{jsonString (statusToken result.Status)},\"required\":{result.Required.ToString().ToLowerInvariant()},\"logPath\":{jsonString result.LogPath},\"diagnostics\":[{diagnostics}]}}")
            |> String.concat ","

        let sourceRules = summary.SourceRules |> List.map jsonString |> String.concat ","
        let samples = summary.SelectedSamples |> List.map jsonString |> String.concat ","
        let caveats = summary.Caveats |> List.map jsonString |> String.concat ","

        "{"
        + String.concat
            ","
            [ "\"overallReadiness\":" + jsonString (readinessToken summary.OverallReadiness)
              "\"packageProofStatus\":"
              + (summary.PackageProofStatus |> Option.map (statusToken >> jsonString) |> Option.defaultValue "null")
              "\"selectedSamples\":[" + samples + "]"
              "\"localFeedPath\":" + jsonString summary.LocalFeedPath
              "\"packageCachePath\":"
              + (summary.PackageCachePath |> Option.map jsonString |> Option.defaultValue "null")
              "\"sourceRules\":[" + sourceRules + "]"
              "\"laneResults\":[" + lanes + "]"
              "\"caveats\":[" + caveats + "]"
              "\"artifactRoot\":" + jsonString summary.ArtifactRoot ]
        + "}"

    let writeSummary (outDir: string) (summary: ValidationSummary) =
        Directory.CreateDirectory outDir |> ignore

        for result in summary.LaneResults do
            writeLaneResult result

        let markdown = Path.Combine(outDir, "summary.md")
        let json = Path.Combine(outDir, "summary.json")
        writeAllText markdown (renderSummaryMarkdown summary)
        writeAllText json (renderSummaryJson summary + Environment.NewLine)
        [ markdown; json ]

    let runLanes (repositoryRoot: string) (outDir: string) (selectedLaneIds: string list) : ValidationSummary =
        Directory.CreateDirectory outDir |> ignore
        let definitions = defaultLaneDefinitions repositoryRoot outDir
        let selected =
            if selectedLaneIds.IsEmpty then
                definitions
            else
                definitions |> List.filter (fun lane -> selectedLaneIds |> List.contains lane.Id)

        let knownIds = definitions |> List.map _.Id |> Set.ofList
        let missing =
            selectedLaneIds
            |> List.filter (fun laneId -> not (knownIds.Contains laneId))
            |> List.map (fun laneId ->
                let result: LaneResult =
                    { LaneId = laneId
                      Status = NotRun
                      Command = "unknown lane"
                      StartedUtc = None
                      CompletedUtc = None
                      Elapsed = None
                      ExitCode = None
                      LogPath = Path.Combine(outDir, laneId, "log.txt")
                      ResultArtifacts = [ Path.Combine(outDir, laneId, "result.json") ]
                      Diagnostics = [ "unknown lane" ]
                      Caveats = []
                      AcceptedException = None
                      Required = true }

                result)

        let results = (selected |> List.map runLane) @ missing
        let readiness = computeOverallReadiness results

        let packageProofStatus =
            results
            |> List.tryFind (fun result -> result.LaneId = "package-proof")
            |> Option.map _.Status

        let summary =
            { PackageProofStatus = packageProofStatus
              SelectedSamples = [ "samples/AntShowcase" ]
              LocalFeedPath = PackageFeed.defaultFeedPath
              PackageCachePath = Some(Path.Combine(outDir, "package-proof", "nuget-cache"))
              SourceRules = [ "FS.GG.UI.* -> nuget-local"; "* -> nuget.org" ]
              LaneResults = results
              OverallReadiness = readiness
              Caveats =
                [ "aggregate-solution is optional and reported separately from focused lanes"
                  if results |> List.exists (fun result -> result.Status <> Passed) then
                      "non-passing lanes are not counted as green" ]
              ArtifactRoot = outDir }

        writeSummary outDir summary |> ignore
        summary
