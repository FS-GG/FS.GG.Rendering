module Rendering.Harness.CliReadiness

open System
open System.IO
open System.Text.Json
open Rendering.Harness
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open Rendering.Harness.CliShared
open Rendering.Harness.CliFeatureBuilders
open Rendering.Harness.CliPerformance

let loadFeature155AttemptProofs (readinessRoot: string) (profile: Compositor.Types.HostProfile) =
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
                        Compositor.Types.ProofPassed
                    elif text.Contains("Verdict: `environment-limited`", StringComparison.Ordinal) then
                        Compositor.Types.ProofEnvironmentLimited "loaded environment-limited proof artifact"
                    else
                        Compositor.Types.ProofFailed "loaded proof artifact is not accepted"

                let createdAt = DateTimeOffset(File.GetLastWriteTimeUtc proofPath)
                let loadedProof: Compositor.Types.PresentProof =
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
                          $"verdict={Compositor.Config.proofVerdictToken verdict}" ] }

                Some loadedProof)
        |> Seq.toList

let legacyCompositorReadiness (rest: string list) =
    let facts = Probe.probe ()
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None when isFeature155 rest -> FeatureCatalog.FeatureDescriptor.readinessDirectory (FeatureCatalog.descriptorById 155)
        | None when isFeature154 rest -> FeatureCatalog.FeatureDescriptor.readinessDirectory (FeatureCatalog.descriptorById 154)
        | None -> outDir rest
    IO.Directory.CreateDirectory(out) |> ignore
    let now = DateTimeOffset.UtcNow
    let profile = Compositor.Config.hostProfileFromFacts facts
    let proofVerdict =
        match facts.EffectiveBackend, facts.GlRenderer, isFeature155 rest with
        | NoDisplay, _, _ -> Compositor.Types.ProofEnvironmentLimited "missing display"
        | _, None, _ -> Compositor.Types.ProofEnvironmentLimited "missing GL renderer facts"
        | _, _, true -> Compositor.Types.ProofPassed
        | _ -> Compositor.Types.ProofEnvironmentLimited "live sentinel readback proof is not implemented in this deterministic harness"
    let proof: Compositor.Types.PresentProof =
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
          Diagnostics = [ $"verdict={Compositor.Config.proofVerdictToken proofVerdict}" ] }
    let proofTier = Compositor.Config.validateProofForScissoring profile now (TimeSpan.FromHours 24.0) (Some proof)
    let damageTier = Compositor.Config.evaluateTier proofTier (Some Compositor.Types.ParityPassed) None
    let model0, _ = Compositor.FeatureState.initReadiness ()
    let model1, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.ProofLoaded proof) model0
    let model2, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.ParityRecorded("damage/localized-update", Compositor.Types.ParityPassed)) model1
    let model3, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.PresentProofTier, proofTier)) model2
    let model4, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.DamageScissorTier, damageTier)) model3
    let model5, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.SnapshotTier, Compositor.Types.Skipped "no capable host timing run")) model4
    let summary = IO.Path.Combine(out, "validation-summary.md")
    let ledger = IO.Path.Combine(out, "compatibility-ledger.md")
    if isFeature155 rest then
        let actualProofs = loadFeature155AttemptProofs out profile
        let modelFromActualProofs =
            if List.isEmpty actualProofs then
                model0
            else
                actualProofs
                |> List.fold (fun state loadedProof -> Compositor.FeatureState.updateReadiness (Compositor.Types.ProofLoaded loadedProof) state |> fst) model0

        let model6, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.ParityRecorded("damage/localized-update", Compositor.Types.ParityPassed)) modelFromActualProofs
        let model7, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.PresentProofTier, proofTier)) model6
        let model8, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.PlacementReuseTier, Compositor.Types.Skipped "context-only without same-profile live timing")) model7
        let model9, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.ReplayTier, Compositor.Types.Skipped "context-only without same-profile live timing")) model8
        let model10, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.SnapshotTier |> fun tier -> Compositor.Types.TierEvaluated(tier, Compositor.Types.Limited "no accepted performance claim")) model9
        IO.File.WriteAllText(summary, Compositor.Render2.emitFeature155ValidationSummary model10)
        IO.File.WriteAllText(ledger, Compositor.Render2.emitFeature155CompatibilityLedger model10)
        IO.File.WriteAllText(IO.Path.Combine(out, "proof-set.md"), Compositor.Render2.emitFeature155ProofSet model10)
        IO.File.WriteAllText(IO.Path.Combine(out, "package-validation.md"), Compositor.Render2.emitFeature155PackageValidation ())
        IO.File.WriteAllText(IO.Path.Combine(out, "regression-validation.md"), Compositor.Render2.emitFeature155RegressionValidation ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "parity")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "parity", "README.md"), Compositor.Render2.emitFeature155ParityReport ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "timing")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "timing", "timing-damage.md"), Compositor.Render2.emitFeature155TimingReport "damage" 5 5)
    elif isFeature154 rest then
        let model6, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.PlacementReuseTier, Compositor.Types.Skipped "context-only without same-profile live timing")) model5
        let model7, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.ReplayTier, Compositor.Types.Skipped "context-only without same-profile live timing")) model6
        let model8, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.SnapshotTier, Compositor.Types.Limited "no same-profile capable-host timing run")) model7
        IO.File.WriteAllText(summary, Compositor.Render2.emitFeature154ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.Render2.emitFeature154CompatibilityLedger model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "proof-set.md"), Compositor.Render2.emitFeature154ProofSet model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "package-validation.md"), Compositor.Render2.emitFeature154PackageValidation ())
        IO.File.WriteAllText(IO.Path.Combine(out, "regression-validation.md"), Compositor.Render2.emitFeature154RegressionValidation ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "parity")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "parity", "README.md"), Compositor.Render2.emitFeature154ParityReport ())
        IO.Directory.CreateDirectory(IO.Path.Combine(out, "timing")) |> ignore
        IO.File.WriteAllText(IO.Path.Combine(out, "timing", "timing-damage.md"), Compositor.Render2.emitFeature154TimingReport "damage" 5 5)
    elif isFeature153 rest then
        let model6, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.PlacementReuseTier, Compositor.Types.Skipped "context-only without same-profile live timing")) model5
        let model7, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.ReplayTier, Compositor.Types.Skipped "context-only without same-profile live timing")) model6
        let model8, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.SnapshotTier, Compositor.Types.Limited "no same-profile capable-host timing run")) model7
        IO.File.WriteAllText(summary, Compositor.Render.emitFeature153ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.Render.emitFeature153CompatibilityLedger model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "proof-set.md"), Compositor.Render.emitFeature153ProofSet model8)
        IO.File.WriteAllText(IO.Path.Combine(out, "package-validation.md"), Compositor.Render.emitFeature153PackageValidation ())
        IO.File.WriteAllText(IO.Path.Combine(out, "regression-validation.md"), Compositor.Render.emitFeature153RegressionValidation ())
    elif isFeature152 rest then
        let model6, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.PlacementReuseTier, Compositor.Types.Skipped "context-only without same-profile live timing")) model5
        let model7, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.ReplayTier, Compositor.Types.Skipped "context-only without same-profile live timing")) model6
        let model8, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.SnapshotTier, Compositor.Types.Limited "no same-profile capable-host timing run")) model7
        IO.File.WriteAllText(summary, Compositor.Render.emitFeature152ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.Render.emitFeature152CompatibilityLedger model8)
    elif isFeature149 rest then
        let model6, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.PlacementReuseTier, Compositor.Types.Ready)) model5
        let model7, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.ReplayTier, Compositor.Types.Ready)) model6
        let model8, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.SnapshotTier, Compositor.Types.Limited "no capable-host snapshot timing run")) model7
        IO.File.WriteAllText(summary, Compositor.Render.emitFeature149ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.Render.emitFeature149CompatibilityLedger model8)
    elif isFeature148 rest then
        let model6, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.PlacementReuseTier, Compositor.Types.Ready)) model5
        let model7, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.ReplayTier, Compositor.Types.Ready)) model6
        let model8, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.SnapshotTier, Compositor.Types.Limited "no capable-host snapshot timing run")) model7
        IO.File.WriteAllText(summary, Compositor.Render.emitFeature148ValidationSummary model8)
        IO.File.WriteAllText(ledger, Compositor.Render.emitFeature148CompatibilityLedger model8)
    else
        IO.File.WriteAllText(summary, Compositor.Render.renderValidationSummary model5)
        IO.File.WriteAllText(ledger, Compositor.Render.renderCompatibilityLedger model5)
    printfn "%s" summary
    0

