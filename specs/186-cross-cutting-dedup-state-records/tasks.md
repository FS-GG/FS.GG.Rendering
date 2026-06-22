---
description: "Task list for Cross-Cutting Dedup + State Records (Pattern C)"
---

# Tasks: Cross-Cutting Dedup + State Records (Pattern C)

**Input**: Design documents from `/specs/186-cross-cutting-dedup-state-records/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/internal-contracts.md, quickstart.md

**Tests**: No NEW tests are written. This is a **byte-identical-by-construction** Pattern-C refactor
(FR-007); verification is the **existing** red/green test set plus byte/semantic diff against a
pre-refactor baseline (research Decision 5). Per-story "verify" tasks below run the existing suites —
they do not author new assertions, and no assertion may be weakened (FR-008/SC-004).

**Organization**: Tasks are grouped by user story. The four stories are independently shippable
(spec Assumptions "Story independence"): US1↔US2 share the metrics path (US2's `FrameScriptState`
feeds US1's builder, so US1-first is natural but not required); US3 and US4 are independent of the
render loop and of each other.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 / US4 (user-story phases only)
- Every task gives an exact file path.

## Path Conventions

Single-repo multi-project F# library. Source under `src/`, tests under `tests/`, feature artifacts
under `specs/186-cross-cutting-dedup-state-records/`. Test command (default local tier):
`DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` (GL via X11).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm a clean starting point so the byte/surface baseline is meaningful.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every** test
> project and record the full red/green set, so pre-existing failures are known up front and not
> mistaken for regressions at merge. Do NOT hand-pick a subset: `dotnet test
> FS.GG.Rendering.slnx` deliberately omits `tests/Package.Tests` (release-only — owns the
> public-surface gate) and `samples/**/*.Tests` (package-feed consumers), which is exactly where
> Feature 175's surprises hid. Use the discovery-based runner so nothing silently drops out.

- [X] T001 Confirm repo builds at HEAD and the working tree is clean for `**/*.fsi` and `readiness/surface-baselines/` (`git status --porcelain -- '**/*.fsi' readiness/surface-baselines/` is empty); this is the SC-006 reference state for `src/Controls/RetainedRender.fsi`, `src/Controls.Elmish/ControlsElmish.fsi`, `src/Testing/TestingVisual.fsi`, `src/Testing/TestingRetainedInspection.fsi`.
- [X] T002 Establish the no-regression red/green baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/186-cross-cutting-dedup-state-records/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**` — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge — SC-004 reference).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Capture the pre-refactor baseline that every user story diffs against, and stand up the
shared-helper seam. No production edit may precede the baseline capture.

**⚠️ CRITICAL**: No user-story work can begin until T003–T005 are complete.

> **⚠️ Early "smoke run" (STANDING, adapted for a byte-identical refactor).** This phase has **no
> defect hypothesis to validate against a live app** — it is a pure structural refactor, not a fix
> (plan "Standing assumption"). The analogous, non-skippable obligation is **baseline-first**: the
> real running suites + emitted artifacts ARE captured live at HEAD **before any production edit**,
> and every story is diffed against that capture. Pulling this forward is mandatory; the plan
> requires the baseline capture as the **first** Foundational task.

- [X] T003 Capture the pre-refactor byte baseline (quickstart Step 0), BEFORE any production edit: `mkdir -p /tmp/186-baseline`; copy `readiness/surface-baselines` → `/tmp/186-baseline/surface-baselines`; copy the affected emitted artifacts (`specs/16[45]-*`, `specs/170-*`) → `/tmp/186-baseline/`; record `git diff --stat -- '**/*.fsi'` (expect empty). The Controls/Elmish suites already encode the byte-level frame/metrics expectations, so the T002 sweep is their frame/metrics baseline.
- [X] T004 Early live smoke run (refactor analog): build the whole solution and confirm the four affected suites are green at HEAD — `DISPLAY=:1 dotnet test tests/Controls.Tests tests/Elmish.Tests tests/Testing.Tests tests/Rendering.Harness.Tests -c Release` — establishing the live diff target for FR-007/FR-008 (treat this green set as the unverified-until-run reference, not the plan's prose).
- [X] T005 Confirm the shared-helper seam for the Testing project (research Decision 3, back-edge hazard): verify `src/Testing/Testing.fsproj` compile order is `TestingVisual` → `TestingRetainedInspection`, and that `src/Testing/TestingVisual.fsi` can host a `module internal …` (precedent: existing `module internal ReadinessFormatting` is absent from `readiness/surface-baselines/FS.GG.UI.Testing.txt`). This is the only `.fsi` that MAY gain new lines (internal only) and it must compile before the retained module — the seam US3/US4 both delegate into.

**Checkpoint**: Baseline captured live + surface clean + internal-helper seam confirmed — user-story implementation can begin (in priority order, or in parallel where files don't overlap).

---

## Phase 3: User Story 1 - Frame metrics defined and built once (Priority: P1) 🎯 MVP

**Goal**: Assemble the public 32-field `FrameMetrics` record at a **single** internal builder site;
the 2 full hand-spelled construction sites delegate to it (FR-001/SC-001/C-METRICS-ONE-SITE).

**Independent Test**: Build; confirm exactly one full 32-field construction remains and the 2 former
sites delegate; run `tests/Elmish.Tests` + `tests/Controls.Tests` metrics suites → identical
red/green + byte-identical emitted metrics; `ControlsElmish.fsi` diff empty.

### Implementation for User Story 1

- [X] T006 [US1] Add the internal `FrameMetricsBuilder` routine in `src/Controls.Elmish/ControlsElmish.fs` (near the public `FrameMetrics` type at lines 63–97): the single site that names all 32 fields, taking the per-frame work-reduction carriers + metadata (memo hit/miss, virtualization counts, damage triple, picture-cache triple, replay quintuple, text-cache pair, invalidation count, product/model/layout flags) and returning a fully-populated `FrameMetrics`. Omit it from `ControlsElmish.fsi` (private by absence). Values byte-identical to the hand-spelled records (FR-007).
- [X] T007 [US1] Rewrite the first full construction site `src/Controls.Elmish/ControlsElmish.fs:1423–1460` to delegate to `FrameMetricsBuilder` instead of re-spelling all 32 fields (byte-identical result).
- [X] T008 [US1] Rewrite the second full construction site `src/Controls.Elmish/ControlsElmish.fs:1957–1990` to delegate to `FrameMetricsBuilder` (byte-identical result). Leave the 4 `{ zero with … }` partial sites (`2026/2092/2132/2171`) as-is — out of FR-001 scope (research Decision 1); if routed through a builder overload they MUST stay byte-identical.
- [X] T009 [US1] Verify US1: build clean; `grep -nE "ProductModelChanged\s*=" src/Controls.Elmish/ControlsElmish.fs` shows the field in the **1** builder only (SC-001); run `DISPLAY=:1 dotnet test tests/Elmish.Tests tests/Controls.Tests -c Release` → identical red/green vs baseline + byte-identical metrics (US1-AS2); `git diff -- src/Controls.Elmish/ControlsElmish.fsi` empty (FR-009).

**Checkpoint**: `FrameMetrics` is built at exactly one site; metrics byte-identical; public surface unchanged. US1 shippable on its own.

---

## Phase 4: User Story 2 - Explicit named frame state for the render-loop god-functions (Priority: P2)

**Goal**: Collapse `RetainedRender.step`'s ~19 loose accumulators into one `FrameState` record
(shared with `init`'s cold-start seeding), and `ControlsElmish.runScriptCore`'s 7 metric carriers
into one `FrameScriptState` record that feeds the US1 builder — preserving exact update order and
values (FR-002/FR-003/FR-004/SC-002/C-STEP-STATE/C-SCRIPT-STATE).

**Independent Test**: Build; confirm `step`/`runScriptCore` declare 0 loose migrated mutables; run
`tests/Controls.Tests` + `tests/Elmish.Tests` retained-render + metrics suites → identical red/green
+ byte-identical rendered frames and metrics; both `.fsi` files diff empty.

> **⚠️ Float accumulation order (Edge Cases / research Decision 6).** Fields MUST be mutated in the
> **exact same sequence** as the former loose mutables across the 8 walks; use `mutable` record
> fields (not immutable copy-on-write) so allocation profile and accumulation order are unchanged.
> Each mutable field carries a `// mutable: hot path` disclosure (constitution III). Byte-identity is
> the gate that catches any reorder slip.

