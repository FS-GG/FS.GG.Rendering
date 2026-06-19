# Tasks: Validation Lane Runner

**Input**: Design documents from `/specs/166-validation-lane-runner/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Required by `spec.md` FR-016 and the constitution. Write the story-specific tests before implementation, exercise the draft `.fsi` surface through F# Interactive or a prelude transcript before `.fs` implementation, and confirm tests fail for the missing behavior.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested as an independently reviewable increment.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare feature-specific test and evidence files without changing public product runtime behavior.

- [X] T001 Create compile-safe placeholder modules for `Feature166TestFixtures.fs`, `Feature166LaneCatalogTests.fs`, `Feature166LaneRunnerPreflightTests.fs`, `Feature166LaneStatusTests.fs`, `Feature166ValidationSummaryTests.fs`, `Feature166CancellationTests.fs`, and `Feature166SchedulingTests.fs`, then add their compile entries in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`
- [X] T002 [P] Create shared Feature166 temporary filesystem and synthetic process helpers in `tests/Rendering.Harness.Tests/Feature166TestFixtures.fs`, with `SYNTHETIC:` comments, rationale, and real-evidence path notes where fixtures replace real validation work
- [X] T003 [P] Create the validation evidence directory placeholder in `specs/166-validation-lane-runner/readiness/lanes/.gitkeep`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the harness-visible contract and MVU/effect surface that all stories depend on.

**Critical**: No user story implementation should begin until this phase is complete.

- [X] T004 Draft the Feature166 public lane-runner contract in `tests/Rendering.Harness/ValidationLanes.fsi` with `ReadinessRole`, expanded `LaneStatus`, `RunRequest`, preflight diagnostics, lane evidence paths, run session fields, summary fields, and MVU messages/effects, then exercise the draft surface through F# Interactive or a prelude transcript recorded in `specs/166-validation-lane-runner/readiness/fsi-contract-transcript.md`
- [X] T005 Add pure request, readiness, status-token, and summary function signatures in `tests/Rendering.Harness/ValidationLanes.fsi` before editing `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T006 [P] Add shared Feature166 result builders and JSON/Markdown assertion helpers in `tests/Rendering.Harness.Tests/Feature166TestFixtures.fs`
- [X] T007 Add the Feature166 test modules to the Expecto aggregation path by verifying `tests/Rendering.Harness.Tests/Program.fs` still discovers all `[<Tests>]` values

**Checkpoint**: FSI contract is ready, transcript evidence exists, and tests can be written against the intended public surface.

---

## Phase 3: User Story 1 - Run Targeted Validation Lanes (Priority: P1) - MVP

**Goal**: A maintainer can list lanes, run one named lane, or run the required lane set and receive lane-level status, elapsed time, and evidence locations.

**Independent Test**: Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter Feature166LaneCatalog` and `dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --out artifacts/validation-lanes` to confirm only the selected lane runs and summaries name its evidence.

### Tests for User Story 1

- [X] T008 [P] [US1] Add catalog tests for required lane ids, optional aggregate role, explicit timeouts, unique lane ids, unique result ids, and isolated evidence paths in `tests/Rendering.Harness.Tests/Feature166LaneCatalogTests.fs`
- [X] T009 [P] [US1] Add request preflight tests for `--required`, repeated `--lane`, `--include-optional aggregate-solution`, unknown lane rejection, and duplicate requested lane rejection in `tests/Rendering.Harness.Tests/Feature166LaneRunnerPreflightTests.fs`
- [X] T010 [US1] Add CLI argument tests for `--list`, default required mode, selected lane mode, optional aggregate inclusion, `--out`, `--run-id`, `--replace-run`, and `--json` in `tests/Rendering.Harness.Tests/Feature166LaneRunnerPreflightTests.fs`

### Implementation for User Story 1

- [X] T011 [US1] Implement explicit `ReadinessRole`, stable lane ids, display names, timeout fields, evidence path fields, aggregate flags, and substitution metadata in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T012 [US1] Replace the Feature163-shaped default lane catalog with `build`, `library-tests`, `package-proof`, `controls`, `rendering-harness`, `antshowcase-sample`, and optional `aggregate-solution` definitions in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T013 [US1] Implement request selection and preflight validation for required lanes, explicit lanes, optional inclusion, unknown lanes, duplicate lane ids, duplicate result ids, run id uniqueness, and writable output roots in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T014 [US1] Wire `validation-lanes` CLI options `--list`, `--required`, repeatable `--lane`, repeatable `--include-optional`, `--out`, `--run-id`, `--replace-run`, and `--json` in `tests/Rendering.Harness/Cli.fs`
- [X] T015 [US1] Keep `scripts/run-validation-lanes.fsx` as the thin forwarder to `tests/Rendering.Harness/Rendering.Harness.fsproj` and verify it forwards all Feature166 options unchanged in `scripts/run-validation-lanes.fsx`
- [X] T016 [US1] Record US1 focused test and single-lane smoke output in `specs/166-validation-lane-runner/readiness/us1-targeted-lanes.md`

