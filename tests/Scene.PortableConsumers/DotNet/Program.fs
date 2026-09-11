open FS.GG.UI.Scene
open System.IO
open RetainedTraceReplay

let scene : RetainedScene =
    { RootId = "isolated-dotnet"
      Revision = 1
      Camera = { PanX = 0.5; PanY = 1.25; Zoom = 2.0 }
      Layers =
        [ { Id = "world"
            Visible = true
            Objects =
              [ { Id = "dot"
                  Selectable = true
                  AccessibleLabel = "dot"
                  Content = { Nodes = [ SceneNode.Circle({ X = 1.5; Y = 2.25 }, 0.5, { Red = 0uy; Green = 0uy; Blue = 0uy; Alpha = 255uy }) ] } } ] } ] }

[<EntryPoint>]
let main args =
    match SvgRetained.project scene with
    | SvgAdapterResult.Rendered projected when projected.RootId = scene.RootId ->
        let document = SvgDocument.ofRetainedScene { X = 0.0; Y = 0.0; Width = 20.0; Height = 20.0 } scene
        match document with
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
            if asset.Document.Id <> scene.RootId || extension.Capabilities.Length <> 2 then failwith "contract envelope drift"
        let independent = SvgAffine.transformPoint (SvgAffine.compose (SvgAffine.translate 10.0 20.0) (SvgAffine.rotateDegrees 90.0)) { X = 2.0; Y = 3.0 }
        if abs (independent.X - 7.0) > 1e-9 || abs (independent.Y - 22.0) > 1e-9 then failwith "affine composition drift"
        let corpus = File.ReadAllText args[0]
        match replay id id corpus with
        | Error divergence -> failwith divergence
        | Ok replayed when replayed.Count < 1 -> failwith "model trace corpus was empty"
        | Ok replayed ->
            File.WriteAllText(args[1], replayed.Canonical)
            match replay (fun action -> if action = "Select" then "ClearSelection" else action) id corpus with
            | Error divergence when divergence.StartsWith "TRACE-DIVERGENCE" -> ()
            | _ -> failwith "incorrect action mapping mutant survived the model corpus"
            match replay id (fun outcome -> if outcome = 1 then 0 else outcome) corpus with
            | Error divergence when divergence.StartsWith "TRACE-DIVERGENCE" ->
                printfn $"retained-trace-replay: runtime=dotnet transitions={replayed.Count} action-mapping-mutant=killed stale-acceptance-mutant=killed"
                0
            | _ -> failwith "stale acceptance mutant survived the model corpus"
    | _ -> 1
