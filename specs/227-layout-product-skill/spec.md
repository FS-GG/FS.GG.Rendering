# Feature Specification: fs-gg-layout consumer product-skill (app + game profiles)

**Feature Branch**: `227-layout-product-skill`

**Created**: 2026-07-01

**Status**: Draft

**Input**: User description: "start the next Rendering owned item on the coordination board." → Coordination board item [FS-GG/FS.GG.Rendering#39](https://github.com/FS-GG/FS.GG.Rendering/issues/39) — "Add an fs-gg-layout consumer product-skill (app + game profiles)" (Backlog, P1 Rendering, epic #34).

## Context

The `app` and `game` scaffold profiles **reference and use** the Yoga-backed Layout capability, yet no consumer `fs-gg-layout` product-skill ships in `template/product-skills/`. The only `fs-gg-layout` skill is the *framework-authoring* one under the repo-root agent surfaces, which the `sdd`/`none` lifecycles gate out. As a result an app/game author ships and runs layout code — HUD/gameplay region splits, responsive bounds, the `LayoutEvidence` spine the starter already exercises — with **zero skill guidance**.

Layout is the only referenced app/game capability with no matching consumer product-skill:

| Capability (app/game) | Consumer product-skill |
|---|---|
| Scene | `fs-gg-scene` |
| SkiaViewer | `fs-gg-skiaviewer` |
| Elmish | `fs-gg-elmish` |
| KeyboardInput | `fs-gg-keyboard-input` |
| Controls | `fs-gg-ui-widgets` |
| Theming | `fs-gg-styling` |
| **Layout** | **— none —** |

This feature closes that gap by authoring a first-class `fs-gg-layout` consumer product-skill, mirroring the Feature 226 `fs-gg-styling` pattern. It is expected to be **content-only** (additive consumer content under the existing `fs-gg-ui-template` contract, no version bump), exactly as Feature 226 was.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - App/game author gets first-class layout guidance (Priority: P1)

An author scaffolds an `app` (or `game`) product and needs to lay the screen out — split it into a HUD region and a gameplay/content region, keep an active item inside the gameplay region, and adapt those regions to the current output size. Today they must reverse-engineer `Product.LayoutEvidence` with no skill to consult. After this feature the scaffolded product carries an `fs-gg-layout` skill that documents the consumer slice of the Layout capability: how to compute regions responsively, the `LayoutEvidence` shape the starter uses, and where the consumer surface ends and the framework layout engine begins.

**Why this priority**: This is the entire value of the feature — it removes the single remaining "referenced capability, no guidance" gap for the two headline profiles. Without it the feature delivers nothing.

**Independent Test**: Scaffold an `app` and a `game` product (or inspect the emitted skill set) and confirm each carries a resolvable `fs-gg-layout` skill under both agent surfaces whose guidance matches the layout code the starter actually ships.

**Acceptance Scenarios**:

1. **Given** a product scaffolded with the `app` profile under any lifecycle, **When** the author lists the vendored product-skills, **Then** `fs-gg-layout` resolves under both `.agents/skills/fs-gg-layout/` and `.claude/skills/fs-gg-layout/`.
2. **Given** a product scaffolded with the `game` profile, **When** the author lists the vendored product-skills, **Then** `fs-gg-layout` resolves under both agent surfaces.
3. **Given** the `headless-scene`, `governed`, or `sample-pack` profile, **When** the product is scaffolded, **Then** `fs-gg-layout` is **not** vendored (it is scoped to the controls/interaction-bearing `app` + `game` profiles, matching where Layout is a referenced capability).
4. **Given** the authored `fs-gg-layout` SKILL.md, **When** it is read, **Then** it documents the consumer layout surface (regions, HUD/gameplay split, responsive bounds, the `LayoutEvidence` shape) and explicitly bounds itself out of the framework layout-engine internals.

---

### User Story 2 - Shipped skill catalog stays coherent (Priority: P2)

The shipped catalog `template/base/docs/skillist-reference.md` is the hand-maintained list a generated spec-kit product advertises, enforced by the Feature 224 currency/parity check. When a new consumer product-skill ships, the catalog must list it, or the currency check fails (or, worse, the catalog silently omits a shipped skill).

**Why this priority**: Required for a coherent release, but secondary to the skill itself existing. A skill that ships without a catalog row would red the currency check.

**Independent Test**: Run the Feature 224 skill-catalog currency check and confirm it passes with a `fs-gg-layout` row present and resolving.

**Acceptance Scenarios**:

1. **Given** the updated catalog, **When** the currency check runs, **Then** the `fs-gg-layout` row is present, names the `app` + `game` profiles, and resolves to a real SKILL.md whose `name:` equals `fs-gg-layout`.
2. **Given** the catalog, **When** the currency check runs, **Then** no row dangles and no shipped product-skill is unlisted.

---

### User Story 3 - Emission matrix test asserts the new set (Priority: P2)

The Feature 219 profile→skill matrix test encodes the exact framework product-skill set each profile vendors and the total source count. Adding `fs-gg-layout` changes the `app` and `game` sets (7 → 8 skills each) and the total source count (16 → 18 sources = 9 skills × 2 surfaces). The test must be updated to assert the new expectation so it stays a true positive gate rather than a stale one.

**Why this priority**: Required for the change to land green, but downstream of the wiring itself.

**Independent Test**: Run the Feature 219 emit-framework-skills test suite and confirm it passes against the new `app`/`game` 8-skill sets and the raised source-count floor.

**Acceptance Scenarios**:

1. **Given** the wired template, **When** the Feature 219 matrix test runs, **Then** the `app` and `game` expected sets each include `fs-gg-layout` (8 skills) and the per-profile derivation from `template.json` matches.
2. **Given** the wired template, **When** the Feature 219 source-count assertion runs, **Then** the framework-skill source floor reflects the added skill (≥18 sources) and every `fs-gg-layout` source is lifecycle-independent, carries a profile predicate, and emits to both agent surfaces.

---

### Edge Cases

- **Lifecycle independence**: `fs-gg-layout`, like the other consumer product-skills, must emit under `spec-kit`, `sdd`, and `none` (gated on profile, never on lifecycle). It must not carry a `lifecycle == "spec-kit"` clause.
- **Catalog suppression**: the catalog page itself ships only under the spec-kit lifecycle; adding the `fs-gg-layout` row must not change that suppression, and must not make the catalog dangle under any lifecycle.
- **Naming collision with the framework skill**: the repo-root framework-authoring `fs-gg-layout` and the new consumer `fs-gg-layout` share the `name:` id. The consumer copy must be the one that resolves in a scaffolded product; the guidance boundary must make clear the consumer skill is not the framework-engine skill.
- **Guidance drift**: the skill's examples must reflect the layout surface the starter actually ships (the `LayoutEvidence` region/bounds shape), not invented API, so a `dotnet build` of the generated product would not contradict the documented examples.
- **No version bump**: the change is additive consumer content; it must not require an `fs-gg-ui-template` version bump (content-only, like Feature 226).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The product MUST ship a consumer `fs-gg-layout` product-skill (a `SKILL.md` whose `name:` is `fs-gg-layout`) under `template/product-skills/fs-gg-layout/`, authored as consumer guidance for the Yoga-backed layout surface an app/game author uses.
- **FR-002**: The `fs-gg-layout` skill MUST cover the consumer slice — computing HUD and gameplay/content regions, splitting the screen responsively by output size, keeping an active item inside the gameplay region, and the `LayoutEvidence` shape the starter already uses — and MUST NOT document the framework layout-engine internals (that boundary stays with the upstream framework `fs-gg-layout` skill).
- **FR-003**: The template MUST vendor `fs-gg-layout` on the `app` and `game` profiles only, emitting to both `.agents/skills/fs-gg-layout/` and `.claude/skills/fs-gg-layout/`, gated on profile and independent of lifecycle (no `spec-kit` clause), matching the wiring shape used for `fs-gg-styling`.
- **FR-004**: The shipped catalog `template/base/docs/skillist-reference.md` MUST list `fs-gg-layout` with its resolved SKILL.md path and its `app, game` profile scope, such that the Feature 224 skill-catalog currency check passes with no dangling or unlisted rows.
- **FR-005**: The Feature 219 profile→skill matrix test MUST be updated so the `app` and `game` expected framework product-skill sets each include `fs-gg-layout` (8 skills each) and the framework-skill source-count floor reflects the added skill (9 skills × 2 surfaces = 18 sources).
- **FR-006**: The change MUST be additive consumer content under the existing `fs-gg-ui-template` contract and MUST NOT require a version bump (content-only, mirroring Feature 226).
- **FR-007**: The `fs-gg-layout` skill's runnable examples MUST reflect the layout surface the generated `app`/`game` starter actually ships, so the documented usage is consistent with a buildable generated product.
- **FR-008**: The template MUST ship a matching `fs-gg-product-layout` wrapper alias pair (`.agents/skills/fs-gg-product-layout/SKILL.md` and `.claude/skills/fs-gg-product-layout/SKILL.md`), each routing to the canonical `template/product-skills/fs-gg-layout/SKILL.md`, so the skill-parity check inventories the canonical body with its wrapper (no `MissingWrapper` finding) — matching the wrapper-alias invariant every shipped product-skill satisfies. (Planning resolved the spec's deferred wrapper question in scope; see research R2.)
- **FR-009**: The repo-owned enumerations that track the shipped product-skill set MUST move in lockstep with the new skill, so those gates stay true-positive rather than stale: the Feature 225 leak-guard backstop (`expectedProductSkillIds`) MUST include `fs-gg-layout` (set 8 → 9), and the Feature 204 framework-source floor MUST reflect the added sources (≥18). (This is the coherence set alongside the Feature 219 matrix of FR-005 and the Feature 224 catalog of FR-004.)

### Key Entities

- **`fs-gg-layout` consumer product-skill**: a `SKILL.md` under `template/product-skills/fs-gg-layout/` carrying the `name: fs-gg-layout` id and consumer layout guidance; vendored per profile into a scaffolded product's agent surfaces.
- **`fs-gg-product-layout` wrapper alias**: the thin `.agents/skills/fs-gg-product-layout/` + `.claude/skills/fs-gg-product-layout/` pair routing to the canonical body, required for skill-parity (see FR-008); mirrors the `fs-gg-product-styling` pattern.
- **Profile→skill emission matrix**: the data (encoded in `template.json` and asserted by the Feature 219 test) mapping each profile to the set of framework product-skills it vendors; gains `fs-gg-layout` on `app` + `game`.
- **Shipped skill catalog**: `template/base/docs/skillist-reference.md`, the hand-maintained advertised skill list enforced by the Feature 224 currency check; gains a `fs-gg-layout` row.
- **`LayoutEvidence` starter shape**: the region/bounds evidence surface (`hudRegionForSize`, `gameplayRegionForSize`, `activeGameplayBoundsForSize`, `movement/spawnUsesGameplayRegion`, `layoutEvidenceForSize`) the app/game starter already ships and the skill documents at the consumer level.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A product scaffolded with the `app` profile and one with the `game` profile each carry a resolvable `fs-gg-layout` skill under both `.agents/skills/` and `.claude/skills/`, under every lifecycle (`spec-kit`, `sdd`, `none`).
- **SC-002**: Products scaffolded with `headless-scene`, `governed`, or `sample-pack` do **not** carry `fs-gg-layout` (scope is exactly `app` + `game`).
- **SC-003**: The Feature 224 skill-catalog currency check passes with `fs-gg-layout` listed and resolving; no row dangles.
- **SC-004**: The Feature 219 emit-framework-skills test passes with the `app` and `game` sets each at 8 skills including `fs-gg-layout` and the source floor at ≥18.
- **SC-005**: The full local test suite that gated Feature 226 is green, and the change ships without an `fs-gg-ui-template` version bump (content-only diff).
- **SC-006**: Every layout usage example in the skill corresponds to the layout surface the generated `app`/`game` starter actually exposes (no invented API).

## Assumptions

- **Content-only, no bump** — like Feature 226, this is additive consumer content under the existing `fs-gg-ui-template` contract; no version-of-truth bump, tag triple, or registry flip is in scope. Delivery to consumers (a coherent-set republish) is a separate, later coordination step, not part of this feature.
- **Scope is `app` + `game`** — the two profiles that reference Layout as a used capability and already ship controls/interaction skills; the issue's matrix and the existing `fs-gg-styling` wiring establish this as the intended scope. Layout guidance is not added to `headless-scene`/`governed`/`sample-pack`.
- **Consumer slice only** — the skill documents the app/game author's layout surface and explicitly bounds out the framework layout-engine internals, which remain owned by the upstream framework `fs-gg-layout` skill.
- **Mirror the 226 pattern** — the SKILL.md structure (Scope / Public Contract / usage / Boundary / Build & Test / Related / Sources), the template.json wiring shape, the catalog row, and the matrix-test update all follow the shipped `fs-gg-styling` precedent.
- **Product wrapper alias** — the catalog notes each capability skill "also ships a `fs-gg-product-<name>` wrapper alias." Planning resolved this **in scope** (research R2): all shipped product-skills carry a matching wrapper and the skill-parity check emits a `MissingWrapper` finding for a canonical skill without one, so `fs-gg-layout` ships its `fs-gg-product-layout` wrapper pair (see FR-008). The issue's four acceptance items do not name the wrapper, but the parity invariant requires it.
- **Existing gates are the acceptance harness** — the Feature 219 matrix test and Feature 224 currency check are the authoritative gates; "done" means both are updated and green alongside the rest of the Feature 226 gating suite.
