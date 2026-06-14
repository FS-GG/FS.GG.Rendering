namespace FS.Skia.UI.Scene

open System
open System.Security.Cryptography
open System.Text

type Size =
    { Width: int
      Height: int }

type Color =
    { Red: byte
      Green: byte
      Blue: byte
      Alpha: byte }

type Point =
    { X: float
      Y: float }

type Rect =
    { X: float
      Y: float
      Width: float
      Height: float }

type StrokeCap =
    | Butt
    | Round
    | Square

type StrokeJoin =
    | Miter
    | RoundJoin
    | Bevel

type BlendMode =
    | SrcOver
    | Multiply
    | Screen
    | Overlay
    | Darken
    | Lighten
    | ColorDodge
    | ColorBurn
    | Difference
    | Exclusion

type Stroke =
    { Width: float
      Cap: StrokeCap
      Join: StrokeJoin
      Miter: float }

type Shader =
    | SolidColor of Color
    | LinearGradient of startPoint: Point * endPoint: Point * colors: Color list
    | RadialGradient of center: Point * radius: float * colors: Color list
    | SweepGradient of center: Point * colors: Color list

type ColorFilter =
    | NoColorFilter
    | BlendColor of Color * BlendMode

type MaskFilter =
    | NoMaskFilter
    | Blur of sigma: float

type ImageFilter =
    | NoImageFilter
    | DropShadow of dx: float * dy: float * blur: float * color: Color

type PathEffect =
    | NoPathEffect
    | Dash of intervals: float list * phase: float
    | Discrete of segmentLength: float * deviation: float
    | Corner of radius: float

type Paint =
    { Fill: Color option
      Stroke: Stroke option
      Opacity: float
      Antialias: bool
      BlendMode: BlendMode
      Shader: Shader option
      ColorFilter: ColorFilter
      MaskFilter: MaskFilter
      ImageFilter: ImageFilter
      PathEffect: PathEffect }

type PathFillType =
    | Winding
    | EvenOdd

type PathCommand =
    | MoveTo of Point
    | LineTo of Point
    | QuadTo of control: Point * point: Point
    | CubicTo of control1: Point * control2: Point * point: Point
    | ArcTo of bounds: Rect * startAngle: float * sweepAngle: float
    | Close

type PathSpec =
    { Commands: PathCommand list
      FillType: PathFillType }

type Clip =
    | RectClip of Rect
    | PathClip of PathSpec

type RegionOperation =
    | Replace
    | RegionUnion
    | RegionIntersect
    | RegionDifference

type Region =
    { Bounds: Rect list
      Operation: RegionOperation }

type ColorSpace =
    | Srgb
    | DisplayP3
    | AdobeRgb

type PerspectiveTransform =
    { M11: float
      M12: float
      M13: float
      M21: float
      M22: float
      M23: float
      M31: float
      M32: float
      M33: float }

type PathOperation =
    | Union
    | Intersect
    | Difference
    | Xor

type PathMeasure =
    { Length: float
      IsClosed: bool }

type FontSpec =
    { Family: string option
      Size: float
      Weight: int option }

type TextRun =
    { Text: string
      Position: Point
      Font: FontSpec
      Paint: Paint }

type TextMetrics =
    { Width: float
      Height: float
      Baseline: float }

type Vertex =
    { Position: Point
      Color: Color option }

type VertexMode =
    | Triangles
    | TriangleStrip
    | TriangleFan

type SceneElementKind =
    | EmptyElement
    | GroupElement
    | RectangleElement
    | CircleElement
    | EllipseElement
    | LineElement
    | PathElement
    | PointsElement
    | VerticesElement
    | ArcElement
    | TextElement
    | TextRunElement
    | ImageElement
    | ClipElement
    | RegionElement
    | ColorSpaceElement
    | PerspectiveElement
    | PictureElement
    | ChartElement
    | TranslateElement
    | SizedTextElement

type RenderReadbackEvidence =
    { Size: Size
      CapabilityCount: int
      Capabilities: string list
      DeterministicHash: string }

type ShapePlacement =
    | FullyInside
    | PartiallyOutOfBounds
    | FullyOutOfBounds

