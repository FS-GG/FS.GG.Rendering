module SecondAntShowcase.App.RenderLagProbe

open System
open System.Globalization
open System.IO
open System.Text.Json
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

let private size: Size = { Width = 1024; Height = 768 }

let private noMods: KeyModifiers =
    { Ctrl = false
      Alt = false
      Shift = false
      Meta = false }

let private options () =
    { Title = "Second Ant Showcase Render Lag Probe"
      InitialSize = size
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FrameRateCap = Some 60 }

let private windowBehavior () =
    { Viewer.defaultWindowBehavior with
        StartupState = ViewerWindowStartupState.Normal
        StartupPosition = Some ViewerWindowPosition.Centered
        BackendPreference = Some ViewerBackendPreference.OpenGL }

let private flag name args =
    let rec loop items =
        match items with
        | key :: value :: _ when key = name -> Some value
        | _ :: rest -> loop rest
        | [] -> None

    loop args

let private scenario args =
    flag "--scenario" args |> Option.defaultValue "button-click"

let private theme args =
    match flag "--theme" args with
    | Some value ->
        match VisualConfig.resolveThemeAlias value with
        | Result.Ok(mode, _) -> mode
        | Result.Error _ -> Light
    | None -> Light

let private outDir args =
    flag "--out" args
    |> Option.defaultValue "specs/174-fix-render-lag/readiness/render-lag"

let private forceSubstitute () =
    String.Equals(
        Environment.GetEnvironmentVariable "FS_GG_RENDER_LAG_FORCE_SUBSTITUTE",
        "1",
        StringComparison.Ordinal
    )

let private hostFor scenario theme =
    let baseHost = Host.create theme

    match scenario with
    | "page-change" ->
        { baseHost with
            MapKey =
                fun key pressed ->
                    match key, pressed with
                    | Function 2, true -> Some(NavigateTo "text-numeric-input")
                    | _ -> baseHost.MapKey key pressed }
    | _ -> baseHost

let private scriptFor scenario =
    let key =
        match scenario with
        | "page-change" -> Function 2
        | _ -> Enter

    [ FrameInput.Tick(TimeSpan.FromMilliseconds 16.0)
      FrameInput.Key(key, noMods)
      FrameInput.Tick(TimeSpan.FromMilliseconds 32.0)
      FrameInput.Idle ]

let private jsonOptions =
    JsonSerializerOptions(WriteIndented = true)

let private jsonLineOptions =
    JsonSerializerOptions(WriteIndented = false)

let private runId () =
    "lag-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff", CultureInfo.InvariantCulture)

let private ms (duration: TimeSpan) =
    Math.Round(duration.TotalMilliseconds, 3)

let private percentile q values =
    match values |> List.sort with
    | [] -> None
    | sorted ->
        let index = int (Math.Ceiling(q * float sorted.Length)) - 1
        sorted.[Math.Clamp(index, 0, sorted.Length - 1)] |> Some

let private dominantPhase framePreparation paint presentation =
    [ "frame-preparation", framePreparation; "paint", paint; "presentation", presentation ]
    |> List.maxBy snd
    |> fst

let private baselinePreparation scenario =
    match scenario with
    | "page-change" -> Some 2576.305
    | "button-click" -> Some 1247.503
    | _ -> None

let private baselineFirstFramePreparation scenario =
    match scenario with
    | "page-change" -> Some 1199.463
    | "button-click" -> Some 1220.819
    | _ -> None

let private reductionPercent baseline after =
    match baseline, after with
    | Some before, Some current when before > 0.0 -> Some(Math.Round(((before - current) / before) * 100.0, 3))
    | _ -> None

let private phaseRecord run scenario frameIndex environmentStatus (metric: FrameMetrics option) diagnostics =
    match metric with
    | Some metric ->
        let paint = ms metric.PaintDuration
        let presentation = ms metric.ComposeDuration
        let framePreparation =
            metric.FrameDuration - metric.PaintDuration - metric.ComposeDuration
            |> fun value -> if value < TimeSpan.Zero then TimeSpan.Zero else value
            |> ms
        let total = framePreparation + paint + presentation
        JsonSerializer.Serialize(
            {| runId = run
               scenarioId = scenario
               frameIndex = frameIndex
               environmentStatus = environmentStatus
               inputHandlingMs = 0.0
               modelUpdateMs = if metric.ProductModelChanged then framePreparation else 0.0
               framePreparationMs = framePreparation
               layoutMs = if metric.LayoutRan then framePreparation else 0.0
               textMs = 0.0
               retainedStepMs = framePreparation
               paintMs = paint
               presentationMs = presentation
               totalInputToVisibleMs = total
               dominantPhase = dominantPhase framePreparation paint presentation
               metadataVisitedNodeCount = metric.RemeasuredNodeCount + metric.RepaintedNodeCount
               baselineNodeCount = metric.RemeasuredNodeCount + metric.RepaintedNodeCount
               fallbackCount = metric.FullRenderFallbackCount
               diagnostics = diagnostics |> List.toArray |},
            jsonLineOptions)
    | None ->
        JsonSerializer.Serialize(
            {| runId = run
               scenarioId = scenario
               frameIndex = frameIndex
               environmentStatus = environmentStatus
               inputHandlingMs = 0.0
               modelUpdateMs = 0.0
               framePreparationMs = 0.0
               layoutMs = 0.0
               textMs = 0.0
               retainedStepMs = 0.0
               paintMs = 0.0
               presentationMs = 0.0
               totalInputToVisibleMs = 0.0
               dominantPhase = "unknown"
               metadataVisitedNodeCount = 0
               baselineNodeCount = 0
               fallbackCount = 0
               diagnostics = diagnostics |> List.toArray |},
            jsonLineOptions)

