module Feature144FsiSemanticTests

open System.IO
open Expecto

let private repositoryRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then dir
        else
            match Directory.GetParent dir |> Option.ofObj with
            | Some parent -> find parent.FullName
            | None -> dir

    find __SOURCE_DIRECTORY__

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
