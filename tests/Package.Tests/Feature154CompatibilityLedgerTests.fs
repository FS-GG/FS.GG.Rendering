module Feature154CompatibilityLedgerTests

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
    testList "Feature154 compatibility ledger" [
        test "compatibility ledger records proof readiness diagnostics and public drift decision" {
            let path = repo "specs/154-compositor-proof-acceptance/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) "ledger exists"
            let text = File.ReadAllText path

            [ "CompositorProof.AcceptedProofSet"
              "CompositorReadiness"
              "No new public `.fsi` surface is required"
              "Controls and Controls.Elmish compositor diagnostics"
              "Synthetic Disclosure" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "validation summary records environment-limited final status without overclaiming" {
            let path = repo "specs/154-compositor-proof-acceptance/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) "summary exists"
            let text = File.ReadAllText path

            [ "Status: `environment-limited`"
              "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              "Selected attempts: `0/3`"
              "zero accepted partial-redraw artifacts" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }
    ]
