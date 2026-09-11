open FS.GG.UI.Scene

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
| result -> failwith $"unexpected projection result: {result}"
