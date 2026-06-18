module Feature148SnapshotLifecycleTests

open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature148 snapshot lifecycle host policy" [
        test "replay cache remains bounded lower-tier storage for snapshot eligibility" {
            Expect.isGreaterThan PictureReplayCache.cap 0 "replay cap exists"
            Expect.isLessThanOrEqual PictureReplayCache.cap 1024 "bounded cap"
        }

        test "environment-limited and failed proof tokens stay distinct for fallback diagnostics" {
            Expect.equal (CompositorProof.readinessToken (CompositorProof.ProofReadiness.EnvironmentLimited "unsupported-host")) "environment-limited" "limited"
            Expect.equal (CompositorProof.readinessToken (CompositorProof.ProofReadiness.Failed "invalid-resource")) "failed" "failed"
        }
    ]
