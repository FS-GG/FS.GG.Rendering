module Feature151TextShapingRegressionTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
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
