namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

// Feature 125: the design-system slice carved out of FS.GG.UI.Controls/Types. Declaration order
// is load-bearing — `ResolvedStyle` is declared immediately before `Theme` so the overlapping
// field names (`Foreground`/`FontFamily`/`FontSize`) resolve to `Theme` for the many unannotated
// `theme.*` accesses in the renderer (F# picks the last-declared type for an ambiguous bare field).

type ValidationState =
    | Valid
    | Invalid of string
    | Pending of string

type VisualState =
    | Normal
    | Disabled
    | Hover
    | Pressed
    | Focused
    | Selected
    | Loading
    | Validation of ValidationState

[<RequireQualifiedAccess>]
type StyleVariant =
    | Primary
    | Danger
    | Ghost
    | Neutral
    | Success
    | Warning

type StyleClass =
    | Variant of StyleVariant
    | Custom of string

type ResolvedStyle =
    { Foreground: Color
      Fill: Color
      Stroke: Color
      StrokeWidth: float
      FontFamily: string option
      FontSize: float
      FontWeight: int option }

type Theme =
    { Name: string
      Foreground: Color
      Background: Color
      Accent: Color
      Danger: Color
      // Feature 125 (FR-004): additive success/warning role colours, sourced from DesignTokens.
      Success: Color
      Warning: Color
      Muted: Color
      FontFamily: string option
      FontSize: float
      Density: float
      CornerRadius: float
      ContrastRequiredRatio: float }