**Checkpoint**: User Story 1 is independently functional as the MVP.

---

## Phase 4: User Story 2 - Diagnose Hung or Failing Lanes (Priority: P2)

**Goal**: A contributor can distinguish passed, failed, timed-out, no-progress-timeout, canceled, skipped, not-run, environment-limited, and infrastructure-error lane outcomes with logs and reasons.

**Independent Test**: Run controlled synthetic lanes that pass, fail, exceed total timeout, stop producing output before no-progress timeout, hit evidence-write failures, and receive cancellation; verify each result is classified differently.

### Tests for User Story 2

- [X] T017 [P] [US2] Add status classification tests for pass, fail, total timeout, no-progress timeout, skipped, not-run, environment-limited, infrastructure error, and log/result write failures in `tests/Rendering.Harness.Tests/Feature166LaneStatusTests.fs`
- [X] T018 [US2] Add MVU transition tests for `PreflightPassed`, `LaneHeartbeatDue`, `LaneTimedOut`, `LaneNoProgressTimedOut`, `InfrastructureErrorRaised`, `OperatorCanceled`, and terminal summary requests in `tests/Rendering.Harness.Tests/Feature166LaneStatusTests.fs`
- [X] T019 [P] [US2] Add cancellation tests that preserve completed lane results and mark active, pending, or unstarted lanes as canceled, skipped, or not-run with reasons in `tests/Rendering.Harness.Tests/Feature166CancellationTests.fs`

### Implementation for User Story 2

- [X] T020 [US2] Expand lane result records with timeout budget, elapsed time, last activity timestamp/text, exit code, log path, result path, diagnostics path, reason, caveats, accepted environment limitation, and substitution fields in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T021 [US2] Implement child-process execution with redirected output, lane log appends, progress heartbeat tracking, total timeout enforcement, no-progress timeout enforcement, and process-tree termination in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T022 [US2] Implement infrastructure-error handling for process start failures, evidence directory creation failures, log write failures, result write failures, diagnostics write failures, and summary write failures in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T023 [US2] Extend the pure `update` function to emit explicit preflight, evidence creation, start process, append log, heartbeat, poll, stop process, lane result, cancellation, infrastructure error, and summary effects in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T024 [US2] Map CLI exit codes `0`, `1`, `2`, `3`, and `130` to ready, blocked/incomplete, preflight error, infrastructure error, and operator cancellation outcomes in `tests/Rendering.Harness/Cli.fs`
- [X] T025 [US2] Record US2 status classification and cancellation test output in `specs/166-validation-lane-runner/readiness/us2-diagnostics.md`

**Checkpoint**: User Story 2 is independently testable with synthetic diagnostic lanes and CLI exit-code behavior.

---

## Phase 5: User Story 3 - Preserve Reviewable Evidence (Priority: P3)

**Goal**: A reviewer can inspect compact Markdown and structured JSON summaries, then drill into separate per-lane logs and results without reading interleaved console output.

**Independent Test**: Run a mixed result set with required, optional, skipped, not-run, failed, timed-out, no-progress-timeout, canceled, environment-limited, infrastructure-error, and substitute lanes; verify `summary.md`, `summary.json`, and per-lane `result.json` records agree.

### Tests for User Story 3

- [X] T026 [P] [US3] Add summary agreement and SC-001 timing tests for Markdown and JSON fields including run id, overall readiness, first blocking required lane, aggregate status, substitutions, caveats, evidence paths, elapsed times, roles, and final summary emission within 10 seconds after the last lane completes in `tests/Rendering.Harness.Tests/Feature166ValidationSummaryTests.fs`
- [X] T027 [US3] Add run-id no-overwrite, replacement notice, per-lane evidence isolation, and separate optional/informational lane summary tests in `tests/Rendering.Harness.Tests/Feature166ValidationSummaryTests.fs`
- [X] T028 [US3] Add readiness-rule tests for required fail, timeout, no-progress timeout, cancellation, skipped or not-run without accepted limitation, environment-limited without accepted limitation, infrastructure error, optional aggregate failure, and incomplete aggregate substitution in `tests/Rendering.Harness.Tests/Feature166ValidationSummaryTests.fs`

### Implementation for User Story 3

