module Feature154DamageScopedParityTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private profile: CompositorProof.HostProfile =
    { ProfileId = "feature154-parity-host"
      Backend = "OpenGL"
      Renderer = Some "test-renderer"
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FramebufferSize = { Width = 640; Height = 480 }
      Scale = Some 1.0
      DisplayEnvironment = CompositorProof.HostDisplayEnvironment.X11
      ProofAlgorithmVersion = CompositorProof.proofAlgorithmVersion }

let private quality: CompositorProof.ProofArtifactQuality =
    { Present = true
      Decodable = true
      NonBlank = true
      Fresh = true
      Synthetic = false }

let private proof id host createdAt : CompositorProof.PresentProof =
    { ProofId = id
      HostProfile = host
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = CompositorProof.PresentProofPassed
      ObservedUntouchedRegions =
        [ { RegionId = "u"
            Kind = CompositorProof.ObservedRegionKind.Untouched
            ExpectedIdentity = "same"
            ActualIdentity = "same"
            Matched = true } ]
      ObservedDamagedRegion =
        Some
            { RegionId = "d"
              Kind = CompositorProof.ObservedRegionKind.Damaged
              ExpectedIdentity = "changed"
              ActualIdentity = "changed"
              Matched = true }
      EvidenceArtifacts = [ $"{id}/proof.md" ]
      CreatedAt = createdAt
      Diagnostics = [ "feature=154" ] }

let private attempt id host createdAt : CompositorProof.LiveProofAttempt =
    { AttemptId = id
      Proof = proof id host createdAt
      ProofMethod = CompositorProof.proofAlgorithmVersion
      ArtifactQuality = quality }

[<Tests>]
let tests =
    testList "Feature154 damage-scoped parity gate" [
        test "same-profile accepted proof set is prerequisite for parity acceptance" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 30.0
            let readiness =
                CompositorProof.evaluateProofSet
                    profile
                    now
                    (TimeSpan.FromHours 1.0)
                    [ attempt "run-1" profile now; attempt "run-2" profile now; attempt "run-3" profile now ]

            Expect.equal (CompositorProof.proofSetReadinessToken readiness) "accepted" "accepted same-profile proof"
        }

        test "cross-profile or stale proof keeps parity fallback-gated" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 30.0
            let other = { profile with ProfileId = "feature154-other-profile" }

            let crossProfile =
                CompositorProof.evaluateProofSet
                    profile
                    now
                    (TimeSpan.FromHours 1.0)
                    [ attempt "run-1" other now; attempt "run-2" profile now; attempt "run-3" profile now ]

            let stale =
                CompositorProof.evaluateProofSet
                    profile
                    now
                    (TimeSpan.FromMinutes 5.0)
                    [ attempt "old-1" profile (now - TimeSpan.FromMinutes 20.0)
                      attempt "old-2" profile (now - TimeSpan.FromMinutes 20.0)
                      attempt "old-3" profile (now - TimeSpan.FromMinutes 20.0) ]

            Expect.equal (CompositorProof.proofSetReadinessToken crossProfile) "fallback-gated" "cross-profile"
            Expect.equal (CompositorProof.proofSetReadinessToken stale) "fallback-gated" "stale"
        }
    ]
