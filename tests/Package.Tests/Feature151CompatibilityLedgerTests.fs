module Feature151CompatibilityLedgerTests

open System
open System.IO
open Expecto

let rec private findRepositoryRoot directory =
    if Directory.GetFiles(directory, "*.sln").Length > 0 || Directory.GetFiles(directory, "*.slnx").Length > 0 then
        directory
    else
        match Directory.GetParent directory |> Option.ofObj with
        | Some parent -> findRepositoryRoot parent.FullName
        | None -> failwithf "Could not locate repository root from %s" directory

let private root = findRepositoryRoot AppContext.BaseDirectory
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

[<Tests>]
let tests =
    testList "Feature151CompatibilityLedger" [
        test "compatibility ledger records public surface, behavior, diagnostics, migration, and limitations" {
            let path = repo "specs/151-complete-p8-layout/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) "ledger exists"
            let text = File.ReadAllText path

            [ "## Public Surface Changes"
              "No new public `.fsi` surface"
              "## Behavior Changes"
              "## Diagnostic Changes"
              "## Migration Guidance"
              "## Surface Baseline References"
              "## Limitations" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "validation summary links every required Feature151 evidence file" {
            let path = repo "specs/151-complete-p8-layout/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) "summary exists"
            let text = File.ReadAllText path

            [ "corpus-validation.md"
              "scrollviewer-validation.md"
              "reuse-validation.md"
              "full-incremental-parity.md"
              "regression-evidence.md"
              "compatibility-ledger.md"
              "package-validation.md"
              "limitations.md"
              "Status: `accepted`" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }
    ]
