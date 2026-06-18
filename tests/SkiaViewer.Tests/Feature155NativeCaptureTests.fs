module Feature155NativeCaptureTests

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

let private quality: CompositorProof.ProofArtifactQuality =
    { Present = true
      Decodable = true
      NonBlank = true
      Fresh = true
      Synthetic = false }

let private observation id kind matched: CompositorProof.PresentProofObservation =
    { RegionId = id
      Kind = kind
      ExpectedIdentity = "sentinel"
      ActualIdentity = if matched then "sentinel" else "damage"
      Matched = matched }

let private proof id verdict createdAt : CompositorProof.PresentProof =
    { ProofId = id
      HostProfile = profile
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = verdict
      ObservedUntouchedRegions = [ observation "undamaged-1" CompositorProof.ObservedRegionKind.Untouched true ]
      ObservedDamagedRegion = Some(observation "damaged-1" CompositorProof.ObservedRegionKind.Damaged true)
      EvidenceArtifacts = [ $"{id}/sentinel-frame.png"; $"{id}/damage-frame.png"; $"{id}/proof.md" ]
      CreatedAt = createdAt
      Diagnostics = [ "feature=155" ] }

let private attempt id createdAt : CompositorProof.LiveProofAttempt =
    { AttemptId = id
      Proof = proof id CompositorProof.PresentProofPassed createdAt
      ProofMethod = CompositorProof.proofAlgorithmVersion
      ArtifactQuality = quality }

[<Tests>]
let tests =
    testList "Feature155 native capture acceptance" [
        test "three fresh non-synthetic attempts accept the proof set" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 55.0
            let readiness =
                CompositorProof.evaluateProofSet
                    profile
                    now
                    (TimeSpan.FromHours 1.0)
                    [ attempt "feature155-run-1" now
                      attempt "feature155-run-2" now
                      attempt "feature155-run-3" now ]

            Expect.equal (CompositorProof.proofSetReadinessToken readiness) "accepted" "proof set"
        }

        test "Feature155_Synthetic artifact quality remains rejection-only" {
            // SYNTHETIC: toggled quality flags prove rejection paths without satisfying native acceptance.
            [ { quality with Present = false }, "missing artifact"
              { quality with Decodable = false }, "undecodable artifact"
              { quality with NonBlank = false }, "blank artifact"
              { quality with Fresh = false }, "stale artifact"
              { quality with Synthetic = true }, "synthetic artifact" ]
            |> List.iter (fun (candidate, reason) ->
                Expect.equal (CompositorProof.artifactQualityFailure candidate) (Some reason) reason
                Expect.isFalse (CompositorProof.artifactQualityAccepted candidate) reason)
        }
    ]
