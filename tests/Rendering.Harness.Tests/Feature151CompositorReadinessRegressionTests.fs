module Feature151CompositorReadinessRegressionTests

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
    testList "Feature151CompositorReadinessRegression" [
        test "compositor readiness remains environment-limited and non-blocking for P8" {
            let regression = File.ReadAllText(repo "specs/151-complete-p8-layout/readiness/regression-evidence.md")
            let limitations = File.ReadAllText(repo "specs/151-complete-p8-layout/readiness/limitations.md")

            Expect.stringContains regression "compositor readiness" "compositor row"
            Expect.stringContains regression "environment-limited" "environment-limited classification"
            Expect.stringContains regression "Feature151 does not accept live" "non-overclaim"
            Expect.stringContains limitations "does not turn environment-limited compositor evidence into an accepted performance" "limitation"
        }
    ]
