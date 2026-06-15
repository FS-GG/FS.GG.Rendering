module Feature108ThemingTests

// Feature 108 (US6) — `Theming.resolve` (mode + accent -> RolePalette) and `Theming.toTheme`
// (palette -> framework Theme) are pure projections, and `FS.GG.UI.Color.Contrast.ratio` matches
// the WCAG relative-luminance reference for known pairs with the AA thresholds checkable via
// `Contrast.verdict` (SC-010). Contrast is REUSED, not re-implemented ([[fs-gg-design-tokens]]).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Theming

let private rgb r g b : Color =
    { Red = byte r
      Green = byte g
      Blue = byte b
      Alpha = 255uy }

[<Tests>]
let tests =
    testList "Feature 108 theming + WCAG contrast (US6, FR-017/018, SC-010)" [
        test "resolve places the accent on the Accent and FocusRing roles (FR-017)" {
            let accent = rgb 255 0 0
            let palette = Theming.resolve Light accent
            Expect.equal palette.Accent accent "accent role = supplied accent"
            Expect.equal palette.FocusRing accent "focus ring tracks the accent"
            Expect.equal palette.Background Theme.light.Background "neutral background seeded from the Light base"
        }

        test "toTheme projects the role palette onto the framework Theme (FR-018)" {
            let accent = rgb 0 128 255
            let palette = Theming.resolve Dark accent
            let theme = Theming.toTheme palette
            Expect.equal theme.Accent accent "the paint theme carries the resolved accent exactly"
            Expect.equal theme.Background palette.Background "background projected onto the Theme"
            Expect.equal theme.Foreground palette.Foreground "foreground projected onto the Theme"
        }

        test "Contrast.ratio matches the WCAG reference: black on white = 21:1 (SC-010)" {
            let ratio = FS.GG.UI.Color.Contrast.ratio (rgb 0 0 0) (rgb 255 255 255)
            Expect.isTrue (abs (ratio - 21.0) < 0.1) (sprintf "black/white is the 21:1 WCAG maximum (got %f)" ratio)
        }

        test "AA thresholds are checkable via Contrast.verdict (SC-010)" {
            Expect.equal (FS.GG.UI.Color.Contrast.verdict FS.GG.UI.Color.Text 4.5) FS.GG.UI.Color.Aa "4.5:1 passes AA for normal text"
            Expect.equal (FS.GG.UI.Color.Contrast.verdict FS.GG.UI.Color.GraphicOrUi 3.0) FS.GG.UI.Color.Aa "3:1 passes AA for large/graphic"
            Expect.equal (FS.GG.UI.Color.Contrast.verdict FS.GG.UI.Color.Text 2.0) FS.GG.UI.Color.Fail "2:1 fails AA for normal text"
        }
    ]
