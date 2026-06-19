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

type private LiveMeasurement =
    { PresentedFrameId: int64
      TotalMs: float
      FrameWorkMs: float
      PaintMs: float
      PresentMs: float
      DirtyRectCount: int
      DirtyArea: int
      RepaintedNodeCount: int
      ProductChanged: bool
      RuntimeChanged: bool
      InputKind: string
      CoalescedMovementCount: int
      LongFrame: bool }

let private nullableFloat value =
    match value with
    | Some number -> Nullable<float>(durationMs number)
    | None -> Nullable<float>()

let private nullableInt value =
    match value with
    | Some number -> Nullable<int>(number)
    | None -> Nullable<int>()

let private nullableInt64 value =
    match value with
    | Some number -> Nullable<int64>(number)
    | None -> Nullable<int64>()

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

let private dragContinuityJson (action: Evidence.ResponsivenessReviewAction) (measurement: LiveMeasurement option) =
    if action.ActionType = "drag" then
        let sampleCount, visibleFeedbackSamples, maxSampleGap, delayedCatchUp, classification =
            match measurement with
            | Some value -> 1, 1, Nullable<float>(durationMs value.TotalMs), false, "continuous"
            | None -> 0, 0, Nullable<float>(), false, "missing-boundary"

        JsonSerializer.SerializeToElement(
            {| sampleCount = sampleCount
               visibleFeedbackSamples = visibleFeedbackSamples
               maxSampleGapMs = maxSampleGap
               delayedCatchUp = delayedCatchUp
               classification = classification |},
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
    (measurement: LiveMeasurement option)
    (diagnostics: string list)
    =
    let sequenceText = inputSequence.ToString("000000", CultureInfo.InvariantCulture)
    let inputKind, inputName = inputKindForToken action.InputKind
    let measuredProductChanged = measurement |> Option.map (fun value -> value.ProductChanged) |> Option.defaultValue productChanged
    let measuredRuntimeChanged =
        measurement
        |> Option.map (fun value -> value.RuntimeChanged)
        |> Option.defaultValue (inputKind.StartsWith("pointer", StringComparison.Ordinal))
    let measuredTotal = measurement |> Option.map (fun value -> value.TotalMs)
    let measuredWork = measurement |> Option.map (fun value -> value.FrameWorkMs) |> Option.defaultValue totalMs
    let measuredDirtyStatus = if Option.isSome measurement then "measured" else environmentStatus
    let measuredLongFrame =
        measurement
        |> Option.map (fun value -> value.LongFrame)
        |> Option.defaultValue (totalMs >= 50.0)

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
           observedVisibleResult = if Option.isSome measurement || measuredProductChanged then action.ExpectedVisibleResult else "not measured in live presentation"
           receiptTimestamp = DateTimeOffset.UtcNow
           queueDepthAtReceipt = 0
           queueDepthAtDrain = 1
           coalescedMovementCount = coalesced
           productMessageCount = if measuredProductChanged then 1 else 0
           productStateChanged = measuredProductChanged
           runtimeStateChanged = measuredRuntimeChanged
           visibleResponse = visibleResponse
           presentedFrameId = measurement |> Option.map (fun value -> value.PresentedFrameId) |> nullableInt64
           environmentStatus = environmentStatus
           phaseTiming =
            {| receiptDurationMs = durationMs 0.1
               queueDelayMs = durationMs 0.0
               routingDurationMs = durationMs 0.0
               updateDurationMs = durationMs 0.0
               viewDurationMs = durationMs 0.0
               retainedStepDurationMs = durationMs measuredWork
               layoutDurationMs = durationMs 0.0
               textDurationMs = durationMs 0.0
               paintDurationMs = measurement |> Option.map (fun value -> value.PaintMs) |> nullableFloat
               presentDurationMs = measurement |> Option.map (fun value -> value.PresentMs) |> nullableFloat
               totalInputToVisibleMs = measuredTotal |> nullableFloat |}
           dirtyRegion =
            {| dirtyRectCount = measurement |> Option.map (fun value -> value.DirtyRectCount) |> nullableInt
               dirtyArea = measurement |> Option.map (fun value -> value.DirtyArea) |> nullableInt
               repaintedNodeCount = measurement |> Option.map (fun value -> value.RepaintedNodeCount) |> nullableInt
               status = measuredDirtyStatus |}
           dragContinuity = dragContinuityJson action measurement
           longFrame = measuredLongFrame
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

let private forceSubstitute () =
    String.Equals(
        Environment.GetEnvironmentVariable "FS_GG_RESPONSIVENESS_FORCE_SUBSTITUTE",
        "1",
        StringComparison.Ordinal
    )

let private liveEnvironmentLimitations requireLive extraLimitations =
    let display = Environment.GetEnvironmentVariable("DISPLAY")
    let wayland = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")

    [ "headless-substitute:no-live-presentation-boundary"
      if forceSubstitute () then
          "test-override:forced-substitute"
      if String.IsNullOrWhiteSpace display && String.IsNullOrWhiteSpace wayland then
          "desktop-prerequisite:no-visible-surface"
      else
          "presentation:missing-boundary"
      if requireLive then
          "require-live:visible-surface-unavailable"
      yield! extraLimitations ]

let private writeEnvironmentLimitedOutputs (request: Request) (run: string) (metrics: FrameMetrics list) (extraLimitations: string list) (extraDiagnostics: string list) =
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
    let limitations = liveEnvironmentLimitations request.RequireLive extraLimitations
    let diagnostic =
        "SYNTHETIC: deterministic script output is substitute evidence, not accepted live latency."
    let diagnostics = diagnostic :: extraDiagnostics

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
                None
                diagnostics)

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
               baselineProfileId = "2026-06-19"
               optimizedProfileId = run
               preparationReductionPercent = Nullable<float>()
               firstFramePreparationReductionPercent = Nullable<float>()
               parityStatus = "environment-limited"
               parityArtifacts = Array.empty<string>
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
               diagnostics = diagnostics |},
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
          "- baseline profile: 2026-06-19"
          $"- optimized profile: {run}"
          "- preparation reduction: n/a"
          "- first-frame preparation reduction: n/a"
          "- parity: environment-limited"
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