type CircleShapeEvidence =
    { Center: Point
      Radius: float
      Bounds: Rect
      Fill: Color
      Placement: ShapePlacement }

type EllipseShapeEvidence =
    { Bounds: Rect
      Fill: Color
      Placement: ShapePlacement }

type LayoutProofLevel =
    | ReadableLayout
    | DeterministicRenderOnly
    | UnsupportedLayoutInspection

type LayoutMeasurementMode =
    | ExactTextBounds
    | ApproximateTextBounds
    | UnsupportedTextBounds

type LayoutOverlapKind =
    | HudTextOverlap
    | HudGameplayOverlap
    | GameplayOutOfBounds

type LayoutOverlapDiagnostic =
    { Kind: LayoutOverlapKind
      FirstName: string
      SecondName: string option
      Bounds: Rect
      Message: string }

type LayoutOverlapStatus =
    | NoLayoutOverlap
    | LayoutOverlaps of LayoutOverlapDiagnostic list

type LayoutRegionEvidence =
    { Name: string
      Bounds: Rect }

type LayoutTextBounds =
    { Name: string
      Text: string
      Bounds: Rect
      MeasurementMode: LayoutMeasurementMode }

type LayoutGameplayBounds =
    { Name: string
      Bounds: Rect }

type LayoutUnsupportedReason =
    { Fact: string
      Reason: string
      Diagnostic: string }

type DiagnosticSeverity =
    | Info
    | Warning
    | Error
    | Fatal

type DiagnosticStage =
    | FrameRender

type RenderDiagnostic =
    { Severity: DiagnosticSeverity
      Stage: DiagnosticStage
      Message: string
      Cause: string option }

type SceneNode =
    | Empty
    | Group of Scene list
    | Rectangle of (float * float * float * float) * Color
    | PaintedRectangle of Rect * Paint
    | Circle of center: Point * radius: float * fill: Color
    | FilledEllipse of bounds: Rect * fill: Color
    | Ellipse of Rect * Paint
    | Line of Point * Point * Paint
    | Path of PathSpec * Paint
    | Points of Point list * Paint
    | Vertices of VertexMode * Vertex list * Paint
    | Arc of Rect * float * float * Paint
    | Text of (float * float) * string * Color
    | TextRun of TextRun
    | Image of (float * float * float * float) * string
    | ClipNode of Clip * Scene
    | RegionNode of Region * Paint
    | ColorSpaceNode of ColorSpace * Scene
    | PerspectiveNode of PerspectiveTransform * Scene
    | PictureNode of Picture
    | Chart of values: float list
    | Translate of (float * float) * Scene
    | SizedText of (float * float) * string * float * Color
    /// Feature 120 (FR-007): a backend replay-cache boundary; transparent to every consumer except
    /// the GL painter (see `Scene.fsi`).
    | CachedSubtree of CacheBoundary

and Scene =
    { Nodes: SceneNode list }

and Picture =
    { Name: string
      Scene: Scene }

and CacheBoundary =
    { CacheId: uint64
      Fingerprint: uint64
      Scene: Scene }

type LayoutEvidenceReport =
    { Scene: Scene
      OutputSize: Size
      ProofLevel: LayoutProofLevel
      HudRegion: LayoutRegionEvidence option
      GameplayRegion: LayoutRegionEvidence option
      TextBounds: LayoutTextBounds list
      GameplayBounds: LayoutGameplayBounds list
      OverlapStatus: LayoutOverlapStatus
      MeasurementMode: LayoutMeasurementMode
      UnsupportedReasons: LayoutUnsupportedReason list
      Diagnostics: string list
      RenderEvidence: RenderReadbackEvidence option }

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

    let segment (startDistance: float) (endDistance: float) (path: PathSpec) =
        if endDistance <= startDistance then
            { path with Commands = [] }
        else
            path

    let combine operation (left: PathSpec) (right: PathSpec) =
        let marker =
            match operation with
            | Union -> []
            | Intersect -> []
            | Difference -> []
            | Xor -> []

        { FillType = left.FillType
          Commands = left.Commands @ marker @ right.Commands }

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

    let measureText (text: string) (font: FontSpec) =
        let size = max 1.0 font.Size
        let glyphAdvance = max 1.0 (size * 0.58)

        { Width = glyphAdvance * float text.Length
          Height = size
          Baseline = size * 0.8 }

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
            | Image(_, source) when String.IsNullOrWhiteSpace source -> [ diagnostic Error "Invalid image resource declaration." (Some "Image source path is empty.") ]
            | Image(_, source) when not (IO.File.Exists source) -> [ diagnostic Error "Invalid image resource declaration." (Some $"Image source '{source}' does not exist.") ]
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

