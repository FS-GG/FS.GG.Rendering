namespace FS.GG.UI.Themes.Default.Theming

open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

/// Feature 108 (US6, FR-017): the theme mode the live-theming primitive resolves against. A closed
/// Controls-local DU (Light | Dark) — deliberately NOT `FS.GG.UI.Color.Palettes.RampVariant`, so
/// `Theming` adds no Controls→Color package dependency (the plan's "no new dependency" constraint).
/// The WCAG contrast reuse (`FS.GG.UI.Color.Contrast.ratio`) is exercised in the Controls.Tests
/// theming suite, which references the Color package directly (test-only, no package impact).
///
/// Placement note: the `RolePalette`/`Theming` surface lives in the CHILD namespace
/// `FS.GG.UI.Themes.Default.Theming` (not the bare `FS.GG.UI.Themes.Default`) so its role
/// field names (`Background`/`Foreground`/`Accent`/`Danger`/`Muted`) are not auto-in-scope during
/// theme/consumer compilation, where they would otherwise poison `Theme` record-field inference (the
/// documented "type-move clashes Theme fields" gotcha). Consumers `open FS.GG.UI.Themes.Default.Theming`.
type ThemeMode =
    | Light
    | Dark

/// Feature 108 (US6, FR-017): the role colours `toTheme` projects onto the framework `Theme` — the
/// live-theming primitive the ControlsShowcase3 author re-derived by hand. A small closed record.
type RolePalette =
    { /// The mode this palette was resolved against; `toTheme` seeds the framework `Theme` from the
      /// matching base (so `Success`/`Warning`/`Name` and the non-colour fields are mode-correct).
      Mode: ThemeMode
      Background: Color
      Foreground: Color
      Accent: Color
      Danger: Color
      Muted: Color
      FocusRing: Color }

/// Feature 108 (US6, FR-017/018): resolve a theme mode + accent into a role palette and project it
/// back onto the framework `Theme`. Pure, total; never throws.
module Theming =
    /// `mode + accent -> role palette`: the mode seeds the neutral/structural roles from the matching
    /// base `Theme` (Light/Dark), the accent overrides the `Accent` and `FocusRing` roles.
    val resolve: mode: ThemeMode -> accent: Color -> RolePalette

    /// Project a role palette onto the framework `Theme` — the "paint theme" passed to the render
    /// path (`Control.renderTree`) so the captured palette is EXACT, while the consumer keeps a
    /// static `host.Theme` for the fragment-reuse key (FR-018). The palette's `Mode` seeds the base
    /// `Theme` (Light/Dark), so `Success`/`Warning`/`Name` and the non-colour fields (font / density /
    /// radius / contrast ratio) are mode-correct; the neutral role colours are then overwritten.
    val toTheme: palette: RolePalette -> Theme
