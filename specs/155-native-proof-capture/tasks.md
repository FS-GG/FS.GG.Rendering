# Tasks: Native Proof Capture

**Input**: Design documents from `specs/155-native-proof-capture/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by the specification and repository constitution. Write focused tests before implementation tasks where the task changes behavior.

**Organization**: Tasks are grouped by user story so each story can be independently validated.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish Feature 155 constants, directories, and validation surface.

- [X] T001 Add Feature 155 constants, readiness paths, target host profile metadata, and artifact path helper declarations in tests/Rendering.Harness/Compositor.fsi
- [X] T002 Add Feature 155 constants, readiness paths, target host profile metadata, and artifact path helper implementation in tests/Rendering.Harness/Compositor.fs
- [X] T003 [P] Create Feature 155 readiness placeholders under specs/155-native-proof-capture/readiness/
- [X] T004 [P] Register Feature 155 test files in tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define the shared native proof-capture vocabulary before user stories.

- [X] T005 [P] Add Feature155 proof workflow transition tests in tests/SkiaViewer.Tests/Feature155ProofWorkflowTests.fs
- [X] T006 [P] Add Feature155 harness package rendering tests in tests/Rendering.Harness.Tests/Feature155ReadinessTests.fs
- [X] T007 Add Feature155 host/capable detection helpers and output renderers to tests/Rendering.Harness/Compositor.fsi
- [X] T008 Implement Feature155 host/capable detection helpers and output renderers in tests/Rendering.Harness/Compositor.fs

**Checkpoint**: Feature 155 can identify the current host capability and render accepted/limited package text without executing capture.

---

## Phase 3: User Story 1 - Capture Real Native Proof Attempts (Priority: P1) 🎯 MVP

**Goal**: Run proof capture on the capable host and produce three accepted current-run attempts.

**Independent Test**: `compositor-live-proof --feature 155 --attempt-count 3` writes three selected accepted attempts and a proof-set summary.

### Tests for User Story 1

- [X] T009 [P] [US1] Add capable-host capture acceptance tests in tests/Rendering.Harness.Tests/Feature155ReadinessTests.fs
- [X] T010 [P] [US1] Add artifact quality rejection tests in tests/SkiaViewer.Tests/Feature155NativeCaptureTests.fs

### Implementation for User Story 1

- [X] T011 [US1] Add Feature155 scene evidence capture helper in tests/Rendering.Harness/Cli.fs
- [X] T012 [US1] Add Feature155 capable-host branch to compositor-live-proof routing in tests/Rendering.Harness/Cli.fs
- [X] T013 [US1] Write selected attempt directories, sentinel artifacts, damage artifacts, proof metadata, and proof-set summary in tests/Rendering.Harness/Cli.fs
- [X] T014 [US1] Generate current-run Feature155 capable-host readiness artifacts under specs/155-native-proof-capture/readiness/live-proof/attempts/

**Checkpoint**: User Story 1 is complete when the capable-host proof run accepts `3/3` selected attempts.

---

## Phase 4: User Story 2 - Interpret the Proof Workflow Effects (Priority: P1)

**Goal**: Keep native capture observable as state, messages, effects, and edge interpretation.

**Independent Test**: Transition tests prove effect order and failure messages; capable-host run proves interpreter effects become artifacts.

### Tests for User Story 2

- [X] T015 [P] [US2] Add proof workflow failure-path tests in tests/SkiaViewer.Tests/Feature155ProofWorkflowTests.fs

### Implementation for User Story 2

- [X] T016 [US2] Connect Feature155 capture diagnostics to existing CompositorProof init/update/effect vocabulary in tests/Rendering.Harness/Cli.fs
- [X] T017 [US2] Record interpreter diagnostics and failure reasons in Feature155 proof output in tests/Rendering.Harness/Compositor.fs

**Checkpoint**: User Story 2 is complete when transition tests and capable-host output both expose the proof workflow sequence and failure reasons.

---

## Phase 5: User Story 3 - Finish P7 Partial-Redraw Readiness (Priority: P1)

**Goal**: Publish accepted P7 partial-redraw correctness readiness for the current host when proof and same-profile parity pass.

**Independent Test**: `compositor-readiness --feature 155` reports accepted proof, accepted parity, selected attempts `3/3`, and accepted partial-redraw readiness for the current host profile.

### Tests for User Story 3

- [X] T018 [P] [US3] Add Feature155 readiness closeout tests in tests/Rendering.Harness.Tests/Feature155ReadinessTests.fs

### Implementation for User Story 3

- [X] T019 [US3] Add Feature155 routing to compositor-parity, compositor-timing, and compositor-readiness in tests/Rendering.Harness/Cli.fs
- [X] T020 [US3] Render Feature155 parity, timing, validation summary, compatibility, package, and regression reports in tests/Rendering.Harness/Compositor.fs
- [X] T021 [US3] Generate current-run Feature155 parity, timing, and final readiness artifacts under specs/155-native-proof-capture/readiness/

**Checkpoint**: User Story 3 is complete when P7 readiness is no longer environment-limited for the current capable host profile.

---

## Phase 6: User Story 4 - Preserve Safe Unsupported-Host Behavior (Priority: P2)

**Goal**: Keep unsupported-host validation fail-closed with zero accepted artifacts.

**Independent Test**: Running the live-proof command with display variables unset writes environment-limited unsupported-host output and does not alter accepted capable-host attempts.

### Tests for User Story 4

- [X] T022 [P] [US4] Add Feature155 unsupported-host regression tests in tests/Rendering.Harness.Tests/Feature155ReadinessTests.fs

### Implementation for User Story 4

- [X] T023 [US4] Extend Feature155 unsupported-host output routing in tests/Rendering.Harness/Cli.fs
- [X] T024 [US4] Generate Feature155 unsupported-host regression artifact under specs/155-native-proof-capture/readiness/live-proof/unsupported/

**Checkpoint**: Unsupported-host output remains non-accepting and separated from accepted capable-host evidence.

---

## Phase 7: User Story 5 - Publish a Reviewable P7 Closeout (Priority: P3)

**Goal**: Make the final closeout package reviewable and explicit about accepted correctness versus performance claims.

**Independent Test**: A reviewer can open validation-summary.md and find proof, parity, timing, fallback, compatibility, package, regression, and limitation status in one place.

### Tests for User Story 5

- [X] T025 [P] [US5] Add package/readiness compatibility tests in tests/Package.Tests/Feature155CompatibilityTests.fs

### Implementation for User Story 5

- [X] T026 [US5] Add Feature155 FSI/readiness transcript artifacts under specs/155-native-proof-capture/readiness/fsi/
- [X] T027 [US5] Update docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md with Feature155 P7 closeout status

**Checkpoint**: Feature 155 has one reviewable P7 closeout package and report status is current.

---

## Phase 8: Polish & Validation

**Purpose**: Validate implementation, mark tasks complete, and prepare merge readiness.

- [X] T028 Run dotnet build FS.GG.Rendering.slnx --no-restore
- [X] T029 Run dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature155 --no-build
- [X] T030 Run dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature155 --no-build
- [X] T031 Run dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature155 --no-build
- [X] T032 Run Feature155 quickstart capable-host, unsupported-host, parity, timing, and readiness commands from specs/155-native-proof-capture/quickstart.md
- [X] T033 Run dotnet test FS.GG.Rendering.slnx --no-restore
- [X] T034 Run git diff --check
- [X] T035 Mark all tasks complete in specs/155-native-proof-capture/tasks.md

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 Setup has no dependencies.
- Phase 2 Foundational depends on Phase 1 and blocks all user stories.
- US1, US2, and US3 are all P1 and should be completed in that order for one developer because US3 consumes US1 proof output.
- US4 depends on US1 routing but must remain independently testable.
- US5 depends on US1-US4 evidence.
- Polish depends on all selected stories.

### User Story Dependencies

- **US1**: Depends on foundational Feature155 constants and renderers.
- **US2**: Depends on proof workflow constants but not final readiness.
- **US3**: Depends on accepted proof output from US1.
- **US4**: Depends on Feature155 live-proof routing.
- **US5**: Depends on closeout evidence from US1-US4.

### Parallel Opportunities

- T003 and T004 can run in parallel.
- T005 and T006 can run in parallel.
- T009 and T010 can run in parallel.
- T018, T022, and T025 can be written in parallel once foundational renderers exist.

## Implementation Strategy

1. Complete setup and foundational renderers.
2. Implement MVP US1 and verify three accepted capable-host attempts.
3. Wire workflow diagnostics and final P7 readiness.
4. Re-run unsupported-host validation.
5. Publish closeout artifacts and report status.
6. Run focused and broad validation.
