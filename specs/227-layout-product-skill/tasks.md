---
description: "Task list for fs-gg-layout consumer product-skill (app + game profiles)"
---

# Tasks: fs-gg-layout consumer product-skill (app + game profiles)

**Input**: Design documents from `/specs/227-layout-product-skill/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/skill-delivery-contract.md, quickstart.md

**Tests**: No new test project. This is a **content/config-only** feature (mirrors Feature 226): no `src/**` change, no `.fsi`/surface-baseline, no version bump. Verification rides the **existing** repo gates (Feature 219 emission matrix, Feature 224 catalog currency, Feature 225 leak guard, Feature 204 lifecycle floor, skill-parity) plus a live scaffold observation. Tasks that "run a gate" are therefore verification tasks against pre-existing suites, not new-test authoring.

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P2) so each is independently implementable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in every task

## Path Conventions

Content/config only — no `src/`. Touch-points are the template product-skill tree (`template/product-skills/`), the repo-root agent-surface wrappers (`.agents/skills/`, `.claude/skills/`), the template manifest (`.template.config/template.json`), the shipped catalog (`template/base/docs/`), three existing Package.Tests gates, and the generated parity report (`docs/reports/`). Every path below is confirmed against the shipped `fs-gg-styling` (Feature 226) diff per plan.md.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline and confirm the sibling precedent before touching any file.

- [X] T001 Establish the no-regression baseline across EVERY test project: `dotnet fsi scripts/baseline-tests.fsx --out specs/227-layout-product-skill/readiness/baseline.md` (globs `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**/*.Tests`; the pre-existing Debug-lane `FS.GG.UI.Build`-not-built red and the NU1403 FSharp.Core lockfile workaround from memory may apply — record any pre-existing reds here so they are not mistaken for regressions at merge)
- [X] T002 [P] Confirm the shipped `fs-gg-styling` (Feature 226) precedent as the copy-exact template: read `template/product-skills/fs-gg-styling/SKILL.md`, `.agents/skills/fs-gg-product-styling/SKILL.md`, `.claude/skills/fs-gg-product-styling/SKILL.md`, and the `fs-gg-styling` blocks in `.template.config/template.json`, `template/base/docs/skillist-reference.md`, and the three Package.Tests gates; record the concrete diff shape under `specs/227-layout-product-skill/readiness/naming.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the wiring/touch-point map every user story edits against, capture the pre-authoring scaffold state, and read the real `LayoutEvidence` starter surface so US1's examples reference shipped API (FR-007) — not invented names.

**⚠️ CRITICAL**: No user-story work can begin until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** This feature ships no runtime code, so the "real app" to drive is the **scaffold path** (plan.md standing-assumption note). T004 is that early live smoke run: produce the artifact and observe the emitted skill set BEFORE authoring, so the "skill is absent today, present after wiring" delta is proven empirically — not assumed from a green test (the Feature 175 lesson: deterministic tests pass while the produced artifact is wrong).

- [X] T003 Build the touch-point / wiring map that every user story depends on: enumerate the exact edits (skill body, wrapper pair, two `template.json` sources, catalog row, Feature 225 backstop, Feature 219 matrix + floor, Feature 204 floor, parity regen, cross-link) with confirmed line anchors from T002, into `specs/227-layout-product-skill/readiness/wiring-map.md`
- [X] T004 **Early live smoke run (scaffold-before)**: scaffold `app` and `game` products from the current template (or, if `dotnet new` is unavailable in-environment, derive the emitted set from `.template.config/template.json`) and record that `fs-gg-layout` is **absent** under both `.agents/skills/` and `.claude/skills/` today, into `specs/227-layout-product-skill/readiness/scaffold-before.md` (disclose any env substitution per the Feature-168 evidence rules)
- [X] T005 Read the real consumer layout surface so US1 examples reference shipped API only: `template/base/src/Product/LayoutEvidence.fs` (and the `LayoutEvidence` re-export in `template/base/src/Product/Program.fs`) plus the public `FS.GG.UI.Layout` surface — record the exact member names (`hudRegionForSize`, `gameplayRegionForSize`, `activeGameplayBoundsForSize`, `movement/spawnUsesGameplayRegion`, `layoutEvidenceForSize`) and the engine-internal names to exclude (`Layout.evaluate`, `Defaults.layoutNode`) into `specs/227-layout-product-skill/readiness/layout-surface.md`