### Implementation for User Story 2

- [X] T010 [US2] Define the internal `FrameState` record with `mutable` fields in `src/Controls/RetainedRender.fs` (the 19 accumulators per data-model.md: `Tc`/`TextHits`/`TextMisses`, `NextId`, `Recomputed`, `ChangedBound`, `Shifted`, `Memo`/`MemoHits`/`MemoMisses`, `MetadataVisited`, `VirtualMaterialized`/`VirtualTotal`, `PcEntries`/`PcClock`, `PictureHits`/`PictureMisses`, `ReplaySkippedNodes`/`ReplayNativeBytes`; hold `RepaintedBoxes : ResizeArray<Rect>` by reference). Omit from `RetainedRender.fsi` (already wholly internal). Add `// mutable: hot path` to each field.
- [X] T011 [US2] Migrate `RetainedRender.step` (`src/Controls/RetainedRender.fs:1455+`) to read/write the `FrameState` record instead of the 19 loose `let mutable` bindings, preserving the **exact same mutation order** across all walks (FR-002). After this, the migrated accumulators have 0 loose mutables in the step region.
- [X] T012 [US2] Converge `RetainedRender.init`'s cold-start seeding (`src/Controls/RetainedRender.fs:1289–1341`) onto the **same** `FrameState` shape: seed `NextId=0UL`, `Memo=Map.empty`, `PcEntries=Map.empty`, `PcClock=0` via the shared record so cold-start cache seeding and first-frame output are byte-identical (FR-003/US2-AS2).
- [X] T013 [US2] Define the internal `FrameScriptState` record in `src/Controls.Elmish/ControlsElmish.fs` holding the 7 metric carriers (`LastMemo`, `LastVirtual`, `LastDamage`, `LastPicture`, `LastReplay`, `LastTextCache`, `LastInvalidated`; lines 1849–1865). The 3 workflow-state mutables (`model`/`retained`/`lastRender`, 1840–1845) MAY also move in for cohesion but are not required (research Decision 2). Omit from `.fsi`.
- [X] T014 [US2] Migrate `ControlsElmish.runScriptCore` (`src/Controls.Elmish/ControlsElmish.fs:1835+`) to thread `FrameScriptState` instead of the 7 loose metric-carrier mutables, and wire those carriers as inputs to the `FrameMetricsBuilder` from US1 (T006). After this, the migrated carriers have 0 loose mutables.
- [X] T015 [US2] Verify US2: build clean; `sed -n '1455,1900p' src/Controls/RetainedRender.fs | grep -c 'let mutable'` → 0 for migrated accumulators and `sed -n '1835,1900p' src/Controls.Elmish/ControlsElmish.fs | grep -c 'let mutable'` → 0 for migrated carriers (SC-002); run `DISPLAY=:1 dotnet test tests/Controls.Tests tests/Elmish.Tests -c Release` → identical red/green + byte-identical rendered frames + metrics (US2-AS4); `git diff -- src/Controls/RetainedRender.fsi src/Controls.Elmish/ControlsElmish.fsi` empty.

