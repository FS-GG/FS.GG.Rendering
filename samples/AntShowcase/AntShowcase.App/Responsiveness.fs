/// Responsiveness evidence command for the AntShowcase sample.
module AntShowcase.App.Responsiveness

open System
open System.Globalization
open System.IO
open System.Text.Json
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open AntShowcase.Core
open AntShowcase.Core.Model

type Request =
    { Page: string
      Theme: ThemeMode
      Script: string
      OutDir: string
      RequireLive: bool
      PrintJson: bool }

let private size: FS.GG.UI.Scene.Size = { Width = 1024; Height = 768 }

let private flag (name: string) (args: string list) =
    let rec loop =
        function
        | key :: value :: _ when key = name -> Some value
        | _ :: rest -> loop rest
        | [] -> None

    loop args

let private hasFlag name args =
    args |> List.contains name

let private tryParseTheme value =
    match value with
    | "light" -> Some Light
    | "dark" -> Some Dark
    | _ -> None

let parse args =
    let page = flag "--page" args |> Option.defaultValue "buttons"
    let script = flag "--script" args |> Option.defaultValue "representative"
    let outDir = flag "--out" args |> Option.defaultValue "artifacts/responsiveness"
    let themeText = flag "--theme" args |> Option.defaultValue "light"

    match tryParseTheme themeText with
    | None -> Error $"unknown theme '{themeText}'"
    | Some theme ->
        if PageRegistry.all |> List.exists (fun pageSpec -> pageSpec.Id = page) |> not then
            Error $"unknown page '{page}'"
        elif script <> "representative" then
            Error $"unknown responsiveness script '{script}'"
        else
            Ok
                { Page = page
                  Theme = theme
                  Script = script
                  OutDir = outDir
                  RequireLive = hasFlag "--require-live" args
                  PrintJson = hasFlag "--json" args }

let private runId () =
    let stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
    let suffix = Guid.NewGuid().ToString("N").Substring(0, 6)
    $"resp-{stamp}-{suffix}"

let private durationMs (value: float) =
    Math.Round(value, 3)

let private jsonOptions = JsonSerializerOptions(WriteIndented = true)

let private jsonNull =
    use document = JsonDocument.Parse("null")
    document.RootElement.Clone()

let private recordJson
    (run: string)
    (inputSequence: int)
    (inputKind: string)
    (inputName: string)
    (visibleResponse: string)
    (productChanged: bool)
    (totalMs: float)
    (coalesced: int)
    (diagnostics: string list)
    =
    let sequenceText = inputSequence.ToString("000000", CultureInfo.InvariantCulture)

    JsonSerializer.Serialize(
        {| recordId = $"{run}-{sequenceText}"
           runId = run
           inputSequenceId = inputSequence
           inputKind = inputKind
           inputName = inputName
           page = "buttons"
           controlGroup = "button"
           receiptTimestamp = DateTimeOffset.UtcNow
           queueDepthAtReceipt = 0
           queueDepthAtDrain = 1
           coalescedMovementCount = coalesced
           productMessageCount = if productChanged then 1 else 0
           productStateChanged = productChanged
           runtimeStateChanged = inputKind.StartsWith("pointer", StringComparison.Ordinal)
           visibleResponse = visibleResponse
           presentedFrameId = Nullable<int64>()
           environmentStatus = "headless-substitute"
           phaseTiming =
            {| receiptDurationMs = durationMs 0.1
               queueDelayMs = durationMs 0.0
               routingDurationMs = durationMs 0.0
               updateDurationMs = durationMs 0.0
               viewDurationMs = durationMs 0.0
               retainedStepDurationMs = durationMs totalMs
               layoutDurationMs = durationMs 0.0
               textDurationMs = durationMs 0.0
               paintDurationMs = Nullable<float>()
               presentDurationMs = Nullable<float>()
               totalInputToVisibleMs = Nullable<float>() |}
           dirtyRegion =
            {| dirtyRectCount = Nullable<int>()
               dirtyArea = Nullable<int>()
               repaintedNodeCount = Nullable<int>()
               status = "headless-substitute" |}
           longFrame = totalMs >= 50.0
           diagnostics = diagnostics |},
        JsonSerializerOptions(WriteIndented = false)
    )

