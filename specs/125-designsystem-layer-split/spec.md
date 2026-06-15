# Feature Specification: Design-System Layer Split (Workstream D, Phase D1)

**Feature Branch**: `125-designsystem-layer-split`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in fs.gg" → Workstream **D, Phase D1** of `docs/reports/2026-06-15-11-34-missing-features-implementation-plan.md` — the behaviour-neutral assembly/layer split that carves the design-system primitives and the default theme out of the monolithic `Controls` package, creating the foundation the design-system enrichment (Workstream F) and concrete themes (Workstream D2: Ant/Fluent/Material) build on.

## Overview

The product committed (in `docs/product/layering.md` and `module-map.md`) to a **four-layer UI architecture**: a semantic **controls catalog**, the **design-system primitives** that describe how controls are styled (token model, theme records, visual-state rules, the style resolver), the **themes** that supply concrete values, and optional **design-specific kits**. Today only the first layer is a real boundary — the design-system primitives and the default Light/Dark theme are **embedded inside the `Controls` package**. Anyone who wants to author a theme, target the design system, or reason about styling separately from the 52-control catalog has to take a dependency on the whole catalog, and the "one semantic control set, many themes" rule exists only on paper.

This feature makes the design-system and default-theme layers **real, separately-referenceable packages** — **without changing any rendered behaviour**. It is a pure structural move: every type, value, token, and resolver that exists today continues to exist and behave identically; they simply live in the layer they belong to. The split is the prerequisite that lets Workstream F generate an enriched token taxonomy *into* the design-system layer and lets Workstream D2 add concrete themes that depend on the design system *without* depending on the catalog.

Because every control reads the theme, this is the highest-blast-radius change in the plan, so it is deliberately scoped as **behaviour-neutral**: the success bar is "nothing a user or consumer can observe changed, but the layer boundaries are now physical." Concrete themes, kits, the enriched token taxonomy, and the central resolver redesign are explicitly **out of scope** here (they are D2 / D3 / F).

This is **library/architecture work**, not a new end-user feature. Its consumers are framework users — theme authors, app builders, and the project's own sample apps and template.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Depend on the design system without the whole catalog (Priority: P1)

A theme author (or any consumer who only needs styling primitives) wants to reference the design-system layer — the token model, the `Theme` record, resolved-style and visual-state vocabulary, and the style resolver — **without** pulling in the 52-control catalog. Today that is impossible because those types live inside the controls package. After this feature, the design-system primitives are their own package whose only dependency is the scene primitives, and a consumer can reference it standalone.

**Why this priority**: This is the MVP and the whole point of the phase. The moment the design-system primitives are a standalone, catalog-free package, the layering rule the product committed to becomes physically true, and Workstreams F and D2 are unblocked. Everything else in this feature protects or completes this slice.

**Independent Test**: Create (or point at) a minimal consumer that references only the design-system package, and confirm it can name and use the token model, the `Theme` record, the resolved-style / visual-state types, and the style resolver — and that it does **not** transitively depend on the controls catalog. A dependency-direction check confirms the catalog is absent from its closure.

**Acceptance Scenarios**:

1. **Given** the split is complete, **When** a consumer references only the design-system package, **Then** all design-system primitives (token model, `Theme`, `ResolvedStyle`, `StyleVariant`, `VisualState`, the style resolver) are available and the controls catalog is **not** in its dependency closure.
2. **Given** the design-system package, **When** its dependencies are inspected, **Then** it depends only on the scene primitives (no dependency on the controls catalog or any theme package).
3. **Given** the `Theme` record after the move, **When** its roles are inspected, **Then** it exposes the **Success** and **Warning** roles (which the underlying tokens already define) alongside the existing roles.

---

### User Story 2 - Swap the default theme as its own layer (Priority: P2)

A consumer wants the default Light/Dark theme — including the mode-plus-accent derivation — to be a **separate layer** they can reference, replace, or sit alongside future themes, rather than something welded into the catalog. After this feature the default theme is its own package that depends only on the design-system layer.

**Why this priority**: Separating the default theme from the primitives proves the theme layer is a genuine seam (the same seam Ant/Fluent/Material will plug into in D2), not just a one-off. It is P2 because the primitives split (US1) is the load-bearing move; the theme split completes the picture and makes the "many themes" future concrete.

