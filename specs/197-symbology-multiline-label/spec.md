# Feature Specification: Symbology Multi-line / Paragraph Label Channel

**Feature Branch**: `197-symbology-multiline-label`

**Created**: 2026-06-25

**Status**: Draft

**Input**: User description: "next item in the plan." — the symbology **M0–M7 roadmap** in [`docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`](../../docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md) is complete, so this resolves to the next **deferred symbology backlog item**. Spec 196 ([symbology label / glyph-text channel](../196-symbology-label-text/spec.md)) shipped a **single-line** identity label and explicitly deferred **multi-line / paragraph text** as out of scope (FR-016). This feature delivers that deferred item: extend the existing optional identity-label channel so it can carry **more than one line** of text, wrapped/stacked and fitted within each grammar's label region, still tofu-free, still opt-in byte-identical when unused.

## Background

Spec 196 added an **optional single-line identity label** (name / callsign / code) to the shared symbology channel vocabulary: one `'stats -> Token` mapping carries the label, it is sited per grammar (Token / Badge / Ring), fitted to its region with real text measurement, and is **tofu-free** when rendered through the headless render bridge's real measurer. A label-free symbol renders byte-identically to the pre-feature symbol, and an overlong single-line label is shrunk/truncated to fit one line.

That single-line constraint is the limit being lifted here. Designers running the `fs-gg-symbology` render→eyeball→tweak loop sometimes need to read **two pieces of identity at once** — e.g. a callsign on one line and a unit code beneath it, or a short two-/three-word name that does not fit legibly on a single line at the on-board size. Today such content either overflows (and is truncated to one line, losing information) or is squeezed below legibility. A **multi-line / short-paragraph** label lets the same channel stack a few lines within the symbol's label region, laid out and fitted with the same real-measurement discipline, so the additional identity is readable without harming the other channels.

This stays a **single opt-in inspection-detail channel**, not a new subsystem: no rich-text styling, no auto-generation from stats, no label-bound motion — those remain deferred (FR-016).

## User Scenarios & Testing *(mandatory)*

A designer has a roster where one line of identity is not enough — a unit needs both a callsign and a short code shown, or a name too long to read on one line at board size. With spec 196 the label is forced onto a single line and the surplus is dropped or shrunk below legibility. This feature lets the same `'stats -> Token` mapping carry a **multi-line** label that is stacked into the symbol's label region, each line drawn with real glyphs (never tofu) and fitted so the block never overflows the footprint, clips a glyph, or collides with another channel. A unit with no label still renders exactly as the pre-feature symbol, and a unit whose label is a single line still renders exactly as it did in spec 196 — multi-line is purely additive on top of both.

### User Story 1 - Author a legible multi-line identity label (Priority: P1)

A designer attaches a label with **more than one line** of identity text (e.g. a callsign above a unit code) to a unit's encoded state and renders the symbol; the lines appear **stacked** within the symbol's label region, each drawn with **real glyphs, not tofu**, in every grammar (Token, Badge, Ring). A label that is a **single line** renders byte-identically to spec 196's single-line label, and a unit with **no** label renders byte-identically to the pre-feature symbol — multi-line is purely additive and opt-in.

**Why this priority**: It is the core value of the thread — reading more than one line of identity on a symbol — and the smallest independently demonstrable slice. With it, a designer can show a callsign and a code together. Shipping it alone closes the deferred multi-line gap.

**Independent Test**: Build `Token`s carrying a no-line (none), a single-line, and a multi-line label; render each in each grammar through the headless render bridge, and confirm (a) a multi-line label's rasterised output shows every line stacked and rendered legibly (non-tofu, matching the installed real measurer), (b) a single-line label's scene is byte-identical to the spec-196 single-line label and a no-label scene is byte-identical to the pre-feature symbol for the same channels, and (c) two `Token`s differing only in their label's line content produce observably different output.

**Acceptance Scenarios**:

