module Feature169ReadinessTests

open System
open Expecto
open FS.GG.UI.Diagnostics

[<Tests>]
let tests =
    testList "Feature169 readiness" [
        test "Synthetic non-blocking classified diagnostics are accepted" {
            let summary = Feature169Fixtures.summarize [ Feature169Fixtures.backendCostAt 1; Feature169Fixtures.renderingLimitation ]
            Expect.equal summary.Status ReadinessDiagnosticStatus.Accepted "classified non-blocking diagnostics are accepted"
        }

        test "Synthetic blocker and unclassified diagnostics fail closed" {
            let blocked = Feature169Fixtures.summarize [ Feature169Fixtures.blocker ]
            let review = Feature169Fixtures.summarize [ Feature169Fixtures.unclassified ]

            Expect.equal blocked.Status ReadinessDiagnosticStatus.Blocked "blocker status"
            Expect.equal review.Status ReadinessDiagnosticStatus.ReviewRequired "unclassified status"
            Expect.equal review.UnclassifiedCount 1 "unclassified count"
        }

        test "Synthetic valid exception changes blocker to accepted while remaining visible" {
            let exceptionRecord: DiagnosticException =
                { ExceptionId = "accepted-package-restore"
                  Scope = "PackageRestoreFailed"
                  Reason = "Synthetic test accepts the package restore blocker by code."
                  ExpiresOn = None
                  AcceptedBy = Some "feature169-test" }

            let summary =
                RuntimeDiagnostics.summarize
                    (Some Feature169Fixtures.runId)
                    [ exceptionRecord ]
                    []
                    [ Feature169Fixtures.blocker ]

            Expect.equal summary.Status ReadinessDiagnosticStatus.Accepted "valid exception accepts blocker"
            Expect.equal summary.ExceptionCount 1 "exception remains counted"
            Expect.equal summary.BlockerCount 0 "excepted blocker no longer blocks"
        }

        test "Synthetic expired or unmatched exceptions require review" {
            let expired: DiagnosticException =
                { ExceptionId = "expired"
                  Scope = "PackageRestoreFailed"
                  Reason = "Expired on purpose."
                  ExpiresOn = Some(DateOnly(2020, 1, 1))
                  AcceptedBy = Some "feature169-test" }

            let unmatched: DiagnosticException =
                { ExceptionId = "unmatched"
                  Scope = "does-not-match"
                  Reason = "Unmatched on purpose."
                  ExpiresOn = None
                  AcceptedBy = Some "feature169-test" }

            let expiredSummary = RuntimeDiagnostics.summarize None [ expired ] [] [ Feature169Fixtures.blocker ]
            let unmatchedSummary = RuntimeDiagnostics.summarize None [ unmatched ] [] [ Feature169Fixtures.backendCostAt 1 ]

            Expect.equal expiredSummary.Status ReadinessDiagnosticStatus.ReviewRequired "expired exception requires review"
            Expect.equal unmatchedSummary.Status ReadinessDiagnosticStatus.ReviewRequired "unmatched exception requires review"
        }

        test "Synthetic environment error becomes environment-limited when no blocker remains" {
            let summary = Feature169Fixtures.summarize [ Feature169Fixtures.environmentLimit ]
            Expect.equal summary.Status ReadinessDiagnosticStatus.EnvironmentLimitedStatus "environment-limited status"
        }

        test "F-DIAG-1: an Error-severity rendering limitation blocks, it does not fall through to accepted" {
            // A fatal framebuffer-wrap failure surfaces as Error/RenderingLimitation. Before the floor
            // it read as Accepted because the ladder only blocked on the ReadinessBlocker *category*.
            let summary = Feature169Fixtures.summarize [ Feature169Fixtures.renderingLimitationError ]
            Expect.equal summary.Status ReadinessDiagnosticStatus.Blocked "Error-severity rendering limitation blocks"
        }

        test "F-DIAG-1: an Error-severity backend-cost diagnostic blocks, independent of category" {
            let summary = Feature169Fixtures.summarize [ Feature169Fixtures.backendCostError ]
            Expect.equal summary.Status ReadinessDiagnosticStatus.Blocked "Error-severity backend cost blocks"
        }

        test "F-DIAG-1: a benign warning-severity rendering limitation is still accepted" {
            // Guard against over-blocking: the floor keys on Error severity only, so Info/Warning
            // classified non-blocking diagnostics remain Accepted (mirrors the non-blocking case above).
            let summary = Feature169Fixtures.summarize [ Feature169Fixtures.renderingLimitation; Feature169Fixtures.backendCostAt 1 ]
            Expect.equal summary.Status ReadinessDiagnosticStatus.Accepted "warning-severity limitation stays accepted"
        }

        test "F-DIAG-1: a valid exception clears the Error-severity floor to accepted" {
            // The floor honors accepted exceptions exactly like the ReadinessBlocker rung.
            let exceptionRecord: DiagnosticException =
                { ExceptionId = "accepted-framebuffer-limitation"
                  Scope = "Framebuffer"
                  Reason = "Synthetic test accepts the framebuffer rendering limitation by code."
                  ExpiresOn = None
                  AcceptedBy = Some "feature169-test" }

            let summary =
                RuntimeDiagnostics.summarize
                    (Some Feature169Fixtures.runId)
                    [ exceptionRecord ]
                    []
                    [ Feature169Fixtures.renderingLimitationError ]

            Expect.equal summary.Status ReadinessDiagnosticStatus.Accepted "excepted Error-severity limitation no longer blocks"
        }
    ]