**Checkpoint**: `step`/`init`/`runScriptCore` operate over named state records; frames + metrics byte-identical; surface unchanged. US2 shippable.

---

## Phase 5: User Story 3 - Inspection-validation logic written once (Priority: P3)

**Goal**: Extract the validate-exceptions → compute-unused/invalid → diagnostics → derive-status
algorithm to one shared `internal` routine; both public `validateCheck` functions delegate, with the
severity asymmetry preserved (retained admits `Warning`+`ReviewRequired`; visual admits neither)
(FR-005/SC-003/C-VALIDATION-ONE-DEF).

**Independent Test**: Build; confirm a single shared routine backs both validators; run
`tests/Testing.Tests` Feature165 (visual) + Feature170 (retained) → identical red/green; a
`Warning`-severity retained finding handled as before and the visual path still rejects/omits
`Warning`.

### Implementation for User Story 3

- [X] T016 [US3] Declare the shared inspection-validation routine as `internal` in a `module internal …` in `src/Testing/TestingVisual.fsi` (the seam from T005; compiles before the retained module), parameterized over (a) accepted-severity predicate, (b) status/severity result type, (c) diagnostic wording (data-model.md "InspectionValidation routine"). No public `val`/`type` added.
- [X] T017 [US3] Implement the shared validation algorithm in `src/Testing/TestingVisual.fs` (the validate→compute→diagnostics→status logic once), and rewrite `VisualInspectionValidation.validateCheck` (`src/Testing/TestingVisual.fs:995–1065`) to delegate to it with the visual knobs (accepted severity = `Blocking` only, line 1014; no `Warning`/`ReviewRequired`). Public signature unchanged.
- [X] T018 [US3] Rewrite `RetainedInspectionValidation.validateCheck` (`src/Testing/TestingRetainedInspection.fs:373–438`) to delegate to the shared routine with the retained knobs (accepted severity = `Blocking || Warning`, line 392; derive `ReviewRequired` when a `Warning` is present, lines 427–428). Public signature unchanged; severity asymmetry preserved (FR-005, Edge Cases).
- [X] T019 [US3] Verify US3: build clean; confirm one shared validation definition backs both call sites; run `DISPLAY=:1 dotnet test tests/Testing.Tests -c Release` → identical red/green for Feature165 + Feature170; confirm a retained `Warning` finding still derives `ReviewRequired` and the visual path still rejects/omits `Warning` (US3-AS2); `git diff -- '**/*.fsi'` shows only NEW `internal` lines in `TestingVisual.fsi`, no public change; `diff -r /tmp/186-baseline/surface-baselines readiness/surface-baselines` empty (SC-006).

