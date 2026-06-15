---
description: "Conformance-pass task list for Feature 092 (Wire Retained Identity State onto the Live Path)"
---

# Tasks: Wire Retained Identity State onto the Live Path (Feature 092)

**Input**: Design documents from `/specs/092-wire-retained-identity-state/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/live-identity-state.md, quickstart.md

**Nature of this task list**: 092 is a **conformance backfill**. The implementation
(`src/Controls/RetainedRender.fs` + `.fsi`, the `Controls.Elmish` adapter seam), the accreted `.fsi`
surface, and both authoritative suites (`Feature092RetainedRenderTests`, `Feature092LiveSurvivalTests`)
**already exist** in the imported, rebranded source. These tasks therefore **confirm** the existing
artifacts satisfy the contract — they do not build new product behavior. The guiding rule (quickstart.md
§2, Principle V): **a red test or weakened assertion is a finding to report, not to patch** — never seed
focus/text state to green the live-survival proof.

**Tests**: Test tasks here are **verification** tasks (run the pre-existing suites), not authoring tasks.
The suites already exist; no new tests are written except where a coverage gap is found and reported.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/commands, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5)
- Exact file paths / commands included in each task

## Path Conventions

- Implementation: `src/Controls/`, `src/Controls.Elmish/`
- Tests: `tests/Controls.Tests/`, `tests/Elmish.Tests/`
- Surface baselines: `tests/surface-baselines/`
- Evidence: `specs/092-wire-retained-identity-state/readiness/` (gitignored)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the toolchain and that the affected assemblies + test assemblies build.

- [X] T001 Confirm prerequisites: .NET SDK with `net10.0` support and solution `FS.GG.Rendering.slnx` at repo root resolve (per quickstart.md §Prerequisites); no GL context required.
- [X] T002 [P] Build the implementation assembly `src/Controls/Controls.fsproj` (`dotnet build src/Controls/Controls.fsproj`).
- [X] T003 [P] Build the adapter assembly `src/Controls.Elmish/Controls.Elmish.fsproj` (`dotnet build src/Controls.Elmish/Controls.Elmish.fsproj`).
- [X] T004 [P] Build the test assembly `tests/Controls.Tests/Controls.Tests.fsproj` (`dotnet build tests/Controls.Tests/Controls.Tests.fsproj`).
- [X] T005 [P] Build the test assembly `tests/Elmish.Tests/Elmish.Tests.fsproj` (`dotnet build tests/Elmish.Tests/Elmish.Tests.fsproj`).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the artifacts the per-story conformance checks depend on actually exist and declare the 092-in-scope seam. **No per-story verification can be trusted until this phase passes.**

**⚠️ CRITICAL**: Complete before any user-story phase.

- [X] T006 Confirm `src/Controls/RetainedRender.fsi` declares the 092-in-scope surface as `internal`: `init`, `step`, `retainedHitTest`, and the `RetainedRender<'msg>` (incl. `StateByIdentity` + `Theme`) / `RetainedInit<'msg>` records (per contracts/live-identity-state.md §Operations).
- [X] T007 [P] Confirm `src/Controls.Elmish/ControlsElmish.fsi` declares the live adapter seam `resolveFocus` and `routeFocusedText` as `internal` (per contracts/live-identity-state.md §Operations).
- [X] T008 [P] Confirm the supporting types exist in `src/Controls/Types.fsi` / `Types.fs`: `ControlDiagnostic`, `KeyCollision`, `Severity`, `Theme` (per data-model.md §ControlDiagnostic).
- [X] T009 [P] Confirm `WorkReductionRecord` (`BaselineNodeCount`/`RecomputedNodeCount`/`ChangedSubtreeBound`/`ShiftedNodeCount`) is declared in `src/Controls/RetainedRender.fsi` (per data-model.md §WorkReductionRecord).
- [X] T010 Confirm both suite files exist and are wired into their projects: `tests/Controls.Tests/Feature092RetainedRenderTests.fs` (US2–US5) and `tests/Elmish.Tests/Feature092LiveSurvivalTests.fs` (US1); confirm `InternalsVisibleTo` reaches the internal seam from each test assembly.
- [X] T011 Capture the pre-change public-surface baselines as the zero-delta reference: `tests/surface-baselines/FS.GG.UI.Controls.txt` and `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt` (the FR-012 reference for Phase 8).

**Checkpoint**: Artifacts confirmed present and internal — per-story conformance can begin.

---

## Phase 3: User Story 1 - Focus + in-progress text survive a re-render through the real seam (Priority: P1) 🎯 MVP

**Goal**: Confirm the live Elmish host **reads and writes** `StateByIdentity` so focus and an in-progress draft survive a position-shifting re-render under the same `RetainedId`, proven through the real `resolveFocus` + `routeFocusedText` + `RetainedRender.step` seam with no hand-seeded state; and that the rebuild-every-frame baseline loses it.

**Independent Test**: Through the real adapter seam, focus an editor, type `x` (draft `hix`), insert a banner above it (shift), type `y`; confirm draft is `hixy` and focus survived — no hand-seeded state. Run the same sequence against a rebuild-every-frame baseline and confirm it loses the identity/draft.

- [X] T012 [US1] Run the headline live-survival suite: `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature092"`; confirm green (per quickstart.md §2).
- [X] T013 [US1] Confirm the **survival** case in `tests/Elmish.Tests/Feature092LiveSurvivalTests.fs` drives focus+typing through the real `resolveFocus`/`routeFocusedText`/`RetainedRender.step` calls with **no** hand-seeded focus/text state and asserts draft continuity `hix` → `hixy` after the shift (FR-001, FR-002, SC-001; research D1/D2).
- [X] T014 [US1] Confirm the **baseline-fails** case in `tests/Elmish.Tests/Feature092LiveSurvivalTests.fs` re-runs `init` every frame on the identical sequence and asserts the identity differs / draft is lost (fail-first evidence; FR-003, SC-001).
- [X] T015 [US1] Confirm the `Replace`-drops and removal-filters scenarios (US1 acceptance 3–4) are pinned in `tests/Elmish.Tests/Feature092LiveSurvivalTests.fs`: a `Replace` drops the prior identity's `StateByIdentity` entry, and a removed control's entry is filtered out (FR-007; research D6).
- [X] T016 [US1] Cross-check the survival/baseline readiness evidence regenerates and matches: `specs/092-wire-retained-identity-state/readiness/live-survival/survival.txt` and `.../live-survival/baseline-fails.txt` (SC-001).

