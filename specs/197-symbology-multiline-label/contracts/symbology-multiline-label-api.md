# Contract — Symbology Multi-line / Paragraph Label

**Feature**: [spec.md](../spec.md) | **Plan**: [plan.md](../plan.md)

This feature adds **no public API surface**. The contract is the **behaviour** of the existing
`FS.GG.UI.Symbology` public surface when `Token.Label` carries multi-line content. The `.fsi` is
unchanged; this document is the behavioural contract the semantic tests assert through that surface.

## Public surface (UNCHANGED — for reference)

```fsharp
// src/Symbology/Symbology.fsi — NO EDIT
type Token = { /* … existing fields … */ Label: string option }

val token:   token: Token -> Scene
val badge:   token: Token -> Scene
val ring:    token: Token -> Scene
val render:  grammar: Grammar -> token: Token -> Scene
val gallery / galleryIn / filmstrip / filmstripIn / animate / animateIn   // signatures UNCHANGED
```

`Token.Label` is interpreted as **possibly multi-line**: embedded `\n` (and `\r\n`) are hard line
breaks; a long line soft-wraps to the grammar's label-region width. No new val, no new field, no new
per-grammar mapping (FR-001).

## Behaviour contract

| # | Given a `Token` (R > 0) rendered via `token`/`badge`/`ring`/`render` | Then | Req |
|---|---|---|---|
| C1 | `Label = None` | scene byte-identical to the pre-feature symbol | FR-002, SC-003 |
| C2 | `Label = Some s`, `s.Trim()` is one line that fits the region at base size (e.g. `"HMR-7"`) | scene byte-identical to the spec-196 single-line render (one glyph-run node, same baseline) | FR-002, SC-003 |
| C3 | `Label = Some s` with internal whitespace, wider than the region | wrapped to multiple stacked lines, each ≤ region width, first line at the 196 baseline | FR-003, FR-005 |
| C4 | `Label = Some s` with embedded `\n` | split into stacked lines (then each soft-wrapped/fitted) | FR-001, FR-003 |
| C5 | wrapped lines exceed the grammar's budget | drawn count capped to budget; last drawn line ends with `…` | FR-005, SC-005 |
| C6 | any drawn line | measured width ≤ region width; not clipped mid-glyph | FR-005, SC-005 |
| C7 | `Label = Some ""` / whitespace / blank-lines-only (`"\n  \n"`) | no label node; equivalent to `None`; no throw | FR-006 |
| C8 | a single unbroken word wider than the region (no whitespace) | shrinks/ellipsis-truncates on one line (no wrap point) — never overflows | FR-005 |
| C9 | same multi-line `Token`, rendered twice (same/separate process), fixed measurement provider | byte-identical scene | FR-008, SC-004 |
| C10 | no measurer installed (pure path) | line nodes still emitted deterministically; no throw | FR-009 |
| C11 | `R ≤ 0` with any `Label` | existing visible placeholder; no label; no throw | FR-007, SC-005 |

| # | Render edge (`Symbology.Render.toPng`, real measurer installed) | Then | Req |
|---|---|---|---|
| C12 | a multi-line labelled `Token` rasterised | **every** line's glyph run is non-tofu (`Missing = false` / `TofuCount = 0`); output non-blank | FR-004, SC-002 |

| # | Boards & governance | Then | Req |
|---|---|---|---|
| C13 | `galleryIn g cols spacing roster` / `filmstripIn` with a multi-line roster | reproducible per grammar under a fixed provider; no signature change | FR-010, SC-001 |
| C14 | `Legibility.score` / `scoreAnimated` on a roster, with vs without labels | `Report` identical; grammar-independent | FR-011, SC-006 |

| # | Surface / governance | Then | Req |
|---|---|---|---|
| C15 | surface baselines | unchanged (no public surface added); recorded | FR-013, SC-007 |
| C16 | `fs-gg-symbology` skill | documents multi-line as opt-in inspection-detail; parity check 0 critical/0 high | FR-015, SC-007 |

## Anti-contract (out of scope — FR-016)

Per-run rich-text styling; auto-label-from-stats; label-bound motion; justified text / advanced
bidi/complex-script layout beyond what the installed measurer already supports; any new GPU/compute
path; shipping new font files.
