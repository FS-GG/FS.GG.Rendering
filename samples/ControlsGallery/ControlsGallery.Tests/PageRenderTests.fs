module ControlsGallery.Tests.PageRenderTests

open Expecto
open FS.GG.UI.Controls
open ControlsGallery.Core
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = GalleryTheme.resolve FS.GG.UI.Themes.Default.Theming.Light GalleryTheme.indigo
let private size: FS.GG.UI.Scene.Size = { Width = 1024; Height = 768 }

/// US1 acceptance #1/#3: each page's `Build demoState` renders a non-empty tree.
[<Tests>]
let pageRenderTests =
    testList "PageRender" [
        for page in Pages.all ->
            test (sprintf "page %s renders a non-empty tree" page.Id) {
                let control = page.Build DemoState.seed
                let result = Control.renderTree theme size control
                Expect.isGreaterThan result.NodeCount 0 "non-empty node count"
                Expect.isNonEmpty result.Bounds "at least one laid-out control"
            }
    ]
