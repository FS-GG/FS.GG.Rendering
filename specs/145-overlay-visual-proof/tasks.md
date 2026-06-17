# Tasks: Overlay Visual Proof

**Input**: Design documents from `/specs/145-overlay-visual-proof/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/overlay-visual-proof.md`, `quickstart.md`

**Tests**: Included because the feature specification marks User Scenarios & Testing as mandatory and defines independent tests for every user story.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independent increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: Which user story the task serves, using `US1`, `US2`, `US3`, or `US4`.
- Every task includes exact repository paths.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish Feature 145 readiness locations and test entry points without changing product behavior.

- [X] T001 Create the readiness directory skeleton and artifact placeholder in `specs/145-overlay-visual-proof/readiness/README.md` and `specs/145-overlay-visual-proof/readiness/artifacts/.gitkeep`
- [X] T002 [P] Create an empty Feature 145 harness test module in `tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs`
- [X] T003 [P] Add the Feature 145 harness test compile entry in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`
- [X] T004 [P] Create an empty Feature 145 AntShowcase test module in `samples/AntShowcase/AntShowcase.Tests/Feature145OverlayVisualProofTests.fs`
- [X] T005 [P] Add the Feature 145 AntShowcase test compile entry in `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define the internal harness evidence model shared by all Feature 145 stories.

**Critical**: No user story work should begin until the Feature 145 evidence model compiles.

- [X] T006 Define `OverlayVisualProofScenario`, `HostCapabilityResult`, `VisualArtifact`, `OverlayVisualCorrelation`, `VisualProofRun`, `UnsupportedHostLimitation`, and `ReadinessCaveatDecision` signatures in `tests/Rendering.Harness/Evidence.fsi`
- [X] T007 Add failing semantic tests for the Feature 145 evidence model through `tests/Rendering.Harness/Evidence.fsi` in `tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs`
- [X] T008 Implement the Feature 145 evidence record types and stable token helpers in `tests/Rendering.Harness/Evidence.fs`
- [X] T009 Add the stable Feature 144 date-picker proof scenario constants and readiness artifact path rules in `tests/Rendering.Harness/Evidence.fs`

**Checkpoint**: Foundation ready - the shared evidence model has FSI-first semantic coverage before implementation proceeds.

---

## Phase 3: User Story 1 - Capture Real Overlay Visual Proof (Priority: P1)

**Goal**: On a capable display/GL host, capture current-run open and final closed overlay artifacts and accept only real, non-empty, scenario-linked proof.

**Independent Test**: Run Feature 145 harness tests or the visual-proof validation on a capable host; it passes only when open and closed artifacts are real, current-run, non-empty, and linked to the expected overlay scenario.

### Tests for User Story 1

Write these tests first and verify they fail before implementation.

- [X] T010 [US1] Add capable-host proof tests for accepted open and closed artifacts, including overlay-above-content and final no-stale-pixel criteria, in `tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs`
- [X] T011 [US1] Add artifact rejection tests for missing, blank, zero-sized, stale, unreadable, and disconnected artifacts in `tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs`

### Implementation for User Story 1

- [X] T012 [US1] Implement current-run visual artifact validation in `tests/Rendering.Harness/Evidence.fs`
- [X] T013 [US1] Implement explicit overlay-above-content, topmost-hit, and closed-state no-stale-pixel acceptance fields in `tests/Rendering.Harness/Evidence.fs`
- [X] T014 [US1] Declare the overlay visual-proof runner signature in `tests/Rendering.Harness/Live.fsi`
- [X] T015 [US1] Implement `Live.runOverlayVisualProof` with open and closed capture records in `tests/Rendering.Harness/Live.fs`
- [X] T016 [US1] Add the `overlay-visual-proof` harness CLI subcommand and help text in `tests/Rendering.Harness/Cli.fs`
- [X] T017 [US1] Run three equivalent capable-host proof attempts, compare scenario names, evidence labels, decisions, and readiness status, and record the result in `specs/145-overlay-visual-proof/readiness/test-results.md`

**Checkpoint**: US1 can prove or reject real capable-host artifacts without relying on deterministic logs as visual proof.

---

## Phase 4: User Story 2 - Preserve Honest Unsupported-Host Reporting (Priority: P1)

**Goal**: On unsupported hosts, report environment-limited status with owner, cause, next proof path, and no fabricated visual success.

