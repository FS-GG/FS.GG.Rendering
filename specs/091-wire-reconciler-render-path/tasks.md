---
description: "Task list for Feature 091 — Wire the Keyed Reconciler onto the Render Path (conformance/backfill pass)"
---

# Tasks: Wire the Keyed Reconciler onto the Render Path (Feature 091)

**Input**: Design documents from `/specs/091-wire-reconciler-render-path/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/retained-render.md ✅, quickstart.md ✅

**Nature of this feature**: This is a **backfill**. The implementation
(`src/Controls/RetainedRender.fs` + `.fsi`, `src/Controls/Reconcile.fs` + `.fsi`), the FSI surface,
and the authoritative Expecto/FsCheck tests (`tests/Controls.Tests/Feature091RetainedRenderTests.fs`)
**already exist** in the imported source. Per plan.md, `/speckit-tasks` and `/speckit-implement`
reduce to a **conformance pass** — confirm the tests are green, the public-surface delta is zero, and
the `Spec → .fsi → semantic tests → implementation` chain is intact — **not** a build. Tasks are
therefore verification/confirmation tasks. No task may weaken, skip, or rewrite an existing test to
make it pass (Principle V); a red test or non-zero surface delta is a finding to report, not to patch.

**Tests**: The authoritative tests already exist and are the user-reachable surface (vertical-slice
rule). No new test tasks are generated; the per-story tasks **run and confirm** the existing test
lists.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/commands, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US4)
- Include exact file paths / commands in descriptions

## Path Conventions

- Single F# project layout: `src/Controls/`, `tests/Controls.Tests/` at repository root
- Solution: `FS.GG.Rendering.slnx`; surface baselines: `tests/surface-baselines/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the toolchain and the documented build/run path work in this environment.

- [X] T001 Confirm `net10.0` SDK is available and `Directory.Build.props` (`LangVersion=latest`) resolves by running `dotnet --info` and `dotnet restore FS.GG.Rendering.slnx`
- [X] T002 Build the solution clean: `dotnet build FS.GG.Rendering.slnx -c Release` (expected: builds with no errors per quickstart.md §1)

**Checkpoint**: Toolchain verified, solution builds — conformance verification can begin.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the existing internal artifacts that ALL four stories' tests depend on are
present, compiled, and reachable from the test assembly. These are the structural preconditions for
every user story below.

**⚠️ CRITICAL**: No per-story confirmation can be trusted until this phase passes.

- [X] T003 [P] Confirm the 091-in-scope surface is declared in `src/Controls/RetainedRender.fsi` — `RetainedId`, `RenderFragment`, `RetainedNode`, `RetainedUiState`, `AnimationClock`, `RetainedRender` (fields `Root`/`NextId`/`StateByIdentity`/`Theme`), `WorkReductionRecord` (fields `BaselineNodeCount`/`RecomputedNodeCount`/`ChangedSubtreeBound`), `RetainedRenderStep`, and `init`/`step`/`advance` — matching the entities in data-model.md
- [X] T004 [P] Confirm the wired reconciler surface is declared in `src/Controls/Reconcile.fsi` — `diff`/`apply`, `ReconcileResult`, `NodePatch` (`Keep`/`Replace`/`Update`), `ChildOp` — matching contracts/retained-render.md §Operations
- [X] T005 [P] Confirm `KeyCollision` (`ControlDiagnosticCode`) and `Severity.Warning` exist in `src/Controls/Types.fsi`/`src/Controls/Types.fs` (the location fixed by plan.md §Project Structure) so the safe-failure channel (FR-009) is wired
- [X] T006 Confirm `[<assembly: InternalsVisibleTo("Controls.Tests")>]` is present in `src/Controls` so the internal surface is reachable, and that `tests/Controls.Tests/Feature091RetainedRenderTests.fs` compiles against it (covered by the T002 build)
- [X] T007 Confirm the four 091 test lists exist in `tests/Controls.Tests/Feature091RetainedRenderTests.fs` — `091 US1 …`, `091 US2 …`, `091 US3 …`, `091 US4 …` — plus the `Gen091` generators, so each story below has a pinning test to run

**Checkpoint**: Internal surface and test lists confirmed present and reachable — user-story confirmation can now proceed in parallel.

---

## Phase 3: User Story 1 - A control keeps its identity across an unrelated re-render (Priority: P1) 🎯 MVP

