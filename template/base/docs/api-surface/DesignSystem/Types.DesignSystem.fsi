namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

/// Validation status of an input control (`ValidationState`): `Valid`, `Invalid` with
/// an error message, or `Pending` with an in-progress message.
type ValidationState =
    | Valid
    | Invalid of string
    | Pending of string

/// Interaction/render state of a control (`VisualState`) consumed by the style resolver:
/// `Normal`, `Disabled`, `Hover`, `Pressed`, `Focused`, `Selected`, `Loading`, or a
/// `Validation`-wrapped `ValidationState`.
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
/// Built-in semantic style variant (`StyleVariant`): `Primary`, `Danger`, `Ghost`,
/// `Neutral`, `Success`, or `Warning`.
/// Feature 093 (E3): the typed, CLOSED set of built-in semantic style variants — the
/// compiler-checked common path for declarative styling. Closure guarantees the resolver's
/// variant layer is a total match (FR-001, FR-002, FR-004). Free-form classes live one level
/// up in <c>StyleClass.Custom</c>.
type StyleVariant =
    | Primary
    | Danger
    | Ghost
    | Neutral
    | Success
    | Warning

/// One attached style class (`StyleClass`): a typed `Variant` wrapping a `StyleVariant`,
/// or a free-form `Custom` consumer-defined class name.
/// Feature 093 (E3): one attached-class entry — either a typed <c>StyleVariant</c> or a
/// free-form, consumer-defined class. A control carries a <c>StyleClass list</c> whose list
/// position IS the attach order the resolver folds left-to-right (FR-001, FR-003).
type StyleClass =
    | Variant of StyleVariant
    | Custom of string

/// Resolved paint and typography for a control (`ResolvedStyle`): `Foreground`, `Fill`,
/// `Stroke`/`StrokeWidth`, and `FontFamily`/`FontSize`/`FontWeight`, produced by `Style.resolve`.
/// Feature 093 (E3) — the per-control output of style resolution: the concrete paint/typography
/// the migrated kinds apply. A FLAT record so the fixed precedence is last-writer-wins per field
/// and the parity proof is a plain structural record comparison. Geometry is NOT here — the
/// resolver governs paint/typography only; geometry stays computed as today (data-model R3).
/// Declared before `Theme` so the shared field names (`Foreground`/`FontFamily`/`FontSize`)
/// resolve to `Theme` for unannotated `theme.*` accesses; produced by `Style.resolve`.
type ResolvedStyle =
    { Foreground: Color
      Fill: Color
      Stroke: Color
      StrokeWidth: float
      FontFamily: string option
      FontSize: float
      FontWeight: int option }

/// Design-token palette and metrics (`Theme`): the named color roles
/// (`Foreground`/`Background`/`Accent`/`Danger`/`Success`/`Warning`/`Muted`), typography
/// (`FontFamily`/`FontSize`), and layout metrics (`Density`/`CornerRadius`/`ContrastRequiredRatio`).
type Theme =
    { Name: string
      Foreground: Color
      Background: Color
      Accent: Color
      Danger: Color
      /// Feature 125 (FR-004): success role colour, sourced from `DesignTokens.{Light,Dark}.success`.
      /// Additive — no D1 render path reads it yet, so output is identical.
      Success: Color
      /// Feature 125 (FR-004): warning role colour, sourced from `DesignTokens.{Light,Dark}.warning`.
      /// Additive — no D1 render path reads it yet, so output is identical.
      Warning: Color
      Muted: Color
      FontFamily: string option
      FontSize: float
      Density: float
      CornerRadius: float
      ContrastRequiredRatio: float }
