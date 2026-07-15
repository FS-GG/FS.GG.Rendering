module Feature169HostDiagnosticMappingTests

open Expecto
open FS.GG.UI.Diagnostics
open FS.GG.UI.SkiaViewer.Host

let private context =
    RuntimeDiagnostics.context (Some "feature169-host") None None [ "stream", "stderr" ]

[<Tests>]
let tests =
    testList "Feature169 SkiaViewer host diagnostic mapping" [
        test "Synthetic damage scoped host diagnostic maps to backend-cost informational" {
            let host = Diagnostics.damageScopedDecision "offscreen-fallback" (Some "readback required")
            let runtime = Diagnostics.toRuntimeDiagnostic context host

            Expect.equal runtime.Source.PackageId (Some "FS.GG.UI.SkiaViewer") "package id"
            Expect.equal runtime.Code (Some "DamageScopedDecision") "stable code"
            Expect.equal runtime.Severity (Some FS.GG.UI.Diagnostics.DiagnosticSeverity.Informational) "severity"
            Expect.equal runtime.Category (Some DiagnosticCategory.BackendCost) "category"
        }

        test "Synthetic frame render failure maps to readiness blocker" {
            let host = Diagnostics.frameRenderFailed "draw command failed"
            let runtime = Diagnostics.toRuntimeDiagnostic context host

            Expect.equal runtime.Severity (Some FS.GG.UI.Diagnostics.DiagnosticSeverity.Error) "severity"
            Expect.equal runtime.Category (Some DiagnosticCategory.ReadinessBlocker) "category"
            Expect.stringContains runtime.Message "draw command failed" "cause preserved"
        }

        // Review F-DIAG-1: a FATAL framebuffer-wrap startup failure (the FBO-0 wrap that OpenGl.fs raises)
        // must map to a ReadinessBlocker, not a soft RenderingLimitation — the product cannot present at
        // all. Previously it fell through to RenderingLimitation/Error, which `summarize` accepted.
        test "Synthetic fatal framebuffer startup failure maps to readiness blocker (F-DIAG-1)" {
            let host = Diagnostics.startupFailed DiagnosticStage.Framebuffer "could not wrap the default framebuffer (FBO 0)"
            let runtime = Diagnostics.toRuntimeDiagnostic context host

            Expect.equal runtime.Severity (Some FS.GG.UI.Diagnostics.DiagnosticSeverity.Error) "fatal folds to Error at the boundary"
            Expect.equal runtime.Category (Some DiagnosticCategory.ReadinessBlocker) "fatal framebuffer fault blocks readiness"
        }

        // The soft path must survive: a Warning-level framebuffer note (e.g. the damage-scoped cost decision)
        // stays a non-blocking category so the escalation is keyed on severity, not on the stage alone.
        test "Synthetic damage-scoped framebuffer note stays non-blocking (F-DIAG-1 guard)" {
            let host = Diagnostics.damageScopedDecision "offscreen-fallback" (Some "readback required")
            let runtime = Diagnostics.toRuntimeDiagnostic context host

            Expect.equal runtime.Category (Some DiagnosticCategory.BackendCost) "damage decision stays a backend-cost note"
        }
    ]