**Goal**: Confirm a matched, unchanged node carries the same stable `RetainedId` across an unrelated
re-render (including a positional shift), and a `Kind` change yields a fresh id (no false reuse).

**Independent Test**: Run the `091 US1 identity survives an unrelated re-render` test list and confirm
green; it covers acceptance scenarios 1–3 (unrelated sibling change, insert-above shift, kind-change ⇒
fresh id). Maps to FR-001/FR-002, SC-001, contract C1/C2.

### Confirmation for User Story 1

- [X] T008 [US1] Run `dotnet run --project tests/Controls.Tests -c Release -- --filter "091 US1"` and confirm the `091 US1 identity survives an unrelated re-render` list passes
- [X] T009 [P] [US1] Confirm in `tests/Controls.Tests/Feature091RetainedRenderTests.fs` that the US1 list asserts (a) same `RetainedId` across an unrelated sibling change, (b) same id across an insert-above positional shift, and (c) a `Kind` change under the same `Key` yields a **fresh** id matched via `Replace` — i.e. the test actually pins FR-001 and FR-002, not a weaker proxy
- [X] T010 [P] [US1] Confirm the implementation in `src/Controls/RetainedRender.fs` mints `RetainedId` from the monotonic `NextId` counter (no clock/random — D1/D2) and that identity follows the diff *match*, not the path-derived `ControlId`

**Checkpoint**: US1 (stable identity — the MVP) confirmed green and faithfully pinned.

---

## Phase 4: User Story 2 - Focus and an in-flight animation survive an unrelated re-render (Priority: P1)

**Goal**: Confirm focus + an in-flight animation clock keyed to `RetainedId` survive a position-shifting
re-render and that the carried clock **advances** (does not reset), while the rebuild-every-frame
baseline loses the state.

**Independent Test**: Run the `091 US2 focus + animation survive an unrelated re-render` test list and
confirm green; it covers both acceptance scenarios (state survives shift + `advance` increases
`Elapsed`; baseline loses it). Maps to FR-003, SC-002, contract C3.

### Confirmation for User Story 2

- [X] T011 [US2] Run `dotnet run --project tests/Controls.Tests -c Release -- --filter "091 US2"` and confirm the `091 US2 focus + animation survive an unrelated re-render` list passes
- [X] T012 [P] [US2] Confirm in `tests/Controls.Tests/Feature091RetainedRenderTests.fs` that the US2 list (a) seeds focus + a started `AnimationClock` keyed by `RetainedId`, (b) asserts state is still found under the unchanged identity after a positional shift, (c) asserts `RetainedRender.advance` increases `Elapsed` (no reset), and (d) asserts the rebuild-every-frame baseline **fails** the same proof
- [X] T013 [P] [US2] Confirm in `src/Controls/RetainedRender.fs` that `StateByIdentity` is re-keyed to the carried `RetainedId` across the diff and that `advance` is a pure no-op on non-positive delta / accumulates clamped `Elapsed` on positive delta (D5/contract `advance`); confirm 091 only **carries** state (does not drive the live clock — that is 099)

**Checkpoint**: US2 (the user-visible payoff — focus/animation survival) confirmed green and faithfully pinned.

---

## Phase 5: User Story 3 - A localized change repaints only the changed subtree, with identical output (Priority: P2)

**Goal**: Confirm a localized change recomputes only the changed subtree
(`RecomputedNodeCount ≤ ChangedSubtreeBound < BaselineNodeCount (N)`) and the wired frame is
byte-identical (structural `Scene` + `Bounds` + `NodeCount`) to `Control.renderTree next`.

**Independent Test**: Run the `091 US3 partial update + golden parity` test list and confirm green; it
covers both acceptance scenarios (work-count bound + golden parity). Maps to FR-006/FR-007, SC-003/SC-004,
contract C4/C5/C8.

### Confirmation for User Story 3

- [X] T014 [US3] Run `dotnet run --project tests/Controls.Tests -c Release -- --filter "091 US3"` and confirm the `091 US3 partial update + golden parity` list passes
- [X] T015 [P] [US3] Confirm in `tests/Controls.Tests/Feature091RetainedRenderTests.fs` that the US3 list asserts the strict `WorkReductionRecord` inequality `RecomputedNodeCount ≤ ChangedSubtreeBound < BaselineNodeCount` for a single localized leaf change over a wide tree (SC-003), and separately asserts wired `Render` == `Control.renderTree next` on `Scene`/`Bounds`/`NodeCount` (SC-004)
- [X] T016 [P] [US3] Confirm in `src/Controls/RetainedRender.fs` that fragment reuse is gated on F# **structural** equality **and** an unshifted `Box` (D4/C8) — a structurally-identical-but-relaid-out subtree must repaint at its new position, not reuse verbatim