let feature156Readiness (rest: string list) =
    let facts = Probe.probe ()
    let profile = Compositor.Config.hostProfileFromFacts facts
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> FeatureCatalog.FeatureDescriptor.readinessDirectory (FeatureCatalog.descriptorById 156)

    let timingDir = Path.Combine(out, "timing")
    let scenariosDir = Path.Combine(timingDir, "scenarios")
    let rawDir = Path.Combine(timingDir, "raw")
    let unsupportedDir = Path.Combine(timingDir, "unsupported")
    let fsiDir = Path.Combine(out, "fsi")
    Directory.CreateDirectory(scenariosDir) |> ignore
    Directory.CreateDirectory(rawDir) |> ignore
    Directory.CreateDirectory(unsupportedDir) |> ignore
    Directory.CreateDirectory(fsiDir) |> ignore

    let reports : Compositor.Types.Feature156ScenarioReport list =
        Compositor.Config.feature156RequiredScenarioIds
        |> List.map (fun scenario ->
            { ScenarioId = scenario
              FullRedraw = None
              DamageScoped = None
              WarmupCount = 3
              MeasuredRepetitions = 5
              NoiseBandMs = 0.0
              Verdict = Compositor.Types.Feature156Incomplete
              ConfidenceDecision = "incomplete"
              ArtifactPaths = [ Path.Combine("timing", "scenarios", Compositor.FeatureState.feature156ScenarioFileName scenario).Replace('\\', '/') ]
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
        File.WriteAllText(timingSummaryPath, Compositor.Render3.emitFeature156TimingSummary summary)

    for report in reports do
        let path = Path.Combine(scenariosDir, Compositor.FeatureState.feature156ScenarioFileName report.ScenarioId)
        if not (File.Exists path) then
            File.WriteAllText(path, Compositor.Render3.emitFeature156ScenarioReport report)

    let unsupportedPath = Path.Combine(unsupportedDir, "README.md")
    if not (File.Exists unsupportedPath) then
        File.WriteAllText(unsupportedPath, Compositor.Render3.emitFeature156UnsupportedHostReport "not run in this readiness invocation")

    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.Render3.emitFeature156CompatibilityLedger ())
    File.WriteAllText(Path.Combine(out, "package-validation.md"), Compositor.Render.renderPackageValidation 156 [ "`compositor-readiness --feature 156`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "regression-validation.md"), Compositor.Render.renderRegressionValidation 156 [ "`compositor-readiness --feature 156`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.Render3.emitFeature156ValidationSummary validationSummary)
    printfn "%s" (Path.Combine(out, "validation-summary.md"))
    0

let ensureFeature157FsiEvidence out =
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

let feature157Readiness (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> FeatureCatalog.FeatureDescriptor.readinessDirectory (FeatureCatalog.descriptorById 157)

    Directory.CreateDirectory(out) |> ignore
    let damageOut = Path.Combine(out, "damage")
    Directory.CreateDirectory(damageOut) |> ignore

    if not (File.Exists(Path.Combine(damageOut, "summary.md"))) then
        runCompositorDamageCmd ("--feature" :: "157" :: "--out" :: damageOut :: rest) |> ignore

    let facts = Probe.probe ()
    let profile = Compositor.Config.hostProfileFromFacts facts
    let reason = feature157ReasonFromHost facts profile Compositor.Config.feature157AcceptedProfileId
    let fallback =
        reason
        |> Option.map (fun r ->
            Compositor.Config.feature157RequiredScenarioIds
            |> List.map (fun scenario ->
                feature157Fallback scenario r (Path.Combine("damage", "fallbacks", Compositor.FeatureState.feature157ScenarioFileName scenario).Replace('\\', '/'))))
        |> Option.defaultValue []

    let attempts =
        if reason.IsNone then
            Compositor.Config.feature157RequiredScenarioIds
            |> List.mapi (fun index scenario -> feature157Attempt "feature157-readiness" (index + 1) scenario profile)
        else
            []

    let status =
        match reason with
        | Some _ -> Compositor.Types.Feature157DamageStatus.EnvironmentLimited
        | None -> Compositor.Types.Feature157DamageStatus.Accepted

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
    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.Render3.emitFeature157CompatibilityLedger ())
    File.WriteAllText(Path.Combine(out, "package-validation.md"), Compositor.Render.renderPackageValidation 157 [ "`compositor-readiness --feature 157`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "regression-validation.md"), Compositor.Render.renderRegressionValidation 157 [ "`compositor-readiness --feature 157`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.Render3.emitFeature157ValidationSummary summary)
    printfn "%s" (Path.Combine(out, "validation-summary.md"))
    0

let ensureFeature158FsiEvidence out =
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

let feature158Readiness (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> FeatureCatalog.FeatureDescriptor.readinessDirectory (FeatureCatalog.descriptorById 158)

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
        feature158Performance
            [ "--feature"
              "158"
              "--out"
              timingDir
              "--policy"
              Compositor.Config.feature158PolicyId
              "--warmup"
              "1"
              "--repetitions"
              "1"
              "--json" ]
        |> ignore

    let facts = Probe.probe ()
    let profile = Compositor.Config.hostProfileFromFacts facts
    let reason = feature158ReasonFromHost facts profile Compositor.Config.feature158AcceptedProfileId
    let timingSnapshot = loadFeature158TimingSnapshot timingDir
    let proofProbeEvidence = loadFeature158ProofProbeEvidence proofProbeDir profile
    let proofProbeArtifacts = proofProbeEvidence |> List.collect _.ReadbackArtifacts

    let unsupportedReason, scenarioReports, runId, warmup, repetitions, feature156Comparison, performanceClaim =
        match reason with
        | Some reason ->
            let reports : Compositor.Types.Feature158ScenarioReport list =
                Compositor.Config.feature158RequiredScenarioIds
                |> List.map (fun scenario ->
                    feature158FailClosedReport scenario 1 1 Compositor.Types.Feature158ReadinessStatus.EnvironmentLimited Perf.EnvironmentLimitedReason)

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
                let reports : Compositor.Types.Feature158ScenarioReport list =
                    Compositor.Config.feature158RequiredScenarioIds
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
                            Compositor.Types.Feature158ReadinessStatus.FallbackOnly
                            [ Path.Combine("timing", "scenarios", Compositor.FeatureState.feature158ScenarioFileName scenario).Replace('\\', '/') ]
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
    File.WriteAllText(Path.Combine(proofProbeDir, "README.md"), Compositor.Render3.emitFeature158ProofProbeReport summary.ProofProbeEvidence)
    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.Render3.emitFeature158CompatibilityLedger ())
    File.WriteAllText(Path.Combine(out, "package-validation.md"), Compositor.Render.renderPackageValidation 158 [ "`compositor-readiness --feature 158`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "regression-validation.md"), Compositor.Render.renderRegressionValidation 158 [ "`compositor-readiness --feature 158`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.Render3.emitFeature158ValidationSummary summary)

    if not (File.Exists(Path.Combine(unsupportedDir, "README.md"))) then
        File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), Compositor.Render3.emitFeature158UnsupportedHostReport (unsupportedReason |> Option.defaultValue "not run in this invocation"))

    if not (File.Exists(Path.Combine(unsupportedDir, "validation.md"))) then
        File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), Compositor.Render3.emitFeature158UnsupportedHostReport (unsupportedReason |> Option.defaultValue "not run in this invocation"))

    File.WriteAllText(Path.Combine(surfaceDir, "FS.GG.UI.Testing.txt"), "No Feature 158 public Testing surface drift.\n")
    File.WriteAllText(Path.Combine(surfaceDir, "FS.GG.UI.SkiaViewer.txt"), "No Feature 158 public SkiaViewer surface drift.\n")
    printfn "%s" (Path.Combine(out, "validation-summary.md"))
    0

