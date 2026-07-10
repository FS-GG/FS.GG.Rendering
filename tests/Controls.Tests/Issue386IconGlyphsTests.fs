module Issue386IconGlyphsTests

// Feature 386: the icon glyph vocabulary moved out of the Controls geometry layer into the
// design system (`IconGlyphs`), keyed by icon-set NAME. `WidgetGeometry.iconGeom` now looks the
// glyph up instead of baking a single house `Path`, so the public `Icon`/`IconButton` `name`
// attribute finally selects a glyph. These tests pin:
//   * byte-identity of the default (house) glyph — a regression guard against a future "cleanup"
//     silently altering rendered output;
//   * the legacy fallback (any unknown name → house), which keeps every pre-386 render identical;
//   * that a distinct name yields a distinct glyph (selection is real, not a one-entry table);
//   * that `iconGeom` routes the name through the table end-to-end (the seam reaches the screen).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light

// The exact commands `iconGeom` baked before #386, reproduced here so the test fails loudly if the
// house glyph ever drifts. `iconGeom` positions the glyph at (box.X + 22, box.Y + box.Height/2, 16).
let private expectedHouse (cx: float) (cy: float) (r: float) : PathSpec =
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

// The filled path `iconGeom` emits first (the glyph), for a given icon name.
let private iconPathSpec (name: string) (box: Rect) : PathSpec option =
    match WidgetGeometry.iconGeom theme box name with
    | first :: _ ->
        match first.Nodes with
        | Path(spec, _) :: _ -> Some spec
        | _ -> None
    | _ -> None

[<Tests>]
let feature386IconGlyphsTests =
    testList "Feature 386 icon glyphs" [

        test "house glyph is byte-identical to the pre-386 hardcoded iconGeom path" {
            let cx, cy, r = 22.0, 10.0, 16.0
            Expect.equal (IconGlyphs.pathFor "house" cx cy r) (expectedHouse cx cy r) "house path unchanged"
        }

        test "an unknown icon name falls back to the house glyph (byte-identical legacy behaviour)" {
            let cx, cy, r = 5.0, 7.0, 16.0
            Expect.equal
                (IconGlyphs.pathFor "no-such-glyph" cx cy r)
                (IconGlyphs.pathFor "house" cx cy r)
                "unknown name → house"
        }

        test "a registered name selects a distinct glyph (name→glyph selection is real)" {
            let cx, cy, r = 5.0, 7.0, 16.0
            Expect.notEqual
                (IconGlyphs.pathFor "diamond" cx cy r)
                (IconGlyphs.pathFor "house" cx cy r)
                "diamond ≠ house"
        }

        test "iconGeom routes the name through the glyph table end-to-end" {
            let box = { X = 0.0; Y = 0.0; Width = 120.0; Height = 40.0 }
            let cx, cy, r = box.X + 22.0, box.Y + box.Height / 2.0, 16.0
            // The default/house path is byte-identical through the whole render seam.
            Expect.equal (iconPathSpec "house" box) (Some(expectedHouse cx cy r)) "iconGeom house is unchanged"
            // A different name reaches a different glyph — the selection is not swallowed by iconGeom.
            Expect.notEqual (iconPathSpec "diamond" box) (iconPathSpec "house" box) "iconGeom diamond ≠ house"
        }
    ]
