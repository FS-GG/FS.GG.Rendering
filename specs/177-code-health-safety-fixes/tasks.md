---
description: "Task list for Code Health — Quick Safety Fixes (Refactoring Phase 0)"
---

# Tasks: Code Health — Quick Safety Fixes (Refactoring Phase 0)

**Input**: Design documents from `/specs/177-code-health-safety-fixes/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/README.md, quickstart.md

**Tests**: This feature *is* about test integrity (US2) and byte-identity (US3, FR-006). The test tasks
below are not optional scaffolding — they are the deliverable assertions called for by FR-003/FR-004
and FR-006. No new TDD test project is introduced.

**Organization**: Tasks are grouped by user story so each of the three Phase 0 items can be
implemented, verified, and (if desired) shipped independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in each description.

## Path Conventions

Multi-project F# UI framework (`net10.0`). This feature edits four files in place:
`src/Controls/RetainedRender.fs`, `src/Layout/Layout.fs`,
`tests/Controls.Tests/Feature093ParityTests.fs`, `tests/Controls.Tests/TypedMigrationTests.fs`.
No `.fsi` file and no surface-area baseline is modified (Tier 2).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish a trustworthy pre-change baseline so byte-identity, golden, and no-regression
invariants are checkable afterward.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every** test
> project and record the full red/green set, so pre-existing failures are known up front and not
> mistaken for regressions at merge. Do NOT hand-pick a subset of projects.

- [X] T001 Confirm a clean working tree (`git status` shows only the `specs/177-code-health-safety-fixes/` design docs) so the FR-002 golden review can read a meaningful diff later.
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/177-code-health-safety-fixes/readiness/baseline.md` (globs every `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**/*.Tests` — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge).
- [X] T003 [P] Record the pre-change FR-006 byte-identity reference: `grep -n 'rev=150\|Revision = 150' src/Layout/Layout.fs` and capture the composed `IntrinsicQuery.QueryIdentity` and `cacheEntry.EntryId` output for representative inputs (used to prove byte-identity in US3).
- [X] T004 [P] Record the FR-001 / Tier-2 starting evidence: `grep -n '1469598103934665603UL' src/Controls/RetainedRender.fs` and `grep -n 'feature159Hash\|feature159ContentIdentity' src/Controls/RetainedRender.fsi` (confirm `feature159Hash` private / `feature159ContentIdentity` `val internal`).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the plan's root-cause hypotheses against a real build+test run **before** banking
any fix.

> **⚠️ "Live smoke run" maps to build+test for this Tier-2 change (per plan.md standing-assumption).**
> This change set has **no app-visible behavior**: the only runtime change is an internal, private
> hash *value* that feeds layer reuse/promotion identity comparisons — no different pixels, layout, or
> user-facing output. Per the plan, the honest smoke for this feature is a clean `dotnet build` + full
> `dotnet test` green run with the Feature 159 identity/reuse/promotion/readiness suites intact, plus
> an explicit golden/artifact diff review. This run is scheduled here (early) and repeated at the end.

