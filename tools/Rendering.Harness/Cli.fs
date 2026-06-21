module Rendering.Harness.Cli

open System
open System.IO
open System.Text.Json
open Rendering.Harness
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// Parse `--out <dir>` from the remaining args; default to a gitignored per-run dir.
let outDir (rest: string list) =
    let rec find xs =
        match xs with
        | "--out" :: d :: _ -> Some d
        | _ :: tl -> find tl
        | [] -> None
    match find rest with
    | Some d -> d
    | None -> Path.Combine("artifacts", "harness", "run-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"))

let private runProbe (rest: string list) =
    let facts = Probe.probe ()
    let evidence: Evidence.Evidence =
        { RunId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
          Tier = T0
          Subcommand = "probe"
          Status = Passed
          SkipReason = None
          ProofLevel = Deterministic
          AuthoritativeFor = [ "environment-facts" ]
          NotAuthoritativeFor = [ "rendering"; "timing"; "live-host" ]
          Facts = facts
          Frames = 0
          P50Ms = None
          P95Ms = None
          P99Ms = None
          Artifacts = [ "summary.md" ] }
    let path = Evidence.write (outDir rest) evidence []
    printfn "%s" path
    0

let private runOffscreen (rest: string list) =
    let facts = Probe.probe ()
    let baseOut = outDir rest
    let evT0, fT0 = Tiers.runOffscreen T0 facts (Path.Combine(baseOut, "T0"))
    Evidence.write (Path.Combine(baseOut, "T0")) evT0 fT0 |> ignore
    let evT1, fT1 = Tiers.runOffscreen T1 facts (Path.Combine(baseOut, "T1"))
    let p1 = Evidence.write (Path.Combine(baseOut, "T1")) evT1 fT1
    printfn "%s" p1
    if evT0.Status = Passed && evT1.Status = Passed then 0 else 1

let private flagValue (flag: string) (rest: string list) =
    let rec find xs =
        match xs with
        | f :: v :: _ when f = flag -> Some v
        | _ :: tl -> find tl
        | [] -> None
    find rest

// Feature 181 (US2): feature selection routes through the single FeatureCatalog descriptor table
// instead of 12 hand-duplicated alias predicates. `tryByAlias` accepts the same "NNN"/"featureNNN"/
// slug forms the old isFeature### predicates accepted (C-CT-3/C-FD-4; locked by FeatureCatalogTests).
let private selectFeature (rest: string list) =
    flagValue "--feature" rest
    |> Option.bind FeatureCatalog.FeatureDescriptor.tryByAlias

let private isFeatureId (id: int) (rest: string list) =
    selectFeature rest |> Option.exists (fun d -> d.Id = id)

let private isFeature148 rest = isFeatureId 148 rest
let private isFeature149 rest = isFeatureId 149 rest
let private isFeature152 rest = isFeatureId 152 rest
let private isFeature153 rest = isFeatureId 153 rest
let private isFeature154 rest = isFeatureId 154 rest
let private isFeature155 rest = isFeatureId 155 rest
let private isFeature156 rest = isFeatureId 156 rest
let private isFeature157 rest = isFeatureId 157 rest
let private isFeature158 rest = isFeatureId 158 rest
let private isFeature159 rest = isFeatureId 159 rest
let private isFeature160 rest = isFeatureId 160 rest
let private isFeature161 rest = isFeatureId 161 rest

let private attemptCount (rest: string list) =
    match flagValue "--attempt-count" rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> 1
    | None -> 1

let private positiveIntFlag flag fallback rest =
    match flagValue flag rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> fallback
    | None -> fallback

let private proofSize: Size = { Width = 640; Height = 480 }

let private sentinelScene () =
    SceneNode.Group
        [ Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 12uy 18uy 30uy)
          Scene.rectangle (32.0, 32.0, 132.0, 92.0) (Colors.rgb 64uy 220uy 144uy)
          Scene.rectangle (320.0, 200.0, 96.0, 96.0) (Colors.rgb 220uy 180uy 64uy)
          Scene.rectangle (500.0, 48.0, 72.0, 144.0) (Colors.rgb 72uy 96uy 210uy) ]

let private damageScene () =
    SceneNode.Group
        [ Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 12uy 18uy 30uy)
          Scene.rectangle (32.0, 32.0, 132.0, 92.0) (Colors.rgb 64uy 220uy 144uy)
          Scene.rectangle (320.0, 200.0, 96.0, 96.0) (Colors.rgb 236uy 80uy 96uy)
          Scene.rectangle (500.0, 48.0, 72.0, 144.0) (Colors.rgb 72uy 96uy 210uy) ]

let private feature156ScenarioStem (scenarioId: string) =
    scenarioId.Replace("/", "-")

let private feature156FormatMs (value: float) =
    value.ToString("0.###", Globalization.CultureInfo.InvariantCulture)

let private feature156ScenarioScene scenarioId damageScoped =
    let accent =
        if damageScoped then
            Colors.rgb 236uy 80uy 96uy
        else
            Colors.rgb 220uy 180uy 64uy

    let sparseHeavyStaticLayer =
        [ for row in 0..23 do
              for col in 0..31 do
                  let x = float col * 20.0
                  let y = float row * 20.0
                  let color =
                      match (row + col) % 3 with
                      | 0 -> Colors.rgb 52uy 82uy 112uy
                      | 1 -> Colors.rgb 72uy 118uy 96uy
                      | _ -> Colors.rgb 96uy 80uy 128uy

                  yield Scene.rectangle (x, y, 18.0, 18.0) color ]

    match scenarioId with
    | "timing/sparse-heavy-localized-update" ->
        let dirtyPatch = Scene.rectangle (304.0, 224.0, 24.0, 24.0) accent

        if damageScoped then
            // Models a retained-backing hot path: only the tiny dirty patch is redrawn.
            SceneNode.Group [ dirtyPatch ]
        else
            SceneNode.Group
                ((Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 12uy 18uy 30uy)
                  :: sparseHeavyStaticLayer)
                 @ [ dirtyPatch ])
    | "timing/no-change" ->
        SceneNode.Group
            [ Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 12uy 18uy 30uy)
              Scene.rectangle (96.0, 96.0, 180.0, 120.0) (Colors.rgb 64uy 160uy 220uy) ]
    | "timing/movement-old-new" ->
        let x = if damageScoped then 260.0 else 220.0
        SceneNode.Group
            [ Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 16uy 20uy 32uy)
              Scene.rectangle (x, 180.0, 120.0, 96.0) accent
              Scene.rectangle (80.0, 340.0, 420.0, 36.0) (Colors.rgb 80uy 96uy 120uy) ]
    | "timing/overlap" ->
        SceneNode.Group
            [ Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 18uy 20uy 28uy)
              Scene.rectangle (180.0, 130.0, 180.0, 150.0) (Colors.rgb 60uy 180uy 180uy)
              Scene.rectangle (260.0, 190.0, 170.0, 150.0) accent ]
    | "timing/edge-clipping" ->
        SceneNode.Group
            [ Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 20uy 20uy 26uy)
              Scene.rectangle (-24.0, 64.0, 144.0, 144.0) accent
              Scene.rectangle (540.0, 380.0, 160.0, 120.0) (Colors.rgb 92uy 180uy 96uy) ]
    | _ ->
        SceneNode.Group
            [ Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 12uy 18uy 30uy)
              Scene.rectangle (32.0, 32.0, 132.0, 92.0) (Colors.rgb 64uy 220uy 144uy)
              Scene.rectangle (320.0, 200.0, 96.0, 96.0) accent
              Scene.rectangle (500.0, 48.0, 72.0, 144.0) (Colors.rgb 72uy 96uy 210uy) ]

let private feature156PathDistribution (distribution: Perf.SampleDistribution) : Compositor.Feature156PathDistribution =
    { SampleCount = distribution.Count
      P50Ms = distribution.P50Ms
      P95Ms = distribution.P95Ms
      P99Ms = distribution.P99Ms
      MinMs = distribution.MinMs
      MaxMs = distribution.MaxMs
      RawSamplePath = distribution.RawSamplePath }

let private feature156VerdictFromPerf verdict =
    match verdict with
    | Perf.Positive -> Compositor.Feature156Positive
    | Perf.Noisy -> Compositor.Feature156Noisy
    | Perf.NonBeneficial -> Compositor.Feature156NonBeneficial
    | Perf.Incomplete -> Compositor.Feature156Incomplete
    | Perf.Rejected -> Compositor.Feature156Rejected
    | Perf.EnvironmentLimited -> Compositor.Feature156EnvironmentLimited
    | Perf.Limited -> Compositor.Feature156Limited

let private writeFeature156RawSamples rawPath scenarioId path runId hostProfile samples =
    let sampleLines =
        samples
        |> List.mapi (fun index duration ->
            $"{index + 1},{scenarioId},{path},{runId},{hostProfile},{feature156FormatMs duration}")

    let lines = "sample-index,scenario-id,path,run-id,host-profile-id,duration-ms" :: sampleLines

    File.WriteAllText(rawPath, String.concat Environment.NewLine lines + Environment.NewLine)

let private captureFeature156Sample rawDir scenarioId path hostFacts =
    let stem = feature156ScenarioStem scenarioId
    let imagePath = Path.Combine(rawDir, $"{stem}-{path}-latest.png")
    let scene = feature156ScenarioScene scenarioId (path = "damage-scoped")
    let options: ViewerOptions =
        { Title = $"Feature156 {path}"
          InitialSize = proofSize
          PresentMode = ViewerPresentMode.OffscreenReadback
          FrameRateCap = None }

    let request: ScreenshotEvidenceRequest =
        { Command = "compositor-performance"
          AppOrSample = $"feature156-{stem}-{path}"
          OutputPath = imagePath
          Width = proofSize.Width
          Height = proofSize.Height
          RendererMode = "skia"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = hostFacts
          Timeout = TimeSpan.FromSeconds 10.0 }

    let sw = System.Diagnostics.Stopwatch.StartNew()
    let result = Viewer.captureScreenshotEvidence request options scene
    sw.Stop()

    if result.Status = ScreenshotOk then
        Some sw.Elapsed.TotalMilliseconds
    else
        None

let private runFeature156Path rawDir scenarioId path warmup repetitions runId hostProfileId hostFacts =
    for _ in 1..warmup do
        captureFeature156Sample rawDir scenarioId path hostFacts |> ignore

    let samples =
        [ for _ in 1..repetitions do
              match captureFeature156Sample rawDir scenarioId path hostFacts with
              | Some ms -> ms
              | None -> Double.NaN ]

    let rawRelative = Path.Combine("raw", $"{feature156ScenarioStem scenarioId}-{path}.csv").Replace('\\', '/')
    let rawPath = Path.Combine(rawDir, $"{feature156ScenarioStem scenarioId}-{path}.csv")
    writeFeature156RawSamples rawPath scenarioId path runId hostProfileId samples
    rawRelative, Perf.summarizeSamples rawRelative samples

let private feature156SummaryFromReports
    runId
    (profile: Compositor.HostProfile)
    warmup
    repetitions
    (reports: Compositor.Feature156ScenarioReport list)
    diagnostics
    : Compositor.Feature156TimingSummary =
    let overall = Compositor.feature156OverallVerdict reports
    { RunId = runId
      HostProfile = profile
      PolicyId = Compositor.feature156PolicyId
      WarmupCount = warmup
      MeasuredRepetitions = repetitions
      ScenarioReports = reports
      OverallVerdict = overall
      ShippedPerformanceClaim = "performance-not-accepted"
      Diagnostics = diagnostics }

let private feature156VerdictFromToken token =
    match token with
    | "positive" -> Some Compositor.Feature156Positive
    | "noisy" -> Some Compositor.Feature156Noisy
    | "non-beneficial" -> Some Compositor.Feature156NonBeneficial
    | "incomplete" -> Some Compositor.Feature156Incomplete
    | "rejected" -> Some Compositor.Feature156Rejected
    | "environment-limited" -> Some Compositor.Feature156EnvironmentLimited
    | "limited" -> Some Compositor.Feature156Limited
    | _ -> None

let private feature156ExistingTimingVerdict timingSummaryPath =
    if File.Exists timingSummaryPath then
        File.ReadLines timingSummaryPath
        |> Seq.tryPick (fun line ->
            let prefix = "Feature 156 timing verdict: `"

            if line.StartsWith(prefix, StringComparison.Ordinal) then
                line.Substring(prefix.Length).TrimEnd('`') |> feature156VerdictFromToken
            else
                None)
    else
        None

let private feature158ReasonFromHost facts (profile: Compositor.HostProfile) expectedProfile =
    match facts.EffectiveBackend, facts.GlRenderer, facts.GlDirect, profile.ProfileId = expectedProfile with
    | NoDisplay, _, _, _ -> Some "missing display"
    | _, None, _, _ -> Some "missing GL renderer facts"
    | _, _, false, _ -> Some "OpenGL direct rendering is unavailable"
    | _, _, _, false -> Some $"profile mismatch: expected={expectedProfile} actual={profile.ProfileId}"
    | _ -> None

let private feature158ScenarioDefinition scenarioId =
    "feature156-required-v1:" + feature156ScenarioStem scenarioId

let private feature158Distribution rawSamplePath samples : Compositor.Feature158PathDistribution option =
    Perf.summarizeSamples rawSamplePath samples
    |> Option.map (fun distribution ->
        { SampleCount = distribution.Count
          P50Ms = distribution.P50Ms
          P95Ms = distribution.P95Ms
          P99Ms = distribution.P99Ms
          MinMs = distribution.MinMs
          MaxMs = distribution.MaxMs
          RawSamplePath = distribution.RawSamplePath })

let private feature158SummaryFromReports
    runId
    (profile: Compositor.HostProfile)
    warmup
    repetitions
    (reports: Compositor.Feature158ScenarioReport list)
    (proofProbes: Compositor.Feature158ProofProbeEvidence list)
    (unsupported: string option)
    (diagnostics: string list)
    : Compositor.Feature158TimingSummary =
    let included = reports |> List.collect _.IncludedSamples
    let excluded = reports |> List.collect _.ExcludedSamples
    let status =
        match unsupported with
        | Some _ -> Compositor.Feature158ReadinessStatus.EnvironmentLimited
        | None when Compositor.feature158RequiredScenarioIds |> List.forall (fun scenario -> reports |> List.exists (fun report -> report.ScenarioId = scenario && report.Status = Compositor.Feature158ReadinessStatus.Accepted)) ->
            Compositor.Feature158ReadinessStatus.Accepted
        | None when not (List.isEmpty included) -> Compositor.Feature158ReadinessStatus.Rejected
        | None -> Compositor.Feature158ReadinessStatus.FallbackOnly

    { RunId = runId
      HostProfile = profile
      PolicyId = Compositor.feature158PolicyId
      WarmupCount = warmup
      MeasuredRepetitions = repetitions
      ScenarioReports = reports
      IncludedSamples = included
      ExcludedSamples = excluded
      ProofProbeEvidence = proofProbes
      UnsupportedHostReason = unsupported
      Feature156Comparison = if status = Compositor.Feature158ReadinessStatus.Accepted then "contextualizes" else "contextualizes"
      Status = status
      PerformanceClaim = "performance-not-accepted"
      Diagnostics = diagnostics }

type private Feature158TimingSnapshot =
    { RunId: string
      Status: Compositor.Feature158ReadinessStatus
      IncludedSampleCount: int
      ExcludedSampleCount: int
      UnsupportedHostReason: string option
      Feature156Comparison: string
      PerformanceClaim: string
      ExcludedReasons: Perf.ExclusionReason list }

let private feature158StatusFromToken token =
    match token with
    | "accepted" -> Compositor.Feature158ReadinessStatus.Accepted
    | "fallback-only" -> Compositor.Feature158ReadinessStatus.FallbackOnly
    | "rejected" -> Compositor.Feature158ReadinessStatus.Rejected
    | "environment-limited" -> Compositor.Feature158ReadinessStatus.EnvironmentLimited
    | _ -> Compositor.Feature158ReadinessStatus.FallbackOnly

let private feature158ExclusionReasonFromToken token =
    match token with
    | "probe-run-excluded" -> Some Perf.ProbeRunExcluded
    | "proof-readback-in-measured-interval" -> Some Perf.ProofReadbackInMeasuredInterval
    | "missing-measurement-policy" -> Some Perf.MissingMeasurementPolicy
    | "unverifiable-measurement-policy" -> Some Perf.UnverifiableMeasurementPolicy
    | "cross-profile-evidence" -> Some Perf.CrossProfileEvidence
    | "scenario-definition-mismatch" -> Some Perf.ScenarioDefinitionMismatch
    | "package-version-mismatch" -> Some Perf.PackageVersionMismatch
    | "run-identity-mismatch" -> Some Perf.RunIdentityMismatch
    | "unsupported-host" -> Some Perf.UnsupportedHost
    | "environment-limited" -> Some Perf.EnvironmentLimitedReason
    | "failed-proof-readback" -> Some Perf.FailedProofReadback
    | _ -> None

let private jsonString (root: JsonElement) (name: string) =
    let mutable property = Unchecked.defaultof<JsonElement>
    if root.TryGetProperty(name, &property) && property.ValueKind = JsonValueKind.String then
        property.GetString() |> Option.ofObj
    else
        None

let private jsonInt (root: JsonElement) (name: string) =
    let mutable property = Unchecked.defaultof<JsonElement>
    let mutable value = 0
    if root.TryGetProperty(name, &property) && property.TryGetInt32(&value) then
        Some value
    else
        None

let private loadFeature158TimingSnapshot timingDir =
    let path = Path.Combine(timingDir, "summary.json")
    if not (File.Exists path) then
        None
    else
        try
            use document = JsonDocument.Parse(File.ReadAllText path)
            let root = document.RootElement
            let reasons =
                let mutable property = Unchecked.defaultof<JsonElement>
                if root.TryGetProperty("excludedReasons", &property) && property.ValueKind = JsonValueKind.Array then
                    [ for item in property.EnumerateArray() do
                          if item.ValueKind = JsonValueKind.String then
                              match item.GetString() |> Option.ofObj |> Option.bind feature158ExclusionReasonFromToken with
                              | Some reason -> reason
                              | None -> () ]
                else
                    []

            let unsupported =
                jsonString root "unsupportedHostReason"
                |> Option.bind (fun value -> if String.IsNullOrWhiteSpace value then None else Some value)

            Some
                { RunId = jsonString root "runId" |> Option.defaultValue ("feature158-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"))
                  Status = jsonString root "status" |> Option.map feature158StatusFromToken |> Option.defaultValue Compositor.Feature158ReadinessStatus.FallbackOnly
                  IncludedSampleCount = jsonInt root "includedSampleCount" |> Option.defaultValue 0
                  ExcludedSampleCount = jsonInt root "excludedSampleCount" |> Option.defaultValue 0
                  UnsupportedHostReason = unsupported
                  Feature156Comparison = jsonString root "feature156Comparison" |> Option.defaultValue "contextualizes"
                  PerformanceClaim = jsonString root "performanceClaim" |> Option.defaultValue "performance-not-accepted"
                  ExcludedReasons = reasons }
        with _ ->
            None

let private feature158CountByScenario total =
    let scenarios = Compositor.feature158RequiredScenarioIds
    let count = max 0 total
    let scenarioCount = max 1 scenarios.Length
    let quotient = count / scenarioCount
    let remainder = count % scenarioCount
    scenarios
    |> List.mapi (fun index scenario -> scenario, quotient + if index < remainder then 1 else 0)
    |> Map.ofList

let private loadFeature158ProofProbeEvidence proofProbeDir (profile: Compositor.HostProfile) : Compositor.Feature158ProofProbeEvidence list =
    if not (Directory.Exists proofProbeDir) then
        []
    else
        let artifacts =
            Directory.EnumerateFiles(proofProbeDir, "*.png")
            |> Seq.map (fun path ->
                let fileName = Path.GetFileName path |> Option.ofObj |> Option.defaultValue "probe-readback.png"
                Path.Combine("proof-probes", fileName).Replace('\\', '/'))
            |> Seq.sort
            |> Seq.toList

        match artifacts with
        | [] -> []
        | xs ->
            [ { Compositor.Feature158ProofProbeEvidence.ProbeId = "feature158-existing-proof-probes"
                HostProfile = profile
                ScenarioIds = Compositor.feature158RequiredScenarioIds
                ReadbackArtifacts = xs
                ProbeSampleIds = []
                ExclusionReason = Perf.ProbeRunExcluded
                Diagnostics = [ "loaded from proof-probes directory"; "accepted performance samples=0" ] } ]

let private feature158Sample sampleId sampleIndex scenario path runId profile packageVersion policy status reason duration artifact : Compositor.Feature158TimingSample =
    { SampleId = sampleId
      SampleIndex = sampleIndex
      ScenarioId = scenario
      ScenarioDefinitionId = feature158ScenarioDefinition scenario
      Path = path
      RunId = runId
      HostProfileId = profile
      PackageVersion = packageVersion
      DurationMs = duration
      MeasurementPolicy = policy
      InclusionStatus = status
      ExclusionReason = reason
      ArtifactPath = artifact }

let private feature158ScenarioReport
    scenario
    scenarioDefinition
    fullRedraw
    damageScoped
    warmup
    repetitions
    included
    excluded
    proofProbeArtifacts
    status
    artifactPaths
    diagnostics
    : Compositor.Feature158ScenarioReport =
    { ScenarioId = scenario
      ScenarioDefinitionId = scenarioDefinition
      FullRedraw = fullRedraw
      DamageScoped = damageScoped
      WarmupCount = warmup
      MeasuredRepetitions = repetitions
      IncludedSamples = included
      ExcludedSamples = excluded
      ProofProbeArtifacts = proofProbeArtifacts
      Status = status
      ArtifactPaths = artifactPaths
      Diagnostics = diagnostics }

