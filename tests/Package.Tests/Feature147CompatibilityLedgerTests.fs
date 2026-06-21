module Feature147CompatibilityLedgerTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

[<Tests>]
let tests =
    testList "Feature147 compatibility ledger" [
        test "compatibility ledger records public metrics, baselines, release notes, and limitations" {
            let path = repo "specs/147-compositor-damage-redraw/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) "ledger exists"
            let text = File.ReadAllText path

            [ "## Public Metrics and Diagnostics"
              "## Baseline References"
              "## Release Notes Draft"
              "## Migration Guidance"
              "## Limitations" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "validation summary lists ready, limited, rejected, or skipped tier statuses" {
            let path = repo "specs/147-compositor-damage-redraw/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) "summary exists"
            let text = File.ReadAllText path
            Expect.stringContains text "Present proof" "present proof tier"
            Expect.stringContains text "limited" "limited status disclosed"
            Expect.stringContains text "skipped" "skipped status disclosed"
        }
    ]
