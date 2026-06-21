module Feature148CompatibilityLedgerTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

[<Tests>]
let tests =
    testList "Feature148 compatibility ledger" [
        test "compatibility ledger records public metrics, baselines, release notes, migration, and limitations" {
            let path = repo "specs/148-compositor-live-integration/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) "ledger exists"
            let text = File.ReadAllText path

            [ "## Public Metrics and Diagnostics"
              "## Baseline References"
              "## Release Notes Draft"
              "## Migration Guidance"
              "## Limitations" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "validation summary lists live proof, damage, placement, replay, and snapshot tier statuses" {
            let path = repo "specs/148-compositor-live-integration/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) "summary exists"
            let text = File.ReadAllText path

            [ "Live proof"
              "Damage scissor"
              "Placement reuse"
              "Replay"
              "Snapshot"
              "environment-limited" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "corpus names target hosts, replay timing, exact budgets, and synthetic disclosure" {
            let path = repo "specs/148-compositor-live-integration/readiness/corpus.md"
            Expect.isTrue (File.Exists path) "corpus exists"
            let text = File.ReadAllText path

            [ "synthetic-non-preserving"
              "timing/replay"
              "32 MiB"
              "64 retained snapshot candidates"
              "proof/live-sentinel-damage-v1" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }
    ]