**Checkpoint**: US3 (work reduction + golden parity) confirmed green and faithfully pinned.

---

## Phase 6: User Story 4 - Reconciler invariants hold on the live path, and malformed input is reported, not fatal (Priority: P2)

**Goal**: Confirm the wired `step` round-trips to a full rebuild, is deterministic and total over ≥1000
generated frame pairs, is a true no-op on structurally identical frames, and surfaces a `KeyCollision`
`Warning` (never throws) on duplicate sibling keys.

**Independent Test**: Run the `091 US4 invariants on the wired path (FsCheck, ≥1000 cases)` test list and
confirm green; it covers round-trip/determinism/totality/identity-at-rest + the duplicate-key diagnostic.
Maps to FR-008/FR-009 (and re-exercises FR-004/FR-005 via the no-op and round-trip invariants; those are primarily pinned by US3/T016), SC-005/SC-006, contract C5/C6/C7/C9.

### Confirmation for User Story 4

- [X] T017 [US4] Run `dotnet run --project tests/Controls.Tests -c Release -- --filter "091 US4"` and confirm the `091 US4 invariants on the wired path (FsCheck, ≥1000 cases)` list passes
- [X] T018 [P] [US4] Confirm in `tests/Controls.Tests/Feature091RetainedRenderTests.fs` that the FsCheck properties use `Config.QuickThrowOnFailure.WithMaxTest 1000` (≥1000 cases each) and cover: round-trip parity to a full rebuild, determinism (identical sequences ⇒ identical render + ids), totality (never throws for any `(prev, next)`), and identity-at-rest (structurally identical frames ⇒ `RecomputedNodeCount = 0`, `NextId` unchanged, no diagnostics) — SC-005, contract C5/C6/C7
- [X] T019 [P] [US4] Confirm the duplicate-key test carries the `Synthetic` token, discloses its malformed literal fixture (Principle V), asserts a `KeyCollision` diagnostic of severity `Warning` on `RetainedRenderStep.Diagnostics`, and asserts `step` completes without throwing (FR-009/SC-006, contract C9)
- [X] T020 [P] [US4] Confirm in `src/Controls/Reconcile.fs` that `diff` matches by `Key` then positionally, emits `KeyCollision` `Warning` on duplicate sibling keys, and never throws (D3/D7), and that `apply prev (diff prev next).Patch` is the round-trip oracle structurally equal to `next`

**Checkpoint**: All four user stories confirmed green on the live wired path with high-volume property evidence.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Confirm the feature-wide contracts (zero public-surface delta, captured evidence, recorded
deviations) and run the full quickstart validation.

- [X] T021 Confirm **zero public-surface-baseline delta** (FR-010, contract C10 — verified by the surface-drift check, no separate SC): run the surface-drift check and `dotnet fsi scripts/refresh-surface-baselines.fsx`, and confirm `tests/surface-baselines/` reports **no changes** for any `FS.GG.UI.Controls*` baseline — a non-zero delta is a contract failure to report, not to accept
- [X] T022 [P] Confirm the captured readiness evidence under `specs/091-wire-reconciler-render-path/readiness/` (`retained-parity`, `work-reduction`, `survives-proof`) is `status=pass` and each names its `authoritative-test=…`, and that `retained-parity` honestly discloses it proves **structural** scene equality, not pixels/desktop visibility (D8)
- [X] T023 [P] Confirm plan.md Complexity Tracking still accurately records the three justified deviations (contract-first order inverted; redundant `internal`/`private` access modifiers in `RetainedRender.fs`; `.fsi` carries later-feature 092–120 fields) — no new deviation was introduced by this pass
- [X] T024 Run the full quickstart.md validation end to end: `dotnet test tests/Controls.Tests -c Release` green (all four 091 lists, ≥1000 FsCheck cases each), surface delta zero, no test skipped or weakened ("Done when" in quickstart.md §Done)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup (needs a clean build) — BLOCKS all user-story confirmation.
- **User Stories (Phase 3–6)**: All depend on Foundational. Because the implementation already exists, the four stories are **mutually independent** and their confirmation can run in parallel (or in priority order P1 US1 → P1 US2 → P2 US3 → P2 US4).
- **Polish (Phase 7)**: Depends on all four stories being confirmed (T024 re-runs the whole suite as the final gate).

