module Feature169ClassificationTests

open Expecto
open FS.GG.UI.Diagnostics

[<Tests>]
let tests =
    testList "Feature169 classification" [
        test "Synthetic mixed fixture groups category severity blockers and action guidance" {
            let summary = Feature169Fixtures.summarize Feature169Fixtures.mixedDiagnostics
            let severity = summary.CountsBySeverity |> Map.ofList
            let category = summary.CountsByCategory |> Map.ofList

            Expect.equal summary.Status ReadinessDiagnosticStatus.Blocked "blocker drives blocked status"
            Expect.equal severity[DiagnosticSeverity.Informational] 1 "informational count"
            Expect.equal severity[DiagnosticSeverity.Warning] 3 "warning count"
            Expect.equal severity[DiagnosticSeverity.Error] 1 "error count"
            Expect.equal category[DiagnosticCategory.Environment] 1 "environment count"
            Expect.equal category[DiagnosticCategory.BackendCost] 1 "backend-cost count"
            Expect.equal category[DiagnosticCategory.RenderingLimitation] 1 "rendering limitation count"
            Expect.equal category[DiagnosticCategory.DeveloperAction] 1 "developer action count"
            Expect.equal category[DiagnosticCategory.ReadinessBlocker] 1 "readiness blocker count"

            let rendered = RuntimeDiagnostics.renderConsole false 12 summary |> String.concat "\n"
            Expect.stringContains rendered "Diagnostics: blocked" "stable status label"
            Expect.stringContains rendered "readiness-blocker/error" "blocker group visible"
            Expect.isTrue (summary.Groups |> List.exists (fun g -> g.Action.IsSome)) "action guidance is retained"
        }

        test "Synthetic stream origin stays in first occurrence context" {
            let summary = Feature169Fixtures.summarize [ Feature169Fixtures.environmentWarning ]
            let first = summary.Groups |> List.head

            Expect.contains first.FirstOccurrence.Details ("stream", "stderr") "stream origin is preserved"
            Expect.equal first.Source.Subsystem "stdout" "source subsystem remains stable"
        }
    ]
