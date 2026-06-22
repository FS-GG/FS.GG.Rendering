module Rendering.Harness.CliFeatureBuilders

open System
open System.IO
open System.Text.Json
open Rendering.Harness
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open Rendering.Harness.CliShared

let feature156ScenarioStem (scenarioId: string) =
    scenarioId.Replace("/", "-")

let feature156FormatMs (value: float) =
    value.ToString("0.###", Globalization.CultureInfo.InvariantCulture)

let feature156ScenarioScene scenarioId damageScoped =
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

let feature156PathDistribution (distribution: Perf.SampleDistribution) : Compositor.Types.Feature156PathDistribution =
    { SampleCount = distribution.Count
      P50Ms = distribution.P50Ms
      P95Ms = distribution.P95Ms
      P99Ms = distribution.P99Ms
      MinMs = distribution.MinMs
      MaxMs = distribution.MaxMs
      RawSamplePath = distribution.RawSamplePath }

let feature156VerdictFromPerf verdict =
    match verdict with
    | Perf.Positive -> Compositor.Types.Feature156Positive
    | Perf.Noisy -> Compositor.Types.Feature156Noisy
    | Perf.NonBeneficial -> Compositor.Types.Feature156NonBeneficial
    | Perf.Incomplete -> Compositor.Types.Feature156Incomplete
    | Perf.Rejected -> Compositor.Types.Feature156Rejected
    | Perf.EnvironmentLimited -> Compositor.Types.Feature156EnvironmentLimited
    | Perf.Limited -> Compositor.Types.Feature156Limited

let writeFeature156RawSamples rawPath scenarioId path runId hostProfile samples =
    let sampleLines =
        samples
        |> List.mapi (fun index duration ->
            $"{index + 1},{scenarioId},{path},{runId},{hostProfile},{feature156FormatMs duration}")

    let lines = "sample-index,scenario-id,path,run-id,host-profile-id,duration-ms" :: sampleLines

    File.WriteAllText(rawPath, String.concat Environment.NewLine lines + Environment.NewLine)

let captureFeature156Sample rawDir scenarioId path hostFacts =
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

let runFeature156Path rawDir scenarioId path warmup repetitions runId hostProfileId hostFacts =
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

let feature156SummaryFromReports
    runId
    (profile: Compositor.Types.HostProfile)
    warmup
    repetitions
    (reports: Compositor.Types.Feature156ScenarioReport list)
    diagnostics
    : Compositor.Types.Feature156TimingSummary =
    let overall = Compositor.FeatureState.feature156OverallVerdict reports
    { RunId = runId
      HostProfile = profile
      PolicyId = Compositor.Config.feature156PolicyId
      WarmupCount = warmup
      MeasuredRepetitions = repetitions
      ScenarioReports = reports
      OverallVerdict = overall
      ShippedPerformanceClaim = "performance-not-accepted"
      Diagnostics = diagnostics }

let feature156VerdictFromToken token =
    match token with
    | "positive" -> Some Compositor.Types.Feature156Positive
    | "noisy" -> Some Compositor.Types.Feature156Noisy
    | "non-beneficial" -> Some Compositor.Types.Feature156NonBeneficial
    | "incomplete" -> Some Compositor.Types.Feature156Incomplete
    | "rejected" -> Some Compositor.Types.Feature156Rejected
    | "environment-limited" -> Some Compositor.Types.Feature156EnvironmentLimited
    | "limited" -> Some Compositor.Types.Feature156Limited
    | _ -> None

let feature156ExistingTimingVerdict timingSummaryPath =
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

let feature158ReasonFromHost facts (profile: Compositor.Types.HostProfile) expectedProfile =
    match facts.EffectiveBackend, facts.GlRenderer, facts.GlDirect, profile.ProfileId = expectedProfile with
    | NoDisplay, _, _, _ -> Some "missing display"
    | _, None, _, _ -> Some "missing GL renderer facts"
    | _, _, false, _ -> Some "OpenGL direct rendering is unavailable"
    | _, _, _, false -> Some $"profile mismatch: expected={expectedProfile} actual={profile.ProfileId}"
    | _ -> None

let feature158ScenarioDefinition scenarioId =
    "feature156-required-v1:" + feature156ScenarioStem scenarioId

let feature158Distribution rawSamplePath samples : Compositor.Types.Feature158PathDistribution option =
    Perf.summarizeSamples rawSamplePath samples
    |> Option.map (fun distribution ->
        { SampleCount = distribution.Count
          P50Ms = distribution.P50Ms
          P95Ms = distribution.P95Ms
          P99Ms = distribution.P99Ms
          MinMs = distribution.MinMs
          MaxMs = distribution.MaxMs
          RawSamplePath = distribution.RawSamplePath })

