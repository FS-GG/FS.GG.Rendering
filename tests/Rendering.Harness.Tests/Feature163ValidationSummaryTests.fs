module Feature163ValidationSummaryTests

open Expecto
open Rendering.Harness

let private result (id: string) (status: ValidationLanes.LaneStatus) (required: bool) : ValidationLanes.LaneResult =
    { LaneId = id
      ReadinessRole = if required then ValidationLanes.Required else ValidationLanes.Optional
      Status = status
      Command = "synthetic command"
      StartedUtc = None
      CompletedUtc = None
      Elapsed = None
      TimeoutBudget = None
      LastActivityUtc = None
      LastActivityText = None
      ExitCode = None
      LogPath = $"lanes/{id}/log.txt"
      ResultPath = $"lanes/{id}/result.json"
      DiagnosticsPath = $"lanes/{id}/diagnostics.md"
      ResultArtifacts = [ $"lanes/{id}/result.json" ]
      RuntimeDiagnostics = None
      Reason = None
      Diagnostics = []
      Caveats = []
      AcceptedEnvironmentLimitation = None
      Substitution = None
      IsAggregate = id = "aggregate-solution" }

[<Tests>]
let tests =
    testList "Feature163 ValidationSummary" [
        test "Synthetic mixed summary renders all eight lane statuses and aggregate separation" {
            // SYNTHETIC: constructed lane results exercise status rendering without running processes.
            let lanes =
                [ result "passed" ValidationLanes.Passed true
                  result "failed" ValidationLanes.Failed true
                  result "timed" ValidationLanes.TimedOut true
                  result "stalled" ValidationLanes.NoProgressTimedOut true
                  result "skipped" ValidationLanes.Skipped true
                  result "canceled" ValidationLanes.Canceled true
                  result "not-run" ValidationLanes.NotRun true
                  result "environment" ValidationLanes.EnvironmentLimited true
                  result "aggregate-solution" ValidationLanes.Skipped false ]

            let summary: ValidationLanes.ValidationSummary =
                { RunId = "synthetic"
                  PolicyVersion = "validation-lanes-v1"
                  OverallReadiness = ValidationLanes.computeOverallReadiness lanes
                  ArtifactRoot = "readiness/lanes"
                  StartedUtc = System.DateTime.UtcNow
                  CompletedUtc = System.DateTime.UtcNow
                  FirstBlockingRequiredLane = ValidationLanes.firstBlockingRequiredLane lanes
                  LaneResults = lanes
                  Caveats = [ "aggregate-solution is separate" ]
                  ReplacementNotice = None }

            let rendered = ValidationLanes.renderSummaryMarkdown summary

            [ "passed"
              "failed"
              "timed-out"
              "no-progress-timeout"
              "skipped"
              "canceled"
              "not-run"
              "environment-limited" ]
            |> List.iter (fun token -> Expect.stringContains rendered token token)

            Expect.stringContains rendered "aggregate-solution" "aggregate row"
            Expect.equal summary.OverallReadiness ValidationLanes.Blocked "unaccepted environment limits and failures are blocked"
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
