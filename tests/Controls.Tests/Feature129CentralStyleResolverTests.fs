module Feature129CentralStyleResolverTests

// Feature 129 (Workstream F, F4) — the central visual-state style resolver
// (`theme → kind → intent → states → style`).
//
// The FRONT HALF is `module internal StyleResolver` in FS.GG.UI.DesignSystem (reached here via
// InternalsVisibleTo). It supplies the kind's structural base under an overridable IntentPolicy,
// then composes the shipped 093 back half `Style.resolve` for the class+state overlay.
//
// Coverage (filters mirror quickstart.md):
//   * parity     (V2 / G1+G2, FR-003/SC-001): default-theme byte-identity — resolveDefault equals
//     the pre-migration `Style.resolve theme <structural base> classes state`, and the migrated
//     Button/IconButton Scene is byte-identical to the frozen pre-migration geometry.
//   * totality   (V3 / G3, FR-004/FR-006/SC-003): the full {kind}×{intent}×{state} cross-product
//     (incl. an unknown kind + unknown intent) is total, deterministic, and preserves 093 precedence.
//   * divergence (V4 / G4+G5, FR-002/FR-005/FR-008/SC-002/SC-006/SC-007): a non-default policy makes
//     `danger` diverge from `primary` THROUGH THE RESOLVER ALONE — neutral keeps them equal, and no
//     control type is forked per intent.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

// ---- parity oracle: the pre-migration structural bases, verbatim from Control.fs:823-839 -------
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

/// The oracle structural base for a kind: outline for `icon-button`, filled otherwise (the
/// defined fallback — matches `StyleResolver.baseStyleFor`'s totality contract).
let private oracleBase (theme: Theme) (kind: string) : ResolvedStyle =
    match kind with
    | "icon-button" -> outlineBase theme
    | _ -> filledBase theme

let private themes = [ "light", Theme.light; "dark", Theme.dark ]
let private knownKinds = [ "button"; "icon-button" ]
let private knownIntents = [ "primary"; "secondary"; "danger"; "ghost" ]

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

// ---- frozen pre-migration Button/IconButton geometry (the Scene parity oracle, Control.fs:816-848)
let private box: Rect = { X = 10.0; Y = 40.0; Width = 284.0; Height = 92.0 }

let private mkText (theme: Theme) (x: float) (baseline: float) (size: float) (color: Color) (s: string) =
    Scene.textRun
        { Text = s
          Position = { X = x; Y = baseline }
          Font = { Family = theme.FontFamily; Size = size; Weight = None }
          Paint = Paint.fill color }

let private frozenFilledScene (theme: Theme) (label: string) : Scene list =
    let h = 38.0
    let textW = (Scene.measureText label { Family = theme.FontFamily; Size = 15.0; Weight = None }).Width
    let w = min box.Width (max 70.0 (textW + 32.0))
    let by = box.Y + box.Height / 2.0 - h / 2.0
    [ Scene.rectangle (box.X, by, w, h) theme.Accent
      mkText theme (box.X + 16.0) (by + h / 2.0 + 5.0) 15.0 theme.Background label ]

let private frozenOutlineScene (theme: Theme) (label: string) : Scene list =
    let h = 38.0
    let textW = (Scene.measureText label { Family = theme.FontFamily; Size = 15.0; Weight = None }).Width
    let w = min box.Width (max 70.0 (textW + 32.0))
    let by = box.Y + box.Height / 2.0 - h / 2.0
    let rect = { X = box.X; Y = by; Width = w; Height = h }
    [ Scene.rectangleWithPaint rect (Paint.stroke theme.Accent 2.0)
      mkText theme (box.X + 16.0) (by + h / 2.0 + 5.0) 15.0 theme.Accent label ]