- [X] T029 [US3] Implement run-id child directory creation, default `artifacts/validation-lanes/<run-id>` output, readiness `--out` support, no-overwrite protection, and explicit replacement notices in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T030 [US3] Write per-lane `log.txt`, `result.json`, `diagnostics.md`, and discovered artifacts under `<run-root>/<lane-id>/` in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T031 [US3] Render reviewer-facing `summary.md` with required lane table, optional/informational lane table, first blocking required lane, aggregate status, substitutions, caveats, and links to logs in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T032 [US3] Render `summary.json` and per-lane `result.json` with the contract shape from `specs/166-validation-lane-runner/contracts/validation-session-record.md` in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T033 [US3] Update validation documentation to describe lane-runner evidence, optional aggregate visibility, targeted substitute disclosure, and preserved direct validation commands in `docs/validation/validation-set.md`
- [X] T034 [US3] Record US3 mixed-summary and evidence-isolation test output in `specs/166-validation-lane-runner/readiness/us3-evidence.md`

**Checkpoint**: User Story 3 is independently reviewable from generated Markdown and JSON evidence.

---

## Phase 6: User Story 4 - Avoid Validation Output Races (Priority: P4)

**Goal**: The lane runner prevents unsafe concurrent writes when lanes share generated output locations.

**Independent Test**: Configure two lanes with the same concurrency group or output scope and confirm the runner serializes, isolates, or rejects the unsafe schedule before concurrent execution starts with an actionable error.

### Tests for User Story 4

- [X] T035 [P] [US4] Add schedule-safety tests for duplicate output scopes, shared concurrency groups, unsafe parallel requests, and actionable conflict diagnostics in `tests/Rendering.Harness.Tests/Feature166SchedulingTests.fs`
- [X] T036 [P] [US4] Add lane catalog tests proving all documented lanes declare concurrency group and output scope metadata in `tests/Rendering.Harness.Tests/Feature166LaneCatalogTests.fs`

### Implementation for User Story 4

- [X] T037 [US4] Add output scope and concurrency group schedule validation that serializes by default and rejects any requested unsafe parallel schedule in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T038 [US4] Add CLI diagnostics naming conflicting lanes, shared output scope or concurrency group, and the action needed to proceed in `tests/Rendering.Harness/Cli.fs`
- [X] T039 [US4] Document race-prevention behavior and direct-command caveats for lanes sharing build or test output locations in `docs/validation/validation-set.md`
- [X] T040 [US4] Record US4 schedule-safety test output in `specs/166-validation-lane-runner/readiness/us4-scheduling.md`

**Checkpoint**: User Story 4 prevents the known validation output race before lane execution.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, documentation alignment, and readiness evidence across the whole feature.

- [X] T041 [P] Update the feature quickstart with final option names, lane ids, exit codes, and evidence paths in `specs/166-validation-lane-runner/quickstart.md`
- [X] T042 [P] Update the retrospective follow-up status for the validation lane runner in `docs/reports/2026-06-19-00-24-framework-and-skills-retrospective.md`
- [X] T043 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter Feature166`, including the SC-001 final-summary timing assertions, and record output in `specs/166-validation-lane-runner/readiness/feature166-tests.md`
- [X] T044 Run `dotnet fsi scripts/run-validation-lanes.fsx --list` and record lane listing evidence in `specs/166-validation-lane-runner/readiness/lane-list.md`
- [X] T045 Run `dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --out specs/166-validation-lane-runner/readiness/lanes` and record single-lane evidence path in `specs/166-validation-lane-runner/readiness/single-lane.md`
- [X] T046 Run `dotnet fsi scripts/run-validation-lanes.fsx --required --out specs/166-validation-lane-runner/readiness/lanes` and record required-lane summary path in `specs/166-validation-lane-runner/readiness/required-lanes.md`
- [X] T047 Run direct validation commands for `tests/Controls.Tests/Controls.Tests.fsproj`, `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`, and `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`, then record preservation evidence in `specs/166-validation-lane-runner/readiness/direct-validation.md`
- [X] T048 Review `tests/Rendering.Harness/ValidationLanes.fsi`, `tests/Rendering.Harness/ValidationLanes.fs`, and `tests/Rendering.Harness/Cli.fs` for public `FS.GG.UI.*` runtime behavior drift, update the repository API surface baseline or equivalent harness contract baseline for `ValidationLanes.fsi`, and record the Tier 1 tooling boundary check in `specs/166-validation-lane-runner/readiness/tier1-tooling-boundary.md`
- [X] T049 Record Feature166 synthetic evidence inventory, rationale, real-evidence path, and PR-description-ready disclosure text in `specs/166-validation-lane-runner/readiness/synthetic-evidence.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user-story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational; delivers the MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational and should follow US1 for CLI/result integration.
- **User Story 3 (Phase 5)**: Depends on Foundational and benefits from US1/US2 result fields.
- **User Story 4 (Phase 6)**: Depends on Foundational and can proceed after catalog/request metadata exists.
- **Polish (Phase 7)**: Depends on the desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: No dependency on other stories after Foundational.
- **US2 (P2)**: Depends on US1 lane selection and catalog metadata for end-to-end CLI behavior.
- **US3 (P3)**: Depends on US1 lane metadata and US2 status/reason fields for complete summaries.
- **US4 (P4)**: Depends on US1 catalog metadata; can be implemented independently of US2/US3 once output scopes exist.

