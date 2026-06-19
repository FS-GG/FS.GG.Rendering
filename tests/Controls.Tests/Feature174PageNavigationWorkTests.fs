module Feature174PageNavigationWorkTests

open Expecto

[<Tests>]
let tests =
    testList "Feature174 page navigation metadata work" [
        test "navigation to text-numeric-input reuses stable shell metadata" {
            let before = Feature174RetainedRenderFixtures.pageNavigationSource ()
            let after = Feature174RetainedRenderFixtures.textNumericDestination ()
            let step = Feature174RetainedRenderFixtures.retainedStep before after

            Expect.equal step.WorkReduction.MetadataFallbackCount 0 "page navigation has no full metadata fallback"
            Expect.isLessThan step.WorkReduction.MetadataVisitedNodeCount step.WorkReduction.BaselineNodeCount "stable shell metadata is reused"
            Expect.isTrue (step.Render.Bounds |> List.exists (fun (id, _) -> id = "stable-chrome")) "stable chrome remains in rendered metadata"
            Feature174RetainedRenderFixtures.assertRenderMetadataParity "page navigation" step.Render (Feature174RetainedRenderFixtures.direct after)
        }
    ]
