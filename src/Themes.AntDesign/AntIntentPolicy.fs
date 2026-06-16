namespace FS.GG.UI.Themes.AntDesign

open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AntIntentPolicy =
    // Feature 132 (D2.1, data-model §2 / contract C3): the front-half intent seam. `ApplyIntent`
    // perturbs ONLY the kind's structural base by lowered intent, then `StyleResolver.resolve` hands
    // off to the 093 back half for the class+state overlay, so precedence (base < classes < state) is
    // preserved. Every intent (incl. `""`/unknown) returns a defined `ResolvedStyle`; never raises
    // (C3 totality). Each mapped intent yields a STRUCTURALLY DISTINCT style so the divergence is
    // observable (Ant primary vs default vs dashed vs text vs link vs danger). All colours come from
    // `theme` roles (token-sourced); no inline literals.
    let private applyIntent (theme: Theme) (intent: string) (baseStyle: ResolvedStyle) : ResolvedStyle =
        match intent.ToLowerInvariant() with
        | "primary" ->
            // Brand-blue fill, on-primary (background) foreground, no border.
            { baseStyle with
                Fill = theme.Accent
                Foreground = theme.Background
                Stroke = theme.Accent
                StrokeWidth = 0.0 }
        | "default" ->
            // Neutral surface fill with a 1px neutral outline.
            { baseStyle with
                Fill = theme.Background
                Foreground = theme.Foreground
                Stroke = theme.Muted
                StrokeWidth = 1.0 }
        | "dashed" ->
            // As default, a heavier outline standing in for Ant's dashed stroke (ResolvedStyle carries
            // no dash pattern; the thicker border keeps it structurally distinct from `default`).
            { baseStyle with
                Fill = theme.Background
                Foreground = theme.Foreground
                Stroke = theme.Muted
                StrokeWidth = 2.0 }
        | "text" ->
            // No fill/stroke; neutral foreground (the accent-on-hover overlay rides the visual state).
            { baseStyle with
                Fill = Colors.transparent
                Foreground = theme.Foreground
                Stroke = Colors.transparent
                StrokeWidth = 0.0 }
        | "link" ->
            // No fill/stroke; accent foreground.
            { baseStyle with
                Fill = Colors.transparent
                Foreground = theme.Accent
                Stroke = Colors.transparent
                StrokeWidth = 0.0 }
        | "danger" ->
            // theme.Danger applied to fill/stroke; on-danger (background) foreground.
            { baseStyle with
                Fill = theme.Danger
                Foreground = theme.Background
                Stroke = theme.Danger
                StrokeWidth = 0.0 }
        | _ ->
            // "" / unknown → identity (structural base unchanged) — total, never raises.
            baseStyle

    let policy: StyleResolver.IntentPolicy = { ApplyIntent = applyIntent }
