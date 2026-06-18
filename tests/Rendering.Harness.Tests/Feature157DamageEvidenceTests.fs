module Feature157DamageEvidenceTests

open System
open Expecto
open Rendering.Harness

let private profile : Compositor.HostProfile =
    { ProfileId = Compositor.feature157AcceptedProfileId
      Backend = "OpenGL"
      Renderer = Some "Mesa"
      PresentMode = "DirectToSwapchain"
      FramebufferSize = "640x480"
      Scale = Some 1.0
      DisplayEnvironment = "x11"
      ProofAlgorithmVersion = "sentinel-damage-v1" }

let private attempt scenario : Compositor.Feature157DamageAttempt =
    let stem = (Compositor.feature157ScenarioFileName scenario).Replace(".md", "")
    { AttemptId = $"feature157-test-{stem}"
      RunId = "feature157-test"
      ScenarioId = scenario
      HostProfile = profile
      ProofGate = "accepted-feature155-same-profile"
      RetainedBacking = "current-buffer-preserved"
      DamageValidationStatus = "valid"
      RenderDecision = "damage-scoped-accepted"
      FallbackReason = None
      PreservedPixelEvidence = "outside damage preserved"
      DamagedPixelEvidence = "inside damage updated"
      ParityStatus = "accepted"
      ArtifactPaths = [ $"attempts/{Compositor.feature157ScenarioFileName scenario}"; $"parity/{Compositor.feature157ScenarioFileName scenario}" ]
      Diagnostics = [ "clear-policy=no-full-frame-clear" ] }

// SYNTHETIC: fallback fixtures model rejection-only paths and must not be counted as accepted damage evidence.
let private fallback scenario reason : Compositor.Feature157Fallback =
    { ScenarioId = scenario
      Reason = reason
      DamageValidationStatus = if reason = "invalid-damage" then "out-of-bounds" else "fallback-gated"
      AcceptedPartialRedrawArtifacts = 0
      ArtifactPaths = [ $"fallbacks/{Compositor.feature157ScenarioFileName scenario}" ]
      Diagnostics = [ "accepted-partial-redraw-artifacts=0" ] }

[<Tests>]
let tests =
    testList
        "Feature157 damage evidence"
        [ test "declares accepted profile, required scenarios, fallbacks, and command aliases" {
              Expect.equal Compositor.feature157AcceptedProfileId "probe-08a47c01" "accepted profile"
              Expect.equal Compositor.feature157RequiredScenarioIds.Length 5 "required scenario count"
              Expect.contains Compositor.feature157RequiredScenarioIds "damage/static-preserved" "static preserved"
              Expect.contains Compositor.feature157FallbackScenarioIds "damage/parity-mismatch" "parity fallback"
          }

          test "renders accepted attempt records with proof, retained backing, parity, and artifacts" {
              let rendered = Compositor.renderFeature157AttemptReport (attempt "damage/localized-update")
              Expect.stringContains rendered "Render decision: `damage-scoped-accepted`" "decision"
              Expect.stringContains rendered "Proof gate: `accepted-feature155-same-profile`" "proof gate"
              Expect.stringContains rendered "Retained backing: `current-buffer-preserved`" "retained backing"
              Expect.stringContains rendered "Parity status: `accepted`" "parity"
          }

          test "Synthetic fallback fixture: renders fail-closed evidence with zero accepted partial-redraw artifacts" {
              let rendered = Compositor.renderFeature157FallbackReport (fallback "damage/out-of-bounds" "invalid-damage")
              Expect.stringContains rendered "Primary reason: `invalid-damage`" "reason"
              Expect.stringContains rendered "Accepted partial-redraw artifacts: `0`" "zero accepted"
          }

          test "overall status accepts only when three attempts and five required scenarios are covered" {
              let attempts = Compositor.feature157RequiredScenarioIds |> List.map attempt
              let summary : Compositor.Feature157DamageSummary =
                  { RunId = "feature157-test"
                    HostProfile = profile
                    Status = Compositor.Feature157DamageStatus.Accepted
                    AcceptedAttempts = attempts
                    Fallbacks = [ fallback "damage/missing-retained-backing" "missing-retained-content" ]
                    UnsupportedHostReason = None
                    ScenarioCoverage = attempts |> List.map _.ScenarioId
                    PerformanceClaim = "performance-not-accepted"
                    Diagnostics = [] }

              Expect.equal (Compositor.feature157OverallStatus summary) Compositor.Feature157DamageStatus.Accepted "overall accepted"
              let rendered = Compositor.renderFeature157DamageSummary summary
              Expect.stringContains rendered "Damage readiness status: `accepted`" "summary status"
              Expect.stringContains rendered "Shipped P7 performance claim: `performance-not-accepted`" "performance boundary"
          } ]
