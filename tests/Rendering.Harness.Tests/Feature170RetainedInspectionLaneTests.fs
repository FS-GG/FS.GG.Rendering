module Feature170RetainedInspectionLaneTests

open System
open System.IO
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList
        "Feature170 retained inspection lane"
        [ test "retained-inspection lane is listed with focused command and artifact paths" {
              let root = Feature166TestFixtures.createTempRoot "feature170-retained-lane"

              try
                  let runRoot = Path.Combine(root, "run")
                  let lane =
                      ValidationLanes.defaultLaneDefinitions root runRoot
                      |> List.find (fun lane -> lane.Id = "retained-inspection")

                  let command = ValidationLanes.commandText lane.Command

                  Expect.equal lane.ReadinessRole ValidationLanes.Optional "retained lane is on-demand"
                  Expect.equal lane.EvidenceDirectory (Path.Combine(runRoot, "retained-inspection")) "lane evidence directory"
                  Expect.stringContains command "tests/Controls.Tests/Controls.Tests.fsproj" "controls command"
                  Expect.stringContains command "tests/Testing.Tests/Testing.Tests.fsproj" "testing command"
                  Expect.stringContains command "tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj" "harness command"
                  Expect.stringContains command "samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj" "antshowcase command"
                  Expect.stringContains command "--filter Feature170" "focused VSTest filters"
                  Expect.stringContains command "--filter-test-list Feature170" "focused AntShowcase Expecto filter"
              finally
                  Feature166TestFixtures.deleteTempRoot root
          }

          test "explicit retained-inspection selection is accepted and unknown lane fails clearly" {
              let root = Feature166TestFixtures.createTempRoot "feature170-retained-preflight"

              try
                  let out = Path.Combine(root, "out")
                  let request =
                      { ValidationLanes.defaultRunRequest out with
                          RequestedLaneIds = [ "retained-inspection" ]
                          RunId = Some "run" }

                  let lanes = ValidationLanes.defaultLaneDefinitions root (Path.Combine(out, "run"))

                  match ValidationLanes.validateRequest root lanes request with
                  | Ok plan ->
                      Expect.equal plan.SelectionMode ValidationLanes.ExplicitSelection "explicit selection"
                      Expect.equal (plan.SelectedLanes |> List.map _.Id) [ "retained-inspection" ] "retained lane selected"
                  | Error diagnostics -> failtestf "unexpected diagnostics: %A" diagnostics

                  let badRequest =
                      { request with
                          RequestedLaneIds = [ "missing-retained-inspection" ]
                          RunId = Some "bad-run" }

                  match ValidationLanes.validateRequest root lanes badRequest with
                  | Ok _ -> failtest "expected unknown lane diagnostic"
                  | Error diagnostics ->
                      Expect.exists diagnostics (fun d -> d.Code = "unknown-lane" && d.LaneIds |> List.contains "missing-retained-inspection") "unknown lane diagnostic names id"
              finally
                  Feature166TestFixtures.deleteTempRoot root
          }

          test "retained-inspection result statuses fail closed when selected for required readiness" {
              [ ValidationLanes.Failed
                ValidationLanes.TimedOut
                ValidationLanes.NoProgressTimedOut
                ValidationLanes.Canceled
                ValidationLanes.Skipped
                ValidationLanes.NotRun
                ValidationLanes.EnvironmentLimited
                ValidationLanes.InfrastructureError ]
              |> List.iter (fun status ->
                  let readiness =
                      [ Feature166TestFixtures.result "retained-inspection" ValidationLanes.Required status ]
                      |> ValidationLanes.computeOverallReadiness

                  Expect.notEqual readiness ValidationLanes.Ready (ValidationLanes.statusToken status))
          } ]
