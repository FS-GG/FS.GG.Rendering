namespace FS.GG.UI.Scene

/// Public contract type exposed by this FS.GG.UI package.
type Size =
    { Width: int
      Height: int }

/// Public contract type exposed by this FS.GG.UI package.
type Color =
    { Red: byte
      Green: byte
      Blue: byte
      Alpha: byte }

/// Public contract type exposed by this FS.GG.UI package.
type Point =
    { X: float
      Y: float }

/// Public contract type exposed by this FS.GG.UI package.
type Rect =
    { X: float
      Y: float
      Width: float
      Height: float }

/// Public contract type exposed by this FS.GG.UI package.
type StrokeCap =
    | Butt
    | Round
    | Square

/// Public contract type exposed by this FS.GG.UI package.
type StrokeJoin =
    | Miter
    | RoundJoin
    | Bevel

/// Public contract type exposed by this FS.GG.UI package.
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

/// Public contract type exposed by this FS.GG.UI package.
type Stroke =
    { Width: float
      Cap: StrokeCap
      Join: StrokeJoin
      Miter: float }

/// Public contract type exposed by this FS.GG.UI package.
type Shader =
    | SolidColor of Color
    | LinearGradient of startPoint: Point * endPoint: Point * colors: Color list
    | RadialGradient of center: Point * radius: float * colors: Color list
    | SweepGradient of center: Point * colors: Color list

/// Public contract type exposed by this FS.GG.UI package.
type ColorFilter =
    | NoColorFilter
    | BlendColor of Color * BlendMode

/// Public contract type exposed by this FS.GG.UI package.
type MaskFilter =
    | NoMaskFilter
    | Blur of sigma: float

/// Public contract type exposed by this FS.GG.UI package.
type ImageFilter =
    | NoImageFilter
    | DropShadow of dx: float * dy: float * blur: float * color: Color

/// Public contract type exposed by this FS.GG.UI package.
type PathEffect =
    | NoPathEffect
    | Dash of intervals: float list * phase: float
    | Discrete of segmentLength: float * deviation: float
    | Corner of radius: float

/// Public contract type exposed by this FS.GG.UI package.
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

/// Public contract type exposed by this FS.GG.UI package.
type PathFillType =
    | Winding
    | EvenOdd

/// Public contract type exposed by this FS.GG.UI package.
type PathCommand =
    | MoveTo of Point
    | LineTo of Point
    | QuadTo of control: Point * point: Point
    | CubicTo of control1: Point * control2: Point * point: Point
    | ArcTo of bounds: Rect * startAngle: float * sweepAngle: float
    | Close

/// Public contract type exposed by this FS.GG.UI package.
type PathSpec =
    { Commands: PathCommand list
      FillType: PathFillType }

/// Public contract type exposed by this FS.GG.UI package.
type Clip =
    | RectClip of Rect
    | PathClip of PathSpec

/// Public contract type exposed by this FS.GG.UI package.
type RegionOperation =
    | Replace
    | RegionUnion
    | RegionIntersect
    | RegionDifference

/// Public contract type exposed by this FS.GG.UI package.
type Region =
    { Bounds: Rect list
      Operation: RegionOperation }

/// Public contract type exposed by this FS.GG.UI package.
type ColorSpace =
    | Srgb
    | DisplayP3
    | AdobeRgb

/// Public contract type exposed by this FS.GG.UI package.
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

/// Public contract type exposed by this FS.GG.UI package.
type PathOperation =
    | Union
    | Intersect
    | Difference
    | Xor

/// Public contract type exposed by this FS.GG.UI package.
type PathMeasure =
    { Length: float
      IsClosed: bool }

/// Public contract type exposed by this FS.GG.UI package.
type FontSpec =
    { Family: string option
      Size: float
      Weight: int option }

/// Public contract type exposed by this FS.GG.UI package.
type TextRun =
    { Text: string
      Position: Point
      Font: FontSpec
      Paint: Paint }

/// Public contract type exposed by this FS.GG.UI package.
type TextMetrics =
    { Width: float
      Height: float
      Baseline: float }

/// Direction evidence associated with a shaped text run.
type TextDirection =
    | AutoDirection
    | LeftToRight
    | RightToLeft
    | MixedDirection

/// Script bucket evidence associated with a shaped text run.
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

/// Availability state for the rendering-edge text shaping provider.
type ShapingProviderAvailability =
    | ProviderInstalled
    | ProviderCleared
    | ProviderUnavailable
    | ProviderFailed

