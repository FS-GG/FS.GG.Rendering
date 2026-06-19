module SecondAntShowcase.Tests.Feature173LiveResponsivenessFailClosedTests

open System.IO
open Expecto

[<Tests>]
let tests =
    testList "Feature173 live responsiveness fail closed" [
        test "missing live boundary remains non-accepted with limitations" {
            let code, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            use doc = Feature173LiveResponsivenessFixtures.summaryJson outDir
            let root = doc.RootElement

            Expect.equal code 4 "live unavailable exit code"
            Expect.equal (root.GetProperty("overallReadiness").GetString()) "environment-limited" "non-accepted"
            Expect.isTrue (Feature173LiveResponsivenessFixtures.hasLimitationContaining "no-live-presentation-boundary" root) "missing boundary named"
        }

        test "environment artifact carries actionable diagnostics" {
            let _, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            let text = File.ReadAllText(Feature173LiveResponsivenessFixtures.environmentFile outDir)

            Expect.stringContains text "visible, focusable desktop session" "visible session prerequisite named"
            Expect.stringContains text "artifact-write-status: complete" "write status named"
        }
    ]
