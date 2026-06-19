module Feature169RuntimeDiagnosticMappingTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Diagnostics

let private context =
    RuntimeDiagnostics.context (Some "feature169-controls") None None [ "stream", "runtime" ]

[<Tests>]
let tests =
    testList "Feature169 Controls runtime diagnostic mapping" [
        test "Synthetic controls backend cost maps to informational backend-cost runtime diagnostic" {
            let control =
                Diagnostics.create
                    (Some "panel")
                    "panel"
                    OffscreenComposition
                    ControlDiagnosticSeverity.Info
                    "Offscreen composition allocated a separate layer."

            let runtime = Diagnostics.toRuntimeDiagnostic context control

            Expect.equal runtime.Source.PackageId (Some "FS.GG.UI.Controls") "package id"
            Expect.equal runtime.Source.SampleId (Some "panel") "control id preserved as sample/source scope"
            Expect.equal runtime.Severity (Some DiagnosticSeverity.Informational) "severity"
            Expect.equal runtime.Category (Some DiagnosticCategory.BackendCost) "category"
            Expect.stringContains runtime.Action.Value "performance-blocked" "action guidance"
        }

        test "Synthetic controls authoring error maps to readiness blocker" {
            let control = Diagnostics.missingRequired (Some "grid") "data-grid" "rows"
            let runtime = Diagnostics.toRuntimeDiagnostic context control

            Expect.equal runtime.Severity (Some DiagnosticSeverity.Error) "severity"
            Expect.equal runtime.Category (Some DiagnosticCategory.ReadinessBlocker) "category"
        }
    ]
