module AppRoot.PerformanceEvidence

//#if (profile == "game")
open System
open System.Diagnostics
open System.Globalization
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open System.Xml.Linq
open Fsgg.Schemas
open FS.GG.Game.Harness
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

/// The one product-authored performance policy. This is the published Contracts 7.x shape used by
/// SDD, not a template-local mirror. Workload identities and executable definition digests are
/// projected into the completed value below after the workload rows are declared.
let private performanceIntentSeed: PerformanceIntentDeclaration =
    { Id = "PI-GENERATED-GAME"
      Disposition = "active"
      TargetFps = 60
      WorkloadIds = []
      WorkloadDefinitionDigests = []
      MaximumExpectedScale = "maximum-content workload"
      MaxP95Ms = 16.67m
      MaxP99Ms = 25.0m
      MaxCatchUpFrames = 0
      StructuralCostBudgets = [ "scene-nodes<=4096" ]
      RequiredCapability = "bounded-headless-update-and-scene-route"
      LiveCompositorRequired = false
      DeferralIssue = None
      EvidenceRefs = [ "readiness/performance-evidence.json" ]
      Rationale = Some "Generated normal-play declaration; live-compositor evidence remains a separate workload." }

let private maximumSceneNodes =
    performanceIntentSeed.StructuralCostBudgets
    |> List.tryPick (fun entry ->
        match entry.Split("<=", StringSplitOptions.TrimEntries) with
        | [| "scene-nodes"; value |] ->
            match Int32.TryParse value with
            | true, parsed when parsed > 0 -> Some parsed
            | _ -> None
        | _ -> None)
    |> Option.defaultWith (fun () ->
        invalidOp "performance intent must declare structuralCostBudgets entry 'scene-nodes<=<positive integer>'")

/// A deliberate acknowledgement that a representative workload is product-authored.
///
/// Start in `Placeholder`, run `PerformanceEvidence`, then copy the emitted `definitionDigest`
/// into `Authored` only after replacing the starter state/message route. The digest covers the
/// authored definition and measurement policy. Changing either invalidates the acknowledgement
/// and fails closed until the new digest is reviewed and copied.
type WorkloadAuthorship =
    | Placeholder of requiredWork: string
    | Authored of definitionDigest: string

type WorkloadProvenance =
    | RunnerIssuedJourney of JourneyReceipt
    | SyntheticConstructed of reason: string

type CompositionClaim =
    | CompleteComposition
    | ComponentOnlySupplemental of reason: string

type RoutedStimulus =
    { Events: int
      PointerEvents: int
      RawInputSamples: int }

type CapabilityMetric =
    | Observed of value: int
    | Unsupported of reason: string

type CostDriverCategory =
    | Simulation
    | AiPathfindingPerception
    | Input
    | SceneRender
    | UiControl
    | EffectsParticles
    | PersistenceEffectResult
    | HostPresentation

type CostDriverDisposition =
    | RequiredIn of workloadIds: string list
    | NonPerformance of reason: string

type PerformanceCostDriver =
    { Id: string
      Category: CostDriverCategory
      ScaleSource: string
      MaximumExpected: int
      VisualElement: string option
      Disposition: CostDriverDisposition }

/// Independent product inventory. It is intentionally not derived from `expectedWorkloads`: adding a
/// gameplay visual or cost driver must edit this list, and the coverage gate compares the two sets.
let performanceCostDrivers =
    [ { Id = "simulation.fixed-step"
        Category = Simulation
        ScaleSource = "Model; one shipped update per sampled frame"
        MaximumExpected = 1
        VisualElement = None
        Disposition = RequiredIn [ "idle"; "movement-aiming"; "firing"; "effects-fog"; "maximum-content" ] }
      { Id = "input.viewer-route"
        Category = Input
        ScaleSource = "shipped host-input mapping and routed-input receipt"
        MaximumExpected = 1
        VisualElement = None
        Disposition = RequiredIn [ "movement-aiming"; "firing" ] }
      { Id = "scene.ball"
        Category = SceneRender
        ScaleSource = "GameplayVisualInventory.Ball"
        MaximumExpected = 1
        VisualElement = Some "Ball"
        Disposition = RequiredIn [ "movement-aiming"; "firing"; "maximum-content" ] }
      { Id = "scene.left-paddle"
        Category = SceneRender
        ScaleSource = "GameplayVisualInventory.LeftPaddle"
        MaximumExpected = 1
        VisualElement = Some "LeftPaddle"
        Disposition = RequiredIn [ "movement-aiming"; "maximum-content" ] }
      { Id = "scene.right-paddle"
        Category = SceneRender
        ScaleSource = "GameplayVisualInventory.RightPaddle"
        MaximumExpected = 1
        VisualElement = Some "RightPaddle"
        Disposition = RequiredIn [ "maximum-content" ] }
      { Id = "ui.score"
        Category = UiControl
        ScaleSource = "GameplayVisualInventory.Score"
        MaximumExpected = 1
        VisualElement = Some "Score"
        Disposition = RequiredIn [ "firing"; "maximum-content" ] }
      { Id = "scene.playfield"
        Category = SceneRender
        ScaleSource = "GameplayVisualInventory.Playfield"
        MaximumExpected = 1
        VisualElement = Some "Playfield"
        Disposition = RequiredIn [ "idle"; "movement-aiming"; "firing"; "effects-fog"; "maximum-content" ] }
      { Id = "effects.product"
        Category = EffectsParticles
        ScaleSource = "starter has no effect/particle system; replace this disposition when one is added"
        MaximumExpected = 1
        VisualElement = None
        Disposition = NonPerformance "no effect/particle system exists in the generated starter" }
      { Id = "host.presentation"
        Category = HostPresentation
        ScaleSource = "protected live-compositor host"
        MaximumExpected = 1
        VisualElement = None
        Disposition =
            NonPerformance
                "bounded headless evidence cannot measure present/drop/swapchain/vsync; use a live-compositor workload" } ]

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
      Provenance: WorkloadProvenance
      Composition: CompositionClaim
      CostDriverIds: string list
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
      PresentCount: CapabilityMetric
      CatchUpFrames: int
      DroppedFrames: CapabilityMetric
      DeclaredEventCount: int
      ObservedEventCount: int
      DeclaredPointerEventCount: int
      ObservedPointerEventCount: int
      RawInputSampleCount: int
      SceneNodeCount: int
      ObservedScale: Map<string, int>
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

