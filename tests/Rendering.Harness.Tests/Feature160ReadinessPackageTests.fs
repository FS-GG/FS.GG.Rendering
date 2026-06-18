module Feature160ReadinessPackageTests

open System
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

let private excludedIteration : Compositor.Feature160Iteration =
    { IterationId = "feature160-excluded"
      RunId = "feature160-readiness"
      HostProfile = profile
      LaneId = Compositor.feature160FocusedLaneId
      PolicyId = Compositor.feature160PolicyId
      DeclaredBoundMinutes = 10
      ActualDuration = TimeSpan.FromSeconds 30.0
      WarmupCount = 3
      MeasuredRepetitions = 5
      ScenarioReports = []
      ScenarioCoverage = []
      IncludedSamples = []
      ExcludedSamples = []
      Status = Compositor.Feature160ReadinessStatus.Rejected
      ExclusionReason = Some Perf.PartialEvidence
      ArtifactPaths = [ "throughput/iterations/feature160-excluded.md" ]
      RestrictedScenario = None
      Diagnostics = [ "partial output was not accepted" ] }

let private summary : Compositor.Feature160ThroughputSummary =
    { RunId = "feature160-readiness"
      HostProfile = profile
      LaneId = Compositor.feature160FocusedLaneId
      PolicyId = Compositor.feature160PolicyId
      DeclaredBoundMinutes = 10
      RequiredAttempts = 3
      WarmupCount = 3
      MeasuredRepetitions = 5
      Iterations = [ excludedIteration ]
      UnsupportedHostReason = Some "missing display"
      FullValidation = None
      CompatibilityImpact = "Feature160ThroughputReadiness helper added"
      PackageValidationStatus = "accepted-with-recorded-limitations"
      RegressionValidationStatus = "accepted-with-recorded-limitations"
      Status = Compositor.Feature160ReadinessStatus.EnvironmentLimited
      ReleaseReadyStatus = "blocked"
      PerformanceClaim = "performance-not-accepted"
      Diagnostics = [ "noisy same-profile timing remains a performance-claim gate" ] }

[<Tests>]
let tests =
    testList "Feature160 readiness package" [
        test "validation summary links reviewer entry point artifacts and performance claim boundary" {
            let rendered = Compositor.renderFeature160ValidationSummary summary
            [ "throughput/summary.md"
              "throughput/iterations/"
              "throughput/excluded/"
              "throughput/unsupported/README.md"
              "full-validation/validation.md"
              "compatibility-ledger.md"
              "package-validation.md"
              "regression-validation.md"
              "performance-not-accepted"
              "Under-5-minute reviewer decision target" ]
            |> List.iter (fun required -> Expect.stringContains rendered required $"contains {required}")
        }

        test "unsupported host and excluded evidence record zero accepted performance contribution" {
            let unsupported = Compositor.renderFeature160UnsupportedHostReport "missing display"
            Expect.stringContains unsupported "Status: `environment-limited`" "status"
            Expect.stringContains unsupported "Accepted same-profile performance artifacts: `0`" "zero artifacts"

            let excluded = Compositor.renderFeature160ExcludedEvidenceReport Perf.PartialEvidence [ excludedIteration ]
            Expect.stringContains excluded "Primary reason: `partial-evidence`" "reason"
            Expect.stringContains excluded "Accepted throughput contribution: `0`" "zero contribution"
        }

        test "missing full validation template is an explicit release-ready blocker" {
            let rendered = Compositor.renderFeature160FullValidationRecord None
            Expect.stringContains rendered "Status: `missing`" "missing"
            Expect.stringContains rendered "Release-ready blocker: `full-validation-missing`" "blocker"
            Expect.stringContains rendered "intentionally not run inside focused throughput collection" "separation"
        }
    ]
