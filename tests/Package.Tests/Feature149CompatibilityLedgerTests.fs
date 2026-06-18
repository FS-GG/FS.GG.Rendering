module Feature149CompatibilityLedgerTests

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
    testList "Feature149 compatibility ledger" [
        test "compatibility ledger records public metrics, baselines, release notes, migration, and limitations" {
            let path = repo "specs/149-complete-compositor-p7/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) "ledger exists"
            let text = File.ReadAllText path

            [ "## Public Metrics and Diagnostics"
              "## Baseline References"
              "## Release Notes Draft"
              "## Migration Guidance"
              "## Limitations"
              "Feature149 harness routes" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "validation summary lists live proof, damage, placement, replay, snapshot, timing, and diagnostics" {
            let path = repo "specs/149-complete-compositor-p7/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) "summary exists"
            let text = File.ReadAllText path

            [ "Live proof"
              "Damage scissor"
              "Placement reuse"
              "Replay"
              "Snapshot"
              "Timing"
              "Public diagnostics"
              "environment-limited" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "corpus names target hosts, timing tiers, budgets, and synthetic disclosure" {
            let path = repo "specs/149-complete-compositor-p7/readiness/corpus.md"
            Expect.isTrue (File.Exists path) "corpus exists"
            let text = File.ReadAllText path

            [ "feature149-capable-host-candidate"
              "synthetic-non-preserving"
              "timing/replay"
              "32 MiB"
              "64 retained snapshot candidates"
              "proof/live-sentinel-damage-v1" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }
    ]