let private journeyKind = "produc" + "tion-journey"
let private completeCompositionKind = "produc" + "tion-composition"

let private runnerReceiptToken (receipt: JourneyReceipt) =
    [ string (JourneyReceipt.schemaVersion receipt)
      JourneyReceipt.runnerIdentity receipt
      JourneyReceipt.runnerVersion receipt
      JourneyReceipt.compositionAuthority receipt
      string (JourneyReceipt.origin receipt)
      JourneyReceipt.routeId receipt
      JourneyReceipt.scenarioId receipt
      JourneyReceipt.testId receipt
      string (JourneyReceipt.inputKind receipt)
      JourneyReceipt.inputIdentity receipt
      JourneyReceipt.inputDigest receipt
      JourneyReceipt.scriptDigest receipt
      JourneyReceipt.traceDigest receipt
      JourneyReceipt.initialFingerprintDigest receipt
      JourneyReceipt.terminalFingerprintDigest receipt
      JourneyReceipt.terminalPredicateIdentity receipt
      string (JourneyReceipt.terminalPredicateReached receipt)
      string (JourneyReceipt.result receipt)
      string (JourneyReceipt.steps receipt)
      string (JourneyReceipt.maxSteps receipt) ]
    |> String.concat "|"
    |> sha256Text

let private provenanceToken =
    function
    | RunnerIssuedJourney receipt -> $"{journeyKind}:{runnerReceiptToken receipt}"
    | SyntheticConstructed reason -> $"synthetic-constructed:{reason}"

// Authorship changes rebuild this assembly and therefore change the runner receipt's composition
// authority MVID. Keep that volatile build identity in the critic digest above, but exclude it from
// the source-declaration digest so copying the emitted authorship digest is not circular.
let private provenanceDefinitionToken =
    function
    | RunnerIssuedJourney receipt ->
        [ journeyKind
          JourneyReceipt.routeId receipt
          JourneyReceipt.scenarioId receipt
          JourneyReceipt.testId receipt
          JourneyReceipt.inputIdentity receipt
          JourneyReceipt.inputDigest receipt
          JourneyReceipt.scriptDigest receipt
          JourneyReceipt.traceDigest receipt
          JourneyReceipt.initialFingerprintDigest receipt
          JourneyReceipt.terminalFingerprintDigest receipt
          JourneyReceipt.terminalPredicateIdentity receipt
          string (JourneyReceipt.terminalPredicateReached receipt)
          string (JourneyReceipt.result receipt)
          string (JourneyReceipt.steps receipt)
          string (JourneyReceipt.maxSteps receipt) ]
        |> String.concat "|"
        |> sha256Text
        |> fun digest -> $"{journeyKind}:{digest}"
    | SyntheticConstructed reason -> $"synthetic-constructed:{reason}"

let private compositionToken =
    function
    | CompleteComposition -> completeCompositionKind
    | ComponentOnlySupplemental reason -> $"component-only-supplemental:{reason}"