let private writeOutputs (request: Request) (run: string) (metrics: FrameMetrics list) =
    let runRoot = Path.Combine(request.OutDir, run)
    Directory.CreateDirectory runRoot |> ignore

    let recordsPath = Path.Combine(runRoot, "records.jsonl")
    let summaryJsonPath = Path.Combine(runRoot, "summary.json")
    let summaryMdPath = Path.Combine(runRoot, "summary.md")
    let environmentPath = Path.Combine(runRoot, "environment.md")

    let productFrames = metrics |> List.filter _.ProductModelChanged |> List.length
    let moveFrame = metrics |> List.tryFind (fun metric -> metric.PointerSamplesReceived > 1)
    let longFrame = metrics |> List.exists (fun metric -> metric.FrameCause = FrameCause.Tick)

    let records =
        [ recordJson run 1 "pointer-move" "move-burst" "environment-limited" false 0.0 (moveFrame |> Option.map (fun metric -> max 0 (metric.PointerSamplesReceived - 1)) |> Option.defaultValue 1) [ "SYNTHETIC: deterministic headless substitute; no live GL presentation boundary measured." ]
          recordJson run 2 "pointer-discrete" "primary-click" "environment-limited" false 0.0 0 [ "SYNTHETIC: pointer shape captured without live presentation." ]
          recordJson run 3 "key-down" "Enter" "environment-limited" (productFrames > 0) 0.0 0 [ "SYNTHETIC: keyboard activation shape captured without live presentation." ]
          recordJson run 4 "key-down" "Space" "environment-limited" (productFrames > 1) 0.0 0 [ "SYNTHETIC: keyboard activation shape captured without live presentation." ]
          recordJson run 5 "key-down" "Escape" "no-visible-response" false 0.0 0 [ "no visible response expected for Escape in the representative script." ] ]

    File.WriteAllLines(recordsPath, records)

    let summaryJson =
        JsonSerializer.Serialize(
            {| runId = run
               scope = $"antshowcase/{request.Page}/{AntTheme.modeName request.Theme}"
               overallReadiness = "environment-limited"
               startedUtc = DateTimeOffset.UtcNow
               completedUtc = DateTimeOffset.UtcNow
               recordsPath = "records.jsonl"
               budgets =
                {| inputReceiptP95Ms = 4
                   inputReceiptMaxMs = 16
                   inputToVisibleP95Ms = 50
                   longFrameThresholdMs = 50 |}
               firstFailedBudget = jsonNull
               groups =
                [| {| page = request.Page
                      inputKind = "pointer-discrete"
                      controlGroup = "button"
                      count = 1
                      p50Ms = Nullable<float>()
                      p95Ms = Nullable<float>()
                      maxMs = Nullable<float>()
                      longFrameCount = if longFrame then 1 else 0
                      readiness = "environment-limited" |}
                   {| page = request.Page
                      inputKind = "key-down"
                      controlGroup = "button"
                      count = 3
                      p50Ms = Nullable<float>()
                      p95Ms = Nullable<float>()
                      maxMs = Nullable<float>()
                      longFrameCount = 0
                      readiness = "environment-limited" |} |]
               slowestInteractions = [||]
               environmentLimitations =
                [ "headless-substitute:no-live-presentation-boundary"
                  if request.RequireLive then "require-live:visible-surface-unavailable" ]
               diagnostics = [ "SYNTHETIC: deterministic script output is substitute evidence, not accepted live latency." ] |},
            jsonOptions
        )

    File.WriteAllText(summaryJsonPath, summaryJson)

    File.WriteAllText(
        summaryMdPath,
        String.concat
            Environment.NewLine
            [ $"# Responsiveness summary {run}"
              ""
              $"- scope: antshowcase/{request.Page}/{AntTheme.modeName request.Theme}"
              "- overall readiness: environment-limited"
              "- records: records.jsonl"
              "- first failed budget: none"
              "- caveat: SYNTHETIC deterministic headless substitute; no accepted live input-to-present readiness claimed."
              ""
              "| Page | Input | Control | Count | p50 | p95 | max | long frames | readiness |"
              "|------|-------|---------|-------|-----|-----|-----|-------------|-----------|"
              $"| {request.Page} | pointer-discrete | button | 1 | n/a | n/a | n/a | {(if longFrame then 1 else 0)} | environment-limited |"
              $"| {request.Page} | key-down | button | 3 | n/a | n/a | n/a | 0 | environment-limited |"
              "" ]
    )

    File.WriteAllText(
        environmentPath,
        String.concat
            Environment.NewLine
            [ "# Responsiveness environment"
              ""
              "- status: environment-limited"
              "- presentation: no live GL presentation boundary measured by the deterministic command"
              "- substitute: ControlsElmish.Perf.runScript representative script"
              "" ]
    )

    summaryJsonPath

let run (args: string list) =
    match parse args with
    | Error message ->
        eprintfn "ant-showcase responsiveness: %s" message
        2
    | Ok request ->
        try
            Directory.CreateDirectory request.OutDir |> ignore
            let host = Host.create request.Theme
            let metrics = ControlsElmish.Perf.runScript host size (Scripts.representative request.Page)
            let run = runId ()
            let summaryPath = writeOutputs request run metrics

            if request.PrintJson then
                printfn """{"summaryJson":"%s","readiness":"environment-limited"}""" summaryPath
            else
                printfn "ant-showcase responsiveness: wrote %s" summaryPath

            4
        with
        | :? UnauthorizedAccessException as ex ->
            eprintfn "ant-showcase responsiveness: output root is not writable: %s" ex.Message
            2
        | :? IOException as ex ->
            eprintfn "ant-showcase responsiveness: output failed: %s" ex.Message
            3
