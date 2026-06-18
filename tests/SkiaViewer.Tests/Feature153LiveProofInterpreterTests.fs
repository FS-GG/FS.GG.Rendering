module Feature153LiveProofInterpreterTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private profile: CompositorProof.HostProfile =
    { ProfileId = "feature153-host"
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

let private proof id verdict createdAt: CompositorProof.PresentProof =
    { ProofId = id
      HostProfile = profile
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = verdict
      ObservedUntouchedRegions = [ observation "undamaged-1" CompositorProof.ObservedRegionKind.Untouched true ]
      ObservedDamagedRegion = Some(observation "damaged-1" CompositorProof.ObservedRegionKind.Damaged true)
      EvidenceArtifacts = [ $"{id}/sentinel-frame.png"; $"{id}/damage-frame.png"; $"{id}/proof.md" ]
      CreatedAt = createdAt
      Diagnostics = [ "feature=153" ] }

let private attempt id createdAt: CompositorProof.LiveProofAttempt =
    { AttemptId = id
      Proof = proof id CompositorProof.PresentProofPassed createdAt
      ProofMethod = CompositorProof.proofAlgorithmVersion
      ArtifactQuality = quality }

[<Tests>]
let tests =
    testList "Feature153 live proof interpreter" [
        test "MVU transition requests sentinel damage and artifact write effects" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 5.0
            let model0, effects0 = CompositorProof.init ()
            Expect.equal model0.Phase CompositorProof.ProofPhase.DetectingProfile "init starts by detecting host profile"
            Expect.contains effects0 CompositorProof.DetectProfile "init requests profile detection"

            let model1, effects1 = CompositorProof.update now "proof.md" (CompositorProof.ProfileDetected profile) model0
            Expect.equal model1.Phase CompositorProof.ProofPhase.PresentingSentinel "profile detection advances to sentinel"
            Expect.exists effects1 (function CompositorProof.PresentSentinelFrame _ -> true | _ -> false) "sentinel frame requested"

            let model2, effects2 = CompositorProof.update now "proof.md" CompositorProof.SentinelPresented model1
            Expect.equal model2.Phase CompositorProof.ProofPhase.PresentingDamage "sentinel advances to damage"
            Expect.exists effects2 (function CompositorProof.PresentDamageFrame _ -> true | _ -> false) "damage frame requested"

            let model3, effects3 = CompositorProof.update now "proof.md" CompositorProof.DamagePresented model2
            Expect.equal model3.Phase CompositorProof.ProofPhase.Observing "damage advances to observation"
            Expect.contains effects3 CompositorProof.ObservePixels "pixel observation requested"

            let observations =
                [ observation "undamaged-1" CompositorProof.ObservedRegionKind.Untouched true
                  observation "damaged-1" CompositorProof.ObservedRegionKind.Damaged true ]

            let model4, effects4 = CompositorProof.update now "proof.md" (CompositorProof.ObservationCompleted observations) model3
            Expect.equal model4.Phase CompositorProof.ProofPhase.Completed "observations complete the attempt"
            Expect.exists effects4 (function CompositorProof.WriteProofArtifact("proof.md", _) -> true | _ -> false) "artifact write is emitted"
        }

        test "artifact quality rejects missing undecodable blank stale or synthetic artifacts" {
            [ { quality with Present = false }, "missing artifact"
              { quality with Decodable = false }, "undecodable artifact"
              { quality with NonBlank = false }, "blank artifact"
              { quality with Fresh = false }, "stale artifact"
              { quality with Synthetic = true }, "synthetic artifact" ]
            |> List.iter (fun (candidate, reason) ->
                Expect.equal (CompositorProof.artifactQualityFailure candidate) (Some reason) reason
                Expect.isFalse (CompositorProof.artifactQualityAccepted candidate) reason)
        }

        test "exactly three selected attempts are recorded when four accepted attempts are available" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 10.0
            let readiness =
                CompositorProof.evaluateProofSet
                    profile
                    now
                    (TimeSpan.FromHours 1.0)
                    [ attempt "run-1" now
                      attempt "run-2" now
                      attempt "run-3" now
                      attempt "run-4" now ]

            match readiness with
            | CompositorProof.ProofSetReadiness.Accepted proofSet ->
                Expect.sequenceEqual proofSet.SelectedAttemptIds [ "run-1"; "run-2"; "run-3" ] "selected trio is explicit"
                Expect.hasLength proofSet.Attempts 3 "accepted proof set records exactly three selected attempts"
                Expect.equal proofSet.FreshnessWindow (TimeSpan.FromHours 1.0) "freshness policy is recorded"
            | other -> failtestf "expected accepted proof set, got %A" other
        }

        test "failed extra attempt prevents a selected trio from hiding bad evidence" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 10.0
            let bad =
                { attempt "run-4" now with
                    Proof = proof "run-4" (CompositorProof.PresentProofFailed CompositorProof.PresentProofFailureCause.ClearedPixels) now }

            let readiness =
                CompositorProof.evaluateProofSet
                    profile
                    now
                    (TimeSpan.FromHours 1.0)
                    [ attempt "run-1" now
                      attempt "run-2" now
                      attempt "run-3" now
                      bad ]

            Expect.equal (CompositorProof.proofSetReadinessToken readiness) "failed" "bad evidence in the set fails closed"
        }
    ]
