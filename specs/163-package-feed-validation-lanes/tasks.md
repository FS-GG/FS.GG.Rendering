# Tasks: Package Feed Validation Lanes

**Input**: Design documents from `/specs/163-package-feed-validation-lanes/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Required. The specification marks user scenarios and testing as mandatory, and the constitution requires failing-first semantic tests before implementation.

**Context**: No extra task-generation constraints were supplied.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested as an independent increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and does not depend on incomplete tasks.
- **[Story]**: User story label for traceability, used only in user-story phases.
- Every task includes exact repository file paths.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the durable feature artifact locations and script entry points used by every story.

- [X] T001 [P] Create readiness directory placeholders in `specs/163-package-feed-validation-lanes/readiness/package-proof/.gitkeep`, `specs/163-package-feed-validation-lanes/readiness/lanes/.gitkeep`, `specs/163-package-feed-validation-lanes/readiness/diagnostics/.gitkeep`, and `specs/163-package-feed-validation-lanes/readiness/fsi/.gitkeep`
- [X] T002 [P] Create thin script entry-point placeholders in `scripts/refresh-local-feed-and-samples.fsx` and `scripts/run-validation-lanes.fsx`
- [X] T003 [P] Add a Feature 163 readiness index in `specs/163-package-feed-validation-lanes/readiness/README.md` listing package proof, lanes, diagnostics, compatibility, package validation, regression validation, and validation summary artifact paths

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Declare the harness-visible surfaces and compile-order wiring required before user stories add behavior.

**Critical**: No user story implementation should begin until this phase is complete.

- [X] T004 [P] Define the package-feed public signature in `tests/Rendering.Harness/PackageFeed.fsi` with package, sample, pin, feed, source-rule, proof, MVU model/message/effect/init/update, edge-interpreter effect contracts, and result types from `data-model.md`, `contracts/package-feed-command.md`, and `contracts/workflow-effects.md`
- [X] T005 [P] Define the validation-lane public signature in `tests/Rendering.Harness/ValidationLanes.fsi` with lane definition, lane result, summary, status, MVU model/message/effect/init/update, edge-interpreter effect contracts, and renderer contracts from `contracts/validation-lane-runner.md`, `contracts/validation-summary.md`, and `contracts/workflow-effects.md`
- [X] T006 Add compiling stub implementations in `tests/Rendering.Harness/PackageFeed.fs` and `tests/Rendering.Harness/ValidationLanes.fs`, then wire `PackageFeed.fsi`, `PackageFeed.fs`, `ValidationLanes.fsi`, and `ValidationLanes.fs` before `Cli.fs` in `tests/Rendering.Harness/Rendering.Harness.fsproj`
- [X] T007 [P] Add Feature 163 test-file placeholders and compile entries for `tests/Rendering.Harness.Tests/Feature163PackageFeedTests.fs`, `tests/Rendering.Harness.Tests/Feature163PackageSourceProofTests.fs`, `tests/Rendering.Harness.Tests/Feature163ValidationLaneTests.fs`, `tests/Rendering.Harness.Tests/Feature163ValidationSummaryTests.fs`, and `tests/Package.Tests/Feature163PackageFeedValidationTests.fs`
- [X] T008 Add shared temporary repository and XML fixture helpers in `tests/Rendering.Harness.Tests/Feature163TestFixtures.fs` and wire it before Feature 163 test files in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`

**Checkpoint**: Harness signatures, stub bodies, and test placeholders compile; user story tests can now be written against the `.fsi` surfaces.

---

## Phase 3: User Story 1 - Prove samples use current local packages (Priority: P1) - MVP

**Goal**: A maintainer can discover current `FS.GG.UI.*` package versions, check selected sample package pins before build/restore, refresh stale pins, and see package-pin evidence.

**Independent Test**: Introduce a stale `FS.GG.UI.*` package pin in a temporary package-consuming sample, run the package-pin check, and verify failure includes package id, expected version, actual version, and sample path; refresh the pin and verify the check passes.

### Tests for User Story 1

Write these tests first and verify they fail against the stubs from Phase 2.

