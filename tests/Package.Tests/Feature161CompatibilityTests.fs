module Feature161CompatibilityTests

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
    testList "Feature161 compatibility package" [
        test "compatibility ledger documents helper surface command surface and claim boundary" {
            let path = repo "specs/161-host-performance-lane-ledger/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) $"ledger exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "Feature161HostLaneReadiness" "Testing helper"
            Expect.stringContains text "compositor-performance --feature 161 --lane host-ledger" "host lane command"
            Expect.stringContains text "performance-not-accepted" "claim boundary"
        }

        test "package validation records command surface Testing helper and FSI evidence" {
            let path = repo "specs/161-host-performance-lane-ledger/readiness/package-validation.md"
            Expect.isTrue (File.Exists path) $"package validation exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "compositor-readiness --feature 161" "readiness command"
            Expect.stringContains text "Feature161HostLaneReadiness" "Testing helper"
            Expect.stringContains text "compositor-host-lane-authoring.fsx" "compositor FSI"
            Expect.stringContains text "feature161-host-lane-readiness-authoring.fsx" "helper FSI"
        }

        test "validation summary links ledger full validation compatibility package regression and unsupported-host evidence" {
            let path = repo "specs/161-host-performance-lane-ledger/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) $"validation summary exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "lane-ledger/summary.md" "lane ledger summary"
            Expect.stringContains text "lane-ledger/host-facts/" "host facts"
            Expect.stringContains text "full-validation/validation.md" "full validation"
            Expect.stringContains text "lane-ledger/unsupported/README.md" "unsupported"
            Expect.stringContains text "performance-not-accepted" "claim boundary"
        }

        test "surface evidence records Feature 161 additive public and harness surfaces" {
            [ "FS.GG.UI.Testing.txt", "Feature161HostLaneReadiness"
              "Rendering.Harness.Compositor.txt", "Feature 161"
              "Rendering.Harness.Perf.txt", "missing-display" ]
            |> List.iter (fun (name, required) ->
                let path = repo $"specs/161-host-performance-lane-ledger/readiness/fsi/{name}"
                Expect.isTrue (File.Exists path) $"surface evidence exists at {path}"
                Expect.stringContains (File.ReadAllText path) required $"surface includes {required}")
        }
    ]
