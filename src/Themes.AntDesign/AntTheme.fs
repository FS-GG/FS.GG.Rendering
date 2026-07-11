namespace FS.GG.UI.Themes.AntDesign

open FS.GG.UI.DesignSystem

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AntTheme =
    // Feature 132 (D2.1, FR-002 / contract C2): every field is sourced from a generated
    // DesignTokensExt (Ant-derived) entry or a DesignTokens DTCG token — no inline hex/size at the
    // use site. `Name` labels the variant and stays a code constant (it is not a design token, and
    // the parity test reads it only for labelling, never branches on it). The Ant primary family
    // (#1677ff) comes from `Seed.colorPrimary` on dark, and from the darker map stop
    // `Map.Light.colorPrimaryActive` (#0958d9) on light for WCAG-AA on the light canvas (#379);
    // the functional families from the per-mode `Map`
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
          // #379: the seed primary (#1677ff) is mid-luminance — as the resting accent it is not
          // WCAG-AA (4.5 Text) on the light canvas (#f5f5f5), failing both as the filled button's
          // near-white label and as the outline icon-button's glyph (3.76 < 4.5). The light accent
          // therefore resolves to Ant's own darker `colorPrimaryActive` (#0958d9 ⇒ 5.65) — a map
          // stop already carried for the pressed state, sourced from a token not inline hex (C2).
          // Dark mode keeps the seed (`antDark` below): #1677ff is AA on the near-black canvas.
          Accent = DesignTokensExt.Map.Light.colorPrimaryActive
          Danger = DesignTokensExt.Map.Light.colorError
          Success = DesignTokensExt.Map.Light.colorSuccess
          Warning = DesignTokensExt.Map.Light.colorWarning
          Muted = DesignTokensExt.Alias.Light.borderDefault
          FontFamily = DesignTokens.Light.fontFamily
          FontSize = DesignTokensExt.Type.Body.fontSize
          Density = DesignTokensExt.Density.middle
          CornerRadius = DesignTokensExt.Seed.borderRadius
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
          IntentPolicy = AntIntentPolicy.dark }

    let resolve (overrides: Theme option) =
        overrides |> Option.defaultValue antLight
