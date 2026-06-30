---
description: "Task list for feature: Emit Framework Skills On Every Lifecycle"
---

# Tasks: Emit Framework Skills On Every Lifecycle (Skills Follow the Product, Not the Lifecycle)

**Input**: Design documents from `/specs/219-emit-framework-skills/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md

**Tests**: REQUIRED for this feature. The plan mandates failing-first evidence — the Feature 204 gate is *re-specified* (three source categories) and a new positive `Feature219EmitFrameworkSkillsTests` is added. No assertion is weakened.

**Organization**: Tasks are grouped by user story (US1 = P1, US2 = P2, US3 = P3) so each story is independently implementable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in each task

## Path Conventions

This is a **template-emission + validation-gate + cross-repo-coordination** feature — there is **no `src/` change**. The "app" under test is the dotnet-template instantiation path (`dotnet new fs-gg-ui` per `lifecycle × profile`). Edited artifacts live at the repo root (`.template.config/`, `template/`, `scripts/`, `tests/Package.Tests/`) and in the cross-repo `FS-GG/.github` registry.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline and confirm the exact edit sites before touching anything.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project so pre-existing failures are known up front and not mistaken for regressions at merge — including `tests/Package.Tests` (release-only public-surface gate) and the `samples/**/*.Tests` projects, which the solution omits. Use the discovery-based runner so nothing silently drops out.

- [X] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/219-emit-framework-skills/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**` — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)
- [X] T002 [P] Inventory the exact edit sites in `.template.config/template.json`: confirm the 6 framework product-skill source pairs (12 `sources[]` clauses: `fs-gg-scene`, `fs-gg-skiaviewer`, `fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-ui-widgets`, `fs-gg-testing`, each with `.agents/skills/` + `.claude/skills/` destinations) each carry `… && lifecycle == "spec-kit"`; confirm `docs/skillist-reference.md` is an ungated `copyOnly` entry on the base source; confirm `template/product-skills/fs-gg-symbology/` is referenced by no source. Record the verbatim conditions for T015/T016/T020/T024.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the source-category taxonomy every story depends on, and PROVE the root-cause hypotheses against a real `dotnet new` before any edit.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** This feature's "app" is the template-instantiation path. Per the plan's Standing Assumption, the central claim ("dropping the lifecycle clause makes `sdd`/`none` emit skills while `spec-kit` stays byte-identical") is **unverified until a real `dotnet new` runs**. T004 reproduces the bug live BEFORE any fix — do not defer it to per-story checkpoints.

- [X] T003 Build the 3-category source-classification map (framework-product-skill / lifecycle-workspace / product) from the current `.template.config/template.json`, per data-model.md "Template source category". Record the pre-edit counts (gated-bucket includes the 12 skill sources today) so the post-edit thresholds in T011 are fixed against reality, not guessed.
- [X] T004 **Early live smoke run (reproduce the bug, PRE-change)**: run `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx` and `dotnet new fs-gg-ui --name Demo --profile app --lifecycle sdd -o /tmp/demo-sdd --allow-scripts yes`; record `find /tmp/demo-sdd -name SKILL.md | wc -l` = **0** (the bug), and confirm a `--lifecycle spec-kit` scaffold currently emits the framework skills. Capture this as live evidence (or `environment-limited` with disclosed substitute) — this is the before-state for the SC-001 (0 → N) and FR-004 (byte-identical) proofs.
- [X] T005 [P] Verify-on-implement gate for `fs-gg-symbology` (R5): inspect `template/product-skills/fs-gg-symbology/` (`SKILL.md` + `reference.fsx`) and confirm content is product-appropriate (no framework-repo-only paths). Decision tree (all three branches): **(a) product-appropriate AND byte-equal** to the spec-kit blanket-copy variant (the repo-root `.agents/skills/fs-gg-symbology/`) → proceed to wire it (T010); **(b) product-appropriate but NOT byte-equal** to the blanket-copy variant → adding the overwrite would change the `spec-kit` output and red GV-3 (FR-004/SC-003), so either reconcile the two so they are byte-equal before wiring, or fall back to "record as not-vendored"; **(c) framework-internal paths** → fall back to "record as not-vendored" and note it in research.md R5; T010 then records the decision instead of adding sources. Capture the byte-equality check result (e.g. `diff -r` of the two source dirs) as the evidence for which branch was taken.
- [X] T006 Draft the validator/test seam first (before implementation): in `scripts/validate-lifecycle-template.fsx` and `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`, sketch the 3-category classification signature (check `source.StartsWith "template/product-skills/"` BEFORE the target-path test) and the named `docs/skillist-reference.md` exception, per research.md R3/R4 — no behavior change yet, just the seam every story edits.