**Checkpoint**: One validation algorithm; both validators delegate; severities preserved; surface baseline empty. US3 shippable.

---

## Phase 6: User Story 4 - Single markdown managed-section updater (Priority: P4)

**Goal**: Unify the three `updateManagedSection` writers behind one shared `internal` abstraction
implementing `(0,0)`→append / `(1,1)`→replace / else→fail-loud identically for all callers
(FR-006/FR-011/SC-003/C-SECTION-ONE-DEF).

**Independent Test**: Build; confirm one shared updater backs all three writers; run
`tests/Testing.Tests` managed-section suites; re-emit affected summary artifacts and byte-compare to
baseline, including the fail-loud branch on duplicate/imbalanced markers.

### Implementation for User Story 4

- [X] T020 [US4] Declare the shared `ManagedSection` updater as `internal` in the `module internal …` in `src/Testing/TestingVisual.fsi` (alongside T016): inputs = target content, begin/end marker pair, new section body, per-writer separator/wording params; output = updated content or loud failure (data-model.md "ManagedSection updater"). No public surface change.
- [X] T021 [US4] Implement the shared `ManagedSection` algorithm once in `src/Testing/TestingVisual.fs`: count `(begin,end)` markers → `(0,0)` append (with separator) / `(1,1)` replace between markers / **else** fail loud (report error / refuse to write — never silent last-wins, FR-011). Rewrite `VisualReadinessMarkdown.updateManagedSection` (`src/Testing/TestingVisual.fs:642–688`) and `VisualInspectionMarkdown.updateManagedSection` (`src/Testing/TestingVisual.fs:1271–1311`) to delegate. Public signatures unchanged.
- [X] T022 [US4] Rewrite the third writer `RetainedInspectionMarkdown.updateManagedSection` (`src/Testing/TestingRetainedInspection.fs:654–694`) to delegate to the shared `ManagedSection` updater. Public signature unchanged.
- [X] T023 [US4] Verify US4: build clean; confirm one shared updater backs all three writers (SC-003); run `DISPLAY=:1 dotnet test tests/Testing.Tests -c Release` → identical red/green incl. the fail-loud branch (US4-AS4); re-emit affected summaries and `diff -r /tmp/186-baseline/170-* specs/170-*` → no diff where logic was identical (US4-AS2/3); `git diff -- '**/*.fsi'` shows only internal additions.

**Checkpoint**: One managed-section updater; all three writers delegate; fail-loud preserved; artifacts byte-identical. US4 shippable.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final whole-feature acceptance against the baseline and the success criteria.