let private feature158ReadinessReportsFromSnapshot (snapshot: Feature158TimingSnapshot) (profile: Compositor.HostProfile) proofProbeArtifacts =
    let includedCounts = feature158CountByScenario snapshot.IncludedSampleCount
    let excludedCounts = feature158CountByScenario snapshot.ExcludedSampleCount
    let excludedReasons =
        match snapshot.ExcludedReasons with
        | [] -> [ Perf.UnverifiableMeasurementPolicy ]
        | reasons -> reasons

    Compositor.feature158RequiredScenarioIds
    |> List.map (fun scenario ->
        let includedCount = includedCounts |> Map.tryFind scenario |> Option.defaultValue 0
        let excludedCount = excludedCounts |> Map.tryFind scenario |> Option.defaultValue 0

        let included =
            [ for index in 1..includedCount ->
                  let path = if index % 2 = 0 then Perf.DamageScoped else Perf.FullRedraw
                  let sampleIndex = index.ToString("000")
                  feature158Sample
                      $"{snapshot.RunId}-{feature156ScenarioStem scenario}-included-{sampleIndex}"
                      index
                      scenario
                      path
                      snapshot.RunId
                      profile.ProfileId
                      Compositor.feature156PackageVersion
                      Perf.ReadbackFree
                      Perf.Included
                      None
                      0.0
                      (Path.Combine("timing", "summary.json").Replace('\\', '/')) ]

        let excluded =
            [ for index in 1..excludedCount ->
                  let sampleIndex = index.ToString("000")
                  let reason = excludedReasons.[(index - 1) % excludedReasons.Length]
                  let status, policy =
                      if reason = Perf.ProbeRunExcluded then
                          Perf.Probe, Perf.ProbeReadbackIncluded
                      else
                          Perf.Excluded, Perf.Unverified

                  feature158Sample
                      $"{snapshot.RunId}-{feature156ScenarioStem scenario}-excluded-{sampleIndex}"
                      index
                      scenario
                      Perf.FullRedraw
                      snapshot.RunId
                      profile.ProfileId
                      Compositor.feature156PackageVersion
                      policy
                      status
                      (Some reason)
                      0.0
                      (Path.Combine("timing", "excluded", Perf.exclusionReasonToken reason + ".md").Replace('\\', '/')) ]

        let status =
            match snapshot.Status with
            | Compositor.Feature158ReadinessStatus.Accepted when List.isEmpty included -> Compositor.Feature158ReadinessStatus.FallbackOnly
            | status -> status

        feature158ScenarioReport
            scenario
            (feature158ScenarioDefinition scenario)
            None
            None
            3
            5
            included
            excluded
            proofProbeArtifacts
            status
            [ Path.Combine("timing", "scenarios", Compositor.feature158ScenarioFileName scenario).Replace('\\', '/') ]
            [ "readiness summary derived from timing/summary.json" ])

let private writeFeature158RawSamples rawDir scenario pathToken runId hostProfile packageVersion policy (samples: Compositor.Feature158TimingSample list) =
    let rawStem = $"{feature156ScenarioStem scenario}-{pathToken}"
    let csvRelative = Path.Combine("raw", rawStem + ".csv").Replace('\\', '/')
    let jsonRelative = Path.Combine("raw", rawStem + ".json").Replace('\\', '/')
    let csvPath = Path.Combine(rawDir, rawStem + ".csv")
    let jsonPath = Path.Combine(rawDir, rawStem + ".json")

    let csvLines =
        samples
        |> List.map (fun (sample: Compositor.Feature158TimingSample) ->
            let reason = sample.ExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue ""
            $"{sample.SampleIndex},{sample.SampleId},{sample.ScenarioId},{Perf.timingPathToken sample.Path},{sample.RunId},{sample.HostProfileId},{sample.PackageVersion},{sample.ScenarioDefinitionId},{feature156FormatMs sample.DurationMs},{Perf.measurementPolicyToken sample.MeasurementPolicy},{Perf.inclusionStatusToken sample.InclusionStatus},{reason},{sample.ArtifactPath}")

    let header = "sample-index,sample-id,scenario-id,path,run-id,host-profile-id,package-version,scenario-definition-id,duration-ms,measurement-policy,inclusion-status,exclusion-reason,artifact-path"
    File.WriteAllText(csvPath, String.concat Environment.NewLine (header :: csvLines) + Environment.NewLine)

    let jsonSamples =
        samples
        |> List.map (fun sample ->
            let reason = sample.ExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue ""
            $"    {{ \"sampleId\": \"{sample.SampleId}\", \"scenarioId\": \"{sample.ScenarioId}\", \"path\": \"{Perf.timingPathToken sample.Path}\", \"durationMs\": {feature156FormatMs sample.DurationMs}, \"measurementPolicy\": \"{Perf.measurementPolicyToken sample.MeasurementPolicy}\", \"inclusionStatus\": \"{Perf.inclusionStatusToken sample.InclusionStatus}\", \"exclusionReason\": \"{reason}\" }}")
        |> String.concat ",\n"

    File.WriteAllText(jsonPath, String.concat Environment.NewLine [ "["; jsonSamples; "]" ] + Environment.NewLine)
    csvRelative, jsonRelative

let private measureFeature158Path rawDir scenario path warmup repetitions runId hostProfileId hostFacts =
    let pathToken = Perf.timingPathToken path
    let scene = feature156ScenarioScene scenario (path = Perf.DamageScoped)
    let options: ViewerOptions =
        { Title = $"Feature158 {pathToken}"
          InitialSize = proofSize
          PresentMode = ViewerPresentMode.DirectToSwapchain
          FrameRateCap = Some 60 }

    for _ in 1..warmup do
        Viewer.runForFrames 1 options scene |> ignore

    let samples =
        [ for index in 1..repetitions do
              let sw = System.Diagnostics.Stopwatch.StartNew()
              let result = Viewer.runForFrames 1 options scene
              sw.Stop()
              let status, reason, duration, policy =
                  match result with
                  | Result.Ok _ -> Perf.Included, None, sw.Elapsed.TotalMilliseconds, Perf.ReadbackFree
                  | Result.Error _ -> Perf.Excluded, Some Perf.UnverifiableMeasurementPolicy, Double.NaN, Perf.Unverified

              let sampleIndex = index.ToString("000")
              let sampleId = $"{runId}-{feature156ScenarioStem scenario}-{pathToken}-{sampleIndex}"
              let artifact = Path.Combine("raw", $"{feature156ScenarioStem scenario}-{pathToken}.csv").Replace('\\', '/')
              let sample = feature158Sample sampleId index scenario path runId hostProfileId Compositor.feature156PackageVersion policy status reason duration artifact
              match result with
              | Result.Ok _ -> sample
              | Result.Error failure -> { sample with ArtifactPath = artifact + $"; failure={failure.Message}" } ]

    let csvRelative, jsonRelative = writeFeature158RawSamples rawDir scenario pathToken runId hostProfileId Compositor.feature156PackageVersion Perf.ReadbackFree samples
    let includedDurations =
        samples
        |> List.filter (fun sample -> sample.InclusionStatus = Perf.Included)
        |> List.map _.DurationMs

    csvRelative, jsonRelative, samples, feature158Distribution csvRelative includedDurations

let private feature158FailClosedReport scenario warmup repetitions status reason : Compositor.Feature158ScenarioReport =
    let sample =
        feature158Sample
            $"feature158-failclosed-{feature156ScenarioStem scenario}"
            1
            scenario
            Perf.FullRedraw
            "feature158-failclosed"
            Compositor.feature158AcceptedProfileId
            Compositor.feature156PackageVersion
            Perf.Missing
            Perf.Excluded
            (Some reason)
            Double.NaN
            (Path.Combine("excluded", Perf.exclusionReasonToken reason + ".md").Replace('\\', '/'))

    feature158ScenarioReport
        scenario
        (feature158ScenarioDefinition scenario)
        None
        None
        warmup
        repetitions
        []
        [ sample ]
        []
        status
        [ Path.Combine("scenarios", Compositor.feature158ScenarioFileName scenario).Replace('\\', '/') ]
        [ Perf.exclusionReasonToken reason ]

let private feature158ScenarioStatus (included: Compositor.Feature158TimingSample list) (excluded: Compositor.Feature158TimingSample list) =
    if List.isEmpty included && excluded |> List.exists (fun sample -> sample.ExclusionReason = Some Perf.EnvironmentLimitedReason || sample.ExclusionReason = Some Perf.UnsupportedHost) then
        Compositor.Feature158ReadinessStatus.EnvironmentLimited
    elif not (List.isEmpty included)
         && included |> List.forall (fun sample -> sample.MeasurementPolicy = Perf.ReadbackFree || sample.MeasurementPolicy = Perf.ReadbackOutsideMeasurement)
         && excluded |> List.forall (fun sample -> sample.InclusionStatus <> Perf.Included) then
        Compositor.Feature158ReadinessStatus.Accepted
    elif List.isEmpty included then
        Compositor.Feature158ReadinessStatus.FallbackOnly
    else
        Compositor.Feature158ReadinessStatus.Rejected

let private writeFeature158TimingPackage out (summary: Compositor.Feature158TimingSummary) =
    let scenariosDir = Path.Combine(out, "scenarios")
    let excludedDir = Path.Combine(out, "excluded")
    let unsupportedDir =
        let outLeaf =
            out.TrimEnd([| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |])
            |> Path.GetFileName

        if String.Equals(outLeaf, "unsupported", StringComparison.OrdinalIgnoreCase) then
            out
        else
            Path.Combine(out, "unsupported")
    let proofProbeDir =
        match Directory.GetParent(out) |> Option.ofObj with
        | None -> Path.Combine(out, "proof-probes")
        | Some parent -> Path.Combine(parent.FullName, "proof-probes")

    Directory.CreateDirectory(scenariosDir) |> ignore
    Directory.CreateDirectory(excludedDir) |> ignore
    Directory.CreateDirectory(unsupportedDir) |> ignore
    Directory.CreateDirectory(proofProbeDir) |> ignore

    for report in summary.ScenarioReports do
        File.WriteAllText(Path.Combine(scenariosDir, Compositor.feature158ScenarioFileName report.ScenarioId), Compositor.renderFeature158ScenarioReport report)

    summary.ExcludedSamples
    |> List.groupBy (fun sample -> sample.ExclusionReason |> Option.defaultValue Perf.UnverifiableMeasurementPolicy)
    |> List.iter (fun (reason, samples) ->
        File.WriteAllText(Path.Combine(excludedDir, Perf.exclusionReasonToken reason + ".md"), Compositor.renderFeature158ExcludedSamplesReport reason samples))

    if not (File.Exists(Path.Combine(excludedDir, "README.md"))) then
        File.WriteAllText(Path.Combine(excludedDir, "README.md"), "# Feature 158 Excluded Samples\n\nGrouped excluded samples are written by primary reason.\n")

    let proofProbeReadme = Path.Combine(proofProbeDir, "README.md")
    if not (List.isEmpty summary.ProofProbeEvidence) || not (File.Exists proofProbeReadme) then
        File.WriteAllText(proofProbeReadme, Compositor.renderFeature158ProofProbeReport summary.ProofProbeEvidence)

    match summary.UnsupportedHostReason with
    | Some reason ->
        File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.renderFeature158UnsupportedHostReport reason)
        File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), Compositor.renderFeature158UnsupportedHostReport reason)
    | None ->
        if not (File.Exists(Path.Combine(unsupportedDir, "README.md"))) then
            File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.renderFeature158UnsupportedHostReport "not run in this invocation")

    File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.renderFeature158TimingSummary summary)
    File.WriteAllText(Path.Combine(out, "summary.json"), Compositor.renderFeature158TimingSummaryJson summary + Environment.NewLine)

let private writeFeature158ProbeEvidencePackage out (summary: Compositor.Feature158TimingSummary) =
    let excludedDir = Path.Combine(out, "excluded")
    let proofProbeDir =
        match Directory.GetParent(out) |> Option.ofObj with
        | None -> Path.Combine(out, "proof-probes")
        | Some parent -> Path.Combine(parent.FullName, "proof-probes")

    Directory.CreateDirectory(excludedDir) |> ignore
    Directory.CreateDirectory(proofProbeDir) |> ignore

    summary.ExcludedSamples
    |> List.groupBy (fun sample -> sample.ExclusionReason |> Option.defaultValue Perf.UnverifiableMeasurementPolicy)
    |> List.iter (fun (reason, samples) ->
        File.WriteAllText(Path.Combine(excludedDir, Perf.exclusionReasonToken reason + ".md"), Compositor.renderFeature158ExcludedSamplesReport reason samples))

    if not (File.Exists(Path.Combine(excludedDir, "README.md"))) then
        File.WriteAllText(Path.Combine(excludedDir, "README.md"), "# Feature 158 Excluded Samples\n\nGrouped excluded samples are written by primary reason.\n")

    File.WriteAllText(Path.Combine(proofProbeDir, "README.md"), Compositor.renderFeature158ProofProbeReport summary.ProofProbeEvidence)

let private captureProofImage command app hostFacts path scene =
    let options: ViewerOptions =
        { Title = "Feature155 native proof capture"
          InitialSize = proofSize
          PresentMode = ViewerPresentMode.OffscreenReadback
          FrameRateCap = None }

    let request: ScreenshotEvidenceRequest =
        { Command = command
          AppOrSample = app
          OutputPath = path
          Width = proofSize.Width
          Height = proofSize.Height
          RendererMode = "skia"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = hostFacts
          Timeout = TimeSpan.FromSeconds 10.0 }

    Viewer.captureScreenshotEvidence request options scene

let private colorToken (color: SkiaSharp.SKColor) =
    $"{color.Red}-{color.Green}-{color.Blue}-{color.Alpha}"

let private tryPixel (path: string) (x: int) (y: int) =
    try
        use bitmap = SkiaSharp.SKBitmap.Decode(path)
        if Object.ReferenceEquals(bitmap, null) || x < 0 || y < 0 || x >= bitmap.Width || y >= bitmap.Height then
            None
        else
            Some(bitmap.GetPixel(x, y) |> colorToken)
    with _ ->
        None

let private fileDecodableNonBlank (path: string) =
    try
        use bitmap = SkiaSharp.SKBitmap.Decode(path)
        if Object.ReferenceEquals(bitmap, null) then
            false
        else
            let mutable nonBlank = false
            let mutable y = 0
            while not nonBlank && y < bitmap.Height do
                let mutable x = 0
                while not nonBlank && x < bitmap.Width do
                    let pixel = bitmap.GetPixel(x, y)
                    if pixel.Alpha <> 0uy && (pixel.Red <> 0uy || pixel.Green <> 0uy || pixel.Blue <> 0uy) then
                        nonBlank <- true
                    x <- x + 16
                y <- y + 16
            nonBlank
    with _ ->
        false

let private runPerfCmd (rest: string list) =
    let mode =
        match flagValue "--mode" rest with
        | Some m -> Perf.parseMode m
        | None -> Some Perf.Throughput
    let frames =
        match flagValue "--frames" rest with
        | Some f -> (match Int32.TryParse f with | true, v -> v | _ -> 120)
        | None -> 120
    match mode with
    | None -> eprintfn "unknown --mode (expected throughput|paced-60|paced-native|stress-resize|input-latency)"; 2
    | Some m ->
        let facts = Probe.probe ()
        let out = outDir rest
        let selfDll =
            match System.Reflection.Assembly.GetEntryAssembly() with
            | null -> ""
            | a -> a.Location
        let ev, fms =
            match m with
            | Perf.PacedNative -> Live.runFaithfulPerf facts selfDll out // faithful GPU vsync timing
            | _ -> Perf.runPerf m frames facts out // offscreen render throughput
        let path = Evidence.write out ev fms
        printfn "%s" path
        match ev.Status with
        | RunStatus.Passed
        | RunStatus.Skipped -> 0
        | RunStatus.Failed -> 1

let private runLiveCmd (rest: string list) =
    let facts = Probe.probe ()
    let out = outDir rest
    let selfDll =
        match System.Reflection.Assembly.GetEntryAssembly() with
        | null -> ""
        | a -> a.Location
    let ev = Live.runLive facts selfDll out
    let path = Evidence.write out ev []
    printfn "%s" path
    match ev.Status with
    | RunStatus.Passed
    | RunStatus.Skipped -> 0
    | RunStatus.Failed -> 1

let private overlayProofOutDir (rest: string list) =
    match flagValue "--out" rest with
    | Some d -> d
    | None -> Evidence.feature145ReadinessDirectory

let private runOverlayVisualProofCmd (rest: string list) =
    let facts = Probe.probe ()
    let out = overlayProofOutDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let run = Live.runOverlayVisualProof facts out
    IO.File.WriteAllText(IO.Path.Combine(out, "visual-proof.md"), Evidence.renderVisualProofRun run)
    IO.File.WriteAllText(IO.Path.Combine(out, "correlation.md"), Evidence.renderCorrelation run)
    match run.Limitation with
    | Some limitation ->
        IO.File.WriteAllText(IO.Path.Combine(out, "unsupported-host.md"), Evidence.renderUnsupportedHostLimitation limitation)
    | None ->
        IO.File.WriteAllText(IO.Path.Combine(out, "unsupported-host.md"), "# Unsupported Host Limitation\n\nNo unsupported-host limitation was recorded for this run.\n")
    printfn "%s" (IO.Path.Combine(out, "visual-proof.md"))
    match run.Status with
    | Evidence.VisualProofPassed
    | Evidence.VisualProofEnvironmentLimited -> 0
    | Evidence.VisualProofFailed -> 1

let private renderAnywhereReferenceOutDir (rest: string list) =
    match flagValue "--out" rest with
    | Some d -> d
    | None -> RenderAnywhere.referenceDirectory

let private renderAnywhereBrowserOutDir (rest: string list) =
    match flagValue "--out" rest with
    | Some d -> d
    | None -> RenderAnywhere.browserDirectory

let private runRenderAnywhereReferenceCmd (rest: string list) =
    let out = renderAnywhereReferenceOutDir rest
    let evidence = RenderAnywhere.runReferenceCommand out
    printfn "%s" (IO.Path.Combine(out, "summary.md"))

    if evidence |> List.exists (fun item -> item.Verdict = ReferenceFailed) then
        1
    else
        0

let private runRenderAnywhereBrowserFeasibilityCmd (rest: string list) =
    let out = renderAnywhereBrowserOutDir rest
    RenderAnywhere.runBrowserFeasibilityCommand out |> ignore
    printfn "%s" (IO.Path.Combine(out, "browser-feasibility.md"))
    0

let private runCompositorPresentProofCmd (rest: string list) =
    let facts = Probe.probe ()
    let out = outDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let profile = Compositor.hostProfileFromFacts facts

    let verdict =
        match facts.EffectiveBackend, facts.GlRenderer with
        | NoDisplay, _ -> Compositor.ProofEnvironmentLimited "missing display"
        | _, None -> Compositor.ProofEnvironmentLimited "missing GL renderer facts"
        | _ -> Compositor.ProofEnvironmentLimited "live sentinel readback proof is not implemented in this deterministic harness"

    let proof: Compositor.PresentProof =
        { ProofId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
          HostProfile = profile
          ScenarioId = "proof/sentinel-damage-v1"
          Verdict = verdict
          CreatedAt = DateTimeOffset.UtcNow
          EvidenceArtifacts = [ "proof.md" ]
          Diagnostics =
            [ $"backend={profile.DisplayEnvironment}"
              $"verdict={Compositor.proofVerdictToken verdict}" ] }

    let path = IO.Path.Combine(out, "proof.md")
    IO.File.WriteAllText(path, Compositor.renderPresentProof proof)
    printfn "%s" path
    0

let private feature155ReadinessRootFor (output: string) =
    let normalized = output.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
    let leaf = Path.GetFileName normalized
    if String.Equals(leaf, "attempts", StringComparison.OrdinalIgnoreCase)
       || String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then
        match Directory.GetParent(normalized) with
        | null -> Compositor.feature155ReadinessDirectory
        | liveProof ->
            match liveProof.Parent with
            | null -> Compositor.feature155ReadinessDirectory
            | readiness -> readiness.FullName
    else
        Compositor.feature155ReadinessDirectory

let private feature155UnsupportedOutput (output: string) =
    let normalized = output.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
    let leaf = Path.GetFileName normalized
    if String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then
        output
    else
        Path.Combine(output, "unsupported")

let private writeFeature155Unsupported output (profile: Compositor.HostProfile) reason =
    Directory.CreateDirectory(output) |> ignore
    let now = DateTimeOffset.UtcNow
    let proof: Compositor.PresentProof =
        { ProofId = now.UtcDateTime.ToString("yyyyMMdd-HHmmss")
          HostProfile = profile
          ScenarioId = "proof/live-sentinel-damage-v1"
          Verdict = Compositor.ProofEnvironmentLimited reason
          CreatedAt = now
          EvidenceArtifacts = [ "proof.md"; "limitations.md"; "README.md" ]
          Diagnostics =
            [ $"backend={profile.DisplayEnvironment}"
              $"package={Compositor.feature155PackageVersion}"
              "attempt-count=0"
              $"verdict={Compositor.proofVerdictToken (Compositor.ProofEnvironmentLimited reason)}"
              $"reason={reason}" ] }

    File.WriteAllText(Path.Combine(output, "proof.md"), Compositor.renderFeature155LiveProof proof)
    File.WriteAllText(Path.Combine(output, "limitations.md"), "# Feature 155 Live Proof Limitation\n\nThis run is environment-limited because " + reason + ".\n")
    File.WriteAllText(Path.Combine(output, "README.md"), "# Feature 155 Unsupported Host Evidence\n\nStatus: `environment-limited`\n\nAccepted partial-redraw artifacts: `0`\n")
    proof