let private evaluateProvenance workload =
    let validate (receipt: JourneyReceipt) =
        let expectedOrigin = "Produc" + "tionJourney"
        let required =
            [ "runner identity", JourneyReceipt.runnerIdentity receipt
              "runner version", JourneyReceipt.runnerVersion receipt
              "composition authority", JourneyReceipt.compositionAuthority receipt
              "route id", JourneyReceipt.routeId receipt
              "scenario id", JourneyReceipt.scenarioId receipt
              "test id", JourneyReceipt.testId receipt
              "input identity", JourneyReceipt.inputIdentity receipt
              "input digest", JourneyReceipt.inputDigest receipt
              "script digest", JourneyReceipt.scriptDigest receipt
              "trace digest", JourneyReceipt.traceDigest receipt
              "initial fingerprint", JourneyReceipt.initialFingerprintDigest receipt
              "terminal fingerprint", JourneyReceipt.terminalFingerprintDigest receipt
              "terminal predicate identity", JourneyReceipt.terminalPredicateIdentity receipt ]

        [ if JourneyReceipt.schemaVersion receipt <> 1 then
              $"workload '{workload.Id}' runner receipt schema is unsupported"
          if not (String.Equals(string (JourneyReceipt.origin receipt), expectedOrigin, StringComparison.Ordinal)) then
              $"workload '{workload.Id}' receipt did not originate from the shipped journey runner"
          for label, value in required do
              if String.IsNullOrWhiteSpace value then
                  $"workload '{workload.Id}' runner receipt is missing {label}"
          if JourneyReceipt.result receipt <> JourneyResult.Passed then
              $"workload '{workload.Id}' runner receipt did not pass"
          if not (JourneyReceipt.terminalPredicateReached receipt) then
              $"workload '{workload.Id}' runner receipt did not reach its terminal predicate"
          if
              JourneyReceipt.steps receipt <= 0
              || JourneyReceipt.steps receipt > JourneyReceipt.maxSteps receipt
          then
              $"workload '{workload.Id}' runner receipt has invalid bounded steps" ]

    let reasons =
        match workload.Provenance with
        | RunnerIssuedJourney receipt -> validate receipt
        | SyntheticConstructed reason ->
            [ $"workload '{workload.Id}' is synthetic-constructed ({reason}); it may support component/stress/throughput evidence but cannot establish shipped-route normal-play or maximum-scale coverage" ]

    let reasons =
        match workload.Classification, workload.Composition with
        | NormalPlay, ComponentOnlySupplemental reason ->
            $"workload '{workload.Id}' is component-only supplemental ({reason}); it cannot claim complete normal-play composition"
            :: reasons
        | _ -> reasons

    { Passed = List.isEmpty reasons; Reasons = reasons }

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

    let structuralBudgets = String.concat "," performanceIntentSeed.StructuralCostBudgets

    let maxP95 = performanceIntentSeed.MaxP95Ms.ToString(CultureInfo.InvariantCulture)
    let maxP99 = performanceIntentSeed.MaxP99Ms.ToString(CultureInfo.InvariantCulture)

    let intentPolicy =
        $"{performanceIntentSeed.Id}|{performanceIntentSeed.Disposition}|{performanceIntentSeed.TargetFps}|{performanceIntentSeed.MaximumExpectedScale}|{maxP95}|{maxP99}|{performanceIntentSeed.MaxCatchUpFrames}|{structuralBudgets}|{performanceIntentSeed.RequiredCapability}|{performanceIntentSeed.LiveCompositorRequired}"

    let costDriverIds = String.concat "," workload.CostDriverIds

    let canonical =
        $"{workload.Id}|{workload.Definition}|{classToken workload.Classification}|{workload.WarmupFrames}|{workload.SampleFrames}|{workload.EventsPerFrame}|{workload.PointerEventsPerFrame}|{provenanceDefinitionToken workload.Provenance}|{compositionToken workload.Composition}|{costDriverIds}|{budget}|{intentPolicy}|{executableSource}"

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
                  if catchUpFrames > performanceIntentSeed.MaxCatchUpFrames then
                      $"sustained catch-up observed in {catchUpFrames} frame(s), exceeding declared maximum {performanceIntentSeed.MaxCatchUpFrames}" ]

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

let private observeRoutedStimulus message =
    match message with
    | ViewerInput _ ->
        { Events = 1
          PointerEvents = 0
          RawInputSamples = 1 }
    | _ ->
        { Events = 0
          PointerEvents = 0
          RawInputSamples = 0 }

let private observeCostScale driverId routed model =
    match performanceCostDrivers |> List.tryFind (fun driver -> driver.Id = driverId) with
    | Some driver ->
        match driver.Category, driver.VisualElement with
        | Simulation, _ -> 1
        | Input, _ -> routed.RawInputSamples
        | (SceneRender | UiControl), Some elementId ->
            GameplayVisualInventory.project model
            |> List.filter (fun item -> GameplayVisualInventory.elementId item.Element = elementId)
            |> List.length
        | _ -> 0
    | None -> 0