/// Dependency-light provider evidence stored with shaped text results.
type ShapingProviderEvidence =
    { Availability: ShapingProviderAvailability
      ProviderId: string
      VersionBucket: string
      Failure: string option }

/// Fallback decision for one shaped run or glyph range.
type TextFallbackDecision =
    | AuthoredFace of family: string
    | SubstitutedFace of requested: string * resolved: string
    | MissingGlyphs of sourceText: string
    | PureFallback
    | ProviderFailure of message: string

/// One stable, drawable glyph emitted by a shaped text result.
type ShapedGlyph =
    { GlyphId: int
      SourceCluster: int
      SourceText: string
      ResolvedFace: string option
      Advance: float
      Offset: Point
      Position: Point
      Missing: bool }

/// One homogeneous text run and its shaping/fallback evidence.
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

/// Indicates whether a shaped text result came from shaping or explicit fallback.
type ShapedTextFallbackMode =
    | Shaped
    | PureFallbackMode
    | ProviderUnavailableFallback
    | ShapingFailedFallback

/// Aggregate metrics derived from a shaped text result.
type ShapedTextMetrics =
    { Advance: float
      Width: float
      Height: float
      Baseline: float
      Bounds: Rect option }

/// Dependency-light authoritative text payload for measurement, drawing, cache evidence, and diagnostics.
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

/// One glyph in the Feature 140 proof data shape. This is a deterministic
/// package-owned representation for measurement, drawing, diagnostics, and
/// future cache/protocol work; it is not a full shaping engine.
type GlyphRunGlyph =
    { GlyphId: int
      SourceText: string
      Advance: float
      Offset: Point
      Cluster: int
      Position: Point
      ResolvedFace: string option
      Missing: bool }

/// Aggregate metrics for a glyph-run proof.
type GlyphRunMetrics =
    { Advance: float
      Height: float
      Baseline: float }

/// Stable glyph-run proof payload.
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

/// Drawable glyph-run proof node payload.
type GlyphRun =
    { Data: GlyphRunData
      Position: Point
      Paint: Paint }

/// Public contract type exposed by this FS.GG.UI package.
type Vertex =
    { Position: Point
      Color: Color option }

/// Public contract type exposed by this FS.GG.UI package.
type VertexMode =
    | Triangles
    | TriangleStrip
    | TriangleFan

/// Public contract type exposed by this FS.GG.UI package.
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

/// Public contract type exposed by this FS.GG.UI package.
type RenderReadbackEvidence =
    { Size: Size
      CapabilityCount: int
      Capabilities: string list
      DeterministicHash: string }

/// Public contract type exposed by this FS.GG.UI package.
type ShapePlacement =
    | FullyInside
    | PartiallyOutOfBounds
    | FullyOutOfBounds

/// Public contract type exposed by this FS.GG.UI package.
type CircleShapeEvidence =
    { Center: Point
      Radius: float
      Bounds: Rect
      Fill: Color
      Placement: ShapePlacement }

/// Public contract type exposed by this FS.GG.UI package.
type EllipseShapeEvidence =
    { Bounds: Rect
      Fill: Color
      Placement: ShapePlacement }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutProofLevel =
    | ReadableLayout
    | DeterministicRenderOnly
    | UnsupportedLayoutInspection

/// Public contract type exposed by this FS.GG.UI package.
type LayoutMeasurementMode =
    | ExactTextBounds
    | ApproximateTextBounds
    | UnsupportedTextBounds

/// Public contract type exposed by this FS.GG.UI package.
type LayoutOverlapKind =
    | HudTextOverlap
    | HudGameplayOverlap
    | GameplayOutOfBounds

/// Public contract type exposed by this FS.GG.UI package.
type LayoutOverlapDiagnostic =
    { Kind: LayoutOverlapKind
      FirstName: string
      SecondName: string option
      Bounds: Rect
      Message: string }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutOverlapStatus =
    | NoLayoutOverlap
    | LayoutOverlaps of LayoutOverlapDiagnostic list

/// Public contract type exposed by this FS.GG.UI package.
type LayoutRegionEvidence =
    { Name: string
      Bounds: Rect }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutTextBounds =
    { Name: string
      Text: string
      Bounds: Rect
      MeasurementMode: LayoutMeasurementMode }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutGameplayBounds =
    { Name: string
      Bounds: Rect }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutUnsupportedReason =
    { Fact: string
      Reason: string
      Diagnostic: string }

/// Public contract type exposed by this FS.GG.UI package.
type DiagnosticSeverity =
    | Info
    | Warning
    | Error
    | Fatal

