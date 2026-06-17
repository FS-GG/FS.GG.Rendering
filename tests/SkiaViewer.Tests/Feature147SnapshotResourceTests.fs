module Feature147SnapshotResourceTests

open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature147 snapshot resource host policy" [
        test "replay cache cap remains the lower-tier resource bound for snapshot gating" {
            Expect.isGreaterThan PictureReplayCache.cap 0 "bounded replay cache exists"
        }

        test "environment-limited proof token remains distinct from failed proof" {
            Expect.equal (CompositorProof.readinessToken (CompositorProof.ProofReadiness.EnvironmentLimited "no display")) "environment-limited" "environment token"
            Expect.equal (CompositorProof.readinessToken (CompositorProof.ProofReadiness.Failed "cleared")) "failed" "failed token"
        }
    ]
