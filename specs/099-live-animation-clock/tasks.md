# Tasks: Live Animation Clock (Feature 099)

**Input**: Design documents from `/specs/099-live-animation-clock/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/live-animation-clock.md, quickstart.md

**Tests**: Test tasks ARE included — but as a **conformance pass**, not authored-from-scratch. Per the plan, the
implementation (`RetainedRender.fs`/`.fsi`, the `ControlRuntime`/`Controls.Elmish` host seam), both Expecto/FsCheck
suites, and the captured `readiness/` evidence **already exist** in the imported source. `/speckit-tasks` and
`/speckit-implement` reduce to confirming the suites are green, the readiness evidence regenerates, and the
public-surface delta is zero — **not** building new behavior.

**Organization**: Tasks are grouped by user story to enable independent verification of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/assemblies, no dependencies)
- **[Story]**: Which user story this task verifies (US1–US5)
- Exact file paths included in each task

## Path Conventions

Single F# solution (`FS.GG.Rendering.slnx`) — `src/` and `tests/` at repository root. No new project is added;
099 wires the existing internal `AnimationClock` slot to the `ControlRuntime` visual-state bridge and the
`Controls.Elmish` host tick.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the build environment and the artifacts under verification are present.

- [X] T001 Build the solution clean: `dotnet build FS.GG.Rendering.slnx -c Release` — expect 0 warnings, 0 errors (quickstart §1)
- [X] T002 [P] Confirm the 099 source artifacts exist and carry the in-scope seam: `src/Controls/RetainedRender.fsi` declares `internal` `defaultTransitionDuration`, `advance`, `clockActive`, `updateClockForState`, `sampleOnPaint`, and the `AnimationClock` record; `src/Controls.Elmish/ControlsElmish.fs` carries the host tick that injects the per-frame delta
- [X] T003 [P] Confirm both authoritative suites exist: `tests/Controls.Tests/Feature099AnimationClockTests.fs` (pure clock core) and `tests/Elmish.Tests/Feature099AnimationSeamTests.fs` (live seam)
- [X] T004 [P] Confirm the captured readiness evidence is present: `specs/099-live-animation-clock/readiness/` contains `us1-animates-vs-snaps.md`, `us2-survival.md`, `us3-determinism.md`, `us3-identity-at-rest.md`, `us4-gc.md`, `scoped-repaint.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The contract gates that MUST hold before any per-story verification is meaningful — the internal
seam signatures and the zero-public-surface invariant.

**⚠️ CRITICAL**: No user-story verification is trustworthy until these gates pass.

- [X] T005 Verify the seam signatures in `src/Controls/RetainedRender.fsi` match the contract `contracts/live-animation-clock.md` exactly (C1–C5): `defaultTransitionDuration: System.TimeSpan`; `advance: delta -> clock -> AnimationClock`; `clockActive: clock -> bool`; `updateClockForState: desired -> priorOwn -> carried -> AnimationClock option`; `sampleOnPaint: clock -> ownScene -> Scene list` (FR-002, FR-003, FR-008, FR-011)
- [X] T006 Verify the `AnimationClock` record fields in `src/Controls/RetainedRender.fsi`/`.fs` match `data-model.md`: `Anim` (feature-073 `Animation`, opacity channel), `Elapsed` (injected-delta `TimeSpan`), `Target` (`VisualState`), `From` (`Scene list`, `[]` for the 099 plain fade-in)
- [X] T007 Confirm zero public-surface delta (FR-012): run `dotnet fsi scripts/refresh-surface-baselines.fsx --check` and verify `tests/surface-baselines/FS.GG.UI.Controls.txt` and `FS.GG.UI.Controls.Elmish.txt` are **byte-unchanged** (the whole seam is `internal`)

**Checkpoint**: Internal contract and surface invariant confirmed — per-story verification can proceed.

---

## Phase 3: User Story 1 - A visual-state transition animates (not snaps) on the live host (Priority: P1) 🎯 MVP

**Goal**: A visual-state change on the real host animates over the framework default (150 ms, `EaseOut`)
through `ControlRuntime.applyRuntimeVisualState` → `RetainedRender.advance` (Tick) → `RetainedRender.step`,
rather than snapping in one frame.

