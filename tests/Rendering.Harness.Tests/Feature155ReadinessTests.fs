module Feature155ReadinessTests

open System
open Expecto
open Rendering.Harness

let private proof id : Compositor.PresentProof =
    { ProofId = id
      HostProfile = Compositor.feature155TargetHostProfiles.Head
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = Compositor.ProofPassed
      CreatedAt = DateTimeOffset.UnixEpoch.AddMinutes 55.0
      EvidenceArtifacts = [ $"{id}/sentinel-frame.png"; $"{id}/damage-frame.png"; $"{id}/proof.md" ]
      Diagnostics = [ "feature=155"; "workflow=DetectProfile>PresentSentinelFrame>PresentDamageFrame>ObservePixels>WriteProofArtifact" ] }

let private acceptedModel () =
    let model0, _ = Compositor.initReadiness ()
    [ proof "feature155-run-1"; proof "feature155-run-2"; proof "feature155-run-3" ]
    |> List.fold (fun model item -> Compositor.updateReadiness (Compositor.ProofLoaded item) model |> fst) model0

[<Tests>]
let tests =
    testList "Feature155 readiness package" [
        test "live proof formatter records native capture gate and artifacts" {
            let rendered = Compositor.renderFeature155LiveProof (proof "feature155-run-1")

            [ "# Feature 155 Native Proof Capture"
              "exactly three selected fresh matching attempts"
              "current-run sentinel and damage artifacts"
              "fresh, decodable, non-blank, and non-synthetic"
              "Damaged pixels must update"
              "undamaged pixels must preserve"
              "zero accepted partial-redraw artifacts" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "proof set accepts three selected capable-host attempts" {
            let rendered = Compositor.renderFeature155ProofSet (acceptedModel ())

            [ "Status: `accepted`"
              "Selected attempts: `3/3`"
              "Proof method: `sentinel-damage-v1`"
              "Native proof capture accepted three current-run capable-host attempts" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "final closeout separates correctness readiness from performance claim" {
            let rendered = Compositor.renderFeature155ValidationSummary (acceptedModel ())

            [ "Status: `accepted`"
              "Proof set: `accepted`"
              "Parity status: `accepted`"
              "Fallback status: `partial-redraw-accepted`"
              "Performance claim: `not-accepted`"
              "Selected attempts: `3/3`"
              "P7 live partial-redraw correctness is accepted" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "scenario inventory covers native capture and unsupported-host paths" {
            [ "proof/native-capable-host-three-run"
              "proof/unsupported-host-zero-accepted"
              "proof/artifact-write-failure"
              "damage/localized-update"
              "readiness/final-p7-closeout" ]
            |> List.iter (fun scenario -> Expect.contains Compositor.feature155ScenarioIds scenario scenario)
        }
    ]
