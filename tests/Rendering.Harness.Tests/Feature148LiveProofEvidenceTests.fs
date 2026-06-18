module Feature148LiveProofEvidenceTests

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
    testList "Feature148 live proof evidence" [
        test "Feature148 live proof formatter records host facts, package version, and limitations" {
            let profile = Compositor.hostProfileFromFacts facts
            let proof: Compositor.PresentProof =
                { ProofId = "proof-148"
                  HostProfile = profile
                  ScenarioId = "proof/live-sentinel-damage-v1"
                  Verdict = Compositor.ProofEnvironmentLimited "missing display"
                  CreatedAt = DateTimeOffset.UnixEpoch
                  EvidenceArtifacts = [ "proof.md"; "limitations.md" ]
                  Diagnostics = [ "verdict=environment-limited" ] }

            let rendered = Compositor.renderFeature148LiveProof proof
            Expect.stringContains rendered "# Feature 148 Live Preservation Proof" "title"
            Expect.stringContains rendered "Scenario: `proof/live-sentinel-damage-v1`" "scenario"
            Expect.stringContains rendered "Verdict: `environment-limited`" "non-overclaim verdict"
            Expect.stringContains rendered "Package version" "package version"
            Expect.stringContains rendered "sentinel-frame" "sentinel artifact schema"
            Expect.stringContains rendered "damage-frame" "damage artifact schema"
        }

        test "Feature148 proof corpus includes real and environment-limited host cases" {
            [ "proof/live-sentinel-damage-v1"
              "proof/non-preserving-host"
              "proof/missing-display"
              "proof/host-error" ]
            |> List.iter (fun scenario ->
                Expect.contains Compositor.feature148ScenarioIds scenario $"scenario {scenario}")

            Expect.isTrue
                (Compositor.feature148TargetHostProfiles |> List.exists (fun profile -> profile.ProfileId = "synthetic-non-preserving"))
                "synthetic profile is present but distinct"
        }

        test "Feature148 Synthetic simulated host evidence remains environment-limited" {
            // SYNTHETIC: this exercises disclosure semantics without pretending a real host readback occurred.
            let token = Compositor.proofVerdictToken (Compositor.ProofEnvironmentLimited "synthetic host")
            Expect.isTrue (TestAssertions.feature148EnvironmentLimited token) "synthetic proof remains diagnostic-only"
        }
    ]
