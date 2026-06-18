module Feature151RenderAnywhereRegressionTests

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
    testList "Feature151RenderAnywhereRegression" [
        test "regression evidence classifies render-anywhere compatibility" {
            let text = File.ReadAllText(repo "specs/151-complete-p8-layout/readiness/regression-evidence.md")

            Expect.stringContains text "render-anywhere" "render-anywhere row"
            Expect.stringContains text "Feature151RenderAnywhereRegression" "evidence filter"
            Expect.stringContains text "accepted" "accepted classification"
        }
    ]
