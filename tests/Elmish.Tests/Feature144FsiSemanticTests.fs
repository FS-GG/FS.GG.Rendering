module Feature144FsiSemanticTests

open System.IO
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.find __SOURCE_DIRECTORY__

[<Tests>]
let tests =
    testList "Feature144 Controls.Elmish public FSI contracts" [
        test "ControlsElmish exposes overlay host interpretation" {
            let contract = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Controls.Elmish", "ControlsElmish.fsi"))

            [ "val interpretOverlayEffect"
              "val interpretOverlayOutcome"
              "OverlayEffect" ]
            |> List.iter (fun token -> Expect.stringContains contract token $"contract contains {token}")
        }
    ]