**Independent Test**: Run the validation against a known no-display or no-GL host; it must not claim a visual pass or accept synthetic artifacts.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T018 [US2] Add unsupported-host classification tests for no-display and missing-GL facts in `tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs`
- [X] T019 [US2] Add tests named with `Synthetic` and use-site comments proving synthetic fixtures, deterministic logs, and unsupported records cannot satisfy real visual proof in `tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs`

### Implementation for User Story 2

- [X] T020 [US2] Implement Feature 145 host capability classification in `tests/Rendering.Harness/Live.fs`
- [X] T021 [US2] Implement unsupported-host limitation rendering with owner, cause, host facts, next proof path, trust rationale, and `notAuthoritativeFor` fields in `tests/Rendering.Harness/Evidence.fs`
- [X] T022 [US2] Wire environment-limited CLI exit handling and unsupported-host report writing in `tests/Rendering.Harness/Cli.fs`
- [X] T023 [US2] Run the unsupported-host validation path and record the limitation in `specs/145-overlay-visual-proof/readiness/unsupported-host.md`

**Checkpoint**: US2 preserves the Feature 144 caveat unless a real capable-host proof passes.

---

## Phase 5: User Story 3 - Correlate Visual Proof With Existing Behavioral Evidence (Priority: P2)

**Goal**: Tie every visual artifact to scenario identity, input step, expected overlay state, topmost hit target, focus state, and product dispatch evidence.

**Independent Test**: Run the representative date-picker overlay flow and verify every accepted artifact has matching behavioral correlation metadata; mismatches fail as overlay validation issues.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T024 [P] [US3] Add AntShowcase date-picker correlation tests in `samples/AntShowcase/AntShowcase.Tests/Feature145OverlayVisualProofTests.fs`
- [X] T025 [US3] Add harness mismatch tests for artifact state, hit target, focus, and dispatch disagreement in `tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs`

### Implementation for User Story 3

- [X] T026 [US3] Extend the date-picker reference evidence with scenario id, input step, expected state, topmost hit target, focus state, and dispatch summary in `samples/AntShowcase/AntShowcase.Core/Evidence.fs`
- [X] T027 [US3] Implement visual-behavior correlation acceptance and mismatch diagnostics in `tests/Rendering.Harness/Evidence.fs`
- [X] T028 [US3] Connect the AntShowcase date-picker reference evidence to the selected visual-proof scenario in `tests/Rendering.Harness/Live.fs`
- [X] T029 [US3] Write the correlation readiness record in `specs/145-overlay-visual-proof/readiness/correlation.md`
- [X] T030 [US3] Run `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj --filter "Feature145|Feature144|DatePicker"` and record the result in `specs/145-overlay-visual-proof/readiness/test-results.md`

**Checkpoint**: US3 makes the screenshot evidence reviewable against deterministic Feature 144 behavior.

---

## Phase 6: User Story 4 - Close or Preserve the P5 Readiness Caveat (Priority: P2)

**Goal**: Produce a clear readiness decision stating whether the Feature 144 visual-proof caveat is closed, environment-gated, or failed.

**Independent Test**: Review readiness output after capable-host and unsupported-host runs; a maintainer can determine within minutes whether the gate is closed or still environment-gated.

### Tests for User Story 4

Write these tests first and verify they fail before implementation.

- [X] T031 [US4] Add readiness decision tests for closed, environment-gated, and failed proof runs in `tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs`

### Implementation for User Story 4

- [X] T032 [US4] Implement `ReadinessCaveatDecision` evaluation for passed, environment-limited, and failed visual-proof runs in `tests/Rendering.Harness/Evidence.fs`
- [X] T033 [US4] Add visual-proof readiness report generation to the overlay-proof CLI path in `tests/Rendering.Harness/Cli.fs`
- [X] T034 [US4] Run the readiness decision path and record whether the caveat is closed, environment-gated, or failed in `specs/145-overlay-visual-proof/readiness/test-results.md`
- [X] T035 [US4] Generate the Feature 145 visual-proof readiness report from the completed readiness decision in `specs/145-overlay-visual-proof/readiness/visual-proof.md`
- [X] T036 [US4] Update the radical rendering architecture report with the Feature 145 outcome in `docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md`

**Checkpoint**: US4 gives maintainers an unambiguous P5 gate decision.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate the full feature, document scope, and keep readiness records honest.

