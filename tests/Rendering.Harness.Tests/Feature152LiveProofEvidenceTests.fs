module Feature152LiveProofEvidenceTests

open System
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature152 live proof evidence" [
        test "formatter records three-run acceptance gate and unsupported-host zero-acceptance rule" {
            let proof: Compositor.PresentProof =
                { ProofId = "proof-152"
                  HostProfile = Compositor.feature152TargetHostProfiles.Head
                  ScenarioId = "proof/live-sentinel-damage-v1"
                  Verdict = Compositor.ProofEnvironmentLimited "missing display"
                  CreatedAt = DateTimeOffset.UnixEpoch
                  EvidenceArtifacts = [ "proof.md"; "limitations.md" ]
                  Diagnostics = [ "verdict=environment-limited" ] }

            let rendered = Compositor.renderFeature152LiveProof proof

            [ "# Feature 152 Live Proof Run Set"
              "three fresh matching capable-host runs"
              "unsupported/README.md"
              "zero accepted partial-redraw artifacts"
              "proof-method-mismatched" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "scenario inventory includes run-set rejection and environment-limited cases" {
            [ "proof/capable-host-three-run"
              "proof/unsupported-host-zero-accepted"
              "proof/proof-method-mismatch"
              "proof/blank-artifact"
              "proof/synthetic-only" ]
            |> List.iter (fun scenario -> Expect.contains Compositor.feature152ScenarioIds scenario scenario)
        }
    ]
