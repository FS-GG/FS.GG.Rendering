# Feature Specification: Emit Framework Skills On Every Lifecycle (Skills Follow the Product, Not the Lifecycle)

**Feature Branch**: `219-emit-framework-skills`

**Created**: 2026-06-30

**Status**: Draft

**Input**: User description: "start the next Rendering owned item on the coordination board" → resolved to Coordination board item **FS-GG/FS.GG.Rendering#30** (P1 Rendering, parent epic FS-GG/.github#74): *Framework skills not vendored on the SDD scaffold path (lifecycle=='spec-kit' gate)*.

## Context (why this exists)

The platform's thesis is "deliver framework knowledge to building agents through curated, vendored skills." A consumer agent that scaffolded a product on the **recommended** path (`fsgg-sdd scaffold`, which sets `lifecycle=sdd`) found **zero** skills inside the product (`find` → 0 `SKILL.md`) and had to research the framework across repositories — the exact failure the platform is meant to prevent.

The framework's product-usage skills already exist and are built to be vendored. They are not emitted because **every framework-skill source is gated on the lifecycle choice** (`lifecycle == "spec-kit"`), even though framework-usage knowledge is orthogonal to which lifecycle workspace the consumer wants. The recommended SDD path and the full-stack composition path both select a non–spec-kit lifecycle, so both emit no skills. Separately, the product still ships a skill **catalog** that lists skills which were never emitted — a dangling table of contents that sends agents to files that do not exist.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Framework skills are vendored regardless of lifecycle choice (Priority: P1)

A developer or building-agent scaffolds a rendering product on the recommended SDD path (a non–spec-kit lifecycle). The generated product contains the framework-usage skills appropriate to its profile, so the agent can build against curated, in-product guidance without researching the framework across other repositories.

**Why this priority**: This is the core incoherence and the reason the board item exists. Without it the platform's central promise ("agents get curated skills") is broken for the recommended path, and every product built that way pays a cross-repo research tax. It is independently valuable on its own.

**Independent Test**: Scaffold a product with `lifecycle=sdd` (and again with `lifecycle=none`) and confirm the product contains the profile-appropriate framework `SKILL.md` files. Confirm a `lifecycle=spec-kit` scaffold still emits exactly what it did before.

**Acceptance Scenarios**:

1. **Given** the recommended SDD path (`lifecycle=sdd`) and the default profile (`app`), **When** a product is scaffolded, **Then** the product contains the `app`-profile framework skills (scene, viewer, elmish, keyboard-input, ui-widgets, symbology — symbology newly wired per FR-007) as readable `SKILL.md` files, in both the editor-agnostic and editor-specific skill locations.
2. **Given** `lifecycle=none` and any profile, **When** a product is scaffolded, **Then** the same profile-appropriate framework skills are present (the lifecycle choice does not suppress framework skills).
3. **Given** `lifecycle=spec-kit` (the default), **When** a product is scaffolded, **Then** the emitted output is unchanged from today's behavior — the same framework skills, the same Spec-Kit command skills, the same lifecycle workspace, with no duplicated, missing, or reordered files.
4. **Given** `lifecycle=sdd`, **When** a product is scaffolded, **Then** the Spec-Kit *command/lifecycle* scaffolding (the `speckit-*` command skills, the Spec Kit lifecycle workspace, the project constitution, and the agent-context files) is still **not** emitted — only the framework-usage skills follow the product.
5. **Given** a non-`app` profile (e.g. headless-scene, governed, sample-pack), **When** a product is scaffolded under `lifecycle=sdd`, **Then** exactly the framework skills that profile would have emitted under `spec-kit` are present — no more, no fewer (this set includes `fs-gg-symbology` on every scene-bearing profile, newly wired per FR-007).

---

### User Story 2 - The skill catalog never dangles (Priority: P2)

An agent reads the product's skill catalog to discover available guidance. Every entry the catalog lists resolves to a skill that is actually present in the product, so the agent never follows the catalog to a non-existent file.

**Why this priority**: A catalog that lists skills resolved to paths that were never emitted actively misleads the consumer (it looks like guidance exists when it does not). It is a real defect but secondary to actually emitting the skills (US1); once skills emit on the SDD path, the catalog must also be made truthful.

