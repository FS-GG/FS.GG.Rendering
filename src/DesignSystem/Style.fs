namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

// Feature 093 (E3): the single pure state→style resolver. A closed, ordered, last-writer-wins
// fold over (theme/token base < attached classes (earlier < later) < current VisualState). No
// selector matching, no specificity, no cross-control cascade (permanent roadmap non-goals).
// Every colour read originates from the active `Theme` / generated `DesignTokens` set (FR-008).
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Style =

    // The variant-only `success`/`warning` colours are NOT `Theme` fields; select the active
    // DTCG token set by the theme's variant name (a custom theme keeps its `light`/`dark` name).
    // FR-008: sourced from generated `DesignTokens`, never an inline literal.
    let isDark (theme: Theme) = theme.Name = "dark"

    let successColor (theme: Theme) =
        if isDark theme then DesignTokens.Dark.success else DesignTokens.Light.success

    let warningColor (theme: Theme) =
        if isDark theme then DesignTokens.Dark.warning else DesignTokens.Light.warning

    // ---- class layer ----------------------------------------------------------------------
    // Each StyleClass is a partial overwrite of the ResolvedStyle fields it owns. The closed
    // `StyleVariant` set is an exhaustive match (totality, FR-002/FR-004).
    let applyVariant (theme: Theme) (variant: StyleVariant) (s: ResolvedStyle) : ResolvedStyle =
        match variant with
        | StyleVariant.Primary -> { s with Fill = theme.Accent; Stroke = theme.Accent; Foreground = theme.Background }
        | StyleVariant.Danger -> { s with Fill = theme.Danger; Stroke = theme.Danger; Foreground = theme.Background }
        | StyleVariant.Success ->
            let c = successColor theme
            { s with Fill = c; Stroke = c; Foreground = theme.Background }
        | StyleVariant.Warning ->
            let c = warningColor theme
            { s with Fill = c; Stroke = c; Foreground = theme.Background }
        | StyleVariant.Ghost ->
            // Low-emphasis / transparent fill; intent shows in the stroke + text colour.
            { s with Fill = Colors.transparent; Stroke = theme.Foreground; Foreground = theme.Foreground }
        | StyleVariant.Neutral -> s // explicit "no intent" — identity delta over the base.

    // `Custom name` resolves through the SAME fold (FR-001): a known name maps to a delta; an
    // unknown name resolves to identity — never an exception or a silent drop (data-model
    // edge case; contrast still governed by `ContrastCheck`, FR-007).
    let applyCustom (theme: Theme) (name: string) (s: ResolvedStyle) : ResolvedStyle =
        match name.Trim().ToLowerInvariant() with
        | "primary" -> applyVariant theme StyleVariant.Primary s
        | "danger" -> applyVariant theme StyleVariant.Danger s
        | "success" -> applyVariant theme StyleVariant.Success s
        | "warning" -> applyVariant theme StyleVariant.Warning s
        | "ghost" -> applyVariant theme StyleVariant.Ghost s
        | "neutral" -> applyVariant theme StyleVariant.Neutral s
        | "muted"
        | "subtle" -> { s with Fill = theme.Muted; Foreground = theme.Background }
        | _ -> s // unknown ⇒ identity delta

    let applyClass (theme: Theme) (cls: StyleClass) (s: ResolvedStyle) : ResolvedStyle =
        match cls with
        | Variant v -> applyVariant theme v s
        | Custom name -> applyCustom theme name s

    // ---- state layer ----------------------------------------------------------------------
    // Applied AFTER the class fold so a state's owned field overrides any class value (FR-003).
    // Colour-only, all token-derived. `Normal`/`Loading` are identity — the procedural baseline
    // paints `Loading` like `Normal`, so the resolver preserves that identity (FR-004 parity).
    let applyValidation (theme: Theme) (v: ValidationState) (s: ResolvedStyle) : ResolvedStyle =
        match v with
        | Valid -> { s with Stroke = successColor theme }
        | Invalid _ -> { s with Stroke = theme.Danger; Foreground = theme.Danger }
        | Pending _ -> { s with Stroke = warningColor theme }

    let applyState (theme: Theme) (state: VisualState) (s: ResolvedStyle) : ResolvedStyle =
        match state with
        | Normal -> s
        | Loading -> s
        | Hover -> { s with Fill = theme.Accent }
        | Pressed -> { s with Fill = theme.Muted }
        | Focused -> { s with Stroke = theme.Accent }
        | Selected -> { s with Fill = theme.Accent; Foreground = theme.Background }
        | Disabled -> { s with Fill = theme.Muted; Stroke = theme.Muted; Foreground = theme.Muted }
        // Qualified: `Validation` also names `AttrCategory.Validation`; this scrutinee is a `VisualState`.
        | VisualState.Validation v -> applyValidation theme v s

    let resolve (theme: Theme) (baseStyle: ResolvedStyle) (classes: StyleClass list) (state: VisualState) : ResolvedStyle =
        classes
        |> List.fold (fun acc cls -> applyClass theme cls acc) baseStyle
        |> applyState theme state
