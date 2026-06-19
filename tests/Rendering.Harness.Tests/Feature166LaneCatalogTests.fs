module Feature166LaneCatalogTests

open System
open System.IO
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature166LaneCatalog" [
        test "default catalog declares required lanes and optional aggregate" {
            let root = Feature166TestFixtures.createTempRoot "feature166-catalog"

            try
                let runRoot = Path.Combine(root, "run")
                let lanes = ValidationLanes.defaultLaneDefinitions root runRoot
                let ids = lanes |> List.map _.Id

                [ "build"
                  "library-tests"
                  "package-proof"
                  "controls"
                  "rendering-harness"
                  "antshowcase-sample"
                  "aggregate-solution" ]
                |> List.iter (fun id -> Expect.contains ids id id)

                let requiredIds =
                    lanes
                    |> List.filter (fun lane -> lane.ReadinessRole = ValidationLanes.Required)
                    |> List.map _.Id

                Expect.isFalse (requiredIds |> List.contains "aggregate-solution") "aggregate is not required"
                Expect.equal (lanes |> List.find (fun lane -> lane.Id = "aggregate-solution")).ReadinessRole ValidationLanes.Optional "aggregate role"
                Expect.isTrue (lanes |> List.forall (fun lane -> lane.Timeout > TimeSpan.Zero)) "timeouts are explicit"
                Expect.isTrue (lanes |> List.forall (fun lane -> lane.ProgressInterval <= TimeSpan.FromSeconds 60.0)) "heartbeats are bounded"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "lane ids result ids and evidence paths are unique" {
            let root = Feature166TestFixtures.createTempRoot "feature166-catalog-unique"

            try
                let lanes = ValidationLanes.defaultLaneDefinitions root (Path.Combine(root, "run"))
                Expect.equal (lanes |> List.map _.Id |> Set.ofList |> Set.count) lanes.Length "lane ids"
                Expect.equal (lanes |> List.map _.ResultPath |> Set.ofList |> Set.count) lanes.Length "result paths"
                Expect.equal (lanes |> List.map _.EvidenceDirectory |> Set.ofList |> Set.count) lanes.Length "evidence paths"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }

        test "documented lanes declare concurrency group and output scope metadata" {
            let root = Feature166TestFixtures.createTempRoot "feature166-catalog-schedule"

            try
                let lanes = ValidationLanes.defaultLaneDefinitions root (Path.Combine(root, "run"))
                Expect.isTrue (lanes |> List.forall (fun lane -> lane.ConcurrencyGroup.IsSome)) "concurrency metadata"
                Expect.isTrue (lanes |> List.forall (fun lane -> lane.OutputScope.IsSome)) "output metadata"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }
    ]
