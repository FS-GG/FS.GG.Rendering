module Feature153LiveProofEvidenceTests

open System
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature153 live proof evidence" [
        test "formatter records exact-three proof gate and unsupported-host zero-acceptance rule" {
            let proof: Compositor.Types.PresentProof =
                { ProofId = "proof-153"
                  HostProfile = Compositor.Config.feature153TargetHostProfiles.Head
                  ScenarioId = "proof/live-sentinel-damage-v1"
                  Verdict = Compositor.Types.ProofEnvironmentLimited "missing display"
                  CreatedAt = DateTimeOffset.UnixEpoch
                  EvidenceArtifacts = [ "proof.md"; "limitations.md"; "attempts/README.md"; "unsupported/README.md" ]
                  Diagnostics = [ "verdict=environment-limited" ] }

            let rendered = Compositor.Render.emitFeature153LiveProof proof

            [ "# Feature 153 Live Proof Interpreter"
              "exactly three selected fresh matching capable-host attempts"
              "attempts/<attempt-id>/sentinel-frame.png"
              "unsupported/"
              "zero accepted partial-redraw artifacts"
              "does not enable partial redraw" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "scenario inventory includes selected-trio and environment-limited cases" {
            [ "proof/capable-host-three-run"
              "proof/unsupported-host-zero-accepted"
              "proof/readback-limited"
              "proof/proof-method-mismatch"
              "proof/selected-trio" ]
            |> List.iter (fun scenario -> Expect.contains Compositor.Config.feature153ScenarioIds scenario scenario)
        }
    ]
