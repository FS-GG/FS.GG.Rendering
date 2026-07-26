module AppRoot.PerformanceEvidence

//#if (profile == "game")
open System
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
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

/// A deliberate acknowledgement that a representative workload is product-authored.
///
/// Start in `Placeholder`, run `PerformanceEvidence`, then copy the emitted `definitionDigest`
/// into `Authored` only after replacing the starter state/message route. The digest covers the
/// authored definition and measurement policy. Changing either invalidates the acknowledgement
/// and fails closed until the new digest is reviewed and copied.
type WorkloadAuthorship =
    | Placeholder of requiredWork: string
    | Authored of definitionDigest: string

type Workload =
    { Id: string
      Definition: string
      Classification: WorkloadClass
      WarmupFrames: int
      SampleFrames: int
      EventsPerFrame: int
      PointerEventsPerFrame: int
      InitialState: unit -> Model
      MessageAt: int -> Msg
      Budget: Budget option
      BlockingDebt: string option
      Authorship: WorkloadAuthorship }

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

let private sha256Text (text: string) =
    SHA256.HashData(Encoding.UTF8.GetBytes text)
    |> Convert.ToHexString
    |> _.ToLowerInvariant()

let private declarationPattern =
    Regex(
        @"Authorship\s*=\s*(?:Placeholder\s+""[^""]*""|Authored\s+""[^""]*"")",
        RegexOptions.CultureInvariant
    )

let private debtPattern =
    Regex(
        @"BlockingDebt\s*=\s*(?:None|Some\s+""[^""]*"")",
        RegexOptions.CultureInvariant
    )

let private countOccurrences (needle: string) (text: string) =
    let rec loop start count =
        let found = text.IndexOf(needle, start, StringComparison.Ordinal)

        if found < 0 then
            count
        else
            loop (found + needle.Length) (count + 1)

    loop 0 0

/// Fingerprint the executable source block for one workload. This binds the declaration to the
/// actual InitialState/MessageAt code rather than trusting its prose. The declaration itself is
/// normalized to a sentinel so copying the emitted digest into `Authored` is not circular.
let private workloadSourceFingerprint id =
    let sourcePath = Path.Combine(__SOURCE_DIRECTORY__, "PerformanceEvidence.fs")

    if not (File.Exists sourcePath) then
        None
    else
        let source = File.ReadAllText sourcePath
        let beginMarker = $"// WORKLOAD-SOURCE-BEGIN {id}"
        let endMarker = $"// WORKLOAD-SOURCE-END {id}"
        let start = source.IndexOf(beginMarker, StringComparison.Ordinal)
        let finish = source.IndexOf(endMarker, max 0 (start + beginMarker.Length), StringComparison.Ordinal)

        if
            countOccurrences beginMarker source <> 1
            || countOccurrences endMarker source <> 1
            || start < 0
            || finish < 0
            || finish <= start
        then
            None
        else
            source.Substring(start, finish + endMarker.Length - start)
            |> fun block -> declarationPattern.Replace(block, "Authorship = <declaration>")
            |> fun block -> debtPattern.Replace(block, "BlockingDebt = <debt>")
            |> _.Replace("\r\n", "\n")
            |> _.Trim()
            |> sha256Text
            |> Some

let definitionDigest workload =
    let budget =
        workload.Budget
        |> Option.map (fun b -> $"{b.P95Ms:R}|{b.P99Ms:R}|{b.MaximumSceneNodes}|{b.AllowSustainedCatchUp}")
        |> Option.defaultValue "none"

    let executableSource =
        workloadSourceFingerprint workload.Id
        |> Option.defaultValue "missing-workload-source-block"

    let canonical =
        $"{workload.Id}|{workload.Definition}|{classToken workload.Classification}|{workload.WarmupFrames}|{workload.SampleFrames}|{workload.EventsPerFrame}|{workload.PointerEventsPerFrame}|{budget}|{executableSource}"

    sha256Text canonical

let private ownerRepoIssue =
    Regex(
        @"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+#[1-9][0-9]*$",
        RegexOptions.CultureInvariant
    )

