module Feature130PublicSurfaceTests

// Feature 130 (Workstream F, F5) — the PUBLIC token/policy/resolver surface.
//
// F5 promotes two proven-internal capabilities to a public, `.fsi`-declared contract:
//   * `FS.GG.UI.DesignSystem.StyleResolver` — the front-half resolver + the overridable `IntentPolicy`
//     seam (`baseStyleFor`/`neutralPolicy`/`resolve`/`resolveDefault`).
//   * `FS.GG.UI.DesignSystem.DesignTokensExt` — the Ant-derived seed/map/alias/component taxonomy
//     plus space/density/type/elevation.
//
// These tests reach the promoted symbols THROUGH THE PUBLIC API ONLY — the DesignSystem
// `InternalsVisibleTo` grants for FS.GG.UI.Controls / Controls.Tests are REMOVED in this same change,
// so a green compile here is machine proof the surface is public, not internal (INV-4/SC-001).
//
// Coverage (filters mirror quickstart.md V4):
//   * public-path consumption — name `StyleResolver.*` + read `DesignTokensExt.*` with no IVT (INV-4).
//   * value parity — representative promoted token values equal their known literals (INV-2/SC-003).
//   * render neutrality — `resolveDefault` is byte-identical to the Feature129 neutral oracle across the
//     full {kind}×{intent}×{state} cross-product (INV-3/SC-003).
//   * divergence — a custom public `IntentPolicy` makes `danger` diverge from `resolveDefault` (INV-5/SC-005).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem
open FS.GG.UI.Themes.Default

// ---- neutral-resolution oracle: the pre-promotion structural bases, verbatim (Feature129) -------
let private filledBase (theme: Theme) : ResolvedStyle =
    { Foreground = theme.Background
      Fill = theme.Accent
      Stroke = theme.Accent
      StrokeWidth = 0.0
      FontFamily = theme.FontFamily
      FontSize = 15.0
      FontWeight = None }

let private outlineBase (theme: Theme) : ResolvedStyle =
    { Foreground = theme.Accent
      Fill = Colors.transparent
      Stroke = theme.Accent
      StrokeWidth = 2.0
      FontFamily = theme.FontFamily
      FontSize = 15.0
      FontWeight = None }

/// The oracle structural base for a kind: outline for `icon-button`, filled otherwise — matches
/// `StyleResolver.baseStyleFor`'s totality contract (the Feature129 parity oracle).
let private oracleBase (theme: Theme) (kind: string) : ResolvedStyle =
    match kind with
    | "icon-button" -> outlineBase theme
    | _ -> filledBase theme

let private themes = [ "light", Theme.light; "dark", Theme.dark ]
let private knownKinds = [ "button"; "icon-button"; "unknown-kind" ]
let private knownIntents = [ "primary"; "secondary"; "danger"; "ghost"; "totally-unknown-intent" ]

/// All 8 VisualState cases, incl. a representative `Validation`.
let private allStates: VisualState list =
    [ Normal
      Disabled
      Hover
      Pressed
      Focused
      Selected
      Loading
      VisualState.Validation(ValidationState.Invalid "err") ]

let private sampleClasses: StyleClass list =
    [ Variant StyleVariant.Danger; Custom "muted" ]

