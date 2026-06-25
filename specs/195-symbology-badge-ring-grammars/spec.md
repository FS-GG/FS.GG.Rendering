# Feature Specification: Badge & Ring Alternative Symbology Grammars

**Feature Branch**: `195-symbology-badge-ring-grammars`

**Created**: 2026-06-25

**Status**: Draft

**Input**: User description: "next item in the plan." — resolved to the **Badge/Ring alternative grammars** thread of roadmap milestone **M7 (Governance & breadth)** in [`docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`](../../docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md). The M7 legibility-linter thread shipped as [spec 194](../194-symbology-legibility-linter/spec.md); the §4 grammar table names **three** symbol form factors — Badge, Token, Ring — of which only the **Directional Token** was built (M1–M6). This feature delivers the two remaining form factors as sibling grammars behind the **same fixed channel vocabulary**.

## User Scenarios & Testing *(mandatory)*

A designer running the `fs-gg-symbology` render→eyeball→tweak loop has already authored a per-game `'stats -> Token` mapping (the editable `ChannelMap`). Today the only way to *draw* a unit is the Directional Token — a heading-rotated arrow silhouette. Some games want a different visual register for the same encoded state: a stable, screen-aligned **emblem** (a unit card / insignia), or a **radial gauge** that reads health and charge as sweeps around a dial. This feature lets the designer render the *same roster, the same mapping* in a different grammar and pick the one that reads best — without re-authoring a single channel assignment.

### User Story 1 - Render a roster in the Badge grammar (Priority: P1)

A designer points the existing `Token` values at a new **Badge** symbol element — a compact, screen-aligned emblem (a framed card / insignia) — and gets a legible symbol that encodes every channel of the unit's state, drawn in a non-directional form factor. No change to the `ChannelMap` is required: the same `Token` that drives the Directional Token drives the Badge.

**Why this priority**: It is the smaller, more self-contained of the two new form factors and proves the core value proposition of the whole feature — *one channel vocabulary, multiple grammars*. Shipping it alone gives the design loop a second, genuinely different visual register and is an independently demonstrable MVP.

**Independent Test**: Build a `Token` set, render each as a Badge symbol, and confirm (a) the output is a non-blank scene, (b) each channel (faction hue, class, identity sigil, confirmed/suspected state, threat, charge, speed, health, shield, heading) observably alters the rendered output when varied, and (c) rendering the same `Token` twice yields byte-identical scene data.

**Acceptance Scenarios**:

1. **Given** a `Token` already used with the Directional Token grammar, **When** the designer renders it in the Badge grammar, **Then** a legible badge symbol is produced with no edit to the channel mapping.
2. **Given** two `Token`s differing in exactly one channel (e.g. faction, or health), **When** both are rendered as badges, **Then** the two scenes differ in a way attributable to that channel (channel presence).
3. **Given** the same `Token` rendered as a badge twice (in-process and across processes), **When** the two scenes are compared, **Then** they are byte-identical (determinism).
4. **Given** a degenerate `Token` (non-positive radius), **When** it is rendered as a badge, **Then** a visible placeholder is produced and no exception is raised.

---

### User Story 2 - Render a roster in the Ring grammar (Priority: P2)

A designer renders the same `Token` values as a **Ring** symbol — a radial/concentric form factor where continuous channels (health, charge) read naturally as arc sweeps and rim fill around a centre. As with Badge, no `ChannelMap` change is required.

**Why this priority**: It completes the §4 grammar breadth (all three form factors) and offers a third visual register, but it depends on the same shared-vocabulary plumbing proven by P1, so it follows Badge.

**Independent Test**: Render a `Token` set as Ring symbols and confirm non-blank output, observable per-channel variation, determinism, and degenerate-input placeholder behaviour — the same battery as P1, against the Ring element.

**Acceptance Scenarios**:

1. **Given** a `Token` already used with another grammar, **When** the designer renders it in the Ring grammar, **Then** a legible ring symbol is produced with no edit to the channel mapping.
2. **Given** `Token`s spanning the full health range, **When** rendered as rings, **Then** the health arc sweep grows monotonically with the health value.
3. **Given** the same `Token` rendered as a ring twice, **When** the two scenes are compared, **Then** they are byte-identical.
4. **Given** a degenerate `Token`, **When** rendered as a ring, **Then** a visible placeholder is produced and no exception is raised.

---

### User Story 3 - Compare grammars on a review board, governed unchanged (Priority: P3)

