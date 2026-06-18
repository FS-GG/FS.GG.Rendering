module Feature152LiveProofTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private profile: CompositorProof.HostProfile =
    { ProfileId = "feature152-host"
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

let private observation id kind: CompositorProof.PresentProofObservation =
    { RegionId = id
      Kind = kind
      ExpectedIdentity = "expected"
      ActualIdentity = "expected"
      Matched = true }

let private proof id createdAt: CompositorProof.PresentProof =
    { ProofId = id
      HostProfile = profile
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = CompositorProof.PresentProofPassed
      ObservedUntouchedRegions = [ observation "u1" CompositorProof.ObservedRegionKind.Untouched ]
      ObservedDamagedRegion = Some(observation "d1" CompositorProof.ObservedRegionKind.Damaged)
      EvidenceArtifacts = [ $"{id}/sentinel-frame.png"; $"{id}/damage-frame.png"; $"{id}/proof.md" ]
      CreatedAt = createdAt
      Diagnostics = [ "feature=152" ] }

let private attempt id createdAt: CompositorProof.LiveProofAttempt =
    { AttemptId = id
      Proof = proof id createdAt
      ProofMethod = CompositorProof.proofAlgorithmVersion
      ArtifactQuality = quality }

[<Tests>]
let tests =
    testList "Feature152 live proof run-set acceptance" [
        test "three fresh matching capable-host attempts produce an accepted proof set" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 10.0

            let readiness =
                CompositorProof.evaluateProofSet
                    profile
                    now
                    (TimeSpan.FromHours 1.0)
                    [ attempt "run-1" now; attempt "run-2" now; attempt "run-3" now ]

            match readiness with
            | CompositorProof.ProofSetReadiness.Accepted proofSet ->
                Expect.equal proofSet.Attempts.Length 3 "three attempts"
                Expect.equal proofSet.HostProfile.ProfileId profile.ProfileId "host profile"
                Expect.equal (CompositorProof.proofSetReadinessToken readiness) "accepted" "token"
            | other -> failtestf "expected accepted proof set, got %A" other
        }

        test "one run remains fallback-gated" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 10.0

            let readiness =
                CompositorProof.evaluateProofSet profile now (TimeSpan.FromHours 1.0) [ attempt "run-1" now ]

            Expect.equal
                (CompositorProof.proofSetReadinessToken readiness)
                "fallback-gated"
                "single run cannot accept live partial redraw"
        }
    ]