**Independent Test**: For each lifecycle×profile combination, cross-check every skill reference in the emitted catalog against the set of skill files actually present in the product; assert zero unresolved references.

**Acceptance Scenarios**:

1. **Given** any lifecycle and profile, **When** a product is scaffolded, **Then** every skill reference in the emitted skill catalog resolves to a skill file that is present in that product.
2. **Given** a scaffold configuration that emits no framework skills, **When** a product is scaffolded, **Then** the catalog does not present a list of skills that are absent (it is suppressed or reflects the empty set).

---

### User Story 3 - Scaffolded task metadata matches the product's test framework (Priority: P3)

A consumer reading the scaffolded task list sees required-skill metadata that matches the product's actual test framework, with no stale references to a framework the product does not use.

**Why this priority**: This is a harmless-but-confusing inconsistency reported alongside the main issue. It is lowest priority and its fix may not live in this repository (see Assumptions); it is captured here so it is not lost, and routed to its owner if it is not Rendering-sourced.

**Independent Test**: Inspect the scaffolded task metadata and confirm the declared required test skill matches the product's actual test framework.

**Acceptance Scenarios**:

1. **Given** a scaffolded product whose test project uses the framework's standard test library, **When** the task metadata is generated, **Then** the declared required test skill names that same library and contains no reference to an unused test framework.

---

### Edge Cases

- **No-regression on the default path**: `lifecycle=spec-kit` is the default and must stay byte-for-byte identical. The framework skills are already emitted there; decoupling them from the lifecycle gate must not cause them to be emitted twice, dropped, or reordered relative to the lifecycle workspace content that shares the same destination folders.
- **Skill destination folders under a non–spec-kit lifecycle**: the base product currently withholds the agent-skill destination folders and re-supplies them only under `spec-kit`. When framework skills emit under `sdd`/`none`, those destination folders must be created by the framework-skill emission itself, and must not be left empty, partially populated, or polluted with lifecycle-only content.
- **A present-but-unwired framework skill**: a `fs-gg-symbology` framework-skill directory exists in the source tree but is currently emitted by **no** configuration (not even under `spec-kit`). The feature must make an explicit decision: either wire it to follow its appropriate profile(s), or record that it is intentionally not vendored — it must not silently remain a dead directory that the catalog might reference.
- **Profile that emits a minimal skill set**: the smallest profile still emits at least the scene skill; confirm that even the minimal profile yields a non-empty, truthful catalog and no dangling entries.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Framework product-usage skills MUST be emitted into a scaffolded product whenever the product's profile calls for them, **independent of the lifecycle choice** — i.e. under `lifecycle=spec-kit`, `sdd`, and `none` alike.
- **FR-002**: The existing profile→framework-skill mapping MUST be preserved exactly. The profile determines *which* framework skills are vendored; the lifecycle choice MUST NOT add to or subtract from that set.
- **FR-003**: Spec-Kit *command/lifecycle* scaffolding — the Spec-Kit command skills, the Spec Kit lifecycle workspace, the project constitution, and the generated agent-context files — MUST remain emitted only under `lifecycle=spec-kit`. This feature changes the gating of framework-usage skills only, not the lifecycle workspace.
- **FR-004**: The `lifecycle=spec-kit` scaffold output MUST remain unchanged (no added, removed, duplicated, or reordered files) relative to the pre-change behavior. The change is purely additive for `sdd`/`none` and a no-op for `spec-kit`.
- **FR-005**: The emitted skill catalog MUST list only skills that are actually present in the scaffolded product, for every lifecycle×profile combination. No entry may resolve to an absent skill.
- **FR-006**: When a scaffold configuration emits no framework skills, the skill catalog MUST NOT present a list of absent skills (it is suppressed or reflects the empty set).
- **FR-007**: The feature MUST resolve the status of every framework-skill source directory present in the tree: each is either wired to emit under its appropriate profile(s) or explicitly recorded as not-vendored. No framework-skill directory may remain present-but-unreferenced by accident (covers the `fs-gg-symbology` case).
- **FR-008**: The scaffolded task metadata MUST NOT declare a required test skill that names a test framework the product does not use. If the offending metadata originates from a source owned by this repository, it MUST be corrected here; if it originates downstream, this feature MUST route it to the owning repository as a cross-repo request and record that routing.
- **FR-009**: Because this changes the observable scaffold output of the `fs-gg-ui-template` cross-repo contract, the change MUST be reflected in the cross-repo registry/compatibility record per the coordination protocol, and the originating cross-repo request (and its board item) MUST be closed/advanced on resolution.