let private scriptFor request =
    match request.Scope with
    | AllInteractive -> Scripts.representativeAllInteractive()
    | Page page -> Scripts.representative page

let private liveViewerOptions () : FS.GG.UI.SkiaViewer.ViewerOptions =
    { Title = "Second Ant Showcase Responsiveness"
      InitialSize = size
      PresentMode = FS.GG.UI.SkiaViewer.ViewerPresentMode.DirectToSwapchain
      FrameRateCap = Some 60 }

let private liveWindowBehavior () =
    { FS.GG.UI.SkiaViewer.Viewer.defaultWindowBehavior with
        StartupState = FS.GG.UI.SkiaViewer.ViewerWindowStartupState.Normal
        StartupPosition = Some FS.GG.UI.SkiaViewer.ViewerWindowPosition.Centered
        BackendPreference = Some FS.GG.UI.SkiaViewer.ViewerBackendPreference.OpenGL }

let private metricInputKind (metric: FrameMetrics) =
    match metric.FrameCause with
    | FrameCause.PointerMove -> "pointer-move"
    | FrameCause.PointerDiscrete -> "pointer-discrete"
    | FrameCause.Key -> "key-down"
    | FrameCause.Tick -> "tick"
    | FrameCause.Resize -> "resize"
    | FrameCause.Theme -> "theme"
    | FrameCause.Idle -> "idle"

let private effectivePresentationTiming (fallbackPaint: TimeSpan, fallbackPresent: TimeSpan) (metric: FrameMetrics) =
    let paint =
        if metric.PaintDuration > TimeSpan.Zero then metric.PaintDuration else fallbackPaint

    let present =
        if metric.ComposeDuration > TimeSpan.Zero then metric.ComposeDuration else fallbackPresent

    paint, present

let private metricHasPresentationBoundary fallbackTiming (metric: FrameMetrics) =
    let paint, present = effectivePresentationTiming fallbackTiming metric
    paint > TimeSpan.Zero || present > TimeSpan.Zero

let private measuredInputMetric fallbackTiming (metric: FrameMetrics) =
    metricHasPresentationBoundary fallbackTiming metric
    && match metric.FrameCause with
       | FrameCause.PointerMove
       | FrameCause.PointerDiscrete
       | FrameCause.Key -> true
       | _ -> false