let private linkedDebtReference (debt: string) =
    let isGitHubIssueUrl =
        match Uri.TryCreate(debt, UriKind.Absolute) with
        | true, uri when uri.Scheme = Uri.UriSchemeHttps && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ->
            let segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries)
            segments.Length = 4
            && segments.[0] <> ""
            && segments.[1] <> ""
            && segments.[2].Equals("issues", StringComparison.OrdinalIgnoreCase)
            && (match Int32.TryParse segments.[3] with
                | true, number -> number > 0
                | _ -> false)
        | _ -> false

    not (String.IsNullOrWhiteSpace debt)
    && (ownerRepoIssue.IsMatch debt || isGitHubIssueUrl)

/// Expected-workload budget semantics. A linked debt permits deliberate BASELINE CAPTURE, not
/// acceptance: its artifact is retained, but Test/Verify still fail until the active target passes.
/// Only normal-play workloads are budget gates; other classes remain separately classified evidence.
let evaluateBudget workload p95 p99 catchUpFrames sceneNodes =
    let budgetVerdict =
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

            { Passed = List.isEmpty reasons
              Reasons =
                if List.isEmpty reasons then
                    []
                else
                    "active normal-play target failed; a linked blocking debt permits baseline capture only, never acceptance"
                    :: reasons }
        | _, _ ->
            { Passed = true
              Reasons = [ "informational non-normal workload; not used as the normal-play budget gate" ] }

    match workload.BlockingDebt with
    | None -> budgetVerdict
    | Some debt when not (linkedDebtReference debt) ->
        { Passed = false
          Reasons =
            "baseline capture requires a linked blocking performance-debt issue (owner/repo#number or https://github.com/owner/repo/issues/number); open/blocking state is validated by the governance network edge"
            :: budgetVerdict.Reasons }
    | Some debt ->
        { Passed = false
          Reasons =
            $"baseline-only-with-linked-debt {debt}; captured evidence does not satisfy acceptance"
            :: budgetVerdict.Reasons }

let evaluateAuthorship workload =
    let actualDigest = definitionDigest workload

    match workloadSourceFingerprint workload.Id, workload.Authorship with
    | None, _ ->
        { Passed = false
          Reasons =
            [ $"workload '{workload.Id}' has no readable WORKLOAD-SOURCE block; executable state/message authorship cannot be verified" ] }
    | Some _, Placeholder requiredWork ->
        { Passed = false
          Reasons = [ $"required workload '{workload.Id}' is still a placeholder: {requiredWork}" ] }
    | Some _, Authored declaredDigest when
        not (String.Equals(declaredDigest, actualDigest, StringComparison.OrdinalIgnoreCase))
        ->
        { Passed = false
          Reasons =
            [ $"authored declaration is stale for workload '{workload.Id}': declared {declaredDigest}, current {actualDigest}; review the changed definition and copy the new digest" ] }
    | Some _, Authored _ -> { Passed = true; Reasons = [] }

let private runWorkload workload =
    let mutable model = workload.InitialState()

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

    let digest = definitionDigest workload
    let authorshipVerdict = evaluateAuthorship workload
    let budgetVerdict = evaluateBudget workload p95 p99 catchUp sceneNodes

    { Workload = workload
      DefinitionDigest = digest
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
      Verdict =
        { Passed = authorshipVerdict.Passed && budgetVerdict.Passed
          Reasons = authorshipVerdict.Reasons @ budgetVerdict.Reasons } }

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

