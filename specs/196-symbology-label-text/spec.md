# Feature Specification: Symbology Label / Glyph Text Channel

**Feature Branch**: `196-symbology-label-text`

**Created**: 2026-06-25

**Status**: Draft

**Input**: User description: "next item in the plan." — resolved to the **label / glyph text** thread of roadmap milestone **M7 (Governance & breadth)** in [`docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`](../../docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md). M7's two sibling threads have shipped — the **legibility linter** ([spec 194](../194-symbology-legibility-linter/spec.md)) and the **Badge/Ring grammars** ([spec 195](../195-symbology-badge-ring-grammars/spec.md), which explicitly deferred label text as backlog in its FR-015). This feature delivers the **third and final** M7 thread: an **optional identity label** on a symbol's text channel, rendered tofu-free, so a designer can put a unit's short name / callsign / code on the symbol when the abstract vector sigil alone is not enough to read identity.

## Background

Identity on a symbol is carried today by the **vector sigil** only (`Bolt` / `Ring` / `Fang` / `Mark`) — text labels were deliberately left out of the original Directional Token (spec 192) because pure-CPU text can render as **tofu** (missing-glyph boxes) when no real font measurer/shaper is installed, and "symbol, not depiction" favoured abstract marks. The source design doc flagged this as the open item to revisit "once tofu-free text is needed" (§9 risk 1, §10.8 M7), via the real-text-measurer path that the rendering edge already provides. Some rosters have more units than there are distinguishable sigils, or want a human-readable callsign on the board; for those, an opt-in text label is the missing identity channel.

## User Scenarios & Testing *(mandatory)*

A designer running the `fs-gg-symbology` render→eyeball→tweak loop has a roster where the abstract sigil is not enough to tell two units apart at a glance — e.g. eight infantry variants that all read as the same class silhouette, or a board where commanders want callsigns shown. Today the only identity channels are faction hue, class silhouette, and the centre sigil; there is no way to put `"A-7"` or `"Hammer"` on the symbol. This feature adds an **optional short text label** to the shared channel vocabulary so the same `'stats -> Token` mapping can carry an identity string, drawn legibly (real glyphs, never tofu) and sited per grammar so it never collides with or clips the other channels. A unit with no label renders exactly as it does today.

### User Story 1 - Put a legible identity label on a symbol (Priority: P1)

A designer adds a short identity string (name / callsign / code) to a unit's encoded state and renders the symbol; the label appears on the symbol drawn with **real glyphs, not tofu**, in every grammar (Token, Badge, Ring). A unit with **no** label renders byte-identically to today — the label is purely additive and opt-in.

**Why this priority**: It is the core value of the whole thread — a readable text identity channel — and is the smallest independently demonstrable slice. With it, a designer can disambiguate units that share a sigil. Shipping it alone closes the last M7 gap.

**Independent Test**: Build `Token`s with and without a label, render each in each grammar through the headless render bridge, and confirm (a) a labelled symbol's rasterised output contains the label's glyphs rendered legibly (non-tofu, matching the installed real measurer), (b) an unlabelled symbol's scene is byte-identical to the pre-feature symbol for the same channels, and (c) two `Token`s differing only in label produce observably different output.

**Acceptance Scenarios**:

1. **Given** a `Token` carrying an identity label, **When** it is rendered in any grammar through the render bridge, **Then** the label is drawn with real glyphs (tofu-free) and is legible at the target on-board size.
2. **Given** two `Token`s identical except one has a label and one has none, **When** both are rendered, **Then** the unlabelled one's scene is byte-identical to the current (pre-feature) symbol and the labelled one's scene additionally contains the label.
3. **Given** two `Token`s differing only in label text, **When** both are rendered, **Then** their scenes differ in a way attributable to the label (channel presence).
4. **Given** the same labelled `Token` rendered twice (in-process and across processes) under the same text-measurement provider, **When** the two scenes are compared, **Then** they are byte-identical (determinism).

---

### User Story 2 - Label fits the symbol without clipping or overflow (Priority: P2)

A designer gives a unit a label that is longer than the symbol can comfortably hold; the label is **fitted** to the symbol's label region — sized/placed using the real text measurement so it never overflows the symbol footprint, never clips mid-glyph, and never overlaps another channel — degrading gracefully (e.g. truncation with an ellipsis, or a shrink-to-fit) rather than spilling.