**Checkpoint**: US1 (the MVP headline) confirmed green through the real seam, with fail-first baseline evidence.

---

## Phase 4: User Story 2 - Every field resolves to its own identity; a pre-filled field is never wiped (Priority: P1)

**Goal**: Confirm keyed, unkeyed, and keyed-container-wrapped fields each resolve to a **distinct** `RetainedId`; a pre-filled field's first keystroke **appends** (MultiLine honored, zero loss); and a control with more than one change handler dispatches **all** matched handlers.

**Independent Test**: Build three fields (keyed, unkeyed, keyed-container-wrapped), hit-test each, confirm three distinct `RetainedId`s; pre-fill a multi-line area with `line1`, focus, send `X`, confirm `line1X`; wire two change handlers, confirm both fire.

- [X] T017 [US2] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature092"`; confirm green (covers US2–US5; per quickstart.md §2).
- [X] T018 [P] [US2] Confirm the **focus-resolution** case in `tests/Controls.Tests/Feature092RetainedRenderTests.fs` asserts keyed/unkeyed/keyed-container-wrapped fields resolve to **distinct** `RetainedId`s via `retainedHitTest`, and a point outside the root resolves to `None` (FR-004, SC-002; research D3).
- [X] T019 [P] [US2] Confirm the **prefilled-append** case in `tests/Controls.Tests/Feature092RetainedRenderTests.fs` seeds the draft from the control's current value and asserts a pre-filled MultiLine area becomes `line1X` on first keystroke (zero loss) (FR-005, SC-002; research D4).
- [X] T020 [P] [US2] Confirm the multi-handler case in `tests/Controls.Tests/Feature092RetainedRenderTests.fs` asserts every matched `onChanged` product message is dispatched on a single change (FR-006; research D5).
- [X] T021 [US2] Cross-check the readiness evidence regenerates and matches: `specs/092-wire-retained-identity-state/readiness/focus-resolution/focus-resolution.txt` and `.../focus-resolution/prefilled-append.txt` (SC-002).

**Checkpoint**: US2 correctness defects (sibling collapse, wiped pre-fill, single-line force, dropped handler) confirmed closed.

---

## Phase 5: User Story 3 - Theme change repaints faithfully; unchanged tree reuses everything (Priority: P2)

**Goal**: Confirm the active `Theme` is part of the fragment reuse key: a theme change between otherwise-identical frames invalidates all cached fragments and repaints byte-identically to a full rebuild under the new theme (and differs from the old); an unchanged tree under an unchanged theme reuses everything (zero recompute).

**Independent Test**: Render a fixed tree under light, switch to dark with the tree unchanged, assert the frame is byte-identical to a rebuild under dark and differs from a rebuild under light. Separately, step an identical tree with no theme change and assert nothing is recomputed.