let private runWorkload workload =
    let mutable model = workload.InitialState()

    for frame in 0 .. max 0 (workload.WarmupFrames - 1) do
        model <- fst (update (workload.MessageAt frame) model)
        view model |> ignore

    let samples = ResizeArray<float>()
    let beforeBytes = GC.GetAllocatedBytesForCurrentThread()
    let mutable sceneNodes = 0
    let mutable catchUp = 0
    let mutable observedEvents = 0
    let mutable observedPointerEvents = 0
    let mutable rawInputSamples = 0
    let mutable observedScale = Map.empty
    let targetFrameMs = 1000.0 / float performanceIntentSeed.TargetFps

    for frame in 0 .. max 0 (workload.SampleFrames - 1) do
        let sw = Stopwatch.StartNew()
        let message = workload.MessageAt frame
        model <- fst (update message model)
        let scene = view model
        sw.Stop()
        let routed = observeRoutedStimulus message
        observedEvents <- observedEvents + routed.Events
        observedPointerEvents <- observedPointerEvents + routed.PointerEvents
        rawInputSamples <- rawInputSamples + routed.RawInputSamples
        observedScale <-
            workload.CostDriverIds
            |> List.fold (fun scales id ->
                let count = observeCostScale id routed model
                scales
                |> Map.change id (fun previous -> Some(max count (previous |> Option.defaultValue 0)))) observedScale
        samples.Add sw.Elapsed.TotalMilliseconds
        sceneNodes <- max sceneNodes (Scene.describe { Nodes = [ scene ] } |> List.length)

        if sw.Elapsed.TotalMilliseconds > targetFrameMs then
            catchUp <- catchUp + 1

    let allocated = GC.GetAllocatedBytesForCurrentThread() - beforeBytes
    let values = List.ofSeq samples

    let p50, p95, p99 =
        percentile 50.0 values, percentile 95.0 values, percentile 99.0 values

    let digest = definitionDigest workload
    let authorshipVerdict = evaluateAuthorship workload
    let provenanceVerdict = evaluateProvenance workload
    let budgetVerdict = evaluateBudget workload p95 p99 catchUp sceneNodes
    let declaredEvents = workload.SampleFrames * workload.EventsPerFrame
    let declaredPointerEvents = workload.SampleFrames * workload.PointerEventsPerFrame
    let routeReasons =
        [ if declaredEvents <> observedEvents then
              $"workload '{workload.Id}' declared event count {declaredEvents}, observed routed count {observedEvents}; bind the message to the missing shipped-route seam"
          if declaredPointerEvents <> observedPointerEvents then
              $"workload '{workload.Id}' declared pointer event count {declaredPointerEvents}, observed routed count {observedPointerEvents}; bind the message to the missing shipped-route seam" ]

    { Workload = workload
      DefinitionDigest = digest
      P50Ms = p50
      P95Ms = p95
      P99Ms = p99
      UpdateCount = workload.SampleFrames
      PresentCount = Unsupported "bounded-headless route has no compositor presentation capability"
      CatchUpFrames = catchUp
      DroppedFrames = Unsupported "bounded-headless route has no swapchain/drop observation capability"
      DeclaredEventCount = declaredEvents
      ObservedEventCount = observedEvents
      DeclaredPointerEventCount = declaredPointerEvents
      ObservedPointerEventCount = observedPointerEvents
      RawInputSampleCount = rawInputSamples
      SceneNodeCount = sceneNodes
      ObservedScale = observedScale
      AllocatedBytes = allocated
      Verdict =
        { Passed =
            authorshipVerdict.Passed
            && provenanceVerdict.Passed
            && budgetVerdict.Passed
            && List.isEmpty routeReasons
          Reasons =
            authorshipVerdict.Reasons
            @ provenanceVerdict.Reasons
            @ routeReasons
            @ budgetVerdict.Reasons } }

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
    { P95Ms = float performanceIntentSeed.MaxP95Ms
      P99Ms = float performanceIntentSeed.MaxP99Ms
      MaximumSceneNodes = maximumSceneNodes
      AllowSustainedCatchUp = performanceIntentSeed.MaxCatchUpFrames > 0 }

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
        Provenance = SyntheticConstructed "starter state has no opaque runner-issued journey receipt"
        Composition = CompleteComposition
        CostDriverIds = [ "simulation.fixed-step"; "scene.playfield" ]
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
        Provenance = SyntheticConstructed "starter state has no opaque runner-issued journey receipt"
        Composition = CompleteComposition
        CostDriverIds =
            [ "simulation.fixed-step"
              "input.viewer-route"
              "scene.ball"
              "scene.left-paddle"
              "scene.playfield" ]
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
        Provenance = SyntheticConstructed "starter state has no opaque runner-issued journey receipt"
        Composition = CompleteComposition
        CostDriverIds =
            [ "simulation.fixed-step"
              "input.viewer-route"
              "scene.ball"
              "ui.score"
              "scene.playfield" ]
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
        Provenance = SyntheticConstructed "starter state has no opaque runner-issued journey receipt"
        Composition = CompleteComposition
        CostDriverIds = [ "simulation.fixed-step"; "scene.playfield" ]
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
        Provenance = SyntheticConstructed "starter state has no opaque runner-issued journey receipt"
        Composition = CompleteComposition
        CostDriverIds =
            [ "simulation.fixed-step"
              "scene.ball"
              "scene.left-paddle"
              "scene.right-paddle"
              "ui.score"
              "scene.playfield" ]
        Budget = Some normalBudget
        BlockingDebt = None
        Authorship = Placeholder "replace Tick with the maximum expected product content route" }
      // WORKLOAD-SOURCE-END maximum-content
      ]

let performanceIntentDeclaration =
    { performanceIntentSeed with
        WorkloadIds = expectedWorkloads |> List.map _.Id
        WorkloadDefinitionDigests =
            expectedWorkloads
            |> List.map (fun workload -> $"{workload.Id}=sha256:{definitionDigest workload}") }

let private duplicateValues values =
    values
    |> List.countBy id
    |> List.choose (fun (value, count) -> if count > 1 then Some value else None)

let private requiredNormalWorkloadIds =
    [ "idle"; "movement-aiming"; "firing"; "effects-fog"; "maximum-content" ]

