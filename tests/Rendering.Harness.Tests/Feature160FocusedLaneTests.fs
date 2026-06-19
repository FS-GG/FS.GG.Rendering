module Feature160FocusedLaneTests

open System
open System.IO
open Expecto
open Rendering.Harness

let private profile : Compositor.HostProfile =
    { ProfileId = Compositor.feature160AcceptedProfileId
      Backend = "OpenGL"
      Renderer = Some "test-renderer"
      PresentMode = "DirectToSwapchain"
      FramebufferSize = "640x480"
      Scale = Some 1.0
      DisplayEnvironment = "x11"
      ProofAlgorithmVersion = "sentinel-damage-v1" }

let private sample scenario : Compositor.Feature158TimingSample =
    { SampleId = $"feature160-{scenario}"
      SampleIndex = 1
      ScenarioId = scenario
      ScenarioDefinitionId = "feature156-required-v1:" + scenario.Replace("/", "-")
      Path = Perf.FullRedraw
      RunId = "feature160-test"
      HostProfileId = Compositor.feature160AcceptedProfileId
      PackageVersion = Compositor.feature156PackageVersion
      DurationMs = 1.0
      MeasurementPolicy = Perf.ReadbackFree
      InclusionStatus = Perf.Included
      ExclusionReason = None
      ArtifactPath = $"throughput/raw/{Compositor.feature160ScenarioFileName scenario}.csv" }

let private scenarioReport scenario : Compositor.Feature158ScenarioReport =
    { ScenarioId = scenario
      ScenarioDefinitionId = "feature156-required-v1:" + scenario.Replace("/", "-")
      FullRedraw = None
      DamageScoped = None
      WarmupCount = 3
      MeasuredRepetitions = 5
      IncludedSamples = [ sample scenario ]
      ExcludedSamples = []
      ProofProbeArtifacts = []
      Status = Compositor.Feature158ReadinessStatus.Accepted
      ArtifactPaths = [ $"throughput/iterations/{Compositor.feature160ScenarioFileName scenario}" ]
      Diagnostics = [] }

let private acceptedIteration index : Compositor.Feature160Iteration =
    let iterationId = sprintf "feature160-test-%03i" index
    let reports = Compositor.feature160RequiredScenarioIds |> List.map scenarioReport
    { IterationId = iterationId
      RunId = "feature160-test"
      HostProfile = profile
      LaneId = Compositor.feature160FocusedLaneId
      PolicyId = Compositor.feature160PolicyId
      DeclaredBoundMinutes = Compositor.feature160MaxIterationMinutes
      ActualDuration = TimeSpan.FromMinutes 1.0
      WarmupCount = 3
      MeasuredRepetitions = 5
      ScenarioReports = reports
      ScenarioCoverage = Compositor.feature160RequiredScenarioIds
      IncludedSamples = reports |> List.collect _.IncludedSamples
      ExcludedSamples = []
      Status = Compositor.Feature160ReadinessStatus.Accepted
      ExclusionReason = None
      ArtifactPaths = [ $"throughput/iterations/{Compositor.feature160IterationFileName iterationId}" ]
      RestrictedScenario = None
      Diagnostics = [ "broad full validation was not invoked" ] }