/// Public contract type exposed by this FS.GG.UI package.
type DiagnosticStage =
    | FrameRender

/// Public contract type exposed by this FS.GG.UI package.
type RenderDiagnostic =
    { Severity: DiagnosticSeverity
      Stage: DiagnosticStage
      Message: string
      Cause: string option }

/// Public contract type exposed by this FS.GG.UI package.
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
    /// Feature 120 (FR-007): a reuse-stable subtree marked as a backend replay-cache boundary.
    /// TRANSPARENT to every Scene-IR consumer except the OpenGL backend painter — `describe`,
    /// diagnostics, `measure`, opacity scaling, and every retained walk recurse straight into
    /// `CacheBoundary.Scene`, so deterministic goldens and at-rest pixels are unchanged. Only the GL
    /// painter consults the `SKPicture` replay cache here; with replay disabled it recurses into
    /// `Scene` identically to the direct walk (the parity oracle).
    | CachedSubtree of CacheBoundary

and Scene =
    { Nodes: SceneNode list }

and Picture =
    { Name: string
      Scene: Scene }

/// Feature 120 (FR-007): the payload of `SceneNode.CachedSubtree` — a stable subtree identity, a
/// collision-resistant structural fingerprint of its render-affecting inputs, and the wrapped
/// subtree itself (both the record source and the transparent fallback).
and CacheBoundary =
    { /// Stable subtree identity (from `RetainedId`) — the replay cache slot.
      CacheId: uint64
      /// Collision-resistant structural fingerprint of the wrapped subtree's render-affecting
      /// inputs; replay is valid iff a cached picture's fingerprint matches this.
      Fingerprint: uint64
      /// The wrapped subtree — record source and transparent fallback.
      Scene: Scene }

/// Public contract type exposed by this FS.GG.UI package.
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

/// Readiness result for one structured visual inspection scope.
[<RequireQualifiedAccess>]
type VisualInspectionStatus =
    /// All required deterministic inspection rules passed.
    | Accepted
    /// One or more blocking findings prevent readiness.
    | Blocked
    /// Required scope was declared but not fully inspected.
    | Incomplete
    /// Required facts are unavailable in the current implementation.
    | Unsupported
    /// Required facts are unavailable because of an explicit host or environment limitation.
    | EnvironmentLimited
    /// The scope is intentionally outside deterministic inspection coverage.
    | NotInspected
    /// The scope was declared but no inspection command ran.
    | NotRun

/// Severity of one visual inspection finding.
[<RequireQualifiedAccess>]
type VisualInspectionSeverity =
    /// The rule passed or was accepted by an explicit exception.
    | Pass
    /// Informational finding.
    | Info
    /// Non-blocking issue or caveat.
    | Warning
    /// Required readiness blocker.
    | Blocking
    /// Required fact cannot be inspected by the implementation.
    | Unsupported
    /// Host or environment prevents producing the fact.
    | EnvironmentLimited

/// Measurement confidence for text and geometric facts.
[<RequireQualifiedAccess>]
type VisualInspectionMeasurementMode =
    /// Fact is measured from authoritative render/layout data.
    | Exact
    /// Fact is derived from deterministic approximation.
    | Approximate
    /// Fact is not implemented by the inspector.
    | Unsupported
    /// Fact was unavailable in this run.
    | Unavailable

/// Text fit classification for one inspected text run.
[<RequireQualifiedAccess>]
type VisualInspectionFitStatus =
    /// Text is contained inside its owner bounds.
    | Inside
    /// Text extends outside its owner bounds.
    | Overflow
    /// Text is clipped by its owner or effective clip.
    | Clipped
    /// Text intentionally wraps.
    | Wrapped
    /// Text is intentionally truncated.
    | Truncated
    /// Text fit cannot be inspected by the implementation.
    | Unsupported
    /// Text fit was unavailable in this run.
    | Unavailable

/// Reviewer-facing visual node kind.
[<RequireQualifiedAccess>]
type VisualInspectionNodeKind =
    /// Root of the inspected visual tree.
    | Root
    /// Container or grouping node.
    | Container
    /// Text-bearing node.
    | Text
    /// Shape or primitive paint node.
    | Shape
    /// Image-bearing node.
    | Image
    /// Overlay surface.
    | Overlay
    /// Popup surface.
    | Popup
    /// Node kind is known only as an implementation string.
    | Custom of string
    /// Node kind is unavailable.
    | Unknown

