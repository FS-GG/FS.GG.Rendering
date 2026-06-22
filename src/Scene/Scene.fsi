namespace FS.GG.UI.Scene

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
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
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
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
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