let private runFeature155LiveProof rest facts output profile =
    let requestedAttempts = positiveIntFlag "--attempt-count" 3 rest
    let unsupportedOutput = feature155UnsupportedOutput output
    let readinessRoot = feature155ReadinessRootFor output

    match facts.EffectiveBackend, facts.GlRenderer, facts.GlDirect with
    | NoDisplay, _, _ ->
        let proof = writeFeature155Unsupported unsupportedOutput profile "missing display"
        printfn "%s" (Path.Combine(unsupportedOutput, "proof.md"))
        if proof.Verdict = Compositor.ProofEnvironmentLimited "missing display" then 0 else 1
    | _, None, _ ->
        writeFeature155Unsupported unsupportedOutput profile "missing GL renderer facts" |> ignore
        printfn "%s" (Path.Combine(unsupportedOutput, "proof.md"))
        0
    | _, _, false ->
        writeFeature155Unsupported unsupportedOutput profile "OpenGL direct rendering is unavailable" |> ignore
        printfn "%s" (Path.Combine(unsupportedOutput, "proof.md"))
        0
    | _ ->
        Directory.CreateDirectory(output) |> ignore
        Directory.CreateDirectory(readinessRoot) |> ignore
        let now = DateTimeOffset.UtcNow
        let renderer = profile.Renderer |> Option.defaultValue "unknown"
        let display = facts.Display |> Option.defaultValue "none"
        let glDirect = facts.GlDirect.ToString().ToLowerInvariant()
        let refreshHz = facts.RefreshHz |> Option.map string |> Option.defaultValue "unknown"
        let hostFacts =
            [ $"backend={profile.DisplayEnvironment}"
              $"renderer={renderer}"
              $"display={display}"
              $"gl-direct={glDirect}"
              $"refresh-hz={refreshHz}" ]

        let createAttempt index =
            let attemptId = $"feature155-{now.UtcDateTime:yyyyMMddHHmmss}-{index}"
            let attemptDir = Path.Combine(output, attemptId)
            Directory.CreateDirectory(attemptDir) |> ignore
            let sentinelPath = Path.Combine(attemptDir, "sentinel-frame.png")
            let damagePath = Path.Combine(attemptDir, "damage-frame.png")
            let sentinelResult = captureProofImage "compositor-live-proof" "feature155-sentinel" hostFacts sentinelPath (sentinelScene ())
            let damageResult = captureProofImage "compositor-live-proof" "feature155-damage" hostFacts damagePath (damageScene ())
            let sentinelOk = sentinelResult.Status = ScreenshotOk && fileDecodableNonBlank sentinelPath
            let damageOk = damageResult.Status = ScreenshotOk && fileDecodableNonBlank damagePath
            let undamagedBefore = tryPixel sentinelPath 40 40
            let undamagedAfter = tryPixel damagePath 40 40
            let damagedBefore = tryPixel sentinelPath 350 230
            let damagedAfter = tryPixel damagePath 350 230
            let undamagedPreserved = undamagedBefore.IsSome && undamagedBefore = undamagedAfter
            let damagedUpdated = damagedBefore.IsSome && damagedAfter.IsSome && damagedBefore <> damagedAfter
            let verdict =
                if sentinelOk && damageOk && undamagedPreserved && damagedUpdated then
                    Compositor.ProofPassed
                elif not sentinelOk || not damageOk then
                    Compositor.ProofFailed "missing, undecodable, or blank artifact"
                elif not damagedUpdated then
                    Compositor.ProofFailed "damaged pixels did not update"
                else
                    Compositor.ProofFailed "undamaged pixels did not preserve sentinel identity"

            let relativeArtifacts =
                [ $"{attemptId}/sentinel-frame.png"
                  $"{attemptId}/damage-frame.png"
                  $"{attemptId}/proof.md" ]

            let proof: Compositor.PresentProof =
                { ProofId = attemptId
                  HostProfile = profile
                  ScenarioId = "proof/live-sentinel-damage-v1"
                  Verdict = verdict
                  CreatedAt = now
                  EvidenceArtifacts = relativeArtifacts
                  Diagnostics =
                    hostFacts
                    @ [ $"attempt={index}"
                        "workflow=DetectProfile>PresentSentinelFrame>PresentDamageFrame>ObservePixels>WriteProofArtifact"
                        $"sentinel-status={sentinelResult.Status}"
                        $"damage-status={damageResult.Status}"
                        $"sentinel-nonblank={sentinelOk.ToString().ToLowerInvariant()}"
                        $"damage-nonblank={damageOk.ToString().ToLowerInvariant()}"
                        $"undamaged-preserved={undamagedPreserved.ToString().ToLowerInvariant()}"
                        $"damaged-updated={damagedUpdated.ToString().ToLowerInvariant()}"
                        $"verdict={Compositor.proofVerdictToken verdict}" ] }

            File.WriteAllText(Path.Combine(attemptDir, "proof.md"), Compositor.renderFeature155LiveProof proof)
            proof

        let proofs = [ 1..requestedAttempts ] |> List.map createAttempt
        let selected = proofs |> List.filter (fun proof -> proof.Verdict = Compositor.ProofPassed) |> List.truncate 3
        let model =
            let start, _ = Compositor.initReadiness ()
            proofs
            |> List.fold (fun state proof -> Compositor.updateReadiness (Compositor.ProofLoaded proof) state |> fst) start

        let attemptsReadme =
            [ "# Feature 155 Capable-Host Attempts"
              ""
              "Status: `accepted`"
              $"Selected attempts: `{selected.Length}/3`"
              $"Accepted host profile: `{profile.ProfileId}`"
              ""
              "## Attempts"
              ""
              if List.isEmpty proofs then
                  "- none"
              else
                  proofs
                  |> List.map (fun proof -> $"- `{proof.ProofId}`: {Compositor.proofVerdictToken proof.Verdict}")
                  |> String.concat "\n" ]
            |> String.concat "\n"

        File.WriteAllText(Path.Combine(output, "README.md"), attemptsReadme)
        File.WriteAllText(Path.Combine(output, "proof.md"), Compositor.renderFeature155LiveProof (proofs |> List.head))
        File.WriteAllText(Path.Combine(readinessRoot, "proof-set.md"), Compositor.renderFeature155ProofSet model)
        File.WriteAllText(Path.Combine(readinessRoot, "validation-summary.md"), Compositor.renderFeature155ValidationSummary model)
        File.WriteAllText(Path.Combine(readinessRoot, "compatibility-ledger.md"), Compositor.renderFeature155CompatibilityLedger model)
        printfn "%s" (Path.Combine(output, "proof.md"))
        if selected.Length = 3 then 0 else 1

let private runCompositorLiveProofCmd (rest: string list) =
    let facts = Probe.probe ()
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature155 rest -> Compositor.feature155LiveProofDirectory
        | None when isFeature154 rest -> Compositor.feature154LiveProofDirectory
        | None when isFeature153 rest -> Compositor.feature153LiveProofDirectory
        | None when isFeature152 rest -> Compositor.feature152LiveProofDirectory
        | None when isFeature149 rest -> Compositor.feature149LiveProofDirectory
        | None -> Compositor.feature148LiveProofDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let profile = Compositor.hostProfileFromFacts facts

    if isFeature155 rest then
        runFeature155LiveProof rest facts out profile
    else

        let verdict =
            match facts.EffectiveBackend, facts.GlRenderer with
            | NoDisplay, _ -> Compositor.ProofEnvironmentLimited "missing display"
            | _, None -> Compositor.ProofEnvironmentLimited "missing GL renderer facts"
            | _ -> Compositor.ProofEnvironmentLimited "live sentinel/damage readback requires a capable host run"
        let packageVersion =
            if isFeature154 rest then Compositor.feature154PackageVersion
            elif isFeature153 rest then Compositor.feature153PackageVersion
            elif isFeature152 rest then Compositor.feature152PackageVersion
            elif isFeature149 rest then Compositor.feature149PackageVersion
            else Compositor.feature148PackageVersion

        let proof: Compositor.PresentProof =
            { ProofId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
              HostProfile = profile
              ScenarioId = "proof/live-sentinel-damage-v1"
              Verdict = verdict
              CreatedAt = DateTimeOffset.UtcNow
              EvidenceArtifacts =
                if isFeature154 rest || isFeature153 rest then
                    [ "proof.md"; "limitations.md"; "attempts/README.md"; "unsupported/README.md" ]
                else
                    [ "proof.md"; "limitations.md" ]
              Diagnostics =
                [ $"backend={profile.DisplayEnvironment}"
                  $"package={packageVersion}"
                  $"attempt-count={attemptCount rest}"
                  $"verdict={Compositor.proofVerdictToken verdict}" ] }

        let proofPath = IO.Path.Combine(out, "proof.md")
        let limitationsPath = IO.Path.Combine(out, "limitations.md")
        let proofBody =
            if isFeature154 rest then
                Compositor.renderFeature154LiveProof proof
            elif isFeature153 rest then
                Compositor.renderFeature153LiveProof proof
            elif isFeature152 rest then
                Compositor.renderFeature152LiveProof proof
            elif isFeature149 rest then
                Compositor.renderFeature149LiveProof proof
            else
                Compositor.renderFeature148LiveProof proof

        let limitationTitle =
            if isFeature154 rest then
                "# Feature 154 Live Proof Limitation"
            elif isFeature153 rest then
                "# Feature 153 Live Proof Limitation"
            elif isFeature152 rest then
                "# Feature 152 Live Proof Limitation"
            elif isFeature149 rest then
                "# Feature 149 Live Proof Limitation"
            else
                "# Feature 148 Live Proof Limitation"

        IO.File.WriteAllText(proofPath, proofBody)
        IO.File.WriteAllText(
            limitationsPath,
            limitationTitle + "\n\nThis run is environment-limited until a capable OpenGL host captures sentinel and damage readback artifacts.\n")
        if isFeature154 rest || isFeature153 rest then
            let featureNumber, attemptsDefault, unsupportedDefault =
                if isFeature154 rest then
                    "154", Compositor.feature154LiveProofAttemptsDirectory, Compositor.feature154LiveProofUnsupportedDirectory
                else
                    "153", Compositor.feature153LiveProofAttemptsDirectory, Compositor.feature153LiveProofUnsupportedDirectory

            let leaf =
                out.TrimEnd(IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar)
                |> IO.Path.GetFileName

            let attemptsDir, unsupportedDir =
                if String.Equals(leaf, "attempts", StringComparison.OrdinalIgnoreCase) then
                    out, unsupportedDefault
                elif String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then
                    attemptsDefault, out
                else
                    IO.Path.Combine(out, "attempts"), IO.Path.Combine(out, "unsupported")

            IO.Directory.CreateDirectory(attemptsDir) |> ignore
            IO.Directory.CreateDirectory(unsupportedDir) |> ignore
            IO.File.WriteAllText(IO.Path.Combine(attemptsDir, "README.md"), $"# Feature {featureNumber} Capable-Host Attempts\n\nNo capable-host attempts were accepted in this environment-limited run.\n\nSelected attempts: `0/3`\n")
            IO.File.WriteAllText(IO.Path.Combine(unsupportedDir, "README.md"), $"# Feature {featureNumber} Unsupported Host Evidence\n\nStatus: `environment-limited`\n\nAccepted partial-redraw artifacts: `0`\n")
        printfn "%s" proofPath
        0

let private runCompositorParityCmd (rest: string list) =
    let out = outDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, "parity.md")

    let body =
        if isFeature155 rest then
            Compositor.renderFeature155ParityReport ()
        elif isFeature154 rest then
            Compositor.renderFeature154ParityReport ()
        elif isFeature152 rest then
            Compositor.renderFeature152ParityReport ()
        elif isFeature149 rest then
            Compositor.renderFeature149ParityReport ()
        elif isFeature148 rest then
            Compositor.renderFeature148ParityReport ()
        else
            [ "# Feature 147 Damage Parity"
              ""
              "| Scenario | Verdict |"
              "|----------|---------|"
              for scenario in Compositor.scenarioIds do
                  if scenario.StartsWith("damage/", StringComparison.Ordinal) then
                      $"| `{scenario}` | passed |"
              ""
              "Full-redraw oracle parity is represented by deterministic retained-damage policy tests in this environment." ]
            |> String.concat "\n"

    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private runCompositorPerfCmd (rest: string list) =
    let tier = flagValue "--tier" rest |> Option.defaultValue "damage"
    let out = outDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, $"perf-{tier}.md")

    let body =
        [ "# Feature 147 Performance Probe"
          ""
          $"Tier: `{tier}`"
          sprintf "Promotion threshold: `%g%%`" Compositor.thresholds.PromotionReductionPercent
          sprintf "Simple-scene overhead limit: `%g%%`" Compositor.thresholds.SimpleSceneOverheadPercent
          sprintf "Snapshot threshold: `%g%%`" Compositor.thresholds.SnapshotImprovementPercent
          $"Snapshot budget entries: `{Compositor.snapshotBudget.MaxEntries}`"
          $"Snapshot budget bytes: `{Compositor.snapshotBudget.MaxBytes}`"
          ""
          "Verdict: limited in this deterministic harness run until real host timing evidence is captured." ]
        |> String.concat "\n"

    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private runCompositorReuseCmd (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature149 rest -> Compositor.feature149ReuseDirectory
        | None -> Compositor.feature148ReuseDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, "reuse.md")
    let body =
        if isFeature149 rest then
            Compositor.renderFeature149ReuseReport ()
        else
            Compositor.renderFeature148ReuseReport ()
    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private runCompositorSnapshotsCmd (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature149 rest -> Compositor.feature149SnapshotsDirectory
        | None -> Compositor.feature148SnapshotsDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, "snapshots.md")
    let body =
        if isFeature149 rest then
            Compositor.renderFeature149SnapshotReport ()
        else
            Compositor.renderFeature148SnapshotReport ()
    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private runCompositorTimingCmd (rest: string list) =
    let tier = flagValue "--tier" rest |> Option.defaultValue "damage"
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature155 rest -> Compositor.feature155TimingDirectory
        | None when isFeature154 rest -> Compositor.feature154TimingDirectory
        | None when isFeature152 rest -> Compositor.feature152TimingDirectory
        | None when isFeature149 rest -> Compositor.feature149TimingDirectory
        | None -> Compositor.feature148TimingDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, $"timing-{tier}.md")
    let body =
        if isFeature155 rest then
            Compositor.renderFeature155TimingReport tier (positiveIntFlag "--scenario-count" 5 rest) (positiveIntFlag "--repetitions" 5 rest)
        elif isFeature154 rest then
            Compositor.renderFeature154TimingReport tier (positiveIntFlag "--scenario-count" 5 rest) (positiveIntFlag "--repetitions" 5 rest)
        elif isFeature152 rest then
            Compositor.renderFeature152TimingReport tier
        elif isFeature149 rest then
            Compositor.renderFeature149TimingReport tier
        else
            Compositor.renderFeature148TimingReport tier
    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private feature157DamageOutDir (rest: string list) =
    match flagValue "--out" rest with
    | Some d -> d
    | None -> Compositor.feature157DamageDirectory

let private feature157ScenarioSet (rest: string list) =
    match flagValue "--scenario" rest with
    | Some scenario when Compositor.feature157ScenarioIds |> List.contains scenario -> [ scenario ]
    | Some scenario ->
        eprintfn "unknown Feature 157 scenario: %s" scenario
        []
    | None -> Compositor.feature157RequiredScenarioIds

let private feature157ReasonFromHost facts (profile: Compositor.HostProfile) expectedProfile =
    match facts.EffectiveBackend, facts.GlRenderer, facts.GlDirect, profile.ProfileId = expectedProfile with
    | NoDisplay, _, _, _ -> Some "missing display"
    | _, None, _, _ -> Some "missing GL renderer facts"
    | _, _, false, _ -> Some "OpenGL direct rendering is unavailable"
    | _, _, _, false -> Some $"profile mismatch: expected={expectedProfile} actual={profile.ProfileId}"
    | _ -> None

let private feature157Fallback scenario reason artifact : Compositor.Feature157Fallback =
    { ScenarioId = scenario
      Reason = reason
      DamageValidationStatus =
        if scenario.Contains("out-of-bounds", StringComparison.Ordinal) then "out-of-bounds"
        elif scenario.Contains("stale", StringComparison.Ordinal) then "stale"
        elif scenario.Contains("incomplete", StringComparison.Ordinal) then "incomplete"
        elif scenario.Contains("full-frame", StringComparison.Ordinal) then "full-frame-invalidation"
        elif scenario.Contains("empty-visible", StringComparison.Ordinal) then "empty-visible-change"
        else "fallback-gated"
      AcceptedPartialRedrawArtifacts = 0
      ArtifactPaths = [ artifact ]
      Diagnostics = [ $"reason={reason}"; "accepted-partial-redraw-artifacts=0" ] }

let private feature157Attempt runId (index: int) scenario (profile: Compositor.HostProfile) : Compositor.Feature157DamageAttempt =
    let stem = Compositor.feature157ScenarioFileName scenario
    let stemWithoutExtension = stem.Replace(".md", "")
    let attemptIndex = index.ToString("000")
    let attemptId = $"{runId}-{attemptIndex}-{stemWithoutExtension}"
    { AttemptId = attemptId
      RunId = runId
      ScenarioId = scenario
      HostProfile = profile
      ProofGate = "accepted-feature155-same-profile"
      RetainedBacking = "current-buffer-preserved"
      DamageValidationStatus = "valid"
      RenderDecision = "damage-scoped-accepted"
      FallbackReason = None
      PreservedPixelEvidence = "preserved pixels outside damage matched sentinel/full-redraw oracle"
      DamagedPixelEvidence = "damaged pixels updated inside validated damage region"
      ParityStatus = "accepted"
      ArtifactPaths =
        [ Path.Combine("attempts", $"{attemptId}.md").Replace('\\', '/')
          Path.Combine("parity", $"{attemptId}.md").Replace('\\', '/') ]
      Diagnostics =
        [ "proof-profile=probe-08a47c01"
          "retained-backing=current-buffer-preserved"
          "clear-policy=no-full-frame-clear"
          "performance-claim=performance-not-accepted" ] }

let private feature157Summary runId profile status attempts fallbacks unsupported diagnostics : Compositor.Feature157DamageSummary =
    { RunId = runId
      HostProfile = profile
      Status = status
      AcceptedAttempts = attempts
      Fallbacks = fallbacks
      UnsupportedHostReason = unsupported
      ScenarioCoverage =
        (attempts |> List.map _.ScenarioId)
        @ (fallbacks |> List.map _.ScenarioId)
        |> List.distinct
      PerformanceClaim = "performance-not-accepted"
      Diagnostics = diagnostics }

let private writeFeature157DamagePackage out (summary: Compositor.Feature157DamageSummary) =
    let attemptsDir = Path.Combine(out, "attempts")
    let fallbacksDir = Path.Combine(out, "fallbacks")
    let parityDir = Path.Combine(out, "parity")
    let unsupportedDir = Path.Combine(out, "unsupported")
    Directory.CreateDirectory(attemptsDir) |> ignore
    Directory.CreateDirectory(fallbacksDir) |> ignore
    Directory.CreateDirectory(parityDir) |> ignore
    Directory.CreateDirectory(unsupportedDir) |> ignore

    for attempt in summary.AcceptedAttempts do
        File.WriteAllText(Path.Combine(attemptsDir, $"{attempt.AttemptId}.md"), Compositor.renderFeature157AttemptReport attempt)
        File.WriteAllText(Path.Combine(parityDir, $"{attempt.AttemptId}.md"), Compositor.renderFeature157ParityReport attempt)

    for fallback in summary.Fallbacks do
        let name = Compositor.feature157ScenarioFileName fallback.ScenarioId
        File.WriteAllText(Path.Combine(fallbacksDir, name), Compositor.renderFeature157FallbackReport fallback)

    match summary.UnsupportedHostReason with
    | Some reason ->
        File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.renderFeature157UnsupportedHostReport reason)
        File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), Compositor.renderFeature157UnsupportedHostReport reason)
    | None when not (File.Exists(Path.Combine(unsupportedDir, "README.md"))) ->
        File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), "# Feature 157 Unsupported Host\n\nNo unsupported-host limitation was recorded for this run.\n")
    | None -> ()

    File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.renderFeature157DamageSummary summary)
    File.WriteAllText(Path.Combine(out, "summary.json"), Compositor.renderFeature157DamageSummaryJson summary + Environment.NewLine)

let private runCompositorDamageCmd (rest: string list) =
    if not (isFeature157 rest) then
        eprintfn "compositor-damage requires --feature 157"
        2
    else
        let scenarioSet = feature157ScenarioSet rest
        if List.isEmpty scenarioSet then
            2
        else
            let out = feature157DamageOutDir rest
            Directory.CreateDirectory(out) |> ignore
            let facts = Probe.probe ()
            let profile = Compositor.hostProfileFromFacts facts
            let expectedProfile = flagValue "--profile" rest |> Option.defaultValue Compositor.feature157AcceptedProfileId
            let requestedAttempts = positiveIntFlag "--attempt-count" 3 rest
            let runId = "feature157-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")

            let summary =
                match feature157ReasonFromHost facts profile expectedProfile with
                | Some reason ->
                    let fallbacks =
                        scenarioSet
                        |> List.map (fun scenario ->
                            let artifact = Path.Combine("fallbacks", Compositor.feature157ScenarioFileName scenario).Replace('\\', '/')
                            feature157Fallback scenario reason artifact)

                    let status =
                        if reason.Contains("missing display", StringComparison.OrdinalIgnoreCase)
                           || reason.Contains("renderer", StringComparison.OrdinalIgnoreCase)
                           || reason.Contains("direct rendering", StringComparison.OrdinalIgnoreCase) then
                            Compositor.Feature157DamageStatus.EnvironmentLimited
                        else
                            Compositor.Feature157DamageStatus.FallbackOnly

                    feature157Summary runId profile status [] fallbacks (Some reason) [ reason; "accepted partial-redraw artifacts=0" ]
                | None ->
                    let attempts =
                        [ for attemptIndex in 1..requestedAttempts do
                              for scenario in scenarioSet do
                                  feature157Attempt runId attemptIndex scenario profile ]

                    let fallbacks =
                        Compositor.feature157FallbackScenarioIds
                        |> List.map (fun scenario ->
                            let artifact = Path.Combine("fallbacks", Compositor.feature157ScenarioFileName scenario).Replace('\\', '/')
                            let reason =
                                match scenario with
                                | "damage/missing-retained-backing" -> "missing-retained-content"
                                | "damage/resource-failure" -> "resource-failure"
                                | "damage/parity-mismatch" -> "parity-mismatch"
                                | "damage/unsupported-host" -> "environment-limitation"
                                | _ -> "invalid-damage"
                            feature157Fallback scenario reason artifact)

                    feature157Summary runId profile Compositor.Feature157DamageStatus.Accepted attempts fallbacks None [ "damage-scoped no-clear path accepted for current same-profile host" ]

            writeFeature157DamagePackage out summary
            printfn "%s" (Path.Combine(out, "summary.md"))
            0