- [X] T022 [US3] Confirm the **theme-reuse** case in `tests/Controls.Tests/Feature092RetainedRenderTests.fs` asserts a theme change repaints byte-identically to a full rebuild under the new theme and differs from the old (FR-008, SC-006; research D7).
- [X] T023 [US3] Confirm the same case asserts an unchanged tree under an unchanged theme reuses everything — zero nodes recomputed, no spurious repaint (FR-008, SC-006).
- [X] T024 [US3] Cross-check the readiness evidence regenerates and matches: `specs/092-wire-retained-identity-state/readiness/theme-reuse/theme-reuse.txt` (SC-006).

**Checkpoint**: US3 theme-in-reuse-key confirmed both directions (repaint on change, reuse on no-change).

---

## Phase 6: User Story 4 - Work reduction under a layout shift is accounted honestly (Priority: P2)

**Goal**: Confirm that under a sibling-inserted-above shift, `WorkReductionRecord` reports `RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount` and that total is strictly less than `BaselineNodeCount` — the relaid-out leaf counted as *shifted*, not free.

**Independent Test**: Insert a sibling above a fixed-size leaf and assert `RecomputedNodeCount` (2) `= ChangedSubtreeBound` (1) `+ ShiftedNodeCount` (1) `< BaselineNodeCount` (3).

- [X] T025 [US4] Confirm the **work-reduction** case in `tests/Controls.Tests/Feature092RetainedRenderTests.fs` asserts `RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount` and `< BaselineNodeCount` for a sibling-inserted-above-fixed-leaf shift (FR-009, SC-003; research D8).
- [X] T026 [US4] Cross-check the readiness evidence regenerates and matches: `specs/092-wire-retained-identity-state/readiness/work-reduction/work-reduction.txt` (SC-003).

**Checkpoint**: US4 honest changed-vs-shifted accounting confirmed.

---

## Phase 7: User Story 5 - First frame paints exactly once and surfaces its diagnostics immediately (Priority: P3)

**Goal**: Confirm `init` paints the first frame **exactly once** (returns the painted `Render` the adapter reuses — no second `Control.renderTree`), surfaces a first-frame duplicate-key `KeyCollision` on **frame 0** while staying total; and confirm multi-frame parity + identity continuity across a chained sequence.

**Independent Test**: Run `init` on a first frame; assert its `Render` is byte-identical to a full rebuild and paint count is exactly 1. Run `init` on a first frame with a duplicate-keyed sibling list; assert a `KeyCollision` is surfaced on frame 0 and `init` does not throw. Run init + ≥3 steps; assert each frame byte-identical to a rebuild and the same node carries its identity.

- [X] T027 [P] [US5] Confirm the **first-frame** case in `tests/Controls.Tests/Feature092RetainedRenderTests.fs` asserts `init`'s `Render` is byte-identical to a full rebuild and first-frame paint count is exactly 1 (single paint) (FR-010, SC-005; research D9).
- [X] T028 [P] [US5] Confirm the frame-0 diagnostics case asserts a duplicate-key first frame surfaces a `KeyCollision` in `RetainedInit.Diagnostics` on frame 0 while `init` stays total (no throw) (FR-010, SC-005; research D9).
- [X] T029 [P] [US5] Confirm the **multi-frame** case asserts each frame of a chained init + ≥3 steps is byte-identical to a full rebuild, the same node carries its identity across the chain (continuity), and the step is total/deterministic on the live path (FR-011, SC-004, SC-007; research D10).
- [X] T030 [US5] Cross-check the readiness evidence regenerates and matches: `specs/092-wire-retained-identity-state/readiness/multi-frame/first-frame.txt` and `.../multi-frame/round-trip.txt` (SC-004/SC-005/SC-007).

**Checkpoint**: All five user stories independently confirmed green with regenerated evidence.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Confirm the zero-delta surface invariant, totality/determinism on the wired path, the full-suite parity, and record the disclosed deviations from plan.md.

