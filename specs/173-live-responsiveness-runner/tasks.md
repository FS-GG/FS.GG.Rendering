# Tasks: Live Responsiveness Runner

**Input**: Design documents from `/specs/173-live-responsiveness-runner/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Included because the feature specification and plan require automated evidence before implementation, plus visible-session validation for accepted readiness.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested as an independently reviewable increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User story label for story phases only.
- Every task names the exact file path or artifact path it changes or produces.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare feature evidence locations and test project compile slots before feature implementation.

- [X] T001 Create feature readiness placeholders under `specs/173-live-responsiveness-runner/readiness/responsiveness/.gitkeep`, `specs/173-live-responsiveness-runner/readiness/logs/.gitkeep`, `specs/173-live-responsiveness-runner/readiness/visual-preferred/.gitkeep`, and `specs/173-live-responsiveness-runner/readiness/visual-minimum/.gitkeep`
- [X] T002 [P] Add Feature 173 sample test compile entries in `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` for `Feature173LiveResponsivenessFixtures.fs`, `Feature173LiveResponsivenessWorkflowTests.fs`, `Feature173LiveResponsivenessCliTests.fs`, `Feature173LiveResponsivenessArtifactTests.fs`, `Feature173LiveResponsivenessBudgetTests.fs`, `Feature173LiveResponsivenessFailClosedTests.fs`, `Feature173LiveResponsivenessCoverageTests.fs`, and `Feature173LiveResponsivenessRegressionTests.fs`
- [X] T003 [P] Add the Feature 173 viewer test compile entry in `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` for `Feature173ReadinessRulesTests.fs`
- [X] T004 [P] Create the Tier 1 compatibility note skeleton in `specs/173-live-responsiveness-runner/readiness/compatibility.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish public contracts, pure evidence model, workflow boundary, and shared readiness rules that all user stories need.

**Critical**: No user story implementation should begin until this phase is complete.

- [X] T005 [P] Draft the additive viewer responsiveness public surface in `src/SkiaViewer/SkiaViewer.fsi` for rejected readiness, input-to-visible max budget, and stable readiness tokens required by `specs/173-live-responsiveness-runner/contracts/readiness-rules.md`
- [X] T006 [P] Draft the live responsiveness evidence public surface in `samples/SecondAntShowcase/SecondAntShowcase.Core/Evidence.fsi` for live review sessions, measured records, drag continuity evidence, coverage summary fields, artifact write status, and validation caveats
- [X] T007 [P] Draft the pure live responsiveness workflow public surface with `Model`, `Msg`, `Effect`, `init`, `update`, and interpreter contract in `samples/SecondAntShowcase/SecondAntShowcase.Core/ResponsivenessWorkflow.fsi`
- [X] T008 Add `ResponsivenessWorkflow.fsi` and `ResponsivenessWorkflow.fs` compile entries before `Evidence.fsi` in `samples/SecondAntShowcase/SecondAntShowcase.Core/SecondAntShowcase.Core.fsproj`
- [X] T009 [P] Add the FSI authoring transcript for `SkiaViewer.fsi`, `Evidence.fsi`, and `ResponsivenessWorkflow.fsi` additions in `specs/173-live-responsiveness-runner/readiness/fsi/live-responsiveness-runner.fsx`
- [X] T010 [P] Add failing semantic tests for viewer readiness tokens, max-latency budget rejection, first failed budget ordering, and slowest interaction reporting in `tests/SkiaViewer.Tests/Feature173ReadinessRulesTests.fs`
- [X] T011 [P] Add failing sample evidence model tests for record tokens, drag continuity tokens, display-only exclusions, path containment fields, and summary Markdown requirements in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessArtifactTests.fs`
- [X] T012 [P] Create shared Feature 173 test fixture helpers for temporary run directories, JSON loading, JSONL loading, fake measured records, and caveat assertions with `Synthetic` test names and `// SYNTHETIC:` source comments in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessFixtures.fs`
- [X] T013 [P] Add failing pure workflow transition tests and filesystem/native-edge interpreter tests in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessWorkflowTests.fs`
- [X] T014 Implement the viewer readiness and budget behavior from T005/T010 in `src/SkiaViewer/SkiaViewer.fs`
- [X] T015 Implement live responsiveness evidence helpers, summary Markdown helpers, drag continuity helpers, path containment helpers, and artifact status helpers in `samples/SecondAntShowcase/SecondAntShowcase.Core/Evidence.fs`
- [X] T016 Implement the pure workflow model, messages, effects, `init`, `update`, and interpreter boundary from T007/T013 in `samples/SecondAntShowcase/SecondAntShowcase.Core/ResponsivenessWorkflow.fs`
- [X] T017 Regenerate the reviewed Core hash baseline in `specs/171-second-antshowcase-sample/readiness/surface-baselines/SecondAntShowcase.Core.txt` and create the Feature 173 surface review note in `specs/173-live-responsiveness-runner/readiness/surface-baselines/SecondAntShowcase.Core.md`

**Checkpoint**: Shared contracts, pure workflow boundary, and readiness helpers are ready for story implementation.

---

## Phase 3: User Story 1 - Accept Live Mouse Responsiveness (Priority: P1) MVP

**Goal**: A maintainer can run the all-interactive responsiveness review in a visible desktop session and receive accepted or rejected readiness based only on measured live input-to-visible evidence.

**Independent Test**: In a visible desktop session, run the all-interactive live responsiveness review for one theme and confirm measured records for every interactive family, a run summary, and an accepted or rejected readiness decision based on visible timing.

### Tests for User Story 1

- [X] T018 [P] [US1] Add failing CLI parser and JSON pointer tests for `--require-live`, `--all-interactive`, `--theme`, `--out`, `--json`, accepted exit code `0`, and rejected exit code `5` in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessCliTests.fs`
- [X] T019 [P] [US1] Add failing artifact shape tests for measured `records.jsonl`, `summary.json`, `summary.md`, `environment.md`, relative `recordsPath`, run-directory path containment, and accepted measured fields in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessArtifactTests.fs`
- [X] T020 [P] [US1] Add failing budget aggregation tests for p95 at or below 100 ms, max at or below 150 ms, first failed budget, and five slowest interactions in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessBudgetTests.fs`

