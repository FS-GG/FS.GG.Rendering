module Feature156CompatibilityTests

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
    testList "Feature156 compatibility package" [
        test "timing summary preserves performance-not-accepted and remaining gates" {
            let path = repo "specs/156-same-profile-timing/readiness/timing/summary.md"
            Expect.isTrue (File.Exists path) "timing summary exists"
            let text = File.ReadAllText path

            [ "Policy id: `same-profile-live-threshold-v2`"
              "Accepted profile id: `probe-08a47c01`"
              "Shipped P7 performance claim: `performance-not-accepted`"
              "Feature 157 damage-scissored no-clear renderer"
              "Feature 160 validation throughput follow-up"
              "Feature 161 host performance lane ledger" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "compatibility ledger documents additive public helper surface" {
            let path = repo "specs/156-same-profile-timing/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) "ledger exists"
            let text = File.ReadAllText path

            [ "CompositorTimingAssertions"
              "FS.GG.UI.SkiaViewer.CompositorProof"
              "compositor-performance --feature 156"
              "Existing Feature 155 proof, parity, fallback, and correctness vocabulary remains authoritative" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "package validation records surface and FSI evidence" {
            let path = repo "specs/156-same-profile-timing/readiness/package-validation.md"
            Expect.isTrue (File.Exists path) "package validation exists"
            let text = File.ReadAllText path

            [ "SkiaViewer and Testing surface baselines"
              "Package FSI transcript coverage"
              "compositor-readiness --feature 156" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }
    ]
