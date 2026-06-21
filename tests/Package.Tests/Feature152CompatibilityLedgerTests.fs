module Feature152CompatibilityLedgerTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

[<Tests>]
let tests =
    testList "Feature152 compatibility ledger" [
        test "compatibility ledger records public proof-set and readiness helper effects" {
            let path = repo "specs/152-compositor-live-proof/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) "ledger exists"
            let text = File.ReadAllText path

            [ "CompositorProof"
              "CompositorReadiness"
              "full redraw"
              "Migration Guidance"
              "Synthetic simulations" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "validation summary records environment-limited status without performance overclaim" {
            let path = repo "specs/152-compositor-live-proof/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) "summary exists"
            let text = File.ReadAllText path

            [ "Status: `environment-limited`"
              "Performance claim: `environment-limited`"
              "zero accepted partial-redraw artifacts"
              "No compositor performance claim is accepted" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }
    ]
