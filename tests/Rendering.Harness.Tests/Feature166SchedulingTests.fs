module Feature166SchedulingTests

open System
open System.IO
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature166Scheduling" [
        test "unsafe parallel requests name lanes sharing concurrency group or output scope" {
            let root = Feature166TestFixtures.createTempRoot "feature166-schedule"

            try
                let out = Path.Combine(root, "out")
                let runRoot = Path.Combine(out, "run")

                let lanes =
                    [ Feature166TestFixtures.laneWith
                          root
                          "a"
                          ValidationLanes.Required
                          "printf a"
                          (TimeSpan.FromSeconds 1.0)
                          None
                          (Some "shared")
                          (Some "shared-output")
                      Feature166TestFixtures.laneWith
                          root
                          "b"
                          ValidationLanes.Required
                          "printf b"
                          (TimeSpan.FromSeconds 1.0)
                          None
                          (Some "shared")
                          (Some "shared-output") ]
                    |> List.map (fun lane ->
                        let dir = Path.Combine(runRoot, lane.Id)
                        { lane with
                            EvidenceDirectory = dir
                            LogPath = Path.Combine(dir, "log.txt")
                            ResultPath = Path.Combine(dir, "result.json")
                            DiagnosticsPath = Path.Combine(dir, "diagnostics.md")
                            OutputRoot = Path.Combine(dir, "out") })

                let request =
                    { ValidationLanes.defaultRunRequest out with
                        RequestedLaneIds = [ "a"; "b" ]
                        RunId = Some "run"
                        AllowParallel = true }

                match ValidationLanes.validateRequest root lanes request with
                | Ok _ -> failtest "expected unsafe schedule"
                | Error diagnostics ->
                    Expect.exists diagnostics (fun d -> d.Code = "unsafe-schedule" && d.LaneIds = [ "a"; "b" ]) "conflict"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "sequential schedule accepts lanes that share generated output metadata" {
            let root = Feature166TestFixtures.createTempRoot "feature166-schedule-sequential"

            try
                let out = Path.Combine(root, "out")
                let runRoot = Path.Combine(out, "run")

                let lanes =
                    [ Feature166TestFixtures.laneWith root "a" ValidationLanes.Required "printf a" (TimeSpan.FromSeconds 1.0) None (Some "shared") (Some "shared-output")
                      Feature166TestFixtures.laneWith root "b" ValidationLanes.Required "printf b" (TimeSpan.FromSeconds 1.0) None (Some "shared") (Some "shared-output") ]
                    |> List.map (fun lane ->
                        let dir = Path.Combine(runRoot, lane.Id)
                        { lane with
                            EvidenceDirectory = dir
                            LogPath = Path.Combine(dir, "log.txt")
                            ResultPath = Path.Combine(dir, "result.json")
                            DiagnosticsPath = Path.Combine(dir, "diagnostics.md")
                            OutputRoot = Path.Combine(dir, "out") })

                let request =
                    { ValidationLanes.defaultRunRequest out with
                        RequestedLaneIds = [ "a"; "b" ]
                        RunId = Some "run"
                        AllowParallel = false }

                match ValidationLanes.validateRequest root lanes request with
                | Ok plan -> Expect.equal (plan.SelectedLanes |> List.map _.Id) [ "a"; "b" ] "sequential selected"
                | Error diagnostics -> failtestf "unexpected diagnostics: %A" diagnostics
            finally
                Feature166TestFixtures.deleteTempRoot root
        }
    ]
