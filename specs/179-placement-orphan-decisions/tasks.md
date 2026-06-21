---
description: "Task list for Feature 179 — Placement & Orphan Decisions (code-health phase 2)"
---

# Tasks: Placement & Orphan Decisions (Code-Health Refactoring Phase 2)

**Input**: Design documents from `/specs/179-placement-orphan-decisions/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓,
contracts/ ✓ (harness-path-map.md, package-surface-changes.md, colorpolicy-relocation.md)

**Tests**: No **new** tests are requested. This feature changes no runtime behavior — it relocates
one project and removes/relocates unreferenced code. Per the plan's standing-assumption note, the
"real evidence" is the existing regression machinery: a captured `dotnet build` + `dotnet test`
baseline diffed after each story (the two documented package-feed reds stay the only non-green
entries). No test tasks are generated; verification tasks run the existing suites instead.

**Organization**: Tasks are grouped by user story (US1→US2→US3, priority order P1→P2→P3). Each story
is independently shippable and diffed against the single captured baseline (FR-011, FR-012).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in each task

## Path Conventions

- F# multi-project solution at repo root: `src/*` packages, `tests/*` test projects,
  **new** `tools/*` tooling, `scripts/*.fsx` helpers, `FS.GG.Rendering.slnx` solution.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Capture the known-good state every story is diffed against, before any change.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** T002 MUST run **every** test project via
> the discovery-based runner (`scripts/baseline-tests.fsx` globs `*.Tests.fsproj`), not the solution
> alone — the solution deliberately omits `tests/Package.Tests` (release-only public-surface gate)
> and the `samples/**/*.Tests` package-feed consumers, which is exactly where pre-existing reds hide.
> The two documented reds (Package.Tests, ControlsGallery package-feed) must be recorded now so they
> are not mistaken for regressions at merge (FR-011, SC-005).

- [X] T001 Create the feature readiness dir `specs/179-placement-orphan-decisions/readiness/` for the baseline/post-change evidence files
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/179-placement-orphan-decisions/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; the two documented package-feed reds are flagged here, not discovered at merge)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Lock in the evidence contract and confirm the documented pre-existing reds before any
relocation/removal begins.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

> **⚠️ Early live smoke run — N/A for this feature (STANDING clause, explicitly resolved).** The
> standing requirement is to drive the real running app before any fix when the plan carries a
> defect/root-cause hypothesis. This feature carries **none**: it changes no runtime behavior of the
> shipped product (pure project relocation + removal of unreferenced code), so there is no app
> behavior to smoke and no hypothesis to confirm against a running app (see plan.md "Standing
> assumption" and research.md "Cross-cutting: baseline & evidence"). T003 records this determination
> explicitly rather than silently omitting the clause. The honest evidence is the T002 baseline,
> diffed after each story.

- [X] T003 In `specs/179-placement-orphan-decisions/readiness/baseline.md`, record the two documented pre-existing package-feed reds (Package.Tests, ControlsGallery package-feed) as the allowed non-green set, and note the early-live-smoke clause is **N/A** (no runtime behavior change, no root-cause hypothesis — per plan standing-assumption); everything else must be green
- [X] T004 Re-verify the harness reference inventory against the working tree with ripgrep before moving anything: `rg -n "tests/Rendering\.Harness/" ` and `rg -n "FS.GG.UI.Input|src/Color/"` — confirm the site counts match `contracts/harness-path-map.md` (categories 1–8) and flag any drift from the captured map before edits begin

**Checkpoint**: Baseline captured, allowed reds recorded, smoke-run N/A documented, reference
inventory confirmed — user stories can begin in priority order.

---

## Phase 3: User Story 1 - Relocate the mis-filed harness CLI (Priority: P1) 🎯 MVP

**Goal**: Move the `Rendering.Harness` production CLI from `tests/Rendering.Harness` →
`tools/Rendering.Harness` (verbatim, 39 files) and rewrite every genuine reference, so `tests/`
holds only test projects (SC-001) and no genuine reference to the old CLI path remains (SC-002).

**Independent Test**: After US1, the solution builds at the new path, `dotnet test` matches the T002
baseline, `rg "tests/Rendering\.Harness/"` returns zero genuine CLI hits, and the three helper
scripts + Feature 170 lane test resolve/pass unchanged.

> **Critical rule (research R1, contract §5–6):** the *test* project `tests/Rendering.Harness.Tests/`
> does **NOT** move — only the CLI `Rendering.Harness` moves. Literals naming the `.Tests` project
> keep their `tests/` path; classify each category-5/6 literal individually before editing.

### Implementation for User Story 1

- [X] T005 [US1] Move the harness project verbatim: `git mv tests/Rendering.Harness tools/Rendering.Harness` (all 39 `.fs`/`.fsi` files incl. `Rendering.Harness.fsproj`, `TestAssertions.fs`); create the top-level `tools/` dir as its first resident
- [X] T006 [US1] Update the solution `FS.GG.Rendering.slnx`: repoint the harness `<Project Path="tests/Rendering.Harness/Rendering.Harness.fsproj" />` → `tools/Rendering.Harness/...`; leave the `tests/Rendering.Harness.Tests/...` entry **unchanged** (contract §1)
- [X] T007 [US1] Update the dependent test project `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`: `ProjectReference ..\Rendering.Harness\Rendering.Harness.fsproj` → `..\..\tools\Rendering.Harness\Rendering.Harness.fsproj` (depth changes; contract §2)
- [X] T008 [P] [US1] Update the 4 linked `TestAssertions.fs` `<Compile Include>` paths `..\Rendering.Harness\TestAssertions.fs` → `..\..\tools\Rendering.Harness\TestAssertions.fs` in `tests/Layout.Tests`, `tests/Scene.Tests`, `tests/SkiaViewer.Tests`, `tests/Controls.Tests` (~line 11 each; contract §3)
- [X] T009 [P] [US1] Update the 3 helper scripts' harness `.fsproj` arg `ArgumentList.Add("tests/Rendering.Harness/Rendering.Harness.fsproj")` → `tools/...` in `scripts/check-agent-skill-parity.fsx`, `scripts/run-validation-lanes.fsx`, `scripts/refresh-local-feed-and-samples.fsx` (~line 12 each; contract §4)
- [X] T010 [US1] Rewrite ONLY the CLI run-command literal in `tools/Rendering.Harness/Live.fs:170` (`dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj …` → `tools/...`); leave the **test-project** literals in `Compositor.fs:2617,2876` and `ValidationLanes.fs:431,448,523` pointing at `tests/Rendering.Harness.Tests/...` (contract §5 — re-grep after to confirm no CLI literal under `tests/` and no test literal flipped to `tools/`)
- [X] T011 [US1] Verify (do NOT edit) the Feature 170 lane-test assertion at `tests/Rendering.Harness.Tests/Feature170RetainedInspectionLaneTests.fs:27` — it asserts the lane command contains the **test** project path (`ValidationLanes.fs:523`, which stays `tests/`), so it must remain unchanged and still pass (contract §6)
- [X] T012 [P] [US1] Update the 5 FSX evidence scripts' `#r` DLL paths `…/tests/Rendering.Harness/bin/…` → `…/tools/Rendering.Harness/bin/…`, recomputing depth per file: `specs/168-skill-parity-evidence/readiness/fsi/skill-parity-authoring.fsx:6`, `specs/156-same-profile-timing/readiness/fsi/compositor-readiness-authoring.fsx:1`, `specs/156-same-profile-timing/readiness/fsi/compositor-performance-authoring.fsx:1`, and verify (no `#r` path change needed for) `specs/163-package-feed-validation-lanes/readiness/fsi/{package-feed,validation-lanes}-authoring.fsx` (`open Rendering.Harness` is a namespace, stays). If any cannot be safely updated, call it out per spec Edge Cases (contract §7)
- [X] T013 [P] [US1] Update the 2 harness mentions in the skill doc `src/Diagnostics/skill/SKILL.md` (`tests/Rendering.Harness/SkillParity.fs` and the `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj …` example) → `tools/...` (contract §8)
- [X] T014 [US1] Build the solution: `dotnet build FS.GG.Rendering.slnx -c Release` — confirm the harness, `Rendering.Harness.Tests`, and the 4 `TestAssertions.fs`-linked test projects build at the new path
- [X] T015 [US1] Run the full suite and diff vs baseline: `dotnet test FS.GG.Rendering.slnx -c Release` matches T002; then run the 3 lanes — `dotnet fsi scripts/run-validation-lanes.fsx`, `scripts/check-agent-skill-parity.fsx`, `scripts/refresh-local-feed-and-samples.fsx` — invoking the harness at its new path with no behavior change (FR-004)
- [X] T016 [US1] Assert SC-002: `rg -n "tests/Rendering\.Harness/"` returns zero genuine **CLI** references (trailing slash; `tests/Rendering.Harness.Tests/` references legitimately remain)

