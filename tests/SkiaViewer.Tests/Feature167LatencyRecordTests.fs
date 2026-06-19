module Feature167LatencyRecordTests

open System.Text.Json
open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature167 latency records" [
        test "JSONL line carries required fields and stable tokens" {
            let line =
                Feature167SchedulerFixtures.latency 4 ViewerResponsivenessInputKind.PointerDiscrete 32.0
                |> Viewer.latencyRecordToJsonLine

            use doc = JsonDocument.Parse line
            let root = doc.RootElement

            Expect.equal (root.GetProperty("inputKind").GetString()) "pointer-discrete" "stable input token"
            Expect.equal (root.GetProperty("visibleResponse").GetString()) "presented-frame" "stable response token"
            Expect.equal (root.GetProperty("environmentStatus").GetString()) "measured" "stable environment token"
            let mutable total = Unchecked.defaultof<JsonElement>
            Expect.isTrue (root.GetProperty("phaseTiming").TryGetProperty("totalInputToVisibleMs", &total)) "phase timing is present"
        }

        test "stable token helpers cover readiness vocabulary" {
            Expect.equal (Viewer.responsivenessReadinessToken ViewerResponsivenessReadiness.Accepted) "accepted" "accepted token"
            Expect.equal (Viewer.responsivenessReadinessToken ViewerResponsivenessReadiness.Rejected) "rejected" "rejected token"
            Expect.equal (Viewer.responsivenessReadinessToken ViewerResponsivenessReadiness.EnvironmentLimited) "environment-limited" "environment-limited token"
        }
    ]
