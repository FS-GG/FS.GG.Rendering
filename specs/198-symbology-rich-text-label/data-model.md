# Phase 1 Data Model: Symbology Rich-Text Label Runs

**Feature**: 198-symbology-rich-text-label | **Date**: 2026-06-25

The feature adds **two public types** and **retypes one existing field**. All other symbology types are
unchanged. Nothing here is persisted — every value is part of the returned `Scene`.

---

## 1. `LabelRun` (new public record)

```fsharp
/// One styled span of identity-label text. Inspection-detail; rendered tofu-free at the render edge.
type LabelRun =
    { Text: string          // the run's text (may contain \n hard breaks)
      Color: Color option   // None ⇒ default labelInk (spec-196 ink)
      Weight: int option    // None ⇒ default weight; maps onto FontSpec.Weight
      Scale: float option }  // None ⇒ 1.0; multiplies the grammar's base label size
```

| Field | Meaning | Default (when `None`) | Validation / rules |
|---|---|---|---|
| `Text` | the span's characters | — | empty/whitespace runs drop (FR-007); `\n`/`\r\n` are hard breaks (R2) |
| `Color` | per-run fill colour | `labelInk` (`rgb 235 235 235`) | author-supplied from `Scene.Color`; not re-mapped/rejected (FR-013) |
| `Weight` | per-run font weight | unset (`FontSpec.Weight = None`) | passed straight to `FontSpec.Weight : int option` |
| `Scale` | per-run size multiplier | `1.0` | resolved size = `max 1.0 (baseSize * scale)` |

**Default-styled run** ⇔ `Color = None && Weight = None && (Scale = None || Scale = Some 1.0)`. An
all-default run set renders **byte-identically** to the equivalent plain label (FR-002, R6).

---

## 2. `LabelText` (new public DU)

```fsharp
[<RequireQualifiedAccess>]
type LabelText =
    | Plain of string          // unstyled; single- or multi-line via \n — the spec-197 channel verbatim
    | Rich of LabelRun list    // ordered styled runs
```

- `[<RequireQualifiedAccess>]` matches the existing `Grammar` convention (`Grammar.Token`); written `LabelText.Plain` / `LabelText.Rich`.
- `LabelText.Plain s` routes through the **unchanged** spec-197 `wrapLabel`/`labelNodes` path (structural zero-drift, R6).
- `LabelText.Rich []`, or `Rich` of all-empty/whitespace runs ⇒ no label (FR-007).

**State transitions**: none — `LabelText`/`LabelRun` are immutable value descriptions, mapped purely to a `Scene`.

---

## 3. `Token` (existing record — one field retyped)

```fsharp
type Token =
    { …                       // all other channels UNCHANGED
      Label: LabelText option }   // was: string option
```

- `Label = None` (the `defaultToken` value) ⇒ no label, byte-identical to the pre-feature symbol (FR-002).
- Migration: every existing `Label = Some "X"` becomes `Label = Some (LabelText.Plain "X")` — value-preserving (R9).
- `Token` purity contract is unchanged: equal `Token` ⇒ equal `Scene` ⇒ equal canonical bytes (under a fixed measurement provider, FR-009/SC-004).

---

## 4. Convenience constructors (new, in the `Symbology` module)

| Constructor | Signature | Purpose |
|---|---|---|
| `plainLabel` | `string -> LabelText` | `= LabelText.Plain`; ergonomic plain label |
| `run` | `string -> LabelRun` | a default-styled run (`{ Text = s; Color = None; Weight = None; Scale = None }`) |
| `richLabel` | `LabelRun list -> LabelText` | `= LabelText.Rich` |

Authors style by record-copying a `run`: `{ Symbology.run "BRAVO-6" with Weight = Some 700; Color = Some teamBlue }`.
(Exact ctor set is a curated-surface detail finalised in the `.fsi`; the contract is FR-001/FR-003.)

---

## 5. Dispatch & layout pipeline (internal — design-loop detail, not contract)

```
Token.Label : LabelText option
   │
   ├─ None ─────────────────────────────────▶ []                         (byte-identical to pre-feature)
   ├─ Some (Plain s) ───────────────────────▶ wrapLabel→labelNodes s     (spec-197 verbatim)
   └─ Some (Rich runs)
        ├─ all runs default-styled ──join──▶ Plain path                 (byte-identical to plain, FR-002)
        └─ any styled run ─────────────────▶ richLabelNodes:
              atomise(runs)  → words carry resolved style (R4)
              greedy inline break to regionWidth (R2)
              cap to per-grammar budget + ellipsis last kept line (R3)
              per line: height = max-run height, common baseline (R5)
              centre by total measured width; emit 1 glyphRunProof / contiguous segment (R5)
              first line @ spec-197 baseline, stack downward
```

### Per-grammar regions & budgets (reused from 197 — provisional geometry, contract is FR-004)

| Grammar | base size | region width | first-line baseline | line budget |
|---|---|---|---|---|
| Token | `R * 0.5` | `R * 1.9` | `Cy + R * 1.5` | ≤ 3 |
| Badge | `R * 0.42` | `R * 1.7` | `Cy + R * 1.42` | ≤ 2 |
| Ring | `R * 0.34` | `R * 1.05` | `Cy + R * 0.52` | ≤ 2 |

Coordinates and budgets are a **design-loop detail** (tunable in the eyeball loop); the **contract** is
FR-004 (sited, observable, non-overlapping) + FR-006 (capped, fitted, no clip/overflow).

---

## 6. Contract vs. design-loop split

| Binding contract (spec FR / SC) | Design-loop detail (tunable, not a contract) |
|---|---|
| One channel, one mapping, no second field (FR-001) | exact constructor set / record field order |
| Opt-in zero-drift: None ≡ pre-feature; Plain ≡ 197; all-default Rich ≡ Plain (FR-002/SC-003) | how all-default runs are joined |
| Attributes = colour/weight/size, each optional w/ default (FR-003) | default ink value, weight ints used in samples |
| Sited, observable, non-overlapping per grammar (FR-004) | exact region rect / baseline / budget numbers |
| Tofu-free per run at the render edge (FR-005) | which sample fonts/weights the loop exercises |
| Fitted per run, capped, no clip/overflow, max-height lines (FR-006/SC-005) | shrink floor (`0.62`), ellipsis glyph |
| Empty/whitespace/empty-run ⇒ no label, no throw (FR-007) | blank-collapse normalisation order |
| `R <= 0` ⇒ placeholder, no throw (FR-008) | placeholder geometry (unchanged) |
| Deterministic under fixed provider (FR-009/SC-004) | — |
| Pure lib never requires a measurer (FR-010) | — |
| Boards/motion unchanged signatures (FR-011) | — |
| Linter grammar-independent, label inspection-detail (FR-012/SC-006) | — |
| Colours author-supplied, guidance-governed (FR-013) | skill wording of the colour caveat |
| Symbology surface baseline regenerated, zero drift elsewhere (FR-015) | — |
| Skill documents rich-text, passes parity (FR-017/SC-007) | exact prose |
