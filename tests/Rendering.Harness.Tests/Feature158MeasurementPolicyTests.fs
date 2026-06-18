module Feature158MeasurementPolicyTests

open System
open System.IO
open Expecto
open Rendering.Harness

let private sample policy status reason : Compositor.Feature158TimingSample =
    { SampleId = "feature158-sample"
      SampleIndex = 1
      ScenarioId = "timing/localized-update"
      ScenarioDefinitionId = "feature156-required-v1:timing-localized-update"
      Path = Perf.FullRedraw
      RunId = "feature158-test"
      HostProfileId = Compositor.feature158AcceptedProfileId
      PackageVersion = Compositor.feature156PackageVersion
      DurationMs = 1.25
      MeasurementPolicy = policy
      InclusionStatus = status
      ExclusionReason = reason
      ArtifactPath = "raw/timing-localized-update-full-redraw.csv" }

let private classifiedSample policy : Perf.ClassifiedTimingSample =
    { ScenarioId = "timing/localized-update"
      ScenarioDefinitionId = "feature156-required-v1:timing-localized-update"
      Path = Perf.FullRedraw
      RunId = "feature158-test"
      HostProfileId = Compositor.feature158AcceptedProfileId
      PackageVersion = Compositor.feature156PackageVersion
      DurationMs = 1.25
      MeasurementPolicy = policy
      InclusionStatus = Perf.Included
      ExclusionReason = None
      ArtifactPath = "raw/timing-localized-update-full-redraw.csv" }

