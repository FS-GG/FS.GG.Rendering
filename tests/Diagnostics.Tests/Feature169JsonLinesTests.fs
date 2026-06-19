module Feature169JsonLinesTests

open Expecto
open FS.GG.UI.Diagnostics

[<Tests>]
let tests =
    testList "Feature169 JSONL artifacts" [
        test "Synthetic JSONL preserves one runtime diagnostic per line" {
            let diagnostics = [ Feature169Fixtures.environmentWarning; Feature169Fixtures.backendCostAt 1 ]
            let jsonl = RuntimeDiagnostics.renderJsonLines diagnostics
            let lines = jsonl.Split('\n') |> Array.filter (System.String.IsNullOrWhiteSpace >> not)

            Expect.equal lines.Length 2 "two records"
            Expect.stringContains lines[0] "\"id\":\"diag-" "record id"
            Expect.stringContains lines[1] "\"category\":\"backend-cost\"" "category token"
        }
    ]
