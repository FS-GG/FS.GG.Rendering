module Feature153CompatibilityLedgerTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

[<Tests>]
let tests =
    testList "Feature153 compatibility ledger" [
        test "compatibility ledger records proof interpreter public effects" {
            let path = repo "specs/153-compositor-proof-interpreter/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) "ledger exists"
            let text = File.ReadAllText path

            [ "CompositorProof.AcceptedProofSet"
              "GlHost.LiveProofHostFacts"
              "Viewer.liveProofInterpreterSupported"
              "CompositorReadiness"
              "Synthetic Disclosure" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "validation summary records environment-limited status without partial-redraw or performance overclaim" {
            let path = repo "specs/153-compositor-proof-interpreter/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) "summary exists"
            let text = File.ReadAllText path

            [ "Status: `environment-limited`"
              "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              "zero accepted partial-redraw artifacts"
              "No compositor performance claim is accepted" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }
    ]
