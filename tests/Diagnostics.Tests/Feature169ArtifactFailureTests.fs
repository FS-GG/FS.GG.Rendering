module Feature169ArtifactFailureTests

open System
open System.IO
open Expecto
open FS.GG.UI.Diagnostics

[<Tests>]
let tests =
    testList "Feature169 artifact failure" [
        test "Synthetic artifact write failure emits developer-action diagnostic" {
            let filePath = Path.Combine(Path.GetTempPath(), "feature169-artifact-root-" + Guid.NewGuid().ToString("N"))
            File.WriteAllText(filePath, "not a directory")

            try
                let summary =
                    RuntimeDiagnostics.writeArtifacts
                        filePath
                        (Some Feature169Fixtures.runId)
                        []
                        [ Feature169Fixtures.backendCostAt 1 ]

                Expect.equal summary.Status ReadinessDiagnosticStatus.ReviewRequired "write failure requires review"
                Expect.isGreaterThan summary.ArtifactWriteDiagnostics.Length 0 "write diagnostic captured"
                Expect.equal summary.ArtifactWriteDiagnostics.Head.Category (Some DiagnosticCategory.DeveloperAction) "developer-action category"
            finally
                if File.Exists filePath then
                    File.Delete filePath
        }
    ]