### Implementation for User Story 1

- [X] T021 [US1] Extend request parsing, scope validation, run id generation, JSON pointer output, and exit-code mapping in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T022 [US1] Add or adapt the live presentation timing hook needed by the sample runner in `src/SkiaViewer/Host/OpenGl.fsi` and `src/SkiaViewer/Host/OpenGl.fs`
- [X] T023 [US1] Wire live input receipt, presentation boundary, frame id, phase timing, and dirty-region facts into viewer latency records in `src/SkiaViewer/SkiaViewer.fs`
- [X] T024 [US1] Implement target resolution for representative live actions in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T025 [US1] Implement visible window focus checks and native mouse or keyboard input generation in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T026 [US1] Implement observed visible-result verification for representative actions in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T027 [US1] Write live measured `records.jsonl`, `summary.json`, `summary.md`, and `environment.md` artifacts under `<out>/<run-id>/` with run-directory containment enforcement in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T028 [US1] Implement accepted versus rejected live readiness classification using measured records, 95 percent at 100 ms, 150 ms max, and slowest-interaction reporting in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T029 [US1] Update the responsiveness usage text and live exit-code documentation in `samples/SecondAntShowcase/README.md`

**Checkpoint**: User Story 1 is independently testable with one visible-session theme run.

---

## Phase 4: User Story 2 - Fail Closed When Live Evidence Is Not Available (Priority: P2)

**Goal**: The responsiveness review remains non-accepted and diagnostic when the visible session, timing boundary, target resolution, or artifact write path cannot support accepted live evidence.

**Independent Test**: Run the responsiveness review without a measurable visible desktop presentation and confirm diagnostic artifacts are written, the exit code is non-accepted, and all caveats remain explicit.

### Tests for User Story 2

