# Tasks: Visual-State Cross-Fade (Feature 103)

**Input**: Design documents from `/specs/103-visual-state-cross-fade/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/visual-state-cross-fade.md, quickstart.md

**Tests**: Test tasks ARE included — but as a **conformance pass**, not authored-from-scratch. Per the plan,
the implementation (the cross-fade composite in `src/Controls/RetainedRender.fs`/`.fsi`, reached through the
`ControlRuntime` bridge and the `Controls.Elmish` host tick) and the authoritative suite
(`tests/Controls.Tests/Feature103CrossFadeTests.fs`) **already exist** in the imported source. The suite
**self-writes** its readiness evidence on each run. `/speckit-tasks` and `/speckit-implement` reduce to
confirming the suite is green, the readiness regenerates, and the public-surface delta is zero — **not**
building new behaviour.

**Organization**: Tasks are grouped by user story to enable independent verification of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/assertions, no dependencies)
- **[Story]**: Which user story this task verifies (US1–US4)
- Exact file paths included in each task

## Path Conventions

Single F# solution (`FS.GG.Rendering.slnx`) — `src/` and `tests/` at repository root. No new project is added;
103 populates the existing internal `AnimationClock.From` snapshot in `updateClockForState` and makes
`sampleOnPaint` a genuine two-layer composite (the seam shared with feature 099).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the build environment and the artifacts under verification are present.

- [X] T001 Build the solution clean: `dotnet build FS.GG.Rendering.slnx -c Release` — expect 0 warnings, 0 errors (quickstart §1)
- [X] T002 [P] Confirm the 103 surface in `src/Controls/RetainedRender.fsi`: the `From: FS.GG.UI.Scene.Scene list` field on the `internal AnimationClock` record, and `internal` `updateClockForState` + `sampleOnPaint` (shared with 099)
- [X] T003 [P] Confirm the authoritative suite exists: `tests/Controls.Tests/Feature103CrossFadeTests.fs` (US1 cross-fade, US2 byte-identity, US3 determinism, US4 edges)
- [X] T004 [P] Confirm the readiness directory `specs/103-visual-state-cross-fade/readiness/` is present (self-written by the suite): `mid-flight-interpolation.md`, `at-rest-byte-identity.md`, `final-frame-identity.md`, `determinism.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The contract gates that MUST hold before any per-story verification is meaningful — the internal
seam signatures and the zero-public-surface invariant.

**⚠️ CRITICAL**: No user-story verification is trustworthy until these gates pass.

- [X] T005 Verify the seam signatures in `src/Controls/RetainedRender.fsi` match `contracts/visual-state-cross-fade.md` (C2/C3): `updateClockForState: desired -> priorOwn -> carried -> AnimationClock option`; `sampleOnPaint: clock -> ownScene -> Scene list` (FR-001, FR-003, FR-005)
- [X] T006 Verify the `AnimationClock.From` field (C1) in `src/Controls/RetainedRender.fsi`/`.fs` matches `data-model.md`: `From: FS.GG.UI.Scene.Scene list` (the prior own-scene snapshot; `[]` ⇒ plain fade-in)
- [X] T007 Confirm zero public-surface delta (FR-009): regenerate via `dotnet fsi scripts/refresh-surface-baselines.fsx` and confirm `tests/surface-baselines/FS.GG.UI.Controls.txt` and `FS.GG.UI.Controls.Elmish.txt` are **byte-unchanged** (the whole seam is `internal`). Confirm via md5/diff, not a script exit code (no real `--check` mode)

**Checkpoint**: Internal contract and surface invariant confirmed — per-story verification can proceed.

---

## Phase 3: User Story 1 - A visual-state transition genuinely cross-fades its colours (Priority: P1) 🎯 MVP

**Goal**: A state change whose paint differs in a colour shows BOTH endpoints mid-flight (prior fading out
under next fading in), so the displayed colour is strictly between the endpoints — a genuine cross-fade, not a
fade-in-from-transparent.

**Independent Test**: Through the real `ControlRuntime.applyRuntimeVisualState` → `advance` (Tick) → `step`
seam, hover a Switch whose track fill swaps `Muted→Accent`, sample mid-flight (75 ms of 150 ms), and inspect
the `sampleOnPaint` composite: `From` = prior own-scene, both endpoint colours present (prior alpha < full),
displayed value strictly between for every differing channel. The pre-R6 fade-in lacks the prior colour.