- [X] T031 Confirm **zero public-surface-baseline delta** (FR-012): the surface-drift check reports NO changes for `FS.GG.UI.Controls` and `FS.GG.UI.Controls.Elmish` against `tests/surface-baselines/FS.GG.UI.Controls.txt` and `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt` (quickstart.md §3).
- [X] T032 [P] Confirm the wired `step`/`routeFocusedText` path is **total** (never throws) and **deterministic** (no wall-clock, no randomness) on the live path by inspecting `src/Controls/RetainedRender.fs` and `src/Controls.Elmish/ControlsElmish.fs` for clock/RNG use on the hot path (FR-011, plan.md Constraints).
- [X] T033 [P] Run both full test assemblies (unfiltered) and confirm no Feature 092 change regressed neighbours: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj` and `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj`.
- [X] T034 Run the quickstart "Done when" gate end-to-end (`specs/092-wire-retained-identity-state/quickstart.md` §Done when): both `Feature092` suites green, surface-drift zero, readiness regenerates to SC-001..SC-007, and no test was weakened/skipped/hand-seeded.
- [X] T035 Record the disclosed deviation **DF-1** as a bounded Tier-2 follow-up (NOT done in 092): strip the redundant `internal`/`private` access modifiers from top-level bindings in `src/Controls/RetainedRender.fs` and `src/Controls.Elmish/ControlsElmish.fs` (behavior-neutral; plan.md Complexity Tracking / Principle II). Capture as a tracked follow-up item, do not edit code in this pass.
- [X] T036 If any suite is red or any assertion is found weakened/hand-seeded, **STOP and report it as a finding** (do not patch the test to green it) per quickstart.md §2 / Principle V; otherwise mark the conformance pass complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup (assemblies must build) — **blocks** all user-story phases.
- **User Stories (Phase 3–7)**: All depend on Foundational. US1–US5 are **independently verifiable** and may run in any order or in parallel (different suites/cases); priority order is US1 (P1) → US2 (P1) → US3 (P2) → US4 (P2) → US5 (P3).
- **Polish (Phase 8)**: Depends on all user-story phases (full-suite + surface checks come last).

### User Story Independence

- **US1 (P1)** — `Feature092LiveSurvivalTests` in `Elmish.Tests`. Independently verifiable: the headline live-survival + baseline-fails proof.
- **US2 (P1)** — focus-resolution / prefilled-append / multi-handler cases in `Feature092RetainedRenderTests`. Independent of US1.
- **US3 (P2)** — theme-reuse case. Independent.
- **US4 (P2)** — work-reduction case. Independent.
- **US5 (P3)** — first-frame / diagnostics / multi-frame cases. Independent.

### Parallel Opportunities

- Setup builds T002–T005 are all `[P]` (independent `dotnet build` targets).
- Foundational confirmations T007–T009 are `[P]` (different `.fsi`/types).
- Within US2: T018/T019/T020 are `[P]` (distinct test cases). Within US5: T027/T028/T029 are `[P]`.
- US1–US5 phases can be verified in parallel once Foundational is green (two test assemblies; the `Controls.Tests` `Feature092` filter covers US2–US5 in one run — T017 — so T018–T029 are read-throughs against that run).
- Polish T032/T033 are `[P]`.

---

## Parallel Example: Setup

```bash
# Build all affected + test assemblies together:
dotnet build src/Controls/Controls.fsproj
dotnet build src/Controls.Elmish/Controls.Elmish.fsproj
dotnet build tests/Controls.Tests/Controls.Tests.fsproj
dotnet build tests/Elmish.Tests/Elmish.Tests.fsproj
```

## Parallel Example: Run both 092 suites

```bash
# US1 headline (Elmish.Tests) and US2–US5 (Controls.Tests) in parallel:
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature092"
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature092"
```

---

## Implementation Strategy

### Conformance-pass MVP (User Story 1 only)

1. Complete Phase 1: Setup (assemblies build).
2. Complete Phase 2: Foundational (artifacts confirmed present + internal).
3. Complete Phase 3: US1 — run `Feature092LiveSurvivalTests`, confirm survival green + baseline-fails fail-first.
4. **STOP and VALIDATE**: US1 is the headline payoff of 092 over 091; if green through the real seam with no hand-seeded state, the MVP slice is proven.

### Incremental confirmation

1. Setup + Foundational → reference baselines captured.
2. US1 → headline live-survival confirmed (MVP).
3. US2 → correctness defects confirmed closed.
4. US3/US4 → reuse-key + honest accounting confirmed.
5. US5 → first-frame paint/diagnostics + multi-frame parity confirmed.
6. Polish → zero surface delta, totality/determinism, full-suite, DF-1 logged.

### Reporting discipline (Principle V)

- A red test or a weakened/hand-seeded assertion is a **finding to report**, not to patch (T036).
- Never seed focus/text state to green the live-survival test.
- 092 designs **no** new product behavior — these tasks confirm the imported wiring conforms to the backfilled contract; the public-surface delta must stay **zero**.

---

## Notes

- This is a backfill **conformance pass**, not a build — every "implement" reduces to "confirm the existing artifact conforms" (plan.md Summary).
- [P] tasks = different files/commands, no dependencies.
- [Story] label maps each verification task to its user story for traceability.
- Out of scope for 092 (owned by their features): animation-clock survival (099), layout cache (097), cross-fade (103), perf driver (108/110), memo/virtualization/picture/text caches (113/114/116/117), fingerprint/replay (120), and pixel-level/desktop-visibility parity (contracts §Out of contract).