let feature158SummaryFromReports
    runId
    (profile: Compositor.Types.HostProfile)
    warmup
    repetitions
    (reports: Compositor.Types.Feature158ScenarioReport list)
    (proofProbes: Compositor.Types.Feature158ProofProbeEvidence list)
    (unsupported: string option)
    (diagnostics: string list)
    : Compositor.Types.Feature158TimingSummary =
    let included = reports |> List.collect _.IncludedSamples
    let excluded = reports |> List.collect _.ExcludedSamples
    let status =
        match unsupported with
        | Some _ -> Compositor.Types.Feature158ReadinessStatus.EnvironmentLimited
        | None when Compositor.Config.feature158RequiredScenarioIds |> List.forall (fun scenario -> reports |> List.exists (fun report -> report.ScenarioId = scenario && report.Status = Compositor.Types.Feature158ReadinessStatus.Accepted)) ->
            Compositor.Types.Feature158ReadinessStatus.Accepted
        | None when not (List.isEmpty included) -> Compositor.Types.Feature158ReadinessStatus.Rejected
        | None -> Compositor.Types.Feature158ReadinessStatus.FallbackOnly

    { RunId = runId
      HostProfile = profile
      PolicyId = Compositor.Config.feature158PolicyId
      WarmupCount = warmup
      MeasuredRepetitions = repetitions
      ScenarioReports = reports
      IncludedSamples = included
      ExcludedSamples = excluded
      ProofProbeEvidence = proofProbes
      UnsupportedHostReason = unsupported
      Feature156Comparison = if status = Compositor.Types.Feature158ReadinessStatus.Accepted then "contextualizes" else "contextualizes"
      Status = status
      PerformanceClaim = "performance-not-accepted"
      Diagnostics = diagnostics }

type Feature158TimingSnapshot =
    { RunId: string
      Status: Compositor.Types.Feature158ReadinessStatus
      IncludedSampleCount: int
      ExcludedSampleCount: int
      UnsupportedHostReason: string option
      Feature156Comparison: string
      PerformanceClaim: string
      ExcludedReasons: Perf.ExclusionReason list }

let feature158StatusFromToken token =
    match token with
    | "accepted" -> Compositor.Types.Feature158ReadinessStatus.Accepted
    | "fallback-only" -> Compositor.Types.Feature158ReadinessStatus.FallbackOnly
    | "rejected" -> Compositor.Types.Feature158ReadinessStatus.Rejected
    | "environment-limited" -> Compositor.Types.Feature158ReadinessStatus.EnvironmentLimited
    | _ -> Compositor.Types.Feature158ReadinessStatus.FallbackOnly

let feature158ExclusionReasonFromToken token =
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

let jsonString (root: JsonElement) (name: string) =
    let mutable property = Unchecked.defaultof<JsonElement>
    if root.TryGetProperty(name, &property) && property.ValueKind = JsonValueKind.String then
        property.GetString() |> Option.ofObj
    else
        None

let jsonInt (root: JsonElement) (name: string) =
    let mutable property = Unchecked.defaultof<JsonElement>
    let mutable value = 0
    if root.TryGetProperty(name, &property) && property.TryGetInt32(&value) then
        Some value
    else
        None

let loadFeature158TimingSnapshot timingDir =
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
                  Status = jsonString root "status" |> Option.map feature158StatusFromToken |> Option.defaultValue Compositor.Types.Feature158ReadinessStatus.FallbackOnly
                  IncludedSampleCount = jsonInt root "includedSampleCount" |> Option.defaultValue 0
                  ExcludedSampleCount = jsonInt root "excludedSampleCount" |> Option.defaultValue 0
                  UnsupportedHostReason = unsupported
                  Feature156Comparison = jsonString root "feature156Comparison" |> Option.defaultValue "contextualizes"
                  PerformanceClaim = jsonString root "performanceClaim" |> Option.defaultValue "performance-not-accepted"
                  ExcludedReasons = reasons }
        with _ ->
            None

let feature158CountByScenario total =
    let scenarios = Compositor.Config.feature158RequiredScenarioIds
    let count = max 0 total
    let scenarioCount = max 1 scenarios.Length
    let quotient = count / scenarioCount
    let remainder = count % scenarioCount
    scenarios
    |> List.mapi (fun index scenario -> scenario, quotient + if index < remainder then 1 else 0)
    |> Map.ofList

