# Phase 1 Data Model: Symbology Full Rich-Text Layout

**Feature**: 199-rich-text-layout | **Date**: 2026-06-26

The feature **extends one existing record**, **adds two public types**, and **adds one DU case**. All other
symbology types are unchanged. Nothing here is persisted — every value is part of the returned `Scene`.

---

## 1. `LabelRun` (existing record — four optional fields added)

```fsharp
type LabelRun =
    { Text: string
      Color: Color option      // 198 — None ⇒ default labelInk
      Weight: int option       // 198 — None ⇒ default weight (FontSpec.Weight)
      Scale: float option      // 198 — None ⇒ 1.0 (× grammar base size)
      Italic: bool option      // NEW — None ⇒ upright (no synthetic slant)
      Underline: bool option   // NEW — None ⇒ no underline
      Strike: bool option      // NEW — None ⇒ no strike-through
      Tracking: float option }  // NEW — None ⇒ 0.0 letter-spacing (em-fraction of resolved size)
```

| New field | Meaning | Default (`None`) | Realisation (pure scene) |
|---|---|---|---|
| `Italic` | synthetic slant | upright | baseline-pivoted shear via `Scene.withPerspective` (research R3) |
| `Underline` | rule below baseline | none | `Scene.line` per drawn fragment, fitted extent (R4) |
| `Strike` | rule at mid-x-height | none | `Scene.line` per drawn fragment, fitted extent (R4) |
| `Tracking` | letter-spacing (em-fraction) | `0.0` | folded into measurement + per-char `glyphRunProof` advance (R6) |

**Default-styled run** (widened from 198) ⇔ `Color = None && Weight = None && (Scale = None || Scale = Some 1.0)
&& Italic ∈ {None; Some false} && Underline ∈ {None; Some false} && Strike ∈ {None; Some false}
&& (Tracking = None || Tracking = Some 0.0)`. An all-default run renders **byte-identically** to the spec-198
run (and through 198's chain, to the 197/196/pre-feature symbol) — FR-004, R7.

---

## 2. `LabelAlign` (new public DU)

```fsharp
type LabelAlign =
    | Leading
    | Center      // DEFAULT — reproduces the spec-198 flow byte-for-byte
    | Trailing
    | Justify
```

- Placement within the per-grammar region span (`centerX ± regionWidth/2`): `Leading` → left, `Center` →
  `centerX - total/2` (the 198 computation verbatim), `Trailing` → `right - total`, `Justify` → space
  distributed across inter-word gaps (R5).
- `Justify` leaves the **last line of each paragraph** and any **single-token line** un-justified, falling
  back to the paragraph's base alignment (FR-008).

---

## 3. `LabelParagraph` (new public record)

```fsharp
type LabelParagraph =
    { Runs: LabelRun list
      Align: LabelAlign }
```

- One paragraph = an ordered run list + one alignment. Hard line breaks **within** a paragraph use the
  existing `\n`/`\r\n` handling in the runs' `Text`; **paragraph** breaks are the list boundaries (each
  paragraph starts a new line; paragraphs may differ in alignment — FR-002).
- An empty / all-whitespace / all-empty-run paragraph contributes no line (FR-009).

---

## 4. `LabelText` (existing DU — one case added)

```fsharp
[<RequireQualifiedAccess>]
type LabelText =
    | Plain of string              // 197 — unstyled, single- or multi-line via \n
    | Rich of LabelRun list        // 198 — ordered styled runs (now also decoration/slant/tracking-capable)
    | Laid of LabelParagraph list  // NEW — explicit paragraphs, each alignable
```

- `Plain`/`Rich` signatures are **byte-stable** (no retype).
- `Laid []`, or `Laid` of all-empty paragraphs ⇒ no label (FR-009).
- `Laid [{ Runs; Align = Center }]` with all-default runs reduces to the `Rich`/`Plain` path ⇒ byte-identical
  to 198 (FR-004, R7).

**State transitions**: none — every type is an immutable value description mapped purely to a `Scene`.

---

## 5. `Token` (existing record — unchanged this feature)

`Token.Label : LabelText option` (retyped in 198) is unchanged in shape; it now admits the new `Laid` case
and the enriched runs. `Label = None` ⇒ no label, byte-identical to the pre-feature symbol. The `Token`
purity contract is unchanged: equal `Token` ⇒ equal `Scene` ⇒ equal canonical bytes (under a fixed
measurement provider, FR-011/SC-004).

---

## 6. Convenience constructors (new, in the `Symbology` module)

| Constructor | Signature | Purpose |
|---|---|---|
| `paragraph` | `LabelRun list -> LabelParagraph` | a `Center`-aligned paragraph (`{ Runs = runs; Align = Center }`) |
| `align` | `LabelAlign -> LabelRun list -> LabelParagraph` | a paragraph with an explicit alignment |
| `laidLabel` | `LabelParagraph list -> LabelText` | `= LabelText.Laid` |