- [X] T009 [P] [US1] Add failing package discovery, stale-pin, compatibility-exception, refresh-mode, PackageFeed pure `init`/`update` transition, and edge-interpreter tests in `tests/Rendering.Harness.Tests/Feature163PackageFeedTests.fs`
- [X] T010 [P] [US1] Add failing AntShowcase package-pin drift assertions in `tests/Package.Tests/Feature163PackageFeedValidationTests.fs`

### Implementation for User Story 1

- [X] T011 [US1] Implement package-feed edge-interpreter support for packable `FS.GG.UI.*` project discovery from `src/*/*.fsproj` in `tests/Rendering.Harness/PackageFeed.fs`
- [X] T012 [US1] Implement package-feed edge-interpreter support for selected sample project discovery and `PackageReference` XML parsing for `FS.GG.UI.*` pins in `tests/Rendering.Harness/PackageFeed.fs`
- [X] T013 [US1] Implement pure PackageFeed MVU transitions and package-pin status classification for `current`, `stale`, `missing-expected-package`, `compatibility-exception`, and `not-selected` in `tests/Rendering.Harness/PackageFeed.fs`
- [X] T014 [US1] Implement package-feed edge-interpreter support for refresh-mode XML updates that rewrite stale selected-sample `FS.GG.UI.*` versions and record changed files in `tests/Rendering.Harness/PackageFeed.fs`
- [X] T015 [US1] Implement package-feed edge-interpreter support for package-version and package-pin Markdown evidence writers for `package-versions.md` and `package-pins.md` in `tests/Rendering.Harness/PackageFeed.fs`
- [X] T016 [US1] Add `package-feed --sample --mode check|refresh --feed --out --pack --allow-exception` CLI parsing and exit-code mapping in `tests/Rendering.Harness/Cli.fs`
- [X] T017 [US1] Implement `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --mode check|refresh` delegation to the harness package-feed command in `scripts/refresh-local-feed-and-samples.fsx`
- [X] T018 [US1] Normalize each AntShowcase `FS.GG.UI.*` package pin to its package-specific discovered current source version in `samples/AntShowcase/AntShowcase.Core/AntShowcase.Core.fsproj`, `samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj`, and `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`
- [X] T019 [US1] Run User Story 1 focused tests and record stale-pin and refresh evidence in `specs/163-package-feed-validation-lanes/readiness/package-validation.md`

**Checkpoint**: User Story 1 is independently functional; package pins can be checked/refreshed before any sample build or test.

---

## Phase 4: User Story 2 - Prove package source selection is deterministic (Priority: P1)

**Goal**: A maintainer can prove selected samples resolve `FS.GG.UI.*` packages only from the configured local feed while approved third-party sources remain available.

**Independent Test**: Run an isolated package proof for a selected sample and verify evidence records source rules, cache path, feed path, resolved `FS.GG.UI.*` versions, and failure on any non-local `FS.GG.UI.*` source.

### Tests for User Story 2

Write these tests first and verify they fail against the current implementation.

- [X] T020 [P] [US2] Add failing source-proof tests for isolated cache, generated NuGet config, missing local packages, source violations, third-party source allowance, no-selected-samples, and no-package-pins in `tests/Rendering.Harness.Tests/Feature163PackageSourceProofTests.fs`

### Implementation for User Story 2

- [X] T021 [US2] Implement local feed expected-package checks and missing-package classification in `tests/Rendering.Harness/PackageFeed.fs`
- [X] T022 [US2] Implement generated package source mapping for `FS.GG.UI.*` local-feed rules and approved external third-party rules in `tests/Rendering.Harness/PackageFeed.fs`
- [X] T023 [US2] Implement isolated-cache and explicit cold-proof policy handling, including `globalCacheCleared` evidence, in `tests/Rendering.Harness/PackageFeed.fs`
- [X] T024 [US2] Implement restore execution effects, restore log capture, and `project.assets.json` artifact copying in `tests/Rendering.Harness/PackageFeed.fs`
- [X] T025 [US2] Implement resolved package/source classification for `source-violation`, `restore-failed`, `assets-unreadable`, `cache-policy-violation`, `no-selected-samples`, and `no-package-pins` in `tests/Rendering.Harness/PackageFeed.fs`
- [X] T026 [US2] Implement `source-proof.md`, `source-proof.json`, `source-rules.nuget.config`, `restore.log`, and `assets/` evidence writers in `tests/Rendering.Harness/PackageFeed.fs`
- [X] T027 [US2] Extend `package-feed --mode proof --isolated-cache --cold --clear-global-cache` CLI parsing and fail-closed exit-code mapping in `tests/Rendering.Harness/Cli.fs`
- [X] T028 [US2] Add package source mapping documentation and defaults for local `FS.GG.UI.*` and approved third-party sources in `samples/AntShowcase/nuget.config`
- [X] T029 [US2] Extend proof-mode script delegation and argument validation in `scripts/refresh-local-feed-and-samples.fsx`
- [X] T030 [US2] Run User Story 2 focused tests and record isolated-cache/source-proof evidence in `specs/163-package-feed-validation-lanes/readiness/package-validation.md`