[<Tests>]
let tests =
    testList "Feature158 measurement policy" [
        test "declares accepted profile policy required scenarios and command aliases" {
            Expect.equal Compositor.feature158AcceptedProfileId "probe-08a47c01" "accepted profile"
            Expect.equal Compositor.feature158PolicyId "readback-free-timing-v1" "policy"
            Expect.equal Compositor.feature158RequiredScenarioIds Compositor.feature156RequiredScenarioIds "same required scenario set"
            Expect.contains Compositor.feature158RequiredScenarioIds "timing/edge-clipping" "edge clipping"
            Expect.equal Compositor.feature158PerformanceCommand "compositor-performance --feature 158" "performance command"
            Expect.equal Compositor.feature158ProbeCommand "compositor-performance --feature 158 --probe-readback" "probe command"
        }

        test "classifies policy tokens and fail-closed exclusion reasons" {
            let cases =
                [ Perf.ReadbackFree, false, false, Perf.Included, None
                  Perf.ReadbackOutsideMeasurement, false, false, Perf.Included, None
                  Perf.ProbeReadbackIncluded, true, false, Perf.Probe, Some Perf.ProbeRunExcluded
                  Perf.Unverified, false, false, Perf.Excluded, Some Perf.UnverifiableMeasurementPolicy
                  Perf.Missing, false, false, Perf.Excluded, Some Perf.MissingMeasurementPolicy
                  Perf.ReadbackFree, false, true, Perf.Excluded, Some Perf.ProofReadbackInMeasuredInterval ]

            for policy, probe, readbackInside, expectedStatus, expectedReason in cases do
                let status, reason = Perf.classifyMeasurementPolicy policy probe readbackInside
                Expect.equal status expectedStatus (Perf.measurementPolicyToken policy)
                Expect.equal reason expectedReason (Perf.measurementPolicyToken policy + " reason")
        }

        test "sample classification rejects cross-run cross-profile package scenario and invalid durations" {
            let definitions = Map.ofList [ "timing/localized-update", "feature156-required-v1:timing-localized-update" ]
            let accepted = classifiedSample Perf.ReadbackFree

            Expect.equal (Perf.classifyTimingSample "feature158-test" Compositor.feature158AcceptedProfileId Compositor.feature156PackageVersion definitions accepted).InclusionStatus Perf.Included "accepted"
            Expect.equal (Perf.classifyTimingSample "other" Compositor.feature158AcceptedProfileId Compositor.feature156PackageVersion definitions accepted).ExclusionReason (Some Perf.RunIdentityMismatch) "run mismatch"
            Expect.equal (Perf.classifyTimingSample "feature158-test" "other-profile" Compositor.feature156PackageVersion definitions accepted).ExclusionReason (Some Perf.CrossProfileEvidence) "profile mismatch"
            Expect.equal (Perf.classifyTimingSample "feature158-test" Compositor.feature158AcceptedProfileId "other-package" definitions accepted).ExclusionReason (Some Perf.PackageVersionMismatch) "package mismatch"

            let invalid = { accepted with DurationMs = nan }
            Expect.equal (Perf.classifyTimingSample "feature158-test" Compositor.feature158AcceptedProfileId Compositor.feature156PackageVersion definitions invalid).ExclusionReason (Some Perf.UnverifiableMeasurementPolicy) "invalid duration"
        }

        test "MVU workflow records policy scenario probe evidence and summary publication" {
            let model0, effects0 = Compositor.initFeature158 3 5
            Expect.contains effects0 Compositor.Feature158DetectHostProfile "host detection"
            Expect.contains effects0 (Compositor.Feature158DeclarePolicy Compositor.feature158PolicyId) "policy declaration"

            let profile : Compositor.HostProfile =
                { ProfileId = Compositor.feature158AcceptedProfileId
                  Backend = "OpenGL"
                  Renderer = Some "test-renderer"
                  PresentMode = "DirectToSwapchain"
                  FramebufferSize = "640x480"
                  Scale = Some 1.0
                  DisplayEnvironment = "x11"
                  ProofAlgorithmVersion = "sentinel-damage-v1" }

            let report : Compositor.Feature158ScenarioReport =
                { ScenarioId = "timing/localized-update"
                  ScenarioDefinitionId = "feature156-required-v1:timing-localized-update"
                  FullRedraw = None
                  DamageScoped = None
                  WarmupCount = 3
                  MeasuredRepetitions = 5
                  IncludedSamples = [ sample Perf.ReadbackFree Perf.Included None ]
                  ExcludedSamples = [ sample Perf.ProbeReadbackIncluded Perf.Probe (Some Perf.ProbeRunExcluded) ]
                  ProofProbeArtifacts = [ "proof-probes/README.md" ]
                  Status = Compositor.Feature158ReadinessStatus.Accepted
                  ArtifactPaths = [ "timing/scenarios/timing-localized-update.md" ]
                  Diagnostics = [] }

            let evidence : Compositor.Feature158ProofProbeEvidence =
                { ProbeId = "probe"
                  HostProfile = profile
                  ScenarioIds = [ "timing/localized-update" ]
                  ReadbackArtifacts = [ "proof-probes/probe.png" ]
                  ProbeSampleIds = [ "probe-sample" ]
                  ExclusionReason = Perf.ProbeRunExcluded
                  Diagnostics = [] }

            let model1, _ = Compositor.updateFeature158 (Compositor.Feature158HostProfileDetected profile) model0
            let model2, _ = Compositor.updateFeature158 (Compositor.Feature158PolicyDeclared Compositor.feature158PolicyId) model1
            let model3, _ = Compositor.updateFeature158 (Compositor.Feature158ScenarioEvaluated report) model2
            let model4, _ = Compositor.updateFeature158 (Compositor.Feature158ProbeEvidenceRecorded evidence) model3
            let model5, _ = Compositor.updateFeature158 (Compositor.Feature158SummaryPublished "timing/summary.md") model4

            Expect.equal model5.ActiveProfile (Some profile) "profile"
            Expect.equal model5.PolicyId (Some Compositor.feature158PolicyId) "policy"
            Expect.contains model5.PublishedArtifacts "timing/summary.md" "summary"
            Expect.equal model5.ProofProbeEvidence.Length 1 "probe evidence"
        }

        test "explicit probe command does not overwrite existing timing summary" {
            let root = Path.Combine(Path.GetTempPath(), "feature158-probe-preserves-summary-" + Guid.NewGuid().ToString("N"))
            let timingDir = Path.Combine(root, "timing")

            try
                let timingExit =
                    Cli.main
                        [| "compositor-performance"
                           "--feature"
                           "158"
                           "--out"
                           timingDir
                           "--policy"
                           Compositor.feature158PolicyId
                           "--warmup"
                           "1"
                           "--repetitions"
                           "1"
                           "--scenario"
                           "timing/localized-update" |]

                Expect.equal timingExit 0 "timing command exits cleanly"
                let summaryPath = Path.Combine(timingDir, "summary.md")
                Expect.isTrue (File.Exists summaryPath) "timing summary exists before probe"
                let beforeProbe = File.ReadAllText summaryPath

                let probeExit =
                    Cli.main
                        [| "compositor-performance"
                           "--feature"
                           "158"
                           "--probe-readback"
                           "--out"
                           timingDir
                           "--scenario"
                           "timing/localized-update" |]

                Expect.equal probeExit 0 "probe command exits cleanly"
                Expect.equal (File.ReadAllText summaryPath) beforeProbe "probe command preserves the accepted timing summary"

                let excludedDir = Path.Combine(timingDir, "excluded")
                let hasProbeOrUnsupportedExclusion =
                    File.Exists(Path.Combine(excludedDir, "probe-run-excluded.md"))
                    || File.Exists(Path.Combine(excludedDir, "environment-limited.md"))

                Expect.isTrue hasProbeOrUnsupportedExclusion "probe command writes excluded probe or unsupported-host evidence"
            finally
                if Directory.Exists root then
                    Directory.Delete(root, true)
        }
    ]
