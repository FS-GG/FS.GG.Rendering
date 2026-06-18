module Feature154ProofAcceptanceTests

open System
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature154 proof acceptance evidence" [
        test "formatter records exact-three proof gate and fail-closed quality rules" {
            let proof: Compositor.PresentProof =
                { ProofId = "proof-154"
                  HostProfile = Compositor.feature154TargetHostProfiles.Head
                  ScenarioId = "proof/live-sentinel-damage-v1"
                  Verdict = Compositor.ProofEnvironmentLimited "missing display"
                  CreatedAt = DateTimeOffset.UnixEpoch
                  EvidenceArtifacts = [ "proof.md"; "limitations.md"; "attempts/README.md"; "unsupported/README.md" ]
                  Diagnostics = [ "verdict=environment-limited"; "attempt-count=3" ] }

            let rendered = Compositor.renderFeature154LiveProof proof

            [ "# Feature 154 Compositor Proof Acceptance"
              "exactly three selected fresh matching capable-host attempts"
              "fresh, decodable, non-blank, non-synthetic sentinel and damage artifacts"
              "Damaged pixels must update"
              "undamaged pixels must preserve"
              "zero accepted partial-redraw artifacts" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "scenario inventory covers proof rejection and unsupported-host paths" {
            [ "proof/capable-host-three-run"
              "proof/unsupported-host-zero-accepted"
              "proof/undecodable-artifact"
              "proof/damaged-pixel-failure"
              "proof/undamaged-preservation-failure" ]
            |> List.iter (fun scenario -> Expect.contains Compositor.feature154ScenarioIds scenario scenario)
        }
    ]
