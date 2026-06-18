module Feature149LiveProofTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private profile: CompositorProof.HostProfile =
    { ProfileId = "feature149-host"
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
      ExpectedIdentity = "expected"
      ActualIdentity = if matched then "expected" else "actual"
      Matched = matched }

let private passedProof now: CompositorProof.PresentProof =
    { ProofId = "feature149-proof"
      HostProfile = profile
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = CompositorProof.PresentProofPassed
      ObservedUntouchedRegions = [ observation "u1" CompositorProof.ObservedRegionKind.Untouched true ]
      ObservedDamagedRegion = Some(observation "d1" CompositorProof.ObservedRegionKind.Damaged true)
      EvidenceArtifacts = [ "proof.md" ]
      CreatedAt = now
      Diagnostics = [ "package=feature149" ] }

[<Tests>]
let tests =
    testList "Feature149 live proof readiness" [
        test "fresh matching accepted proof is ready and deterministic" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 10.0
            let proof = passedProof now

            Expect.equal (CompositorProof.readiness profile now (TimeSpan.FromHours 1.0) (Some proof)) CompositorProof.ProofReadiness.Ready "ready"
            Expect.stringStarts proof.ProofId "feature149-proof" "proof id"
            Expect.equal (CompositorProof.verdictToken proof.Verdict) "passed" "passed token"
        }

        test "stale proof, host mismatch, and algorithm mismatch reject readiness" {
            let now = DateTimeOffset.UnixEpoch.AddHours 2.0
            let stale = { passedProof DateTimeOffset.UnixEpoch with CreatedAt = DateTimeOffset.UnixEpoch }
            let hostMismatch = { passedProof now with HostProfile = { profile with ProfileId = "other-host" } }
            let algorithmMismatch = { passedProof now with HostProfile = { profile with ProofAlgorithmVersion = "old" } }

            Expect.equal (CompositorProof.readiness profile now (TimeSpan.FromMinutes 5.0) (Some stale)) CompositorProof.ProofReadiness.Stale "stale"
            Expect.equal (CompositorProof.readiness profile now (TimeSpan.FromMinutes 5.0) (Some hostMismatch)) CompositorProof.ProofReadiness.HostMismatch "host mismatch"
            Expect.equal (CompositorProof.readiness profile now (TimeSpan.FromMinutes 5.0) (Some algorithmMismatch)) CompositorProof.ProofReadiness.HostMismatch "algorithm mismatch"
        }

        test "MVU proof workflow records artifact write after observations" {
            let now = DateTimeOffset.UnixEpoch.AddMinutes 1.0
            let m0, effects0 = CompositorProof.init ()
            Expect.equal effects0 [ CompositorProof.DetectProfile ] "detect effect"

            let m1, _ = CompositorProof.update now "proof.md" (CompositorProof.ProfileDetected profile) m0
            let m2, _ = CompositorProof.update now "proof.md" CompositorProof.SentinelPresented m1
            let m3, _ = CompositorProof.update now "proof.md" CompositorProof.DamagePresented m2
            let m4, effects4 =
                CompositorProof.update
                    now
                    "proof.md"
                    (CompositorProof.ObservationCompleted [ observation "u1" CompositorProof.ObservedRegionKind.Untouched true; observation "d1" CompositorProof.ObservedRegionKind.Damaged true ])
                    m3

            Expect.equal m4.Phase CompositorProof.ProofPhase.Completed "completed"
            Expect.isSome m4.Proof "proof"
            Expect.isTrue (effects4 |> List.exists (function CompositorProof.WriteProofArtifact("proof.md", _) -> true | _ -> false)) "artifact effect"
        }
    ]