- [X] T037 [P] Update final validation commands and any confirmed command names in `specs/145-overlay-visual-proof/quickstart.md`
- [X] T038 [P] Verify no product-facing behavior, public `.fsi`, package surface, or compatibility change was made and record the Tier 2 scope result in `specs/145-overlay-visual-proof/readiness/scope-review.md`
- [X] T039 Run `dotnet restore FS.GG.Rendering.slnx`, `dotnet build FS.GG.Rendering.slnx`, and focused Feature 145 test commands, then record results in `specs/145-overlay-visual-proof/readiness/test-results.md`
- [X] T040 Confirm `src/Testing`, `src/SkiaViewer`, and public surface baselines were untouched or document any Tier 1 reclassification stop in `specs/145-overlay-visual-proof/readiness/scope-review.md`
- [X] T041 Review all Feature 145 readiness files for stale artifacts, synthetic visual claims, and missing unsupported-host disclosure in `specs/145-overlay-visual-proof/readiness/README.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on setup and blocks all user story work.
- **US1 and US2 (P1)**: Start after foundational model work. US1 proves capable-host artifacts; US2 protects unsupported-host honesty.
- **US3 (P2)**: Starts after the shared evidence model and can use fixtures, but final completion depends on US1/US2 run result shapes.
- **US4 (P2)**: Depends on US1, US2, and US3 outputs because it summarizes the final readiness decision.
- **Polish (Phase 7)**: Depends on all desired stories being complete.

### User Story Dependencies

- **US1**: Requires Phase 2 only.
- **US2**: Requires Phase 2 only.
- **US3**: Requires Phase 2 for test-driven work; final correlation uses US1 artifact and US2 status shapes.
- **US4**: Requires US1 and US2 decision inputs; uses US3 correlation when visual proof exists.

### Dependency Graph

```text
Phase 1 Setup
  -> Phase 2 Foundation
      -> US1 Capture real proof
      -> US2 Unsupported-host honesty
          -> US3 Behavioral correlation
              -> US4 Caveat decision
                  -> Phase 7 Polish
```

### Parallel Opportunities

- T002, T003, T004, and T005 can run in parallel after T001.
- T024 can run in parallel with T025 because it touches AntShowcase tests while T025 touches harness tests.
- T037 and T038 can run in parallel during polish because they update different documents.
- After Phase 2, US1 and US2 can be implemented by separate contributors if they coordinate edits to `tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs`, `tests/Rendering.Harness/Evidence.fs`, `tests/Rendering.Harness/Live.fs`, and `tests/Rendering.Harness/Cli.fs`.

---

## Parallel Example: User Story 3

```bash
Task: "T024 Add AntShowcase date-picker correlation tests in samples/AntShowcase/AntShowcase.Tests/Feature145OverlayVisualProofTests.fs"
Task: "T025 Add harness mismatch tests for artifact state, hit target, focus, and dispatch disagreement in tests/Rendering.Harness.Tests/Feature145OverlayVisualProofTests.fs"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete US1 to prove real capable-host artifact acceptance and rejection.
3. Complete US2 before claiming readiness so unsupported hosts cannot become false visual passes.
4. Stop and validate the P1 slice with `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature145|Feature144"`.

### Incremental Delivery

1. Deliver setup and foundation.
2. Deliver US1 and US2 as the P1 readiness-proof core.
3. Deliver US3 to correlate pixels with deterministic Feature 144 behavior.
4. Deliver US4 to close, preserve, or fail the P5 readiness caveat.
5. Finish polish validation and readiness documentation.

### Validation Commands

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature145|Feature144"
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj --filter "Feature145|Feature144|DatePicker"
```

Run the `src/Testing` or `src/SkiaViewer` focused commands from `quickstart.md` only if those paths are changed; otherwise record that they were not required in `specs/145-overlay-visual-proof/readiness/test-results.md`.

---

## Notes

- Keep the feature Tier 2: no product-facing overlay behavior changes and no public package/API changes.
- Stop for Tier 1 reclassification before any unavoidable public `.fsi`, package surface, dependency, or compatibility change.
- Capable-host visual proof requires real current-run artifacts under `specs/145-overlay-visual-proof/readiness/artifacts/`.
- Unsupported-host records must not include synthetic screenshots or `provesScreenshot=true`.
- Every task above follows the required checkbox, sequential ID, optional `[P]`, optional story label, and exact-path checklist format.