**Checkpoint**: User Story 2 is independently functional; source proof fails closed when the local-feed rule cannot be proven.

---

## Phase 5: User Story 3 - Run diagnosable validation lanes (Priority: P2)

**Goal**: A maintainer can run named validation lanes with isolated outputs, per-lane logs/results, timeout handling, no-progress diagnostics, cancellation handling, and non-green incomplete statuses.

**Independent Test**: Run a short successful lane, a failing lane, and a lane that exceeds its timeout; verify each writes separate logs/results and is classified correctly in the summary.

### Tests for User Story 3

Write these tests first and verify they fail against the current implementation.

- [X] T031 [P] [US3] Add failing lane definition, pass/fail/timeout/hung/cancel/no-progress classification, output isolation, and minimum-lane tests in `tests/Rendering.Harness.Tests/Feature163ValidationLaneTests.fs`

### Implementation for User Story 3

- [X] T032 [US3] Implement validation lane definition, status, result, diagnostic, and output-isolation models in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T033 [US3] Implement pure lane-runner MVU transitions for run request, lane start, output, completion, timeout, no-progress, cancellation, diagnostics, and summary request in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T034 [US3] Implement process interpreter effects for starting processes, appending logs, polling, stopping, and preserving stdout/stderr in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T035 [US3] Implement lane-specific result, log, diagnostics, and output-root path allocation in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T036 [US3] Implement default lane definitions for `package-proof`, `antshowcase-sample`, `controls`, `rendering-harness`, and `aggregate-solution` in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T037 [US3] Add `validation-lanes --lane --out --json` CLI parsing and usage text in `tests/Rendering.Harness/Cli.fs`
- [X] T038 [US3] Implement `dotnet fsi scripts/run-validation-lanes.fsx --lane ... --out ...` delegation to the harness validation-lanes command in `scripts/run-validation-lanes.fsx`
- [X] T039 [US3] Implement per-lane `result.json`, `log.txt`, diagnostics, and lane summary artifact writing under `specs/163-package-feed-validation-lanes/readiness/lanes/` in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T040 [US3] Run User Story 3 focused tests and record lane status/output-isolation evidence in `specs/163-package-feed-validation-lanes/readiness/package-validation.md`

**Checkpoint**: User Story 3 is independently functional; lanes produce diagnosable, isolated evidence and never count timed-out, hung, canceled, skipped, not-run, or environment-limited results as passed.

---

## Phase 6: User Story 4 - Read an honest readiness summary (Priority: P3)

**Goal**: A reviewer can open one summary and understand package proof status, lane status, cache/source locations, aggregate-solution status, caveats, incomplete evidence, and readiness.

**Independent Test**: Generate a summary from mixed lane results containing passed, failed, timed-out, hung, skipped, canceled, not-run, and environment-limited statuses; verify every status is visible and overall readiness is blocked or incomplete.

### Tests for User Story 4

Write these tests first and verify they fail against the current implementation.

- [X] T041 [P] [US4] Add failing mixed-status summary tests covering passed, failed, timed-out, hung, skipped, canceled, not-run, and environment-limited lanes plus package-proof inclusion, aggregate-separation, and fail-closed readiness in `tests/Rendering.Harness.Tests/Feature163ValidationSummaryTests.fs`
- [X] T042 [US4] Add failing source-controlled readiness evidence assertions for compatibility, package validation, regression validation, and final summary links in `tests/Package.Tests/Feature163PackageFeedValidationTests.fs`

