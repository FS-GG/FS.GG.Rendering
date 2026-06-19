module Feature169AdapterDiagnosticMappingTests

open Expecto
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Diagnostics

[<Tests>]
let tests =
    testList "Feature169 Controls.Elmish adapter diagnostic mapping" [
        test "Synthetic adapter diagnostic maps to developer-action warning" {
            let context = RuntimeDiagnostics.context (Some "feature169-adapter") None None [ "stream", "runtime" ]
            let adapter = ControlsElmish.diagnostic "control-runtime" "StaleTarget" "Stale control target was ignored."

            let runtime = ControlsElmish.adapterDiagnosticToRuntimeDiagnostic context adapter

            Expect.equal runtime.Source.PackageId (Some "FS.GG.UI.Controls.Elmish") "package id"
            Expect.equal runtime.Source.Subsystem "control-runtime" "adapter source"
            Expect.equal runtime.Code (Some "StaleTarget") "code"
            Expect.equal runtime.Severity (Some DiagnosticSeverity.Warning) "severity"
            Expect.equal runtime.Category (Some DiagnosticCategory.DeveloperAction) "category"
        }
    ]
