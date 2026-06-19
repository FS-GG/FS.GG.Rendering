/// Responsiveness evidence command for the SecondAntShowcase sample.
module SecondAntShowcase.App.Responsiveness

open System
open System.Globalization
open System.IO
open System.Text.Json
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

type ResponsivenessScope =
    | Page of string
    | AllInteractive

type Request =
    { Scope: ResponsivenessScope
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

let private scopeText scope =
    match scope with
    | Page page -> page
    | AllInteractive -> "all-interactive"

let private scopePath scope theme =
    $"second-antshowcase/{scopeText scope}/{AntTheme.modeName theme}"

let parse args =
    let page = flag "--page" args
    let allInteractive = hasFlag "--all-interactive" args
    let script = flag "--script" args |> Option.defaultValue "representative"
    let outDir = flag "--out" args |> Option.defaultValue "artifacts/responsiveness"
    let themeText = flag "--theme" args |> Option.defaultValue "light"

    match tryParseTheme themeText with
    | None -> Error $"unknown theme '{themeText}'"
    | Some theme ->
        if allInteractive && Option.isSome page then
            Error "--page and --all-interactive are mutually exclusive"
        elif page |> Option.exists (fun pageId -> PageRegistry.all |> List.exists (fun pageSpec -> pageSpec.Id = pageId) |> not) then
            Error $"unknown page '{Option.get page}'"
        elif script <> "representative" then
            Error $"unknown responsiveness script '{script}'"
        else
            let scope =
                if allInteractive then AllInteractive
                else Page(page |> Option.defaultValue "buttons")

            Ok
                { Scope = scope
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

let private jsonString value =
    JsonSerializer.SerializeToElement(value, JsonSerializerOptions(WriteIndented = false))

let private actionPlan request =
    let actions =
        match request.Scope with
        | AllInteractive -> InteractionContracts.all
        | Page pageId ->
            InteractionContracts.all
            |> List.filter (fun contract -> contract.PageId = pageId)

    actions |> List.map Evidence.responsivenessActionOfContract

let private displayOnlyExclusions () =
    InteractionContracts.displayOnlyReasons
    |> Map.toList
    |> List.map (fun (controlId, reason) -> Evidence.responsivenessDisplayOnlyAction controlId reason)

let private inputKindForToken token =
    match token with
    | "pointer-move" -> "pointer-move", "move-burst"
    | "pointer-discrete" -> "pointer-discrete", "primary-click"
    | "key-down" -> "key-down", "keyboard-input"
    | "wheel" -> "wheel", "wheel"
    | other -> other, other

let private dragContinuityJson (action: Evidence.ResponsivenessReviewAction) =
    if action.ActionType = "drag" then
        JsonSerializer.SerializeToElement(
            {| sampleCount = 0
               visibleFeedbackSamples = 0
               maxSampleGapMs = Nullable<float>()
               delayedCatchUp = false
               classification = "missing-boundary" |},
            JsonSerializerOptions(WriteIndented = false)
        )
    else
        jsonNull

let private recordJson
    (run: string)
    (inputSequence: int)
    (action: Evidence.ResponsivenessReviewAction)
    (visibleResponse: string)
    (environmentStatus: string)
    (acceptanceStatus: string)
    (productChanged: bool)
    (totalMs: float)
    (coalesced: int)
    (diagnostics: string list)
    =
    let sequenceText = inputSequence.ToString("000000", CultureInfo.InvariantCulture)
    let inputKind, inputName = inputKindForToken action.InputKind

    JsonSerializer.Serialize(
        {| recordId = $"{run}-{sequenceText}"
           runId = run
           inputSequenceId = inputSequence
           inputKind = inputKind
           inputName = inputName
           page = action.PageId
           controlGroup = action.ControlFamily
           controlFamily = action.ControlFamily
           controlIds = action.ControlIds |> List.toArray
           actionType = action.ActionType
           expectedVisibleResult = action.ExpectedVisibleResult
           observedVisibleResult = if productChanged then action.ExpectedVisibleResult else "not measured in live presentation"
           receiptTimestamp = DateTimeOffset.UtcNow
           queueDepthAtReceipt = 0
           queueDepthAtDrain = 1
           coalescedMovementCount = coalesced
           productMessageCount = if productChanged then 1 else 0
           productStateChanged = productChanged
           runtimeStateChanged = inputKind.StartsWith("pointer", StringComparison.Ordinal)
           visibleResponse = visibleResponse
           presentedFrameId = Nullable<int64>()
           environmentStatus = environmentStatus
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
               status = environmentStatus |}
           dragContinuity = dragContinuityJson action
           longFrame = totalMs >= 50.0
           acceptanceStatus = acceptanceStatus
           diagnostics = diagnostics |},
        JsonSerializerOptions(WriteIndented = false)
    )

let private ensureRunRoot outputRoot run =
    let rootFull = Path.GetFullPath(outputRoot)
    let runRoot = Path.GetFullPath(Path.Combine(rootFull, run))
    let prefix =
        if rootFull.EndsWith(string Path.DirectorySeparatorChar, StringComparison.Ordinal) then
            rootFull
        else
            rootFull + string Path.DirectorySeparatorChar

    if not (runRoot.StartsWith(prefix, StringComparison.Ordinal)) then
        invalidOp "responsiveness run directory escaped the output root"

    runRoot

let private liveEnvironmentLimitations requireLive =
    let display = Environment.GetEnvironmentVariable("DISPLAY")
    let wayland = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")

    [ "headless-substitute:no-live-presentation-boundary"
      if String.IsNullOrWhiteSpace display && String.IsNullOrWhiteSpace wayland then
          "desktop-prerequisite:no-visible-surface"
      else
          "presentation:missing-boundary"
      if requireLive then
          "require-live:visible-surface-unavailable" ]

let private writeOutputs (request: Request) (run: string) (metrics: FrameMetrics list) =
    let runRoot = ensureRunRoot request.OutDir run
    Directory.CreateDirectory runRoot |> ignore

    let recordsPath = Path.Combine(runRoot, "records.jsonl")
    let summaryJsonPath = Path.Combine(runRoot, "summary.json")
    let summaryMdPath = Path.Combine(runRoot, "summary.md")
    let environmentPath = Path.Combine(runRoot, "environment.md")

    let anyProductChange = metrics |> List.exists _.ProductModelChanged
    let moveFrame = metrics |> List.tryFind (fun metric -> metric.PointerSamplesReceived > 1)
    let longFrame = metrics |> List.exists (fun metric -> metric.FrameCause = FrameCause.Tick)
    let actions = actionPlan request
    let displayOnly = displayOnlyExclusions ()
    let limitations = liveEnvironmentLimitations request.RequireLive
    let diagnostic =
        "SYNTHETIC: deterministic script output is substitute evidence, not accepted live latency."

    let records =
        actions
        |> List.mapi (fun index action ->
            let coalesced =
                if action.InputKind = "pointer-move" then
                    moveFrame |> Option.map (fun metric -> max 0 (metric.PointerSamplesReceived - 1)) |> Option.defaultValue 1
                else
                    0

            recordJson
                run
                (index + 1)
                action
                "environment-limited"
                "headless-substitute"
                "environment-limited"
                (anyProductChange && action.ActionId = "button-click")
                0.0
                coalesced
                [ diagnostic ])

    File.WriteAllLines(recordsPath, records)

    let requiredFamilies = actions |> List.map _.ControlFamily |> List.distinct |> List.sort
    let acceptedFamilies: string list = []
    let rejectedFamilies: string list = []
    let blockedFamilies = requiredFamilies
    let missingFamilies: string list = []
    let dragContinuity =
        actions
        |> List.filter (fun action -> action.ActionType = "drag")
        |> List.map (fun action ->
            {| controlFamily = action.ControlFamily
               actionId = action.ActionId
               classification = "missing-boundary" |})
        |> List.toArray
    let groupByFamily =
        actions
        |> List.groupBy (fun action -> action.PageId, action.InputKind, action.ControlFamily)
        |> List.map (fun ((pageId, inputKind, family), grouped) ->
            {| page = pageId
               inputKind = inputKind
               controlGroup = family
               controlFamily = family
               count = List.length grouped
               p50Ms = Nullable<float>()
               p95Ms = Nullable<float>()
               maxMs = Nullable<float>()
               longFrameCount = if longFrame then 1 else 0
               readiness = "environment-limited" |})
        |> List.toArray

    let firstFailedBudget =
        JsonSerializer.SerializeToElement(
            {| kind = "environment-boundary"
               scope = jsonString (scopePath request.Scope request.Theme)
               inputKind = jsonNull
               measuredMs = 1.0
               budgetMs = 0.0 |},
            JsonSerializerOptions(WriteIndented = false)
        )

    let summaryJson =
        JsonSerializer.Serialize(
            {| runId = run
               scope = scopePath request.Scope request.Theme
               overallReadiness = "environment-limited"
               startedUtc = DateTimeOffset.UtcNow
               completedUtc = DateTimeOffset.UtcNow
               recordsPath = "records.jsonl"
               budgets =
                {| inputReceiptP95Ms = 4
                   inputReceiptMaxMs = 16
                   inputToVisibleP95Ms = Evidence.responsivenessTargetP95Ms
                   inputToVisibleMaxMs = Evidence.responsivenessTargetMaxMs
                   longFrameThresholdMs = 50 |}
               firstFailedBudget = firstFailedBudget
               groups = groupByFamily
               coverage =
                {| requiredInteractiveFamilies = requiredFamilies |> List.toArray
                   acceptedInteractiveFamilies = acceptedFamilies |> List.toArray
                   rejectedInteractiveFamilies = rejectedFamilies |> List.toArray
                   blockedInteractiveFamilies = blockedFamilies |> List.toArray
                   displayOnlyExclusions =
                    displayOnly
                    |> List.map (fun action ->
                        {| controlId = action.ControlIds |> List.tryHead |> Option.defaultValue action.ActionId
                           reason = action.DisplayOnlyReason |> Option.defaultValue "display-only" |})
                    |> List.toArray
                   missingInteractiveFamilies = missingFamilies |> List.toArray |}
               slowestInteractions = [||]
               dragContinuity = dragContinuity
               artifactWriteStatus = "complete"
               environmentLimitations = limitations
               diagnostics = [ diagnostic ] |},
            jsonOptions
        )

    File.WriteAllText(summaryJsonPath, summaryJson)

    let groupMarkdown =
        groupByFamily
        |> Array.toList
        |> List.map (fun group ->
            $"| {group.page} | {group.inputKind} | {group.controlFamily} | {group.count} | n/a | n/a | n/a | {group.longFrameCount} | environment-limited |")

    let missingMarkdown =
        if List.isEmpty missingFamilies then
            [ "- none" ]
        else
            missingFamilies |> List.map (fun family -> $"- `{family}`")

    let summaryMarkdown =
        [ $"# Responsiveness summary {run}"
          ""
          $"- scope: {scopePath request.Scope request.Theme}"
          "- overall readiness: environment-limited"
          "- records: records.jsonl"
          "- first failed budget: environment-boundary"
          "- caveat: SYNTHETIC deterministic headless substitute; no accepted live input-to-present readiness claimed."
          $"- required interactive families: {List.length requiredFamilies}"
          $"- accepted interactive families: {List.length acceptedFamilies}"
          $"- rejected interactive families: {List.length rejectedFamilies}"
          $"- blocked interactive families: {List.length blockedFamilies}"
          $"- display-only exclusions: {List.length displayOnly}"
          $"- missing interactive families: {List.length missingFamilies}"
          "- artifact write status: complete"
          ""
          "Links: `summary.json`, `records.jsonl`, `environment.md`"
          ""
          "| Page | Input | Control | Count | p50 | p95 | max | long frames | readiness |"
          "|------|-------|---------|-------|-----|-----|-----|-------------|-----------|" ]
        @ groupMarkdown
        @ [ ""; "## Missing Interactive Families"; "" ]
        @ missingMarkdown
        @ [ ""; "## Environment Limitations"; "" ]
        @ (limitations |> List.map (fun limitation -> $"- {limitation}"))
        @ [ ""; "## Drag Continuity"; "" ]
        @ (if Array.isEmpty dragContinuity then
               [ "- none" ]
           else
               dragContinuity
               |> Array.toList
               |> List.map (fun drag -> $"- `{drag.controlFamily}`: {drag.classification}"))
        @ [ "" ]

    File.WriteAllText(
        summaryMdPath,
        String.concat Environment.NewLine summaryMarkdown
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
              "- artifact-write-status: complete"
              for limitation in limitations do
                  $"- limitation: {limitation}"
              "- diagnostic: live responsiveness acceptance requires a visible, focusable desktop session and measured presentation boundary"
              "" ]
    )

    summaryJsonPath

let run (args: string list) =
    match parse args with
    | Error message ->
        eprintfn "second-ant-showcase responsiveness: %s" message
        2
    | Ok request ->
        try
            Directory.CreateDirectory request.OutDir |> ignore
            let host = Host.create request.Theme
            let scriptPage =
                match request.Scope with
                | Page page -> page
                | AllInteractive -> "buttons"

            let metrics = ControlsElmish.Perf.runScript host size (Scripts.representative scriptPage)
            let run = runId ()
            let summaryPath = writeOutputs request run metrics

            if request.PrintJson then
                printfn """{"runId":"%s","summaryJson":"%s","readiness":"environment-limited"}""" run summaryPath
            else
                printfn "second-ant-showcase responsiveness: wrote %s" summaryPath

            4
        with
        | :? UnauthorizedAccessException as ex ->
            eprintfn "second-ant-showcase responsiveness: output root is not writable: %s" ex.Message
            3
        | :? IOException as ex ->
            eprintfn "second-ant-showcase responsiveness: output failed: %s" ex.Message
            3