### Key Entities *(include if feature involves data)*

- **Framework product-skill**: A unit of in-product, framework-usage guidance (a `SKILL.md` plus supporting files) that points an agent at the vendored framework contract surface. Identified by its skill name; associated with one or more product profiles.
- **Lifecycle choice**: The consumer's selection of which lifecycle workspace, if any, is scaffolded alongside the product (`spec-kit`, `sdd`, `none`). Orthogonal to framework skills after this change.
- **Product profile**: The shape of the generated product (e.g. app, headless-scene, governed, sample-pack). Determines which framework skills apply.
- **Skill catalog**: The in-product reference that enumerates available skills and their locations. Must stay consistent with the skills actually emitted.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On the recommended SDD scaffold path, a freshly scaffolded default product contains the full set of profile-appropriate framework skills (≥1 `SKILL.md`, up from 0 today) — the count goes from **0** to the profile's expected number.
- **SC-002**: For every lifecycle×profile combination, **100%** of the entries in the emitted skill catalog resolve to a skill that is present in the product (zero dangling references).
- **SC-003**: A `lifecycle=spec-kit` scaffold produces an output identical to the pre-change scaffold — **0** files added, removed, duplicated, or reordered.
- **SC-004**: An agent building a product on the SDD path can locate the framework-usage guidance entirely from within the generated product — **0** other repositories need to be consulted to find the framework skills. (Evidenced as a proxy by the profile's full expected skill-set resolving in-product — see US1/US2 independent tests; this is an outcome metric, not a separately gated assertion.)

## Assumptions

- **Resolved board item**: "the next Rendering owned item" is taken to be FS-GG/FS.GG.Rendering#30 — the lowest-numbered, board-ordered P1 Rendering item in the shared Backlog/Composition bucket (siblings #31, #32 deferred), per the user's confirmation.
- **Change classification**: This is treated as a **Tier 1 (contracted)** change to the `fs-gg-ui-template` cross-repo contract — it alters the contract's observable scaffold output — but it is **additive and surface-neutral**: no template parameter is added, removed, or renamed; the `spec-kit` path is a no-op. The cross-repo registry/compatibility record is updated as part of resolution (FR-009).
- **Skills are advisory, not gates**: Vendoring more skills changes only what guidance ships in the product; it introduces no mandatory skill-loading step and blocks no build or task. A product still builds and ships without consulting any skill.
- **Catalog truthfulness over completeness**: For US2, the governing requirement is that the catalog never lists an absent skill. Whether that is achieved by suppressing the catalog when empty, or by scoping it to the emitted set, is an implementation choice deferred to planning; either satisfies FR-005/FR-006.
- **`fs-gg-symbology` default**: Absent a contrary decision in planning, the conservative default is to preserve current emission behavior (it is not emitted today), but the feature must record that decision explicitly rather than leave the directory silently unreferenced (FR-007).
- **Test-skill metadata ownership (§5.2 / FR-008)**: The offending `xunit` required-skill / task metadata is **not present anywhere in this repository's source tree** (verified by search), so it is generated downstream by the lifecycle/task tooling rather than by the Rendering template. The default plan is therefore to route this papercut to its owning repository as a cross-repo request; it is corrected in-repo only if a Rendering-owned source is found to emit it.
- **Skill content is unchanged**: This feature changes *when/whether* existing skills are emitted, not the *content* of any skill; no new framework guidance is authored here.
- **No new product code**: This is a template/scaffold-emission and cross-repo-coordination feature; no `.fs`/`.fsi` public surface is added, removed, or changed.
