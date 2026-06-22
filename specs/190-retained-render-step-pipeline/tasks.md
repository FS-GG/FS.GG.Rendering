---
description: "Task list for RetainedRender.step pipeline decomposition (feature 190)"
---

# Tasks: `RetainedRender.step` Pipeline Decomposition (Pattern B + C)

**Input**: Design documents from `specs/190-retained-render-step-pipeline/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/stage-contracts.md, quickstart.md

**Tests**: INCLUDED — the spec explicitly requires per-stage isolation unit tests (FR-003 / SC-003,
US1 acceptance scenario 2) and the injected-regression gate (FR-015 / SC-008, US3). Test tasks are
therefore first-class, not optional.

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P3) for independent
implementation and testing. This is a **Tier 2** internal refactor targeting byte-identical output.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup/Foundational/Polish have no story label)
- Exact file paths are in each description.

## Path Conventions

Single F# library project: production code in `src/Controls/`, tests in `tests/Controls.Tests/` and
`tests/Elmish.Tests/`, surface gate in `tests/Package.Tests/`. Repo-root commands use the solution
`FS.GG.Rendering.slnx`. GL suites run under `DISPLAY=:1`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Capture the immutable pre-change baseline EVERYTHING diffs against (FR-012). No production
edit may precede this phase.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Use the discovery-based runner so no test
> project silently drops out (the slnx omits `Package.Tests` + samples, exactly where Feature 175's
> surprises hid). Record the full red/green set up front so pre-existing reds are not mistaken for
> regressions at merge.

- [X] T001 Create the readiness scaffold `specs/190-retained-render-step-pipeline/readiness/` with placeholder files `baseline.md`, `golden-hash-review.md`, `line-counts.md`, `perf-budget.md`, `us2-decision.md`, `final-tests.md` (mirrors the 189 readiness convention)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/190-retained-render-step-pipeline/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — recording the full red/green set; pre-existing reds flagged here)
- [X] T003 [P] Capture the pre-change byte-identity + perf + surface references per quickstart Step 0: golden-hash corpus fingerprints (`tests/Controls.Tests` Retained/hashScene/Feature174 → `/tmp/baseline-hashes.log`), per-frame perf-lane numbers (`tests/Elmish.Tests` Feature160/161/167/173 → `/tmp/baseline-perf.log`), and a copy of `readiness/surface-baselines/FS.GG.UI.Controls.txt`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Stand up the compile-order seams and the regression gate that EVERY user story depends on.
US1 cannot be accepted without the gate (US3 is a hard gate on US1, per spec).

**⚠️ CRITICAL**: No user-story stage extraction may begin until T004–T008 are complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** The byte-identity claim (threading `FrameState`
> through named stages preserves frame bytes) is an **unverified hypothesis** until the real app is
> driven. Feature 175 showed the deterministic core can pass while the running app stays broken. T005
> drives the live render path BEFORE any stage edit, establishing the live reference the gate protects.

- [X] T004 **Compile probe (research R6)**: add STUB `val internal diffStage/layoutStage/paintStage/assemblyStage` + `type internal FrameState` + `type internal FrameContext<'msg>` to `src/Controls/RetainedRender.fsi`, give them stub bodies in `src/Controls/RetainedRender.fs`, create a stub `src/Controls/Internal/CompositorPolicy.fs`, register it before `RetainedRender.fs` in `src/Controls/Controls.fsproj`, then `dotnet build FS.GG.Rendering.slnx -c Debug` to PROVE no producer→consumer back-edge and that the retained type family resolves in the new compile order (FR-009). A back-edge or a >250-line residual stage triggers the R3(a) type-re-home fallback — record the outcome in `readiness/baseline.md`
- [X] T005 **Early live smoke run**: drive the real viewer/sample through `init` + a sequence of `step` frames under `DISPLAY=:1` (use the `run` / `fs-gg-skiaviewer` path), exercise a localized change, a theme switch, and an idle frame, and capture live render evidence confirming the pre-change pipeline behaviour (or `environment-limited` with a disclosed substitute per Feature-168 rules). This is the live reference the byte-identity gate protects
- [X] T006 Relocate the step-INDEPENDENT policy cluster (feature-159 reuse/promotion family + feature-147 `unionArea`/`damageRegionSet`/`placementDamage`/`classifyDamageFallback`/`promotionDecision`/`snapshotVerdict` + their `DamageSetInputs`/`PromotionInputs`/`Feature159*`/`Compositor*`/`SnapshotResourceVerdict` types) from `src/Controls/RetainedRender.fs` into `src/Controls/Internal/CompositorPolicy.fs` (Pattern E, research R3); keep the namespace so unqualified consumer refs resolve; update internal qualified call sites + `RetainedRender.fsi`; build green and confirm byte-identical output (size enabler toward SC-001; no back-edge because the cluster references no stage)
- [X] T007 Promote `FrameState` from `type private` to a namespace/module `internal` type and finalize the `type internal FrameContext<'msg>` shape in `src/Controls/RetainedRender.fsi` (Constitution I/II — seams drafted before bodies; visibility declared in `.fsi`, no `.fs` access modifier; all `internal` ⇒ public surface unchanged)
- [X] T008 Stand up the regression-gate harness in `tests/Controls.Tests/Feature190GateTests.fs`: (a) golden-hash corpus equivalence check vs `/tmp/baseline-hashes.log`, (b) a golden-hash REVIEW step (delta surfaces for sign-off, never a silent accept — FR-005), (c) per-frame alloc-count + frame-time budget assertions wired onto the existing lanes 160/161/167/173 (FR-006), and (d) a `retained-step-*` trace-span parity assertion (FR-008). Harness only here; the injected-regression proof is T024

