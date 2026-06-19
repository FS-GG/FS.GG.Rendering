/// Theme resolution for the showcase: consume the shipped `FS.GG.UI.Themes.AntDesign`
/// theme directly (R3). `resolve` maps the sample's `ThemeMode` to the concrete Ant
/// variants — Light→antLight, Dark→antDark — with NO accent seam (unlike G1). The
/// showcase renders the shipped variants verbatim; it never tweaks tokens (FR-016).
///
/// The package's own module is also named `AntTheme`, so its members are referenced
/// fully-qualified (`FS.GG.UI.Themes.AntDesign.AntTheme.*`) to avoid the self-clash.
module SecondAntShowcase.Core.AntTheme

open FS.GG.UI.DesignSystem
open SecondAntShowcase.Core.Model

/// Ant light theme (the shipped variant).
let antLight: Theme = FS.GG.UI.Themes.AntDesign.AntTheme.antLight

/// Ant dark theme (the shipped variant).
let antDark: Theme = FS.GG.UI.Themes.AntDesign.AntTheme.antDark

/// Resolve a mode into the renderable Ant theme.
let resolve (mode: ThemeMode): Theme =
    match mode with
    | Light -> antLight
    | Dark -> antDark

/// A default theme for content that needs a `Theme` at build time (e.g. rich-text
/// default style). Rendering always re-themes per the live model.
let defaultTheme: Theme = antLight

/// Stable textual name for a mode (status display / evidence).
let modeName (mode: ThemeMode): string =
    match mode with
    | Light -> "antLight"
    | Dark -> "antDark"
