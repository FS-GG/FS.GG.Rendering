open FS.GG.UI.Scene
open Fable.Core
open RetainedTraceReplay

[<Import("readFileSync", "node:fs")>]
let readFileSync (path: string) (encoding: string) : string = jsNative

[<Import("writeFileSync", "node:fs")>]
let writeFileSync (path: string) (contents: string) : unit = jsNative

[<Emit("process.argv[2]")>]
let tracePath: string = jsNative

[<Emit("process.argv[3]")>]
let resultPath: string = jsNative

let fixture : RetainedScene =
    { RootId = "isolated-fable"
      Revision = 3
      Camera = { PanX = 10.5; PanY = -4.25; Zoom = 2.0 }
      Layers =
        [ { Id = "world"
            Visible = true
            Objects =
              [ { Id = "free-coordinate"
                  Selectable = true
                  AccessibleLabel = "free coordinate object"
                  Content = { Nodes = [ SceneNode.Rectangle((0.25, 1.5, 2.75, 3.5), { Red = 20uy; Green = 40uy; Blue = 60uy; Alpha = 255uy }) ] } } ] } ] }

match SvgRetained.project fixture with
| SvgAdapterResult.Rendered projected ->
    let point = SvgRetained.toScreenPoint projected.Camera { X = 2.25; Y = -1.5 }
    if point <> { X = 15.0; Y = -7.25 } then failwith "camera transform drift"
    match SvgDocument.ofRetainedScene { X = 0.0; Y = 0.0; Width = 20.0; Height = 20.0 } fixture with
    | Error issues -> failwith $"document adapter failed: {issues}"
    | Ok accepted when not (SvgDocument.validate 0 SvgDocument.defaultLimits accepted).IsEmpty -> failwith "document validation failed"
    | Ok accepted ->
        let asset =
            { AssetId = "neutral-grid"
              Version = "0.29.0-preview.1"
              Sha256 = String.replicate 64 "a"
              License = "MIT"
              Document = accepted }
        let extension =
            { ExtensionId = "svg-scene-export"
              Version = "0.29.0-preview.1"
              EntryPoint = "FS.GG.UI.Scene.SvgDocument"
              Capabilities = [ "document"; "affine" ]
              Support = SvgRuntimeSupport.ContractOnly "serialization arrives in SVG-SCENE-02.3" }
        if asset.Document.Id <> fixture.RootId || extension.Capabilities.Length <> 2 then failwith "contract envelope drift"
    let independent = SvgAffine.transformPoint (SvgAffine.compose (SvgAffine.translate 10.0 20.0) (SvgAffine.rotateDegrees 90.0)) { X = 2.0; Y = 3.0 }
    if abs (independent.X - 7.0) > 1e-9 || abs (independent.Y - 22.0) > 1e-9 then failwith "affine composition drift"
    let reflectedSkew = SvgAffine.transformPoint (SvgAffine.compose (SvgAffine.scale -1.0 2.0) (SvgAffine.skewXDegrees 45.0)) { X = 2.0; Y = 3.0 }
    if abs (reflectedSkew.X + 5.0) > 1e-9 || abs (reflectedSkew.Y - 6.0) > 1e-9 then failwith "skew/reflection composition drift"
    if SvgAffine.tryInverse (SvgAffine.scale 0.0 1.0) <> Error SvgAffineError.Singular then failwith "singular affine was accepted"
    if SvgAffine.tryInverse { SvgAffine.identity with A = System.Double.NaN } <> Error SvgAffineError.NonFinite then failwith "non-finite affine was accepted"
| result -> failwith $"unexpected projection result: {result}"

let corpus = readFileSync tracePath "utf8"
match replay id id corpus with
| Error divergence -> failwith divergence
| Ok replayed when replayed.Count < 1 -> failwith "model trace corpus was empty"
| Ok replayed ->
    writeFileSync resultPath replayed.Canonical
    match replay (fun action -> if action = "Select" then "ClearSelection" else action) id corpus with
    | Error divergence when divergence.StartsWith "TRACE-DIVERGENCE" -> ()
    | _ -> failwith "incorrect action mapping mutant survived the model corpus"
    match replay id (fun outcome -> if outcome = 1 then 0 else outcome) corpus with
    | Error divergence when divergence.StartsWith "TRACE-DIVERGENCE" ->
        printfn $"retained-trace-replay: runtime=fable-node transitions={replayed.Count} action-mapping-mutant=killed stale-acceptance-mutant=killed"
    | _ -> failwith "stale acceptance mutant survived the model corpus"
