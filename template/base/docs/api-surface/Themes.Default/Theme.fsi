namespace FS.GG.UI.Themes.Default

open FS.GG.UI.DesignSystem

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
/// Palette and density tokens for controls: built-in `light`/`dark` themes plus `withDensity`/`withAccent`/`resolve`.
module Theme =
    /// The built-in light `Theme` (DTCG `DesignTokens.Light` palette).
    val light: Theme
    /// The built-in dark `Theme` (DTCG `DesignTokens.Dark` palette).
    val dark: Theme
    /// Return `theme` scaled by `density` (spacing/size multiplier) for compact or comfortable layouts.
    val withDensity: density: float -> theme: Theme -> Theme
    /// Return `theme` with its accent colour replaced by `accent`.
    val withAccent: accent: FS.GG.UI.Scene.Color -> theme: Theme -> Theme
    /// Resolve the effective `Theme`: the caller's `overrides` if present, otherwise the `light` default.
    val resolve: overrides: Theme option -> Theme
