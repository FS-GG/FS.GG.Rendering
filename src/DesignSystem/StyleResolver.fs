namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

// Feature 129 (F4): the FRONT HALF of style resolution — the single, total, deterministic path
// `theme + control-kind + semantic-intent + visual-state(s) → ResolvedStyle`. It supplies the
// kind's structural `baseStyle`, lets the theme's own `IntentPolicy` perturb it by intent, then
// hands off to the shipped 093 back half `Style.resolve` for the class+state overlay, preserving
// the 093/096 precedence (`base < classes (attach order) < visual state`) verbatim.
//
// Feature 173: the policy is no longer an argument the caller chooses — it rides on the `Theme`.
// A control names a kind and an intent, never a theme, so the only way a theme's intent language
// could reach the screen was for the theme to carry it. The Default theme carries
// `IntentPolicy.neutral` (intent ignored ⇒ output unchanged); the Ant theme carries a policy that
// makes `primary`/`default`/`dashed`/`text`/`link`/`danger` structurally distinct.
module StyleResolver =

    /// The kind's structural base style — the exact literals relocated verbatim from
    /// `buttonGeom` (`Control.fs:823-839`). Total over every `kind`: `"button"` → filled accent,
    /// `"icon-button"` → accent outline, any other/unknown kind → the filled base (a defined,
    /// visible fallback — never empty, transparent-fill-only, or an exception). (FR-004, R5)
    let baseStyleFor (theme: Theme) (kind: string) : ResolvedStyle =
        match kind with
        | "icon-button" ->
            // outline: transparent fill, accent stroke, accent text.
            { Foreground = theme.Accent
              Fill = Colors.transparent
              Stroke = theme.Accent
              StrokeWidth = 2.0
              StrokeDash = []
              FontFamily = theme.FontFamily
              // #384: the base tracks the theme's body size (Ant `fontSize` = 14) instead of a frozen
              // 15.0. A theme now restyles button typography, and the class layer's `StyleClass.Font`
              // (or the theme's `IntentPolicy`) overlays deltas on top of this themed base.
              FontSize = theme.FontSize
              FontWeight = None }
        | _ ->
            // filled (the "button" base, and the defined fallback for any unknown kind).
            { Foreground = theme.Background
              Fill = theme.Accent
              Stroke = theme.Accent
              StrokeWidth = 0.0
              StrokeDash = []
              FontFamily = theme.FontFamily
              // #384: base tracks `theme.FontSize` (was a frozen 15.0) — see the icon-button branch.
              FontSize = theme.FontSize
              FontWeight = None }

    /// The single front-half resolution path: supply the kind's structural base, let the theme's
    /// policy perturb it by intent, then hand off to the 093 back half for the unchanged
    /// `base < classes (attach order) < visual state` overlay. Total + deterministic.
    ///
    /// The intent is lowered once, here, so every policy matches on a canonical string and none has
    /// to remember to do it.
    let resolve
        (theme: Theme)
        (kind: string)
        (intent: string)
        (classes: StyleClass list)
        (state: VisualState)
        : ResolvedStyle =
        let baseStyle = baseStyleFor theme kind
        let intended = theme.IntentPolicy.ApplyIntent theme kind (intent.ToLowerInvariant()) baseStyle
        Style.resolve theme intended classes state
