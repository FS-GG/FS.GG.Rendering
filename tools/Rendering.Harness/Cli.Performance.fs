module Rendering.Harness.CliPerformance

open System
open System.IO
open Rendering.Harness
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open Rendering.Harness.CliShared
open Rendering.Harness.CliFeatureBuilders

let feature156Performance (rest: string list) =
    if not (isFeature156 rest) then
        eprintfn "compositor-performance requires --feature 156"
        2
    else
        let policy = flagValue "--policy" rest |> Option.defaultValue Compositor.Config.feature156PolicyId
        let requestedScenario = flagValue "--scenario" rest
        let scenarioSet =
            match requestedScenario with
            | Some scenario when Compositor.Config.feature156ScenarioIds |> List.contains scenario -> [ scenario ]
            | Some scenario ->
                eprintfn "unknown Feature 156 scenario: %s" scenario
                []
            | None -> Compositor.Config.feature156RequiredScenarioIds

        if policy <> Compositor.Config.feature156PolicyId then
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
                | None -> Compositor.Config.feature156TimingDirectory

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
            let profile = Compositor.Config.hostProfileFromFacts facts
            let expectedProfile = flagValue "--profile" rest |> Option.defaultValue Compositor.Config.feature156AcceptedProfileId
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

            let failClosedReports (verdict: Compositor.Types.Feature156ScenarioVerdict) reason : Compositor.Types.Feature156ScenarioReport list =
                scenarioSet
                |> List.map (fun scenario ->
                    { ScenarioId = scenario
                      FullRedraw = None
                      DamageScoped = None
                      WarmupCount = warmup
                      MeasuredRepetitions = repetitions
                      NoiseBandMs = 0.0
                      Verdict = verdict
                      ConfidenceDecision = Compositor.FeatureState.feature156VerdictToken verdict
                      ArtifactPaths = [ Path.Combine("scenarios", Compositor.FeatureState.feature156ScenarioFileName scenario).Replace('\\', '/') ]
                      RejectionReasons = [ reason ]
                      ProofOverheadIncluded = false })

            let reports, diagnostics =
                match facts.EffectiveBackend, facts.GlRenderer, facts.GlDirect, profile.ProfileId = expectedProfile with
                | NoDisplay, _, _, _ ->
                    let reason = "missing display"
                    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.Render3.emitFeature156UnsupportedHostReport reason)
                    failClosedReports Compositor.Types.Feature156EnvironmentLimited reason, [ reason; "accepted performance artifacts=0" ]
                | _, None, _, _ ->
                    let reason = "missing GL renderer facts"
                    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.Render3.emitFeature156UnsupportedHostReport reason)
                    failClosedReports Compositor.Types.Feature156EnvironmentLimited reason, [ reason; "accepted performance artifacts=0" ]
                | _, _, false, _ ->
                    let reason = "OpenGL direct rendering is unavailable"
                    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.Render3.emitFeature156UnsupportedHostReport reason)
                    failClosedReports Compositor.Types.Feature156EnvironmentLimited reason, [ reason; "accepted performance artifacts=0" ]
                | _, _, _, false ->
                    let reason = $"profile mismatch: expected={expectedProfile} actual={profile.ProfileId}"
                    failClosedReports Compositor.Types.Feature156Rejected reason, [ reason ]
                | _ ->
                    let reports : Compositor.Types.Feature156ScenarioReport list =
                        scenarioSet
                        |> List.map (fun scenario ->
                            let fullRaw, full = runFeature156Path rawDir scenario "full-redraw" warmup repetitions runId profile.ProfileId hostFacts
                            let damageRaw, damage = runFeature156Path rawDir scenario "damage-scoped" warmup repetitions runId profile.ProfileId hostFacts
                            let decision = Perf.evaluateScenario repetitions full damage
                            let verdict = feature156VerdictFromPerf decision.Verdict
                            let scenarioPath = Path.Combine("scenarios", Compositor.FeatureState.feature156ScenarioFileName scenario).Replace('\\', '/')
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
                let path = Path.Combine(scenariosDir, Compositor.FeatureState.feature156ScenarioFileName report.ScenarioId)
                File.WriteAllText(path, Compositor.Render3.emitFeature156ScenarioReport report)

            let summary = feature156SummaryFromReports runId profile warmup repetitions reports diagnostics
            File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.Render3.emitFeature156TimingSummary summary)

            if rest |> List.contains "--json" then
                let json =
                    [ "{"
                      $"  \"runId\": \"{summary.RunId}\","
                      $"  \"profileId\": \"{profile.ProfileId}\","
                      $"  \"policyId\": \"{summary.PolicyId}\","
                      $"  \"overallVerdict\": \"{Compositor.FeatureState.feature156VerdictToken summary.OverallVerdict}\","
                      $"  \"shippedPerformanceClaim\": \"{summary.ShippedPerformanceClaim}\""
                      "}" ]
                    |> String.concat Environment.NewLine

                File.WriteAllText(Path.Combine(out, "summary.json"), json + Environment.NewLine)

            printfn "%s" (Path.Combine(out, "summary.md"))
            0

