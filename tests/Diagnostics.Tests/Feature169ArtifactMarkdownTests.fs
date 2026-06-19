module Feature169ArtifactMarkdownTests

open Expecto
open FS.GG.UI.Diagnostics

[<Tests>]
let tests =
    testList "Feature169 Markdown artifacts" [
        test "Synthetic Markdown reviewer summary exposes status counts blockers and exceptions" {
            let exceptionRecord: DiagnosticException =
                { ExceptionId = "accepted-package-restore"
                  Scope = "PackageRestoreFailed"
                  Reason = "Synthetic accepted blocker."
                  ExpiresOn = None
                  AcceptedBy = Some "feature169-test" }

            let summary =
                RuntimeDiagnostics.summarize
                    (Some Feature169Fixtures.runId)
                    [ exceptionRecord ]
                    [ "diagnostics-records.jsonl" ]
                    [ Feature169Fixtures.blocker ]

            let markdown = RuntimeDiagnostics.renderMarkdown summary

            Expect.stringContains markdown "status: `accepted`" "accepted status visible"
            Expect.stringContains markdown "severity counts" "severity counts visible"
            Expect.stringContains markdown "category counts" "category counts visible"
            Expect.stringContains markdown "accepted-package-restore" "exception visible"
            Expect.stringContains markdown "diagnostics-records.jsonl" "artifact path visible"
        }
    ]
