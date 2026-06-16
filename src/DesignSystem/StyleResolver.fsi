namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

/// Front half of style resolution: theme + control-kind + semantic-intent + visual-state(s) -> ResolvedStyle.
/// Supplies the kind's structural base (under an overridable intent policy), then composes the 093 back-half
/// Style.resolve for the class+state overlay. Total + deterministic; the default path is intent-neutral.
module StyleResolver =

    /// The overridable (theme, lowered-intent-string, structural base) -> adjusted base seam.
    /// `neutralPolicy` keeps it identity; a divergent policy (e.g. "danger" -> theme.Danger) drives intent
    /// divergence with no control edits.
    type IntentPolicy =
        { ApplyIntent: Theme -> string -> ResolvedStyle -> ResolvedStyle }

    /// The kind's structural base style. Total over kind: "icon-button" -> accent outline; any other kind ->
    /// filled accent (a defined, visible fallback — never empty, transparent-only, or an exception).
    val baseStyleFor: theme: Theme -> kind: string -> ResolvedStyle

    /// The default, intent-agnostic policy: returns the structural base unchanged.
    val neutralPolicy: IntentPolicy

    /// The single front-half resolution path. Total + deterministic.
    val resolve:
        policy: IntentPolicy ->
        theme: Theme ->
        kind: string ->
        intent: string ->
        classes: StyleClass list ->
        state: VisualState ->
            ResolvedStyle

    /// The neutral path control render code calls — intent threaded but ignored, byte-identical to the
    /// pre-promotion internal call.
    val resolveDefault:
        theme: Theme ->
        kind: string ->
        intent: string ->
        classes: StyleClass list ->
        state: VisualState ->
            ResolvedStyle
