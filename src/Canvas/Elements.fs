namespace FS.GG.UI.Canvas

open System
open FS.GG.UI.Scene

type Element<'props> = 'props -> Scene

[<RequireQualifiedAccess>]
module Elements =

    // Deterministic FNV-1a fold over a sub-scene's render-affecting inputs. Pure and process-stable
    // (no String.GetHashCode, which is per-process randomized): same scene ⇒ same fingerprint across
    // runs (FR-008/FR-011). Used to key `cached` replay boundaries by content.
    let private offsetBasis = 0xcbf29ce484222325UL
    let private prime = 0x100000001b3UL
    let private step (h: uint64) (x: uint64) = (h ^^^ x) * prime
    let private mixFloat (h: uint64) (f: float) = step h (uint64 (BitConverter.DoubleToInt64Bits f))

    let private mixString (h: uint64) (s: string) =
        let mutable acc = step h (uint64 s.Length)
        for b in Text.Encoding.UTF8.GetBytes s do
            acc <- step acc (uint64 b)
        acc

    let private mixBool (h: uint64) (b: bool) = step h (if b then 1UL else 0UL)
    let private mixInt (h: uint64) (i: int) = step h (uint64 (uint32 i))

    let private mixColor (h: uint64) (c: Color) =
        step (step (step (step h (uint64 c.Red)) (uint64 c.Green)) (uint64 c.Blue)) (uint64 c.Alpha)

    let private mixPoint (h: uint64) (p: Point) = mixFloat (mixFloat h p.X) p.Y

    let private mixRect (h: uint64) (r: Rect) =
        mixFloat (mixFloat (mixFloat (mixFloat h r.X) r.Y) r.Width) r.Height

    // FNV-tagged Option/List folds so `None`/`[]` and length are part of the fingerprint (a field
    // gaining/losing a value, or a list changing length, must move the hash).
    let private mixOption mix (h: uint64) (o: 'a option) =
        match o with
        | None -> step h 0UL
        | Some value -> mix (step h 1UL) value

    let private mixList mix (h: uint64) (xs: 'a list) =
        List.fold mix (step h (uint64 (List.length xs))) xs

    let private mixStringOption (h: uint64) (o: string option) = mixOption mixString h o

    let private mixStrokeCap (h: uint64) =
        function
        | Butt -> step h 1UL
        | Round -> step h 2UL
        | Square -> step h 3UL

    let private mixStrokeJoin (h: uint64) =
        function
        | Miter -> step h 1UL
        | RoundJoin -> step h 2UL
        | Bevel -> step h 3UL

    let private mixStroke (h: uint64) (s: Stroke) =
        mixFloat (mixStrokeJoin (mixStrokeCap (mixFloat h s.Width) s.Cap) s.Join) s.Miter

    let private mixBlendMode (h: uint64) (mode: BlendMode) =
        let tag =
            match mode with
            | BlendMode.SrcOver -> 1UL
            | BlendMode.Multiply -> 2UL
            | BlendMode.Screen -> 3UL
            | BlendMode.Overlay -> 4UL
            | BlendMode.Darken -> 5UL
            | BlendMode.Lighten -> 6UL
            | BlendMode.ColorDodge -> 7UL
            | BlendMode.ColorBurn -> 8UL
            | BlendMode.Difference -> 9UL
            | BlendMode.Exclusion -> 10UL
        step h tag

    let private mixShader (h: uint64) =
        function
        | SolidColor color -> mixColor (step h 1UL) color
        | LinearGradient(s, e, colors) -> mixList mixColor (mixPoint (mixPoint (step h 2UL) s) e) colors
        | RadialGradient(center, radius, colors) -> mixList mixColor (mixFloat (mixPoint (step h 3UL) center) radius) colors
        | SweepGradient(center, colors) -> mixList mixColor (mixPoint (step h 4UL) center) colors

    let private mixColorFilter (h: uint64) =
        function
        | NoColorFilter -> step h 1UL
        | BlendColor(color, mode) -> mixBlendMode (mixColor (step h 2UL) color) mode

    let private mixMaskFilter (h: uint64) =
        function
        | NoMaskFilter -> step h 1UL
        | Blur sigma -> mixFloat (step h 2UL) sigma

    let private mixImageFilter (h: uint64) =
        function
        | NoImageFilter -> step h 1UL
        | DropShadow(dx, dy, blur, color) -> mixColor (mixFloat (mixFloat (mixFloat (step h 2UL) dx) dy) blur) color

    let private mixPathEffect (h: uint64) =
        function
        | NoPathEffect -> step h 1UL
        | Dash(intervals, phase) -> mixFloat (mixList mixFloat (step h 2UL) intervals) phase
        | Discrete(segmentLength, deviation) -> mixFloat (mixFloat (step h 3UL) segmentLength) deviation
        | Corner radius -> mixFloat (step h 4UL) radius

    let private mixPaint (h: uint64) (p: Paint) =
        let h = mixOption mixColor h p.Fill
        let h = mixOption mixStroke h p.Stroke
        let h = mixFloat h p.Opacity
        let h = mixBool h p.Antialias
        let h = mixBlendMode h p.BlendMode
        let h = mixOption mixShader h p.Shader
        let h = mixColorFilter h p.ColorFilter
        let h = mixMaskFilter h p.MaskFilter
        let h = mixImageFilter h p.ImageFilter
        mixPathEffect h p.PathEffect

    let private mixPathFillType (h: uint64) =
        function
        | Winding -> step h 1UL
        | EvenOdd -> step h 2UL

    let private mixPathCommand (h: uint64) =
        function
        | MoveTo p -> mixPoint (step h 1UL) p
        | LineTo p -> mixPoint (step h 2UL) p
        | QuadTo(c, p) -> mixPoint (mixPoint (step h 3UL) c) p
        | CubicTo(c1, c2, p) -> mixPoint (mixPoint (mixPoint (step h 4UL) c1) c2) p
        | ArcTo(b, sa, sw) -> mixFloat (mixFloat (mixRect (step h 5UL) b) sa) sw
        | Close -> step h 6UL

    let private mixPathSpec (h: uint64) (spec: PathSpec) =
        mixPathFillType (mixList mixPathCommand h spec.Commands) spec.FillType

    let private mixClip (h: uint64) =
        function
        | RectClip r -> mixRect (step h 1UL) r
        | PathClip spec -> mixPathSpec (step h 2UL) spec

    let private mixRegionOperation (h: uint64) =
        function
        | Replace -> step h 1UL
        | RegionUnion -> step h 2UL
        | RegionIntersect -> step h 3UL
        | RegionDifference -> step h 4UL

    let private mixRegion (h: uint64) (r: Region) =
        mixRegionOperation (mixList mixRect h r.Bounds) r.Operation

    let private mixColorSpace (h: uint64) =
        function
        | Srgb -> step h 1UL
        | DisplayP3 -> step h 2UL
        | AdobeRgb -> step h 3UL

    let private mixPerspective (h: uint64) (t: PerspectiveTransform) =
        [ t.M11; t.M12; t.M13; t.M21; t.M22; t.M23; t.M31; t.M32; t.M33 ] |> List.fold mixFloat h

    let private mixFont (h: uint64) (f: FontSpec) =
        mixOption mixInt (mixFloat (mixStringOption h f.Family) f.Size) f.Weight

    let private mixTextRun (h: uint64) (run: TextRun) =
        mixPaint (mixFont (mixPoint (mixString h run.Text) run.Position) run.Font) run.Paint

    let private mixVertexMode (h: uint64) =
        function
        | Triangles -> step h 1UL
        | TriangleStrip -> step h 2UL
        | TriangleFan -> step h 3UL

    let private mixVertex (h: uint64) (v: Vertex) = mixOption mixColor (mixPoint h v.Position) v.Color

    let private mixGlyphRun (h: uint64) (run: GlyphRun) =
        let h = mixString h run.Data.Text
        let h = mixFont h run.Data.Font
        let h = mixString h run.Data.Fingerprint
        let h = mixPoint h run.Position
        mixPaint h run.Paint

    // Deterministic fingerprint over EVERY render-affecting field of a sub-scene — paint included
    // (Review P2 / #45): the earlier fold was paint-blind (ignored `Paint` on painted rectangles,
    // points, lines, paths, clip/perspective/colour-space transforms) and hashed ellipses, arcs,
    // vertices, text/glyph runs, regions and pictures to a single constant, so the `PictureReplayCache`
    // replayed stale pixels when only those fields changed under one `cached` key. The `match` is
    // exhaustive (no wildcard) so any future `SceneNode` case is a compile error until it is hashed.
    let rec private mixScene (h: uint64) (scene: Scene) : uint64 =
        let mutable acc = step h (uint64 (List.length scene.Nodes))
        for node in scene.Nodes do
            acc <- mixNode acc node
        acc

    and private mixNode (h: uint64) (node: SceneNode) : uint64 =
        match node with
        | Empty -> step h 1UL
        | Group children -> List.fold mixScene (step h 2UL) children
        | Rectangle((x, y, w, ht), fill) -> mixColor (mixFloat (mixFloat (mixFloat (mixFloat (step h 3UL) x) y) w) ht) fill
        | PaintedRectangle(b, paint) -> mixPaint (mixRect (step h 4UL) b) paint
        | Circle(c, r, fill) -> mixColor (mixFloat (mixPoint (step h 5UL) c) r) fill
        | FilledEllipse(b, fill) -> mixColor (mixRect (step h 6UL) b) fill
        | Ellipse(b, paint) -> mixPaint (mixRect (step h 7UL) b) paint
        | Line(s, e, paint) -> mixPaint (mixPoint (mixPoint (step h 8UL) s) e) paint
        | Path(spec, paint) -> mixPaint (mixPathSpec (step h 9UL) spec) paint
        | Points(pts, paint) -> mixPaint (mixList mixPoint (step h 10UL) pts) paint
        | Vertices(mode, vs, paint) -> mixPaint (mixList mixVertex (mixVertexMode (step h 11UL) mode) vs) paint
        | Arc(b, sa, sw, paint) -> mixPaint (mixFloat (mixFloat (mixRect (step h 12UL) b) sa) sw) paint
        | Text((x, y), t, fill) -> mixColor (mixString (mixFloat (mixFloat (step h 13UL) x) y) t) fill
        | TextRun run -> mixTextRun (step h 14UL) run
        | Image((x, y, w, ht), src) -> mixString (mixFloat (mixFloat (mixFloat (mixFloat (step h 15UL) x) y) w) ht) src
        | ClipNode(clip, s) -> mixScene (mixClip (step h 16UL) clip) s
        | RegionNode(region, paint) -> mixPaint (mixRegion (step h 17UL) region) paint
        | ColorSpaceNode(cs, s) -> mixScene (mixColorSpace (step h 18UL) cs) s
        | PerspectiveNode(t, s) -> mixScene (mixPerspective (step h 19UL) t) s
        | PictureNode picture -> mixScene (mixString (step h 20UL) picture.Name) picture.Scene
        | Chart values -> List.fold mixFloat (step h 21UL) values
        | Translate((dx, dy), s) -> mixScene (mixFloat (mixFloat (step h 22UL) dx) dy) s
        | SizedText((x, y), t, sz, fill) -> mixColor (mixFloat (mixString (mixFloat (mixFloat (step h 23UL) x) y) t) sz) fill
        | CachedSubtree b -> mixScene (step (step h 24UL) b.CacheId) b.Scene
        | GlyphRun run -> mixGlyphRun (step h 25UL) run

    let rect (w: float) (h: float) (paint: Paint) : Scene =
        Scene.rectangleWithPaint { X = 0.0; Y = 0.0; Width = w; Height = h } paint

    let sprite (image: string) (w: float) (h: float) : Scene = Scene.image (0.0, 0.0, w, h) image

    let circle (r: float) (fill: Color) : Scene = Scene.circle { X = 0.0; Y = 0.0 } r fill

    let polyline (points: Point list) (paint: Paint) : Scene =
        match points with
        | [] | [ _ ] -> Scene.empty
        | first :: rest ->
            let commands =
                Path.moveTo first.X first.Y :: (rest |> List.map (fun p -> Path.lineTo p.X p.Y))
            Scene.path (Path.create PathFillType.Winding commands) paint

    let at (x: float) (y: float) (scene: Scene) : Scene = Scene.translate x y scene

    let layer (scenes: Scene list) : Scene = Scene.group scenes

    let cached (key: string) (scene: Scene) : Scene =
        let cacheId = mixString offsetBasis key
        let fingerprint = mixScene offsetBasis scene
        { Nodes = [ CachedSubtree { CacheId = cacheId; Fingerprint = fingerprint; Scene = scene } ] }