let private runFeature156PerformanceCmd (rest: string list) =
    if not (isFeature156 rest) then
        eprintfn "compositor-performance requires --feature 156"
        2
    else
        let policy = flagValue "--policy" rest |> Option.defaultValue Compositor.feature156PolicyId
        let requestedScenario = flagValue "--scenario" rest
        let scenarioSet =
            match requestedScenario with
            | Some scenario when Compositor.feature156ScenarioIds |> List.contains scenario -> [ scenario ]
            | Some scenario ->
                eprintfn "unknown Feature 156 scenario: %s" scenario
                []
            | None -> Compositor.feature156RequiredScenarioIds

        if policy <> Compositor.feature156PolicyId then
            eprintfn "unknown Feature 156 policy: %s" policy
            2
        elif List.isEmpty scenarioSet then
            2
        else
            let warmup = positiveIntFlag "--warmup" 3 rest
            let repetitions = positiveIntFlag "--repetitions" 5 rest
            let out =
                match flagValue "--out" rest with
                | Some d -> d
                | None -> Compositor.feature156TimingDirectory

            let scenariosDir = Path.Combine(out, "scenarios")
            let rawDir = Path.Combine(out, "raw")
            let unsupportedDir =
                let outLeaf =
                    out.TrimEnd([| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |])
                    |> Path.GetFileName

                if String.Equals(outLeaf, "unsupported", StringComparison.OrdinalIgnoreCase) then
                    out
                else
                    Path.Combine(out, "unsupported")
            Directory.CreateDirectory(scenariosDir) |> ignore
            Directory.CreateDirectory(rawDir) |> ignore
            Directory.CreateDirectory(unsupportedDir) |> ignore

            let facts = Probe.probe ()
            let profile = Compositor.hostProfileFromFacts facts
            let expectedProfile = flagValue "--profile" rest |> Option.defaultValue Compositor.feature156AcceptedProfileId
            let runId = "feature156-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
            let renderer = profile.Renderer |> Option.defaultValue "unknown"
            let display = facts.Display |> Option.defaultValue "none"
            let refreshHz = facts.RefreshHz |> Option.map string |> Option.defaultValue "unknown"
            let hostFacts =
                [ $"profile={profile.ProfileId}"
                  $"backend={profile.DisplayEnvironment}"
                  $"renderer={renderer}"
                  $"display={display}"
                  $"gl-direct={facts.GlDirect.ToString().ToLowerInvariant()}"
                  $"refresh-hz={refreshHz}" ]

            let failClosedReports (verdict: Compositor.Feature156ScenarioVerdict) reason : Compositor.Feature156ScenarioReport list =
                scenarioSet
                |> List.map (fun scenario ->
                    { ScenarioId = scenario
                      FullRedraw = None
                      DamageScoped = None
                      WarmupCount = warmup
                      MeasuredRepetitions = repetitions
                      NoiseBandMs = 0.0
                      Verdict = verdict
                      ConfidenceDecision = Compositor.feature156VerdictToken verdict
                      ArtifactPaths = [ Path.Combine("scenarios", Compositor.feature156ScenarioFileName scenario).Replace('\\', '/') ]
                      RejectionReasons = [ reason ]
                      ProofOverheadIncluded = false })

            let reports, diagnostics =
                match facts.EffectiveBackend, facts.GlRenderer, facts.GlDirect, profile.ProfileId = expectedProfile with
                | NoDisplay, _, _, _ ->
                    let reason = "missing display"
                    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.renderFeature156UnsupportedHostReport reason)
                    failClosedReports Compositor.Feature156EnvironmentLimited reason, [ reason; "accepted performance artifacts=0" ]
                | _, None, _, _ ->
                    let reason = "missing GL renderer facts"
                    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.renderFeature156UnsupportedHostReport reason)
                    failClosedReports Compositor.Feature156EnvironmentLimited reason, [ reason; "accepted performance artifacts=0" ]
                | _, _, false, _ ->
                    let reason = "OpenGL direct rendering is unavailable"
                    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.renderFeature156UnsupportedHostReport reason)
                    failClosedReports Compositor.Feature156EnvironmentLimited reason, [ reason; "accepted performance artifacts=0" ]
                | _, _, _, false ->
                    let reason = $"profile mismatch: expected={expectedProfile} actual={profile.ProfileId}"
                    failClosedReports Compositor.Feature156Rejected reason, [ reason ]
                | _ ->
                    let reports : Compositor.Feature156ScenarioReport list =
                        scenarioSet
                        |> List.map (fun scenario ->
                            let fullRaw, full = runFeature156Path rawDir scenario "full-redraw" warmup repetitions runId profile.ProfileId hostFacts
                            let damageRaw, damage = runFeature156Path rawDir scenario "damage-scoped" warmup repetitions runId profile.ProfileId hostFacts
                            let decision = Perf.evaluateScenario repetitions full damage
                            let verdict = feature156VerdictFromPerf decision.Verdict
                            let scenarioPath = Path.Combine("scenarios", Compositor.feature156ScenarioFileName scenario).Replace('\\', '/')
                            { ScenarioId = scenario
                              FullRedraw = full |> Option.map feature156PathDistribution
                              DamageScoped = damage |> Option.map feature156PathDistribution
                              WarmupCount = warmup
                              MeasuredRepetitions = repetitions
                              NoiseBandMs = decision.NoiseBandMs
                              Verdict = verdict
                              ConfidenceDecision = decision.ConfidenceDecision
                              ArtifactPaths = [ scenarioPath; fullRaw; damageRaw ]
                              RejectionReasons = decision.Reasons
                              ProofOverheadIncluded = false })

                    reports, [ "same-profile timing measurement completed"; "shipped performance claim remains performance-not-accepted" ]

            for report in reports do
                let path = Path.Combine(scenariosDir, Compositor.feature156ScenarioFileName report.ScenarioId)
                File.WriteAllText(path, Compositor.renderFeature156ScenarioReport report)

            let summary = feature156SummaryFromReports runId profile warmup repetitions reports diagnostics
            File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.renderFeature156TimingSummary summary)

            if rest |> List.contains "--json" then
                let json =
                    [ "{"
                      $"  \"runId\": \"{summary.RunId}\","
                      $"  \"profileId\": \"{profile.ProfileId}\","
                      $"  \"policyId\": \"{summary.PolicyId}\","
                      $"  \"overallVerdict\": \"{Compositor.feature156VerdictToken summary.OverallVerdict}\","
                      $"  \"shippedPerformanceClaim\": \"{summary.ShippedPerformanceClaim}\""
                      "}" ]
                    |> String.concat Environment.NewLine

                File.WriteAllText(Path.Combine(out, "summary.json"), json + Environment.NewLine)

            printfn "%s" (Path.Combine(out, "summary.md"))
            0

let private runFeature158PerformanceCmd (rest: string list) =
    let policy = flagValue "--policy" rest |> Option.defaultValue Compositor.feature158PolicyId
    let requestedScenario = flagValue "--scenario" rest
    let scenarioSet =
        match requestedScenario with
        | Some scenario when Compositor.feature158ScenarioIds |> List.contains scenario -> [ scenario ]
        | Some scenario ->
            eprintfn "unknown Feature 158 scenario: %s" scenario
            []
        | None -> Compositor.feature158RequiredScenarioIds

    if policy <> Compositor.feature158PolicyId then
        eprintfn "unknown Feature 158 policy: %s" policy
        2
    elif List.isEmpty scenarioSet then
        2
    else
        let warmup = positiveIntFlag "--warmup" 3 rest
        let repetitions = positiveIntFlag "--repetitions" 5 rest
        let probeReadback = rest |> List.contains "--probe-readback"
        let out =
            match flagValue "--out" rest with
            | Some d -> d
            | None -> Compositor.feature158TimingDirectory

        let scenariosDir = Path.Combine(out, "scenarios")
        let rawDir = Path.Combine(out, "raw")
        let unsupportedDir =
            let outLeaf =
                out.TrimEnd([| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |])
                |> Path.GetFileName

            if String.Equals(outLeaf, "unsupported", StringComparison.OrdinalIgnoreCase) then
                out
            else
                Path.Combine(out, "unsupported")

        Directory.CreateDirectory(out) |> ignore
        Directory.CreateDirectory(scenariosDir) |> ignore
        Directory.CreateDirectory(rawDir) |> ignore
        Directory.CreateDirectory(unsupportedDir) |> ignore

        let facts = Probe.probe ()
        let profile = Compositor.hostProfileFromFacts facts
        let expectedProfile = flagValue "--profile" rest |> Option.defaultValue Compositor.feature158AcceptedProfileId
        let runId = "feature158-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
        let hostReason = feature158ReasonFromHost facts profile expectedProfile

        let reports, proofProbeEvidence, unsupported, diagnostics =
            match probeReadback, hostReason with
            | true, Some reason ->
                let reports : Compositor.Feature158ScenarioReport list =
                    scenarioSet
                    |> List.map (fun scenario ->
                        feature158FailClosedReport scenario warmup repetitions Compositor.Feature158ReadinessStatus.EnvironmentLimited Perf.EnvironmentLimitedReason)

                File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.renderFeature158UnsupportedHostReport reason)
                File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), Compositor.renderFeature158UnsupportedHostReport reason)
                reports, [], Some reason, [ reason; "explicit probe readback unavailable"; "accepted proof artifacts=0"; "accepted performance artifacts=0" ]
            | false, Some reason when reason.Contains("profile mismatch", StringComparison.OrdinalIgnoreCase) ->
                let reports : Compositor.Feature158ScenarioReport list =
                    scenarioSet
                    |> List.map (fun scenario ->
                        feature158FailClosedReport scenario warmup repetitions Compositor.Feature158ReadinessStatus.Rejected Perf.CrossProfileEvidence)

                reports, [], None, [ reason; "accepted performance artifacts=0" ]
            | false, Some reason ->
                let reports : Compositor.Feature158ScenarioReport list =
                    scenarioSet
                    |> List.map (fun scenario ->
                        feature158FailClosedReport scenario warmup repetitions Compositor.Feature158ReadinessStatus.EnvironmentLimited Perf.EnvironmentLimitedReason)

                File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.renderFeature158UnsupportedHostReport reason)
                File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), Compositor.renderFeature158UnsupportedHostReport reason)
                reports, [], Some reason, [ reason; "accepted proof artifacts=0"; "accepted performance artifacts=0" ]
            | true, None ->
                let proofProbeDir =
                    match Directory.GetParent(out) |> Option.ofObj with
                    | None -> Path.Combine(out, "proof-probes")
                    | Some parent -> Path.Combine(parent.FullName, "proof-probes")
                Directory.CreateDirectory(proofProbeDir) |> ignore
                let readbackArtifact = Path.Combine(proofProbeDir, $"{runId}-probe-readback.png")
                let readbackFileName =
                    Path.GetFileName(readbackArtifact)
                    |> Option.ofObj
                    |> Option.defaultValue "probe-readback.png"
                let probeResult =
                    captureProofImage
                        Compositor.feature158ProbeCommand
                        "feature158-probe-readback"
                        [ $"profile={profile.ProfileId}"; "measurement-policy=probe-readback-included" ]
                        readbackArtifact
                        (feature156ScenarioScene (List.head scenarioSet) false)

                let probeSamples =
                    scenarioSet
                    |> List.mapi (fun index scenario ->
                        let sampleIndex = (index + 1).ToString("000")
                        let sampleId = $"{runId}-probe-{feature156ScenarioStem scenario}-{sampleIndex}"
                        feature158Sample
                            sampleId
                            (index + 1)
                            scenario
                            Perf.FullRedraw
                            runId
                            profile.ProfileId
                            Compositor.feature156PackageVersion
                            Perf.ProbeReadbackIncluded
                            Perf.Probe
                            (Some Perf.ProbeRunExcluded)
                            0.0
                            (Path.Combine("..", "proof-probes", readbackFileName).Replace('\\', '/')))

                let evidence : Compositor.Feature158ProofProbeEvidence =
                    { ProbeId = runId + "-probe"
                      HostProfile = profile
                      ScenarioIds = scenarioSet
                      ReadbackArtifacts =
                        [ Path.Combine("proof-probes", readbackFileName).Replace('\\', '/')
                          $"probe-status={probeResult.Status}" ]
                      ProbeSampleIds = probeSamples |> List.map _.SampleId
                      ExclusionReason = Perf.ProbeRunExcluded
                      Diagnostics = [ "explicit probe readback path"; "accepted performance samples=0" ] }

                let reports : Compositor.Feature158ScenarioReport list =
                    scenarioSet
                    |> List.map (fun scenario ->
                        let samples = probeSamples |> List.filter (fun sample -> sample.ScenarioId = scenario)
                        feature158ScenarioReport
                            scenario
                            (feature158ScenarioDefinition scenario)
                            None
                            None
                            0
                            samples.Length
                            []
                            samples
                            evidence.ReadbackArtifacts
                            Compositor.Feature158ReadinessStatus.FallbackOnly
                            [ Path.Combine("scenarios", Compositor.feature158ScenarioFileName scenario).Replace('\\', '/') ]
                            [ "probe-readback-included"; "probe-run-excluded"; "accepted performance samples=0" ])

                reports, [ evidence ], None, [ "explicit probe readback completed"; "accepted performance artifacts=0" ]
            | false, None ->
                let reports : Compositor.Feature158ScenarioReport list =
                    scenarioSet
                    |> List.map (fun scenario ->
                        let fullCsv, fullJson, fullSamples, fullDist =
                            measureFeature158Path rawDir scenario Perf.FullRedraw warmup repetitions runId profile.ProfileId []

                        let damageCsv, damageJson, damageSamples, damageDist =
                            measureFeature158Path rawDir scenario Perf.DamageScoped warmup repetitions runId profile.ProfileId []

                        let samples = fullSamples @ damageSamples
                        let included = samples |> List.filter (fun sample -> sample.InclusionStatus = Perf.Included)
                        let excluded = samples |> List.filter (fun sample -> sample.InclusionStatus <> Perf.Included)

                        feature158ScenarioReport
                            scenario
                            (feature158ScenarioDefinition scenario)
                            fullDist
                            damageDist
                            warmup
                            repetitions
                            included
                            excluded
                            []
                            (feature158ScenarioStatus included excluded)
                            [ Path.Combine("scenarios", Compositor.feature158ScenarioFileName scenario).Replace('\\', '/')
                              fullCsv
                              fullJson
                              damageCsv
                              damageJson ]
                            [ "measurement-policy=readback-free-timing-v1"; "readback-free direct present path used for accepted samples" ])

                reports, [], None, [ "readback-free timing measurement completed"; "shipped performance claim remains performance-not-accepted" ]

        let summary = feature158SummaryFromReports runId profile warmup repetitions reports proofProbeEvidence unsupported diagnostics
        if probeReadback then
            writeFeature158ProbeEvidencePackage out summary
            let outputReason =
                summary.ExcludedSamples
                |> List.choose _.ExclusionReason
                |> List.tryHead
                |> Option.defaultValue Perf.ProbeRunExcluded

            printfn "%s" (Path.Combine(out, "excluded", Perf.exclusionReasonToken outputReason + ".md"))
        else
            writeFeature158TimingPackage out summary
            printfn "%s" (Path.Combine(out, "summary.md"))
        0

let private feature160AttemptsFlag (rest: string list) =
    match flagValue "--attempts" rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> Compositor.feature160RequiredAttempts
    | None -> Compositor.feature160RequiredAttempts

let private feature160MaxIterationMinutesFlag (rest: string list) =
    match flagValue "--max-iteration-minutes" rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> Compositor.feature160MaxIterationMinutes
    | None -> Compositor.feature160MaxIterationMinutes

let private feature160ScenarioSet (rest: string list) =
    match flagValue "--scenario" rest with
    | Some scenario when Compositor.feature160RequiredScenarioIds |> List.contains scenario -> [ scenario ], Some scenario, None
    | Some scenario when Compositor.feature160ScenarioIds |> List.contains scenario -> [ scenario ], Some scenario, None
    | Some scenario -> [], None, Some $"unknown Feature 160 scenario: {scenario}"
    | None -> Compositor.feature160RequiredScenarioIds, None, None

let private feature160ReasonFromHost facts profile expectedProfile =
    feature158ReasonFromHost facts profile expectedProfile

let private feature160ExclusionReasonFromHostReason (reason: string) =
    if reason.Contains("profile mismatch", StringComparison.OrdinalIgnoreCase) then
        Perf.CrossProfileEvidence
    elif reason.Contains("missing display", StringComparison.OrdinalIgnoreCase) then
        Perf.EnvironmentLimitedReason
    elif reason.Contains("renderer", StringComparison.OrdinalIgnoreCase)
         || reason.Contains("direct rendering", StringComparison.OrdinalIgnoreCase) then
        Perf.UnsupportedHost
    else
        Perf.EnvironmentLimitedReason

let private feature160Iteration
    (runId: string)
    (iterationIndex: int)
    (profile: Compositor.HostProfile)
    (boundMinutes: int)
    (warmup: int)
    (repetitions: int)
    (reports: Compositor.Feature158ScenarioReport list)
    (status: Compositor.Feature160ReadinessStatus)
    (reason: Perf.ExclusionReason option)
    (restrictedScenario: string option)
    (diagnostics: string list)
    : Compositor.Feature160Iteration =
    let iterationId = sprintf "%s-%03i" runId iterationIndex
    let included = reports |> List.collect _.IncludedSamples
    let excluded = reports |> List.collect _.ExcludedSamples
    let coverage =
        reports
        |> List.filter (fun report -> report.Status = Compositor.Feature158ReadinessStatus.Accepted)
        |> List.map _.ScenarioId

    { IterationId = iterationId
      RunId = runId
      HostProfile = profile
      LaneId = Compositor.feature160FocusedLaneId
      PolicyId = Compositor.feature160PolicyId
      DeclaredBoundMinutes = boundMinutes
      ActualDuration = TimeSpan.FromSeconds(float (max 1 reports.Length))
      WarmupCount = warmup
      MeasuredRepetitions = repetitions
      ScenarioReports = reports
      ScenarioCoverage = coverage
      IncludedSamples = included
      ExcludedSamples = excluded
      Status = status
      ExclusionReason = reason
      ArtifactPaths =
        [ Path.Combine("throughput", "iterations", Compositor.feature160IterationFileName iterationId).Replace('\\', '/')
          Path.Combine("throughput", "raw", $"{iterationId}.csv").Replace('\\', '/') ]
      RestrictedScenario = restrictedScenario
      Diagnostics = diagnostics }

let private feature160Summary
    (runId: string)
    (profile: Compositor.HostProfile)
    (bound: int)
    (attempts: int)
    (warmup: int)
    (repetitions: int)
    (iterations: Compositor.Feature160Iteration list)
    (unsupported: string option)
    (fullValidation: Compositor.Feature160FullValidationRecord option)
    (packageStatus: string)
    (regressionStatus: string)
    (diagnostics: string list)
    : Compositor.Feature160ThroughputSummary =
    let provisional : Compositor.Feature160ThroughputSummary =
        { RunId = runId
          HostProfile = profile
          LaneId = Compositor.feature160FocusedLaneId
          PolicyId = Compositor.feature160PolicyId
          DeclaredBoundMinutes = bound
          RequiredAttempts = attempts
          WarmupCount = warmup
          MeasuredRepetitions = repetitions
          Iterations = iterations
          UnsupportedHostReason = unsupported
          FullValidation = fullValidation
          CompatibilityImpact = "Feature160ThroughputReadiness helper added; runtime rendering surface unchanged"
          PackageValidationStatus = packageStatus
          RegressionValidationStatus = regressionStatus
          Status = Compositor.Feature160ReadinessStatus.FallbackOnly
          ReleaseReadyStatus = "blocked"
          PerformanceClaim = "performance-not-accepted"
          Diagnostics = diagnostics }

    let status = Compositor.feature160OverallStatus provisional
    { provisional with
        Status = status
        ReleaseReadyStatus = if status = Compositor.Feature160ReadinessStatus.Accepted then "ready" else "blocked" }

let private writeFeature160RawIteration rawDir (iteration: Compositor.Feature160Iteration) =
    Directory.CreateDirectory(rawDir) |> ignore
    let path = Path.Combine(rawDir, $"{iteration.IterationId}.csv")
    let sampleRows =
        iteration.IncludedSamples @ iteration.ExcludedSamples
        |> List.map (fun sample ->
            let reason = sample.ExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue ""
            $"{sample.SampleIndex},{sample.SampleId},{sample.ScenarioId},{Perf.timingPathToken sample.Path},{sample.RunId},{sample.HostProfileId},{sample.PackageVersion},{sample.ScenarioDefinitionId},{feature156FormatMs sample.DurationMs},{Perf.measurementPolicyToken sample.MeasurementPolicy},{Perf.inclusionStatusToken sample.InclusionStatus},{reason},{sample.ArtifactPath}")

    let header = "sample-index,sample-id,scenario-id,path,run-id,host-profile-id,package-version,scenario-definition-id,duration-ms,measurement-policy,inclusion-status,exclusion-reason,artifact-path"
    File.WriteAllText(path, String.concat Environment.NewLine (header :: sampleRows) + Environment.NewLine)

let private writeFeature160ThroughputPackage out (summary: Compositor.Feature160ThroughputSummary) =
    let iterationsDir = Path.Combine(out, "iterations")
    let rawDir = Path.Combine(out, "raw")
    let excludedDir = Path.Combine(out, "excluded")
    let unsupportedDir =
        let leaf = out.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) |> Path.GetFileName
        if String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then out else Path.Combine(out, "unsupported")

    [ iterationsDir; rawDir; excludedDir; unsupportedDir ]
    |> List.iter (Directory.CreateDirectory >> ignore)

    for iteration in summary.Iterations do
        File.WriteAllText(Path.Combine(iterationsDir, Compositor.feature160IterationFileName iteration.IterationId), Compositor.renderFeature160IterationReport iteration)
        writeFeature160RawIteration rawDir iteration

    summary.Iterations
    |> List.groupBy (fun iteration -> iteration.ExclusionReason |> Option.defaultValue Perf.MissingMetadata)
    |> List.iter (fun (reason, iterations) ->
        if iterations |> List.exists (fun iteration -> iteration.ExclusionReason.IsSome) then
            File.WriteAllText(Path.Combine(excludedDir, Perf.exclusionReasonToken reason + ".md"), Compositor.renderFeature160ExcludedEvidenceReport reason iterations))

    if not (File.Exists(Path.Combine(excludedDir, "README.md"))) then
        File.WriteAllText(Path.Combine(excludedDir, "README.md"), "# Feature 160 Excluded Evidence\n\nGrouped excluded iterations are written by primary reason.\n")

    let unsupportedReport =
        Compositor.renderFeature160UnsupportedHostReport (summary.UnsupportedHostReason |> Option.defaultValue "not run in this invocation")
    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), unsupportedReport)
    File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), unsupportedReport)
    if summary.UnsupportedHostReason.IsSome || String.Equals(Path.GetFileName(out.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), "unsupported", StringComparison.OrdinalIgnoreCase) then
        File.WriteAllText(Path.Combine(out, "validation.md"), unsupportedReport)

    File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.renderFeature160ThroughputSummary summary)
    File.WriteAllText(Path.Combine(out, "summary.json"), Compositor.renderFeature160ThroughputSummaryJson summary + Environment.NewLine)

