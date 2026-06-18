module Feature157DamageReadinessHelperTests

open Expecto
open FS.GG.UI.Testing

let private scenario status id =
    { ScenarioId = id
      Status = status
      AcceptedAttemptCount = if status = CompositorDamageAccepted then 3 else 0
      ArtifactPaths = [ $"damage/{id}.md" ]
      FallbackReason = if status = CompositorDamageAccepted then None else Some "full-redraw-fallback" }

let private required =
    [ "damage/static-preserved"
      "damage/localized-update"
      "damage/movement-old-new"
      "damage/scroll-shifted"
      "damage/nested-retained" ]

// SYNTHETIC: helper checks use in-memory scenario rows so rejection and fallback states stay separate from real damage artifacts.
let private check status =
    { Feature = "157-no-clear-damage-scissor"
      RequiredScenarioIds = required
      Scenarios = required |> List.map (scenario status)
      AcceptedAttemptCount = if status = CompositorDamageAccepted then 5 else 0
      UnsupportedHostStatus = CompositorDamageEnvironmentLimited
      AcceptedPartialRedrawArtifacts = 0
      CompatibilityAccepted = true
      PackageAccepted = true
      RegressionAccepted = true
      PerformanceClaim = "performance-not-accepted"
      Limitations = [] }

[<Tests>]
let tests =
    testList
        "Feature157 damage readiness helper"
        [ test "accepts complete same-profile damage readiness without accepting performance" {
              let result = CompositorDamageReadiness.validate (check CompositorDamageAccepted)
              Expect.isTrue result.Accepted "accepted"
              Expect.equal result.Status CompositorDamageAccepted "status"
              Expect.equal (CompositorDamageReadiness.statusText result.Status) "accepted" "status text"
          }

          test "Synthetic helper fixture: keeps fallback-only damage readiness non-accepting but reviewable" {
              let result = CompositorDamageReadiness.validate (check CompositorDamageFallbackOnly)
              Expect.isFalse result.Accepted "not accepted"
              Expect.equal result.Status CompositorDamageFallbackOnly "fallback-only"
              Expect.isNonEmpty result.Diagnostics "fallback diagnostics"
          }

          test "Synthetic helper fixture: rejects missing scenarios and bad performance claim" {
              let broken =
                  { check CompositorDamageAccepted with
                      RequiredScenarioIds = required @ [ "damage/missing" ]
                      PerformanceClaim = "accepted" }

              let result = CompositorDamageReadiness.validate broken
              Expect.equal result.Status CompositorDamageRejected "rejected"
              Expect.contains result.MissingScenarios "damage/missing" "missing scenario"
              Expect.exists result.Diagnostics (fun item -> item.Contains("performance claim")) "performance boundary"
          }

          test "Synthetic helper fixture: environment-limited unsupported host requires zero accepted partial-redraw artifacts" {
              let limited =
                  { check CompositorDamageAccepted with
                      UnsupportedHostStatus = CompositorDamageEnvironmentLimited
                      AcceptedPartialRedrawArtifacts = 1 }

              let result = CompositorDamageReadiness.validate limited
              Expect.equal result.Status CompositorDamageEnvironmentLimited "environment limited"
              Expect.exists result.Diagnostics (fun item -> item.Contains("zero accepted partial-redraw")) "zero accepted diagnostic"
          } ]