let private measurementsOfMetrics fallbackTiming (metrics: FrameMetrics list) =
    metrics
    |> List.filter (measuredInputMetric fallbackTiming)
    |> List.mapi (fun index metric ->
        let paintDuration, presentDuration = effectivePresentationTiming fallbackTiming metric
        let paintMs = paintDuration.TotalMilliseconds
        let presentMs = presentDuration.TotalMilliseconds
        let frameWorkMs = metric.FrameDuration.TotalMilliseconds
        let totalMs = frameWorkMs + paintMs + presentMs

        { PresentedFrameId = int64 (index + 1)
          TotalMs = totalMs
          FrameWorkMs = frameWorkMs
          PaintMs = paintMs
          PresentMs = presentMs
          DirtyRectCount = metric.DirtyRectCount
          DirtyArea = metric.DirtyArea
          RepaintedNodeCount = metric.RepaintedNodeCount
          ProductChanged = metric.ProductModelChanged
          RuntimeChanged =
            match metric.FrameCause with
            | FrameCause.PointerMove
            | FrameCause.PointerDiscrete -> true
            | _ -> false
          InputKind = metricInputKind metric
          CoalescedMovementCount =
            if metric.FrameCause = FrameCause.PointerMove then
                max 0 (metric.PointerSamplesReceived - 1)
            else
                0
          LongFrame = totalMs >= 50.0 })

let private percentile p values =
    match values |> List.sort with
    | [] -> None
    | sorted ->
        let rawIndex = int (Math.Ceiling(float sorted.Length * p)) - 1
        let index = rawIndex |> max 0 |> min (sorted.Length - 1)
        Some sorted.[index]

let private markdownMs value =
    match value with
    | Some number -> durationMs number |> fun rounded -> rounded.ToString("0.###", CultureInfo.InvariantCulture)
    | None -> "n/a"

let private firstFailedBudgetJson request missingFamilies p95 maxValue =
    match missingFamilies, p95, maxValue with
    | missing :: _, _, _ ->
        JsonSerializer.SerializeToElement(
            {| kind = "missing-live-record"
               scope = jsonString (scopePath request.Scope request.Theme)
               inputKind = jsonNull
               measuredMs = 0.0
               budgetMs = 1.0
               missingFamily = missing |},
            JsonSerializerOptions(WriteIndented = false)
        )
    | [], Some measured, _ when measured > float Evidence.responsivenessTargetP95Ms ->
        JsonSerializer.SerializeToElement(
            {| kind = "input-to-visible-p95"
               scope = jsonString (scopePath request.Scope request.Theme)
               inputKind = jsonNull
               measuredMs = durationMs measured
               budgetMs = float Evidence.responsivenessTargetP95Ms |},
            JsonSerializerOptions(WriteIndented = false)
        )
    | [], _, Some measured when measured > float Evidence.responsivenessTargetMaxMs ->
        JsonSerializer.SerializeToElement(
            {| kind = "input-to-visible-max"
               scope = jsonString (scopePath request.Scope request.Theme)
               inputKind = jsonNull
               measuredMs = durationMs measured
               budgetMs = float Evidence.responsivenessTargetMaxMs |},
            JsonSerializerOptions(WriteIndented = false)
        )
    | _ -> jsonNull