/// REQUIRED PRODUCT AUTHORING. Every untouched row is deliberately a failing placeholder.
///
/// For each row: replace `InitialState` and `MessageAt` with representative product state/messages,
/// rewrite `Definition` to name that route, run PerformanceEvidence once, review the emitted
/// `definitionDigest`, then change `Placeholder` to `Authored "<digest>"`. The measurement always
/// drives the real `update` + scene `view` route; there is no local statistics-only escape hatch.
let expectedWorkloads =
    [ // WORKLOAD-SOURCE-BEGIN idle
      { Id = "idle"
        Definition = "PLACEHOLDER: author representative idle state and messages through update + view"
        Classification = NormalPlay
        WarmupFrames = 20
        SampleFrames = 120
        EventsPerFrame = 0
        PointerEventsPerFrame = 0
        InitialState = (fun () -> initialModel)
        MessageAt = (fun _ -> Tick(1.0 / 60.0))
        Budget = Some normalBudget
        BlockingDebt = None
        Authorship = Placeholder "replace starter idle state/message route, then copy the emitted definitionDigest" }
      // WORKLOAD-SOURCE-END idle
      // WORKLOAD-SOURCE-BEGIN movement-aiming
      { Id = "movement-aiming"
        Definition = "PLACEHOLDER: author simultaneous movement and aiming state/messages through update + view"
        Classification = NormalPlay
        WarmupFrames = 20
        SampleFrames = 120
        EventsPerFrame = 1
        PointerEventsPerFrame = 1
        InitialState = (fun () -> initialModel)
        MessageAt =
          (fun frame ->
              if frame % 2 = 0 then
                  ViewerInput(Letter 'W', true)
              else
                  Tick(1.0 / 60.0))
        Budget = Some normalBudget
        BlockingDebt = None
        Authorship = Placeholder "replace starter keyboard/tick route with product movement plus aiming" }
      // WORKLOAD-SOURCE-END movement-aiming
      // WORKLOAD-SOURCE-BEGIN firing
      { Id = "firing"
        Definition = "PLACEHOLDER: author combat/firing state and messages through update + view"
        Classification = NormalPlay
        WarmupFrames = 20
        SampleFrames = 120
        EventsPerFrame = 1
        PointerEventsPerFrame = 1
        InitialState = (fun () -> initialModel)
        MessageAt = (fun _ -> NoOp)
        Budget = Some normalBudget
        BlockingDebt = None
        Authorship = Placeholder "replace NoOp with representative combat and firing messages" }
      // WORKLOAD-SOURCE-END firing
      // WORKLOAD-SOURCE-BEGIN effects-fog
      { Id = "effects-fog"
        Definition = "PLACEHOLDER: author effects/fog state and messages through update + view"
        Classification = NormalPlay
        WarmupFrames = 20
        SampleFrames = 120
        EventsPerFrame = 0
        PointerEventsPerFrame = 0
        InitialState = (fun () -> initialModel)
        MessageAt = (fun _ -> Tick(1.0 / 60.0))
        Budget = Some normalBudget
        BlockingDebt = None
        Authorship = Placeholder "replace Tick with the product effects and fog workload route" }
      // WORKLOAD-SOURCE-END effects-fog
      // WORKLOAD-SOURCE-BEGIN maximum-content
      { Id = "maximum-content"
        Definition = "PLACEHOLDER: author maximum-expected-content state and messages through update + view"
        Classification = NormalPlay
        WarmupFrames = 20
        SampleFrames = 120
        EventsPerFrame = 0
        PointerEventsPerFrame = 0
        InitialState = (fun () -> initialModel)
        MessageAt = (fun _ -> Tick(1.0 / 60.0))
        Budget = Some normalBudget
        BlockingDebt = None
        Authorship = Placeholder "replace Tick with the maximum expected product content route" }
      // WORKLOAD-SOURCE-END maximum-content
      ]

let writeExpectedWorkloadEvidence (path: string) =
    let results = expectedWorkloads |> List.map runWorkload
    let directory = Path.GetDirectoryName path

    if not (String.IsNullOrWhiteSpace directory) then
        Directory.CreateDirectory directory |> ignore

    use stream = File.Create path
    use json = new Utf8JsonWriter(stream, JsonWriterOptions(Indented = true))
    json.WriteStartObject()
    json.WriteNumber("schemaVersion", 2)
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

        match result.Workload.Authorship with
        | Placeholder requiredWork ->
            json.WriteString("authorship", "placeholder")
            json.WriteString("requiredAuthoringWork", requiredWork)
            json.WriteNull("declaredDefinitionDigest")
        | Authored declaredDigest ->
            json.WriteString("authorship", "authored")
            json.WriteNull("requiredAuthoringWork")
            json.WriteString("declaredDefinitionDigest", declaredDigest)

        match result.Workload.BlockingDebt with
        | Some debt -> json.WriteString("blockingDebt", debt)
        | None -> json.WriteNull("blockingDebt")

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
