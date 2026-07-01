---
description: "Task list for feature 228 — fix scaffold skill leak"
---

# Tasks: fs-gg-ui template must not write UI skills into orchestrator-owned skill trees

**Input**: Design documents from `/specs/228-fix-scaffold-skill-leak/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: This feature's verification *is* test-logic correction. The existing Feature 204/219 Expecto gates currently assert the leaky shape, so they are part of the fix surface (not optional). Test tasks below are therefore REQUIRED, per plan.md and the gate-assertion contract.

**Change tier**: Tier 2 (content/config only) — no `src/**`, no `.fsi`, no dependency, no version bump (FR-007).

**Organization**: Tasks grouped by user story. Because the pre-fix gates assert the leak, the template fix (US1) and the gate corrections (US3) must land in the **same commit** to keep the build green — see Dependencies.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 / US2 / US3 (maps to spec.md user stories)
- Exact file paths included in every task

## Path Conventions

Single-repo template/package change. Touch-points (verified in plan.md):
`.template.config/template.json`, `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`,
`tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs`, `scripts/validate-lifecycle-template.fsx`,
`specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md` (regenerated),
evidence under `specs/228-fix-scaffold-skill-leak/readiness/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline before touching anything.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project — the solution
> deliberately omits `tests/Package.Tests` (release-only, owns the surface gate) and `samples/**/*.Tests`
> (feed consumers), which is exactly where the Feature 175 surprises hid. Use the discovery-based runner
> so nothing silently drops out.

- [X] T001 Confirm feature prerequisites: on branch `228-fix-scaffold-skill-leak`, `.NET net10.0` SDK present, and `dotnet new list fs-gg-ui` resolves (template installed from local feed) — record versions in `specs/228-fix-scaffold-skill-leak/readiness/env.md`
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/228-fix-scaffold-skill-leak/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Map the exact leak surface and **reproduce the defect on the live scaffold** before any edit, so the fix is validated against real artifact placement (FR-008 / plan Standing-assumption note), not a green test alone.

**⚠️ CRITICAL**: No user-story work begins until the leak is confirmed on the current template.

> **⚠️ Early live smoke run (STANDING, do not omit).** The "app to drive" here is the **scaffold path**.
> Drive the real scaffold under `--lifecycle sdd` on the *current (pre-fix)* template and observe the
> `.claude/skills/fs-gg-*` intrusion, before any `template.json` edit. Treat the plan's root-cause
> hypothesis as unverified until the scaffold has actually been run and the leaked files observed.

- [X] T003 Build the leak-surface map: enumerate the 9 per-profile `.claude/skills/fs-gg-*/` product-skill sources in `.template.config/template.json` and confirm each is `profile`-gated but missing the `lifecycle == "spec-kit"` clause; confirm the 9 matching `.agents/skills/fs-gg-*/` siblings and the base/sample/feedback sources are already correct. Record the source→target×condition table in `specs/228-fix-scaffold-skill-leak/readiness/leak-surface-map.md` (cross-checks data-model.md)
- [X] T004 Static before-scan: run the `template.json` scan from quickstart §0 and confirm it prints `9` ungated `.claude/skills/` product sources; save output to `specs/228-fix-scaffold-skill-leak/readiness/leak-before.md`
- [X] T005 **Early live smoke run (leak repro)**: `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report`, then inspect the `sdd/game` scaffold and confirm `.claude/skills/fs-gg-*` are PRESENT (8 for `game`) — the intrusion SDD would flag as `providerWroteSddTree`. Append the observed leaked paths to `specs/228-fix-scaffold-skill-leak/readiness/leak-before.md`
- [X] T006 Confirm the pre-fix gates encode the leak (they must go red after the template fix): run `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature204|Feature219"` and record which assertions currently assert the leaky shape (Feature 219 G-EMIT blanket "must not be lifecycle-gated"; Feature 204 classifier framework=18/workspace=6 floors) in `specs/228-fix-scaffold-skill-leak/readiness/pre-fix-gate-state.md`