**Checkpoint**: Compile order proven, live reference captured, gate harness live — stage extraction can begin.

---

## Phase 3: User Story 1 - `step` is a stage composition (Priority: P1) 🎯 MVP

**Goal**: Re-express `step` as a short composition of four named, independently testable internal
stages (diff → layout → paint → assembly) threading `FrameState` + `FrameContext`, byte-identical to
today's output, public surface unchanged.

**Independent Test**: Stage unit tests pass in isolation; the scene corpus rendered through the
composed `step` is byte-identical (golden-hash zero-delta or reviewed delta); surface diff empty; perf
within budget; full red set unchanged.

### Tests for User Story 1 (write FIRST, ensure they FAIL before extraction) ⚠️

- [X] T009 [P] [US1] `diffStage` isolation test (contract C-DIFF) in `tests/Controls.Tests/Feature190StagePipelineTests.fs`: crafted prev/next incl. a duplicate-key tree → asserts patch/diagnostics/dirty-set match the inline result and `KeyCollision` fires (FR-010)
- [X] T010 [P] [US1] `layoutStage` isolation test (C-LAYOUT) in `tests/Controls.Tests/Feature190StagePipelineTests.fs`: crafted `dirty` set → asserts `LayoutResult.Invalidated`/`Remeasured`/`ThemeChanged`; an empty dirty set re-measures nothing (idle-frame edge case)
- [X] T011 [P] [US1] `paintStage` isolation test (C-PAINT) in `tests/Controls.Tests/Feature190StagePipelineTests.fs`: Keep/Replace/Update + ChildInsert/Move/Remove cases → asserts the `RetainedNode` and the `st` mutations (Recomputed/Shifted/ChangedBound/Memo*/RepaintedBoxes) match the inline build
- [X] T012 [P] [US1] `assemblyStage` isolation test (C-ASM) in `tests/Controls.Tests/Feature190StagePipelineTests.fs`: golden comparison of all 40 `WorkReductionRecord` fields for a crafted `(st, newRoot, layout, diff)`
- [X] T013 [US1] Composition byte-identity + trace-parity test (C-COMPOSE, C-TRACE) in `tests/Controls.Tests/Feature190StagePipelineTests.fs`: corpus scenes + `hashScene` zero-delta vs baseline, and `FS_GG_RENDER_LAG_TRACE=1` emits the full pre-change `retained-step-*` span set. Give the trace-parity test an Expecto label containing the token `trace` so the quickstart `--filter "Feature190.*trace"` selects it

### Implementation for User Story 1

