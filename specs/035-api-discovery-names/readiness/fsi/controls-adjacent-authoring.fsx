#r "nuget: FS.GG.UI.Controls, 0.1.9-preview.1"
#r "nuget: FS.GG.UI.Controls.Elmish, 0.1.9-preview.1"

open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish

type Msg =
    | Changed of string

let text = FS.GG.UI.Controls.TextBlock.create []
let input = FS.GG.UI.Controls.TextBox.onChanged Changed
let grid = FS.GG.UI.Controls.DataGrid.create [] []
let adapter = ControlsElmish.diagnostic "sample" "ok" "ready"

printfn "%A %A %A %A" text input grid adapter
