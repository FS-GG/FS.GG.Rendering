module Feature150ScrollFixtures

open FS.GG.UI.Controls
open FS.GG.UI.Scene
open FS.GG.UI.Themes.Default

type Msg = Noop

let theme = Theme.light

let viewport : Size = { Width = 240; Height = 120 }

let rows count =
    [ for index in 1..count -> TextBlock.create [ TextBlock.text (sprintf "row %02d" index) ] ]

let scrollViewer id children =
    let content = Stack.create [ Stack.children children ]
    Control.create "scroll-viewer" [ Attr.children [ content ] ] |> Control.withKey id

let render control =
    Control.renderTree theme viewport control