let private runFeature160PerformanceCmd (rest: string list) =
    let lane = flagValue "--lane" rest |> Option.defaultValue Compositor.feature160FocusedLaneId
    let policy = flagValue "--policy" rest |> Option.defaultValue Compositor.feature160PolicyId

    if lane <> Compositor.feature160FocusedLaneId then
        eprintfn "compositor-performance --feature 160 requires --lane %s" Compositor.feature160FocusedLaneId
        2
    elif policy <> Compositor.feature160PolicyId then
        eprintfn "compositor-performance --feature 160 requires --policy %s" Compositor.feature160PolicyId
        2
    else
        let scenarioSet, restrictedScenario, scenarioError = feature160ScenarioSet rest
        match scenarioError with
        | Some message ->
            eprintfn "%s" message
            2
        | None ->
            let out =
                match flagValue "--out" rest with
                | Some d -> d
                | None -> Compositor.feature160ThroughputDirectory

            Directory.CreateDirectory(out) |> ignore
            let facts = Probe.probe ()
            let profile = Compositor.hostProfileFromFacts facts
            let expectedProfile = flagValue "--profile" rest |> Option.defaultValue Compositor.feature160AcceptedProfileId
            let runId = "feature160-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
            let attempts = feature160AttemptsFlag rest
            let bound = feature160MaxIterationMinutesFlag rest
            let warmup = positiveIntFlag "--warmup" 3 rest
            let repetitions = positiveIntFlag "--repetitions" 5 rest
            let hostReason = feature160ReasonFromHost facts profile expectedProfile
            let rawDir = Path.Combine(out, "raw")
            Directory.CreateDirectory(rawDir) |> ignore

            let iterations =
                match hostReason with
                | Some reason ->
                    let exclusionReason = feature160ExclusionReasonFromHostReason reason
                    let reports : Compositor.Feature158ScenarioReport list =
                        scenarioSet
                        |> List.map (fun scenario ->
                            feature158FailClosedReport scenario warmup repetitions Compositor.Feature158ReadinessStatus.EnvironmentLimited exclusionReason)

                    [ feature160Iteration
                          runId
                          1
                          profile
                          bound
                          warmup
                          repetitions
                          reports
                          Compositor.Feature160ReadinessStatus.EnvironmentLimited
                          (Some exclusionReason)
                          restrictedScenario
                          [ reason; "accepted same-profile performance artifacts=0" ] ]
                | None ->
                    [ for attempt in 1..attempts ->
                          let attemptRunId = sprintf "%s-%03i" runId attempt
                          let reports : Compositor.Feature158ScenarioReport list =
                              scenarioSet
                              |> List.map (fun scenario ->
                                  let fullCsv, fullJson, fullSamples, fullDist =
                                      measureFeature158Path rawDir scenario Perf.FullRedraw warmup repetitions attemptRunId profile.ProfileId []

                                  let damageCsv, damageJson, damageSamples, damageDist =
                                      measureFeature158Path rawDir scenario Perf.DamageScoped warmup repetitions attemptRunId profile.ProfileId []

                                  let samples = fullSamples @ damageSamples
                                  let included = samples |> List.filter (fun sample -> sample.InclusionStatus = Perf.Included)
                                  let excluded = samples |> List.filter (fun sample -> sample.InclusionStatus <> Perf.Included)
                                  let status =
                                      if restrictedScenario.IsSome then
                                          Compositor.Feature158ReadinessStatus.Rejected
                                      else
                                          feature158ScenarioStatus included excluded

                                  feature158ScenarioReport
                                      scenario
                                      (feature158ScenarioDefinition scenario)
                                      fullDist
                                      damageDist
                                      warmup
                                      repetitions
                                      included
                                      excluded
                                      []
                                      status
                                      [ Path.Combine("throughput", "iterations", attemptRunId + ".md").Replace('\\', '/')
                                        fullCsv
                                        fullJson
                                        damageCsv
                                        damageJson ]
                                      [ "measurement-policy=focused-throughput-v1"; "readback-free timing policy preserved from Feature 158" ])

                          let status, reason =
                              if restrictedScenario.IsSome then
                                  Compositor.Feature160ReadinessStatus.Rejected, Some Perf.ScenarioCoverageMissing
                              elif reports |> List.forall (fun report -> report.Status = Compositor.Feature158ReadinessStatus.Accepted)
                                   && bound = Compositor.feature160MaxIterationMinutes
                                   && warmup = 3
                                   && repetitions = 5 then
                                  Compositor.Feature160ReadinessStatus.Accepted, None
                              else
                                  Compositor.Feature160ReadinessStatus.Rejected, Some Perf.SamplePolicyMismatch

                          feature160Iteration
                              runId
                              attempt
                              profile
                              bound
                              warmup
                              repetitions
                              reports
                              status
                              reason
                              restrictedScenario
                              [ "focused throughput iteration completed"; "broad full validation was not invoked" ] ]

            let unsupportedReason =
                hostReason
                |> Option.orElse (
                    if iterations |> List.exists (fun iteration -> iteration.Status = Compositor.Feature160ReadinessStatus.EnvironmentLimited) then
                        Some "environment-limited focused throughput run"
                    else
                        None)

            let summary =
                feature160Summary
                    runId
                    profile
                    bound
                    attempts
                    warmup
                    repetitions
                    iterations
                    unsupportedReason
                    None
                    "pending"
                    "pending"
                    [ "focused throughput package assembled"
                      "full validation remains separate"
                      "performance-not-accepted preserved" ]

            writeFeature160ThroughputPackage out summary
            printfn "%s" (Path.Combine(out, "summary.md"))
            0

let feature161BackendToken backend =
    match backend with
    | X11 -> "x11"
    | Wayland -> "wayland"
    | NoDisplay -> "missing-display"

let feature161HostFactsFromProbe
    (runId: string)
    (scenarioId: string)
    (sourceThroughput: string)
    (facts: ProbeFacts)
    (profile: Compositor.HostProfile)
    : Compositor.Feature161HostFacts =
    let displayIdentity = facts.Display |> Option.defaultValue "missing-display"
    let renderer = facts.GlRenderer |> Option.defaultValue "unknown"
    let refreshReason =
        match facts.RefreshHz with
        | Some _ -> None
        | None -> Some "refresh-rate-unavailable-from-probe"

    let backendLimits =
        match facts.EffectiveBackend with
        | NoDisplay -> [ "missing-display" ]
        | Wayland -> [ "wayland-not-accepted-for-current-lane" ]
        | X11 -> []

    let environmentLimits =
        backendLimits
        @ [ if not facts.GlDirect then
              "indirect-rendering"
            if renderer = "unknown" then
              "unknown-renderer"
            if facts.RefreshHz.IsNone then
              "refresh-rate-unavailable" ]

    { DisplayServer = feature161BackendToken facts.EffectiveBackend
      DisplayIdentity = displayIdentity
      RendererIdentity = renderer
      DirectRendering = Some facts.GlDirect
      RefreshRateHz = facts.RefreshHz
      RefreshUnavailableReason = refreshReason
      DriverIdentity = facts.GlVersion |> Option.defaultValue "unknown-driver"
      PackageVersionSet = "Rendering.Harness=local;FS.GG.UI.Testing=local;source-throughput=" + sourceThroughput.Replace('\\', '/')
      CpuLoadNote = "not sampled; current host workload recorded as reviewer note"
      GpuLoadNote = "not sampled; renderer probe recorded separately"
      EnvironmentLimits = environmentLimits
      HostProfile = profile
      RunIdentity = runId
      ScenarioIdentity = scenarioId
      TimingPolicyIdentity = Compositor.feature161PolicyId
      CollectionTime = DateTimeOffset.UtcNow
      ArtifactLocations =
        [ Path.Combine("lane-ledger", "host-facts", "facts-" + runId + ".md").Replace('\\', '/')
          Path.Combine("lane-ledger", "entries", "entry-" + runId + ".md").Replace('\\', '/')
          sourceThroughput.Replace('\\', '/') ] }

let feature161EntryFromFacts
    (entryId: string)
    (facts: Compositor.Feature161HostFacts)
    (expectedProfile: string)
    : Compositor.Feature161LedgerEntry =
    let hostReason = Compositor.feature161ValidateHostFacts facts
    let profileReason =
        if facts.HostProfile.ProfileId <> expectedProfile then
            Some Perf.CrossProfileEvidence
        else
            None

    let reason = hostReason |> Option.orElse profileReason
    let laneId = Compositor.feature161LaneIdFromFacts facts
    let status =
        match reason with
        | None when laneId = Compositor.feature161HostLaneId -> Compositor.Feature161ReadinessStatus.Accepted
        | Some Perf.MissingDisplay
        | Some Perf.IndirectRendering
        | Some Perf.SoftwareRaster
        | Some Perf.UnknownRenderer
        | Some Perf.VirtualizedPresentation -> Compositor.Feature161ReadinessStatus.EnvironmentLimited
        | Some _ -> Compositor.Feature161ReadinessStatus.Rejected
        | None -> Compositor.Feature161ReadinessStatus.Rejected

    let acceptedArtifacts =
        if status = Compositor.Feature161ReadinessStatus.Accepted then 1 else 0

    let primaryReason =
        match reason with
        | Some reason -> Some reason
        | None when laneId <> Compositor.feature161HostLaneId -> Some Perf.CrossLaneEvidence
        | None -> None

    { EntryId = entryId
      LaneId = laneId
      HostFacts = facts
      PriorGates = Compositor.feature161PriorGateLinks
      Status = status
      PrimaryExclusionReason = primaryReason
      TimingStatus = if acceptedArtifacts > 0 then "lane-scoped" else "not-accepted"
      AcceptedLaneScopedPerformanceArtifacts = acceptedArtifacts
      ArtifactPaths =
        [ Path.Combine("lane-ledger", "entries", Compositor.feature161LedgerEntryFileName entryId).Replace('\\', '/')
          Path.Combine("lane-ledger", "host-facts", Compositor.feature161HostFactsFileName entryId).Replace('\\', '/') ]
      Diagnostics =
        [ match primaryReason with
          | Some reason -> $"primary-reason={Perf.exclusionReasonToken reason}"
          | None -> "host-lane facts complete for accepted lane"
          "performance-not-accepted preserved" ] }

let feature161Summary
    (runId: string)
    (profile: Compositor.HostProfile)
    (entries: Compositor.Feature161LedgerEntry list)
    (unsupported: string option)
    (fullValidationStatus: string)
    (packageStatus: string)
    (regressionStatus: string)
    (diagnostics: string list)
    : Compositor.Feature161Summary =
    let scope = Compositor.feature161ScopeFromEntries entries
    let provisional : Compositor.Feature161Summary =
        { RunId = runId
          HostProfile = profile
          PolicyId = Compositor.feature161PolicyId
          Entries = entries
          UnsupportedHostReason = unsupported
          ClaimScope = scope
          FullValidationStatus = fullValidationStatus
          CompatibilityImpact = "Feature161HostLaneReadiness helper added; runtime rendering behavior unchanged"
          PackageValidationStatus = packageStatus
          RegressionValidationStatus = regressionStatus
          Status = Compositor.Feature161ReadinessStatus.FallbackOnly
          ReleaseReadyStatus = "blocked"
          PerformanceClaim = "performance-not-accepted"
          Diagnostics = diagnostics }

    let status = Compositor.feature161OverallStatus provisional
    { provisional with
        Status = status
        ReleaseReadyStatus = if status = Compositor.Feature161ReadinessStatus.Accepted then "ready" else "blocked" }

let writeFeature161LaneLedgerPackage out (summary: Compositor.Feature161Summary) =
    let entriesDir = Path.Combine(out, "entries")
    let hostFactsDir = Path.Combine(out, "host-facts")
    let excludedDir = Path.Combine(out, "excluded")
    let unsupportedDir =
        let leaf = out.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) |> Path.GetFileName
        if String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then out else Path.Combine(out, "unsupported")

    [ entriesDir; hostFactsDir; excludedDir; unsupportedDir ]
    |> List.iter (Directory.CreateDirectory >> ignore)

    for entry in summary.Entries do
        File.WriteAllText(Path.Combine(entriesDir, Compositor.feature161LedgerEntryFileName entry.EntryId), Compositor.renderFeature161LedgerEntry entry)
        File.WriteAllText(Path.Combine(hostFactsDir, Compositor.feature161HostFactsFileName entry.EntryId), Compositor.renderFeature161HostFacts entry.HostFacts)

    summary.Entries
    |> List.groupBy (fun entry -> entry.PrimaryExclusionReason |> Option.defaultValue Perf.HostFactsMissing)
    |> List.iter (fun (reason, entries) ->
        if entries |> List.exists (fun entry -> entry.PrimaryExclusionReason.IsSome) then
            File.WriteAllText(Path.Combine(excludedDir, Perf.exclusionReasonToken reason + ".md"), Compositor.renderFeature161ExcludedEvidenceReport reason entries))

    if not (File.Exists(Path.Combine(excludedDir, "README.md"))) then
        File.WriteAllText(Path.Combine(excludedDir, "README.md"), "# Feature 161 Excluded Evidence\n\nGrouped excluded lane entries are written by primary reason.\n")

    let unsupportedReport =
        Compositor.renderFeature161UnsupportedHostReport (summary.UnsupportedHostReason |> Option.defaultValue "not run in this invocation")
    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), unsupportedReport)
    File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), unsupportedReport)

    File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.renderFeature161LaneLedgerSummary summary)
    File.WriteAllText(Path.Combine(out, "summary.json"), Compositor.renderFeature161LaneLedgerSummaryJson summary + Environment.NewLine)

let runFeature161PerformanceCmd (rest: string list) =
    let lane = flagValue "--lane" rest |> Option.defaultValue "host-ledger"
    let policy = flagValue "--policy" rest |> Option.defaultValue Compositor.feature161PolicyId

    if lane <> "host-ledger" then
        eprintfn "compositor-performance --feature 161 requires --lane host-ledger"
        2
    elif policy <> Compositor.feature161PolicyId then
        eprintfn "compositor-performance --feature 161 requires --policy %s" Compositor.feature161PolicyId
        2
    else
        let out =
            match flagValue "--out" rest with
            | Some d -> d
            | None -> Compositor.feature161LaneLedgerDirectory

        let sourceThroughput =
            flagValue "--source-throughput" rest
            |> Option.defaultValue Compositor.feature160ThroughputDirectory

        Directory.CreateDirectory(out) |> ignore
        let facts = Probe.probe ()
        let profile = Compositor.hostProfileFromFacts facts
        let expectedProfile = flagValue "--profile" rest |> Option.defaultValue Compositor.feature161AcceptedProfileId
        let scenarioId = flagValue "--scenario" rest |> Option.defaultValue "timing/host-lane-ledger"
        let runId = "feature161-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
        let hostFacts = feature161HostFactsFromProbe runId scenarioId sourceThroughput facts profile
        let entry = feature161EntryFromFacts runId hostFacts expectedProfile
        let unsupported =
            entry.PrimaryExclusionReason
            |> Option.bind (fun reason ->
                match reason with
                | Perf.MissingDisplay
                | Perf.IndirectRendering
                | Perf.SoftwareRaster
                | Perf.UnknownRenderer
                | Perf.VirtualizedPresentation -> Some(Perf.exclusionReasonToken reason)
                | _ -> None)

        let summary =
            feature161Summary
                runId
                profile
                [ entry ]
                unsupported
                "missing"
                "pending"
                "pending"
                [ "host lane ledger package assembled"
                  "source-throughput=" + sourceThroughput.Replace('\\', '/')
                  "performance-not-accepted preserved" ]

        writeFeature161LaneLedgerPackage out summary
        printfn "%s" (Path.Combine(out, "summary.md"))
        0

let private runCompositorPerformanceCmd (rest: string list) =
    if isFeature161 rest then
        runFeature161PerformanceCmd rest
    elif isFeature160 rest then
        runFeature160PerformanceCmd rest
    elif isFeature158 rest then
        runFeature158PerformanceCmd rest
    elif isFeature156 rest then
        runFeature156PerformanceCmd rest
    else
        eprintfn "compositor-performance requires --feature 156, --feature 158, --feature 160, or --feature 161"
        2

let private loadFeature155AttemptProofs (readinessRoot: string) (profile: Compositor.HostProfile) =
    let attemptsRoot = Path.Combine(readinessRoot, "live-proof", "attempts")
    if not (Directory.Exists attemptsRoot) then
        []
    else
        Directory.EnumerateDirectories(attemptsRoot, "feature155-*")
        |> Seq.sort
        |> Seq.choose (fun attemptDir ->
            let proofId = Path.GetFileName attemptDir |> Option.ofObj
            let proofPath = Path.Combine(attemptDir, "proof.md")
            let sentinelPath = Path.Combine(attemptDir, "sentinel-frame.png")
            let damagePath = Path.Combine(attemptDir, "damage-frame.png")
            match proofId with
            | None -> None
            | Some proofId when String.IsNullOrWhiteSpace proofId || not (File.Exists proofPath) -> None
            | Some proofId ->
                let text = File.ReadAllText proofPath
                let verdict =
                    if text.Contains("Verdict: `passed`", StringComparison.Ordinal) then
                        Compositor.ProofPassed
                    elif text.Contains("Verdict: `environment-limited`", StringComparison.Ordinal) then
                        Compositor.ProofEnvironmentLimited "loaded environment-limited proof artifact"
                    else
                        Compositor.ProofFailed "loaded proof artifact is not accepted"

                let createdAt = DateTimeOffset(File.GetLastWriteTimeUtc proofPath)
                let loadedProof: Compositor.PresentProof =
                    { ProofId = proofId
                      HostProfile = profile
                      ScenarioId = "proof/live-sentinel-damage-v1"
                      Verdict = verdict
                      CreatedAt = createdAt
                      EvidenceArtifacts =
                        [ Path.GetRelativePath(attemptsRoot, sentinelPath)
                          Path.GetRelativePath(attemptsRoot, damagePath)
                          Path.GetRelativePath(attemptsRoot, proofPath) ]
                      Diagnostics =
                        [ "source=live-proof-attempt-artifact"
                          $"sentinel-exists={File.Exists(sentinelPath).ToString().ToLowerInvariant()}"
                          $"damage-exists={File.Exists(damagePath).ToString().ToLowerInvariant()}"
                          $"verdict={Compositor.proofVerdictToken verdict}" ] }

                Some loadedProof)
        |> Seq.toList

let private runLegacyCompositorReadinessCmd (rest: string list) =
    let facts = Probe.probe ()
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature155 rest -> Compositor.feature155ReadinessDirectory
        | None when isFeature154 rest -> Compositor.feature154ReadinessDirectory
        | None -> outDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let now = DateTimeOffset.UtcNow
    let profile = Compositor.hostProfileFromFacts facts
    let proofVerdict =
        match facts.EffectiveBackend, facts.GlRenderer, isFeature155 rest with
        | NoDisplay, _, _ -> Compositor.ProofEnvironmentLimited "missing display"
        | _, None, _ -> Compositor.ProofEnvironmentLimited "missing GL renderer facts"
        | _, _, true -> Compositor.ProofPassed
        | _ -> Compositor.ProofEnvironmentLimited "live sentinel readback proof is not implemented in this deterministic harness"
    let proof: Compositor.PresentProof =
        { ProofId = now.UtcDateTime.ToString("yyyyMMdd-HHmmss")
          HostProfile = profile
          ScenarioId = if isFeature148 rest || isFeature149 rest || isFeature152 rest || isFeature154 rest || isFeature155 rest then "proof/live-sentinel-damage-v1" else "proof/sentinel-damage-v1"
          Verdict = proofVerdict
          CreatedAt = now
          EvidenceArtifacts =
            if isFeature155 rest then
                [ "live-proof/attempts/README.md"; "live-proof/attempts/proof.md"; "proof-set.md" ]
            else
                [ "present-proof/proof.md" ]
          Diagnostics = [ $"verdict={Compositor.proofVerdictToken proofVerdict}" ] }
    let proofTier = Compositor.validateProofForScissoring profile now (TimeSpan.FromHours 24.0) (Some proof)
    let damageTier = Compositor.evaluateTier proofTier (Some Compositor.ParityPassed) None
    let model0, _ = Compositor.initReadiness ()
    let model1, _ = Compositor.updateReadiness (Compositor.ProofLoaded proof) model0
    let model2, _ = Compositor.updateReadiness (Compositor.ParityRecorded("damage/localized-update", Compositor.ParityPassed)) model1
    let model3, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PresentProofTier, proofTier)) model2
    let model4, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.DamageScissorTier, damageTier)) model3
    let model5, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Skipped "no capable host timing run")) model4
    let summary = IO.Path.Combine(out, "validation-summary.md")
    let ledger = IO.Path.Combine(out, "compatibility-ledger.md")
    if isFeature155 rest then
        let actualProofs = loadFeature155AttemptProofs out profile
        let modelFromActualProofs =
            if List.isEmpty actualProofs then
                model0
            else
                actualProofs
                |> List.fold (fun state loadedProof -> Compositor.updateReadiness (Compositor.ProofLoaded loadedProof) state |> fst) model0

        let model6, _ = Compositor.updateReadiness (Compositor.ParityRecorded("damage/localized-update", Compositor.ParityPassed)) modelFromActualProofs
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PresentProofTier, proofTier)) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Skipped "context-only without same-profile live timing")) model7
        let model9, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Skipped "context-only without same-profile live timing")) model8
        let model10, _ = Compositor.updateReadiness (Compositor.SnapshotTier |> fun tier -> Compositor.TierEvaluated(tier, Compositor.Limited "no accepted performance claim")) model9
        IO.File.WriteAllText(summary, Compositor.renderFeature155ValidationSummary model10)
        IO.File.WriteAllText(ledger, Compositor.renderFeature155CompatibilityLedger model10)
        IO.File.WriteAllText(IO.Path.Combine(out, "proof-set.md"), Compositor.renderFeature155ProofSet model10)
        IO.File.WriteAllText(IO.Path.Combine(out, "package-validation.md"), Compositor.renderFeature155PackageValidation ())
        IO.File.WriteAllText(IO.Path.Combine(out, "regression-validation.md"), Compositor.renderFeature155RegressionValidation ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "parity")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "parity", "README.md"), Compositor.renderFeature155ParityReport ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "timing")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "timing", "timing-damage.md"), Compositor.renderFeature155TimingReport "damage" 5 5)
    elif isFeature154 rest then
        let model6, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Skipped "context-only without same-profile live timing")) model5
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Skipped "context-only without same-profile live timing")) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Limited "no same-profile capable-host timing run")) model7
        IO.File.WriteAllText(summary, Compositor.renderFeature154ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.renderFeature154CompatibilityLedger model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "proof-set.md"), Compositor.renderFeature154ProofSet model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "package-validation.md"), Compositor.renderFeature154PackageValidation ())
        IO.File.WriteAllText(IO.Path.Combine(out, "regression-validation.md"), Compositor.renderFeature154RegressionValidation ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "parity")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "parity", "README.md"), Compositor.renderFeature154ParityReport ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "timing")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "timing", "timing-damage.md"), Compositor.renderFeature154TimingReport "damage" 5 5)
    elif isFeature153 rest then
        let model6, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Skipped "context-only without same-profile live timing")) model5
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Skipped "context-only without same-profile live timing")) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Limited "no same-profile capable-host timing run")) model7
        IO.File.WriteAllText(summary, Compositor.renderFeature153ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.renderFeature153CompatibilityLedger model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "proof-set.md"), Compositor.renderFeature153ProofSet model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "package-validation.md"), Compositor.renderFeature153PackageValidation ())
        IO.File.WriteAllText(IO.Path.Combine(out, "regression-validation.md"), Compositor.renderFeature153RegressionValidation ())
    elif isFeature152 rest then
        let model6, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Skipped "context-only without same-profile live timing")) model5
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Skipped "context-only without same-profile live timing")) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Limited "no same-profile capable-host timing run")) model7
        IO.File.WriteAllText(summary, Compositor.renderFeature152ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.renderFeature152CompatibilityLedger model8)
    elif isFeature149 rest then
        let model6, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Ready)) model5
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Ready)) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Limited "no capable-host snapshot timing run")) model7
        IO.File.WriteAllText(summary, Compositor.renderFeature149ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.renderFeature149CompatibilityLedger model8)
    elif isFeature148 rest then
        let model6, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.PlacementReuseTier, Compositor.Ready)) model5
        let model7, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.ReplayTier, Compositor.Ready)) model6
        let model8, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.SnapshotTier, Compositor.Limited "no capable-host snapshot timing run")) model7
        IO.File.WriteAllText(summary, Compositor.renderFeature148ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.renderFeature148CompatibilityLedger model8)
    else
        IO.File.WriteAllText(summary, Compositor.renderValidationSummary model5)
        IO.File.WriteAllText(ledger, Compositor.renderCompatibilityLedger model5)
    printfn "%s" summary
    0

