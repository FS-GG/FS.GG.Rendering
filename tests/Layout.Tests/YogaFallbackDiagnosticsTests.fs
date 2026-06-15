module YogaFallbackDiagnosticsTests

open System
open Expecto
open FS.GG.UI.Layout

// This test mutates a PROCESS-GLOBAL AppContext switch (`ForceYogaFailure`). Expecto runs tests in
// parallel by default, so without sequencing a concurrent test (e.g. the feature 097 incremental
// suite, which depends on the real Yoga path) would observe the forced failure and diverge. Run it
// in the sequenced (non-parallel) phase so the global mutation is isolated.
[<Tests>]
let yogaFallbackDiagnosticsTests =
    testSequenced
    <| testList "Yoga fallback diagnostics" [
        test "recoverable Yoga execution failure returns safe bounds and emits fallback diagnostic" {
            let root =
                { Defaults.layoutNode "root" with
                    Children = [ { Defaults.layoutNode "child" with Intent = { Defaults.layoutIntent with Size = { Width = Some 96.0; Height = Some 24.0 } } } ] }

            AppContext.SetSwitch("FS.GG.UI.Layout.ForceYogaFailure", true)

            try
                let result = Layout.evaluate (Defaults.availableSpace 320.0 180.0) root

                Expect.equal result.Bounds.Length 2 "pure fallback still returns root and child bounds"
                Expect.all result.Bounds (fun item -> item.Bounds.Width >= 0.0 && item.Bounds.Height >= 0.0) "fallback bounds are safe"
                Expect.exists
                    result.Diagnostics
                    (fun item ->
                        item.Code = FallbackBoundsApplied
                        && item.Severity = FS.GG.UI.Layout.DiagnosticSeverity.Warning
                        && item.Constraint = Some "yoga"
                        && item.FallbackApplied
                        && item.NodeId = Some "root"
                        && item.Message.Contains("pure fallback layout"))
                    "Yoga fallback diagnostic is observable through existing public fields"
            finally
                AppContext.SetSwitch("FS.GG.UI.Layout.ForceYogaFailure", false)
        }
    ]
