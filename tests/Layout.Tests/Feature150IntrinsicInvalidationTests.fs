module Feature150IntrinsicInvalidationTests

open Expecto
open FS.GG.UI.Layout

[<Tests>]
let tests =
    testList "Feature150IntrinsicInvalidation" [
        test "layout input keys change for content size, visibility, and child order" {
            let baseline = Feature150Fixtures.dynamicColumn 24.0
            let changedSize = Feature150Fixtures.dynamicColumn 48.0
            let hidden = { baseline with Visibility = Hidden }
            let reordered = { baseline with Children = List.rev baseline.Children }

            let baselineKey = Layout.layoutInputKey baseline

            Expect.notEqual (Layout.layoutInputKey changedSize) baselineKey "content size invalidates"
            Expect.notEqual (Layout.layoutInputKey hidden) baselineKey "visibility invalidates"
            Expect.notEqual (Layout.layoutInputKey reordered) baselineKey "child order invalidates"
        }
    ]

