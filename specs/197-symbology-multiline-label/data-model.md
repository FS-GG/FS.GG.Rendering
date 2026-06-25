# Phase 1 Data Model — Symbology Multi-line / Paragraph Label Channel

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md)

This feature adds **no new type and no new public field**. It widens the *behaviour* of the existing
`Token.Label : string option`. The "entities" below are the existing type (unchanged) and the internal
layout pipeline (private to `Symbology.fs`).

## Entity: `Token` (existing — UNCHANGED surface)

The fixed channel-set value (`src/Symbology/Symbology.fsi:42`). The relevant field:

| Field | Type | Change | Meaning |
|---|---|---|---|
| `Label` | `string option` | **none (surface unchanged)** | Optional identity text. **Now** interpreted as possibly **multi-line**: embedded `\n` are hard breaks; a long line soft-wraps to the region width. `None`/empty/whitespace ⇒ no label (byte-identical to pre-feature). |

All other fields (`Cx`,`Cy`,`R`,`Faction`,`Klass`,`Sigil`,`Heading`,`Health`,`State`,`Speed`,`Shield`, …)
are unchanged. `defaultToken.Label` stays `None`.

**No `.fsi` edit, no baseline change** (FR-013). The only contract change is **behavioural** (Tier 1):
how a non-empty `Label` containing whitespace/`\n` and exceeding one line is laid out.

## Internal pipeline (private to `Symbology.fs` — NOT public surface)

```
Label (string option)
  └─ raw ──► wrapLabel regionWidth baseSize budget ──► (string list)   // normalise → wrap → cap → ellipsis
                                                          └─ per line ─► fitLabel regionWidth size ──► (string * FontSpec)
                                                                                                          └─► Scene.glyphRunProof  // one node/line
  └─ all line nodes ──► withLabel (Scene list) ──► Scene.group (channelNodes @ lineNodes)
```

### `wrapLabel` (new, private)

| Step | Rule | Requirement |
|---|---|---|
| Guard | `String.IsNullOrWhiteSpace raw` ⇒ `[]` | FR-006 |
| Split | Split on `\r\n`/`\n` into paragraph segments | FR-001 |
| Normalise | Trim each segment; **drop** empty/whitespace segments | FR-006 |
| Wrap | Greedy **whitespace** word-wrap each segment to `regionWidth` (measured at `baseSize`); never break inside a word | FR-005, R2 |
| Cap | Keep at most `budget` lines | FR-005 (bounded line count) |
| Ellipsis | If any content dropped (over budget, or last line itself truncated), the **last kept line** ends with `…` | FR-005, SC-005 |

Output: an ordered `string list` (length `0 … budget`). Deterministic for a fixed measurement provider
(FR-008).

### `labelNodes` (was `labelNode`, now list-returning, private)

- Input: `centerX`, `baselineY`, `regionWidth`, `baseSize`, `lineHeight`, `budget`, `label : string option`.
- For each wrapped line `i`: run the existing **`fitLabel regionWidth size line`** (shrink→ellipsis,
  guarantees ≤ region, no mid-glyph clip), centre it on `centerX`, baseline at `baselineY + lineHeight*i`,
  emit `Scene.glyphRunProof pos text font (Paint.fill labelInk)`.
- Output: `Scene list` (empty when no drawable line).

### `withLabel` (signature change, private)

- `withLabel (lineNodes: Scene list) (channelNodes: Scene list) : Scene = Scene.group (channelNodes @ lineNodes)`.
- `[]` ⇒ `Scene.group channelNodes` — **byte-identical to no-label** (FR-002/SC-003).
- `[one]` ⇒ `channelNodes @ [one]` — **byte-identical to the spec-196 single-line label** (FR-002/SC-003).

## Per-grammar label region (provisional geometry — design-loop, NOT contract)

First line keeps spec 196's baseline / region width / base size (the **zero-drift anchor**); the budget &
line-height are new. Coordinates are tunable in the loop; the contract is FR-003 (sited, observable,
non-overlapping) + FR-005 (capped, no overflow).

| Grammar | First-line baseline (unchanged from 196) | Region width | Base size | **Line budget** | Stacking |
|---|---|---|---|---|---|
| Token | `Cy + R*1.5` (below the health arc) | `R*1.9` | `R*0.5` | **≤ 3** | downward, `+TextMetrics.Height` |
| Badge | `Cy + R*1.42` (band below health bar/pips) | `R*1.7` | `R*0.42` | **≤ 2** | downward |
| Ring | `Cy + R*0.52` (beneath sigil, inner disc) | `R*1.05` | `R*0.34` | **≤ 2** | downward |

## Validation rules (from requirements)

| Rule | Source |
|---|---|
| No label ⇒ byte-identical to pre-feature symbol | FR-002 / SC-003 |
| One-line-fitting label ⇒ byte-identical to spec-196 single-line label | FR-002 / SC-003 |
| Every drawn line ≤ region width, no mid-glyph clip | FR-005 / SC-005 |
| Drawn line count ≤ per-grammar budget; surplus ⇒ ellipsis on last line | FR-005 / SC-005 |
| Empty / whitespace / blank-lines-only ⇒ no nodes, no throw | FR-006 / SC-005 |
| Degenerate token (`R ≤ 0`) + label ⇒ placeholder, no throw | FR-007 / SC-005 |
| Identical multi-line `Token` under fixed provider ⇒ identical bytes | FR-008 / SC-004 |
| Pure path never installs/requires a measurer, never throws | FR-009 |
| Every line tofu-free under the render-edge measurer | FR-004 / SC-002 |
| Linter `Report` unchanged by label presence; grammar-independent | FR-011 / SC-006 |
| No baseline moves (no public surface added) | FR-013 / SC-007 |

## State transitions

None — the label is a pure `Token -> Scene` projection (no stateful workflow; Constitution IV N/A).
