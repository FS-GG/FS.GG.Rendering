module AppRoot.PerformanceEvidence

//#if (profile == "game")
open System
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Xml.Linq
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open AppRoot.Model
open AppRoot.View

type WorkloadClass =
    | NormalPlay
    | Stress
    | Throughput
    | LiveCompositor

type Budget =
    { P95Ms: float
      P99Ms: float
      MaximumSceneNodes: int
      AllowSustainedCatchUp: bool }

type Workload =
    { Id: string
      Definition: string
      Classification: WorkloadClass
      WarmupFrames: int
      SampleFrames: int
      EventsPerFrame: int
      PointerEventsPerFrame: int
      MessageAt: int -> Msg
      Budget: Budget option
      BlockingDebt: string option }

type Verdict = { Passed: bool; Reasons: string list }

type WorkloadResult =
    { Workload: Workload
      DefinitionDigest: string
      P50Ms: float
      P95Ms: float
      P99Ms: float
      UpdateCount: int
      PresentCount: int
      CatchUpFrames: int
      DroppedFrames: int
      EventCount: int
      PointerEventCount: int
      SceneNodeCount: int
      AllocatedBytes: int64
      Verdict: Verdict }

let private classToken =
    function
    | NormalPlay -> "normal"
    | Stress -> "stress"
    | Throughput -> "throughput"
    | LiveCompositor -> "live-compositor"

let private percentile value samples =
    match samples |> List.sort with
    | [] -> 0.0
    | sorted ->
        let index =
            Math.Ceiling(value / 100.0 * float sorted.Length)
            |> int
            |> fun i -> Math.Clamp(i - 1, 0, sorted.Length - 1)

        sorted.[index]

let definitionDigest workload =
    let budget =
        workload.Budget
        |> Option.map (fun b -> $"{b.P95Ms:R}|{b.P99Ms:R}|{b.MaximumSceneNodes}|{b.AllowSustainedCatchUp}")
        |> Option.defaultValue "none"

    let canonical =
        $"{workload.Id}|{workload.Definition}|{classToken workload.Classification}|{workload.WarmupFrames}|{workload.SampleFrames}|{workload.EventsPerFrame}|{workload.PointerEventsPerFrame}|{budget}"

    SHA256.HashData(Encoding.UTF8.GetBytes canonical)
    |> Convert.ToHexString
    |> _.ToLowerInvariant()

/// Shared expected-workload verdict semantics. Only normal-play workloads are release gates:
/// stress/throughput/live-compositor results remain separately classified evidence.
let evaluateBudget workload p95 p99 catchUpFrames sceneNodes =
    let linkedDebt =
        workload.BlockingDebt
        |> Option.exists (fun debt ->
            not (String.IsNullOrWhiteSpace debt)
            && (debt.Contains('#') || Uri.TryCreate(debt, UriKind.Absolute) |> fst))

    match workload.Classification, workload.Budget with
    | NormalPlay, None ->
        { Passed = false
          Reasons = [ "normal-play workload has no declared budget" ] }
    | NormalPlay, Some budget ->
        let reasons =
            [ if p95 > budget.P95Ms then
                  $"p95 {p95:F3} ms exceeds {budget.P95Ms:F3} ms"
              if p99 > budget.P99Ms then
                  $"p99 {p99:F3} ms exceeds {budget.P99Ms:F3} ms"
              if sceneNodes > budget.MaximumSceneNodes then
                  $"scene nodes {sceneNodes} exceed {budget.MaximumSceneNodes}"
              if catchUpFrames > 0 && not budget.AllowSustainedCatchUp then
                  $"sustained catch-up observed in {catchUpFrames} frame(s)" ]

        if List.isEmpty reasons then
            { Passed = true; Reasons = [] }
        elif linkedDebt then
            { Passed = true
              Reasons = "baseline-over-budget-with-linked-debt" :: reasons }
        else
            { Passed = false
              Reasons = "active normal-play target failed without a blocking debt reference" :: reasons }
    | _, _ ->
        { Passed = true
          Reasons = [ "informational non-normal workload; not used as the normal-play gate" ] }