type SceneEvidenceFormat =
    | Hash
    | Png
    | Metadata

type SceneEvidenceFailureClassification =
    | UnsupportedEnvironment
    | ProductDefect

type SceneEvidenceFailure =
    { BlockedStage: string
      Classification: SceneEvidenceFailureClassification
      DiagnosticCategory: string
      Message: string }

type SceneEvidenceRequest =
    { Scene: Scene
      OutputSize: Size
      Format: SceneEvidenceFormat
      RendererMode: string
      EvidencePath: string option }

type SceneEvidence =
    { Format: SceneEvidenceFormat
      OutputSize: Size
      RendererMode: string
      EvidencePath: string option
      Value: string }

// Feature 105 (US3, FR-009): the closed set of scene-evidence failure stages, typed so the
// internal classification is a compile-checked DU instead of a bare string. The public
// `SceneEvidenceFailure.BlockedStage`/`DiagnosticCategory` fields stay `string`, written via the
// single `EvidenceStage.name` projection at construction, so the evidence text is byte-identical
// "scene"/"renderer". Hidden from consumers by absence from Scene.fsi.
[<RequireQualifiedAccess>]
type EvidenceStage =
    | Scene
    | Renderer

module SceneEvidence =
    let stageName (stage: EvidenceStage) : string =
        match stage with
        | EvidenceStage.Scene -> "scene"
        | EvidenceStage.Renderer -> "renderer"

    let supportedRendererMode mode =
        String.IsNullOrWhiteSpace mode
        || String.Equals(mode, "deterministic-scene", StringComparison.Ordinal)

    let writeEvidence (path: string) (value: string) =
        let directory = IO.Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace directory) then
            IO.Directory.CreateDirectory(directory |> string) |> ignore

        IO.File.WriteAllText(path, value)

    let render (request: SceneEvidenceRequest) =
        if request.OutputSize.Width <= 0 || request.OutputSize.Height <= 0 then
            Result.Error
                { BlockedStage = stageName EvidenceStage.Scene
                  Classification = ProductDefect
                  DiagnosticCategory = stageName EvidenceStage.Scene
                  Message = "Scene evidence output size must be positive." }
        elif not (supportedRendererMode request.RendererMode) then
            Result.Error
                { BlockedStage = stageName EvidenceStage.Renderer
                  Classification = UnsupportedEnvironment
                  DiagnosticCategory = stageName EvidenceStage.Renderer
                  Message = $"Scene evidence renderer mode '{request.RendererMode}' is not available for non-window deterministic evidence." }
        else
            let readback = Scene.renderReadbackEvidence request.OutputSize request.Scene

            let value =
                match request.Format with
                | Hash -> readback.DeterministicHash
                | Metadata -> $"size={request.OutputSize.Width}x{request.OutputSize.Height};capabilities={readback.CapabilityCount};hash={readback.DeterministicHash}"
                | Png -> readback.DeterministicHash

            request.EvidencePath |> Option.iter (fun path -> writeEvidence path value)

            Result.Ok
                { Format = request.Format
                  OutputSize = request.OutputSize
                  RendererMode = "deterministic-scene"
                  EvidencePath = request.EvidencePath
                  Value = value }

    let renderHash size scene =
        render
            { Scene = scene
              OutputSize = size
              Format = Hash
              RendererMode = "deterministic-scene"
              EvidencePath = None }

    let renderPng size scene =
        match
            render
                { Scene = scene
                  OutputSize = size
                  Format = Png
                  RendererMode = "deterministic-scene"
                  EvidencePath = None }
        with
        | Result.Ok evidence -> Result.Ok(Encoding.UTF8.GetBytes evidence.Value)
        | Result.Error failure -> Result.Error failure

