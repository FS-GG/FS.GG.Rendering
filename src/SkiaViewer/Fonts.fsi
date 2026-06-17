namespace FS.GG.UI.SkiaViewer

open SkiaSharp
open FS.GG.UI.Scene

/// Feature 136 (FR-001/FR-002/SC-005): the bundled-font registry. Every glyph renders through a real
/// embedded typeface loaded via `SKTypeface.FromStream` from this assembly's manifest resources —
/// never the host's `SKTypeface.Default` — so text output is host-independent (the determinism basis
/// for byte-identical same-seed evidence). A per-character fallback chain (requested family → other
/// bundled families → deliberate ASCII substitute → disclosed tofu box) renders authored characters
/// correctly and **never** a different-but-plausible glyph (the `@`→`7` defect). Resolutions are
/// disclosed (FR-001). Typefaces/`SKFont`s are cached per `(family, weight, size)`.
module Fonts =

    /// What actually rendered for one source character (FR-001 disclosure). The only permitted
    /// outcomes — there is no "wrong but plausible glyph" case.
    [<RequireQualifiedAccess>]
    type FallbackResolution =
        /// Drawn from a bundled face that covers the character (the normal case: `@`, `#`, letters).
        | Authored of family: string
        /// A deliberate, legible swap for an uncovered decorative (`—`→`–`, `▸`→`>`, `·`→`•`), drawn
        /// from `family`.
        | Substituted of original: char * substitute: char * family: string
        /// No bundled coverage and no deliberate substitute → a visible missing-glyph box.
        | Tofu of original: char

    /// Per-character resolution: the source character, the glyph actually drawn, the `SKFont` that
    /// covers it (its advance is the draw advance), and the disclosure.
    type ResolvedChar =
        { Original: char
          Rendered: char
          Font: SKFont
          Resolution: FallbackResolution }

    /// Disclosure aggregate for a rendered string or page (FR-001 evidence record).
    type FallbackReport =
        { SubstitutedCount: int
          TofuCount: int
          AffectedCodePoints: int list }

    /// Rendering-edge text shaping provider status.
    type TextShapingProviderStatus =
        { Evidence: ShapingProviderEvidence
          Diagnostics: string list }

    /// The default proportional family used when a request carries no family (`Noto Sans`).
    val defaultSansFamily: string
    /// The default monospace family (`Noto Sans Mono`).
    val defaultMonoFamily: string

    /// Resolve a `FontSpec` to its primary cached `SKFont` (family + weight + size). Loads the embedded
    /// faces on first use; **fails loudly** (raises) if a bundled asset is missing (Principle VI).
    val resolveFont: font: FontSpec -> SKFont

    /// Resolve each character of `text` to the font that covers it, following the fixed per-character
    /// fallback chain. Pure (no disclosure side effects); the renderer records disclosures as it draws.
    val resolveText: font: FontSpec -> text: string -> ResolvedChar list

    /// The draw/measure advance of one resolved character (tofu boxes advance ≈0.6·size; every other
    /// character advances by its drawing font's real glyph advance). Internal: the renderer advances
    /// the pen by exactly this so measure == draw.
    val internal charAdvance: size: float -> rc: ResolvedChar -> float

    /// Total advance width of `text` under `font` = the sum of the per-character advances of the fonts
    /// that draw each character. Equals exactly what the renderer advances by, so a box sized from this
    /// fits the drawn text (FR-002).
    val measureWidth: font: FontSpec -> text: string -> float

    /// Real-metrics measurer matching the renderer, shaped for the `Scene` measurement seam
    /// (`Scene.setRealTextMeasurer`). `Height`/`Baseline` follow the resolved primary font's metrics.
    val realMeasure: text: string -> font: FontSpec -> TextMetrics

    /// Install the HarfBuzz-backed shaping provider and the matching measurement seam.
    val installShapingProvider: unit -> TextShapingProviderStatus

    /// Clear the shaping provider and return to explicit bundled-font fallback evidence.
    val clearShapingProvider: unit -> TextShapingProviderStatus

    /// Read the active shaping provider state.
    val shapingProviderStatus: unit -> TextShapingProviderStatus

    /// Shape text when the provider is installed, otherwise return explicit fallback evidence.
    val shapeText: text: string -> font: FontSpec -> ShapedTextResult

    /// Build drawable glyph-run data from `shapeText`.
    val buildShapedGlyphRunData: text: string -> font: FontSpec -> GlyphRunData

    /// Build Feature 140 glyph-run proof data using the same bundled-font fallback and advances that
    /// the renderer uses to draw text. This is a proof helper, not full shaping.
    val buildGlyphRunData: text: string -> font: FontSpec -> GlyphRunData

    /// Aggregate per-character resolutions into a disclosure report.
    val report: resolved: ResolvedChar list -> FallbackReport

    /// A structured diagnostic line for every non-`Authored` outcome (FR-001 disclosure at the use site).
    val diagnostics: resolved: ResolvedChar list -> string list

    /// Install this registry's real measurer into the `Scene` measurement seam so control box sizing
    /// uses true advances. Idempotent; the host calls this once before layout.
    val installMeasurementSeam: unit -> unit
