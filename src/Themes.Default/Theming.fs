namespace FS.GG.UI.Themes.Default.Theming

open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem
open FS.GG.UI.Themes.Default

// Feature 108 (US6, FR-017/018): the live-theming primitive (see Theming.fsi). Pure projections
// over the framework `Theme` — no I/O, no Color-package dependency. Child namespace so the role
// field names do not collide with `Theme`'s during `Control.fs` record-field inference.
type ThemeMode =
    | Light
    | Dark

type RolePalette =
    { Mode: ThemeMode
      Background: Color
      Foreground: Color
      Accent: Color
      Danger: Color
      Muted: Color
      FocusRing: Color }

module Theming =
    let private baseThemeFor =
        function
        | Light -> Theme.light
        | Dark -> Theme.dark

    let resolve (mode: ThemeMode) (accent: Color) : RolePalette =
        let baseTheme = baseThemeFor mode

        { Mode = mode
          Background = baseTheme.Background
          Foreground = baseTheme.Foreground
          Accent = accent
          Danger = baseTheme.Danger
          Muted = baseTheme.Muted
          // The focus ring tracks the accent so a re-accented theme keeps the ring visible against
          // the same backgrounds (the ControlsShowcase3 author's hand-rolled rule).
          FocusRing = accent }

    let toTheme (palette: RolePalette) : Theme =
        // Seed from the palette's OWN mode base (Review P3 / #46): the earlier code always seeded from
        // `Theme.light`, so a Dark palette projected a Theme carrying light `Success`/`Warning` (and a
        // "light" `Name`). The neutral role colours below are overwritten from the palette; `Mode` now
        // carries the correct `Success`/`Warning`/`Name` and the mode-appropriate non-colour fields.
        { baseThemeFor palette.Mode with
            Foreground = palette.Foreground
            Background = palette.Background
            Accent = palette.Accent
            Danger = palette.Danger
            Muted = palette.Muted }