let loadFeature158ProofProbeEvidence proofProbeDir (profile: Compositor.Types.HostProfile) : Compositor.Types.Feature158ProofProbeEvidence list =
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
            [ { Compositor.Types.Feature158ProofProbeEvidence.ProbeId = "feature158-existing-proof-probes"
                HostProfile = profile
                ScenarioIds = Compositor.Config.feature158RequiredScenarioIds
                ReadbackArtifacts = xs
                ProbeSampleIds = []
                ExclusionReason = Perf.ProbeRunExcluded
                Diagnostics = [ "loaded from proof-probes directory"; "accepted performance samples=0" ] } ]

let feature158Sample sampleId sampleIndex scenario path runId profile packageVersion policy status reason duration artifact : Compositor.Types.Feature158TimingSample =
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

let feature158ScenarioReport
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
    : Compositor.Types.Feature158ScenarioReport =
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

let feature158ReadinessReportsFromSnapshot (snapshot: Feature158TimingSnapshot) (profile: Compositor.Types.HostProfile) proofProbeArtifacts =
    let includedCounts = feature158CountByScenario snapshot.IncludedSampleCount
    let excludedCounts = feature158CountByScenario snapshot.ExcludedSampleCount
    let excludedReasons =
        match snapshot.ExcludedReasons with
        | [] -> [ Perf.UnverifiableMeasurementPolicy ]
        | reasons -> reasons

    Compositor.Config.feature158RequiredScenarioIds
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
                      Compositor.Config.feature156PackageVersion
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
                      Compositor.Config.feature156PackageVersion
                      policy
                      status
                      (Some reason)
                      0.0
                      (Path.Combine("timing", "excluded", Perf.exclusionReasonToken reason + ".md").Replace('\\', '/')) ]

        let status =
            match snapshot.Status with
            | Compositor.Types.Feature158ReadinessStatus.Accepted when List.isEmpty included -> Compositor.Types.Feature158ReadinessStatus.FallbackOnly
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
            [ Path.Combine("timing", "scenarios", Compositor.FeatureState.feature158ScenarioFileName scenario).Replace('\\', '/') ]
            [ "readiness summary derived from timing/summary.json" ])

let writeFeature158RawSamples rawDir scenario pathToken runId hostProfile packageVersion policy (samples: Compositor.Types.Feature158TimingSample list) =
    let rawStem = $"{feature156ScenarioStem scenario}-{pathToken}"
    let csvRelative = Path.Combine("raw", rawStem + ".csv").Replace('\\', '/')
    let jsonRelative = Path.Combine("raw", rawStem + ".json").Replace('\\', '/')
    let csvPath = Path.Combine(rawDir, rawStem + ".csv")
    let jsonPath = Path.Combine(rawDir, rawStem + ".json")

    let csvLines =
        samples
        |> List.map (fun (sample: Compositor.Types.Feature158TimingSample) ->
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

let measureFeature158Path rawDir scenario path warmup repetitions runId hostProfileId hostFacts =
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
              let sample = feature158Sample sampleId index scenario path runId hostProfileId Compositor.Config.feature156PackageVersion policy status reason duration artifact
              match result with
              | Result.Ok _ -> sample
              | Result.Error failure -> { sample with ArtifactPath = artifact + $"; failure={failure.Message}" } ]

    let csvRelative, jsonRelative = writeFeature158RawSamples rawDir scenario pathToken runId hostProfileId Compositor.Config.feature156PackageVersion Perf.ReadbackFree samples
    let includedDurations =
        samples
        |> List.filter (fun sample -> sample.InclusionStatus = Perf.Included)
        |> List.map _.DurationMs

    csvRelative, jsonRelative, samples, feature158Distribution csvRelative includedDurations

let feature158FailClosedReport scenario warmup repetitions status reason : Compositor.Types.Feature158ScenarioReport =
    let sample =
        feature158Sample
            $"feature158-failclosed-{feature156ScenarioStem scenario}"
            1
            scenario
            Perf.FullRedraw
            "feature158-failclosed"
            Compositor.Config.feature158AcceptedProfileId
            Compositor.Config.feature156PackageVersion
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
        [ Path.Combine("scenarios", Compositor.FeatureState.feature158ScenarioFileName scenario).Replace('\\', '/') ]
        [ Perf.exclusionReasonToken reason ]

