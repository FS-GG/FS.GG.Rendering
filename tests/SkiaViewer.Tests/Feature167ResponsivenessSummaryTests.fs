module Feature167ResponsivenessSummaryTests

open System
open System.IO
open System.Text.Json
open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature167 responsiveness summary" [
        test "summary JSON and Markdown agree on readiness and first failed budget" {
            let records =
                [ Feature167SchedulerFixtures.latency 1 ViewerResponsivenessInputKind.KeyDown 20.0
                  Feature167SchedulerFixtures.latency 2 ViewerResponsivenessInputKind.PointerDiscrete 72.0 ]

            let summary =
                Viewer.summarizeResponsivenessRecords
                    "resp-test"
                    "antshowcase/buttons/light"
                    "records.jsonl"
                    Feature167SchedulerFixtures.now
                    (Feature167SchedulerFixtures.now.AddSeconds 1.0)
                    Viewer.defaultResponsivenessBudget
                    records

            let json = Viewer.responsivenessSummaryToJson summary
            let markdown = Viewer.responsivenessSummaryToMarkdown summary
            use doc = JsonDocument.Parse json

            Expect.equal (doc.RootElement.GetProperty("overallReadiness").GetString()) "rejected" "JSON readiness"
            Expect.stringContains markdown "overall readiness: rejected" "Markdown readiness"
            Expect.stringContains markdown "input-to-visible-p95" "Markdown names first failed budget"
        }

        test "writeResponsivenessRun writes all required artifacts" {
            let root = Path.Combine(Path.GetTempPath(), "fs-gg-feature167-" + Guid.NewGuid().ToString("N"))
            let records = [ Feature167SchedulerFixtures.latency 1 ViewerResponsivenessInputKind.KeyDown 12.0 ]
            let summary =
                Viewer.summarizeResponsivenessRecords
                    "resp-write"
                    "test"
                    "records.jsonl"
                    Feature167SchedulerFixtures.now
                    Feature167SchedulerFixtures.now
                    Viewer.defaultResponsivenessBudget
                    records

            let paths = Viewer.writeResponsivenessRun root summary records

            Expect.equal paths.Length 4 "records, summary.json, summary.md, environment.md"
            Expect.isTrue (File.Exists(Path.Combine(root, "resp-write", "records.jsonl"))) "records written"
            Expect.isTrue (File.Exists(Path.Combine(root, "resp-write", "summary.json"))) "summary JSON written"
        }
    ]
