module SecondAntShowcase.Tests.VisualTestHelpers

open FS.GG.UI.Controls
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

let preferredSize = VisualConfig.preferredSize
let minimumSize = VisualConfig.minimumSize

let renderShell size mode pageId =
    let model = { Host.initModel with Mode = mode; CurrentPage = pageId }
    Control.renderTree (AntTheme.resolve mode) size (Shell.view size model)

let rec kinds (control: Control<'msg>): string list =
    control.Kind :: (control.Children |> List.collect kinds)

let renderPage page =
    Control.renderTree (AntTheme.resolve Light) preferredSize (page.View DemoState.seed)
