namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

/// Front half of style resolution: theme + control-kind + semantic-intent + visual-state(s) -> ResolvedStyle.
/// Supplies the kind's structural base, lets the theme's own `IntentPolicy` perturb it by intent, then
/// composes the 093 back-half Style.resolve for the class+state overlay. Total + deterministic.
module StyleResolver =

    /// The kind's structural base style. Total over kind: "icon-button" -> accent outline; any other kind ->
    /// filled accent (a defined, visible fallback — never empty, transparent-only, or an exception).
    val baseStyleFor: theme: Theme -> kind: string -> ResolvedStyle

    /// The single front-half resolution path the control render code calls. The theme's `IntentPolicy`
    /// decides what the intent means (`IntentPolicy.neutral` ignores it); the intent is lowered before the
    /// policy sees it. Total + deterministic.
    val resolve:
        theme: Theme ->
        kind: string ->
        intent: string ->
        classes: StyleClass list ->
        state: VisualState ->
            ResolvedStyle
