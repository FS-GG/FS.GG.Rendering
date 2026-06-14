namespace FS.Skia.UI.SkiaViewer

#nowarn "44"
#nowarn "3261"
#nowarn "3391"

open System
open System.IO
open SkiaSharp
open FS.Skia.UI.Scene

/// Single shared scene painter (feature 063, FR-001/002). Both the interactive
/// `GlHost.drawScene` and the image-evidence `drawScreenshotScene` delegate to
/// `paintNode`, so the evidence and interactive renderers can never diverge again.
/// The `match` over `SceneNode` is **exhaustive — no wildcard** — so a new case is a
/// compile error until handled. Non-public (`internal`): no SkiaViewer surface change.
module internal SceneRenderer =

    // Feature 120 (US3): the active backend replay cache for the current present, set by the OpenGL host
    // before `drawScene`. `None` (or a disabled cache) ⇒ `CachedSubtree` paints its wrapped scene
    // directly (transparent), so the painter is byte-identical to the pre-120 direct walk.
    let mutable activeReplayCache: PictureReplayCache.Cache option = None

    let skColor color =
        SKColor(color.Red, color.Green, color.Blue, color.Alpha)

    let skPoint (point: Point) =
        SKPoint(float32 point.X, float32 point.Y)

    let skRect (bounds: Rect) =
        SKRect(float32 bounds.X, float32 bounds.Y, float32 (bounds.X + bounds.Width), float32 (bounds.Y + bounds.Height))

    let withOpacity opacity (color: Color) =
        let bounded = Math.Clamp(opacity, 0.0, 1.0)
        { color with Alpha = byte (Math.Round(float color.Alpha * bounded)) }

    let paintColor (paint: Paint) =
        paint.Fill
        |> Option.defaultValue Colors.white
        |> withOpacity paint.Opacity
        |> skColor

    let blendMode (mode: BlendMode) =
        match mode with
        | SrcOver -> SKBlendMode.SrcOver
        | Multiply -> SKBlendMode.Multiply
        | Screen -> SKBlendMode.Screen
        | Overlay -> SKBlendMode.Overlay
        | Darken -> SKBlendMode.Darken
        | Lighten -> SKBlendMode.Lighten
        | ColorDodge -> SKBlendMode.ColorDodge
        | ColorBurn -> SKBlendMode.ColorBurn
        | BlendMode.Difference -> SKBlendMode.Difference
        | Exclusion -> SKBlendMode.Exclusion

    let strokeCap cap =
        match cap with
        | Butt -> SKStrokeCap.Butt
        | StrokeCap.Round -> SKStrokeCap.Round
        | Square -> SKStrokeCap.Square

    let strokeJoin join =
        match join with
        | Miter -> SKStrokeJoin.Miter
        | RoundJoin -> SKStrokeJoin.Round
        | Bevel -> SKStrokeJoin.Bevel

    let vertexMode mode =
        match mode with
        | Triangles -> SKVertexMode.Triangles
        | TriangleStrip -> SKVertexMode.TriangleStrip
        | TriangleFan -> SKVertexMode.TriangleFan

    let configurePaint (scenePaint: Paint) (paint: SKPaint) =
        paint.Color <- paintColor scenePaint
        paint.IsAntialias <- scenePaint.Antialias
        paint.BlendMode <- blendMode scenePaint.BlendMode

        match scenePaint.Stroke with
        | Some stroke ->
            paint.Style <- SKPaintStyle.Stroke
            paint.StrokeWidth <- float32 stroke.Width
            paint.StrokeCap <- strokeCap stroke.Cap
            paint.StrokeJoin <- strokeJoin stroke.Join
            paint.StrokeMiter <- float32 stroke.Miter
        | None -> paint.Style <- SKPaintStyle.Fill

        match scenePaint.Shader with
        | Some(SolidColor color) ->
            paint.Shader <- SKShader.CreateColor(color |> withOpacity scenePaint.Opacity |> skColor)
        | Some(LinearGradient(startPoint, endPoint, colors)) when not colors.IsEmpty ->
            paint.Shader <-
                SKShader.CreateLinearGradient(
                    skPoint startPoint,
                    skPoint endPoint,
                    colors |> List.map (withOpacity scenePaint.Opacity >> skColor) |> List.toArray,
                    SKShaderTileMode.Clamp
                )
        | Some(RadialGradient(center, radius, colors)) when radius > 0.0 && not colors.IsEmpty ->
            paint.Shader <-
                SKShader.CreateRadialGradient(
                    skPoint center,
                    float32 radius,
                    colors |> List.map (withOpacity scenePaint.Opacity >> skColor) |> List.toArray,
                    SKShaderTileMode.Clamp
                )
        | Some(SweepGradient(center, colors)) when not colors.IsEmpty ->
            paint.Shader <-
                SKShader.CreateSweepGradient(
                    skPoint center,
                    colors |> List.map (withOpacity scenePaint.Opacity >> skColor) |> List.toArray
                )
        | _ -> ()

        match scenePaint.ColorFilter with
        | NoColorFilter -> ()
        | BlendColor(color, mode) ->
            paint.ColorFilter <- SKColorFilter.CreateBlendMode(color |> withOpacity scenePaint.Opacity |> skColor, blendMode mode)

        match scenePaint.MaskFilter with
        | NoMaskFilter -> ()
        | Blur sigma when sigma > 0.0 ->
            paint.MaskFilter <- SKMaskFilter.CreateBlur(SKBlurStyle.Normal, float32 sigma)
        | _ -> ()

        match scenePaint.ImageFilter with
        | NoImageFilter -> ()
        | DropShadow(dx, dy, blur, color) when blur >= 0.0 ->
            paint.ImageFilter <-
                SKImageFilter.CreateDropShadow(float32 dx, float32 dy, float32 blur, float32 blur, color |> withOpacity scenePaint.Opacity |> skColor)
        | _ -> ()

        match scenePaint.PathEffect with
        | NoPathEffect -> ()
        | Dash(intervals, phase) when not intervals.IsEmpty ->
            paint.PathEffect <- SKPathEffect.CreateDash(intervals |> List.map float32 |> List.toArray, float32 phase)
        | Discrete(segmentLength, deviation) when segmentLength > 0.0 ->
            paint.PathEffect <- SKPathEffect.CreateDiscrete(float32 segmentLength, float32 deviation)
        | Corner radius when radius >= 0.0 ->
            paint.PathEffect <- SKPathEffect.CreateCorner(float32 radius)
        | _ -> ()

    let toSkPath path =
        let skPath = new SKPath()
        skPath.FillType <-
            match path.FillType with
            | Winding -> SKPathFillType.Winding
            | EvenOdd -> SKPathFillType.EvenOdd

        for command in path.Commands do
            match command with
            | MoveTo p -> skPath.MoveTo(float32 p.X, float32 p.Y)
            | LineTo p -> skPath.LineTo(float32 p.X, float32 p.Y)
            | QuadTo(c, p) -> skPath.QuadTo(float32 c.X, float32 c.Y, float32 p.X, float32 p.Y)
            | CubicTo(c1, c2, p) -> skPath.CubicTo(float32 c1.X, float32 c1.Y, float32 c2.X, float32 c2.Y, float32 p.X, float32 p.Y)
            | ArcTo(bounds, startAngle, sweepAngle) ->
                skPath.ArcTo(skRect bounds, float32 startAngle, float32 sweepAngle, false)
            | Close -> skPath.Close()

        skPath

    let vectorGlyphPattern character =
        match Char.ToUpperInvariant character with
        | 'A' -> [ "01110"; "10001"; "10001"; "11111"; "10001"; "10001"; "10001" ]
        | 'B' -> [ "11110"; "10001"; "10001"; "11110"; "10001"; "10001"; "11110" ]
        | 'C' -> [ "01111"; "10000"; "10000"; "10000"; "10000"; "10000"; "01111" ]
        | 'D' -> [ "11110"; "10001"; "10001"; "10001"; "10001"; "10001"; "11110" ]
        | 'E' -> [ "11111"; "10000"; "10000"; "11110"; "10000"; "10000"; "11111" ]
        | 'F' -> [ "11111"; "10000"; "10000"; "11110"; "10000"; "10000"; "10000" ]
        | 'G' -> [ "01111"; "10000"; "10000"; "10011"; "10001"; "10001"; "01111" ]
        | 'H' -> [ "10001"; "10001"; "10001"; "11111"; "10001"; "10001"; "10001" ]
        | 'I' -> [ "11111"; "00100"; "00100"; "00100"; "00100"; "00100"; "11111" ]
        | 'J' -> [ "00111"; "00010"; "00010"; "00010"; "10010"; "10010"; "01100" ]
        | 'K' -> [ "10001"; "10010"; "10100"; "11000"; "10100"; "10010"; "10001" ]
        | 'L' -> [ "10000"; "10000"; "10000"; "10000"; "10000"; "10000"; "11111" ]
        | 'M' -> [ "10001"; "11011"; "10101"; "10101"; "10001"; "10001"; "10001" ]
        | 'N' -> [ "10001"; "11001"; "10101"; "10011"; "10001"; "10001"; "10001" ]
        | 'O' -> [ "01110"; "10001"; "10001"; "10001"; "10001"; "10001"; "01110" ]
        | 'P' -> [ "11110"; "10001"; "10001"; "11110"; "10000"; "10000"; "10000" ]
        | 'Q' -> [ "01110"; "10001"; "10001"; "10001"; "10101"; "10010"; "01101" ]
        | 'R' -> [ "11110"; "10001"; "10001"; "11110"; "10100"; "10010"; "10001" ]
        | 'S' -> [ "01111"; "10000"; "10000"; "01110"; "00001"; "00001"; "11110" ]
        | 'T' -> [ "11111"; "00100"; "00100"; "00100"; "00100"; "00100"; "00100" ]
        | 'U' -> [ "10001"; "10001"; "10001"; "10001"; "10001"; "10001"; "01110" ]
        | 'V' -> [ "10001"; "10001"; "10001"; "10001"; "10001"; "01010"; "00100" ]
        | 'W' -> [ "10001"; "10001"; "10001"; "10101"; "10101"; "10101"; "01010" ]
        | 'X' -> [ "10001"; "10001"; "01010"; "00100"; "01010"; "10001"; "10001" ]
        | 'Y' -> [ "10001"; "10001"; "01010"; "00100"; "00100"; "00100"; "00100" ]
        | 'Z' -> [ "11111"; "00001"; "00010"; "00100"; "01000"; "10000"; "11111" ]
        | '0' -> [ "01110"; "10001"; "10011"; "10101"; "11001"; "10001"; "01110" ]
        | '1' -> [ "00100"; "01100"; "00100"; "00100"; "00100"; "00100"; "01110" ]
        | '2' -> [ "01110"; "10001"; "00001"; "00010"; "00100"; "01000"; "11111" ]
        | '3' -> [ "11110"; "00001"; "00001"; "01110"; "00001"; "00001"; "11110" ]
        | '4' -> [ "00010"; "00110"; "01010"; "10010"; "11111"; "00010"; "00010" ]
        | '5' -> [ "11111"; "10000"; "10000"; "11110"; "00001"; "00001"; "11110" ]
        | '6' -> [ "01110"; "10000"; "10000"; "11110"; "10001"; "10001"; "01110" ]
        | '7' -> [ "11111"; "00001"; "00010"; "00100"; "01000"; "01000"; "01000" ]
        | '8' -> [ "01110"; "10001"; "10001"; "01110"; "10001"; "10001"; "01110" ]
        | '9' -> [ "01110"; "10001"; "10001"; "01111"; "00001"; "00001"; "01110" ]
        | '.' -> [ "00000"; "00000"; "00000"; "00000"; "00000"; "01100"; "01100" ]
        | ',' -> [ "00000"; "00000"; "00000"; "00000"; "01100"; "01100"; "01000" ]
        | ':' -> [ "00000"; "01100"; "01100"; "00000"; "01100"; "01100"; "00000" ]
        | ';' -> [ "00000"; "01100"; "01100"; "00000"; "01100"; "01100"; "01000" ]
        | '/' -> [ "00001"; "00010"; "00010"; "00100"; "01000"; "01000"; "10000" ]
        | '\\' -> [ "10000"; "01000"; "01000"; "00100"; "00010"; "00010"; "00001" ]
        | '-' -> [ "00000"; "00000"; "00000"; "11111"; "00000"; "00000"; "00000" ]
        | '_' -> [ "00000"; "00000"; "00000"; "00000"; "00000"; "00000"; "11111" ]
        | '+' -> [ "00000"; "00100"; "00100"; "11111"; "00100"; "00100"; "00000" ]
        | '=' -> [ "00000"; "00000"; "11111"; "00000"; "11111"; "00000"; "00000" ]
        | '(' -> [ "00010"; "00100"; "01000"; "01000"; "01000"; "00100"; "00010" ]
        | ')' -> [ "01000"; "00100"; "00010"; "00010"; "00010"; "00100"; "01000" ]
        | '[' -> [ "01110"; "01000"; "01000"; "01000"; "01000"; "01000"; "01110" ]
        | ']' -> [ "01110"; "00010"; "00010"; "00010"; "00010"; "00010"; "01110" ]
        | '&' -> [ "01100"; "10010"; "10100"; "01000"; "10101"; "10010"; "01101" ]
        | '%' -> [ "11001"; "11010"; "00010"; "00100"; "01000"; "01011"; "10011" ]
        | '!' -> [ "00100"; "00100"; "00100"; "00100"; "00100"; "00000"; "00100" ]
        | '?' -> [ "01110"; "10001"; "00001"; "00010"; "00100"; "00000"; "00100" ]
        | ' ' -> [ "00000"; "00000"; "00000"; "00000"; "00000"; "00000"; "00000" ]
        | _ -> [ "11111"; "00001"; "00010"; "00100"; "00100"; "00000"; "00100" ]

    let drawVectorText (canvas: SKCanvas) x y (text: string) size color antialias =
        let cell = Math.Max(1.0f, float32 size / 7.0f)
        let glyphAdvance = cell * 6.0f
        let top = float32 y - float32 size

        use paint = new SKPaint()
        paint.Color <- color
        paint.IsAntialias <- antialias
        paint.Style <- SKPaintStyle.Fill

        text
        |> Seq.iteri (fun index character ->
            let left = float32 x + float32 index * glyphAdvance

            vectorGlyphPattern character
            |> List.iteri (fun row line ->
                line
                |> Seq.iteri (fun column value ->
                    if value = '1' then
                        canvas.DrawRect(left + float32 column * cell, top + float32 row * cell, cell * 0.86f, cell * 0.86f, paint))))

    let drawTextWithFallback (canvas: SKCanvas) x y (text: string) size color antialias =
        let mutable nativeDrawn = false

        try
            use paint = new SKPaint()
            use font = new SKFont(SKTypeface.Default, float32 size)
            paint.Color <- color
            paint.IsAntialias <- antialias

            if font.ContainsGlyphs(text) then
                canvas.DrawText(text, SKPoint(float32 x, float32 y), font, paint)
                nativeDrawn <- true
        with _ ->
            nativeDrawn <- false

        if not nativeDrawn then
            drawVectorText canvas x y text size color antialias

    /// Paint one `SceneNode` onto `canvas`. Exhaustive over every `SceneNode` case —
    /// **no wildcard** — so a new primitive forces both render paths to handle it.
    let rec paintNode (canvas: SKCanvas) (node: SceneNode) =
        match node with
        | Empty -> ()
        | Group scenes -> scenes |> List.iter (fun scene -> scene.Nodes |> List.iter (paintNode canvas))
        | Rectangle((x, y, width, height), fill) ->
            use paint = new SKPaint()
            paint.Color <- skColor fill
            paint.Style <- SKPaintStyle.Fill
            canvas.DrawRect(float32 x, float32 y, float32 width, float32 height, paint)
        | PaintedRectangle(bounds, scenePaint) ->
            use paint = new SKPaint()
            configurePaint scenePaint paint
            canvas.DrawRect(float32 bounds.X, float32 bounds.Y, float32 bounds.Width, float32 bounds.Height, paint)
        | Circle(center, radius, fill) ->
            use paint = new SKPaint()
            paint.Color <- skColor fill
            paint.Style <- SKPaintStyle.Fill

            let bounds =
                { X = center.X - radius
                  Y = center.Y - radius
                  Width = radius * 2.0
                  Height = radius * 2.0 }

            canvas.DrawOval(skRect bounds, paint)
        | FilledEllipse(bounds, fill) ->
            use paint = new SKPaint()
            paint.Color <- skColor fill
            paint.Style <- SKPaintStyle.Fill
            canvas.DrawOval(skRect bounds, paint)
        | Ellipse(bounds, scenePaint) ->
            use paint = new SKPaint()
            configurePaint scenePaint paint
            canvas.DrawOval(skRect bounds, paint)
        | Line(startPoint, endPoint, scenePaint) ->
            use paint = new SKPaint()
            configurePaint { scenePaint with Stroke = Some(scenePaint.Stroke |> Option.defaultValue { Width = 1.0; Cap = Butt; Join = Miter; Miter = 4.0 }) } paint
            canvas.DrawLine(float32 startPoint.X, float32 startPoint.Y, float32 endPoint.X, float32 endPoint.Y, paint)
        | Path(path, scenePaint) ->
            use paint = new SKPaint()
            use skPath = toSkPath path
            configurePaint scenePaint paint
            canvas.DrawPath(skPath, paint)
        | Points(points, scenePaint) ->
            use paint = new SKPaint()
            configurePaint scenePaint paint
            for point in points do
                canvas.DrawPoint(float32 point.X, float32 point.Y, paint)
        | Vertices(mode, vertices, scenePaint) ->
            use paint = new SKPaint()
            configurePaint scenePaint paint

            if vertices.Length >= 3 then
                let positions =
                    vertices
                    |> List.map (fun vertex -> SKPoint(float32 vertex.Position.X, float32 vertex.Position.Y))
                    |> List.toArray

                let colors =
                    vertices
                    |> List.map (fun vertex -> vertex.Color |> Option.defaultValue (scenePaint.Fill |> Option.defaultValue Colors.white) |> skColor)
                    |> List.toArray

                use skVertices = SKVertices.CreateCopy(vertexMode mode, positions, colors)
                canvas.DrawVertices(skVertices, SKBlendMode.SrcOver, paint)
            else
                for vertex in vertices do
                    use vertexPaint = new SKPaint()
                    configurePaint scenePaint vertexPaint
                    vertexPaint.Color <- vertex.Color |> Option.defaultValue (scenePaint.Fill |> Option.defaultValue Colors.white) |> skColor
                    canvas.DrawCircle(float32 vertex.Position.X, float32 vertex.Position.Y, 2.0f, vertexPaint)
        | Arc(bounds, startAngle, sweepAngle, scenePaint) ->
            use paint = new SKPaint()
            configurePaint scenePaint paint
            canvas.DrawArc(skRect bounds, float32 startAngle, float32 sweepAngle, false, paint)
        | Text((x, y), text, color) ->
            drawTextWithFallback canvas x y text 24.0 (skColor color) true
        | TextRun run ->
            drawTextWithFallback canvas run.Position.X run.Position.Y run.Text run.Font.Size (paintColor run.Paint) run.Paint.Antialias
        | Image((x, y, width, height), source) ->
            let destination = SKRect(float32 x, float32 y, float32 (x + width), float32 (y + height))

            if File.Exists source then
                use image = SKImage.FromEncodedData(source)

                if isNull image then
                    use paint = new SKPaint()
                    paint.Color <- SKColor(96uy, 128uy, 160uy, 255uy)
                    paint.Style <- SKPaintStyle.Stroke
                    paint.StrokeWidth <- 2.0f
                    canvas.DrawRect(destination, paint)
                else
                    canvas.DrawImage(image, destination)
            else
                use paint = new SKPaint()
                paint.Color <- SKColor(96uy, 128uy, 160uy, 255uy)
                paint.Style <- SKPaintStyle.Stroke
                paint.StrokeWidth <- 2.0f
                canvas.DrawRect(destination, paint)
        | ClipNode(clip, clippedScene) ->
            canvas.Save() |> ignore

            match clip with
            | RectClip bounds ->
                canvas.ClipRect(skRect bounds) |> ignore
            | PathClip path ->
                use skPath = toSkPath path
                canvas.ClipPath(skPath) |> ignore

            clippedScene.Nodes |> List.iter (paintNode canvas)
            canvas.Restore()
        | RegionNode(region, scenePaint) ->
            use paint = new SKPaint()
            configurePaint scenePaint paint

            for bounds in region.Bounds do
                canvas.DrawRect(float32 bounds.X, float32 bounds.Y, float32 bounds.Width, float32 bounds.Height, paint)
        | ColorSpaceNode(_, scene) -> scene.Nodes |> List.iter (paintNode canvas)
        | PerspectiveNode(transform, scene) ->
            canvas.Save() |> ignore
            let matrix =
                SKMatrix(
                    float32 transform.M11,
                    float32 transform.M12,
                    float32 transform.M13,
                    float32 transform.M21,
                    float32 transform.M22,
                    float32 transform.M23,
                    float32 transform.M31,
                    float32 transform.M32,
                    float32 transform.M33
                )

            canvas.Concat(&matrix)
            scene.Nodes |> List.iter (paintNode canvas)
            canvas.Restore()
        | PictureNode picture -> picture.Scene.Nodes |> List.iter (paintNode canvas)
        | Chart values ->
            if not values.IsEmpty then
                let maxValue = values |> List.max
                let chartLeft = 32.0f
                let chartTop = 180.0f
                let chartHeight = 220.0f
                let barWidth = 32.0f
                use paint = new SKPaint()
                paint.Color <- SKColor(96uy, 190uy, 120uy, 255uy)
                paint.Style <- SKPaintStyle.Fill

                values
                |> List.iteri (fun index value ->
                    let normalized = if maxValue <= 0.0 then 0.0 else value / maxValue
                    let height = float32 normalized * chartHeight
                    let x = chartLeft + float32 index * (barWidth + 12.0f)
                    let y = chartTop + chartHeight - height
                    canvas.DrawRect(x, y, barWidth, height, paint))
        | Translate((dx, dy), scene) ->
            canvas.Save() |> ignore
            canvas.Translate(float32 dx, float32 dy)
            scene.Nodes |> List.iter (paintNode canvas)
            canvas.Restore()
        | SizedText((x, y), text, size, color) ->
            drawTextWithFallback canvas x y text size (skColor color) true
        // Feature 120 (US3, FR-007): a backend replay-cache boundary. With an active cache, replay the
        // recorded picture on a hit or record-then-draw on a miss; otherwise (no/disabled cache) recurse
        // straight into the wrapped scene — TRANSPARENT, byte-identical to the direct walk (FR-011).
        | CachedSubtree boundary ->
            match activeReplayCache with
            | Some cache ->
                PictureReplayCache.paintBoundary cache canvas (fun c (s: Scene) -> s.Nodes |> List.iter (paintNode c)) boundary
            | None -> boundary.Scene.Nodes |> List.iter (paintNode canvas)