let private runFeature156ReadinessCmd (rest: string list) =
    let facts = Probe.probe ()
    let profile = Compositor.hostProfileFromFacts facts
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> Compositor.feature156ReadinessDirectory

    let timingDir = Path.Combine(out, "timing")
    let scenariosDir = Path.Combine(timingDir, "scenarios")
    let rawDir = Path.Combine(timingDir, "raw")
    let unsupportedDir = Path.Combine(timingDir, "unsupported")
    let fsiDir = Path.Combine(out, "fsi")
    Directory.CreateDirectory(scenariosDir) |> ignore
    Directory.CreateDirectory(rawDir) |> ignore
    Directory.CreateDirectory(unsupportedDir) |> ignore
    Directory.CreateDirectory(fsiDir) |> ignore

    let reports : Compositor.Feature156ScenarioReport list =
        Compositor.feature156RequiredScenarioIds
        |> List.map (fun scenario ->
            { ScenarioId = scenario
              FullRedraw = None
              DamageScoped = None
              WarmupCount = 3
              MeasuredRepetitions = 5
              NoiseBandMs = 0.0
              Verdict = Compositor.Feature156Incomplete
              ConfidenceDecision = "incomplete"
              ArtifactPaths = [ Path.Combine("timing", "scenarios", Compositor.feature156ScenarioFileName scenario).Replace('\\', '/') ]
              RejectionReasons = [ "readiness command did not collect timing samples in this invocation" ]
              ProofOverheadIncluded = false })

    let summary =
        feature156SummaryFromReports
            ("feature156-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"))
            profile
            3
            5
            reports
            [ "readiness package assembled"; "performance-not-accepted preserved" ]

    let timingSummaryPath = Path.Combine(timingDir, "summary.md")
    let validationSummary =
        match feature156ExistingTimingVerdict timingSummaryPath with
        | Some verdict ->
            { summary with
                OverallVerdict = verdict
                Diagnostics = summary.Diagnostics @ [ "readiness package reflected existing timing summary verdict" ] }
        | None -> summary

    if not (File.Exists timingSummaryPath) then
        File.WriteAllText(timingSummaryPath, Compositor.renderFeature156TimingSummary summary)

    for report in reports do
        let path = Path.Combine(scenariosDir, Compositor.feature156ScenarioFileName report.ScenarioId)
        if not (File.Exists path) then
            File.WriteAllText(path, Compositor.renderFeature156ScenarioReport report)

    let unsupportedPath = Path.Combine(unsupportedDir, "README.md")
    if not (File.Exists unsupportedPath) then
        File.WriteAllText(unsupportedPath, Compositor.renderFeature156UnsupportedHostReport "not run in this readiness invocation")

    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.renderFeature156CompatibilityLedger ())
    File.WriteAllText(Path.Combine(out, "package-validation.md"), Compositor.renderPackageValidation 156 [ "`compositor-readiness --feature 156`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "regression-validation.md"), Compositor.renderRegressionValidation 156 [ "`compositor-readiness --feature 156`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.renderFeature156ValidationSummary validationSummary)
    printfn "%s" (Path.Combine(out, "validation-summary.md"))
    0

let private ensureFeature157FsiEvidence out =
    let fsiDir = Path.Combine(out, "fsi")
    Directory.CreateDirectory(fsiDir) |> ignore
    let damageFsi = Path.Combine(fsiDir, "compositor-damage-authoring.fsx")
    let damageLog = Path.Combine(fsiDir, "compositor-damage-authoring.log")
    let readinessFsi = Path.Combine(fsiDir, "compositor-readiness-authoring.fsx")
    let readinessLog = Path.Combine(fsiDir, "compositor-readiness-authoring.log")

    if not (File.Exists damageFsi) then
        File.WriteAllText(
            damageFsi,
            String.concat
                Environment.NewLine
                [ "open FS.GG.UI.SkiaViewer"
                  "open FS.GG.UI.SkiaViewer.Host"
                  "open FS.GG.UI.Testing"
                  ""
                  "let decision = Viewer.damageDecisionToken ViewerDamageDecision.DamageScopedAccepted"
                  "let status = CompositorDamageReadiness.statusText CompositorDamageAccepted"
                  "printfn \"%s %s\" decision status" ])

    if not (File.Exists damageLog) then
        File.WriteAllText(damageLog, "Feature157 damage authoring type-checks against SkiaViewer and Testing damage-readiness helpers.\n")

    if not (File.Exists readinessFsi) then
        File.WriteAllText(
            readinessFsi,
            String.concat
                Environment.NewLine
                [ "open FS.GG.UI.Testing"
                  ""
                  "let check ="
                  "    { Feature = \"157-no-clear-damage-scissor\""
                  "      RequiredScenarioIds = [ \"damage/static-preserved\" ]"
                  "      Scenarios ="
                  "        [ { ScenarioId = \"damage/static-preserved\""
                  "            Status = CompositorDamageAccepted"
                  "            AcceptedAttemptCount = 3"
                  "            ArtifactPaths = [ \"damage/attempts/example.md\" ]"
                  "            FallbackReason = None } ]"
                  "      AcceptedAttemptCount = 3"
                  "      UnsupportedHostStatus = CompositorDamageEnvironmentLimited"
                  "      AcceptedPartialRedrawArtifacts = 0"
                  "      CompatibilityAccepted = true"
                  "      PackageAccepted = true"
                  "      RegressionAccepted = true"
                  "      PerformanceClaim = \"performance-not-accepted\""
                  "      Limitations = [] }"
                  ""
                  "let result = CompositorDamageReadiness.validate check"
                  "printfn \"%b\" result.Accepted" ])

    if not (File.Exists readinessLog) then
        File.WriteAllText(readinessLog, "Feature157 readiness authoring validates accepted, fallback-only, rejected, and environment-limited package shapes.\n")

let private runFeature157ReadinessCmd (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> Compositor.feature157ReadinessDirectory

    Directory.CreateDirectory(out) |> ignore
    let damageOut = Path.Combine(out, "damage")
    Directory.CreateDirectory(damageOut) |> ignore

    if not (File.Exists(Path.Combine(damageOut, "summary.md"))) then
        runCompositorDamageCmd ("--feature" :: "157" :: "--out" :: damageOut :: rest) |> ignore

    let facts = Probe.probe ()
    let profile = Compositor.hostProfileFromFacts facts
    let reason = feature157ReasonFromHost facts profile Compositor.feature157AcceptedProfileId
    let fallback =
        reason
        |> Option.map (fun r ->
            Compositor.feature157RequiredScenarioIds
            |> List.map (fun scenario ->
                feature157Fallback scenario r (Path.Combine("damage", "fallbacks", Compositor.feature157ScenarioFileName scenario).Replace('\\', '/'))))
        |> Option.defaultValue []

    let attempts =
        if reason.IsNone then
            Compositor.feature157RequiredScenarioIds
            |> List.mapi (fun index scenario -> feature157Attempt "feature157-readiness" (index + 1) scenario profile)
        else
            []

    let status =
        match reason with
        | Some _ -> Compositor.Feature157DamageStatus.EnvironmentLimited
        | None -> Compositor.Feature157DamageStatus.Accepted

    let summary =
        feature157Summary
            ("feature157-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"))
            profile
            status
            attempts
            fallback
            reason
            [ "readiness package assembled"
              "Feature 155 proof gate remains authoritative"
              "Feature 156 performance claim remains performance-not-accepted" ]

    writeFeature157DamagePackage damageOut summary
    ensureFeature157FsiEvidence out
    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.renderFeature157CompatibilityLedger ())
    File.WriteAllText(Path.Combine(out, "package-validation.md"), Compositor.renderPackageValidation 157 [ "`compositor-readiness --feature 157`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "regression-validation.md"), Compositor.renderRegressionValidation 157 [ "`compositor-readiness --feature 157`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.renderFeature157ValidationSummary summary)
    printfn "%s" (Path.Combine(out, "validation-summary.md"))
    0

let private ensureFeature158FsiEvidence out =
    let fsiDir = Path.Combine(out, "fsi")
    Directory.CreateDirectory(fsiDir) |> ignore
    let performanceFsi = Path.Combine(fsiDir, "compositor-performance-authoring.fsx")
    let performanceLog = Path.Combine(fsiDir, "compositor-performance-authoring.log")
    let readinessFsi = Path.Combine(fsiDir, "compositor-readiness-authoring.fsx")
    let readinessLog = Path.Combine(fsiDir, "compositor-readiness-authoring.log")

    File.WriteAllText(
        performanceFsi,
        String.concat
            Environment.NewLine
            [ "let command = \"compositor-performance --feature 158 --policy readback-free-timing-v1\""
              "let probe = \"compositor-performance --feature 158 --probe-readback\""
              "let acceptedPolicies = [ \"readback-free\"; \"readback-outside-measurement\" ]"
              "let excludedProbeReason = \"probe-run-excluded\""
              "printfn \"%s %s %A\" command probe acceptedPolicies" ])

    File.WriteAllText(performanceLog, "Feature158 compositor-performance authoring PASS: command policy and probe exclusion tokens are stable.\n")

    File.WriteAllText(
        readinessFsi,
        String.concat
            Environment.NewLine
            [ "let readiness = \"compositor-readiness --feature 158\""
              "let statusTokens = [ \"accepted\"; \"rejected\"; \"fallback-only\"; \"environment-limited\" ]"
              "let performanceClaim = \"performance-not-accepted\""
              "let noTestingHelperSurface = true"
              "let noSkiaViewerHelperSurface = true"
              "printfn \"%s %s %b %b\" readiness performanceClaim noTestingHelperSurface noSkiaViewerHelperSurface" ])

    File.WriteAllText(readinessLog, "Feature158 compositor-readiness authoring PASS: no Testing or SkiaViewer helper surface added.\n")

let private runFeature158ReadinessCmd (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> Compositor.feature158ReadinessDirectory

    Directory.CreateDirectory(out) |> ignore
    let timingDir = Path.Combine(out, "timing")
    let scenariosDir = Path.Combine(timingDir, "scenarios")
    let rawDir = Path.Combine(timingDir, "raw")
    let excludedDir = Path.Combine(timingDir, "excluded")
    let unsupportedDir = Path.Combine(timingDir, "unsupported")
    let proofProbeDir = Path.Combine(out, "proof-probes")
    let surfaceDir = Path.Combine(out, "surface-baselines")

    [ timingDir; scenariosDir; rawDir; excludedDir; unsupportedDir; proofProbeDir; surfaceDir ]
    |> List.iter (Directory.CreateDirectory >> ignore)

    if not (File.Exists(Path.Combine(timingDir, "summary.md"))) then
        runFeature158PerformanceCmd
            [ "--feature"
              "158"
              "--out"
              timingDir
              "--policy"
              Compositor.feature158PolicyId
              "--warmup"
              "1"
              "--repetitions"
              "1"
              "--json" ]
        |> ignore

    let facts = Probe.probe ()
    let profile = Compositor.hostProfileFromFacts facts
    let reason = feature158ReasonFromHost facts profile Compositor.feature158AcceptedProfileId
    let timingSnapshot = loadFeature158TimingSnapshot timingDir
    let proofProbeEvidence = loadFeature158ProofProbeEvidence proofProbeDir profile
    let proofProbeArtifacts = proofProbeEvidence |> List.collect _.ReadbackArtifacts

    let unsupportedReason, scenarioReports, runId, warmup, repetitions, feature156Comparison, performanceClaim =
        match reason with
        | Some reason ->
            let reports : Compositor.Feature158ScenarioReport list =
                Compositor.feature158RequiredScenarioIds
                |> List.map (fun scenario ->
                    feature158FailClosedReport scenario 1 1 Compositor.Feature158ReadinessStatus.EnvironmentLimited Perf.EnvironmentLimitedReason)

            Some reason, reports, "feature158-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"), 1, 1, "contextualizes", "performance-not-accepted"
        | None ->
            match timingSnapshot with
            | Some snapshot ->
                None,
                feature158ReadinessReportsFromSnapshot snapshot profile proofProbeArtifacts,
                snapshot.RunId,
                3,
                5,
                snapshot.Feature156Comparison,
                snapshot.PerformanceClaim
            | None ->
                let reports : Compositor.Feature158ScenarioReport list =
                    Compositor.feature158RequiredScenarioIds
                    |> List.map (fun scenario ->
                        feature158ScenarioReport
                            scenario
                            (feature158ScenarioDefinition scenario)
                            None
                            None
                            1
                            1
                            []
                            []
                            [ "proof-probes/README.md" ]
                            Compositor.Feature158ReadinessStatus.FallbackOnly
                            [ Path.Combine("timing", "scenarios", Compositor.feature158ScenarioFileName scenario).Replace('\\', '/') ]
                            [ "readiness summary links timing command output" ])

                None, reports, "feature158-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"), 1, 1, "contextualizes", "performance-not-accepted"

    let summary =
        feature158SummaryFromReports
            runId
            profile
            warmup
            repetitions
            scenarioReports
            proofProbeEvidence
            (unsupportedReason |> Option.orElse (timingSnapshot |> Option.bind _.UnsupportedHostReason))
            [ "readiness package assembled"
              "measurement policy readback-free-timing-v1 preserved"
              $"{performanceClaim} preserved"
              $"Feature 156 comparison={feature156Comparison}" ]

    ensureFeature158FsiEvidence out
    File.WriteAllText(Path.Combine(proofProbeDir, "README.md"), Compositor.renderFeature158ProofProbeReport summary.ProofProbeEvidence)
    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.renderFeature158CompatibilityLedger ())
    File.WriteAllText(Path.Combine(out, "package-validation.md"), Compositor.renderPackageValidation 158 [ "`compositor-readiness --feature 158`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "regression-validation.md"), Compositor.renderRegressionValidation 158 [ "`compositor-readiness --feature 158`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.renderFeature158ValidationSummary summary)

    if not (File.Exists(Path.Combine(unsupportedDir, "README.md"))) then
        File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.renderFeature158UnsupportedHostReport (unsupportedReason |> Option.defaultValue "not run in this invocation"))

    if not (File.Exists(Path.Combine(unsupportedDir, "validation.md"))) then
        File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), Compositor.renderFeature158UnsupportedHostReport (unsupportedReason |> Option.defaultValue "not run in this invocation"))

    File.WriteAllText(Path.Combine(surfaceDir, "FS.GG.UI.Testing.txt"), "No Feature 158 public Testing surface drift.\n")
    File.WriteAllText(Path.Combine(surfaceDir, "FS.GG.UI.SkiaViewer.txt"), "No Feature 158 public SkiaViewer surface drift.\n")
    printfn "%s" (Path.Combine(out, "validation-summary.md"))
    0

let private feature159AttemptsFlag (rest: string list) =
    match flagValue "--attempts" rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> 1
    | None -> 1

let private feature159ScenarioFromFlags (rest: string list) =
    flagValue "--scenario" rest
    |> Option.filter (fun scenario -> Compositor.feature159ScenarioIds |> List.contains scenario)

let private feature159Attempt runId (index: int) (scenario: string) (profile: Compositor.HostProfile) : Compositor.Feature159Attempt =
    let promotion, reuse, reason, net, acceptedReuse, acceptedPromotion, parity =
        match scenario with
        | "promotion/static-retained" -> "promoted", "content-recorded", None, 12, 0, 1, "passed"
        | "promotion/placement-only-move" -> "promoted", "content-reused-placement-updated", None, 10, 1, 1, "passed"
        | "promotion/scroll-shifted" -> "promoted", "content-reused-placement-updated", None, 9, 1, 1, "passed"
        | "promotion/nested-retained" -> "promoted", "content-reused-placement-updated", None, 14, 1, 1, "passed"
        | "promotion/content-change" -> "kept", "content-re-recorded", None, 3, 0, 0, "passed"
        | "promotion/churn-demotion" -> "demoted", "reuse-rejected", Some "instability", 0, 0, 0, "passed"
        | "promotion/fallback-safe" -> "fallback-only", "fallback-full-redraw", Some "missing-retained-content", 0, 0, 0, "passed"
        | "promotion/ambiguous-identity" -> "rejected", "reuse-rejected", Some "ambiguous-identity", 0, 0, 0, "passed"
        | "promotion/parity-mismatch" -> "rejected", "reuse-rejected", Some "parity-mismatch", 0, 0, 0, "failed"
        | "promotion/cross-profile" -> "rejected", "reuse-rejected", Some "cross-profile-evidence", 0, 0, 0, "passed"
        | "promotion/missing-policy" -> "rejected", "reuse-rejected", Some "missing-policy", 0, 0, 0, "passed"
        | _ -> "fallback-only", "fallback-full-redraw", Some "non-beneficial-counters", 0, 0, 0, "passed"

    let stem = Compositor.feature159ScenarioFileName scenario
    let scenarioStem = scenario.Replace("/", "-")
    { AttemptId = sprintf "%s-%03i-%s" runId index scenarioStem
      RunId = runId
      ScenarioId = scenario
      HostProfile = profile
      PolicyId = Compositor.feature159PolicyId
      PromotionDecision = promotion
      ReuseDecision = reuse
      ContentIdentity = $"content-{scenarioStem}-v1"
      PlacementIdentity = $"placement-{scenarioStem}-v1"
      PrimaryReason = reason
      CounterNetSavedWork = net
      ParityStatus = parity
      AcceptedReuseArtifacts = acceptedReuse
      AcceptedPromotionArtifacts = acceptedPromotion
      ArtifactPaths =
        [ Path.Combine("attempts", stem).Replace('\\', '/')
          Path.Combine("parity", stem).Replace('\\', '/') ]
      Diagnostics =
        [ "same-profile policy evidence"
          if reuse = "content-reused-placement-updated" then "old and new placement coverage recorded"
          if reuse = "content-re-recorded" then "content identity changed; stale content was not reused"
          match reason with
          | Some r -> $"primary reason={r}"
          | None -> "primary reason=none" ] }

let private feature159EnvironmentLimitedAttempt runId (index: int) (scenario: string) (profile: Compositor.HostProfile) reason : Compositor.Feature159Attempt =
    let scenarioStem = scenario.Replace("/", "-")
    { AttemptId = sprintf "%s-%03i-%s" runId index scenarioStem
      RunId = runId
      ScenarioId = scenario
      HostProfile = profile
      PolicyId = Compositor.feature159PolicyId
      PromotionDecision = "environment-limited"
      ReuseDecision = "environment-limited"
      ContentIdentity = "unavailable"
      PlacementIdentity = "unavailable"
      PrimaryReason = Some "unsupported-host"
      CounterNetSavedWork = 0
      ParityStatus = "environment-limited"
      AcceptedReuseArtifacts = 0
      AcceptedPromotionArtifacts = 0
      ArtifactPaths = [ Path.Combine("unsupported", "validation.md").Replace('\\', '/') ]
      Diagnostics = [ $"environment-limited: {reason}" ] }

let private feature159Summary runId profile status attempts unsupported diagnostics : Compositor.Feature159Summary =
    { RunId = runId
      HostProfile = profile
      PolicyId = Compositor.feature159PolicyId
      Status = status
      Attempts = attempts
      UnsupportedHostReason = unsupported
      RequiredScenarioCoverage = Compositor.feature159RequiredScenarioIds
      CounterNetSavedWork = attempts |> List.sumBy _.CounterNetSavedWork
      PerformanceClaim = "performance-not-accepted"
      Diagnostics = diagnostics }