let private costDriverProblems (results: WorkloadResult list) =
    let workloadById = expectedWorkloads |> List.map (fun workload -> workload.Id, workload) |> Map.ofList
    let resultById = results |> List.map (fun result -> result.Workload.Id, result) |> Map.ofList
    let driverById = performanceCostDrivers |> List.map (fun driver -> driver.Id, driver) |> Map.ofList
    let duplicateDriverIds = performanceCostDrivers |> List.map _.Id |> duplicateValues
    let inventoryVisuals =
        performanceCostDrivers
        |> List.choose _.VisualElement
        |> List.sort
    let shippedVisuals =
        GameplayVisualInventory.all
        |> List.map GameplayVisualInventory.elementId
        |> List.sort
    let duplicateDriverText = String.concat ", " duplicateDriverIds
    let shippedVisualText = String.concat "," shippedVisuals
    let inventoryVisualText = String.concat "," inventoryVisuals

    [ if not (List.isEmpty duplicateDriverIds) then
          $"duplicate performance cost-driver ids: {duplicateDriverText}"
      if inventoryVisuals <> (List.distinct inventoryVisuals) then
          "duplicate visual-element bindings in the performance cost-driver inventory"
      if inventoryVisuals <> shippedVisuals then
          $"performance visual coverage differs from GameplayVisualInventory; required={shippedVisualText}; bound={inventoryVisualText}"
      for driver in performanceCostDrivers do
          if String.IsNullOrWhiteSpace driver.ScaleSource || driver.MaximumExpected <= 0 then
              $"cost driver '{driver.Id}' has no inspectable positive scale source"

          match driver.Disposition with
          | NonPerformance reason when String.IsNullOrWhiteSpace reason ->
              $"cost driver '{driver.Id}' has an empty non-performance disposition"
          | NonPerformance _ -> ()
          | RequiredIn workloadIds ->
              if List.isEmpty workloadIds then
                  $"cost driver '{driver.Id}' has no required workload binding"

              for workloadId in workloadIds do
                  match Map.tryFind workloadId workloadById, Map.tryFind workloadId resultById with
                  | None, _ -> $"cost driver '{driver.Id}' names missing workload '{workloadId}'"
                  | Some workload, Some result ->
                      if not (List.contains driver.Id workload.CostDriverIds) then
                          $"cost driver '{driver.Id}' is unbound from required workload '{workloadId}'"

                      let observed = result.ObservedScale |> Map.tryFind driver.Id |> Option.defaultValue 0
                      if observed < driver.MaximumExpected then
                          $"cost driver '{driver.Id}' maximum scale is underrepresented in workload '{workloadId}': expected {driver.MaximumExpected} from {driver.ScaleSource}, observed {observed}"
                  | Some _, None -> $"cost driver '{driver.Id}' has no result for required workload '{workloadId}'"
      for workload in expectedWorkloads do
          let duplicateBindings = workload.CostDriverIds |> duplicateValues
          let duplicateBindingText = String.concat ", " duplicateBindings
          if not (List.isEmpty duplicateBindings) then
              $"workload '{workload.Id}' has duplicate cost-driver bindings: {duplicateBindingText}"
          for driverId in workload.CostDriverIds do
              if not (Map.containsKey driverId driverById) then
                  $"workload '{workload.Id}' names unknown cost driver '{driverId}'" ]

let private capabilityMetricToken =
    function
    | Observed value -> $"observed:{value}"
    | Unsupported reason -> $"unsupported:{reason}"

let private criticInputDigest (results: WorkloadResult list) coverageProblems =
    let intent =
        performanceIntentDeclaration.WorkloadDefinitionDigests
        |> String.concat ","
    let provenance =
        expectedWorkloads
        |> List.map (fun workload -> $"{workload.Id}={provenanceToken workload.Provenance}")
        |> String.concat ","
    let drivers =
        performanceCostDrivers
        |> List.map (fun driver ->
            let disposition =
                match driver.Disposition with
                | RequiredIn ids ->
                    let workloadIds = String.concat "," ids
                    $"required:{workloadIds}"
                | NonPerformance reason -> $"non-performance:{reason}"
            $"{driver.Id}|{driver.Category}|{driver.ScaleSource}|{driver.MaximumExpected}|{driver.VisualElement}|{disposition}")
        |> String.concat ";"
    let measuredEvidence =
        results
        |> List.map (fun result ->
            let observedScale =
                result.ObservedScale
                |> Map.toList
                |> List.map (fun (id, count) -> $"{id}={count}")
                |> String.concat ","
            let reasons = String.concat "," result.Verdict.Reasons
            $"{result.Workload.Id}|p50={result.P50Ms:R}|p95={result.P95Ms:R}|p99={result.P99Ms:R}|updates={result.UpdateCount}|present={capabilityMetricToken result.PresentCount}|catchup={result.CatchUpFrames}|drops={capabilityMetricToken result.DroppedFrames}|declaredEvents={result.DeclaredEventCount}|observedEvents={result.ObservedEventCount}|declaredPointers={result.DeclaredPointerEventCount}|observedPointers={result.ObservedPointerEventCount}|rawInputs={result.RawInputSampleCount}|sceneNodes={result.SceneNodeCount}|allocated={result.AllocatedBytes}|scale={observedScale}|passed={result.Verdict.Passed}|reasons={reasons}")
        |> String.concat ";"
    let packages =
        declaredPackageVersions ()
        |> List.map (fun (id, version) -> $"{id}={version}")
        |> String.concat ";"
    let coverageVerdict = String.concat ";" coverageProblems
    let host =
        $"{Environment.OSVersion.Platform};{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture};{Environment.Version}"
    let capability =
        $"{performanceIntentDeclaration.RequiredCapability}|live={performanceIntentDeclaration.LiveCompositorRequired}|bounded-headless-update-and-scene-route|not-authoritative=live-compositor,swapchain,vblank,vsync"
    sha256Text
        $"performance-representativeness-v1|{intent}|{provenance}|{drivers}|{measuredEvidence}|coverage={coverageVerdict}|packages={packages}|host={host}|capability={capability}"