**Why this priority**: A label that overflows or clips mid-glyph is worse than no label — it harms legibility and can obscure other channels. Real-measurement-driven fit is what makes the channel trustworthy, but it builds on the basic label placement proven by P1, so it follows.

**Independent Test**: Render a `Token` with a label far wider than its region and confirm the drawn label stays within the symbol's label footprint (measured via the real measurer), is not cut mid-glyph, and does not overlap an adjacent channel; confirm an empty/whitespace label produces no label and no exception.

**Acceptance Scenarios**:

1. **Given** a label longer than its region, **When** the symbol is rendered, **Then** the label is fitted (truncated/shrunk) to stay within the region and is not clipped mid-glyph.
2. **Given** an empty or whitespace-only label, **When** the symbol is rendered, **Then** no label is drawn and no exception is raised (equivalent to having no label).
3. **Given** a degenerate `Token` (`R <= 0`) that also carries a label, **When** it is rendered, **Then** the existing visible placeholder is produced and no exception is raised (the placeholder rule wins over the label).
4. **Given** a label that fits, **When** the symbol is rendered, **Then** the label sits in its designated per-grammar region and does not overlap the sigil, health, or other channels.

---

### User Story 3 - Labels on review boards, governed unchanged (Priority: P3)

A designer assembles a review board (gallery / motion filmstrip) of a labelled roster in any grammar to A/B legibility, and runs the existing legibility linter (spec 194). Labels render reproducibly on the board; the linter continues to return a **grammar-independent** verdict and treats the label as an **inspection-detail** channel (it may report label usage but does not change its governance of the pre-attentive channels). The design-loop skill documents the label as an **opt-in identity channel** with its legibility caveats (short strings, inspection-only, requires the real measurer to be tofu-free).

**Why this priority**: It turns the label into something the loop can compare and govern, and confirms the linter and skill keep working. Strictly additive on top of P1/P2.

**Independent Test**: Render a labelled roster as a gallery in each grammar (reproducible per grammar); confirm the legibility linter's report for the roster is identical regardless of which grammar displays it; confirm the skill documents the label channel and passes the skill-parity check.

**Acceptance Scenarios**:

1. **Given** a labelled roster and a selected grammar, **When** a review gallery is produced, **Then** every unit's label is drawn in that grammar and the board is byte-reproducible (under a fixed measurement provider).
2. **Given** the same labelled roster, **When** the legibility linter scores it, **Then** its verdict is grammar-independent and the addition of labels does not alter the governance of the existing pre-attentive channels.
3. **Given** the design-loop skill's documentation, **When** an agent reads it, **Then** the label is described as an opt-in inspection-detail identity channel that requires the real measurer for tofu-free output, with guidance to keep label strings short.

---

### Edge Cases

