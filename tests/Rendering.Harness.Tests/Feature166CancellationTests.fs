module Feature166CancellationTests

open System
open System.IO
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature166Cancellation" [
        test "Synthetic operator cancellation preserves completed lanes and marks unfinished lanes" {
            // SYNTHETIC: constructed MVU state models an operator interrupt without sending a real signal.
            let root = Feature166TestFixtures.createTempRoot "feature166-cancel"

            try
                let completed = Feature166TestFixtures.result "build" ValidationLanes.Required ValidationLanes.Passed
                let lane = Feature166TestFixtures.lane root "rendering-harness" "sleep 1"

                let model: ValidationLanes.Model =
                    { ValidationLanes.LaneDefinitions = [ lane ]
                      RunPlan = None
                      ActiveLaneId = Some "rendering-harness"
                      PendingLaneIds = [ "controls" ]
                      CompletedResults = [ completed ]
                      CanceledLaneIds = []
                      Summary = None
                      Diagnostics = [] }

                let canceled, effects = ValidationLanes.update (ValidationLanes.OperatorCanceled "operator canceled") model
                Expect.contains canceled.CompletedResults completed "completed result preserved"
                Expect.contains canceled.CanceledLaneIds "rendering-harness" "active canceled"
                Expect.contains canceled.CanceledLaneIds "controls" "pending canceled"
                Expect.contains effects (ValidationLanes.StopProcess "rendering-harness") "active stop"
                Expect.contains effects ValidationLanes.WriteSummary "summary"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "Synthetic readiness is blocked when required lanes are canceled skipped or not-run" {
            // SYNTHETIC: direct result records isolate readiness rules from child-process timing.
            [ ValidationLanes.Canceled
              ValidationLanes.Skipped
              ValidationLanes.NotRun ]
            |> List.iter (fun status ->
                let readiness =
                    [ Feature166TestFixtures.result "build" ValidationLanes.Required ValidationLanes.Passed
                      Feature166TestFixtures.result "controls" ValidationLanes.Required status ]
                    |> ValidationLanes.computeOverallReadiness

                Expect.notEqual readiness ValidationLanes.Ready (ValidationLanes.statusToken status))
        }
    ]