- [X] T014 [US1] Extract `diffStage` as `let internal` inside `module internal RetainedRender` (`src/Controls/RetainedRender.fs`), taking `(prev, next)` and returning `(DiffResult, dirty, invalidated)`; preserve the `retained-step-diff` + `retained-step-layout-dirty-set` spans
- [X] T015 [US1] Extract `layoutStage` (`src/Controls/RetainedRender.fs`): seed `FrameState` from `ctx.Prev`, compute `themeChanged`, run `evaluateLayoutIncremental` over `dirty`, return `{Root;BoundsById;LayoutResult;Remeasured;ThemeChanged}`; preserve `retained-step-layout-incremental`. The text-measure-hook install/clear stays in the orchestrator (research R4), NOT in this stage
- [X] T016 [US1] Extract `paintStage` (`src/Controls/RetainedRender.fs`): the reuse-driven reconciliation walk with `build`/`carry`/`buildFresh` as local `let rec` and `mint`/`metadataFor`/`paintOwn`/`paintFresh` taking `(ctx, st, boundsById)` explicitly; returns `newRoot`; preserves `retained-build-paint-own` + `retained-step-build`; mutates only `st` (FR-002)
- [X] T017 [US1] Extract `assemblyStage` (`src/Controls/RetainedRender.fs`): the read-only post-build walks (`countVirtual`, damage-reduce, `walkPictures` + replay/avoided-work, `collectOffscreen`, `indexPriorOwn`+`collect` clocks, scene `assemble`, render result) and the `WorkReductionRecord` + `RetainedRenderStep` construction; preserves all nine `retained-step-*` post-build spans
- [X] T018 [US1] Rewrite `step` (`src/Controls/RetainedRender.fs`) as the composition `diffStage >> layoutStage >> paintStage >> assemblyStage`, with the orchestrator owning the text-measure-hook lifetime around layout+paint (install before `layoutStage`, clear after `paintStage`, always-clears on the total path — research R4) and constructing the `FrameContext`
- [X] T019 [US1] Verify SC-001 sizes: no stage body > ≈250 lines, no resulting file > ≈1,500 lines; record `wc -l` for `RetainedRender.fs`/`Internal/CompositorPolicy.fs` and each stage in `specs/190-retained-render-step-pipeline/readiness/line-counts.md`
- [X] T020 [US1] Run byte-identity + surface drift (quickstart Step 2 + Step 6): confirm golden-hash zero-delta (or record reviewed deltas in `readiness/golden-hash-review.md`) and that `scripts/refresh-surface-baselines.fsx` leaves `readiness/surface-baselines/FS.GG.UI.Controls.txt` unchanged (FR-004/SC-006)

**Checkpoint**: `step` is a 4-stage composition; stages unit-tested; byte-identical; surface unchanged — **MVP deliverable, independently shippable even if US2/US3 polish remains**.

---

## Phase 4: User Story 2 - `init` converges onto the shared stages (Priority: P2)

**Goal**: Eliminate `init`'s parallel build/paint/seed copy by re-expressing it on the shared
`paintStage`/`assemblyStage` bodies in their cold-start configuration — **conditional on a real
reduction** (FR-007/FR-016; carry-forward lesson 180 SC-005 / 189 US4).

**Independent Test**: `init`'s `RetainedInit` (scene/bounds/identities/seeded caches/metrics) is
byte-identical to baseline; the duplicated scaffolding is gone; net line count drops.

- [X] T021 [US2] Feasibility gate: assess whether `init` can reuse `paintStage` (seed config: full layout, no prior-fragment reuse) + `assemblyStage` (cold seed) WITHOUT distorting cold-start semantics or adding indirection, and whether it nets a real line/duplication reduction. Record the go/no-go decision (with the line-count delta) in `specs/190-retained-render-step-pipeline/readiness/us2-decision.md`. **If no net reduction → DROP US2 here** (FR-016) and skip T022–T023
- [~] T022 [US2] (DROPPED — US2 no-go per T021, readiness/us2-decision.md) (if go) Re-express `init` (`src/Controls/RetainedRender.fs`) onto the shared `paintStage`/`assemblyStage` in cold-start configuration, removing the parallel `build`/`paintOwn`/`seedPictures` copy; keep the cold-vs-steady distinction as a `FrameState` seed parameter, not a code fork
- [~] T023 [US2] (DROPPED — US2 no-go per T021, readiness/us2-decision.md) (if go) Cold-start byte-identity test in `tests/Controls.Tests/Feature190StagePipelineTests.fs` (or `Feature092` extension): `RetainedInit` scene/bounds-by-id/minted identities/seeded caches/metrics byte-identical to baseline; confirm the net line drop in `readiness/us2-decision.md` (SC-007)

**Checkpoint**: `init` shares one set of stage bodies (or US2 is dropped-and-recorded). US1 still green.

---

## Phase 5: User Story 3 - Hot-path regression gate in place and green (Priority: P3)

**Goal**: Formally demonstrate the §7 regression gate (stood up in T008) catches a regression and
passes on the real decomposition, and that per-frame perf is within budget.

**Independent Test**: The gate goes RED on an intentionally perturbed `step` and GREEN on the real
decomposition; perf lanes are within the agreed margin.

- [X] T024 [US3] Injected-regression demonstration (FR-015/SC-008) in `tests/Controls.Tests/Feature190GateTests.fs`: perturb `step` (reorder an accumulation OR drop a damage box), confirm the gate goes RED; revert and confirm GREEN. Record the demonstration (the perturbation + both results) in `readiness/golden-hash-review.md`
- [X] T025 [US3] Perf budget assertion (FR-006/SC-004): run lanes 160/161/167/173 under `DISPLAY=:1`, compare per-scenario alloc count + frame time against `/tmp/baseline-perf.log`, confirm within the **default margin of ±5% per-frame allocation count and ±10% frame time** (tighten or justify-and-loosen per scenario, recording any deviation from the default); record numbers + the applied margin in `specs/190-retained-render-step-pipeline/readiness/perf-budget.md`
- [X] T026 [US3] Golden-hash review sign-off (FR-005/SC-002): finalize `readiness/golden-hash-review.md` with either zero-delta confirmation or 100%-reviewed-and-approved deltas (zero silent changes)

