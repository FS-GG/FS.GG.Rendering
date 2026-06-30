---
description: "Task list for Consumer Skill Catalog Currency"
---

# Tasks: Consumer Skill Catalog Currency

**Input**: Design documents from `/specs/224-skill-catalog-currency/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/catalog-currency-check.md ✅, quickstart.md ✅

**Tests**: Tests ARE requested for this feature. The durable fix (US3) *is* a repo-owned currency
check plus a deliberate dangling-id regression (SC-003), and US1/US2 are verified by that same
check. Test tasks are therefore first-class, not optional.

**Organization**: Tasks are grouped by user story (P1 → P2 → P3) so each can be implemented and
verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)
- Every task names exact file paths.

## Path Conventions

Single-repo F# framework + `dotnet new` template. Shipped docs live under `template/base/docs/`;
the new check lives in `tests/Package.Tests/`; the reused skill enumerator is
`tools/Rendering.Harness/SkillParity.fs`. Evidence is written under
`specs/224-skill-catalog-currency/readiness/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Restore/build the graph and capture the no-regression baseline before touching content.

- [X] T001 Restore + build the `src` + `tests` graph; if the stale FSharp.Core lockfile hash blocks restore, apply the documented workaround (see auto-memory `nu1403-fsharp-core-lockfile-workaround.md`). Confirm `tools/Rendering.Harness` builds and `tests/Package.Tests` restores (it is lockfile-opt-out, release-only).
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/224-skill-catalog-currency/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**` — and records the full red/green set so pre-existing reds are flagged here, not discovered at merge).
- [X] T003 [P] Inventory the in-scope files read-only: confirm the two shipped docs `template/base/docs/skillist-reference.md` and `template/base/docs/scaffold-map.md` exist, and confirm the 7 product-skill dirs under `template/product-skills/` (`fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-scene`, `fs-gg-skiaviewer`, `fs-gg-symbology`, `fs-gg-testing`, `fs-gg-ui-widgets`). Record the list in `specs/224-skill-catalog-currency/readiness/inscope-files.md`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Pin the ground-truth produced skill surface (the catalog must match *that*, not the
framework repo layout), finalize the two open seam decisions, and draft the check's home — all
before any catalog edit.

**⚠️ CRITICAL**: No user story work begins until this phase completes.

> **⚠️ Early live run (STANDING, do not omit / do not defer).** The plan's root-cause findings
> (R0–R4) are verified only by *reading* code and *listing* dirs — they are **unverified
> assumptions until a product is actually scaffolded**. T005 scaffolds real products and
> enumerates what they actually carry; author catalog rows from T005's output, never from the
> framework-repo `src/**/skill` layout.

- [X] T004 Build the classification / root-cause map every story depends on: from research.md, enumerate (a) the 8 defunct `fs-gg-*` ids + the `fsdocs-*`/`fsharp-*` families to be removed (FR-002), (b) the reference *forms* the two docs actually use (catalog table rows; scaffold-map prose code-spans at `template/base/docs/scaffold-map.md:131,140,149,153`), and (c) the hypothesized shipping set. Record in `specs/224-skill-catalog-currency/readiness/rootcause-map.md`.
- [X] T005 **Early live run** (standing assumption / quickstart §1): scaffold a spec-kit product and a non-spec-kit (`sdd`/`none`) product from the local template via `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx`, then for each scaffold enumerate `<scaffold>/.agents/skills` + `<scaffold>/.claude/skills` and test `<scaffold>/docs/skillist-reference.md` presence (present under spec-kit, absent under sdd/none). Capture raw listings to `specs/224-skill-catalog-currency/readiness/produced-surface.md`.
- [X] T006 From T005 evidence, finalize the **R1 produced-surface ground truth**: confirm (or correct) the shipping set = profile-wired product skills (`fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-scene`, `fs-gg-skiaviewer`, `fs-gg-symbology`, `fs-gg-testing`, `fs-gg-ui-widgets`) + the co-shipping `speckit-*` command skills, and confirm zero defunct/`fsdocs-*`/`fsharp-*` ids ship. **Also confirm `speckit-*` ids are actually *discoverable* by `SkillParity.discoverDefaultSurfaces`/`inventorySkills` (a `SKILL.md` whose `name:` equals the referenced id), not merely present as directories** — if discovery does not surface them, the check (T009) cannot resolve valid `speckit-*` refs (closes analysis finding U1). Record the confirmed set, the discoverability result, and the **R2 decision** (Option A hand-maintained-under-check [default] vs Option B tiny generator) in `specs/224-skill-catalog-currency/readiness/produced-surface.md`. **Decision gate**: T010/T016 depend on the confirmed set; T009 depends on `speckit-*` discoverability; T018 depends on the R2 choice.
- [X] T007 Wire the check's home: add a `ProjectReference` to `..\..\tools\Rendering.Harness\Rendering.Harness.fsproj` in `tests/Package.Tests/Package.Tests.fsproj` so the new test can reuse `SkillParity` discovery (`discoverDefaultSurfaces` / `inventorySkills` / `parseFrontMatter`), and register the new test file `Feature224SkillCatalogCurrencyTests.fs` in that `.fsproj` `<ItemGroup>` immediately before `Tests.fs`. Confirm `dotnet build tests/Package.Tests` succeeds with an empty stub module.
- [X] T008 Decide the **Justified-exception** seam (FR-009 / data-model "Justified exception"): default to an in-test allowlist of `(id, reason)` pairs, empty after this feature; document the chosen mechanism (allowlist vs inline doc annotation) in `specs/224-skill-catalog-currency/readiness/exception-mechanism.md`. No silent exemption.

**Checkpoint**: Produced-surface ground truth confirmed against a live scaffold; check home builds; seam decisions recorded — user-story work can begin.

---

## Phase 3: User Story 1 - Catalog lists only the skills a product actually ships (Priority: P1) 🎯 MVP

**Goal**: Every id and path in `docs/skillist-reference.md` resolves to a skill the scaffolded
product actually carries; no defunct/unvendored id remains.

**Independent Test**: Scaffold a fresh product, open its `docs/skillist-reference.md`, confirm every
listed id resolves to a `SKILL.md` present in that product and that none of the FR-002 defunct ids
appear (SC-001, SC-005).

### Tests for User Story 1 ⚠️ (write FIRST, must FAIL before content fix)

- [X] T009 [US1] In `tests/Package.Tests/Feature224SkillCatalogCurrencyTests.fs`, write the catalog reference-extraction + resolution test: parse the `` `id` `` table-cell references from `template/base/docs/skillist-reference.md`, resolve each against the `SkillParity`-discovered produced surface (a `SKILL.md` whose `name:` equals the id under `template/product-skills`, `.agents/skills`, `.claude/skills`, `speckit-*`), and assert zero unresolved. **Also assert path-column correctness (FR-003, closes analysis finding G1): each row's column-2 path must be a consumer-resolvable location (`.agents/skills/<id>/` or `.claude/skills/<id>/`) — not a framework-only `src/*/skill/*` path — so a row with a right id but a wrong/framework path still fails.** Run `dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency` and confirm it **FAILS** against today's catalog, naming the defunct ids (quickstart §2).

### Implementation for User Story 1

- [X] T010 [US1] Rewrite `template/base/docs/skillist-reference.md` content: list exactly the confirmed shipping set from T006 with consumer-resolvable paths (`.agents/skills/<id>/SKILL.md`, not `src/**/skill`), drop all 8 defunct `fs-gg-*` ids and the `fsdocs-*`/`fsharp-*` families (FR-001/FR-002/FR-003), and add a one-line profile note for profile-specific skills (R1). Keep the file's spec-kit emission gating untouched.
- [X] T011 [US1] Replace the false provenance header in `template/base/docs/skillist-reference.md` (the "GENERATED from the live SkillRegistry … RefreshSurfaceBaselines … TargetMetadataDrift" claim) with an honest statement that the file is hand-maintained and **enforced by the Feature 224 currency check** (R0/R2 Option A).
- [X] T012 [US1] Re-run `dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency` and confirm the catalog portion now **PASSES** (SC-001: 100% of catalog ids resolve; SC-005: defunct id set appears zero times).

**Checkpoint**: Catalog resolves 100% against the real produced surface — MVP deliverable on its own.

---

## Phase 4: User Story 2 - Cross-references point at skills that actually ship (Priority: P2)

**Goal**: Every "see the X skill" pointer in `docs/scaffold-map.md` names a skill present in the
product.

**Independent Test**: Grep the shipped scaffold map for skill references; confirm each names a
shipping skill and follows to a real skill (SC-002).

### Tests for User Story 2 ⚠️ (extend the check FIRST, must FAIL before repoint)

- [X] T013 [US2] Extend the test in `tests/Package.Tests/Feature224SkillCatalogCurrencyTests.fs` to also extract **prose code-span** references from `template/base/docs/scaffold-map.md` (inline `` `fs-gg-…` ``/`` `speckit-…` `` tokens used as "see the X skill" pointers, e.g. lines 131/140/149/153) and resolve them the same way. Confirm the test **FAILS** naming the dangling refs (`fs-gg-typed-controls`, `fs-gg-controls-host`, `fs-gg-viewer-host`).

### Implementation for User Story 2

- [X] T014 [US2] Repoint the dangling skill references in `template/base/docs/scaffold-map.md` to the shipping skill that absorbed each responsibility (e.g. `fs-gg-typed-controls`/`fs-gg-controls-host` → `fs-gg-ui-widgets`; `fs-gg-viewer-host` → `fs-gg-skiaviewer`), per FR-004 and the T006 confirmed set.
- [X] T015 [US2] Re-run `dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency`; confirm both docs now **PASS** (SC-002: zero dangling scaffold-map refs).

**Checkpoint**: Both shipped docs resolve fully and independently.

---

## Phase 5: User Story 3 - Drift cannot silently recur (Priority: P3)

**Goal**: The repo-owned check fails the pack/test lane whenever any shipped-doc skill reference has
no resolvable skill, with an actionable, locatable message — and the documented refresh path passes
first time.

**Independent Test**: Inject a dangling id into either doc → check fails naming it (id + doc +
line); revert → check passes (SC-003).

### Tests for User Story 3 ⚠️

- [X] T016 [US3] Add the deliberate dangling-id regression test in `tests/Package.Tests/Feature224SkillCatalogCurrencyTests.fs`: assert that a known-bogus reference (`fs-gg-does-not-exist`) would produce exactly one Currency finding, and that the real docs produce none (SC-003 both directions). Per quickstart §4, also document the manual inject→red / revert→green loop in `specs/224-skill-catalog-currency/readiness/regression-evidence.md`.

### Implementation for User Story 3

- [X] T017 [US3] Finalize the failure-message shape in `tests/Package.Tests/Feature224SkillCatalogCurrencyTests.fs` so each finding names **id + doc + line** (FR-006), matching the contract example (`skillist-reference.md:NN  'id' → no SKILL.md with name: id in package`). Assert the message contains all three fields.
- [X] T018 [US3] Implement the Justified-exception handling chosen in T008 (FR-009): the check exempts an unresolved id **only** if it appears in the allowlist with a non-empty reason; add a test that a reason-less exemption still fails. Default allowlist is empty.
- [X] T019 [US3] Document the refresh path (FR-007 / SC-004) in the honest header of `template/base/docs/skillist-reference.md` and in `specs/224-skill-catalog-currency/readiness/refresh-path.md`: under Option A, "edit to satisfy the check (`dotnet test … --filter Feature224SkillCatalogCurrency`)". **If T006 chose Option B**, instead add `scripts/refresh-skill-catalog.fsx` that emits rows from `SkillParity` discovery and have the check assert committed == regenerated; confirm regenerate-then-test is green on first run with no hand-edits.

**Checkpoint**: Drift is gated; findings are locatable; refresh passes first run. All three stories functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Surface/baseline hygiene, gating-intact regression, and cross-repo coherence.

- [X] T020 [P] If any public helper was added to `tools/Rendering.Harness/SkillParity.fs` (Principle II conditional), update `tools/Rendering.Harness/SkillParity.fsi` AND the surface-area baseline in the same change; if the check stayed self-contained in the test, confirm no `.fsi`/baseline delta is needed and note that in readiness.
- [X] T021 [P] Confirm emission gating is intact (no Feature 219/204 regression): `dotnet test tests/Package.Tests --filter Feature219` and `--filter Feature204` stay green — catalog still present under spec-kit, absent under sdd/none (quickstart §6); only its content changed.
- [X] T022 Run the full quickstart end-to-end (`specs/224-skill-catalog-currency/quickstart.md` steps 1–6) and record pass/fail per step in `specs/224-skill-catalog-currency/readiness/quickstart-evidence.md`.
- [X] T023 [P] Re-run the no-regression baseline (`dotnet fsi scripts/baseline-tests.fsx --out specs/224-skill-catalog-currency/readiness/baseline-after.md`) and diff against T002 to prove no new reds.
- [X] T024 Cross-repo coherence (FR-010 / research R4) via the `cross-repo-coordination` skill: sequence this change onto the Feature 220 republish vehicle (#33), feed the downstream pin bump (FS-GG/FS.GG.Templates#8), update the `fs-gg-ui-template` contract/registry coherence, and comment the result on coordination issue #36.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup. **Blocks all user stories.** T005 (live run) → T006 (confirmed set) is the hard gate; no catalog row is authored before T006.
- **User Stories (Phases 3–5)**: all depend on Foundational. They share one test file and the two docs, so they are most safely done in priority order (P1 → P2 → P3) rather than fully parallel.
- **Polish (Phase 6)**: depends on all desired stories complete.

### User Story Dependencies

- **US1 (P1)**: depends only on Foundational (esp. T006 confirmed set, T007 check home). Independently testable and shippable as MVP.
- **US2 (P2)**: depends on Foundational; extends the same check file as US1 (T013 builds on T009) and edits a different doc — sequence after US1 to avoid churn on the shared test file.
- **US3 (P3)**: depends on US1+US2 having defined what "resolvable" means and on the check existing; finalizes message shape, exceptions, and refresh path.

### Within Each User Story

- Test extension is written and made to FAIL before the corresponding doc fix (T009→T010, T013→T014, T016).
- Doc content fix before re-run-to-green (T010/T011→T012, T014→T015).

### Parallel Opportunities

- T003 runs parallel to T001/T002 (read-only inventory).
- In Phase 6, T020 / T021 / T023 are independent ([P]); T022/T024 follow once green.
- The three user stories touch a **shared** test file and the two docs, so within-story tasks are largely sequential; cross-story parallelism is limited by that shared surface (sequence P1→P2→P3).

---

## Parallel Example: Phase 6 Polish

```bash
# Independent verification tasks can run together:
Task: "T020 .fsi + surface-area baseline update (only if public helper added)"
Task: "T021 Gating-intact regression: dotnet test --filter Feature219 / Feature204"
Task: "T023 Re-run baseline-tests.fsx and diff against T002"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1 (Setup) + Phase 2 (Foundational) — **including the T005 live scaffold run** that
   confirms the produced skill surface before any row is authored.
