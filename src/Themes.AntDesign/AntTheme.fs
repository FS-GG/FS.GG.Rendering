namespace FS.GG.UI.Themes.AntDesign

open FS.GG.UI.DesignSystem

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AntTheme =
    // Feature 132 (D2.1, FR-002 / contract C2): every field is sourced from a generated
    // DesignTokensExt (Ant-derived) entry or a DesignTokens DTCG token — no inline hex/size at the
    // use site. `Name` labels the variant and stays a code constant (it is not a design token, and
    // the parity test reads it only for labelling, never branches on it). The Ant primary family
    // (#1677ff) comes from `Seed.colorPrimary`; the functional families from the per-mode `Map`
    // error/success/warning roles (issue #359 — the seed reds/greens/oranges are tuned for a light
    // canvas and read too dark on the near-black dark canvas, so, following the dark-theme convention
    // Ant's dark algorithm also applies, dark mode carries lightened stops chosen to clear WCAG AA both
    // as an on-fill label and as a fill boundary on the canvas); surface/text/border roles from the
    // per-mode Alias layer; the 8-unit grid and Ant control radius/density/type from the Seed/Density/
    // Type layers.
    let antLight: Theme =
        { Name = "AntDesign"
          Foreground = DesignTokensExt.Alias.Light.textDefault
          Background = DesignTokensExt.Alias.Light.surfaceCanvas
          Accent = DesignTokensExt.Seed.colorPrimary
          Danger = DesignTokensExt.Map.Light.colorError
          Success = DesignTokensExt.Map.Light.colorSuccess
          Warning = DesignTokensExt.Map.Light.colorWarning
          Muted = DesignTokensExt.Alias.Light.borderDefault
          FontFamily = DesignTokens.Light.fontFamily
          FontSize = DesignTokensExt.Type.Body.fontSize
          Density = DesignTokensExt.Density.middle
          CornerRadius = DesignTokensExt.Seed.borderRadius
          ContrastRequiredRatio = DesignTokens.Light.contrastRequiredRatio
          IntentPolicy = AntIntentPolicy.light }

    let antDark: Theme =
        { Name = "AntDesign Dark"
          Foreground = DesignTokensExt.Alias.Dark.textDefault
          Background = DesignTokensExt.Alias.Dark.surfaceCanvas
          Accent = DesignTokensExt.Seed.colorPrimary
          Danger = DesignTokensExt.Map.Dark.colorError
          Success = DesignTokensExt.Map.Dark.colorSuccess
          Warning = DesignTokensExt.Map.Dark.colorWarning
          Muted = DesignTokensExt.Alias.Dark.borderDefault
          FontFamily = DesignTokens.Dark.fontFamily
          FontSize = DesignTokensExt.Type.Body.fontSize
          Density = DesignTokensExt.Density.middle
          CornerRadius = DesignTokensExt.Seed.borderRadius
          ContrastRequiredRatio = DesignTokens.Dark.contrastRequiredRatio
          IntentPolicy = AntIntentPolicy.dark }

    let resolve (overrides: Theme option) =
        overrides |> Option.defaultValue antLight