**Independent Test**: Confirm the default-theme package depends only on the design-system package (not the catalog), and that a consumer can obtain the Light and Dark themes and the accent derivation from it.

**Acceptance Scenarios**:

1. **Given** the split is complete, **When** the default-theme package is inspected, **Then** it depends only on the design-system package and provides the Light theme, the Dark theme, and the mode-plus-accent derivation.
2. **Given** a consumer that wants styling values, **When** it references the default-theme package, **Then** it obtains the same Light/Dark values that ship today, unchanged.

---

### User Story 3 - Nothing a user or consumer can observe changed (Priority: P1)

The project's own apps, tests, sample gallery, and any downstream consumer must behave **exactly** as before the split. Rendered output, accessibility contract, and interactive behaviour of every control are identical; the only change is where types physically live (a disclosed namespace relocation), and the framework's quality gates stay green.

**Why this priority**: This is co-critical with US1. A layer split that subtly changes behaviour is a defect, not a refactor — the entire value of doing D1 as its own phase is the guarantee that themes/kits/F build on a verified-identical base. The high blast radius (every control touches the theme) makes this guarantee the thing that has to be proven, not assumed.

**Independent Test**: Run the full existing automated suite — it passes unchanged. Render the reference scenes / gallery and confirm output is identical to pre-split. Run the public-surface drift gate and confirm it is green with the new package baselines and the regenerated (smaller) controls baseline committed in the same change.

**Acceptance Scenarios**:

1. **Given** the split is complete, **When** the full existing test suite runs, **Then** every test that passed before passes after, with no test deleted to make this true.
2. **Given** identical input, **When** the reference scenes / gallery are rendered before and after the split, **Then** the rendered output and accessibility contract are identical.
3. **Given** the two new packages and the reduced controls package, **When** the public-surface drift gate runs, **Then** it is green because new baselines are added and the controls baseline is regenerated and committed in the same change.
4. **Given** the namespace relocation of design-system / theme types, **When** a consumer or maintainer looks for the rationale and migration guidance, **Then** a decision record exists and the template and bridge/migration docs reflect the new layout.

---

### Edge Cases

- **Compile order / circularity**: the controls catalog must consume design-system types from the design-system package; the design system must never depend back on the catalog or on a theme. How is the dependency direction kept acyclic (design-system → scene; theme → design-system; controls → design-system)?
- **A type publicly available today goes missing**: if any type/value that consumers could reference before the split is dropped (rather than relocated), that is a regression — how is "everything that was public is still public, possibly under a new namespace" verified?
- **Drift gate on untracked baselines**: the gate fails on untracked/changed baselines; what guarantees the new-package baselines and regenerated controls baseline land in the *same* change rather than as a follow-up?
- **Theme record gains roles**: adding `Success`/`Warning` to the `Theme` record changes its shape — is that purely additive and free of behaviour change for existing render paths?
- **Sample gallery and template**: both reference the relocated types; how is each updated so the build and the gallery's evidence path stay green?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The design-system primitives — the token model, the `Theme` record, `ResolvedStyle`, `StyleVariant`, `VisualState`, and the style resolver — MUST be published as a **standalone design-system package** whose only dependency is the scene primitives, and MUST NOT be defined inside the controls catalog package.
- **FR-002**: The default Light/Dark theme values and the mode-plus-accent derivation MUST be published as a **separate default-theme package** that depends only on the design-system package.
- **FR-003**: The controls catalog package MUST consume the design-system types from the design-system package (not define them), and MUST add a reference to that package; its rendered behaviour MUST be unchanged.
- **FR-004**: The `Theme` record MUST gain the **Success** and **Warning** roles (already present in the underlying tokens) as part of this move; the addition MUST be purely additive (no change to existing role values or render output).
- **FR-005**: The behaviour, accessibility contract, and rendered output of every existing control MUST be **identical** before and after the split (the change is behaviour-neutral).
- **FR-006**: The full existing automated test suite MUST pass after the split with no test removed, weakened, or newly skipped to accommodate the move.
- **FR-007**: The public-surface drift gate MUST be green: a baseline row + committed baseline for each new package MUST be added, and the controls baseline regenerated, **all within this same change** (never deferred).
- **FR-008**: The namespace relocation of design-system and theme types MUST be recorded as a **decision record** in `docs/product/decisions/`, and the project template and bridge/migration documentation MUST be updated to reflect the new package layout.
- **FR-009**: The solution definition MUST include the two new projects, and the compile order across the three packages MUST be preserved so the build is green at each step.
- **FR-010**: The new packages MUST follow the established `FS.GG.UI.*` identity scheme (`FS.GG.UI.DesignSystem`, `FS.GG.UI.Themes.Default`).
- **FR-011**: The `module-map.md` rows for the design-system and theme layers MUST move from "embedded in `Controls`" to "owned assembly," reflecting the now-physical boundary.
- **FR-012**: Every type and value that is publicly referenceable today MUST remain publicly referenceable after the split (relocated namespace permitted); none MUST be dropped or have its capability reduced.