let feature158Performance (rest: string list) =
    let policy = flagValue "--policy" rest |> Option.defaultValue Compositor.Config.feature158PolicyId
    let requestedScenario = flagValue "--scenario" rest
    let scenarioSet =
        match requestedScenario with
        | Some scenario when Compositor.Config.feature158ScenarioIds |> List.contains scenario -> [ scenario ]
        | Some scenario ->
            eprintfn "unknown Feature 158 scenario: %s" scenario
            []
        | None -> Compositor.Config.feature158RequiredScenarioIds

    if policy <> Compositor.Config.feature158PolicyId then
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
            | None -> Compositor.Config.feature158TimingDirectory

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
        let profile = Compositor.Config.hostProfileFromFacts facts
        let expectedProfile = flagValue "--profile" rest |> Option.defaultValue Compositor.Config.feature158AcceptedProfileId
        let runId = "feature158-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
        let hostReason = feature158ReasonFromHost facts profile expectedProfile

        let reports, proofProbeEvidence, unsupported, diagnostics =
            match probeReadback, hostReason with
            | true, Some reason ->
                let reports : Compositor.Types.Feature158ScenarioReport list =
                    scenarioSet
                    |> List.map (fun scenario ->
                        feature158FailClosedReport scenario warmup repetitions Compositor.Types.Feature158ReadinessStatus.EnvironmentLimited Perf.EnvironmentLimitedReason)

                File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.Render3.emitFeature158UnsupportedHostReport reason)
                File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), Compositor.Render3.emitFeature158UnsupportedHostReport reason)
                reports, [], Some reason, [ reason; "explicit probe readback unavailable"; "accepted proof artifacts=0"; "accepted performance artifacts=0" ]
            | false, Some reason when reason.Contains("profile mismatch", StringComparison.OrdinalIgnoreCase) ->
                let reports : Compositor.Types.Feature158ScenarioReport list =
                    scenarioSet
                    |> List.map (fun scenario ->
                        feature158FailClosedReport scenario warmup repetitions Compositor.Types.Feature158ReadinessStatus.Rejected Perf.CrossProfileEvidence)

                reports, [], None, [ reason; "accepted performance artifacts=0" ]
            | false, Some reason ->
                let reports : Compositor.Types.Feature158ScenarioReport list =
                    scenarioSet
                    |> List.map (fun scenario ->
                        feature158FailClosedReport scenario warmup repetitions Compositor.Types.Feature158ReadinessStatus.EnvironmentLimited Perf.EnvironmentLimitedReason)

                File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.Render3.emitFeature158UnsupportedHostReport reason)
                File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), Compositor.Render3.emitFeature158UnsupportedHostReport reason)
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
                        Compositor.Config.feature158ProbeCommand
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
                            Compositor.Config.feature156PackageVersion
                            Perf.ProbeReadbackIncluded
                            Perf.Probe
                            (Some Perf.ProbeRunExcluded)
                            0.0
                            (Path.Combine("..", "proof-probes", readbackFileName).Replace('\\', '/')))

                let evidence : Compositor.Types.Feature158ProofProbeEvidence =
                    { ProbeId = runId + "-probe"
                      HostProfile = profile
                      ScenarioIds = scenarioSet
                      ReadbackArtifacts =
                        [ Path.Combine("proof-probes", readbackFileName).Replace('\\', '/')
                          $"probe-status={probeResult.Status}" ]
                      ProbeSampleIds = probeSamples |> List.map _.SampleId
                      ExclusionReason = Perf.ProbeRunExcluded
                      Diagnostics = [ "explicit probe readback path"; "accepted performance samples=0" ] }

                let reports : Compositor.Types.Feature158ScenarioReport list =
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
                            Compositor.Types.Feature158ReadinessStatus.FallbackOnly
                            [ Path.Combine("scenarios", Compositor.FeatureState.feature158ScenarioFileName scenario).Replace('\\', '/') ]
                            [ "probe-readback-included"; "probe-run-excluded"; "accepted performance samples=0" ])

                reports, [ evidence ], None, [ "explicit probe readback completed"; "accepted performance artifacts=0" ]
            | false, None ->
                let reports : Compositor.Types.Feature158ScenarioReport list =
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
                            [ Path.Combine("scenarios", Compositor.FeatureState.feature158ScenarioFileName scenario).Replace('\\', '/')
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

let feature160AttemptsFlag (rest: string list) =
    match flagValue "--attempts" rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> Compositor.Config.feature160RequiredAttempts
    | None -> Compositor.Config.feature160RequiredAttempts

let feature160MaxIterationMinutesFlag (rest: string list) =
    match flagValue "--max-iteration-minutes" rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> Compositor.Config.feature160MaxIterationMinutes
    | None -> Compositor.Config.feature160MaxIterationMinutes

let feature160ScenarioSet (rest: string list) =
    match flagValue "--scenario" rest with
    | Some scenario when Compositor.Config.feature160RequiredScenarioIds |> List.contains scenario -> [ scenario ], Some scenario, None
    | Some scenario when Compositor.Config.feature160ScenarioIds |> List.contains scenario -> [ scenario ], Some scenario, None
    | Some scenario -> [], None, Some $"unknown Feature 160 scenario: {scenario}"
    | None -> Compositor.Config.feature160RequiredScenarioIds, None, None

let feature160ReasonFromHost facts profile expectedProfile =
    feature158ReasonFromHost facts profile expectedProfile

let feature160ExclusionReasonFromHostReason (reason: string) =
    if reason.Contains("profile mismatch", StringComparison.OrdinalIgnoreCase) then
        Perf.CrossProfileEvidence
    elif reason.Contains("missing display", StringComparison.OrdinalIgnoreCase) then
        Perf.EnvironmentLimitedReason
    elif reason.Contains("renderer", StringComparison.OrdinalIgnoreCase)
         || reason.Contains("direct rendering", StringComparison.OrdinalIgnoreCase) then
        Perf.UnsupportedHost
    else
        Perf.EnvironmentLimitedReason

let feature160Iteration
    (runId: string)
    (iterationIndex: int)
    (profile: Compositor.Types.HostProfile)
    (boundMinutes: int)
    (warmup: int)
    (repetitions: int)
    (reports: Compositor.Types.Feature158ScenarioReport list)
    (status: Compositor.Types.Feature160ReadinessStatus)
    (reason: Perf.ExclusionReason option)
    (restrictedScenario: string option)
    (diagnostics: string list)
    : Compositor.Types.Feature160Iteration =
    let iterationId = sprintf "%s-%03i" runId iterationIndex
    let included = reports |> List.collect _.IncludedSamples
    let excluded = reports |> List.collect _.ExcludedSamples
    let coverage =
        reports
        |> List.filter (fun report -> report.Status = Compositor.Types.Feature158ReadinessStatus.Accepted)
        |> List.map _.ScenarioId

    { IterationId = iterationId
      RunId = runId
      HostProfile = profile
      LaneId = Compositor.Config.feature160FocusedLaneId
      PolicyId = Compositor.Config.feature160PolicyId
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
        [ Path.Combine("throughput", "iterations", Compositor.FeatureState.feature160IterationFileName iterationId).Replace('\\', '/')
          Path.Combine("throughput", "raw", $"{iterationId}.csv").Replace('\\', '/') ]
      RestrictedScenario = restrictedScenario
      Diagnostics = diagnostics }

let feature160Summary
    (runId: string)
    (profile: Compositor.Types.HostProfile)
    (bound: int)
    (attempts: int)
    (warmup: int)
    (repetitions: int)
    (iterations: Compositor.Types.Feature160Iteration list)
    (unsupported: string option)
    (fullValidation: Compositor.Types.Feature160FullValidationRecord option)
    (packageStatus: string)
    (regressionStatus: string)
    (diagnostics: string list)
    : Compositor.Types.Feature160ThroughputSummary =
    let provisional : Compositor.Types.Feature160ThroughputSummary =
        { RunId = runId
          HostProfile = profile
          LaneId = Compositor.Config.feature160FocusedLaneId
          PolicyId = Compositor.Config.feature160PolicyId
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
          Status = Compositor.Types.Feature160ReadinessStatus.FallbackOnly
          ReleaseReadyStatus = "blocked"
          PerformanceClaim = "performance-not-accepted"
          Diagnostics = diagnostics }

    let status = Compositor.FeatureState.feature160OverallStatus provisional
    { provisional with
        Status = status
        ReleaseReadyStatus = if status = Compositor.Types.Feature160ReadinessStatus.Accepted then "ready" else "blocked" }

let writeFeature160RawIteration rawDir (iteration: Compositor.Types.Feature160Iteration) =
    Directory.CreateDirectory(rawDir) |> ignore
    let path = Path.Combine(rawDir, $"{iteration.IterationId}.csv")
    let sampleRows =
        iteration.IncludedSamples @ iteration.ExcludedSamples
        |> List.map (fun sample ->
            let reason = sample.ExclusionReason |> Option.map Perf.exclusionReasonToken |> Option.defaultValue ""
            $"{sample.SampleIndex},{sample.SampleId},{sample.ScenarioId},{Perf.timingPathToken sample.Path},{sample.RunId},{sample.HostProfileId},{sample.PackageVersion},{sample.ScenarioDefinitionId},{feature156FormatMs sample.DurationMs},{Perf.measurementPolicyToken sample.MeasurementPolicy},{Perf.inclusionStatusToken sample.InclusionStatus},{reason},{sample.ArtifactPath}")

    let header = "sample-index,sample-id,scenario-id,path,run-id,host-profile-id,package-version,scenario-definition-id,duration-ms,measurement-policy,inclusion-status,exclusion-reason,artifact-path"
    File.WriteAllText(path, String.concat Environment.NewLine (header :: sampleRows) + Environment.NewLine)

let writeFeature160ThroughputPackage out (summary: Compositor.Types.Feature160ThroughputSummary) =
    let iterationsDir = Path.Combine(out, "iterations")
    let rawDir = Path.Combine(out, "raw")
    let excludedDir = Path.Combine(out, "excluded")
    let unsupportedDir =
        let leaf = out.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) |> Path.GetFileName
        if String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then out else Path.Combine(out, "unsupported")

    [ iterationsDir; rawDir; excludedDir; unsupportedDir ]
    |> List.iter (Directory.CreateDirectory >> ignore)

    for iteration in summary.Iterations do
        File.WriteAllText(Path.Combine(iterationsDir, Compositor.FeatureState.feature160IterationFileName iteration.IterationId), Compositor.Render4.emitFeature160IterationReport iteration)
        writeFeature160RawIteration rawDir iteration

    summary.Iterations
    |> List.groupBy (fun iteration -> iteration.ExclusionReason |> Option.defaultValue Perf.MissingMetadata)
    |> List.iter (fun (reason, iterations) ->
        if iterations |> List.exists (fun iteration -> iteration.ExclusionReason.IsSome) then
            File.WriteAllText(Path.Combine(excludedDir, Perf.exclusionReasonToken reason + ".md"), Compositor.Render4.emitFeature160ExcludedEvidenceReport reason iterations))

    if not (File.Exists(Path.Combine(excludedDir, "README.md"))) then
        File.WriteAllText(Path.Combine(excludedDir, "README.md"), "# Feature 160 Excluded Evidence\n\nGrouped excluded iterations are written by primary reason.\n")

    let unsupportedReport =
        Compositor.Render4.emitFeature160UnsupportedHostReport (summary.UnsupportedHostReason |> Option.defaultValue "not run in this invocation")
    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), unsupportedReport)
    File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), unsupportedReport)
    if summary.UnsupportedHostReason.IsSome || String.Equals(Path.GetFileName(out.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), "unsupported", StringComparison.OrdinalIgnoreCase) then
        File.WriteAllText(Path.Combine(out, "validation.md"), unsupportedReport)

    File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.Render4.emitFeature160ThroughputSummary summary)
    File.WriteAllText(Path.Combine(out, "summary.json"), Compositor.Render4.emitFeature160ThroughputSummaryJson summary + Environment.NewLine)

