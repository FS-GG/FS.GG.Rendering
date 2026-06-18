# Tasks: Performance Validation Throughput

**Input**: Design documents from `/specs/160-performance-validation-throughput/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by the feature specification and constitution. Story tests should be written first and observed failing before implementation.

**Organization**: Tasks are grouped by user story so focused throughput, release-gate separation, scenario comparability, and reviewer readiness can be implemented and tested independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks
- **[Story]**: User story label from spec.md
- Every task names the exact file or readiness path to change

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the Feature 160 workspace and readiness tree.

- [x] T001 Create the Feature 160 readiness directory guide in specs/160-performance-validation-throughput/readiness/README.md with the throughput, full-validation, fsi, compatibility, package, regression, and validation-summary locations from plan.md
- [x] T002 [P] Create the focused throughput directory guide in specs/160-performance-validation-throughput/readiness/throughput/README.md with iterations/, raw/, excluded/, unsupported/, and summary.md expectations
- [x] T003 [P] Create the full validation directory guide in specs/160-performance-validation-throughput/readiness/full-validation/README.md with the required `dotnet test FS.GG.Rendering.slnx --no-restore` release-gate record shape

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish Feature 160 signatures, FSI authoring transcripts, and status vocabulary before story implementation.

**CRITICAL**: No `.fs` implementation work should begin until these shared signatures and FSI authoring transcripts are drafted.

- [x] T004 Draft Feature 160 harness signatures for constants, paths, status tokens, iteration records, full-validation records, throughput summary records, MVU model/messages/effects, and render functions in tests/Rendering.Harness/Compositor.fsi
- [x] T005 Create and record the pre-implementation compositor performance FSI authoring transcript for the T004 signatures in specs/160-performance-validation-throughput/readiness/fsi/compositor-performance-authoring.fsx before adding Compositor.fs implementations
- [x] T006 [P] Draft Feature 160 package-visible throughput readiness status, scenario evidence, readiness check, validation result, and Feature160ThroughputReadiness module signatures in src/Testing/Testing.fsi
- [x] T007 [P] Create and record the pre-implementation throughput readiness helper FSI authoring transcript for the T006 signatures in specs/160-performance-validation-throughput/readiness/fsi/feature160-throughput-readiness-authoring.fsx before adding Testing.fs implementations
- [x] T008 [P] Add Feature 160 exclusion reason token coverage for timed-out, canceled, partial-evidence, cross-profile-evidence, stale-evidence, mixed-policy, missing-metadata, unsupported-host, environment-limited, scenario-coverage-missing, sample-policy-mismatch, run-identity-mismatch, artifact-unreadable, and readback-contaminated in tests/Rendering.Harness/Perf.fsi
- [x] T009 Create the Feature 160 exclusion reason FSI/surface authoring note for the T008 tokens in specs/160-performance-validation-throughput/readiness/fsi/feature160-exclusion-reasons-authoring.fsx before adding Perf.fs mappings

**Checkpoint**: Feature 160 shared contracts are drafted, exercised through FSI authoring transcripts, and user story tests can target stable names before `.fs` implementation begins.

---

## Phase 3: User Story 1 - Repeat Focused Performance Iterations Quickly (Priority: P1) MVP

**Goal**: Provide a bounded focused performance lane that can run repeated P7 timing iterations without invoking broad release validation each time.

**Independent Test**: Run the focused lane tests and verify a Feature 160 focused iteration declares the 10 minute bound, records required iteration metadata, excludes timed-out/canceled/partial runs, and does not run the broad release gate.

### Tests for User Story 1

> Write these tests first and confirm they fail before implementation.

- [x] T010 [P] [US1] Create failing focused-lane command, bound, no-broad-suite, timeout, canceled, partial-evidence, and expanded exclusion-reason token tests in tests/Rendering.Harness.Tests/Feature160FocusedLaneTests.fs
- [x] T011 [US1] Add tests/Rendering.Harness.Tests/Feature160FocusedLaneTests.fs to the compile list before Program.fs in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj

### Implementation for User Story 1

- [x] T012 [US1] After T010/T011 are observed failing, implement Feature 160 focused lane constants, accepted profile, policy `focused-throughput-v1`, 10 minute bound, attempts requirement, paths, and status token logic in tests/Rendering.Harness/Compositor.fs
- [x] T013 [US1] Implement Feature 160 MVU init/update handling for host detection, lane declaration, policy declaration, bound declaration, iteration start/completion, timeout, cancellation, exclusion, and artifact publication in tests/Rendering.Harness/Compositor.fs
- [x] T014 [US1] Implement `compositor-performance --feature 160 --lane focused` CLI parsing for --out, --policy, --attempts, --max-iteration-minutes, --scenario, and --json in tests/Rendering.Harness/Cli.fs
- [x] T015 [US1] Implement focused iteration artifact writing for summary.md, summary.json when requested, iterations/iteration-*.md, raw/*.csv, raw/*.json, excluded/*.md, and unsupported/README.md in tests/Rendering.Harness/Cli.fs
- [x] T016 [US1] After T010/T011 are observed failing, implement Feature 160 exclusion reason token mappings and fail-closed classification for timed-out, canceled, partial-evidence, missing-metadata, cross-profile-evidence, stale-evidence, mixed-policy, unsupported-host, environment-limited, scenario-coverage-missing, sample-policy-mismatch, run-identity-mismatch, artifact-unreadable, and readback-contaminated focused iterations in tests/Rendering.Harness/Perf.fs
- [x] T017 [US1] Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature160&Focused"` and record the result in specs/160-performance-validation-throughput/readiness/package-validation.md