### Implementation for User Story 4

- [X] T043 [US4] Implement overall readiness computation for `ready`, `blocked`, `incomplete`, and `environment-limited` in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T044 [US4] Implement Markdown summary rendering with package proof, selected samples, local feed, cache, source rules, required lanes, aggregate lane status, caveats, accepted exceptions, and reviewer checklist in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T045 [US4] Implement machine-checkable `summary.json` rendering for package proof, lane results, overall readiness, caveats, and artifact paths in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T046 [US4] Create compatibility evidence stating repository validation harness/script contracts changed and no public UI framework API changed in `specs/163-package-feed-validation-lanes/readiness/compatibility-ledger.md`
- [X] T047 [US4] Create package validation evidence covering package-feed command tests, source-proof tests, lane-runner tests, Package.Tests assertions, surface-drift command outcome, pack/local-feed outcome, and AntShowcase proof in `specs/163-package-feed-validation-lanes/readiness/package-validation.md`
- [X] T048 [US4] Create regression validation evidence for AntShowcase package-only restore behavior, existing package validation evidence, existing Rendering.Harness commands, Feature 160/161 lane/readiness preservation, and concurrent output isolation in `specs/163-package-feed-validation-lanes/readiness/regression-validation.md`
- [X] T049 [US4] Create final reviewer summary linking package proof, lane summary, diagnostics, compatibility, package validation, regression validation, and incomplete evidence in `specs/163-package-feed-validation-lanes/readiness/validation-summary.md`
- [X] T050 [US4] Run User Story 4 focused tests and record mixed-status summary evidence covering all eight lane statuses in `specs/163-package-feed-validation-lanes/readiness/package-validation.md`

**Checkpoint**: User Story 4 is independently functional; reviewers can distinguish focused lane success from incomplete aggregate validation from one summary.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, FSI evidence, documentation closeout, and regression checks across all stories.