/// Paint contribution role for a node or coverage fact.
[<RequireQualifiedAccess>]
type VisualInspectionPaintRole =
    /// Background paint.
    | Background
    /// Surface paint.
    | Surface
    /// Border paint.
    | Border
    /// Foreground paint.
    | Foreground
    /// Content paint.
    | Content
    /// Overlay paint.
    | Overlay
    /// No paint contribution was observed.
    | None
    /// Paint role could not be classified.
    | Unknown

/// Semantic surface role used for containment and overlap checks.
[<RequireQualifiedAccess>]
type VisualInspectionSurfaceRole =
    /// Root screen surface.
    | Root
    /// Shell or page frame.
    | Shell
    /// Primary content region.
    | Content
    /// Navigation region.
    | Navigation
    /// Feedback/status region.
    | Feedback
    /// Overlay region.
    | Overlay
    /// Popup region.
    | Popup
    /// Floating non-popup region.
    | Floating
    /// Caller-defined surface role.
    | Custom of string
    /// Surface role could not be classified.
    | Unknown

/// Classification for effective clipping.
[<RequireQualifiedAccess>]
type VisualInspectionClipStatus =
    /// No clipping affects the node or region.
    | None
    /// Clipping is intentional and owned.
    | Intentional
    /// Clipping is accidental and blocks readiness.
    | Accidental
    /// Clipping cannot be inspected by the implementation.
    | Unsupported
    /// Clipping was unavailable in this run.
    | Unavailable

/// Coverage state for a paint contribution.
[<RequireQualifiedAccess>]
type VisualInspectionCoverageStatus =
    /// Target is fully covered by intentional paint.
    | Complete
    /// Target is only partially covered.
    | Partial
    /// Required paint is missing.
    | Missing
    /// Coverage cannot be inspected by the implementation.
    | Unsupported
    /// Coverage was unavailable in this run.
    | Unavailable

/// Scope identity for one inspected page, screen, or control tree.
type VisualInspectionScope =
    { ScopeId: string
      Title: string
      Required: bool }

/// Explicit unsupported or unavailable fact recorded by an inspector.
type VisualInspectionUnsupportedFact =
    { Fact: string
      OwnerId: string option
      Required: bool
      Reason: string
      Diagnostic: string
      EnvironmentLimited: bool }

/// One inspected visual node with final bounds and relationship metadata.
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

/// Measured text facts for one rendered text run.
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

/// Named visual region used for containment, overlap, and paint checks.
type VisualRegionBoundary =
    { RegionId: string
      Name: string
      Role: VisualInspectionSurfaceRole
      Bounds: Rect option
      Required: bool
      OwnerNodeIds: string list
      AllowedOverlapRoles: VisualInspectionSurfaceRole list }

/// Evidence that a region or node has intentional paint coverage.
type VisualPaintCoverage =
    { CoverageId: string
      TargetId: string
      PaintRole: VisualInspectionPaintRole
      CoverageBounds: Rect option
      CoverageStatus: VisualInspectionCoverageStatus
      Reason: string option }

/// Effective clipping evidence for one node or region.
type VisualClipFact =
    { ClipId: string
      NodeId: string
      ClipBounds: Rect option
      ClipStatus: VisualInspectionClipStatus
      Reason: string option
      AffectedTextRunIds: string list }

/// One deterministic validation finding tied to a rule and affected visual ids.
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

/// Machine-checkable inspection evidence for one scope.
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

/// Aggregate summary over one or more inspection artifacts and validation results.
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

/// Readiness result for retained-render inspection evidence.
[<RequireQualifiedAccess>]
type RetainedInspectionStatus =
    /// Evidence was produced, validated, and accepted for the inspected scope.
    | Accepted
    /// Validation found a blocking retained-render or damage-locality issue.
    | Blocked
    /// Evidence was produced but needs human review before it can be accepted.
    | ReviewRequired
    /// The requested retained or damage fact is not supported by the inspected path.
    | Unsupported
    /// The environment prevented complete retained inspection while still producing useful evidence.
    | EnvironmentLimited
    /// The scope was intentionally skipped by the inspection request.
    | NotInspected
    /// The inspection did not execute and no evidence was produced.
    | NotRun

/// Retained-render node transition classification.
[<RequireQualifiedAccess>]
type RetainedNodeStatus =
    /// The retained renderer preserved the node identity across the transition.
    | Retained
    /// The node was matched and reused without needing repaint evidence.
    | Reused
    /// The node was matched and repainted within the transition.
    | Repainted
    /// The node was matched and moved between prior and current bounds.
    | Shifted
    /// The node both moved and repainted in the same transition.
    | ShiftedAndRepainted
    /// The node appears in the current frame but not the prior frame.
    | Added
    /// The node appears in the prior frame but not the current frame.
    | Removed
    /// The node was present and unaffected by the transition.
    | Unaffected
    /// The node could not be classified with the available retained facts.
    | Unsupported

