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
    ]
