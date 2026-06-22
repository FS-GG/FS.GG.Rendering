module Feature152LiveProofEvidenceTests

open System
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature152 live proof evidence" [
        test "formatter records three-run acceptance gate and unsupported-host zero-acceptance rule" {
            let proof: Compositor.Types.PresentProof =
                { ProofId = "proof-152"
                  HostProfile = Compositor.Config.feature152TargetHostProfiles.Head
                  ScenarioId = "proof/live-sentinel-damage-v1"
                  Verdict = Compositor.Types.ProofEnvironmentLimited "missing display"
                  CreatedAt = DateTimeOffset.UnixEpoch
                  EvidenceArtifacts = [ "proof.md"; "limitations.md" ]
                  Diagnostics = [ "verdict=environment-limited" ] }

            let rendered = Compositor.Render.emitFeature152LiveProof proof

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
            |> List.iter (fun scenario -> Expect.contains Compositor.Config.feature152ScenarioIds scenario scenario)
        }
    ]
