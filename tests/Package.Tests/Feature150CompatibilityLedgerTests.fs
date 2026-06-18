module Feature150CompatibilityLedgerTests

open System
open System.IO
open Expecto

let rec private findRepositoryRoot (directory: string) =
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
    testList "Feature150 compatibility ledger" [
        test "compatibility ledger records public surface, behavior, migration, and limitations" {
            let path = repo "specs/150-intrinsic-layout-protocol/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) "ledger exists"
            let text = File.ReadAllText path

            [ "## Public Surface Changes"
              "## Behavior Changes"
              "## Diagnostic Changes"
              "## Migration Guidance"
              "## Limitations" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "validation summary links Feature150 evidence in one review path" {
            let path = repo "specs/150-intrinsic-layout-protocol/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) "summary exists"
            let text = File.ReadAllText path

            [ "scrollviewer-validation.md"
              "intrinsic-cache-validation.md"
              "full-incremental-parity.md"
              "compatibility-ledger.md"
              "Current Status" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }
    ]
