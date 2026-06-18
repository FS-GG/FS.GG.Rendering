module Feature148SnapshotEligibilityTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature148 snapshot eligibility policy" [
        test "expensive stable supported snapshot is eligible when benefit clears threshold" {
            Expect.equal (RetainedRender.snapshotVerdict true 1024L 4096L 24.0 20.0) SnapshotReady "ready"
        }

        test "simple, churning, unsupported, and over-budget snapshot cases demote or limit" {
            Expect.equal (RetainedRender.snapshotVerdict true 1024L 4096L 5.0 20.0) (SnapshotDemoted "snapshot benefit below threshold") "simple scene"
            Expect.equal (RetainedRender.snapshotVerdict true 4097L 4096L 30.0 20.0) (SnapshotDemoted "snapshot budget exceeded") "over budget"
            Expect.equal (RetainedRender.snapshotVerdict false 1024L 4096L 30.0 20.0) (SnapshotLimited "snapshot host unsupported") "unsupported"
        }
    ]
