module SecondAntShowcase.Tests.Feature174ResponsivenessBudgetTests

open System.IO
open System.Text.Json
open Expecto

[<Tests>]
let tests =
    testList "Feature174 responsiveness summary fields" [
        test "environment-limited summary exposes baseline, optimized profile, and null reductions" {
            let code, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            use doc = Feature173LiveResponsivenessFixtures.summaryJson outDir
            let root = doc.RootElement

            Expect.equal code 4 "headless responsiveness fails closed"
            Expect.equal (root.GetProperty("baselineProfileId").GetString()) "2026-06-19" "baseline profile"
            Expect.isNonEmpty (root.GetProperty("optimizedProfileId").GetString()) "optimized profile"
            Expect.equal (root.GetProperty("preparationReductionPercent").ValueKind) JsonValueKind.Null "preparation reduction is not fabricated"
            Expect.equal (root.GetProperty("firstFramePreparationReductionPercent").ValueKind) JsonValueKind.Null "first-frame reduction is not fabricated"
            Expect.equal (root.GetProperty("parityStatus").GetString()) "environment-limited" "parity status is fail-closed"
            Expect.equal (root.GetProperty("parityArtifacts").GetArrayLength()) 0 "no parity artifacts are invented"
        }

        test "responsiveness markdown links render-lag correlation placeholders" {
            let _, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            let summaryPath = Feature173LiveResponsivenessFixtures.summaryFile outDir
            let runRoot =
                match Directory.GetParent(summaryPath) with
                | null -> failtest "summary has a run directory"
                | parent -> parent.FullName
            let markdown = File.ReadAllText(Path.Combine(runRoot, "summary.md"))

            Expect.stringContains markdown "baseline profile: 2026-06-19" "baseline is visible"
            Expect.stringContains markdown "preparation reduction: n/a" "environment-limited reduction is n/a"
            Expect.stringContains markdown "first-frame preparation reduction: n/a" "environment-limited first-frame reduction is n/a"
            Expect.stringContains markdown "parity: environment-limited" "parity status is visible"
        }
    ]