**Checkpoint**: User Story 1 is independently testable through the focused-lane test filter and the focused command can publish bounded iteration evidence.

---

## Phase 4: User Story 2 - Preserve Full Validation as the Release Gate (Priority: P1)

**Goal**: Keep full solution validation separate from focused throughput so faster timing loops cannot mark the feature release-ready by themselves.

**Independent Test**: Assemble readiness with passing focused evidence but missing/failing/stale full validation and verify release-ready status is blocked while throughput status remains independently reported. Stale records include mismatched implementation commit, validation command, package/surface baseline, or readiness artifact set.

### Tests for User Story 2

> Write these tests first and confirm they fail before implementation.

- [x] T018 [P] [US2] Create failing release-gate separation tests for missing, failing, interrupted, stale, and current full validation records in tests/Rendering.Harness.Tests/Feature160ReleaseGateSeparationTests.fs, including commit, validation-command, package/surface-baseline, and readiness-artifact mismatches as stale
- [x] T019 [US2] Add tests/Rendering.Harness.Tests/Feature160ReleaseGateSeparationTests.fs to the compile list before Program.fs in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj

### Implementation for User Story 2

- [x] T020 [US2] After T018/T019 are observed failing, implement Feature 160 full validation record types, status evaluation, staleness diagnostics for commit, validation-command, package/surface-baseline, and readiness-artifact mismatches, and release-ready blocker decisions in tests/Rendering.Harness/Compositor.fs
- [x] T021 [US2] Extend `compositor-readiness --feature 160` assembly so full validation is read from readiness/full-validation/ and never executed inside focused throughput iteration collection in tests/Rendering.Harness/Cli.fs
- [x] T022 [US2] Render focused throughput status and full validation status as separate decisions in the Feature 160 validation summary in tests/Rendering.Harness/Compositor.fs
- [x] T023 [US2] Write the full validation record template and blocker explanation to specs/160-performance-validation-throughput/readiness/full-validation/validation.md
- [x] T024 [US2] Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature160&ReleaseGate"` and record the result in specs/160-performance-validation-throughput/readiness/package-validation.md

**Checkpoint**: User Story 2 is independently testable through the release-gate test filter and readiness blocks release-ready status without broad validation.

---

## Phase 5: User Story 3 - Keep Performance Scenario Coverage Comparable (Priority: P2)

**Goal**: Preserve the Feature 158 P7 timing scenario set and sample policy for every accepted focused iteration.

**Independent Test**: Compare a focused iteration summary with the required P7 scenario set and verify every required category is covered, or the iteration is excluded with a reviewer-visible reason.

### Tests for User Story 3

> Write these tests first and confirm they fail before implementation.

- [x] T025 [US3] Extend tests/Rendering.Harness.Tests/Feature160FocusedLaneTests.fs with failing tests for the five required timing scenario ids, warmup `3`, measured repetitions `5`, scenario definition ids, sample policy comparability, restricted --scenario non-acceptance, and missing coverage exclusion