let feature160Performance (rest: string list) =
    let lane = flagValue "--lane" rest |> Option.defaultValue Compositor.Config.feature160FocusedLaneId
    let policy = flagValue "--policy" rest |> Option.defaultValue Compositor.Config.feature160PolicyId

    if lane <> Compositor.Config.feature160FocusedLaneId then
        eprintfn "compositor-performance --feature 160 requires --lane %s" Compositor.Config.feature160FocusedLaneId
        2
    elif policy <> Compositor.Config.feature160PolicyId then
        eprintfn "compositor-performance --feature 160 requires --policy %s" Compositor.Config.feature160PolicyId
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
                | None -> Compositor.Config.feature160ThroughputDirectory

            Directory.CreateDirectory(out) |> ignore
            let facts = Probe.probe ()
            let profile = Compositor.Config.hostProfileFromFacts facts
            let expectedProfile = flagValue "--profile" rest |> Option.defaultValue Compositor.Config.feature160AcceptedProfileId
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
                    let reports : Compositor.Types.Feature158ScenarioReport list =
                        scenarioSet
                        |> List.map (fun scenario ->
                            feature158FailClosedReport scenario warmup repetitions Compositor.Types.Feature158ReadinessStatus.EnvironmentLimited exclusionReason)

                    [ feature160Iteration
                          runId
                          1
                          profile
                          bound
                          warmup
                          repetitions
                          reports
                          Compositor.Types.Feature160ReadinessStatus.EnvironmentLimited
                          (Some exclusionReason)
                          restrictedScenario
                          [ reason; "accepted same-profile performance artifacts=0" ] ]
                | None ->
                    [ for attempt in 1..attempts ->
                          let attemptRunId = sprintf "%s-%03i" runId attempt
                          let reports : Compositor.Types.Feature158ScenarioReport list =
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
                                          Compositor.Types.Feature158ReadinessStatus.Rejected
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
                                  Compositor.Types.Feature160ReadinessStatus.Rejected, Some Perf.ScenarioCoverageMissing
                              elif reports |> List.forall (fun report -> report.Status = Compositor.Types.Feature158ReadinessStatus.Accepted)
                                   && bound = Compositor.Config.feature160MaxIterationMinutes
                                   && warmup = 3
                                   && repetitions = 5 then
                                  Compositor.Types.Feature160ReadinessStatus.Accepted, None
                              else
                                  Compositor.Types.Feature160ReadinessStatus.Rejected, Some Perf.SamplePolicyMismatch

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
                    if iterations |> List.exists (fun iteration -> iteration.Status = Compositor.Types.Feature160ReadinessStatus.EnvironmentLimited) then
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
    (profile: Compositor.Types.HostProfile)
    : Compositor.Types.Feature161HostFacts =
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
      TimingPolicyIdentity = Compositor.Config.feature161PolicyId
      CollectionTime = DateTimeOffset.UtcNow
      ArtifactLocations =
        [ Path.Combine("lane-ledger", "host-facts", "facts-" + runId + ".md").Replace('\\', '/')
          Path.Combine("lane-ledger", "entries", "entry-" + runId + ".md").Replace('\\', '/')
          sourceThroughput.Replace('\\', '/') ] }