**Checkpoint**: Leak reproduced live under `sdd`, surface map confirmed to exactly 9 sources, pre-fix gate assertions catalogued. Fix work can begin.

---

## Phase 3: User Story 1 - SDD-orchestrated scaffold completes cleanly (Priority: P1) 🎯 MVP

**Goal**: The `fs-gg-ui` template writes zero UI skills into `.claude/skills/` under `sdd`, so an SDD-orchestrated scaffold returns success with no `providerWroteSddTree` and the full-stack path proceeds to governance-overlay + `doctor`.

**Independent Test**: Scaffold `app`/`game` under `sdd`; scaffold report returns success (no `providerWroteSddTree`/`providerFailed`) and orchestrator-owned trees contain zero provider-written UI skill files.

> **Build note**: this template edit will turn the *pre-fix* Feature 204/219 gates red. Those gate
> corrections live in US3 (T013–T015) and MUST be applied in the same commit as T007 to keep the build
> green. US1 is the product value; US3 is the guard that keeps it — but they are co-committed.

### Implementation for User Story 1

- [X] T007 [US1] Apply the core fix in `.template.config/template.json`: append ` && lifecycle == "spec-kit"` to the `condition` of the 9 `.claude/skills/fs-gg-*/` product-skill sources — targets `fs-gg-scene`, `fs-gg-symbology`, `fs-gg-skiaviewer`, `fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-ui-widgets`, `fs-gg-styling`, `fs-gg-layout`, `fs-gg-testing`. Leave the 9 `.agents/skills/fs-gg-*/` siblings untouched; do not add any `.codex/skills/` source (data-model.md table). Note: all **9** sources are fixed, but the `game` profile triggers only **8** of them (per T005) — the counts differ because the sources are profile-gated, not because one is missed
- [X] T008 [US1] Static after-scan: re-run the quickstart §0 `template.json` scan and confirm it now prints `0` ungated `.claude/skills/` product sources; append to `specs/228-fix-scaffold-skill-leak/readiness/leak-before.md` (before/after in one file)
- [X] T009 [US1] Live after-observation (`sdd`): re-run `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report`; confirm `count(.claude/skills/fs-gg-*) == 0` and `set(.agents/skills/fs-gg-*) == S(profile)` for `app` and `game` under `sdd`. Record in `specs/228-fix-scaffold-skill-leak/readiness/fixed-scaffold-sdd.md` (SC-002)
- [X] T010 [US1] End-to-end SDD-orchestrated acceptance (SC-001/SC-004/FR-005): in a scratch dir run `fsgg-sdd scaffold --provider rendering --param productName=Spaceinvaders --profile game` (or `new-sdd-fullstack ./SpaceInvaders Spaceinvaders`); confirm `outcome: success`, no `scaffold.providerWroteSddTree`, and the full-stack script proceeds past scaffold into governance-overlay + `doctor`. The **TestSpec tutorial Part A step 2** (FR-005) exercises this same SDD-orchestrated scaffold path and is covered **transitively** by this task — it lives in the SDD repo and needs no separate rendering-side task; note that transitive coverage in the evidence. Record in `specs/228-fix-scaffold-skill-leak/readiness/success-criteria.md`. If `fsgg-sdd` is unavailable in-env, mark `environment-limited` with the disclosed substitute (the T009 live scaffold + boundary-rule reasoning)

**Checkpoint**: The reported defect is fixed and observed on the real scaffold. MVP deliverable complete.

---

## Phase 4: User Story 2 - Standalone Spec Kit product keeps its skills; provider tree never shrinks (Priority: P2)

**Goal**: `spec-kit` output stays byte-identical to today; `.agents/skills/` is never reduced under any lifecycle; `sdd` and `none` produce identical skill-tree output.

**Independent Test**: Scaffold `game` under `spec-kit` and diff its full emitted skill set against the pre-fix baseline (identical); across all three lifecycles confirm `.agents/skills/` holds exactly S(profile).

### Implementation for User Story 2