**Checkpoint**: Harness lives under `tools/`; `tests/` has zero `OutputType=Exe` production CLIs
(SC-001); build green, test diff = baseline, lanes pass. US1 is independently shippable.

---

## Phase 4: User Story 2 - Retire the orphaned `FS.GG.UI.Input` package (Priority: P2)

**Goal**: Delete `src/Input/` + `tests/Input.Tests/` and unpublish the `FS.GG.UI.Input` package
(remove its surface baseline + manifest row in the **same** change), the single intentional public
package-surface removal (Tier 1), leaving the surface-drift gate internally consistent.

**Independent Test**: After US2, `src/Input/` and `tests/Input.Tests/` are gone, the surface diff
shows **only** `FS.GG.UI.Input.txt` removed, the manifest lists 12 packages (was 13), the solution
builds, and the surface-drift gate passes; `SkiaViewer`/`Controls`/`Controls.Elmish` (live
`src/KeyboardInput/` path) build unchanged.

### Implementation for User Story 2

- [X] T017 [US2] Delete the orphan project `src/Input/` (`Input.fsproj`, `KeyboardInput.fs`, `KeyboardInput.fsi`, `README.md`) and its only consumer `tests/Input.Tests/` (3 files): `git rm -r src/Input tests/Input.Tests` (FR-005)
- [X] T018 [US2] De-list both from the solution `FS.GG.Rendering.slnx`: remove the `src/Input/Input.fsproj` and `tests/Input.Tests/Input.Tests.fsproj` `<Project>` entries (FR-005)
- [X] T019 [US2] Remove the surface baseline + manifest row in the **same** change (Tier 1, contract package-surface-changes.md): delete `readiness/surface-baselines/FS.GG.UI.Input.txt` AND remove the `"FS.GG.UI.Input", "Input"` row (~line 33) from `scripts/refresh-surface-baselines.fsx` so the gate sees no orphaned baseline and no unbaselined package (FR-006, SC-004)
- [X] T020 [P] [US2] Drop the `FS.GG.UI.Input` package-inventory line from `docs/usage.md` (~line 65); leave the existing `docs/bridge/package-deprecation-notice.md` / `package-identity-migration.md` notices in place (still valid — package now removed, not renamed)
- [X] T021 [US2] Build the solution: `dotnet build FS.GG.Rendering.slnx -c Release` — confirm it builds without `src/Input`/`Input.Tests` and that `SkiaViewer`, `Controls`, `Controls.Elmish` build unchanged (FR-007)
- [X] T022 [US2] Run the full suite incl. the surface-drift gate and diff vs baseline: `dotnet test FS.GG.Rendering.slnx -c Release` matches T002 (the `SurfaceAreaTests` gate passes with Input gone); then assert `git diff --stat readiness/surface-baselines/` shows **only** `FS.GG.UI.Input.txt` deleted and the manifest now lists 12 packages, no other row changed (SC-004)