### Implementation for User Story 3

- [x] T026 [US3] Implement `feature160RequiredScenarioIds`, `feature160ScenarioIds`, `feature160ScenarioFileName`, warmup `3`, measured repetitions `5`, and Feature 158 scenario-definition reuse in tests/Rendering.Harness/Compositor.fs
- [x] T027 [US3] Implement scenario coverage aggregation and missing-scenario exclusion reason `scenario-coverage-missing` in tests/Rendering.Harness/Compositor.fs
- [x] T028 [US3] Implement sample policy validation for readback-free/readback-outside-measurement samples and sample-policy-mismatch exclusions in tests/Rendering.Harness/Perf.fs
- [x] T029 [US3] Extend `compositor-performance --feature 160 --scenario <id>` so single-scenario debugging output is published but cannot satisfy final throughput acceptance in tests/Rendering.Harness/Cli.fs
- [x] T030 [US3] Render comparison text showing whether a run supersedes, confirms, or cannot be compared to prior Feature 158 evidence in tests/Rendering.Harness/Compositor.fs
- [x] T031 [US3] Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature160&Scenario"` and record the result in specs/160-performance-validation-throughput/readiness/regression-validation.md

**Checkpoint**: User Story 3 preserves comparable scenario coverage and sample policy for accepted throughput.

---

## Phase 6: User Story 4 - Publish Reviewer-Readable Throughput Evidence (Priority: P2)

**Goal**: Provide one reviewer-readable readiness entry point with throughput, exclusions, unsupported-host behavior, full validation, compatibility, package validation, regression validation, and final performance claim status.

**Independent Test**: Open the readiness summary and verify a reviewer can determine throughput status, full validation status, excluded evidence, unsupported-host status, compatibility impact, artifact paths, and why the performance claim remains `performance-not-accepted`.

### Tests for User Story 4

> Write these tests first and confirm they fail before implementation.

- [x] T032 [P] [US4] Create failing readiness package tests for validation-summary links, throughput summary links, unsupported-host zero accepted artifacts, excluded evidence links, full-validation links, noisy-timing-as-performance-claim-gate, reviewer checklist coverage for the 5 minute decision target, and `performance-not-accepted` in tests/Rendering.Harness.Tests/Feature160ReadinessPackageTests.fs
- [x] T033 [US4] Add tests/Rendering.Harness.Tests/Feature160ReadinessPackageTests.fs to the compile list before Program.fs in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [x] T034 [P] [US4] Create failing package-visible throughput readiness helper tests for accepted, blocked, rejected, environment-limited, missing scenarios, zero unsupported artifacts, noisy timing preserving throughput while blocking the shipped performance claim, and overclaimed performance status in tests/Testing.Tests/Feature160ThroughputReadinessTests.fs
- [x] T035 [US4] Add tests/Testing.Tests/Feature160ThroughputReadinessTests.fs to the compile list before Program.fs in tests/Testing.Tests/Testing.Tests.fsproj
- [x] T036 [P] [US4] Create failing package compatibility tests for compatibility-ledger.md, package-validation.md, regression-validation.md, validation-summary.md, and Feature160ThroughputReadiness surface notes in tests/Package.Tests/Feature160CompatibilityTests.fs
- [x] T037 [US4] Add tests/Package.Tests/Feature160CompatibilityTests.fs to the compile list before Tests.fs in tests/Package.Tests/Package.Tests.fsproj

### Implementation for User Story 4

