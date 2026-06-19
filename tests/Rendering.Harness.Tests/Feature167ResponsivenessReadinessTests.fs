module Feature167ResponsivenessReadinessTests

open System
open System.IO
open System.Text.Json
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature167 responsiveness readiness summary" [
        test "validation lane reader consumes summary.json without Markdown parsing" {
            let dir = Path.Combine(Path.GetTempPath(), "feature167-lanes-" + Guid.NewGuid().ToString("N"))
            Directory.CreateDirectory dir |> ignore
            let path = Path.Combine(dir, "summary.json")

            let json =
                JsonSerializer.Serialize(
                    {| runId = "resp-lane"
                       scope = "antshowcase/buttons/light"
                       overallReadiness = "environment-limited"
                       groups =
                        [| {| count = 2 |}
                           {| count = 1 |} |]
                       firstFailedBudget = null
                       environmentLimitations = [ "headless-substitute:no-live-presentation-boundary" ]
                       diagnostics = [ "SYNTHETIC: deterministic substitute" ] |},
                    JsonSerializerOptions(WriteIndented = true)
                )

            File.WriteAllText(path, json)

            match ValidationLanes.readResponsivenessSummary path with
            | Error message -> failtest message
            | Ok summary ->
                Expect.equal summary.OverallReadiness "environment-limited" "readiness token"
                Expect.equal summary.RecordCount 3 "record count is aggregated from groups"
                Expect.equal (ValidationLanes.responsivenessSummaryLaneStatus summary) ValidationLanes.EnvironmentLimited "lane status maps environment limitation"
        }
    ]