**Checkpoint**: Touch-point map confirmed, pre-authoring scaffold state captured, and the shipped layout surface documented — user-story implementation can begin.

---

## Phase 3: User Story 1 - App/game author gets first-class layout guidance (Priority: P1) 🎯 MVP

**Goal**: A scaffolded `app`/`game` product carries a resolvable `fs-gg-layout` consumer skill (under both agent surfaces, every lifecycle) whose guidance matches the layout code the starter actually ships and explicitly bounds out the framework layout engine.

**Independent Test**: Scaffold an `app` and a `game` product (or inspect the emitted skill set) and confirm each carries a resolvable `fs-gg-layout` SKILL.md under both `.agents/skills/fs-gg-layout/` and `.claude/skills/fs-gg-layout/`, whose `name:` is `fs-gg-layout` and whose examples match `LayoutEvidence.fs`.

### Implementation for User Story 1

- [X] T006 [US1] Author the consumer skill body `template/product-skills/fs-gg-layout/SKILL.md`: frontmatter `name: fs-gg-layout` + one-line consumer `description:`; sections mirroring `fs-gg-styling` (Scope · Consumer surface · Compute HUD + gameplay/content regions responsively by output size · Keep an active item inside the gameplay region · The `LayoutEvidence` shape · Boundary · Build & Test Commands · Generated Product · Related · Sources). Use only the member names recorded in T005 (FR-002, FR-007). Author leak-clean from the first draft (FR / Feature 225): no `Feature \d+` / `spec-\d+` stamps, no `readiness`/`package-feed`/`.gitignore`/`BaseOutputPath` framework-evidence tokens
- [X] T007 [US1] Author `Boundary` section in `template/product-skills/fs-gg-layout/SKILL.md` that names the exclusion (Yoga layout-**engine** internals `Layout.evaluate`, `Defaults.layoutNode`, `.fsi`/surface-baselines) and points authority to the upstream framework `fs-gg-layout` (`src/Layout/skill/SKILL.md`) — resolves the shared-`name:` collision edge case (part of the same file as T006; sequential)
- [X] T008 [P] [US1] Author the Codex wrapper `.agents/skills/fs-gg-product-layout/SKILL.md`: `name: fs-gg-product-layout` + matching `description:`, thin body routing to `../../../template/product-skills/fs-gg-layout/SKILL.md` (mirror `fs-gg-product-styling`; FR-008)
- [X] T009 [P] [US1] Author the Claude wrapper `.claude/skills/fs-gg-product-layout/SKILL.md`: `name: fs-gg-product-layout` + matching `description:`, thin body routing to `../../../template/product-skills/fs-gg-layout/SKILL.md` (mirror `fs-gg-product-styling`; FR-008)
- [X] T010 [US1] Wire emission in `.template.config/template.json`: append two `sources` entries, each `condition: "(profile == \"app\" || profile == \"game\")"`, `source: "template/product-skills/fs-gg-layout/"`, `target` = `.agents/skills/fs-gg-layout/` and `.claude/skills/fs-gg-layout/` respectively — no `lifecycle` clause (FR-003; lifecycle-independent edge case)
- [X] T011 [US1] Verify receipt (C1): scaffold `app` and `game` under `spec-kit`/`sdd`/`none` (or fall back to the Feature 219 `template.json` derivation) and confirm `fs-gg-layout/SKILL.md` resolves under **both** agent surfaces for app+game and is **absent** for `headless-scene`/`governed`/`sample-pack`; record the scaffold-after transcript under `specs/227-layout-product-skill/readiness/scaffold-after.md` (SC-001, SC-002) and the US1 content hand-read under `specs/227-layout-product-skill/readiness/us1-read.md`

**Checkpoint**: `fs-gg-layout` exists, is vendored to app+game across all lifecycles, and reads as consumer-slice guidance — MVP is independently demoable. (Feature 219/224/225 gates will now red until US2/US3 update their enumerations; that is expected and closed below.)

---

## Phase 4: User Story 2 - Shipped skill catalog stays coherent (Priority: P2)

**Goal**: The hand-maintained catalog lists `fs-gg-layout` so the Feature 224 currency check passes with no dangling or unlisted rows.

