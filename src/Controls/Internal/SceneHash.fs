namespace FS.GG.UI.Controls

open System
open FS.GG.UI.Scene

/// Feature 189 (US2, FR-003 / D3): `hashScene` — the deterministic FNV-1a structural fingerprint over
/// render-affecting Scene data — relocated verbatim from `ControlInternals` (preserving the exact
/// tag->fields->children mix order and the `mutable h` hot-path accumulator). `module internal`;
/// `Scene list -> uint64` name/shape preserved so callers resolve unchanged. Byte-identical output.
module internal SceneHash =
    let hashScene (scenes: Scene list) : uint64 =
        // Feature 178 (US2): constants + core step from the shared Hashing primitive; the typed
        // `uint64` mixers below are unchanged, so the fingerprint is byte-identical.
        let mutable h = Hashing.offsetBasis // mutable: hot path / FNV-1a accumulator
        let mix (x: uint64) = h <- Hashing.step h x
        let bits (d: float) = uint64 (System.BitConverter.DoubleToInt64Bits d)
        let mixTag (t: int) = mix (uint64 (uint32 t))
        let mixBool (v: bool) = mix (if v then 1UL else 0UL)
        let mixByte (v: byte) = mix (uint64 v)
        let mixInt (v: int) = mix (uint64 (uint32 v))
        let mixFloat (v: float) = mix (bits v)

        let mixStr (s: string) =
            mix (uint64 s.Length)
            for c in s do
                mix (uint64 (uint16 c))

        let mixStringOption =
            function
            | None -> mixTag 0
            | Some value ->
                mixTag 1
                mixStr value

        let mixOption mixValue =
            function
            | None -> mixTag 0
            | Some value ->
                mixTag 1
                mixValue value

        let mixList mixValue values =
            mix (uint64 (List.length values))
            values |> List.iter mixValue

        let mixColor (c: Color) =
            mixByte c.Red
            mixByte c.Green
            mixByte c.Blue
            mixByte c.Alpha

        let mixPoint (p: Point) =
            mixFloat p.X
            mixFloat p.Y

        let mixRect (r: Rect) =
            mixFloat r.X
            mixFloat r.Y
            mixFloat r.Width
            mixFloat r.Height

        let mixStrokeCap =
            function
            | Butt -> mixTag 1
            | Round -> mixTag 2
            | Square -> mixTag 3

        let mixStrokeJoin =
            function
            | Miter -> mixTag 1
            | RoundJoin -> mixTag 2
            | Bevel -> mixTag 3

        let mixBlendMode (mode: BlendMode) =
            match mode with
            | BlendMode.SrcOver -> mixTag 1
            | BlendMode.Multiply -> mixTag 2
            | BlendMode.Screen -> mixTag 3
            | BlendMode.Overlay -> mixTag 4
            | BlendMode.Darken -> mixTag 5
            | BlendMode.Lighten -> mixTag 6
            | BlendMode.ColorDodge -> mixTag 7
            | BlendMode.ColorBurn -> mixTag 8
            | BlendMode.Difference -> mixTag 9
            | BlendMode.Exclusion -> mixTag 10

        let mixStroke (s: Stroke) =
            mixFloat s.Width
            mixStrokeCap s.Cap
            mixStrokeJoin s.Join
            mixFloat s.Miter

        let mixShader =
            function
            | SolidColor color ->
                mixTag 1
                mixColor color
            | LinearGradient(startPoint, endPoint, colors) ->
                mixTag 2
                mixPoint startPoint
                mixPoint endPoint
                mixList mixColor colors
            | RadialGradient(center, radius, colors) ->
                mixTag 3
                mixPoint center
                mixFloat radius
                mixList mixColor colors
            | SweepGradient(center, colors) ->
                mixTag 4
                mixPoint center
                mixList mixColor colors

        let mixColorFilter =
            function
            | NoColorFilter -> mixTag 1
            | BlendColor(color, mode) ->
                mixTag 2
                mixColor color
                mixBlendMode mode

        let mixMaskFilter =
            function
            | NoMaskFilter -> mixTag 1
            | Blur sigma ->
                mixTag 2
                mixFloat sigma

        let mixImageFilter =
            function
            | NoImageFilter -> mixTag 1
            | DropShadow(dx, dy, blur, color) ->
                mixTag 2
                mixFloat dx
                mixFloat dy
                mixFloat blur
                mixColor color

        let mixPathEffect =
            function
            | NoPathEffect -> mixTag 1
            | Dash(intervals, phase) ->
                mixTag 2
                mixList mixFloat intervals
                mixFloat phase
            | Discrete(segmentLength, deviation) ->
                mixTag 3
                mixFloat segmentLength
                mixFloat deviation
            | Corner radius ->
                mixTag 4
                mixFloat radius

        let mixPaint (p: Paint) =
            mixOption mixColor p.Fill
            mixOption mixStroke p.Stroke
            mixFloat p.Opacity
            mixBool p.Antialias
            mixBlendMode p.BlendMode
            mixOption mixShader p.Shader
            mixColorFilter p.ColorFilter
            mixMaskFilter p.MaskFilter
            mixImageFilter p.ImageFilter
            mixPathEffect p.PathEffect

        let mixPathFillType =
            function
            | Winding -> mixTag 1
            | EvenOdd -> mixTag 2

        let mixPathCommand =
            function
            | MoveTo point ->
                mixTag 1
                mixPoint point
            | LineTo point ->
                mixTag 2
                mixPoint point
            | QuadTo(control, point) ->
                mixTag 3
                mixPoint control
                mixPoint point
            | CubicTo(control1, control2, point) ->
                mixTag 4
                mixPoint control1
                mixPoint control2
                mixPoint point
            | ArcTo(bounds, startAngle, sweepAngle) ->
                mixTag 5
                mixRect bounds
                mixFloat startAngle
                mixFloat sweepAngle
            | Close -> mixTag 6

        let mixPathSpec (p: PathSpec) =
            mixList mixPathCommand p.Commands
            mixPathFillType p.FillType

        let mixClip =
            function
            | RectClip rect ->
                mixTag 1
                mixRect rect
            | PathClip path ->
                mixTag 2
                mixPathSpec path

        let mixRegionOperation (operation: RegionOperation) =
            match operation with
            | Replace -> mixTag 1
            | RegionUnion -> mixTag 2
            | RegionIntersect -> mixTag 3
            | RegionDifference -> mixTag 4

        let mixRegion (r: Region) =
            mixList mixRect r.Bounds
            mixRegionOperation r.Operation

        let mixColorSpace =
            function
            | Srgb -> mixTag 1
            | DisplayP3 -> mixTag 2
            | AdobeRgb -> mixTag 3

        let mixPerspective (t: PerspectiveTransform) =
            mixFloat t.M11
            mixFloat t.M12
            mixFloat t.M13
            mixFloat t.M21
            mixFloat t.M22
            mixFloat t.M23
            mixFloat t.M31
            mixFloat t.M32
            mixFloat t.M33

        let mixFont (font: FontSpec) =
            mixStringOption font.Family
            mixFloat font.Size
            mixOption mixInt font.Weight

        let mixTextRun (run: TextRun) =
            mixStr run.Text
            mixPoint run.Position
            mixFont run.Font
            mixPaint run.Paint

        let mixVertexMode =
            function
            | Triangles -> mixTag 1
            | TriangleStrip -> mixTag 2
            | TriangleFan -> mixTag 3

        let mixVertex (v: Vertex) =
            mixPoint v.Position
            mixOption mixColor v.Color

        let mixGlyphRun (run: GlyphRun) =
            mixStr run.Data.Text
            mixFont run.Data.Font
            mixStr run.Data.Fingerprint
            mixPoint run.Position
            mixPaint run.Paint

        let rec goNodes (nodes: SceneNode list) =
            mixTag 0xA1
            mix (uint64 (List.length nodes))
            nodes |> List.iter goNode
            mixTag 0xA2

        and goScene (s: Scene) = goNodes s.Nodes

        and goNode (node: SceneNode) =
            match node with
            | Empty -> mixTag 1
            | Group scenes ->
                mixTag 2
                mix (uint64 (List.length scenes))
                scenes |> List.iter goScene
            | Rectangle(b, c) ->
                mixTag 3
                let x, y, w, ht = b
                mixFloat x
                mixFloat y
                mixFloat w
                mixFloat ht
                mixColor c
            | PaintedRectangle(r, p) ->
                mixTag 4
                mixRect r
                mixPaint p
            | Circle(ctr, rad, fill) ->
                mixTag 5
                mixPoint ctr
                mixFloat rad
                mixColor fill
            | FilledEllipse(b, fill) ->
                mixTag 6
                mixRect b
                mixColor fill
            | Ellipse(r, p) ->
                mixTag 7
                mixRect r
                mixPaint p
            | Line(a, b, p) ->
                mixTag 8
                mixPoint a
                mixPoint b
                mixPaint p
            | Path(spec, p) ->
                mixTag 9
                mixPathSpec spec
                mixPaint p
            | Points(pts, p) ->
                mixTag 10
                mixList mixPoint pts
                mixPaint p
            | Vertices(m, vs, p) ->
                mixTag 11
                mixVertexMode m
                mixList mixVertex vs
                mixPaint p
            | Arc(r, sa, ea, p) ->
                mixTag 12
                mixRect r
                mixFloat sa
                mixFloat ea
                mixPaint p
            | Text((x, y), t, c) ->
                mixTag 13
                mixFloat x
                mixFloat y
                mixStr t
                mixColor c
            | TextRun run ->
                mixTag 14
                mixTextRun run
            | Image((x, y, w, ht), src) ->
                mixTag 15
                mixFloat x
                mixFloat y
                mixFloat w
                mixFloat ht
                mixStr src
            | ClipNode(clip, scene) ->
                mixTag 16
                mixClip clip
                goScene scene
            | RegionNode(region, p) ->
                mixTag 17
                mixRegion region
                mixPaint p
            | ColorSpaceNode(cs, scene) ->
                mixTag 18
                mixColorSpace cs
                goScene scene
            | PerspectiveNode(t, scene) ->
                mixTag 19
                mixPerspective t
                goScene scene
            | PictureNode picture ->
                mixTag 20
                mixStr picture.Name
                goScene picture.Scene
            | Chart values ->
                mixTag 21
                mixList mixFloat values
            | Translate((dx, dy), scene) ->
                mixTag 22
                mixFloat dx
                mixFloat dy
                goScene scene
            | SizedText((x, y), t, size, c) ->
                mixTag 23
                mixFloat x
                mixFloat y
                mixStr t
                mixFloat size
                mixColor c
            | CachedSubtree boundary ->
                mixTag 24
                goScene boundary.Scene
            | GlyphRun run ->
                mixTag 25
                mixGlyphRun run

        mix (uint64 (List.length scenes))
        scenes |> List.iter goScene
        h
