# Feature Specification: Consumer Theming/Styling Product Skill

**Feature Branch**: `226-consumer-styling-skill`

**Created**: 2026-07-01

**Status**: Draft

**Change Classification**: **Tier 2 (internal/content change)** — package-content only (FR-010). Adds one shipped consumer styling skill, its product-skill wrapper, template wiring, a catalog row, a backstop-list entry, and one cross-link. It introduces **no** public API surface, touches **no** `.fs`/`.fsi` module, and updates **no** surface-area baseline; therefore the Tier 1 artifact chain (`.fsi` updates, baseline updates) does not apply. (Per Constitution → Change Classification, which requires every feature to declare its tier.)

**Input**: User description: "start the next Rendering owned item on the coordination board." → Coordination board (FS-GG/Coordination, Projects v2) next Rendering-owned Backlog item: **FS-GG/FS.GG.Rendering#38 — "Add a consumer theming/styling product skill (capability gap)"** (parent epic #34, contract `fs-gg-ui-template`).

## Context *(why this exists)*

A scaffolded FS.GG.UI product ships with seven product skills (`template/product-skills/fs-gg-*`) that teach a product author how to build scenes, compose controls, wire Elmish/keyboard input, render in the viewer, draw symbology, and test. **None of them teach the author how to style or theme the product.** A consumer composing `Button`/`TextBox` via the shipped `fs-gg-ui-widgets` skill has no shipped guidance on selecting a theme, setting style variants/classes on a control, or applying a resolved style — even though the framework fully supports all of this.

The styling capability is real but has **no consumer surface**:

- `fs-gg-design-system` is the canonical framework-internal skill (DTCG token source, `StyleResolver` internals, surface baselines). It is correctly **not shipped** to consumers — it documents the pipeline that produces styles, not how a product author consumes them.
- `fs-gg-ant-design` is the only styling-pattern advice and is **also not shipped**.

This feature closes that gap by adding one thin, shipped, consumer-facing styling skill — the *consume-a-style* slice, not the *build-the-resolver* pipeline.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A product author can learn to theme and style their product from a shipped skill (Priority: P1)

A developer who scaffolded an FS.GG.UI product wants their `Button`s, `TextBox`es, and surfaces to look themed and to vary by intent/state. Today they open the shipped skill set, find skills for controls/scene/input/testing, and find **nothing** about styling — so they either give up, guess at internal types, or reach for framework-internal docs they were never shipped. With this feature, a shipped styling skill teaches them, in product-author language, how to: pick/apply a theme, set a control's style variant and style class, and apply a resolved style to a control.

**Why this priority**: This is the capability gap itself. Without the skill body, nothing else in the feature has value. A single shipped skill that teaches the consumer styling slice is the MVP.

**Independent Test**: Read the new skill as a product author with no access to framework-internal repos; confirm it answers "how do I theme my product and style a control" using only consumer-visible concepts (theme selection, style variant, style class, resolved style applied to a control) and never instructs the reader to edit token sources, the resolver, or surface baselines.

**Acceptance Scenarios**:

1. **Given** the shipped product-skill set, **When** a product author looks for styling guidance, **Then** exactly one product skill covers theming/styling at the consumer slice and is discoverable by name and description.
2. **Given** the new styling skill, **When** an author follows it to apply a theme and set a control's variant/class, **Then** the guidance uses only the consumer-facing styling surface (theme selection, style variant, style class, resolved style) and does not require touching the DTCG token source, the `StyleResolver` internals, or surface baselines.
3. **Given** the new styling skill, **When** it is reviewed for leakage, **Then** it contains no framework-repo process vocabulary (framework feature/spec numbers, framework evidence-process references, framework-internal feedback-capture paths) — i.e. it passes the repo-owned product-skill leak guard.

---

### User Story 2 - A scaffolded product actually carries the styling skill on the right profiles (Priority: P2)

The skill only delivers value if a real scaffolded product receives it. A product author who scaffolds a UI-bearing product profile must find the styling skill present in their generated product alongside the other product skills — not stranded in the framework repo.

