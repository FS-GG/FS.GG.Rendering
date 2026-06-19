module Feature167ResponsivenessSummaryTests

open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature167 responsiveness summary helpers" [
        test "summary accepts measured records inside budget" {
            let record = Feature167ResponsivenessFixtures.latency 24.0
            let summary =
                Viewer.summarizeResponsivenessRecords
                    "resp-elmish"
                    "elmish/fixture"
                    "records.jsonl"
                    record.ReceiptTimestamp
                    record.ReceiptTimestamp
                    Viewer.defaultResponsivenessBudget
                    [ record ]

            Expect.equal summary.OverallReadiness ViewerResponsivenessReadiness.Accepted "record is accepted"
            Expect.equal summary.Groups.Length 1 "group summary emitted"
        }
    ]
