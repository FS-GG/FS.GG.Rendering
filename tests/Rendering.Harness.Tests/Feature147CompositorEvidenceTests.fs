module Feature147CompositorEvidenceTests

open System
open Expecto
open Rendering.Harness

let private facts =
    { EffectiveBackend = X11
      Display = Some ":1"
      GlRenderer = Some "Mesa"
      GlVersion = Some "4.6"
      GlDirect = true
      RefreshHz = Some 60.0
      Extensions = []
      SwapControl = Some 1
      VblankSource = Some "glx"
      UinputAvailable = false }

[<Tests>]
let tests =
    testList "Feature147 compositor evidence contracts" [
        test "host profile is deterministic from probe facts" {
            let a = Compositor.Config.hostProfileFromFacts facts
            let b = Compositor.Config.hostProfileFromFacts facts
            Expect.equal a b "same facts produce same host profile"
            Expect.equal a.DisplayEnvironment "x11" "display environment"
        }

        test "present proof formatter includes verdict, scenario, and host" {
            let profile = Compositor.Config.hostProfileFromFacts facts
            let proof: Compositor.Types.PresentProof =
                { ProofId = "proof"
                  HostProfile = profile
                  ScenarioId = "proof/sentinel-damage-v1"
                  Verdict = Compositor.Types.ProofPassed
                  CreatedAt = DateTimeOffset.UnixEpoch
                  EvidenceArtifacts = [ "proof.md" ]
                  Diagnostics = [ "ok" ] }

            let rendered = Compositor.Render.renderPresentProof proof
            Expect.stringContains rendered "Verdict: `passed`" "verdict"
            Expect.stringContains rendered "proof/sentinel-damage-v1" "scenario"
            Expect.stringContains rendered profile.ProfileId "host"
        }

        test "tier evaluator rejects failed parity even with passed proof" {
            let verdict = Compositor.Config.evaluateTier Compositor.Types.Ready (Some(Compositor.Types.ParityFailed "pixel mismatch")) (Some true)
            Expect.equal verdict (Compositor.Types.Rejected "pixel mismatch") "parity failure dominates"
        }
    ]
