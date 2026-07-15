namespace FS.GG.UI.Color

// Issue #174 — the pairing catalogs the color policies validate.
//
// These lived in `tests/Controls.Tests/Feature127ColorPolicyTests.fs`, so the policy linted a
// hand-authored list of token pairings and never saw a `ResolvedStyle` the resolver actually
// produced. A consumer could not violate a policy the policy never looked at. Both catalogs now
// live here, beside the engine, and `emittedPairings` derives its rows from `StyleResolver.resolve`
// rather than restating token values — the gate measures the styles that reach the screen.
//
// `module internal` with NO companion .fsi, matching ColorPolicy: additive, zero public-surface
// delta, reached by Controls.Tests via the existing InternalsVisibleTo grant. Per the repo's
// Principle II, visibility lives in .fsi; this file has none, so no top-level access modifiers
// beyond `private` appear on bindings.

open System.Collections.Generic
open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

module internal StyleCatalog =

    // `open FS.GG.UI.Scene` brings SceneNode.Text into scope, which shadows Role.Text; the Role
    // cases are therefore qualified throughout this file (same hazard ColorPolicy.fs documents).
    let private pairing name fg bg role : ColorPolicy.Pairing =
        { Name = name
          Foreground = fg
          Background = bg
          Role = role }

    // ---- catalog 1: design-system token pairings -------------------------------------------

    /// Real design-system token values. Both policies evaluate this same list; the order is the
    /// report row order. The Ant semantic families (primary/success/warning/error/info/
    /// text-on-surface) are all present (SC-003); `primary-hover-fg-on-surface` is the
    /// policy-divergent pairing (ratio ≈ 2.99: WCAG Fail, ant Pass — drives FR-005 divergence and
    /// FR-010 no-overclaim); `decorative-hairline-on-surface` is the out-of-scope exemplar for
    /// `ant` (FR-011).
    ///
    /// This catalog asserts what the TOKENS are, not what the resolver emits from them — see
    /// `emittedPairings` for the latter. Both are needed: a token can be sound and still be
    /// composed into an unreadable style.
    let designSystemTokens: ColorPolicy.Pairing list =
        [ pairing "text-on-canvas" DesignTokensExt.Alias.Light.textDefault DesignTokensExt.Alias.Light.surfaceCanvas Role.Text
          pairing "text-on-surface" DesignTokensExt.Alias.Light.textDefault DesignTokensExt.Map.Light.colorBgContainer Role.Text
          pairing
              "muted-text-on-surface"
              DesignTokensExt.Alias.Light.textSecondary
              DesignTokensExt.Map.Light.colorBgContainer
              Role.Text
          pairing
              "primary-fg-on-surface"
              DesignTokensExt.Seed.colorPrimary
              DesignTokensExt.Map.Light.colorBgContainer
              Role.GraphicOrUi
          pairing
              "success-fg-on-surface"
              DesignTokensExt.Seed.colorSuccess
              DesignTokensExt.Map.Light.colorBgContainer
              Role.GraphicOrUi
          pairing
              "warning-fg-on-surface"
              DesignTokensExt.Seed.colorWarning
              DesignTokensExt.Map.Light.colorBgContainer
              Role.GraphicOrUi
          pairing
              "error-fg-on-surface"
              DesignTokensExt.Seed.colorError
              DesignTokensExt.Map.Light.colorBgContainer
              Role.GraphicOrUi
          pairing
              "info-fg-on-surface"
              DesignTokensExt.Seed.colorInfo
              DesignTokensExt.Map.Light.colorBgContainer
              Role.GraphicOrUi
          pairing
              "primary-hover-fg-on-surface"
              DesignTokensExt.Map.Light.colorPrimaryHover
              DesignTokensExt.Map.Light.colorBgContainer
              Role.GraphicOrUi
          pairing
              "decorative-hairline-on-surface"
              DesignTokensExt.Map.Light.colorBorder
              DesignTokensExt.Map.Light.colorBgContainer
              Role.Decorative ]

    // ---- catalog 2: the styles the resolver actually emits -----------------------------------

    /// The kinds `StyleResolver.baseStyleFor` branches on. Any other kind resolves to the same
    /// filled base as `"button"`, so enumerating a third kind would only duplicate rows.
    let kinds = [ "button"; "icon-button" ]

    /// The closed `StyleVariant` set, plus the no-class baseline. Enumerating the closed set means
    /// a new variant cannot be added without a row appearing here.
    let variants =
        [ "neutral", StyleVariant.Neutral
          "primary", StyleVariant.Primary
          "danger", StyleVariant.Danger
          "success", StyleVariant.Success
          "warning", StyleVariant.Warning
          "ghost", StyleVariant.Ghost ]

    /// The semantic-intent vocabulary a theme's `IntentPolicy` recognises — the Ant control types
    /// (Features 132/173). `""` is the identity/no-intent base (already covered by the `neutral`
    /// variant), so only the non-empty entries are scenarios; they are enumerated here so the
    /// neutral chrome the policy paints reaches the catalog rather than being masked behind a pinned
    /// `intent = ""`. Hand-maintained like `variants`: `IntentPolicy` carries its vocabulary as a
    /// function, not introspectable data, so a new Ant intent must be listed here to be measured
    /// (`AntIntentPolicy.fs`). A theme whose policy is `IntentPolicy.neutral` (the Default themes)
    /// ignores every intent, so these scenarios dedup back onto its structural base — no effect.
    let intents = [ "primary"; "default"; "dashed"; "text"; "link"; "danger" ]

    /// The style requests the resolver is asked for. A control reaches the screen one of two ways —
    /// as a `StyleVariant`-classed control under the identity intent, or as an intent-typed control
    /// over the bare structural base (no competing class, so the intent's own chrome survives) — and
    /// both families are measured. The middle segment of a row label is the scenario name; intent
    /// scenarios are prefixed `@` so a row's provenance stays legible and the two families never
    /// collide on a name.
    let scenarios: (string * StyleClass list * string) list =
        [ for name, variant in variants -> name, [ Variant variant ], ""
          for intent in intents -> "@" + intent, [], intent ]

    /// Every `VisualState` case. The `Invalid`/`Pending` payloads are messages, not colours, so an
    /// empty string is representative.
    let states =
        [ "normal", Normal
          "hover", Hover
          "pressed", Pressed
          "focused", Focused
          "focused-hover", FocusedHover
          "selected", Selected
          "loading", Loading
          "disabled", Disabled
          "valid", VisualState.Validation Valid
          "invalid", VisualState.Validation(Invalid "")
          "pending", VisualState.Validation(Pending "") ]

    /// The contrast pairings one `ResolvedStyle` puts on screen, measured against `canvas` (the
    /// surface the control is painted onto):
    ///
    ///   * `#text` — the label over the control's own effective surface (the fill composited over
    ///     the canvas, so a translucent or fully-transparent fill measures against what shows
    ///     through, never against a colour nobody sees).
    ///   * `#border` / `#surface` — the control's visible boundary against the canvas, per WCAG
    ///     1.4.11: a stroked control is identified by its border, an unstroked one by its fill.
    ///     Measuring a stroke against its own fill instead would flag every same-colour outline —
    ///     an outline the colour of its fill is just a fatter fill, not a defect.
    ///
    /// A control with neither stroke nor fill (a text/ghost button) has no boundary to measure, so
    /// it emits no boundary pairing at all rather than an unmeasurable one.
    ///
    /// `inactive` marks a disabled control: WCAG 1.4.3 and 1.4.11 both exempt inactive components,
    /// so its pairings are `Decorative`. Without this the resolver's `Disabled` delta (which paints
    /// foreground, fill and stroke all `theme.Muted`) would report a 1.00 ratio as a failure.
    let pairingsOfStyle (canvas: Color) (label: string) (inactive: bool) (style: ResolvedStyle) : ColorPolicy.Pairing list =
        let surface = Contrast.compositeOver canvas style.Fill
        let textRole = if inactive then Role.Decorative else Role.Text
        let edgeRole = if inactive then Role.Decorative else Role.GraphicOrUi

        let boundary =
            if style.StrokeWidth > 0.0 && style.Stroke.Alpha > 0uy then
                [ pairing (label + "#border") style.Stroke canvas edgeRole ]
            elif style.Fill.Alpha > 0uy then
                [ pairing (label + "#surface") style.Fill canvas edgeRole ]
            else
                []

        pairing (label + "#text") style.Foreground surface textRole :: boundary

    /// Every distinct contrast pairing `StyleResolver.resolve` can emit for `theme` over the closed
    /// (kind × scenario × visual-state) domain — the styles the renderer paints, derived by calling
    /// the resolver, never by restating its literals.
    ///
    /// Each scenario is either a `StyleVariant` class (under the identity intent) or a theme
    /// `IntentPolicy` intent over the bare base, so the neutral chrome the policy paints
    /// (`default`/`dashed`/`text`/`link`, the point of Features 132/173) is measured, not masked
    /// behind a pinned `intent = ""`.
    ///
    /// Rows are deduplicated by `(Foreground, Background, Role)` — the same three colours contrast
    /// identically however they were reached, so the 264 (kind × scenario × state) combinations
    /// collapse to a few dozen distinct pairings (fewest for the intent-agnostic Default themes,
    /// most — ~40 — for the Ant themes, whose `IntentPolicy` chrome is genuinely distinct). The
    /// surviving `Name` is the first combination (in the `kinds × scenarios × states` declaration
    /// order above) that emits it, so the row is a stable, reproducible witness.
    let emittedPairings (theme: Theme) : ColorPolicy.Pairing list =
        let all =
            [ for kind in kinds do
                  for scenarioName, classes, intent in scenarios do
                      for stateName, state in states do
                          let style = StyleResolver.resolve theme kind intent classes state
                          let label = sprintf "%s/%s/%s" kind scenarioName stateName
                          yield! pairingsOfStyle theme.Background label (state = Disabled) style ]

        let seen = HashSet<Color * Color * Role>()
        all |> List.filter (fun p -> seen.Add((p.Foreground, p.Background, p.Role)))
