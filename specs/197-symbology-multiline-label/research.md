# Phase 0 Research — Symbology Multi-line / Paragraph Label Channel

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-06-25

All Technical Context unknowns are resolved below. The spec left field-shape, region geometry, line
budget, and wrap/cap/truncate policy as planning details (Assumptions); each is decided here, grounded
against the spec-196 implementation in `src/Symbology/Symbology.fs` and the Scene text vocabulary.

## R1 — How is multi-line expressed? (field shape)

- **Decision**: Carry multi-line in the **existing `Token.Label : string option`** via **embedded line
  breaks** (`\n`, and `\r\n` normalised to `\n`) plus **soft-wrap** at runtime. No new field, no
  `string list`, no second channel, no `.fsi` change.
- **Rationale**: FR-001 (one shared channel, one mapping), FR-013 (zero surface drift), and Idiomatic
  Simplicity. Existing callers pass `Some "HMR-7"`; reusing the field means those renders stay
  byte-identical (FR-002) and no baseline moves. A `string list` / new `LabelLines` field would add
  public surface *and* break the single-line byte-identity story (callers would have to change shape).
- **Alternatives considered**: (a) `Label : string list` — adds surface, breaks byte-identity, rejected.
  (b) A new `LabelLines : string list option` beside `Label` — two channels for one concept, contradicts
  FR-001, rejected. (c) Newlines-only (no soft-wrap) — fails US2.1 "lines wider than the region wrap",
  rejected in favour of newlines **and** soft-wrap.

## R2 — Wrap policy: where do soft breaks happen?

- **Decision**: **Greedy whitespace word-wrap.** Split each `\n`-delimited paragraph segment on runs of
  whitespace into words; pack words onto a line while the measured `prefix + " " + word` width ≤ region
  width (measured via `Scene.measureTextResolved` at base size); start a new line when the next word
  would overflow. **Do not break inside a word** (no hyphen breaks, no mid-word splits). A single word
  that alone exceeds the region has no wrap point and is handled by the per-line fit (R3).
- **Rationale**: Whitespace-only wrapping is deterministic, simple, and — critically — **preserves every
  spec-196 fixture**: the labelled golden uses `"HMR-7"` (fits one line; `DeterminismTests.fs:130`) and
  the overlong-fit fixture is `"THIS-CALLSIGN-IS-FAR-TOO-LONG-TO-FIT-1234567890"` (no whitespace;
  `LabelTests.fs:67`), so neither wraps and both stay byte-identical. Hyphen/mid-word breaking would
  wrap that fixture and drift the test for no user benefit.
- **Alternatives considered**: (a) break on hyphens too — drifts the existing overlong fixture, rejected.
  (b) character-level wrap (CJK-style) — out of scope (FR-016 advanced typography), the measurer/shaper
  owns complex-script behaviour; rejected. (c) balanced/Knuth-Plass wrap — over-engineered for a few
  short lines, rejected.

## R3 — Per-line fit (overflow / clip guard) and the cap+ellipsis

- **Decision**: **Reuse the existing `fitLabel`** (`Symbology.fs:273`) for **each** emitted line: it
  shrinks the font toward a floor and, if still over at the floor, ellipsis-truncates at a measured glyph
  boundary, guaranteeing the line is ≤ region width and never clipped mid-glyph. After wrapping +
  per-line fit, **cap** the line list to the grammar's line budget (R4); if any content was dropped
  (more wrapped lines than the budget, or a within-budget last line that was itself truncated), the
  **last drawn line ends with the ellipsis** (`…`, already defined `Symbology.fs:261`), re-fit so the
  ellipsised last line is itself ≤ region width.
- **Rationale**: FR-005/SC-005 — never overflow, never clip, bounded line count, graceful surplus. Reuse
  keeps one fit policy (the one already tested) rather than a second implementation. The ellipsis on the
  last line signals "more text exists" the same way the single-line truncation does.
- **Alternatives considered**: shrink the **whole block** to fit more lines — harms legibility unevenly
  across units and is non-local; rejected in favour of fixed base size + cap + ellipsis.

## R4 — Per-grammar line budget & line-height (vertical siting)

