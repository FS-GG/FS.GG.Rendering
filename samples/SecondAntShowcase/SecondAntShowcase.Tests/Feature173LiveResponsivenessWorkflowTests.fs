module SecondAntShowcase.Tests.Feature173LiveResponsivenessWorkflowTests

open Expecto
open SecondAntShowcase.Core

let private request: ResponsivenessWorkflow.RunRequest =
    { RunId = "resp-feature173"
      Scope = "second-antshowcase/all-interactive/light"
      Theme = "light"
      OutputRoot = "out"
      RequireLive = true
      ActionIds = [ "button-click"; "slider-rating" ] }

[<Tests>]
let tests =
    testList "Feature173 live responsiveness workflow" [
        test "init requests live-session checking" {
            let model, effects = ResponsivenessWorkflow.init request

            Expect.equal model.Status ResponsivenessWorkflow.NotStarted "initial status"
            Expect.equal effects [ ResponsivenessWorkflow.CheckLiveSession ] "live check requested"
        }

        test "live unavailable fails closed and requests artifact persistence" {
            let model, _ = ResponsivenessWorkflow.init request
            let model', effects = ResponsivenessWorkflow.update (ResponsivenessWorkflow.LiveSessionUnavailable "no-visible-surface") model

            Expect.equal model'.Status ResponsivenessWorkflow.EnvironmentLimited "environment-limited"
            Expect.contains model'.EnvironmentLimitations "no-visible-surface" "limitation captured"
            Expect.equal effects [ ResponsivenessWorkflow.PersistArtifacts ] "diagnostic artifacts requested"
        }

        test "artifact write failure fails the workflow" {
            let model, _ = ResponsivenessWorkflow.init request
            let model', _ = ResponsivenessWorkflow.update (ResponsivenessWorkflow.ArtifactWriteFailed "summary.json denied") model

            Expect.equal model'.Status ResponsivenessWorkflow.Failed "write failures fail closed"
            Expect.contains model'.Diagnostics "summary.json denied" "diagnostic retained"
        }

        test "interpreter maps edge failures into messages" {
            let model, _ = ResponsivenessWorkflow.init request
            let interpreter: ResponsivenessWorkflow.Interpreter =
                { CheckLiveSession = fun () -> Error "missing-boundary"
                  ExerciseActions = fun _ -> Ok []
                  PersistArtifacts = fun _ -> Ok [ "summary.json" ] }

            let msg = ResponsivenessWorkflow.interpret interpreter model ResponsivenessWorkflow.CheckLiveSession

            Expect.equal msg (ResponsivenessWorkflow.LiveSessionUnavailable "missing-boundary") "edge failure becomes a message"
        }
    ]
