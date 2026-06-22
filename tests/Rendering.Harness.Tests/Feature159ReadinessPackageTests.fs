module Feature159ReadinessPackageTests

open Expecto
open Rendering.Harness

let private profile : Compositor.Types.HostProfile =
    { ProfileId = Compositor.Config.feature159AcceptedProfileId
      Backend = "OpenGL"
      Renderer = Some "test"
      PresentMode = "DirectToSwapchain"
      FramebufferSize = "640x480"
      Scale = Some 1.0
      DisplayEnvironment = "x11"
      ProofAlgorithmVersion = "sentinel-damage-v1" }

let private attempt scenario : Compositor.Types.Feature159Attempt =
    { AttemptId = $"attempt-{scenario}"
      RunId = "run-159"
      ScenarioId = scenario
      HostProfile = profile
      PolicyId = Compositor.Config.feature159PolicyId
      PromotionDecision = "promoted"
      ReuseDecision = "content-reused-placement-updated"
      ContentIdentity = "content"
      PlacementIdentity = "placement"
      PrimaryReason = None
      CounterNetSavedWork = 10
      ParityStatus = "passed"
      AcceptedReuseArtifacts = 1
      AcceptedPromotionArtifacts = 1
      ArtifactPaths = [ $"promotion/attempts/{Compositor.FeatureState.feature159ScenarioFileName scenario}" ]
      Diagnostics = [] }

let private summary : Compositor.Types.Feature159Summary =
    { RunId = "run-159"
      HostProfile = profile
      PolicyId = Compositor.Config.feature159PolicyId
      Status = Compositor.Types.Feature159ReadinessStatus.Accepted
      Attempts = Compositor.Config.feature159RequiredScenarioIds |> List.map attempt
      UnsupportedHostReason = None
      RequiredScenarioCoverage = Compositor.Config.feature159RequiredScenarioIds
      CounterNetSavedWork = 70
      PerformanceClaim = "performance-not-accepted"
      Diagnostics = [] }

[<Tests>]
let tests =
    testList "Feature159 readiness package" [
        test "validation summary links required package artifacts and preserves claim boundary" {
            let rendered = Compositor.Render3.emitFeature159ValidationSummary summary
            [ "promotion/summary.md"
              "promotion/attempts/"
              "promotion/reuse/README.md"
              "promotion/unsupported/validation.md"
              "counters/promotion.md"
              "compatibility-ledger.md"
              "package-validation.md"
              "regression-validation.md"
              "performance-not-accepted" ]
            |> List.iter (fun required -> Expect.stringContains rendered required $"contains {required}")
        }

        test "overall status accepts covered same-profile net-positive parity attempts" {
            Expect.equal (Compositor.FeatureState.feature159OverallStatus summary) Compositor.Types.Feature159ReadinessStatus.Accepted "accepted"
            Expect.equal (Compositor.FeatureState.feature159StatusToken summary.Status) "accepted" "status token"
        }

        test "unsupported host report records zero accepted artifacts" {
            let rendered = Compositor.Render3.emitFeature159UnsupportedHostReport "missing display"
            Expect.stringContains rendered "Status: `environment-limited`" "status"
            Expect.stringContains rendered "Accepted Feature 159 reuse artifacts: `0`" "zero reuse"
            Expect.stringContains rendered "Accepted Feature 159 promotion artifacts: `0`" "zero promotion"
        }
    ]
