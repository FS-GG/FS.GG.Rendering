---
description: "Task list for Input/Render Responsiveness"
---

# Tasks: Input/Render Responsiveness

**Input**: Design documents from `/specs/167-input-render-responsiveness/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Required. The feature specification mandates independent tests, latency evidence, compatibility checks, and readiness artifacts.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks in the same phase because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User story label. Setup, foundational, and polish tasks do not include a story label.
- Every task includes the exact target path or evidence path.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the feature evidence scaffold and reserve compile slots for focused tests.

- [X] T001 Create the feature readiness scaffold in specs/167-input-render-responsiveness/readiness/.gitkeep and specs/167-input-render-responsiveness/readiness/responsiveness/.gitkeep
- [X] T002 [P] Create the FSI contract evidence ledger in specs/167-input-render-responsiveness/readiness/fsi-contract-transcript.md
- [X] T003 [P] Create the compatibility evidence ledger in specs/167-input-render-responsiveness/readiness/compatibility.md
- [X] T004 [P] Create the scheduler test evidence ledger in specs/167-input-render-responsiveness/readiness/scheduler-tests.md
- [X] T005 [P] Create the synthetic evidence disclosure ledger in specs/167-input-render-responsiveness/readiness/synthetic-evidence.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the public additive contracts and shared scheduler vocabulary before any user story implementation.

**Critical**: No user story implementation should begin until the FSI surface and test fixture scaffolding are in place.

- [X] T006 Draft additive responsiveness diagnostics, latency record, budget, and run option signatures in src/SkiaViewer/SkiaViewer.fsi
- [X] T007 Draft additive OpenGL host receipt, wake/signal, presentation timing, and environment status signatures in src/SkiaViewer/Host/OpenGl.fsi
- [X] T008 Draft additive Controls.Elmish timing contribution and diagnostics-disabled compatibility signatures in src/Controls.Elmish/ControlsElmish.fsi
- [X] T009 Exercise the drafted public signatures through F# Interactive and record the transcript in specs/167-input-render-responsiveness/readiness/fsi-contract-transcript.md
- [X] T010 Update intentional public-surface baselines in tests/surface-baselines/FS.GG.UI.SkiaViewer.txt and tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt
- [X] T011 [P] Add shared scheduler test fixtures and register them in tests/SkiaViewer.Tests/Feature167SchedulerFixtures.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T012 [P] Add shared responsiveness metrics test fixtures and register them in tests/Elmish.Tests/Feature167ResponsivenessFixtures.fs and tests/Elmish.Tests/Elmish.Tests.fsproj
- [X] T013 [P] Add AntShowcase responsiveness test fixtures and register them in samples/AntShowcase/AntShowcase.Tests/Feature167ResponsivenessFixtures.fs and samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj
- [X] T014 Create implementation-local scheduler model, message, effect, input envelope, queue, dirty state, and frame-drain skeletons in src/SkiaViewer/SkiaViewer.fs

**Checkpoint**: Foundation ready. User story tests can now be written against the planned public signatures and implementation contracts.

---

## Phase 3: User Story 1 - See End-to-End Interaction Latency (Priority: P1) MVP

**Goal**: A maintainer can enable responsiveness diagnostics and receive correlated latency records for pointer and keyboard activations from receipt through visible response.

**Independent Test**: Run a diagnostic replay with one pointer activation and one keyboard activation against a representative screen; each activation produces a record with total visible-response time, queue state, phase breakdown, and state-changed status.

### Tests for User Story 1

Write these tests first and verify they fail before implementation.

- [X] T015 [P] [US1] Add failing pointer and keyboard latency record shape tests in tests/Elmish.Tests/Feature167ResponsivenessMetricsTests.fs and tests/Elmish.Tests/Elmish.Tests.fsproj
- [X] T016 [P] [US1] Add failing JSONL required-field, stable-token, and failure-stage contract tests in tests/SkiaViewer.Tests/Feature167LatencyRecordTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T017 [P] [US1] Add failing AntShowcase representative pointer/key script shape tests in samples/AntShowcase/AntShowcase.Tests/Feature167ResponsivenessShapeTests.fs and samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj

### Implementation for User Story 1

- [X] T018 [US1] Implement public latency record, phase timing, visible response, environment status, and budget types in src/SkiaViewer/SkiaViewer.fs
- [X] T019 [US1] Implement JSONL latency record writing plus queued-processing, update, recomposition, paint, presentation, and diagnostic write-failure reporting in src/SkiaViewer/SkiaViewer.fs
- [X] T020 [US1] Thread responsiveness options and record sinks through viewer launch functions in src/SkiaViewer/Host/Viewer.fs and src/SkiaViewer/SkiaViewer.fs
- [X] T021 [US1] Capture native receipt timestamps, callback duration, and presentation boundary facts in src/SkiaViewer/Host/OpenGl.fs
- [X] T022 [US1] Contribute routing, update, retained step, layout, text, and no-visible-response timing facts from src/Controls.Elmish/ControlsElmish.fs
- [X] T023 [US1] Add AntShowcase representative pointer and keyboard activation script definitions in samples/AntShowcase/AntShowcase.Core/Scripts.fs
- [X] T024 [US1] Record US1 test commands, diagnostic sample output, and any missing-boundary caveats in specs/167-input-render-responsiveness/readiness/scheduler-tests.md

**Checkpoint**: User Story 1 is independently testable and provides the MVP diagnostics surface.

---

## Phase 4: User Story 2 - Keep Input Receipt Short (Priority: P1)

**Goal**: Native pointer and keyboard callbacks enqueue timestamped work, signal processing, and return without doing retained scene recomposition or presentation work.

**Independent Test**: Run a burst replay with continuous movement, pointer clicks, and keyboard activations; receipt durations remain short, discrete order is preserved, continuous movement is coalesced, and dirty frames recompose at most once before presentation.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T025 [P] [US2] Add failing input queue ordering and continuous-move coalescing tests in tests/SkiaViewer.Tests/Feature167InputQueueTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T026 [US2] Add failing frame-drain, dirty-state, and at-most-one-recomposition tests in tests/SkiaViewer.Tests/Feature167SchedulerDrainTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T027 [US2] Add failing native receipt callback no-render and receipt-duration classification tests in tests/SkiaViewer.Tests/Feature167ReceiptCallbackTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T028 [P] [US2] Add failing multi-message folding, no-state-change input, and deterministic Perf.runScript compatibility tests in tests/Elmish.Tests/Feature167InteractionSemanticsTests.fs and tests/Elmish.Tests/Elmish.Tests.fsproj

### Implementation for User Story 2

- [X] T029 [US2] Implement monotonic sequence assignment and enqueue policy for discrete and continuous inputs in src/SkiaViewer/SkiaViewer.fs
- [X] T030 [US2] Replace immediate pointer/key processing with enqueue-and-signal behavior in src/SkiaViewer/Host/OpenGl.fs
- [X] T031 [US2] Implement frame/update loop drain batches, lifecycle priority, resize handling, and dirty-state recomposition policy in src/SkiaViewer/SkiaViewer.fs
- [X] T032 [US2] Fold all product messages from one input before requesting retained scene recomposition in src/Controls.Elmish/ControlsElmish.fs
- [X] T033 [US2] Record queue depth, coalesced movement count, no-visible-response reason, and long-frame facts in src/SkiaViewer/SkiaViewer.fs
- [X] T034 [US2] Preserve screenshot/readback and close behavior while queued input is pending in src/SkiaViewer/Host/OpenGl.fs
- [X] T035 [US2] Document burst replay evidence and any Synthetic test disclosure in specs/167-input-render-responsiveness/readiness/scheduler-tests.md and specs/167-input-render-responsiveness/readiness/synthetic-evidence.md

**Checkpoint**: User Story 2 is independently testable and proves input receipt no longer performs heavyweight retained rendering.

---

## Phase 5: User Story 3 - Validate Responsiveness Budgets (Priority: P2)

**Goal**: A sample owner can run a documented responsiveness check and receive p50, p95, max latency, long-frame counts, first failed budget, and readiness status by scope.

**Independent Test**: Run the responsiveness check against passing and deliberately slow fixtures; the passing fixture is accepted, the slow fixture is blocked, and the report names the first failed budget plus the slowest interactions.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T036 [P] [US3] Add failing responsiveness summary percentile and budget tests in tests/Elmish.Tests/Feature167ResponsivenessSummaryTests.fs and tests/Elmish.Tests/Elmish.Tests.fsproj
- [X] T037 [P] [US3] Add failing summary.json and summary.md agreement tests in tests/SkiaViewer.Tests/Feature167ResponsivenessSummaryTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T038 [P] [US3] Add failing validation-lane summary reader tests in tests/Rendering.Harness.Tests/Feature167ResponsivenessReadinessTests.fs and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T039 [P] [US3] Add failing AntShowcase CLI responsiveness exit-code tests in samples/AntShowcase/AntShowcase.Tests/Feature167ResponsivenessCliTests.fs and samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj

### Implementation for User Story 3

- [X] T040 [US3] Implement responsiveness summary aggregation, percentile calculation, readiness tokens, and first failed budget selection in src/SkiaViewer/SkiaViewer.fs
- [X] T041 [US3] Implement summary.json, summary.md, records.jsonl, and environment.md output generation in src/SkiaViewer/SkiaViewer.fs
- [X] T042 [US3] Add validation-lane parsing for responsiveness summary.json in tests/Rendering.Harness/ValidationLanes.fsi and tests/Rendering.Harness/ValidationLanes.fs
- [X] T043 [US3] Add AntShowcase responsiveness command-line parsing for page, theme, script, out, require-live, and json flags in samples/AntShowcase/AntShowcase.App/Program.fs
- [X] T044 [US3] Implement AntShowcase responsiveness run orchestration and output path handling in samples/AntShowcase/AntShowcase.App/Responsiveness.fs
- [X] T045 [US3] Implement environment-limited and unwritable-output exit code handling in samples/AntShowcase/AntShowcase.App/Program.fs
- [X] T046 [US3] Capture representative responsiveness outputs under specs/167-input-render-responsiveness/readiness/responsiveness/<run-id>/summary.md, specs/167-input-render-responsiveness/readiness/responsiveness/<run-id>/summary.json, specs/167-input-render-responsiveness/readiness/responsiveness/<run-id>/records.jsonl, and specs/167-input-render-responsiveness/readiness/responsiveness/<run-id>/environment.md

**Checkpoint**: User Story 3 is independently testable and produces reviewer-readable plus machine-readable readiness evidence.

---

## Phase 6: User Story 4 - Preserve Existing Interaction Semantics (Priority: P3)

**Goal**: Existing pointer, focus, keyboard, and product update behavior remains compatible while diagnostics and scheduling are enabled or disabled.

**Independent Test**: Run existing interaction tests and focused keyboard baselines after the scheduler change; pointer activation, focus routing, key-down Enter/Space activation, key-up behavior, and diagnostics-disabled behavior match the prior outcomes.

### Tests for User Story 4

Write these tests first and verify they fail before implementation where the compatibility guard is new.

- [X] T047 [P] [US4] Add focused diagnostics-disabled compatibility tests in tests/Elmish.Tests/Feature167DiagnosticsDisabledTests.fs and tests/Elmish.Tests/Elmish.Tests.fsproj
- [X] T048 [P] [US4] Add focused keyboard Enter/Space key-down and key-up compatibility tests in samples/AntShowcase/AntShowcase.Tests/Feature167KeyboardCompatibilityTests.fs and samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj
- [X] T049 [P] [US4] Add ordered discrete input final-state parity tests in tests/SkiaViewer.Tests/Feature167InteractionParityTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj

### Implementation for User Story 4

- [X] T050 [US4] Preserve diagnostics-disabled fast path, existing frame metrics behavior, and clock-free Perf.runScript outputs in src/Controls.Elmish/ControlsElmish.fs
- [X] T051 [US4] Preserve pointer activation, authored binding fallback, focus routing, and ordered discrete final-state behavior in src/Controls.Elmish/ControlsElmish.fs
- [X] T052 [US4] Preserve AntShowcase key-down Enter/Space activation and key-up non-activation behavior in samples/AntShowcase/AntShowcase.Core/Host.fs
- [X] T053 [US4] Run existing interaction compatibility commands and record outcomes in specs/167-input-render-responsiveness/readiness/compatibility.md

**Checkpoint**: User Story 4 proves behavior compatibility after the scheduler and diagnostics changes.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation, package surface evidence, and readiness closure.

- [X] T054 [P] Update SkiaViewer package documentation for responsiveness diagnostics in src/SkiaViewer/README.md
- [X] T055 [P] Update Controls.Elmish package documentation for timing contributions and deterministic Perf.runScript compatibility in src/Controls.Elmish/README.md
- [X] T056 [P] Update AntShowcase documentation for the responsiveness command in samples/AntShowcase/README.md
- [X] T057 Run focused SkiaViewer Feature167 tests and record results in specs/167-input-render-responsiveness/readiness/scheduler-tests.md
- [X] T058 Run focused Elmish Feature167 tests, including deterministic Perf.runScript compatibility, and record results in specs/167-input-render-responsiveness/readiness/scheduler-tests.md
- [X] T059 Run Controls and KeyboardInput compatibility tests and record results in specs/167-input-render-responsiveness/readiness/compatibility.md
- [X] T060 Run AntShowcase interaction and responsiveness tests and record results in specs/167-input-render-responsiveness/readiness/compatibility.md
- [X] T061 Run scripts/run-validation-lanes.fsx for responsiveness summary validation and record results in specs/167-input-render-responsiveness/readiness/scheduler-tests.md
- [X] T062 Run package surface and local package validation with dotnet restore FS.GG.Rendering.slnx, dotnet build, dotnet fsi scripts/refresh-surface-baselines.fsx, git diff over tests/surface-baselines/, and dotnet pack FS.GG.Rendering.slnx, then record results in specs/167-input-render-responsiveness/readiness/compatibility.md
- [X] T063 Review all Synthetic test names and comments, then finalize specs/167-input-render-responsiveness/readiness/synthetic-evidence.md
- [X] T064 Finalize readiness entry point linking scheduler, compatibility, FSI, synthetic, and responsiveness outputs in specs/167-input-render-responsiveness/readiness/README.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup. Blocks all user stories because the feature is Tier 1 and must define `.fsi` surfaces before implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational. This is the MVP diagnostics surface.
- **User Story 2 (Phase 4)**: Depends on Foundational. It can be developed after or alongside US1, but final evidence should include both P1 stories.
- **User Story 3 (Phase 5)**: Depends on US1 records and benefits from US2 scheduling facts.
- **User Story 4 (Phase 6)**: Depends on US2 scheduling behavior and can run once compatibility guards are added.
- **Polish (Phase 7)**: Depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2. No dependency on other user stories.
- **US2 (P1)**: Can start after Phase 2. No dependency on US1 for queue behavior, but final records should use the US1 diagnostics vocabulary.
- **US3 (P2)**: Depends on US1 latency records and summary-compatible outputs; uses US2 long-frame and queue facts.
- **US4 (P3)**: Depends on the implemented scheduling path from US2.

### Within Each User Story

- Tests must be written and observed failing before implementation.
- Public `.fsi` contracts precede `.fs` implementation.
- Models and shared fixtures precede services, sinks, and host integration.
- Host callback changes precede live AntShowcase evidence.
- Evidence is recorded after the relevant tests and commands run.

### Parallel Opportunities

- Setup ledger creation tasks T002 through T005 can run in parallel.
- Foundation fixture tasks T011 through T013 can run in parallel after FSI drafts are ready.
- US1 test tasks T015 through T017 can run in parallel.
- US2 test tasks T025 and T028 can run in parallel; T026 and T027 both update tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj and should be sequenced with T025.
- US3 test tasks T036 through T039 can run in parallel.
- US4 test tasks T047 through T049 can run in parallel.
- Documentation tasks T054 through T056 can run in parallel.

---

## Parallel Example: User Story 1

```bash
# Launch US1 test authoring tasks together:
Task: "T015 Add failing pointer and keyboard latency record shape tests in tests/Elmish.Tests/Feature167ResponsivenessMetricsTests.fs and tests/Elmish.Tests/Elmish.Tests.fsproj"
Task: "T016 Add failing JSONL required-field, stable-token, and failure-stage contract tests in tests/SkiaViewer.Tests/Feature167LatencyRecordTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj"
Task: "T017 Add failing AntShowcase representative pointer/key script shape tests in samples/AntShowcase/AntShowcase.Tests/Feature167ResponsivenessShapeTests.fs and samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj"
```

## Parallel Example: User Story 2

```bash
# Launch US2 scheduler tests together:
Task: "T025 Add failing input queue ordering and continuous-move coalescing tests in tests/SkiaViewer.Tests/Feature167InputQueueTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj"
Task: "T028 Add failing multi-message folding, no-state-change input, and deterministic Perf.runScript compatibility tests in tests/Elmish.Tests/Feature167InteractionSemanticsTests.fs and tests/Elmish.Tests/Elmish.Tests.fsproj"
```

## Parallel Example: User Story 3

```bash
# Launch US3 report and CLI tests together:
Task: "T036 Add failing responsiveness summary percentile and budget tests in tests/Elmish.Tests/Feature167ResponsivenessSummaryTests.fs and tests/Elmish.Tests/Elmish.Tests.fsproj"
Task: "T037 Add failing summary.json and summary.md agreement tests in tests/SkiaViewer.Tests/Feature167ResponsivenessSummaryTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj"
Task: "T038 Add failing validation-lane summary reader tests in tests/Rendering.Harness.Tests/Feature167ResponsivenessReadinessTests.fs and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj"
Task: "T039 Add failing AntShowcase CLI responsiveness exit-code tests in samples/AntShowcase/AntShowcase.Tests/Feature167ResponsivenessCliTests.fs and samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 for US1.
3. Stop and validate US1 independently with focused Elmish, SkiaViewer, and AntShowcase diagnostic-shape tests.
4. Record the MVP evidence in specs/167-input-render-responsiveness/readiness/scheduler-tests.md.

### Incremental Delivery

1. Add US1 to expose end-to-end latency records.
2. Add US2 to move live receipt to queue-and-signal scheduling.
3. Add US3 to aggregate budget reports and AntShowcase CLI evidence.
4. Add US4 to lock compatibility behavior.
5. Finish Phase 7 to package documentation, surface baselines, and readiness proof.

### Validation Commands

Run these commands as the relevant tasks complete:

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release --no-restore --filter Feature167
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --no-restore --filter Feature167
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore --filter "Interaction|Pointer|Focus"
dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj -c Release --no-restore
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Interaction|Responsiveness|Feature167"
dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --out artifacts/validation-lanes
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx -c Release --no-restore
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff --stat tests/surface-baselines/
dotnet pack FS.GG.Rendering.slnx -c Release --no-build -o ~/.local/share/nuget-local
```

## Notes

- [P] tasks use different files or can be coordinated without waiting on an incomplete implementation task.
- Synthetic tests must include `Synthetic` in the test name and a comment explaining the real-evidence path or limitation.
- Environment-limited live runs must write explicit environment evidence and cannot be reported as accepted readiness.
- Do not mark slow or environment-limited responsiveness evidence as accepted unless the contracts' substitute-evidence requirements are met.
