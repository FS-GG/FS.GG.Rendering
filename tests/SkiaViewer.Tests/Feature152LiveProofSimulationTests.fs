module Feature152LiveProofSimulationTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private profile: CompositorProof.HostProfile =
    { ProfileId = "feature152-host"
      Backend = "OpenGL"
      Renderer = Some "synthetic"
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FramebufferSize = { Width = 640; Height = 480 }
      Scale = Some 1.0
      DisplayEnvironment = CompositorProof.HostDisplayEnvironment.X11
      ProofAlgorithmVersion = CompositorProof.proofAlgorithmVersion }

let private quality synthetic nonBlank: CompositorProof.ProofArtifactQuality =
    { Present = true
      Decodable = true
      NonBlank = nonBlank
      Fresh = true
      Synthetic = synthetic }

let private proof verdict: CompositorProof.PresentProof =
    { ProofId = "proof"
      HostProfile = profile
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = verdict
      ObservedUntouchedRegions = []
      ObservedDamagedRegion = None
      EvidenceArtifacts = [ "proof.md" ]
      CreatedAt = DateTimeOffset.UnixEpoch
      Diagnostics = [] }

let private attempt id verdict artifactQuality: CompositorProof.LiveProofAttempt =
    { AttemptId = id
      Proof = proof verdict
      ProofMethod = CompositorProof.proofAlgorithmVersion
      ArtifactQuality = artifactQuality }

[<Tests>]
let tests =
    testList "Feature152 Synthetic live proof rejection simulations" [
        test "Synthetic artifacts are fallback-gated and cannot accept proof sets" {
            // SYNTHETIC: artifact quality is modeled directly to prove the fail-closed gate.
            let attempts =
                [ attempt "run-1" CompositorProof.PresentProofPassed (quality true true)
                  attempt "run-2" CompositorProof.PresentProofPassed (quality true true)
                  attempt "run-3" CompositorProof.PresentProofPassed (quality true true) ]

            let readiness = CompositorProof.evaluateProofSet profile DateTimeOffset.UnixEpoch (TimeSpan.FromHours 1.0) attempts

            Expect.equal (CompositorProof.proofSetReadinessToken readiness) "fallback-gated" "synthetic cannot accept"
        }

        test "unsupported, blank, stale, host-mismatched, and proof-method-mismatched attempts fail closed" {
            // SYNTHETIC: deterministic failure vocabulary without native GL mutation.
            let now = DateTimeOffset.UnixEpoch.AddHours 2.0
            let baseAttempt = attempt "run" CompositorProof.PresentProofPassed (quality false true)
            let limited = attempt "limited" (CompositorProof.PresentProofEnvironmentLimited "missing display") (quality false true)
            let blank = attempt "blank" CompositorProof.PresentProofPassed (quality false false)
            let stale = { baseAttempt with Proof = { baseAttempt.Proof with CreatedAt = DateTimeOffset.UnixEpoch } }
            let hostMismatch =
                { baseAttempt with
                    Proof = { baseAttempt.Proof with HostProfile = { profile with ProfileId = "other-host" } } }
            let methodMismatch = { baseAttempt with ProofMethod = "other-method" }

            [ limited; blank; stale; hostMismatch; methodMismatch ]
            |> List.iter (fun bad ->
                let readiness =
                    CompositorProof.evaluateProofSet profile now (TimeSpan.FromMinutes 5.0) [ bad; baseAttempt; baseAttempt ]

                Expect.notEqual (CompositorProof.proofSetReadinessToken readiness) "accepted" $"bad attempt {bad.AttemptId}")
        }
    ]