/// Visible dirty-region classification for retained damage inspection.
[<RequireQualifiedAccess>]
type DamageInspectionStatus =
    /// No visible dirty region was reported for the transition.
    | Empty
    /// Dirty area is bounded to the expected affected region set.
    | Localized
    /// Dirty area is wider than expected but does not cover the full frame.
    | Broad
    /// Dirty area covers the full visible frame.
    | FullSurface
    /// Damage facts are unavailable or unsupported for the inspected path.
    | Unsupported
    /// Damage inspection was intentionally not performed.
    | NotInspected

/// Reviewed allowance for broad or full-surface retained damage.
///
/// Use exceptions to keep deliberate broad damage visible in evidence instead of
/// silently downgrading a validation finding.
type IntentionalDamageException =
    { ExceptionId: string
      RuleId: string
      ScopeId: string
      TransitionId: string
      AffectedIds: string list
      Reason: string
      ExpiresWith: string option }

/// Before/after frame identity and scenario expectations for retained inspection.
///
/// The transition connects retained node facts to the interaction and expected
/// affected visual regions that validators use for damage-locality checks.
type RetainedFrameTransition =
    { TransitionId: string
      PriorFrameId: string option
      CurrentFrameId: string
      InteractionId: string option
      ExpectedAffectedRegionIds: string list
      MaximumDirtyPercentage: float option
      IntentionalExceptions: IntentionalDamageException list }

/// Stable fact about one retained visual node.
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

/// Visible retained-render damage facts for one transition.
///
/// Dirty area values are expressed in visible frame coordinates. `UnionArea`
/// is the true clipped union of dirty rectangles, not the area of the bounding
/// rectangle.
///
/// Feature 183 (US3): the three transposable retained node counters `damageRegion` takes, grouped and
/// named so a caller cannot silently swap repainted/shifted/unaffected (a transposition is now a
/// compile error). Values and results are unchanged.
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

/// Validation finding for retained node and damage locality evidence.
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

/// Machine-checkable retained-render evidence for one inspected scope or transition.
///
/// The artifact can embed the final visual inspection artifact so retained
/// facts, damage facts, and screenshot/readback evidence stay correlated.
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

/// Reviewer- and machine-readable retained inspection rollup.
///
/// Summaries are intended for readiness reports: they preserve blocking
/// findings, unsupported facts, accepted exceptions, related visual evidence,
/// command evidence, caveats, and diagnostics.
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

/// Public contract module exposed by this FS.GG.UI package.
module Colors =
    /// Public contract function exposed by this FS.GG.UI package.
    val rgba: red: byte -> green: byte -> blue: byte -> alpha: byte -> Color
    /// Public contract function exposed by this FS.GG.UI package.
    val rgb: red: byte -> green: byte -> blue: byte -> Color
    /// Public contract function exposed by this FS.GG.UI package.
    val black: Color
    /// Public contract function exposed by this FS.GG.UI package.
    val white: Color
    /// Public contract function exposed by this FS.GG.UI package.
    val transparent: Color

/// Public contract module exposed by this FS.GG.UI package.
module Paint =
    /// Public contract function exposed by this FS.GG.UI package.
    val fill: color: Color -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val stroke: color: Color -> width: float -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val withOpacity: opacity: float -> paint: Paint -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val withBlendMode: blendMode: BlendMode -> paint: Paint -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val withAntialias: antialias: bool -> paint: Paint -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val withStrokeCap: cap: StrokeCap -> paint: Paint -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val withStrokeJoin: join: StrokeJoin -> paint: Paint -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val withMiter: miter: float -> paint: Paint -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val withShader: shader: Shader -> paint: Paint -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val withColorFilter: filter: ColorFilter -> paint: Paint -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val withMaskFilter: filter: MaskFilter -> paint: Paint -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val withImageFilter: filter: ImageFilter -> paint: Paint -> Paint
    /// Public contract function exposed by this FS.GG.UI package.
    val withPathEffect: effect: PathEffect -> paint: Paint -> Paint

