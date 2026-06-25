# Contract: Symbology Full Rich-Text Layout

**Feature**: 199-rich-text-layout | **Date**: 2026-06-26

The public-surface delta and the observable behaviour the implementation MUST honour. Like spec 198 (and
unlike 197), this feature **does** change `Symbology.fsi`; the symbology surface baseline is regenerated
(FR-017) with zero drift on every other baseline.

---

## A. Public surface delta (`src/Symbology/Symbology.fsi`)

**Extended record** (four optional fields added to the existing `LabelRun`)
```fsharp
type LabelRun =
    { Text: string
      Color: Color option
      Weight: int option
      Scale: float option
      Italic: bool option        // NEW — None ⇒ upright
      Underline: bool option     // NEW — None ⇒ no underline
      Strike: bool option        // NEW — None ⇒ no strike-through
      Tracking: float option }    // NEW — None ⇒ 0.0 letter-spacing (em-fraction of resolved size)
```

**Added types**
```fsharp
type LabelAlign =
    | Leading
    | Center        // default — reproduces the spec-198 flow
    | Trailing
    | Justify

type LabelParagraph =
    { Runs: LabelRun list
      Align: LabelAlign }
```

**Added DU case** (on the existing `LabelText`)
```fsharp
[<RequireQualifiedAccess>]
type LabelText =
    | Plain of string              // unchanged (197)
    | Rich of LabelRun list        // unchanged signature (198)
    | Laid of LabelParagraph list  // NEW
```

**Added convenience constructors** (in `module Symbology`)
```fsharp
val paragraph: LabelRun list -> LabelParagraph          // Center-aligned
val align:     LabelAlign -> LabelRun list -> LabelParagraph
val laidLabel: LabelParagraph list -> LabelText
```

**Unchanged** (signatures byte-stable): `plainLabel`, `run`, `richLabel`, `defaultToken`, `token`,
`animate`, `gallery`, `filmstrip`, `badge`, `ring`, `render`, `galleryIn`, `filmstripIn`, `animateIn`, the
`LabelText.Plain`/`LabelText.Rich` cases, and the `Faction`/`Klass`/`Sigil`/`TokenState`/`Motion`/`Grammar`
types. The board/motion entry points thread the whole `Token`, so they carry laid-out / decorated labels
with **no signature change** (FR-013).

---

## B. Behavioural contract