[<Tests>]
let tests =
    testList "Feature160 focused lane" [
        test "declares focused lane constants scenarios bound and exclusion tokens" {
            Expect.equal Compositor.feature160PolicyId "focused-throughput-v1" "policy"
            Expect.equal Compositor.feature160FocusedLaneId "focused" "lane"
            Expect.equal Compositor.feature160MaxIterationMinutes 10 "bound"
            Expect.equal Compositor.feature160RequiredAttempts 3 "attempts"
            Expect.equal Compositor.feature160RequiredScenarioIds Compositor.feature158RequiredScenarioIds "Feature 158 scenario set"
            Expect.contains Compositor.feature160RequiredScenarioIds "timing/edge-clipping" "edge clipping"
            Expect.contains Compositor.feature160ScenarioIds "timing/sparse-heavy-localized-update" "damage-friendly debug scenario"
            Expect.isFalse (Compositor.feature160RequiredScenarioIds |> List.contains "timing/sparse-heavy-localized-update") "debug scenario is not an acceptance requirement"

            [ Perf.TimedOut, "timed-out"
              Perf.Canceled, "canceled"
              Perf.PartialEvidence, "partial-evidence"
              Perf.CrossProfileEvidence, "cross-profile-evidence"
              Perf.StaleEvidence, "stale-evidence"
              Perf.MixedPolicy, "mixed-policy"
              Perf.MissingMetadata, "missing-metadata"
              Perf.UnsupportedHost, "unsupported-host"
              Perf.EnvironmentLimitedReason, "environment-limited"
              Perf.ScenarioCoverageMissing, "scenario-coverage-missing"
              Perf.SamplePolicyMismatch, "sample-policy-mismatch"
              Perf.RunIdentityMismatch, "run-identity-mismatch"
              Perf.ArtifactUnreadable, "artifact-unreadable"
              Perf.ReadbackContaminated, "readback-contaminated" ]
            |> List.iter (fun (reason, token) -> Expect.equal (Perf.exclusionReasonToken reason) token token)
        }

        test "MVU declares host lane policy bound timeout and iteration publication effects" {
            let model0, effects0 = Compositor.initFeature160 3 10
            Expect.contains effects0 Compositor.Feature160DetectHostProfile "host detection"
            Expect.contains effects0 (Compositor.Feature160DeclareFocusedLane Compositor.feature160FocusedLaneId) "lane"
            Expect.contains effects0 (Compositor.Feature160DeclarePolicy Compositor.feature160PolicyId) "policy"
            Expect.contains effects0 (Compositor.Feature160DeclareIterationBound 10) "bound"

            let iteration = acceptedIteration 1
            let model1, _ = Compositor.updateFeature160 (Compositor.Feature160HostProfileDetected profile) model0
            let model2, _ = Compositor.updateFeature160 (Compositor.Feature160LaneDeclared Compositor.feature160FocusedLaneId) model1
            let model3, _ = Compositor.updateFeature160 (Compositor.Feature160PolicyDeclared Compositor.feature160PolicyId) model2
            let model4, _ = Compositor.updateFeature160 (Compositor.Feature160BoundDeclared 10) model3
            let model5, _ = Compositor.updateFeature160 (Compositor.Feature160IterationCompleted iteration) model4
            let model6, _ = Compositor.updateFeature160 (Compositor.Feature160ArtifactPublished "throughput/summary.md") model5

            Expect.equal model6.ActiveProfile (Some profile) "profile"
            Expect.equal model6.LaneId (Some Compositor.feature160FocusedLaneId) "lane"
            Expect.equal model6.PolicyId (Some Compositor.feature160PolicyId) "policy"
            Expect.equal model6.DeclaredBoundMinutes (Some 10) "bound"
            Expect.equal model6.Iterations.Length 1 "iteration"
            Expect.contains model6.PublishedArtifacts "throughput/summary.md" "summary"
        }

        test "restricted single-scenario debugging cannot satisfy final throughput acceptance" {
            let restricted =
                { acceptedIteration 1 with
                    Status = Compositor.Feature160ReadinessStatus.Rejected
                    ExclusionReason = Some Perf.ScenarioCoverageMissing
                    RestrictedScenario = Some "timing/no-change"
                    ScenarioCoverage = [ "timing/no-change" ] }

            let summary : Compositor.Feature160ThroughputSummary =
                { RunId = "feature160-test"
                  HostProfile = profile
                  LaneId = Compositor.feature160FocusedLaneId
                  PolicyId = Compositor.feature160PolicyId
                  DeclaredBoundMinutes = 10
                  RequiredAttempts = 3
                  WarmupCount = 3
                  MeasuredRepetitions = 5
                  Iterations = [ restricted ]
                  UnsupportedHostReason = None
                  FullValidation = None
                  CompatibilityImpact = "test"
                  PackageValidationStatus = "test"
                  RegressionValidationStatus = "test"
                  Status = Compositor.Feature160ReadinessStatus.Rejected
                  ReleaseReadyStatus = "blocked"
                  PerformanceClaim = "performance-not-accepted"
                  Diagnostics = [] }

            Expect.equal (Compositor.feature160FocusedThroughputStatus summary) Compositor.Feature160ReadinessStatus.Rejected "restricted scenario rejected"
            let rendered = Compositor.renderFeature160ThroughputSummary summary
            Expect.stringContains rendered "scenario-coverage-missing" "reason"
            Expect.stringContains rendered "Focused throughput collection does not run" "no broad suite"
        }

        test "CLI writes focused package and preserves broad validation separation" {
            let root = Path.Combine(Path.GetTempPath(), "feature160-focused-" + Guid.NewGuid().ToString("N"))
            try
                let exitCode =
                    Cli.main
                        [| "compositor-performance"
                           "--feature"
                           "160"
                           "--lane"
                           "focused"
                           "--out"
                           root
                           "--policy"
                           Compositor.feature160PolicyId
                           "--attempts"
                           "1"
                           "--max-iteration-minutes"
                           "10"
                           "--profile"
                           "impossible-profile"
                           "--json" |]

                Expect.equal exitCode 0 "command exits cleanly"
                let summary = Path.Combine(root, "summary.md")
                Expect.isTrue (File.Exists summary) "summary exists"
                let text = File.ReadAllText summary
                Expect.stringContains text "Declared per-iteration bound minutes: `10`" "bound"
                Expect.stringContains text "Full validation is recorded separately" "release gate separation"
                Expect.isTrue (File.Exists(Path.Combine(root, "summary.json"))) "json summary exists"
                Expect.isTrue (Directory.Exists(Path.Combine(root, "excluded"))) "excluded dir"
            finally
                if Directory.Exists root then
                    Directory.Delete(root, true)
        }
    ]
