open FS.GG.UI.Scene

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
let main _ =
    match SvgRetained.project scene with
    | SvgAdapterResult.Rendered projected when projected.RootId = scene.RootId -> 0
    | _ -> 1
