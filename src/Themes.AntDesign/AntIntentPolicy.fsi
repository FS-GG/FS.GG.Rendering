namespace FS.GG.UI.Themes.AntDesign

open FS.GG.UI.DesignSystem

/// Ant Design's intent language as an `IntentPolicy`: it maps a lowered button intent
/// (`primary` / `default` / `dashed` / `text` / `link` / `danger`) onto a structurally distinct
/// `ResolvedStyle` delta over the control kind's base. Total over every intent — `""` and unknown
/// intents return the base unchanged and never raise.
///
/// `AntTheme.antLight` and `antDark` carry these, so controls resolve through them with no control
/// edit. Brand colours come from the theme's roles; the neutral chrome comes from the generated Ant
/// `Map`/`Component` token layers, which is why the policy is bound per mode.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AntIntentPolicy =
    /// The policy `AntTheme.antLight` carries.
    val light: IntentPolicy

    /// The policy `AntTheme.antDark` carries.
    val dark: IntentPolicy