- [x] T038 [US4] After T034/T035 are observed failing, implement Feature160ThroughputReadiness.statusText and Feature160ThroughputReadiness.validate for accepted, blocked, rejected, fallback-only, and environment-limited packages in src/Testing/Testing.fs
- [x] T039 [US4] Render Feature 160 throughput summary, iteration report, excluded evidence report, unsupported-host report, compatibility ledger, package validation, regression validation, and validation summary in tests/Rendering.Harness/Compositor.fs
- [x] T040 [US4] Extend `compositor-readiness --feature 160` to publish compatibility-ledger.md, package-validation.md, regression-validation.md, validation-summary.md, fsi records, and final performance claim status in tests/Rendering.Harness/Cli.fs
- [x] T041 [US4] Re-run and record the compositor performance authoring transcript from T005 in specs/160-performance-validation-throughput/readiness/fsi/compositor-performance-authoring.fsx after implementation
- [x] T042 [US4] Re-run and record the throughput readiness helper authoring transcript from T007 in specs/160-performance-validation-throughput/readiness/fsi/feature160-throughput-readiness-authoring.fsx after implementation
- [x] T043 [US4] Populate compatibility impact notes in specs/160-performance-validation-throughput/readiness/compatibility-ledger.md, including any FS.GG.UI.Testing surface change or an explicit no-new-helper statement
- [x] T044 [US4] Populate package validation notes in specs/160-performance-validation-throughput/readiness/package-validation.md with Rendering.Harness, Testing.Tests, Package.Tests, FSI transcript, and surface-drift outcomes
- [x] T045 [US4] Populate reviewer closeout summary in specs/160-performance-validation-throughput/readiness/validation-summary.md with throughput status, full-validation status, host scope, exclusions, unsupported-host result, compatibility impact, artifact paths, noisy-timing remaining-gate notes when applicable, reviewer checklist evidence for the 5 minute decision target, and `performance-not-accepted`

**Checkpoint**: User Story 4 provides the reviewer entry point and package-visible validation coverage.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate package surface, collect readiness evidence, and close Feature 160 against the quickstart.

- [x] T046 [P] Update FS.GG.UI.Testing surface evidence for Feature160ThroughputReadiness in specs/160-performance-validation-throughput/readiness/fsi/FS.GG.UI.Testing.txt if src/Testing/Testing.fsi changed
- [x] T047 [P] Update Rendering.Harness surface evidence for Feature 160 command and readiness signatures in specs/160-performance-validation-throughput/readiness/fsi/Rendering.Harness.Compositor.txt
- [x] T048 Run `dotnet build FS.GG.Rendering.slnx --no-restore` and record the result in specs/160-performance-validation-throughput/readiness/package-validation.md
- [x] T049 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature160"` and record the result in specs/160-performance-validation-throughput/readiness/package-validation.md
- [x] T050 Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-restore --filter "Feature160"` and record the result in specs/160-performance-validation-throughput/readiness/package-validation.md
- [x] T051 Run `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature160"` and record the result in specs/160-performance-validation-throughput/readiness/package-validation.md
- [x] T052 Run focused throughput collection from quickstart.md and record accepted or excluded iteration artifacts under specs/160-performance-validation-throughput/readiness/throughput/
- [x] T053 Run unsupported-host validation from quickstart.md and record zero accepted same-profile performance artifacts in specs/160-performance-validation-throughput/readiness/throughput/unsupported/README.md
- [x] T054 Run `dotnet test FS.GG.Rendering.slnx --no-restore` and record the full validation status in specs/160-performance-validation-throughput/readiness/full-validation/validation.md
- [x] T055 Populate Feature 155, Feature 157, Feature 158, Feature 159, unsupported-host, package, and public-surface preservation evidence in specs/160-performance-validation-throughput/readiness/regression-validation.md
- [x] T056 Run final `compositor-readiness --feature 160` assembly and verify the reviewer entry point plus 5 minute decision checklist at specs/160-performance-validation-throughput/readiness/validation-summary.md
- [x] T057 Update the Feature 160 tracker line and closeout notes in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md with SC-010 closeout evidence and any bounded deferrals

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **US1 Focused Lane (Phase 3)**: Depends on Foundational. This is the MVP.
- **US2 Release Gate Separation (Phase 4)**: Depends on Foundational and can proceed alongside US1 after shared Compositor.fs edit coordination.
- **US3 Scenario Comparability (Phase 5)**: Depends on US1 focused iteration artifacts and Feature 158 policy references.
- **US4 Reviewer Evidence (Phase 6)**: Depends on US1 and US2 summary fields; can start package-helper tests after Foundational.
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Start after Foundational. No dependency on other stories.
- **US2 (P1)**: Start after Foundational. No dependency on accepted throughput implementation, but final readiness assembly uses US1 artifacts.
- **US3 (P2)**: Start after US1 scenario contracts and focused iteration publication exist because it extends focused iteration coverage.
- **US4 (P2)**: Start after Foundational for package helper tests; final summary implementation depends on US1 and US2 fields.

