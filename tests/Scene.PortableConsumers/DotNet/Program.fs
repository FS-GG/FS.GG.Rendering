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