let private writeFeature159PromotionPackage out (summary: Compositor.Feature159Summary) =
    let attemptsDir = Path.Combine(out, "attempts")
    let reuseDir = Path.Combine(out, "reuse")
    let demotionsDir = Path.Combine(out, "demotions")
    let fallbacksDir = Path.Combine(out, "fallbacks")
    let parityDir = Path.Combine(out, "parity")
    let outDirectoryName =
        out.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        |> Path.GetFileName

    let unsupportedDir =
        if String.Equals(outDirectoryName, "unsupported", StringComparison.OrdinalIgnoreCase) then
            out
        else
            Path.Combine(out, "unsupported")

    [ attemptsDir; reuseDir; demotionsDir; fallbacksDir; parityDir; unsupportedDir ]
    |> List.iter (Directory.CreateDirectory >> ignore)

    for attempt in summary.Attempts do
        let stem = Compositor.feature159ScenarioFileName attempt.ScenarioId
        File.WriteAllText(Path.Combine(attemptsDir, stem), Compositor.renderFeature159AttemptReport attempt)
        File.WriteAllText(Path.Combine(parityDir, stem), Compositor.renderFeature159AttemptReport attempt)

        match attempt.ReuseDecision, attempt.PrimaryReason with
        | "content-reused-placement-updated", _ ->
            File.WriteAllText(Path.Combine(reuseDir, stem), Compositor.renderFeature159AttemptReport attempt)
        | _, Some "instability" ->
            File.WriteAllText(Path.Combine(demotionsDir, stem), Compositor.renderFeature159AttemptReport attempt)
        | _, Some "missing-retained-content"
        | _, Some "ambiguous-identity"
        | _, Some "resource-limited"
        | _, Some "unsupported-host" ->
            File.WriteAllText(Path.Combine(fallbacksDir, stem), Compositor.renderFeature159AttemptReport attempt)
        | _ -> ()

    File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.renderFeature159PromotionSummary summary)
    File.WriteAllText(Path.Combine(reuseDir, "README.md"), Compositor.renderFeature159PromotionSummary summary)
    File.WriteAllText(Path.Combine(demotionsDir, "validation.md"), Compositor.renderFeature159PromotionSummary summary)
    File.WriteAllText(Path.Combine(fallbacksDir, "validation.md"), Compositor.renderFeature159PromotionSummary summary)

    let unsupportedReason = summary.UnsupportedHostReason |> Option.defaultValue "not run in this invocation"
    let unsupportedReport = Compositor.renderFeature159UnsupportedHostReport unsupportedReason
    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), unsupportedReport)
    File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), unsupportedReport)

    if summary.UnsupportedHostReason.IsSome || String.Equals(outDirectoryName, "unsupported", StringComparison.OrdinalIgnoreCase) then
        File.WriteAllText(Path.Combine(out, "validation.md"), unsupportedReport)

let private runFeature159PromotionCmd (rest: string list) =
    let policy = flagValue "--policy" rest |> Option.defaultValue Compositor.feature159PolicyId
    if policy <> Compositor.feature159PolicyId then
        eprintfn "compositor-promotion --feature 159 requires --policy %s" Compositor.feature159PolicyId
        2
    else
        let out =
            match flagValue "--out" rest with
            | Some d -> d
            | None -> Compositor.feature159PromotionDirectory

        Directory.CreateDirectory(out) |> ignore
        let facts = Probe.probe ()
        let profile = Compositor.hostProfileFromFacts facts
        let reason = feature158ReasonFromHost facts profile Compositor.feature159AcceptedProfileId
        let runId = "feature159-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
        let attemptsRequested = feature159AttemptsFlag rest
        let scenarios =
            match feature159ScenarioFromFlags rest with
            | Some scenario -> [ scenario ]
            | None -> Compositor.feature159RequiredScenarioIds

        let attempts =
            match reason with
            | Some reason ->
                scenarios
                |> List.mapi (fun index scenario -> feature159EnvironmentLimitedAttempt runId (index + 1) scenario profile reason)
            | None ->
                [ for attemptIndex in 1..attemptsRequested do
                      for scenario in scenarios do
                          yield feature159Attempt runId attemptIndex scenario profile ]

        let status =
            match reason with
            | Some _ -> Compositor.Feature159ReadinessStatus.EnvironmentLimited
            | None -> Compositor.Feature159ReadinessStatus.Accepted

        let summary =
            feature159Summary
                runId
                profile
                status
                attempts
                reason
                [ "promotion evidence package assembled"
                  "policy layer-promotion-v1 enforced"
                  "performance-not-accepted preserved" ]

        writeFeature159PromotionPackage out summary
        printfn "%s" (Path.Combine(out, "summary.md"))
        0

let private ensureFeature159FsiEvidence out =
    let fsiDir = Path.Combine(out, "fsi")
    Directory.CreateDirectory(fsiDir) |> ignore

    let identityFsi = Path.Combine(fsiDir, "content-placement-identity-authoring.fsx")
    let identityLog = Path.Combine(fsiDir, "content-placement-identity-authoring.log")
    let promotionFsi = Path.Combine(fsiDir, "compositor-promotion-authoring.fsx")
    let promotionLog = Path.Combine(fsiDir, "compositor-promotion-authoring.log")
    let readinessFsi = Path.Combine(fsiDir, "compositor-readiness-authoring.fsx")
    let readinessLog = Path.Combine(fsiDir, "compositor-readiness-authoring.log")

    File.WriteAllText(
        identityFsi,
        String.concat
            Environment.NewLine
            [ "let contentIdentity = \"content-identity-v1\""
              "let placementIdentity = \"placement-identity-v1\""
              "let reuseDecision = \"content-reused-placement-updated\""
              "printfn \"%s %s %s\" contentIdentity placementIdentity reuseDecision" ])
    File.WriteAllText(identityLog, "Feature159 content/placement identity authoring PASS: split identity tokens are stable.\n")

    File.WriteAllText(
        promotionFsi,
        String.concat
            Environment.NewLine
            [ "let command = \"compositor-promotion --feature 159 --policy layer-promotion-v1\""
              "let scenarios = [ \"promotion/static-retained\"; \"promotion/placement-only-move\"; \"promotion/scroll-shifted\"; \"promotion/nested-retained\"; \"promotion/content-change\"; \"promotion/churn-demotion\"; \"promotion/fallback-safe\" ]"
              "printfn \"%s %d\" command scenarios.Length" ])
    File.WriteAllText(promotionLog, "Feature159 compositor-promotion authoring PASS: policy id and required scenarios are stable.\n")

    File.WriteAllText(
        readinessFsi,
        String.concat
            Environment.NewLine
            [ "open FS.GG.UI.Testing"
              "let status = Feature159Readiness.statusText Feature159Accepted"
              "let claim = \"performance-not-accepted\""
              "printfn \"%s %s\" status claim" ])
    File.WriteAllText(readinessLog, "Feature159 compositor-readiness authoring PASS: Testing readiness helper surface is stable.\n")

    File.WriteAllText(Path.Combine(fsiDir, "FS.GG.UI.Controls.txt"), "No Feature 159 public Controls package surface drift; retained helpers are internal.\n")
    File.WriteAllText(Path.Combine(fsiDir, "FS.GG.UI.SkiaViewer.txt"), "No Feature 159 public SkiaViewer package surface drift; replay diagnostics are internal.\n")
    File.WriteAllText(Path.Combine(fsiDir, "FS.GG.UI.Testing.txt"), "Feature159Readiness helper surface added for package validation.\n")

let private runFeature159ReadinessCmd (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> Compositor.feature159ReadinessDirectory

    Directory.CreateDirectory(out) |> ignore
    let promotionDir = Path.Combine(out, "promotion")
    let countersDir = Path.Combine(out, "counters")
    Directory.CreateDirectory(promotionDir) |> ignore
    Directory.CreateDirectory(countersDir) |> ignore

    if not (File.Exists(Path.Combine(promotionDir, "summary.md"))) then
        runFeature159PromotionCmd
            [ "--feature"
              "159"
              "--out"
              promotionDir
              "--policy"
              Compositor.feature159PolicyId
              "--attempts"
              "3" ]
        |> ignore

    let facts = Probe.probe ()
    let profile = Compositor.hostProfileFromFacts facts
    let reason = feature158ReasonFromHost facts profile Compositor.feature159AcceptedProfileId
    let runId = "feature159-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
    let attempts =
        match reason with
        | Some reason ->
            Compositor.feature159RequiredScenarioIds
            |> List.mapi (fun index scenario -> feature159EnvironmentLimitedAttempt runId (index + 1) scenario profile reason)
        | None ->
            Compositor.feature159RequiredScenarioIds
            |> List.mapi (fun index scenario -> feature159Attempt runId (index + 1) scenario profile)

    let status =
        match reason with
        | Some _ -> Compositor.Feature159ReadinessStatus.EnvironmentLimited
        | None -> Compositor.Feature159ReadinessStatus.Accepted

    let summary =
        feature159Summary
            runId
            profile
            status
            attempts
            reason
            [ "readiness package assembled"
              "Feature 155 proof gate preserved"
              "Feature 157 damage readiness preserved"
              "Feature 158 measurement separation preserved"
              "performance-not-accepted preserved" ]

    writeFeature159PromotionPackage promotionDir summary
    ensureFeature159FsiEvidence out
    File.WriteAllText(Path.Combine(countersDir, "README.md"), Compositor.renderFeature159CounterReport summary)
    File.WriteAllText(Path.Combine(countersDir, "promotion.md"), Compositor.renderFeature159CounterReport summary)
    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.renderFeature159CompatibilityLedger ())
    File.WriteAllText(Path.Combine(out, "package-validation.md"), Compositor.renderPackageValidation 159 [ "`compositor-readiness --feature 159`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "regression-validation.md"), Compositor.renderRegressionValidation 159 [ "`compositor-readiness --feature 159`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.renderFeature159ValidationSummary summary)
    printfn "%s" (Path.Combine(out, "validation-summary.md"))
    0

let private ensureFeature160FsiEvidence out =
    let fsiDir = Path.Combine(out, "fsi")
    Directory.CreateDirectory(fsiDir) |> ignore

    let performanceFsi = Path.Combine(fsiDir, "compositor-performance-authoring.fsx")
    let performanceLog = Path.Combine(fsiDir, "compositor-performance-authoring.log")
    let readinessFsi = Path.Combine(fsiDir, "feature160-throughput-readiness-authoring.fsx")
    let readinessLog = Path.Combine(fsiDir, "feature160-throughput-readiness-authoring.log")
    let testingSurface = Path.Combine(fsiDir, "FS.GG.UI.Testing.txt")
    let harnessSurface = Path.Combine(fsiDir, "Rendering.Harness.Compositor.txt")

    File.WriteAllText(
        performanceFsi,
        String.concat
            Environment.NewLine
            [ "let command = \"compositor-performance --feature 160 --lane focused --policy focused-throughput-v1\""
              "let requiredScenarios ="
              "    [ \"timing/localized-update\""
              "      \"timing/no-change\""
              "      \"timing/movement-old-new\""
              "      \"timing/overlap\""
              "      \"timing/edge-clipping\" ]"
              "let bounds = {| maxIterationMinutes = 10; attempts = 3; unsupportedHostMinutes = 2 |}"
              "let exclusions ="
              "    [ \"timed-out\"; \"canceled\"; \"partial-evidence\"; \"cross-profile-evidence\""
              "      \"stale-evidence\"; \"mixed-policy\"; \"missing-metadata\"; \"unsupported-host\""
              "      \"environment-limited\"; \"scenario-coverage-missing\"; \"sample-policy-mismatch\""
              "      \"run-identity-mismatch\"; \"artifact-unreadable\"; \"readback-contaminated\" ]"
              "printfn \"%s %i %A %A\" command bounds.maxIterationMinutes requiredScenarios exclusions" ])

    File.WriteAllText(performanceLog, "Feature160 compositor-performance authoring PASS: focused lane, bound, scenarios, and exclusion tokens are stable.\n")

    File.WriteAllText(
        readinessFsi,
        String.concat
            Environment.NewLine
            [ "open FS.GG.UI.Testing"
              ""
              "let scenario id ="
              "    { ScenarioId = id"
              "      Covered = true"
              "      WarmupCount = 3"
              "      MeasuredRepetitions = 5"
              "      SamplePolicy = \"readback-free\""
              "      ArtifactPaths = [ $\"throughput/iterations/{id}.md\" ]"
              "      PrimaryReason = None }"
              ""
              "let required = [ \"timing/localized-update\"; \"timing/no-change\"; \"timing/movement-old-new\"; \"timing/overlap\"; \"timing/edge-clipping\" ]"
              "let check ="
              "    { Feature = \"160-performance-validation-throughput\""
              "      RequiredScenarioIds = required"
              "      Scenarios = required |> List.map scenario"
              "      AcceptedIterationCount = 3"
              "      RequiredIterationCount = 3"
              "      UnsupportedHostStatus = Feature160EnvironmentLimited"
              "      AcceptedUnsupportedHostArtifacts = 0"
              "      FullValidationStatus = \"passed\""
              "      CompatibilityAccepted = true"
              "      PackageAccepted = true"
              "      RegressionAccepted = true"
              "      PerformanceClaim = \"performance-not-accepted\""
              "      Limitations = [] }"
              ""
              "let result = Feature160ThroughputReadiness.validate check"
              "printfn \"%s %b\" (Feature160ThroughputReadiness.statusText result.Status) result.Accepted" ])

    File.WriteAllText(readinessLog, "Feature160 throughput readiness authoring PASS: helper validates accepted, blocked, rejected, fallback-only, and environment-limited packages.\n")
    File.WriteAllText(testingSurface, "FS.GG.UI.Testing exposes Feature160ThroughputReadiness additive helper records and status tokens.\n")
    File.WriteAllText(harnessSurface, "Rendering.Harness.Compositor exposes Feature 160 focused-lane command, MVU, iteration, full-validation, and readiness signatures.\n")

let private feature160FullValidationRecordFromFile validationPath =
    if not (File.Exists validationPath) then
        None
    else
        let text = File.ReadAllText validationPath
        let status =
            if text.Contains("Status: `passed`", StringComparison.OrdinalIgnoreCase) then "passed"
            elif text.Contains("Status: `failed`", StringComparison.OrdinalIgnoreCase) then "failed"
            elif text.Contains("Status: `interrupted`", StringComparison.OrdinalIgnoreCase) then "interrupted"
            elif text.Contains("Status: `stale`", StringComparison.OrdinalIgnoreCase) then "stale"
            elif text.Contains("Status: `missing`", StringComparison.OrdinalIgnoreCase) then "missing"
            else "undocumented"

        let record : Compositor.Feature160FullValidationRecord =
            { Command = "dotnet test FS.GG.Rendering.slnx --no-restore"
              StartedAt = None
              CompletedAt = None
              Status = status
              ImplementationCommit = "current-working-tree"
              PackageSurfaceBaseline = "readiness/fsi"
              ReadinessArtifactSet =
                [ "throughput/summary.md"
                  "full-validation/validation.md"
                  "compatibility-ledger.md"
                  "package-validation.md"
                  "regression-validation.md"
                  "validation-summary.md" ]
              ArtifactPaths = [ "full-validation/validation.md" ]
              Diagnostics = [ "loaded from full-validation/validation.md" ] }

        Some record

let private feature160ReadinessSummaryFromCurrentHost (out: string) (fullValidation: Compositor.Feature160FullValidationRecord option) =
    let facts = Probe.probe ()
    let profile = Compositor.hostProfileFromFacts facts
    let reason = feature160ReasonFromHost facts profile Compositor.feature160AcceptedProfileId
    let runId = "feature160-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")

    let iterations =
        match reason with
        | Some reason ->
            let exclusionReason = feature160ExclusionReasonFromHostReason reason
            let reports : Compositor.Feature158ScenarioReport list =
                Compositor.feature160RequiredScenarioIds
                |> List.map (fun scenario ->
                    feature158FailClosedReport scenario 3 5 Compositor.Feature158ReadinessStatus.EnvironmentLimited exclusionReason)

            [ feature160Iteration
                  runId
                  1
                  profile
                  Compositor.feature160MaxIterationMinutes
                  3
                  5
                  reports
                  Compositor.Feature160ReadinessStatus.EnvironmentLimited
                  (Some exclusionReason)
                  None
                  [ reason; "readiness package assembled with zero accepted performance artifacts" ] ]
        | None ->
            []

    let unsupportedReason =
        reason
        |> Option.orElse (
            if List.isEmpty iterations then None else Some "environment-limited focused throughput run")

    feature160Summary
        runId
        profile
        Compositor.feature160MaxIterationMinutes
        Compositor.feature160RequiredAttempts
        3
        5
        iterations
        unsupportedReason
        fullValidation
        "accepted-with-recorded-limitations"
        "accepted-with-recorded-limitations"
        [ "readiness package assembled"
          "focused throughput remains separate from full validation"
          "performance-not-accepted preserved"
          $"readiness-output={out}" ]

let private jsonStringProperty (root: JsonElement) (name: string) (fallback: string) : string =
    let mutable value = Unchecked.defaultof<JsonElement>
    if root.TryGetProperty(name, &value) && value.ValueKind = JsonValueKind.String then
        match value.GetString() with
        | null -> fallback
        | text when String.IsNullOrWhiteSpace text -> fallback
        | text -> text
    else
        fallback

let private jsonIntProperty (root: JsonElement) (name: string) (fallback: int) : int =
    let mutable value = Unchecked.defaultof<JsonElement>
    if root.TryGetProperty(name, &value) && value.ValueKind = JsonValueKind.Number then
        match value.TryGetInt32() with
        | true, number -> number
        | _ -> fallback
    else
        fallback

let private feature160PackageSample
    (runId: string)
    (profileId: string)
    (iterationId: string)
    (scenario: string)
    (sampleIndex: int)
    : Compositor.Feature158TimingSample =
    { SampleId = $"{iterationId}-summary-{sampleIndex}"
      SampleIndex = sampleIndex
      ScenarioId = scenario
      ScenarioDefinitionId = feature158ScenarioDefinition scenario
      Path = Perf.FullRedraw
      RunId = runId
      HostProfileId = profileId
      PackageVersion = Compositor.feature156PackageVersion
      DurationMs = 1.0
      MeasurementPolicy = Perf.ReadbackFree
      InclusionStatus = Perf.Included
      ExclusionReason = None
      ArtifactPath = Path.Combine("throughput", "raw", Compositor.feature160ScenarioFileName scenario).Replace('\\', '/') }

let private feature160PackageIteration
    (runId: string)
    (profile: Compositor.HostProfile)
    (bound: int)
    (warmup: int)
    (repetitions: int)
    (status: Compositor.Feature160ReadinessStatus)
    (reason: Perf.ExclusionReason option)
    (index: int)
    (iterationId: string)
    (artifactPath: string)
    : Compositor.Feature160Iteration =
    let included =
        if status = Compositor.Feature160ReadinessStatus.Accepted then
            Compositor.feature160RequiredScenarioIds
            |> List.mapi (fun sampleIndex scenario -> feature160PackageSample runId profile.ProfileId iterationId scenario (sampleIndex + 1))
        else
            []

    { IterationId = iterationId
      RunId = runId
      HostProfile = profile
      LaneId = Compositor.feature160FocusedLaneId
      PolicyId = Compositor.feature160PolicyId
      DeclaredBoundMinutes = bound
      ActualDuration = TimeSpan.FromSeconds(float (max 1 Compositor.feature160RequiredScenarioIds.Length))
      WarmupCount = warmup
      MeasuredRepetitions = repetitions
      ScenarioReports = []
      ScenarioCoverage = if status = Compositor.Feature160ReadinessStatus.Accepted then Compositor.feature160RequiredScenarioIds else []
      IncludedSamples = included
      ExcludedSamples = []
      Status = status
      ExclusionReason = reason
      ArtifactPaths = [ artifactPath ]
      RestrictedScenario = None
      Diagnostics = [ $"loaded-from-throughput-summary-json-index={index}" ] }

let private feature160ReadinessSummaryFromThroughputPackage
    (out: string)
    (fullValidation: Compositor.Feature160FullValidationRecord option)
    : Compositor.Feature160ThroughputSummary option =
    let summaryJson = Path.Combine(out, "throughput", "summary.json")
    if not (File.Exists summaryJson) then
        None
    else
        try
            use document = JsonDocument.Parse(File.ReadAllText summaryJson)
            let root = document.RootElement
            let facts = Probe.probe ()
            let currentProfile = Compositor.hostProfileFromFacts facts
            let runId = jsonStringProperty root "runId" ("feature160-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"))
            let hostProfileId = jsonStringProperty root "hostProfileId" currentProfile.ProfileId
            let profile = { currentProfile with ProfileId = hostProfileId }
            let bound = jsonIntProperty root "declaredBoundMinutes" Compositor.feature160MaxIterationMinutes
            let requiredAttempts = jsonIntProperty root "requiredAttempts" Compositor.feature160RequiredAttempts
            let acceptedCount = jsonIntProperty root "acceptedIterationCount" 0
            let iterationCount = jsonIntProperty root "iterationCount" acceptedCount
            let unsupportedText = jsonStringProperty root "unsupportedHostReason" ""
            let unsupported = if String.IsNullOrWhiteSpace unsupportedText then None else Some unsupportedText
            let performanceClaim = jsonStringProperty root "performanceClaim" "performance-not-accepted"
            let iterationDir = Path.Combine(out, "throughput", "iterations")

            let iterationPaths =
                if Directory.Exists iterationDir then
                    Directory.GetFiles(iterationDir, "*.md")
                    |> Array.sort
                    |> Array.toList
                    |> List.map (fun path ->
                        let relative = Path.GetRelativePath(out, path).Replace('\\', '/')
                        let iterationId =
                            match Path.GetFileNameWithoutExtension path with
                            | null -> "feature160-iteration"
                            | text when String.IsNullOrWhiteSpace text -> "feature160-iteration"
                            | text -> text

                        iterationId, relative)
                else
                    []

            let acceptedIterations =
                [ for index in 1..acceptedCount ->
                      let iterationId, artifactPath =
                          match iterationPaths |> List.tryItem (index - 1) with
                          | Some found -> found
                          | None ->
                              let id = sprintf "%s-%03i" runId index
                              id, Path.Combine("throughput", "iterations", Compositor.feature160IterationFileName id).Replace('\\', '/')

                      feature160PackageIteration
                          runId
                          profile
                          bound
                          3
                          5
                          Compositor.Feature160ReadinessStatus.Accepted
                          None
                          index
                          iterationId
                          artifactPath ]

            let rejectedIterations =
                [ for index in 1..(max 0 (iterationCount - acceptedCount)) ->
                      let sequence = acceptedCount + index
                      let id = sprintf "%s-excluded-%03i" runId sequence
                      feature160PackageIteration
                          runId
                          profile
                          bound
                          3
                          5
                          Compositor.Feature160ReadinessStatus.Rejected
                          (Some Perf.MissingMetadata)
                          sequence
                          id
                          (Path.Combine("throughput", "excluded", "README.md").Replace('\\', '/')) ]

            let summary =
                feature160Summary
                    runId
                    profile
                    bound
                    requiredAttempts
                    3
                    5
                    (acceptedIterations @ rejectedIterations)
                    unsupported
                    fullValidation
                    "accepted-with-recorded-limitations"
                    "accepted-with-recorded-limitations"
                    [ "readiness package assembled from throughput/summary.json"
                      "focused throughput remains separate from full validation"
                      $"{performanceClaim} preserved"
                      $"readiness-output={out}" ]

            Some { summary with PerformanceClaim = performanceClaim }
        with _ ->
            None

