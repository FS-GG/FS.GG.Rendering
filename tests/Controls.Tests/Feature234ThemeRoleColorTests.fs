module Feature234ThemeRoleColorTests

// Review P3 / #46 (feature 234) — the state→style resolver reads the theme's own `Success`/`Warning`
// role colours (added feature 125) instead of string-matching `theme.Name = "dark"` against Default
// DTCG token literals. The old fold silently ignored `Theme.Success`/`Theme.Warning` and mis-resolved
// every theme whose name was not literally "light"/"dark" (AntDesign's "AntDesign Dark", and
// `Theming.toTheme`'s output) to Default light tokens.
//
// Also covers the `Theming.toTheme` fix: the projected paint theme now carries mode-appropriate
// `Success`/`Warning`/`Name`, seeded from the palette's own `Mode` (was always `Theme.light`).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem
open FS.GG.UI.Themes.Default
open FS.GG.UI.Themes.AntDesign
open FS.GG.UI.Themes.Default.Theming

let private baseStyle (theme: Theme) : ResolvedStyle =
    { Foreground = theme.Foreground
      Fill = theme.Background
      Stroke = theme.Foreground
      StrokeWidth = 1.0
      FontFamily = theme.FontFamily
      FontSize = 14.0
      FontWeight = None }

// The Success variant paints Fill+Stroke with the resolver's success colour; Pending validation
// strokes with the warning colour. These are the observable seams onto `successColor`/`warningColor`.
let private successFill (theme: Theme) =
    (Style.resolve theme (baseStyle theme) [ Variant StyleVariant.Success ] Normal).Fill

let private warningStroke (theme: Theme) =
    (Style.resolve theme (baseStyle theme) [] (VisualState.Validation(Pending "checking"))).Stroke

[<Tests>]
let feature234ThemeRoleColorTests =
    testList "Review P3 theme role colours (#46)" [

        test "Success/Warning resolve from the theme's OWN role fields, not by Name string-match" {
            // A custom theme whose Success/Warning differ from every Default/Ant token, and whose Name
            // is neither "light" nor "dark" — the old string-match would have fallen through to Default
            // light tokens and dropped these entirely.
            let customSuccess = Colors.rgba 1uy 2uy 3uy 255uy
            let customWarning = Colors.rgba 4uy 5uy 6uy 255uy
            let custom =
                { Theme.dark with
                    Name = "brand-midnight"
                    Success = customSuccess
                    Warning = customWarning }
            Expect.equal (successFill custom) customSuccess "Success variant paints theme.Success"
            Expect.equal (warningStroke custom) customWarning "Pending validation strokes theme.Warning"
        }

        test "AntDesign Dark resolves its own Success/Warning fields" {
            // AntTheme.antDark.Name = "AntDesign Dark" — the old `theme.Name = "dark"` test was false, so
            // the resolver read DesignTokens.Light tokens rather than the theme. It happens the Ant seed
            // colours coincide with Default light tokens, so this was not a *visible* Ant regression; the
            // contract still requires resolving from the theme's own fields (the visible breakages are the
            // custom-theme and Dark `toTheme` cases below).
            Expect.equal (successFill AntTheme.antDark) AntTheme.antDark.Success "Ant dark Success comes from the Ant theme"
            Expect.equal (warningStroke AntTheme.antDark) AntTheme.antDark.Warning "Ant dark Warning comes from the Ant theme"
        }

        test "built-in Default themes still resolve their mode tokens (no regression)" {
            Expect.equal (successFill Theme.light) DesignTokens.Light.success "light Success unchanged"
            Expect.equal (successFill Theme.dark) DesignTokens.Dark.success "dark Success unchanged"
            Expect.equal (warningStroke Theme.dark) DesignTokens.Dark.warning "dark Warning unchanged"
        }

        test "Theming.toTheme carries mode-appropriate Success/Warning/Name (was always light)" {
            let accent = Colors.rgba 0uy 128uy 255uy 255uy
            let darkTheme = Theming.toTheme (Theming.resolve Dark accent)
            Expect.equal darkTheme.Success Theme.dark.Success "dark palette projects dark Success"
            Expect.equal darkTheme.Warning Theme.dark.Warning "dark palette projects dark Warning"
            Expect.equal darkTheme.Name "dark" "dark palette projects the dark Name (no longer pinned to light)"
            // End-to-end through the resolver: a Dark live-theme now paints the dark success colour.
            Expect.equal (successFill darkTheme) DesignTokens.Dark.success "Dark live-theme resolves dark success"

            let lightTheme = Theming.toTheme (Theming.resolve Light accent)
            Expect.equal lightTheme.Success Theme.light.Success "light palette projects light Success"
            Expect.equal lightTheme.Name "light" "light palette projects the light Name"
        }
    ]