module LayoutEvidence =
    let private intersects first second =
        first.X < second.X + second.Width
        && first.X + first.Width > second.X
        && first.Y < second.Y + second.Height
        && first.Y + first.Height > second.Y

    let private overlapDiagnostics report =
        let hudTextOverlaps =
            report.TextBounds
            |> List.mapi (fun index first ->
                report.TextBounds
                |> List.skip (index + 1)
                |> List.choose (fun second ->
                    if intersects first.Bounds second.Bounds then
                        Some
                            { Kind = HudTextOverlap
                              FirstName = first.Name
                              SecondName = Some second.Name
                              Bounds = first.Bounds
                              Message = $"HUD text '{first.Name}' overlaps '{second.Name}'" }
                    else
                        None))
            |> List.concat

        let hudGameplayOverlaps =
            report.TextBounds
            |> List.collect (fun text ->
                report.GameplayBounds
                |> List.choose (fun gameplay ->
                    if intersects text.Bounds gameplay.Bounds then
                        Some
                            { Kind = HudGameplayOverlap
                              FirstName = text.Name
                              SecondName = Some gameplay.Name
                              Bounds = text.Bounds
                              Message = $"HUD text '{text.Name}' overlaps gameplay '{gameplay.Name}'" }
                    else
                        None))

        hudTextOverlaps @ hudGameplayOverlaps

    let classify report =
        let overlaps = overlapDiagnostics report

        let missingFacts =
            report.HudRegion.IsNone
            || report.GameplayRegion.IsNone
            || report.TextBounds.IsEmpty
            || report.GameplayBounds.IsEmpty

        if not report.UnsupportedReasons.IsEmpty || report.MeasurementMode = UnsupportedTextBounds then
            { report with
                ProofLevel = UnsupportedLayoutInspection
                OverlapStatus = if overlaps.IsEmpty then report.OverlapStatus else LayoutOverlaps overlaps
                Diagnostics =
                    report.Diagnostics
                    @ (report.UnsupportedReasons |> List.map (fun reason -> $"{reason.Fact}: {reason.Reason}"))
                    @ (overlaps |> List.map _.Message) }
        elif missingFacts || not overlaps.IsEmpty then
            { report with
                ProofLevel = DeterministicRenderOnly
                OverlapStatus = if overlaps.IsEmpty then report.OverlapStatus else LayoutOverlaps overlaps
                Diagnostics =
                    report.Diagnostics
                    @ [ if report.HudRegion.IsNone then "missing HUD region"
                        if report.GameplayRegion.IsNone then "missing gameplay region"
                        if report.TextBounds.IsEmpty then "missing HUD text bounds"
                        if report.GameplayBounds.IsEmpty then "missing gameplay bounds"
                        yield! overlaps |> List.map _.Message ] }
        else
            { report with
                ProofLevel = ReadableLayout
                OverlapStatus = NoLayoutOverlap }

    let fromRenderEvidence scene evidence =
        { Scene = scene
          OutputSize = evidence.Size
          ProofLevel = DeterministicRenderOnly
          HudRegion = None
          GameplayRegion = None
          TextBounds = []
          GameplayBounds = []
          OverlapStatus = NoLayoutOverlap
          MeasurementMode = ApproximateTextBounds
          UnsupportedReasons = []
          Diagnostics = [ "deterministic render metadata only" ]
          RenderEvidence = Some evidence }

    let unsupported scene outputSize reason =
        { Scene = scene
          OutputSize = outputSize
          ProofLevel = UnsupportedLayoutInspection
          HudRegion = None
          GameplayRegion = None
          TextBounds = []
          GameplayBounds = []
          OverlapStatus = NoLayoutOverlap
          MeasurementMode = UnsupportedTextBounds
          UnsupportedReasons = [ reason ]
          Diagnostics = [ $"unsupported layout fact: {reason.Fact}; {reason.Reason}" ]
          RenderEvidence = None }