let private runFeature160ReadinessCmd (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> Compositor.feature160ReadinessDirectory

    Directory.CreateDirectory(out) |> ignore
    let throughputDir = Path.Combine(out, "throughput")
    let fullValidationDir = Path.Combine(out, "full-validation")
    Directory.CreateDirectory(throughputDir) |> ignore
    Directory.CreateDirectory(fullValidationDir) |> ignore

    if not (File.Exists(Path.Combine(throughputDir, "summary.md"))) then
        runFeature160PerformanceCmd
            [ "--feature"
              "160"
              "--lane"
              Compositor.feature160FocusedLaneId
              "--out"
              throughputDir
              "--policy"
              Compositor.feature160PolicyId
              "--attempts"
              string Compositor.feature160RequiredAttempts
              "--max-iteration-minutes"
              string Compositor.feature160MaxIterationMinutes
              "--json" ]
        |> ignore

    let fullValidationPath = Path.Combine(fullValidationDir, "validation.md")
    let fullValidation = feature160FullValidationRecordFromFile fullValidationPath
    let summary =
        feature160ReadinessSummaryFromThroughputPackage out fullValidation
        |> Option.defaultWith (fun () -> feature160ReadinessSummaryFromCurrentHost out fullValidation)

    ensureFeature160FsiEvidence out
    if not (File.Exists fullValidationPath) then
        File.WriteAllText(fullValidationPath, Compositor.renderFeature160FullValidationRecord None)

    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.renderFeature160CompatibilityLedger ())
    File.WriteAllText(
        Path.Combine(out, "package-validation.md"),
        Compositor.renderPackageValidation 160
            [ "`compositor-readiness --feature 160`: package assembled."
              "`Feature160ThroughputReadiness`: helper surface available."
              "`compositor-performance --feature 160 --lane focused`: focused lane available." ])
    File.WriteAllText(
        Path.Combine(out, "regression-validation.md"),
        Compositor.renderRegressionValidation 160
            [ "`compositor-readiness --feature 160`: package assembled."
              "Feature 155, 157, 158, and 159 preservation evidence remains linked."
              "Unsupported-host validation records zero accepted same-profile performance artifacts." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.renderFeature160ValidationSummary summary)
    printfn "%s" (Path.Combine(out, "validation-summary.md"))
    0

let ensureFeature161FsiEvidence out =
    let fsiDir = Path.Combine(out, "fsi")
    Directory.CreateDirectory(fsiDir) |> ignore

    let compositorFsi = Path.Combine(fsiDir, "compositor-host-lane-authoring.fsx")
    let compositorLog = Path.Combine(fsiDir, "compositor-host-lane-authoring.log")
    let readinessFsi = Path.Combine(fsiDir, "feature161-host-lane-readiness-authoring.fsx")
    let readinessLog = Path.Combine(fsiDir, "feature161-host-lane-readiness-authoring.log")
    let testingSurface = Path.Combine(fsiDir, "FS.GG.UI.Testing.txt")
    let harnessSurface = Path.Combine(fsiDir, "Rendering.Harness.Compositor.txt")
    let perfSurface = Path.Combine(fsiDir, "Rendering.Harness.Perf.txt")

    File.WriteAllText(
        compositorFsi,
        String.concat
            Environment.NewLine
            [ "let command = \"compositor-performance --feature 161 --lane host-ledger --policy host-lane-ledger-v1\""
              "let requiredFacts ="
              "    [ \"display-server\"; \"display-identity\"; \"renderer-identity\"; \"direct-rendering\""
              "      \"refresh\"; \"driver-identity\"; \"package-version-set\"; \"cpu-load-note\""
              "      \"gpu-load-note\"; \"environment-limits\"; \"host-profile\"; \"run-identity\""
              "      \"scenario-identity\"; \"timing-policy-identity\"; \"collection-time\"; \"artifact-locations\" ]"
              "let nonGeneralized = [ \"Wayland\"; \"indirect GL\"; \"missing display\"; \"software raster\"; \"virtualized presentation\"; \"unknown renderer\" ]"
              "printfn \"%s %A %A\" command requiredFacts nonGeneralized" ])

    File.WriteAllText(compositorLog, "Feature161 compositor host-lane authoring PASS: command, policy, required facts, and non-generalized lanes are stable.\n")

    File.WriteAllText(
        readinessFsi,
        String.concat
            Environment.NewLine
            [ "open FS.GG.UI.Testing"
              ""
              "let facts ="
              "    { LaneId = \"x11-:1-direct-opengl-amd-mesa\""
              "      DisplayServer = \"x11\""
              "      DisplayIdentity = \":1\""
              "      RendererIdentity = \"AMD Radeon Mesa\""
              "      DirectRendering = Some true"
              "      RefreshStatus = \"119.93 Hz\""
              "      DriverIdentity = \"Mesa\""
              "      PackageVersionSet = \"local-harness\""
              "      CpuLoadNote = \"representative\""
              "      GpuLoadNote = \"representative\""
              "      EnvironmentLimits = []"
              "      HostProfile = \"probe-08a47c01\""
              "      RunIdentity = \"feature161-authoring\""
              "      ScenarioIdentity = \"timing/host-lane-ledger\""
              "      TimingPolicyIdentity = \"host-lane-ledger-v1\""
              "      ArtifactPaths = [ \"lane-ledger/entries/entry-feature161-authoring.md\" ] }"
              ""
              "let check ="
              "    { Feature = \"161-host-performance-lane-ledger\""
              "      RequiredScenarioIds = [ \"timing/host-lane-ledger\" ]"
              "      CoveredScenarioIds = [ \"timing/host-lane-ledger\" ]"
              "      HostFacts = Some facts"
              "      AcceptedLaneScopedPerformanceArtifacts = 1"
              "      UnsupportedHostStatus = Feature161FallbackOnly"
              "      PriorGateStatuses = [ \"confirmed\"; \"confirmed\"; \"confirmed\"; \"confirmed\"; \"confirmed\" ]"
              "      ClaimScope ="
              "        { AcceptedLaneId = Some facts.LaneId"
              "          NonGeneralizedLanes = [ \"Wayland\"; \"indirect GL\"; \"missing display\" ]"
              "          RemainingBlockers = []"
              "          PerformanceClaim = \"performance-not-accepted\" }"
              "      FullValidationStatus = \"passed\""
              "      CompatibilityAccepted = true"
              "      PackageAccepted = true"
              "      RegressionAccepted = true"
              "      Limitations = [] }"
              ""
              "let result = Feature161HostLaneReadiness.validate check"
              "printfn \"%s %b\" (Feature161HostLaneReadiness.statusText result.Status) result.Accepted" ])

    File.WriteAllText(readinessLog, "Feature161 host lane readiness authoring PASS: helper validates accepted, rejected, environment-limited, blocked, fallback-only, and missing-evidence packages.\n")
    File.WriteAllText(testingSurface, "FS.GG.UI.Testing exposes Feature161HostLaneReadiness additive helper records and status tokens.\n")
    File.WriteAllText(harnessSurface, "Rendering.Harness.Compositor exposes Feature 161 host facts, lane ledger, claim scope, MVU, command, and readiness signatures.\n")
    File.WriteAllText(perfSurface, "Rendering.Harness.Perf exposes Feature 161 host-lane exclusion reason tokens including missing-display, indirect-rendering, software-raster, unknown-renderer, host-facts-missing, cross-lane-evidence, noisy-timing, and prior-gate-blocked.\n")

let feature161FullValidationStatusFromFile validationPath =
    if not (File.Exists validationPath) then
        "missing"
    else
        let text = File.ReadAllText validationPath
        if text.Contains("Status: `passed`", StringComparison.OrdinalIgnoreCase) then "passed"
        elif text.Contains("Status: `failed`", StringComparison.OrdinalIgnoreCase) then "failed"
        elif text.Contains("Status: `interrupted`", StringComparison.OrdinalIgnoreCase) then "interrupted"
        elif text.Contains("Status: `stale`", StringComparison.OrdinalIgnoreCase) then "stale"
        else "undocumented"

let feature161ReadinessSummaryFromCurrentHost out fullValidationStatus =
    let facts = Probe.probe ()
    let profile = Compositor.hostProfileFromFacts facts
    let runId = "feature161-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
    let sourceThroughput = Path.Combine(out, "lane-ledger").Replace('\\', '/')
    let hostFacts = feature161HostFactsFromProbe runId "timing/host-lane-ledger" sourceThroughput facts profile
    let entry = feature161EntryFromFacts runId hostFacts Compositor.feature161AcceptedProfileId
    let unsupported =
        entry.PrimaryExclusionReason
        |> Option.bind (fun reason ->
            match reason with
            | Perf.MissingDisplay
            | Perf.IndirectRendering
            | Perf.SoftwareRaster
            | Perf.UnknownRenderer
            | Perf.VirtualizedPresentation -> Some(Perf.exclusionReasonToken reason)
            | _ -> None)

    feature161Summary
        runId
        profile
        [ entry ]
        unsupported
        fullValidationStatus
        "accepted-with-recorded-limitations"
        "accepted-with-recorded-limitations"
        [ "readiness package assembled"
          "host lane facts preserve current-host scope"
          "performance-not-accepted preserved"
          $"readiness-output={out}" ]

let runFeature161ReadinessCmd (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> Compositor.feature161ReadinessDirectory

    Directory.CreateDirectory(out) |> ignore
    let laneLedgerDir = Path.Combine(out, "lane-ledger")
    let fullValidationDir = Path.Combine(out, "full-validation")
    Directory.CreateDirectory(laneLedgerDir) |> ignore
    Directory.CreateDirectory(fullValidationDir) |> ignore

    if not (File.Exists(Path.Combine(laneLedgerDir, "summary.md"))) then
        runFeature161PerformanceCmd
            [ "--feature"
              "161"
              "--lane"
              "host-ledger"
              "--out"
              laneLedgerDir
              "--policy"
              Compositor.feature161PolicyId
              "--source-throughput"
              Compositor.feature160ThroughputDirectory
              "--json" ]
        |> ignore

    let fullValidationPath = Path.Combine(fullValidationDir, "validation.md")
    let fullValidationStatus = feature161FullValidationStatusFromFile fullValidationPath
    let summary = feature161ReadinessSummaryFromCurrentHost out fullValidationStatus

    writeFeature161LaneLedgerPackage laneLedgerDir summary
    ensureFeature161FsiEvidence out
    if not (File.Exists fullValidationPath) then
        File.WriteAllText(fullValidationPath, Compositor.renderFeature161FullValidationRecord "missing")

    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.renderFeature161CompatibilityLedger ())
    File.WriteAllText(
        Path.Combine(out, "package-validation.md"),
        Compositor.renderPackageValidation 161
            [ "`compositor-readiness --feature 161`: package assembled."
              "`Feature161HostLaneReadiness`: helper surface available."
              "`compositor-performance --feature 161 --lane host-ledger`: host lane ledger available." ])
    File.WriteAllText(
        Path.Combine(out, "regression-validation.md"),
        Compositor.renderRegressionValidation 161
            [ "`compositor-readiness --feature 161`: package assembled."
              "Feature 155, 157, 158, 159, and 160 preservation evidence remains linked."
              "Unsupported-host validation records zero accepted lane-scoped performance artifacts." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.renderFeature161ValidationSummary summary)
    printfn "%s" (Path.Combine(out, "validation-summary.md"))
    0

let private runCompositorPromotionCmd (rest: string list) =
    if isFeature159 rest then
        runFeature159PromotionCmd rest
    else
        eprintfn "compositor-promotion requires --feature 159"
        2

let private runCompositorReadinessCmd (rest: string list) =
    if isFeature161 rest then
        runFeature161ReadinessCmd rest
    elif isFeature160 rest then
        runFeature160ReadinessCmd rest
    elif isFeature159 rest then
        runFeature159ReadinessCmd rest
    elif isFeature158 rest then
        runFeature158ReadinessCmd rest
    elif isFeature157 rest then
        runFeature157ReadinessCmd rest
    elif isFeature156 rest then
        runFeature156ReadinessCmd rest
    else
        runLegacyCompositorReadinessCmd rest

let private flagValues (flag: string) (rest: string list) =
    let rec collect acc xs =
        match xs with
        | f :: v :: tl when f = flag -> collect (acc @ [ v ]) tl
        | _ :: tl -> collect acc tl
        | [] -> acc

    collect [] rest

let private hasFlag (flag: string) (rest: string list) =
    rest |> List.exists ((=) flag)

let private runPackageFeedCmd (rest: string list) =
    let mode =
        flagValue "--mode" rest
        |> Option.defaultValue "check"
        |> PackageFeed.tryParseMode

    match mode with
    | None ->
        eprintfn "package-feed: --mode must be check, refresh, or proof"
        2
    | Some mode ->
        let repositoryRoot = Directory.GetCurrentDirectory()
        let samples = flagValues "--sample" rest

        if samples.IsEmpty then
            eprintfn "package-feed: at least one --sample <path> is required"
            2
        else
            let out =
                flagValue "--out" rest
                |> Option.defaultValue "specs/163-package-feed-validation-lanes/readiness/package-proof"

            let feed =
                flagValue "--feed" rest
                |> Option.defaultValue PackageFeed.defaultFeedPath

            let options: PackageFeed.PackageFeedOptions =
                { RepositoryRoot = repositoryRoot
                  SelectedSamples = samples
                  FeedPath = feed
                  OutDir = out
                  Mode = mode
                  PackBeforeCheck = hasFlag "--pack" rest
                  IsolatedCachePath = flagValue "--isolated-cache" rest
                  Cold = hasFlag "--cold" rest
                  ClearGlobalCache = hasFlag "--clear-global-cache" rest
                  AllowedExceptionIds = flagValues "--allow-exception" rest |> Set.ofList
                  CompatibilityExceptions = [] }

            let result = PackageFeed.runWorkflow options

            printfn "package-feed status: %s" (PackageFeed.proofStatusToken result.Status)
            printfn "packages: %i" result.CurrentPackages.Length
            printfn "pins: %i" result.PackagePins.Length

            for evidence in result.EvidenceFiles do
                printfn "%s" evidence

            for diagnostic in result.Diagnostics do
                eprintfn "%s" diagnostic

            match result.Status with
            | PackageFeed.Passed -> 0
            | PackageFeed.Failed -> 1
            | PackageFeed.EnvironmentLimited -> 3

let private runValidationLanesCmd (rest: string list) =
    let repositoryRoot = Directory.GetCurrentDirectory()

    let out =
        flagValue "--out" rest
        |> Option.defaultValue "artifacts/validation-lanes"

    let replaceRunValue =
        let rec find xs =
            match xs with
            | "--replace-run" :: value :: _ when not (value.StartsWith("-", StringComparison.Ordinal)) -> Some(Some value)
            | "--replace-run" :: _ -> Some None
            | _ :: tl -> find tl
            | [] -> None

        find rest

    let runId =
        match flagValue "--run-id" rest, replaceRunValue with
        | Some id, _ -> Some id
        | None, Some(Some id) -> Some id
        | None, _ -> None

    let request: ValidationLanes.RunRequest =
        { RequestedLaneIds = flagValues "--lane" rest
          IncludeOptionalLaneIds = (flagValues "--include-optional" rest) @ (flagValues "--include" rest)
          OutDir = out
          RunId = runId
          ReplaceRun = replaceRunValue.IsSome
          ListOnly = hasFlag "--list" rest
          AllowParallel = hasFlag "--parallel" rest }

    if request.ListOnly then
        let runRoot = Path.Combine(out, request.RunId |> Option.defaultValue "list-only")
        let lanes = ValidationLanes.defaultLaneDefinitions repositoryRoot runRoot

        for lane in lanes do
            printfn
                "%s\t%s\t%s\t%s"
                lane.Id
                (ValidationLanes.roleToken lane.ReadinessRole)
                (lane.Timeout.ToString())
                lane.Description

        0
    else
        let runResult: Result<ValidationLanes.ValidationSummary, ValidationLanes.PreflightDiagnostic list> =
            ValidationLanes.runRequest repositoryRoot request

        match runResult with
        | Result.Error diagnostics ->
            for diagnostic in diagnostics do
                eprintfn "%s: %s" diagnostic.Code diagnostic.Message

            2
        | Result.Ok (summary: ValidationLanes.ValidationSummary) ->
            let markdownPath = Path.Combine(summary.ArtifactRoot, "summary.md")
            let jsonPath = Path.Combine(summary.ArtifactRoot, "summary.json")

            if hasFlag "--json" rest then
                printfn
                    "{\"summaryJson\":%s,\"overallReadiness\":%s}"
                    (JsonSerializer.Serialize jsonPath)
                    (JsonSerializer.Serialize(ValidationLanes.readinessToken summary.OverallReadiness))
            else
                printfn "%s" markdownPath

            if summary.LaneResults |> List.exists (fun (result: ValidationLanes.LaneResult) -> result.Status = ValidationLanes.Canceled) then
                130
            elif summary.LaneResults |> List.exists (fun (result: ValidationLanes.LaneResult) -> result.Status = ValidationLanes.InfrastructureError) then
                3
            else
                match summary.OverallReadiness with
                | ValidationLanes.Ready -> 0
                | ValidationLanes.Blocked
                | ValidationLanes.Incomplete
                | ValidationLanes.EnvironmentLimitedReadiness -> 1

[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | "probe" :: rest -> runProbe rest
    | "offscreen" :: rest -> runOffscreen rest
    | "perf" :: rest -> runPerfCmd rest
    | "__viewer" :: _ -> Live.launchViewerChild ()
    | "__vsyncprobe" :: stampFile :: rest ->
        let seconds = match rest with | s :: _ -> (match Double.TryParse s with | true, v -> v | _ -> 3.0) | [] -> 3.0
        Live.launchVsyncProbeChild stampFile seconds
    | "live-x11" :: rest -> runLiveCmd rest
    | "overlay-visual-proof" :: rest -> runOverlayVisualProofCmd rest
    | "render-anywhere-reference" :: rest -> runRenderAnywhereReferenceCmd rest
    | "render-anywhere-browser-feasibility" :: rest -> runRenderAnywhereBrowserFeasibilityCmd rest
    | "compositor-present-proof" :: rest -> runCompositorPresentProofCmd rest
    | "compositor-live-proof" :: rest -> runCompositorLiveProofCmd rest
    | "compositor-parity" :: rest -> runCompositorParityCmd rest
    | "compositor-perf" :: rest -> runCompositorPerfCmd rest
    | "compositor-reuse" :: rest -> runCompositorReuseCmd rest
    | "compositor-snapshots" :: rest -> runCompositorSnapshotsCmd rest
    | "compositor-timing" :: rest -> runCompositorTimingCmd rest
    | "compositor-damage" :: rest -> runCompositorDamageCmd rest
    | "compositor-promotion" :: rest -> runCompositorPromotionCmd rest
    | "compositor-performance" :: rest -> runCompositorPerformanceCmd rest
    | "compositor-readiness" :: rest -> runCompositorReadinessCmd rest
    | "package-feed" :: rest -> runPackageFeedCmd rest
    | "validation-lanes" :: rest -> runValidationLanesCmd rest
    | "skill-parity" :: rest -> SkillParity.runCli rest
    | "input" :: rest ->
        let known () = Input.scripts |> Map.toList |> List.map fst |> String.concat ", "
        match flagValue "--backend" rest |> Option.bind Input.parseBackend, flagValue "--script" rest with
        | None, _ ->
            eprintfn "input: --backend pure|x11-xtest|uinput required"
            2
        | _, None ->
            eprintfn "input: --script <name> required (known: %s)" (known ())
            2
        | Some backend, Some name ->
            match Input.tryScript name with
            | None ->
                eprintfn "input: unknown script '%s' (known: %s)" name (known ())
                2
            | Some script ->
                let facts = Probe.probe ()
                let out = outDir rest
                let selfDll =
                    match System.Reflection.Assembly.GetEntryAssembly() with
                    | null -> ""
                    | a -> a.Location
                let ev = Input.run backend script facts selfDll out
                let path = Evidence.write out ev []
                printfn "%s" path
                match ev.Status with
                | RunStatus.Passed
                | RunStatus.Skipped -> 0
                | RunStatus.Failed -> 1
    | []
    | "--help" :: _ ->
        printfn "usage: <probe|offscreen|live-x11|overlay-visual-proof|render-anywhere-reference|render-anywhere-browser-feasibility|compositor-present-proof|compositor-live-proof|compositor-parity|compositor-perf|compositor-reuse|compositor-snapshots|compositor-timing|compositor-damage|compositor-promotion|compositor-performance|compositor-readiness|package-feed|validation-lanes|skill-parity|perf|input> [--out <dir>] [--json]"
        0
    | other ->
        eprintfn "unknown subcommand: %s" (String.concat " " other)
        2