**Checkpoint**: `FS.GG.UI.Input` fully retired; exactly one public surface changed; gate consistent;
build green, test diff = baseline. US2 is independently shippable.

---

## Phase 5: User Story 3 - Retire `src/Color/` while preserving its live internal policy (Priority: P3)

**Goal**: Create a new non-packed `src/ColorPolicy` project holding `Contrast.fsi`/`Contrast.fs` +
`ColorPolicy.fs` (moved verbatim, namespace `FS.GG.UI.Color` preserved, `IsPackable=false`,
`InternalsVisibleTo Controls.Tests`); delete dead `Palettes.*` + `tests/Color.Tests/` + `src/Color/`;
repoint consumers. No shipped surface changes (Color never shipped).

**Independent Test**: After US3, `src/ColorPolicy/` builds (`IsPackable=false`, no baseline),
`Controls.Tests` Feature 108/127/131 color suites compile and pass unchanged, the policy report is
byte-identical, `src/Color/` + `tests/Color.Tests/` are gone, and no surface baseline changed for
Color (FR-010, SC-006).

> **Refinement (research R3):** `Contrast` (with `Role`/`Verdict`/`ContrastResult`) is **live** —
> depended on by `ColorPolicy.fs` and `Feature108ThemingTests.fs` — so it relocates, not deletes.
> Only `Palettes` is truly dead.

### Implementation for User Story 3

