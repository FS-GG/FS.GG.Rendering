namespace FS.GG.UI.Scene

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

type TextDirection =
    | AutoDirection
    | LeftToRight
    | RightToLeft
    | MixedDirection

type TextScript =
    | AutoScript
    | LatinScript
    | ArabicScript
    | DevanagariScript
    | ThaiScript
    | EmojiScript
    | SymbolScript
    | MixedScript
    | UnknownScript

type ShapingProviderAvailability =
    | ProviderInstalled
    | ProviderCleared
    | ProviderUnavailable
    | ProviderFailed

type ShapingProviderEvidence =
    { Availability: ShapingProviderAvailability
      ProviderId: string
      VersionBucket: string
      Failure: string option }

type TextFallbackDecision =
    | AuthoredFace of family: string
    | SubstitutedFace of requested: string * resolved: string
    | MissingGlyphs of sourceText: string
    | PureFallback
    | ProviderFailure of message: string

type ShapedGlyph =
    { GlyphId: int
      SourceCluster: int
      SourceText: string
      ResolvedFace: string option
      Advance: float
      Offset: Point
      Position: Point
      Missing: bool }

type TextShapeRun =
    { TextRange: int * int
      SourceText: string
      ResolvedFont: string option
      Direction: TextDirection
      Script: TextScript
      FallbackDecision: TextFallbackDecision
      Glyphs: ShapedGlyph list
      Advance: float
      Diagnostics: string list }

type ShapedTextFallbackMode =
    | Shaped
    | PureFallbackMode
    | ProviderUnavailableFallback
    | ShapingFailedFallback

type ShapedTextMetrics =
    { Advance: float
      Width: float
      Height: float
      Baseline: float
      Bounds: Rect option }

type ShapedTextResult =
    { Text: string
      Font: FontSpec
      Provider: ShapingProviderEvidence
      Runs: TextShapeRun list
      Glyphs: ShapedGlyph list
      Metrics: ShapedTextMetrics
      Diagnostics: string list
      Fingerprint: string
      FallbackMode: ShapedTextFallbackMode }

type GlyphRunGlyph =
    { GlyphId: int
      SourceText: string
      Advance: float
      Offset: Point
      Cluster: int
      Position: Point
      ResolvedFace: string option
      Missing: bool }

type GlyphRunMetrics =
    { Advance: float
      Height: float
      Baseline: float }

type GlyphRunData =
    { Text: string
      Font: FontSpec
      Provider: ShapingProviderEvidence
      Runs: TextShapeRun list
      Glyphs: GlyphRunGlyph list
      Metrics: GlyphRunMetrics
      Fingerprint: string
      FallbackMode: ShapedTextFallbackMode
      FallbackDiagnostics: string list }

type GlyphRun =
    { Data: GlyphRunData
      Position: Point
      Paint: Paint }

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
    | GlyphRunElement

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
    | GlyphRun of GlyphRun
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

[<RequireQualifiedAccess>]
type VisualInspectionStatus =
    | Accepted
    | Blocked
    | Incomplete
    | Unsupported
    | EnvironmentLimited
    | NotInspected
    | NotRun

[<RequireQualifiedAccess>]
type VisualInspectionSeverity =
    | Pass
    | Info
    | Warning
    | Blocking
    | Unsupported
    | EnvironmentLimited

[<RequireQualifiedAccess>]
type VisualInspectionMeasurementMode =
    | Exact
    | Approximate
    | Unsupported
    | Unavailable

[<RequireQualifiedAccess>]
type VisualInspectionFitStatus =
    | Inside
    | Overflow
    | Clipped
    | Wrapped
    | Truncated
    | Unsupported
    | Unavailable

[<RequireQualifiedAccess>]
type VisualInspectionNodeKind =
    | Root
    | Container
    | Text
    | Shape
    | Image
    | Overlay
    | Popup
    | Custom of string
    | Unknown

[<RequireQualifiedAccess>]
type VisualInspectionPaintRole =
    | Background
    | Surface
    | Border
    | Foreground
    | Content
    | Overlay
    | None
    | Unknown

[<RequireQualifiedAccess>]
type VisualInspectionSurfaceRole =
    | Root
    | Shell
    | Content
    | Navigation
    | Feedback
    | Overlay
    | Popup
    | Floating
    | Custom of string
    | Unknown

[<RequireQualifiedAccess>]
type VisualInspectionClipStatus =
    | None
    | Intentional
    | Accidental
    | Unsupported
    | Unavailable

[<RequireQualifiedAccess>]
type VisualInspectionCoverageStatus =
    | Complete
    | Partial
    | Missing
    | Unsupported
    | Unavailable

type VisualInspectionScope =
    { ScopeId: string
      Title: string
      Required: bool }

type VisualInspectionUnsupportedFact =
    { Fact: string
      OwnerId: string option
      Required: bool
      Reason: string
      Diagnostic: string
      EnvironmentLimited: bool }

type VisualInspectionNode =
    { NodeId: string
      ParentId: string option
      Kind: VisualInspectionNodeKind
      OwnerId: string option
      Bounds: Rect option
      Clip: VisualInspectionClipStatus
      ZOrder: int
      PaintRole: VisualInspectionPaintRole
      SurfaceRole: VisualInspectionSurfaceRole
      TextRunIds: string list
      Children: string list
      Dynamic: bool
      UnsupportedFacts: VisualInspectionUnsupportedFact list }

