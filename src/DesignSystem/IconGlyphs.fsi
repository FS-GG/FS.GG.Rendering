namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

/// Feature 386: the icon glyph vocabulary — a design-system concern (`docs/product/layering.md` §1:
/// icons are Theme/design-system-owned, NOT baked into the Controls geometry layer). Maps an
/// icon-set NAME to a filled glyph `PathSpec` positioned by its centre and radius; the caller
/// (`WidgetGeometry.iconGeom`) supplies the paint, the design system owns the shape.
///
/// `internal` for now, reached by `FS.GG.UI.Controls` (and the Controls test assembly) via
/// `InternalsVisibleTo` — the same internal→public staging `DesignTokensExt`/`StyleResolver` used
/// before their F5 promotion. This move establishes design-system ownership and the name→glyph
/// seam; a per-theme glyph set carried on `Theme` is the follow-up.
module internal IconGlyphs =

    /// The glyph the icon set maps `name` to, as a filled path centred at (`cx`, `cy`) with radius
    /// `r`. Unknown names fall back to the historical default `"house"` glyph, so every name that
    /// reached `iconGeom` before this change renders byte-identically. Total + deterministic.
    val pathFor: name: string -> cx: float -> cy: float -> r: float -> PathSpec