### Within Each User Story

- Write tests first and confirm they fail for missing behavior.
- Update `tests/Rendering.Harness/ValidationLanes.fsi` before exposing new harness-visible behavior.
- Implement pure request/update/readiness behavior before edge interpreter work.
- Implement CLI wiring after the underlying pure functions exist.
- Record evidence at each checkpoint before moving to the next story.

---

## Parallel Opportunities

- T002 and T003 can run in parallel after T001 creates compile-safe placeholders and project entries.
- T006 can run in parallel with T004/T005 once the fixture shape is clear.
- US1 tests T008 and T009 can be authored in parallel; T010 follows in the same preflight test file.
- US2 tests T017 and T019 can be authored in parallel; T018 follows in the same status test file.
- US3 test file sections T026, T027, and T028 should be sequenced within the same file.
- US4 tests T035 and T036 can be authored in parallel.
- Polish documentation tasks T041 and T042 can run in parallel after implementation behavior stabilizes; T049 follows the synthetic tests and final evidence run.

## Parallel Example: User Story 1

```bash
Task: "Add catalog tests for required lane ids, optional aggregate role, explicit timeouts, unique lane ids, unique result ids, and isolated evidence paths in tests/Rendering.Harness.Tests/Feature166LaneCatalogTests.fs"
Task: "Add request preflight tests for --required, repeated --lane, --include-optional aggregate-solution, unknown lane rejection, and duplicate requested lane rejection in tests/Rendering.Harness.Tests/Feature166LaneRunnerPreflightTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "Add status classification tests for pass, fail, total timeout, no-progress timeout, skipped, not-run, environment-limited, infrastructure error, and log/result write failures in tests/Rendering.Harness.Tests/Feature166LaneStatusTests.fs"
Task: "Add cancellation tests that preserve completed lane results and mark active, pending, or unstarted lanes as canceled, skipped, or not-run with reasons in tests/Rendering.Harness.Tests/Feature166CancellationTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "Add summary agreement and SC-001 timing tests for Markdown and JSON fields including run id, overall readiness, first blocking required lane, aggregate status, substitutions, caveats, evidence paths, elapsed times, roles, and final summary emission within 10 seconds after the last lane completes in tests/Rendering.Harness.Tests/Feature166ValidationSummaryTests.fs"
Task: "Update validation documentation to describe lane-runner evidence, optional aggregate visibility, targeted substitute disclosure, and preserved direct validation commands in docs/validation/validation-set.md"
```

## Parallel Example: User Story 4

```bash
Task: "Add schedule-safety tests for duplicate output scopes, shared concurrency groups, unsafe parallel requests, and actionable conflict diagnostics in tests/Rendering.Harness.Tests/Feature166SchedulingTests.fs"
Task: "Add lane catalog tests proving all documented lanes declare concurrency group and output scope metadata in tests/Rendering.Harness.Tests/Feature166LaneCatalogTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete US1 tests T008-T010 and confirm they fail.
3. Complete US1 implementation T011-T015.
4. Complete US1 evidence T016.
5. Stop and validate that targeted lane execution and required-lane selection work without relying on the optional aggregate lane.

### Incremental Delivery

1. Deliver US1 for named lanes and required set selection.
2. Add US2 for bounded failures, no-progress diagnosis, cancellation, and exit codes.
3. Add US3 for durable per-lane evidence and reviewer summaries.
4. Add US4 for output-race prevention.
5. Complete Phase 7 readiness evidence and documentation alignment.

### Validation Commands

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter Feature166
dotnet fsi scripts/run-validation-lanes.fsx --list
dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --out specs/166-validation-lane-runner/readiness/lanes
dotnet fsi scripts/run-validation-lanes.fsx --required --out specs/166-validation-lane-runner/readiness/lanes
```

---

## Notes

- `[P]` means the task touches different files or independent test sections and has no dependency on another incomplete task.
- `[US1]`, `[US2]`, `[US3]`, and `[US4]` map directly to the prioritized user stories in `spec.md`.
- Synthetic tests must include `Synthetic` in the test name and a `SYNTHETIC:` comment where process or evidence fixtures replace real validation work. Each synthetic use must also record rationale, real-evidence path or infeasibility, and PR-description-ready disclosure in `specs/166-validation-lane-runner/readiness/synthetic-evidence.md`.
- Existing direct validation workflows must remain runnable outside the lane runner.
