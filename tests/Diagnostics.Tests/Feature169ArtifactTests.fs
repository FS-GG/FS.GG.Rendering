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

        test "F-DIAG-4: the .jsonl records carry the synthesized exception-problem record that drove the verdict" {
            let dir = Path.Combine(Path.GetTempPath(), "feature169-jsonl-synth-" + Guid.NewGuid().ToString("N"))

            let unmatched: DiagnosticException =
                { ExceptionId = "unmatched-exception"
                  Scope = "does-not-match-any-diagnostic"
                  Reason = "synthetic F-DIAG-4 fixture"
                  ExpiresOn = None
                  AcceptedBy = None }

            try
                let summary =
                    RuntimeDiagnostics.writeArtifacts dir (Some Feature169Fixtures.runId) [ unmatched ] [ Feature169Fixtures.backendCostAt 1 ]

                // The unmatched exception is what pushes the verdict to review-required, so its synthesized
                // record is verdict-bearing — it must be present in the per-record artifact, not only the summary.
                Expect.equal summary.Status ReadinessDiagnosticStatus.ReviewRequired "the unmatched exception drove review-required"

                let jsonl = File.ReadAllText(Path.Combine(dir, "diagnostics-records.jsonl"))
                Expect.stringContains jsonl "\"code\":\"UnmatchedDiagnosticException\"" "the synthesized exception-problem record is in the .jsonl"
                // Non-vacuous: the raw input diagnostic is still there too (the synthesized record is additive).
                Expect.stringContains jsonl "\"category\":\"backend-cost\"" "the raw input record is still present"
            finally
                if Directory.Exists dir then
                    Directory.Delete(dir, true)
        }

        test "F-DIAG-3: a persisted summary discloses another artifact's write failure (no returned-vs-persisted drift)" {
            let dir = Path.Combine(Path.GetTempPath(), "feature169-persist-drift-" + Guid.NewGuid().ToString("N"))

            try
                Directory.CreateDirectory dir |> ignore
                // Make the .jsonl write fail (its path is a directory) while the summary writes succeed.
                Directory.CreateDirectory(Path.Combine(dir, "diagnostics-records.jsonl")) |> ignore

                let summary =
                    RuntimeDiagnostics.writeArtifacts dir (Some Feature169Fixtures.runId) [] [ Feature169Fixtures.backendCostAt 1 ]

                // The clean diagnostics alone would be accepted; the .jsonl write failure is a
                // DeveloperAction that flips the returned verdict to review-required.
                Expect.equal summary.Status ReadinessDiagnosticStatus.ReviewRequired "the write failure requires review"

                // The persisted summary must agree with the returned one — it is written after the
                // .jsonl failure is known, so it discloses it (before the F-DIAG-3 reorder it read 'accepted').
                let json = File.ReadAllText(Path.Combine(dir, "diagnostics-summary.json"))
                Expect.stringContains json "\"status\":\"review-required\"" "the persisted .json discloses the .jsonl write failure"
                // The dedicated write-diagnostics disclosure must also agree with the returned summary,
                // not just the status: the persisted .json array and .md section carry the failure record.
                Expect.stringContains json "\"code\":\"ArtifactWriteFailed\"" "the persisted .json artifactWriteDiagnostics array is populated"
                let md = File.ReadAllText(Path.Combine(dir, "diagnostics-summary.md"))
                Expect.stringContains md "status: `review-required`" "the persisted .md discloses the .jsonl write failure"
                Expect.stringContains md "## Artifact Write Warnings" "the persisted .md carries the write-warnings section"
                Expect.isNonEmpty summary.ArtifactWriteDiagnostics "the returned summary lists the write diagnostic"
            finally
                if Directory.Exists dir then
                    Directory.Delete(dir, true)
        }
    ]
