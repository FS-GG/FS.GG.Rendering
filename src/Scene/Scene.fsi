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

/// One glyph in the Feature 140 proof data shape. This is a deterministic
/// package-owned representation for measurement, drawing, diagnostics, and
/// future cache/protocol work; it is not a full shaping engine.
type GlyphRunGlyph =
    { GlyphId: int
      SourceText: string
      Advance: float
      Offset: Point
      Cluster: int
      Position: Point }

/// Aggregate metrics for a glyph-run proof.
type GlyphRunMetrics =
    { Advance: float
      Height: float
      Baseline: float }

/// Stable glyph-run proof payload.
type GlyphRunData =
    { Text: string
      Font: FontSpec
      Glyphs: GlyphRunGlyph list
      Metrics: GlyphRunMetrics
      Fingerprint: string
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