2. Complete Phase 3 (US1): failing catalog check (T009) → rewrite catalog + honest header
   (T010/T011) → green (T012).
3. **STOP and VALIDATE**: catalog resolves 100% against a freshly scaffolded product (SC-001/SC-005).
4. Ship-ready MVP increment (rides #33 with siblings).

### Incremental Delivery

1. Setup + Foundational → ground truth locked.
2. US1 → catalog correct + honest header → validate (MVP).
3. US2 → scaffold-map cross-refs repointed → validate.
4. US3 → regression test + message shape + exceptions + refresh path → drift-proofed.
5. Polish → gating-intact, baseline-clean, `.fsi`/baseline hygiene, cross-repo coherence (#33 / Templates#8 / #36).

---

## Notes

- [P] = different files, no dependency on an incomplete task.
- The whole feature is gated on T005's live evidence — do NOT author catalog rows from the
  framework-repo `src/**/skill` layout; author from what the scaffolded product actually carries.
- US1/US2/US3 share `tests/Package.Tests/Feature224SkillCatalogCurrencyTests.fs` and the two docs;
  keep them sequential to avoid same-file churn.
- Commit after each story checkpoint; this work reaches consumers only via the #33 republish + the
  FS.GG.Templates#8 pin bump (FR-010), not as a standalone release.
