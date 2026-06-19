module Feature169RuntimeDiagnosticsReadinessTests

open Expecto
open FS.GG.UI.Diagnostics
open FS.GG.UI.Testing

let private source =
    RuntimeDiagnostics.source (Some "Testing.Tests") "runtime-diagnostics" None None

let private context =
    RuntimeDiagnostics.context (Some "feature169-testing") None None []

let private diagnostic severity category =
    // SYNTHETIC: minimal helper diagnostics isolate the Testing wrapper from producer adapters.
    RuntimeDiagnostics.create
        source
        (Some "SyntheticRuntimeDiagnostic")
        (Some severity)
        (Some category)
        "Synthetic runtime diagnostic."
        (Some "No action required in this wrapper test.")
        context

[<Tests>]
let tests =
    testList "Feature169 runtime diagnostics readiness helper" [
        test "Synthetic accepted summary passes accepted requirement" {
            let summary = RuntimeDiagnostics.summarize None [] [] [ diagnostic DiagnosticSeverity.Informational DiagnosticCategory.BackendCost ]
            let result = RuntimeDiagnosticReadiness.validate { Summary = summary; RequiredStatus = None; RequireAccepted = true }

            Expect.isTrue result.Accepted "accepted"
            Expect.equal result.Status "accepted" "status token"
        }

        test "Synthetic blocked summary fails with visible diagnostics" {
            let summary = RuntimeDiagnostics.summarize None [] [] [ diagnostic DiagnosticSeverity.Error DiagnosticCategory.ReadinessBlocker ]
            let result = RuntimeDiagnosticReadiness.validate { Summary = summary; RequiredStatus = Some ReadinessDiagnosticStatus.Accepted; RequireAccepted = true }

            Expect.isFalse result.Accepted "not accepted"
            Expect.isTrue (result.Diagnostics |> List.exists (fun d -> d.Contains("blocker"))) "blocker diagnostic visible"
        }
    ]