A designer assembles a review board (gallery / motion filmstrip) of a roster in a chosen grammar — or side-by-side across grammars — to A/B which form factor reads best at the target on-board size, and runs the existing legibility linter (spec 194) over the roster. Because every grammar shares the one fixed channel vocabulary, the linter's verdict is grammar-independent: the same roster lints to the same report regardless of which grammar draws it.

**Why this priority**: It turns the two new elements into a decision the loop can actually make (compare and choose), and confirms the governance gate (the linter) and the design-loop skill both keep working unchanged. Valuable but strictly additive on top of P1/P2.

**Independent Test**: Render the same roster as a gallery in each available grammar, confirm each board is reproducible, and confirm the legibility linter returns the identical report for the roster irrespective of the grammar selected for display.

**Acceptance Scenarios**:

1. **Given** a roster and a selected grammar, **When** a review gallery is produced, **Then** every unit is drawn in that grammar and the board is byte-reproducible.
2. **Given** the same roster, **When** the legibility linter scores it, **Then** the report (usage summary, findings, verdict) is identical no matter which grammar is chosen for rendering.
3. **Given** the design-loop skill's grammar documentation, **When** an agent reads it, **Then** Badge and Ring are described as selectable alternatives to the Token grammar behind the same channel set.

---

### Edge Cases

- **Degenerate / zero-area token** (`R <= 0`): each new grammar MUST degrade to a visible placeholder, never a blank scene and never an exception — mirroring the Token grammar's existing rule.
- **Heading on a screen-aligned grammar**: Badge and Ring do not rigidly rotate the whole symbol the way the Directional Token does; heading MUST still be encoded observably (e.g. a discrete pointer/pip) so the channel is not silently dropped.
- **`Custom` faction colour**: each grammar MUST honour an arbitrary custom faction colour on its hue channel, as the Token grammar does.
- **Channel a grammar can't site identically**: where a continuous channel (e.g. health) maps to a different primitive than the Token's belly arc, the value MUST still vary the output monotonically and legibly; no channel may be dropped from a grammar.
- **Empty roster / single unit** on a comparison board: review boards MUST render reproducibly with zero or one unit.
- **Motion overlay on a new grammar**: a grammar-agnostic motion overlay applied to a Badge/Ring symbol MUST remain deterministic; any motion that cannot be expressed identically across grammars is out of scope this iteration (see Assumptions).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The library MUST provide two new pure symbol grammars — **Badge** and **Ring** — as sibling elements to the existing Directional Token, each a deterministic function from the existing `Token` value to a scene description.
- **FR-002**: Both new grammars MUST consume the **same fixed channel vocabulary** (the existing `Token` channel set) with **no new per-game mapping required** — a single `'stats -> Token` mapping drives any of the three grammars unchanged.
- **FR-003**: Each new grammar MUST render **every channel** of the `Token` observably — faction hue, class, identity sigil, confirmed/suspected state, threat, charge, speed, health, shield, and heading — such that varying any one channel measurably alters the output (channel presence).
- **FR-004**: Each new grammar MUST be **pure and deterministic**: identical `Token` ⇒ identical scene ⇒ identical canonical bytes, in-process and across processes, with no wall-clock, randomness, or IO.
- **FR-005**: Each new grammar MUST degrade a **degenerate token** (`R <= 0`) to a **visible placeholder** and MUST NOT raise on degenerate or otherwise valid input.
- **FR-006**: The Badge grammar MUST be **screen-aligned** (it does not rigidly rotate the whole symbol with heading); it MUST still encode heading through a discrete, observable indicator so the channel is preserved.
- **FR-007**: The Ring grammar MUST render continuous channels (at minimum health and charge) as **radial/arc reads** around a centre, with health varying the arc sweep monotonically.
- **FR-008**: A designer MUST be able to assemble a **review board** (static gallery, and motion filmstrip where motion applies) of a roster in a **selected grammar**, reproducibly, so grammars can be compared at the target on-board size.
- **FR-009**: The existing **legibility linter** (spec 194) MUST continue to apply unchanged and return a **grammar-independent** report for a given roster — the channel-capacity governance does not depend on which grammar draws the symbols.
- **FR-010**: The existing **Directional Token** grammar and its motion/gallery/filmstrip behaviour MUST be **unchanged** (zero behavioural drift) by this feature.
- **FR-011**: This is an additive **public-surface (Tier 1)** change to the existing symbology package: the package's surface baseline MUST be regenerated to capture the new grammars, with **zero surface drift** on every other package baseline.
- **FR-012**: The new grammars MUST stay in the **pure scene-only layer** — no rendering, raster, GL, or IO dependency added; they depend only on the channel/`Token` types and the scene primitive vocabulary already available to the existing grammar.
- **FR-013**: The `fs-gg-symbology` design-loop skill MUST document **Badge and Ring as selectable grammars** behind the same channel vocabulary (when to prefer each; that the `ChannelMap` is unchanged across grammars), authored canonically and mirrored to every skill tree, passing the skill-parity check.
- **FR-014**: Motion overlays that are **grammar-agnostic** (expressed around the symbol centre/radius) MUST remain usable and deterministic on Badge/Ring symbols. Motion behaviour that cannot be expressed identically across grammars is out of scope (Assumptions / FR-015).
- **FR-015**: The following remain **out of scope** for this feature and stay backlog: **label / glyph text** (the third M7 thread), auto-selecting a grammar from stats without a human in the loop, and any new GPU/compute path.