### Within Each User Story

- FSI authoring transcripts and failing tests before implementation.
- `.fsi` or package-visible signature changes before `.fs` implementation.
- Pure MVU/status decisions before CLI filesystem publication.
- CLI publication before readiness artifact closeout.
- Story-specific test command before moving to the next priority.

### Parallel Opportunities

- T002 and T003 can run in parallel after T001.
- T006 and T008 can run in parallel with T004 because they touch different signature files.
- T010, T018, T032, T034, and T036 can be drafted in parallel after Foundational if compile-file entries are coordinated.
- T046 and T047 can run in parallel after public surface decisions are known.
- Focused harness, Testing helper, and Package compatibility test execution in T049-T051 can run in parallel once implementation is complete.

---

## Parallel Example: User Story 1

```text
Task: "Create failing focused-lane command, bound, no-broad-suite, timeout, canceled, partial-evidence, and expanded exclusion-reason token tests in tests/Rendering.Harness.Tests/Feature160FocusedLaneTests.fs"
Task: "Create the Feature 160 exclusion reason FSI/surface authoring note for the T008 tokens in specs/160-performance-validation-throughput/readiness/fsi/feature160-exclusion-reasons-authoring.fsx"
```

## Parallel Example: User Story 2

```text
Task: "Create failing release-gate separation tests for missing, failing, interrupted, stale, and current full validation records in tests/Rendering.Harness.Tests/Feature160ReleaseGateSeparationTests.fs, including commit, validation-command, package/surface-baseline, and readiness-artifact mismatches as stale"
Task: "Write the full validation record template and blocker explanation to specs/160-performance-validation-throughput/readiness/full-validation/validation.md"
```

## Parallel Example: User Story 3

```text
Task: "Implement sample policy validation for readback-free/readback-outside-measurement samples and sample-policy-mismatch exclusions in tests/Rendering.Harness/Perf.fs"
Task: "Render comparison text showing whether a run supersedes, confirms, or cannot be compared to prior Feature 158 evidence in tests/Rendering.Harness/Compositor.fs"
```

## Parallel Example: User Story 4

```text
Task: "Create failing package-visible throughput readiness helper tests for accepted, blocked, rejected, environment-limited, missing scenarios, zero unsupported artifacts, noisy timing preserving throughput while blocking the shipped performance claim, and overclaimed performance status in tests/Testing.Tests/Feature160ThroughputReadinessTests.fs"
Task: "Create failing package compatibility tests for compatibility-ledger.md, package-validation.md, regression-validation.md, validation-summary.md, and Feature160ThroughputReadiness surface notes in tests/Package.Tests/Feature160CompatibilityTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundational contracts.
3. Complete Phase 3 US1 focused lane tests and implementation.
4. Validate US1 with `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature160&Focused"`.
5. Stop and review focused throughput evidence before adding release-gate and reviewer package behavior.

### Incremental Delivery

1. Add US1 focused lane and prove bounded repeated iterations.
2. Add US2 release-gate separation and prove full validation still blocks release-ready status.
3. Add US3 scenario coverage comparability and prove Feature 158 policy preservation.
4. Add US4 reviewer package and package-visible helper validation.
5. Complete polish validation from quickstart.md and readiness artifacts.

### Parallel Team Strategy

1. One developer owns shared Compositor.fsi/Compositor.fs contracts to avoid F# signature conflicts.
2. A second developer writes harness tests in tests/Rendering.Harness.Tests/.
3. A third developer writes Testing and Package tests in tests/Testing.Tests/ and tests/Package.Tests/.
4. After contracts stabilize, CLI publication and readiness rendering can proceed with file-level coordination.

## Notes

- [P] tasks are safe to parallelize only when their named files do not conflict with active edits.
- Synthetic fixtures are allowed only for rejection/helper tests, must include `Synthetic` in the test name, and must include a source comment explaining why real GL evidence is not used.
- The focused lane can accept Feature 160 throughput, but the shipped compositor performance claim remains `performance-not-accepted` until same-profile timing, Feature 159 reuse/promotion, Feature 160 throughput, and Feature 161 host-lane gates are all complete and positive.
