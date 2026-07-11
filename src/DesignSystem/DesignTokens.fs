// GENERATED — do not edit. Source: src/Themes.Default/design-tokens.tokens.json
// Regenerate via: dotnet fsi scripts/generate-design-tokens.fsx
//
// Feature 069/125: the flat light/dark primitive blocks that feed Theme.light/dark. These VALUES are
// generated from the DTCG source; the paired hand-curated DesignTokens.fsi is the sole public-surface
// declaration. Currency of both files is enforced by this generator's --check (the Feature 126 gate).
namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

module DesignTokens =
    module Light =
        let foreground : Color = Colors.rgba 31uy 41uy 55uy 255uy
        let background : Color = Colors.rgba 248uy 250uy 252uy 255uy
        let accent : Color = Colors.rgba 37uy 99uy 235uy 255uy
        let danger : Color = Colors.rgba 185uy 28uy 28uy 255uy
        let success : Color = Colors.rgba 21uy 128uy 61uy 255uy
        let warning : Color = Colors.rgba 180uy 83uy 9uy 255uy
        let muted : Color = Colors.rgba 100uy 116uy 139uy 255uy
        let fontFamily : string option = None
        let fontSize : float = 14.0
        let density : float = 1.0
        let cornerRadius : float = 4.0
        let contrastRequiredRatio : float = 4.5
        let controlHeight : float = 32.0
        let controlHeightSm : float = 24.0
        let controlHeightLg : float = 40.0
        let spaceXs : float = 4.0
        let spaceSm : float = 8.0
        let spaceMd : float = 16.0
        let spaceLg : float = 24.0

    module Dark =
        let foreground : Color = Colors.rgba 241uy 245uy 249uy 255uy
        let background : Color = Colors.rgba 17uy 24uy 39uy 255uy
        let accent : Color = Colors.rgba 96uy 165uy 250uy 255uy
        let danger : Color = Colors.rgba 255uy 149uy 146uy 255uy
        let success : Color = Colors.rgba 74uy 222uy 128uy 255uy
        let warning : Color = Colors.rgba 251uy 191uy 36uy 255uy
        let muted : Color = Colors.rgba 148uy 163uy 184uy 255uy
        let fontFamily : string option = None
        let fontSize : float = 14.0
        let density : float = 1.0
        let cornerRadius : float = 4.0
        let contrastRequiredRatio : float = 4.5
        let controlHeight : float = 32.0
        let controlHeightSm : float = 24.0
        let controlHeightLg : float = 40.0
        let spaceXs : float = 4.0
        let spaceSm : float = 8.0
        let spaceMd : float = 16.0
        let spaceLg : float = 24.0
