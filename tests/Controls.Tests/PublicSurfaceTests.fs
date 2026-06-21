module ControlsPublicSurfaceTests

open System.IO
open Expecto
open FS.GG.UI.DesignSystem
open FS.GG.TestSupport

let repositoryRoot = RepositoryRoot.value

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

        test "Feature141 retained renderer unification keeps public Controls surface stable" {
            let controlFsi = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Controls", "Control.fsi"))
            let typesFsi = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Controls", "Types.fsi"))

            Expect.isTrue (controlFsi.Contains("module internal ControlInternals")) "assembly-owner changes stay internal"
            Expect.isFalse (controlFsi.Contains("module RetainedRender")) "retained rendering is not promoted to public Control.fsi"
            Expect.isTrue (typesFsi.Contains("type ControlRenderResult<'msg>")) "public render result remains present"
            Expect.isTrue (typesFsi.Contains("Scene: Scene")) "ControlRenderResult.Scene remains public"
            Expect.isTrue (typesFsi.Contains("Bounds: (ControlId * Rect) list")) "ControlRenderResult.Bounds remains public"
            Expect.isTrue (typesFsi.Contains("Diagnostics: ControlDiagnostic list")) "ControlRenderResult.Diagnostics remains public"
            Expect.isTrue (typesFsi.Contains("EventBindings: ControlEventBinding<'msg> list")) "ControlRenderResult.EventBindings remains public"
            Expect.isTrue (typesFsi.Contains("BoundIds: Set<ControlId>")) "ControlRenderResult.BoundIds remains public"
            Expect.isTrue (typesFsi.Contains("NodeCount: int")) "ControlRenderResult.NodeCount remains public"
        }
    ]
