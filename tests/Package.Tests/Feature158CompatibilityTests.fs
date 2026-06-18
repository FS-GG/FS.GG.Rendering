module Feature158CompatibilityTests

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
    testList "Feature158 compatibility package" [
        test "compatibility ledger documents no new package helper surface" {
            let path = repo "specs/158-separate-proof-timing/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) $"ledger exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "No new `FS.GG.UI.Testing` public helper surface" "Testing no helper"
            Expect.stringContains text "No new `FS.GG.UI.SkiaViewer` public helper surface" "SkiaViewer no helper"
            Expect.stringContains text "performance-not-accepted" "claim boundary"
        }

        test "package validation records command surface and FSI evidence" {
            let path = repo "specs/158-separate-proof-timing/readiness/package-validation.md"
            Expect.isTrue (File.Exists path) $"package validation exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "compositor-readiness --feature 158" "readiness command"
            Expect.stringContains text "No Testing or SkiaViewer package-visible helper surface" "no helper"
        }

        test "validation summary keeps proof probes separate from performance claim" {
            let path = repo "specs/158-separate-proof-timing/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) $"validation summary exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "proof-probes/README.md" "probe links"
            Expect.stringContains text "timing/excluded/" "excluded links"
            Expect.stringContains text "performance-not-accepted" "claim boundary"
        }
    ]
