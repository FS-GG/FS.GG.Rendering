module ControlsDiagnosticsTests

open Expecto
open FS.GG.UI.Controls

let assertMessageContains (diagnostic: ControlDiagnostic) expected =
    Expect.stringContains diagnostic.Message expected $"diagnostic message names {expected}"

// ── #459 regression guard: this must keep COMPILING ───────────────────────────────────────────────
//
// `open FS.GG.UI.Controls` is above, and this is a perfectly ordinary `Result` railway — the kind
// every consumer writes. Before `ControlDiagnosticSeverity` carried `[<RequireQualifiedAccess>]`, its
// `Error` case shadowed `FSharp.Core`'s `Result.Error` for anyone who opened this namespace, and the
// bare `Error "..."` below failed with:
//
//     error FS0003: This value is not a function and cannot be applied.
//                   It has type 'ControlDiagnosticSeverity', which does not accept arguments.
//
// A message naming neither `Result`, nor the shadowing, nor the fix — a reporting agent lost a build
// cycle to it. So the guard is a COMPILE-TIME one, deliberately: delete the attribute and this file
// stops building. That is strictly stronger than any runtime assertion, because the failure it guards
// against is itself a compile failure in consumer code, and no runtime test can see one.
let private decodeRailway (s: string) : Result<int, string> =
    match System.Int32.TryParse s with
    | true, value -> Ok value
    | _ -> Error $"not a number: {s}"

[<Tests>]
let diagnosticsTests =
    testList "Controls boundary diagnostics" [
        // The compile-time guard above is the real assertion (#459); this pins its behaviour so the
        // railway cannot be quietly deleted as "unused" — which would take the guard with it.
        test "Result.Error is reachable through `open FS.GG.UI.Controls` (severity does not shadow it)" {
            Expect.equal (decodeRailway "7") (Ok 7) "a Result railway decodes through the open namespace"
            Expect.equal (decodeRailway "x") (Error "not a number: x") "...and its Error case is Result's, not ControlDiagnosticSeverity's"
            Expect.equal ControlDiagnosticSeverity.Error ControlDiagnosticSeverity.Error "the severity case is still reachable, qualified"
        }

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
                  Diagnostics.create None "capability:charts" StaleGeneratedReference ControlDiagnosticSeverity.Error "Generated capability `charts` is not active.", [ "charts" ]
                  Diagnostics.missingRequired (Some "data-grid") "data-grid" "rows", [ "rows" ]
                  Diagnostics.catalogOmission "data-grid" "evidence", [ "data-grid"; "evidence" ]
                  Diagnostics.create None "generated-profile:app" MissingRequiredAttribute ControlDiagnosticSeverity.Error "Generated profile `app` is missing `FS.GG.UI.Controls.Elmish`.", [ "app"; "FS.GG.UI.Controls.Elmish" ]
                  Diagnostics.create None "adapter-contract" MissingRequiredAttribute ControlDiagnosticSeverity.Error "Adapter contract `ControlsElmish.program` is missing.", [ "ControlsElmish.program" ]
                  Diagnostics.duplicateRuntimeDefinition "KeyboardModel" "src/Controls/KeyboardInput.fs", [ "KeyboardModel"; "src/Controls/KeyboardInput.fs" ]
                  Diagnostics.unsupportedEnvironment "rich-text" "drop-shadow", [ "drop-shadow" ]
                  Diagnostics.create None "migration" MissingRequiredAttribute ControlDiagnosticSeverity.Error "Migration guidance is missing the legacy Charts replacement path.", [ "legacy Charts"; "replacement path" ] ]

            diagnostics
            |> List.iter (fun (diagnostic, expectedTerms) ->
                expectedTerms
                |> List.iter (assertMessageContains diagnostic))
        }
    ]
