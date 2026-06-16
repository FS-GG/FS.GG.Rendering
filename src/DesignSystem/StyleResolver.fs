namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

// Feature 129 (F4): the FRONT HALF of style resolution — the single, total, deterministic path
// `theme + control-kind + semantic-intent + visual-state(s) → ResolvedStyle`. It supplies the
// kind's structural `baseStyle` (under an overridable intent policy) and then hands off to the
// shipped 093 back half `Style.resolve` for the class+state overlay, preserving the 093/096
// precedence (`base < classes (attach order) < visual state`) verbatim.
//
// `module internal` with NO .fsi (mirrors 126/127): zero public-surface delta. Reached by the
// Button render path (`buttonGeom`) and the parity/totality/divergence tests via the
// `InternalsVisibleTo` grants in DesignSystem.fsproj (FS.GG.UI.Controls, Controls.Tests).
//
// The default policy (`neutralPolicy`) is intent-AGNOSTIC: it returns the kind's structural base
// unchanged, so wiring the resolver is byte-identical under the default theme (the intent the
// renderer drops today is dropped here too — but now as an explicit identity policy over a
// THREADED argument, making it a live seam rather than dead code). A non-default policy supplied
// directly to `resolve` proves intent divergence is reachable with zero control edits (US3).
module StyleResolver =

    /// The overridable `(theme, lowered-intent-string, structural base) → adjusted base` seam.
    /// `neutralPolicy` keeps it identity (intent ignored ⇒ default-neutral); a divergent policy
    /// (e.g. mapping `"danger"` to `theme.Danger`) is what D2/Ant and F5 will supply.
    type IntentPolicy =
        { ApplyIntent: Theme -> string -> ResolvedStyle -> ResolvedStyle }

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
              FontFamily = theme.FontFamily
              FontSize = 15.0
              FontWeight = None }
        | _ ->
            // filled (the "button" base, and the defined fallback for any unknown kind).
            { Foreground = theme.Background
              Fill = theme.Accent
              Stroke = theme.Accent
              StrokeWidth = 0.0
              FontFamily = theme.FontFamily
              FontSize = 15.0
              FontWeight = None }

    /// The default, intent-agnostic policy: returns the structural base unchanged.
    let neutralPolicy: IntentPolicy = { ApplyIntent = fun _ _ s -> s }

    /// The single front-half resolution path: supply the kind's structural base, let the policy
    /// perturb it by intent (neutral = identity), then hand off to the 093 back half for the
    /// unchanged `base < classes (attach order) < visual state` overlay. Total + deterministic.
    let resolve
        (policy: IntentPolicy)
        (theme: Theme)
        (kind: string)
        (intent: string)
        (classes: StyleClass list)
        (state: VisualState)
        : ResolvedStyle =
        Style.resolve theme (policy.ApplyIntent theme intent (baseStyleFor theme kind)) classes state

    /// Convenience: the neutral path that `buttonGeom` calls — intent threaded but ignored, so
    /// byte-identical to the pre-migration `Style.resolve theme (structural base) classes state`.
    /// Written as a fully-applied function (not the eta-reduced `resolve neutralPolicy`) so its
    /// arity matches the curated public signature in StyleResolver.fsi; semantically identical.
    let resolveDefault theme kind intent classes state = resolve neutralPolicy theme kind intent classes state
