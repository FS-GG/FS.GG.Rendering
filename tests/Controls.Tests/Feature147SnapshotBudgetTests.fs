module Feature147SnapshotBudgetTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature147 snapshot budget policy" [
        test "supported, in-budget, beneficial snapshot is ready" {
            Expect.equal (CompositorPolicy.snapshotVerdict true 1024L 2048L 25.0 20.0) SnapshotReady "ready"
        }

        test "unsupported, over-budget, and low-benefit snapshots do not claim readiness" {
            Expect.equal (CompositorPolicy.snapshotVerdict false 1024L 2048L 25.0 20.0) (SnapshotLimited "snapshot host unsupported") "unsupported is limited"
            Expect.equal (CompositorPolicy.snapshotVerdict true 4096L 2048L 25.0 20.0) (SnapshotDemoted "snapshot budget exceeded") "over budget demotes"
            Expect.equal (CompositorPolicy.snapshotVerdict true 1024L 2048L 5.0 20.0) (SnapshotDemoted "snapshot benefit below threshold") "low benefit demotes"
        }
    ]
