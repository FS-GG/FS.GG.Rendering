module Feature151RenderAnywhereRegressionTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
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
