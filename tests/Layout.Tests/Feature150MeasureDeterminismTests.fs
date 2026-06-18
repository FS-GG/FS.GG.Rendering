module Feature150MeasureDeterminismTests

open Expecto
open FS.GG.UI.Layout

[<Tests>]
let tests =
    testList "Feature150MeasureDeterminism" [
        test "repeated equivalent protocol measurement is deterministic" {
            let root = Feature150Fixtures.intrinsicColumn ()
            let constraints = Layout.constraintsFromAvailable Parent Feature150Fixtures.available

            let first = Layout.measureProtocol constraints root
            let second = Layout.measureProtocol constraints root

            Expect.equal second.MeasuredSize first.MeasuredSize "measured size stable"
            Expect.equal second.ChildPlacements first.ChildPlacements "child placements stable"
            Expect.equal second.CacheEntryId first.CacheEntryId "cache identity stable"
        }
    ]

