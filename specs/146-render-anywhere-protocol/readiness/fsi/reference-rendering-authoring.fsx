#r "nuget: FS.GG.UI.Scene"
#r "nuget: FS.GG.UI.SkiaViewer"

open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let package = SceneCodec.export (Scene.circle { X = 32.0; Y = 32.0 } 20.0 Colors.white)

let request =
    { PackageBytes = package.CanonicalBytes
      OutputDirectory = "specs/146-render-anywhere-protocol/readiness/reference"
      OutputSize = { Width = 64; Height = 64 }
      Resources = [] }

let model, effects = ReferenceRendering.init request
printfn "%A %d" model.Inspection effects.Length