- **Decision**: Each grammar keeps spec 196's label **baseline/region width and base size** for the
  **first** line, and gains a **line budget** (max drawn lines) and a **line-height** for stacking
  **downward**. Provisional budgets (design-loop tunable): **Token ≤ 3**, **Badge ≤ 2**, **Ring ≤ 2**
  (the ring's inner disc is the tightest region). Line-height = `TextMetrics.Height` of the line's font
  (from `Scene.measureTextResolved`; `TextMetrics = { Width; Height; Baseline }`, `src/Scene/Types.fsi:181`),
  falling back to `baseSize * 1.15` if `Height ≤ 0`. The block top line sits at spec 196's exact
  baseline; subsequent lines at `+ lineHeight * i`.
- **Rationale**: FR-003 (sited, non-overlapping) + FR-002 (first-line baseline is the zero-drift anchor —
  one fitting line reproduces spec 196 exactly). Budgets are provisional geometry; the **contract** is
  "capped, observable, no overflow into the sigil/health/other channels", verified by the per-line
  ≤ region assertion and the eyeball loop, not by pinned coordinates.
- **Alternatives considered**: center the block vertically on the 196 baseline — a one-line block then
  shifts by half a line-height vs 196 ⇒ **byte-drift**; rejected. Expand the footprint to fit more lines
  — changes board layout/goldens; rejected (cap instead).

## R5 — Zero-drift mechanism (the emit shape)

- **Decision**: The label render returns a **`Scene list`** of glyph-run nodes (0..budget), and
  `withLabel` **appends** them to the grammar's child list: `Scene.group (nodes @ lineNodes)`. Empty list
  ⇒ `Scene.group nodes` (byte-identical to no-label); a single fitting line ⇒ `nodes @ [glyphRunProof …]`
  with the **same position/text/font/paint** as spec 196 ⇒ byte-identical.
- **Rationale**: spec 196 appends its single node directly (not inside an extra `group`), so to stay
  byte-identical the multi-line path must also append **bare sibling nodes**, never a wrapping `group`.
  Returning a list (vs `Scene option`) makes both the 0-line and 1-line cases reduce to the exact 196
  child list. Verified against `0dda10bd…` (no label), `6710215b…` (`"HMR-7"`, one line), and the
  gallery/filmstrip/badge/ring goldens.
- **Alternatives considered**: wrap lines in `Scene.group [ … ]` and append the group — adds an extra IR
  node even for one line ⇒ drifts `6710215b…`; rejected.

## R6 — Empty / whitespace / blank-line normalisation

- **Decision**: `String.IsNullOrWhiteSpace raw` ⇒ no nodes (existing `fitLabel` guard). After splitting
  on `\n`, **trim each segment and drop segments that are empty/whitespace** (deterministic collapse), so
  `"A\n\n\nB"` ⇒ two lines `A`,`B` with no wasted gap, and `"\n  \n"` ⇒ no label.
- **Rationale**: FR-006 — empty/whitespace/blank-lines-only ⇒ no label, no throw; interior blanks must
  not draw wasted gaps that push content into other channels.
- **Alternatives considered**: preserve blank lines as gaps — wastes the budget and risks overflow;
  rejected.

## R7 — Determinism & the measurer-optional pure path

- **Decision**: Wrapping and stacking use only `Scene.measureTextResolved`, which with **no** measurer is
  byte-identical to the pure `measureText` heuristic and with the render-edge measurer matches the drawn
  advances. The library **never installs and never requires** a measurer and never throws without one;
  it emits the line nodes regardless. The layout is a **deterministic function of the resolved
  measurement** (FR-008/FR-009): identical multi-line `Token` under a fixed provider ⇒ identical scene ⇒
  identical canonical bytes (in-process and cross-process).
- **Rationale**: mirrors spec 196's measurer-optional contract; the only determinism axis is the
  measurement provider, which the tests fix.
- **Alternatives considered**: none — this is the established repo seam.

## R8 — Tofu-free verification (render edge), per line

- **Decision**: Add a `Symbology.Render.Tests` case that rasterises a **multi-line** labelled token
  through `Render.toPng` under the installed real measurer and asserts **every** line's glyph run is
  non-tofu (`Missing = false` / `TofuCount = 0`) and the output is non-blank — extending the existing
  single-line `RenderLabelTests.fs`.
- **Rationale**: FR-004/SC-002 — tofu-free is a render-edge property and must be proven on **every** line,
  not just the first; Constitution V (real evidence, no synthetic substitute).
- **Alternatives considered**: assert tofu-free in the pure library — impossible/incorrect (the pure
  fallback has no real shaper); rejected, kept at the edge.

## R9 — Test-battery shape (fail-before / pass-after, no weakening)

- **Decision**:
  - **Existing assertions stay green UNMODIFIED.** The `HMR-7` golden, the no-whitespace overlong fit,
    the `Label = None` goldens, gallery/filmstrip/badge/ring goldens, and the empty/whitespace cases all
    hold by construction (R2/R5). `LabelTests.fs` gains a **list-returning** helper but its current
    single-node assertions are unchanged.
  - **New `MultilineLabelTests.fs`**: (1) a one-line label is byte-identical to its spec-196 render
    (zero-drift anchor); (2) a `\n`-bearing label emits N stacked nodes, first at the 196 baseline;
    (3) a too-wide whitespace label wraps to multiple lines each ≤ region; (4) an over-budget label is
    capped and the last drawn line ends with `…`; (5) blank-line collapse; (6) every drawn line ≤ region
    width (no overflow), none clipped mid-glyph.
  - **DeterminismTests**: multi-line render-twice byte-equal + a **new pinned multi-line golden**;
    existing goldens asserted **unchanged** in the same file.
  - **ChannelPresence / Placeholder / Gallery / Legibility**: extended per the plan's file table.
  - **Render**: the R8 multi-line tofu test.
- **Rationale**: Constitution V — fail-before/pass-after, real evidence, no test weakened or deleted;
  new assertions are at least as strong (per-line ≤ region generalises the single-line ≤ region).
- **Alternatives considered**: rewrite the overlong fit test — unnecessary (the fixture has no
  whitespace, so it is unaffected); leave it as-is.

## Resolved unknowns summary

| Unknown | Resolution |
|---|---|
| Multi-line representation | Existing `Label : string option` + embedded `\n` + whitespace soft-wrap (R1) |
| Where soft breaks happen | Greedy whitespace word-wrap, no mid-word breaks (R2) |
| Per-line overflow/clip guard | Reuse `fitLabel` per line; cap + ellipsis on the last drawn line (R3) |
| Per-grammar vertical budget | Token ≤ 3 / Badge ≤ 2 / Ring ≤ 2 (provisional); line-height = `TextMetrics.Height` (R4) |
| Zero-drift mechanism | `Scene list` appended as bare siblings; `[]`≡no-label, `[one]`≡196 (R5) |
| Empty/blank handling | `IsNullOrWhiteSpace` ⇒ none; trim + drop blank segments (R6) |
| Determinism / measurer-optional | `measureTextResolved`; provider-relative; never requires/throws (R7) |
| Tofu-free verification | Render-bridge raster test, every line non-tofu (R8) |
| Test shape | Existing green unmodified; new multi-line battery + multi-line golden (R9) |
