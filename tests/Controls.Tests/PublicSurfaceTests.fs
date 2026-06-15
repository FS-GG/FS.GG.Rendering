module ControlsPublicSurfaceTests

open System.IO
open Expecto
open FS.GG.UI.DesignSystem

let repositoryRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then
            dir
        else
            (match Directory.GetParent dir |> Option.ofObj with Some p -> find p.FullName | None -> dir)
    find __SOURCE_DIRECTORY__

[<Tests>]
let publicSurfaceTests =
    testList "Controls public surface files" [
        test "all required signature files exist" {
            // Feature 125: Theme.fsi (and DesignTokens/Style/Theming) relocated out of Controls into the
            // FS.GG.UI.DesignSystem / FS.GG.UI.Themes.Default packages; this list verifies the remaining
            // Controls .fsi surface. The relocation itself is proven by the surface-baseline drift gate.
            [ "Types.fsi"; "Control.fsi"; "Attributes.fsi"; "Accessibility.fsi"; "Diagnostics.fsi"; "Catalog.fsi"; "TextInput.fsi"; "Collections.fsi"; "Charts.fsi"; "CustomControl.fsi" ]
            |> List.iter (fun file ->
                let path = Path.Combine(repositoryRoot, "src", "Controls", file)
                Expect.isTrue (File.Exists path) $"{file} exists")
        }

        test "additive typed front-door signature files exist" {
            [ "Widget.fsi"; "Widgets/Primitives.fsi"; "Widgets/TextBoxWidget.fsi"; "Widgets/DataGridWidget.fsi" ]
            |> List.iter (fun file ->
                let path = Path.Combine(repositoryRoot, "src", "Controls", file.Replace("/", string Path.DirectorySeparatorChar))
                Expect.isTrue (File.Exists path) $"{file} exists")
        }
    ]