1. **Given** a `Token` whose label carries two or more lines, **When** it is rendered in any grammar through the render bridge, **Then** the lines are drawn stacked within the label region, each with real glyphs (tofu-free) and legible at the target on-board size.
2. **Given** two `Token`s identical except one has a single-line label and the other has the same text as a multi-line label, **When** both are rendered, **Then** their scenes differ in a way attributable to the line layout (channel presence), and neither raises.
3. **Given** a `Token` whose label is a single line (no line breaks and short enough not to wrap), **When** it is rendered, **Then** its scene is byte-identical to the spec-196 single-line label for that token (zero drift on the existing single-line behaviour).
4. **Given** the same multi-line-labelled `Token` rendered twice (in-process and across processes) under the same text-measurement provider, **When** the two scenes are compared, **Then** they are byte-identical (determinism).

---

### User Story 2 - Multi-line label fits the region without clipping or overflow (Priority: P2)

A designer gives a unit a label with more content than the symbol's label region can hold — long lines, or more lines than fit. The label is **fitted** to the symbol's multi-line label region using real text measurement: long lines wrap (or shrink) at measured boundaries, the number of drawn lines is **capped** to what the region holds, and surplus content degrades gracefully (e.g. the last drawn line is truncated with an ellipsis) rather than spilling. The block never overflows the footprint horizontally or vertically, never clips a glyph mid-line, and never overlaps the sigil, health, or other channels.

**Why this priority**: A multi-line block that overflows vertically into the sigil/health channels, or clips, is worse than a single line — it harms legibility and obscures pre-attentive channels. Real-measurement-driven multi-line fit is what makes the wider channel trustworthy, but it builds on the basic line stacking proven by P1, so it follows.

**Independent Test**: Render a `Token` with a label whose lines are far wider and more numerous than its region holds, and confirm the drawn block stays within the symbol's label footprint (measured via the real measurer), no line is cut mid-glyph, the line count is capped, surplus is indicated by truncation rather than overflow, and the block does not overlap an adjacent channel; confirm an empty / whitespace / blank-lines-only label produces no label and no exception.

**Acceptance Scenarios**:

1. **Given** a label with lines wider than the region, **When** the symbol is rendered, **Then** each line is fitted (wrapped/shrunk/truncated) to stay within the region and is not clipped mid-glyph.
2. **Given** a label with more lines than the region can hold, **When** the symbol is rendered, **Then** the drawn line count is capped to the region and the surplus is indicated (e.g. ellipsis on the last drawn line) without overflowing into adjacent channels.
3. **Given** an empty, whitespace-only, or blank-lines-only label, **When** the symbol is rendered, **Then** no label is drawn and no exception is raised (equivalent to having no label).
4. **Given** a degenerate `Token` (`R <= 0`) that also carries a multi-line label, **When** it is rendered, **Then** the existing visible placeholder is produced and no exception is raised (the placeholder rule wins over the label).
5. **Given** a multi-line label that fits, **When** the symbol is rendered, **Then** the block sits in its designated per-grammar region and does not overlap the sigil, health, or other channels.

---

### User Story 3 - Multi-line labels on review boards, governed unchanged (Priority: P3)

A designer assembles a review board (gallery / motion filmstrip) of a multi-line-labelled roster in any grammar to A/B legibility, and runs the existing legibility linter (spec 194). Multi-line labels render reproducibly on the board; the linter continues to return a **grammar-independent** verdict and treats the label — single- or multi-line — as an **inspection-detail** channel (it may report label usage but does not change its governance of the pre-attentive channels). The design-loop skill documents the multi-line label with its legibility caveats (keep to a few short lines, inspection-only, requires the real measurer to be tofu-free, complements the vector sigil).

**Why this priority**: It turns the wider label into something the loop can compare and govern, and confirms the linter and skill keep working. Strictly additive on top of P1/P2.

**Independent Test**: Render a multi-line-labelled roster as a gallery in each grammar (reproducible per grammar); confirm the legibility linter's report for the roster is identical regardless of which grammar displays it and unchanged in its pre-attentive governance versus the same roster without labels; confirm the skill documents the multi-line label channel and passes the skill-parity check.

**Acceptance Scenarios**:

1. **Given** a multi-line-labelled roster and a selected grammar, **When** a review gallery is produced, **Then** every unit's label lines are drawn in that grammar and the board is byte-reproducible (under a fixed measurement provider).
2. **Given** the same roster scored with and without labels, **When** the legibility linter scores each, **Then** its verdict is grammar-independent and the addition of (single- or multi-line) labels does not alter the governance of the existing pre-attentive channels.
3. **Given** the design-loop skill's documentation, **When** an agent reads it, **Then** the multi-line label is described as an opt-in inspection-detail identity channel that requires the real measurer for tofu-free output, with guidance to keep to a few short lines and that it complements, never replaces, the vector sigil.