**Why this priority**: Authoring the body (US1) is necessary but the audit that surfaced #38 found *delivery* bugs are a recurring failure mode (a sibling skill was authored but never shipped). Delivery is a distinct, independently testable outcome and must not be assumed.

**Independent Test**: Scaffold each UI-bearing product profile and confirm the styling skill is present in the produced product's shipped skill set (and absent from profiles that ship no controls), independent of the chosen lifecycle.

**Acceptance Scenarios**:

1. **Given** a scaffold of a profile that ships controls, **When** the product is produced, **Then** the styling skill is present in the produced product-skill set with its product wrapper, exactly as the other shipped skills are.
2. **Given** a scaffold of a profile that ships **no** controls (scene-only), **When** the product is produced, **Then** the styling skill is **not** forced in where it has no controls to style.
3. **Given** any supported lifecycle choice on a UI-bearing profile, **When** the product is produced, **Then** the styling skill ships the same way (skill presence follows the product surface, not the lifecycle).

---

### User Story 3 - An author composing controls is pointed to the styling skill (Priority: P3)

An author following the shipped controls skill (`fs-gg-ui-widgets`) to compose `Button`/`TextBox` should be told where to learn how to make those controls themed — without having to already know a styling skill exists.

**Why this priority**: Discoverability multiplies the value of US1/US2 but is not itself the capability. The skill is still usable if found by its own name/description; the cross-link removes a discovery dead-end.

**Independent Test**: Read `fs-gg-ui-widgets` as a product author and confirm it explicitly points to the styling skill for theming the controls it teaches you to compose.

**Acceptance Scenarios**:

1. **Given** the shipped `fs-gg-ui-widgets` skill, **When** an author reads it, **Then** it contains a clear pointer to the styling skill for theming/styling the controls it covers.

---

### Edge Cases

- **Scope creep into the pipeline**: The skill must stay the consumer slice. If it starts documenting how the resolver computes a style, how tokens are sourced, or how surface baselines are authored, it has duplicated framework-internal `fs-gg-design-system` and must be trimmed back.
- **Profile with no controls**: Scene-only profiles (no controls surface) have nothing to style at the control level; the feature must decide and document whether the skill ships there at all (default: ship only where controls ship).
- **Leak reintroduction**: A new shipped skill is exactly the kind of content the de-leak guard (sibling feature) scans; the skill must be authored to pass that guard from the start, and the guard's discovery surface must include it.
- **Delivery-only-on-republish**: Like all product-skill content, the skill reaches consumers only when shipped in a republished template package and re-pinned downstream; "merged on the branch" is not "delivered."
- **Naming collision**: The shipped skill name must not collide with, or be confused for, the framework-internal `fs-gg-design-system` / `fs-gg-ant-design` skills, which remain unshipped.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The product MUST ship a single, consumer-facing styling/theming product skill in the shipped product-skill set (`template/product-skills/fs-gg-*`), parallel in form to the existing seven shipped skills.
- **FR-002**: The styling skill MUST teach the consumer slice only: how to select/apply a theme, how to set a control's style variant and style class, and how to apply a resolved style to a control.
- **FR-003**: The styling skill MUST NOT document framework-internal styling machinery — the DTCG token source, the `StyleResolver` pipeline internals, or surface baselines — that authority remains with the unshipped `fs-gg-design-system` skill.
- **FR-004**: The styling skill MUST be authored to pass the repo-owned product-skill leak guard: no framework feature/spec numbers, no framework evidence-process references, no framework-internal feedback-capture paths.
- **FR-005**: The styling skill MUST be wired into the template configuration so it is produced on the product profiles that ship controls, carried by its product-skill wrapper exactly as the other shipped skills are.
- **FR-006**: The styling skill MUST NOT be force-shipped onto profiles that ship no controls (scene-only profiles), where it would have nothing to style.
- **FR-007**: Whether the styling skill ships MUST depend on the product surface (controls present), NOT on the lifecycle choice (`spec-kit`/`sdd`/`none`).
- **FR-008**: The shipped `fs-gg-ui-widgets` skill MUST cross-link to the styling skill so an author composing controls can discover how to theme them.
- **FR-009**: The repo-owned product-skill discovery/parity surface (skill-parity check and the de-leak guard) MUST enumerate the new styling skill as part of the shipped product-skill set, so it is neither orphaned nor invisible to those checks.
- **FR-010**: The feature MUST be package-content only: it adds one product skill and its wiring/cross-link, and MUST NOT change the behavior or capability of any existing shipped skill, nor alter the framework's styling code surface.

