# Feature Specification: Deliver the Symbology Product Skill to Consumers

**Feature Branch**: `223-deliver-symbology-skill`

**Created**: 2026-06-30

**Status**: Draft

**Input**: User description: "start the next Rendering owned item on the coordination board." → resolved to Coordination board (FS-GG Projects v2 #1) Rendering item **FS-GG/FS.GG.Rendering#35** — "[cross-repo] Symbology product skill is authored but never delivered to consumers" (status: Ready; parent epic #34; publish vehicle #33, now released).

## Context *(non-normative)*

The `fs-gg-symbology` product skill (`template/product-skills/fs-gg-symbology/`) is high-quality content built across earlier features, but it is **stranded in the framework repo** — it reaches no generated product. Three defects compound:

1. **Not in the ship list.** `.template.config/template.json` sources six product skills (scene, skiaviewer, elmish, keyboard-input, ui-widgets, testing) each dual-emitted, but never sources `template/product-skills/fs-gg-symbology`. So no profile's generated app receives it — including `game`, the profile that most needs unit symbology.
2. **Missing consumer wrapper.** The six delivered product skills each have a `fs-gg-product-*` wrapper; `fs-gg-product-symbology` does not exist on either wrapper surface (`.claude/skills/` and `.agents/skills/`).
3. **Parity blind spot masking defect 2.** The skill-parity harness marks the product skill as `requiresWrapper`, but its missing-wrapper check (`tools/Rendering.Harness/SkillParity.fs:847`) is satisfied by *either* the product alias `fs-gg-product-symbology` **or** the bare canonical name `fs-gg-symbology`. The framework wrapper `.claude/skills/fs-gg-symbology` already occupies the bare name, so the check passes while the product wrapper is genuinely absent. Parity is green over a real hole.

All three are verified in source on `main`: `grep -i symbology .template.config/template.json` returns nothing; `.claude/skills/fs-gg-product-symbology` does not exist while `.claude/skills/fs-gg-symbology` does; and `SkillParity.fs:840-847` ORs the canonical-name match with the alias match.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Symbology skill ships in the generated product (Priority: P1)

A developer scaffolds a product from the `fs-gg-ui-template` (at minimum the `game` profile) and finds the symbology product skill present and routable in the generated app, alongside the other product skills — so they can author legible unit symbology without reaching into the framework repo.

**Why this priority**: This is the core defect — the skill reaches no consumer. Wiring it into the ship list is the single change that delivers the feature's primary value; without it the skill remains stranded regardless of the parity fix or wrapper.

**Independent Test**: Generate the `game` profile app from the template and confirm the symbology skill content is emitted to the generated app's skill directories. Delivers value on its own: a scaffolded product now contains symbology guidance.

**Acceptance Scenarios**:

1. **Given** the template at the `game` profile, **When** a product is scaffolded, **Then** the symbology product skill is present in the generated app's skill directories, dual-emitted to both the `.agents/skills/` and `.claude/skills/` surfaces like the other product skills.
2. **Given** the template at the `app` profile, **When** a product is scaffolded, **Then** symbology is present (or its intentional absence is an explicit, documented profile decision — not a silent omission).
3. **Given** a generated app, **When** the symbology product skill is invoked through its consumer wrapper name, **Then** it routes to the symbology skill content.

---

### User Story 2 - Symbology is reachable through the standard consumer wrapper (Priority: P2)

A developer in a generated product invokes symbology the same way they invoke every other product skill — via the `fs-gg-product-symbology` wrapper — and it resolves, with no collision against the framework's bare `fs-gg-symbology` skill.

**Why this priority**: The skill must be *reachable* the way consumers reach every other product skill. Without the wrapper the content can ship but be invoked inconsistently. Depends on US1 having something to route to.

**Independent Test**: In a generated app (or the template's wrapper surfaces) confirm a `fs-gg-product-symbology` wrapper exists on both wrapper surfaces and routes to the symbology product-skill content.

**Acceptance Scenarios**:

1. **Given** the template/generated app, **When** the wrapper surfaces are listed, **Then** `fs-gg-product-symbology` exists on both `.claude/skills/` and `.agents/skills/` and routes to the symbology product-skill content.
2. **Given** the framework's bare `fs-gg-symbology` skill and the new `fs-gg-product-symbology` wrapper coexisting, **When** either is invoked, **Then** each resolves to its own target with no name collision.

---

### User Story 3 - Parity harness fails honestly when a product wrapper is missing (Priority: P2)

A maintainer relies on the skill-parity harness to catch a product skill that lacks its consumer wrapper. The harness must report a missing-wrapper finding when the product-specific wrapper is absent, even if a same-named framework wrapper exists.

**Why this priority**: This closes the blind spot that hid the defect in the first place; without it the same class of bug can silently reappear for any product skill whose name collides with a framework wrapper. It guards the durability of US1/US2 rather than delivering new consumer value, hence P2.

**Independent Test**: Temporarily remove the `fs-gg-product-symbology` wrapper and run the parity harness; it MUST emit a missing-wrapper finding for the symbology product skill.

**Acceptance Scenarios**:

1. **Given** a product skill that requires a wrapper and whose product-alias wrapper is absent, **When** the parity harness runs, **Then** it emits a missing-wrapper finding — even if a framework wrapper occupies the bare canonical name.
2. **Given** the symbology product skill with its `fs-gg-product-symbology` wrapper present, **When** the parity harness runs, **Then** parity is green with no missing-wrapper finding for symbology.

---

### Edge Cases

- **Bare-name occupation**: a framework wrapper occupies the bare canonical name (`fs-gg-symbology`). The parity check MUST distinguish "product wrapper present" from "any same-named wrapper present" so the framework wrapper cannot satisfy the product skill's requirement.
- **Profile divergence**: the `game` and `app` profiles may differ on whether symbology ships. Each profile's inclusion/exclusion MUST be intentional and asserted, not incidental.
- **Other product skills with name collisions**: the parity fix MUST NOT cause false missing-wrapper findings for product skills that are correctly delivered (regression guard against the six existing product skills).
- **Republish timing**: the original publish vehicle (#33) is already released; consumers receive this change only on the **next** `fs-gg-ui-template` republish. The feature is not "delivered to consumers" until that republish carries it.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The template ship list MUST source the symbology product skill for the `game` profile **at minimum** (the exact shipping profile set is decided by FR-002), dual-emitted to both consumer skill surfaces, matching how the other six product skills are sourced and emitted.
- **FR-002**: The feature MUST make an explicit, documented decision on whether the `app` profile also ships symbology, and the chosen behavior MUST be asserted by a test.
- **FR-003**: A consumer wrapper `fs-gg-product-symbology` MUST exist on both wrapper surfaces and route to the symbology product-skill content, consistent with the other `fs-gg-product-*` wrappers.
- **FR-004**: The skill-parity harness MUST report a missing-wrapper finding for a product skill whose required product-alias wrapper is absent, even when a framework wrapper occupies the same bare canonical name. The bare-canonical-name match MUST NOT, on its own, satisfy a product skill's wrapper requirement.
- **FR-005**: An emit test (in the style of the Feature 219/220 emit tests) MUST assert symbology is present in the generated app for each profile that is expected to ship it.
- **FR-006**: The parity fix MUST NOT introduce false missing-wrapper findings for the already-correctly-delivered product skills (no regressions in existing parity results).
- **FR-007**: The change MUST be carried by an `fs-gg-ui-template` republish so consumers actually receive it; the cross-repo contract status for `fs-gg-ui-template` MUST be updated per the contract-change protocol when the republish lands.
- **FR-008**: On resolution, cross-repo issue **#35** MUST be updated/closed and its acceptance checklist satisfied, with the result reflected on the Coordination board.

### Key Entities *(include if feature involves data)*

- **Symbology product skill**: the authored consumer-facing skill content (`template/product-skills/fs-gg-symbology/`) that teaches authoring of legible unit symbology; the artifact to be delivered.
- **Template ship list**: the manifest (`.template.config/template.json`) that selects which product skills are copied into a generated app, per profile.
- **Profile**: a generation variant of `fs-gg-ui-template` (e.g. `game`, `app`) determining which content a scaffolded product receives.
- **Consumer wrapper**: a `fs-gg-product-*` entry on the consumer skill surfaces that routes an invocation to product-skill content.
- **Skill-parity finding**: the harness's record of a missing/mismatched wrapper for a skill that requires one.
- **`fs-gg-ui-template` contract entry**: the registry record whose version/coherence reflects what consumers can resolve.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A product scaffolded from the `game` profile contains the symbology product skill on both consumer skill surfaces — verified by an emit test that fails before this feature and passes after.
- **SC-002**: Symbology is invocable in a generated product through `fs-gg-product-symbology` on both wrapper surfaces, consistent with the other six product skills (7 of 7 product skills reachable via their product wrappers, up from 6 of 7).
- **SC-003**: Removing the `fs-gg-product-symbology` wrapper causes the parity harness to report a missing-wrapper finding for symbology (the blind spot is closed; the test fails before this feature's parity fix and detects the gap after).
- **SC-004**: The full parity run reports zero missing-wrapper findings with all seven product wrappers present, and no new findings for the previously-passing product skills (no regressions).
- **SC-005**: The `app` profile's symbology inclusion/exclusion matches the documented decision and is asserted by a test.
- **SC-006**: Cross-repo issue #35's acceptance items are all satisfied and the item is closed/Done on the Coordination board, carried by an `fs-gg-ui-template` republish reflected in the contract registry.

## Assumptions

- The next Rendering item to start is **#35** because it is the only Rendering item in **Ready** status whose blocker (#33, the republish vehicle) is now closed/released; #31 is already In progress and its producer authoring landed in features 220–222; #34/#36/#37/#38 are Backlog.
- "Dual-emit to both surfaces" follows the existing pattern: the six current product skills are each sourced twice in `template.json` (once per surface, `.agents/skills/` and `.claude/skills/`). Symbology will follow the same two-entry shape.
- The symbology product-skill *content* itself is already complete and correct; this feature delivers and wires it, it does not re-author the skill.
- The `game` profile is in scope for shipping symbology (it most needs unit symbology); the `app` profile is treated as a deliberate decision point (default assumption: include it, matching the other broadly-useful product skills, unless a profile constraint argues otherwise).
- The parity fix narrows the satisfying condition for product skills to the product-alias wrapper (and any explicitly intended self-exposure), rather than removing the bare-name match wholesale, to avoid breaking framework/ant canonical self-exposure paths already handled at `SkillParity.fs:842-847`.
- This feature owns the producer-side delivery; the actual republish/version bump rides the repo's standard release flow and updates the `fs-gg-ui-template` contract entry in `FS-GG/.github`.