---

### Edge Cases

- **No label (the default)**: a `Token` with no label MUST render byte-identically to the current (pre-feature) symbol — the channel stays purely additive and opt-in (zero behavioural drift when unused).
- **Single-line label**: a label that is a single line (no breaks, fits without wrapping) MUST render byte-identically to spec 196's single-line label — multi-line is additive on top of the existing single-line behaviour (zero drift for single-line content).
- **Empty / whitespace / blank-lines-only label**: treated as no label — no text node emitted, no exception. Interior blank lines are normalised deterministically (e.g. collapsed) rather than drawn as empty gaps that waste the region.
- **Overlong line**: each line is fitted to the region width (wrap/shrink/truncate), never overflowing horizontally or clipping mid-glyph.
- **Too many lines**: the drawn line count is capped to what the region holds; surplus is indicated (e.g. trailing ellipsis on the last drawn line) rather than overflowing vertically into the sigil/health/other channels.
- **Degenerate token (`R <= 0`) with a multi-line label**: the existing visible-placeholder rule applies and takes precedence; never a blank scene, never an exception.
- **No real measurer installed (pure-fallback path)**: the symbol's scene is still produced deterministically and the label still emits its line text nodes; tofu-free *rendering* and *line wrapping at measured boundaries* are properties of the render edge (the render bridge installs the real measurer/shaper). The feature MUST NOT make the pure library depend on a measurer being installed, and MUST NOT throw when none is.
- **Determinism vs. measurement provider**: for a fixed measurement provider, identical multi-line-labelled `Token` ⇒ byte-identical scene. Where line breaking and block geometry are computed from measured text, the result is a deterministic function of the resolved measurement; the contract is reproducibility under a fixed provider, not identical bytes across different providers.
- **Label vs. sigil**: the (multi-line) label is a separate identity channel from the vector sigil; both may be present, and they MUST NOT be sited so they overlap.
- **Long Unicode / non-Latin lines**: each line is treated as an opaque short string; wrapping, fitting, and tofu-free behaviour follow whatever the installed real measurer/shaper supports (no new shaping/bidi responsibility is taken on by this feature).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The existing optional identity-label channel MUST support **more than one line** of text (a short paragraph), carried through the **same shared channel** so a single `'stats -> Token` mapping drives every grammar unchanged — no new per-grammar mapping and no second label channel.
- **FR-002**: Multi-line MUST be **opt-in and layered-additive**: a symbol with **no** label MUST render **byte-identically** to the current (pre-feature) symbol, and a symbol whose label is a **single line** MUST render **byte-identically** to spec 196's single-line label for the same channels (zero behavioural drift for no-label and single-line content).
- **FR-003**: Each grammar (Token, Badge, Ring) MUST **site the multi-line label** in a designated region of its symbol such that, when present, the stacked lines observably alter the output and do **not overlap** the sigil, health, or other channels.
- **FR-004**: When the symbol is rendered through the headless render bridge (which installs the real text measurer/shaper), **every line** of the label MUST be drawn with **real glyphs — tofu-free** — and legible at the target on-board size. *Legible* is operationalized as **tofu-free** (`Missing = false` for every covered glyph on every drawn line) at the grammar's label-region size, with the design-loop eyeball pass as the human legibility check; the automated assertion is the tofu-free property, not a measured contrast/size threshold.
- **FR-005**: The multi-line label MUST be **fitted** to its region using real text measurement: long lines MUST wrap or shrink at measured boundaries, the number of drawn lines MUST be **capped** to what the region holds, surplus content MUST degrade gracefully (e.g. truncation with an ellipsis on the last drawn line), and the drawn block MUST NOT clip mid-glyph and MUST NOT overflow the footprint horizontally or vertically into adjacent channels.
- **FR-006**: An **empty, whitespace-only, or blank-lines-only** label MUST be treated as no label (no text node, equivalent output to having no label) and MUST NOT raise; interior blank/whitespace lines MUST be normalised deterministically rather than drawn as wasted gaps.
- **FR-007**: A **degenerate token** (`R <= 0`) carrying a multi-line label MUST still degrade to the existing **visible placeholder** and MUST NOT raise; the placeholder rule takes precedence over the label.
- **FR-008**: The multi-line label MUST be **pure and deterministic** with respect to a fixed text-measurement provider: identical multi-line-labelled `Token` ⇒ identical scene ⇒ identical canonical bytes, in-process and across processes, with no wall-clock, randomness, or IO performed by the library. Line breaking and block layout MUST be a deterministic function of the resolved measurement.
- **FR-009**: The pure symbology library MUST **NOT depend** on a real measurer being installed: it MUST emit a deterministic scene (including the label's line text nodes) on the pure-fallback path and MUST NOT throw when no measurer is present. Tofu-free *rendering* and measured wrapping are properties of the render edge, not preconditions of the pure library.
- **FR-010**: A designer MUST be able to assemble a **review board** (gallery, and filmstrip where motion applies) of a multi-line-labelled roster in a selected grammar, reproducibly under a fixed measurement provider, with **no signature change** to the existing board/motion entry points.
- **FR-011**: The existing **legibility linter** (spec 194) MUST continue to return a **grammar-independent** verdict; if it reports the label at all, the label — single- or multi-line — MUST be treated as an **inspection-detail** channel that does not change the governance of the existing pre-attentive channels.
- **FR-012**: The existing **Directional Token, Badge, and Ring** grammars and their motion/gallery/filmstrip behaviour MUST be **unchanged** for any `Token` without a label **and** for any `Token` whose label is a single line (zero behavioural drift); the existing channel set is otherwise untouched.
- **FR-013**: This is an additive change to the symbology package. If it alters the package's curated public surface, that surface baseline MUST be regenerated to capture the change, with **zero surface drift** on every other package baseline; if the multi-line capability reuses the existing label surface without adding new public surface, the baselines MUST remain unchanged and that MUST be recorded.
- **FR-014**: The new multi-line capability MUST stay in the **pure scene-only layer** for scene construction — it adds no rendering, raster, GL, or IO dependency to the pure library; it depends only on the existing scene text/measurement vocabulary already available to the grammars, and bundles **no new font files**.
- **FR-015**: The `fs-gg-symbology` design-loop skill MUST document the multi-line label — when to use it, that it requires the real measurer for tofu-free output, that authors should keep to a few short lines, how surplus lines/width degrade, and that it complements rather than replaces the vector sigil — authored canonically and mirrored to every skill tree, passing the skill-parity check.
- **FR-016**: The following remain **out of scope**: rich-text styling (per-run colour/weight/size runs within the label), automatic label generation from stats without a human in the loop, label-bound motion, justified text / advanced bidirectional or complex-script typography beyond what the installed measurer already supports, any new GPU/compute path, and shipping new font files. The feature uses whatever font/measurer the render edge already provides.

### Key Entities *(include if feature involves data)*

- **Token (existing, extended)**: the fixed channel-set value describing one unit's encoded state. Its existing optional identity-label channel is **widened** so the label may carry multiple lines; all other channels and their meanings are unchanged. A `Token` with no label, or with a single-line label, behaves exactly as before this feature.
- **Multi-line identity label (channel, extended)**: the optional short identity text — now allowed to span **a few short lines / a short paragraph** rather than one line. An inspection-detail channel, distinct from and complementary to the vector sigil; sited per grammar so the stacked lines never collide with other channels; tofu-free when rendered through the real measurer.
- **Grammar (existing)**: the selectable form factor (Token / Badge / Ring). Each sites the multi-line label in its own region; the label is part of the one shared vocabulary, so the choice of grammar changes only where/how the lines are drawn, never the `ChannelMap`.
- **Text measurement provider (existing, at the render edge)**: the real measurer/shaper installed by the rendering edge that makes label glyphs tofu-free and drives both line wrapping and block fit. The pure library does not require it; the render bridge supplies it.
- **Legibility report (existing, spec 194)**: the governance artifact; grammar-independent, and unchanged in its pre-attentive-channel governance by the addition of the inspection-only label, whether single- or multi-line.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A roster's `'stats -> Token` mapping can attach a **multi-line** identity label that renders in **all three** grammars with **zero** additional per-grammar mapping and **zero** signature change to the existing board/motion entry points.
- **SC-002**: A multi-line-labelled symbol rendered through the render bridge shows **every line** with **real glyphs (tofu-free)**; a roster of distinct multi-line labels is mutually distinguishable at the target on-board size. *Distinguishable* and *legible* are operationalized as **tofu-free glyph runs** (`Missing = false`) on every drawn line plus **observably distinct rendered output** for distinct labels — not a measured contrast/size metric — with the eyeball-loop as the human check.
- **SC-003**: A `Token` with **no** label renders **byte-identically** to the current pre-feature symbol, and a `Token` with a **single-line** label renders **byte-identically** to the spec-196 single-line label, in every grammar (layered zero behavioural drift).
- **SC-004**: For a fixed measurement provider, rendering the same multi-line-labelled `Token` twice — same process and separate process — yields **byte-identical** scene data (100% reproducible).
- **SC-005**: An overlong / over-tall multi-line label stays within the symbol footprint with **zero** mid-glyph clips, **zero** horizontal or vertical overflow into adjacent channels, and a **bounded** drawn line count; an empty/whitespace/blank-lines-only label and a degenerate-token-with-label both produce **zero** exceptions.
- **SC-006**: The legibility linter's verdict for a fixed roster is **identical** regardless of grammar, and is unchanged in its governance of the existing pre-attentive channels by the presence of single- or multi-line labels.
- **SC-007**: Surface baselines reflect the change exactly — only the symbology package's baseline moves if new public surface is added (else all baselines are unchanged and that is recorded), with **zero drift** on all other package baselines; the design-loop skill documents the multi-line label channel and passes the skill-parity check with **zero** critical/high findings.

## Assumptions

- **How multi-line is expressed**: the label is carried by the **same shared label channel** spec 196 introduced (the natural extension of the "one channel vocabulary" principle), now permitted to contain **more than one line**. Multi-line content arises from **both** author-supplied hard line breaks **and** soft-wrapping of a long line to the region width at measured word/glyph boundaries — the standard short-paragraph behaviour. The exact field shape (e.g. embedded line breaks in the existing string vs a list of lines), the per-grammar multi-line region geometry, the maximum drawn line count, and the wrap/shrink/truncate policy are **implementation / design-loop details resolved at planning**, not contracts; the contracts are FR-002 (layered opt-in, zero drift for no-label and single-line), FR-003 (sited, observable, non-overlapping), FR-005 (fitted, capped, no clip/overflow), and FR-008 (deterministic).
- **What the label encodes**: a short, human-readable identity over a **few** short lines (e.g. callsign + code). It is an **inspection-detail** channel, not a pop-out one — the design-loop guidance keeps it to a few short lines and treats the label as complementary to, not a replacement for, the vector sigil and the pre-attentive channels.
- **Tofu-free and measured wrapping are render-edge properties**: the pure library emits a deterministic scene with line text nodes; **legible, tofu-free glyphs** and **wrapping at measured boundaries** come from the real measurer/shaper that the render bridge already installs. The pure library neither installs nor requires a measurer and never throws without one — matching the existing seam where the rendering edge owns the real measurer.
- **Determinism is provider-relative**: identical multi-line-labelled `Token` ⇒ byte-identical scene **under a fixed measurement provider**. Where line breaking and block geometry are computed from measured text, the output is a deterministic function of the resolved measurement; the feature does not promise identical bytes across *different* providers, only reproducibility under a fixed one.
- **Reused, unchanged**: the three grammars, the single-line label channel (spec 196), the legibility linter (spec 194), the headless render bridge (which installs the real measurer), the scene text/measurement vocabulary, the board/motion entry points, and the design-loop skill scaffolding all exist and are reused as-is; this feature adds only the multi-line widening of the label channel, its per-grammar multi-line siting, its wrap/cap/placeholder behaviour, tests, any regenerated surface baseline, and the skill's multi-line documentation.
- **One vocabulary, one widened channel**: no rich text, no new grammars, no new label channel, and no change to the channel-capacity governance beyond noting the label as inspection-detail — label breadth as a widening of a single opt-in channel, not a new subsystem.
