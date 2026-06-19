module SecondAntShowcase.Tests.Feature172ResponsivenessFailClosedTests

open System.Text.Json
open Expecto
open SecondAntShowcase.App

[<Tests>]
let tests =
    testList "Feature172 responsiveness fail-closed behavior" [
        test "require-live writes a visible limitation instead of accepting substitute evidence" {
            let outDir = SecondAntShowcase.Tests.Feature172ResponsivenessFixtures.tempDir ()
            let code =
                Responsiveness.run
                    [ "--all-interactive"
                      "--require-live"
                      "--out"; outDir
                      "--json" ]

            use doc = SecondAntShowcase.Tests.Feature172ResponsivenessFixtures.summaryJson outDir
            let limitations = doc.RootElement.GetProperty("environmentLimitations").EnumerateArray() |> Seq.map _.GetString() |> Seq.toList

            Expect.equal code 4 "require-live without presentation boundary is blocked/environment-limited"
            Expect.contains limitations "require-live:visible-surface-unavailable" "require-live caveat is visible"
            Expect.equal (doc.RootElement.GetProperty("overallReadiness").GetString()) "environment-limited" "not accepted"
        }

        test "records remain blocked when there is no measured presentation boundary" {
            let outDir = SecondAntShowcase.Tests.Feature172ResponsivenessFixtures.tempDir ()
            let _ = Responsiveness.run [ "--all-interactive"; "--out"; outDir; "--json" ]
            let line = SecondAntShowcase.Tests.Feature172ResponsivenessFixtures.records outDir |> List.head
            use doc = JsonDocument.Parse line
            let root = doc.RootElement

            Expect.equal (root.GetProperty("environmentStatus").GetString()) "headless-substitute" "environment status is explicit"
            Expect.equal (root.GetProperty("visibleResponse").GetString()) "environment-limited" "visible response is not presented-frame"
            Expect.equal (root.GetProperty("acceptanceStatus").GetString()) "blocked" "record is not accepted"
            Expect.isFalse (root.GetProperty("phaseTiming").GetProperty("totalInputToVisibleMs").ValueKind = JsonValueKind.Number) "no live total timing is synthesized"
        }
    ]
