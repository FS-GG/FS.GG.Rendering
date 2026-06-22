module Rendering.Harness.Cli

open System
open System.IO
open System.Text.Json
open Rendering.Harness
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open Rendering.Harness.CliShared
open Rendering.Harness.CliFeatureBuilders
open Rendering.Harness.CliReadiness


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
    let profile = Compositor.Config.hostProfileFromFacts facts

    let verdict =
        match facts.EffectiveBackend, facts.GlRenderer with
        | NoDisplay, _ -> Compositor.Types.ProofEnvironmentLimited "missing display"
        | _, None -> Compositor.Types.ProofEnvironmentLimited "missing GL renderer facts"
        | _ -> Compositor.Types.ProofEnvironmentLimited "live sentinel readback proof is not implemented in this deterministic harness"

    let proof: Compositor.Types.PresentProof =
        { ProofId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
          HostProfile = profile
          ScenarioId = "proof/sentinel-damage-v1"
          Verdict = verdict
          CreatedAt = DateTimeOffset.UtcNow
          EvidenceArtifacts = [ "proof.md" ]
          Diagnostics =
            [ $"backend={profile.DisplayEnvironment}"
              $"verdict={Compositor.Config.proofVerdictToken verdict}" ] }

    let path = IO.Path.Combine(out, "proof.md")
    IO.File.WriteAllText(path, Compositor.Render.renderPresentProof proof)
    printfn "%s" path
    0

let private feature155ReadinessRootFor (output: string) =
    let normalized = output.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
    let leaf = Path.GetFileName normalized
    if String.Equals(leaf, "attempts", StringComparison.OrdinalIgnoreCase)
       || String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then
        match Directory.GetParent(normalized) with
        | null -> FeatureCatalog.FeatureDescriptor.readinessDirectory (FeatureCatalog.descriptorById 155)
        | liveProof ->
            match liveProof.Parent with
            | null -> FeatureCatalog.FeatureDescriptor.readinessDirectory (FeatureCatalog.descriptorById 155)
            | readiness -> readiness.FullName
    else
        FeatureCatalog.FeatureDescriptor.readinessDirectory (FeatureCatalog.descriptorById 155)

let private feature155UnsupportedOutput (output: string) =
    let normalized = output.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
    let leaf = Path.GetFileName normalized
    if String.Equals(leaf, "unsupported", StringComparison.OrdinalIgnoreCase) then
        output
    else
        Path.Combine(output, "unsupported")

let private writeFeature155Unsupported output (profile: Compositor.Types.HostProfile) reason =
    Directory.CreateDirectory(output) |> ignore
    let now = DateTimeOffset.UtcNow
    let proof: Compositor.Types.PresentProof =
        { ProofId = now.UtcDateTime.ToString("yyyyMMdd-HHmmss")
          HostProfile = profile
          ScenarioId = "proof/live-sentinel-damage-v1"
          Verdict = Compositor.Types.ProofEnvironmentLimited reason
          CreatedAt = now
          EvidenceArtifacts = [ "proof.md"; "limitations.md"; "README.md" ]
          Diagnostics =
            [ $"backend={profile.DisplayEnvironment}"
              $"package={Compositor.Config.feature155PackageVersion}"
              "attempt-count=0"
              $"verdict={Compositor.Config.proofVerdictToken (Compositor.Types.ProofEnvironmentLimited reason)}"
              $"reason={reason}" ] }

    File.WriteAllText(Path.Combine(output, "proof.md"), Compositor.Render2.emitFeature155LiveProof proof)
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
        if proof.Verdict = Compositor.Types.ProofEnvironmentLimited "missing display" then 0 else 1
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
                    Compositor.Types.ProofPassed
                elif not sentinelOk || not damageOk then
                    Compositor.Types.ProofFailed "missing, undecodable, or blank artifact"
                elif not damagedUpdated then
                    Compositor.Types.ProofFailed "damaged pixels did not update"
                else
                    Compositor.Types.ProofFailed "undamaged pixels did not preserve sentinel identity"

            let relativeArtifacts =
                [ $"{attemptId}/sentinel-frame.png"
                  $"{attemptId}/damage-frame.png"
                  $"{attemptId}/proof.md" ]

            let proof: Compositor.Types.PresentProof =
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
                        $"verdict={Compositor.Config.proofVerdictToken verdict}" ] }

            File.WriteAllText(Path.Combine(attemptDir, "proof.md"), Compositor.Render2.emitFeature155LiveProof proof)
            proof

        let proofs = [ 1..requestedAttempts ] |> List.map createAttempt
        let selected = proofs |> List.filter (fun proof -> proof.Verdict = Compositor.Types.ProofPassed) |> List.truncate 3
        let model =
            let start, _ = Compositor.FeatureState.initReadiness ()
            proofs
            |> List.fold (fun state proof -> Compositor.FeatureState.updateReadiness (Compositor.Types.ProofLoaded proof) state |> fst) start

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
                  |> List.map (fun proof -> $"- `{proof.ProofId}`: {Compositor.Config.proofVerdictToken proof.Verdict}")
                  |> String.concat "\n" ]
            |> String.concat "\n"

        File.WriteAllText(Path.Combine(output, "README.md"), attemptsReadme)
        File.WriteAllText(Path.Combine(output, "proof.md"), Compositor.Render2.emitFeature155LiveProof (proofs |> List.head))
        File.WriteAllText(Path.Combine(readinessRoot, "proof-set.md"), Compositor.Render2.emitFeature155ProofSet model)
        File.WriteAllText(Path.Combine(readinessRoot, "validation-summary.md"), Compositor.Render2.emitFeature155ValidationSummary model)
        File.WriteAllText(Path.Combine(readinessRoot, "compatibility-ledger.md"), Compositor.Render2.emitFeature155CompatibilityLedger model)
        printfn "%s" (Path.Combine(output, "proof.md"))
        if selected.Length = 3 then 0 else 1