- [X] T051 [P] Capture PackageFeed FSI authoring transcript and log in `specs/163-package-feed-validation-lanes/readiness/fsi/package-feed-authoring.fsx` and `specs/163-package-feed-validation-lanes/readiness/fsi/package-feed-authoring.log`
- [X] T052 [P] Capture ValidationLanes FSI authoring transcript and log in `specs/163-package-feed-validation-lanes/readiness/fsi/validation-lanes-authoring.fsx` and `specs/163-package-feed-validation-lanes/readiness/fsi/validation-lanes-authoring.log`
- [X] T053 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature163"` and record the result in `specs/163-package-feed-validation-lanes/readiness/package-validation.md`
- [X] T054 Run `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature163"` and record the result in `specs/163-package-feed-validation-lanes/readiness/package-validation.md`
- [X] T055 Run the package proof and lane commands from `specs/163-package-feed-validation-lanes/quickstart.md` and record command results in `specs/163-package-feed-validation-lanes/readiness/validation-summary.md`
- [X] T056 Update final command notes, accepted caveats, and repeatability instructions in `specs/163-package-feed-validation-lanes/quickstart.md`
- [X] T057 Run `dotnet fsi scripts/refresh-surface-baselines.fsx`, verify `git status --porcelain tests/surface-baselines/` is empty unless a package-visible `.fsi` change intentionally updates baselines, and record the surface-drift outcome or no-public-UI-surface rationale in `specs/163-package-feed-validation-lanes/readiness/compatibility-ledger.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks user-story implementation because `.fsi` contracts and compile-order stubs must exist first.
- **User Story 1 (Phase 3)**: Depends on Foundational; recommended MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational and can develop source-proof pieces in parallel with User Story 1, but end-to-end proof requires User Story 1 package-pin checks.
- **User Story 3 (Phase 5)**: Depends on Foundational; can proceed after lane signatures exist.
- **User Story 4 (Phase 6)**: Depends on User Stories 1-3 for complete package proof and lane evidence, but mixed-status summary tests can be written earlier.
- **Polish (Phase 7)**: Depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: No dependency on other stories after Foundational.
- **US2 (P1)**: Uses PackageFeed models from Foundational and package-pin status from US1 for full proof acceptance.
- **US3 (P2)**: No dependency on US1/US2 for lane mechanics; the default `package-proof` lane invokes the package proof once US1/US2 are complete.
- **US4 (P3)**: Summarizes outputs from US1, US2, and US3.

### Within Each User Story

- Tests come first and must fail against the previous implementation or stubs.
- `.fsi` surface changes are made before `.fs` behavior.
- Pure MVU transitions are implemented before edge interpreters.
- Evidence writers are implemented before CLI/script closeout.
- Each checkpoint should be validated before moving to the next priority story.

---

## Parallel Opportunities

- T001, T002, and T003 can run in parallel.
- T004 and T005 can run in parallel because they define separate `.fsi` surfaces.
- T007 should finish before T008 wires shared fixture ordering because both coordinate `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`.
- T009 and T010 can run in parallel because they target different test projects.
- T020 can run in parallel with late US1 implementation once foundational PackageFeed types exist.
- T031 can run in parallel with US1/US2 implementation because lane tests target `ValidationLanes`.
- T041 can start once `ValidationLanes.fsi` exists; T042 should wait for the readiness file names to settle.
- T051 and T052 can run in parallel after the corresponding `.fsi` and implementation surfaces compile.

---

## Parallel Example: User Story 1

```bash
# Package-feed harness tests and Package.Tests drift assertions can be authored together:
Task: "T009 Add failing package discovery, stale-pin, compatibility-exception, and refresh-mode tests in tests/Rendering.Harness.Tests/Feature163PackageFeedTests.fs"
Task: "T010 Add failing AntShowcase package-pin drift assertions in tests/Package.Tests/Feature163PackageFeedValidationTests.fs"
```

## Parallel Example: User Story 2

```bash
# Source proof tests can be written while US1 package-pin implementation is finishing:
Task: "T020 Add failing source-proof tests for isolated cache, generated NuGet config, missing local packages, source violations, third-party source allowance, no-selected-samples, and no-package-pins in tests/Rendering.Harness.Tests/Feature163PackageSourceProofTests.fs"
```

## Parallel Example: User Story 3

```bash
# Lane mechanics are isolated from package-feed implementation:
Task: "T031 Add failing lane definition, pass/fail/timeout/hung/cancel/no-progress classification, output isolation, and minimum-lane tests in tests/Rendering.Harness.Tests/Feature163ValidationLaneTests.fs"
```

## Parallel Example: User Story 4

```bash
# Summary rendering tests can begin with synthetic lane/package-proof fixtures:
Task: "T041 Add failing mixed-status summary, package-proof inclusion, aggregate-separation, and fail-closed readiness tests in tests/Rendering.Harness.Tests/Feature163ValidationSummaryTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 through T019.
3. Validate `package-feed --mode check|refresh` against temporary fixtures and AntShowcase package pins.
4. Stop and review before adding source proof or lane orchestration.

### Incremental Delivery

1. Add US1 package discovery, pin check, refresh, and evidence.
2. Add US2 isolated source proof and generated NuGet source mapping.
3. Add US3 validation lanes with timeout, no-progress, cancellation, and output isolation.
4. Add US4 honest readiness summary and source-controlled evidence.
5. Finish FSI transcripts, focused tests, Package.Tests evidence checks, and quickstart validation.

### Parallel Team Strategy

1. One contributor owns `PackageFeed` stories US1/US2.
2. One contributor owns `ValidationLanes` story US3.
3. One contributor authors US4 summary tests and readiness documents after artifact names stabilize.
4. Coordinate edits to `tests/Rendering.Harness/Cli.fs` and project files because they are shared integration points.

---

## Notes

- Keep scripts thin; core behavior belongs in `tests/Rendering.Harness/PackageFeed.fs` and `tests/Rendering.Harness/ValidationLanes.fs`.
- Do not clear global NuGet caches unless `--cold --clear-global-cache` is explicitly supplied and recorded.
- Preserve AntShowcase as a package-consuming sample; do not replace sample package references with direct source project references to satisfy validation.
- Non-green statuses remain visible and fail closed unless an accepted compatibility or environment exception is recorded.