/// Public contract module exposed by this FS.GG.UI package.
module Path =
    /// Public contract function exposed by this FS.GG.UI package.
    val create: fillType: PathFillType -> commands: PathCommand list -> PathSpec
    /// Public contract function exposed by this FS.GG.UI package.
    val moveTo: x: float -> y: float -> PathCommand
    /// Public contract function exposed by this FS.GG.UI package.
    val lineTo: x: float -> y: float -> PathCommand
    /// Public contract function exposed by this FS.GG.UI package.
    val quadTo: control: Point -> point: Point -> PathCommand
    /// Public contract function exposed by this FS.GG.UI package.
    val cubicTo: control1: Point -> control2: Point -> point: Point -> PathCommand
    /// Public contract function exposed by this FS.GG.UI package.
    val close: PathCommand
    /// Public contract function exposed by this FS.GG.UI package.
    val bounds: path: PathSpec -> Rect option
    /// Public contract function exposed by this FS.GG.UI package.
    val measure: path: PathSpec -> PathMeasure
    /// Public contract function exposed by this FS.GG.UI package.
    val segment: startDistance: float -> endDistance: float -> path: PathSpec -> PathSpec
    /// Public contract function exposed by this FS.GG.UI package.
    val combine: operation: PathOperation -> left: PathSpec -> right: PathSpec -> PathSpec

/// Public contract module exposed by this FS.GG.UI package.
module Scene =
    /// Public contract function exposed by this FS.GG.UI package.
    val empty: Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val group: scenes: Scene list -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val rectangle: bounds: float * float * float * float -> fill: Color -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val rectangleWithPaint: bounds: Rect -> paint: Paint -> Scene
    /// Self-describing, `Rect`-based rectangle constructor (parallels `filledEllipse`);
    /// avoids the positional `(float * float * float * float)` arity slip.
    val filledRectangle: bounds: Rect -> fill: Color -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val circle: center: Point -> radius: float -> fill: Color -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val filledEllipse: bounds: Rect -> fill: Color -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val ellipse: bounds: Rect -> paint: Paint -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val line: startPoint: Point -> endPoint: Point -> paint: Paint -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val path: path: PathSpec -> paint: Paint -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val points: points: Point list -> paint: Paint -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val vertices: mode: VertexMode -> vertices: Vertex list -> paint: Paint -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val arc: bounds: Rect -> startAngle: float -> sweepAngle: float -> paint: Paint -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val text: position: float * float -> text: string -> color: Color -> Scene
    /// Self-describing, `Point`-based text constructor (parallels `circle`);
    /// avoids the positional `(float * float)` arity slip.
    val textAt: position: Point -> text: string -> color: Color -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val textRun: run: TextRun -> Scene
    /// Build deterministic glyph-run proof data using the dependency-light Scene measurement heuristic.
    val buildGlyphRun: text: string -> font: FontSpec -> GlyphRunData
    /// Build dependency-light pure-fallback shaped text evidence without a rendering-edge provider.
    val buildFallbackShapedText: text: string -> font: FontSpec -> ShapedTextResult
    /// Deterministic fingerprint over shaped text evidence.
    val shapedTextFingerprint: result: ShapedTextResult -> string
    /// Project shaped text aggregate metrics into the existing `TextMetrics` shape.
    val measureShapedText: result: ShapedTextResult -> TextMetrics
    /// Convert a shaped text result into drawable glyph-run data.
    val glyphRunDataFromShapedText: result: ShapedTextResult -> GlyphRunData
    /// Deterministic fingerprint over glyph-run proof data.
    val glyphRunFingerprint: data: GlyphRunData -> string
    /// Measure the already-built glyph-run proof data.
    val measureGlyphRun: data: GlyphRunData -> TextMetrics
    /// Draw already-built glyph-run proof data at `position` with `paint`.
    val glyphRun: position: Point -> data: GlyphRunData -> paint: Paint -> Scene
    /// Convenience constructor that builds proof data and emits a drawable glyph-run proof node.
    val glyphRunProof: position: Point -> text: string -> font: FontSpec -> paint: Paint -> Scene
    /// The pure, host-independent text-measure heuristic (calibrated to the bundled default family;
    /// deliberately conservative so a box sized by it is never narrower than the renderer draws).
    val measureText: text: string -> font: FontSpec -> TextMetrics
    /// Feature 136 (R2/FR-002): install (`Some`) or clear (`None`) a real-metrics text measurer used by
    /// `measureTextResolved`. The rendering edge (`SkiaViewer.Fonts`) injects a measurer matching the
    /// bundled-font renderer's advances, so the advance used to size a text box equals the advance used
    /// to draw it (no mid-word clip). Process-wide; the pure `measureText` heuristic is unchanged.
    val setRealTextMeasurer: measurer: (string -> FontSpec -> TextMetrics) option -> unit
    /// Current version bucket for the active text measurement provider/fallback path.
    val textMeasurementVersionBucket: unit -> string
    /// Set the active text measurement version bucket used by retained cache keys.
    val setTextMeasurementVersionBucket: bucket: string -> unit
    /// Measure via the installed real measurer when present, else the pure `measureText` heuristic.
    /// With no measurer installed this is byte-identical to `measureText` (pure-caller default).
    val measureTextResolved: text: string -> font: FontSpec -> TextMetrics
    /// Public contract function exposed by this FS.GG.UI package.
    val image: bounds: float * float * float * float -> source: string -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val clipped: clip: Clip -> scene: Scene -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val region: region: Region -> paint: Paint -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val withColorSpace: colorSpace: ColorSpace -> scene: Scene -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val withPerspective: transform: PerspectiveTransform -> scene: Scene -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val picture: picture: Picture -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val chart: values: float list -> Scene
    /// Offset an entire sub-scene by (dx, dy). Offsets ALL node kinds uniformly —
    /// including Path/Points/Vertices/Chart — by pushing a canvas translation, so it
    /// replaces a hand-written coordinate-walking shift. Nesting composes additively.
    val translate: dx: float -> dy: float -> scene: Scene -> Scene
    /// A Text node with an explicit font size, for chrome sized to its container.
    /// Bare `Scene.text` (no size) keeps its current default-font rendering.
    val sizedText: position: (float * float) -> text: string -> size: float -> color: Color -> Scene
    /// Public contract function exposed by this FS.GG.UI package.
    val describe: scene: Scene -> SceneElementKind list
    /// Public contract function exposed by this FS.GG.UI package.
    val diagnostics: scene: Scene -> RenderDiagnostic list
    /// Public contract function exposed by this FS.GG.UI package.
    val renderReadbackEvidence: size: Size -> scene: Scene -> RenderReadbackEvidence
    /// Public contract function exposed by this FS.GG.UI package.
    val circleEvidence: outputSize: Size -> center: Point -> radius: float -> fill: Color -> CircleShapeEvidence
    /// Public contract function exposed by this FS.GG.UI package.
    val ellipseEvidence: outputSize: Size -> bounds: Rect -> fill: Color -> EllipseShapeEvidence

