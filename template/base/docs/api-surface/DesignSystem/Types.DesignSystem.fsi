// See skill: fs-gg-styling
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
    /// Feature 175 (FR-005): simultaneously hovered AND focused — the combined Ant state (hover fill +
    /// focus ring), so neither affordance suppresses the other.
    | FocusedHover
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

/// A typography delta a `StyleClass` can carry (`FontDelta`): an optional override of the
/// resolved `Size` and/or `Weight`. `None` leaves the folded-in value untouched, so a `Font`
/// class overlays only the typography fields it names.
/// #384: the class/state overlay was colour-only, so no attached class could restyle
/// `FontSize`/`FontWeight`; `FontDelta` carries those deltas into the resolver fold.
type FontDelta =
    { Size: float option
      Weight: int option }

/// One attached style class (`StyleClass`): a typed `Variant` wrapping a `StyleVariant`,
/// a free-form `Custom` consumer-defined class name, or a `Font` typography delta.
/// Feature 093 (E3): one attached-class entry — either a typed <c>StyleVariant</c> or a
/// free-form, consumer-defined class. A control carries a <c>StyleClass list</c> whose list
/// position IS the attach order the resolver folds left-to-right (FR-001, FR-003).
type StyleClass =
    | Variant of StyleVariant
    | Custom of string
    /// #384: a typography-only class — carries a <c>FontDelta</c> so a class can restyle
    /// <c>FontSize</c>/<c>FontWeight</c> the way <c>Variant</c>/<c>Custom</c> restyle colour.
    | Font of FontDelta

/// The `(theme, control-kind, lowered-intent, structural base) -> adjusted base` seam a `Theme`
/// carries. Total over every intent: `""` and unknown intents must return a defined style, never
/// raise. The kind is supplied because a kind's geometry constrains what an intent can mean — an
/// outlined `icon-button` cannot honour a filled `primary` without hiding its own label.
///
/// Equality and hashing are by `Name` alone, keeping `Theme` structurally equatable (the retained
/// renderer diffs themes) and deterministically hashable across processes.
///
/// Declared before `ResolvedStyle` and `Theme` so `Theme` remains the last-declared type of the
/// mutually recursive group — F# binds an ambiguous bare field name to the last declaration.
[<CustomEquality; NoComparison>]
type IntentPolicy =
    { Name: string
      ApplyIntent: Theme -> string -> string -> ResolvedStyle -> ResolvedStyle }

/// Resolved paint and typography for a control (`ResolvedStyle`): `Foreground`, `Fill`,
/// `Stroke`/`StrokeWidth`/`StrokeDash`, and `FontFamily`/`FontSize`/`FontWeight`, produced by
/// `Style.resolve`.
/// Feature 093 (E3) — the per-control output of style resolution: the concrete paint/typography
/// the migrated kinds apply. A FLAT record so the fixed precedence is last-writer-wins per field
/// and the parity proof is a plain structural record comparison. Geometry is NOT here — the
/// resolver governs paint/typography only; geometry stays computed as today (data-model R3).
/// Declared before `Theme` so the shared field names (`Foreground`/`FontFamily`/`FontSize`)
/// resolve to `Theme` for unannotated `theme.*` accesses; produced by `Style.resolve`.
and ResolvedStyle =
    { Foreground: Color
      Fill: Color
      Stroke: Color
      StrokeWidth: float
      /// The stroke's dash pattern as on/off intervals; `[]` is a solid stroke. Rendered through
      /// `PathEffect.Dash`, so a dashed border is real rather than approximated by a thicker one.
      StrokeDash: float list
      FontFamily: string option
      FontSize: float
      FontWeight: int option }

/// Design-token palette and metrics (`Theme`): the named color roles
/// (`Foreground`/`Background`/`Accent`/`Danger`/`Success`/`Warning`/`Muted`), typography
/// (`FontFamily`/`FontSize`), layout metrics (`Density`/`CornerRadius`), the dimension/spacing
/// model (`ControlHeight`/`ControlHeightSm`/`ControlHeightLg` + the `Space{Xs,Sm,Md,Lg}` scale — #385),
/// and the `IntentPolicy` that maps this theme's semantic intents onto structural style deltas.
and Theme =
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
      /// #385: standard interactive control height (Ant `controlHeight`). Geometry reads this
      /// instead of a frozen literal, so a theme restyles control sizing.
      ControlHeight: float
      /// #385: compact interactive control height (Ant `controlHeightSM`).
      ControlHeightSm: float
      /// #385: large interactive control height (Ant `controlHeightLG`).
      ControlHeightLg: float
      /// #385: extra-small spacing/gap step (Ant `Space.xs`).
      SpaceXs: float
      /// #385: small spacing/gap step (Ant `Space.sm`).
      SpaceSm: float
      /// #385: medium spacing/gap step (Ant `Space.md`).
      SpaceMd: float
      /// #385: large spacing/gap step (Ant `Space.lg`).
      SpaceLg: float
      /// How this theme perturbs a control's structural base by semantic intent. The render path
      /// resolves through it (`StyleResolver.resolve`), so a theme's intent language reaches the
      /// screen without any control edit. `IntentPolicy.neutral` ignores intent entirely.
      IntentPolicy: IntentPolicy }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
/// Built-in intent policies.
module IntentPolicy =
    /// The intent-agnostic policy: every intent returns the kind's structural base unchanged.
    val neutral: IntentPolicy
