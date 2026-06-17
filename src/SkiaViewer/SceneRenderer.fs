namespace FS.GG.UI.SkiaViewer

#nowarn "44"
#nowarn "3261"
#nowarn "3391"

open System
open System.IO
open SkiaSharp
open FS.GG.UI.Scene

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

    // Feature 136 (FR-001): a tofu (missing-glyph) box — an unambiguous hollow rectangle, never a
    // plausible-looking letter or digit. Drawn only for characters with no bundled coverage and no
    // deliberate substitute. Its advance matches `Fonts.charAdvance` (≈0.6·size) so measure == draw.
    let private drawTofuBox (canvas: SKCanvas) (left: float32) (baseline: float32) (size: float) (color: SKColor) antialias =
        use paint = new SKPaint()
        paint.Color <- color
        paint.IsAntialias <- antialias
        paint.Style <- SKPaintStyle.Stroke
        paint.StrokeWidth <- max 1.0f (float32 size / 16.0f)
        let h = float32 size * 0.7f
        let w = float32 size * 0.5f
        canvas.DrawRect(left, baseline - h, w, h, paint)

    // Feature 136 (FR-001/SC-005): per-present text-fallback disclosure accumulator. The renderer
    // records every non-`Authored` per-character outcome as it DRAWS (not during measurement — that
    // would double count). The host resets it at the start of each present/screenshot and reads it via
    // `SkiaViewer` (T017). Single-present, single-threaded edge mutation (constitution IV).
    let mutable fallbackEvents: ResizeArray<Fonts.ResolvedChar> = ResizeArray()

    let resetFallbackEvents () = fallbackEvents <- ResizeArray()

    let rec private drawGlyphRunData (canvas: SKCanvas) x y (data: GlyphRunData) (color: SKColor) antialias =
        use paint = new SKPaint()
        paint.Color <- color
        paint.IsAntialias <- antialias
        paint.Style <- SKPaintStyle.Fill

        if data.Provider.Availability = ProviderInstalled && not data.Glyphs.IsEmpty then
            let glyphs = data.Glyphs |> List.map (fun g -> uint16 g.GlyphId) |> List.toArray
            let positions =
                data.Glyphs
                |> List.map (fun g -> SKPoint(float32 (x + g.Position.X + g.Offset.X), float32 (y + g.Position.Y + g.Offset.Y)))
                |> List.toArray

            use builder = new SKTextBlobBuilder()
            builder.AddPositionedRun(glyphs, Fonts.resolveFont data.Font, positions)
            use blob = builder.Build()

            if not (isNull blob) then
                canvas.DrawText(blob, 0.0f, 0.0f, paint)
        else
            drawFallbackText canvas x y data.Text data.Font color antialias

    /// Draw `text` with its baseline at (x, y) through the bundled-font registry. Each character is
    /// resolved to the real typeface that covers it (per-character fallback chain) and drawn at the
    /// registry's advance, so the drawn width equals the measured width (no mid-word clip) and mixed
    /// case is preserved (no force-uppercase). Uncovered characters render as a disclosed tofu box —
    /// never a different-but-plausible glyph. Replaces the host-`SKTypeface.Default`/5×7 vector path.
    and private drawFallbackText (canvas: SKCanvas) x y (text: string) (font: FontSpec) (color: SKColor) antialias =
        let size = max 1.0 font.Size
        let resolved = Fonts.resolveText font text
        use paint = new SKPaint()
        paint.Color <- color
        paint.IsAntialias <- antialias
        paint.Style <- SKPaintStyle.Fill
        let baseline = float32 y
        let mutable penX = float32 x

        for rc in resolved do
            match rc.Resolution with
            | Fonts.FallbackResolution.Authored _ -> ()
            | _ -> fallbackEvents.Add rc

            match rc.Resolution with
            | Fonts.FallbackResolution.Tofu _ -> drawTofuBox canvas penX baseline size color antialias
            | _ -> canvas.DrawText(string rc.Rendered, SKPoint(penX, baseline), rc.Font, paint)

            penX <- penX + float32 (Fonts.charAdvance size rc)

    let drawText (canvas: SKCanvas) x y (text: string) (font: FontSpec) (color: SKColor) antialias =
        let shaped = Fonts.buildShapedGlyphRunData text font

        if shaped.Provider.Availability = ProviderInstalled then
            let resolved = Fonts.resolveText font text

            for rc in resolved do
                match rc.Resolution with
                | Fonts.FallbackResolution.Authored _ -> ()
                | _ -> fallbackEvents.Add rc

        drawGlyphRunData canvas x y shaped color antialias

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
            drawText canvas x y text { Family = None; Size = 24.0; Weight = None } (skColor color) true
        | TextRun run ->
            drawText canvas run.Position.X run.Position.Y run.Text run.Font (paintColor run.Paint) run.Paint.Antialias
        | GlyphRun run ->
            drawGlyphRunData canvas run.Position.X run.Position.Y run.Data (paintColor run.Paint) run.Paint.Antialias
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
            drawText canvas x y text { Family = None; Size = size; Weight = None } (skColor color) true
        // Feature 120 (US3, FR-007): a backend replay-cache boundary. With an active cache, replay the
        // recorded picture on a hit or record-then-draw on a miss; otherwise (no/disabled cache) recurse
        // straight into the wrapped scene — TRANSPARENT, byte-identical to the direct walk (FR-011).
        | CachedSubtree boundary ->
            match activeReplayCache with
            | Some cache ->
                PictureReplayCache.paintBoundary cache canvas (fun c (s: Scene) -> s.Nodes |> List.iter (paintNode c)) boundary
            | None -> boundary.Scene.Nodes |> List.iter (paintNode canvas)