- [X] T030 [P] [US2] Add failing fail-closed tests for no visible surface, hidden or unfocusable window, missing presentation boundary, unreliable timestamps, timeout, and substitute-only evidence in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessFailClosedTests.fs`
- [X] T031 [US2] Add failing CLI exit-code tests for invalid request `2`, artifact failure `3`, live unavailable `4`, and measured rejection `5` in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessCliTests.fs`
- [X] T032 [P] [US2] Add failing artifact write failure tests for incomplete `records.jsonl`, `summary.json`, `summary.md`, `environment.md`, and any artifact path outside the run directory in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessArtifactTests.fs`

### Implementation for User Story 2

- [X] T033 [US2] Implement visible-session prerequisite detection and environment limitation tokens in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T034 [US2] Map missing presentation boundaries, low-precision timestamps, non-monotonic timestamps, and timeouts to non-accepted records in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T035 [US2] Implement complete-write enforcement, path containment enforcement, and write-failure readiness mapping for `records.jsonl`, `summary.json`, `summary.md`, and `environment.md` in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T036 [US2] Keep deterministic `ControlsElmish.Perf.runScript` substitute output visibly non-accepted in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T037 [US2] Add actionable missing-prerequisite and artifact-write diagnostics to `environment.md` generation in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`

**Checkpoint**: User Story 2 is independently testable in a headless or non-presenting environment.

---

## Phase 5: User Story 3 - Preserve Interaction Quality Across The Showcase (Priority: P3)

**Goal**: The live responsiveness fix measures the representative interactions reviewers care about while preserving existing showcase coverage, navigation, overlays, value-changing controls, and visual readiness behavior.

**Independent Test**: Run live responsiveness evidence, deterministic interaction regressions, coverage checks, and visual-readiness checks; verify previous accepted behavior remains intact or any blocked check is explicitly disclosed.

### Tests for User Story 3

- [X] T038 [P] [US3] Add failing all-interactive coverage tests for every `InteractionContracts.all` family, missing-family diagnostics, and display-only exclusions in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessCoverageTests.fs`
- [X] T039 [P] [US3] Add failing regression tests for navigation, disclosure, slider/rating, value-changing drags, and existing visual-readiness behavior in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessRegressionTests.fs`
- [X] T040 [US3] Add failing drag continuity budget tests for continuous feedback, delayed catch-up, insufficient samples, and missing boundary classifications in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessBudgetTests.fs`

### Implementation for User Story 3

- [X] T041 [US3] Resolve all timed representative actions from `InteractionContracts.all` at runtime without duplicating family literals in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T042 [US3] Preserve display-only exclusions from `InteractionContracts.displayOnlyReasons` with reasons and excluded status in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T043 [US3] Implement missing or ambiguous visible-control diagnostics in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T044 [US3] Implement drag continuity sampling and classification for slider/rating/value-changing interactions in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T045 [US3] Preserve responsiveness CLI dispatch in `samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs` and verify coverage, slider/rating, navigation, overlay, visual-readiness, and review-findings behavior through tests in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessRegressionTests.fs`

**Checkpoint**: All user stories are independently functional and regression-covered.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Package-consuming validation, documentation, readiness evidence, and final disclosure.

