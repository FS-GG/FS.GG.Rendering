module AntShowcase.Tests.VisualShellTests

open Expecto
open AntShowcase.Core
open AntShowcase.Core.Model
open AntShowcase.Tests.VisualTestHelpers

[<Tests>]
let visualShellTests =
    testList "VisualShell" [
        test "preferred shell regions are disjoint" {
            let regions = ShellLayout.calculate preferredSize
            Expect.isTrue (ShellLayout.allDisjoint regions) "top, nav, content, feedback, and status do not overlap"
        }

        test "minimum shell regions are disjoint" {
            let regions = ShellLayout.calculate minimumSize
            Expect.isTrue (ShellLayout.allDisjoint regions) "minimum-size shell regions do not overlap"
        }

        test "navigation labels fit the rail after truncation" {
            for page in PageRegistry.all do
                let label = ShellLayout.truncateLabel 26 page.Title
                Expect.isTrue (ShellLayout.navLabelFits label) (sprintf "nav label fits: %s" page.Id)
        }

        test "every page renders through the full shell at preferred size" {
            for page in PageRegistry.all do
                let result = renderShell preferredSize Light page.Id
                Expect.isGreaterThan result.NodeCount 0 (sprintf "shell rendered %s" page.Id)
                Expect.isNonEmpty result.Bounds (sprintf "bounds emitted for %s" page.Id)
        }

        test "theme and current page affordances render in full shell" {
            let light = renderShell preferredSize Light "charts-statistical"
            let dark = renderShell preferredSize Dark "charts-statistical"
            let lightEvidence =
                Evidence.retainedInspectionEvidence
                    "charts-statistical"
                    "antLight"
                    "preferred"
                    preferredSize.Width
                    preferredSize.Height

            let darkEvidence =
                Evidence.retainedInspectionEvidence
                    "charts-statistical"
                    "antDark"
                    "preferred"
                    preferredSize.Width
                    preferredSize.Height

            Expect.isGreaterThan light.NodeCount 0 "light shell renders"
            Expect.isGreaterThan dark.NodeCount 0 "dark shell renders"
            Expect.equal lightEvidence.RetainedStatus "accepted" "light retained shell evidence accepted"
            Expect.equal darkEvidence.RetainedStatus "accepted" "dark retained shell evidence accepted"
            Expect.equal lightEvidence.ScreenshotPreferredTargetCount 38 "preferred screenshot count preserved"
            Expect.equal darkEvidence.ScreenshotMinimumTargetCount 12 "minimum screenshot count preserved"
            Expect.stringContains (Evidence.retainedInspectionToMarkdown lightEvidence) "affected regions" "reviewer fields visible"
        }
    ]
