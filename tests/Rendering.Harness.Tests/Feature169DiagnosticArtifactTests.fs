module Feature169DiagnosticArtifactTests

open System
open System.IO
open Expecto
open FS.GG.UI.Diagnostics
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature169 validation lane diagnostic artifact consumption" [
        test "Synthetic lane runner consumes diagnostics-summary artifact as typed status" {
            let root = Feature166TestFixtures.createTempRoot "feature169-diagnostic-artifact"

            try
                let lane =
                    Feature166TestFixtures.laneWith
                        root
                        "diagnostic-artifact"
                        ValidationLanes.Required
                        "mkdir -p lanes/diagnostic-artifact/out && printf '{\"schemaVersion\":\"runtime-diagnostics-v1\",\"runId\":\"synthetic\",\"status\":\"blocked\",\"artifactPaths\":[\"diagnostics-summary.json\"]}' > lanes/diagnostic-artifact/out/diagnostics-summary.json"
                        (TimeSpan.FromSeconds 5.0)
                        None
                        (Some "diagnostics")
                        (Some "diagnostics")

                let result = ValidationLanes.runLane lane

                Expect.equal result.Status ValidationLanes.Failed "typed blocked status fails lane"
                Expect.isSome result.RuntimeDiagnostics "typed summary attached"
                Expect.isTrue (result.ResultArtifacts |> List.exists (fun path -> path.EndsWith("diagnostics-summary.json"))) "diagnostic artifact linked"
            finally
                Feature166TestFixtures.deleteTempRoot root
        }
    ]
