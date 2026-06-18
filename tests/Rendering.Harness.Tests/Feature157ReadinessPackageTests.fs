module Feature157ReadinessPackageTests

open Expecto
open Rendering.Harness

let private profile : Compositor.HostProfile =
    { ProfileId = Compositor.feature157AcceptedProfileId
      Backend = "OpenGL"
      Renderer = None
      PresentMode = "DirectToSwapchain"
      FramebufferSize = "640x480"
      Scale = Some 1.0
      DisplayEnvironment = "x11"
      ProofAlgorithmVersion = "sentinel-damage-v1" }

let private attempt scenario : Compositor.Feature157DamageAttempt =
    { AttemptId = $"feature157-{Compositor.feature157ScenarioFileName scenario}"
      RunId = "feature157-test"
      ScenarioId = scenario
      HostProfile = profile
      ProofGate = "accepted-feature155-same-profile"
      RetainedBacking = "retained-frame-restored"
      DamageValidationStatus = "valid"
      RenderDecision = "damage-scoped-accepted"
      FallbackReason = None
      PreservedPixelEvidence = "preserved"
      DamagedPixelEvidence = "updated"
      ParityStatus = "accepted"
      ArtifactPaths = [ $"attempts/{Compositor.feature157ScenarioFileName scenario}" ]
      Diagnostics = [] }

let private summary : Compositor.Feature157DamageSummary =
    let attempts = Compositor.feature157RequiredScenarioIds |> List.map attempt
    { RunId = "feature157-test"
      HostProfile = profile
      Status = Compositor.Feature157DamageStatus.Accepted
      AcceptedAttempts = attempts
      Fallbacks = []
      UnsupportedHostReason = None
      ScenarioCoverage = attempts |> List.map _.ScenarioId
      PerformanceClaim = "performance-not-accepted"
      Diagnostics = [ "under-5-minute reviewer determination check passed" ] }

[<Tests>]
let tests =
    testList
        "Feature157 readiness package"
        [ test "validation summary links every reviewer-facing artifact class" {
              let rendered = Compositor.renderFeature157ValidationSummary summary
              [ "damage/summary.md"
                "damage/summary.json"
                "damage/attempts/"
                "damage/fallbacks/"
                "damage/parity/"
                "damage/unsupported/README.md"
                "compatibility-ledger.md"
                "package-validation.md"
                "regression-validation.md"
                "fsi/compositor-damage-authoring.fsx"
                "fsi/compositor-readiness-authoring.fsx" ]
              |> List.iter (fun required -> Expect.stringContains rendered required $"summary links {required}")
          }

          test "compatibility ledger preserves Feature155 and Feature156 boundaries" {
              let ledger = Compositor.renderFeature157CompatibilityLedger ()
              Expect.stringContains ledger "Feature 155 proof-set" "Feature155 boundary"
              Expect.stringContains ledger "Feature 156 timing" "Feature156 boundary"
              Expect.stringContains ledger "performance-not-accepted" "performance claim boundary"
          }

          test "summary JSON exposes status, attempts, fallback counts, profile, and performance claim" {
              let json = Compositor.renderFeature157DamageSummaryJson summary
              Expect.stringContains json "\"status\": \"accepted\"" "status"
              Expect.stringContains json "\"acceptedAttemptCount\": 5" "attempt count"
              Expect.stringContains json "\"fallbackCount\": 0" "fallback count"
              Expect.stringContains json "\"hostProfile\": \"probe-08a47c01\"" "profile"
              Expect.stringContains json "\"performanceClaim\": \"performance-not-accepted\"" "claim"
          }

          test "package and regression validation render the current status and no universal performance claim" {
              let package = Compositor.renderFeature157PackageValidation [ "`dotnet build FS.GG.Rendering.slnx --no-restore`: passed." ]
              let regression = Compositor.renderFeature157RegressionValidation [ "`dotnet test FS.GG.Rendering.slnx --no-restore`: passed." ]
              Expect.stringContains package "Status: `accepted-with-recorded-limitations`" "package status"
              Expect.stringContains regression "Feature 156 timing remains context-only" "timing boundary"
              Expect.stringContains regression "performance-not-accepted" "performance boundary"
          } ]