/// Public contract type exposed by this FS.GG.UI package.
type SceneEvidenceFormat =
    | Hash
    | Png
    | Metadata

/// Public contract type exposed by this FS.GG.UI package.
type SceneEvidenceFailureClassification =
    | UnsupportedEnvironment
    | ProductDefect

/// Public contract type exposed by this FS.GG.UI package.
type SceneEvidenceFailure =
    { BlockedStage: string
      Classification: SceneEvidenceFailureClassification
      DiagnosticCategory: string
      Message: string }

/// Public contract type exposed by this FS.GG.UI package.
type SceneEvidenceRequest =
    { Scene: Scene
      OutputSize: Size
      Format: SceneEvidenceFormat
      RendererMode: string
      EvidencePath: string option }

/// Public contract type exposed by this FS.GG.UI package.
type SceneEvidence =
    { Format: SceneEvidenceFormat
      OutputSize: Size
      RendererMode: string
      EvidencePath: string option
      Value: string }

/// Public contract module exposed by this FS.GG.UI package.
module SceneEvidence =
    /// Public contract function exposed by this FS.GG.UI package.
    val render: request: SceneEvidenceRequest -> Result<SceneEvidence, SceneEvidenceFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val renderHash: size: Size -> scene: Scene -> Result<SceneEvidence, SceneEvidenceFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val renderPng: size: Size -> scene: Scene -> Result<byte[], SceneEvidenceFailure>

/// Public contract module exposed by this FS.GG.UI package.
module LayoutEvidence =
    /// Public contract function exposed by this FS.GG.UI package.
    val classify: report: LayoutEvidenceReport -> LayoutEvidenceReport
    /// Public contract function exposed by this FS.GG.UI package.
    val fromRenderEvidence: scene: Scene -> evidence: RenderReadbackEvidence -> LayoutEvidenceReport
    /// Public contract function exposed by this FS.GG.UI package.
    val unsupported: scene: Scene -> outputSize: Size -> reason: LayoutUnsupportedReason -> LayoutEvidenceReport