**Checkpoint**: Root-cause map confirmed against a live `dotnet new` (sdd/none = 0 skills, spec-kit = present); symbology decision made; validator seam drafted — user-story implementation can begin.

---

## Phase 3: User Story 1 - Framework skills are vendored regardless of lifecycle choice (Priority: P1) 🎯 MVP

**Goal**: Framework `fs-gg-*` skills emit under `lifecycle ∈ {spec-kit, sdd, none}` per the product profile, with `spec-kit` byte-identical and the lifecycle workspace still `spec-kit`-only.

**Independent Test**: Scaffold with `--lifecycle sdd` and again `--lifecycle none` → product contains the profile-appropriate framework `SKILL.md` files under both `.agents/skills/` and `.claude/skills/`. Scaffold `--lifecycle spec-kit` → byte-identical to pre-change. Lifecycle workspace (`.specify/`, `speckit-*`, constitution, agent-context) absent under `sdd`/`none`.

### Tests for User Story 1 (write FIRST, ensure they FAIL before implementation) ⚠️

- [X] T007 [P] [US1] Create `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs` (NEW) asserting the positive G-EMIT facts: for each profile, `sdd` and `none` products contain exactly the profile-appropriate `fs-gg-*` `SKILL.md` set (app → scene/skiaviewer/elmish/keyboard-input/ui-widgets/symbology; headless-scene → scene/symbology; governed → scene/testing/symbology; sample-pack → scene/skiaviewer/elmish/symbology) under both `.agents/skills/` and `.claude/skills/` — per data-model.md matrix. Register it in `tests/Package.Tests/Package.Tests.fsproj`. Run and confirm it FAILS (skills absent today under sdd/none).
- [X] T008 [P] [US1] Amend `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`: migrate the `gatedSourceAudit()` mirror to the 3-category model; update GV-2 counts (gated drops by 12, framework-skill ≥ 12, product ≥ 3 — fix exact numbers against post-edit `template.json`); update GV-4/GV-5 so `sdd`/`none` expect framework-skills-PRESENT under `.agents/skills/fs-gg-*` (was: absent); keep GV-3 (`spec-kit` byte-identical) intact. Run and confirm the count/expectation assertions FAIL against the unedited `template.json`.

### Implementation for User Story 1

- [X] T009 [US1] In `.template.config/template.json`, drop the `&& lifecycle == "spec-kit"` conjunct from the 12 framework product-skill source clauses (the 6 skills × 2 destinations confirmed in T002), leaving the profile predicate intact (FR-001/FR-002).
- [X] T010 [US1] In `.template.config/template.json`, add the `fs-gg-symbology` source pair (`template/product-skills/fs-gg-symbology/` → `.agents/skills/fs-gg-symbology/` and `→ .claude/skills/fs-gg-symbology/`), profile-gated to the scene-bearing set (`app || headless-scene || governed || sample-pack`), with NO lifecycle clause (FR-007 / R5). **Only take this "wire it" path if T005 landed on branch (a)** — product-appropriate AND byte-equal to the spec-kit blanket-copy variant, so the new overwrite keeps `spec-kit` byte-identical (GV-3 at T013 is the proof). If T005 landed on branch (b) "not byte-equal" or branch (c) "framework-internal" and chose "not-vendored", instead record that decision in research.md R5 and skip the source add (do NOT add a source that would red GV-3).
- [X] T011 [US1] Amend `scripts/validate-lifecycle-template.fsx` `verifyGatedSources()` to the 3-category model: classify framework-product-skill FIRST by `source.StartsWith "template/product-skills/"` (assert `¬lifecycle=="spec-kit"` ∧ has-profile-predicate), then lifecycle-workspace by target prefix, then product; update count thresholds; update the live-run `gatedAbsent`/`diff-vs-default` logic so `.agents/skills/fs-gg-*` counts as **product-present** (not gated-absent) and `diff-vs-default=gated-only` still holds (R3 live-run impact).
- [X] T012 [US1] Regenerate the readiness artifact and run the env-free verdict-core: `dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report` → confirm `verdict-core OK` and that `specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md` records the 3-category `gated-condition` line (per data-model.md "validation report" transitions).
- [X] T013 [US1] Run the live `dotnet new` matrix proof: `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx` → confirm `spec-kit/P: diff-vs-today=none` (FR-004/SC-003), `sdd|none/P: product-present=ok` + `framework-skills-present=ok (<n> SKILL.md)` (FR-001/SC-001), workspace still absent under sdd/none (FR-003). Then `dotnet test tests/Package.Tests` → Feature 219 (T007) and amended Feature 204 (T008) now GREEN.

