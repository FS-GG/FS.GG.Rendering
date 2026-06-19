module Feature174PageNavigationParityTests

open Expecto

[<Tests>]
let tests =
    testList "Feature174 page navigation parity" [
        test "dense destination page preserves bounds, diagnostics, bindings, and ids" {
            let before = Feature174RetainedRenderFixtures.pageNavigationSource ()
            let after = Feature174RetainedRenderFixtures.textNumericDestination ()
            let step = Feature174RetainedRenderFixtures.retainedStep before after

            Feature174RetainedRenderFixtures.assertRenderMetadataParity "dense destination" step.Render (Feature174RetainedRenderFixtures.direct after)
        }

        test "no-replay-cache page preserves metadata parity" {
            let page = Feature174RetainedRenderFixtures.noReplayCachePage ()
            let step = Feature174RetainedRenderFixtures.retainedStep page page

            Feature174RetainedRenderFixtures.assertRenderMetadataParity "no replay cache" step.Render (Feature174RetainedRenderFixtures.direct page)
        }
    ]