**Independent Test**: On the live seam, hover a button animating the opacity channel, inject 16 ms deltas,
capture the sampled frame sequence; confirm the first frame ≠ the snapped target, ≥1 structurally-distinct
intermediate frame precedes a frame byte-equal to the static snapped target, and the final frame equals the
target exactly. A no-seam build snaps on frame 0 and fails the assertion.

- [X] T008 [US1] Run the live-seam suite filtered to US1: `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature099"` and confirm the animate-not-snap assertions in `tests/Elmish.Tests/Feature099AnimationSeamTests.fs` are green (FR-001, FR-002)
- [X] T009 [US1] Confirm the suite asserts the no-seam counterfactual (a build without the seam snaps the target on frame 0 with no intermediate frame) in `tests/Elmish.Tests/Feature099AnimationSeamTests.fs`
- [X] T010 [P] [US1] Regenerate and check `specs/099-live-animation-clock/readiness/us1-animates-vs-snaps.md` against SC-001 (≥1 intermediate frame, converges exactly, no overshoot)

**Checkpoint**: US1 (the MVP headline payoff) verified on the real seam.

---

## Phase 4: User Story 2 - An in-flight animation survives an unrelated re-render and completes (Priority: P1)

**Goal**: A mid-flight animation keeps advancing on its same trajectory when an unrelated re-render shifts the
animating control's position, because the clock rides the stable `RetainedId`-keyed `StateByIdentity` map.

**Independent Test**: Through the real `advance` (Tick) + `step` seam, hover a button, tick it mid-flight to
32 ms (two 16 ms deltas), insert a banner above it (sibling shift), then tick once more across the shift and
continue to completion; confirm the `RetainedId` is stable, elapsed **continues** across the straddling tick
(32 ms → 48 ms, not reset), and the shifted trajectory is byte-identical to the unshifted trajectory — with
no hand-seeded clock.

