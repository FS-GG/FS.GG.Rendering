module ControlsDiagnosticsTests

open Expecto
open FS.GG.UI.Controls

let assertMessageContains (diagnostic: ControlDiagnostic) expected =
    Expect.stringContains diagnostic.Message expected $"diagnostic message names {expected}"

[<Tests>]
let diagnosticsTests =
    testList "Controls boundary diagnostics" [
        test "boundary diagnostics name stale references and leaking dependencies" {
            let stale = Diagnostics.stalePackageReference "FS.GG.UI.Charts" "template/capabilities.yml"
            let leak = Diagnostics.dependencyLeak "FS.GG.UI.Controls" "src/SkiaViewer/SkiaViewer.fsproj"

            Expect.equal stale.Code StaleGeneratedReference "stale package references use stale-reference code"
            assertMessageContains stale "FS.GG.UI.Charts"
            assertMessageContains stale "template/capabilities.yml"
            assertMessageContains leak "FS.GG.UI.Controls"
            assertMessageContains leak "src/SkiaViewer/SkiaViewer.fsproj"
        }

        test "runtime catalog target and scope diagnostics name actionable subjects" {
            let catalog = Diagnostics.catalogOmission "data-grid" "evidence"
            let duplicate = Diagnostics.duplicateRuntimeDefinition "KeyboardInput" "src/Input/KeyboardInput.fs"
            let staleTarget = Diagnostics.staleEventTarget "save-button" "click"
            let unsupported = Diagnostics.unsupportedScopeExpansion "renderer-neutral controls" "template guidance"

            assertMessageContains catalog "data-grid"
            assertMessageContains catalog "evidence"
            assertMessageContains duplicate "KeyboardInput"
            assertMessageContains duplicate "src/Input/KeyboardInput.fs"
            assertMessageContains staleTarget "save-button"
            assertMessageContains staleTarget "click"
            assertMessageContains unsupported "renderer-neutral controls"
            assertMessageContains unsupported "template guidance"
        }

        test "validation diagnostics name packages capabilities controls profiles adapters runtime environment and migration gaps" {
            let diagnostics =
                [ Diagnostics.stalePackageReference "FS.GG.UI.Charts" "template/capabilities.yml", [ "FS.GG.UI.Charts"; "template/capabilities.yml" ]
                  Diagnostics.create None "capability:charts" StaleGeneratedReference Error "Generated capability `charts` is not active.", [ "charts" ]
                  Diagnostics.missingRequired (Some "data-grid") "data-grid" "rows", [ "rows" ]
                  Diagnostics.catalogOmission "data-grid" "evidence", [ "data-grid"; "evidence" ]
                  Diagnostics.create None "generated-profile:app" MissingRequiredAttribute Error "Generated profile `app` is missing `FS.GG.UI.Controls.Elmish`.", [ "app"; "FS.GG.UI.Controls.Elmish" ]
                  Diagnostics.create None "adapter-contract" MissingRequiredAttribute Error "Adapter contract `ControlsElmish.program` is missing.", [ "ControlsElmish.program" ]
                  Diagnostics.duplicateRuntimeDefinition "KeyboardModel" "src/Controls/KeyboardInput.fs", [ "KeyboardModel"; "src/Controls/KeyboardInput.fs" ]
                  Diagnostics.unsupportedEnvironment "rich-text" "drop-shadow", [ "drop-shadow" ]
                  Diagnostics.create None "migration" MissingRequiredAttribute Error "Migration guidance is missing the legacy Charts replacement path.", [ "legacy Charts"; "replacement path" ] ]

            diagnostics
            |> List.iter (fun (diagnostic, expectedTerms) ->
                expectedTerms
                |> List.iter (assertMessageContains diagnostic))
        }
    ]
