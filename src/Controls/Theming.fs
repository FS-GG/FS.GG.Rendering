namespace FS.GG.UI.Controls.Theming

open FS.GG.UI.Scene
open FS.GG.UI.Controls

// Feature 108 (US6, FR-017/018): the live-theming primitive (see Theming.fsi). Pure projections
// over the framework `Theme` — no I/O, no Color-package dependency. Child namespace so the role
// field names do not collide with `Theme`'s during `Control.fs` record-field inference.
type ThemeMode =
    | Light
    | Dark

type RolePalette =
    { Background: Color
      Foreground: Color
      Accent: Color
      Danger: Color
      Muted: Color
      FocusRing: Color }

module Theming =
    let resolve (mode: ThemeMode) (accent: Color) : RolePalette =
        let baseTheme =
            match mode with
            | Light -> Theme.light
            | Dark -> Theme.dark

        { Background = baseTheme.Background
          Foreground = baseTheme.Foreground
          Accent = accent
          Danger = baseTheme.Danger
          Muted = baseTheme.Muted
          // The focus ring tracks the accent so a re-accented theme keeps the ring visible against
          // the same backgrounds (the ControlsShowcase3 author's hand-rolled rule).
          FocusRing = accent }

    let toTheme (palette: RolePalette) : Theme =
        { Theme.light with
            Foreground = palette.Foreground
            Background = palette.Background
            Accent = palette.Accent
            Danger = palette.Danger
            Muted = palette.Muted }