type VisualTextInspection =
    { TextId: string
      OwnerNodeId: string
      Text: string
      TextBounds: Rect option
      OwnerBounds: Rect option
      Baseline: float option
      MeasurementMode: VisualInspectionMeasurementMode
      FitStatus: VisualInspectionFitStatus
      Required: bool
      Diagnostics: string list }

type VisualRegionBoundary =
    { RegionId: string
      Name: string
      Role: VisualInspectionSurfaceRole
      Bounds: Rect option
      Required: bool
      OwnerNodeIds: string list
      AllowedOverlapRoles: VisualInspectionSurfaceRole list }

type VisualPaintCoverage =
    { CoverageId: string
      TargetId: string
      PaintRole: VisualInspectionPaintRole
      CoverageBounds: Rect option
      CoverageStatus: VisualInspectionCoverageStatus
      Reason: string option }

type VisualClipFact =
    { ClipId: string
      NodeId: string
      ClipBounds: Rect option
      ClipStatus: VisualInspectionClipStatus
      Reason: string option
      AffectedTextRunIds: string list }

type VisualInspectionFinding =
    { FindingId: string
      RuleId: string
      Severity: VisualInspectionSeverity
      AffectedNodeIds: string list
      AffectedRegionIds: string list
      Message: string
      Expected: string
      Actual: string
      ExceptionId: string option
      Diagnostics: string list }

type VisualInspectionArtifact =
    { ArtifactId: string
      Scope: VisualInspectionScope
      OutputSize: Size
      Presentation: string
      ReadinessStatus: VisualInspectionStatus
      Nodes: VisualInspectionNode list
      Regions: VisualRegionBoundary list
      TextRuns: VisualTextInspection list
      PaintCoverage: VisualPaintCoverage list
      ClipFacts: VisualClipFact list
      Findings: VisualInspectionFinding list
      UnsupportedFacts: VisualInspectionUnsupportedFact list
      Diagnostics: string list
      GeneratedAtUtc: string }

type VisualInspectionSummary =
    { RunId: string
      OverallStatus: VisualInspectionStatus
      ArtifactCount: int
      InspectedScopes: string list
      NotInspectedScopes: string list
      NotRunScopes: string list
      StatusCounts: (string * int) list
      FindingCounts: (string * int) list
      BlockingFindings: VisualInspectionFinding list
      UnsupportedFacts: VisualInspectionUnsupportedFact list
      AcceptedExceptions: string list
      InvalidExceptions: string list
      RelatedVisualEvidence: string list
      Caveats: string list
      Diagnostics: string list }

[<RequireQualifiedAccess>]
type RetainedInspectionStatus =
    | Accepted
    | Blocked
    | ReviewRequired
    | Unsupported
    | EnvironmentLimited
    | NotInspected
    | NotRun

[<RequireQualifiedAccess>]
type RetainedNodeStatus =
    | Retained
    | Reused
    | Repainted
    | Shifted
    | ShiftedAndRepainted
    | Added
    | Removed
    | Unaffected
    | Unsupported

[<RequireQualifiedAccess>]
type DamageInspectionStatus =
    | Empty
    | Localized
    | Broad
    | FullSurface
    | Unsupported
    | NotInspected

type IntentionalDamageException =
    { ExceptionId: string
      RuleId: string
      ScopeId: string
      TransitionId: string
      AffectedIds: string list
      Reason: string
      ExpiresWith: string option }

type RetainedFrameTransition =
    { TransitionId: string
      PriorFrameId: string option
      CurrentFrameId: string
      InteractionId: string option
      ExpectedAffectedRegionIds: string list
      MaximumDirtyPercentage: float option
      IntentionalExceptions: IntentionalDamageException list }

type RetainedNodeInspection =
    { NodeId: string
      ParentId: string option
      RetainedIdentity: string option
      Kind: string
      OwnerId: string option
      Status: RetainedNodeStatus
      PriorBounds: Rect option
      CurrentBounds: Rect option
      AffectedRegionIds: string list
      Repainted: bool
      Shifted: bool
      UnsupportedFacts: VisualInspectionUnsupportedFact list
      Diagnostics: string list }

type DamageRegionInspection =
    { TransitionId: string
      DamageStatus: DamageInspectionStatus
      FrameBounds: Rect
      DirtyRectangles: Rect list
      UnionBounds: Rect option
      UnionArea: int
      VisibleDirtyArea: int
      DirtyPercentage: float
      AffectedRegionIds: string list
      AffectedNodeIds: string list
      RepaintedNodeCount: int
      ShiftedNodeCount: int
      UnaffectedNodeCount: int
      Cause: string option
      Diagnostics: string list }

type DamageLocalityFinding =
    { FindingId: string
      RuleId: string
      Severity: VisualInspectionSeverity
      TransitionId: string
      AffectedNodeIds: string list
      AffectedRegionIds: string list
      Message: string
      Expected: string
      Actual: string
      ExceptionId: string option
      Diagnostics: string list }

type RetainedInspectionArtifact =
    { ArtifactId: string
      RunId: string
      Scope: VisualInspectionScope
      OutputSize: Size
      Presentation: string
      Transition: RetainedFrameTransition option
      FinalVisualArtifact: VisualInspectionArtifact option
      RetainedNodes: RetainedNodeInspection list
      Damage: DamageRegionInspection option
      Findings: DamageLocalityFinding list
      UnsupportedFacts: VisualInspectionUnsupportedFact list
      RelatedVisualEvidence: string list
      ReadinessStatus: RetainedInspectionStatus
      Diagnostics: string list
      GeneratedAtUtc: string }

