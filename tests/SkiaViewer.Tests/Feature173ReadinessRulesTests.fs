module Feature173ReadinessRulesTests

open System
open Expecto
open FS.GG.UI.SkiaViewer

let private featureBudget =
    { Viewer.defaultResponsivenessBudget with
        InputToVisibleP95 = TimeSpan.FromMilliseconds 100.0
        InputToVisibleMax = TimeSpan.FromMilliseconds 150.0 }

let private withTotal total record =
    { record with
        PhaseTiming = Feature167SchedulerFixtures.phase total
        LongFrame = false }

let private environmentLimited record =
    { record with
        VisibleResponse = ViewerResponsivenessVisibleResponse.EnvironmentLimited
        EnvironmentStatus = ViewerResponsivenessEnvironmentStatus.MissingBoundary
        PhaseTiming =
            { Feature167SchedulerFixtures.phase 0.0 with
                TotalInputToVisibleDuration = None }
        PresentedFrameId = None
        LongFrame = false }

[<Tests>]
let tests =
    testList "Feature173 readiness rules" [
        test "stable readiness tokens include rejected" {
            Expect.equal (Viewer.responsivenessReadinessToken ViewerResponsivenessReadiness.Rejected) "rejected" "rejected token"
        }

        test "measured max latency over budget rejects readiness" {
            let records =
                [ for index in 1..19 do
                      Feature167SchedulerFixtures.latency index ViewerResponsivenessInputKind.PointerDiscrete 80.0
                  Feature167SchedulerFixtures.latency 20 ViewerResponsivenessInputKind.PointerDiscrete 151.0
                  |> withTotal 151.0 ]

            let summary =
                Viewer.summarizeResponsivenessRecords
                    "resp-feature173-max"
                    "second-antshowcase/all-interactive/light"
                    "records.jsonl"
                    Feature167SchedulerFixtures.now
                    Feature167SchedulerFixtures.now
                    featureBudget
                    records

            Expect.equal summary.OverallReadiness ViewerResponsivenessReadiness.Rejected "measured max budget miss is rejected"
            Expect.equal (summary.FirstFailedBudget |> Option.map _.Kind) (Some "input-to-visible-max") "max budget is named"
        }

        test "environment boundary failure is ordered before timing budgets" {
            let records =
                [ Feature167SchedulerFixtures.latency 1 ViewerResponsivenessInputKind.PointerDiscrete 250.0
                  |> environmentLimited ]

            let summary =
                Viewer.summarizeResponsivenessRecords
                    "resp-feature173-env"
                    "second-antshowcase/all-interactive/light"
                    "records.jsonl"
                    Feature167SchedulerFixtures.now
                    Feature167SchedulerFixtures.now
                    featureBudget
                    records

            Expect.equal summary.OverallReadiness ViewerResponsivenessReadiness.EnvironmentLimited "environment-limited dominates budget rejection"
            Expect.equal (summary.FirstFailedBudget |> Option.map _.Kind) (Some "environment-boundary") "environment failure is first"
        }

        test "slowest interaction reporting keeps five measured records" {
            let records =
                [ 1.0; 2.0; 3.0; 4.0; 5.0; 6.0 ]
                |> List.mapi (fun index total ->
                    Feature167SchedulerFixtures.latency (index + 1) ViewerResponsivenessInputKind.PointerDiscrete total
                    |> withTotal total)

            let summary =
                Viewer.summarizeResponsivenessRecords
                    "resp-feature173-slowest"
                    "second-antshowcase/all-interactive/light"
                    "records.jsonl"
                    Feature167SchedulerFixtures.now
                    Feature167SchedulerFixtures.now
                    featureBudget
                    records

            Expect.equal summary.SlowestInteractions.Length 5 "five slowest interactions are retained"
            Expect.equal summary.SlowestInteractions.Head.TotalInputToVisible (Some(TimeSpan.FromMilliseconds 6.0)) "slowest is first"
        }
    ]