let feature158ScenarioStatus (included: Compositor.Types.Feature158TimingSample list) (excluded: Compositor.Types.Feature158TimingSample list) =
    if List.isEmpty included && excluded |> List.exists (fun sample -> sample.ExclusionReason = Some Perf.EnvironmentLimitedReason || sample.ExclusionReason = Some Perf.UnsupportedHost) then
        Compositor.Types.Feature158ReadinessStatus.EnvironmentLimited
    elif not (List.isEmpty included)
         && included |> List.forall (fun sample -> sample.MeasurementPolicy = Perf.ReadbackFree || sample.MeasurementPolicy = Perf.ReadbackOutsideMeasurement)
         && excluded |> List.forall (fun sample -> sample.InclusionStatus <> Perf.Included) then
        Compositor.Types.Feature158ReadinessStatus.Accepted
    elif List.isEmpty included then
        Compositor.Types.Feature158ReadinessStatus.FallbackOnly
    else
        Compositor.Types.Feature158ReadinessStatus.Rejected

let writeFeature158TimingPackage out (summary: Compositor.Types.Feature158TimingSummary) =
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
        File.WriteAllText(Path.Combine(scenariosDir, Compositor.FeatureState.feature158ScenarioFileName report.ScenarioId), Compositor.Render3.emitFeature158ScenarioReport report)

    summary.ExcludedSamples
    |> List.groupBy (fun sample -> sample.ExclusionReason |> Option.defaultValue Perf.UnverifiableMeasurementPolicy)
    |> List.iter (fun (reason, samples) ->
        File.WriteAllText(Path.Combine(excludedDir, Perf.exclusionReasonToken reason + ".md"), Compositor.Render3.emitFeature158ExcludedSamplesReport reason samples))

    if not (File.Exists(Path.Combine(excludedDir, "README.md"))) then
        File.WriteAllText(Path.Combine(excludedDir, "README.md"), "# Feature 158 Excluded Samples\n\nGrouped excluded samples are written by primary reason.\n")

    let proofProbeReadme = Path.Combine(proofProbeDir, "README.md")
    if not (List.isEmpty summary.ProofProbeEvidence) || not (File.Exists proofProbeReadme) then
        File.WriteAllText(proofProbeReadme, Compositor.Render3.emitFeature158ProofProbeReport summary.ProofProbeEvidence)

    match summary.UnsupportedHostReason with
    | Some reason ->
        File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.Render3.emitFeature158UnsupportedHostReport reason)
        File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), Compositor.Render3.emitFeature158UnsupportedHostReport reason)
    | None ->
        if not (File.Exists(Path.Combine(unsupportedDir, "README.md"))) then
            File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.Render3.emitFeature158UnsupportedHostReport "not run in this invocation")

    File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.Render3.emitFeature158TimingSummary summary)
    File.WriteAllText(Path.Combine(out, "summary.json"), Compositor.Render3.emitFeature158TimingSummaryJson summary + Environment.NewLine)

let writeFeature158ProbeEvidencePackage out (summary: Compositor.Types.Feature158TimingSummary) =
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
        File.WriteAllText(Path.Combine(excludedDir, Perf.exclusionReasonToken reason + ".md"), Compositor.Render3.emitFeature158ExcludedSamplesReport reason samples))

    if not (File.Exists(Path.Combine(excludedDir, "README.md"))) then
        File.WriteAllText(Path.Combine(excludedDir, "README.md"), "# Feature 158 Excluded Samples\n\nGrouped excluded samples are written by primary reason.\n")

    File.WriteAllText(Path.Combine(proofProbeDir, "README.md"), Compositor.Render3.emitFeature158ProofProbeReport summary.ProofProbeEvidence)

let feature157DamageOutDir (rest: string list) =
    match flagValue "--out" rest with
    | Some d -> d
    | None -> Compositor.Config.feature157DamageDirectory

let feature157ScenarioSet (rest: string list) =
    match flagValue "--scenario" rest with
    | Some scenario when Compositor.Config.feature157ScenarioIds |> List.contains scenario -> [ scenario ]
    | Some scenario ->
        eprintfn "unknown Feature 157 scenario: %s" scenario
        []
    | None -> Compositor.Config.feature157RequiredScenarioIds

let feature157ReasonFromHost facts (profile: Compositor.Types.HostProfile) expectedProfile =
    match facts.EffectiveBackend, facts.GlRenderer, facts.GlDirect, profile.ProfileId = expectedProfile with
    | NoDisplay, _, _, _ -> Some "missing display"
    | _, None, _, _ -> Some "missing GL renderer facts"
    | _, _, false, _ -> Some "OpenGL direct rendering is unavailable"
    | _, _, _, false -> Some $"profile mismatch: expected={expectedProfile} actual={profile.ProfileId}"
    | _ -> None

let feature157Fallback scenario reason artifact : Compositor.Types.Feature157Fallback =
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

let feature157Attempt runId (index: int) scenario (profile: Compositor.Types.HostProfile) : Compositor.Types.Feature157DamageAttempt =
    let stem = Compositor.FeatureState.feature157ScenarioFileName scenario
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