[<Tests>]
let feature129CentralStyleResolverTests =
    testList
        "Feature129 central style resolver"
        [
          // ============ parity (V2 / G1+G2) ============================================
          testList
              "parity"
              [
                // T013 (G1, FR-003/SC-001): default-policy style byte-identity vs the pre-migration
                // oracle, for both themes, across every kind × intent × state — the intent is ignored.
                test "resolveDefault is byte-identical to the pre-migration oracle (G1)" {
                    for (tname, theme) in themes do
                        for kind in knownKinds do
                            for intent in knownIntents do
                                for state in allStates do
                                    for classes in [ []; sampleClasses ] do
                                        let migrated =
                                            StyleResolver.resolveDefault theme kind intent classes state

                                        let oracle = Style.resolve theme (oracleBase theme kind) classes state

                                        Expect.equal
                                            migrated
                                            oracle
                                            (sprintf
                                                "resolveDefault %s/%s/%s/%A must byte-equal the pre-migration oracle"
                                                tname
                                                kind
                                                intent
                                                state)
                }

                // T014 (G2, FR-003/SC-001): the migrated Button/IconButton Scene is byte-identical to
                // the frozen pre-migration geometry — and the intent (now threaded) never changes it
                // under the default policy.
                test "migrated Button/IconButton Scene byte-matches the pre-migration geometry, intent-independent (G2)" {
                    for (tname, theme) in themes do
                        for intent in knownIntents do
                            let btn = Button.create [ Button.text "Save"; Attr.style intent ]
                            Expect.equal
                                (ControlInternals.faithfulContent theme box btn)
                                (frozenFilledScene theme "Save")
                                (sprintf "button.%s intent=%s Scene matches the frozen filled geometry" tname intent)

                            let icon = IconButton.create [ IconButton.icon "Go"; Attr.style intent ]
                            Expect.equal
                                (ControlInternals.faithfulContent theme box icon)
                                (frozenOutlineScene theme "Go")
                                (sprintf "icon-button.%s intent=%s Scene matches the frozen outline geometry" tname intent)
                }
              ]

          // ============ totality (V3 / G3) ============================================
          testList
              "totality"
              [
                // T008 (G3, FR-004/SC-003): the full cross-product — incl. an unknown kind and an
                // unknown intent string — yields a concrete style with zero exceptions, and is
                // deterministic across two runs.
                test "resolve is total + deterministic over the full kind×intent×state cross-product (G3)" {
                    let kinds = knownKinds @ [ "Custom" ] // an unknown/unmapped kind
                    let intents = knownIntents @ [ "totally-unknown-intent" ] // an unknown intent string

                    let runOnce () =
                        [ for kind in kinds do
                              for intent in intents do
                                  for state in allStates do
                                      yield StyleResolver.resolve StyleResolver.neutralPolicy Theme.light kind intent [] state ]

                    let first = runOnce ()
                    let second = runOnce ()

                    // every combination produced a concrete ResolvedStyle (no exception reaching here)
                    Expect.equal first.Length (kinds.Length * intents.Length * allStates.Length) "every combination resolved"
                    // determinism: byte-equal across two independent runs
                    Expect.equal first second "the resolver is deterministic across repeated runs"
                }

                // T009 (FR-006, contract guarantee 5): with non-empty classes and a non-Normal state,
                // resolve == Style.resolve theme (baseStyleFor theme kind) classes state — proving the
                // front half supplies only the base and the 093 precedence is preserved, not replaced.
                test "resolve preserves the 093 base<classes<state precedence (composition, not replacement)" {
                    for kind in knownKinds do
                        for state in [ Hover; Pressed; Selected; Disabled ] do
                            let viaResolver =
                                StyleResolver.resolve StyleResolver.neutralPolicy Theme.light kind "primary" sampleClasses state

                            let viaBackHalf =
                                Style.resolve Theme.light (StyleResolver.baseStyleFor Theme.light kind) sampleClasses state

                            Expect.equal
                                viaResolver
                                viaBackHalf
                                (sprintf "front half supplies only baseStyle; 093 overlay unchanged (%s/%A)" kind state)
                }
              ]

          // ============ divergence (V4 / G4+G5) ============================================
          testList
              "divergence"
              [
                // T019 (G4, FR-002/FR-005/SC-002/SC-007): a non-default policy maps `danger` to
                // theme.Danger, making it diverge from `primary` — while `neutralPolicy` keeps the two
                // EQUAL (today's intent-drop preserved by default).
                test "a divergent IntentPolicy makes danger differ from primary; neutral keeps them equal (G4)" {
                    let divergentPolicy: StyleResolver.IntentPolicy =
                        { ApplyIntent =
                            fun theme intent s ->
                                match intent with
                                | "danger" -> { s with Fill = theme.Danger; Stroke = theme.Danger; Foreground = theme.Background }
                                | _ -> s }

                    for kind in knownKinds do
                        // Normal + no classes so the base-level intent delta survives (a state like
                        // Hover would overwrite Fill and mask the divergence).
                        let dangerDiv = StyleResolver.resolve divergentPolicy Theme.light kind "danger" [] Normal
                        let primaryDiv = StyleResolver.resolve divergentPolicy Theme.light kind "primary" [] Normal
                        Expect.notEqual dangerDiv primaryDiv (sprintf "divergent policy: danger ≠ primary for %s" kind)

                        let dangerNeutral = StyleResolver.resolve StyleResolver.neutralPolicy Theme.light kind "danger" [] Normal
                        let primaryNeutral = StyleResolver.resolve StyleResolver.neutralPolicy Theme.light kind "primary" [] Normal
                        Expect.equal dangerNeutral primaryNeutral (sprintf "neutral policy: danger ≡ primary for %s (intent dropped)" kind)
                }

                // T020 (G5, FR-008/SC-006/SC-007): the divergence above is reached through the resolver
                // ALONE — the control render path stays neutral (danger Button renders == primary Button),
                // and no control type is forked per intent (control-type count carries no intent kinds).
                test "divergence needs no control edit: control path stays neutral; no per-intent control type (G5)" {
                    // control render path is intent-neutral by default (proven without any control edit)
                    let dangerBtn = Button.create [ Button.text "Save"; Attr.style "danger" ]
                    let primaryBtn = Button.create [ Button.text "Save"; Attr.style "primary" ]
                    Expect.equal
                        (ControlInternals.faithfulContent Theme.light box dangerBtn)
                        (ControlInternals.faithfulContent Theme.light box primaryBtn)
                        "default control render does NOT diverge by intent — divergence lives only in the resolver seam"

                    // no control type is forked per intent (FR-008/SC-006): no catalog kind encodes an intent
                    let intentForkedKinds =
                        Catalog.supportedControls
                        |> List.filter (fun d ->
                            knownIntents |> List.exists (fun i -> d.Id.Contains i))

                    Expect.isEmpty
                        intentForkedKinds
                        "no control type is forked per intent — the seam, not a new control, carries divergence"
                }
              ]
        ]