- [X] T023 [US3] Create the new project `src/ColorPolicy/ColorPolicy.fsproj`: `<IsPackable>false</IsPackable>`, `<InternalsVisibleTo Include="Controls.Tests" />`, `ProjectReference ..\Scene\Scene.fsproj`, assembly name `ColorPolicy`, compile order `Contrast.fsi` → `Contrast.fs` → `ColorPolicy.fs` (contract colorpolicy-relocation.md)
- [X] T024 [US3] Move the three live files verbatim into the new project: `git mv src/Color/Contrast.fsi src/Color/Contrast.fs src/Color/ColorPolicy.fs src/ColorPolicy/` — keep the `FS.GG.UI.Color` namespace and `module internal ColorPolicy` unchanged so consumers are edit-free (SC-006)
- [X] T025 [US3] Delete the dead `Palettes` modules and the now-empty orphan project: `git rm src/Color/Palettes.fsi src/Color/Palettes.fs` then `git rm -r src/Color` (Color.fsproj, README, remaining files) — `Palettes` was used only by `PaletteTests` (FR-008)
- [X] T026 [US3] Delete the Color test project `tests/Color.Tests/` entirely (`ContrastTests.fs`, `PaletteTests.fs`, `Program.fs`, `Color.Tests.fsproj`): `git rm -r tests/Color.Tests` — disclosed coverage reduction; `Contrast` keeps indirect coverage via Feature 108/127 (research R3, Principle V)
- [X] T027 [US3] Update the solution `FS.GG.Rendering.slnx`: remove `src/Color/Color.fsproj` and `tests/Color.Tests/Color.Tests.fsproj` `<Project>` entries; add `src/ColorPolicy/ColorPolicy.fsproj`
- [X] T028 [US3] Repoint the live consumer `tests/Controls.Tests/Controls.Tests.fsproj` (~line 180): `ProjectReference ..\..\src\Color\Color.fsproj` → `..\..\src\ColorPolicy\ColorPolicy.fsproj`; leave the Feature 108/127/131 `.fs` call sites unchanged (namespace + IVT preserved)
- [X] T029 [P] [US3] Update the two policy scripts' `#load` paths `src/Color/Contrast.fs` + `src/Color/ColorPolicy.fs` → `src/ColorPolicy/...`: `scripts/validate-design-system-template.fsx:31–32` and `scripts/generate-policy-report.fsx`
- [X] T030 [US3] Build the solution: `dotnet build FS.GG.Rendering.slnx -c Release` — confirm `src/ColorPolicy` builds (no baseline, IsPackable=false) and `Controls.Tests` compiles against the relocated project
- [X] T031 [US3] Run the color suites and policy scripts: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "Feature108|Feature127|Feature131"` passes unchanged (SC-006, FR-009); then `dotnet fsi scripts/validate-design-system-template.fsx` and `dotnet fsi scripts/generate-policy-report.fsx` produce byte-identical output (Feature 127 drift gate green)
- [X] T032 [US3] Assert FR-010/SC-004 for Color: `git diff --stat readiness/surface-baselines/` shows **no** change beyond US2's `FS.GG.UI.Input.txt` removal — no `FS.GG.UI.Color` baseline exists or is added

**Checkpoint**: `Contrast`+`ColorPolicy` live in `src/ColorPolicy` (non-packed); `Palettes` +
`src/Color/` + `tests/Color.Tests/` removed; color suites pass; no Color surface change. US3 is
independently shippable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final whole-feature acceptance against the baseline and the success criteria.

- [X] T033 Final full build + test: `dotnet build FS.GG.Rendering.slnx -c Release && dotnet fsi scripts/baseline-tests.fsx --out specs/179-placement-orphan-decisions/readiness/post-change.md` (every `*.Tests.fsproj`); diff `post-change.md` vs `baseline.md` — only the two documented package-feed reds may be non-green (FR-011, SC-005)
- [X] T034 [P] Verify the success criteria in `specs/179-placement-orphan-decisions/readiness/post-change.md`: SC-001 (`tests/` has zero `OutputType=Exe` production CLIs), SC-002 (`rg "tests/Rendering\.Harness/"` zero CLI hits), SC-003 (net source reduction ≈ −1,400 lines from Input + Palettes, no production code deleted), SC-004 (one surface delta: Input removed, gate consistent)
- [X] T035 [P] Run the quickstart.md end-to-end validation (Steps 0–3 + Final acceptance) and record the post state, confirming no genuine `tests/Rendering.Harness/` CLI reference and no unexpected surface drift remain

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T002 (baseline) **gates everything**.
- **Foundational (Phase 2)**: Depends on Setup (needs the captured baseline) — BLOCKS all stories.
- **User Stories (Phases 3–5)**: Each depends on Foundational. Sequenced **P1 → P2 → P3** per the
  plan (US1 highest-touch first under the baseline; US3 most delicate last). The stories touch
  disjoint files (harness/scripts vs `src/Input` vs `src/Color`) so they are independently
  shippable, but the plan runs them in order, each diffed against the one baseline.
- **Polish (Phase 6)**: Depends on all three stories.

### User Story Dependencies

- **US1 (P1)**: After Foundational. No dependency on US2/US3 — fully independent (MVP).
- **US2 (P2)**: After Foundational. Independent of US1/US3 (disjoint files); sequenced after US1.
- **US3 (P3)**: After Foundational. Independent of US1/US2 (disjoint files); sequenced last.

### Within Each User Story

- **US1**: T005 (move) → T006/T007/T010 (solution + dependent-project edits, same/related files,
  sequential) ; T008/T009/T012/T013 are `[P]` (different files) ; T011 verify-only after T010 ;
  T014 build → T015 test/lanes → T016 grep assertion.
- **US2**: T017 (delete) → T018 (slnx) ; T019 (surface baseline+manifest, same change) ; T020 `[P]`
  (docs) → T021 build → T022 test + surface-diff assertion.
- **US3**: T023 (new project) → T024 (move files) → T025/T026 (deletes) → T027 (slnx) →
  T028 (Controls.Tests ref) ; T029 `[P]` (scripts) → T030 build → T031 color suites + scripts →
  T032 surface assertion.

### Parallel Opportunities

- **US1**: T008, T009, T012, T013 (linked includes, helper scripts, FSX evidence, skill doc — all
  different files) can run together after the move (T005) and once the slnx/ProjectReference edits
  (T006/T007) are in place.
- **US2**: T020 (docs) `[P]` alongside the deletion/de-listing tasks.
- **US3**: T029 (policy-script `#load` paths) `[P]` alongside the relocation/deletion edits.
- **Polish**: T034 and T035 `[P]`.
- Across stories: the three stories edit disjoint files and *could* be parallelized by separate
  developers after Foundational, but the plan's evidence model (one baseline, diffed per story in
  priority order) favors sequential execution.