| # | Given | Then | Spec |
|---|---|---|---|
| B1 | `Label = None` | scene byte-identical to the pre-feature symbol | FR-004 / SC-003 |
| B2 | `Label = Some (Plain s)` | scene byte-identical to the spec-197 render of `s` | FR-004 / SC-003 |
| B3 | `Label = Some (Rich runs)`, all runs default (incl. new attrs unset) | scene byte-identical to the equivalent `Plain` label | FR-004 / SC-003 |
| B4 | `Label = Some (Laid [{ Runs; Align = Center }])`, single paragraph, all-default runs | scene byte-identical to the equivalent `Rich`/`Plain` label (default = 198 flow) | FR-004 / SC-003 |
| B5 | a `Rich` run setting `Italic`/`Underline`/`Strike`/`Tracking` | drawn with its slant / decoration / tracking and **real glyphs** (tofu-free at the render edge), in reading order, within the region | FR-003 / FR-006 |
| B6 | two `Token`s, same characters, one run sets a new attribute (e.g. italic) | observably different canonical bytes; neither raises (the attribute is a channel) | FR-003 / SC-002 |
| B7 | a `Laid` paragraph with `Center` / `Leading` / `Trailing` alignment | drawn lines centred / left-sited / right-sited within the per-grammar region; block stays in the footprint | FR-001 / FR-007 |
| B8 | a `Justify` paragraph over content that wraps to ≥2 lines | inter-word space distributed so each wrapped line fills the region width; the **last line of the paragraph** (and any single-token line) left un-justified; no glyph stretched, no mid-glyph clip | FR-007 / FR-008 |
| B9 | a `Laid` label with explicit paragraphs / line breaks | the authored paragraph/line structure is produced (paragraphs may differ in alignment), capped and fitted as in 197/198 | FR-002 / FR-007 |
| B10 | a laid-out / decorated label wider / taller / more numerous than the region | per-run-fitted (wrap/shrink/truncate), drawn-line count capped, surplus ellipsised on the last drawn line, no mid-glyph clip, no overflow into other channels, under **every** alignment; mixed-size lines sized to the tallest run on a common baseline | FR-007 / SC-005 |
| B11 | a run that wraps and sets underline/strike | decoration follows **each** drawn line fragment's fitted geometry; never extends past the region or a clipped glyph | FR-008 |
| B12 | a run with `Tracking` set | letter-spacing is included in the run's measurement (affects wrap/justify/fit) so tracking never pushes the block past the region | FR-007 |
| B13 | `Laid []`, or `Laid` of all-empty/whitespace paragraphs/runs, or `Plain ""`/whitespace, regardless of alignment/decoration | no label node, no exception | FR-009 |
| B14 | `R <= 0` with any laid-out / decorated label | visible placeholder, no exception (placeholder wins) | FR-010 |
| B15 | same laid-out `Token` rendered twice (in- and cross-process) under a fixed provider | byte-identical scene, including justified lines | FR-011 / SC-004 |
| B16 | no real measurer installed (pure fallback) | deterministic scene with per-run styled text nodes and the recorded alignment/decoration; no throw | FR-012 |
| B17 | `galleryIn`/`filmstripIn`/`render`/`animateIn` over a laid-out roster | every unit's label drawn with its alignment/decoration in the selected grammar; board byte-reproducible | FR-013 |
| B18 | `Legibility.score`/`scoreAnimated` on a roster with vs without laid-out / decorated labels | identical, grammar-independent verdict; pre-attentive governance unchanged | FR-014 / SC-006 |
| B19 | a run/paragraph with author-supplied `Color` / `Align` / decoration | used as-is; never re-mapped or rejected at runtime | FR-015 |

---

## C. Determinism & purity

- Equal `Token` ⇒ equal `Scene` ⇒ equal `SceneCodec.export(...).CanonicalBytes`, in- and cross-process,
  **under a fixed measurement provider** (FR-011/SC-004). Break, justify, alignment, the slant matrix,
  decoration geometry, and tracked advances are pure functions of the resolved per-run measurement + fixed
  constants. No wall-clock, randomness, or IO.
- The pure library neither installs nor requires a real measurer and never throws without one (FR-012).
  Tofu-free *rendering*, measured *wrapping*, and measured *justification* are render-edge properties (FR-006).
- **No new scene primitive, no new font file, no GPU/compute path** (FR-018/FR-019): slant uses the existing
  `Scene.withPerspective`; underline/strike use the existing `Scene.line`; tracking uses per-glyph
  `Scene.glyphRunProof` positioning — all already in the Scene vocabulary referenced by the grammars.

---

## D. Surface / governance

- The **symbology** surface baseline under `readiness/surface-baselines/` is regenerated to capture the four
  new `LabelRun` fields, `LabelAlign`, `LabelParagraph`, the `LabelText.Laid` case, and the new constructors;
  **every other** package baseline is unchanged (FR-017). Constitution II: all new public types/fields are
  declared in the `.fsi`.
- The `fs-gg-symbology` skill documents full rich-text layout and passes the skill-parity check
  (`critical=0 high=0`, SC-007).

---

## E. Out of scope (rejected at the contract boundary — FR-019)

Inline images, hyperlinks, bullet / numbered lists, per-glyph styling, per-run font family, automatic
generation or styling of labels from stats without a human in the loop, label-bound motion / animating runs,
advanced bidirectional / complex-script typography beyond what the installed measurer already supports, any
new GPU/compute path, and shipping new font files. Italic/underline/strike are achieved with synthetic
slant + the existing line/glyph primitives, not new font assets.
