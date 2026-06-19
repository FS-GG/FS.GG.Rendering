module SecondAntShowcase.Tests.Feature172VisualRegressionTests

open Expecto
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model
open SecondAntShowcase.Tests.VisualTestHelpers

[<Tests>]
let tests =
    testList "Feature172 visual regression preservation" [
        test "opaque preferred and minimum shells still render retained inspection evidence" {
            for sizeRole, size in [ "preferred", preferredSize; "minimum", minimumSize ] do
                for themeId, mode in [ "antLight", Light; "antDark", Dark ] do
                    let shell = renderShell size mode "buttons"
                    let evidence = Evidence.retainedInspectionEvidence "buttons" themeId sizeRole size.Width size.Height

                    Expect.isGreaterThan shell.NodeCount 0 (sprintf "%s %s shell renders" themeId sizeRole)
                    Expect.equal evidence.RetainedStatus "accepted" (sprintf "%s %s retained evidence accepted" themeId sizeRole)
                    Expect.isGreaterThanOrEqual evidence.RepaintedNodeCount 0 "retained evidence carries repaint facts"
                    Expect.isNonEmpty evidence.ReviewerSummary "retained evidence carries reviewer summary"
        }

        test "Ant-like navigation and visual readiness target counts stay stable" {
            let rendered = renderShell preferredSize Light "buttons"
            let evidence = Evidence.retainedInspectionEvidence "buttons" "antLight" "preferred" preferredSize.Width preferredSize.Height

            Expect.isNonEmpty rendered.Bounds "navigation/content bounds are present"
            Expect.equal evidence.ScreenshotPreferredTargetCount 38 "preferred matrix count preserved"
            Expect.equal evidence.ScreenshotMinimumTargetCount 12 "minimum matrix count preserved"
        }
    ]
