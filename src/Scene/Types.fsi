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