**Checkpoint**: US1 fully functional — `find … SKILL.md` non-empty under `sdd`/`none`, `spec-kit` byte-identical, both gates green. This is the MVP and closes the core of #30.

---

## Phase 4: User Story 2 - The skill catalog never dangles (Priority: P2)

**Goal**: Every entry in an emitted `docs/skillist-reference.md` resolves to a present skill; no catalog is emitted that lists absent skills.

**Independent Test**: For each lifecycle×profile, cross-check every catalog reference against skills actually present → zero unresolved. Under `sdd`/`none` the catalog is not emitted (FR-006); under `spec-kit` it is present and resolves (FR-005).

### Tests for User Story 2 (write FIRST, ensure they FAIL before implementation) ⚠️

- [X] T014 [P] [US2] Add the catalog-resolution assertions to `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs` (G-CATALOG): for every lifecycle×profile, every skill reference in the emitted `docs/skillist-reference.md` resolves to a present skill (zero dangling, SC-002), and the catalog is ABSENT under `sdd`/`none`. Run and confirm it FAILS today (ungated catalog dangles under sdd/none).

### Implementation for User Story 2

- [X] T015 [US2] In `.template.config/template.json`, move `docs/skillist-reference.md` emission from the base source's ungated `copyOnly` list to a `lifecycle == "spec-kit"`-gated source, preserving `copyOnly` (no `sourceName` substitution so governance tokens stay verbatim) — R4 mechanics. Source: `template/base/docs/skillist-reference.md`.
- [X] T016 [US2] Add the named `docs/skillist-reference.md` exception to the lifecycle-workspace classification in BOTH `scripts/validate-lifecycle-template.fsx` and the `tests/Package.Tests/Feature204LifecycleTemplateTests.fs` mirror (a `spec-kit`-gated product-path doc is intentional, not a mis-gated product source) — R4.
- [X] T017 [US2] Verify: `dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report` stays green with the exception; live matrix shows `catalog-dangling: none`, catalog absent under `sdd`/`none`, present+resolving under `spec-kit`; `dotnet test tests/Package.Tests` → T014 GREEN.

**Checkpoint**: US1 AND US2 both hold independently — skills emit on every lifecycle AND the catalog never dangles.

---

## Phase 5: User Story 3 - Scaffolded task metadata matches the product's test framework (Priority: P3)

**Goal**: The scaffolded task metadata declares no required test skill naming a test framework the product does not use (`xunit`). Fix in-repo if Rendering-owned; otherwise route to the owner (R6 / FR-008).

**Independent Test**: Inspect scaffolded task metadata → declared required test skill matches the product's actual test framework; no `xunit` reference.

### Implementation for User Story 3

