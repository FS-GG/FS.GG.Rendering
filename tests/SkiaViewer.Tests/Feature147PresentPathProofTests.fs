module Feature147PresentPathProofTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private size = { Width = 640; Height = 480 }

let private profile: CompositorProof.HostProfile =
    { ProfileId = "host-a"
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
    { ProofId = "proof"
      HostProfile = profile
      ScenarioId = "proof/sentinel-damage-v1"
      Verdict = CompositorProof.PresentProofPassed
      ObservedUntouchedRegions = [ observation "u1" CompositorProof.ObservedRegionKind.Untouched true ]
      ObservedDamagedRegion = Some(observation "d1" CompositorProof.ObservedRegionKind.Damaged true)
      EvidenceArtifacts = []
      CreatedAt = now
      Diagnostics = [] }

[<Tests>]
let tests =
    testList "Feature147 present-path proof" [
        test "readiness accepts only fresh matching passed proof" {
            let now = DateTimeOffset.UtcNow
            let maxAge = TimeSpan.FromHours 1.0

            Expect.equal (CompositorProof.readiness profile now maxAge (Some(passedProof now))) CompositorProof.ProofReadiness.Ready "fresh proof"
            Expect.equal (CompositorProof.readiness profile now maxAge None) CompositorProof.ProofReadiness.Missing "missing proof"
            Expect.equal (CompositorProof.readiness profile now maxAge (Some(passedProof (now - TimeSpan.FromHours 2.0)))) CompositorProof.ProofReadiness.Stale "stale proof"

            let other = { profile with ProfileId = "host-b" }
            Expect.equal (CompositorProof.readiness other now maxAge (Some(passedProof now))) CompositorProof.ProofReadiness.HostMismatch "host mismatch"
        }

        test "observation classification distinguishes passed, cleared, and limited evidence" {
            let passed =
                [ observation "u1" CompositorProof.ObservedRegionKind.Untouched true
                  observation "d1" CompositorProof.ObservedRegionKind.Damaged true ]

            let cleared =
                [ observation "u1" CompositorProof.ObservedRegionKind.Untouched false
                  observation "d1" CompositorProof.ObservedRegionKind.Damaged true ]

            Expect.equal (CompositorProof.classifyObservations passed) CompositorProof.PresentProofPassed "passed"
            Expect.equal (CompositorProof.classifyObservations cleared) (CompositorProof.PresentProofFailed CompositorProof.PresentProofFailureCause.ClearedPixels) "cleared"

            match CompositorProof.classifyObservations [] with
            | CompositorProof.PresentProofEnvironmentLimited _ -> ()
            | other -> failtestf "expected environment-limited, got %A" other
        }

        test "MVU update emits sentinel, damage, observe, and artifact effects" {
            let now = DateTimeOffset.UtcNow
            let m0, e0 = CompositorProof.init ()
            Expect.equal e0 [ CompositorProof.DetectProfile ] "startup detects profile"

            let m1, e1 = CompositorProof.update now "proof.md" (CompositorProof.ProfileDetected profile) m0
            Expect.equal m1.Phase CompositorProof.ProofPhase.PresentingSentinel "sentinel phase"
            Expect.isTrue (e1 |> List.exists (function CompositorProof.PresentSentinelFrame _ -> true | _ -> false)) "sentinel effect"

            let m2, e2 = CompositorProof.update now "proof.md" CompositorProof.SentinelPresented m1
            Expect.equal m2.Phase CompositorProof.ProofPhase.PresentingDamage "damage phase"
            Expect.isTrue (e2 |> List.exists (function CompositorProof.PresentDamageFrame _ -> true | _ -> false)) "damage effect"

            let m3, e3 = CompositorProof.update now "proof.md" CompositorProof.DamagePresented m2
            Expect.equal m3.Phase CompositorProof.ProofPhase.Observing "observe phase"
            Expect.equal e3 [ CompositorProof.ObservePixels ] "observe effect"

            let m4, e4 =
                CompositorProof.update
                    now
                    "proof.md"
                    (CompositorProof.ObservationCompleted [ observation "u1" CompositorProof.ObservedRegionKind.Untouched true; observation "d1" CompositorProof.ObservedRegionKind.Damaged true ])
                    m3

            Expect.equal m4.Phase CompositorProof.ProofPhase.Completed "completed"
            Expect.isSome m4.Proof "proof captured"
            Expect.isTrue (e4 |> List.exists (function CompositorProof.WriteProofArtifact("proof.md", _) -> true | _ -> false)) "artifact effect"
        }
    ]