Existing `plainLabel`/`run`/`richLabel` (198) are unchanged. Decoration/slant/tracking are set by
record-copy on a `run`, e.g. `{ Symbology.run "quoted" with Italic = Some true; Underline = Some true }`.
(Exact ctor set is a curated-surface detail finalised in the `.fsi`; the contract is FR-001/FR-003.)

---

## 7. Dispatch & layout pipeline (internal — design-loop detail, not contract)

```
Token.Label : LabelText option
   │
   ├─ None ─────────────────────────────────▶ []                          (byte-identical to pre-feature)
   ├─ Some (Plain s) ───────────────────────▶ wrapLabel→labelNodes s      (spec-197 verbatim)
   ├─ Some (Rich runs)
   │     ├─ all runs default ──────join────▶ Plain path                  (byte-identical to plain)
   │     └─ any styled run ────────────────▶ richLabelNodes (decoration/slant/tracking-aware):
   │           atomise (tracking-aware) → greedy break → cap+ellipsis →
   │           per line max-height + common baseline → centred per-segment emit:
   │             tracking ⇒ per-char glyphRunProof advance (else single node)
   │             italic   ⇒ wrap node in withPerspective baseline shear
   │             under/strike ⇒ append Scene.line over the fitted fragment
   └─ Some (Laid paras)
         ├─ single Center para, all-default ─reduce─▶ Rich/Plain path     (byte-identical to 198)
         └─ else ────────────────────────────────▶ laidLabelNodes:
               for each paragraph (sharing the per-grammar line budget):
                 atomise (tracking-aware) → greedy break → per-line max-height
                 alignPlace per line by paragraph.Align (R5):
                   Leading/Center/Trailing → startX; Justify → distribute gaps
                   (last paragraph line + single-token line ⇒ base-align fallback)
                 emit per-segment (same decoration/slant/tracking as richLabelNodes)
               cap total drawn lines to the per-grammar budget + ellipsis last kept line
               first line of first paragraph @ spec-197/198 baseline, stack downward
```

### Per-grammar regions & budgets (reused from 197/198 — provisional geometry, contract is FR-005)

| Grammar | base size | region width | first-line baseline | line budget (shared across paragraphs) |
|---|---|---|---|---|
| Token | `R * 0.5` | `R * 1.9` | `Cy + R * 1.5` | ≤ 3 |
| Badge | `R * 0.42` | `R * 1.7` | `Cy + R * 1.42` | ≤ 2 |
| Ring | `R * 0.34` | `R * 1.05` | `Cy + R * 0.52` | ≤ 2 |

Coordinates and budgets are a **design-loop detail** (tunable in the eyeball loop); the **contract** is FR-005
(sited, observable, non-overlapping) + FR-007 (capped, fitted, no clip/overflow, under every alignment).

---

## 8. Contract vs. design-loop split

| Binding contract (spec FR / SC) | Design-loop detail (tunable, not a contract) |
|---|---|
| One channel, one mapping, no second field (FR-001) | exact constructor set / record field order |
| Explicit paragraph + per-paragraph alignment (FR-001/FR-002) | how paragraphs reduce to the 198 path |
| Run attrs italic/underline/strike/tracking, each optional w/ default (FR-003) | slant constant (~0.21), decoration thickness/offsets, tracking em-fraction units |
| Layered zero-drift: None ≡ pre-feature; Plain ≡ 197; all-default Rich ≡ Plain; default Center single-para ≡ 198 (FR-004/SC-003) | how all-default runs/paragraphs are collapsed |
| Sited, observable, non-overlapping per grammar (FR-005) | exact region rect / baseline / budget numbers |
| Tofu-free per run at the render edge incl. slant/decoration/tracking (FR-006) | which sample fonts/weights/attrs the loop exercises |
| Fitted per run, capped, no clip/overflow, max-height lines, decoration ≤ fitted extent, last paragraph line un-justified (FR-007/FR-008/SC-005) | shrink floor (`0.62`), ellipsis glyph, justify-gap rounding |
| Justify by measured widths, no glyph stretch, single-token fallback (FR-007/FR-008) | base-alignment used for the fallback (Leading) |
| Empty/whitespace/empty-run/empty-paragraph ⇒ no label, no throw (FR-009) | blank-collapse normalisation order |
| `R <= 0` ⇒ placeholder, no throw (FR-010) | placeholder geometry (unchanged) |
| Deterministic under fixed provider (FR-011/SC-004) | — |
| Pure lib never requires a measurer; no new primitive/font/GPU (FR-012/FR-018/FR-019) | — |
| Boards/motion unchanged signatures (FR-013) | — |
| Linter grammar-independent, label inspection-detail (FR-014/SC-006) | — |
| Alignment/decoration/colours author-supplied, guidance-governed (FR-015) | skill wording of the caveats |
| Symbology surface baseline regenerated, zero drift elsewhere (FR-017) | — |
| Skill documents full rich-text layout, passes parity (FR-020/SC-007) | exact prose |
