module Feature163ValidationSummaryTests

open Expecto
open Rendering.Harness

let private result (id: string) (status: ValidationLanes.LaneStatus) (required: bool) : ValidationLanes.LaneResult =
    { LaneId = id
      Status = status
      Command = "synthetic command"
      StartedUtc = None
      CompletedUtc = None
      Elapsed = None
      ExitCode = None
      LogPath = $"lanes/{id}/log.txt"
      ResultArtifacts = [ $"lanes/{id}/result.json" ]
      Diagnostics = []
      Caveats = []
      AcceptedException = None
      Required = required }

[<Tests>]
let tests =
    testList "Feature163 ValidationSummary" [
        test "Synthetic mixed summary renders all eight lane statuses and aggregate separation" {
            // SYNTHETIC: constructed lane results exercise status rendering without running processes.
            let lanes =
                [ result "passed" ValidationLanes.Passed true
                  result "failed" ValidationLanes.Failed true
                  result "timed" ValidationLanes.TimedOut true
                  result "hung" ValidationLanes.Hung true
                  result "skipped" ValidationLanes.Skipped true
                  result "canceled" ValidationLanes.Canceled true
                  result "not-run" ValidationLanes.NotRun true
                  result "environment" ValidationLanes.EnvironmentLimited true
                  result "aggregate-solution" ValidationLanes.Skipped false ]

            let summary: ValidationLanes.ValidationSummary =
                { PackageProofStatus = Some ValidationLanes.Passed
                  SelectedSamples = [ "samples/AntShowcase" ]
                  LocalFeedPath = "~/.local/share/nuget-local"
                  PackageCachePath = Some "readiness/package-proof/nuget-cache"
                  SourceRules = [ "FS.GG.UI.* -> nuget-local"; "* -> nuget.org" ]
                  LaneResults = lanes
                  OverallReadiness = ValidationLanes.computeOverallReadiness lanes
                  Caveats = [ "aggregate-solution is separate" ]
                  ArtifactRoot = "readiness/lanes" }

            let rendered = ValidationLanes.renderSummaryMarkdown summary

            [ "passed"
              "failed"
              "timed-out"
              "hung"
              "skipped"
              "canceled"
              "not-run"
              "environment-limited" ]
            |> List.iter (fun token -> Expect.stringContains rendered token token)

            Expect.stringContains rendered "aggregate-solution" "aggregate row"
            Expect.equal summary.OverallReadiness ValidationLanes.EnvironmentLimitedReadiness "environment limits are not green"
        }

        test "Synthetic readiness is blocked for failed required lanes and incomplete for not-run lanes" {
            // SYNTHETIC: direct result records isolate summary readiness rules from process execution.
            let blocked =
                [ result "package-proof" ValidationLanes.Passed true
                  result "controls" ValidationLanes.Failed true ]

            let incomplete =
                [ result "package-proof" ValidationLanes.Passed true
                  result "aggregate-solution" ValidationLanes.NotRun true ]

            let ready =
                [ result "package-proof" ValidationLanes.Passed true
                  result "controls" ValidationLanes.Passed true ]

            Expect.equal (ValidationLanes.computeOverallReadiness blocked) ValidationLanes.Blocked "blocked"
            Expect.equal (ValidationLanes.computeOverallReadiness incomplete) ValidationLanes.Incomplete "incomplete"
            Expect.equal (ValidationLanes.computeOverallReadiness ready) ValidationLanes.Ready "ready"
        }
    ]