- [X] T008 [US1] Run the US1 suite: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "103"` and confirm the cross-fade test in `tests/Controls.Tests/Feature103CrossFadeTests.fs` is green — both endpoint colours present mid-flight, prior alpha < full, displayed colour strictly between (SC-001/INV-3; FR-001, FR-002)
- [X] T009 [US1] Confirm the suite asserts `clock.From = prior (Normal) own-scene snapshot` and the no-seam counterfactual (the pre-R6 fade-in-from-transparent lacks the prior colour mid-flight) in `tests/Controls.Tests/Feature103CrossFadeTests.fs` (FR-003)
- [X] T010 [P] [US1] Confirm the self-written `specs/103-visual-state-cross-fade/readiness/mid-flight-interpolation.md` shows `status=pass` and `prior-colour-present-mid-flight=true` against SC-001

**Checkpoint**: US1 (the MVP headline payoff — a genuine cross-fade) verified on the real seam.

---

## Phase 4: User Story 2 - At-rest and settled output is byte-identical to the static render (Priority: P1)

**Goal**: A no-active-clock frame is byte-identical to the static render (no animation attribute, zero
recompute/remeasure), and a settled transition paints the snapped static render for every channel. The
cross-fade is a mid-flight-only overlay; the settle and fast paths are untouched.

**Independent Test**: Step the live path with no active clock and assert the scene equals `Control.renderTree`
byte-for-byte with no animation attribute and zero recompute/remeasure; advance a clock past the duration and
assert the settled frame equals the snapped static Hover render byte-for-byte.

- [X] T011 [US2] Confirm the at-rest byte-identity test in `tests/Controls.Tests/Feature103CrossFadeTests.fs` is green — no animation clock at rest, scene byte-identical to the static render (SC-002/INV-1; FR-004)
- [X] T012 [US2] Confirm the final-frame identity test is green — a settled transition is byte-identical to the snapped static Hover render for every channel (SC-003/INV-2; FR-005)
- [X] T013 [US2] Confirm the settle/fast-path test is green — an at-rest re-step recomputes 0 nodes and re-measures 0 nodes (the cross-fade is a mid-flight-only overlay; the settle path is unchanged) (FR-004)
- [X] T014 [P] [US2] Confirm the self-written `readiness/at-rest-byte-identity.md` (SC-002) and `readiness/final-frame-identity.md` (SC-003) show `status=pass`

**Checkpoint**: US1 + US2 (the full MVP — a genuine cross-fade that is invisible at rest and after settle) verified.

---

## Phase 5: User Story 3 - The cross-fade is deterministic under injected deltas (Priority: P2)

**Goal**: Replaying an identical injected-delta sequence reproduces an identical sampled-frame sequence (no
wall-clock); a non-positive delta is a no-op (no rewind); a past-duration delta settles canonically.

**Independent Test**: Run a fixed 7-frame injected-delta sequence twice and assert identical sampled frames;
60 FsCheck cases over random bounded sequences; a 0 ms re-step mid-flight leaves the frame unchanged.

- [X] T015 [US3] Confirm the determinism tests in `tests/Controls.Tests/Feature103CrossFadeTests.fs` are green — fixed-sequence replay identical + 60 FsCheck random sequences identical (SC-004/INV-4; FR-006)
- [X] T016 [US3] Confirm the non-positive-delta assertion is green — a 0 ms re-step mid-flight never rewinds (the sampled frame is unchanged) (FR-006)
- [X] T017 [P] [US3] Confirm the self-written `readiness/determinism.md` shows `status=pass`, `wall-clock-consulted=false`, `two-runs-identical=true` against SC-004

**Checkpoint**: The deterministic-replay guard under US1/US2 verified.

---

## Phase 6: User Story 4 - Cross-fade edge cases (retarget, held-state, return-to-Normal, no-colour-delta) (Priority: P2)

**Goal**: A mid-flight retarget re-seeds `From` from the previous target's snapshot and resets `Elapsed`; a
held settled state stays a `Keep` (single scoped repaint); a settled return-to-`Normal` drops the clock; a
no-colour-delta transition introduces no permanent artifact.

**Independent Test**: Hover then press mid-flight (assert `Target = Pressed`, `Elapsed = 0`, `From` = Hover
snapshot); settle + hold Hover (assert recompute = remeasure = 0, clock settled); unhover (assert a return
fade starts active then the settled clock is dropped, returning byte-identical at-rest); a label (no colour
delta) settles + returns byte-identical with no new colour mid-flight.

- [X] T018 [US4] Confirm the retarget-continuity test (INV-5) is green — a Hover→Pressed mid-flight change re-aims `Target`, resets `Elapsed` to 0, and re-seeds `From` from the previous (Hover) own-scene snapshot (FR-003)
- [X] T019 [US4] Confirm the held-state scoped-repaint test (INV-6) is green — a held, settled state repaints 0 / re-measures 0 nodes, the clock stays settled (no spurious re-fire), byte-identical to the static Hover render (FR-007)
- [X] T020 [US4] Confirm the return-to-Normal test (INV-1) is green — a settled Hover clock unhovered starts an active return fade, then once settled is dropped, returning the identity to byte-identical at-rest output (FR-003)
- [X] T021 [US4] Confirm the no-colour-delta test is green — a label (no Normal/Hover colour delta) introduces no new colour mid-flight and settles + returns byte-identical (no permanent artifact) (SC-006; FR-008)

**Checkpoint**: All four user stories independently verified.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature conformance closeout — full suite, surface gate, evidence mapping, cross-artifact
consistency, and the scoped-out boundaries.

- [X] T022 Run the full test suite (`dotnet test FS.GG.Rendering.slnx -c Release`) and confirm 0 failures (no regression introduced by the verification pass; 18 honest skips remain, unrelated to 103)
- [X] T023 Re-confirm zero public-surface delta after the full run: `tests/surface-baselines/FS.GG.UI.Controls.txt` and `FS.GG.UI.Controls.Elmish.txt` byte-unchanged (FR-009)
- [X] T024 [P] Verify the readiness → success-criteria mapping in `specs/103-visual-state-cross-fade/quickstart.md` §2–§3 is accurate (each self-written `readiness/` file maps to SC-001/002/003/004) and that every file discloses its `DeterministicRenderOnly` / structural-scene-equality scope (no pixel/desktop claim)
- [X] T025 [P] Confirm the documented scope boundary holds in `src/Controls/RetainedRender.fsi`: 103 owns the two-snapshot cross-fade composite (`From` populated, prior-out-under-next); the live single-channel clock (feature 099, plain `From = []` fade-in) and the no-alloc idle `advanceStateClocks` (feature 121) are carried in the same `.fsi` but remain out of 103 scope
- [X] T026 Record the inherited Tier-2 follow-up DF-1 (redundant `internal`/`private` access modifiers in `RetainedRender.fs`) as out-of-scope for this backfill (Complexity Tracking in plan.md) — not edited here
- [X] T027 Run `/speckit-analyze` to confirm cross-artifact consistency (spec ↔ plan ↔ tasks) per quickstart "Done When"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup — confirms the contract signatures and zero-surface gate that
  make all per-story verification meaningful. **Blocks Phases 3–6.**
- **User Stories (Phases 3–6)**: All depend on Foundational. Once it passes, all four can be verified in
  parallel (they read different assertions in the same suite).
- **Polish (Phase 7)**: Depends on all desired user-story phases being verified.

### User Story Dependencies

- **US1 (P1)**: Independent — the cross-fade list in `Feature103CrossFadeTests`.
- **US2 (P1)**: Independent — the byte-identity list. Co-critical with US1 for the MVP.
- **US3 (P2)**: Independent — the determinism list.
- **US4 (P2)**: Independent — the edges list.

### Parallel Opportunities

- Setup T002, T003, T004 are independent inspections — run in parallel.
- All four user-story lists run from a single `--filter "103"` invocation; the readiness checks ([P] T010,
  T014, T017) each read a distinct file — run in parallel.
- Polish T024 and T025 are independent reads — run in parallel.

---

## Parallel Example: Verification fan-out

```bash
# One suite carries all four user stories:
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "103"

