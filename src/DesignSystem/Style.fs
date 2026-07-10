namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

// Feature 093 (E3): the single pure state→style resolver. A closed, ordered, last-writer-wins
// fold over (theme/token base < attached classes (earlier < later) < current VisualState). No
// selector matching, no specificity, no cross-control cascade (permanent roadmap non-goals).
// Every colour read originates from the active `Theme` / generated `DesignTokens` set (FR-008).
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Style =

    // Feature 125 (FR-004) made `Success`/`Warning` first-class `Theme` role colours; read them
    // directly (FR-008: every colour originates from the active `Theme`). The previous fold selected
    // a DTCG token set by string-matching `theme.Name = "dark"`, which silently ignored the theme's
    // own `Success`/`Warning` and mis-resolved every theme whose name was not literally "light"/"dark"
    // (e.g. AntDesign's "AntDesign Dark", and `Theming.toTheme`'s output) to Default light tokens.
    let successColor (theme: Theme) = theme.Success

    let warningColor (theme: Theme) = theme.Warning

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
    // edge case).
    //
    // The resolver does NOT check contrast. It is pure colour algebra over the theme and can emit a
    // `ResolvedStyle` nobody can read (a danger button whose hover fill is the accent; an outline
    // button whose hovered label matches its own fill). Contrast is measured one level out, over the
    // style set this resolver emits, by `FS.GG.UI.Color.StyleCatalog.emittedPairings` — see the
    // committed `docs/reports/color-policy-emitted-*.md` gate. An earlier comment here claimed a
    // `ContrastCheck` (FR-007) governed this path; no such call ever existed (issue #174).
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
    // Applied AFTER the class fold, so a state's owned field still wins over any class value
    // (FR-003) — but `Fill` is now a FUNCTION of the value the class layer left there rather than a
    // replacement for it (issue #181, below). Colour-only; every colour still originates from the
    // active theme (FR-008), the emphasis states deriving a shade of a theme colour rather than
    // naming one of their own. `Normal`/`Loading` are identity — the procedural baseline paints
    // `Loading` like `Normal`, so the resolver preserves that identity (FR-004 parity).

    // Issue #181: the state layer used to *replace* the fill (`Hover -> theme.Accent`), so a `danger`
    // button turned primary-blue under the pointer — a destructive action wearing the primary-action
    // colour at the exact moment it is about to be clicked. `success`/`warning` lost their intent the
    // same way. The rule "state wins over class" was implemented correctly; the rule was wrong.
    //
    // The state layer now MODULATES the fill it is handed instead of naming a colour of its own. That
    // is origin-agnostic: the fill may come from the kind's structural base, from a variant/custom
    // class, or from the theme's own `IntentPolicy` (Feature 173) — the modulation preserves whichever
    // intent put it there, so no layer has to publish "who owns Fill".
    //
    // Modulation scales every channel toward a common endpoint (black or white), so the HUE is
    // preserved: a uniform scale in RGB moves HSV's value and saturation, never rotates hue.
    let private scaleTowards (endpoint: byte) (amount: float) (c: byte) : byte =
        let e = float endpoint
        let v = float c
        byte (System.Math.Round(v + (e - v) * amount))

    /// Modulate a colour toward `endpoint` by `amount` (0.0 = identity, 1.0 = the endpoint), leaving
    /// alpha untouched. A fully transparent colour is returned unchanged: a `ghost` variant's fill is
    /// deliberately `Colors.transparent`, and nudging the RGB of something nobody can see would only
    /// invent an invisible colour — and, worse, give the contrast gate a boundary pairing to measure
    /// where the control has no visible boundary at all.
    let private modulate (endpoint: byte) (amount: float) (c: Color) : Color =
        if c.Alpha = 0uy then
            c
        else
            { c with
                Red = scaleTowards endpoint amount c.Red
                Green = scaleTowards endpoint amount c.Green
                Blue = scaleTowards endpoint amount c.Blue }

    /// Which way is "away from the label". Ant shades a light theme's hover LIGHTER
    /// (`colorPrimaryHover`), but a fixed direction is only ever right for one polarity of theme: on a
    /// dark theme the label is dark, and lifting the fill toward it is what makes a `danger` button's
    /// text bleed into its background. Emphasis therefore moves the fill AWAY from the foreground it
    /// carries, which lightens on a dark theme and darkens on a light one — hue preserved either way,
    /// and contrast rising monotonically with emphasis rather than falling.
    ///
    /// The weights are the sRGB luma coefficients. Picking a direction only needs to know which colour
    /// is the lighter of two; it does not need WCAG's linearized relative luminance (that lives in
    /// `FS.GG.UI.Color.Contrast`, one level out, which measures what this module emits).
    let private luma (c: Color) =
        0.2126 * float c.Red + 0.7152 * float c.Green + 0.0722 * float c.Blue

    /// A step of `amount` away from `s.Foreground`, applied to `s.Fill`.
    let private emphasize (amount: float) (s: ResolvedStyle) : Color =
        let endpoint = if luma s.Foreground > luma s.Fill then 0uy else 255uy
        modulate endpoint amount s.Fill

    // The three emphasis steps, in increasing commitment: a transient touch, a press, and a standing
    // selection. Each is a step further from the label, so each is also a step more readable.
    let private hoverAmount = 0.10
    let private pressedAmount = 0.20
    let private selectedAmount = 0.28

    // Validation tints the BORDER, never the label. `Invalid` used to also paint `Foreground = theme.Danger`
    // (issue #359): a danger-red label over whatever fill the class layer left — invisible red-on-red for a
    // `danger` control (ratio 1.00), red-on-fill for every other filled intent (≤1.6). The stroke is the
    // legible invalid affordance (a red border is the conventional form-field signal, and it is measured as
    // a 3:1 GraphicOrUi element, not 4.5 Text); the label stays whatever readable foreground the fill layer
    // already chose. `Invalid` is now symmetric with `Valid`/`Pending`, which were always stroke-only.
    //
    // The signal rides the stroke, so a variant that draws no border (`ghost`/`text`/`link`, whose base
    // `StrokeWidth` is 0) shows no visible invalid affordance — validation is a form-field concern and those
    // are borderless BUTTONS, and the alternative (a red label) fails WCAG on the dark canvas anyway (the
    // 3.25 `ghost/invalid` row this fix clears). Forcing a stroke width per state is a geometry change left
    // to the kind bases, not this colour-only resolver.
    let applyValidation (theme: Theme) (v: ValidationState) (s: ResolvedStyle) : ResolvedStyle =
        match v with
        | Valid -> { s with Stroke = successColor theme }
        | Invalid _ -> { s with Stroke = theme.Danger }
        | Pending _ -> { s with Stroke = warningColor theme }

    let applyState (theme: Theme) (state: VisualState) (s: ResolvedStyle) : ResolvedStyle =
        match state with
        | Normal -> s
        | Loading -> s
        | Hover -> { s with Fill = emphasize hoverAmount s }
        | Pressed -> { s with Fill = emphasize pressedAmount s }
        // The focus ring is an accessibility affordance, not an intent: it names the accent outright so
        // a focused control is identifiable by the same ring wherever it appears.
        | Focused -> { s with Stroke = theme.Accent }
        // Feature 175 (FR-005): combined hover+focus applies BOTH orthogonal deltas (hover fill +
        // focus stroke) so neither affordance suppresses the other.
        | FocusedHover ->
            { s with
                Fill = emphasize hoverAmount s
                Stroke = theme.Accent }
        // `Selected` used to repaint the label `theme.Background` as well. That was a no-op for every
        // FILLED style (whose foreground is already the background colour) and actively harmful for the
        // unfilled ones: a selected `ghost` button painted a background-coloured label straight onto the
        // background — invisible, ratio 1.00. The fill each layer chose already carries a readable
        // foreground; emphasis has no business relighting it.
        | Selected -> { s with Fill = emphasize selectedAmount s }
        // `Disabled` is the one state that deliberately DOES discard the intent: an inactive control
        // must not advertise a destructive (or any) action it will not perform. WCAG 1.4.3/1.4.11 both
        // exempt inactive components, which is why the contrast gate rates these pairings Decorative.
        | Disabled -> { s with Fill = theme.Muted; Stroke = theme.Muted; Foreground = theme.Muted }
        // Qualified: `Validation` also names `AttrCategory.Validation`; this scrutinee is a `VisualState`.
        | VisualState.Validation v -> applyValidation theme v s

    let resolve (theme: Theme) (baseStyle: ResolvedStyle) (classes: StyleClass list) (state: VisualState) : ResolvedStyle =
        classes
        |> List.fold (fun acc cls -> applyClass theme cls acc) baseStyle
        |> applyState theme state