let feature161EntryFromFacts
    (entryId: string)
    (facts: Compositor.Types.Feature161HostFacts)
    (expectedProfile: string)
    : Compositor.Types.Feature161LedgerEntry =
    let hostReason = Compositor.FeatureState.feature161ValidateHostFacts facts
    let profileReason =
        if facts.HostProfile.ProfileId <> expectedProfile then
            Some Perf.CrossProfileEvidence
        else
            None

    let reason = hostReason |> Option.orElse profileReason
    let laneId = Compositor.FeatureState.feature161LaneIdFromFacts facts
    let status =
        match reason with
        | None when laneId = Compositor.Config.feature161HostLaneId -> Compositor.Types.Feature161ReadinessStatus.Accepted
        | Some Perf.MissingDisplay
        | Some Perf.IndirectRendering
        | Some Perf.SoftwareRaster
        | Some Perf.UnknownRenderer
        | Some Perf.VirtualizedPresentation -> Compositor.Types.Feature161ReadinessStatus.EnvironmentLimited
        | Some _ -> Compositor.Types.Feature161ReadinessStatus.Rejected
        | None -> Compositor.Types.Feature161ReadinessStatus.Rejected

    let acceptedArtifacts =
        if status = Compositor.Types.Feature161ReadinessStatus.Accepted then 1 else 0

    let primaryReason =
        match reason with
        | Some reason -> Some reason
        | None when laneId <> Compositor.Config.feature161HostLaneId -> Some Perf.CrossLaneEvidence
        | None -> None

    { EntryId = entryId
      LaneId = laneId
      HostFacts = facts
      PriorGates = Compositor.Config.feature161PriorGateLinks
      Status = status
      PrimaryExclusionReason = primaryReason
      TimingStatus = if acceptedArtifacts > 0 then "lane-scoped" else "not-accepted"
      AcceptedLaneScopedPerformanceArtifacts = acceptedArtifacts
      ArtifactPaths =
        [ Path.Combine("lane-ledger", "entries", Compositor.FeatureState.feature161LedgerEntryFileName entryId).Replace('\\', '/')
          Path.Combine("lane-ledger", "host-facts", Compositor.FeatureState.feature161HostFactsFileName entryId).Replace('\\', '/') ]
      Diagnostics =
        [ match primaryReason with
          | Some reason -> $"primary-reason={Perf.exclusionReasonToken reason}"
          | None -> "host-lane facts complete for accepted lane"
          "performance-not-accepted preserved" ] }

