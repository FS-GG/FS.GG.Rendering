namespace FS.GG.UI.Scene

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
    | Rectangle of bounds: (float * float * float * float) * fill: Color
    | PaintedRectangle of bounds: Rect * paint: Paint
    | Circle of center: Point * radius: float * fill: Color
    | FilledEllipse of bounds: Rect * fill: Color
    | Ellipse of bounds: Rect * paint: Paint
    | Line of startPoint: Point * endPoint: Point * paint: Paint
    | Path of path: PathSpec * paint: Paint
    | Points of points: Point list * paint: Paint
    | Vertices of mode: VertexMode * vertices: Vertex list * paint: Paint
    | Arc of bounds: Rect * startAngle: float * sweepAngle: float * paint: Paint
    | Text of position: (float * float) * text: string * fill: Color
    | TextRun of run: TextRun
    | Image of bounds: (float * float * float * float) * source: string
    | ClipNode of clip: Clip * scene: Scene
    | RegionNode of region: Region * paint: Paint
    | ColorSpaceNode of colorSpace: ColorSpace * scene: Scene
    | PerspectiveNode of transform: PerspectiveTransform * scene: Scene
    | PictureNode of picture: Picture
    | Chart of values: float list
    | Translate of offset: (float * float) * scene: Scene
    | SizedText of position: (float * float) * text: string * size: float * fill: Color
    | GlyphRun of run: GlyphRun
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

// Feature 183 (US3): named, transposition-safe grouping of the 3 retained node counters for
// `damageRegion` (values/results unchanged).
type DamageNodeCounts =
    { Repainted: int
      Shifted: int
      Unaffected: int }

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