- [X] T011 [US2] Run the live-seam suite and confirm the survival assertions in `tests/Elmish.Tests/Feature099AnimationSeamTests.fs` are green: identity stable across the shift, elapsed continues (not reset), shifted trajectory byte-identical to the unshifted one (FR-005)
- [X] T012 [US2] Confirm the survival proof uses the real `RetainedId`-keyed carry with **no** hand-seeded clock (the real-seam replacement for feature 092's `startedClock()` precondition) in `tests/Elmish.Tests/Feature099AnimationSeamTests.fs`
- [X] T013 [P] [US2] Regenerate and check `specs/099-live-animation-clock/readiness/us2-survival.md` against SC-002 (identity stable, elapsed continues, byte-identical trajectory)

**Checkpoint**: US1 + US2 (the full MVP) verified.

---

## Phase 5: User Story 3 - The pure clock core is deterministic, and an at-rest identity is invisible (Priority: P2)

**Goal**: `advance` is pure/total/deterministic (identical injected-delta sequence ⇒ byte-identical output, no
wall-clock, per-identity independence), and an at-rest identity is byte-identical to the full static rebuild
with zero recompute/remeasure; a settled return-to-`Normal` clock is dropped.

**Independent Test**: Drive `RetainedRender.advance` with a fixed 12-frame delta sequence twice and assert
byte-identical (both settle at 150 ms); 1000 FsCheck cases over random sequences. Step the live retained path
on a no-active-clock frame and assert byte-identical to `Control.renderTree`, at-rest recompute = remeasure =
0, and the settled `Normal` clock dropped.

- [X] T014 [P] [US3] Run the pure-core suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature099"` and confirm determinism (fixed 12-frame replay byte-identical, 1000 FsCheck cases) in `tests/Controls.Tests/Feature099AnimationClockTests.fs` (FR-003, FR-007)
- [X] T015 [P] [US3] Confirm the advance-edge assertions in `tests/Controls.Tests/Feature099AnimationClockTests.fs`: non-positive delta no-op (no rewind), large-delta clamp (settle at End, no overshoot), mid-flight retarget from current sampled value (no snap to start), settled return-to-`Normal` drop, multi-clock independence (FR-004, FR-008)
- [X] T016 [P] [US3] Confirm the identity-at-rest assertion in `tests/Controls.Tests/Feature099AnimationClockTests.fs`: a no-active-clock frame is byte-identical to the full `Control.renderTree` static rebuild with at-rest recompute = 0 and remeasure = 0 (FR-006)
- [X] T017 [P] [US3] Regenerate and check `specs/099-live-animation-clock/readiness/us3-determinism.md` (SC-004) and `specs/099-live-animation-clock/readiness/us3-identity-at-rest.md` (SC-003)

**Checkpoint**: The pure-core determinism and invisible-at-rest guards under US1/US2 verified.

---

## Phase 6: User Story 4 - A removed identity's animation clock is garbage-collected (Priority: P3)

**Goal**: When a control owning an active clock is removed, the next frame's `StateByIdentity` carries no clock
for that identity — collected by the existing `liveIds` filter, no new GC code path.

**Independent Test**: Hover a button (clock active), confirm a clock is present for its identity while live,
re-render with the button removed, and confirm the next frame's `StateByIdentity` carries no clock for that
identity.

- [X] T018 [US4] Run the live-seam suite and confirm the GC assertion in `tests/Elmish.Tests/Feature099AnimationSeamTests.fs`: clock present while live, absent after removal — via the existing `liveIds` filter, matching focus/text GC (FR-010)
- [X] T019 [P] [US4] Regenerate and check `specs/099-live-animation-clock/readiness/us4-gc.md` against SC-005 (removed identity's clock absent from the next frame)

**Checkpoint**: Clock GC hygiene verified.

---

## Phase 7: User Story 5 - One active animation does not force a whole-tree repaint (Priority: P2)

**Goal**: A structurally-unchanged animating frame takes the `Keep` fast path (steady-state recompute = 0,
remeasure = 0) while still sampling the active clock, so one animation never invalidates the rest of the tree's
fast path.

**Independent Test**: On a steady-state animating frame, inspect the `RetainedRender.step` `WorkReduction`
metric and confirm steady-state recompute = 0 and remeasure = 0 while the frame still reports a change (the
clock was sampled).

- [X] T020 [US5] Run the live-seam suite and confirm the scoped-repaint assertion in `tests/Elmish.Tests/Feature099AnimationSeamTests.fs`: `WorkReduction` steady-state recompute = 0 and remeasure = 0, yet the frame reports a change (clock sampled) (FR-009)
- [X] T021 [P] [US5] Regenerate and check `specs/099-live-animation-clock/readiness/scoped-repaint.md` against SC-006 (one active animation ≠ whole-tree repaint)

**Checkpoint**: All five user stories independently verified.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature conformance closeout — full suite, surface gate, evidence mapping, cross-artifact
consistency, and the scoped-out boundaries.

- [X] T022 Run the full test suite (`dotnet test FS.GG.Rendering.slnx -c Release`) and confirm 0 failures across `Controls.Tests` and `Elmish.Tests` (no regression introduced by the verification pass)
- [X] T023 Re-confirm zero public-surface delta after the full run: `tests/surface-baselines/FS.GG.UI.Controls.txt` and `FS.GG.UI.Controls.Elmish.txt` byte-unchanged (FR-012)
- [X] T024 [P] Verify the readiness → success-criteria mapping table in `specs/099-live-animation-clock/quickstart.md` §5 is accurate (each `readiness/` file maps to SC-001…SC-006) and that every file discloses its `DeterministicRenderOnly` / structural-scene-equality scope (no pixel/desktop claim)
- [X] T025 [P] Confirm the documented scope boundary holds in `src/Controls/RetainedRender.fsi`: 099 owns only the live opacity-channel fade-in (`From = []`); the two-snapshot cross-fade composite (feature 103) and the no-alloc idle `advanceStateClocks` (feature 121) are carried in the same `.fsi` but remain out of 099 scope — not asserted by the Feature099 suites
- [X] T026 Record the inherited Tier-2 follow-up DF-1 (redundant `internal`/`private` access modifiers in `RetainedRender.fs`) as out-of-scope for this backfill (Complexity Tracking in plan.md) — not edited here
- [X] T027 Run `/speckit-analyze` to confirm cross-artifact consistency (spec ↔ plan ↔ tasks) per quickstart "Done When"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup — confirms the contract signatures and zero-surface gate that
  make all per-story verification meaningful. **Blocks Phases 3–7.**
- **User Stories (Phases 3–7)**: All depend on Foundational. Once it passes, all five can be verified in
  parallel (they read different suites / different assertions).
- **Polish (Phase 8)**: Depends on all desired user-story phases being verified.

### User Story Dependencies

- **US1 (P1)**: Independent — `Feature099AnimationSeamTests` (Elmish.Tests).
- **US2 (P1)**: Independent — same suite, survival assertions. Co-critical with US1 for the MVP.
- **US3 (P2)**: Independent — `Feature099AnimationClockTests` (Controls.Tests). Different assembly from
  US1/US2/US4/US5 → fully parallelizable.
- **US4 (P3)**: Independent — same suite as US1 (GC assertion).
- **US5 (P2)**: Independent — same suite as US1 (`WorkReduction` assertion).

### Parallel Opportunities

- Setup T002, T003, T004 are independent inspections — run in parallel.
- The pure-core verification (US3: T014–T017, all in `Controls.Tests`) runs fully in parallel with the
  live-seam verification (US1/US2/US4/US5, all in `Elmish.Tests`) — different assemblies, different `--filter`
  runs.
- Every readiness-evidence check marked [P] (T010, T013, T017, T019, T021) reads a distinct file — run in
  parallel.
- Polish T024 and T025 are independent reads — run in parallel.

---

## Parallel Example: Verification fan-out

```bash
# The two suites are in different assemblies — run them concurrently:
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature099"   # US3 (pure core)
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj   --filter "Feature099"   # US1/US2/US4/US5 (live seam)

# Readiness-evidence checks each touch a distinct file:
Task: "Check readiness/us1-animates-vs-snaps.md against SC-001"
Task: "Check readiness/us2-survival.md against SC-002"
Task: "Check readiness/us3-determinism.md against SC-004"
Task: "Check readiness/us3-identity-at-rest.md against SC-003"
Task: "Check readiness/us4-gc.md against SC-005"
Task: "Check readiness/scoped-repaint.md against SC-006"
```

---

## Implementation Strategy

> This is a **conformance backfill** (task C3 of the 2026-06-15 missing-features plan). "Implementation" =
> confirming the existing artifacts are green and the contract holds — no new product behavior is built.

### MVP First (US1 + US2)

1. Phase 1: Setup — clean build + artifact presence.
2. Phase 2: Foundational — contract signatures + zero-surface gate (CRITICAL; blocks story verification).
3. Phase 3 + Phase 4: verify US1 (animate-not-snap) and US2 (survival) on the real seam.
4. **STOP and VALIDATE**: the MVP headline payoff (transitions animate and survive a shift) is proven through
   the real `ControlRuntime`/`advance`/`step` seam.

### Incremental Verification

1. Setup + Foundational → contract confirmed.
2. US1 → US2 → the MVP (P1) is proven.
3. US3 (P2) → pure-core determinism + invisible-at-rest guards.
4. US5 (P2) → scoped-repaint efficiency guard.
5. US4 (P3) → clock GC hygiene.
6. Polish → full suite, surface gate, evidence mapping, cross-artifact analyze.

---

## Notes

- [P] tasks = different files/assemblies, no dependencies.
- [Story] label maps each verification task to its user story for traceability.
- This pass must introduce **no** source edits beyond what is needed to make the suites green; if anything is
  red, that is a finding to report, not a license to redesign. The follow-up DF-1 cleanup is explicitly NOT
  performed here (T026).
- The surface-drift gate (T007, T023) is the direct verification of FR-012 (which has no separate SC).
- All proofs are `DeterministicRenderOnly`, judged by structural scene equality + clock-trajectory
  byte-equality; pixel/desktop-visibility is out of scope and disclosed in each readiness file.
