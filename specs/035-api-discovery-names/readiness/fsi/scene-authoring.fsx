#r "nuget: FS.GG.UI.Scene, 0.1.9-preview.1"

open FS.GG.UI.Scene

let rect : Rect = { X = 0.0; Y = 0.0; Width = 120.0; Height = 48.0 }
let paint = Paint.Solid Colors.black
let stroke = Stroke.create 1.0 paint
let run = TextRun.create "Hello" paint
let kind = SceneElementKind.RectangleElement

printfn "%A %A %A %A %A" rect paint stroke run kind