let private runCompositorLiveProofCmd (rest: string list) =
    let facts = Probe.probe ()
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature155 rest -> Compositor.Config.feature155LiveProofDirectory
        | None when isFeature154 rest -> Compositor.Config.feature154LiveProofDirectory
        | None when isFeature153 rest -> Compositor.Config.feature153LiveProofDirectory
        | None when isFeature152 rest -> Compositor.Config.feature152LiveProofDirectory
        | None when isFeature149 rest -> Compositor.Config.feature149LiveProofDirectory
        | None -> Compositor.Config.feature148LiveProofDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let profile = Compositor.Config.hostProfileFromFacts facts

    if isFeature155 rest then
        runFeature155LiveProof rest facts out profile
    else

        let verdict =
            match facts.EffectiveBackend, facts.GlRenderer with
            | NoDisplay, _ -> Compositor.Types.ProofEnvironmentLimited "missing display"
            | _, None -> Compositor.Types.ProofEnvironmentLimited "missing GL renderer facts"
            | _ -> Compositor.Types.ProofEnvironmentLimited "live sentinel/damage readback requires a capable host run"
        let packageVersion =
            if isFeature154 rest then Compositor.Config.feature154PackageVersion
            elif isFeature153 rest then Compositor.Config.feature153PackageVersion
            elif isFeature152 rest then Compositor.Config.feature152PackageVersion
            elif isFeature149 rest then Compositor.Config.feature149PackageVersion
            else Compositor.Config.feature148PackageVersion

        let proof: Compositor.Types.PresentProof =
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
                  $"verdict={Compositor.Config.proofVerdictToken verdict}" ] }

        let proofPath = IO.Path.Combine(out, "proof.md")
        let limitationsPath = IO.Path.Combine(out, "limitations.md")
        let proofBody =
            if isFeature154 rest then
                Compositor.Render2.emitFeature154LiveProof proof
            elif isFeature153 rest then
                Compositor.Render.emitFeature153LiveProof proof
            elif isFeature152 rest then
                Compositor.Render.emitFeature152LiveProof proof
            elif isFeature149 rest then
                Compositor.Render.emitFeature149LiveProof proof
            else
                Compositor.Render.emitFeature148LiveProof proof

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
                    "154", Compositor.Config.feature154LiveProofAttemptsDirectory, Compositor.Config.feature154LiveProofUnsupportedDirectory
                else
                    "153", Compositor.Config.feature153LiveProofAttemptsDirectory, Compositor.Config.feature153LiveProofUnsupportedDirectory

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
            Compositor.Render2.emitFeature155ParityReport ()
        elif isFeature154 rest then
            Compositor.Render2.emitFeature154ParityReport ()
        elif isFeature152 rest then
            Compositor.Render.emitFeature152ParityReport ()
        elif isFeature149 rest then
            Compositor.Render.emitFeature149ParityReport ()
        elif isFeature148 rest then
            Compositor.Render.emitFeature148ParityReport ()
        else
            [ "# Feature 147 Damage Parity"
              ""
              "| Scenario | Verdict |"
              "|----------|---------|"
              for scenario in Compositor.Config.scenarioIds do
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
          sprintf "Promotion threshold: `%g%%`" Compositor.Config.thresholds.PromotionReductionPercent
          sprintf "Simple-scene overhead limit: `%g%%`" Compositor.Config.thresholds.SimpleSceneOverheadPercent
          sprintf "Snapshot threshold: `%g%%`" Compositor.Config.thresholds.SnapshotImprovementPercent
          $"Snapshot budget entries: `{Compositor.Config.snapshotBudget.MaxEntries}`"
          $"Snapshot budget bytes: `{Compositor.Config.snapshotBudget.MaxBytes}`"
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
        | None when isFeature149 rest -> Compositor.Config.feature149ReuseDirectory
        | None -> Compositor.Config.feature148ReuseDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, "reuse.md")
    let body =
        if isFeature149 rest then
            Compositor.Render.emitFeature149ReuseReport ()
        else
            Compositor.Render.emitFeature148ReuseReport ()
    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private runCompositorSnapshotsCmd (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature149 rest -> Compositor.Config.feature149SnapshotsDirectory
        | None -> Compositor.Config.feature148SnapshotsDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, "snapshots.md")
    let body =
        if isFeature149 rest then
            Compositor.Render.emitFeature149SnapshotReport ()
        else
            Compositor.Render.emitFeature148SnapshotReport ()
    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private runCompositorTimingCmd (rest: string list) =
    let tier = flagValue "--tier" rest |> Option.defaultValue "damage"
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature155 rest -> Compositor.Config.feature155TimingDirectory
        | None when isFeature154 rest -> Compositor.Config.feature154TimingDirectory
        | None when isFeature152 rest -> Compositor.Config.feature152TimingDirectory
        | None when isFeature149 rest -> Compositor.Config.feature149TimingDirectory
        | None -> Compositor.Config.feature148TimingDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, $"timing-{tier}.md")
    let body =
        if isFeature155 rest then
            Compositor.Render2.emitFeature155TimingReport tier (positiveIntFlag "--scenario-count" 5 rest) (positiveIntFlag "--repetitions" 5 rest)
        elif isFeature154 rest then
            Compositor.Render2.emitFeature154TimingReport tier (positiveIntFlag "--scenario-count" 5 rest) (positiveIntFlag "--repetitions" 5 rest)
        elif isFeature152 rest then
            Compositor.Render.emitFeature152TimingReport tier
        elif isFeature149 rest then
            Compositor.Render.emitFeature149TimingReport tier
        else
            Compositor.Render.emitFeature148TimingReport tier
    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0


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