### User Story Dependencies

- **US1 (P1)** — foundation of stable identity; no dependency on other stories. The MVP.
- **US2 (P1)** — relies conceptually on US1's stable identity but is independently testable via its own list.
- **US3 (P2)** — relies conceptually on US1's identity to know what is unchanged; independently testable.
- **US4 (P2)** — protects US1–US3 (safety/robustness); independently testable via FsCheck.

### Within Each User Story

- Run the story's test list (the authoritative gate) → confirm the test faithfully pins the requirement → confirm the implementation embodies the decision. Do not weaken a test to green it.

### Parallel Opportunities

- Foundational surface checks T003/T004/T005 are different files → run in parallel.
- Once Phase 2 passes, the four story confirmations (T008, T011, T014, T017) can run in parallel.
- Within each story, the `[P]` confirmation tasks (test-faithfulness check vs. implementation check) touch different files and run in parallel.
- Polish T022/T023 are independent reads → parallel.

---

## Parallel Example: Foundational + User Stories

```bash
# Phase 2 — confirm the internal surface (different files, in parallel):
Task: "T003 Confirm RetainedRender.fsi 091 surface matches data-model.md"
Task: "T004 Confirm Reconcile.fsi diff/apply surface matches the contract"
Task: "T005 Confirm KeyCollision / Severity.Warning exist in Types.*"

# After Phase 2 — run all four story test lists in parallel:
Task: "T008 Run 091 US1 list"
Task: "T011 Run 091 US2 list"
Task: "T014 Run 091 US3 list"
Task: "T017 Run 091 US4 list"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (build clean).
2. Complete Phase 2: Foundational (surface + tests reachable — CRITICAL).
3. Complete Phase 3: US1 — confirm stable identity green and faithfully pinned.
4. **STOP and VALIDATE**: the MVP slice (diff confers a carried identity) is independently confirmed.

### Incremental Delivery (conformance order)

1. Setup + Foundational → chain confirmed reachable.
2. US1 (stable identity) → confirm → MVP gate.
3. US2 (focus/animation survival) → confirm.
4. US3 (work reduction + golden parity) → confirm.
5. US4 (invariants + safe failure) → confirm.
6. Polish (zero surface delta + evidence + full-suite gate) → feature conformance complete.

### Note on this being a backfill

No product behavior is built here. If any task surfaces a **red test**, a **non-zero surface delta**,
or an **`.fsi`/data-model mismatch**, that is a conformance finding to surface to the author — not a
license to edit a test or the surface to make the pass go green (Principle V; FR-010).

---

## Deferred Follow-Ups (out of scope for this backfill)

These are explicit, bounded deferrals recorded per the constitution's Development Workflow
("Any intentional deferral MUST be explicit in the spec or plan and scoped as a bounded follow-up").
They are **not** done in this conformance pass — which is a documentation/verification backfill, not a
code edit — but they are scoped here so the deferral is tracked, not silent.

- [ ] **DF-1 [Tier 2, Principle II]** Strip the redundant `internal`/`private` access modifiers from
  the top-level bindings in `src/Controls/RetainedRender.fs` (e.g. `type internal RetainedId`,
  `let private isMemoizable`). Visibility is already fully declared by `src/Controls/RetainedRender.fsi`,
  so this is **behavior-neutral** and adds **zero** surface delta. Scope: one imported file; no spec
  or test change. Why deferred from 091: bundling a code edit into a doc backfill would mix concerns
  (plan.md Complexity Tracking, deviation #2). Done when: the modifiers are removed, the solution still
  builds, and the surface-drift check (T021) still reports zero changes.

## Notes

- [P] tasks = different files/commands, no dependencies.
- [Story] label maps each task to its user story (US1–US4) for traceability to FR/SC/contract IDs.
- Each user story is independently confirmable via its own `091 US…` test-list filter.
- Verify the tests are genuine (faithfully pin the requirement) — do not weaken or skip to pass.
- A red test or non-zero surface delta is a finding, not a task to "fix" by editing the test/surface.