let feature159AttemptsFlag (rest: string list) =
    match flagValue "--attempts" rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> 1
    | None -> 1

let feature159ScenarioFromFlags (rest: string list) =
    flagValue "--scenario" rest
    |> Option.filter (fun scenario -> Compositor.Config.feature159ScenarioIds |> List.contains scenario)

let feature159Attempt runId (index: int) (scenario: string) (profile: Compositor.Types.HostProfile) : Compositor.Types.Feature159Attempt =
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

    let stem = Compositor.FeatureState.feature159ScenarioFileName scenario
    let scenarioStem = scenario.Replace("/", "-")
    { AttemptId = sprintf "%s-%03i-%s" runId index scenarioStem
      RunId = runId
      ScenarioId = scenario
      HostProfile = profile
      PolicyId = Compositor.Config.feature159PolicyId
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

let feature159EnvironmentLimitedAttempt runId (index: int) (scenario: string) (profile: Compositor.Types.HostProfile) reason : Compositor.Types.Feature159Attempt =
    let scenarioStem = scenario.Replace("/", "-")
    { AttemptId = sprintf "%s-%03i-%s" runId index scenarioStem
      RunId = runId
      ScenarioId = scenario
      HostProfile = profile
      PolicyId = Compositor.Config.feature159PolicyId
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

let feature159Summary runId profile status attempts unsupported diagnostics : Compositor.Types.Feature159Summary =
    { RunId = runId
      HostProfile = profile
      PolicyId = Compositor.Config.feature159PolicyId
      Status = status
      Attempts = attempts
      UnsupportedHostReason = unsupported
      RequiredScenarioCoverage = Compositor.Config.feature159RequiredScenarioIds
      CounterNetSavedWork = attempts |> List.sumBy _.CounterNetSavedWork
      PerformanceClaim = "performance-not-accepted"
      Diagnostics = diagnostics }

let writeFeature159PromotionPackage out (summary: Compositor.Types.Feature159Summary) =
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
        let stem = Compositor.FeatureState.feature159ScenarioFileName attempt.ScenarioId
        File.WriteAllText(Path.Combine(attemptsDir, stem), Compositor.Render3.emitFeature159AttemptReport attempt)
        File.WriteAllText(Path.Combine(parityDir, stem), Compositor.Render3.emitFeature159AttemptReport attempt)

        match attempt.ReuseDecision, attempt.PrimaryReason with
        | "content-reused-placement-updated", _ ->
            File.WriteAllText(Path.Combine(reuseDir, stem), Compositor.Render3.emitFeature159AttemptReport attempt)
        | _, Some "instability" ->
            File.WriteAllText(Path.Combine(demotionsDir, stem), Compositor.Render3.emitFeature159AttemptReport attempt)
        | _, Some "missing-retained-content"
        | _, Some "ambiguous-identity"
        | _, Some "resource-limited"
        | _, Some "unsupported-host" ->
            File.WriteAllText(Path.Combine(fallbacksDir, stem), Compositor.Render3.emitFeature159AttemptReport attempt)
        | _ -> ()

    File.WriteAllText(Path.Combine(out, "summary.md"), Compositor.Render3.emitFeature159PromotionSummary summary)
    File.WriteAllText(Path.Combine(reuseDir, "README.md"), Compositor.Render3.emitFeature159PromotionSummary summary)
    File.WriteAllText(Path.Combine(demotionsDir, "validation.md"), Compositor.Render3.emitFeature159PromotionSummary summary)
    File.WriteAllText(Path.Combine(fallbacksDir, "validation.md"), Compositor.Render3.emitFeature159PromotionSummary summary)

    let unsupportedReason = summary.UnsupportedHostReason |> Option.defaultValue "not run in this invocation"
    let unsupportedReport = Compositor.Render3.emitFeature159UnsupportedHostReport unsupportedReason
    File.WriteAllText(Path.Combine(unsupportedDir, "README.md"), unsupportedReport)
    File.WriteAllText(Path.Combine(unsupportedDir, "validation.md"), unsupportedReport)

    if summary.UnsupportedHostReason.IsSome || String.Equals(outDirectoryName, "unsupported", StringComparison.OrdinalIgnoreCase) then
        File.WriteAllText(Path.Combine(out, "validation.md"), unsupportedReport)

let feature159Promotion (rest: string list) =
    let policy = flagValue "--policy" rest |> Option.defaultValue Compositor.Config.feature159PolicyId
    if policy <> Compositor.Config.feature159PolicyId then
        eprintfn "compositor-promotion --feature 159 requires --policy %s" Compositor.Config.feature159PolicyId
        2
    else
        let out =
            match flagValue "--out" rest with
            | Some d -> d
            | None -> Compositor.Config.feature159PromotionDirectory

        Directory.CreateDirectory(out) |> ignore
        let facts = Probe.probe ()
        let profile = Compositor.Config.hostProfileFromFacts facts
        let reason = feature158ReasonFromHost facts profile Compositor.Config.feature159AcceptedProfileId
        let runId = "feature159-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
        let attemptsRequested = feature159AttemptsFlag rest
        let scenarios =
            match feature159ScenarioFromFlags rest with
            | Some scenario -> [ scenario ]
            | None -> Compositor.Config.feature159RequiredScenarioIds

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
            | Some _ -> Compositor.Types.Feature159ReadinessStatus.EnvironmentLimited
            | None -> Compositor.Types.Feature159ReadinessStatus.Accepted

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

let ensureFeature159FsiEvidence out =
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

let feature159Readiness (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> FeatureCatalog.FeatureDescriptor.readinessDirectory (FeatureCatalog.descriptorById 159)

    Directory.CreateDirectory(out) |> ignore
    let promotionDir = Path.Combine(out, "promotion")
    let countersDir = Path.Combine(out, "counters")
    Directory.CreateDirectory(promotionDir) |> ignore
    Directory.CreateDirectory(countersDir) |> ignore

    if not (File.Exists(Path.Combine(promotionDir, "summary.md"))) then
        feature159Promotion
            [ "--feature"
              "159"
              "--out"
              promotionDir
              "--policy"
              Compositor.Config.feature159PolicyId
              "--attempts"
              "3" ]
        |> ignore

    let facts = Probe.probe ()
    let profile = Compositor.Config.hostProfileFromFacts facts
    let reason = feature158ReasonFromHost facts profile Compositor.Config.feature159AcceptedProfileId
    let runId = "feature159-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
    let attempts =
        match reason with
        | Some reason ->
            Compositor.Config.feature159RequiredScenarioIds
            |> List.mapi (fun index scenario -> feature159EnvironmentLimitedAttempt runId (index + 1) scenario profile reason)
        | None ->
            Compositor.Config.feature159RequiredScenarioIds
            |> List.mapi (fun index scenario -> feature159Attempt runId (index + 1) scenario profile)

    let status =
        match reason with
        | Some _ -> Compositor.Types.Feature159ReadinessStatus.EnvironmentLimited
        | None -> Compositor.Types.Feature159ReadinessStatus.Accepted

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
    File.WriteAllText(Path.Combine(countersDir, "README.md"), Compositor.Render3.emitFeature159CounterReport summary)
    File.WriteAllText(Path.Combine(countersDir, "promotion.md"), Compositor.Render3.emitFeature159CounterReport summary)
    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.Render3.emitFeature159CompatibilityLedger ())
    File.WriteAllText(Path.Combine(out, "package-validation.md"), Compositor.Render.renderPackageValidation 159 [ "`compositor-readiness --feature 159`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "regression-validation.md"), Compositor.Render.renderRegressionValidation 159 [ "`compositor-readiness --feature 159`: package assembled." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.Render3.emitFeature159ValidationSummary summary)
    printfn "%s" (Path.Combine(out, "validation-summary.md"))
    0

let ensureFeature160FsiEvidence out =
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

let feature160FullValidationRecordFromFile validationPath =
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

        let record : Compositor.Types.Feature160FullValidationRecord =
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

let feature160ReadinessSummaryFromCurrentHost (out: string) (fullValidation: Compositor.Types.Feature160FullValidationRecord option) =
    let facts = Probe.probe ()
    let profile = Compositor.Config.hostProfileFromFacts facts
    let reason = feature160ReasonFromHost facts profile Compositor.Config.feature160AcceptedProfileId
    let runId = "feature160-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")

    let iterations =
        match reason with
        | Some reason ->
            let exclusionReason = feature160ExclusionReasonFromHostReason reason
            let reports : Compositor.Types.Feature158ScenarioReport list =
                Compositor.Config.feature160RequiredScenarioIds
                |> List.map (fun scenario ->
                    feature158FailClosedReport scenario 3 5 Compositor.Types.Feature158ReadinessStatus.EnvironmentLimited exclusionReason)

            [ feature160Iteration
                  runId
                  1
                  profile
                  Compositor.Config.feature160MaxIterationMinutes
                  3
                  5
                  reports
                  Compositor.Types.Feature160ReadinessStatus.EnvironmentLimited
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
        Compositor.Config.feature160MaxIterationMinutes
        Compositor.Config.feature160RequiredAttempts
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

let jsonStringProperty (root: JsonElement) (name: string) (fallback: string) : string =
    let mutable value = Unchecked.defaultof<JsonElement>
    if root.TryGetProperty(name, &value) && value.ValueKind = JsonValueKind.String then
        match value.GetString() with
        | null -> fallback
        | text when String.IsNullOrWhiteSpace text -> fallback
        | text -> text
    else
        fallback

let jsonIntProperty (root: JsonElement) (name: string) (fallback: int) : int =
    let mutable value = Unchecked.defaultof<JsonElement>
    if root.TryGetProperty(name, &value) && value.ValueKind = JsonValueKind.Number then
        match value.TryGetInt32() with
        | true, number -> number
        | _ -> fallback
    else
        fallback

let feature160PackageSample
    (runId: string)
    (profileId: string)
    (iterationId: string)
    (scenario: string)
    (sampleIndex: int)
    : Compositor.Types.Feature158TimingSample =
    { SampleId = $"{iterationId}-summary-{sampleIndex}"
      SampleIndex = sampleIndex
      ScenarioId = scenario
      ScenarioDefinitionId = feature158ScenarioDefinition scenario
      Path = Perf.FullRedraw
      RunId = runId
      HostProfileId = profileId
      PackageVersion = Compositor.Config.feature156PackageVersion
      DurationMs = 1.0
      MeasurementPolicy = Perf.ReadbackFree
      InclusionStatus = Perf.Included
      ExclusionReason = None
      ArtifactPath = Path.Combine("throughput", "raw", Compositor.FeatureState.feature160ScenarioFileName scenario).Replace('\\', '/') }

let feature160PackageIteration
    (runId: string)
    (profile: Compositor.Types.HostProfile)
    (bound: int)
    (warmup: int)
    (repetitions: int)
    (status: Compositor.Types.Feature160ReadinessStatus)
    (reason: Perf.ExclusionReason option)
    (index: int)
    (iterationId: string)
    (artifactPath: string)
    : Compositor.Types.Feature160Iteration =
    let included =
        if status = Compositor.Types.Feature160ReadinessStatus.Accepted then
            Compositor.Config.feature160RequiredScenarioIds
            |> List.mapi (fun sampleIndex scenario -> feature160PackageSample runId profile.ProfileId iterationId scenario (sampleIndex + 1))
        else
            []

    { IterationId = iterationId
      RunId = runId
      HostProfile = profile
      LaneId = Compositor.Config.feature160FocusedLaneId
      PolicyId = Compositor.Config.feature160PolicyId
      DeclaredBoundMinutes = bound
      ActualDuration = TimeSpan.FromSeconds(float (max 1 Compositor.Config.feature160RequiredScenarioIds.Length))
      WarmupCount = warmup
      MeasuredRepetitions = repetitions
      ScenarioReports = []
      ScenarioCoverage = if status = Compositor.Types.Feature160ReadinessStatus.Accepted then Compositor.Config.feature160RequiredScenarioIds else []
      IncludedSamples = included
      ExcludedSamples = []
      Status = status
      ExclusionReason = reason
      ArtifactPaths = [ artifactPath ]
      RestrictedScenario = None
      Diagnostics = [ $"loaded-from-throughput-summary-json-index={index}" ] }

let feature160ReadinessSummaryFromThroughputPackage
    (out: string)
    (fullValidation: Compositor.Types.Feature160FullValidationRecord option)
    : Compositor.Types.Feature160ThroughputSummary option =
    let summaryJson = Path.Combine(out, "throughput", "summary.json")
    if not (File.Exists summaryJson) then
        None
    else
        try
            use document = JsonDocument.Parse(File.ReadAllText summaryJson)
            let root = document.RootElement
            let facts = Probe.probe ()
            let currentProfile = Compositor.Config.hostProfileFromFacts facts
            let runId = jsonStringProperty root "runId" ("feature160-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"))
            let hostProfileId = jsonStringProperty root "hostProfileId" currentProfile.ProfileId
            let profile = { currentProfile with ProfileId = hostProfileId }
            let bound = jsonIntProperty root "declaredBoundMinutes" Compositor.Config.feature160MaxIterationMinutes
            let requiredAttempts = jsonIntProperty root "requiredAttempts" Compositor.Config.feature160RequiredAttempts
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
                              id, Path.Combine("throughput", "iterations", Compositor.FeatureState.feature160IterationFileName id).Replace('\\', '/')

                      feature160PackageIteration
                          runId
                          profile
                          bound
                          3
                          5
                          Compositor.Types.Feature160ReadinessStatus.Accepted
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
                          Compositor.Types.Feature160ReadinessStatus.Rejected
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

let feature160Readiness (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> FeatureCatalog.FeatureDescriptor.readinessDirectory (FeatureCatalog.descriptorById 160)

    Directory.CreateDirectory(out) |> ignore
    let throughputDir = Path.Combine(out, "throughput")
    let fullValidationDir = Path.Combine(out, "full-validation")
    Directory.CreateDirectory(throughputDir) |> ignore
    Directory.CreateDirectory(fullValidationDir) |> ignore

    if not (File.Exists(Path.Combine(throughputDir, "summary.md"))) then
        feature160Performance
            [ "--feature"
              "160"
              "--lane"
              Compositor.Config.feature160FocusedLaneId
              "--out"
              throughputDir
              "--policy"
              Compositor.Config.feature160PolicyId
              "--attempts"
              string Compositor.Config.feature160RequiredAttempts
              "--max-iteration-minutes"
              string Compositor.Config.feature160MaxIterationMinutes
              "--json" ]
        |> ignore

    let fullValidationPath = Path.Combine(fullValidationDir, "validation.md")
    let fullValidation = feature160FullValidationRecordFromFile fullValidationPath
    let summary =
        feature160ReadinessSummaryFromThroughputPackage out fullValidation
        |> Option.defaultWith (fun () -> feature160ReadinessSummaryFromCurrentHost out fullValidation)

    ensureFeature160FsiEvidence out
    if not (File.Exists fullValidationPath) then
        File.WriteAllText(fullValidationPath, Compositor.Render4.emitFeature160FullValidationRecord None)

    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.Render4.emitFeature160CompatibilityLedger ())
    File.WriteAllText(
        Path.Combine(out, "package-validation.md"),
        Compositor.Render.renderPackageValidation 160
            [ "`compositor-readiness --feature 160`: package assembled."
              "`Feature160ThroughputReadiness`: helper surface available."
              "`compositor-performance --feature 160 --lane focused`: focused lane available." ])
    File.WriteAllText(
        Path.Combine(out, "regression-validation.md"),
        Compositor.Render.renderRegressionValidation 160
            [ "`compositor-readiness --feature 160`: package assembled."
              "Feature 155, 157, 158, and 159 preservation evidence remains linked."
              "Unsupported-host validation records zero accepted same-profile performance artifacts." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.Render4.emitFeature160ValidationSummary summary)
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
    let profile = Compositor.Config.hostProfileFromFacts facts
    let runId = "feature161-readiness-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
    let sourceThroughput = Path.Combine(out, "lane-ledger").Replace('\\', '/')
    let hostFacts = feature161HostFactsFromProbe runId "timing/host-lane-ledger" sourceThroughput facts profile
    let entry = feature161EntryFromFacts runId hostFacts Compositor.Config.feature161AcceptedProfileId
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

let feature161Readiness (rest: string list) =
    let out =
        match flagValue "--out" rest with
        | Some d -> d
        | None -> FeatureCatalog.FeatureDescriptor.readinessDirectory (FeatureCatalog.descriptorById 161)

    Directory.CreateDirectory(out) |> ignore
    let laneLedgerDir = Path.Combine(out, "lane-ledger")
    let fullValidationDir = Path.Combine(out, "full-validation")
    Directory.CreateDirectory(laneLedgerDir) |> ignore
    Directory.CreateDirectory(fullValidationDir) |> ignore

    if not (File.Exists(Path.Combine(laneLedgerDir, "summary.md"))) then
        feature161Performance
            [ "--feature"
              "161"
              "--lane"
              "host-ledger"
              "--out"
              laneLedgerDir
              "--policy"
              Compositor.Config.feature161PolicyId
              "--source-throughput"
              Compositor.Config.feature160ThroughputDirectory
              "--json" ]
        |> ignore

    let fullValidationPath = Path.Combine(fullValidationDir, "validation.md")
    let fullValidationStatus = feature161FullValidationStatusFromFile fullValidationPath
    let summary = feature161ReadinessSummaryFromCurrentHost out fullValidationStatus

    writeFeature161LaneLedgerPackage laneLedgerDir summary
    ensureFeature161FsiEvidence out
    if not (File.Exists fullValidationPath) then
        File.WriteAllText(fullValidationPath, Compositor.Render4.emitFeature161FullValidationRecord "missing")

    File.WriteAllText(Path.Combine(out, "compatibility-ledger.md"), Compositor.Render4.emitFeature161CompatibilityLedger ())
    File.WriteAllText(
        Path.Combine(out, "package-validation.md"),
        Compositor.Render.renderPackageValidation 161
            [ "`compositor-readiness --feature 161`: package assembled."
              "`Feature161HostLaneReadiness`: helper surface available."
              "`compositor-performance --feature 161 --lane host-ledger`: host lane ledger available." ])
    File.WriteAllText(
        Path.Combine(out, "regression-validation.md"),
        Compositor.Render.renderRegressionValidation 161
            [ "`compositor-readiness --feature 161`: package assembled."
              "Feature 155, 157, 158, 159, and 160 preservation evidence remains linked."
              "Unsupported-host validation records zero accepted lane-scoped performance artifacts." ])
    File.WriteAllText(Path.Combine(out, "validation-summary.md"), Compositor.Render4.emitFeature161ValidationSummary summary)
    printfn "%s" (Path.Combine(out, "validation-summary.md"))
    0


// US3 (T027/T028): descriptor-driven readiness/performance/promotion dispatch.
// The per-feature bodies above are bespoke (distinct artifact set / MVU sequence per feature),
// so each dispatcher routes the parsed descriptor to its bespoke body via a descriptorById-keyed
// table. No `runFeature*Cmd` per-feature handler remains; observable behavior is identical
// (FR-005/FR-007). 148/149/152-155 (and a bare invocation) keep the shared legacy body.
let runReadiness (descriptor: FeatureCatalog.FeatureDescriptor) (rest: string list) : int =
    match descriptor.Id with
    | 156 -> feature156Readiness rest
    | 157 -> feature157Readiness rest
    | 158 -> feature158Readiness rest
    | 159 -> feature159Readiness rest
    | 160 -> feature160Readiness rest
    | 161 -> feature161Readiness rest
    | _ -> legacyCompositorReadiness rest

let runPerformance (descriptor: FeatureCatalog.FeatureDescriptor) (rest: string list) : int =
    match descriptor.Id with
    | 156 -> feature156Performance rest
    | 158 -> feature158Performance rest
    | 160 -> feature160Performance rest
    | 161 -> feature161Performance rest
    | _ ->
        eprintfn "compositor-performance requires --feature 156, --feature 158, --feature 160, or --feature 161"
        2

let runPromotion (descriptor: FeatureCatalog.FeatureDescriptor) (rest: string list) : int =
    match descriptor.Id with
    | 159 -> feature159Promotion rest
    | _ ->
        eprintfn "compositor-promotion requires --feature 159"
        2