let private runWorkload workload =
    let mutable model = initialModel

    for frame in 0 .. max 0 (workload.WarmupFrames - 1) do
        model <- fst (update (workload.MessageAt frame) model)
        view model |> ignore

    let samples = ResizeArray<float>()
    let beforeBytes = GC.GetAllocatedBytesForCurrentThread()
    let mutable sceneNodes = 0
    let mutable catchUp = 0

    for frame in 0 .. max 0 (workload.SampleFrames - 1) do
        let sw = Stopwatch.StartNew()
        model <- fst (update (workload.MessageAt frame) model)
        let scene = view model
        sw.Stop()
        samples.Add sw.Elapsed.TotalMilliseconds
        sceneNodes <- max sceneNodes (Scene.describe { Nodes = [ scene ] } |> List.length)

        if sw.Elapsed.TotalMilliseconds > 16.67 then
            catchUp <- catchUp + 1

    let allocated = GC.GetAllocatedBytesForCurrentThread() - beforeBytes
    let values = List.ofSeq samples

    let p50, p95, p99 =
        percentile 50.0 values, percentile 95.0 values, percentile 99.0 values

    { Workload = workload
      DefinitionDigest = definitionDigest workload
      P50Ms = p50
      P95Ms = p95
      P99Ms = p99
      UpdateCount = workload.SampleFrames
      PresentCount = 0
      CatchUpFrames = catchUp
      DroppedFrames = 0
      EventCount = workload.SampleFrames * workload.EventsPerFrame
      PointerEventCount = workload.SampleFrames * workload.PointerEventsPerFrame
      SceneNodeCount = sceneNodes
      AllocatedBytes = allocated
      Verdict = evaluateBudget workload p95 p99 catchUp sceneNodes }

let private declaredPackageVersions () =
    let path = Path.Combine(Directory.GetCurrentDirectory(), "Directory.Packages.props")

    if not (File.Exists path) then
        []
    else
        let document = XDocument.Load path

        let properties =
            document.Descendants()
            |> Seq.filter (fun element ->
                not (isNull element.Parent) && element.Parent.Name.LocalName = "PropertyGroup")
            |> Seq.map (fun element -> element.Name.LocalName, element.Value.Trim())
            |> Map.ofSeq

        let resolveVersion (version: string) =
            if version.StartsWith("$(") && version.EndsWith(")") then
                properties
                |> Map.tryFind (version.Substring(2, version.Length - 3))
                |> Option.defaultValue version
            else
                version

        document.Descendants(XName.Get "PackageVersion")
        |> Seq.choose (fun element ->
            let includeAttribute = element.Attribute(XName.Get "Include")
            let versionAttribute = element.Attribute(XName.Get "Version")

            if isNull includeAttribute || isNull versionAttribute then
                None
            else
                Some(includeAttribute.Value, resolveVersion versionAttribute.Value))
        |> Seq.sortBy fst
        |> List.ofSeq

let private normalBudget =
    { P95Ms = 16.67
      P99Ms = 25.0
      MaximumSceneNodes = 4096
      AllowSustainedCatchUp = false }

/// Replace or extend these named representative workloads as the product grows. Each one drives
/// the real `update` and scene `view` route; no local statistics framework is needed.
let expectedWorkloads =
    [ { Id = "idle"
        Definition = "fixed 60 Hz update and real scene route with no authored input"
        Classification = NormalPlay
        WarmupFrames = 20
        SampleFrames = 120
        EventsPerFrame = 0
        PointerEventsPerFrame = 0
        MessageAt = (fun _ -> Tick(1.0 / 60.0))
        Budget = Some normalBudget
        BlockingDebt = None }
      { Id = "movement-aiming"
        Definition = "alternating movement input and fixed 60 Hz update through the real route"
        Classification = NormalPlay
        WarmupFrames = 20
        SampleFrames = 120
        EventsPerFrame = 1
        PointerEventsPerFrame = 1
        MessageAt =
          (fun frame ->
              if frame % 2 = 0 then
                  ViewerInput(Letter 'W', true)
              else
                  Tick(1.0 / 60.0))
        Budget = Some normalBudget
        BlockingDebt = None }
      { Id = "firing"
        Definition = "replace NoOp with the product firing message; pointer and event facts retained"
        Classification = NormalPlay
        WarmupFrames = 20
        SampleFrames = 120
        EventsPerFrame = 1
        PointerEventsPerFrame = 1
        MessageAt = (fun _ -> NoOp)
        Budget = Some normalBudget
        BlockingDebt = None }
      { Id = "effects-fog"
        Definition = "replace Tick with the product effects/fog workload message sequence"
        Classification = NormalPlay
        WarmupFrames = 20
        SampleFrames = 120
        EventsPerFrame = 0
        PointerEventsPerFrame = 0
        MessageAt = (fun _ -> Tick(1.0 / 60.0))
        Budget = Some normalBudget
        BlockingDebt = None }
      { Id = "maximum-content"
        Definition = "replace Tick with the product maximum-content workload message sequence"
        Classification = NormalPlay
        WarmupFrames = 20
        SampleFrames = 120
        EventsPerFrame = 0
        PointerEventsPerFrame = 0
        MessageAt = (fun _ -> Tick(1.0 / 60.0))
        Budget = Some normalBudget
        BlockingDebt = None } ]