### Key Entities *(include if feature involves data)*

- **Token (existing)**: the fixed channel-set value that describes one unit's encoded state. Unchanged by this feature — it is the shared input to all three grammars. Its fields are the channel vocabulary (faction, class, sigil, state, threat, charge, speed, health, shield, heading, centre, radius).
- **Badge symbol (new)**: a screen-aligned emblem/insignia form factor that encodes the full channel set; heading shown via a discrete indicator rather than whole-body rotation.
- **Ring symbol (new)**: a radial/concentric gauge form factor that encodes the full channel set; continuous channels read as arc sweeps and rim fill around a centre.
- **Grammar selection (new)**: the designer's choice of which form factor draws a roster; the unit of A/B comparison in the design loop. The choice changes the drawing, never the `ChannelMap`.
- **Legibility report (existing, spec 194)**: the governance artifact over a roster's channel usage; grammar-independent by construction.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A roster authored for the Directional Token renders in **all three** grammars (Token, Badge, Ring) with **zero changes** to the per-game channel mapping.
- **SC-002**: For each new grammar, **every** channel, when varied in isolation, produces an observable change in the rendered output (no silently-dropped channel).
- **SC-003**: For each new grammar, rendering the same `Token` twice — in the same process and in a separate process — yields **byte-identical** scene data (100% reproducible).
- **SC-004**: A degenerate token renders a **visible placeholder** with **zero** exceptions across both new grammars.
- **SC-005**: The legibility linter returns an **identical** report for a fixed roster regardless of which grammar is selected to display it.
- **SC-006**: The existing Token grammar's golden/determinism/rendering behaviour shows **zero** change; only the symbology package's surface baseline moves, with **zero drift** on all other package baselines.
- **SC-007**: The design-loop skill documents Badge and Ring as selectable grammars and passes the skill-parity check with **zero** critical/high findings.

## Assumptions

- **Visual identity of the two grammars** is under-specified by the source design doc (Badge/Ring were "sketched, unbuilt"). Reasonable, testable defaults are adopted and are themselves subject to the human-in-the-loop render→eyeball→tweak approval: **Badge** = a compact, screen-aligned framed emblem (frame hue = faction, frame stroke width = threat, solid/dashed frame = confirmed/suspected, interior gradient = charge, a bottom health bar, a row of speed pips, a corner shield mount, a centre sigil, a class-driven outline/glyph, and an edge heading pip); **Ring** = a centred radial gauge (outer ring hue = faction, ring thickness = threat, solid/dashed ring = state, radial interior gradient = charge, a health arc sweep, rim speed beads, a ring shield mount, a centre sigil, a class-driven inner glyph, and a heading needle). The exact geometry is an implementation/design-loop detail, not a contract; the contract is FR-003 (every channel observable) and FR-004 (determinism).
- **Screen-aligned vs directional**: Badge and Ring do **not** rigidly rotate the whole body with heading (unlike the Directional Token); heading is encoded discretely. This is the design value of an alternative grammar — a stable emblem some games prefer.
- **Motion this iteration** is limited to overlays that are grammar-agnostic (centred pulse/blink/etc. around the symbol radius); bespoke per-grammar motion is deferred. Static galleries are the primary review surface for the new grammars; filmstrips apply only where the overlay is grammar-agnostic.
- **Reused, unchanged**: the `Token` channel set, the legibility linter (spec 194), the headless render bridge, and the design-loop skill scaffolding all exist and are reused as-is; this feature adds only the two new pure elements (plus the grammar-selection affordance for review boards), their tests, the regenerated symbology surface baseline, and the skill's grammar documentation.
- **One channel vocabulary**: no new channels, no new `Token` fields, and no change to the §4 capacity table are introduced — grammar breadth, not channel breadth.
