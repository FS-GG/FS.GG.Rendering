namespace FS.GG.UI.Scene.Text

open FS.GG.UI.Scene

/// Feature 188 (US2): the unified shaped-text core and the `realTextMeasurer` single owner, relocated
/// out of `module Scene`. The module is `internal` — `module Scene` re-exposes the public entry points
/// as thin delegations, so the public package surface is unchanged (surface-neutral relocation).
module internal Shaping =
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
    /// The pure, host-independent text-measure heuristic.
    val measureText: text: string -> font: FontSpec -> TextMetrics
    /// Install (`Some`) or clear (`None`) the process-wide real-metrics text measurer.
    val setRealTextMeasurer: measurer: (string -> FontSpec -> TextMetrics) option -> unit
    /// Current version bucket for the active text measurement provider/fallback path.
    val textMeasurementVersionBucket: unit -> string
    /// Set the active text measurement version bucket used by retained cache keys.
    val setTextMeasurementVersionBucket: bucket: string -> unit
    /// Measure via the installed real measurer when present, else the pure `measureText` heuristic.
    val measureTextResolved: text: string -> font: FontSpec -> TextMetrics