let private declarationProblems () =
    let duplicateIds = expectedWorkloads |> List.map _.Id |> duplicateValues
    let duplicateBindings = performanceIntentDeclaration.WorkloadDefinitionDigests |> duplicateValues
    let duplicateIdText = String.concat ", " duplicateIds
    let requiredIdText = String.concat ", " requiredNormalWorkloadIds
    let duplicateBindingText = String.concat ", " duplicateBindings
    let authoredProblems =
        expectedWorkloads
        |> List.collect (fun workload ->
            let verdict = evaluateAuthorship workload
            verdict.Reasons)

    [ if not (List.isEmpty duplicateIds) then
          $"duplicate workload ids: {duplicateIdText}"
      if performanceIntentDeclaration.WorkloadIds <> requiredNormalWorkloadIds then
          $"normal-play workload ids must be exactly: {requiredIdText}"
      if not (List.isEmpty duplicateBindings) then
          $"duplicate workload digest bindings: {duplicateBindingText}"
      if performanceIntentDeclaration.TargetFps <= 0 then
          "performance intent target FPS must be positive"
      if String.IsNullOrWhiteSpace performanceIntentDeclaration.MaximumExpectedScale then
          "performance intent maximum expected scale is required"
      if String.IsNullOrWhiteSpace performanceIntentDeclaration.RequiredCapability then
          "performance intent measurement capability is required"
      yield! authoredProblems ]

let private yamlScalar (value: string) = JsonSerializer.Serialize value

let private yamlList values =
    values |> List.map yamlScalar |> String.concat ", " |> fun values -> $"[{values}]"

let writePerformanceIntentDeclaration (path: string) =
    let intent = performanceIntentDeclaration
    let directory = Path.GetDirectoryName path

    if not (String.IsNullOrWhiteSpace directory) then
        Directory.CreateDirectory directory |> ignore

    let optional name value =
        value |> Option.map (fun actual -> $"  {name}: {yamlScalar actual}") |> Option.toList

    let maxP95 = intent.MaxP95Ms.ToString(CultureInfo.InvariantCulture)
    let maxP99 = intent.MaxP99Ms.ToString(CultureInfo.InvariantCulture)

    [ "performanceIntent:"
      $"  id: {yamlScalar intent.Id}"
      $"  disposition: {yamlScalar intent.Disposition}"
      $"  targetFps: {intent.TargetFps}"
      $"  workloadIds: {yamlList intent.WorkloadIds}"
      $"  workloadDefinitionDigests: {yamlList intent.WorkloadDefinitionDigests}"
      $"  maximumExpectedScale: {yamlScalar intent.MaximumExpectedScale}"
      $"  maxP95Ms: {maxP95}"
      $"  maxP99Ms: {maxP99}"
      $"  maxCatchUpFrames: {intent.MaxCatchUpFrames}"
      $"  structuralCostBudgets: {yamlList intent.StructuralCostBudgets}"
      $"  requiredCapability: {yamlScalar intent.RequiredCapability}"
      $"  liveCompositorRequired: {intent.LiveCompositorRequired.ToString().ToLowerInvariant()}"
      yield! optional "deferralIssue" intent.DeferralIssue
      $"  evidenceRefs: {yamlList intent.EvidenceRefs}"
      yield! optional "rationale" intent.Rationale ]
    |> fun lines -> File.WriteAllLines(path, lines)

    match declarationProblems () with
    | [] ->
        printfn "status=ok performance-intent=%s workloads=%d" path intent.WorkloadIds.Length
        0
    | problems ->
        problems |> List.iter (printfn "status=failed performance-intent reason=%s")
        1

let private writeIntentJson (json: Utf8JsonWriter) =
    let intent = performanceIntentDeclaration
    json.WriteStartObject("performanceIntent")
    json.WriteString("id", intent.Id)
    json.WriteString("disposition", intent.Disposition)
    json.WriteNumber("targetFps", intent.TargetFps)
    json.WriteStartArray("workloadIds")
    intent.WorkloadIds |> List.iter json.WriteStringValue
    json.WriteEndArray()
    json.WriteStartArray("workloadDefinitionDigests")
    intent.WorkloadDefinitionDigests |> List.iter json.WriteStringValue
    json.WriteEndArray()
    json.WriteString("maximumExpectedScale", intent.MaximumExpectedScale)
    json.WriteNumber("maxP95Ms", intent.MaxP95Ms)
    json.WriteNumber("maxP99Ms", intent.MaxP99Ms)
    json.WriteNumber("maxCatchUpFrames", intent.MaxCatchUpFrames)
    json.WriteStartArray("structuralCostBudgets")
    intent.StructuralCostBudgets |> List.iter json.WriteStringValue
    json.WriteEndArray()
    json.WriteString("requiredCapability", intent.RequiredCapability)
    json.WriteBoolean("liveCompositorRequired", intent.LiveCompositorRequired)
    intent.DeferralIssue |> Option.iter (fun value -> json.WriteString("deferralIssue", value))
    json.WriteStartArray("evidenceRefs")
    intent.EvidenceRefs |> List.iter json.WriteStringValue
    json.WriteEndArray()
    intent.Rationale |> Option.iter (fun value -> json.WriteString("rationale", value))
    json.WriteEndObject()

