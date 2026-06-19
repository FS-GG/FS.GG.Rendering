module Feature163ValidationLaneTests

open System
open System.IO
open Expecto
open Rendering.Harness

let private lane
    (root: string)
    (id: string)
    (args: string)
    (timeout: TimeSpan)
    (noProgress: TimeSpan option)
    : ValidationLanes.LaneDefinition =
    let dir = Path.Combine(root, id)
    let command: ValidationLanes.LaneCommand = { FileName = "bash"; Arguments = [ "-lc"; args ] }

    { Id = id
      DisplayName = id
      Description = id
      ReadinessRole = ValidationLanes.Required
      Command = command
      WorkingDirectory = root
      Timeout = timeout
      NoProgressTimeout = noProgress
      ProgressInterval = TimeSpan.FromSeconds 1.0
      EvidenceDirectory = dir
      LogPath = Path.Combine(dir, "log.txt")
      ResultPath = Path.Combine(dir, "result.json")
      DiagnosticsPath = Path.Combine(dir, "diagnostics.md")
      OutputRoot = Path.Combine(dir, "out")
      ConcurrencyGroup = Some "test"
      OutputScope = Some id
      IsAggregate = false
      SubstitutesFor = None }

[<Tests>]
let tests =
    testList "Feature163 ValidationLanes" [
        test "default lane definitions include required minimum lanes with isolated outputs" {
            let root = Feature163TestFixtures.createTempRoot "feature163-lanes"

            try
                let lanes = ValidationLanes.defaultLaneDefinitions root (Path.Combine(root, "lanes"))
                let ids = lanes |> List.map _.Id

                [ "build"; "library-tests"; "package-proof"; "antshowcase-sample"; "controls"; "rendering-harness"; "aggregate-solution" ]
                |> List.iter (fun id -> Expect.contains ids id id)

                let outputRoots = lanes |> List.map _.OutputRoot |> Set.ofList
                let logPaths = lanes |> List.map _.LogPath |> Set.ofList
                Expect.equal outputRoots.Count lanes.Length "isolated output roots"
                Expect.equal logPaths.Count lanes.Length "isolated logs"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        test "pure lane MVU records start completion cancellation and summary effects" {
            let root = Feature163TestFixtures.createTempRoot "feature163-lane-mvu"

            try
                let def = lane root "pass" "printf ok" (TimeSpan.FromSeconds 2.0) None
                let model, effects = ValidationLanes.init [ def ]
                Expect.contains effects ValidationLanes.RegisterCancelHandler "cancel handler"

                let request =
                    { ValidationLanes.defaultRunRequest root with
                        RequestedLaneIds = [ "pass" ] }

                let _, requestEffects = ValidationLanes.update (ValidationLanes.RunRequested request) model
                Expect.contains requestEffects (ValidationLanes.ValidateRequest request) "validate"

                let plan: ValidationLanes.LaneRunPlan =
                    { Request = request
                      RunId = "run"
                      SelectionMode = ValidationLanes.ExplicitSelection
                      ArtifactRoot = Path.Combine(root, "run")
                      SelectedLanes = [ def ]
                      Diagnostics = []
                      ReplacementNotice = None }

                let scheduled, scheduleEffects = ValidationLanes.update (ValidationLanes.PreflightPassed plan) model
                Expect.contains scheduleEffects (ValidationLanes.CreateRunRoot plan.ArtifactRoot) "create run root"

                let running, runningEffects = ValidationLanes.update (ValidationLanes.LaneStarted("pass", DateTime.UtcNow)) scheduled
                Expect.contains runningEffects (ValidationLanes.PollProcess "pass") "poll"

                let result: ValidationLanes.LaneResult =
                    { LaneId = "pass"
                      ReadinessRole = ValidationLanes.Required
                      Status = ValidationLanes.Passed
                      Command = "bash -lc pass"
                      StartedUtc = None
                      CompletedUtc = None
                      Elapsed = None
                      TimeoutBudget = None
                      LastActivityUtc = None
                      LastActivityText = None
                      ExitCode = Some 0
                      LogPath = "log"
                      ResultPath = "result"
                      DiagnosticsPath = "diagnostics"
                      ResultArtifacts = [ "result" ]
                      RuntimeDiagnostics = None
                      Reason = None
                      Diagnostics = []
                      Caveats = []
                      AcceptedEnvironmentLimitation = None
                      Substitution = None
                      IsAggregate = false }

                let completed, completedEffects = ValidationLanes.update (ValidationLanes.LaneCompleted result) running
                Expect.contains completed.CompletedResults result "completed"
                Expect.contains completedEffects (ValidationLanes.WriteLaneResult "pass") "write result"

                let canceled, cancelEffects = ValidationLanes.update (ValidationLanes.LaneCanceled("pass", "manual")) completed
                Expect.contains canceled.CanceledLaneIds "pass" "canceled"
                Expect.contains cancelEffects (ValidationLanes.StopProcess "pass") "stop"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }

        test "process runner classifies passed failed timed-out and no-progress lanes" {
            let root = Feature163TestFixtures.createTempRoot "feature163-lane-runner"

            try
                let passed = ValidationLanes.runLane (lane root "passed" "printf ok" (TimeSpan.FromSeconds 2.0) None)
                Expect.equal passed.Status ValidationLanes.Passed "passed"
                Expect.isTrue (File.Exists passed.LogPath) "passed log"

                let failed = ValidationLanes.runLane (lane root "failed" "printf fail; exit 7" (TimeSpan.FromSeconds 2.0) None)
                Expect.equal failed.Status ValidationLanes.Failed "failed"
                Expect.equal failed.ExitCode (Some 7) "exit code"

                let timedOut = ValidationLanes.runLane (lane root "timed" "sleep 2" (TimeSpan.FromMilliseconds 100.0) None)
                Expect.equal timedOut.Status ValidationLanes.TimedOut "timed out"

                let stalled = ValidationLanes.runLane (lane root "stalled" "printf 'start\n'; sleep 2" (TimeSpan.FromSeconds 2.0) (Some(TimeSpan.FromMilliseconds 100.0)))
                Expect.equal stalled.Status ValidationLanes.NoProgressTimedOut "no-progress timeout"
            finally
                Feature163TestFixtures.deleteTempRoot root
        }
    ]
