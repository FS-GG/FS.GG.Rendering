module Feature158ReadinessPackageTests

open Expecto
open Rendering.Harness

let private profile : Compositor.Types.HostProfile =
    { ProfileId = Compositor.Config.feature158AcceptedProfileId
      Backend = "OpenGL"
      Renderer = Some "Mesa"
      PresentMode = "DirectToSwapchain"
      FramebufferSize = "640x480"
      Scale = Some 1.0
      DisplayEnvironment = "x11"
      ProofAlgorithmVersion = "sentinel-damage-v1" }

let private dist path : Compositor.Types.Feature158PathDistribution =
    { SampleCount = 5
      P50Ms = 4.0
      P95Ms = 5.0
      P99Ms = 6.0
      MinMs = 3.0
      MaxMs = 6.0
      RawSamplePath = path }

let private timingSample scenario index policy status reason : Compositor.Types.Feature158TimingSample =
    { SampleId = $"feature158-{index}"
      SampleIndex = index
      ScenarioId = scenario
      ScenarioDefinitionId = "feature156-required-v1:" + (Compositor.FeatureState.feature158ScenarioFileName scenario).Replace(".md", "")
      Path = Perf.DamageScoped
      RunId = "feature158-test"
      HostProfileId = profile.ProfileId
      PackageVersion = Compositor.Config.feature156PackageVersion
      DurationMs = 3.5
      MeasurementPolicy = policy
      InclusionStatus = status
      ExclusionReason = reason
      ArtifactPath = $"raw/{Compositor.FeatureState.feature158ScenarioFileName scenario}.csv" }

let private report scenario : Compositor.Types.Feature158ScenarioReport =
    let included = [ for i in 1..5 -> timingSample scenario i Perf.ReadbackFree Perf.Included None ]
    let excluded = [ timingSample scenario 99 Perf.ProbeReadbackIncluded Perf.Probe (Some Perf.ProbeRunExcluded) ]
    { ScenarioId = scenario
      ScenarioDefinitionId = "feature156-required-v1:" + (Compositor.FeatureState.feature158ScenarioFileName scenario).Replace(".md", "")
      FullRedraw = Some(dist $"raw/{Compositor.FeatureState.feature158ScenarioFileName scenario}-full.csv")
      DamageScoped = Some(dist $"raw/{Compositor.FeatureState.feature158ScenarioFileName scenario}-damage.csv")
      WarmupCount = 3
      MeasuredRepetitions = 5
      IncludedSamples = included
      ExcludedSamples = excluded
      ProofProbeArtifacts = [ "proof-probes/README.md" ]
      Status = Compositor.Types.Feature158ReadinessStatus.Accepted
      ArtifactPaths = [ $"scenarios/{Compositor.FeatureState.feature158ScenarioFileName scenario}" ]
      Diagnostics = [] }

let private summary : Compositor.Types.Feature158TimingSummary =
    let reports = Compositor.Config.feature158RequiredScenarioIds |> List.map report
    { RunId = "feature158-test"
      HostProfile = profile
      PolicyId = Compositor.Config.feature158PolicyId
      WarmupCount = 3
      MeasuredRepetitions = 5
      ScenarioReports = reports
      IncludedSamples = reports |> List.collect _.IncludedSamples
      ExcludedSamples = reports |> List.collect _.ExcludedSamples
      ProofProbeEvidence =
        [ { ProbeId = "probe"
            HostProfile = profile
            ScenarioIds = Compositor.Config.feature158RequiredScenarioIds
            ReadbackArtifacts = [ "proof-probes/probe.png" ]
            ProbeSampleIds = [ "probe-sample" ]
            ExclusionReason = Perf.ProbeRunExcluded
            Diagnostics = [] } ]
      UnsupportedHostReason = None
      Feature156Comparison = "contextualizes"
      Status = Compositor.Types.Feature158ReadinessStatus.Accepted
      PerformanceClaim = "performance-not-accepted"
      Diagnostics = [ "under-5-minute reviewer inspection evidence" ] }

[<Tests>]
let tests =
    testList "Feature158 readiness package" [
        test "timing summary exposes policy samples exclusions proof links and remaining gates" {
            let rendered = Compositor.Render3.emitFeature158TimingSummary summary
            [ "Feature 158 measurement-separation status: `accepted`"
              "Policy id: `readback-free-timing-v1`"
              "Included timing samples: `25`"
              "Excluded timing samples: `5`"
              "`probe-run-excluded`: `5`"
              "Proof/probe evidence: `../proof-probes/README.md`"
              "Feature 159 net-positive reuse/promotion counters"
              "Feature 161 host performance lane ledger"
              "Shipped P7 performance claim: `performance-not-accepted`" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "scenario report separates included timing from excluded probe readback" {
            let rendered = Compositor.Render3.emitFeature158ScenarioReport (report "timing/localized-update")
            Expect.stringContains rendered "Measurement-separation status: `accepted`" "accepted"
            Expect.stringContains rendered "`readback-free`" "included policy"
            Expect.stringContains rendered "`probe-readback-included`" "probe policy"
            Expect.stringContains rendered "`probe-run-excluded`" "probe exclusion"
        }

        test "validation summary links the reviewer entry package and preserves claim boundary" {
            let rendered = Compositor.Render3.emitFeature158ValidationSummary summary
            [ "timing/summary.md"
              "timing/summary.json"
              "timing/excluded/"
              "proof-probes/README.md"
              "compatibility-ledger.md"
              "package-validation.md"
              "regression-validation.md"
              "performance-not-accepted" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "compatibility package documents no new Testing or SkiaViewer helper surface" {
            let ledger = Compositor.Render3.emitFeature158CompatibilityLedger ()
            let package = Compositor.Render.renderPackageValidation 158 [ "`dotnet build FS.GG.Rendering.slnx --no-restore`: passed." ]
            Expect.stringContains ledger "No new `FS.GG.UI.Testing` public helper surface" "Testing no helper"
            Expect.stringContains ledger "No new `FS.GG.UI.SkiaViewer` public helper surface" "SkiaViewer no helper"
            Expect.stringContains package "No Testing or SkiaViewer package-visible helper surface" "package no helper"
        }
    ]
