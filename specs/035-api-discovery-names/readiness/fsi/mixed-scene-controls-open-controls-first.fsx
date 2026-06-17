#r "nuget: FS.GG.UI.Controls, 0.1.9-preview.1"
#r "nuget: FS.GG.UI.Scene, 0.1.9-preview.1"

open FS.GG.UI.Controls
open FS.GG.UI.Scene

let rect : FS.GG.UI.Scene.Rect = { X = 0.0; Y = 0.0; Width = 100.0; Height = 30.0 }
let paint = FS.GG.UI.Scene.Paint.Solid FS.GG.UI.Scene.Colors.black
let stack = FS.GG.UI.Controls.Stack.children []
let text = FS.GG.UI.Controls.TextBlock.create []
let change = FS.GG.UI.Controls.TextBox.onChanged id

printfn "%A %A %A %A %A" rect paint stack text change
