module Feature150IntrinsicCacheTests

open Expecto
open FS.GG.UI.Layout

[<Tests>]
let tests =
    testList "Feature150IntrinsicCache" [
        test "cache entries are deterministic over full dependency keys" {
            let root = Feature150Fixtures.intrinsicColumn ()
            let inputKey = Layout.layoutInputKey root
            let dependencies = root.Children |> List.map Layout.layoutInputKey

            let first = Layout.cacheEntry MeasuredLayoutEntry root.Id "constraints" inputKey dependencies "result"
            let second = Layout.cacheEntry MeasuredLayoutEntry root.Id "constraints" inputKey dependencies "result"
            let miss = Layout.cacheEntry MeasuredLayoutEntry root.Id "constraints" (inputKey + "-changed") dependencies "result"

            Expect.equal second.EntryId first.EntryId "same keys reuse same id"
            Expect.notEqual miss.EntryId first.EntryId "changed layout input key is a miss"
        }

        test "contentExtent exposes intrinsic dependency keys" {
            let root = Feature150Fixtures.intrinsicColumn ()
            let extent = Layout.contentExtent 120.0 80.0 (Some root)

            Expect.equal extent.ExtentSource IntrinsicResult "intrinsic extent accepted"
            Expect.isNonEmpty extent.DependencyKeys "query dependency keys are visible"
        }
    ]

