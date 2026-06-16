namespace FS.GG.UI.Themes.AntDesign

open FS.GG.UI.DesignSystem

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
/// Ant Design concrete theme: brand-blue primary, Ant functional families, 8-unit grid,
/// Ant control sizing/radii/type — all composed from the generated Ant-derived DesignTokensExt.
module AntTheme =
    /// Ant light `Theme` (Ant-derived `DesignTokensExt` light entries).
    val antLight: Theme
    /// Ant dark `Theme` (Ant-derived `DesignTokensExt` dark entries).
    val antDark: Theme
    /// Resolve the effective Ant `Theme`: the caller's `overrides` if present, else `antLight`.
    val resolve: overrides: Theme option -> Theme
