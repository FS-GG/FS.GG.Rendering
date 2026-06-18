module Feature149LiveProofEvidenceTests

open System
open Expecto
open Rendering.Harness

let private facts =
    { EffectiveBackend = NoDisplay
      Display = None
      GlRenderer = None
      GlVersion = None
      GlDirect = false
      RefreshHz = None
      Extensions = []
      SwapControl = None
      VblankSource = None
      UinputAvailable = false }

[<Tests>]
let tests =
    testList "Feature149 live proof evidence" [
        test "Feature149 formatter records host facts, package version, and acceptance gate" {
            let profile = Compositor.hostProfileFromFacts facts
            let proof: Compositor.PresentProof =
                { ProofId = "proof-149"
                  HostProfile = profile
                  ScenarioId = "proof/live-sentinel-damage-v1"
                  Verdict = Compositor.ProofEnvironmentLimited "missing display"
                  CreatedAt = DateTimeOffset.UnixEpoch
                  EvidenceArtifacts = [ "proof.md"; "limitations.md" ]
                  Diagnostics = [ "verdict=environment-limited" ] }

            let rendered = Compositor.renderFeature149LiveProof proof

            Expect.stringContains rendered "# Feature 149 Live Compositor Proof" "title"
            Expect.stringContains rendered "Scenario: `proof/live-sentinel-damage-v1`" "scenario"
            Expect.stringContains rendered "Verdict: `environment-limited`" "non-overclaim verdict"
            Expect.stringContains rendered "Package version" "package version"
            Expect.stringContains rendered "three fresh capable-host runs" "acceptance gate"
            Expect.stringContains rendered "sentinel-frame" "sentinel artifact schema"
            Expect.stringContains rendered "damage-frame" "damage artifact schema"
        }

        test "Feature149 proof corpus includes rejection and environment-limited cases" {
            [ "proof/live-sentinel-damage-v1"
              "proof/capable-host-three-run"
              "proof/blank-artifact"
              "proof/algorithm-mismatch"
              "proof/synthetic-only"
              "proof/missing-display"
              "proof/host-error" ]
            |> List.iter (fun scenario ->
                Expect.contains Compositor.feature149ScenarioIds scenario $"scenario {scenario}")

            Expect.isTrue
                (Compositor.feature149TargetHostProfiles |> List.exists (fun profile -> profile.ProfileId = "feature149-capable-host-candidate"))
                "capable-host candidate profile is present"
        }

        test "Feature149 Synthetic simulated host evidence remains environment-limited" {
            // SYNTHETIC: this exercises disclosure semantics without pretending a real host readback occurred.
            let token = Compositor.proofVerdictToken (Compositor.ProofEnvironmentLimited "synthetic host")
            Expect.isTrue (TestAssertions.feature149EnvironmentLimited token) "synthetic proof remains diagnostic-only"
        }
    ]
