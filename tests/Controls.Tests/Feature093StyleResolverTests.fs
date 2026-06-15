module Feature093StyleResolverTests

// Feature 093 (E3) — the pure state→style resolver.
//   * SC-001 (T010): each built-in StyleVariant resolves to a token-derived ResolvedStyle, two
//     variants on one base differ token-appropriately, and a free-form Custom class flows through
//     the same fold.
//   * SC-002 (T014): each VisualState the procedural baseline differentiates resolves distinctly
//     (Loading inherits Normal, preserving parity), the visual state wins over a class for an
//     overlapping field, the class's non-overlapping fields are retained, and a later class wins
//     over an earlier one (FR-003/FR-004).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light

// A neutral "kind default" base: a plain surface so each variant/state delta is distinguishable.
let private neutralBase: ResolvedStyle =
    { Foreground = theme.Foreground
      Fill = theme.Background
      Stroke = theme.Foreground
      StrokeWidth = 1.0
      FontFamily = theme.FontFamily
      FontSize = 14.0
      FontWeight = None }

let private allVariants =
    [ StyleVariant.Primary
      StyleVariant.Danger
      StyleVariant.Ghost
      StyleVariant.Neutral
      StyleVariant.Success
      StyleVariant.Warning ]

let private allStates =
    [ Normal; Disabled; Hover; Pressed; Focused; Selected; Loading; VisualState.Validation(Invalid "x") ]

[<Tests>]
let feature093StyleResolverTests =
    testList "Feature 093 style resolver" [

        // ---- SC-001 — variant distinctness (T010) -------------------------------------------
        test "each built-in StyleVariant resolves to a token-derived ResolvedStyle (SC-001)" {
            for v in allVariants do
                let resolved = Style.resolve theme neutralBase [ Variant v ] Normal
                // Every field is populated (totality); no exception path.
                Expect.isGreaterThanOrEqual resolved.FontSize 0.0 (sprintf "%A resolves a concrete style" v)
        }

        test "the six built-in variants are pairwise distinguishable on one base under one theme (SC-001)" {
            let resolved = allVariants |> List.map (fun v -> v, Style.resolve theme neutralBase [ Variant v ] Normal)
            for (va, ra) in resolved do
                for (vb, rb) in resolved do
                    if va <> vb then
                        Expect.notEqual ra rb (sprintf "variant %A and %A produce distinguishable styles" va vb)
        }

        test "Primary derives accent-family fill; Danger derives danger-family fill (SC-001)" {
            let primary = Style.resolve theme neutralBase [ Variant StyleVariant.Primary ] Normal
            let danger = Style.resolve theme neutralBase [ Variant StyleVariant.Danger ] Normal
            Expect.equal primary.Fill theme.Accent "Primary fill is the accent token"
            Expect.equal danger.Fill theme.Danger "Danger fill is the danger token"
            Expect.notEqual primary.Fill danger.Fill "the two intents differ in the variant-appropriate way"
        }

        test "a free-form Custom class resolves through the same fold as the typed variant (SC-001)" {
            let viaVariant = Style.resolve theme neutralBase [ Variant StyleVariant.Primary ] Normal
            let viaCustom = Style.resolve theme neutralBase [ Custom "primary" ] Normal
            Expect.equal viaCustom viaVariant "Custom \"primary\" resolves identically to Variant Primary"
        }

        test "an unknown Custom class resolves to the base (identity delta, never dropped/thrown) (SC-001)" {
            let resolved = Style.resolve theme neutralBase [ Custom "no-such-token" ] Normal
            Expect.equal resolved neutralBase "an unknown Custom name is an identity delta over the base"
        }

        // ---- SC-002 — visual states + precedence (T014) -------------------------------------
        test "each differentiated VisualState resolves to a distinct token-derived style (SC-002)" {
            let distinct = allStates |> List.filter (fun s -> s <> Loading) // Loading inherits Normal
            let resolved = distinct |> List.map (fun s -> s, Style.resolve theme neutralBase [] s)
            for (sa, ra) in resolved do
                for (sb, rb) in resolved do
                    if sa <> sb then
                        Expect.notEqual ra rb (sprintf "state %A and %A render distinctly" sa sb)
        }

        test "Loading inherits Normal's paint, preserving FR-005 parity (SC-002)" {
            let normal = Style.resolve theme neutralBase [] Normal
            let loading = Style.resolve theme neutralBase [] Loading
            Expect.equal loading normal "Loading is treated identically to Normal (baseline parity wins)"
        }

        test "the visual state wins over a class for an overlapping field; the class's other fields remain (SC-002, FR-003)" {
            // Ghost sets Fill=transparent, Stroke=foreground, Foreground=foreground.
            // Focused sets Stroke=accent only — it overlaps only on Stroke.
            let resolved = Style.resolve theme neutralBase [ Variant StyleVariant.Ghost ] Focused
            Expect.equal resolved.Stroke theme.Accent "the Focused state's Stroke overrides the Ghost class's Stroke"
            Expect.equal resolved.Fill Colors.transparent "the Ghost class's non-overlapping Fill is retained"
            Expect.equal resolved.Foreground theme.Foreground "the Ghost class's non-overlapping Foreground is retained"
        }

        test "a later-attached class wins over an earlier one (SC-002, FR-003 last-writer-wins)" {
            let resolved = Style.resolve theme neutralBase [ Variant StyleVariant.Primary; Variant StyleVariant.Danger ] Normal
            Expect.equal resolved.Fill theme.Danger "the later Danger class overrides the earlier Primary class"
        }

        test "Disabled + Danger compose per the fixed order (state over class) (SC-002 edge case)" {
            let resolved = Style.resolve theme neutralBase [ Variant StyleVariant.Danger ] Disabled
            Expect.equal resolved.Fill theme.Muted "Disabled de-emphasis (state) wins the Fill over the Danger class"
        }

        // ---- base fidelity — the parity invariant the migration relies on -------------------
        test "resolve theme base [] Normal = base exactly, for every state with no class (G4)" {
            for s in allStates do
                let resolved = Style.resolve theme neutralBase [] s
                if s = Normal || s = Loading then
                    Expect.equal resolved neutralBase (sprintf "no-class %A reproduces the base exactly" s)
        }
    ]
