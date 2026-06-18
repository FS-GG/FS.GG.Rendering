module Feature151PackageValidationTests

open System
open System.IO
open Expecto

let rec private findRepositoryRoot directory =
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
    testList "Feature151PackageValidation" [
        test "package validation records full solution, surface, pack, and local feed evidence" {
            let path = repo "specs/151-complete-p8-layout/readiness/package-validation.md"
            Expect.isTrue (File.Exists path) "package validation exists"
            let text = File.ReadAllText path

            [ "dotnet test FS.GG.Rendering.slnx"
              "dotnet fsi scripts/refresh-surface-baselines.fsx"
              "dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local"
              "dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local"
              "Local feed path"
              "Status: `accepted`" ]
            |> List.iter (fun required -> Expect.stringContains text required required)
        }

        test "limitations disclose environment limits without accepting compositor overclaims" {
            let path = repo "specs/151-complete-p8-layout/readiness/limitations.md"
            Expect.isTrue (File.Exists path) "limitations exists"
            let text = File.ReadAllText path

            Expect.stringContains text "P7 live compositor partial-redraw proof remains environment-limited" "compositor limitation"
            Expect.stringContains text "Browser backend acceptance is not claimed" "browser non-claim"
            Expect.stringContains text "general constraint solver is not introduced" "solver non-claim"
        }
    ]