- [X] T018 [US3] Re-confirm ownership: `grep -rIE 'xunit|requiredSkills|tasks\.yml' .` across the repo (incl. `.specify/`, `template/`) → confirm NO Rendering-owned source emits the `xunit` required-skill (per R6). If a source IS found, correct it in place and add a guard assertion; record the result.
- [X] T019 [US3] Since the offending metadata is generated downstream (not in-repo), file it as a cross-repo request to the owning repository via the `cross-repo-coordination` skill (GitHub issue + Coordination board entry referencing #30/§5.2/FR-008) and record the routing in `specs/219-emit-framework-skills/` (e.g. a note in research.md R6 / a routing record). FR-008 is satisfied by the routing, not an in-repo edit. NOTE: verification of US3's acceptance scenario (a scaffolded product's declared required test skill matching its framework) is owned by the receiving repo's issue — this feature does not assert it in-repo because the metadata is generated downstream; that gap is intentional and recorded here.

**Checkpoint**: All three user stories resolved — US3 routed and recorded.

---

## Phase 6: Polish & Cross-Cutting Concerns (FR-009 + closure)

**Purpose**: Land the cross-repo registry delta and close the contract/board item — the "no half-landing" constraint requires these with the code.

- [X] T020 Update `FS-GG/.github` `registry/dependencies.yml` → `fs-gg-ui-template.parameters.lifecycle.notes`: refine "Gates `.specify/`, constitution, `.agents/`, …" to state framework product-skills under `.agents/skills/fs-gg-*` / `.claude/skills/fs-gg-*` are NOT lifecycle-gated (profile-gated, emit under every lifecycle); only the lifecycle workspace is gated. Mark additive / surface-neutral (R7 / FR-009 / contract Registry delta).
- [X] T021 [P] Update the `FS-GG/.github` `docs/registry/compatibility.md` projection of the T020 change.
- [X] T022 Run the full `quickstart.md` validation (Scenarios 1–5) end-to-end and confirm all Done signals: `result: pass`, all `spec-kit/* diff-vs-today=none`, all `sdd|none/* framework-skills-present=ok`, `catalog-dangling: none`, `symbology: wired (scene-profiles)` (or `not-vendored`).
- [X] T023 Capture per-phase feedback via the `fs-gg-feedback-capture` skill into `specs/219-emit-framework-skills/feedback/` (process friction, generalizable-code candidates, severity).
- [X] T024 Close `FS-GG/FS.GG.Rendering#30` and advance its Coordination board item `In review → Done` (after PR merge + registry update), per data-model.md state transition and FR-009. (Note: feed exposure / version bump is owned by the `speckit-merge` release flow, not hard-coded here.)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories. T004 (live bug reproduction) and T005 (symbology decision) gate the implementation.
- **User Stories (Phase 3–5)**: All depend on Foundational. US2 and US3 are independent of US1 but share `template.json` / the validator (sequence the edits; see below). US3 is fully independent (cross-repo routing).
- **Polish (Phase 6)**: Depends on US1–US3 being complete (registry delta describes the landed contract).

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational. The MVP.
- **US2 (P2)**: Depends only on Foundational; touches the same `template.json` and validator/test files as US1 — do US1's edits first, then US2's, to avoid same-file churn (not a logical dependency).
- **US3 (P3)**: Fully independent — can proceed any time after Setup (it's a grep + cross-repo route).

### Within Each User Story

- Tests (T007/T008, T014) MUST be written and FAIL before the implementation tasks in that story.
- `template.json` edit → validator/test amendment → env-free verdict → live matrix proof (the recurring order within US1 and US2).

### Parallel Opportunities

- T002 ‖ T001 (inventory while baseline runs).
- T005 [P] (symbology inspection) ‖ T003/T004.
- T007 [P] ‖ T008 [P] — different test files (new Feature219 vs. amended Feature204).
- T021 [P] (compatibility projection) ‖ other Phase 6 docs once T020 lands.
- **Caution**: T009/T010/T011 (US1) and T015/T016 (US2) edit the SAME files (`template.json`, the validator, the Feature204 test) — NOT parallel with each other; sequence them.

---

## Parallel Example: User Story 1 tests

```bash
# Write both failing tests together (different files):
Task: "Create tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs (positive G-EMIT facts)"   # T007
Task: "Amend tests/Package.Tests/Feature204LifecycleTemplateTests.fs to 3-category model"          # T008
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup (T001–T002).
2. Phase 2 Foundational (T003–T006) — including the **early live smoke run T004** that reproduces the bug against a real `dotnet new` before any edit.
3. Phase 3 US1 (T007–T013): failing tests → drop lifecycle clause → wire symbology → amend validator → live matrix proof.
4. **STOP and VALIDATE**: `find … SKILL.md` non-empty under `sdd`/`none`; `spec-kit` byte-identical; both gates green. This alone resolves the core of #30.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → MVP (skills emit on every lifecycle).
3. US2 → catalog never dangles.
4. US3 → task-metadata papercut routed.
5. Polish → registry delta + #30 closure (the "no half-landing" close-out).

---

## Notes

- No `src/` change — this feature edits template wiring, its validation gate, and the cross-repo registry only.
- **No half-landing** (plan constraint): the `template.json` edit, the Feature 204 gate amendment, the catalog gating, the symbology decision, and the registry delta MUST land together. A skills-emit change that reds the Feature 204 gate is not "done."
- All live proofs are `FS_GG_RUN_LIFECYCLE_VALIDATION=1`-gated and disclosed as such (provenance line) — not faked; the env-free verdict-core keeps CI deterministic.
- [P] = different files, no incomplete-task dependency. Verify tests fail before implementing. Commit after each task or logical group.