let writeExpectedWorkloadEvidence (path: string) =
    let results = expectedWorkloads |> List.map runWorkload
    let coverageProblems = costDriverProblems results
    let criticDigest = criticInputDigest results coverageProblems
    let directory = Path.GetDirectoryName path

    if not (String.IsNullOrWhiteSpace directory) then
        Directory.CreateDirectory directory |> ignore

    use stream = File.Create path
    use json = new Utf8JsonWriter(stream, JsonWriterOptions(Indented = true))
    json.WriteStartObject()
    json.WriteNumber("schemaVersion", 3)
    json.WriteStartObject("compatibility")
    json.WriteStartArray("acceptedLegacySchemaVersions")
    json.WriteNumberValue(2)
    json.WriteEndArray()
    json.WriteString("legacyRepresentativeness", "legacy-unreviewed")
    json.WriteEndObject()
    writeIntentJson json
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
    json.WriteStartArray("costDrivers")
    for driver in performanceCostDrivers do
        json.WriteStartObject()
        json.WriteString("id", driver.Id)
        json.WriteString("category", string driver.Category)
        json.WriteString("scaleSource", driver.ScaleSource)
        json.WriteNumber("maximumExpected", driver.MaximumExpected)
        match driver.VisualElement with
        | Some value -> json.WriteString("visualElement", value)
        | None -> json.WriteNull("visualElement")
        match driver.Disposition with
        | RequiredIn workloadIds ->
            json.WriteString("disposition", "required-in-workloads")
            json.WriteStartArray("requiredWorkloadIds")
            workloadIds |> List.iter json.WriteStringValue
            json.WriteEndArray()
        | NonPerformance reason ->
            json.WriteString("disposition", "non-performance")
            json.WriteString("reason", reason)
        json.WriteEndObject()
    json.WriteEndArray()
    json.WriteStartArray("workloads")

    for result in results do
        json.WriteStartObject()
        json.WriteString("id", result.Workload.Id)
        json.WriteString("definition", result.Workload.Definition)
        json.WriteString("class", classToken result.Workload.Classification)
        json.WriteString("definitionDigest", result.DefinitionDigest)
        match result.Workload.Provenance with
        | RunnerIssuedJourney receipt ->
            json.WriteString("stateProvenance", journeyKind)
            json.WriteStartObject("provenanceReceipt")
            json.WriteNumber("schemaVersion", JourneyReceipt.schemaVersion receipt)
            json.WriteString("runnerIdentity", JourneyReceipt.runnerIdentity receipt)
            json.WriteString("runnerVersion", JourneyReceipt.runnerVersion receipt)
            json.WriteString("compositionAuthority", JourneyReceipt.compositionAuthority receipt)
            json.WriteString("origin", string (JourneyReceipt.origin receipt))
            json.WriteString("routeId", JourneyReceipt.routeId receipt)
            json.WriteString("scenarioId", JourneyReceipt.scenarioId receipt)
            json.WriteString("testId", JourneyReceipt.testId receipt)
            json.WriteString("inputKind", string (JourneyReceipt.inputKind receipt))
            json.WriteString("inputIdentity", JourneyReceipt.inputIdentity receipt)
            json.WriteString("inputDigest", JourneyReceipt.inputDigest receipt)
            json.WriteString("scriptDigest", JourneyReceipt.scriptDigest receipt)
            json.WriteString("traceDigest", JourneyReceipt.traceDigest receipt)
            json.WriteString("initialFingerprintDigest", JourneyReceipt.initialFingerprintDigest receipt)
            json.WriteString("terminalFingerprintDigest", JourneyReceipt.terminalFingerprintDigest receipt)
            json.WriteString("terminalPredicateIdentity", JourneyReceipt.terminalPredicateIdentity receipt)
            json.WriteBoolean("terminalPredicateReached", JourneyReceipt.terminalPredicateReached receipt)
            json.WriteString("result", string (JourneyReceipt.result receipt))
            json.WriteNumber("steps", JourneyReceipt.steps receipt)
            json.WriteNumber("maxSteps", JourneyReceipt.maxSteps receipt)
            json.WriteString("receiptDigest", $"sha256:{runnerReceiptToken receipt}")
            json.WriteEndObject()
        | SyntheticConstructed reason ->
            json.WriteString("stateProvenance", "synthetic-constructed")
            json.WriteString("syntheticReason", reason)
            json.WriteNull("provenanceReceipt")

        match result.Workload.Composition with
        | CompleteComposition -> json.WriteString("compositionClaim", completeCompositionKind)
        | ComponentOnlySupplemental reason ->
            json.WriteString("compositionClaim", "component-only-supplemental")
            json.WriteString("componentOnlyReason", reason)

        json.WriteStartArray("costDriverIds")
        result.Workload.CostDriverIds |> List.iter json.WriteStringValue
        json.WriteEndArray()

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
        let writeCapabilityMetric (name: string) (metric: CapabilityMetric) =
            json.WriteStartObject(name)
            match metric with
            | Observed value ->
                json.WriteString("status", "observed")
                json.WriteNumber("value", value)
            | Unsupported reason ->
                json.WriteString("status", "unsupported")
                json.WriteString("reason", reason)
            json.WriteEndObject()

        writeCapabilityMetric "presentCount" result.PresentCount
        json.WriteNumber("catchUpFrames", result.CatchUpFrames)
        writeCapabilityMetric "droppedFrames" result.DroppedFrames
        json.WriteNumber("declaredEventCount", result.DeclaredEventCount)
        json.WriteNumber("observedEventCount", result.ObservedEventCount)
        json.WriteNumber("declaredPointerEventCount", result.DeclaredPointerEventCount)
        json.WriteNumber("observedPointerEventCount", result.ObservedPointerEventCount)
        json.WriteNumber("rawInputSampleCount", result.RawInputSampleCount)
        json.WriteNumber("allocatedBytes", result.AllocatedBytes)
        json.WriteStartObject("observedScale")
        result.ObservedScale |> Map.iter (fun name value -> json.WriteNumber(name, value))
        json.WriteEndObject()
        json.WriteStartObject("sceneNodesByLayer")
        json.WriteNumber("product-scene", result.SceneNodeCount)
        json.WriteEndObject()
        json.WriteBoolean("passed", result.Verdict.Passed)
        json.WriteStartArray("reasons")
        result.Verdict.Reasons |> List.iter json.WriteStringValue
        json.WriteEndArray()
        json.WriteEndObject()

    json.WriteEndArray()
    json.WriteStartObject("critic")
    json.WriteString("rubricVersion", "performance-representativeness-v1")
    json.WriteString("inputDigest", criticDigest)
    json.WriteString("status", "external-review-required")
    json.WriteString("reviewBoundary", "attributable review system at the exact landing commit")
    json.WriteString("preferredMode", "fresh-context-subagent")
    json.WriteString("fallbackMode", "separated-pass-with-independence-disclosure")
    json.WriteString(
        "prohibitedProof",
        "in-repo JSON, author-entered identity, or a same-context mode string cannot establish independence"
    )
    json.WriteStartArray("acceptedOutcomes")
    [ "supported"
      "underrepresentative"
      "synthetic-only"
      "unmeasured"
      "misclassified"
      "ambiguous" ]
    |> List.iter json.WriteStringValue
    json.WriteEndArray()
    json.WriteBoolean("representativeReady", false)
    json.WriteEndObject()
    json.WriteEndObject()
    json.Flush()

    let declarationFailures = declarationProblems ()
    let failures = results |> List.filter (_.Verdict.Passed >> not)

    if
        List.isEmpty failures
        && List.isEmpty declarationFailures
        && List.isEmpty coverageProblems
    then
        printfn
            "status=ok performance-evidence workloads=%d capability=bounded-headless artifact=%s"
            results.Length
            path

        0
    else
        declarationFailures
        |> List.iter (printfn "status=failed performance-intent reason=%s")

        coverageProblems
        |> List.iter (printfn "status=failed performance-coverage reason=%s")

        failures
        |> List.iter (fun result ->
            printfn
                "status=failed workload=%s reasons=%s"
                result.Workload.Id
                (String.concat " | " result.Verdict.Reasons))

        1

