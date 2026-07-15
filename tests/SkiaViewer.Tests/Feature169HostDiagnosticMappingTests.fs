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

        test "F-DIAG-1: a fatal framebuffer startup failure maps to readiness blocker, not rendering limitation" {
            let host = Diagnostics.startupFailed DiagnosticStage.Framebuffer "SkiaSharp could not wrap the window's default framebuffer (FBO 0)."
            let runtime = Diagnostics.toRuntimeDiagnostic context host

            Expect.equal runtime.Severity (Some FS.GG.UI.Diagnostics.DiagnosticSeverity.Error) "fatal collapses to Error severity"
            Expect.equal runtime.Category (Some DiagnosticCategory.ReadinessBlocker) "fatal framebuffer failure blocks readiness"
        }

        test "F-DIAG-1: a benign informational framebuffer diagnostic stays a rendering limitation" {
            // The present-mode announce (Info/Framebuffer, not a damage decision) must not be escalated.
            let host = Diagnostics.create DiagnosticSeverity.Info DiagnosticStage.Framebuffer "present-mode=DirectToSwapchain readback=false." None
            let runtime = Diagnostics.toRuntimeDiagnostic context host

            Expect.equal runtime.Category (Some DiagnosticCategory.RenderingLimitation) "informational framebuffer note is not a blocker"
        }
    ]
