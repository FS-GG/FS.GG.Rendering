module PortableDocumentFixture

open FS.GG.UI.Scene

let private color red green blue alpha = { Red = red; Green = green; Blue = blue; Alpha = alpha }
let private point x y = { X = x; Y = y }
let private rect x y width height = { X = x; Y = y; Width = width; Height = height }

let private fillPaint =
    { Fill = Some(color 8uy 16uy 32uy 255uy)
      Stroke = None
      Opacity = 0.8
      Antialias = true
      BlendMode = BlendMode.SrcOver
      Shader = Some(Shader.LinearGradient(point 0.0 0.0, point 20.0 0.0, [ color 255uy 0uy 0uy 255uy; color 0uy 0uy 255uy 255uy ]))
      ColorFilter = ColorFilter.NoColorFilter
      MaskFilter = MaskFilter.NoMaskFilter
      ImageFilter = ImageFilter.NoImageFilter
      PathEffect = PathEffect.NoPathEffect }

let private strokePaint =
    { fillPaint with
        Fill = Some(color 4uy 120uy 40uy 255uy)
        Stroke = Some { Width = 2.0; Cap = StrokeCap.Round; Join = StrokeJoin.Bevel; Miter = 4.0 }
        Shader = Some(Shader.SolidColor(color 240uy 120uy 20uy 255uy))
        PathEffect = PathEffect.Dash([ 3.0; 2.0 ], 1.0) }

let private leaf id scene presentation =
    { Id = id
      SemanticId = Some("semantic:" + id)
      Visible = true
      Transform = SvgAffine.identity
      ClipId = None
      MaskId = None
      Presentation = presentation
      Content = SvgElementContent.SceneLeaf scene }