- **No label (the default)**: a `Token` with no label MUST render byte-identically to the current (pre-feature) symbol — the channel is purely additive and opt-in (zero behavioural drift when unused).
- **Empty / whitespace-only label**: treated as no label — no text node emitted, no exception.
- **Overlong label**: fitted to the label region (truncate/shrink), never overflowing the footprint or clipping mid-glyph.
- **Degenerate token (`R <= 0`) with a label**: the existing visible-placeholder rule applies and takes precedence; never a blank scene, never an exception.
- **No real measurer installed (pure-fallback path)**: the symbol's scene is still produced deterministically and the label still emits a text node; tofu-free *rendering* is a property of the render edge (the render bridge installs the real measurer/shaper). The feature MUST NOT make the pure library depend on a measurer being installed, and MUST NOT throw when none is.
- **Determinism vs. measurement provider**: for a fixed measurement provider, identical labelled `Token` ⇒ byte-identical scene. Where the symbol's geometry is fit using measured text width, the result is a deterministic function of the resolved measurement; the contract is reproducibility under a fixed provider, not identical bytes across different providers.
- **Label vs. sigil**: the label is a separate identity channel from the vector sigil; both may be present, and they MUST NOT be sited so they overlap.
- **Long Unicode / non-Latin label**: the label is treated as an opaque short string; fitting and tofu-free behaviour follow whatever the installed real measurer/shaper supports (no new shaping responsibility is taken on by this feature).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The symbology vocabulary MUST gain an **optional identity label** (a short text string) as part of the shared channel set, so a single `'stats -> Token` mapping can carry a label that drives every grammar unchanged — no new per-grammar mapping.
- **FR-002**: The label MUST be **optional and opt-in**: a symbol with no label MUST render **byte-identically** to the current (pre-feature) symbol for the same channels (zero behavioural drift when the channel is unused).
- **FR-003**: Each grammar (Token, Badge, Ring) MUST **site the label** in a designated region of its symbol such that, when present, the label observably alters the output and does **not overlap** the sigil, health, or other channels.
- **FR-004**: When the symbol is rendered through the headless render bridge (which installs the real text measurer/shaper), the label MUST be drawn with **real glyphs — tofu-free** — and legible at the target on-board size. *Legible* is operationalized as **tofu-free** (`Missing = false` for every covered glyph) at the grammar's label-region size ([data-model.md](./data-model.md)), with the design-loop eyeball pass as the human legibility check; the automated assertion is the tofu-free property, not a measured contrast/size threshold.
- **FR-005**: The label MUST be **fitted** to its region using real text measurement: an overlong label MUST be truncated or shrunk to stay within the symbol footprint, MUST NOT clip mid-glyph, and MUST NOT overflow into adjacent channels.
- **FR-006**: An **empty or whitespace-only** label MUST be treated as no label (no text node, equivalent output to having no label) and MUST NOT raise.
- **FR-007**: A **degenerate token** (`R <= 0`) carrying a label MUST still degrade to the existing **visible placeholder** and MUST NOT raise; the placeholder rule takes precedence over the label.
- **FR-008**: The label channel MUST be **pure and deterministic** with respect to a fixed text-measurement provider: identical labelled `Token` ⇒ identical scene ⇒ identical canonical bytes, in-process and across processes, with no wall-clock, randomness, or IO performed by the library.
- **FR-009**: The pure symbology library MUST **NOT depend** on a real measurer being installed: it MUST emit a deterministic scene (including the label's text node) on the pure-fallback path and MUST NOT throw when no measurer is present. Tofu-free *rendering* is a property of the render edge, not a precondition of the pure library.
- **FR-010**: A designer MUST be able to assemble a **review board** (gallery, and filmstrip where motion applies) of a labelled roster in a selected grammar, reproducibly under a fixed measurement provider.
- **FR-011**: The existing **legibility linter** (spec 194) MUST continue to return a **grammar-independent** verdict; if it reports the label at all, the label MUST be treated as an **inspection-detail** channel that does not change the governance of the existing pre-attentive channels.
- **FR-012**: The existing **Directional Token, Badge, and Ring** grammars and their motion/gallery/filmstrip behaviour MUST be **unchanged** for any `Token` without a label (zero behavioural drift); the existing channel set is otherwise untouched.
- **FR-013**: This is an additive **public-surface (Tier 1)** change to the symbology package: the package's surface baseline MUST be regenerated to capture the new label channel, with **zero surface drift** on every other package baseline.
- **FR-014**: The new label capability MUST stay in the **pure scene-only layer** for scene construction — it adds no rendering, raster, GL, or IO dependency to the pure library; it depends only on the existing scene text/measurement vocabulary already available to the grammars.
- **FR-015**: The `fs-gg-symbology` design-loop skill MUST document the label as an **opt-in inspection-detail identity channel** — when to use it, that it requires the real measurer for tofu-free output, that strings should be kept short, and that it does not replace the vector sigil — authored canonically and mirrored to every skill tree, passing the skill-parity check.
- **FR-016**: The following remain **out of scope**: multi-line / paragraph text, rich text styling (per-run colour/weight runs beyond a single label style), automatic label generation from stats without a human in the loop, label-bound motion, and any new GPU/compute path. Bundling or shipping new font files is out of scope — the feature uses whatever font/measurer the render edge already provides.

### Key Entities *(include if feature involves data)*

- **Token (existing, extended)**: the fixed channel-set value describing one unit's encoded state. This feature adds **one optional channel** — an identity label string — to the shared vocabulary; all existing channels and their meanings are unchanged. A `Token` with no label is the default and behaves exactly as today.
- **Identity label (new channel)**: an optional short text string (name / callsign / code) that reads identity directly. An inspection-detail channel, distinct from and complementary to the vector sigil; sited per grammar so it never collides with other channels; tofu-free when rendered through the real measurer.
- **Grammar (existing)**: the selectable form factor (Token / Badge / Ring). Each sites the label in its own region; the label is part of the one shared vocabulary, so the choice of grammar changes only where/how the label is drawn, never the `ChannelMap`.
- **Text measurement provider (existing, at the render edge)**: the real measurer/shaper installed by the rendering edge that makes label glyphs tofu-free and drives fit-to-region. The pure library does not require it; the render bridge supplies it.
- **Legibility report (existing, spec 194)**: the governance artifact; grammar-independent, and unchanged in its pre-attentive-channel governance by the addition of the inspection-only label.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A roster's `'stats -> Token` mapping can attach an identity label that renders in **all three** grammars with **zero** additional per-grammar mapping.
- **SC-002**: A labelled symbol rendered through the render bridge shows the label with **real glyphs (tofu-free)**; a roster of distinct labels is mutually distinguishable at the target on-board size. *Distinguishable* and *legible* are operationalized as **tofu-free glyph runs** (`Missing = false`) plus **observably distinct rendered output** for distinct labels — not a measured contrast/size metric — with the eyeball-loop as the human check.
- **SC-003**: A `Token` with **no** label renders **byte-identically** to the current pre-feature symbol in every grammar (zero behavioural drift when the channel is unused).
- **SC-004**: For a fixed measurement provider, rendering the same labelled `Token` twice — same process and separate process — yields **byte-identical** scene data (100% reproducible).
- **SC-005**: An overlong label stays within the symbol footprint with **zero** mid-glyph clips and **zero** overflow into adjacent channels; an empty/whitespace label and a degenerate-token-with-label both produce **zero** exceptions.
- **SC-006**: The legibility linter's verdict for a fixed roster is **identical** regardless of grammar, and is unchanged in its governance of the existing pre-attentive channels by the presence of labels.
- **SC-007**: Only the symbology package's surface baseline moves (to capture the label channel), with **zero drift** on all other package baselines; the design-loop skill documents the label channel and passes the skill-parity check with **zero** critical/high findings.

## Assumptions

- **Where the label lives**: the label is added as **one optional field on the shared `Token`** (the natural extension of the "one channel vocabulary" principle that has governed every symbology feature), defaulting to "no label" so that `defaultToken` and every existing label-free `Token` render byte-identically. The exact field name/shape and the per-grammar label regions are an implementation/design-loop detail resolved at planning, not a contract; the contract is FR-002 (opt-in, zero drift when unused) + FR-003 (sited, observable, non-overlapping) + FR-008 (deterministic).
- **What the label encodes**: a short, human-readable identity string (name / callsign / code). It is an **inspection-detail** channel, not a pop-out one — the design-loop guidance keeps strings short and treats the label as complementary to, not a replacement for, the vector sigil and the pre-attentive channels.
- **Tofu-free is a render-edge property**: the pure library emits a deterministic scene with a text node; **legible, tofu-free glyphs** come from the real measurer/shaper that the render bridge already installs. The pure library neither installs nor requires a measurer and never throws without one — matching the existing seam where the rendering edge owns the real measurer.
- **Determinism is provider-relative**: identical labelled `Token` ⇒ byte-identical scene **under a fixed measurement provider**. Where label geometry is fit using measured width, the output is a deterministic function of the resolved measurement; the feature does not promise identical bytes across *different* providers, only reproducibility under a fixed one (mirroring how the repo already treats the measurement seam).
- **Reused, unchanged**: the three grammars, the legibility linter (spec 194), the headless render bridge (which installs the real measurer), the scene text/measurement vocabulary, and the design-loop skill scaffolding all exist and are reused as-is; this feature adds only the optional label channel, its per-grammar siting, its fit/placeholder behaviour, tests, the regenerated symbology surface baseline, and the skill's label documentation.
- **One vocabulary, one new channel**: no rich text, no multi-line text, no new grammars, and no change to the §4 channel-capacity governance beyond noting the label as inspection-detail — label breadth as a single opt-in channel, not a new subsystem.