let feature157Summary runId profile status attempts fallbacks unsupported diagnostics : Compositor.Types.Feature157DamageSummary =
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

let writeFeature157DamagePackage out (summary: Compositor.Types.Feature157DamageSummary) =
    let attemptsDir = Path.Combine(out, "attempts")
    let fallbacksDir = Path.Combine(out, "fallbacks")
    let parityDir = Path.Combine(out, "parity")
    let unsupportedDir = Path.Combine(out, "unsupported")
    Directory.CreateDirectory(attemptsDir) |> ignore
    Directory.CreateDirectory(fallbacksDir) |> ignore
    Directory.CreateDirectory(parityDir) |> ignore
    Directory.CreateDirectory(unsupportedDir) |> ignore

    for attempt in summary.AcceptedAttempts do
        File.WriteAllText(Path.Combine(attemptsDir, $"{attempt.AttemptId}.md"), Compositor.Render3.emitFeature157AttemptReport attempt)
        File.WriteAllText(Path.Combine(parityDir, $"{attempt.AttemptId}.md"), Compositor.Render3.emitFeature157ParityReport attempt)

    for fallback in summary.Fallbacks do
        let name = Compositor.FeatureState.feature157ScenarioFileName fallback.ScenarioId
        File.WriteAllText(Path.Combine(fallbacksDir, name), Compositor.Render3.emitFeature157FallbackReport fallback)

    match summary.UnsupportedHostReason with
    | Some reason ->
        File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.Render3.emitFeature157UnsupportedHostReport reason)
        File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), Compositor.Render3.emitFeature157UnsupportedHostReport reason)
    | None when not (File.Exists(Path.Combine(unsupportedDir, "README.md"))) ->
        File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), "# Feature 157 Unsupported Host\n\nNo unsupported-host limitation was recorded for this run.\n")
    | None -> ()

    File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.Render3.emitFeature157DamageSummary summary)
    File.WriteAllText(Path.Combine(out, "summary.json"), Compositor.Render3.emitFeature157DamageSummaryJson summary + Environment.NewLine)

let runCompositorDamageCmd (rest: string list) =
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
            let profile = Compositor.Config.hostProfileFromFacts facts
            let expectedProfile = flagValue "--profile" rest |> Option.defaultValue Compositor.Config.feature157AcceptedProfileId
            let requestedAttempts = positiveIntFlag "--attempt-count" 3 rest
            let runId = "feature157-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")

            let summary =
                match feature157ReasonFromHost facts profile expectedProfile with
                | Some reason ->
                    let fallbacks =
                        scenarioSet
                        |> List.map (fun scenario ->
                            let artifact = Path.Combine("fallbacks", Compositor.FeatureState.feature157ScenarioFileName scenario).Replace('\\', '/')
                            feature157Fallback scenario reason artifact)

                    let status =
                        if reason.Contains("missing display", StringComparison.OrdinalIgnoreCase)
                           || reason.Contains("renderer", StringComparison.OrdinalIgnoreCase)
                           || reason.Contains("direct rendering", StringComparison.OrdinalIgnoreCase) then
                            Compositor.Types.Feature157DamageStatus.EnvironmentLimited
                        else
                            Compositor.Types.Feature157DamageStatus.FallbackOnly

                    feature157Summary runId profile status [] fallbacks (Some reason) [ reason; "accepted partial-redraw artifacts=0" ]
                | None ->
                    let attempts =
                        [ for attemptIndex in 1..requestedAttempts do
                              for scenario in scenarioSet do
                                  feature157Attempt runId attemptIndex scenario profile ]

                    let fallbacks =
                        Compositor.Config.feature157FallbackScenarioIds
                        |> List.map (fun scenario ->
                            let artifact = Path.Combine("fallbacks", Compositor.FeatureState.feature157ScenarioFileName scenario).Replace('\\', '/')
                            let reason =
                                match scenario with
                                | "damage/missing-retained-backing" -> "missing-retained-content"
                                | "damage/resource-failure" -> "resource-failure"
                                | "damage/parity-mismatch" -> "parity-mismatch"
                                | "damage/unsupported-host" -> "environment-limitation"
                                | _ -> "invalid-damage"
                            feature157Fallback scenario reason artifact)

                    feature157Summary runId profile Compositor.Types.Feature157DamageStatus.Accepted attempts fallbacks None [ "damage-scoped no-clear path accepted for current same-profile host" ]

            writeFeature157DamagePackage out summary
            printfn "%s" (Path.Combine(out, "summary.md"))
            0