- [X] T005 Confirm the root-cause map from research.md against the live tree: re-verify line numbers and the surrounding fold/cache code at `src/Controls/RetainedRender.fs:851`, `src/Layout/Layout.fs:839/847/964/974`, `tests/Controls.Tests/Feature093ParityTests.fs:77`, and `tests/Controls.Tests/TypedMigrationTests.fs:555` (file/line references may have shifted since report time per spec Assumption).
- [X] T006 **Early smoke run (build+test)**: run `dotnet build` then `dotnet test`, confirming green and that the Feature 159 suites (`Feature159IdentitySplitTests`, `Feature159ReuseCounterTests`, `Feature159PromotionEvidenceTests`, `Feature159ReadinessPackageTests`) execute. This validates the "safe base" premise before any edit. (Already partly covered by T002's baseline; T006 is the explicit confirmation gate the plan requires before fixes begin.)
- [X] T007 [P] Confirm no golden depends on an absolute hash: `grep -rn` for any 15+ digit `ContentId` literal across `tests/` and `specs/` (expect no hits) so the US1 change is known to be relation-only before it is made.

**Checkpoint**: Baseline green, root-cause map re-confirmed against the live tree, no absolute-hash golden present — user-story work can begin.

---

## Phase 3: User Story 1 - Trustworthy retained-render content hash (Priority: P1) 🎯 MVP

**Goal**: Resolve the `feature159Hash` offset typo so the retained-render content-identity hash behaves
like every other FNV-1a accumulator in the repo, with no silently invalidated golden.

**Independent Test**: After the edit the seed equals `0xcbf29ce484222325UL`, the old literal is gone,
the Feature 159 relational suites stay green, and any persisted-evidence change is an explicitly
reviewed diff (not silent).

### Implementation for User Story 1

- [X] T008 [US1] Change the FNV-1a offset seed at `src/Controls/RetainedRender.fs:851` from `1469598103934665603UL` to `0xcbf29ce484222325UL` (hex form, matching `Composition.fs:157`, `Control.fs:2454`, `Control.fs:2830`). Do NOT touch the FNV prime `1099511628211UL` or the fold shape. Leave `src/Controls/RetainedRender.fsi` unchanged.
- [X] T009 [US1] Verify the edit: `grep -rn '0xcbf29ce484222325UL' src/Controls/RetainedRender.fs` (expect line 851) and `grep -rn '1469598103934665603UL' src/` (expect NO matches).
- [X] T010 [US1] Run the Feature 159 relational suites: `dotnet test --filter 'FullyQualifiedName~Feature159'` — confirm identity-split, reuse-count, promotion, and readiness suites stay green (they assert relations between freshly computed identities, so shifting the seed preserves every relation).
- [X] T011 [US1] **FR-002 golden gate**: run `git status specs/**/readiness/` and `git diff --stat specs/`. Any regenerated evidence that embeds an absolute hash MUST be reviewed and explicitly accepted (never silent). Confirm `git diff -- src/Controls/RetainedRender.fsi` is empty (Tier 2).

**Checkpoint**: `feature159Hash` resolved to the canonical basis with zero ambiguity (SC-002); Feature 159 suites green; any golden change reviewed/accepted (SC-005).

---

## Phase 4: User Story 2 - Tests that can actually fail (Priority: P2)

**Goal**: Replace the two `Expect.isTrue true` placeholders with real, falsifiable assertions over
state already in scope, leaving each test non-empty.

**Independent Test**: Zero `Expect.isTrue true` remains in either touched file; both test lists build
and pass; each affected test keeps at least one meaningful assertion.

### Implementation for User Story 2

- [X] T012 [P] [US2] Rewrite the placeholder at `tests/Controls.Tests/Feature093ParityTests.fs:77` (test `T020 — capture the pre-refactor procedural baselines`): after `captureBaselines ()`, assert the expected baseline files exist on disk under `specs/093-visual-state-style-layer/readiness/parity/` (e.g. `Expect.isTrue (File.Exists path) …` for each written `button/check-box/check-box-checked.<theme>.normal.scene.txt`, or assert the file set is present and non-empty). Falsifiable: fails if `captureBaselines` writes nothing or to the wrong place.
- [X] T013 [P] [US2] Rewrite the placeholder at `tests/Controls.Tests/TypedMigrationTests.fs:555` (test `stateful facades introduce no parallel model type (SC-003)`): replace the vacuous assert with a runtime equality exercising the reused types — assert the typed `init` (e.g. `TextArea.init`/`ListView.init`) equals the canonical underlying `init` (`TextInput`/`Collections` baseline) for the same input; remove the now-redundant `ignore taInit/lvInit` lines. Confirm the exact canonical `init` signatures/values during implementation to keep the equality honest, and ensure the chosen equality is **genuinely falsifiable** — a different input must produce a different value (i.e. it actually exercises `init`), not a trivially-true structural identity that would pass regardless.
- [X] T014 [US2] Verify no placeholder remains: `grep -rn 'Expect.isTrue true' tests/Controls.Tests/Feature093ParityTests.fs tests/Controls.Tests/TypedMigrationTests.fs` (expect NO matches), and confirm neither edited test is left with zero assertions (FR-004).
- [X] T015 [US2] Run the affected suites: `dotnet test --filter 'FullyQualifiedName~Feature093Parity'` and `dotnet test --filter 'FullyQualifiedName~TypedMigration'` — both build and pass.

**Checkpoint**: Zero always-true assertions in the two files; each affected test retains a meaningful, falsifiable assertion (SC-003).

---

## Phase 5: User Story 3 - Single source of truth for the layout cache version (Priority: P3)

**Goal**: Centralize the `rev=150` / `Revision = 150` layout cache version behind one named constant
feeding all four sites, with byte-identical composed output.

**Independent Test**: The `rev=150` raw literal appears once; both former string sites and both
`Revision` field sites derive from the constant; composed `QueryIdentity` / `cacheEntry.EntryId`
strings are byte-identical to the pre-change baseline (T003).

### Implementation for User Story 3

- [X] T016 [US3] Introduce one private numeric source of truth in `src/Layout/Layout.fs` (e.g. `[<Literal>] layoutCacheRevision = 150`), placed before its first use and **omitted from `src/Layout/Layout.fsi`** (private by omission — no `private` keyword required on the literal). 
- [X] T017 [US3] Derive the token from the constant at both string sites: `src/Layout/Layout.fs:839` (interpolated `$"…|rev={layoutCacheRevision}"`) and `:964` (`$"rev={layoutCacheRevision}"`), each rendering exactly the bytes `rev=150` (invariant integer formatting). Derive the int field from the constant at both record sites: `:847` and `:974` (`Revision = layoutCacheRevision`).
- [X] T018 [US3] Add/confirm a byte-identity test asserting the composed `IntrinsicQuery.QueryIdentity` and `cacheEntry.EntryId` strings for representative inputs are byte-for-byte identical to the T003 baseline (FR-006). Run `dotnet test --filter 'FullyQualifiedName~Layout'`.
- [X] T019 [US3] Verify single-source (SC-004): `grep -n '"rev=150"' src/Layout/Layout.fs` (expect **no** hand-written `rev=150` string — the token is interpolated) and `grep -n 'rev=150\|Revision = 150\|layoutCacheRevision' src/Layout/Layout.fs` (expect exactly one `layoutCacheRevision = 150` source-of-truth literal, no bare `Revision = 150`, and all four sites deriving from the constant); confirm `git diff -- src/Layout/Layout.fsi` is empty (Tier 2).

**Checkpoint**: `rev=150` literal in exactly one place; all four sites derive from it; cache identity/key strings byte-identical (SC-004).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Whole-change-set verification mapping back to the success criteria.

- [X] T020 Final smoke run (build+test): `dotnet build` succeeds and `dotnet test` is fully green with no newly skipped tests vs. the T002 baseline (SC-001).
- [X] T021 [P] Tier-2 surface gate: `git diff --stat -- '*.fsi'` is empty, and the repo's API surface-drift check reports no surface change (FR-009, SC-005).
- [X] T022 [P] Run the full quickstart.md validation (all three scenarios + final acceptance) and confirm each success criterion SC-001…SC-005 is met.
- [X] T023 Capture per-phase fs-gg-feedback-capture notes (process friction / generalizable-code candidates) into `specs/177-code-health-safety-fixes/feedback/` if any surfaced during implementation.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories** — the early build+test smoke run (T006) must pass before any fix.
- **User Stories (Phase 3–5)**: All depend on Foundational completion. The three stories touch **disjoint files**, so once Foundational is done they can proceed fully in parallel or in priority order (P1 → P2 → P3).
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)** — `src/Controls/RetainedRender.fs` only. No dependency on US2/US3.
- **US2 (P2)** — `tests/Controls.Tests/Feature093ParityTests.fs` + `TypedMigrationTests.fs` only. No dependency on US1/US3.
- **US3 (P3)** — `src/Layout/Layout.fs` only. No dependency on US1/US2.

