module Feature153LiveProofSimulationTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private profile: CompositorProof.HostProfile =
    { ProfileId = "feature153-synthetic-host"
      Backend = "OpenGL"
      Renderer = Some "synthetic"
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FramebufferSize = { Width = 640; Height = 480 }
      Scale = Some 1.0
      DisplayEnvironment = CompositorProof.HostDisplayEnvironment.X11
      ProofAlgorithmVersion = CompositorProof.proofAlgorithmVersion }

let private quality synthetic: CompositorProof.ProofArtifactQuality =
    { Present = true
      Decodable = true
      NonBlank = true
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
    testList "Feature153 Synthetic live proof simulations" [
        test "Feature153_Synthetic unsupported host stays environment-limited with zero accepted attempts" {
            // SYNTHETIC: direct environment-limited verdict proves unsupported-host fail-closed semantics without a native GL dependency.
            let limited =
                attempt
                    "limited"
                    (CompositorProof.PresentProofEnvironmentLimited "missing display")
                    (quality false)

            let readiness =
                CompositorProof.evaluateProofSet
                    profile
                    DateTimeOffset.UnixEpoch
                    (TimeSpan.FromHours 1.0)
                    [ limited; limited; limited ]

            Expect.equal (CompositorProof.proofSetReadinessToken readiness) "environment-limited" "unsupported host cannot accept"
        }

        test "Feature153_Synthetic readback limited classifier does not accept missing observations" {
            // SYNTHETIC: missing observation list represents a readback-limited host path.
            let verdict = CompositorProof.classifyObservations []
            Expect.equal (CompositorProof.verdictToken verdict) "environment-limited" "missing readback observations are limited"
        }

        test "Feature153_Synthetic synthetic artifacts are fallback-gated" {
            // SYNTHETIC: artifact quality is modeled directly to prove the non-accepting synthetic gate.
            let attempts =
                [ attempt "run-1" CompositorProof.PresentProofPassed (quality true)
                  attempt "run-2" CompositorProof.PresentProofPassed (quality true)
                  attempt "run-3" CompositorProof.PresentProofPassed (quality true) ]

            let readiness = CompositorProof.evaluateProofSet profile DateTimeOffset.UnixEpoch (TimeSpan.FromHours 1.0) attempts

            Expect.equal (CompositorProof.proofSetReadinessToken readiness) "fallback-gated" "synthetic cannot accept"
        }

        test "Feature153_Synthetic fewer than three attempts remain fallback-gated" {
            // SYNTHETIC: deterministic attempt records prove the exact-three gate without a live host.
            let readiness =
                CompositorProof.evaluateProofSet
                    profile
                    DateTimeOffset.UnixEpoch
                    (TimeSpan.FromHours 1.0)
                    [ attempt "run-1" CompositorProof.PresentProofPassed (quality false)
                      attempt "run-2" CompositorProof.PresentProofPassed (quality false) ]

            Expect.equal (CompositorProof.proofSetReadinessToken readiness) "fallback-gated" "fewer than three attempts cannot accept"
        }
    ]