- [X] T011 [US2] Live observation (`none` ≡ `sdd`, FR-003 / SC-003): from the T009 report run, confirm the `none/app` and `none/game` scaffolds produce `count(.claude/skills/fs-gg-*) == 0` and `set(.agents/skills/fs-gg-*) == S(profile)`, byte-identical skill-tree to the `sdd` column. Record in `specs/228-fix-scaffold-skill-leak/readiness/fixed-scaffold-none.md`
- [X] T012 [US2] Provider-tree-intact + spec-kit-unchanged (SC-003): confirm `.agents/skills/` == S(profile) for `app`/`game` under all three lifecycles, and that `spec-kit` still emits S(profile) into BOTH surfaces (diff-vs-today = none). Record in `specs/228-fix-scaffold-skill-leak/readiness/agents-tree-intact.md` (spec-kit byte-identity is also asserted by Feature 204 GV-3, verified in T016)

**Checkpoint**: The invariants that must not move (spec-kit set, provider tree) are proven unmoved; `sdd ≡ none` confirmed.

---

## Phase 5: User Story 3 - A regression guard proves the boundary holds (Priority: P3)

**Goal**: Repo-owned automated checks fail on the pre-fix template and pass on the fixed one, covering every profile that ships product skills, so a future added skill cannot silently re-leak.

**Independent Test**: The corrected gates fail on the pre-fix template (T006 state) and pass on the fixed template, naming any offending `.claude/skills/` path.

> These gate corrections are co-committed with T007 (see US1 build note). They encode the corrected
> invariant per `contracts/gate-assertion-contract.md`.

### Implementation for User Story 3

- [X] T013 [P] [US3] Correct Feature 219 G-EMIT in `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs` (~L138-155): make the per-source lifecycle assertion surface-specific — `.agents/skills/`-targeted product sources MUST NOT contain `lifecycle == "spec-kit"` and MUST contain a `profile ==` predicate; `.claude/skills/`-targeted product sources MUST contain `lifecycle == "spec-kit"`. Keep `sources.Length >= 18`, the profile predicate, and the "each id emits under BOTH surfaces" structural pairing (G-219.1/.2/.3). Update the comment
- [X] T014 [P] [US3] Correct Feature 204 in `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`: (a) `gatedSourceAudit` classifier (~L118-138) routes a `template/product-skills/` source to **framework** only when its target is under `.agents/skills/`, and to **lifecycle-workspace** when its target is under `.claude/skills/` (G-204.1); (b) GV-2 floors (~L162-169) framework `>=18`→`>=9`, workspace `>=6`→`>=15` (product `>=3`); (c) update the expected `gated-condition:` report string to the corrected wording (G-204.2/.3)
- [X] T015 [US3] Update `scripts/validate-lifecycle-template.fsx`: update the emitted `gated-condition:` line; add a per-(`sdd`|`none`)×profile observation `claude-product-skills=0` alongside the existing `.agents/skills/` `framework-skills-present=ok` (extend `frameworkSkillCount` with a `.claude/skills/` product-mirror count). Keep `.agents/skills/` framework-present=ok (G-live)
- [X] T016 [US3] Regenerate the shared readiness artifact both gates read: `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report` → `specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md` (regenerated by the fsx, not hand-edited)
- [X] T017 [US3] Run the corrected gates: `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature204|Feature219"` — expect Feature 204 GV-2/GV-4/GV-5 green (`framework=9`, `workspace>=15`, 0 violations, sdd/none `claude-product-skills=0` + `framework-skills-present=ok`, spec-kit `diff-vs-today=none`) and Feature 219 G-EMIT green. Capture transcript to `specs/228-fix-scaffold-skill-leak/readiness/gate-transcripts.md` (SC-005)

**Checkpoint**: The guard is live and green on the fix; it would have caught Feature 227's leak.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full regression confirmation, evidence completion, quickstart validation.

