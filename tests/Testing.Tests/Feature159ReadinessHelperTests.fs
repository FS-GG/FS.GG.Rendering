module Feature159ReadinessHelperTests

open Expecto
open FS.GG.UI.Testing

let private required =
    [ "promotion/static-retained"
      "promotion/placement-only-move"
      "promotion/scroll-shifted"
      "promotion/nested-retained"
      "promotion/content-change"
      "promotion/churn-demotion"
      "promotion/fallback-safe" ]

let private scenario status id =
    { ScenarioId = id
      Status = status
      PromotionDecision = if status = Feature159Accepted then "promoted" else Feature159Readiness.statusText status
      ReuseDecision = if status = Feature159Accepted then "content-reused-placement-updated" else "fallback-full-redraw"
      AcceptedAttemptCount = if status = Feature159Accepted then 3 else 0
      CounterNetSavedWork = if status = Feature159Accepted then 10 else 0
      ParityPassed = status <> Feature159Rejected
      ArtifactPaths = [ $"promotion/attempts/{id}.md" ]
      PrimaryReason = if status = Feature159Accepted then None else Some(Feature159Readiness.statusText status) }

let private check status =
    { Feature = "159-layer-promotion-keys"
      RequiredScenarioIds = required
      Scenarios = required |> List.map (scenario status)
      AcceptedAttemptCount = if status = Feature159Accepted then 3 else 0
      UnsupportedHostStatus = Feature159EnvironmentLimited
      AcceptedReuseArtifacts = 0
      AcceptedPromotionArtifacts = 0
      CompatibilityAccepted = true
      PackageAccepted = true
      RegressionAccepted = true
      PerformanceClaim = "performance-not-accepted"
      Limitations = [] }

[<Tests>]
let tests =
    testList "Feature159 readiness helper" [
        test "accepts complete net-positive readiness without accepting shipped performance" {
            let result = Feature159Readiness.validate (check Feature159Accepted)
            Expect.isTrue result.Accepted "accepted"
            Expect.equal result.Status Feature159Accepted "status"
            Expect.equal (Feature159Readiness.statusText result.Status) "accepted" "token"
        }

        test "Synthetic helper fixture: non-beneficial fallback and rejected states fail closed" {
            // SYNTHETIC: in-memory status rows exercise package helper rejection policy without live GL artifacts.
            [ Feature159NonBeneficial; Feature159FallbackOnly; Feature159Rejected; Feature159EnvironmentLimited ]
            |> List.iter (fun status ->
                let result = Feature159Readiness.validate (check status)
                Expect.isFalse result.Accepted $"status {status} is not accepted")
        }

        test "Synthetic helper fixture: missing scenarios and performance overclaim are rejected" {
            let invalid =
                { check Feature159Accepted with
                    RequiredScenarioIds = required @ [ "promotion/missing" ]
                    PerformanceClaim = "accepted" }

            let result = Feature159Readiness.validate invalid
            Expect.equal result.Status Feature159Rejected "rejected"
            Expect.contains result.MissingScenarios "promotion/missing" "missing"
            Expect.exists result.Diagnostics (fun item -> item.Contains("performance claim")) "claim boundary"
        }
    ]