### Key Entities

- **Styling product skill**: The new shipped consumer-facing skill teaching theme selection, style variant, style class, and resolved-style application; lives beside the other `template/product-skills/fs-gg-*` skills.
- **Product-skill wrapper**: The shipped `fs-gg-product-*` form/wrapper that each product skill is delivered through to a scaffolded product.
- **Shipped product-skill set**: The collection of skills a produced product actually carries (today: seven), wired in the template configuration and enumerated by the skill-parity/leak surfaces.
- **Product profile**: The scaffold profile choice (`app`, `game`, `sample-pack`, `governed`, `headless-scene`) that determines which surfaces — including controls — a produced product carries.
- **Consumer styling surface**: The product-author-visible styling concepts (theme, style variant, style class, resolved style applied to a control) — distinct from the framework-internal token source / resolver / surface baselines.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A product author can answer "how do I theme my product and style a control" using only the shipped product-skill set, with zero references out to framework-internal repos or unshipped skills.
- **SC-002**: The shipped product-skill set grows by exactly one skill (the styling skill); no existing shipped skill changes capability or behavior.
- **SC-003**: 100% of scaffolded controls-bearing product profiles produce a product that carries the styling skill; 100% of scene-only profiles produce a product that does not force it in.
- **SC-004**: The styling skill passes the repo-owned product-skill leak guard and is enumerated by the skill-parity check — zero leak-class hits, zero "skill present but undiscovered" gaps.
- **SC-005**: An author reading `fs-gg-ui-widgets` reaches the styling skill via an explicit in-skill pointer in one hop (no external search required).
- **SC-006**: The styling skill stays within the consumer slice — it contains zero instructions to edit the token source, the resolver, or surface baselines.

## Assumptions

- **Which profiles get it**: The styling skill ships to the product profiles that ship controls / the `fs-gg-ui-widgets` skill (default reading: `app`, `game`, `sample-pack`); scene-only profiles (`headless-scene`, `governed`) do not force it in. The exact controls-bearing profile set is confirmed against the real template wiring during planning, mirroring how `fs-gg-ui-widgets` itself is gated.
- **Skill naming**: The shipped skill takes a consumer-oriented `fs-gg-*` name distinct from the unshipped framework-internal `fs-gg-design-system` / `fs-gg-ant-design`, so the shipped/unshipped boundary stays legible.
- **Consumer styling surface is sufficient and stable**: The product-author-visible styling concepts (theme selection, style variant, style class, resolved style applied to a control) are the right and sufficient consumer slice to document; this is provisional until confirmed against the actual public styling surface a produced product can reach.
- **Delivery rides a republish**: As package content, the skill reaches consumers only when shipped in a republished `FS.GG.UI.Template` (publish vehicle tracked as #33-class republish) and re-pinned downstream (FS-GG/FS.GG.Templates#8); this feature's "done" is authored-and-wired-on-branch, with delivery sequenced by the epic, consistent with siblings #35/#36/#37/Feature 225.
- **Leak guard is the gate**: The repo-owned product-skill leak guard delivered by the de-leak sibling (Feature 225) is the mechanism that keeps this new skill clean; this feature authors the skill to satisfy it rather than introducing a separate guard.
- **No catalog/registry surface change**: This feature does not alter the cross-repo consumer catalog (sibling #36/Feature 224 territory) or the `fs-gg-ui-template` contract shape beyond adding shipped skill content already covered by that contract.
