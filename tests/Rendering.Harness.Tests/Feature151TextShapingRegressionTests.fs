module Feature151TextShapingRegressionTests

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
    testList "Feature151TextShapingRegression" [
        test "text-shaping regression evidence is classified without claiming new text behavior" {
            let regression = File.ReadAllText(repo "specs/151-complete-p8-layout/readiness/regression-evidence.md")
            let limitations = File.ReadAllText(repo "specs/151-complete-p8-layout/readiness/limitations.md")

            Expect.stringContains regression "text-shaping" "text-shaping row"
            Expect.stringContains regression "no new text behavior claimed" "no overclaim row"
            Expect.stringContains limitations "Text shaping behavior is regression-classified only" "limitation disclosure"
        }
    ]