All three user stories are mutually independent (disjoint files) and independently testable.

### Within Each User Story

- US1: edit (T008) → verify literal (T009) → relational suites (T010) → golden gate (T011).
- US2: the two rewrites (T012, T013) are parallel (different files) → verify no placeholder (T014) → run suites (T015).
- US3: constant (T016) → derive at all sites (T017) → byte-identity test (T018) → single-source verify (T019).

### Parallel Opportunities

- Setup: T003 and T004 in parallel.
- Foundational: T007 in parallel with the build+test gate prep.
- **Across stories**: US1, US2, US3 can all run in parallel after Phase 2 (disjoint files).
- Within US2: T012 and T013 in parallel.
- Polish: T021 and T022 in parallel.

---

## Parallel Example: All three user stories after Foundational

```bash
# Disjoint files — run the three Phase 0 items concurrently:
Task: "US1 — fix feature159Hash seed in src/Controls/RetainedRender.fs:851"
Task: "US2 — rewrite placeholders in tests/Controls.Tests/{Feature093ParityTests,TypedMigrationTests}.fs"
Task: "US3 — centralize rev=150 in src/Layout/Layout.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (comprehensive baseline).
2. Complete Phase 2: Foundational — **including the early build+test smoke run (T006)** that validates the safe-base premise before any fix.
3. Complete Phase 3: User Story 1 (the only candidate latent bug).
4. **STOP and VALIDATE**: Feature 159 suites green; golden diff reviewed (SC-002, SC-005).
5. Ship if ready — US1 alone is a complete, valuable increment.

### Incremental Delivery

1. Setup + Foundational → trusted base.
2. US1 (P1) → test → ship (MVP: the hash fix).
3. US2 (P2) → test → ship (test integrity).
4. US3 (P3) → test → ship (single-source cache version).
5. Each story adds value without touching another story's files.

---

## Notes

- [P] tasks = different files, no dependencies.
- This is a Tier-2 internal change: **no `.fsi` edits**, no surface-area baseline edits (guarded by T021).
- The only intended runtime change is the internal `feature159Hash` value, reviewed against goldens under T011 (FR-002, FR-008).
- Commit after each user story or logical group; stop at any checkpoint to validate independently.
- Avoid: weakening any assertion, leaving an empty test body, or any non-byte-identical layout cache-key change.