# Readiness-evidence checks each touch a distinct self-written file:
Task: "Check readiness/mid-flight-interpolation.md against SC-001"
Task: "Check readiness/at-rest-byte-identity.md against SC-002"
Task: "Check readiness/final-frame-identity.md against SC-003"
Task: "Check readiness/determinism.md against SC-004"
```

---

## Implementation Strategy

> This is a **conformance backfill** (task C4 of the 2026-06-15 missing-features plan). "Implementation" =
> confirming the existing artifacts are green and the contract holds — no new product behaviour is built.

### MVP First (US1 + US2)

1. Phase 1: Setup — clean build + artifact presence.
2. Phase 2: Foundational — contract signatures + zero-surface gate (CRITICAL; blocks story verification).
3. Phase 3 + Phase 4: verify US1 (genuine cross-fade) and US2 (at-rest/settled byte-identity).
4. **STOP and VALIDATE**: the MVP (a transition genuinely cross-fades, invisible at rest and after settle) is
   proven through the real `ControlRuntime`/`advance`/`step`/`sampleOnPaint` seam.

### Incremental Verification

1. Setup + Foundational → contract confirmed.
2. US1 → US2 → the MVP (P1) is proven.
3. US3 (P2) → deterministic replay.
4. US4 (P2) → edge behaviours (retarget, held-state, return-to-Normal, no-colour-delta).
5. Polish → full suite, surface gate, readiness mapping, cross-artifact analyze.

---

## Notes

- [P] tasks = different files/assertions, no dependencies.
- [Story] label maps each verification task to its user story for traceability.
- This pass must introduce **no** source edits beyond what is needed to make the suite green; if anything is
  red, that is a finding to report, not a license to redesign. The follow-up DF-1 cleanup is explicitly NOT
  performed here (T026).
- The readiness evidence is **self-written by the suite** (gitignored under `specs/*/readiness/`); confirming
  it regenerates `status=pass` is the readiness deliverable (no separate authoring, unlike 097).
- The surface-drift gate (T007, T023) is the direct verification of FR-009 (which has no separate SC).
- All proofs are `DeterministicRenderOnly`, judged by structural scene equality + descriptive colour/alpha
  inspection; pixel/desktop-visibility is out of scope and disclosed in each readiness file.
</content>