- [X] T024 Final acceptance sweep (quickstart Step 5): `mkdir -p /tmp/186-after`; `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release > /tmp/186-after/test-sweep.txt 2>&1`; diff the Passed/Failed/error lines against the recorded red/green baseline `specs/186-cross-cutting-dedup-state-records/readiness/baseline.md` (T002) — NOT `/tmp/186-baseline`, which holds only surface-baselines + artifacts (T003); also re-run `scripts/baseline-tests.fsx` if Package/sample tiers matter → **identical** red/green set, no assertion weakened (SC-004/FR-008).
- [X] T025 [P] Verify the public surface is byte-identical (SC-006): `diff -r /tmp/186-baseline/surface-baselines readiness/surface-baselines` empty; `git diff -- '**/*.fsi'` shows only NEW `internal` lines in `src/Testing/TestingVisual.fsi` (no public `val`/`type` added/changed/removed); confirm no package version bump; spot-check `tests/Package.Tests` + `tests/Controls.Tests/PublicSurfaceTests.fs` + `Feature170RetainedInspectionSurfaceTests.fs` pass.
- [X] T026 [P] Verify no new project/dependency/inter-project reference (FR-010/C-NO-NEW-DEP): `git diff -- '**/*.fsproj' 'FS.GG.Rendering.slnx'` shows no new `<ProjectReference>` / `<PackageReference>` / `<Compile>` of a new file/project.
- [X] T027 [P] SC-007 walkthrough (no commit required): document in `specs/186-cross-cutting-dedup-state-records/readiness/` that adding one new per-frame metric field requires editing exactly **1** builder site (`FrameMetricsBuilder`) for it to appear on every frame-emit path.
- [X] T028 [P] Capture per-phase feedback via the `fs-gg-feedback-capture` skill into `specs/186-cross-cutting-dedup-state-records/feedback/` (process friction, generalizable-code candidates, severity).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories** — the byte baseline (T003) must be captured before any production edit.
- **User Stories (Phase 3–6)**: All depend on Foundational completion. US1 → US2 → US3 → US4 in priority order, or in parallel where files don't overlap (see below).
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. Self-contained in `ControlsElmish.fs`. No dependency on other stories.
- **US2 (P2)**: After Foundational. `RetainedRender.fs` part is independent; the `runScriptCore`/`FrameScriptState` part (T014) **wires into US1's `FrameMetricsBuilder`** (T006) — do US1 first, or stub the builder call. Still independently testable.
- **US3 (P3)**: After Foundational. Lives in `src/Testing`, independent of the render loop and of US4.
- **US4 (P4)**: After Foundational. Lives in `src/Testing`, independent of the render loop and of US3. US3 (T016) and US4 (T020) both add to the same `module internal` in `TestingVisual.fsi` — coordinate that one shared declaration block if run in parallel.

### Within Each User Story

- Define the internal helper/record/seam → rewrite each call site to delegate → verify against baseline.
- Story complete (byte-identical + surface clean) before moving to the next priority.

### Parallel Opportunities

- Setup T001 and the baseline run T002 are sequential (T002 reflects the clean state).
- After Foundational: US1 (Controls.Elmish), US3+US4 (Testing) touch **disjoint files** and can run in parallel with US2's `RetainedRender.fs` work. The only cross-file couplings: US2-T014 → US1-T006 (builder), and US3-T016 / US4-T020 share one `.fsi` `module internal` block.
- Polish T025, T026, T027, T028 are independent `[P]` checks/writes.

---

## Parallel Example: post-Foundational fan-out

```bash
# Disjoint-file stories can proceed concurrently (US1-first if you want the builder ready for US2-T014):
Task: "US1 — FrameMetricsBuilder + delegate 2 sites in src/Controls.Elmish/ControlsElmish.fs"
Task: "US2 (step/init) — FrameState record in src/Controls/RetainedRender.fs"
Task: "US3 — shared validation routine in src/Testing/TestingVisual.fs + delegates"
Task: "US4 — shared ManagedSection updater in src/Testing/TestingVisual.fs + 3 delegates"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (CRITICAL — captures the byte baseline live before any edit) → 3. Phase 3 US1 → 4. **STOP and VALIDATE**: metrics built at 1 site, byte-identical metrics, `.fsi` empty diff → 5. ship if ready.

### Incremental Delivery

1. Setup + Foundational → baseline ready.
2. US1 → verify byte-identical metrics → ship (MVP).
3. US2 → verify byte-identical frames + metrics → ship.
4. US3 → verify validation red/green + severity asymmetry → ship.
5. US4 → verify section append/replace/fail-loud + artifact byte-identity → ship.
6. Polish → final whole-solution sweep + surface/dep/SC-007 checks.

---

## Notes

- This is **byte-identical-by-construction**: every helper re-expresses existing values; the gate is "same red/green + byte-identical frames/metrics + empty surface diff."
- No new tests are authored; no assertion may be weakened to pass (FR-008/SC-004).
- New records/builders are private **by `.fsi` absence**; the only `.fsi` that may gain lines is `TestingVisual.fsi` (internal-only) (FR-009/research Decision 3).
- Preserve float accumulation order in US2 (Edge Cases) and fail-loud at every deduplicated site (FR-011).
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