let feature161Summary
    (runId: string)
    (profile: Compositor.Types.HostProfile)
    (entries: Compositor.Types.Feature161LedgerEntry list)
    (unsupported: string option)
    (fullValidationStatus: string)
    (packageStatus: string)
    (regressionStatus: string)
    (diagnostics: string list)
    : Compositor.Types.Feature161Summary =
    let scope = Compositor.FeatureState.feature161ScopeFromEntries entries
    let provisional : Compositor.Types.Feature161Summary =
        { RunId = runId
          HostProfile = profile
          PolicyId = Compositor.Config.feature161PolicyId
          Entries = entries
          UnsupportedHostReason = unsupported
          ClaimScope = scope
          FullValidationStatus = fullValidationStatus
          CompatibilityImpact = "Feature161HostLaneReadiness helper added; runtime rendering behavior unchanged"
          PackageValidationStatus = packageStatus
          RegressionValidationStatus = regressionStatus
          Status = Compositor.Types.Feature161ReadinessStatus.FallbackOnly
          ReleaseReadyStatus = "blocked"
          PerformanceClaim = "performance-not-accepted"
          Diagnostics = diagnostics }

    let status = Compositor.FeatureState.feature161OverallStatus provisional
    { provisional with
        Status = status
        ReleaseReadyStatus = if status = Compositor.Types.Feature161ReadinessStatus.Accepted then "ready" else "blocked" }