**Checkpoint**: Gate demonstrably catches regressions and is green; perf within budget.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T027 Full-matrix re-run via `dotnet fsi scripts/baseline-tests.fsx --out specs/190-retained-render-step-pipeline/readiness/final-tests.md`; confirm the red set EQUALS the T002 baseline (no new failures — SC-005). **Also review `git diff` over `tests/**` to confirm no existing assertion was narrowed/weakened and no `skip`/`ptest`/`ftest` was added (FR-011 / Constitution V — red-set equality alone does not catch a silently-weakened-but-still-green assertion)
- [X] T028 [P] Surface baseline + bump decision: re-run `dotnet fsi scripts/refresh-surface-baselines.fsx`; confirm `git diff --exit-code readiness/surface-baselines/FS.GG.UI.Controls.txt` is empty → NO version bump; bump `FS.GG.UI.Controls` only if the reviewed diff is non-empty (FR-014/SC-006)
- [X] T029 [P] Capture per-phase fs-gg / Spec Kit feedback into `specs/190-retained-render-step-pipeline/feedback/phase-feedback.md` (the `fs-gg-feedback-capture` skill: process friction, generalizable-code candidates, severity)
- [X] T030 Run the full `quickstart.md` validation end-to-end (Steps 0–6) and confirm every "Done when" box; update the memory `current-project-god-module-decomposition.md` to mark Phase 6 complete

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately; T002/T003 capture the references all gates use.
- **Foundational (Phase 2)**: depends on Setup. T004 (compile probe) gates T006–T008. **BLOCKS all user stories.**
- **US1 (Phase 3)**: depends on Foundational (needs the seams from T004/T007 and the gate from T008).
- **US2 (Phase 4)**: depends on US1 (reuses the extracted `paintStage`/`assemblyStage`). Independently droppable (FR-016).
- **US3 (Phase 5)**: gate harness is foundational (T008); its formal demonstration depends on US1 being landed (something real to gate).
- **Polish (Phase 6)**: depends on all desired stories complete.

### Within Each User Story

- US1 tests (T009–T013) are written to FAIL before extraction (T014–T018), then pass after.
- Stages extract in producer→consumer order: diff → layout → paint → assembly (T014→T017), then the `step` composition (T018), then size/byte-identity verification (T019–T020).

### Parallel Opportunities

- T003 ∥ T002 setup references.
- US1 stage unit tests **T009, T010, T011, T012 run in parallel** (same new test file, independent test bodies — coordinate one author or split into per-stage files if conflicting).
- T028 ∥ T029 in Polish.
- T014–T017 are **sequential** (same file `RetainedRender.fs`, producer→consumer order) — NOT parallel.

---

## Parallel Example: User Story 1 tests

```bash
# Author the four stage isolation tests together (write-to-fail), then extract:
Task: "diffStage isolation test (C-DIFF) in tests/Controls.Tests/Feature190StagePipelineTests.fs"
Task: "layoutStage isolation test (C-LAYOUT) in tests/Controls.Tests/Feature190StagePipelineTests.fs"
Task: "paintStage isolation test (C-PAINT) in tests/Controls.Tests/Feature190StagePipelineTests.fs"
Task: "assemblyStage isolation test (C-ASM) in tests/Controls.Tests/Feature190StagePipelineTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → baseline + references captured.
2. Phase 2 Foundational → compile probe green, live reference captured, policy cluster relocated, seams + gate harness in place.
3. Phase 3 US1 → stage tests fail → extract four stages → `step` is the composition → byte-identical, surface unchanged.
4. **STOP and VALIDATE**: US1 is independently shippable. This is the campaign's final structural deliverable.

### Incremental Delivery

- US1 (MVP) → US2 convergence (only if it nets a reduction, else dropped-and-recorded) → US3 formal gate demonstration + perf sign-off → Polish.
- Each increment preserves byte-identity and the empty public-surface diff.

---

## Notes

- [P] = different files / independent bodies, no incomplete-task dependency.
- This is **Tier 2**: target byte-identical output; any `hashScene`/scene delta is reviewed + recorded (FR-005), never silent.
- The single highest-risk change in the repo — the regression gate (T008/T024/T025) is the safety net; do not extract a stage before T004–T008 are green.
- Mutation on the threaded `FrameState` hot path is sanctioned (Constitution III, already disclosed `// mutable: hot path`); do not "fix" it to immutable.
- Commit after each task or logical group; stop at any checkpoint to validate independently.
