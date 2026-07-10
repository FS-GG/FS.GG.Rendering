namespace FS.GG.UI.Scene

open System
open System.Security.Cryptography
open System.Text

module Colors =
    let rgba red green blue alpha =
        { Red = red
          Green = green
          Blue = blue
          Alpha = alpha }

    let rgb red green blue =
        rgba red green blue 255uy

    let black = rgba 0uy 0uy 0uy 255uy
    let white = rgba 255uy 255uy 255uy 255uy
    let transparent = rgba 0uy 0uy 0uy 0uy

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Paint =
    let fill color =
        { Fill = Some color
          Stroke = None
          Opacity = 1.0
          Antialias = true
          BlendMode = BlendMode.SrcOver
          Shader = None
          ColorFilter = NoColorFilter
          MaskFilter = NoMaskFilter
          ImageFilter = NoImageFilter
          PathEffect = NoPathEffect }

    let stroke color width =
        // `Fill` carries the paint colour for BOTH fills and strokes: the painter sets
        // `paint.Color` from `Fill` (defaulting a `None` to white) and selects stroke vs fill
        // style from `Stroke`. Keep `Fill = Some color` so a stroked shape actually renders in
        // its colour instead of white-on-white (previously `Fill = None` discarded the colour).
        { fill color with
            Stroke =
                Some
                    { Width = width
                      Cap = StrokeCap.Butt
                      Join = StrokeJoin.Miter
                      Miter = 4.0 } }

    let withOpacity opacity paint =
        { paint with Opacity = opacity }

    let withBlendMode blendMode paint =
        { paint with BlendMode = blendMode }

    let withAntialias antialias paint =
        { paint with Antialias = antialias }

    let private ensureStroke paint =
        paint.Stroke
        |> Option.defaultValue
            { Width = 1.0
              Cap = StrokeCap.Butt
              Join = StrokeJoin.Miter
              Miter = 4.0 }

    let withStrokeCap cap paint =
        { paint with Stroke = Some { ensureStroke paint with Cap = cap } }

    let withStrokeJoin join paint =
        { paint with Stroke = Some { ensureStroke paint with Join = join } }

    let withMiter miter paint =
        { paint with Stroke = Some { ensureStroke paint with Miter = miter } }

    let withShader shader paint = { paint with Shader = Some shader }
    let withColorFilter filter paint = { paint with ColorFilter = filter }
    let withMaskFilter filter paint = { paint with MaskFilter = filter }
    let withImageFilter filter paint = { paint with ImageFilter = filter }

    let withPathEffect effect paint =
        { paint with PathEffect = effect }

