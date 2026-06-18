module Feature148LiveProofTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private size = { Width = 640; Height = 480 }

let private profile: CompositorProof.HostProfile =
    { ProfileId = "feature148-host"
      Backend = "OpenGL"
      Renderer = Some "Mesa"
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FramebufferSize = size
      Scale = Some 1.0
      DisplayEnvironment = CompositorProof.HostDisplayEnvironment.X11
      ProofAlgorithmVersion = CompositorProof.proofAlgorithmVersion }

let private observation id kind matched: CompositorProof.PresentProofObservation =
    { RegionId = id
      Kind = kind
      ExpectedIdentity = "sentinel"
      ActualIdentity = if matched then "sentinel" else "cleared"
      Matched = matched }

let private passedProof now: CompositorProof.PresentProof =
    { ProofId = "feature148-proof"
      HostProfile = profile
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = CompositorProof.PresentProofPassed
      ObservedUntouchedRegions = [ observation "u1" CompositorProof.ObservedRegionKind.Untouched true ]
      ObservedDamagedRegion = Some(observation "d1" CompositorProof.ObservedRegionKind.Damaged true)
      EvidenceArtifacts = [ "sentinel-frame.png"; "damage-frame.png"; "proof.md" ]
      CreatedAt = now
      Diagnostics = [ "package=feature148" ] }

[<Tests>]
let tests =
    testList "Feature148 live proof readiness" [
        test "live proof readiness accepts only fresh matching passed proof with artifacts" {
            let now = DateTimeOffset.UtcNow
            let proof = passedProof now

            Expect.equal (CompositorProof.readiness profile now (TimeSpan.FromHours 1.0) (Some proof)) CompositorProof.ProofReadiness.Ready "ready"
            Expect.contains proof.EvidenceArtifacts "sentinel-frame.png" "sentinel artifact"
            Expect.contains proof.EvidenceArtifacts "damage-frame.png" "damage artifact"
        }

        test "stale, host-mismatch, and algorithm-mismatch proofs are rejected before scissor readiness" {
            let now = DateTimeOffset.UtcNow
            let stale = passedProof (now - TimeSpan.FromHours 2.0)
            let hostMismatch = { passedProof now with HostProfile = { profile with ProfileId = "other-host" } }
            let algorithmMismatch = { passedProof now with HostProfile = { profile with ProofAlgorithmVersion = "other-algorithm" } }

            Expect.equal (CompositorProof.readiness profile now (TimeSpan.FromMinutes 5.0) (Some stale)) CompositorProof.ProofReadiness.Stale "stale"
            Expect.equal (CompositorProof.readiness profile now (TimeSpan.FromMinutes 5.0) (Some hostMismatch)) CompositorProof.ProofReadiness.HostMismatch "host mismatch"
            Expect.equal (CompositorProof.readiness profile now (TimeSpan.FromMinutes 5.0) (Some algorithmMismatch)) CompositorProof.ProofReadiness.HostMismatch "algorithm mismatch"
        }

        test "MVU transition records deterministic proof id and writes proof artifact" {
            let now = DateTimeOffset.Parse("2026-06-18T00:00:00Z")
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

            let proof = m4.Proof |> Option.defaultWith (fun () -> failtest "proof missing")
            Expect.stringStarts proof.ProofId "proof-feature148-host-" "deterministic proof id includes profile"
            Expect.isTrue (effects4 |> List.exists (function CompositorProof.WriteProofArtifact("proof.md", _) -> true | _ -> false)) "artifact effect"
        }
    ]