type RetainedInspectionSummary =
    { RunId: string
      OverallStatus: RetainedInspectionStatus
      ArtifactCount: int
      InspectedScopes: string list
      NotInspectedScopes: string list
      StatusCounts: (string * int) list
      DamageStatusCounts: (string * int) list
      NodeStatusCounts: (string * int) list
      DirtyAreaSummaries: (string * float * string list) list
      BlockingFindings: DamageLocalityFinding list
      UnsupportedFacts: VisualInspectionUnsupportedFact list
      AcceptedExceptions: string list
      InvalidExceptions: string list
      RelatedVisualEvidence: string list
      CommandEvidence: (string * string) list
      Caveats: string list
      Diagnostics: string list }

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

    let private measureTextHeuristic (text: string) (font: FontSpec) =
        // Feature 136 (R2/T016): the pure, host-independent heuristic kept for pure callers and pure
        // goldens. The per-glyph advance ratio `0.58·size` is calibrated against the bundled default
        // family (Noto Sans averages ~0.49·size; the probe in research.md R1 measured "Stable" at
        // 0.49·size·n): it stays deliberately *conservative* (>= the real average advance) so a box
        // sized by this heuristic is never narrower than the bundled-font renderer draws.
        let size = max 1.0 font.Size
        let glyphAdvance = max 1.0 (size * 0.58)

        { Width = glyphAdvance * float text.Length
          Height = size
          Baseline = size * 0.8 }

    let private pureFallbackProvider =
        { Availability = ProviderUnavailable
          ProviderId = "scene-pure-fallback"
          VersionBucket = "scene-pure-fallback/v1"
          Failure = None }

    let private directionOf (text: string) =
        let mutable hasRtl = false
        let mutable hasLtr = false

        for ch in text do
            let code = int ch

            if (code >= 0x0590 && code <= 0x08FF) || (code >= 0xFB1D && code <= 0xFEFC) then
                hasRtl <- true
            elif Char.IsLetter ch then
                hasLtr <- true

        match hasLtr, hasRtl with
        | true, true -> MixedDirection
        | false, true -> RightToLeft
        | true, false -> LeftToRight
        | false, false -> AutoDirection

    let private scriptOf (text: string) =
        let buckets =
            text
            |> Seq.choose (fun ch ->
                let code = int ch

                if (code >= 0x0041 && code <= 0x024F) then Some LatinScript
                elif code >= 0x0600 && code <= 0x06FF then Some ArabicScript
                elif code >= 0x0900 && code <= 0x097F then Some DevanagariScript
                elif code >= 0x0E00 && code <= 0x0E7F then Some ThaiScript
                elif code >= 0x2600 && code <= 0x27BF then Some SymbolScript
                elif Char.IsSurrogate ch then Some EmojiScript
                elif Char.IsLetterOrDigit ch then Some UnknownScript
                else None)
            |> Seq.distinct
            |> Seq.toList

        match buckets with
        | [] -> AutoScript
        | [ single ] -> single
        | _ -> MixedScript

    let private glyphRunFingerprintOf provider runs fallbackMode text font glyphs metrics diagnostics =
        let glyphPayload =
            glyphs
            |> List.map (fun g ->
                sprintf
                    "%d:%s:%.12g:%.12g:%.12g:%d:%.12g:%.12g:%s:%b"
                    g.GlyphId
                    g.SourceText
                    g.Advance
                    g.Offset.X
                    g.Offset.Y
                    g.Cluster
                    g.Position.X
                    g.Position.Y
                    (g.ResolvedFace |> Option.defaultValue "")
                    g.Missing)
            |> String.concat "|"

        let runPayload =
            runs
            |> List.map (fun r ->
                let start, length = r.TextRange
                sprintf "%d:%d:%s:%A:%A:%A:%.12g:%s" start length r.SourceText r.Direction r.Script r.FallbackDecision r.Advance (String.concat ";" r.Diagnostics))
            |> String.concat "|"

        let payload =
            String.concat
                "\u001f"
                [ sprintf "%A:%s:%s:%A" provider.Availability provider.ProviderId provider.VersionBucket provider.Failure
                  sprintf "%A" fallbackMode
                  runPayload
                  text
                  sprintf "%A" font
                  glyphPayload
                  sprintf "%.12g:%.12g:%.12g" metrics.Advance metrics.Height metrics.Baseline
                  String.concat "|" diagnostics ]

        SHA256.HashData(Encoding.UTF8.GetBytes payload)
        |> Convert.ToHexString
        |> fun value -> value.ToLowerInvariant()

    let private shapedTextFingerprintOf (result: ShapedTextResult) =
        let glyphPayload =
            result.Glyphs
            |> List.map (fun g ->
                sprintf
                    "%d:%d:%s:%s:%.12g:%.12g:%.12g:%.12g:%.12g:%b"
                    g.GlyphId
                    g.SourceCluster
                    g.SourceText
                    (g.ResolvedFace |> Option.defaultValue "")
                    g.Advance
                    g.Offset.X
                    g.Offset.Y
                    g.Position.X
                    g.Position.Y
                    g.Missing)
            |> String.concat "|"

        let runPayload =
            result.Runs
            |> List.map (fun r ->
                let start, length = r.TextRange
                sprintf "%d:%d:%s:%s:%A:%A:%A:%.12g:%s" start length r.SourceText (r.ResolvedFont |> Option.defaultValue "") r.Direction r.Script r.FallbackDecision r.Advance (String.concat ";" r.Diagnostics))
            |> String.concat "|"

        let boundsPayload =
            result.Metrics.Bounds
            |> Option.map (fun b -> sprintf "%.12g:%.12g:%.12g:%.12g" b.X b.Y b.Width b.Height)
            |> Option.defaultValue ""

        let payload =
            String.concat
                "\u001f"
                [ result.Text
                  sprintf "%A" result.Font
                  sprintf "%A:%s:%s:%A" result.Provider.Availability result.Provider.ProviderId result.Provider.VersionBucket result.Provider.Failure
                  runPayload
                  glyphPayload
                  sprintf "%.12g:%.12g:%.12g:%.12g:%s" result.Metrics.Advance result.Metrics.Width result.Metrics.Height result.Metrics.Baseline boundsPayload
                  String.concat "|" result.Diagnostics
                  sprintf "%A" result.FallbackMode ]

        SHA256.HashData(Encoding.UTF8.GetBytes payload)
        |> Convert.ToHexString
        |> fun value -> value.ToLowerInvariant()

    let buildGlyphRun (text: string) (font: FontSpec) : GlyphRunData =
        let metrics = measureTextHeuristic text font
        let perGlyph =
            if String.IsNullOrEmpty text then
                0.0
            else
                metrics.Width / float text.Length

        let mutable x = 0.0

        let glyphs =
            text
            |> Seq.mapi (fun index ch ->
                let current = x
                x <- x + perGlyph

                { GlyphId = int ch
                  SourceText = string ch
                  Advance = perGlyph
                  Offset = { X = 0.0; Y = 0.0 }
                  Cluster = index
                  Position = { X = current; Y = 0.0 }
                  ResolvedFace = font.Family
                  Missing = false })
            |> Seq.toList

        let glyphMetrics =
            { Advance = metrics.Width
              Height = metrics.Height
              Baseline = metrics.Baseline }

        let diagnostics =
            text
            |> Seq.choose (fun ch ->
                if Char.IsSurrogate ch then
                    Some(sprintf "glyph-run-proof: unsupported surrogate code unit U+%04X deferred to full shaping" (int ch))
                else
                    None)
            |> Seq.toList

        let shapedGlyphs =
            glyphs
            |> List.map (fun g ->
                ({ GlyphId = g.GlyphId
                   SourceCluster = g.Cluster
                   SourceText = g.SourceText
                   ResolvedFace = g.ResolvedFace
                   Advance = g.Advance
                   Offset = g.Offset
                   Position = g.Position
                   Missing = g.Missing }
                 : ShapedGlyph))

        let run =
            { TextRange = (0, text.Length)
              SourceText = text
              ResolvedFont = font.Family
              Direction = directionOf text
              Script = scriptOf text
              FallbackDecision = PureFallback
              Glyphs = shapedGlyphs
              Advance = metrics.Width
              Diagnostics = diagnostics }

        { Text = text
          Font = font
          Provider = pureFallbackProvider
          Runs = [ run ]
          Glyphs = glyphs
          Metrics = glyphMetrics
          Fingerprint = glyphRunFingerprintOf pureFallbackProvider [ run ] PureFallbackMode text font glyphs glyphMetrics diagnostics
          FallbackMode = PureFallbackMode
          FallbackDiagnostics = diagnostics }

    let buildFallbackShapedText (text: string) (font: FontSpec) : ShapedTextResult =
        let data = buildGlyphRun text font

        let glyphs =
            data.Glyphs
            |> List.map (fun g ->
                ({ GlyphId = g.GlyphId
                   SourceCluster = g.Cluster
                   SourceText = g.SourceText
                   ResolvedFace = g.ResolvedFace
                   Advance = g.Advance
                   Offset = g.Offset
                   Position = g.Position
                   Missing = g.Missing }
                 : ShapedGlyph))

        let metrics =
            { Advance = data.Metrics.Advance
              Width = data.Metrics.Advance
              Height = data.Metrics.Height
              Baseline = data.Metrics.Baseline
              Bounds =
                Some
                    { X = 0.0
                      Y = -data.Metrics.Baseline
                      Width = data.Metrics.Advance
                      Height = data.Metrics.Height } }

        let result =
            { Text = text
              Font = font
              Provider = pureFallbackProvider
              Runs = data.Runs
              Glyphs = glyphs
              Metrics = metrics
              Diagnostics = data.FallbackDiagnostics
              Fingerprint = ""
              FallbackMode = PureFallbackMode }

        { result with Fingerprint = shapedTextFingerprintOf result }

    let shapedTextFingerprint result = shapedTextFingerprintOf result

    let measureShapedText (result: ShapedTextResult) : TextMetrics =
        { Width = result.Metrics.Width
          Height = result.Metrics.Height
          Baseline = result.Metrics.Baseline }

    let glyphRunDataFromShapedText (result: ShapedTextResult) : GlyphRunData =
        let glyphs =
            result.Glyphs
            |> List.map (fun g ->
                ({ GlyphId = g.GlyphId
                   SourceText = g.SourceText
                   Advance = g.Advance
                   Offset = g.Offset
                   Cluster = g.SourceCluster
                   Position = g.Position
                   ResolvedFace = g.ResolvedFace
                   Missing = g.Missing }
                 : GlyphRunGlyph))

        let metrics =
            { Advance = result.Metrics.Advance
              Height = result.Metrics.Height
              Baseline = result.Metrics.Baseline }

        let diagnostics =
            result.Diagnostics

        let data =
            { Text = result.Text
              Font = result.Font
              Provider = result.Provider
              Runs = result.Runs
              Glyphs = glyphs
              Metrics = metrics
              Fingerprint = ""
              FallbackMode = result.FallbackMode
              FallbackDiagnostics = diagnostics }

        { data with Fingerprint = glyphRunFingerprintOf data.Provider data.Runs data.FallbackMode data.Text data.Font data.Glyphs data.Metrics data.FallbackDiagnostics }

    let glyphRunFingerprint (data: GlyphRunData) =
        glyphRunFingerprintOf data.Provider data.Runs data.FallbackMode data.Text data.Font data.Glyphs data.Metrics data.FallbackDiagnostics

    let measureGlyphRun (data: GlyphRunData) =
        { Width = data.Metrics.Advance
          Height = data.Metrics.Height
          Baseline = data.Metrics.Baseline }

    let glyphRun position (data: GlyphRunData) paint =
        { Nodes = [ GlyphRun { Data = data; Position = position; Paint = paint } ] }

    let glyphRunProof position text font paint =
        glyphRun position (buildGlyphRun text font) paint

    let measureText (text: string) (font: FontSpec) =
        measureTextHeuristic text font

    // Feature 136 (R2/FR-002): the real-metrics measurer seam. `measureText` above stays pure; the
    // rendering edge (`SkiaViewer.Fonts`) installs a measurer here that returns the bundled-font
    // renderer's true advances so the advance used to SIZE a text box equals the advance used to DRAW
    // it. Process-wide, disclosed interpreter-edge mutation (constitution IV). `None` (the default) ⇒
    // `measureTextResolved` is byte-identical to the pure `measureText` path.
    let mutable realTextMeasurer: (string -> FontSpec -> TextMetrics) option = None
    let mutable measurementVersionBucket = pureFallbackProvider.VersionBucket

    let setRealTextMeasurer (measurer: (string -> FontSpec -> TextMetrics) option) = realTextMeasurer <- measurer

    let textMeasurementVersionBucket () = measurementVersionBucket

    let setTextMeasurementVersionBucket (bucket: string) =
        measurementVersionBucket <-
            if String.IsNullOrWhiteSpace bucket then
                pureFallbackProvider.VersionBucket
            else
                bucket

    let measureTextResolved (text: string) (font: FontSpec) : TextMetrics =
        match realTextMeasurer with
        | Some m -> m text font
        | None -> measureText text font

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

    /// A capability-set digest of `scene`: it hashes the sorted, DISTINCT set of element-type markers
    /// produced by `describe` (plus the output size), deliberately discarding every node PAYLOAD —
    /// geometry, colour, and OPACITY/ALPHA. Consequently an opacity-only (or any value-only) change does
    /// NOT change `renderHash` (Workstream E3 limitation, by design — this is a coarse "what kinds of things
    /// are drawn" hash, not a render fingerprint). For a collision-resistant, value-sensitive (alpha-sensitive)
    /// structural fingerprint, use feature 120's `RetainedRender.hashScene` instead.
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
    let private intersects (first: Rect) (second: Rect) =
        first.X < second.X + second.Width
        && first.X + first.Width > second.X
        && first.Y < second.Y + second.Height
        && first.Y + first.Height > second.Y

    let private overlapDiagnostics (report: LayoutEvidenceReport) =
        let hudTextOverlaps =
            report.TextBounds
            |> List.mapi (fun index (first: LayoutTextBounds) ->
                report.TextBounds
                |> List.skip (index + 1)
                |> List.choose (fun (second: LayoutTextBounds) ->
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
            |> List.collect (fun (text: LayoutTextBounds) ->
                report.GameplayBounds
                |> List.choose (fun (gameplay: LayoutGameplayBounds) ->
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

    let classify (report: LayoutEvidenceReport) =
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

    let fromRenderEvidence scene (evidence: RenderReadbackEvidence) : LayoutEvidenceReport =
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

    let unsupported scene outputSize (reason: LayoutUnsupportedReason) : LayoutEvidenceReport =
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

module VisualInspection =
    let private cleanToken (value: string) =
        if String.IsNullOrWhiteSpace value then
            "unknown"
        else
            value.Trim().ToLowerInvariant().Replace(" ", "-").Replace("_", "-")

    let statusText status =
        match status with
        | VisualInspectionStatus.Accepted -> "accepted"
        | VisualInspectionStatus.Blocked -> "blocked"
        | VisualInspectionStatus.Incomplete -> "incomplete"
        | VisualInspectionStatus.Unsupported -> "unsupported"
        | VisualInspectionStatus.EnvironmentLimited -> "environment-limited"
        | VisualInspectionStatus.NotInspected -> "not-inspected"
        | VisualInspectionStatus.NotRun -> "not-run"

    let severityText severity =
        match severity with
        | VisualInspectionSeverity.Pass -> "pass"
        | VisualInspectionSeverity.Info -> "info"
        | VisualInspectionSeverity.Warning -> "warning"
        | VisualInspectionSeverity.Blocking -> "blocking"
        | VisualInspectionSeverity.Unsupported -> "unsupported"
        | VisualInspectionSeverity.EnvironmentLimited -> "environment-limited"

    let measurementModeText mode =
        match mode with
        | VisualInspectionMeasurementMode.Exact -> "exact"
        | VisualInspectionMeasurementMode.Approximate -> "approximate"
        | VisualInspectionMeasurementMode.Unsupported -> "unsupported"
        | VisualInspectionMeasurementMode.Unavailable -> "unavailable"

    let fitStatusText status =
        match status with
        | VisualInspectionFitStatus.Inside -> "inside"
        | VisualInspectionFitStatus.Overflow -> "overflow"
        | VisualInspectionFitStatus.Clipped -> "clipped"
        | VisualInspectionFitStatus.Wrapped -> "wrapped"
        | VisualInspectionFitStatus.Truncated -> "truncated"
        | VisualInspectionFitStatus.Unsupported -> "unsupported"
        | VisualInspectionFitStatus.Unavailable -> "unavailable"

    let nodeKindText kind =
        match kind with
        | VisualInspectionNodeKind.Root -> "root"
        | VisualInspectionNodeKind.Container -> "container"
        | VisualInspectionNodeKind.Text -> "text"
        | VisualInspectionNodeKind.Shape -> "shape"
        | VisualInspectionNodeKind.Image -> "image"
        | VisualInspectionNodeKind.Overlay -> "overlay"
        | VisualInspectionNodeKind.Popup -> "popup"
        | VisualInspectionNodeKind.Custom value -> cleanToken value
        | VisualInspectionNodeKind.Unknown -> "unknown"

    let paintRoleText role =
        match role with
        | VisualInspectionPaintRole.Background -> "background"
        | VisualInspectionPaintRole.Surface -> "surface"
        | VisualInspectionPaintRole.Border -> "border"
        | VisualInspectionPaintRole.Foreground -> "foreground"
        | VisualInspectionPaintRole.Content -> "content"
        | VisualInspectionPaintRole.Overlay -> "overlay"
        | VisualInspectionPaintRole.None -> "none"
        | VisualInspectionPaintRole.Unknown -> "unknown"

    let surfaceRoleText role =
        match role with
        | VisualInspectionSurfaceRole.Root -> "root"
        | VisualInspectionSurfaceRole.Shell -> "shell"
        | VisualInspectionSurfaceRole.Content -> "content"
        | VisualInspectionSurfaceRole.Navigation -> "navigation"
        | VisualInspectionSurfaceRole.Feedback -> "feedback"
        | VisualInspectionSurfaceRole.Overlay -> "overlay"
        | VisualInspectionSurfaceRole.Popup -> "popup"
        | VisualInspectionSurfaceRole.Floating -> "floating"
        | VisualInspectionSurfaceRole.Custom value -> cleanToken value
        | VisualInspectionSurfaceRole.Unknown -> "unknown"

    let clipStatusText status =
        match status with
        | VisualInspectionClipStatus.None -> "none"
        | VisualInspectionClipStatus.Intentional -> "intentional"
        | VisualInspectionClipStatus.Accidental -> "accidental"
        | VisualInspectionClipStatus.Unsupported -> "unsupported"
        | VisualInspectionClipStatus.Unavailable -> "unavailable"

    let coverageStatusText status =
        match status with
        | VisualInspectionCoverageStatus.Complete -> "complete"
        | VisualInspectionCoverageStatus.Partial -> "partial"
        | VisualInspectionCoverageStatus.Missing -> "missing"
        | VisualInspectionCoverageStatus.Unsupported -> "unsupported"
        | VisualInspectionCoverageStatus.Unavailable -> "unavailable"

    let unsupportedFact fact ownerId required reason diagnostic environmentLimited =
        { Fact = fact
          OwnerId = ownerId
          Required = required
          Reason = reason
          Diagnostic = diagnostic
          EnvironmentLimited = environmentLimited }

    let stableFindingId (ruleId: string) (affectedIds: string list) =
        let ids =
            affectedIds
            |> List.filter (String.IsNullOrWhiteSpace >> not)
            |> List.map cleanToken
            |> List.sort

        match ids with
        | [] -> cleanToken ruleId
        | _ -> cleanToken ruleId + ":" + String.concat "+" ids

    let finding ruleId severity affectedNodeIds affectedRegionIds message expected actual =
        { FindingId = stableFindingId ruleId (affectedNodeIds @ affectedRegionIds)
          RuleId = ruleId
          Severity = severity
          AffectedNodeIds = affectedNodeIds |> List.sort
          AffectedRegionIds = affectedRegionIds |> List.sort
          Message = message
          Expected = expected
          Actual = actual
          ExceptionId = None
          Diagnostics = [] }

    let private duplicateIds ids =
        ids
        |> List.countBy id
        |> List.choose (fun (id, count) -> if count > 1 then Some id else None)

    let artifactDiagnostics (artifact: VisualInspectionArtifact) =
        let nodeIds = artifact.Nodes |> List.map _.NodeId
        let regionIds = artifact.Regions |> List.map _.RegionId
        let findingIds = artifact.Findings |> List.map _.FindingId
        let parentIds = nodeIds |> Set.ofList

        [ for id in duplicateIds nodeIds do
              $"duplicate visual inspection node id: {id}"
          for id in duplicateIds regionIds do
              $"duplicate visual inspection region id: {id}"
          for id in duplicateIds findingIds do
              $"duplicate visual inspection finding id: {id}"
          for node in artifact.Nodes do
              match node.ParentId with
              | Some parent when not (Set.contains parent parentIds) -> $"node {node.NodeId} references missing parent {parent}"
              | _ -> ()
              match node.Bounds with
              | Some bounds when bounds.Width < 0.0 || bounds.Height < 0.0 || Double.IsNaN bounds.Width || Double.IsNaN bounds.Height ->
                  $"node {node.NodeId} has invalid bounds"
              | _ -> ()
          for region in artifact.Regions do
              match region.Bounds with
              | Some bounds when bounds.Width < 0.0 || bounds.Height < 0.0 || Double.IsNaN bounds.Width || Double.IsNaN bounds.Height ->
                  $"region {region.RegionId} has invalid bounds"
              | _ -> ()
          for fact in artifact.UnsupportedFacts do
              if String.IsNullOrWhiteSpace fact.Fact || String.IsNullOrWhiteSpace fact.Reason then
                  "unsupported visual inspection fact is missing fact name or reason" ]

    let normalizeArtifact (artifact: VisualInspectionArtifact) =
        { artifact with
            Nodes = artifact.Nodes |> List.sortBy (fun node -> node.ZOrder, node.NodeId)
            Regions = artifact.Regions |> List.sortBy _.RegionId
            TextRuns = artifact.TextRuns |> List.sortBy _.TextId
            PaintCoverage = artifact.PaintCoverage |> List.sortBy _.CoverageId
            ClipFacts = artifact.ClipFacts |> List.sortBy _.ClipId
            Findings = artifact.Findings |> List.sortBy _.FindingId
            UnsupportedFacts = artifact.UnsupportedFacts |> List.sortBy (fun fact -> fact.Fact, defaultArg fact.OwnerId "") }

module RetainedInspection =
    let private cleanToken (value: string) =
        if String.IsNullOrWhiteSpace value then
            "unknown"
        else
            value.Trim().ToLowerInvariant().Replace(" ", "-").Replace("_", "-")

    let statusText status =
        match status with
        | RetainedInspectionStatus.Accepted -> "accepted"
        | RetainedInspectionStatus.Blocked -> "blocked"
        | RetainedInspectionStatus.ReviewRequired -> "review-required"
        | RetainedInspectionStatus.Unsupported -> "unsupported"
        | RetainedInspectionStatus.EnvironmentLimited -> "environment-limited"
        | RetainedInspectionStatus.NotInspected -> "not-inspected"
        | RetainedInspectionStatus.NotRun -> "not-run"

    let nodeStatusText status =
        match status with
        | RetainedNodeStatus.Retained -> "retained"
        | RetainedNodeStatus.Reused -> "reused"
        | RetainedNodeStatus.Repainted -> "repainted"
        | RetainedNodeStatus.Shifted -> "shifted"
        | RetainedNodeStatus.ShiftedAndRepainted -> "shifted-and-repainted"
        | RetainedNodeStatus.Added -> "added"
        | RetainedNodeStatus.Removed -> "removed"
        | RetainedNodeStatus.Unaffected -> "unaffected"
        | RetainedNodeStatus.Unsupported -> "unsupported"

    let damageStatusText status =
        match status with
        | DamageInspectionStatus.Empty -> "empty"
        | DamageInspectionStatus.Localized -> "localized"
        | DamageInspectionStatus.Broad -> "broad"
        | DamageInspectionStatus.FullSurface -> "full-surface"
        | DamageInspectionStatus.Unsupported -> "unsupported"
        | DamageInspectionStatus.NotInspected -> "not-inspected"

    let unsupportedFact fact ownerId required reason diagnostic environmentLimited =
        VisualInspection.unsupportedFact fact ownerId required reason diagnostic environmentLimited

    let stableFindingId ruleId transitionId affectedIds =
        let ids =
            affectedIds
            |> List.filter (String.IsNullOrWhiteSpace >> not)
            |> List.map cleanToken
            |> List.sort

        let prefix = cleanToken ruleId + ":" + cleanToken transitionId
        match ids with
        | [] -> prefix
        | _ -> prefix + ":" + String.concat "+" ids

    let finding ruleId severity transitionId affectedNodeIds affectedRegionIds message expected actual =
        { FindingId = stableFindingId ruleId transitionId (affectedNodeIds @ affectedRegionIds)
          RuleId = ruleId
          Severity = severity
          TransitionId = transitionId
          AffectedNodeIds = affectedNodeIds |> List.sort
          AffectedRegionIds = affectedRegionIds |> List.sort
          Message = message
          Expected = expected
          Actual = actual
          ExceptionId = None
          Diagnostics = [] }

    let private clipRect (frame: Rect) (rect: Rect) =
        let x1 = max frame.X rect.X
        let y1 = max frame.Y rect.Y
        let x2 = min (frame.X + frame.Width) (rect.X + rect.Width)
        let y2 = min (frame.Y + frame.Height) (rect.Y + rect.Height)

        if x2 <= x1 || y2 <= y1 then
            None
        else
            Some({ X = x1; Y = y1; Width = x2 - x1; Height = y2 - y1 }: Rect)

    let private clipped frame rects =
        rects
        |> List.choose (clipRect frame)
        |> List.distinct

    let dirtyUnionBounds frameBounds dirtyRectangles =
        match clipped frameBounds dirtyRectangles with
        | [] -> None
        | rects ->
            let minX = rects |> List.map _.X |> List.min
            let minY = rects |> List.map _.Y |> List.min
            let maxX = rects |> List.map (fun r -> r.X + r.Width) |> List.max
            let maxY = rects |> List.map (fun r -> r.Y + r.Height) |> List.max
            Some({ X = minX; Y = minY; Width = maxX - minX; Height = maxY - minY }: Rect)

    let dirtyUnionArea frameBounds dirtyRectangles =
        let rects = clipped frameBounds dirtyRectangles

        match rects with
        | [] -> 0
        | _ ->
            let xs =
                rects
                |> List.collect (fun r -> [ r.X; r.X + r.Width ])
                |> List.distinct
                |> List.sort

            let ys =
                rects
                |> List.collect (fun r -> [ r.Y; r.Y + r.Height ])
                |> List.distinct
                |> List.sort

            let covered x1 x2 y1 y2 =
                rects
                |> List.exists (fun r ->
                    x1 >= r.X
                    && x2 <= r.X + r.Width
                    && y1 >= r.Y
                    && y2 <= r.Y + r.Height)

            let mutable area = 0.0

            for x1, x2 in xs |> List.pairwise do
                for y1, y2 in ys |> List.pairwise do
                    if x2 > x1 && y2 > y1 && covered x1 x2 y1 y2 then
                        area <- area + ((x2 - x1) * (y2 - y1))

            area |> int

    let damageRegion transitionId frameBounds dirtyRectangles expectedAffectedRegionIds affectedNodeIds repaintedNodeCount shiftedNodeCount unaffectedNodeCount cause maximumDirtyPercentage =
        let clippedRectangles = clipped frameBounds dirtyRectangles
        let area = dirtyUnionArea frameBounds clippedRectangles
        let frameArea = max 0.0 (frameBounds.Width * frameBounds.Height)
        let dirtyPercentage =
            if frameArea <= 0.0 then
                0.0
            else
                float area / frameArea * 100.0

        let status =
            if clippedRectangles.IsEmpty || area = 0 then
                DamageInspectionStatus.Empty
            elif area >= int frameArea && frameArea > 0.0 then
                DamageInspectionStatus.FullSurface
            else
                match maximumDirtyPercentage with
                | Some limit when dirtyPercentage > limit -> DamageInspectionStatus.Broad
                | _ -> DamageInspectionStatus.Localized

        { TransitionId = transitionId
          DamageStatus = status
          FrameBounds = frameBounds
          DirtyRectangles = clippedRectangles |> List.sortBy (fun r -> r.Y, r.X, r.Width, r.Height)
          UnionBounds = dirtyUnionBounds frameBounds clippedRectangles
          UnionArea = area
          VisibleDirtyArea = area
          DirtyPercentage = dirtyPercentage
          AffectedRegionIds = expectedAffectedRegionIds |> List.distinct |> List.sort
          AffectedNodeIds = affectedNodeIds |> List.distinct |> List.sort
          RepaintedNodeCount = repaintedNodeCount
          ShiftedNodeCount = shiftedNodeCount
          UnaffectedNodeCount = unaffectedNodeCount
          Cause = cause
          Diagnostics = [] }

    let private duplicateIds ids =
        ids
        |> List.countBy id
        |> List.choose (fun (id, count) -> if count > 1 then Some id else None)

    let artifactDiagnostics (artifact: RetainedInspectionArtifact) =
        let nodeIds = artifact.RetainedNodes |> List.map _.NodeId
        let findingIds = artifact.Findings |> List.map _.FindingId

        [ for id in duplicateIds nodeIds do
              $"duplicate retained inspection node id: {id}"
          for id in duplicateIds findingIds do
              $"duplicate retained inspection finding id: {id}"
          for node in artifact.RetainedNodes do
              match node.Status, node.PriorBounds, node.CurrentBounds with
              | RetainedNodeStatus.Shifted, None, _
              | RetainedNodeStatus.Shifted, _, None
              | RetainedNodeStatus.ShiftedAndRepainted, None, _
              | RetainedNodeStatus.ShiftedAndRepainted, _, None ->
                  $"shifted retained node {node.NodeId} is missing prior or current bounds"
              | _ -> ()
              for fact in node.UnsupportedFacts do
                  if String.IsNullOrWhiteSpace fact.Fact || String.IsNullOrWhiteSpace fact.Reason then
                      $"retained node {node.NodeId} has unsupported fact missing fact name or reason"
          for fact in artifact.UnsupportedFacts do
              if String.IsNullOrWhiteSpace fact.Fact || String.IsNullOrWhiteSpace fact.Reason then
                  "retained inspection unsupported fact is missing fact name or reason" ]

    let normalizeArtifact (artifact: RetainedInspectionArtifact) =
        { artifact with
            RetainedNodes = artifact.RetainedNodes |> List.sortBy _.NodeId
            Damage =
                artifact.Damage
                |> Option.map (fun damage ->
                    { damage with
                        DirtyRectangles = damage.DirtyRectangles |> List.sortBy (fun r -> r.Y, r.X, r.Width, r.Height)
                        AffectedNodeIds = damage.AffectedNodeIds |> List.distinct |> List.sort
                        AffectedRegionIds = damage.AffectedRegionIds |> List.distinct |> List.sort })
            Findings = artifact.Findings |> List.sortBy _.FindingId
            UnsupportedFacts = artifact.UnsupportedFacts |> List.sortBy (fun fact -> fact.Fact, defaultArg fact.OwnerId "")
            RelatedVisualEvidence = artifact.RelatedVisualEvidence |> List.distinct |> List.sort
            Diagnostics = artifact.Diagnostics |> List.distinct |> List.sort }