let document =
    let hole =
        { Commands =
            [ PathCommand.MoveTo(point 5.0 5.0); PathCommand.LineTo(point 35.0 5.0); PathCommand.LineTo(point 35.0 35.0); PathCommand.LineTo(point 5.0 35.0); PathCommand.Close
              PathCommand.MoveTo(point 15.0 15.0); PathCommand.LineTo(point 25.0 15.0); PathCommand.LineTo(point 25.0 25.0); PathCommand.LineTo(point 15.0 25.0); PathCommand.Close ]
          FillType = PathFillType.EvenOdd }
    let scene =
        let radialPaint =
            { fillPaint with
                Shader = Some(Shader.RadialGradient(point 92.0 58.0, 7.0, [ color 255uy 255uy 255uy 255uy; color 30uy 40uy 180uy 255uy ])) }
        { Nodes =
            [ SceneNode.PaintedRectangle(rect 2.0 2.0 36.0 36.0, strokePaint)
              SceneNode.Path(hole, fillPaint)
              SceneNode.Arc(rect 45.0 5.0 30.0 30.0, 0.0, 360.0, strokePaint)
              SceneNode.Path({ Commands = [ PathCommand.ArcTo(rect 80.0 5.0 20.0 20.0, 20.0, -300.0) ]; FillType = PathFillType.Winding }, strokePaint)
              SceneNode.TextRun
                  { Text = "portable"
                    Position = point 5.0 55.0
                    Font = { Family = Some "Noto Sans"; Size = 12.0; Weight = Some 400 }
                    Paint = fillPaint }
              SceneNode.Empty
              SceneNode.Group
                  [ { Nodes = [ SceneNode.Rectangle((75.0, 45.0, 8.0, 8.0), color 100uy 20uy 180uy 255uy) ] }
                    { Nodes = [ SceneNode.Circle(point 86.0 49.0, 4.0, color 20uy 160uy 80uy 255uy) ] } ]
              SceneNode.FilledEllipse(rect 88.0 52.0 8.0 12.0, color 30uy 60uy 200uy 255uy)
              SceneNode.Ellipse(rect 98.0 52.0 12.0 12.0, radialPaint)
              SceneNode.Line(point 75.0 68.0, point 110.0 68.0, strokePaint)
              SceneNode.Translate((2.0, 3.0), { Nodes = [ SceneNode.SizedText((75.0, 75.0), "all rows", 6.0, color 20uy 20uy 20uy 255uy) ] })
              SceneNode.ClipNode(Clip.RectClip(rect 70.0 40.0 45.0 38.0), { Nodes = [ SceneNode.Text((72.0, 44.0), "clip", color 0uy 0uy 0uy 255uy) ] }) ] }
    let symbolChild = leaf "symbol-child" { Nodes = [ SceneNode.Circle(point 5.0 5.0, 5.0, color 10uy 180uy 80uy 255uy) ] } None
    let maskChild = leaf "mask-child" { Nodes = [ SceneNode.Rectangle((0.0, 0.0, 1.0, 1.0), color 255uy 255uy 255uy 160uy) ] } None
    let gradient =
        { Geometry = SvgGradientGeometry.Linear(point 0.0 0.0, point 1.0 1.0)
          Units = SvgCoordinateUnits.ObjectBoundingBox
          Transform = SvgAffine.compose (SvgAffine.rotateDegrees 15.0) (SvgAffine.scale 0.8 1.2)
          Spread = SvgSpreadMethod.Reflect
          Stops =
            [ { Offset = 0.0; Color = color 255uy 0uy 0uy 255uy; StopOpacity = 1.0 }
              { Offset = 0.4; Color = color 0uy 255uy 0uy 255uy; StopOpacity = 0.7 }
              { Offset = 1.0; Color = color 0uy 0uy 255uy 255uy; StopOpacity = 1.0 } ]
          InheritFrom = None }
    let radialGradient =
        { Geometry = SvgGradientGeometry.Radial(point 0.5 0.5, 0.5, Some(point 0.35 0.35))
          Units = SvgCoordinateUnits.ObjectBoundingBox
          Transform = SvgAffine.skewXDegrees 10.0
          Spread = SvgSpreadMethod.Pad
          Stops =
            [ { Offset = 0.0; Color = color 255uy 255uy 255uy 255uy; StopOpacity = 1.0 }
              { Offset = 1.0; Color = color 20uy 40uy 160uy 255uy; StopOpacity = 1.0 } ]
          InheritFrom = None }
    let presentation =
        { FillSource = Some(SvgPaintSource.Definition "gradient")
          StrokeStyle = Some { Source = SvgPaintSource.Solid(color 10uy 10uy 10uy 255uy); Width = 1.5; Cap = StrokeCap.Square; Join = StrokeJoin.Miter; Miter = 4.0; Dash = [ 2.0; 1.0 ]; DashOffset = 0.5 }
          OverallOpacity = 0.9
          FillRule = PathFillType.EvenOdd }
    let instance =
        { leaf "symbol-instance" { Nodes = [] } None with
            Transform = SvgAffine.translate 70.0 45.0
            ClipId = Some "clip-nested"
            MaskId = Some "mask-alpha"
            Presentation = Some presentation
            Content = SvgElementContent.SymbolInstance("symbol", Some(rect 0.0 0.0 20.0 20.0)) }
    let luminanceElement =
        { leaf "luminance-element" { Nodes = [ SceneNode.Rectangle((102.0, 2.0, 12.0, 12.0), color 255uy 255uy 255uy 255uy) ] } None with
            MaskId = Some "mask-luminance"
            Presentation = Some { presentation with FillSource = Some(SvgPaintSource.Definition "radial-gradient"); StrokeStyle = None } }
    { Schema = SvgDocument.schema
      Id = "portable-document"
      ViewBox = rect 0.0 0.0 120.0 80.0
      Definitions =
        [ { Id = "gradient"; Content = SvgDefinitionContent.Gradient gradient }
          { Id = "radial-gradient"; Content = SvgDefinitionContent.Gradient radialGradient }
          { Id = "symbol"; Content = SvgDefinitionContent.Symbol(Some(rect 0.0 0.0 10.0 10.0), [ symbolChild ]) }
          { Id = "clip-base"; Content = SvgDefinitionContent.Clip(SvgCoordinateUnits.UserSpaceOnUse, [ SvgClipShape.Rectangle(rect 0.0 0.0 110.0 75.0) ]) }
          { Id = "clip-nested"; Content = SvgDefinitionContent.Clip(SvgCoordinateUnits.UserSpaceOnUse, [ SvgClipShape.Intersection [ "clip-base" ]; SvgClipShape.Path hole ]) }
          { Id = "mask-alpha"; Content = SvgDefinitionContent.Mask(SvgCoordinateUnits.UserSpaceOnUse, rect 0.0 0.0 120.0 80.0, SvgMaskKind.Alpha, [ maskChild ]) }
          { Id = "mask-luminance"; Content = SvgDefinitionContent.Mask(SvgCoordinateUnits.ObjectBoundingBox, rect 0.0 0.0 1.0 1.0, SvgMaskKind.Luminance, [ maskChild |> fun child -> { child with Id = "mask-luminance-child"; SemanticId = Some "semantic:mask-luminance-child" } ]) }
          { Id = "font"; Content = SvgDefinitionContent.Font { Family = "Noto Sans"; Source = "fonts/missing-noto.woff2"; Sha256 = String.replicate 64 "a"; License = "OFL-1.1" } } ]
      Children = [ leaf "gallery" scene None; instance; luminanceElement ] }

let verifyRoundTrip runtime =
    match SvgDocument.serialize document with
    | Error issues -> failwith $"{runtime} serialization failed: {issues}"
    | Ok serialized ->
        match SvgDocument.deserialize serialized with
        | Error issues -> failwith $"{runtime} deserialization failed: {issues}"
        | Ok restored when SvgDocument.serialize restored <> Ok serialized -> failwith $"{runtime} typed document canonical round trip drifted"
        | Ok _ ->
            match SvgDocument.exportSvg "portable-fixture" document with
            | Error issues -> failwith $"{runtime} SVG export failed: {issues}"
            | Ok svg when not (svg.Contains("<linearGradient") && svg.Contains("<symbol") && svg.Contains("<mask") && svg.Contains("<use")) -> failwith $"{runtime} SVG export omitted selected definitions"
            | Ok svg -> serialized, svg
