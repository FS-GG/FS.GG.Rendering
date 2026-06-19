module Feature169ArtifactTests

open System
open System.IO
open Expecto
open FS.GG.UI.Diagnostics

[<Tests>]
let tests =
    testList "Feature169 artifacts" [
        test "Synthetic diagnostics-summary JSON carries schema status counts and groups" {
            let summary = Feature169Fixtures.summarize Feature169Fixtures.mixedDiagnostics
            let json = RuntimeDiagnostics.renderJson summary

            Expect.stringContains json "\"schemaVersion\":\"runtime-diagnostics-v1\"" "schema token"
            Expect.stringContains json "\"status\":\"blocked\"" "status token"
            Expect.stringContains json "\"readiness-blocker\":1" "category count"
            Expect.stringContains json "\"occurrenceCount\":1" "group count"
        }

        test "Synthetic artifact writer overwrites stale prior blocker artifact" {
            let dir = Path.Combine(Path.GetTempPath(), "feature169-artifacts-" + Guid.NewGuid().ToString("N"))

            try
                Directory.CreateDirectory dir |> ignore
                File.WriteAllText(Path.Combine(dir, "diagnostics-summary.json"), "{\"status\":\"blocked\",\"stale\":true}")

                let summary =
                    RuntimeDiagnostics.writeArtifacts
                        dir
                        (Some Feature169Fixtures.runId)
                        []
                        [ Feature169Fixtures.backendCostAt 1 ]

                let json = File.ReadAllText(Path.Combine(dir, "diagnostics-summary.json"))
                Expect.equal summary.Status ReadinessDiagnosticStatus.Accepted "clean run is accepted"
                Expect.isFalse (json.Contains("stale")) "stale JSON was overwritten"
                Expect.stringContains json "\"status\":\"accepted\"" "new status written"
            finally
                if Directory.Exists dir then
                    Directory.Delete(dir, true)
        }
    ]