---

## Parallel Example: User Story 1 (after T005–T007)

```bash
# Independent reference rewrites — different files, no ordering between them:
Task: "T008 Update 4 linked TestAssertions.fs includes → ..\..\tools\Rendering.Harness\..."
Task: "T009 Update 3 helper scripts' harness .fsproj arg → tools/..."
Task: "T012 Update 5 FSX evidence #r DLL paths → tools/Rendering.Harness/bin/..."
Task: "T013 Update 2 harness mentions in src/Diagnostics/skill/SKILL.md → tools/..."
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup — capture the baseline (T002).
2. Phase 2 Foundational — record allowed reds, confirm reference inventory (smoke-run N/A).
3. Phase 3 US1 — relocate the harness, rewrite all references.
4. **STOP and VALIDATE**: build green, `dotnet test` = baseline, lanes pass, SC-002 grep clean.
5. Ship US1 — `tests/` is now honest (CLI moved to `tools/`).

### Incremental Delivery

1. Setup + Foundational → baseline locked.
2. US1 (harness → `tools/`) → diff vs baseline → ship (MVP).
3. US2 (retire `FS.GG.UI.Input`) → surface gate consistent → ship.
4. US3 (retire `src/Color/`, preserve `ColorPolicy`) → color suites green → ship.
5. Polish → final post-change diff + SC verification.

---

## Notes

- **No new tests** — evidence is the existing suites staying green, diffed against the T002 baseline
  (plan standing-assumption; Principle V). The only disclosed coverage reduction is deleting
  `tests/Color.Tests/` (research R3); re-homing `ContrastTests.fs` is an out-of-scope follow-up.
- **Early-live-smoke is N/A** here (no runtime behavior change, no root-cause hypothesis) — recorded
  explicitly in T003, not silently dropped.
- **Critical do-not-break rule (US1):** the `tests/Rendering.Harness.Tests/` *test* project does NOT
  move; only the `Rendering.Harness` *CLI* does. Re-grep after T010 (contract §5).
- **Tier 1 (US2):** baseline file + manifest row removed in the **same** change so the surface gate
  is never transiently inconsistent.
- `[P]` = different files, no dependencies. Commit after each task or logical group; stop at any
  checkpoint to validate the story independently.