**Independent Test**: Run the Feature 224 currency check and confirm it passes with a `fs-gg-layout` row present, resolving to a real SKILL.md whose `name:` equals `fs-gg-layout`, scoped `app, game`.

### Implementation for User Story 2

- [X] T012 [US2] Add the catalog row to the "Product capability skills" table in `template/base/docs/skillist-reference.md`: `fs-gg-layout` | `.agents/skills/fs-gg-layout/SKILL.md` | `app, game` (mirror the `fs-gg-styling` row; FR-004)
- [X] T013 [US2] Verify catalog currency: `dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency` — confirm green with the `fs-gg-layout` row resolving and no row dangling / no shipped skill unlisted (SC-003); record transcript under `specs/227-layout-product-skill/readiness/gate-feature224.md`

**Checkpoint**: Catalog is coherent and Feature 224 is green.

---

## Phase 5: User Story 3 - Emission matrix test asserts the new set (Priority: P2)

**Goal**: The Feature 219 matrix, Feature 204 floor, and Feature 225 backstop assert the new 9-skill / 18-source set so they stay true-positive gates rather than stale.

**Independent Test**: Run the Feature 219, 204, and 225 suites and confirm they pass against the `app`/`game` 8-skill sets, the raised source-count floor (≥18), and the 9-id product-skill backstop including `fs-gg-layout`.

### Implementation for User Story 3

- [X] T014 [P] [US3] Add `"fs-gg-layout"` to `expectedProductSkillIds` in `tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs` (backstop set 8 → 9); update any "covers the N expected ids" count label accordingly (FR-009)
- [X] T015 [P] [US3] Update `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs`: add `fs-gg-layout` to the `app` and `game` rows of `expectedFrameworkSkills` (7 → 8 skills each) and raise the framework-skill source-count floor `>=16` → `>=18`, updating the `8×2=16` comment to `9×2=18`
- [X] T016 [P] [US3] Raise the framework-source floor `>=16` → `>=18` (and its comment) in `tests/Package.Tests/Feature204LifecycleTemplateTests.fs` (FR-009)
- [X] T017 [US3] Verify the emission/floor/backstop gates: `dotnet test tests/Package.Tests --filter Feature219EmitFrameworkSkills`, `--filter Feature204LifecycleTemplate`, `--filter Feature225ProductSkillVocabulary` — confirm all green: app+game sets at 8 skills incl. `fs-gg-layout`, sources ≥18, every `fs-gg-layout` source lifecycle-independent + profile-predicated + both surfaces, and zero leak findings (SC-004; C3); record transcripts under `specs/227-layout-product-skill/readiness/gate-feature219-204-225.md`

**Checkpoint**: All enumeration gates assert the new set and are green.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Regenerate the generated proof, add discoverability, refresh agent context, and prove the full release-gate suite green with no version bump.