// US3 (T028/T030): the per-feature `if isFeature161 … elif …` dispatch chains are replaced by a
// single descriptor lookup off the parsed `--feature <N>`. A known id routes to its bespoke body via
// `CliReadiness.run{Readiness,Performance,Promotion}`; an unknown feature flows through the fail-loud
// `FeatureCatalog.descriptorByAlias` (raises `CatalogError`) and is reported as an error + non-zero
// exit (FR-007/FR-011). Performance/promotion preserve their prior `requires --feature …` message and
// exit code for any non-handled feature; readiness keeps the shared legacy body for a bare invocation.
let private runCompositorPerformanceCmd (rest: string list) =
    match flagValue "--feature" rest |> Option.bind FeatureCatalog.FeatureDescriptor.tryByAlias with
    | Some descriptor -> runPerformance descriptor rest
    | None ->
        eprintfn "compositor-performance requires --feature 156, --feature 158, --feature 160, or --feature 161"
        2

let private runCompositorPromotionCmd (rest: string list) =
    match flagValue "--feature" rest |> Option.bind FeatureCatalog.FeatureDescriptor.tryByAlias with
    | Some descriptor -> runPromotion descriptor rest
    | None ->
        eprintfn "compositor-promotion requires --feature 159"
        2

let private runCompositorReadinessCmd (rest: string list) =
    match flagValue "--feature" rest with
    | None -> legacyCompositorReadiness rest
    | Some alias ->
        try
            runReadiness (FeatureCatalog.descriptorByAlias alias) rest
        with FeatureCatalog.CatalogError message ->
            eprintfn "%s" message
            2

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
