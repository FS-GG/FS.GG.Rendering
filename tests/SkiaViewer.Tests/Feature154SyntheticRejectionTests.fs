module Feature154SyntheticRejectionTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private profile: CompositorProof.HostProfile =
    { ProfileId = "feature154-synthetic-host"
      Backend = "OpenGL"
      Renderer = Some "synthetic"
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FramebufferSize = { Width = 640; Height = 480 }
      Scale = Some 1.0
      DisplayEnvironment = CompositorProof.HostDisplayEnvironment.X11
      ProofAlgorithmVersion = CompositorProof.proofAlgorithmVersion }

let private quality present decodable nonBlank fresh synthetic : CompositorProof.ProofArtifactQuality =
    { Present = present
      Decodable = decodable
      NonBlank = nonBlank
      Fresh = fresh
      Synthetic = synthetic }

let private proof id verdict artifacts : CompositorProof.PresentProof =
    { ProofId = id
      HostProfile = profile
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = verdict
      ObservedUntouchedRegions = []
      ObservedDamagedRegion = None
      EvidenceArtifacts = artifacts
      CreatedAt = DateTimeOffset.UnixEpoch
      Diagnostics = [ "feature=154"; "fixture=synthetic" ] }

let private attempt id verdict artifactQuality artifacts : CompositorProof.LiveProofAttempt =
    { AttemptId = id
      Proof = proof id verdict artifacts
      ProofMethod = CompositorProof.proofAlgorithmVersion
      ArtifactQuality = artifactQuality }

let private evaluate attempts =
    CompositorProof.evaluateProofSet profile DateTimeOffset.UnixEpoch (TimeSpan.FromHours 1.0) attempts
    |> CompositorProof.proofSetReadinessToken

[<Tests>]
let tests =
    testList "Feature154 Synthetic rejection gates" [
        test "Feature154_Synthetic stale missing blank undecodable and synthetic artifacts are fallback-gated" {
            // SYNTHETIC: direct artifact-quality records cover rejection paths without pretending to be live GL evidence.
            [ attempt "stale" CompositorProof.PresentProofPassed (quality true true true false false) [ "proof.md" ]
              attempt "missing" CompositorProof.PresentProofPassed (quality false true true true false) []
              attempt "blank" CompositorProof.PresentProofPassed (quality true true false true false) [ "proof.md" ]
              attempt "undecodable" CompositorProof.PresentProofPassed (quality true false true true false) [ "proof.md" ]
              attempt "synthetic" CompositorProof.PresentProofPassed (quality true true true true true) [ "proof.md" ] ]
            |> List.iter (fun bad ->
                Expect.equal (evaluate [ bad; bad; bad ]) "fallback-gated" $"{bad.AttemptId} cannot accept")
        }

        test "Feature154_Synthetic incomplete evidence and unsupported host remain non-accepting" {
            // SYNTHETIC: incomplete observation and environment-limited verdicts prove fail-closed status without a native host.
            let incomplete = attempt "incomplete" CompositorProof.PresentProofPassed (quality true true true true false) []
            let unsupported =
                attempt
                    "unsupported"
                    (CompositorProof.PresentProofEnvironmentLimited "missing display")
                    (quality true true true true false)
                    [ "proof.md" ]

            Expect.equal (evaluate [ incomplete; incomplete; incomplete ]) "fallback-gated" "incomplete evidence"
            Expect.equal (evaluate [ unsupported; unsupported; unsupported ]) "environment-limited" "unsupported host"
        }

        test "Feature154_Synthetic damaged and undamaged pixel failures fail closed" {
            // SYNTHETIC: explicit failure causes stand in for damaged-pixel and preservation failures.
            let damaged =
                attempt
                    "damaged-pixel-failure"
                    (CompositorProof.PresentProofFailed CompositorProof.PresentProofFailureCause.StalePixels)
                    (quality true true true true false)
                    [ "proof.md" ]
            let undamaged =
                attempt
                    "undamaged-preservation-failure"
                    (CompositorProof.PresentProofFailed CompositorProof.PresentProofFailureCause.ClearedPixels)
                    (quality true true true true false)
                    [ "proof.md" ]

            Expect.equal (evaluate [ damaged; damaged; damaged ]) "failed" "damaged pixel failure"
            Expect.equal (evaluate [ undamaged; undamaged; undamaged ]) "failed" "undamaged preservation failure"
        }
    ]
