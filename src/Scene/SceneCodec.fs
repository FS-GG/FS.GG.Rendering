namespace FS.GG.UI.Scene

open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography
open System.Text

type ProtocolVersion =
    { Major: int
      Minor: int }

type RequirementLevel =
    | Required
    | Optional

type DegradationPolicy =
    | Reject
    | Degrade
    | Ignore

type CapabilityRequirement =
    { CapabilityId: string
      RequirementLevel: RequirementLevel
      DegradationPolicy: DegradationPolicy
      AffectedScenePaths: string list }

type ResourceKind =
    | ImageResource
    | FontResource
    | BinaryResource of string

type ResourceEntry =
    { ResourceId: string
      Kind: ResourceKind
      ContentHash: string
      ByteLength: int64 option
      Required: bool
      MediaType: string option
      SourceLabel: string option }

type ResourceAvailabilityStatus =
    | ResourceAvailable
    | ResourceMissing
    | ResourceCorrupted
    | ResourceHashMismatch
    | ResourceDuplicated

type ResourceAvailability =
    { ResourceId: string
      Kind: ResourceKind option
      ContentHash: string option
      ByteLength: int64 option
      Status: ResourceAvailabilityStatus }

type TargetCapabilityProfile =
    { ProfileId: string
      SupportedCapabilities: string list }

type PackageExportOptions =
    { ProfileId: string
      Resources: ResourceEntry list
      OptionalCapabilities: string list }

type PackageInspectionOptions =
    { TargetProfile: TargetCapabilityProfile option
      Resources: ResourceAvailability list }

type PackageDiagnosticStage =
    | Parse
    | Version
    | Capability
    | Resource
    | ScenePayload
    | TextPayload

type PackageDiagnostic =
    { Severity: DiagnosticSeverity
      Stage: PackageDiagnosticStage
      Message: string
      ScenePath: string option
      CapabilityId: string option
      ResourceId: string option }

type PackageInspectionStatus =
    | PackageAccepted
    | PackageAcceptedWithDegradation
    | PackageRejected

type CapabilityVerdict =
    { Requirement: CapabilityRequirement
      Supported: bool
      Degraded: bool
      Diagnostics: PackageDiagnostic list }

type ResourceVerdict =
    { Entry: ResourceEntry
      Availability: ResourceAvailability option
      Accepted: bool
      Degraded: bool
      Diagnostics: PackageDiagnostic list }

type PortableScenePackage =
    { Version: ProtocolVersion
      ProfileId: string
      Capabilities: CapabilityRequirement list
      Resources: ResourceEntry list
      Scene: Scene
      CanonicalBytes: byte[]
      PackageIdentity: string
      Diagnostics: PackageDiagnostic list }

type PackageInspectionReport =
    { Status: PackageInspectionStatus
      PackageIdentity: string option
      Version: ProtocolVersion option
      ProfileId: string option
      CapabilityVerdicts: CapabilityVerdict list
      ResourceVerdicts: ResourceVerdict list
      Diagnostics: PackageDiagnostic list }

type SemanticComparison =
    { Equivalent: bool
      ExpectedCapabilities: SceneElementKind list
      ActualCapabilities: SceneElementKind list
      Diagnostics: PackageDiagnostic list }

