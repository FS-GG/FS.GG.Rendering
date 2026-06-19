module SecondAntShowcase.Tests.Feature174EnvironmentLimitedTests

open Expecto

[<Tests>]
let tests =
    testList "Feature174 environment-limited evidence" [
        test "render lag probe names the live-boundary limitation without claiming parity" {
            let code, outDir = Feature174RenderLagFixtures.runProbe "button-click"
            use doc = Feature174RenderLagFixtures.summaryJson outDir
            let root = doc.RootElement
            let limitations = Feature174RenderLagFixtures.arrayStrings (root.GetProperty("environmentLimitations"))

            Expect.equal code 1 "substitute run fails closed"
            Expect.contains limitations "live-evidence:environment-limited" "summary names live limitation"
            Expect.equal (root.GetProperty("parityStatus").GetString()) "not-run" "parity is not claimed"
        }

        test "responsiveness fail-closed summary keeps feature 174 fields non-accepted" {
            let code, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            use doc = Feature173LiveResponsivenessFixtures.summaryJson outDir
            let root = doc.RootElement

            Expect.equal code 4 "responsiveness substitute fails closed"
            Expect.equal (root.GetProperty("overallReadiness").GetString()) "environment-limited" "overall readiness"
            Expect.equal (root.GetProperty("parityStatus").GetString()) "environment-limited" "parity readiness"
            Expect.isTrue (Feature173LiveResponsivenessFixtures.hasLimitationContaining "no-live-presentation-boundary" root) "missing live boundary named"
        }
    ]

