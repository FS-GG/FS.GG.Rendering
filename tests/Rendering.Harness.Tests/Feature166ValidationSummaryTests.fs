module Feature166ValidationSummaryTests

open System
open System.Diagnostics
open System.IO
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature166ValidationSummary" [
        test "Synthetic Markdown and JSON summaries agree on readiness fields and evidence paths" {
            // SYNTHETIC: mixed result set exercises reviewer summary rendering without running long lanes.
            let root = Feature166TestFixtures.createTempRoot "feature166-summary"

            try
                let results =
                    [ Feature166TestFixtures.result "build" ValidationLanes.Required ValidationLanes.Passed
                      Feature166TestFixtures.result "controls" ValidationLanes.Required ValidationLanes.Failed
                      Feature166TestFixtures.result "aggregate-solution" ValidationLanes.Optional ValidationLanes.NotRun ]

                let summary = Feature166TestFixtures.summary root results
                let markdown = ValidationLanes.renderSummaryMarkdown summary
                let json = ValidationLanes.renderSummaryJson summary

                Expect.stringContains markdown "controls" "blocking lane"
                Expect.stringContains markdown "aggregate-solution" "aggregate row"
                Expect.stringContains markdown "summary.json" "summary json link"
                Expect.stringContains json "\"overallReadiness\":\"blocked\"" "json readiness"
                Expect.stringContains json "\"firstBlockingRequiredLane\":\"controls\"" "first blocker"
                Expect.stringContains json "lanes/controls/log.txt" "log path"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "Synthetic summary write completes under SC-001 final summary timing budget" {
            // SYNTHETIC: small result set measures summary emission, not real lane duration.
            let root = Feature166TestFixtures.createTempRoot "feature166-summary-timing"

            try
                let summary =
                    [ Feature166TestFixtures.result "build" ValidationLanes.Required ValidationLanes.Passed ]
                    |> Feature166TestFixtures.summary root

                let stopwatch = Stopwatch.StartNew()
                let paths = ValidationLanes.writeSummary root summary
                stopwatch.Stop()

                Expect.isLessThan stopwatch.Elapsed.TotalSeconds 10.0 "summary emitted within budget"
                paths |> List.iter (fun path -> Expect.isTrue (File.Exists path) path)
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "Synthetic readiness rules keep optional aggregate failures separate from required readiness" {
            // SYNTHETIC: direct results prove optional aggregate failure does not override required success.
            let readyWithOptionalFailure =
                [ Feature166TestFixtures.result "build" ValidationLanes.Required ValidationLanes.Passed
                  Feature166TestFixtures.result "controls" ValidationLanes.Required ValidationLanes.Passed
                  Feature166TestFixtures.result "aggregate-solution" ValidationLanes.Optional ValidationLanes.Failed ]

            Expect.equal (ValidationLanes.computeOverallReadiness readyWithOptionalFailure) ValidationLanes.Ready "optional failure separate"

            [ ValidationLanes.Failed
              ValidationLanes.TimedOut
              ValidationLanes.NoProgressTimedOut
              ValidationLanes.Canceled
              ValidationLanes.EnvironmentLimited
              ValidationLanes.InfrastructureError ]
            |> List.iter (fun status ->
                let readiness =
                    [ Feature166TestFixtures.result "build" ValidationLanes.Required ValidationLanes.Passed
                      Feature166TestFixtures.result "controls" ValidationLanes.Required status ]
                    |> ValidationLanes.computeOverallReadiness

                Expect.equal readiness ValidationLanes.Blocked (ValidationLanes.statusToken status))

            let incomplete =
                [ Feature166TestFixtures.result "build" ValidationLanes.Required ValidationLanes.Passed
                  Feature166TestFixtures.result "controls" ValidationLanes.Required ValidationLanes.NotRun ]

            Expect.equal (ValidationLanes.computeOverallReadiness incomplete) ValidationLanes.Incomplete "not-run incomplete"
        }

        test "replacement notice is rendered when a run is explicitly replaced" {
            let root = Feature166TestFixtures.createTempRoot "feature166-summary-replace"

            try
                let summary =
                    { Feature166TestFixtures.summary root [ Feature166TestFixtures.result "build" ValidationLanes.Required ValidationLanes.Passed ] with
                        ReplacementNotice = Some "Run `same-run` replaced existing evidence." }

                let markdown = ValidationLanes.renderSummaryMarkdown summary
                Expect.stringContains markdown "Replacement notice" "replacement"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }
    ]
