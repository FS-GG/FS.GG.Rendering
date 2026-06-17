#r "nuget: FS.GG.UI.SkiaViewer, 0.1.9-preview.1"
#r "nuget: FS.GG.UI.KeyboardInput, 0.1.9-preview.1"

open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.KeyboardInput

let options =
    { ViewerOptions.Default with
        InitialSize = Some { Width = 640.0; Height = 480.0 }
        InitialPosition = Some(ViewerWindowPosition.Coordinates(20, 40)) }

let model, _ = Keyboard.init []
let down = KeyDown "A"
let up = KeyUp "A"
let keyboardModel : KeyboardModel = model

printfn "%A %A %A %A" options keyboardModel down up
