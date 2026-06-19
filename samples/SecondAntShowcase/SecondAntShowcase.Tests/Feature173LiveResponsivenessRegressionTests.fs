module SecondAntShowcase.Tests.Feature173LiveResponsivenessRegressionTests

open Expecto
open SecondAntShowcase.Core

[<Tests>]
let tests =
    testList "Feature173 live responsiveness regressions" [
        test "interaction coverage remains clean" {
            let coverage = InteractionContracts.coverage ()

            Expect.isTrue (InteractionContracts.isClean coverage) "coverage remains clean"
        }

        test "representative contracts preserve key interactive families" {
            let families = InteractionContracts.all |> List.map _.ContractId |> Set.ofList

            Expect.isTrue (families.Contains "navigation") "navigation family remains covered"
            Expect.isTrue (families.Contains "disclosure") "disclosure family remains covered"
            Expect.isTrue (families.Contains "slider-rating") "slider/rating family remains covered"
        }

        test "summary preserves visual-readiness caveat vocabulary" {
            let _, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            use doc = Feature173LiveResponsivenessFixtures.summaryJson outDir
            let root = doc.RootElement

            Expect.equal (root.GetProperty("artifactWriteStatus").GetString()) "complete" "artifact status remains explicit"
            Expect.isTrue (Feature173LiveResponsivenessFixtures.hasLimitationContaining "headless-substitute" root) "substitute caveat remains visible"
        }
    ]
