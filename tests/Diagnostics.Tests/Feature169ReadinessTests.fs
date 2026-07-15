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

        // Review F-DIAG-1: an Error in a category with no severity rule (RenderingLimitation / BackendCost)
        // must BLOCK — it used to fall through to Accepted. A Warning in the same category stays accepted, so
        // the escalation is keyed on severity, not on the category alone. This is the exact reachable path a
        // fatal framebuffer-wrap failure takes through `summarize`.
        test "Synthetic Error in a soft category blocks readiness (F-DIAG-1)" {
            let renderErr = Feature169Fixtures.summarize [ Feature169Fixtures.renderingLimitationError ]
            let backendErr = Feature169Fixtures.summarize [ Feature169Fixtures.backendCostError ]

            Expect.equal renderErr.Status ReadinessDiagnosticStatus.Blocked "RenderingLimitation/Error blocks"
            Expect.equal backendErr.Status ReadinessDiagnosticStatus.Blocked "BackendCost/Error blocks"

            // The Warning-severity sibling must remain accepted — proves the block keys on severity, not category.
            let renderWarn = Feature169Fixtures.summarize [ Feature169Fixtures.renderingLimitation ]
            Expect.equal renderWarn.Status ReadinessDiagnosticStatus.Accepted "RenderingLimitation/Warning stays accepted"
        }

        // Review F-DIAG-1: the block is honestly waivable through the same exception path as any other, so a
        // deliberately-accepted rendering error can still ship with a visible caveat rather than a hidden pass.
        test "Synthetic Error in a soft category is waivable by exception (F-DIAG-1)" {
            let waiver: DiagnosticException =
                { ExceptionId = "accepted-framebuffer-wrap"
                  Scope = "FramebufferWrapFailed"
                  Reason = "Synthetic test accepts the framebuffer limitation by code."
                  ExpiresOn = None
                  AcceptedBy = Some "feature169-test" }

            let summary =
                RuntimeDiagnostics.summarize
                    (Some Feature169Fixtures.runId)
                    [ waiver ]
                    []
                    [ Feature169Fixtures.renderingLimitationError ]

            Expect.equal summary.Status ReadinessDiagnosticStatus.Accepted "valid exception accepts the rendering error"
            Expect.equal summary.ExceptionCount 1 "exception remains counted"
        }
    ]
