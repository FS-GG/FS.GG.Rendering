module Feature154ProofSetAcceptanceTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private profile: CompositorProof.HostProfile =
    { ProfileId = "feature154-host"
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
      ExpectedIdentity = "expected"
      ActualIdentity = if matched then "expected" else "actual"
      Matched = matched }

let private proofWith profile id verdict createdAt : CompositorProof.PresentProof =
    { ProofId = id
      HostProfile = profile
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = verdict
      ObservedUntouchedRegions = [ observation "undamaged-1" CompositorProof.ObservedRegionKind.Untouched true ]
      ObservedDamagedRegion = Some(observation "damaged-1" CompositorProof.ObservedRegionKind.Damaged true)
      EvidenceArtifacts = [ $"{id}/sentinel-frame.png"; $"{id}/damage-frame.png"; $"{id}/proof.md" ]
      CreatedAt = createdAt
      Diagnostics = [ "feature=154" ] }

let private attempt id createdAt : CompositorProof.LiveProofAttempt =
    { AttemptId = id
      Proof = proofWith profile id CompositorProof.PresentProofPassed createdAt
      ProofMethod = CompositorProof.proofAlgorithmVersion
      ArtifactQuality = quality }

[<Tests>]
let tests =
    testList "Feature154 proof-set acceptance" [
        test "exactly three fresh matching attempts are accepted and selected" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 15.0
            let readiness =
                CompositorProof.evaluateProofSet
                    profile
                    now
                    (TimeSpan.FromHours 1.0)
                    [ attempt "feature154-run-1" now
                      attempt "feature154-run-2" now
                      attempt "feature154-run-3" now ]

            match readiness with
            | CompositorProof.ProofSetReadiness.Accepted proofSet ->
                Expect.sequenceEqual proofSet.SelectedAttemptIds [ "feature154-run-1"; "feature154-run-2"; "feature154-run-3" ] "selected attempts"
                Expect.equal proofSet.ProofMethod CompositorProof.proofAlgorithmVersion "proof method"
                Expect.equal (CompositorProof.proofSetReadinessToken readiness) "accepted" "token"
            | other -> failtestf "expected accepted proof set, got %A" other
        }

        test "freshness, host profile, and proof method mismatches fail closed" {
            let now = DateTimeOffset.UnixEpoch.AddHours 2.0
            let stale = attempt "feature154-stale" (now - TimeSpan.FromHours 2.0)
            let otherProfile = { profile with ProfileId = "feature154-other-host" }
            let hostMismatch = { attempt "feature154-host" now with Proof = proofWith otherProfile "feature154-host" CompositorProof.PresentProofPassed now }
            let methodMismatch = { attempt "feature154-method" now with ProofMethod = "other-proof-method" }

            [ [ stale; attempt "run-2" now; attempt "run-3" now ], "fallback-gated"
              [ hostMismatch; attempt "run-2" now; attempt "run-3" now ], "fallback-gated"
              [ methodMismatch; attempt "run-2" now; attempt "run-3" now ], "fallback-gated" ]
            |> List.iter (fun (attempts, token) ->
                let readiness = CompositorProof.evaluateProofSet profile now (TimeSpan.FromHours 1.0) attempts
                Expect.equal (CompositorProof.proofSetReadinessToken readiness) token token)
        }

        test "damaged update and undamaged preservation failures fail the proof set" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 15.0
            let damagedFailure =
                { attempt "feature154-damaged-failure" now with
                    Proof = proofWith profile "feature154-damaged-failure" (CompositorProof.PresentProofFailed CompositorProof.PresentProofFailureCause.StalePixels) now }
            let undamagedFailure =
                { attempt "feature154-undamaged-failure" now with
                    Proof = proofWith profile "feature154-undamaged-failure" (CompositorProof.PresentProofFailed CompositorProof.PresentProofFailureCause.ClearedPixels) now }

            [ damagedFailure; undamagedFailure ]
            |> List.iter (fun badAttempt ->
                let readiness =
                    CompositorProof.evaluateProofSet
                        profile
                        now
                        (TimeSpan.FromHours 1.0)
                        [ badAttempt; attempt "run-2" now; attempt "run-3" now ]

                Expect.equal (CompositorProof.proofSetReadinessToken readiness) "failed" "failed-pixel evidence cannot accept")
        }
    ]