let writeExpectedWorkloadEvidence (path: string) =
    let results = expectedWorkloads |> List.map runWorkload
    let directory = Path.GetDirectoryName path

    if not (String.IsNullOrWhiteSpace directory) then
        Directory.CreateDirectory directory |> ignore

    use stream = File.Create path
    use json = new Utf8JsonWriter(stream, JsonWriterOptions(Indented = true))
    json.WriteStartObject()
    json.WriteNumber("schemaVersion", 1)
    json.WriteString("measurementCapability", "bounded-headless-update-and-scene-route")
    json.WriteString("notAuthoritativeFor", "live-compositor,swapchain,vblank,vsync")

    json.WriteString(
        "hostProfile",
        $"{Environment.OSVersion.Platform};{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture};{Environment.Version}"
    )

    json.WriteStartObject("packageVersions")

    for packageId, version in declaredPackageVersions () do
        json.WriteString(packageId, version)

    json.WriteEndObject()
    json.WriteString("warmupSamplePolicy", "per-workload; monotonic Stopwatch; warmup excluded")
    json.WriteStartArray("workloads")

    for result in results do
        json.WriteStartObject()
        json.WriteString("id", result.Workload.Id)
        json.WriteString("definition", result.Workload.Definition)
        json.WriteString("class", classToken result.Workload.Classification)
        json.WriteString("definitionDigest", result.DefinitionDigest)
        json.WriteNumber("warmupFrames", result.Workload.WarmupFrames)
        json.WriteNumber("sampleFrames", result.Workload.SampleFrames)
        json.WriteNumber("p50Ms", result.P50Ms)
        json.WriteNumber("p95Ms", result.P95Ms)
        json.WriteNumber("p99Ms", result.P99Ms)
        json.WriteNumber("updateCount", result.UpdateCount)
        json.WriteNumber("presentCount", result.PresentCount)
        json.WriteNumber("catchUpFrames", result.CatchUpFrames)
        json.WriteNumber("droppedFrames", result.DroppedFrames)
        json.WriteNumber("eventCount", result.EventCount)
        json.WriteNumber("pointerEventCount", result.PointerEventCount)
        json.WriteNumber("allocatedBytes", result.AllocatedBytes)
        json.WriteStartObject("sceneNodesByLayer")
        json.WriteNumber("product-scene", result.SceneNodeCount)
        json.WriteEndObject()
        json.WriteBoolean("passed", result.Verdict.Passed)
        json.WriteStartArray("reasons")
        result.Verdict.Reasons |> List.iter json.WriteStringValue
        json.WriteEndArray()
        json.WriteEndObject()

    json.WriteEndArray()
    json.WriteEndObject()
    json.Flush()

    let failures = results |> List.filter (_.Verdict.Passed >> not)

    if List.isEmpty failures then
        printfn
            "status=ok performance-evidence workloads=%d capability=bounded-headless artifact=%s"
            results.Length
            path

        0
    else
        failures
        |> List.iter (fun result ->
            printfn
                "status=failed workload=%s reasons=%s"
                result.Workload.Id
                (String.concat " | " result.Verdict.Reasons))

        1
//#else
let writeExpectedWorkloadEvidence _ = 0
//#endif
