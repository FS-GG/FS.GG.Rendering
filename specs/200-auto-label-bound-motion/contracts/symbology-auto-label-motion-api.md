# Contract: Symbology Auto-Label & Label-Bound Motion API

**Package**: `FS.GG.UI.Symbology` (`src/Symbology/Symbology.fsi`) · **Tier 1** (adds public surface; alters label-channel behaviour) · **Spec**: [spec.md](../spec.md) · **Plan**: [plan.md](../plan.md)

This contract is the curated `.fsi` delta plus the behaviour table the semantic tests and surface baseline enforce. Everything not listed here is **byte-stable** from spec 199 — in particular **every existing type and every board/motion entry-point signature is unchanged** (FR-005/FR-017).

## 1. Surface delta (`.fsi`)

### New public types

```fsharp
/// Channel selectors a `Token`'s auto-label projection may read. Each reads ONLY the named
/// encoded channel (never a game's raw stats — FR-002) and renders a fixed, game-agnostic code.
type AutoField =
    | FactionCode   // Ally->"ALY" | Enemy->"ENY" | Neutral->"NEU" | Custom _ ->"CUS"
    | KlassCode     // Mobile->"MOB" | Heavy->"HVY" | Scout->"SCT"
    | StateCode     // Confirmed->"CFM" | Suspected->"SUS"
    | HealthTier    // round(Health*100) -> "H"+nn
    | ThreatTier    // bucket Threat [0,1] -> "T0".."T4"
    | SpeedPips     // Speed (0..4) -> "S0".."S4"
    | ShieldFlag    // Shield=true -> "SHD"; false -> contributes nothing

/// An opt-in auto-label projection request (FR-001). Pure, deterministic function of the
/// `Token`'s channels; overridable by an explicit `Label` (FR-003); yields no label when it
/// projects to no drawable glyphs (FR-004).
type AutoLabelSpec =
    { Fields: AutoField list   // ordered; [] -> no label
      Separator: string }      // joins rendered field codes

/// An opt-in binding of the resolved label to the existing motion timeline (FR-005). Sampled as
/// a deterministic function of the motion phase; byte-identical to the static label at the rest
/// phase (FR-007); fitted at every phase (FR-011).
type LabelMotion =
    | TypeOn   // whole-glyph prefix reveal; rest = fully revealed
    | Fade     // run alpha ramp; rest = full alpha
    | Pulse    // size/alpha oscillation; rest = unscaled
    | Scroll   // overflow ticker within the region; rest = offset 0
```

### Extended `Token` (two `None`-defaulted fields)

```fsharp
type Token =
    { …                               // all existing fields byte-stable
      Label: LabelText option         // EXISTING — explicit label; WINS over AutoLabel
      AutoLabel: AutoLabelSpec option  // NEW — None = off (default)
      LabelMotion: LabelMotion option } // NEW — None = off (default)
```

### New constructors (in `module Symbology`)

```fsharp
val autoLabel:    fields: AutoField list -> AutoLabelSpec          // Separator = " "
val autoLabelSep: separator: string -> fields: AutoField list -> AutoLabelSpec
val labelMotion:  kind: LabelMotion -> LabelMotion
```

### Unchanged (explicitly byte-stable)

`Faction` · `Klass` · `Sigil` · `TokenState` · `Motion` · `LabelRun` · `LabelAlign` · `LabelParagraph` · `LabelText` · `Grammar` · `defaultToken` (value gains two `None` fields, signature unchanged) · `plainLabel` · `run` · `richLabel` · `paragraph` · `align` · `laidLabel` · `token` · `animate` · `gallery` · `filmstrip` · `badge` · `ring` · `render` · `galleryIn` · `filmstripIn` · `animateIn`.

## 2. Behaviour contract

| # | Given | Then |
|---|---|---|
| C1 | `Token` with `AutoLabel = Some spec`, `Label = None` | the projected label (from `spec.Fields` over the `Token`'s channels) is drawn via the existing dispatch, fitted, tofu-free at the render edge |
| C2 | `Token` with `AutoLabel = Some _` **and** `Label = Some _` | the **explicit** `Label` is drawn; the projection is ignored; exactly one resolved label (FR-003) |
| C3 | two `Token`s differing in one channel a selected `AutoField` reads | their auto-labels differ; two `Token`s with identical channels ⇒ byte-identical auto-labels (FR-004) |
| C4 | `AutoLabel` whose `Fields = []` or projects to empty/whitespace | no label (no text node), no exception (FR-004/FR-012) |
| C5 | `Token` with `AutoLabel = None`, `LabelMotion = None` | byte-identical to spec 199 in every grammar (and through 199's chain to 198/197/pre-feature) (FR-008) |
| C6 | `Token` with `LabelMotion = Some kind`, sampled at the rest phase (`animate`/`filmstrip` phase ⇒ `ph = 0.0`) | byte-identical to the equivalent static spec-199 label (FR-007) |
| C7 | `Token` with `LabelMotion = Some kind`, non-rest phase | the label's drawn state advances with the phase; other channels unaffected; tofu-free + fitted (FR-006/FR-010/FR-011) |
| C8 | `LabelMotion = Some Scroll` over content longer than the region | the line scrolls within the region across phases; no mid-glyph clip; no overflow into adjacent channels; line count stays capped (FR-011) |
| C9 | same `(Token, phase)` rendered twice (in-/cross-process) under the same measurer | byte-identical scene (FR-006/FR-015) |
| C10 | `Token` with **both** `AutoLabel` and `LabelMotion` | projection resolves first, then the resolved label animates per phase; deterministic, fitted, tofu-free (FR-013) |
| C11 | degenerate `Token` (`R <= 0`) with an auto / motion label | visible placeholder; auto/motion suppressed; never throws (FR-014) |
| C12 | any auto/motion label, pure-fallback path (no measurer installed) | scene still produced deterministically (resolved label's styled nodes + recorded phase); no throw (FR-016) |
| C13 | a roster scored by the spec-194 legibility linter, with vs without auto/motion labels | identical, grammar-independent verdict; pre-attentive governance unchanged (FR-018) |
| C14 | the package surface baseline | only `FS.GG.UI.Symbology.*` moves (the three types, two `Token` fields, ctors); every other baseline unchanged (FR-020) |

## 3. Out of scope (deferred — FR-021)

Per-game stat → label semantics inside the library (the `'stats -> Token` mapping stays the caller's); inline images; hyperlinks; bullet/numbered lists; per-glyph styling; advanced bidirectional / complex-script typography beyond the installed measurer; any new GPU/compute path; new font files.
