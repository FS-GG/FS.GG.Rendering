module Feature151RetainedRenderingRegressionTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

[<Tests>]
let tests =
    testList "Feature151RetainedRenderingRegression" [
        test "retained rendering regression evidence is classified for P8 readiness" {
            let regression = File.ReadAllText(repo "specs/151-complete-p8-layout/readiness/regression-evidence.md")

            Expect.stringContains regression "retained rendering parity" "retained row"
            Expect.stringContains regression "Feature151RetainedRenderingRegression" "evidence filter"
            Expect.stringContains regression "accepted" "accepted classification"
        }

        test "viewer limitations do not claim new compositor partial-redraw acceptance" {
            let limitations = File.ReadAllText(repo "specs/151-complete-p8-layout/readiness/limitations.md")

            Expect.stringContains limitations "P7 live compositor partial-redraw proof remains environment-limited" "viewer limitation"
            Expect.stringContains limitations "not claimed by P8" "P8 non-claim"
        }
    ]
