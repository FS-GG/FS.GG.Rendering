module SecondAntShowcase.Tests.Feature173LiveResponsivenessBudgetTests

open Expecto
open SecondAntShowcase.Core

[<Tests>]
let tests =
    testList "Feature173 live responsiveness budgets" [
        test "summary exposes p95 and max budgets" {
            let _, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            use doc = Feature173LiveResponsivenessFixtures.summaryJson outDir
            let budgets = doc.RootElement.GetProperty("budgets")

            Expect.equal (budgets.GetProperty("inputToVisibleP95Ms").GetInt32()) Evidence.responsivenessTargetP95Ms "p95 budget"
            Expect.equal (budgets.GetProperty("inputToVisibleMaxMs").GetInt32()) Evidence.responsivenessTargetMaxMs "max budget"
        }

        test "fail-closed run names environment-boundary as first failed budget" {
            let _, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            use doc = Feature173LiveResponsivenessFixtures.summaryJson outDir
            let first = doc.RootElement.GetProperty("firstFailedBudget")

            Expect.equal (first.GetProperty("kind").GetString()) "environment-boundary" "environment boundary failure is first"
        }

        test "Synthetic drag continuity helper classifies continuous and delayed feedback" {
            // SYNTHETIC: helper-level classification isolates drag continuity vocabulary from live pointer sampling.
            let continuous = Evidence.responsivenessDragContinuity "slider-rating" 4 4 (Some 16.0) false
            let delayed = Evidence.responsivenessDragContinuity "slider-rating" 4 2 (Some 80.0) true

            Expect.equal continuous.Classification "continuous" "continuous classification"
            Expect.equal delayed.Classification "delayed-catch-up" "delayed classification"
        }
    ]