### Key Entities *(include if feature involves data)*

- **Design-system package** (`FS.GG.UI.DesignSystem`): the new layer holding the token model, the `Theme` record (now including Success/Warning), `ResolvedStyle`, `StyleVariant`, `VisualState`, and the style resolver. Depends only on the scene primitives.
- **Default-theme package** (`FS.GG.UI.Themes.Default`): the new layer holding the Light/Dark theme values, the token source + generation, and the mode-plus-accent derivation. Depends only on the design-system package.
- **Controls catalog package** (`FS.GG.UI.Controls`, refactored): the existing catalog, now consuming design-system types from the design-system package rather than defining them. Behaviour identical.
- **Layering contract**: the acyclic dependency rule made physical — design-system → scene; theme → design-system; controls → design-system. The thing this feature converts from documentation into compiled structure.
- **Surface baselines**: the committed public-API snapshots the drift gate checks; this feature adds two (new packages) and regenerates one (reduced controls).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the previously-passing automated tests pass after the split, with **zero** tests deleted, weakened, or newly skipped to achieve it.
- **SC-002**: A consumer can reference the design-system package and use all its primitives **without** the controls catalog appearing anywhere in its dependency closure (verified by a dependency-direction check).
- **SC-003**: The rendered output and accessibility contract of the reference scenes / sample gallery are **identical** before and after the split (byte-identical evidence where the render path is deterministic).
- **SC-004**: The public-surface drift gate passes with committed baselines for both new packages and the regenerated controls baseline — green in the **same** change that performs the move.
- **SC-005**: **Zero net loss** of public capability: every type/value publicly available before the split is still available afterwards (possibly under a new namespace); a before/after public-surface comparison shows relocations only, no removals.
- **SC-006**: The three packages form an **acyclic** dependency graph in the committed solution (design-system → scene; theme → design-system; controls → design-system), confirmed by a successful build with no dependency cycle.

## Assumptions

- **No external consumers yet (pre-1.0, in-repo only).** The only consumers of the relocated types today are in this repository (the sample gallery, the template, the framework's own tests). The reasonable default is therefore a **clean namespace relocation recorded in a decision record**, rather than shipping backward-source-compat shims (type aliases / type-forwarding). If a future external-consumer requirement emerges, compat shims can be added behind that requirement; they are out of scope here.
- **"Behaviour-neutral" is the hard constraint**, verified by the existing suite plus render-identity evidence — not by adding new behaviour. Any behaviour change discovered to be necessary is split into its own Tier-1/2 feature, never folded into this move (per the constitution's change-classification discipline).
- **Concrete themes, kits, the enriched token taxonomy, and the central resolver redesign are out of scope.** This feature is purely the structural split (plan tasks D1.1–D1.5). Ant/Fluent/Material themes are D2, kits are D3, token taxonomy + policy + `resolve` redesign are Workstream F.
- **Token source stays with the default theme.** The `design-tokens.tokens.json` source and its generation tooling move with the default-theme layer; the *generated* token model is what the design-system package exposes. (Workstream F later relocates/expands generated tokens into the design-system package — not here.)
- **Package identity follows Stage R8's `FS.GG.UI.*` scheme**, consistent with the existing package-identity decision record.
- **The drift gate and solution updates are part of this change, not a follow-up** — sequencing the baseline regeneration separately is the single most likely way to redden CI, so it is treated as in-scope and atomic with the move.