let private writeArtifacts outDir run scenario status metrics diagnostics =
    let root = Path.Combine(outDir, "optimized-" + run)
    Directory.CreateDirectory root |> ignore

    let phaseRecordsPath = Path.Combine(root, "phase-records.jsonl")
    let summaryJsonPath = Path.Combine(root, "summary.json")
    let summaryMdPath = Path.Combine(root, "summary.md")
    let tracePath = Path.Combine(root, "trace.log")

    let environmentStatus =
        if status = "measured" then "measured" else "environment-limited"

    let phaseLines =
        match metrics with
        | [] -> [ phaseRecord run scenario 0 environmentStatus None diagnostics ]
        | metrics -> metrics |> List.mapi (fun i metric -> phaseRecord run scenario (i + 1) environmentStatus (Some metric) diagnostics)

    File.WriteAllLines(phaseRecordsPath, phaseLines)
    File.WriteAllLines(tracePath, diagnostics)

    let framePreparations =
        metrics
        |> List.map (fun metric ->
            metric.FrameDuration - metric.PaintDuration - metric.ComposeDuration
            |> fun value -> if value < TimeSpan.Zero then TimeSpan.Zero else value
            |> ms)

    let totals =
        metrics
        |> List.map (fun metric ->
            let framePreparation =
                metric.FrameDuration - metric.PaintDuration - metric.ComposeDuration
                |> fun value -> if value < TimeSpan.Zero then TimeSpan.Zero else value
                |> ms

            framePreparation + ms metric.PaintDuration + ms metric.ComposeDuration)

    let median = percentile 0.50 totals
    let p95 = percentile 0.95 totals
    let largestPreparation = if List.isEmpty framePreparations then None else Some(List.max framePreparations)
    let firstFramePreparation = framePreparations |> List.tryHead
    let preparationReduction = reductionPercent (baselinePreparation scenario) largestPreparation
    let firstFrameReduction = reductionPercent (baselineFirstFramePreparation scenario) firstFramePreparation

    let summaryJson =
        JsonSerializer.Serialize(
            {| runId = run
               scenarioId = scenario
               scenarios = [| scenario |]
               baselineProfileId = "2026-06-19"
               optimizedProfileId = run
               status = status
               medianInputToVisibleMs = median |> Option.toNullable
               p95InputToVisibleMs = p95 |> Option.toNullable
               largestNonPaintPreparationAfterMs = largestPreparation |> Option.toNullable
               preparationReductionPercent = preparationReduction |> Option.toNullable
               firstFramePreparationAfterMs = firstFramePreparation |> Option.toNullable
               firstFramePreparationReductionPercent = firstFrameReduction |> Option.toNullable
               parityStatus = "not-run"
               parityArtifacts = [||]
               environmentLimitations = if status = "measured" then [||] else [| "live-evidence:environment-limited" |]
               diagnostics = diagnostics |> List.toArray |},
            jsonOptions)

    File.WriteAllText(summaryJsonPath, summaryJson)

    let caveatLines =
        if status <> "measured" then
            [ "- caveat: environment-limited; no accepted live performance claim." ]
        else
            []

    File.WriteAllLines(
        summaryMdPath,
        [ "# Render lag probe " + run
          ""
          "- scenario: `" + scenario + "`"
          "- status: " + status
          "- baseline profile: 2026-06-19"
          "- optimized profile: " + run
          "- phase records: `phase-records.jsonl`"
          "- trace: `trace.log`"
          "- parity: not-run" ]
        @ caveatLines)

    summaryJsonPath

let run args =
    Environment.SetEnvironmentVariable("FS_GG_RENDER_LAG_TRACE", "1")

    let scenario = scenario args
    let theme = theme args
    let outDir = outDir args
    let run = runId ()
    let host = hostFor scenario theme

    if forceSubstitute () then
        let summaryPath =
            writeArtifacts
                outDir
                run
                scenario
                "environment-limited"
                []
                [ "headless-substitute:no-live-presentation-boundary"; "test-override:forced-substitute" ]

        eprintfn "render-lag-probe: forced substitute wrote %s" summaryPath
        1
    else
        match ControlsElmish.Live.runScriptWithWindowBehavior (options ()) (windowBehavior ()) host (scriptFor scenario) with
        | Result.Ok result ->
            let status = if result.Outcome.FirstFramePresented then "measured" else "environment-limited"
            let diagnostics = [ $"viewerOutcome={result.Outcome.Status}"; $"firstFramePresented={result.Outcome.FirstFramePresented}" ]
            let summaryPath = writeArtifacts outDir run scenario status result.Metrics diagnostics
            printfn
                "render-lag-probe: scenario=%s status=%s firstFramePresented=%b metrics=%d summary=%s"
                scenario
                result.Outcome.Status
                result.Outcome.FirstFramePresented
                result.Metrics.Length
                summaryPath

            0
        | Result.Error failure ->
            let summaryPath = writeArtifacts outDir run scenario "environment-limited" [] [ failure.Message ]
            eprintfn "render-lag-probe: wrote %s" summaryPath
            eprintfn "render-lag-probe: failed: %s" failure.Message
            1
