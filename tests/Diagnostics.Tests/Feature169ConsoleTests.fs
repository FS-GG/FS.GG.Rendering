module Feature169ConsoleTests

open Expecto
open FS.GG.UI.Diagnostics

[<Tests>]
let tests =
    testList "Feature169 console rendering" [
        test "Synthetic default console output stays compact while verbose keeps detail" {
            let diagnostics =
                Feature169Fixtures.mixedDiagnostics
                @ Feature169Fixtures.repeatedBackendCost 100

            let summary = Feature169Fixtures.summarize diagnostics
            let compact = RuntimeDiagnostics.renderConsole false 12 summary
            let verbose = RuntimeDiagnostics.renderConsole true 12 summary

            Expect.isLessThanOrEqual compact.Length 12 "compact default line budget"
            Expect.isGreaterThan verbose.Length compact.Length "verbose output has more detail"
            Expect.isTrue (compact |> List.exists (fun line -> line.Contains("Diagnostics: blocked"))) "status line"
            Expect.isTrue (verbose |> List.exists (fun line -> line.Contains("DamageScopedDecision"))) "verbose detail"
        }
    ]
