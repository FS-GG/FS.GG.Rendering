open FS.GG.UI.Scene
open System.IO
open RetainedTraceReplay
open PortableDocumentFixture

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
    let serialized, exported = verifyRoundTrip "dotnet"
    File.WriteAllText(args[2], serialized)
    File.WriteAllText(args[3], exported)
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
                  Capabilities = [ "document"; "affine"; "serialization"; "svg-export" ]
                  Support = SvgRuntimeSupport.Supported }
            if asset.Document.Id <> scene.RootId || extension.Capabilities.Length <> 4 then failwith "contract envelope drift"
        let independent = SvgAffine.transformPoint (SvgAffine.compose (SvgAffine.translate 10.0 20.0) (SvgAffine.rotateDegrees 90.0)) { X = 2.0; Y = 3.0 }
        if abs (independent.X - 7.0) > 1e-9 || abs (independent.Y - 22.0) > 1e-9 then failwith "affine composition drift"
        let reflectedSkew = SvgAffine.transformPoint (SvgAffine.compose (SvgAffine.scale -1.0 2.0) (SvgAffine.skewXDegrees 45.0)) { X = 2.0; Y = 3.0 }
        if abs (reflectedSkew.X + 5.0) > 1e-9 || abs (reflectedSkew.Y - 6.0) > 1e-9 then failwith "skew/reflection composition drift"
        if SvgAffine.tryInverse (SvgAffine.scale 0.0 1.0) <> Error SvgAffineError.Singular then failwith "singular affine was accepted"
        if SvgAffine.tryInverse { SvgAffine.identity with A = System.Double.NaN } <> Error SvgAffineError.NonFinite then failwith "non-finite affine was accepted"
        let corpus = File.ReadAllText args[0]
        match replay id id corpus with
        | Error divergence -> failwith divergence
        | Ok replayed when replayed.Count < 1 -> failwith "model trace corpus was empty"
        | Ok replayed ->
            File.WriteAllText(args[1], replayed.Canonical)
            match DocumentTraceReplay.replay (File.ReadAllText args[4]) with
            | Error divergence -> failwith divergence
            | Ok documentReplayed ->
                File.WriteAllText(args[5], documentReplayed.Canonical)
                printfn $"document-trace-replay: runtime=dotnet transitions={documentReplayed.Count}"
            for name, mutant in
                [ "wrong-order", DocumentTraceReplay.ReplayMutation.WrongOrder
                  "stale-revision-acceptance", DocumentTraceReplay.ReplayMutation.AcceptStaleRevision
                  "invalid-reference-acceptance", DocumentTraceReplay.ReplayMutation.AcceptInvalidReference
                  "lost-capture", DocumentTraceReplay.ReplayMutation.PreserveReleasedCapture
                  "non-atomic-edit", DocumentTraceReplay.ReplayMutation.ApplyInvalidEdit ] do
                match DocumentTraceReplay.replayMutant mutant (File.ReadAllText args[4]) with
                | Error divergence when divergence.StartsWith "DOCUMENT-TRACE-DIVERGENCE" && divergence.Contains "trace=" && divergence.Contains "step=" ->
                    printfn $"document-mutant: runtime=dotnet name={name} killed-at={divergence}"
                | Error why -> failwith $"{name} mutant lacked first-divergence evidence: {why}"
                | Ok() -> failwith $"{name} mutant survived the document model corpus"
            match replay (fun action -> if action = "Select" then "ClearSelection" else action) id corpus with
            | Error divergence when divergence.StartsWith "TRACE-DIVERGENCE" -> ()
            | _ -> failwith "incorrect action mapping mutant survived the model corpus"
            match replay id (fun outcome -> if outcome = 1 then 0 else outcome) corpus with
            | Error divergence when divergence.StartsWith "TRACE-DIVERGENCE" ->
                printfn $"retained-trace-replay: runtime=dotnet transitions={replayed.Count} action-mapping-mutant=killed stale-acceptance-mutant=killed"
                0
            | _ -> failwith "stale acceptance mutant survived the model corpus"
    | _ -> 1