let writeFeature161LaneLedgerPackage out (summary: Compositor.Types.Feature161Summary) =
    let entriesDir = Path.Combine(out, "entries")
    let hostFactsDir = Path.Combine(out, "host-facts")
    let excludedDir = Path.Combine(out, "excluded")
    let unsupportedDir =
        let leaf = out.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) |> Path.GetFileName
        if String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then out else Path.Combine(out, "unsupported")

    [ entriesDir; hostFactsDir; excludedDir; unsupportedDir ]
    |> List.iter (Directory.CreateDirectory >> ignore)

    for entry in summary.Entries do
        File.WriteAllText(Path.Combine(entriesDir, Compositor.FeatureState.feature161LedgerEntryFileName entry.EntryId), Compositor.Render4.emitFeature161LedgerEntry entry)
        File.WriteAllText(Path.Combine(hostFactsDir, Compositor.FeatureState.feature161HostFactsFileName entry.EntryId), Compositor.Render4.emitFeature161HostFacts entry.HostFacts)

    summary.Entries
    |> List.groupBy (fun entry -> entry.PrimaryExclusionReason |> Option.defaultValue Perf.HostFactsMissing)
    |> List.iter (fun (reason, entries) ->
        if entries |> List.exists (fun entry -> entry.PrimaryExclusionReason.IsSome) then
            File.WriteAllText(Path.Combine(excludedDir, Perf.exclusionReasonToken reason + ".md"), Compositor.Render4.emitFeature161ExcludedEvidenceReport reason entries))

    if not (File.Exists(Path.Combine(excludedDir, "README.md"))) then
        File.WriteAllText(Path.Combine(excludedDir, "README.md"), "# Feature 161 Excluded Evidence\n\nGrouped excluded lane entries are written by primary reason.\n")

    let unsupportedReport =
        Compositor.Render4.emitFeature161UnsupportedHostReport (summary.UnsupportedHostReason |> Option.defaultValue "not run in this invocation")
    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), unsupportedReport)
    File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), unsupportedReport)

    File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.Render4.emitFeature161LaneLedgerSummary summary)
    File.WriteAllText(Path.Combine(out, "summary.json"), Compositor.Render4.emitFeature161LaneLedgerSummaryJson summary + Environment.NewLine)

let feature161Performance (rest: string list) =
    let lane = flagValue "--lane" rest |> Option.defaultValue "host-ledger"
    let policy = flagValue "--policy" rest |> Option.defaultValue Compositor.Config.feature161PolicyId

    if lane <> "host-ledger" then
        eprintfn "compositor-performance --feature 161 requires --lane host-ledger"
        2
    elif policy <> Compositor.Config.feature161PolicyId then
        eprintfn "compositor-performance --feature 161 requires --policy %s" Compositor.Config.feature161PolicyId
        2
    else
        let out =
            match flagValue "--out" rest with
            | Some d -> d
            | None -> Compositor.Config.feature161LaneLedgerDirectory

        let sourceThroughput =
            flagValue "--source-throughput" rest
            |> Option.defaultValue Compositor.Config.feature160ThroughputDirectory

        Directory.CreateDirectory(out) |> ignore
        let facts = Probe.probe ()
        let profile = Compositor.Config.hostProfileFromFacts facts
        let expectedProfile = flagValue "--profile" rest |> Option.defaultValue Compositor.Config.feature161AcceptedProfileId
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

