module Feature166LaneStatusTests

open System
open System.IO
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature166LaneStatus" [
        test "Synthetic status tokens cover every contracted lane outcome" {
            // SYNTHETIC: token-only results isolate the public status vocabulary from process execution.
            [ ValidationLanes.Passed, "passed"
              ValidationLanes.Failed, "failed"
              ValidationLanes.TimedOut, "timed-out"
              ValidationLanes.NoProgressTimedOut, "no-progress-timeout"
              ValidationLanes.Canceled, "canceled"
              ValidationLanes.Skipped, "skipped"
              ValidationLanes.InfrastructureError, "infrastructure-error"
              ValidationLanes.EnvironmentLimited, "environment-limited"
              ValidationLanes.NotRun, "not-run" ]
            |> List.iter (fun (status, token) -> Expect.equal (ValidationLanes.statusToken status) token token)
        }

        test "process runner classifies pass fail timeout no-progress and evidence infrastructure failures" {
            let root = Feature166TestFixtures.createTempRoot "feature166-status-process"

            try
                let passed = ValidationLanes.runLane (Feature166TestFixtures.lane root "passed" "printf 'ok\n'")
                Expect.equal passed.Status ValidationLanes.Passed "passed"
                Expect.isTrue (File.Exists passed.ResultPath) "result"

                let failed = ValidationLanes.runLane (Feature166TestFixtures.lane root "failed" "printf 'fail\n'; exit 7")
                Expect.equal failed.Status ValidationLanes.Failed "failed"
                Expect.equal failed.ExitCode (Some 7) "exit code"

                let timed =
                    Feature166TestFixtures.laneWith
                        root
                        "timed"
                        ValidationLanes.Required
                        "sleep 2"
                        (TimeSpan.FromMilliseconds 100.0)
                        None
                        (Some "timed")
                        (Some "timed")
                    |> ValidationLanes.runLane

                Expect.equal timed.Status ValidationLanes.TimedOut "timed out"

                let stalled =
                    Feature166TestFixtures.laneWith
                        root
                        "stalled"
                        ValidationLanes.Required
                        "printf 'start\n'; sleep 2"
                        (TimeSpan.FromSeconds 2.0)
                        (Some(TimeSpan.FromMilliseconds 100.0))
                        (Some "stalled")
                        (Some "stalled")
                    |> ValidationLanes.runLane

                Expect.equal stalled.Status ValidationLanes.NoProgressTimedOut "no progress"

                let blocker = Path.Combine(root, "blocker")
                File.WriteAllText(blocker, "not a directory")

                let infra =
                    { Feature166TestFixtures.lane root "infra" "printf no" with
                        EvidenceDirectory = blocker
                        LogPath = Path.Combine(blocker, "log.txt")
                        ResultPath = Path.Combine(blocker, "result.json")
                        DiagnosticsPath = Path.Combine(blocker, "diagnostics.md") }
                    |> ValidationLanes.runLane

                Expect.equal infra.Status ValidationLanes.InfrastructureError "infrastructure error"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "Synthetic MVU transitions emit preflight heartbeat timeout cancellation and summary effects" {
            // SYNTHETIC: direct messages exercise pure MVU transitions without starting child processes.
            let root = Feature166TestFixtures.createTempRoot "feature166-status-mvu"

            try
                let lane = Feature166TestFixtures.lane root "rendering-harness" "printf ok"
                let model, effects = ValidationLanes.init [ lane ]
                Expect.contains effects ValidationLanes.RegisterCancelHandler "cancel handler"

                let request =
                    { ValidationLanes.defaultRunRequest (Path.Combine(root, "out")) with
                        RequestedLaneIds = [ lane.Id ] }

                let _, requestEffects = ValidationLanes.update (ValidationLanes.RunRequested request) model
                Expect.contains requestEffects (ValidationLanes.ValidateRequest request) "validate"

                let plan: ValidationLanes.LaneRunPlan =
                    { Request = request
                      RunId = "run"
                      SelectionMode = ValidationLanes.ExplicitSelection
                      ArtifactRoot = Path.Combine(root, "out", "run")
                      SelectedLanes = [ lane ]
                      Diagnostics = []
                      ReplacementNotice = None }

                let scheduled, scheduleEffects = ValidationLanes.update (ValidationLanes.PreflightPassed plan) model
                Expect.contains scheduleEffects (ValidationLanes.CreateRunRoot plan.ArtifactRoot) "run root"

                let running, heartbeatEffects =
                    ValidationLanes.update (ValidationLanes.LaneHeartbeatDue(lane.Id, DateTime.UtcNow)) scheduled

                Expect.contains heartbeatEffects (ValidationLanes.PublishHeartbeat lane.Id) "heartbeat"

                let _, timeoutEffects = ValidationLanes.update (ValidationLanes.LaneTimedOut(lane.Id, "timeout")) running
                Expect.contains timeoutEffects (ValidationLanes.StopProcess lane.Id) "stop timeout"

                let _, noProgressEffects = ValidationLanes.update (ValidationLanes.LaneNoProgressTimedOut(lane.Id, "stalled")) running
                Expect.contains noProgressEffects (ValidationLanes.StopProcess lane.Id) "stop no-progress"

                let _, infraEffects = ValidationLanes.update (ValidationLanes.InfrastructureErrorRaised(Some lane.Id, "write failed")) running
                Expect.contains infraEffects ValidationLanes.WriteSummary "summary on infra"

                let canceled, cancelEffects = ValidationLanes.update (ValidationLanes.OperatorCanceled "ctrl-c") running
                Expect.contains canceled.Diagnostics "ctrl-c" "cancel reason"
                Expect.contains cancelEffects ValidationLanes.WriteSummary "summary on cancel"

                let _, summaryEffects = ValidationLanes.update ValidationLanes.SummaryRequested running
                Expect.contains summaryEffects ValidationLanes.WriteSummary "summary"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }
    ]