module Path =
    let create fillType commands =
        { Commands = commands
          FillType = fillType }

    let moveTo x y = MoveTo { X = x; Y = y }
    let lineTo x y = LineTo { X = x; Y = y }
    let quadTo control point = QuadTo(control, point)
    let cubicTo control1 control2 point = CubicTo(control1, control2, point)
    let close = Close

    let private commandPoints path =
        path.Commands
        |> List.collect (function
            | MoveTo p
            | LineTo p -> [ p ]
            | QuadTo(c, p) -> [ c; p ]
            | CubicTo(c1, c2, p) -> [ c1; c2; p ]
            | ArcTo(bounds, _, _) ->
                [ { X = bounds.X; Y = bounds.Y }
                  { X = bounds.X + bounds.Width; Y = bounds.Y + bounds.Height } ]
            | Close -> [])

    let bounds path =
        match commandPoints path with
        | [] -> None
        | pts ->
            let minX = pts |> List.map _.X |> List.min
            let minY = pts |> List.map _.Y |> List.min
            let maxX = pts |> List.map _.X |> List.max
            let maxY = pts |> List.map _.Y |> List.max

            Some
                { X = minX
                  Y = minY
                  Width = maxX - minX
                  Height = maxY - minY }

    let private distance (a: Point) (b: Point) =
        let dx = b.X - a.X
        let dy = b.Y - a.Y
        Math.Sqrt(dx * dx + dy * dy)

    let measure (path: PathSpec) =
        let folder (last: Point option, length: float) command =
            match command, last with
            | MoveTo p, _ -> Some p, length
            | LineTo p, Some previous -> Some p, length + distance previous p
            | QuadTo(_, p), Some previous -> Some p, length + distance previous p
            | CubicTo(_, _, p), Some previous -> Some p, length + distance previous p
            | ArcTo(bounds, _, sweep), _ ->
                let radius = (abs bounds.Width + abs bounds.Height) / 4.0
                last, length + (Math.PI * 2.0 * radius * abs sweep / 360.0)
            | Close, _ -> last, length
            | _, None -> last, length

        let _, length = path.Commands |> List.fold folder (None, 0.0)

        { Length = length
          IsClosed = path.Commands |> List.exists ((=) Close) }

    // R2/P6: the current point advances only for the vertex-bearing commands `measure` also uses to
    // accumulate length (MoveTo/LineTo and the curve endpoints); ArcTo/Close never move it there, so a
    // polyline of these points shares `measure`'s chord metric exactly. `segment` extracts along that
    // same polyline — it is honest about the flattening (disclosed in Scene.fsi), not a Skia arc-length
    // reparameterisation.
    let private polyline (path: PathSpec) : Point list =
        path.Commands
        |> List.choose (function
            | MoveTo p -> Some p
            | LineTo p -> Some p
            | QuadTo(_, p) -> Some p
            | CubicTo(_, _, p) -> Some p
            | ArcTo _
            | Close -> None)

    let private lerpPoint (a: Point) (b: Point) (t: float) : Point =
        { X = a.X + (b.X - a.X) * t
          Y = a.Y + (b.Y - a.Y) * t }

    let segment (startDistance: float) (endDistance: float) (path: PathSpec) =
        let points = polyline path

        if endDistance <= startDistance || List.length points < 2 then
            { path with Commands = [] }
        else
            // Walk the polyline once, accumulating chord length, emitting the vertices whose cumulative
            // distance falls inside [startDistance, endDistance] plus the two interpolated boundary points.
            let clampStart = max 0.0 startDistance

            let struct (extracted, _, _) =
                List.pairwise points
                |> List.fold
                    (fun (struct (acc, travelled, _)) (a, b) ->
                        let span = distance a b
                        let segEnd = travelled + span

                        let acc =
                            if span <= 0.0 then
                                acc
                            else
                                // Interpolate the entry/exit points where the window boundaries cut this chord.
                                let clip d = (d - travelled) / span

                                let acc =
                                    if travelled <= clampStart && clampStart < segEnd then
                                        lerpPoint a b (clip clampStart) :: acc
                                    else
                                        acc

                                let acc =
                                    if travelled < endDistance && endDistance <= segEnd then
                                        lerpPoint a b (clip endDistance) :: acc
                                    elif segEnd < endDistance && segEnd > clampStart then
                                        b :: acc
                                    else
                                        acc

                                acc

                        struct (acc, segEnd, ()))
                    (struct ([], 0.0, ()))

            match List.rev extracted with
            | [] -> { path with Commands = [] }
            | head :: tail ->
                { path with
                    Commands = MoveTo head :: (tail |> List.map LineTo) }

    // R2/P6: true boolean geometry (Intersect/Difference) needs a clipping kernel the Skia-free `Scene`
    // layer does not have, so `combine` no longer returns the same silent concatenation for all four
    // operations. `Union`/`Xor` ARE expressible as overlapping subpaths filled by the nonzero-winding
    // resp. even-odd rule, so they succeed with the matching fill type; `Intersect`/`Difference` fail
    // loud with a typed `PathCombineError` (the `Evidence.renderPng` honest-failure idiom).
    let combine operation (left: PathSpec) (right: PathSpec) : Result<PathSpec, PathCombineError> =
        let concatenated fillType =
            Result.Ok
                { FillType = fillType
                  Commands = left.Commands @ right.Commands }

        match operation with
        | Union -> concatenated Winding
        | Xor -> concatenated EvenOdd
        | Intersect
        | Difference ->
            Result.Error
                { Operation = operation
                  Message =
                    $"Path.combine %A{operation} needs boolean path clipping, which the Skia-free Scene layer does not implement; use Union/Xor (fill-rule composition) or perform the clip in the SkiaViewer layer." }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Scene =
    let empty = { Nodes = [ Empty ] }

    let group scenes =
        { Nodes = [ Group scenes ] }

    let rectangle bounds fill =
        { Nodes = [ Rectangle(bounds, fill) ] }

    let rectangleWithPaint bounds paint =
        { Nodes = [ PaintedRectangle(bounds, paint) ] }

    let filledRectangle (bounds: Rect) fill =
        { Nodes = [ Rectangle((bounds.X, bounds.Y, bounds.Width, bounds.Height), fill) ] }

    let circle center radius fill =
        { Nodes = [ Circle(center, radius, fill) ] }

    let filledEllipse bounds fill =
        { Nodes = [ FilledEllipse(bounds, fill) ] }

    let ellipse bounds paint =
        { Nodes = [ Ellipse(bounds, paint) ] }

    let line startPoint endPoint paint =
        { Nodes = [ Line(startPoint, endPoint, paint) ] }

    let path path paint =
        { Nodes = [ Path(path, paint) ] }

    let points points paint =
        { Nodes = [ Points(points, paint) ] }

    let vertices mode vertices paint =
        { Nodes = [ Vertices(mode, vertices, paint) ] }

    let arc bounds startAngle sweepAngle paint =
        { Nodes = [ Arc(bounds, startAngle, sweepAngle, paint) ] }

    let text position text color =
        { Nodes = [ Text(position, text, color) ] }

    let textAt (position: Point) text color =
        { Nodes = [ Text((position.X, position.Y), text, color) ] }

    let textRun run =
        { Nodes = [ TextRun run ] }

    // Feature 188 (US2): the shaped-text core moved to `Text.Shaping`; these stay as thin public
    // delegations so glyph runs / shaped text / fingerprints / measurement keep their public names and
    // byte-identical results.
    let buildGlyphRun (text: string) (font: FontSpec) : GlyphRunData = Text.Shaping.buildGlyphRun text font

    let buildFallbackShapedText (text: string) (font: FontSpec) : ShapedTextResult = Text.Shaping.buildFallbackShapedText text font

    let shapedTextFingerprint (result: ShapedTextResult) : string = Text.Shaping.shapedTextFingerprint result

    let measureShapedText (result: ShapedTextResult) : TextMetrics = Text.Shaping.measureShapedText result

    let glyphRunDataFromShapedText (result: ShapedTextResult) : GlyphRunData = Text.Shaping.glyphRunDataFromShapedText result

    let glyphRunFingerprint (data: GlyphRunData) : string = Text.Shaping.glyphRunFingerprint data

    let measureGlyphRun (data: GlyphRunData) : TextMetrics = Text.Shaping.measureGlyphRun data

    let glyphRun position (data: GlyphRunData) paint =
        { Nodes = [ GlyphRun { Data = data; Position = position; Paint = paint } ] }

    let glyphRunProof position text font paint =
        glyphRun position (buildGlyphRun text font) paint

    let measureText (text: string) (font: FontSpec) : TextMetrics = Text.Shaping.measureText text font

    // Feature 136/188 (R2/FR-002): the real-metrics measurer seam now lives in `Text.Shaping` (its
    // single owner). These stay as thin public delegations so `Scene.setRealTextMeasurer` /
    // `measureTextResolved` and the version-bucket accessors keep their public names and the
    // set/clear/measure lifecycle is byte-identical (`None` ⇒ pure `measureText` path).
    let setRealTextMeasurer (measurer: (string -> FontSpec -> TextMetrics) option) : unit = Text.Shaping.setRealTextMeasurer measurer

    let textMeasurementVersionBucket () : string = Text.Shaping.textMeasurementVersionBucket ()

    let setTextMeasurementVersionBucket (bucket: string) : unit = Text.Shaping.setTextMeasurementVersionBucket bucket

    let measureTextResolved (text: string) (font: FontSpec) : TextMetrics = Text.Shaping.measureTextResolved text font

    let image bounds source =
        { Nodes = [ Image(bounds, source) ] }

    let clipped clip scene =
        { Nodes = [ ClipNode(clip, scene) ] }

    let region region paint =
        { Nodes = [ RegionNode(region, paint) ] }

    let withColorSpace colorSpace scene =
        { Nodes = [ ColorSpaceNode(colorSpace, scene) ] }

    let withPerspective transform scene =
        { Nodes = [ PerspectiveNode(transform, scene) ] }

    let picture picture =
        { Nodes = [ PictureNode picture ] }

    let chart values =
        { Nodes = [ Chart values ] }

    let translate dx dy scene =
        { Nodes = [ Translate((dx, dy), scene) ] }

    let sizedText position text size color =
        { Nodes = [ SizedText(position, text, size, color) ] }

    let rec describe scene =
        let describeNode node =
            match node with
            | Empty -> [ EmptyElement ]
            | Group scenes -> GroupElement :: (scenes |> List.collect describe)
            | Rectangle _ -> [ RectangleElement ]
            | PaintedRectangle _ -> [ RectangleElement ]
            | Circle _ -> [ CircleElement ]
            | FilledEllipse _ -> [ EllipseElement ]
            | Ellipse _ -> [ EllipseElement ]
            | Line _ -> [ LineElement ]
            | Path _ -> [ PathElement ]
            | Points _ -> [ PointsElement ]
            | Vertices _ -> [ VerticesElement ]
            | Arc _ -> [ ArcElement ]
            | Text _ -> [ TextElement ]
            | TextRun _ -> [ TextRunElement ]
            | Image _ -> [ ImageElement ]
            | ClipNode(_, scene) -> ClipElement :: describe scene
            | RegionNode _ -> [ RegionElement ]
            | ColorSpaceNode(_, scene) -> ColorSpaceElement :: describe scene
            | PerspectiveNode(_, scene) -> PerspectiveElement :: describe scene
            | PictureNode picture -> PictureElement :: describe picture.Scene
            | Chart _ -> [ ChartElement ]
            | Translate(_, scene) -> TranslateElement :: describe scene
            | SizedText _ -> [ SizedTextElement ]
            | GlyphRun _ -> [ GlyphRunElement ]
            // Feature 120 (FR-007): transparent — describe the wrapped subtree, no marker element.
            | CachedSubtree boundary -> describe boundary.Scene

        scene.Nodes |> List.collect describeNode

    let rec diagnostics scene =
        let diagnostic severity message cause =
            { Severity = severity
              Stage = DiagnosticStage.FrameRender
              Message = message
              Cause = cause }

        let paintDiagnostics paint =
            [ match paint.PathEffect with
              | Dash([], _) -> diagnostic Warning "Dash path effect has no intervals." (Some "path-effect")
              | Discrete(segmentLength, _) when segmentLength <= 0.0 -> diagnostic Warning "Discrete path effect requires a positive segment length." (Some "path-effect")
              | Corner radius when radius < 0.0 -> diagnostic Warning "Corner path effect requires a non-negative radius." (Some "path-effect")
              | _ -> () ]

        let rec nodeDiagnostics node =
            match node with
            | Group scenes -> scenes |> List.collect diagnostics
            | PaintedRectangle(_, paint) -> paintDiagnostics paint
            | FilledEllipse(_, fill) ->
                if fill.Alpha = 0uy then
                    [ diagnostic Warning "Filled ellipse is transparent." (Some "fill") ]
                else
                    []
            | Circle(_, radius, fill) ->
                [ if radius <= 0.0 then
                      diagnostic Error "Circle radius must be positive." (Some "radius")
                  if fill.Alpha = 0uy then
                      diagnostic Warning "Circle fill is transparent." (Some "fill") ]
            | Ellipse(_, paint)
            | Line(_, _, paint)
            | Path(_, paint)
            | Points(_, paint)
            | Vertices(_, _, paint)
            | Arc(_, _, _, paint)
            | RegionNode(_, paint)
            | TextRun { Paint = paint } -> paintDiagnostics paint
            | GlyphRun run ->
                paintDiagnostics run.Paint
                @ (run.Data.FallbackDiagnostics
                   |> List.map (fun message -> diagnostic Warning message (Some "glyph-run-proof")))
            | Image(_, source) when String.IsNullOrWhiteSpace source -> [ diagnostic Error "Invalid image resource declaration." (Some "Image source path is empty.") ]
            // Existence of the source on disk is a host-level (render-edge) concern, not a pure
            // structural property of the Scene value. Probing IO.File.Exists here made diagnostics
            // non-deterministic (machine/CWD/filesystem-dependent) in the dependency root every
            // package pins; the viewer host validates the resource when it loads it. (#357)
            | ClipNode(_, scene)
            | ColorSpaceNode(_, scene)
            | PerspectiveNode(_, scene)
            | Translate(_, scene) -> diagnostics scene
            | PictureNode picture -> diagnostics picture.Scene
            // Feature 120 (FR-007): transparent — recurse into the wrapped subtree.
            | CachedSubtree boundary -> diagnostics boundary.Scene
            | _ -> []

        scene.Nodes |> List.collect nodeDiagnostics

    let renderReadbackEvidence (size: Size) scene =
        let capabilities =
            describe scene
            |> List.map string
            |> List.distinct
            |> List.sort

        let payload = String.concat "|" ([ string size.Width; string size.Height ] @ capabilities)
        let hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes payload)

        { Size = size
          CapabilityCount = capabilities.Length
          Capabilities = capabilities
          DeterministicHash = Convert.ToHexString(hashBytes).ToLowerInvariant() }

    let private classifyPlacement (outputSize: Size) bounds =
        let output =
            { X = 0.0
              Y = 0.0
              Width = float outputSize.Width
              Height = float outputSize.Height }

        let intersects =
            bounds.X < output.X + output.Width
            && bounds.X + bounds.Width > output.X
            && bounds.Y < output.Y + output.Height
            && bounds.Y + bounds.Height > output.Y

        let inside =
            bounds.X >= output.X
            && bounds.Y >= output.Y
            && bounds.X + bounds.Width <= output.X + output.Width
            && bounds.Y + bounds.Height <= output.Y + output.Height

        if inside then FullyInside
        elif intersects then PartiallyOutOfBounds
        else FullyOutOfBounds

    let circleEvidence (outputSize: Size) (center: Point) radius fill : CircleShapeEvidence =
        let bounds =
            { X = center.X - radius
              Y = center.Y - radius
              Width = radius * 2.0
              Height = radius * 2.0 }

        { Center = center
          Radius = radius
          Bounds = bounds
          Fill = fill
          Placement = classifyPlacement outputSize bounds }

    let ellipseEvidence (outputSize: Size) (bounds: Rect) fill : EllipseShapeEvidence =
        { Bounds = bounds
          Fill = fill
          Placement = classifyPlacement outputSize bounds }
