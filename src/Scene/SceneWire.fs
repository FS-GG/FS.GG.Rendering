namespace FS.GG.UI.Scene

open System
open System.IO
open System.Text

/// Feature 187 (US3): internal wire codec carved out of SceneCodec.fs (bodies out, contracts stay).
/// Carries no `.fsi`; `module internal` keeps it off the public surface (FR-007), matching the
/// SceneRenderer.fs / Numeric.fs precedent. Every reader/writer is the verbatim body moved from
/// SceneCodec, so the wire bytes are unchanged (SC-004). The node codec keeps its deliberate
/// FS0025 exhaustive `writeSceneNode` match + per-tag `sceneNodeCodec` read table + Feature183
/// symmetry oracle (the read drift it removes was already removed by prior work).
module internal SceneWire =
    let enumTag value cases =
        cases |> List.findIndex ((=) value)

    let readEnum tag cases label =
        cases
        |> List.tryItem tag
        |> Option.defaultWith (fun () -> failwithf "Unknown %s tag %d" label tag)

    let writeString (writer: BinaryWriter) (value: string) =
        let bytes = Encoding.UTF8.GetBytes(value)
        writer.Write(bytes.Length)
        writer.Write(bytes)

    let readString (reader: BinaryReader) =
        let length = reader.ReadInt32()
        if length < 0 then failwith "Negative string length"
        Encoding.UTF8.GetString(reader.ReadBytes(length))

    let writeOption (writer: BinaryWriter) (writeValue: BinaryWriter -> 'a -> unit) (value: 'a option) =
        match value with
        | Some item ->
            writer.Write(true)
            writeValue writer item
        | None -> writer.Write(false)

    let readOption (reader: BinaryReader) (readValue: BinaryReader -> 'a) : 'a option =
        if reader.ReadBoolean() then Some(readValue reader) else None

    let writeList (writer: BinaryWriter) (writeValue: BinaryWriter -> 'a -> unit) (values: 'a list) =
        writer.Write(List.length values)
        values |> List.iter (writeValue writer)

    let readList (reader: BinaryReader) (readValue: BinaryReader -> 'a) : 'a list =
        let length = reader.ReadInt32()
        if length < 0 then failwith "Negative list length"
        [ for _ in 1 .. length -> readValue reader ]

    let writeColor (writer: BinaryWriter) (color: Color) =
        writer.Write(color.Red)
        writer.Write(color.Green)
        writer.Write(color.Blue)
        writer.Write(color.Alpha)

    let readColor (reader: BinaryReader) : Color =
        { Red = reader.ReadByte()
          Green = reader.ReadByte()
          Blue = reader.ReadByte()
          Alpha = reader.ReadByte() }

    let writePoint (writer: BinaryWriter) (point: Point) =
        writer.Write(point.X)
        writer.Write(point.Y)

    let readPoint (reader: BinaryReader) : Point =
        { X = reader.ReadDouble()
          Y = reader.ReadDouble() }

    let writeRect (writer: BinaryWriter) (rect: Rect) =
        writer.Write(rect.X)
        writer.Write(rect.Y)
        writer.Write(rect.Width)
        writer.Write(rect.Height)

    let readRect (reader: BinaryReader) : Rect =
        { X = reader.ReadDouble()
          Y = reader.ReadDouble()
          Width = reader.ReadDouble()
          Height = reader.ReadDouble() }

    let writeStringOption (writer: BinaryWriter) (value: string option) =
        writeOption writer writeString value

    let readStringOption (reader: BinaryReader) =
        readOption reader readString

    let writeIntOption (writer: BinaryWriter) (value: int option) =
        writeOption writer (fun (w: BinaryWriter) (v: int) -> w.Write(v)) value

    let readIntOption (reader: BinaryReader) =
        readOption reader (fun (r: BinaryReader) -> r.ReadInt32())

    let writeInt64Option (writer: BinaryWriter) (value: int64 option) =
        writeOption writer (fun (w: BinaryWriter) (v: int64) -> w.Write(v)) value

    let readInt64Option (reader: BinaryReader) =
        readOption reader (fun (r: BinaryReader) -> r.ReadInt64())

    let writeStrokeCap (writer: BinaryWriter) (cap: StrokeCap) =
        writer.Write(enumTag cap [ Butt; StrokeCap.Round; Square ])

    let readStrokeCap (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Butt; StrokeCap.Round; Square ] "stroke-cap"

    let writeStrokeJoin (writer: BinaryWriter) (join: StrokeJoin) =
        writer.Write(enumTag join [ Miter; RoundJoin; Bevel ])

    let readStrokeJoin (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Miter; RoundJoin; Bevel ] "stroke-join"

    let writeBlendMode (writer: BinaryWriter) (mode: BlendMode) =
        writer.Write(enumTag mode [ SrcOver; Multiply; Screen; Overlay; Darken; Lighten; ColorDodge; ColorBurn; BlendMode.Difference; Exclusion ])

    let readBlendMode (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ SrcOver; Multiply; Screen; Overlay; Darken; Lighten; ColorDodge; ColorBurn; BlendMode.Difference; Exclusion ] "blend-mode"

    let writeStroke (writer: BinaryWriter) (stroke: Stroke) =
        writer.Write(stroke.Width)
        writeStrokeCap writer stroke.Cap
        writeStrokeJoin writer stroke.Join
        writer.Write(stroke.Miter)

    let readStroke (reader: BinaryReader) : Stroke =
        { Width = reader.ReadDouble()
          Cap = readStrokeCap reader
          Join = readStrokeJoin reader
          Miter = reader.ReadDouble() }

    let rec writeShader (writer: BinaryWriter) (shader: Shader) =
        match shader with
        | SolidColor color ->
            writer.Write(0)
            writeColor writer color
        | LinearGradient(startPoint, endPoint, colors) ->
            writer.Write(1)
            writePoint writer startPoint
            writePoint writer endPoint
            writeList writer writeColor colors
        | RadialGradient(center, radius, colors) ->
            writer.Write(2)
            writePoint writer center
            writer.Write(radius)
            writeList writer writeColor colors
        | SweepGradient(center, colors) ->
            writer.Write(3)
            writePoint writer center
            writeList writer writeColor colors

    and readShader (reader: BinaryReader) : Shader =
        match reader.ReadInt32() with
        | 0 -> SolidColor(readColor reader)
        | 1 ->
            let startPoint = readPoint reader
            let endPoint = readPoint reader
            let colors = readList reader readColor
            LinearGradient(startPoint, endPoint, colors)
        | 2 ->
            let center = readPoint reader
            let radius = reader.ReadDouble()
            let colors = readList reader readColor
            RadialGradient(center, radius, colors)
        | 3 ->
            let center = readPoint reader
            let colors = readList reader readColor
            SweepGradient(center, colors)
        | tag -> failwithf "Unknown shader tag %d" tag

    let writeColorFilter (writer: BinaryWriter) (filter: ColorFilter) =
        match filter with
        | NoColorFilter -> writer.Write(0)
        | BlendColor(color, mode) ->
            writer.Write(1)
            writeColor writer color
            writeBlendMode writer mode

    let readColorFilter (reader: BinaryReader) =
        match reader.ReadInt32() with
        | 0 -> NoColorFilter
        | 1 ->
            let color = readColor reader
            let mode = readBlendMode reader
            BlendColor(color, mode)
        | tag -> failwithf "Unknown color-filter tag %d" tag

    let writeMaskFilter (writer: BinaryWriter) (filter: MaskFilter) =
        match filter with
        | NoMaskFilter -> writer.Write(0)
        | Blur sigma ->
            writer.Write(1)
            writer.Write(sigma)

    let readMaskFilter (reader: BinaryReader) =
        match reader.ReadInt32() with
        | 0 -> NoMaskFilter
        | 1 -> Blur(reader.ReadDouble())
        | tag -> failwithf "Unknown mask-filter tag %d" tag

    let writeImageFilter (writer: BinaryWriter) (filter: ImageFilter) =
        match filter with
        | NoImageFilter -> writer.Write(0)
        | DropShadow(dx, dy, blur, color) ->
            writer.Write(1)
            writer.Write(dx)
            writer.Write(dy)
            writer.Write(blur)
            writeColor writer color

    let readImageFilter (reader: BinaryReader) =
        match reader.ReadInt32() with
        | 0 -> NoImageFilter
        | 1 ->
            let dx = reader.ReadDouble()
            let dy = reader.ReadDouble()
            let blur = reader.ReadDouble()
            let color = readColor reader
            DropShadow(dx, dy, blur, color)
        | tag -> failwithf "Unknown image-filter tag %d" tag

    let writePathEffect (writer: BinaryWriter) (effect: PathEffect) =
        match effect with
        | NoPathEffect -> writer.Write(0)
        | Dash(intervals, phase) ->
            writer.Write(1)
            writeList writer (fun (w: BinaryWriter) (v: float) -> w.Write(v)) intervals
            writer.Write(phase)
        | Discrete(segmentLength, deviation) ->
            writer.Write(2)
            writer.Write(segmentLength)
            writer.Write(deviation)
        | Corner radius ->
            writer.Write(3)
            writer.Write(radius)

    let readPathEffect (reader: BinaryReader) =
        match reader.ReadInt32() with
        | 0 -> NoPathEffect
        | 1 ->
            let intervals = readList reader (fun (r: BinaryReader) -> r.ReadDouble())
            let phase = reader.ReadDouble()
            Dash(intervals, phase)
        | 2 ->
            let segmentLength = reader.ReadDouble()
            let deviation = reader.ReadDouble()
            Discrete(segmentLength, deviation)
        | 3 -> Corner(reader.ReadDouble())
        | tag -> failwithf "Unknown path-effect tag %d" tag

    let writePaint (writer: BinaryWriter) (paint: Paint) =
        writeOption writer writeColor paint.Fill
        writeOption writer writeStroke paint.Stroke
        writer.Write(paint.Opacity)
        writer.Write(paint.Antialias)
        writeBlendMode writer paint.BlendMode
        writeOption writer writeShader paint.Shader
        writeColorFilter writer paint.ColorFilter
        writeMaskFilter writer paint.MaskFilter
        writeImageFilter writer paint.ImageFilter
        writePathEffect writer paint.PathEffect

    let readPaint (reader: BinaryReader) : Paint =
        { Fill = readOption reader readColor
          Stroke = readOption reader readStroke
          Opacity = reader.ReadDouble()
          Antialias = reader.ReadBoolean()
          BlendMode = readBlendMode reader
          Shader = readOption reader readShader
          ColorFilter = readColorFilter reader
          MaskFilter = readMaskFilter reader
          ImageFilter = readImageFilter reader
          PathEffect = readPathEffect reader }

    let writePathFillType (writer: BinaryWriter) (fillType: PathFillType) =
        writer.Write(enumTag fillType [ Winding; EvenOdd ])

    let readPathFillType (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Winding; EvenOdd ] "path-fill-type"

    let writePathCommand (writer: BinaryWriter) (command: PathCommand) =
        match command with
        | MoveTo point ->
            writer.Write(0)
            writePoint writer point
        | LineTo point ->
            writer.Write(1)
            writePoint writer point
        | QuadTo(control, point) ->
            writer.Write(2)
            writePoint writer control
            writePoint writer point
        | CubicTo(control1, control2, point) ->
            writer.Write(3)
            writePoint writer control1
            writePoint writer control2
            writePoint writer point
        | ArcTo(bounds, startAngle, sweepAngle) ->
            writer.Write(4)
            writeRect writer bounds
            writer.Write(startAngle)
            writer.Write(sweepAngle)
        | Close -> writer.Write(5)

    let readPathCommand (reader: BinaryReader) : PathCommand =
        match reader.ReadInt32() with
        | 0 -> MoveTo(readPoint reader)
        | 1 -> LineTo(readPoint reader)
        | 2 ->
            let control = readPoint reader
            let point = readPoint reader
            QuadTo(control, point)
        | 3 ->
            let control1 = readPoint reader
            let control2 = readPoint reader
            let point = readPoint reader
            CubicTo(control1, control2, point)
        | 4 ->
            let bounds = readRect reader
            let startAngle = reader.ReadDouble()
            let sweepAngle = reader.ReadDouble()
            ArcTo(bounds, startAngle, sweepAngle)
        | 5 -> Close
        | tag -> failwithf "Unknown path-command tag %d" tag

    let writePathSpec (writer: BinaryWriter) (path: PathSpec) =
        writeList writer writePathCommand path.Commands
        writePathFillType writer path.FillType

    let readPathSpec (reader: BinaryReader) : PathSpec =
        { Commands = readList reader readPathCommand
          FillType = readPathFillType reader }

    let writeClip (writer: BinaryWriter) (clip: Clip) =
        match clip with
        | RectClip bounds ->
            writer.Write(0)
            writeRect writer bounds
        | PathClip path ->
            writer.Write(1)
            writePathSpec writer path

    let readClip (reader: BinaryReader) : Clip =
        match reader.ReadInt32() with
        | 0 -> RectClip(readRect reader)
        | 1 -> PathClip(readPathSpec reader)
        | tag -> failwithf "Unknown clip tag %d" tag

    let writeRegionOperation (writer: BinaryWriter) (operation: RegionOperation) =
        writer.Write(enumTag operation [ Replace; RegionUnion; RegionIntersect; RegionDifference ])

    let readRegionOperation (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Replace; RegionUnion; RegionIntersect; RegionDifference ] "region-operation"

    let writeRegion (writer: BinaryWriter) (region: Region) =
        writeList writer writeRect region.Bounds
        writeRegionOperation writer region.Operation

    let readRegion (reader: BinaryReader) : Region =
        { Bounds = readList reader readRect
          Operation = readRegionOperation reader }

    let writeColorSpace (writer: BinaryWriter) (colorSpace: ColorSpace) =
        writer.Write(enumTag colorSpace [ Srgb; DisplayP3; AdobeRgb ])

    let readColorSpace (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Srgb; DisplayP3; AdobeRgb ] "color-space"

    let writePerspectiveTransform (writer: BinaryWriter) (transform: PerspectiveTransform) =
        writer.Write(transform.M11)
        writer.Write(transform.M12)
        writer.Write(transform.M13)
        writer.Write(transform.M21)
        writer.Write(transform.M22)
        writer.Write(transform.M23)
        writer.Write(transform.M31)
        writer.Write(transform.M32)
        writer.Write(transform.M33)

    let readPerspectiveTransform (reader: BinaryReader) : PerspectiveTransform =
        { M11 = reader.ReadDouble()
          M12 = reader.ReadDouble()
          M13 = reader.ReadDouble()
          M21 = reader.ReadDouble()
          M22 = reader.ReadDouble()
          M23 = reader.ReadDouble()
          M31 = reader.ReadDouble()
          M32 = reader.ReadDouble()
          M33 = reader.ReadDouble() }

    let writeFontSpec (writer: BinaryWriter) (font: FontSpec) =
        writeStringOption writer font.Family
        writer.Write(font.Size)
        writeIntOption writer font.Weight

    let readFontSpec (reader: BinaryReader) : FontSpec =
        { Family = readStringOption reader
          Size = reader.ReadDouble()
          Weight = readIntOption reader }

    let writeTextDirection (writer: BinaryWriter) (direction: TextDirection) =
        writer.Write(enumTag direction [ AutoDirection; LeftToRight; RightToLeft; MixedDirection ])

    let readTextDirection (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ AutoDirection; LeftToRight; RightToLeft; MixedDirection ] "text-direction"

    let writeTextScript (writer: BinaryWriter) (script: TextScript) =
        writer.Write(enumTag script [ AutoScript; LatinScript; ArabicScript; DevanagariScript; ThaiScript; EmojiScript; SymbolScript; MixedScript; UnknownScript ])

    let readTextScript (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ AutoScript; LatinScript; ArabicScript; DevanagariScript; ThaiScript; EmojiScript; SymbolScript; MixedScript; UnknownScript ] "text-script"

    let writeProviderAvailability (writer: BinaryWriter) (availability: ShapingProviderAvailability) =
        writer.Write(enumTag availability [ ProviderInstalled; ProviderCleared; ProviderUnavailable; ProviderFailed ])

    let readProviderAvailability (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ ProviderInstalled; ProviderCleared; ProviderUnavailable; ProviderFailed ] "provider-availability"

    let writeProviderEvidence (writer: BinaryWriter) (provider: ShapingProviderEvidence) =
        writeProviderAvailability writer provider.Availability
        writeString writer provider.ProviderId
        writeString writer provider.VersionBucket
        writeStringOption writer provider.Failure

    let readProviderEvidence (reader: BinaryReader) : ShapingProviderEvidence =
        { Availability = readProviderAvailability reader
          ProviderId = readString reader
          VersionBucket = readString reader
          Failure = readStringOption reader }

    let writeFallbackDecision (writer: BinaryWriter) (decision: TextFallbackDecision) =
        match decision with
        | AuthoredFace family ->
            writer.Write(0)
            writeString writer family
        | SubstitutedFace(requested, resolved) ->
            writer.Write(1)
            writeString writer requested
            writeString writer resolved
        | MissingGlyphs sourceText ->
            writer.Write(2)
            writeString writer sourceText
        | PureFallback -> writer.Write(3)
        | ProviderFailure message ->
            writer.Write(4)
            writeString writer message

    let readFallbackDecision (reader: BinaryReader) =
        match reader.ReadInt32() with
        | 0 -> AuthoredFace(readString reader)
        | 1 ->
            let requested = readString reader
            let resolved = readString reader
            SubstitutedFace(requested, resolved)
        | 2 -> MissingGlyphs(readString reader)
        | 3 -> PureFallback
        | 4 -> ProviderFailure(readString reader)
        | tag -> failwithf "Unknown fallback-decision tag %d" tag

    let writeShapedGlyph (writer: BinaryWriter) (glyph: ShapedGlyph) =
        writer.Write(glyph.GlyphId)
        writer.Write(glyph.SourceCluster)
        writeString writer glyph.SourceText
        writeStringOption writer glyph.ResolvedFace
        writer.Write(glyph.Advance)
        writePoint writer glyph.Offset
        writePoint writer glyph.Position
        writer.Write(glyph.Missing)

    let readShapedGlyph (reader: BinaryReader) : ShapedGlyph =
        { GlyphId = reader.ReadInt32()
          SourceCluster = reader.ReadInt32()
          SourceText = readString reader
          ResolvedFace = readStringOption reader
          Advance = reader.ReadDouble()
          Offset = readPoint reader
          Position = readPoint reader
          Missing = reader.ReadBoolean() }

    let writeTextShapeRun (writer: BinaryWriter) (run: TextShapeRun) =
        let startIndex, length = run.TextRange
        writer.Write(startIndex)
        writer.Write(length)
        writeString writer run.SourceText
        writeStringOption writer run.ResolvedFont
        writeTextDirection writer run.Direction
        writeTextScript writer run.Script
        writeFallbackDecision writer run.FallbackDecision
        writeList writer writeShapedGlyph run.Glyphs
        writer.Write(run.Advance)
        writeList writer writeString run.Diagnostics

    let readTextShapeRun (reader: BinaryReader) : TextShapeRun =
        let startIndex = reader.ReadInt32()
        let length = reader.ReadInt32()
        { TextRange = (startIndex, length)
          SourceText = readString reader
          ResolvedFont = readStringOption reader
          Direction = readTextDirection reader
          Script = readTextScript reader
          FallbackDecision = readFallbackDecision reader
          Glyphs = readList reader readShapedGlyph
          Advance = reader.ReadDouble()
          Diagnostics = readList reader readString }

    let writeFallbackMode (writer: BinaryWriter) (mode: ShapedTextFallbackMode) =
        writer.Write(enumTag mode [ Shaped; PureFallbackMode; ProviderUnavailableFallback; ShapingFailedFallback ])

    let readFallbackMode (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Shaped; PureFallbackMode; ProviderUnavailableFallback; ShapingFailedFallback ] "shaped-text-fallback-mode"

    let writeGlyphRunGlyph (writer: BinaryWriter) (glyph: GlyphRunGlyph) =
        writer.Write(glyph.GlyphId)
        writeString writer glyph.SourceText
        writer.Write(glyph.Advance)
        writePoint writer glyph.Offset
        writer.Write(glyph.Cluster)
        writePoint writer glyph.Position
        writeStringOption writer glyph.ResolvedFace
        writer.Write(glyph.Missing)

    let readGlyphRunGlyph (reader: BinaryReader) : GlyphRunGlyph =
        { GlyphId = reader.ReadInt32()
          SourceText = readString reader
          Advance = reader.ReadDouble()
          Offset = readPoint reader
          Cluster = reader.ReadInt32()
          Position = readPoint reader
          ResolvedFace = readStringOption reader
          Missing = reader.ReadBoolean() }

    let writeGlyphRunMetrics (writer: BinaryWriter) (metrics: GlyphRunMetrics) =
        writer.Write(metrics.Advance)
        writer.Write(metrics.Height)
        writer.Write(metrics.Baseline)

    let readGlyphRunMetrics (reader: BinaryReader) : GlyphRunMetrics =
        { Advance = reader.ReadDouble()
          Height = reader.ReadDouble()
          Baseline = reader.ReadDouble() }

    let writeGlyphRunData (writer: BinaryWriter) (data: GlyphRunData) =
        writeString writer data.Text
        writeFontSpec writer data.Font
        writeProviderEvidence writer data.Provider
        writeList writer writeTextShapeRun data.Runs
        writeList writer writeGlyphRunGlyph data.Glyphs
        writeGlyphRunMetrics writer data.Metrics
        writeString writer data.Fingerprint
        writeFallbackMode writer data.FallbackMode
        writeList writer writeString data.FallbackDiagnostics

    let readGlyphRunData (reader: BinaryReader) : GlyphRunData =
        { Text = readString reader
          Font = readFontSpec reader
          Provider = readProviderEvidence reader
          Runs = readList reader readTextShapeRun
          Glyphs = readList reader readGlyphRunGlyph
          Metrics = readGlyphRunMetrics reader
          Fingerprint = readString reader
          FallbackMode = readFallbackMode reader
          FallbackDiagnostics = readList reader readString }

    let writeTextRun (writer: BinaryWriter) (run: TextRun) =
        writeString writer run.Text
        writePoint writer run.Position
        writeFontSpec writer run.Font
        writePaint writer run.Paint

    let readTextRun (reader: BinaryReader) : TextRun =
        { Text = readString reader
          Position = readPoint reader
          Font = readFontSpec reader
          Paint = readPaint reader }

    let writeVertexMode (writer: BinaryWriter) (mode: VertexMode) =
        writer.Write(enumTag mode [ Triangles; TriangleStrip; TriangleFan ])

    let readVertexMode (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Triangles; TriangleStrip; TriangleFan ] "vertex-mode"

    let writeVertex (writer: BinaryWriter) (vertex: Vertex) =
        writePoint writer vertex.Position
        writeOption writer writeColor vertex.Color

    let readVertex (reader: BinaryReader) : Vertex =
        { Position = readPoint reader
          Color = readOption reader readColor }

    /// Feature 183 (US2 / FR-002): one row per `SceneNode` case driving the **read** side of the frozen
    /// wire format. The write side stays an exhaustive `match node` (`writeSceneNode` below) — `FS0025`
    /// (incomplete match) escalated to error is the compile-time half of the symmetry gate; this table
    /// is the read-side half. Together with the every-case round-trip test (`Feature183` in Scene.Tests:
    /// 25 rows, contiguous tags 0..24, `deserialize (serialize x) = x` + bytes == baseline) a new case
    /// is forced to add a write arm (compile) and a row (test). The `Read` closures read **payload only**
    /// (the tag is consumed by the driver). Wire format frozen: tags 0..24, field order, encodings.
    type SceneNodeCodecRow =
        { Tag: int
          Read: BinaryReader -> SceneNode }

    let rec writeScene (writer: BinaryWriter) (scene: Scene) =
        writeList writer writeSceneNode scene.Nodes

    and readScene (reader: BinaryReader) : Scene =
        { Nodes = readList reader readSceneNode }

    and writeSceneNode (writer: BinaryWriter) (node: SceneNode) =
        match node with
        | Empty -> writer.Write(0)
        | Group scenes ->
            writer.Write(1)
            writeList writer writeScene scenes
        | Rectangle((x, y, width, height), fill) ->
            writer.Write(2)
            writer.Write(x)
            writer.Write(y)
            writer.Write(width)
            writer.Write(height)
            writeColor writer fill
        | PaintedRectangle(bounds, paint) ->
            writer.Write(3)
            writeRect writer bounds
            writePaint writer paint
        | Circle(center, radius, fill) ->
            writer.Write(4)
            writePoint writer center
            writer.Write(radius)
            writeColor writer fill
        | FilledEllipse(bounds, fill) ->
            writer.Write(5)
            writeRect writer bounds
            writeColor writer fill
        | Ellipse(bounds, paint) ->
            writer.Write(6)
            writeRect writer bounds
            writePaint writer paint
        | Line(startPoint, endPoint, paint) ->
            writer.Write(7)
            writePoint writer startPoint
            writePoint writer endPoint
            writePaint writer paint
        | SceneNode.Path(path, paint) ->
            writer.Write(8)
            writePathSpec writer path
            writePaint writer paint
        | Points(points, paint) ->
            writer.Write(9)
            writeList writer writePoint points
            writePaint writer paint
        | Vertices(mode, vertices, paint) ->
            writer.Write(10)
            writeVertexMode writer mode
            writeList writer writeVertex vertices
            writePaint writer paint
        | Arc(bounds, startAngle, sweepAngle, paint) ->
            writer.Write(11)
            writeRect writer bounds
            writer.Write(startAngle)
            writer.Write(sweepAngle)
            writePaint writer paint
        | Text((x, y), text, color) ->
            writer.Write(12)
            writer.Write(x)
            writer.Write(y)
            writeString writer text
            writeColor writer color
        | TextRun run ->
            writer.Write(13)
            writeTextRun writer run
        | Image((x, y, width, height), source) ->
            writer.Write(14)
            writer.Write(x)
            writer.Write(y)
            writer.Write(width)
            writer.Write(height)
            writeString writer source
        | ClipNode(clip, scene) ->
            writer.Write(15)
            writeClip writer clip
            writeScene writer scene
        | RegionNode(region, paint) ->
            writer.Write(16)
            writeRegion writer region
            writePaint writer paint
        | ColorSpaceNode(colorSpace, scene) ->
            writer.Write(17)
            writeColorSpace writer colorSpace
            writeScene writer scene
        | PerspectiveNode(transform, scene) ->
            writer.Write(18)
            writePerspectiveTransform writer transform
            writeScene writer scene
        | PictureNode picture ->
            writer.Write(19)
            writeString writer picture.Name
            writeScene writer picture.Scene
        | Chart values ->
            writer.Write(20)
            writeList writer (fun (w: BinaryWriter) (v: float) -> w.Write(v)) values
        | Translate((dx, dy), scene) ->
            writer.Write(21)
            writer.Write(dx)
            writer.Write(dy)
            writeScene writer scene
        | SizedText((x, y), text, size, color) ->
            writer.Write(22)
            writer.Write(x)
            writer.Write(y)
            writeString writer text
            writer.Write(size)
            writeColor writer color
        | GlyphRun run ->
            writer.Write(23)
            writeGlyphRunData writer run.Data
            writePoint writer run.Position
            writePaint writer run.Paint
        | CachedSubtree boundary ->
            writer.Write(24)
            writer.Write(boundary.CacheId)
            writer.Write(boundary.Fingerprint)
            writeScene writer boundary.Scene

    /// The per-case read table (tags 0..24, frozen order). Payload-only readers — the tag is consumed
    /// by `readSceneNode`. Constructed to read each case's bytes in the exact order `writeSceneNode`
    /// emits them, so round-trips are byte-stable.
    and sceneNodeCodec : SceneNodeCodecRow list =
        [ { Tag = 0; Read = fun _ -> Empty }
          { Tag = 1
            Read = fun reader -> Group(readList reader readScene) }
          { Tag = 2
            Read =
                fun reader ->
                    let x = reader.ReadDouble()
                    let y = reader.ReadDouble()
                    let width = reader.ReadDouble()
                    let height = reader.ReadDouble()
                    Rectangle((x, y, width, height), readColor reader) }
          { Tag = 3
            Read =
                fun reader ->
                    let bounds = readRect reader
                    let paint = readPaint reader
                    PaintedRectangle(bounds, paint) }
          { Tag = 4
            Read =
                fun reader ->
                    let center = readPoint reader
                    let radius = reader.ReadDouble()
                    let fill = readColor reader
                    Circle(center, radius, fill) }
          { Tag = 5
            Read =
                fun reader ->
                    let bounds = readRect reader
                    let fill = readColor reader
                    FilledEllipse(bounds, fill) }
          { Tag = 6
            Read =
                fun reader ->
                    let bounds = readRect reader
                    let paint = readPaint reader
                    Ellipse(bounds, paint) }
          { Tag = 7
            Read =
                fun reader ->
                    let startPoint = readPoint reader
                    let endPoint = readPoint reader
                    let paint = readPaint reader
                    Line(startPoint, endPoint, paint) }
          { Tag = 8
            Read =
                fun reader ->
                    let path = readPathSpec reader
                    let paint = readPaint reader
                    SceneNode.Path(path, paint) }
          { Tag = 9
            Read =
                fun reader ->
                    let points = readList reader readPoint
                    let paint = readPaint reader
                    Points(points, paint) }
          { Tag = 10
            Read =
                fun reader ->
                    let mode = readVertexMode reader
                    let vertices = readList reader readVertex
                    let paint = readPaint reader
                    Vertices(mode, vertices, paint) }
          { Tag = 11
            Read =
                fun reader ->
                    let bounds = readRect reader
                    let startAngle = reader.ReadDouble()
                    let sweepAngle = reader.ReadDouble()
                    let paint = readPaint reader
                    Arc(bounds, startAngle, sweepAngle, paint) }
          { Tag = 12
            Read =
                fun reader ->
                    let x = reader.ReadDouble()
                    let y = reader.ReadDouble()
                    let text = readString reader
                    let color = readColor reader
                    Text((x, y), text, color) }
          { Tag = 13; Read = fun reader -> TextRun(readTextRun reader) }
          { Tag = 14
            Read =
                fun reader ->
                    let x = reader.ReadDouble()
                    let y = reader.ReadDouble()
                    let width = reader.ReadDouble()
                    let height = reader.ReadDouble()
                    let source = readString reader
                    Image((x, y, width, height), source) }
          { Tag = 15
            Read =
                fun reader ->
                    let clip = readClip reader
                    let scene = readScene reader
                    ClipNode(clip, scene) }
          { Tag = 16
            Read =
                fun reader ->
                    let region = readRegion reader
                    let paint = readPaint reader
                    RegionNode(region, paint) }
          { Tag = 17
            Read =
                fun reader ->
                    let colorSpace = readColorSpace reader
                    let scene = readScene reader
                    ColorSpaceNode(colorSpace, scene) }
          { Tag = 18
            Read =
                fun reader ->
                    let transform = readPerspectiveTransform reader
                    let scene = readScene reader
                    PerspectiveNode(transform, scene) }
          { Tag = 19
            Read =
                fun reader ->
                    let name = readString reader
                    let scene = readScene reader
                    PictureNode { Name = name; Scene = scene } }
          { Tag = 20
            Read = fun reader -> Chart(readList reader (fun (r: BinaryReader) -> r.ReadDouble())) }
          { Tag = 21
            Read =
                fun reader ->
                    let dx = reader.ReadDouble()
                    let dy = reader.ReadDouble()
                    let scene = readScene reader
                    Translate((dx, dy), scene) }
          { Tag = 22
            Read =
                fun reader ->
                    let x = reader.ReadDouble()
                    let y = reader.ReadDouble()
                    let text = readString reader
                    let size = reader.ReadDouble()
                    let color = readColor reader
                    SizedText((x, y), text, size, color) }
          { Tag = 23
            Read =
                fun reader ->
                    let data = readGlyphRunData reader
                    let position = readPoint reader
                    let paint = readPaint reader
                    GlyphRun { Data = data; Position = position; Paint = paint } }
          { Tag = 24
            Read =
                fun reader ->
                    let cacheId = reader.ReadUInt64()
                    let fingerprint = reader.ReadUInt64()
                    let scene = readScene reader
                    CachedSubtree { CacheId = cacheId; Fingerprint = fingerprint; Scene = scene } } ]

    and readerByTag : Map<int, BinaryReader -> SceneNode> =
        sceneNodeCodec |> List.map (fun row -> row.Tag, row.Read) |> Map.ofList

    and readSceneNode (reader: BinaryReader) : SceneNode =
        let tag = reader.ReadInt32()

        match Map.tryFind tag readerByTag with
        | Some read -> read reader
        // Retained ONLY for genuinely-unknown/corrupt tags — never as a stand-in for a missing case
        // (a missing case is caught by the round-trip symmetry test, FR-002).
        | None -> failwithf "Unknown scene-node tag %d" tag