[<Tests>]
let feature130PublicSurfaceTests =
    testList "Feature 130 public token/resolver surface" [

        // ===== public-path consumption (INV-4/SC-001) — T005 ==========================================
        // Naming StyleResolver.* and reading DesignTokensExt.* compiles & runs with NO IVT grant. The
        // assertions are incidental; the COMPILE is the proof of public visibility.
        test "promoted StyleResolver + DesignTokensExt are reachable through the public API only (INV-4/SC-001)" {
            // StyleResolver public members are nameable and callable.
            let resolved =
                StyleResolver.resolve StyleResolver.neutralPolicy Theme.light "button" "primary" [] Normal
            let viaDefault = StyleResolver.resolveDefault Theme.light "button" "primary" [] Normal
            Expect.equal resolved viaDefault "resolve neutralPolicy == resolveDefault (public seam)"

            let baseStyle = StyleResolver.baseStyleFor Theme.light "button"
            Expect.equal baseStyle.Fill Theme.light.Accent "baseStyleFor button is the filled accent base"

            // DesignTokensExt leaves are nameable from a public consumer (one per layer/group).
            Expect.isTrue
                (DesignTokensExt.Seed.colorPrimary <> Colors.transparent
                 && DesignTokensExt.Map.Light.colorText <> Colors.transparent
                 && DesignTokensExt.Alias.Dark.textDefault <> Colors.transparent
                 && DesignTokensExt.Component.Button.primaryBg <> Colors.transparent
                 && DesignTokensExt.Space.md > 0.0
                 && DesignTokensExt.Density.comfortable > 0.0
                 && DesignTokensExt.Type.Body.fontSize > 0.0
                 && DesignTokensExt.Elevation.medium <> "")
                "every taxonomy layer/group is publicly nameable"
        }

        // ===== value parity (INV-2/SC-003) — T006 =====================================================
        // Representative promoted values from every layer equal their known literals — promotion is
        // visibility-only, token values are byte-identical to the pre-promotion (regenerated) file.
        test "representative promoted DesignTokensExt values equal their known literals (INV-2/SC-003)" {
            Expect.equal DesignTokensExt.Seed.colorPrimary (Colors.rgba 22uy 119uy 255uy 255uy) "Seed.colorPrimary"
            Expect.equal DesignTokensExt.Seed.controlHeight 32.0 "Seed.controlHeight"
            Expect.equal DesignTokensExt.Map.Light.colorText (Colors.rgba 31uy 41uy 55uy 255uy) "Map.Light.colorText"
            Expect.equal DesignTokensExt.Map.Dark.colorText (Colors.rgba 241uy 245uy 249uy 255uy) "Map.Dark.colorText"
            Expect.equal DesignTokensExt.Alias.Light.textDefault (Colors.rgba 31uy 41uy 55uy 255uy) "Alias.Light.textDefault"
            Expect.equal DesignTokensExt.Component.Button.primaryBg (Colors.rgba 22uy 119uy 255uy 255uy) "Component.Button.primaryBg"
            Expect.equal DesignTokensExt.Space.md 16.0 "Space.md"
            Expect.equal DesignTokensExt.Density.comfortable 1.0 "Density.comfortable"
            Expect.equal DesignTokensExt.Type.Body.fontSize 14.0 "Type.Body.fontSize"
            Expect.equal DesignTokensExt.Elevation.medium "0 4 12 #00000014" "Elevation.medium"
        }

        // ===== render neutrality (INV-3/SC-003) — T007 ================================================
        // resolveDefault is byte-identical to the pre-promotion neutral oracle across the FULL
        // {kind}×{intent}×{state} cross-product (incl. unknown kind/intent), both themes, ±classes.
        test "resolveDefault is byte-identical to the Feature129 neutral oracle across the cross-product (INV-3/SC-003)" {
            for (tname, theme) in themes do
                for kind in knownKinds do
                    for intent in knownIntents do
                        for state in allStates do
                            for classes in [ []; sampleClasses ] do
                                let migrated = StyleResolver.resolveDefault theme kind intent classes state
                                let oracle = Style.resolve theme (oracleBase theme kind) classes state
                                Expect.equal
                                    migrated
                                    oracle
                                    (sprintf "resolveDefault %s/%s/%s/%A neutral parity" tname kind intent state)
        }

        // ===== divergence via a public policy (INV-5/SC-005) — T022 ===================================
        // A custom public IntentPolicy mapping "danger" -> theme.Danger makes a danger button diverge from
        // resolveDefault — proven from the public seam, with ZERO control edits.
        test "a custom public IntentPolicy makes danger diverge from resolveDefault — no control edits (INV-5/SC-005)" {
            let divergentPolicy: StyleResolver.IntentPolicy =
                { ApplyIntent =
                    fun theme intent s ->
                        match intent with
                        | "danger" -> { s with Fill = theme.Danger; Stroke = theme.Danger; Foreground = theme.Background }
                        | _ -> s }

            for kind in [ "button"; "icon-button" ] do
                // Normal + no classes so the base-level intent delta survives (Hover would overwrite Fill).
                let dangerDivergent = StyleResolver.resolve divergentPolicy Theme.light kind "danger" [] Normal
                let dangerDefault = StyleResolver.resolveDefault Theme.light kind "danger" [] Normal
                Expect.notEqual
                    dangerDivergent
                    dangerDefault
                    (sprintf "divergent policy: danger differs from resolveDefault for %s" kind)

                // neutral keeps danger == primary (today's intent-drop preserved by default).
                let dangerNeutral = StyleResolver.resolveDefault Theme.light kind "danger" [] Normal
                let primaryNeutral = StyleResolver.resolveDefault Theme.light kind "primary" [] Normal
                Expect.equal
                    dangerNeutral
                    primaryNeutral
                    (sprintf "neutral policy: danger == primary for %s (intent dropped)" kind)
        }
    ]
