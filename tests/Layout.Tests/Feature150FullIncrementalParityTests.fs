module Feature150FullIncrementalParityTests

open Expecto
open FS.GG.UI.Layout

[<Tests>]
let tests =
    testList "Feature150FullIncrementalParity" [
        test "warm incremental layout preserves full bounds for representative corpus root" {
            let root = Feature150Fixtures.intrinsicColumn ()
            let full = Layout.evaluate Feature150Fixtures.available root
            let incremental = Layout.evaluateIncremental full [] Feature150Fixtures.available root

            Expect.equal incremental.Bounds full.Bounds "warm incremental bounds equal full"
            Expect.isEmpty incremental.Invalidated "no changed nodes are remeasured"
        }
    ]

