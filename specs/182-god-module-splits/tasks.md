---
description: "Task list for God-Module Splits (Code-Health Refactoring Phase 5)"
---

# Tasks: God-Module Splits (Code-Health Refactoring Phase 5)

**Input**: Design documents from `/specs/182-god-module-splits/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: No NEW test tasks. This is a Tier 2 structural refactor whose acceptance gate is
*byte-stability against a captured baseline*. The existing `*.Tests.fsproj` suites + the three oracles
(surface diff, red/green parity, artifact/render byte-diff) ARE the test evidence (Constitution V).
Never weaken an assertion or edit a surface baseline to green a build.

**Organization**: Tasks grouped by user story (US1…US6, each an independently-shippable split).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1…US6)
- All paths are repo-root-relative; all commands run with `DISPLAY=:1` exported (GL needs a display)

## Standing invariants (apply to EVERY story task)

- **Surface frozen** (FR-002, SC-001): each touched package's `.fsi` and `readiness/surface-baselines/*.txt`
  stay byte-identical. `git diff --exit-code readiness/surface-baselines/` MUST be empty after every story.
- **No `private`/`internal`/`public` on `.fs` top-level bindings** (Constitution II): new files use
  `module internal` (FS0078) or a companion internal `.fsi`; the *union* of public surface across split
  files MUST equal the pre-split `.fsi` exactly.
- **Compile order** (FR-010): every new file is inserted into the `.fsproj` `<Compile Include>` order
  *before* the residual god-file, with no back-edge, no new cycle, no new project/dependency/inter-project
  reference. A seam needing a back-edge or a public-symbol relocation is OUT of scope → retain per FR-009.
- **Byte-stable output** (FR-003): when size/legibility and byte-stable output conflict, byte-stable
  output wins. A split that changes output, surface, or red/green state overshot → narrow or retain (FR-009).

---

## Phase 1: Setup (Shared Infrastructure & Baseline Capture)

**Purpose**: Create the readiness scaffold and capture the single pre-edit baseline shared by all six stories.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every**
> `*.Tests.fsproj` and record the full red/green set, so pre-existing failures are known up front and not
> mistaken for regressions at merge. Use the discovery-based runner (`scripts/baseline-tests.fsx`) — it
> globs `*.Tests.fsproj`, including the release-only `tests/Package.Tests` (owns the public-surface gate)
> and the `samples/**/*.Tests` package-feed consumers that the solution deliberately omits.

- [X] T001 Create `specs/182-god-module-splits/readiness/{baseline,post-change}/` directories per plan.md Project Structure
- [X] T002 Capture the surface baseline: run `dotnet fsi scripts/refresh-surface-baselines.fsx`, confirm `git diff --exit-code readiness/surface-baselines/` is empty at HEAD, then copy all 12 `readiness/surface-baselines/*.txt` into `specs/182-god-module-splits/readiness/baseline/`
- [X] T003 Establish the no-regression test baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/182-god-module-splits/readiness/baseline/` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set)
- [X] T004 Capture the artifact/render baseline for the six touched subsystems into `specs/182-god-module-splits/readiness/baseline/` (run AFTER T003 so binaries are built): regenerate readiness/evidence (MD+JSON), viewer observations/screenshots, scene hashes/fingerprints, and damage regions via the deterministic Rendering.Harness entry point `dotnet fsi scripts/run-validation-lanes.fsx` (= `tools/Rendering.Harness` `validation-lanes`), plus the per-subsystem preludes `scripts/controls-prelude.fsx`, `scripts/controls-elmish-prelude.fsx`, `scripts/diagnostics-prelude.fsx` for scene hashes/fingerprints/traces; tee each command's stdout/stderr/`echo $?`. **Write the exact command set used into `specs/182-god-module-splits/readiness/baseline/regen-commands.md` — every Oracle-3 MUST re-run THIS set byte-for-byte identically into `post-change/`** (pattern: feature 181 quickstart Step 0b)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Lock the evidence contract every story is gated on. No code edits in this phase.

**⚠️ CRITICAL**: No user-story split may begin until this phase is complete.

> **Early live smoke run — resolved N/A (per plan.md).** This feature carries no defect/root-cause
> hypothesis: it is a pure structural refactor that must not change any observed output. The template's
> standing early-live-smoke clause is therefore resolved as **N/A**; the comprehensive baseline capture
> (T002–T004) replaces it as the binding pre-edit gate.

- [X] T005 Record the allowed pre-existing non-green set from T003 (known `Package.Tests` / `ControlsGallery` stale-feed reds, per features 180/181) as baseline-not-regression in `specs/182-god-module-splits/readiness/baseline/known-reds.md` — every later red/green parity check compares against THIS set
- [X] T006 Confirm the three-oracle validation loop runs end-to-end against the captured baseline (surface diff empty; `baseline-tests.fsx` red/green parity reproducible; `diff -r baseline/ post-change/` byte-clean on an unchanged tree), per `contracts/surface-invariance.md` and quickstart.md Step 1 — this is the per-story gate, dry-run it once now

**Checkpoint**: Baseline captured, known-reds recorded, oracle loop confirmed — story splits can now begin (in priority order, or in parallel by different contributors).

---

## Phase 3: User Story 1 - Split the SkiaViewer god-module (Priority: P1) 🎯 MVP

**Goal**: Break `module Viewer` in `src/SkiaViewer/SkiaViewer.fs` (4,063 lines) into concern-scoped files
and unify `runPresentedPersistentWindow`/`runPersistentWindow` behind one lifecycle scaffold (FR-004),
with byte-identical viewer evidence and a frozen `FS.GG.UI.SkiaViewer.txt` surface.

**Independent Test**: Build `FS.GG.UI.SkiaViewer`, run its test project + viewer-driven smoke/evidence
lanes; `FS.GG.UI.SkiaViewer.txt` surface baseline unchanged and all viewer evidence/screenshots/observations
byte-identical to baseline.

### Implementation for User Story 1

- [X] T007 [US1] Carve the type header / `RenderLagTrace` / `RequireQualifiedAccess` types preceding `module Viewer` into `src/SkiaViewer/Viewer.Types.fs` (`module internal`, + internal `.fsi` only if needed); add it to `src/SkiaViewer/SkiaViewer.fsproj` `<Compile Include>` order BEFORE `SkiaViewer.fs`
- [X] T008 [US1] Extract responsiveness summarization into `src/SkiaViewer/ViewerResponsiveness.fs`; insert before `SkiaViewer.fs` in the `.fsproj` compile order
- [X] T009 [US1] Extract window-behavior / validation into `src/SkiaViewer/ViewerWindowBehavior.fs`; insert before `SkiaViewer.fs` in the `.fsproj` compile order
- [X] T010 [US1] Extract native run-loops into `src/SkiaViewer/ViewerRunLoops.fs` AND unify `runPresentedPersistentWindow` (`SkiaViewer.fs:2114`) ≈ `runPersistentWindow` (`:2437`) behind one persistent-window scaffold (FR-004) — OR, if unification changes behavior/surface, leave both explicit and record the reason per FR-009 in `contracts/split-viewer.md`; insert before `SkiaViewer.fs`
- [X] T011 [US1] Extract evidence / screenshot emission into `src/SkiaViewer/ViewerEvidence.fs` (preserve emission ordering & constants verbatim, FR-003); insert before `SkiaViewer.fs`
- [X] T012 [US1] Shrink `src/SkiaViewer/SkiaViewer.fs` to app/interactive runners + the public `module Viewer` union, referencing the extracted modules; keep `SkiaViewer.fsi` UNTOUCHED and the public-surface union identical
- [X] T013 [US1] Run the three oracles for US1 (quickstart.md Step 1): (1) `refresh-surface-baselines.fsx` → `git diff --exit-code readiness/surface-baselines/` empty (`FS.GG.UI.SkiaViewer.txt` unchanged); (2) `dotnet build FS.GG.Rendering.slnx -c Release` + `baseline-tests.fsx` red/green parity vs T005 known-reds; (3) re-run the identical T004 command set (`regen-commands.md`) to regenerate viewer evidence/screenshots/observations/diagnostics into `post-change/`, then `diff -r` byte-clean vs baseline

**Checkpoint**: US1 independently shippable — viewer split builds, full suite at baseline red/green, surface + viewer evidence byte-stable (SC-001/002/003/004).

---

## Phase 4: User Story 2 - Split the Control god-module (Priority: P2)

**Goal**: Divide `ControlInternals` (~2,990 lines inside the 3,570-line `src/Controls/Control.fs`) into
geometry/assembly modules and hoist the ×17 `match pts with | [] -> emptyState` chart preamble into a
`withPoints` combinator + shared bar-layout helper (FR-005), with byte-identical scene/scene-hash/fingerprint.

**Independent Test**: Build `FS.GG.UI.Controls`, run `Controls.Tests`; `FS.GG.UI.Controls.txt` surface
baseline unchanged and all control scene-hash / fingerprint / inspection outputs byte-identical.

> Serialize US2 before US5 (both touch `src/Controls/` but different files) to keep one clean per-story
> `FS.GG.UI.Controls.txt` surface diff (plan.md Sequencing).

### Implementation for User Story 2

- [X] T014 [US2] Extract the `*Geom` chart-geometry family into `src/Controls/ChartGeometry.fs` AND hoist the ×17 chart preamble into a `withPoints` combinator + shared bar-layout helper (FR-005) — OR leave a genuinely-divergent call site explicit per FR-009 (record in `contracts/split-control.md`); add to `src/Controls/Controls.fsproj` compile order BEFORE `Control.fs`
- [X] T015 [US2] Extract widget geometry into `src/Controls/WidgetGeometry.fs`; insert before `Control.fs` in the `.fsproj` compile order
- [X] T016 [US2] Extract `SceneHash`/`Fingerprint` into `src/Controls/SceneFingerprint.fs`; insert before `Control.fs` in the `.fsproj` compile order
- [X] T017 [US2] Extract `LayoutEval` into `src/Controls/LayoutEval.fs`; insert before `Control.fs` in the `.fsproj` compile order
- [X] T018 [US2] Extract node assembly into `src/Controls/NodeAssembly.fs`; insert before `Control.fs` in the `.fsproj` compile order
- [X] T019 [US2] Shrink `src/Controls/Control.fs` to assembly glue + the public `module Control`, referencing the extracted modules; keep `Control.fsi` UNTOUCHED and the public-surface union identical
- [X] T020 [US2] Run the three oracles for US2: (1) surface diff empty (`FS.GG.UI.Controls.txt` unchanged); (2) build + `baseline-tests.fsx` red/green parity vs T005; (3) re-run the identical T004 command set (`regen-commands.md`) to regenerate chart scene + scene-hash + fingerprint for every chart control into `post-change/`, then `diff -r` byte-clean vs baseline

**Checkpoint**: US2 independently shippable — Control split builds, suite at baseline red/green, surface + scene-hash/fingerprint byte-stable.

---

## Phase 5: User Story 3 - Split the Scene god-module (Priority: P3)

**Goal**: Move `VisualInspection`/`RetainedInspection`/`LayoutEvidence`/`SceneEvidence` into their own files,
separate the ~767-line type block, finish the started `cleanToken`/`duplicateIds`/`finding` dedup (FR-006),
and isolate the `realTextMeasurer` module-level mutable — in the dependency-free `src/Scene/Scene.fs` (2,077 lines).

**Independent Test**: Build `FS.GG.UI.Scene`, run Scene tests + the codec round-trip suite;
`FS.GG.UI.Scene.txt` surface baseline unchanged and all visual/retained inspection records (tokens,
findings, serialized form) byte-identical.

### Implementation for User Story 3

- [X] T021 [US3] Carve the ~767-line type block (from ~`Scene.fs:432`) into `src/Scene/SceneTypes.fs` (`module internal` / internal `.fsi` as needed); add to `src/Scene/Scene.fsproj` compile order BEFORE `Scene.fs`
- [X] T022 [US3] Extract `VisualInspection` into `src/Scene/VisualInspection.fs` and `RetainedInspection` into `src/Scene/RetainedInspection.fs`, completing the shared `cleanToken`/`duplicateIds`/`finding` dedup between them (FR-006) with inspection records byte-identical to baseline; insert both before `Scene.fs` in compile order
- [X] T023 [US3] Extract `LayoutEvidence` into `src/Scene/LayoutEvidence.fs` and `SceneEvidence` into `src/Scene/SceneEvidence.fs`; insert before `Scene.fs` in compile order
- [X] T024 [US3] Shrink `src/Scene/Scene.fs` to root primitives with the `realTextMeasurer`/`measurementVersionBucket` mutable side-channel isolated — keep identical observable behavior (no change to initialization timing or first-use semantics); keep `Scene.fsi` UNTOUCHED and the public-surface union identical
- [X] T025 [US3] Run the three oracles for US3: (1) surface diff empty (`FS.GG.UI.Scene.txt` unchanged); (2) build + `baseline-tests.fsx` red/green parity vs T005; (3) re-run the identical T004 command set (`regen-commands.md`) to regenerate visual + retained inspection records into `post-change/`, then `diff -r` byte-clean vs baseline (tokens, findings, serialized form)

**Checkpoint**: US3 independently shippable — Scene split builds, suite at baseline red/green, surface + inspection records byte-stable; FR-006 dedup completed or retained with rationale.

---

## Phase 6: User Story 4 - Split the Testing god-module (Priority: P4)

**Goal**: Divide `src/Testing/Testing.fs` (4,629 lines, the largest `src` file; ~30 top-level modules
already grouped by domain) into per-domain files, with byte-identical emitted readiness/evidence MD+JSON.

**Independent Test**: Build `FS.GG.UI.Testing`, run the suites that consume it; `FS.GG.UI.Testing.txt`
surface baseline unchanged and all emitted readiness/evidence markdown + JSON byte-identical.

### Implementation for User Story 4

- [X] T026 [P] [US4] Extract the Visual domain modules into `src/Testing/TestingVisual.fs`; add to `src/Testing/Testing.fsproj` compile order BEFORE `Testing.fs`
- [X] T027 [P] [US4] Extract the RetainedInspection domain modules into `src/Testing/TestingRetainedInspection.fs`; insert before `Testing.fs` in compile order
- [X] T028 [P] [US4] Extract the Evidence domain modules into `src/Testing/TestingEvidence.fs`; insert before `Testing.fs` in compile order
- [X] T029 [P] [US4] Extract the Compositor domain modules into `src/Testing/TestingCompositor.fs`; insert before `Testing.fs` in compile order
- [X] T030 [P] [US4] Extract the Feature-readiness domain modules into `src/Testing/TestingFeatureReadiness.fs`; insert before `Testing.fs` in compile order
- [X] T031 [US4] Shrink `src/Testing/Testing.fs` to residual glue / re-exports preserving the public union; reconcile final `.fsproj` compile order across T026–T030 (no back-edge, no cycle); keep `Testing.fsi` UNTOUCHED
- [X] T032 [US4] Run the three oracles for US4: (1) surface diff empty (`FS.GG.UI.Testing.txt` unchanged); (2) build + `baseline-tests.fsx` red/green parity vs T005; (3) re-run the identical T004 command set (`regen-commands.md`) to regenerate every readiness/evidence MD+JSON into `post-change/`, then `diff -r` byte-clean vs baseline

**Checkpoint**: US4 independently shippable — Testing split builds, suite at baseline red/green, surface + readiness/evidence artifacts byte-stable.

---

## Phase 7: User Story 5 - Tame the `RetainedRender.step` god-function (Priority: P5)

**Goal**: Restructure `step` (~600 lines, ~30 `let mutable` accumulators, in `src/Controls/RetainedRender.fs`
@1424) around a `StepMetrics` record + named passes, and unify the build/paint scaffolding it duplicates
with `init` (@1254) (FR-007), with byte-identical rendered scene / damage regions / metrics / promotion decisions.

**Independent Test**: Build `FS.GG.UI.Controls`, run the retained-render + damage-locality suites; step
output (rendered scene, damage regions, metrics, promotion decisions) byte-identical to baseline.

> Touches `src/Controls/` like US2 but a different file (`RetainedRender.fs`); serialize after US2 for a
> clean per-story `FS.GG.UI.Controls.txt` diff.

### Implementation for User Story 5

- [X] T033 [US5] Define the internal `StepMetrics` record (Layout/id, Memo, Text cache, Metadata, Virtualization, Picture cache, Replay field groups per data-model.md) replacing `step`'s ~30 `let mutable` accumulators — in `src/Controls/StepMetrics.fs` (or an internal block in `RetainedRender.fs`); if a new file, insert before `RetainedRender.fs` in `Controls.fsproj` compile order; record `StepMetrics` does NOT appear in any `.fsi`
- [X] T034 [US5] Pull each pass of `step` into a named function threading `StepMetrics`; retain in-place mutation on the hot path with a one-line `// mutable: hot path` disclosure comment (Constitution III, FR-007) — no added per-frame allocation
- [X] T035 [US5] Unify the build/paint scaffolding duplicated between `init` (`RetainedRender.fs:1254`) and `step` (`:1424`) so neither duplicates the other (FR-007, US5 acceptance #2) — OR retain explicit per FR-009 in `contracts/refactor-retainedrender.md` if unification changes output
- [X] T036 [US5] Run the three oracles for US5: (1) surface diff empty (`FS.GG.UI.Controls.txt` unchanged — `StepMetrics` stays internal); (2) build + `baseline-tests.fsx` red/green parity vs T005; (3) re-run the identical T004 command set (`regen-commands.md`) to regenerate rendered scene + damage regions + step metrics + promotion decisions into `post-change/`, then `diff -r` byte-clean vs baseline; confirm no render-lag/timing regression

**Checkpoint**: US5 independently shippable — `step` restructured over `StepMetrics`, suite at baseline red/green, rendered/damage/metrics byte-stable, hot-path timing not regressed.

---

## Phase 8: User Story 6 - Tame the `runInteractiveAppWithLauncher` god-function (Priority: P6)

**Goal**: Promote the ~20 `ref` cells of ad-hoc frame state in `runInteractiveAppWithLauncher` (~500 lines,
in `src/Controls.Elmish/ControlsElmish.fs` @1186) to a `FrameLoopState` record + module-level functions
(FR-007), with byte-identical frame-loop transitions / emitted commands / render-lag traces.

**Independent Test**: Build `FS.GG.UI.Controls.Elmish`, run its tests + any frame-loop/render-lag trace
suite; `FS.GG.UI.Controls.Elmish.txt` surface baseline unchanged and frame-loop behavior/traces byte-identical.

### Implementation for User Story 6

- [X] T037 [US6] Define the internal `FrameLoopState` record (the 12 fields from data-model.md: `pointerState`, `focused`, `retained`, `lastRender`, `lastView`, `lastRuntimeModel`, `scrollOffsets`, `surfacedDiagnostics`, `pendingMove`, `pointerSampleCount`, `lastWorkReduction`, `lastPresentTiming`) replacing the ~20 `ref` cells — in `src/Controls.Elmish/FrameLoopState.fs` (or internal block); if a new file, insert before `ControlsElmish.fs` in `Controls.Elmish.fsproj` compile order; record it does NOT appear in `ControlsElmish.fsi` and stays interpreter-edge state, not the Elmish `Model` (Constitution IV)
- [X] T038 [US6] Rewrite `runInteractiveAppWithLauncher` over `FrameLoopState` + module-level functions replacing the ad-hoc closures; retain per-frame mutation where simpler/faster with a one-line disclosure comment (Constitution III, FR-007); keep `update` pure and I/O at the edge
- [X] T039 [US6] Run the three oracles for US6: (1) surface diff empty (`FS.GG.UI.Controls.Elmish.txt` unchanged — `FrameLoopState` stays internal); (2) build + `baseline-tests.fsx` red/green parity vs T005; (3) re-run the identical T004 command set (`regen-commands.md`) to regenerate frame-loop transitions + emitted commands + render-lag traces into `post-change/`, then `diff -r` byte-clean vs baseline

**Checkpoint**: US6 independently shippable — frame loop over `FrameLoopState`, suite at baseline red/green, traces byte-stable.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Phase-end verification across all six splits; record FR-009 retentions; confirm success criteria.

- [X] T040 Phase-end full sweep (quickstart.md Step 2): `dotnet build FS.GG.Rendering.slnx -c Release` + `dotnet fsi scripts/baseline-tests.fsx --out specs/182-god-module-splits/readiness/post-change/`; confirm red/green parity vs T005 known-reds (SC-003)
- [X] T041 Phase-end surface invariance: `dotnet fsi scripts/refresh-surface-baselines.fsx && git diff --exit-code readiness/surface-baselines/` empty across all 12 baselines — zero baseline edits (SC-001)
- [X] T042 [P] Verify size targets (SC-005), BOTH thresholds: (a) **module size** — `wc -l src/SkiaViewer/*.fs src/Controls/Control*.fs src/Controls/RetainedRender.fs src/Scene/*.fs src/Testing/*.fs src/Controls.Elmish/ControlsElmish*.fs`, confirm no touched module > ~1,500 lines; (b) **function size** — measure the line span of each refactored function and each new named pass (esp. `RetainedRender.step` @`src/Controls/RetainedRender.fs` and `runInteractiveAppWithLauncher` @`src/Controls.Elmish/ControlsElmish.fs`), confirm none exceeds ~150 lines. Record any unit retained over target per FR-009 with its rationale
- [X] T043 [P] Confirm SC-007: no new project, package dependency, or inter-project reference introduced; dependency graph acyclic and unchanged (FR-010)
- [X] T044 Record every FR-009 retention (un-split seam / un-unified dedup: FR-004 viewer scaffold, FR-005 ×17 preamble, FR-006 Scene dedup, FR-007 init/step scaffold) with its rationale in the relevant `contracts/*.md`, and confirm SC-006 (each dedup unified OR explicitly retained with reason)
- [X] T045 Final success-criteria sign-off against quickstart.md "Done when": SC-001…SC-007 all satisfied; archive `post-change/` evidence under `specs/182-god-module-splits/readiness/post-change/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T002 → T003 → T004 are sequential (surface snapshot; then the build+test sweep; then T004's artifact/render regen, which needs T003's built binaries).
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories. No code edits.
- **User Stories (Phases 3–8)**: All depend on Foundational. Each is independently shippable; none depends on another.
  - Sequenced P1→P6 for payoff. **US2 (Control) and US5 (RetainedRender)** both touch `src/Controls/` (different files) — serialize US5 after US2 for a clean per-story `Controls.txt` diff.
- **Polish (Phase 9)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)** — MVP. SkiaViewer. No dependency on other stories.
- **US2 (P2)** — Control. Independent. Serialize before US5 (shared project, distinct files).
- **US3 (P3)** — Scene. Fully independent (dependency-free root project).
- **US4 (P4)** — Testing. Independent; the per-domain carve-outs T026–T030 are mutually [P].
- **US5 (P5)** — RetainedRender. Independent; serialize after US2.
- **US6 (P6)** — FrameLoopState. Independent; the most contained, sequenced last.

### Within Each User Story

- Extract concern modules (insert into `.fsproj` BEFORE residual) → shrink residual file → run the three oracles.
- The story is "green" only when all three oracles pass (surface diff empty, red/green parity, artifact/render byte-diff clean).

### Parallel Opportunities

- Setup: T002→T003→T004 are sequential (T004 needs T003's built binaries) — no intra-Setup parallelism.
- US4: T026–T030 [P] (five distinct new files, no cross-dependency) — the residual-shrink T031 reconciles compile order afterward.
- Polish: T042 [P] and T043 [P] run together.
- Across stories: different contributors can take US1/US3/US4/US6 in parallel after Foundational (US2 before US5 is the only intra-project serialization).

---

## Parallel Example: User Story 4 (Testing per-domain carve-outs)

```bash
# Launch all five per-domain extractions together (distinct new files, no cross-dependency):
Task: "Extract Visual domain into src/Testing/TestingVisual.fs"
Task: "Extract RetainedInspection domain into src/Testing/TestingRetainedInspection.fs"
Task: "Extract Evidence domain into src/Testing/TestingEvidence.fs"
Task: "Extract Compositor domain into src/Testing/TestingCompositor.fs"
Task: "Extract Feature-readiness domain into src/Testing/TestingFeatureReadiness.fs"
# Then reconcile Testing.fsproj compile order + shrink residual Testing.fs (T031), run oracles (T032).
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1: Setup — capture the single shared baseline (surface + tests + artifacts).
2. Complete Phase 2: Foundational — record known-reds, confirm the oracle loop (no code edits).
3. Complete Phase 3: US1 — split SkiaViewer, unify the window-lifecycle scaffold.
4. **STOP and VALIDATE**: run the three oracles; US1 holds surface + viewer-evidence byte-stability on its own.
5. Ship US1 independently (SC-004).

### Incremental Delivery

1. Setup + Foundational → baseline locked.
2. US1 (SkiaViewer) → three oracles green → ship (MVP).
3. US2 (Control) → ship. Then US5 (RetainedRender) → ship (serialized after US2).
4. US3 (Scene) → ship. US4 (Testing) → ship. US6 (FrameLoopState) → ship.
5. Each split adds legibility without changing surface or output; each is reverted/re-scoped (FR-009) rather than forced if any oracle fails.
6. Polish → phase-end full sweep + size-target + dependency-graph confirmation + FR-009 retention log.

### Notes

- [P] = different files, no dependencies. [Story] label maps each task to its split for traceability.
- This is a refactor: the existing suites + the three oracles ARE the test evidence. Never weaken an
  assertion or edit a surface baseline to green a build (Constitution V; quickstart.md Step 1 warning).
- Commit after each story (each is an independently-shippable, byte-stable slice).
- If a seam needs a back-edge, relocates a public symbol, or changes output → retain per FR-009 and record why.
