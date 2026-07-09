namespace FS.GG.UI.Themes.AntDesign

open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AntIntentPolicy =
    // Feature 132 (D2.1, data-model §2 / contract C3): the intent seam. `ApplyIntent` perturbs ONLY the
    // kind's structural base by lowered intent, then `StyleResolver.resolve` hands off to the 093 back
    // half for the class+state overlay, so precedence (base < classes < state) is preserved. Every intent
    // (incl. `""`/unknown) returns a defined `ResolvedStyle`; never raises (C3 totality).
    //
    // Feature 173: `AntTheme.antLight`/`antDark` now CARRY these policies, so a `Button.view` styled
    // `Dashed`/`Text`/`Link` under the Ant theme reaches the screen structurally distinct rather than
    // collapsing onto the neutral filled base.
    //
    // Ant's neutral button chrome (the container surface a `default` button fills, its border, its label)
    // cannot come from `Theme` roles: `Theme` has ONE `Background`, which collapses Ant's layout /
    // container / elevated surfaces onto the canvas gray. It comes from the generated Ant `Map`/`Component`
    // token layers instead, bound once per mode below. The brand roles (`Accent`, `Danger`) stay theme
    // roles so a re-accented Ant theme still re-accents its buttons.

    /// Ant's dashed control border: equal 4px on/off runs, zero phase. Geometry, not colour — the DTCG
    /// source carries no dash token, and `ResolvedStyle.StrokeDash` is what the geometry hands to
    /// `PathEffect.Dash`.
    let private dashPattern = [ 4.0; 4.0 ]

    /// `surface`/`border`/`text` are the mode's neutral chrome. `name` is the policy's identity: `Theme`
    /// equality (and so the retained renderer's ThemeChanged diff) compares policies by it, so the two
    /// modes must not share one.
    let private policyFor (name: string) (surface: Color) (border: Color) (text: Color) : IntentPolicy =
        // The outline kinds: an `icon-button` has no room for a fill behind its glyph, so intent moves the
        // stroke and the label, never the fill.
        let outline (theme: Theme) (intent: string) (b: ResolvedStyle) =
            match intent with
            | "default" -> { b with Stroke = border; Foreground = text }
            | "dashed" ->
                { b with
                    Stroke = border
                    Foreground = text
                    StrokeDash = dashPattern }
            | "text" ->
                { b with
                    Stroke = Colors.transparent
                    StrokeWidth = 0.0
                    Foreground = text }
            | "link" ->
                { b with
                    Stroke = Colors.transparent
                    StrokeWidth = 0.0
                    Foreground = theme.Accent }
            | "danger" -> { b with Stroke = theme.Danger; Foreground = theme.Danger }
            // "primary" IS the accent outline the structural base already carries; "" / unknown → identity.
            | _ -> b

        // The filled kinds ("button", and the filled fallback every unknown kind shares).
        let filled (theme: Theme) (intent: string) (b: ResolvedStyle) =
            match intent with
            | "primary" ->
                // Brand-blue fill, on-primary label, no border — the structural base, stated explicitly.
                { b with
                    Fill = theme.Accent
                    Foreground = theme.Background
                    Stroke = theme.Accent
                    StrokeWidth = 0.0
                    StrokeDash = [] }
            | "default" ->
                { b with
                    Fill = surface
                    Foreground = text
                    Stroke = border
                    StrokeWidth = 1.0
                    StrokeDash = [] }
            | "dashed" ->
                // As `default`, with a real dashed stroke rather than a heavier solid one.
                { b with
                    Fill = surface
                    Foreground = text
                    Stroke = border
                    StrokeWidth = 1.0
                    StrokeDash = dashPattern }
            | "text" ->
                { b with
                    Fill = Colors.transparent
                    Foreground = text
                    Stroke = Colors.transparent
                    StrokeWidth = 0.0
                    StrokeDash = [] }
            | "link" ->
                { b with
                    Fill = Colors.transparent
                    Foreground = theme.Accent
                    Stroke = Colors.transparent
                    StrokeWidth = 0.0
                    StrokeDash = [] }
            | "danger" ->
                { b with
                    Fill = theme.Danger
                    Foreground = theme.Background
                    Stroke = theme.Danger
                    StrokeWidth = 0.0
                    StrokeDash = [] }
            // "" / unknown → identity (structural base unchanged) — total, never raises.
            | _ -> b

        { Name = name
          ApplyIntent =
            fun theme kind intent baseStyle ->
                match kind with
                | "icon-button" -> outline theme intent baseStyle
                | _ -> filled theme intent baseStyle }

    /// The policy `AntTheme.antLight` carries.
    let light: IntentPolicy =
        policyFor
            "ant-light"
            DesignTokensExt.Map.Light.colorBgContainer
            DesignTokensExt.Component.Button.defaultBorder
            DesignTokensExt.Map.Light.colorText

    /// The policy `AntTheme.antDark` carries.
    let dark: IntentPolicy =
        policyFor
            "ant-dark"
            DesignTokensExt.Map.Dark.colorBgContainer
            DesignTokensExt.Map.Dark.colorBorder
            DesignTokensExt.Map.Dark.colorText
