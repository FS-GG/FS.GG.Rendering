module Feature159PromotionEvidenceTests

open Expecto
open Rendering.Harness

let private profile : Compositor.HostProfile =
    { ProfileId = Compositor.feature159AcceptedProfileId
      Backend = "OpenGL"
      Renderer = Some "test"
      PresentMode = "DirectToSwapchain"
      FramebufferSize = "640x480"
      Scale = Some 1.0
      DisplayEnvironment = "x11"
      ProofAlgorithmVersion = "sentinel-damage-v1" }

let private attempt scenario promotion reuse reason net : Compositor.Feature159Attempt =
    { AttemptId = $"attempt-{scenario}"
      RunId = "run-159"
      ScenarioId = scenario
      HostProfile = profile
      PolicyId = Compositor.feature159PolicyId
      PromotionDecision = promotion
      ReuseDecision = reuse
      ContentIdentity = "content"
      PlacementIdentity = "placement"
      PrimaryReason = reason
      CounterNetSavedWork = net
      ParityStatus = "passed"
      AcceptedReuseArtifacts = if reuse = "content-reused-placement-updated" then 1 else 0
      AcceptedPromotionArtifacts = if promotion = "promoted" then 1 else 0
      ArtifactPaths = [ $"promotion/attempts/{Compositor.feature159ScenarioFileName scenario}" ]
      Diagnostics = [] }

[<Tests>]
let tests =
    testList "Feature159 promotion evidence" [
        test "required scenario inventory includes promotion and fail-closed paths" {
            [ "promotion/static-retained"
              "promotion/nested-retained"
              "promotion/placement-only-move"
              "promotion/churn-demotion"
              "promotion/fallback-safe" ]
            |> List.iter (fun scenario ->
                Expect.contains Compositor.feature159ScenarioIds scenario $"scenario {scenario}")
        }

        test "promotion attempt report renders split identity and counters" {
            let rendered = Compositor.renderFeature159AttemptReport (attempt "promotion/placement-only-move" "promoted" "content-reused-placement-updated" None 10)
            Expect.stringContains rendered "Promotion decision: `promoted`" "promotion"
            Expect.stringContains rendered "Reuse decision: `content-reused-placement-updated`" "reuse"
            Expect.stringContains rendered "Content identity: `content`" "content identity"
            Expect.stringContains rendered "Placement identity: `placement`" "placement identity"
        }

        test "workflow records attempts and publishes artifacts as effects" {
            let model, effects = Compositor.initFeature159 ()
            Expect.contains effects (Compositor.Feature159DeclarePolicy Compositor.feature159PolicyId) "policy effect"
            let model', effects' =
                Compositor.updateFeature159
                    (Compositor.Feature159AttemptRecorded(attempt "promotion/static-retained" "promoted" "content-recorded" None 12))
                    model
            Expect.equal model'.Attempts.Length 1 "attempt recorded"
            Expect.contains effects' (Compositor.Feature159WriteArtifact Compositor.feature159PromotionSummaryPath) "summary effect"
        }
    ]
