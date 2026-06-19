module Feature174RetainedRenderParityTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature174 retained render-result parity" [
        test "button activation keeps visual metadata, event binding, bound id, and diagnostics parity" {
            let before = Feature174RetainedRenderFixtures.buttonScenario "Primary"
            let after = Feature174RetainedRenderFixtures.buttonScenario "Clicked"
            let step = Feature174RetainedRenderFixtures.retainedStep before after

            Feature174RetainedRenderFixtures.assertRenderMetadataParity "button activation" step.Render (Feature174RetainedRenderFixtures.direct after)
            Expect.isTrue (step.Render.BoundIds.Contains "primary-button") "primary button remains a bound target"
            Expect.equal step.Render.EventBindings.Length 1 "button action binding remains present"
        }

        test "duplicate-key diagnostics remain byte-equivalent through retained metadata" {
            let duplicate =
                Feature174RetainedRenderFixtures.shell
                    "dup-page"
                    [ Feature174RetainedRenderFixtures.text "duplicate" "A"
                      Feature174RetainedRenderFixtures.text "duplicate" "B" ]

            let step = Feature174RetainedRenderFixtures.retainedStep duplicate duplicate
            let direct = Feature174RetainedRenderFixtures.direct duplicate

            Expect.equal step.Render.Diagnostics direct.Diagnostics "duplicate-key diagnostics are retained from metadata"
        }

        test "theme changes keep retained metadata parity while repainting the whole tree" {
            let tree = Feature174RetainedRenderFixtures.buttonScenario "Primary"
            let init = RetainedRender.init Feature174RetainedRenderFixtures.theme Feature174RetainedRenderFixtures.size tree
            let step = RetainedRender.step FS.GG.UI.Themes.Default.Theme.dark Feature174RetainedRenderFixtures.size init.Retained tree
            let direct = Control.renderTree FS.GG.UI.Themes.Default.Theme.dark Feature174RetainedRenderFixtures.size tree

            Feature174RetainedRenderFixtures.assertRenderMetadataParity "theme change" step.Render direct
            Expect.equal step.WorkReduction.RepaintedNodeCount step.WorkReduction.BaselineNodeCount "theme change still repaints all nodes"
        }
    ]
