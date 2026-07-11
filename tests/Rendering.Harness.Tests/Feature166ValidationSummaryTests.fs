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
                    [ Feature166TestFixtures.result root "build" ValidationLanes.Required ValidationLanes.Passed
                      Feature166TestFixtures.result root "controls" ValidationLanes.Required ValidationLanes.Failed
                      Feature166TestFixtures.result root "aggregate-solution" ValidationLanes.Optional ValidationLanes.NotRun ]

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
                    [ Feature166TestFixtures.result root "build" ValidationLanes.Required ValidationLanes.Passed ]
                    |> Feature166TestFixtures.summary root

                let stopwatch = Stopwatch.StartNew()
                let paths = ValidationLanes.writeSummary root summary
                stopwatch.Stop()

                Expect.isLessThan stopwatch.Elapsed.TotalSeconds 10.0 "summary emitted within budget"
                paths |> List.iter (fun path -> Expect.isTrue (File.Exists path) path)
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "Synthetic lane evidence is written under the run root, never the process CWD" {
            // REGRESSION (#448): writeSummary emitted each LaneResult's paths verbatim, so a path that
            // did not resolve under the run root escaped — a relative one against the process CWD,
            // dropping lanes/<id>/{result.json,diagnostics.md} wherever the harness happened to run.
            let root = Feature166TestFixtures.createTempRoot "feature166-summary-rooting"

            // Unique per run: the escape lands at <cwd>/lanes/<laneId>, and a fixed id would let a
            // stale directory from an earlier (pre-fix) run decide this assertion instead of the code.
            let laneId = "rooting-probe-" + Guid.NewGuid().ToString("N")
            let escaped = Path.Combine(Directory.GetCurrentDirectory(), "lanes", laneId)

            try
                let summary =
                    [ Feature166TestFixtures.result root laneId ValidationLanes.Required ValidationLanes.Passed ]
                    |> Feature166TestFixtures.summary root

                ValidationLanes.writeSummary root summary |> ignore

                Expect.isTrue
                    (File.Exists(Path.Combine(root, "lanes", laneId, "result.json")))
                    "lane result written under the run root"

                Expect.isTrue
                    (File.Exists(Path.Combine(root, "lanes", laneId, "diagnostics.md")))
                    "lane diagnostics written under the run root"

                Expect.isFalse (Directory.Exists escaped) "no lane evidence written beside the process CWD"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "Synthetic lane evidence that escapes the run root is refused, not silently scattered" {
            // REGRESSION (#448), failure leg: the escape used to be silent — files written outside the
            // run root, no error, run reports success. Assert the reason names the lane and the path,
            // so a future writer that reintroduces a CWD-relative evidence path fails loudly.
            let root = Feature166TestFixtures.createTempRoot "feature166-summary-escape"
            let laneId = "escape-probe-" + Guid.NewGuid().ToString("N")
            let cwdRelative = $"lanes/{laneId}/result.json"

            try
                let escaping =
                    { Feature166TestFixtures.result root laneId ValidationLanes.Required ValidationLanes.Passed with
                        ResultPath = cwdRelative }

                let summary = Feature166TestFixtures.summary root [ escaping ]

                let reason =
                    try
                        ValidationLanes.writeSummary root summary |> ignore
                        None
                    with :? System.InvalidOperationException as ex ->
                        Some ex.Message

                let reason =
                    match reason with
                    | Some message -> message
                    | None -> failtest "writeSummary accepted evidence that escapes the run root"

                Expect.stringContains reason laneId "reason names the offending lane"
                Expect.stringContains reason cwdRelative "reason names the offending path"
                Expect.isFalse
                    (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "lanes", laneId, "result.json")))
                    "the refused evidence was not written on the way out"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "Synthetic readiness rules keep optional aggregate failures separate from required readiness" {
            // SYNTHETIC: direct results prove optional aggregate failure does not override required success.
            let readyWithOptionalFailure =
                [ Feature166TestFixtures.result Feature166TestFixtures.syntheticRunRoot "build" ValidationLanes.Required ValidationLanes.Passed
                  Feature166TestFixtures.result Feature166TestFixtures.syntheticRunRoot "controls" ValidationLanes.Required ValidationLanes.Passed
                  Feature166TestFixtures.result Feature166TestFixtures.syntheticRunRoot "aggregate-solution" ValidationLanes.Optional ValidationLanes.Failed ]

            Expect.equal (ValidationLanes.computeOverallReadiness readyWithOptionalFailure) ValidationLanes.Ready "optional failure separate"

            [ ValidationLanes.Failed
              ValidationLanes.TimedOut
              ValidationLanes.NoProgressTimedOut
              ValidationLanes.Canceled
              ValidationLanes.EnvironmentLimited
              ValidationLanes.InfrastructureError ]
            |> List.iter (fun status ->
                let readiness =
                    [ Feature166TestFixtures.result Feature166TestFixtures.syntheticRunRoot "build" ValidationLanes.Required ValidationLanes.Passed
                      Feature166TestFixtures.result Feature166TestFixtures.syntheticRunRoot "controls" ValidationLanes.Required status ]
                    |> ValidationLanes.computeOverallReadiness

                Expect.equal readiness ValidationLanes.Blocked (ValidationLanes.statusToken status))

            let incomplete =
                [ Feature166TestFixtures.result Feature166TestFixtures.syntheticRunRoot "build" ValidationLanes.Required ValidationLanes.Passed
                  Feature166TestFixtures.result Feature166TestFixtures.syntheticRunRoot "controls" ValidationLanes.Required ValidationLanes.NotRun ]

            Expect.equal (ValidationLanes.computeOverallReadiness incomplete) ValidationLanes.Incomplete "not-run incomplete"
        }

        test "replacement notice is rendered when a run is explicitly replaced" {
            let root = Feature166TestFixtures.createTempRoot "feature166-summary-replace"

            try
                let summary =
                    { Feature166TestFixtures.summary root [ Feature166TestFixtures.result root "build" ValidationLanes.Required ValidationLanes.Passed ] with
                        ReplacementNotice = Some "Run `same-run` replaced existing evidence." }

                let markdown = ValidationLanes.renderSummaryMarkdown summary
                Expect.stringContains markdown "Replacement notice" "replacement"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }
    ]