- [X] T018 Re-run the full baseline runner (`dotnet fsi scripts/baseline-tests.fsx --out specs/228-fix-scaffold-skill-leak/readiness/baseline-after.md`) and diff vs T002 — confirm no new reds outside the intended Feature 204/219 changes (Constitution V: no assertion weakened to green a build)
- [X] T019 Walk the whole `quickstart.md` recipe end-to-end (steps 0→3) and confirm every "Expected outcome" holds; record any deviation
- [X] T020 [P] Complete the success-criteria evidence index in `specs/228-fix-scaffold-skill-leak/readiness/success-criteria.md`, mapping SC-001…SC-005 to their evidence files (quickstart §"Success mapping" table)
- [X] T021 Confirm out-of-scope invariants held (FR-007 Tier-2 guarantee): run `git diff --name-only main...HEAD` and confirm the change touches **only** `.template.config/template.json`, the two `tests/Package.Tests/Feature20{4,19}*.fs` gates, `scripts/validate-lifecycle-template.fsx`, and `specs/**` — **no `src/**` file, no `.fsi`, and no version change** in any `*.fsproj`/`*.nuspec`/`Directory.*.props`. Also confirm `.codex/skills/` still never written; Feature 224/225 gates untouched and green; `docs/reports/skills-parity.md` NOT regenerated (no skill added/removed). Note the `git diff` evidence in `specs/228-fix-scaffold-skill-leak/readiness/non-goals-held.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup — BLOCKS all user stories (leak must be reproduced live first).
- **User Stories (Phase 3-5)**: all depend on Foundational.
- **Polish (Phase 6)**: depends on all user stories.

### Critical co-commit constraint

The template fix **T007 (US1)** turns the pre-fix gates red. The gate corrections **T013–T015 (US3)**
must be committed together with T007. So although US1 is the priority product value, its *build-green*
delivery requires the US3 gate edits in the same commit. Sequence within the fix commit:
T013 + T014 (parallel, different test files) → T007 → T015 → T016 → T017.

### User Story Dependencies

- **US1 (P1)**: after Foundational. Product value. Co-commits with US3 gate edits (build constraint).
- **US2 (P2)**: after US1 (reads the same live report run T009). Independently testable via the `none`/`spec-kit`/`agents` observations.
- **US3 (P3)**: after Foundational; edits co-commit with T007. Independently testable: gate fails pre-fix, passes post-fix.

### Within stories

- T013 and T014 are [P] (different files).
- T009 produces the live report that T011/T012 read (do T009 before T011/T012).
- T016 must run after T015 (script change) and before T017 (gates read the regenerated artifact).

### Parallel Opportunities

- T013 and T014 can run in parallel (`Feature219…fs` vs `Feature204…fs`).
- T020 can run in parallel with other polish tasks.
- Evidence-writing tasks that touch distinct readiness files are independent once their observation exists.

---

## Parallel Example: US3 gate corrections

```bash
# Different test files — run together:
Task: "Correct Feature 219 G-EMIT surface-specific assertion in tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs"
Task: "Correct Feature 204 classifier + GV-2 floors in tests/Package.Tests/Feature204LifecycleTemplateTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup (baseline).
2. Phase 2 Foundational — **reproduce the leak live under `sdd`** before editing (T005).
3. Phase 3 US1 — apply the 9-clause fix + observe the SDD scaffold clean. Co-commit the US3 gate edits (T013–T015) so the build stays green.
4. **STOP and VALIDATE**: SDD scaffold returns success, `.claude/skills/` = 0, `.agents/skills/` = S(profile).

### Incremental Delivery

1. Setup + Foundational → leak confirmed live.
2. US1 (+ co-committed US3 gate edits) → SDD scaffold clean → MVP.
3. US2 → spec-kit/agents/none invariants proven unmoved.
4. US3 → guard green and catalogued as the regression backstop.
5. Polish → full baseline re-run, quickstart walk, evidence index.

---

## Notes

- [P] = different files, no dependencies.
- Tier 2: no `src/**`, no `.fsi`, no dependency, no version bump (FR-007).
- FR-008: delivery confirmed by an **observed scaffold** (T005 before / T009–T010 after), not a green test alone.
- Do NOT edit Feature 224/225 gates or regenerate `docs/reports/skills-parity.md` (no skill added/removed).
- The `.agents/skills/` provider surface must never shrink — every observation checks it, and no assertion over it is loosened.
