module Feature155CompatibilityTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

[<Tests>]
let tests =
    testList "Feature155 compatibility closeout" [
        test "validation summary records accepted correctness without performance overclaim" {
            let path = repo "specs/155-native-proof-capture/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) "summary exists"
            let text = File.ReadAllText path

            [ "Status: `accepted`"
              "Proof set: `accepted`"
              "Parity status: `accepted`"
              "Performance claim: `not-accepted`"
              "Selected attempts: `3/3`" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "compatibility ledger scopes Feature155 to current-host P7 closeout" {
            let path = repo "specs/155-native-proof-capture/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) "ledger exists"
            let text = File.ReadAllText path

            [ "Feature 155 reuses the Feature 154 proof-set"
              "No new public `.fsi` surface is required"
              "current-host P7 correctness closeout"
              "Performance remains a separate claim" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }
    ]