- [X] T018 [P] Add the scene↔layout cross-link: a `[[fs-gg-layout]]` pointer in the `Related` section of `template/product-skills/fs-gg-scene/SKILL.md` and the reciprocal `[[fs-gg-scene]]` in the new body (confirm direction against the shipped skills; add a `Related` pointer from `fs-gg-ui-widgets` only if the parity/leak read shows an asymmetry)
- [X] T019 Regenerate the skill-parity report (never hand-edit): `dotnet fsi scripts/check-agent-skill-parity.fsx --fail-on High` — confirm `Overall status: Passed`, zero High+ findings, `fs-gg-layout` inventoried as canonical with its `fs-gg-product-layout` wrapper (canonical +1, wrapper +2); the run writes `docs/reports/skills-parity.md`
- [X] T020 [P] Refresh the managed SPECKIT plan-pointer / agent context in `CLAUDE.md` (agent-context step) so it references `specs/227-layout-product-skill/plan.md`
- [X] T021 Run the full Feature-226 release-gate suite green and confirm content-only delivery: the four Package.Tests filters from quickstart step 1 + skill-parity, then `git diff --stat` confirming **no** `fs-gg-ui-template` version-of-truth edit, no `src/**` change, no `.fsi`/surface-baseline (SC-005; C4)
- [X] T022 [P] Record the success-criteria mapping (SC-001…SC-006 → proving artifact) under `specs/227-layout-product-skill/readiness/success-criteria.md`, per the quickstart "Success mapping" table

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories (T005's surface read gates US1's example accuracy; T003's map gates every edit).
- **User Stories (Phase 3–5)**: All depend on Foundational completion.
  - **US1 (P1)** is the MVP and should land first: it introduces the skill + wiring. Adding the wiring (T010) intentionally reds Feature 219/224/225 until US2/US3 close them.
  - **US2 (P2)** and **US3 (P2)** are independent of each other and can run in parallel once US1's skill body/wiring exist (both only touch enumerations that reference the now-existing skill). Together they return the suite to green.
- **Polish (Phase 6)**: Depends on US1–US3 (T019 parity regen and T021 full-suite green require all edits in place).

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational. No dependency on US2/US3.
- **US2 (P2)**: Depends on US1's skill body existing (catalog row must resolve to a real SKILL.md). Independent of US3.
- **US3 (P2)**: Depends on US1's wiring existing (matrix derives from `template.json`). Independent of US2.

### Within Each User Story

- US1: body (T006) → boundary in same file (T007) → wrappers (T008, T009 parallel) → wiring (T010) → receipt verification (T011).
- US2: catalog row (T012) → verify (T013).
- US3: three enumeration edits (T014, T015, T016 parallel — different files) → verify (T017).

### Parallel Opportunities

- **Setup**: T002 runs alongside/after T001.
- **US1**: T008 and T009 (the two wrappers, different files) run in parallel.
- **US3**: T014, T015, T016 (three different test files) run in parallel.
- **US2 ∥ US3**: once US1 exists, the whole of US2 and US3 can proceed concurrently.
- **Polish**: T018, T020, T022 are `[P]` (different files); T019 and T021 are serial gate runs.

---

## Parallel Example: User Story 1 & downstream

```bash
# US1 — author the two wrapper aliases together (different files):
Task: "Author .agents/skills/fs-gg-product-layout/SKILL.md"
Task: "Author .claude/skills/fs-gg-product-layout/SKILL.md"

# After US1 lands, run US2 and US3 enumeration edits concurrently:
Task: "Add fs-gg-layout catalog row in template/base/docs/skillist-reference.md"          # US2
Task: "Add fs-gg-layout to Feature225ProductSkillVocabularyTests.fs expectedProductSkillIds" # US3
Task: "Add fs-gg-layout to Feature219EmitFrameworkSkillsTests.fs + raise floor 16->18"     # US3
Task: "Raise framework-source floor 16->18 in Feature204LifecycleTemplateTests.fs"          # US3
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (baseline + precedent read).
2. Complete Phase 2: Foundational — including the **early live smoke run** (T004) that captures the pre-authoring scaffold state before any edit.
3. Complete Phase 3: US1 — author the skill body + wrappers + wiring.
4. **STOP and VALIDATE**: scaffold app+game and confirm `fs-gg-layout` resolves under both surfaces, all lifecycles (T011). This is the shippable increment: the capability gap is closed.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → skill exists and is vendored (MVP) — but the release-gate suite is intentionally red until enumerations catch up.
3. US2 + US3 (parallel) → catalog + matrix/floor/backstop assert the new 9-skill / 18-source set → suite back to green.
4. Polish → parity regen, cross-link, agent context, full-suite + no-bump proof.

### Content-only guardrails (do not violate)

- **No** `src/**` change, **no** new `.fsi`/surface-baseline, **no** `fs-gg-ui-template` version bump, tag triple, or registry flip (C4 / SC-005). Consumer delivery is the separate epic-#34 republish.
- Author the body leak-clean from the first draft (Feature 225 scans it the moment it exists via dynamic discovery).
- Every code example must correspond to the shipped `LayoutEvidence` surface (T005), never invented API (FR-007 / SC-006).

---

## Notes

- `[P]` tasks = different files, no dependencies.
- `[Story]` label maps each task to its user story for traceability; Setup/Foundational/Polish carry no story label.
- Verification tasks (T011, T013, T017, T019, T021) run **existing** gates — this feature authors no new test.
- Record every transcript / hand-read under `specs/227-layout-product-skill/readiness/`.
- Commit after each task or logical group.
- Stop at any checkpoint to validate a story independently.