module SceneCodec =
    let magicHeader = "FSGGSCENE"
    let supportedVersion = { Major = 1; Minor = 0 }

    let defaultExportOptions =
        { ProfileId = "scene-portable/v1"
          Resources = []
          OptionalCapabilities = [] }

    let defaultInspectionOptions =
        { TargetProfile = None
          Resources = [] }

    let private requiredProfileTag = 1
    let private requiredCapabilitiesTag = 2
    let private requiredResourcesTag = 3
    let private requiredSceneTag = 4

    // Tag table:
    // 1 required profile id, 2 required capability profile, 3 required resource manifest,
    // 4 required scene payload. Tags 1..999 are required; tags >=1000 are length-skippable optional.

    let private diagnostic
        (severity: DiagnosticSeverity)
        (stage: PackageDiagnosticStage)
        (message: string)
        (scenePath: string option)
        (capabilityId: string option)
        (resourceId: string option)
        : PackageDiagnostic =
        { Severity = severity
          Stage = stage
          Message = message
          ScenePath = scenePath
          CapabilityId = capabilityId
          ResourceId = resourceId }

    let private error (stage: PackageDiagnosticStage) (message: string) =
        diagnostic Error stage message None None None

    let private warning (stage: PackageDiagnosticStage) (message: string) =
        diagnostic Warning stage message None None None

    let private sha256Hex (bytes: byte[]) =
        SHA256.HashData bytes |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

    let packageIdentity (bytes: byte[]) =
        "sha256:" + sha256Hex bytes

    let private stringHash (value: string) =
        value |> Encoding.UTF8.GetBytes |> sha256Hex

    let private enumTag value cases =
        cases |> List.findIndex ((=) value)

    let private readEnum tag cases label =
        cases
        |> List.tryItem tag
        |> Option.defaultWith (fun () -> failwithf "Unknown %s tag %d" label tag)

    let private writeString (writer: BinaryWriter) (value: string) =
        let bytes = Encoding.UTF8.GetBytes(value)
        writer.Write(bytes.Length)
        writer.Write(bytes)

    let private readString (reader: BinaryReader) =
        let length = reader.ReadInt32()
        if length < 0 then failwith "Negative string length"
        Encoding.UTF8.GetString(reader.ReadBytes(length))

    let private writeOption (writer: BinaryWriter) (writeValue: BinaryWriter -> 'a -> unit) (value: 'a option) =
        match value with
        | Some item ->
            writer.Write(true)
            writeValue writer item
        | None -> writer.Write(false)

    let private readOption (reader: BinaryReader) (readValue: BinaryReader -> 'a) : 'a option =
        if reader.ReadBoolean() then Some(readValue reader) else None

    let private writeList (writer: BinaryWriter) (writeValue: BinaryWriter -> 'a -> unit) (values: 'a list) =
        writer.Write(List.length values)
        values |> List.iter (writeValue writer)

    let private readList (reader: BinaryReader) (readValue: BinaryReader -> 'a) : 'a list =
        let length = reader.ReadInt32()
        if length < 0 then failwith "Negative list length"
        [ for _ in 1 .. length -> readValue reader ]

    let private writeColor (writer: BinaryWriter) (color: Color) =
        writer.Write(color.Red)
        writer.Write(color.Green)
        writer.Write(color.Blue)
        writer.Write(color.Alpha)

    let private readColor (reader: BinaryReader) : Color =
        { Red = reader.ReadByte()
          Green = reader.ReadByte()
          Blue = reader.ReadByte()
          Alpha = reader.ReadByte() }

    let private writePoint (writer: BinaryWriter) (point: Point) =
        writer.Write(point.X)
        writer.Write(point.Y)

    let private readPoint (reader: BinaryReader) : Point =
        { X = reader.ReadDouble()
          Y = reader.ReadDouble() }

    let private writeRect (writer: BinaryWriter) (rect: Rect) =
        writer.Write(rect.X)
        writer.Write(rect.Y)
        writer.Write(rect.Width)
        writer.Write(rect.Height)

    let private readRect (reader: BinaryReader) : Rect =
        { X = reader.ReadDouble()
          Y = reader.ReadDouble()
          Width = reader.ReadDouble()
          Height = reader.ReadDouble() }

    let private writeStringOption (writer: BinaryWriter) (value: string option) =
        writeOption writer writeString value

    let private readStringOption (reader: BinaryReader) =
        readOption reader readString

    let private writeIntOption (writer: BinaryWriter) (value: int option) =
        writeOption writer (fun (w: BinaryWriter) (v: int) -> w.Write(v)) value

    let private readIntOption (reader: BinaryReader) =
        readOption reader (fun (r: BinaryReader) -> r.ReadInt32())

    let private writeInt64Option (writer: BinaryWriter) (value: int64 option) =
        writeOption writer (fun (w: BinaryWriter) (v: int64) -> w.Write(v)) value

    let private readInt64Option (reader: BinaryReader) =
        readOption reader (fun (r: BinaryReader) -> r.ReadInt64())

    let private writeStrokeCap (writer: BinaryWriter) (cap: StrokeCap) =
        writer.Write(enumTag cap [ Butt; StrokeCap.Round; Square ])

    let private readStrokeCap (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Butt; StrokeCap.Round; Square ] "stroke-cap"

    let private writeStrokeJoin (writer: BinaryWriter) (join: StrokeJoin) =
        writer.Write(enumTag join [ Miter; RoundJoin; Bevel ])

    let private readStrokeJoin (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Miter; RoundJoin; Bevel ] "stroke-join"

    let private writeBlendMode (writer: BinaryWriter) (mode: BlendMode) =
        writer.Write(enumTag mode [ SrcOver; Multiply; Screen; Overlay; Darken; Lighten; ColorDodge; ColorBurn; BlendMode.Difference; Exclusion ])

    let private readBlendMode (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ SrcOver; Multiply; Screen; Overlay; Darken; Lighten; ColorDodge; ColorBurn; BlendMode.Difference; Exclusion ] "blend-mode"

    let private writeStroke (writer: BinaryWriter) (stroke: Stroke) =
        writer.Write(stroke.Width)
        writeStrokeCap writer stroke.Cap
        writeStrokeJoin writer stroke.Join
        writer.Write(stroke.Miter)

    let private readStroke (reader: BinaryReader) : Stroke =
        { Width = reader.ReadDouble()
          Cap = readStrokeCap reader
          Join = readStrokeJoin reader
          Miter = reader.ReadDouble() }

    let rec private writeShader (writer: BinaryWriter) (shader: Shader) =
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

    and private readShader (reader: BinaryReader) : Shader =
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

    let private writeColorFilter (writer: BinaryWriter) (filter: ColorFilter) =
        match filter with
        | NoColorFilter -> writer.Write(0)
        | BlendColor(color, mode) ->
            writer.Write(1)
            writeColor writer color
            writeBlendMode writer mode

    let private readColorFilter (reader: BinaryReader) =
        match reader.ReadInt32() with
        | 0 -> NoColorFilter
        | 1 ->
            let color = readColor reader
            let mode = readBlendMode reader
            BlendColor(color, mode)
        | tag -> failwithf "Unknown color-filter tag %d" tag

    let private writeMaskFilter (writer: BinaryWriter) (filter: MaskFilter) =
        match filter with
        | NoMaskFilter -> writer.Write(0)
        | Blur sigma ->
            writer.Write(1)
            writer.Write(sigma)

    let private readMaskFilter (reader: BinaryReader) =
        match reader.ReadInt32() with
        | 0 -> NoMaskFilter
        | 1 -> Blur(reader.ReadDouble())
        | tag -> failwithf "Unknown mask-filter tag %d" tag

    let private writeImageFilter (writer: BinaryWriter) (filter: ImageFilter) =
        match filter with
        | NoImageFilter -> writer.Write(0)
        | DropShadow(dx, dy, blur, color) ->
            writer.Write(1)
            writer.Write(dx)
            writer.Write(dy)
            writer.Write(blur)
            writeColor writer color

    let private readImageFilter (reader: BinaryReader) =
        match reader.ReadInt32() with
        | 0 -> NoImageFilter
        | 1 ->
            let dx = reader.ReadDouble()
            let dy = reader.ReadDouble()
            let blur = reader.ReadDouble()
            let color = readColor reader
            DropShadow(dx, dy, blur, color)
        | tag -> failwithf "Unknown image-filter tag %d" tag

    let private writePathEffect (writer: BinaryWriter) (effect: PathEffect) =
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

    let private readPathEffect (reader: BinaryReader) =
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

    let private writePaint (writer: BinaryWriter) (paint: Paint) =
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

    let private readPaint (reader: BinaryReader) : Paint =
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

    let private writePathFillType (writer: BinaryWriter) (fillType: PathFillType) =
        writer.Write(enumTag fillType [ Winding; EvenOdd ])

    let private readPathFillType (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Winding; EvenOdd ] "path-fill-type"

    let private writePathCommand (writer: BinaryWriter) (command: PathCommand) =
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

    let private readPathCommand (reader: BinaryReader) : PathCommand =
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

    let private writePathSpec (writer: BinaryWriter) (path: PathSpec) =
        writeList writer writePathCommand path.Commands
        writePathFillType writer path.FillType

    let private readPathSpec (reader: BinaryReader) : PathSpec =
        { Commands = readList reader readPathCommand
          FillType = readPathFillType reader }

    let private writeClip (writer: BinaryWriter) (clip: Clip) =
        match clip with
        | RectClip bounds ->
            writer.Write(0)
            writeRect writer bounds
        | PathClip path ->
            writer.Write(1)
            writePathSpec writer path

    let private readClip (reader: BinaryReader) : Clip =
        match reader.ReadInt32() with
        | 0 -> RectClip(readRect reader)
        | 1 -> PathClip(readPathSpec reader)
        | tag -> failwithf "Unknown clip tag %d" tag

    let private writeRegionOperation (writer: BinaryWriter) (operation: RegionOperation) =
        writer.Write(enumTag operation [ Replace; RegionUnion; RegionIntersect; RegionDifference ])

    let private readRegionOperation (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Replace; RegionUnion; RegionIntersect; RegionDifference ] "region-operation"

    let private writeRegion (writer: BinaryWriter) (region: Region) =
        writeList writer writeRect region.Bounds
        writeRegionOperation writer region.Operation

    let private readRegion (reader: BinaryReader) : Region =
        { Bounds = readList reader readRect
          Operation = readRegionOperation reader }

    let private writeColorSpace (writer: BinaryWriter) (colorSpace: ColorSpace) =
        writer.Write(enumTag colorSpace [ Srgb; DisplayP3; AdobeRgb ])

    let private readColorSpace (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Srgb; DisplayP3; AdobeRgb ] "color-space"

    let private writePerspectiveTransform (writer: BinaryWriter) (transform: PerspectiveTransform) =
        writer.Write(transform.M11)
        writer.Write(transform.M12)
        writer.Write(transform.M13)
        writer.Write(transform.M21)
        writer.Write(transform.M22)
        writer.Write(transform.M23)
        writer.Write(transform.M31)
        writer.Write(transform.M32)
        writer.Write(transform.M33)

    let private readPerspectiveTransform (reader: BinaryReader) : PerspectiveTransform =
        { M11 = reader.ReadDouble()
          M12 = reader.ReadDouble()
          M13 = reader.ReadDouble()
          M21 = reader.ReadDouble()
          M22 = reader.ReadDouble()
          M23 = reader.ReadDouble()
          M31 = reader.ReadDouble()
          M32 = reader.ReadDouble()
          M33 = reader.ReadDouble() }

    let private writeFontSpec (writer: BinaryWriter) (font: FontSpec) =
        writeStringOption writer font.Family
        writer.Write(font.Size)
        writeIntOption writer font.Weight

    let private readFontSpec (reader: BinaryReader) : FontSpec =
        { Family = readStringOption reader
          Size = reader.ReadDouble()
          Weight = readIntOption reader }

    let private writeTextDirection (writer: BinaryWriter) (direction: TextDirection) =
        writer.Write(enumTag direction [ AutoDirection; LeftToRight; RightToLeft; MixedDirection ])

    let private readTextDirection (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ AutoDirection; LeftToRight; RightToLeft; MixedDirection ] "text-direction"

    let private writeTextScript (writer: BinaryWriter) (script: TextScript) =
        writer.Write(enumTag script [ AutoScript; LatinScript; ArabicScript; DevanagariScript; ThaiScript; EmojiScript; SymbolScript; MixedScript; UnknownScript ])

    let private readTextScript (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ AutoScript; LatinScript; ArabicScript; DevanagariScript; ThaiScript; EmojiScript; SymbolScript; MixedScript; UnknownScript ] "text-script"

    let private writeProviderAvailability (writer: BinaryWriter) (availability: ShapingProviderAvailability) =
        writer.Write(enumTag availability [ ProviderInstalled; ProviderCleared; ProviderUnavailable; ProviderFailed ])

    let private readProviderAvailability (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ ProviderInstalled; ProviderCleared; ProviderUnavailable; ProviderFailed ] "provider-availability"

    let private writeProviderEvidence (writer: BinaryWriter) (provider: ShapingProviderEvidence) =
        writeProviderAvailability writer provider.Availability
        writeString writer provider.ProviderId
        writeString writer provider.VersionBucket
        writeStringOption writer provider.Failure

    let private readProviderEvidence (reader: BinaryReader) : ShapingProviderEvidence =
        { Availability = readProviderAvailability reader
          ProviderId = readString reader
          VersionBucket = readString reader
          Failure = readStringOption reader }

    let private writeFallbackDecision (writer: BinaryWriter) (decision: TextFallbackDecision) =
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

    let private readFallbackDecision (reader: BinaryReader) =
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

    let private writeShapedGlyph (writer: BinaryWriter) (glyph: ShapedGlyph) =
        writer.Write(glyph.GlyphId)
        writer.Write(glyph.SourceCluster)
        writeString writer glyph.SourceText
        writeStringOption writer glyph.ResolvedFace
        writer.Write(glyph.Advance)
        writePoint writer glyph.Offset
        writePoint writer glyph.Position
        writer.Write(glyph.Missing)

    let private readShapedGlyph (reader: BinaryReader) : ShapedGlyph =
        { GlyphId = reader.ReadInt32()
          SourceCluster = reader.ReadInt32()
          SourceText = readString reader
          ResolvedFace = readStringOption reader
          Advance = reader.ReadDouble()
          Offset = readPoint reader
          Position = readPoint reader
          Missing = reader.ReadBoolean() }

    let private writeTextShapeRun (writer: BinaryWriter) (run: TextShapeRun) =
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

    let private readTextShapeRun (reader: BinaryReader) : TextShapeRun =
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

    let private writeFallbackMode (writer: BinaryWriter) (mode: ShapedTextFallbackMode) =
        writer.Write(enumTag mode [ Shaped; PureFallbackMode; ProviderUnavailableFallback; ShapingFailedFallback ])

    let private readFallbackMode (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Shaped; PureFallbackMode; ProviderUnavailableFallback; ShapingFailedFallback ] "shaped-text-fallback-mode"

    let private writeGlyphRunGlyph (writer: BinaryWriter) (glyph: GlyphRunGlyph) =
        writer.Write(glyph.GlyphId)
        writeString writer glyph.SourceText
        writer.Write(glyph.Advance)
        writePoint writer glyph.Offset
        writer.Write(glyph.Cluster)
        writePoint writer glyph.Position
        writeStringOption writer glyph.ResolvedFace
        writer.Write(glyph.Missing)

    let private readGlyphRunGlyph (reader: BinaryReader) : GlyphRunGlyph =
        { GlyphId = reader.ReadInt32()
          SourceText = readString reader
          Advance = reader.ReadDouble()
          Offset = readPoint reader
          Cluster = reader.ReadInt32()
          Position = readPoint reader
          ResolvedFace = readStringOption reader
          Missing = reader.ReadBoolean() }

    let private writeGlyphRunMetrics (writer: BinaryWriter) (metrics: GlyphRunMetrics) =
        writer.Write(metrics.Advance)
        writer.Write(metrics.Height)
        writer.Write(metrics.Baseline)

    let private readGlyphRunMetrics (reader: BinaryReader) : GlyphRunMetrics =
        { Advance = reader.ReadDouble()
          Height = reader.ReadDouble()
          Baseline = reader.ReadDouble() }

    let private writeGlyphRunData (writer: BinaryWriter) (data: GlyphRunData) =
        writeString writer data.Text
        writeFontSpec writer data.Font
        writeProviderEvidence writer data.Provider
        writeList writer writeTextShapeRun data.Runs
        writeList writer writeGlyphRunGlyph data.Glyphs
        writeGlyphRunMetrics writer data.Metrics
        writeString writer data.Fingerprint
        writeFallbackMode writer data.FallbackMode
        writeList writer writeString data.FallbackDiagnostics

    let private readGlyphRunData (reader: BinaryReader) : GlyphRunData =
        { Text = readString reader
          Font = readFontSpec reader
          Provider = readProviderEvidence reader
          Runs = readList reader readTextShapeRun
          Glyphs = readList reader readGlyphRunGlyph
          Metrics = readGlyphRunMetrics reader
          Fingerprint = readString reader
          FallbackMode = readFallbackMode reader
          FallbackDiagnostics = readList reader readString }

    let private writeTextRun (writer: BinaryWriter) (run: TextRun) =
        writeString writer run.Text
        writePoint writer run.Position
        writeFontSpec writer run.Font
        writePaint writer run.Paint

    let private readTextRun (reader: BinaryReader) : TextRun =
        { Text = readString reader
          Position = readPoint reader
          Font = readFontSpec reader
          Paint = readPaint reader }

    let private writeVertexMode (writer: BinaryWriter) (mode: VertexMode) =
        writer.Write(enumTag mode [ Triangles; TriangleStrip; TriangleFan ])

    let private readVertexMode (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Triangles; TriangleStrip; TriangleFan ] "vertex-mode"

    let private writeVertex (writer: BinaryWriter) (vertex: Vertex) =
        writePoint writer vertex.Position
        writeOption writer writeColor vertex.Color

    let private readVertex (reader: BinaryReader) : Vertex =
        { Position = readPoint reader
          Color = readOption reader readColor }

    let rec private writeScene (writer: BinaryWriter) (scene: Scene) =
        writeList writer writeSceneNode scene.Nodes

    and private readScene (reader: BinaryReader) : Scene =
        { Nodes = readList reader readSceneNode }

    and private writeSceneNode (writer: BinaryWriter) (node: SceneNode) =
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

    and private readSceneNode (reader: BinaryReader) : SceneNode =
        match reader.ReadInt32() with
        | 0 -> Empty
        | 1 -> Group(readList reader readScene)
        | 2 ->
            let x = reader.ReadDouble()
            let y = reader.ReadDouble()
            let width = reader.ReadDouble()
            let height = reader.ReadDouble()
            Rectangle((x, y, width, height), readColor reader)
        | 3 ->
            let bounds = readRect reader
            let paint = readPaint reader
            PaintedRectangle(bounds, paint)
        | 4 ->
            let center = readPoint reader
            let radius = reader.ReadDouble()
            let fill = readColor reader
            Circle(center, radius, fill)
        | 5 ->
            let bounds = readRect reader
            let fill = readColor reader
            FilledEllipse(bounds, fill)
        | 6 ->
            let bounds = readRect reader
            let paint = readPaint reader
            Ellipse(bounds, paint)
        | 7 ->
            let startPoint = readPoint reader
            let endPoint = readPoint reader
            let paint = readPaint reader
            Line(startPoint, endPoint, paint)
        | 8 ->
            let path = readPathSpec reader
            let paint = readPaint reader
            SceneNode.Path(path, paint)
        | 9 ->
            let points = readList reader readPoint
            let paint = readPaint reader
            Points(points, paint)
        | 10 ->
            let mode = readVertexMode reader
            let vertices = readList reader readVertex
            let paint = readPaint reader
            Vertices(mode, vertices, paint)
        | 11 ->
            let bounds = readRect reader
            let startAngle = reader.ReadDouble()
            let sweepAngle = reader.ReadDouble()
            let paint = readPaint reader
            Arc(bounds, startAngle, sweepAngle, paint)
        | 12 ->
            let x = reader.ReadDouble()
            let y = reader.ReadDouble()
            let text = readString reader
            let color = readColor reader
            Text((x, y), text, color)
        | 13 -> TextRun(readTextRun reader)
        | 14 ->
            let x = reader.ReadDouble()
            let y = reader.ReadDouble()
            let width = reader.ReadDouble()
            let height = reader.ReadDouble()
            let source = readString reader
            Image((x, y, width, height), source)
        | 15 ->
            let clip = readClip reader
            let scene = readScene reader
            ClipNode(clip, scene)
        | 16 ->
            let region = readRegion reader
            let paint = readPaint reader
            RegionNode(region, paint)
        | 17 ->
            let colorSpace = readColorSpace reader
            let scene = readScene reader
            ColorSpaceNode(colorSpace, scene)
        | 18 ->
            let transform = readPerspectiveTransform reader
            let scene = readScene reader
            PerspectiveNode(transform, scene)
        | 19 ->
            let name = readString reader
            let scene = readScene reader
            PictureNode { Name = name; Scene = scene }
        | 20 -> Chart(readList reader (fun (r: BinaryReader) -> r.ReadDouble()))
        | 21 ->
            let dx = reader.ReadDouble()
            let dy = reader.ReadDouble()
            let scene = readScene reader
            Translate((dx, dy), scene)
        | 22 ->
            let x = reader.ReadDouble()
            let y = reader.ReadDouble()
            let text = readString reader
            let size = reader.ReadDouble()
            let color = readColor reader
            SizedText((x, y), text, size, color)
        | 23 ->
            let data = readGlyphRunData reader
            let position = readPoint reader
            let paint = readPaint reader
            GlyphRun { Data = data; Position = position; Paint = paint }
        | 24 ->
            let cacheId = reader.ReadUInt64()
            let fingerprint = reader.ReadUInt64()
            let scene = readScene reader
            CachedSubtree { CacheId = cacheId; Fingerprint = fingerprint; Scene = scene }
        | tag -> failwithf "Unknown scene-node tag %d" tag

    let private writeRequirementLevel (writer: BinaryWriter) (level: RequirementLevel) =
        writer.Write(enumTag level [ Required; Optional ])

    let private readRequirementLevel (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Required; Optional ] "requirement-level"

    let private writeDegradationPolicy (writer: BinaryWriter) (policy: DegradationPolicy) =
        writer.Write(enumTag policy [ Reject; Degrade; Ignore ])

    let private readDegradationPolicy (reader: BinaryReader) =
        readEnum (reader.ReadInt32()) [ Reject; Degrade; Ignore ] "degradation-policy"

    let private writeCapabilityRequirement (writer: BinaryWriter) (requirement: CapabilityRequirement) =
        writeString writer requirement.CapabilityId
        writeRequirementLevel writer requirement.RequirementLevel
        writeDegradationPolicy writer requirement.DegradationPolicy
        writeList writer writeString requirement.AffectedScenePaths

    let private readCapabilityRequirement (reader: BinaryReader) : CapabilityRequirement =
        { CapabilityId = readString reader
          RequirementLevel = readRequirementLevel reader
          DegradationPolicy = readDegradationPolicy reader
          AffectedScenePaths = readList reader readString }

    let private writeResourceKind (writer: BinaryWriter) (kind: ResourceKind) =
        match kind with
        | ImageResource -> writer.Write(0)
        | FontResource -> writer.Write(1)
        | BinaryResource label ->
            writer.Write(2)
            writeString writer label

    let private readResourceKind (reader: BinaryReader) =
        match reader.ReadInt32() with
        | 0 -> ImageResource
        | 1 -> FontResource
        | 2 -> BinaryResource(readString reader)
        | tag -> failwithf "Unknown resource-kind tag %d" tag

    let private writeResourceEntry (writer: BinaryWriter) (entry: ResourceEntry) =
        writeString writer entry.ResourceId
        writeResourceKind writer entry.Kind
        writeString writer entry.ContentHash
        writeInt64Option writer entry.ByteLength
        writer.Write(entry.Required)
        writeStringOption writer entry.MediaType
        writeStringOption writer entry.SourceLabel

    let private readResourceEntry (reader: BinaryReader) : ResourceEntry =
        { ResourceId = readString reader
          Kind = readResourceKind reader
          ContentHash = readString reader
          ByteLength = readInt64Option reader
          Required = reader.ReadBoolean()
          MediaType = readStringOption reader
          SourceLabel = readStringOption reader }

    let private writeSection (writer: BinaryWriter) (tag: int) (writePayload: BinaryWriter -> unit) =
        use stream = new MemoryStream()
        use sectionWriter = new BinaryWriter(stream, Encoding.UTF8, true)
        writePayload sectionWriter
        sectionWriter.Flush()
        let bytes = stream.ToArray()
        writer.Write(tag)
        writer.Write(bytes.Length)
        writer.Write(bytes)

    let private writeEnvelope (profile: string) (capabilities: CapabilityRequirement list) (resources: ResourceEntry list) (scene: Scene) =
        use stream = new MemoryStream()
        use writer = new BinaryWriter(stream, Encoding.UTF8, true)
        writer.Write(Encoding.ASCII.GetBytes magicHeader)
        writer.Write(supportedVersion.Major)
        writer.Write(supportedVersion.Minor)
        writeSection writer requiredProfileTag (fun w -> writeString w profile)
        writeSection writer requiredCapabilitiesTag (fun w -> writeList w writeCapabilityRequirement capabilities)
        writeSection writer requiredResourcesTag (fun w -> writeList w writeResourceEntry resources)
        writeSection writer requiredSceneTag (fun w -> writeScene w scene)
        writer.Flush()
        stream.ToArray()

    let private capabilityId kind =
        match kind with
        | EmptyElement -> "scene.empty"
        | GroupElement -> "scene.group"
        | RectangleElement -> "scene.rectangle"
        | CircleElement -> "scene.circle"
        | EllipseElement -> "scene.ellipse"
        | LineElement -> "scene.line"
        | PathElement -> "scene.path"
        | PointsElement -> "scene.points"
        | VerticesElement -> "scene.vertices"
        | ArcElement -> "scene.arc"
        | TextElement -> "scene.text"
        | TextRunElement -> "scene.text-run"
        | ImageElement -> "scene.image"
        | ClipElement -> "scene.clip"
        | RegionElement -> "scene.region"
        | ColorSpaceElement -> "scene.color-space"
        | PerspectiveElement -> "scene.perspective"
        | PictureElement -> "scene.picture"
        | ChartElement -> "scene.chart"
        | TranslateElement -> "scene.translate"
        | SizedTextElement -> "scene.sized-text"
        | GlyphRunElement -> "scene.glyph-run"

    let private sourceMediaType (source: string) =
        let lower = source.ToLowerInvariant()
        if lower.EndsWith(".png", StringComparison.Ordinal) then Some "image/png"
        elif lower.EndsWith(".jpg", StringComparison.Ordinal) || lower.EndsWith(".jpeg", StringComparison.Ordinal) then Some "image/jpeg"
        elif lower.EndsWith(".webp", StringComparison.Ordinal) then Some "image/webp"
        else Some "application/octet-stream"

    let private autoResource (source: string) (index: int) : ResourceEntry =
        let resourceId = sprintf "image-%04d" index
        { ResourceId = resourceId
          Kind = ImageResource
          ContentHash = "unresolved:" + stringHash source
          ByteLength = None
          Required = true
          MediaType = sourceMediaType source
          SourceLabel = Some source }

    let private normalizeSceneResources (provided: ResourceEntry list) (scene: Scene) : Scene * ResourceEntry list =
        let bySource =
            provided
            |> List.choose (fun entry -> entry.SourceLabel |> Option.map (fun label -> label, entry))
            |> Map.ofList

        // mutable: stable package-local image ids are allocated during a single authored-order walk.
        let allocated = Dictionary<string, ResourceEntry>(StringComparer.Ordinal)
        let mutable nextIndex = 1

        let resourceFor (source: string) : ResourceEntry =
            match Map.tryFind source bySource with
            | Some entry -> entry
            | None ->
                match allocated.TryGetValue source with
                | true, entry -> entry
                | false, _ ->
                    let entry = autoResource source nextIndex
                    nextIndex <- nextIndex + 1
                    allocated[source] <- entry
                    entry

        let rec sceneWithResources (scene: Scene) : Scene =
            { Nodes = scene.Nodes |> List.map nodeWithResources }

        and nodeWithResources (node: SceneNode) : SceneNode =
            match node with
            | Image(bounds, source) ->
                let entry = resourceFor source
                Image(bounds, entry.ResourceId)
            | Group scenes -> Group(scenes |> List.map sceneWithResources)
            | ClipNode(clip, scene) -> ClipNode(clip, sceneWithResources scene)
            | ColorSpaceNode(colorSpace, scene) -> ColorSpaceNode(colorSpace, sceneWithResources scene)
            | PerspectiveNode(transform, scene) -> PerspectiveNode(transform, sceneWithResources scene)
            | PictureNode picture -> PictureNode { picture with Scene = sceneWithResources picture.Scene }
            | Translate(offset, scene) -> Translate(offset, sceneWithResources scene)
            | CachedSubtree boundary -> CachedSubtree { boundary with Scene = sceneWithResources boundary.Scene }
            | other -> other

        let transformed = sceneWithResources scene

        let resources =
            provided @ (allocated.Values |> Seq.toList)
            |> List.distinctBy _.ResourceId
            |> List.sortBy _.ResourceId

        transformed, resources

    let private capabilityPaths (scene: Scene) =
        let add (kind: SceneElementKind) (path: string) (acc: Map<string, string list>) =
            let id = capabilityId kind
            let paths = Map.tryFind id acc |> Option.defaultValue []
            Map.add id (path :: paths) acc

        let rec walkScene (path: string) (acc: Map<string, string list>) (scene: Scene) =
            scene.Nodes
            |> List.mapi (fun index node -> index, node)
            |> List.fold (fun state (index, node) -> walkNode (path + $"/nodes/{index}") state node) acc

        and walkNode (path: string) (acc: Map<string, string list>) (node: SceneNode) =
            match node with
            | Empty -> add EmptyElement path acc
            | Group scenes ->
                scenes
                |> List.mapi (fun index scene -> index, scene)
                |> List.fold (fun state (index, scene) -> walkScene (path + $"/group/{index}") state scene) (add GroupElement path acc)
            | Rectangle _ -> add RectangleElement path acc
            | PaintedRectangle _ -> add RectangleElement path acc
            | Circle _ -> add CircleElement path acc
            | FilledEllipse _ -> add EllipseElement path acc
            | Ellipse _ -> add EllipseElement path acc
            | Line _ -> add LineElement path acc
            | SceneNode.Path _ -> add PathElement path acc
            | Points _ -> add PointsElement path acc
            | Vertices _ -> add VerticesElement path acc
            | Arc _ -> add ArcElement path acc
            | Text _ -> add TextElement path acc
            | TextRun _ -> add TextRunElement path acc
            | Image _ -> add ImageElement path acc
            | ClipNode(_, scene) -> walkScene (path + "/clip") (add ClipElement path acc) scene
            | RegionNode _ -> add RegionElement path acc
            | ColorSpaceNode(_, scene) -> walkScene (path + "/color-space") (add ColorSpaceElement path acc) scene
            | PerspectiveNode(_, scene) -> walkScene (path + "/perspective") (add PerspectiveElement path acc) scene
            | PictureNode picture -> walkScene (path + "/picture") (add PictureElement path acc) picture.Scene
            | Chart _ -> add ChartElement path acc
            | Translate(_, scene) -> walkScene (path + "/translate") (add TranslateElement path acc) scene
            | SizedText _ -> add SizedTextElement path acc
            | GlyphRun _ -> add GlyphRunElement path acc
            | CachedSubtree boundary -> walkScene (path + "/cached") acc boundary.Scene

        walkScene "" Map.empty scene
        |> Map.toList
        |> List.map (fun (id, paths) -> id, List.rev paths)

    let private requirementsFor (optionalCapabilityIds: string list) (scene: Scene) : CapabilityRequirement list =
        let optional = optionalCapabilityIds |> Set.ofList
        let required =
            capabilityPaths scene
            |> List.map (fun (id, paths) ->
                { CapabilityId = id
                  RequirementLevel = if Set.contains id optional then Optional else Required
                  DegradationPolicy = if Set.contains id optional then Degrade else Reject
                  AffectedScenePaths = paths })

        let extras =
            optionalCapabilityIds
            |> List.filter (fun id -> required |> List.exists (fun item -> item.CapabilityId = id) |> not)
            |> List.map (fun id ->
                { CapabilityId = id
                  RequirementLevel = Optional
                  DegradationPolicy = Degrade
                  AffectedScenePaths = [] })

        (required @ extras) |> List.sortBy _.CapabilityId

    let exportScene (options: PackageExportOptions) (scene: Scene) =
        let normalizedScene, resources = normalizeSceneResources options.Resources scene
        let capabilities = requirementsFor options.OptionalCapabilities normalizedScene
        let canonicalBytes = writeEnvelope options.ProfileId capabilities resources normalizedScene

        { Version = supportedVersion
          ProfileId = options.ProfileId
          Capabilities = capabilities
          Resources = resources
          Scene = normalizedScene
          CanonicalBytes = canonicalBytes
          PackageIdentity = packageIdentity canonicalBytes
          Diagnostics = [] }

    let export scene = exportScene defaultExportOptions scene

    type private ParsedSections =
        { ProfileId: string option
          Capabilities: CapabilityRequirement list option
          Resources: ResourceEntry list option
          Scene: Scene option
          Diagnostics: PackageDiagnostic list }

    let private emptySections : ParsedSections =
        { ProfileId = None
          Capabilities = None
          Resources = None
          Scene = None
          Diagnostics = [] }

    let private parseSection (tag: int) (payload: byte[]) (sections: ParsedSections) : ParsedSections =
        use stream = new MemoryStream(payload)
        use reader = new BinaryReader(stream, Encoding.UTF8, true)

        match tag with
        | tag when tag = requiredProfileTag -> { sections with ProfileId = Some(readString reader) }
        | tag when tag = requiredCapabilitiesTag -> { sections with Capabilities = Some(readList reader readCapabilityRequirement) }
        | tag when tag = requiredResourcesTag -> { sections with Resources = Some(readList reader readResourceEntry) }
        | tag when tag = requiredSceneTag -> { sections with Scene = Some(readScene reader) }
        | tag when tag >= 1000 ->
            { sections with
                Diagnostics = warning Parse $"Skipped unknown optional package tag {tag}." :: sections.Diagnostics }
        | tag ->
            failwithf "Unknown required package tag %d" tag

    let importPackage (bytes: byte[]) =
        try
            if bytes.Length < magicHeader.Length + 8 then
                Result.Error [ error Parse "Package is too short to contain a portable scene header." ]
            else
                use stream = new MemoryStream(bytes)
                use reader = new BinaryReader(stream, Encoding.UTF8, true)
                let magic = Encoding.ASCII.GetString(reader.ReadBytes(magicHeader.Length))

                if magic <> magicHeader then
                    Result.Error [ error Parse $"Invalid package magic header '{magic}'." ]
                else
                    let version =
                        { Major = reader.ReadInt32()
                          Minor = reader.ReadInt32() }

                    let mutable sections = emptySections

                    while stream.Position < stream.Length do
                        let tag = reader.ReadInt32()
                        let length = reader.ReadInt32()
                        if length < 0 || stream.Position + int64 length > stream.Length then
                            failwithf "Invalid length %d for package tag %d" length tag
                        let payload = reader.ReadBytes(length)
                        sections <- parseSection tag payload sections

                    let missing =
                        [ if sections.ProfileId.IsNone then "profile"
                          if sections.Capabilities.IsNone then "capabilities"
                          if sections.Resources.IsNone then "resources"
                          if sections.Scene.IsNone then "scene" ]

                    if not missing.IsEmpty then
                        Result.Error [ error Parse ("Package is missing required sections: " + String.concat ", " missing) ]
                    else
                        let diagnostics =
                            [ yield! List.rev sections.Diagnostics
                              if version.Major <> supportedVersion.Major then
                                  yield error Version $"Producer protocol {version.Major}.{version.Minor} is outside supported major {supportedVersion.Major}.x."
                              elif version.Minor > supportedVersion.Minor then
                                  yield warning Version $"Producer protocol minor {version.Minor} is newer than supported minor {supportedVersion.Minor}; required tags and capabilities will decide acceptance." ]

                        Result.Ok
                            { Version = version
                              ProfileId = sections.ProfileId.Value
                              Capabilities = sections.Capabilities.Value
                              Resources = sections.Resources.Value
                              Scene = sections.Scene.Value
                              CanonicalBytes = Array.copy bytes
                              PackageIdentity = packageIdentity bytes
                              Diagnostics = diagnostics }
        with ex ->
            Result.Error [ error Parse ex.Message ]

    let private allKnownCapabilities =
        [ EmptyElement
          GroupElement
          RectangleElement
          CircleElement
          EllipseElement
          LineElement
          PathElement
          PointsElement
          VerticesElement
          ArcElement
          TextElement
          TextRunElement
          ImageElement
          ClipElement
          RegionElement
          ColorSpaceElement
          PerspectiveElement
          PictureElement
          ChartElement
          TranslateElement
          SizedTextElement
          GlyphRunElement ]
        |> List.map capabilityId

    let private duplicateResourceIds (resources: ResourceAvailability list) =
        resources
        |> List.groupBy _.ResourceId
        |> List.choose (fun (id, items) -> if List.length items > 1 then Some id else None)
        |> Set.ofList

    let private capabilityVerdicts (options: PackageInspectionOptions) (package: PortableScenePackage) : CapabilityVerdict list =
        let supported =
            options.TargetProfile
            |> Option.map _.SupportedCapabilities
            |> Option.defaultValue allKnownCapabilities
            |> Set.ofList

        package.Capabilities
        |> List.map (fun (requirement: CapabilityRequirement) ->
            let isSupported = Set.contains requirement.CapabilityId supported
            let degraded = (not isSupported) && requirement.RequirementLevel = Optional && requirement.DegradationPolicy <> Reject
            let diagnostics =
                [ if not isSupported then
                      let severity = if requirement.RequirementLevel = Required || requirement.DegradationPolicy = Reject then Error else Warning
                      yield
                          diagnostic
                              severity
                              Capability
                              $"Capability '{requirement.CapabilityId}' is not supported by the target profile."
                              (requirement.AffectedScenePaths |> List.tryHead)
                              (Some requirement.CapabilityId)
                              None ]

            { Requirement = requirement
              Supported = isSupported
              Degraded = degraded
              Diagnostics = diagnostics })

    let private resourceVerdicts (options: PackageInspectionOptions) (package: PortableScenePackage) : ResourceVerdict list =
        let availabilityById =
            options.Resources
            |> List.groupBy (fun (availability: ResourceAvailability) -> availability.ResourceId)
            |> Map.ofList

        package.Resources
        |> List.map (fun (entry: ResourceEntry) ->
            let observed : ResourceAvailability option =
                Map.tryFind entry.ResourceId availabilityById |> Option.bind List.tryHead
            let duplicated = duplicateResourceIds options.Resources |> Set.contains entry.ResourceId

            let status : ResourceAvailabilityStatus option =
                if duplicated then Some ResourceDuplicated
                else observed |> Option.map _.Status

            let accepted, degraded, diagnostics : bool * bool * PackageDiagnostic list =
                match status, observed with
                | Some ResourceAvailable, Some availability ->
                    let kindMismatch =
                        availability.Kind
                        |> Option.exists ((<>) entry.Kind)

                    let hashMismatch =
                        availability.ContentHash
                        |> Option.exists (fun hash -> not (String.Equals(hash, entry.ContentHash, StringComparison.Ordinal)))

                    let lengthMismatch =
                        match availability.ByteLength, entry.ByteLength with
                        | Some actual, Some expected -> actual <> expected
                        | _ -> false

                    if kindMismatch || hashMismatch || lengthMismatch then
                        false, false, [ diagnostic Error Resource $"Resource '{entry.ResourceId}' metadata does not match the manifest." None None (Some entry.ResourceId) ]
                    else
                        true, false, []
                | Some ResourceAvailable, None ->
                    false, false, [ diagnostic Error Resource $"Resource '{entry.ResourceId}' was marked available without metadata." None None (Some entry.ResourceId) ]
                | Some ResourceHashMismatch, _
                | Some ResourceCorrupted, _
                | Some ResourceDuplicated, _ ->
                    false, false, [ diagnostic Error Resource $"Resource '{entry.ResourceId}' is not usable: {status.Value}." None None (Some entry.ResourceId) ]
                | Some ResourceMissing, _
                | None, _ ->
                    if entry.Required then
                        false, false, [ diagnostic Error Resource $"Required resource '{entry.ResourceId}' is unavailable." None None (Some entry.ResourceId) ]
                    else
                        true, true, [ diagnostic Warning Resource $"Optional resource '{entry.ResourceId}' is unavailable and will degrade." None None (Some entry.ResourceId) ]

            { Entry = entry
              Availability = observed
              Accepted = accepted
              Degraded = degraded
              Diagnostics = diagnostics })

    let inspectWith (options: PackageInspectionOptions) (bytes: byte[]) =
        match importPackage bytes with
        | Result.Error diagnostics ->
            { Status = PackageRejected
              PackageIdentity = Some(packageIdentity bytes)
              Version = None
              ProfileId = None
              CapabilityVerdicts = []
              ResourceVerdicts = []
              Diagnostics = diagnostics }
        | Result.Ok package ->
            let capabilityVerdicts : CapabilityVerdict list = capabilityVerdicts options package
            let resourceVerdicts : ResourceVerdict list = resourceVerdicts options package

            let diagnostics =
                [ yield! package.Diagnostics
                  yield! capabilityVerdicts |> List.collect _.Diagnostics
                  yield! resourceVerdicts |> List.collect _.Diagnostics ]

            let rejected =
                diagnostics |> List.exists (fun d -> d.Severity = Error || d.Severity = Fatal)

            let degraded =
                capabilityVerdicts |> List.exists _.Degraded
                || resourceVerdicts |> List.exists _.Degraded
                || diagnostics |> List.exists (fun d -> d.Severity = Warning)

            let status =
                if rejected then PackageRejected
                elif degraded then PackageAcceptedWithDegradation
                else PackageAccepted

            { Status = status
              PackageIdentity = Some package.PackageIdentity
              Version = Some package.Version
              ProfileId = Some package.ProfileId
              CapabilityVerdicts = capabilityVerdicts
              ResourceVerdicts = resourceVerdicts
              Diagnostics = diagnostics }

    let inspect (bytes: byte[]) =
        inspectWith defaultInspectionOptions bytes

    let compareScenes (expected: Scene) (actual: Scene) =
        let expectedCapabilities = Scene.describe expected
        let actualCapabilities = Scene.describe actual

        let diagnostics =
            [ if expectedCapabilities <> actualCapabilities then
                  yield diagnostic Error ScenePayload "Scene capability sequence differs." None None None
              if expected <> actual then
                  yield diagnostic Error ScenePayload "Scene payload differs after import." None None None ]

        { Equivalent = diagnostics.IsEmpty
          ExpectedCapabilities = expectedCapabilities
          ActualCapabilities = actualCapabilities
          Diagnostics = diagnostics }

    let formatDiagnostics diagnostics =
        diagnostics
        |> List.map (fun diagnostic ->
            let severity = string diagnostic.Severity
            let stage = string diagnostic.Stage
            let path = diagnostic.ScenePath |> Option.map (sprintf " scene=%s") |> Option.defaultValue ""
            let cap = diagnostic.CapabilityId |> Option.map (sprintf " capability=%s") |> Option.defaultValue ""
            let res = diagnostic.ResourceId |> Option.map (sprintf " resource=%s") |> Option.defaultValue ""
            $"{severity} {stage}: {diagnostic.Message}{path}{cap}{res}")