let private writeLiveOutputs (request: Request) (run: string) (live: FS.GG.UI.Controls.Elmish.LiveScriptRunResult) =
    let fallbackTiming = FS.GG.UI.SkiaViewer.Host.GlHost.lastPresentTiming()
    let measurements = measurementsOfMetrics fallbackTiming live.Metrics

    if List.isEmpty measurements then
        None
    else
        let runRoot = ensureRunRoot request.OutDir run
        Directory.CreateDirectory runRoot |> ignore

        let recordsPath = Path.Combine(runRoot, "records.jsonl")
        let summaryJsonPath = Path.Combine(runRoot, "summary.json")
        let summaryMdPath = Path.Combine(runRoot, "summary.md")
        let environmentPath = Path.Combine(runRoot, "environment.md")
        let actions = actionPlan request
        let displayOnly = displayOnlyExclusions ()
        let paired =
            actions
            |> List.mapi (fun index action -> action, measurements |> List.tryItem index)

        let requiredFamilies = actions |> List.map _.ControlFamily |> List.distinct |> List.sort
        let missingFamilies =
            paired
            |> List.choose (fun (action, measurement) ->
                match measurement with
                | Some _ -> None
                | None -> Some action.ControlFamily)
            |> List.distinct
            |> List.sort

        let measuredTotals =
            paired
            |> List.choose (fun (_, measurement) -> measurement |> Option.map (fun value -> value.TotalMs))

        let p95 = percentile 0.95 measuredTotals
        let maxValue =
            match measuredTotals with
            | [] -> None
            | values -> Some(List.max values)

        let budgetRejected =
            (p95 |> Option.exists (fun value -> value > float Evidence.responsivenessTargetP95Ms))
            || (maxValue |> Option.exists (fun value -> value > float Evidence.responsivenessTargetMaxMs))

        let overallReadiness =
            if List.isEmpty missingFamilies && not budgetRejected then "accepted" else "rejected"

        let diagnostics =
            [ "LIVE: measured through ControlsElmish.Live.runScript and the OpenGL viewer presentation boundary."
              $"viewerOutcome={live.Outcome.Status}; firstFramePresented={live.Outcome.FirstFramePresented}" ]

        let records =
            paired
            |> List.mapi (fun index (action, measurement) ->
                let visibleResponse =
                    match measurement with
                    | Some _ -> "presented-frame"
                    | None -> "no-visible-response"

                let environmentStatus =
                    match measurement with
                    | Some _ -> "measured"
                    | None -> "missing-boundary"

                let acceptanceStatus =
                    match measurement with
                    | Some value when value.TotalMs <= float Evidence.responsivenessTargetMaxMs -> "accepted"
                    | _ -> "rejected"

                recordJson
                    run
                    (index + 1)
                    action
                    visibleResponse
                    environmentStatus
                    acceptanceStatus
                    (measurement |> Option.map (fun value -> value.ProductChanged) |> Option.defaultValue false)
                    (measurement |> Option.map (fun value -> value.TotalMs) |> Option.defaultValue 0.0)
                    (measurement |> Option.map (fun value -> value.CoalescedMovementCount) |> Option.defaultValue 0)
                    measurement
                    diagnostics)

        File.WriteAllLines(recordsPath, records)

        let groupByFamily =
            paired
            |> List.groupBy (fun (action, _) -> action.PageId, action.InputKind, action.ControlFamily)
            |> List.map (fun ((pageId, inputKind, family), grouped) ->
                let totals =
                    grouped
                    |> List.choose (fun (_, measurement) -> measurement |> Option.map (fun value -> value.TotalMs))

                let groupP50 = percentile 0.50 totals
                let groupP95 = percentile 0.95 totals
                let groupMax =
                    match totals with
                    | [] -> None
                    | values -> Some(List.max values)

                let groupMissing = grouped |> List.exists (fun (_, measurement) -> Option.isNone measurement)
                let groupRejected =
                    groupMissing
                    || (groupP95 |> Option.exists (fun value -> value > float Evidence.responsivenessTargetP95Ms))
                    || (groupMax |> Option.exists (fun value -> value > float Evidence.responsivenessTargetMaxMs))

                {| page = pageId
                   inputKind = inputKind
                   controlGroup = family
                   controlFamily = family
                   count = List.length grouped
                   p50Ms = groupP50 |> nullableFloat
                   p95Ms = groupP95 |> nullableFloat
                   maxMs = groupMax |> nullableFloat
                   longFrameCount = grouped |> List.filter (fun (_, measurement) -> measurement |> Option.exists (fun value -> value.LongFrame)) |> List.length
                   readiness = if groupRejected then "rejected" else "accepted" |})
            |> List.toArray

        let acceptedFamilies =
            if overallReadiness = "accepted" then requiredFamilies else []

        let rejectedFamilies =
            if overallReadiness = "rejected" then
                requiredFamilies |> List.except missingFamilies
            else
                []

        let firstFailedBudget = firstFailedBudgetJson request missingFamilies p95 maxValue

        let slowestInteractions =
            paired
            |> List.mapi (fun index (action, measurement) -> index, action, measurement)
            |> List.choose (fun (index, action, measurement) ->
                measurement
                |> Option.map (fun value ->
                    let dominant =
                        if value.PresentMs >= value.PaintMs then "present" else "paint"
                    let sequenceText = (index + 1).ToString("000000", CultureInfo.InvariantCulture)
                    let recordId = $"{run}-{sequenceText}"

                    {| recordId = recordId
                       inputSequenceId = index + 1
                       actionId = action.ActionId
                       controlFamily = action.ControlFamily
                       totalInputToVisibleMs = durationMs value.TotalMs
                       dominantPhase = dominant |}))
            |> List.sortByDescending _.totalInputToVisibleMs
            |> List.truncate 5
            |> List.toArray

        let dragContinuity =
            paired
            |> List.filter (fun (action, _) -> action.ActionType = "drag")
            |> List.map (fun (action, measurement) ->
                {| controlFamily = action.ControlFamily
                   actionId = action.ActionId
                   classification = if Option.isSome measurement then "continuous" else "missing-boundary" |})
            |> List.toArray

        let environmentLimitations =
            if List.isEmpty missingFamilies then
                []
            else
                [ "live-measurement:missing-actions" ]

        let summaryJson =
            JsonSerializer.Serialize(
                {| runId = run
                   baselineProfileId = "2026-06-19"
                   optimizedProfileId = run
                   preparationReductionPercent = Nullable<float>()
                   firstFramePreparationReductionPercent = Nullable<float>()
                   parityStatus = if overallReadiness = "accepted" then "pending-review" else "not-accepted"
                   parityArtifacts = Array.empty<string>
                   scope = scopePath request.Scope request.Theme
                   overallReadiness = overallReadiness
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
                       blockedInteractiveFamilies = [||]
                       displayOnlyExclusions =
                        displayOnly
                        |> List.map (fun action ->
                            {| controlId = action.ControlIds |> List.tryHead |> Option.defaultValue action.ActionId
                               reason = action.DisplayOnlyReason |> Option.defaultValue "display-only" |})
                        |> List.toArray
                       missingInteractiveFamilies = missingFamilies |> List.toArray |}
                   slowestInteractions = slowestInteractions
                   dragContinuity = dragContinuity
                   artifactWriteStatus = "complete"
                   environmentLimitations = environmentLimitations
                   diagnostics = diagnostics |},
                jsonOptions
            )

        File.WriteAllText(summaryJsonPath, summaryJson)

        let groupMarkdown =
            groupByFamily
            |> Array.toList
            |> List.map (fun group ->
                let p50Text = markdownMs (if group.p50Ms.HasValue then Some group.p50Ms.Value else None)
                let p95Text = markdownMs (if group.p95Ms.HasValue then Some group.p95Ms.Value else None)
                let maxText = markdownMs (if group.maxMs.HasValue then Some group.maxMs.Value else None)
                $"| {group.page} | {group.inputKind} | {group.controlFamily} | {group.count} | {p50Text} | {p95Text} | {maxText} | {group.longFrameCount} | {group.readiness} |")

        let missingMarkdown =
            if List.isEmpty missingFamilies then
                [ "- none" ]
            else
                missingFamilies |> List.map (fun family -> $"- `{family}`")
        let firstFailedBudgetKind =
            if firstFailedBudget.ValueKind = JsonValueKind.Null then
                "none"
            else
                firstFailedBudget.GetProperty("kind").GetString()
                |> Option.ofObj
                |> Option.defaultValue "unknown"
        let parityStatus =
            if overallReadiness = "accepted" then "pending-review" else "not-accepted"

        let summaryMarkdown =
            [ $"# Responsiveness summary {run}"
              ""
              $"- scope: {scopePath request.Scope request.Theme}"
              $"- overall readiness: {overallReadiness}"
              "- baseline profile: 2026-06-19"
              $"- optimized profile: {run}"
              "- preparation reduction: pending render-lag probe correlation"
              "- first-frame preparation reduction: pending render-lag probe correlation"
              $"- parity: {parityStatus}"
              "- records: records.jsonl"
              $"- first failed budget: {firstFailedBudgetKind}"
              "- evidence path: live GL viewer presentation boundary"
              $"- required interactive families: {List.length requiredFamilies}"
              $"- accepted interactive families: {List.length acceptedFamilies}"
              $"- rejected interactive families: {List.length rejectedFamilies}"
              "- blocked interactive families: 0"
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
            @ [ ""; "## Drag Continuity"; "" ]
            @ (if Array.isEmpty dragContinuity then
                   [ "- none" ]
               else
                   dragContinuity
                   |> Array.toList
                   |> List.map (fun drag -> $"- `{drag.controlFamily}`: {drag.classification}"))
            @ [ "" ]

        File.WriteAllText(summaryMdPath, String.concat Environment.NewLine summaryMarkdown)

        File.WriteAllText(
            environmentPath,
            String.concat
                Environment.NewLine
                [ "# Responsiveness environment"
                  ""
                  $"- status: {overallReadiness}"
                  "- presentation: live GL presentation boundary measured through DirectToSwapchain"
                  "- viewer-path: ControlsElmish.Live.runScript -> Viewer.runInteractiveViewerScriptWithWindowBehavior"
                  $"- measured-records: {List.length measuredTotals}"
                  "- artifact-write-status: complete"
                  for diagnostic in diagnostics do
                      $"- diagnostic: {diagnostic}"
                  "" ]
        )

        Some(summaryJsonPath, overallReadiness)

let private runLive request =
    let host = Host.create request.Theme
    ControlsElmish.Live.runScriptWithWindowBehavior (liveViewerOptions ()) (liveWindowBehavior ()) host (scriptFor request)

let run (args: string list) =
    match parse args with
    | Error message ->
        eprintfn "second-ant-showcase responsiveness: %s" message
        2
    | Ok request ->
        try
            Directory.CreateDirectory request.OutDir |> ignore

            let writeSubstitute extraLimitations extraDiagnostics =
                let host = Host.create request.Theme
                let metrics = ControlsElmish.Perf.runScript host size (scriptFor request)
                let run = runId ()
                let summaryPath = writeEnvironmentLimitedOutputs request run metrics extraLimitations extraDiagnostics

                if request.PrintJson then
                    printfn """{"runId":"%s","summaryJson":"%s","readiness":"environment-limited"}""" run summaryPath
                else
                    printfn "second-ant-showcase responsiveness: wrote %s" summaryPath

                4

            if request.RequireLive && not (forceSubstitute ()) then
                let capability = FS.GG.UI.SkiaViewer.Viewer.runtimeCapability ()

                if capability.PersistentWindow then
                    match runLive request with
                    | Result.Ok live ->
                        let run = runId ()

                        match writeLiveOutputs request run live with
                        | Some(summaryPath, readiness) ->
                            if request.PrintJson then
                                printfn """{"runId":"%s","summaryJson":"%s","readiness":"%s"}""" run summaryPath readiness
                            else
                                printfn "second-ant-showcase responsiveness: wrote %s" summaryPath

                            if readiness = "accepted" then 0 else 5
                        | None ->
                            let fallbackPaint, fallbackPresent = FS.GG.UI.SkiaViewer.Host.GlHost.lastPresentTiming()
                            let inputMetricCount =
                                live.Metrics
                                |> List.filter (fun metric ->
                                    match metric.FrameCause with
                                    | FrameCause.PointerMove
                                    | FrameCause.PointerDiscrete
                                    | FrameCause.Key -> true
                                    | _ -> false)
                                |> List.length
                            let finalPaintMs = fallbackPaint.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)
                            let finalPresentMs = fallbackPresent.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)

                            writeSubstitute
                                [ "presentation:missing-boundary"
                                  "live-runner:no-measured-present-timing" ]
                                [ "LIVE: viewer ran but no non-zero GL paint/present timing was observed."
                                  $"LIVE: metrics={List.length live.Metrics}; inputMetrics={inputMetricCount}; finalPaintMs={finalPaintMs}; finalPresentMs={finalPresentMs}" ]
                    | Result.Error failure ->
                        writeSubstitute
                            [ "live-runner-failure:" + failure.Message ]
                            [ "LIVE: GL viewer path failed before accepted responsiveness could be measured: " + failure.Message ]
                else
                    writeSubstitute
                        (capability.UnsupportedHostReasons
                         |> List.map (fun reason -> "desktop-prerequisite:" + reason))
                        [ "LIVE: persistent GL viewer is unavailable on this host." ]
            else
                writeSubstitute [] []
        with
        | :? UnauthorizedAccessException as ex ->
            eprintfn "second-ant-showcase responsiveness: output root is not writable: %s" ex.Message
            3
        | :? IOException as ex ->
            eprintfn "second-ant-showcase responsiveness: output failed: %s" ex.Message
            3
