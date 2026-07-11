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
    // Feature 175 (FR-005): a control that is simultaneously hovered AND focused — the combined Ant
    // state (hover fill + focus ring), so neither affordance suppresses the other.
    | FocusedHover
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

// #384: a typography delta a `StyleClass` can carry. The class layer was colour-only
// (`Style.applyVariant`/`applyCustom` only ever rewrote Fill/Stroke/Foreground), so no attached
// class could restyle `FontSize`/`FontWeight`. `FontDelta` names those two overridable fields as
// options: `Some` overrides the folded-in value, `None` leaves it — the same last-writer-wins,
// only-the-fields-it-owns overlay the colour classes already use.
type FontDelta =
    { Size: float option
      Weight: int option }

type StyleClass =
    | Variant of StyleVariant
    | Custom of string
    // #384: restyle typography without touching colour. Carries an explicit `FontDelta` into the
    // resolver fold so a control (or a theme's `IntentPolicy`, which composes over the same
    // `ResolvedStyle`) can change label size/weight the way `Variant`/`Custom` change colour.
    | Font of FontDelta

// Feature 173: a `Theme` carries the `IntentPolicy` that maps its semantic intents onto structural
// style deltas, so the policy reaches the render path with the theme — no control names a theme, and
// nothing has to thread a policy alongside one. The three types are mutually recursive because a
// policy reads the theme's roles and returns a `ResolvedStyle`.
//
// `IntentPolicy` is declared FIRST so `Theme` stays the last-declared type of the group: F# binds an
// ambiguous bare field name to the last declaration, and the renderer's many unannotated `theme.*`
// accesses depend on `Foreground`/`FontFamily`/`FontSize`/`Name` binding to `Theme`, not to
// `ResolvedStyle` or `IntentPolicy`.
//
// Equality is by `Name` alone. `Theme` must stay equatable — the retained renderer's
// `prev.Theme <> theme` drives ThemeChanged invalidation — and a bare function field would strip that
// equality at compile time and hash by closure identity at run time. Naming the policy keeps theme
// equality structural and its hash stable across processes.
[<CustomEquality; NoComparison>]
type IntentPolicy =
    { Name: string
      ApplyIntent: Theme -> string -> string -> ResolvedStyle -> ResolvedStyle }

    override this.Equals(other) =
        match other with
        | :? IntentPolicy as that -> this.Name = that.Name
        | _ -> false

    override this.GetHashCode() = hash this.Name

and ResolvedStyle =
    { Foreground: Color
      Fill: Color
      Stroke: Color
      StrokeWidth: float
      // Empty ⇒ a solid stroke. A non-empty on/off interval list is a real dash pattern, handed to
      // `PathEffect.Dash` by the geometry — so `dashed` is rendered, not faked with a thicker border.
      StrokeDash: float list
      FontFamily: string option
      FontSize: float
      FontWeight: int option }

and Theme =
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
      // #385: token-sourced dimension/spacing metrics (Ant control-size + Space scale). Geometry
      // reads these instead of frozen literals, so a theme restyles control sizing the way palette
      // roles restyle colour. Sourced from DesignTokens.{Light,Dark}.
      ControlHeight: float
      ControlHeightSm: float
      ControlHeightLg: float
      SpaceXs: float
      SpaceSm: float
      SpaceMd: float
      SpaceLg: float
      IntentPolicy: IntentPolicy }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module IntentPolicy =
    /// The intent-agnostic policy: every intent (including `""` and unknown) returns the kind's
    /// structural base unchanged. The Default theme's policy, so its output is unchanged.
    let neutral: IntentPolicy =
        { Name = "neutral"
          ApplyIntent = fun _ _ _ style -> style }
