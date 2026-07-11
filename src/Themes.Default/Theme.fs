namespace FS.GG.UI.Themes.Default

open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Theme =
    // Feature 069: every migrated field is sourced from the generated DesignTokens module
    // (single source: src/Themes.Default/design-tokens.tokens.json) — value-identical to the
    // pre-feature literals, zero inline color/size/density/radius/contrast literals. `Name`
    // labels the variant and stays a code constant (it is not a design token).
    let light : Theme =
        { Name = "light"
          Foreground = DesignTokens.Light.foreground
          Background = DesignTokens.Light.background
          Accent = DesignTokens.Light.accent
          Danger = DesignTokens.Light.danger
          // Feature 125 (FR-004): additive success/warning roles, token-sourced; no field below changes.
          Success = DesignTokens.Light.success
          Warning = DesignTokens.Light.warning
          Muted = DesignTokens.Light.muted
          FontFamily = DesignTokens.Light.fontFamily
          FontSize = DesignTokens.Light.fontSize
          Density = DesignTokens.Light.density
          CornerRadius = DesignTokens.Light.cornerRadius
          // #385: token-sourced dimension/spacing metrics feed the control geometry.
          ControlHeight = DesignTokens.Light.controlHeight
          ControlHeightSm = DesignTokens.Light.controlHeightSm
          ControlHeightLg = DesignTokens.Light.controlHeightLg
          SpaceXs = DesignTokens.Light.spaceXs
          SpaceSm = DesignTokens.Light.spaceSm
          SpaceMd = DesignTokens.Light.spaceMd
          SpaceLg = DesignTokens.Light.spaceLg
          // Intent-agnostic: the Default theme renders every intent as the kind's structural base.
          IntentPolicy = IntentPolicy.neutral }

    let dark : Theme =
        { Name = "dark"
          Foreground = DesignTokens.Dark.foreground
          Background = DesignTokens.Dark.background
          Accent = DesignTokens.Dark.accent
          Danger = DesignTokens.Dark.danger
          // Feature 125 (FR-004): additive success/warning roles, token-sourced; no field below changes.
          Success = DesignTokens.Dark.success
          Warning = DesignTokens.Dark.warning
          Muted = DesignTokens.Dark.muted
          FontFamily = DesignTokens.Dark.fontFamily
          FontSize = DesignTokens.Dark.fontSize
          Density = DesignTokens.Dark.density
          CornerRadius = DesignTokens.Dark.cornerRadius
          // #385: token-sourced dimension/spacing metrics feed the control geometry.
          ControlHeight = DesignTokens.Dark.controlHeight
          ControlHeightSm = DesignTokens.Dark.controlHeightSm
          ControlHeightLg = DesignTokens.Dark.controlHeightLg
          SpaceXs = DesignTokens.Dark.spaceXs
          SpaceSm = DesignTokens.Dark.spaceSm
          SpaceMd = DesignTokens.Dark.spaceMd
          SpaceLg = DesignTokens.Dark.spaceLg
          IntentPolicy = IntentPolicy.neutral }

    let withDensity (density: float) (theme: Theme) =
        { theme with Density = max 0.5 density }

    let withAccent accent (theme: Theme) =
        { theme with Accent = accent }

    let resolve (overrides: Theme option) =
        overrides |> Option.defaultValue light
