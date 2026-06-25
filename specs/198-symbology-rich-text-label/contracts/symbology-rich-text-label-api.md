# Contract: Symbology Rich-Text Label Runs

**Feature**: 198-symbology-rich-text-label | **Date**: 2026-06-25

The public-surface delta and the observable behaviour the implementation MUST honour. Unlike spec 197
(zero surface delta), this feature **does** change `Symbology.fsi`; the symbology surface baseline is
regenerated (FR-015) with zero drift on every other baseline.

---

## A. Public surface delta (`src/Symbology/Symbology.fsi`)

**Added types**
```fsharp
type LabelRun =
    { Text: string
      Color: Color option
      Weight: int option
      Scale: float option }

[<RequireQualifiedAccess>]
type LabelText =
    | Plain of string
    | Rich of LabelRun list
```

**Changed field** (on the existing `Token` record)
```fsharp
//  Label: string option        // before (spec 196/197)
    Label: LabelText option      // after  (this feature)
```

**Added convenience constructors** (in `module Symbology`)
```fsharp
val plainLabel: string -> LabelText
val run:        string -> LabelRun
val richLabel:  LabelRun list -> LabelText
```

**Unchanged** (signatures byte-stable): `defaultToken`, `token`, `animate`, `gallery`, `filmstrip`,
`badge`, `ring`, `render`, `galleryIn`, `filmstripIn`, `animateIn`, and the `Faction`/`Klass`/`Sigil`/
`TokenState`/`Motion`/`Grammar` types. The board/motion entry points thread the whole `Token`, so they
carry styled labels with **no signature change** (FR-011).

---

## B. Behavioural contract

| # | Given | Then | Spec |
|---|---|---|---|
| B1 | `Label = None` | scene byte-identical to the pre-feature symbol (no label node) | FR-002 / SC-003 |
| B2 | `Label = Some (LabelText.Plain s)` | scene byte-identical to the spec-197 render of `s` (single- or multi-line) | FR-002 / SC-003 |
| B3 | `Label = Some (LabelText.Rich runs)` with all runs default-styled | scene byte-identical to the equivalent `Plain` label | FR-002 / SC-003 |
| B4 | a `Rich` label with ≥1 non-default run | each run drawn in its own colour/weight/size, real glyphs (tofu-free at the render edge), reading order, within the region | FR-001 / FR-003 / FR-005 |
| B5 | two `Token`s, same characters, different run styling | observably different canonical bytes (style is a channel) | FR-001 / SC-002 |
| B6 | a `Rich` label wider/taller/more numerous than the region | per-run-fitted (wrap/shrink/truncate), line count capped, surplus ellipsised, no mid-glyph clip, no overflow into other channels | FR-006 / SC-005 |
| B7 | a line mixing run sizes/weights | line height = tallest run; runs share a common baseline; no vertical overlap | FR-006 |
| B8 | `Label = Some (LabelText.Rich [])`, or `Rich` of all-empty/whitespace runs, or `Plain ""`/whitespace | no label node, no exception | FR-007 |
| B9 | `R <= 0` with any label | visible placeholder, no exception (placeholder wins) | FR-008 |
| B10 | same `Rich`-labelled `Token` rendered twice (in- and cross-process) under a fixed provider | byte-identical scene | FR-009 / SC-004 |
| B11 | no real measurer installed (pure fallback) | deterministic scene with per-run text nodes; no throw | FR-010 |
| B12 | `galleryIn`/`filmstripIn`/`render`/`animateIn` over a styled roster | every unit's runs drawn in the selected grammar; board byte-reproducible | FR-011 |
| B13 | `Legibility.score`/`scoreAnimated` on a roster with vs without styled labels | identical, grammar-independent verdict; pre-attentive governance unchanged | FR-012 / SC-006 |
| B14 | a run with an author-supplied `Color` | colour used as-is; never re-mapped or rejected at runtime | FR-013 |

---

## C. Determinism & purity

- Equal `Token` ⇒ equal `Scene` ⇒ equal `SceneCodec.export(...).CanonicalBytes`, in- and cross-process,
  **under a fixed measurement provider** (FR-009/SC-004). No wall-clock, randomness, or IO.
- The pure library neither installs nor requires a real measurer and never throws without one (FR-010).
  Tofu-free *rendering* and measured per-run wrapping are render-edge properties (FR-005).

---

## D. Surface / governance

- The **symbology** surface baseline under `readiness/surface-baselines/` is regenerated to capture
  `LabelRun`, `LabelText`, the retyped `Token.Label`, and the new constructors; **every other** package
  baseline is unchanged (FR-015). Constitution II: all new public types are declared in the `.fsi`.
- The `fs-gg-symbology` skill documents rich-text runs and passes the skill-parity check
  (`critical=0 high=0`, SC-007).

---

## E. Out of scope (rejected at the contract boundary — FR-018)

Full rich-text layout (justification/alignment beyond region siting, inline images, hyperlinks, lists);
attributes beyond colour/weight/size (italic, underline, strike, letter-/word-spacing, per-glyph
styling, per-run font family); auto-generating/styling labels from stats; label-bound motion / animating
runs; advanced bidi/complex-script typography beyond the installed measurer; any new GPU/compute path;
shipping new font files.
