module SecondAntShowcase.Tests.Feature174RenderLagProbeTests

open System.IO
open System.Text.Json
open Expecto

[<Tests>]
let tests =
    testList "Feature174 render lag probe artifacts" [
        test "forced substitute writes contracted button-click artifacts" {
            let code, outDir = Feature174RenderLagFixtures.runProbe "button-click"
            use doc = Feature174RenderLagFixtures.summaryJson outDir
            use recordDoc = Feature174RenderLagFixtures.firstPhaseRecord outDir
            let root = doc.RootElement
            let record = recordDoc.RootElement

            Expect.equal code 1 "substitute run fails closed"
            Expect.equal (root.GetProperty("scenarioId").GetString()) "button-click" "summary scenario"
            Expect.equal (root.GetProperty("baselineProfileId").GetString()) "2026-06-19" "baseline id"
            Expect.isNonEmpty (root.GetProperty("optimizedProfileId").GetString()) "optimized profile id"
            Expect.equal (root.GetProperty("status").GetString()) "environment-limited" "summary status"
            Expect.equal (root.GetProperty("parityStatus").GetString()) "not-run" "live parity is not claimed"
            Expect.equal (root.GetProperty("preparationReductionPercent").ValueKind) JsonValueKind.Null "no synthetic reduction"
            Expect.equal (root.GetProperty("firstFramePreparationReductionPercent").ValueKind) JsonValueKind.Null "no synthetic first-frame reduction"

            Expect.equal (record.GetProperty("scenarioId").GetString()) "button-click" "record scenario"
            Expect.equal (record.GetProperty("environmentStatus").GetString()) "environment-limited" "record status"
            Expect.equal (record.GetProperty("dominantPhase").GetString()) "unknown" "no synthetic dominant phase"
            Expect.equal (record.GetProperty("metadataVisitedNodeCount").GetInt32()) 0 "no synthetic metadata visits"
        }

        test "page-change artifacts include phase, trace, and markdown files" {
            let code, outDir = Feature174RenderLagFixtures.runProbe "page-change"
            use doc = Feature174RenderLagFixtures.summaryJson outDir
            let root = doc.RootElement
            let trace = File.ReadAllText(Feature174RenderLagFixtures.traceFile outDir)
            let markdown = File.ReadAllText(Feature174RenderLagFixtures.summaryMarkdownFile outDir)

            Expect.equal code 1 "substitute run fails closed"
            Expect.equal (root.GetProperty("scenarioId").GetString()) "page-change" "summary scenario"
            Expect.isTrue (File.Exists(Feature174RenderLagFixtures.phaseRecordsFile outDir)) "phase records written"
            Expect.stringContains trace "headless-substitute:no-live-presentation-boundary" "trace names missing live boundary"
            Expect.stringContains markdown "baseline profile: 2026-06-19" "markdown names baseline"
            Expect.stringContains markdown "no accepted live performance claim" "markdown names caveat"
            Expect.stringStarts (Feature174RenderLagFixtures.runRoot outDir) (Path.GetFullPath outDir) "run root stays under out dir"
        }
    ]
