#r "nuget: FS.GG.UI.Scene"

open FS.GG.UI.Scene

let scene =
    Scene.filledRectangle { X = 0.0; Y = 0.0; Width = 120.0; Height = 80.0 } (Colors.rgb 24uy 28uy 36uy)

let package = SceneCodec.export scene
let report = SceneCodec.inspect package.CanonicalBytes
let identity = SceneCodec.packageIdentity package.CanonicalBytes
let comparison = SceneCodec.compareScenes package.Scene package.Scene

printfn "%s %A %b" identity report.Status comparison.Equivalent
