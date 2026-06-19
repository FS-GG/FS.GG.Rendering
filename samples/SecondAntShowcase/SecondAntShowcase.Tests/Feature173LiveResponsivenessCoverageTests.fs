module SecondAntShowcase.Tests.Feature173LiveResponsivenessCoverageTests

open Expecto
open SecondAntShowcase.Core

[<Tests>]
let tests =
    testList "Feature173 live responsiveness coverage" [
        test "all-interactive summary derives required families from interaction contracts" {
            let _, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            use doc = Feature173LiveResponsivenessFixtures.summaryJson outDir
            let coverage = doc.RootElement.GetProperty("coverage")

            Expect.equal (coverage.GetProperty("requiredInteractiveFamilies").GetArrayLength()) InteractionContracts.all.Length "all contracts required"
            Expect.equal (coverage.GetProperty("blockedInteractiveFamilies").GetArrayLength()) InteractionContracts.all.Length "blocked families named"
            Expect.equal (coverage.GetProperty("missingInteractiveFamilies").GetArrayLength()) 0 "no family is omitted"
        }

        test "display-only exclusions keep reasons" {
            let _, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            use doc = Feature173LiveResponsivenessFixtures.summaryJson outDir
            let exclusions = doc.RootElement.GetProperty("coverage").GetProperty("displayOnlyExclusions")
            let first = exclusions.EnumerateArray() |> Seq.head

            Expect.isGreaterThan (exclusions.GetArrayLength()) 0 "exclusions present"
            Expect.isNonEmpty (first.GetProperty("controlId").GetString()) "control id"
            Expect.isNonEmpty (first.GetProperty("reason").GetString()) "reason"
        }
    ]