/// Dependency-light helpers for structured visual inspection evidence.
module VisualInspection =
    /// Stable lowercase token for an inspection readiness status.
    val statusText: status: VisualInspectionStatus -> string
    /// Stable lowercase token for a finding severity.
    val severityText: severity: VisualInspectionSeverity -> string
    /// Stable lowercase token for a measurement mode.
    val measurementModeText: mode: VisualInspectionMeasurementMode -> string
    /// Stable lowercase token for text fit status.
    val fitStatusText: status: VisualInspectionFitStatus -> string
    /// Stable lowercase token for a node kind.
    val nodeKindText: kind: VisualInspectionNodeKind -> string
    /// Stable lowercase token for paint role.
    val paintRoleText: role: VisualInspectionPaintRole -> string
    /// Stable lowercase token for surface role.
    val surfaceRoleText: role: VisualInspectionSurfaceRole -> string
    /// Stable lowercase token for clipping status.
    val clipStatusText: status: VisualInspectionClipStatus -> string
    /// Stable lowercase token for paint coverage status.
    val coverageStatusText: status: VisualInspectionCoverageStatus -> string
    /// Create an explicit unsupported fact.
    val unsupportedFact:
        fact: string ->
        ownerId: string option ->
        required: bool ->
        reason: string ->
        diagnostic: string ->
        environmentLimited: bool ->
            VisualInspectionUnsupportedFact
    /// Build a stable finding id from a rule id and affected ids.
    val stableFindingId: ruleId: string -> affectedIds: string list -> string
    /// Create a deterministic finding with a generated stable id.
    val finding:
        ruleId: string ->
        severity: VisualInspectionSeverity ->
        affectedNodeIds: string list ->
        affectedRegionIds: string list ->
        message: string ->
        expected: string ->
        actual: string ->
            VisualInspectionFinding
    /// Validate artifact identity, ordering, and unsupported-fact disclosure.
    val artifactDiagnostics: artifact: VisualInspectionArtifact -> string list
    /// Sort nodes, regions, text runs, findings, and unsupported facts deterministically.
    val normalizeArtifact: artifact: VisualInspectionArtifact -> VisualInspectionArtifact

/// Dependency-light helpers for retained-render and damage-locality inspection.
module RetainedInspection =
    /// Stable lowercase token for a retained readiness status.
    val statusText: status: RetainedInspectionStatus -> string
    /// Stable lowercase token for a retained node status.
    val nodeStatusText: status: RetainedNodeStatus -> string
    /// Stable lowercase token for a damage status.
    val damageStatusText: status: DamageInspectionStatus -> string
    /// Create an explicit retained/damage unsupported fact.
    val unsupportedFact:
        fact: string ->
        ownerId: string option ->
        required: bool ->
        reason: string ->
        diagnostic: string ->
        environmentLimited: bool ->
            VisualInspectionUnsupportedFact
    /// Build a stable retained finding id from a rule, transition, and affected ids.
    val stableFindingId: ruleId: string -> transitionId: string -> affectedIds: string list -> string
    /// Create a deterministic retained/damage finding.
    val finding:
        ruleId: string ->
        severity: VisualInspectionSeverity ->
        transitionId: string ->
        affectedNodeIds: string list ->
        affectedRegionIds: string list ->
        message: string ->
        expected: string ->
        actual: string ->
            DamageLocalityFinding
    /// Compute the true visible union area of dirty rectangles clipped to a frame.
    val dirtyUnionArea: frameBounds: Rect -> dirtyRectangles: Rect list -> int
    /// Compute the bounding rectangle of clipped dirty rectangles.
    val dirtyUnionBounds: frameBounds: Rect -> dirtyRectangles: Rect list -> Rect option
    /// Build visible damage evidence from dirty rectangles and retained counters.
    ///
    /// The returned `DirtyPercentage` is computed from the true clipped dirty
    /// union area divided by the visible frame area.
    val damageRegion:
        transitionId: string ->
        frameBounds: Rect ->
        dirtyRectangles: Rect list ->
        expectedAffectedRegionIds: string list ->
        affectedNodeIds: string list ->
        nodeCounts: DamageNodeCounts ->
        cause: string option ->
        maximumDirtyPercentage: float option ->
            DamageRegionInspection
    /// Validate artifact identity, retained node bounds, and unsupported-fact disclosure.
    val artifactDiagnostics: artifact: RetainedInspectionArtifact -> string list
    /// Sort retained nodes, damage facts, findings, and unsupported facts deterministically.
    val normalizeArtifact: artifact: RetainedInspectionArtifact -> RetainedInspectionArtifact
