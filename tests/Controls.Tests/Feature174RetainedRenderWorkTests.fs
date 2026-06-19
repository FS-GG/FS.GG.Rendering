module Feature174RetainedRenderWorkTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature174 retained metadata work scaling" [
        test "first frame exposes retained metadata equivalent to direct render" {
            let tree = Feature174RetainedRenderFixtures.buttonScenario "Primary"
            let init = RetainedRender.init Feature174RetainedRenderFixtures.theme Feature174RetainedRenderFixtures.size tree
            let direct = Feature174RetainedRenderFixtures.direct tree

            Feature174RetainedRenderFixtures.assertRenderMetadataParity "first frame" init.Render direct
            Expect.equal init.Retained.Root.Metadata.NodeCount init.Render.NodeCount "root metadata carries first-frame node count"
            Expect.equal (init.Retained.Root.Metadata.InFlowBounds @ init.Retained.Root.Metadata.OverlayBounds) init.Render.Bounds "root metadata carries first-frame bounds"
        }

        test "button-click metadata visits changed ancestors instead of the full tree" {
            let before = Feature174RetainedRenderFixtures.buttonScenario "Primary"
            let after = Feature174RetainedRenderFixtures.buttonScenario "Clicked"
            let step = Feature174RetainedRenderFixtures.retainedStep before after
            let work = step.WorkReduction

            Expect.equal work.MetadataFallbackCount 0 "normal button-click path has no full metadata fallback"
            Expect.isGreaterThan work.MetadataVisitedNodeCount 0 "the changed button path recomputes some metadata"
            Expect.isLessThan work.MetadataVisitedNodeCount work.BaselineNodeCount "metadata work is below the full-tree baseline"
            Expect.isLessThanOrEqual work.MetadataVisitedNodeCount (work.ChangedSubtreeBound + 4) "metadata visits are bounded by the changed leaf plus ancestors"
        }

        test "idle retained step reuses metadata snapshots without visits" {
            let tree = Feature174RetainedRenderFixtures.buttonScenario "Primary"
            let step = Feature174RetainedRenderFixtures.retainedStep tree tree

            Expect.equal step.WorkReduction.MetadataVisitedNodeCount 0 "unchanged tree reuses retained metadata"
            Expect.equal step.WorkReduction.MetadataFallbackCount 0 "unchanged tree has no metadata fallback"
            Feature174RetainedRenderFixtures.assertRenderMetadataParity "idle" step.Render (Feature174RetainedRenderFixtures.direct tree)
        }

        test "metadata counters remain valid when the page has no replay-cache boundary" {
            let before = Feature174RetainedRenderFixtures.noReplayCachePage ()
            let after = Feature174RetainedRenderFixtures.shell "plain-page" [ Feature174RetainedRenderFixtures.text "plain-0" "Updated row" ]
            let step = Feature174RetainedRenderFixtures.retainedStep before after

            Expect.equal step.WorkReduction.MetadataFallbackCount 0 "no replay-cache boundary does not force metadata fallback"
            Expect.isLessThan step.WorkReduction.MetadataVisitedNodeCount step.WorkReduction.BaselineNodeCount "plain retained metadata still improves"
        }
    ]
