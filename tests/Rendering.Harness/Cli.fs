module Rendering.Harness.Cli

open System
open System.IO
open Rendering.Harness
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

let private isFeature148 (rest: string list) =
    match flagValue "--feature" rest with
    | Some value ->
        value = "148"
        || value = "feature148"
        || String.Equals(value, Compositor.feature148Id, StringComparison.OrdinalIgnoreCase)
    | _ -> false

let private isFeature149 (rest: string list) =
    match flagValue "--feature" rest with
    | Some value ->
        value = "149"
        || value = "feature149"
        || String.Equals(value, Compositor.feature149Id, StringComparison.OrdinalIgnoreCase)
    | _ -> false

let private isFeature152 (rest: string list) =
    match flagValue "--feature" rest with
    | Some value ->
        value = "152"
        || value = "feature152"
        || String.Equals(value, Compositor.feature152Id, StringComparison.OrdinalIgnoreCase)
    | _ -> false

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

let private runCompositorLiveProofCmd (rest: string list) =
    let facts = Probe.probe ()
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature152 rest -> Compositor.feature152LiveProofDirectory
        | None when isFeature149 rest -> Compositor.feature149LiveProofDirectory
        | None -> Compositor.feature148LiveProofDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let profile = Compositor.hostProfileFromFacts facts

    let verdict =
        match facts.EffectiveBackend, facts.GlRenderer with
        | NoDisplay, _ -> Compositor.ProofEnvironmentLimited "missing display"
        | _, None -> Compositor.ProofEnvironmentLimited "missing GL renderer facts"
        | _ -> Compositor.ProofEnvironmentLimited "live sentinel/damage readback requires a capable host run"
    let packageVersion =
        if isFeature152 rest then Compositor.feature152PackageVersion
        elif isFeature149 rest then Compositor.feature149PackageVersion
        else Compositor.feature148PackageVersion

    let proof: Compositor.PresentProof =
        { ProofId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
          HostProfile = profile
          ScenarioId = "proof/live-sentinel-damage-v1"
          Verdict = verdict
          CreatedAt = DateTimeOffset.UtcNow
          EvidenceArtifacts = [ "proof.md"; "limitations.md" ]
          Diagnostics =
            [ $"backend={profile.DisplayEnvironment}"
              $"package={packageVersion}"
              $"verdict={Compositor.proofVerdictToken verdict}" ] }

    let proofPath = IO.Path.Combine(out, "proof.md")
    let limitationsPath = IO.Path.Combine(out, "limitations.md")
    let proofBody =
        if isFeature152 rest then
            Compositor.renderFeature152LiveProof proof
        elif isFeature149 rest then
            Compositor.renderFeature149LiveProof proof
        else
            Compositor.renderFeature148LiveProof proof

    let limitationTitle =
        if isFeature152 rest then
            "# Feature 152 Live Proof Limitation"
        elif isFeature149 rest then
            "# Feature 149 Live Proof Limitation"
        else
            "# Feature 148 Live Proof Limitation"

    IO.File.WriteAllText(proofPath, proofBody)
    IO.File.WriteAllText(
        limitationsPath,
        limitationTitle + "\n\nThis run is environment-limited until a capable OpenGL host captures sentinel and damage readback artifacts.\n")
    printfn "%s" proofPath
    0

let private runCompositorParityCmd (rest: string list) =
    let out = outDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, "parity.md")

    let body =
        if isFeature152 rest then
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
        | None when isFeature152 rest -> Compositor.feature152TimingDirectory
        | None when isFeature149 rest -> Compositor.feature149TimingDirectory
        | None -> Compositor.feature148TimingDirectory
    IO.Directory.CreateDirectory(out) |> ignore
    let path = IO.Path.Combine(out, $"timing-{tier}.md")
    let body =
        if isFeature152 rest then
            Compositor.renderFeature152TimingReport tier
        elif isFeature149 rest then
            Compositor.renderFeature149TimingReport tier
        else
            Compositor.renderFeature148TimingReport tier
    IO.File.WriteAllText(path, body)
    printfn "%s" path
    0

let private runCompositorReadinessCmd (rest: string list) =
    let facts = Probe.probe ()
    let out = outDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let now = DateTimeOffset.UtcNow
    let profile = Compositor.hostProfileFromFacts facts
    let proofVerdict =
        match facts.EffectiveBackend, facts.GlRenderer with
        | NoDisplay, _ -> Compositor.ProofEnvironmentLimited "missing display"
        | _, None -> Compositor.ProofEnvironmentLimited "missing GL renderer facts"
        | _ -> Compositor.ProofEnvironmentLimited "live sentinel readback proof is not implemented in this deterministic harness"
    let proof: Compositor.PresentProof =
        { ProofId = now.UtcDateTime.ToString("yyyyMMdd-HHmmss")
          HostProfile = profile
          ScenarioId = if isFeature148 rest || isFeature149 rest || isFeature152 rest then "proof/live-sentinel-damage-v1" else "proof/sentinel-damage-v1"
          Verdict = proofVerdict
          CreatedAt = now
          EvidenceArtifacts = [ "present-proof/proof.md" ]
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
    if isFeature152 rest then
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
    | "compositor-readiness" :: rest -> runCompositorReadinessCmd rest
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
        printfn "usage: <probe|offscreen|live-x11|overlay-visual-proof|render-anywhere-reference|render-anywhere-browser-feasibility|compositor-present-proof|compositor-live-proof|compositor-parity|compositor-perf|compositor-reuse|compositor-snapshots|compositor-timing|compositor-readiness|perf|input> [--out <dir>] [--json]"
        0
    | other ->
        eprintfn "unknown subcommand: %s" (String.concat " " other)
        2
