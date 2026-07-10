namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

// Feature 386: icon glyphs live in the design system, not the Controls geometry layer
// (`docs/product/layering.md` §1). `WidgetGeometry.iconGeom` previously baked a single hardcoded
// house `Path` and ignored the icon NAME entirely — so the public `Icon`/`IconButton` contract
// ("a glyph chosen from the icon set by name", `Control.fsi`) was unfulfilled: every name drew the
// house. This table restores name→glyph selection and moves ownership to the layer that owns it.
//
// `internal` (reached by `FS.GG.UI.Controls` and `Controls.Tests` via `InternalsVisibleTo`), so the
// design-system package's PUBLIC surface is unchanged — the same staging `DesignTokensExt`/
// `StyleResolver` used before their F5 promotion. A per-theme glyph set on `Theme` is the follow-up.
module internal IconGlyphs =

    // Each glyph is pure geometry: a filled path expressed from its centre (cx, cy) and radius r.
    // The design system owns the shape; the caller supplies the paint.

    /// The historical default. Relocated VERBATIM from `WidgetGeometry.iconGeom` so its output is
    /// byte-identical — the fixed 3.0px roof inset is preserved as-is, not re-derived proportionally.
    let private house (cx: float) (cy: float) (r: float) : PathSpec =
        Path.create
            Winding
            [ Path.moveTo (cx - r) cy
              Path.lineTo cx (cy - r)
              Path.lineTo (cx + r) cy
              Path.lineTo (cx + r - 3.0) cy
              Path.lineTo (cx + r - 3.0) (cy + r)
              Path.lineTo (cx - r + 3.0) (cy + r)
              Path.lineTo (cx - r + 3.0) cy
              Path.close ]

    /// A second, deliberately-distinct primitive so name→glyph selection is real and testable (not a
    /// one-entry table): a filled diamond with apexes up/right/down/left.
    let private diamond (cx: float) (cy: float) (r: float) : PathSpec =
        Path.create
            Winding
            [ Path.moveTo cx (cy - r)
              Path.lineTo (cx + r) cy
              Path.lineTo cx (cy + r)
              Path.lineTo (cx - r) cy
              Path.close ]

    // The vocabulary. Names are the icon-set keys the `Icon`/`IconButton` `name` attribute looks up.
    // `"house"`/`"home"` name the default glyph explicitly; new glyphs are added as one line each.
    let private table: Map<string, float -> float -> float -> PathSpec> =
        Map [ "house", house
              "home", house
              "diamond", diamond ]

    let pathFor (name: string) (cx: float) (cy: float) (r: float) : PathSpec =
        let glyph = table |> Map.tryFind name |> Option.defaultValue house
        glyph cx cy r