/// Emits the exact evidence-plus-input-digest package a fresh-context critic must cold-read. Approval
/// lives in an attributable review system at the exact landing commit, never in this authored tree,
/// so a critic cannot edit samples, issue provenance, waive a red budget, or upgrade capability.
let writePerformanceCriticRequest (path: string) =
    let directory = Path.GetDirectoryName path
    let evidencePath =
        if String.IsNullOrWhiteSpace directory then
            "performance-evidence.json"
        else
            Path.Combine(directory, "performance-evidence.json")

    let exitCode = writeExpectedWorkloadEvidence evidencePath
    let evidenceBytes = File.ReadAllBytes evidencePath
    let evidenceDigest = SHA256.HashData evidenceBytes |> Convert.ToHexString |> _.ToLowerInvariant()
    use evidence = JsonDocument.Parse evidenceBytes
    let inputDigest = evidence.RootElement.GetProperty("critic").GetProperty("inputDigest").GetString()

    if not (String.IsNullOrWhiteSpace directory) then
        Directory.CreateDirectory directory |> ignore

    use stream = File.Create path
    use json = new Utf8JsonWriter(stream, JsonWriterOptions(Indented = true))
    json.WriteStartObject()
    json.WriteNumber("schemaVersion", 1)
    json.WriteString("rubricVersion", "performance-representativeness-v1")
    json.WriteString("inputDigest", inputDigest)
    json.WriteString("evidenceArtifact", evidencePath)
    json.WriteString("evidenceArtifactDigest", $"sha256:{evidenceDigest}")
    json.WriteNumber("machineExitCode", exitCode)
    json.WriteString("requiredReviewBoundary", "attributable external review at the exact landing commit")
    json.WriteBoolean("representativeReady", false)
    json.WriteEndObject()
    json.Flush()
    exitCode
//#else
let writeExpectedWorkloadEvidence _ = 0
let writePerformanceIntentDeclaration _ = 0
let writePerformanceCriticRequest _ = 0
//#endif