- [X] T046 [P] Update Feature 173 quick validation notes with final command outcomes in `specs/173-live-responsiveness-runner/readiness/compatibility.md`
- [X] T047 [P] Update the sample usage and caveat documentation for live responsiveness in `samples/SecondAntShowcase/README.md`
- [X] T048 Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release` and record the result in `specs/173-live-responsiveness-runner/readiness/logs/skia-viewer-tests.log`
- [X] T049 Run `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release` and record the result in `specs/173-live-responsiveness-runner/readiness/logs/elmish-tests.log`
- [X] T050 Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release` and record the result in `specs/173-live-responsiveness-runner/readiness/logs/controls-tests.log`
- [X] T051 Run `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase` and record the result in `specs/173-live-responsiveness-runner/readiness/logs/package-refresh.log`
- [X] T052 Run `dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore` and record the result in `specs/173-live-responsiveness-runner/readiness/logs/second-antshowcase-tests.log`
- [X] T053 Run the headless fail-closed responsiveness command from `specs/173-live-responsiveness-runner/quickstart.md` and preserve its non-accepted artifacts under `specs/173-live-responsiveness-runner/readiness/responsiveness/headless-fail-closed/`
- [X] T054 Run the visible light-theme live responsiveness command from `specs/173-live-responsiveness-runner/quickstart.md` and preserve accepted or rejected artifacts under `specs/173-live-responsiveness-runner/readiness/responsiveness/`
- [X] T055 Run the visible dark-theme live responsiveness command from `specs/173-live-responsiveness-runner/quickstart.md` and preserve accepted or rejected artifacts under `specs/173-live-responsiveness-runner/readiness/responsiveness/`
- [X] T056 Run coverage, deterministic evidence, visual-readiness, and review-findings commands from `specs/173-live-responsiveness-runner/quickstart.md` and preserve outputs under `specs/173-live-responsiveness-runner/readiness/`
- [X] T057 Create the final validation package with commands, exit codes, live run directories, failed budgets, five slowest interactions, caveats, and readiness decision in `specs/173-live-responsiveness-runner/readiness/validation-summary.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational and is the MVP acceptance path.
- **User Story 2 (Phase 4)**: Depends on Foundational; can be implemented after or alongside US1 with coordination because both touch `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`.
- **User Story 3 (Phase 5)**: Depends on Foundational; validates coverage and interaction quality after the runner model exists.
- **Polish (Phase 6)**: Depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: MVP. Provides accepted/rejected live measured evidence.
- **US2 (P2)**: Depends on the shared runner request/artifact model from Foundation; no acceptance on substitute evidence.
- **US3 (P3)**: Depends on the shared interaction contracts and runner action resolution; preserves showcase behavior.

### Within Each User Story

- Write tests first and confirm they fail for the missing behavior.
- Update `.fsi` public surfaces before `.fs` implementation when a public surface changes.
- Exercise public `.fsi` additions through the FSI transcript before `.fs` implementation.
- Implement pure evidence/readiness/workflow helpers before the CLI edge.
- Complete artifact writing before final readiness classification.
- Complete each story checkpoint before moving to lower-priority work.

---

## Parallel Opportunities

- Setup tasks T002, T003, and T004 can run in parallel after T001.
- Foundational FSI tasks T005, T006, and T007 can run in parallel; T009-T013 can run in parallel after their surfaces exist; implementation tasks T014-T016 follow the FSI transcript and failing tests.
- US1 test tasks T018, T019, and T020 can run in parallel.
- US2 test tasks T030 and T032 can run in parallel; T031 shares `Feature173LiveResponsivenessCliTests.fs` with US1 and should be serialized with T018.
- US3 test tasks T038 and T039 can run in parallel; T040 shares `Feature173LiveResponsivenessBudgetTests.fs` with US1 and should be serialized with T020.
- Final validation commands T048, T049, and T050 can run in parallel on machines with enough capacity.

---

## Parallel Example: User Story 1

```text
Task: "T018 [P] [US1] Add failing CLI parser and JSON pointer tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessCliTests.fs"
Task: "T019 [P] [US1] Add failing artifact shape tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessArtifactTests.fs"
Task: "T020 [P] [US1] Add failing budget aggregation tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessBudgetTests.fs"
```

## Parallel Example: User Story 2

```text
Task: "T030 [P] [US2] Add failing fail-closed tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessFailClosedTests.fs"
Task: "T032 [P] [US2] Add failing artifact write failure tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessArtifactTests.fs"
```

## Parallel Example: User Story 3

```text
Task: "T038 [P] [US3] Add failing all-interactive coverage tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessCoverageTests.fs"
Task: "T039 [P] [US3] Add failing regression tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature173LiveResponsivenessRegressionTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 for the live measured acceptance path.
3. Stop and validate US1 with one visible-session theme run.
4. Preserve non-accepted output when the run is rejected by timing budgets.

### Incremental Delivery

1. Deliver US1 so maintainers can collect measured live evidence.
2. Add US2 so blocked or environment-limited runs fail closed.
3. Add US3 so coverage, drag continuity, and existing showcase behavior remain protected.
4. Run Phase 6 validation and publish `validation-summary.md`.

### Validation Commands

```sh
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase
dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore
```

---

## Notes

- Accepted readiness must use measured live records only.
- Substitute, headless, skipped, timed-out, blocked, environment-limited, degraded, and manual-review-pending evidence must stay visible and non-green.
- Synthetic test fixtures must use `Synthetic` in test names and `// SYNTHETIC:` source comments with the reason.
- Display-only exclusions are explicit exclusions with reasons, not timed failures.
- Any public framework or sample surface addition requires `.fsi`, FSI transcript coverage, tests, implementation, and reviewed baseline updates.
