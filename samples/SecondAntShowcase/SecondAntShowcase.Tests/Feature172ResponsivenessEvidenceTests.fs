module SecondAntShowcase.Tests.Feature172ResponsivenessEvidenceTests

open System.Text.Json
open Expecto
open SecondAntShowcase.App
open SecondAntShowcase.Core

[<Tests>]
let tests =
    testList "Feature172 responsiveness evidence shape" [
        test "all-interactive records include sample review fields for every interactive family" {
            let outDir = SecondAntShowcase.Tests.Feature172ResponsivenessFixtures.tempDir ()
            let code =
                Responsiveness.run
                    [ "--script"; "representative"
                      "--theme"; "light"
                      "--all-interactive"
                      "--out"; outDir
                      "--json" ]

            let lines = SecondAntShowcase.Tests.Feature172ResponsivenessFixtures.records outDir
            use first = JsonDocument.Parse(List.head lines)
            let root = first.RootElement
            let mutable property = Unchecked.defaultof<JsonElement>

            Expect.equal code 4 "headless substitute remains non-accepted"
            Expect.equal lines.Length InteractionContracts.all.Length "one record per interactive family"
            Expect.isTrue (root.TryGetProperty("controlIds", &property)) "control ids are written"
            Expect.isTrue (root.TryGetProperty("actionType", &property)) "action type is written"
            Expect.isTrue (root.TryGetProperty("expectedVisibleResult", &property)) "expected visible result is written"
            Expect.equal (root.GetProperty("acceptanceStatus").GetString()) "environment-limited" "substitute record is environment-limited, not accepted"
        }

        test "summary carries budgets, coverage, and relative records path" {
            let outDir = SecondAntShowcase.Tests.Feature172ResponsivenessFixtures.tempDir ()
            let _ =
                Responsiveness.run
                    [ "--script"; "representative"
                      "--theme"; "dark"
                      "--all-interactive"
                      "--out"; outDir
                      "--json" ]

            use doc = SecondAntShowcase.Tests.Feature172ResponsivenessFixtures.summaryJson outDir
            let root = doc.RootElement
            let budgets = root.GetProperty("budgets")
            let coverage = root.GetProperty("coverage")

            Expect.equal (root.GetProperty("recordsPath").GetString()) "records.jsonl" "records path is relative to run root"
            Expect.equal (budgets.GetProperty("inputToVisibleP95Ms").GetInt32()) 100 "p95 budget is feature budget"
            Expect.equal (budgets.GetProperty("inputToVisibleMaxMs").GetInt32()) 150 "max budget is feature budget"
            Expect.equal (coverage.GetProperty("requiredInteractiveFamilies").GetArrayLength()) InteractionContracts.all.Length "all families required"
            Expect.equal (coverage.GetProperty("acceptedInteractiveFamilies").GetArrayLength()) 0 "substitute evidence accepts no families"
            Expect.equal (coverage.GetProperty("blockedInteractiveFamilies").GetArrayLength()) InteractionContracts.all.Length "all families are blocked until live evidence"
            Expect.isGreaterThan (coverage.GetProperty("displayOnlyExclusions").GetArrayLength()) 0 "display-only exclusions are explicit"
            Expect.equal (coverage.GetProperty("missingInteractiveFamilies").GetArrayLength()) 0 "families are enumerated, not missing"
        }
    ]
