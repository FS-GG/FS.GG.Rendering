namespace FS.GG.UI.Controls.Theming

open FS.GG.UI.Scene
open FS.GG.UI.Controls

/// Feature 108 (US6, FR-017): the theme mode the live-theming primitive resolves against. A closed
/// Controls-local DU (Light | Dark) — deliberately NOT `FS.GG.UI.Color.Palettes.RampVariant`, so
/// `Theming` adds no Controls→Color package dependency (the plan's "no new dependency" constraint).
/// The WCAG contrast reuse (`FS.GG.UI.Color.Contrast.ratio`) is exercised in the Controls.Tests
/// theming suite, which references the Color package directly (test-only, no package impact).
///
/// Placement note: the `RolePalette`/`Theming` surface lives in the CHILD namespace
/// `FS.GG.UI.Controls.Theming` (not the contract sketch's bare `FS.GG.UI.Controls`) so its role
/// field names (`Background`/`Foreground`/`Accent`/`Danger`/`Muted`) are not auto-in-scope during
/// `Control.fs` compilation, where they would otherwise poison `Theme` record-field inference (the
/// documented "type-move clashes Theme fields" gotcha). Consumers `open FS.GG.UI.Controls.Theming`.
type ThemeMode =
    | Light
    | Dark

/// Feature 108 (US6, FR-017): the role colours `toTheme` projects onto the framework `Theme` — the
/// live-theming primitive the ControlsShowcase3 author re-derived by hand. A small closed record.
type RolePalette =
    { Background: Color
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
    /// static `host.Theme` for the fragment-reuse key (FR-018). Non-colour fields (font / density /
    /// radius / contrast ratio) carry from `Theme.light`.
    val toTheme: palette: RolePalette -> Theme
