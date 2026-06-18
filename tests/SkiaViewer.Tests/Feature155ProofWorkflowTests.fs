module Feature155ProofWorkflowTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private profile: CompositorProof.HostProfile =
    { ProfileId = "feature155-host"
      Backend = "OpenGL"
      Renderer = Some "test-renderer"
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FramebufferSize = { Width = 640; Height = 480 }
      Scale = Some 1.0
      DisplayEnvironment = CompositorProof.HostDisplayEnvironment.X11
      ProofAlgorithmVersion = CompositorProof.proofAlgorithmVersion }

let private observation id kind matched: CompositorProof.PresentProofObservation =
    { RegionId = id
      Kind = kind
      ExpectedIdentity = "sentinel"
      ActualIdentity = if matched then "sentinel" else "damage"
      Matched = matched }

[<Tests>]
let tests =
    testList "Feature155 proof workflow" [
        test "MVU transition requests native proof effects in order" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 55.0
            let model0, effects0 = CompositorProof.init ()
            Expect.equal model0.Phase CompositorProof.ProofPhase.DetectingProfile "initial phase"
            Expect.contains effects0 CompositorProof.DetectProfile "profile detection"

            let model1, effects1 = CompositorProof.update now "proof.md" (CompositorProof.ProfileDetected profile) model0
            Expect.equal model1.Phase CompositorProof.ProofPhase.PresentingSentinel "sentinel phase"
            Expect.exists effects1 (function CompositorProof.PresentSentinelFrame _ -> true | _ -> false) "sentinel effect"

            let model2, effects2 = CompositorProof.update now "proof.md" CompositorProof.SentinelPresented model1
            Expect.equal model2.Phase CompositorProof.ProofPhase.PresentingDamage "damage phase"
            Expect.exists effects2 (function CompositorProof.PresentDamageFrame _ -> true | _ -> false) "damage effect"

            let model3, effects3 = CompositorProof.update now "proof.md" CompositorProof.DamagePresented model2
            Expect.equal model3.Phase CompositorProof.ProofPhase.Observing "observe phase"
            Expect.contains effects3 CompositorProof.ObservePixels "observe effect"

            let observations =
                [ observation "undamaged-1" CompositorProof.ObservedRegionKind.Untouched true
                  observation "damaged-1" CompositorProof.ObservedRegionKind.Damaged true ]

            let model4, effects4 = CompositorProof.update now "proof.md" (CompositorProof.ObservationCompleted observations) model3
            Expect.equal model4.Phase CompositorProof.ProofPhase.Completed "completed phase"
            Expect.exists effects4 (function CompositorProof.WriteProofArtifact("proof.md", _) -> true | _ -> false) "write effect"
        }

        test "failure message completes the proof without accepted evidence" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 56.0
            let model0, _ = CompositorProof.init ()
            let model1, _ = CompositorProof.update now "proof.md" (CompositorProof.ProfileDetected profile) model0
            let model2, effects = CompositorProof.update now "proof.md" (CompositorProof.ProofFailed CompositorProof.PresentProofFailureCause.Timeout) model1

            Expect.equal model2.Phase CompositorProof.ProofPhase.Completed "failed proof completes"
            Expect.exists effects (function
                | CompositorProof.WriteProofArtifact("proof.md", proof) ->
                    match proof.Verdict with
                    | CompositorProof.PresentProofFailed CompositorProof.PresentProofFailureCause.Timeout -> true
                    | _ -> false
                | _ -> false) "timeout proof is written"
        }
    ]
