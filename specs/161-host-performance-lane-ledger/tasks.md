# Tasks: Host Performance Lane Ledger

**Input**: Design documents from `/specs/161-host-performance-lane-ledger/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by the feature specification and constitution. Story tests should be written first and observed failing before implementation.

**Organization**: Tasks are grouped by user story so host fact capture, lane scoping, fail-closed behavior, and reviewer evidence can be implemented and tested independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks
- **[Story]**: User story label from spec.md
- Every task names the exact file or readiness path to change

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the Feature 161 readiness tree and durable evidence locations.

- [X] T001 Create the Feature 161 readiness directory guide in specs/161-host-performance-lane-ledger/readiness/README.md with lane-ledger, full-validation, fsi, compatibility, package, regression, and validation-summary locations from plan.md
- [X] T002 [P] Create the lane ledger directory guide in specs/161-host-performance-lane-ledger/readiness/lane-ledger/README.md with entries/, host-facts/, excluded/, unsupported/, summary.md, and optional summary.json expectations
- [X] T003 [P] Create the full validation directory guide in specs/161-host-performance-lane-ledger/readiness/full-validation/README.md with the required `dotnet test FS.GG.Rendering.slnx --no-restore` release-gate record shape
- [X] T004 [P] Create the FSI evidence directory guide in specs/161-host-performance-lane-ledger/readiness/fsi/README.md with compositor, Perf, Testing helper, and surface evidence expectations

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish Feature 161 signatures, status vocabulary, and FSI authoring evidence before story implementation.

**CRITICAL**: No `.fs` implementation work should begin until these shared signatures and FSI authoring transcripts are drafted.

- [X] T005 Draft Feature 161 harness signatures for constants, lane facts, ledger entries, exclusion reasons, prior gate links, claim scope, readiness result, MVU model/messages/effects, and render functions in tests/Rendering.Harness/Compositor.fsi
- [X] T006 [P] Draft Feature 161 performance exclusion reason token signatures for missing-display, indirect-rendering, software-raster, unknown-renderer, virtualized-presentation, ambiguous-gpu, refresh-rate-unavailable, package-version-mismatch, load-non-representative, host-facts-missing, host-facts-contradictory, cross-lane-evidence, stale-evidence, noisy-timing, prior-gate-blocked, and artifact-unreadable in tests/Rendering.Harness/Perf.fsi
- [X] T007 [P] Draft Feature 161 package-visible host lane readiness status, host fact evidence, claim scope, readiness check, validation result, and Feature161HostLaneReadiness module signatures in src/Testing/Testing.fsi
- [X] T008 Create and record the pre-implementation compositor host lane FSI authoring transcript for the T005 signatures in specs/161-host-performance-lane-ledger/readiness/fsi/compositor-host-lane-authoring.fsx before adding Compositor.fs implementations
- [X] T009 [P] Create and record the pre-implementation Testing helper FSI authoring transcript for the T007 signatures in specs/161-host-performance-lane-ledger/readiness/fsi/feature161-host-lane-readiness-authoring.fsx before adding Testing.fs implementations

**Checkpoint**: Feature 161 shared contracts are drafted, exercised through FSI authoring transcripts, and user story tests can target stable names before `.fs` implementation begins.

---

## Phase 3: User Story 1 - Record Complete Host Lane Facts (Priority: P1) MVP

**Goal**: Every compositor timing run considered for P7 performance acceptance records complete host facts or a reviewer-visible exclusion reason.

**Independent Test**: Assemble readiness for a timing run and verify that display, renderer, direct-rendering, refresh, driver, package, load, environment, profile, run, scenario, policy, collection time, and artifact facts are recorded or the run is rejected with the missing fact named.

### Tests for User Story 1

> Write these tests first and confirm they fail before implementation.

- [X] T010 [P] [US1] Create failing host fact completeness, missing fact, contradictory fact, stale fact, unreadable fact, and historical P7 evidence status tests in tests/Rendering.Harness.Tests/Feature161HostLaneFactTests.fs
- [X] T011 [US1] Add tests/Rendering.Harness.Tests/Feature161HostLaneFactTests.fs to the compile list before Feature160FocusedLaneTests.fs in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj

### Implementation for User Story 1

- [X] T012 [US1] After T010/T011 are observed failing, implement Feature 161 host fact records, required fact validation, completeness decisions, and primary exclusion reason selection in tests/Rendering.Harness/Compositor.fs
- [X] T013 [US1] Implement Feature 161 MVU init/update handling for timing-run discovery, host profile detection, host fact collection, host fact validation, prior-gate linking, and diagnostics in tests/Rendering.Harness/Compositor.fs
- [X] T014 [US1] Implement Feature 161 edge fact collection for display server, display identity, renderer identity, direct rendering status, refresh facts, driver identity, package version set, CPU/GPU load notes, environment limits, host profile, run identity, scenario identity, timing policy identity, collection time, and artifact locations in tests/Rendering.Harness/Cli.fs
- [X] T015 [US1] Implement host fact and ledger artifact publication for lane-ledger/host-facts/facts-*.md and lane-ledger/entries/entry-*.md in tests/Rendering.Harness/Cli.fs
- [X] T016 [US1] Render historical Feature 155, Feature 157, Feature 158, Feature 159, and Feature 160 evidence status as confirmed, superseded, contextual-only, or unusable in tests/Rendering.Harness/Compositor.fs
- [X] T017 [US1] Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature161&HostLaneFact"` and record the result in specs/161-host-performance-lane-ledger/readiness/package-validation.md

**Checkpoint**: User Story 1 is independently testable through the host fact test filter and can publish complete or explicitly excluded lane fact records.

---

## Phase 4: User Story 2 - Scope Performance Claims to Known Lanes (Priority: P1)

**Goal**: Any compositor performance claim names the exact host lane it applies to and refuses cross-lane aggregation.

**Independent Test**: Review the readiness summary and verify that it names the accepted lane only when collected facts confirm X11 `:1` direct OpenGL AMD/Mesa for profile `probe-08a47c01`, lists non-generalized lanes, and rejects mixed-lane artifacts.

### Tests for User Story 2

> Write these tests first and confirm they fail before implementation.

- [X] T018 [P] [US2] Create failing lane claim scope, non-generalized lane, cross-lane aggregation rejection, refresh behavior mismatch, package mismatch, scenario mismatch, policy mismatch, run identity mismatch, environment-limit mismatch, and load-comparability tests in tests/Rendering.Harness.Tests/Feature161LaneLedgerTests.fs
- [X] T019 [US2] Add tests/Rendering.Harness.Tests/Feature161LaneLedgerTests.fs to the compile list before Feature160FocusedLaneTests.fs in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj

### Implementation for User Story 2

- [X] T020 [US2] After T018/T019 are observed failing, implement Feature 161 lane id construction and lane equality rules for display server, display identity, renderer identity, direct-rendering mode, refresh behavior or reason unavailable, driver identity, package version set, host profile, scenario definition, timing policy, run identity, and environment/load comparability in tests/Rendering.Harness/Compositor.fs
- [X] T021 [US2] Implement cross-lane, refresh, environment-limit, load-non-representative, stale-package, scenario, timing-policy, host-profile, and run-identity rejection decisions with exactly one primary exclusion reason in tests/Rendering.Harness/Compositor.fs
- [X] T022 [US2] Implement Feature 161 exclusion reason token mappings and diagnostic text in tests/Rendering.Harness/Perf.fs
- [X] T023 [US2] Implement `compositor-performance --feature 161 --lane host-ledger --policy host-lane-ledger-v1 --source-throughput <dir> --out <dir> --json` command parsing in tests/Rendering.Harness/Cli.fs
- [X] T024 [US2] Render claim scope, accepted lane id, host profile, supporting ledger entries, remaining blockers, and non-generalized lanes in tests/Rendering.Harness/Compositor.fs
- [X] T025 [US2] Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature161&LaneLedger"` and record the result in specs/161-host-performance-lane-ledger/readiness/package-validation.md

**Checkpoint**: User Story 2 is independently testable through the lane ledger test filter and cannot combine timing artifacts across host lanes.

---

## Phase 5: User Story 3 - Fail Closed for Unsupported or Mismatched Lanes (Priority: P1)

**Goal**: Unsupported, mismatched, noisy, cross-profile, or environment-limited runs preserve diagnostics while recording zero accepted lane-scoped performance artifacts.

**Independent Test**: Assemble readiness for unsupported, missing-display, indirect-rendering, software-raster, unknown-renderer, mismatched-profile, stale-version, and noisy timing cases and verify that every case records zero accepted lane-scoped performance artifacts.

### Tests for User Story 3

> Write these tests first and confirm they fail before implementation.

- [X] T026 [US3] Extend tests/Rendering.Harness.Tests/Feature161LaneLedgerTests.fs with failing unsupported-host, missing-display, indirect-rendering, software-raster, unknown-renderer, virtualized-presentation, mismatched-profile, stale-version, noisy-timing, and changed-facts-between-proof-and-timing tests
- [X] T027 [P] [US3] Create failing package-visible host lane readiness helper tests for accepted, rejected, fallback-only, environment-limited, blocked, missing facts, zero unsupported artifacts, noisy timing, prior-gate blocked, and overclaimed performance status in tests/Testing.Tests/Feature161HostLaneReadinessTests.fs
- [X] T028 [US3] Add tests/Testing.Tests/Feature161HostLaneReadinessTests.fs to the compile list before Program.fs in tests/Testing.Tests/Testing.Tests.fsproj

### Implementation for User Story 3

- [X] T029 [US3] After T026-T028 are observed failing, implement fail-closed classification for unsupported hosts, missing display, indirect rendering, software rasterization, unknown renderer, virtualized presentation, ambiguous GPU, stale package versions, and non-representative load in tests/Rendering.Harness/Compositor.fs
- [X] T030 [US3] Implement noisy timing preservation, prior P7 gate linkage, and final `performance-not-accepted` claim boundary for same-profile timing, Feature 159 reuse/promotion, Feature 160 throughput, and Feature 161 lane facts in tests/Rendering.Harness/Compositor.fs
- [X] T031 [US3] Implement Feature161HostLaneReadiness.statusText and Feature161HostLaneReadiness.validate for accepted, rejected, fallback-only, environment-limited, blocked, and missing-evidence packages in src/Testing/Testing.fs
- [X] T032 [US3] Implement unsupported-host publication under lane-ledger/unsupported/README.md with accepted lane-scoped performance artifacts `0` in tests/Rendering.Harness/Cli.fs
- [X] T033 [US3] Record unsupported-host command expectations and zero accepted artifacts in specs/161-host-performance-lane-ledger/readiness/lane-ledger/unsupported/README.md
- [X] T034 [US3] Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature161&Unsupported"` and record the result in specs/161-host-performance-lane-ledger/readiness/package-validation.md
- [X] T035 [US3] Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-restore --filter "Feature161"` and record the result in specs/161-host-performance-lane-ledger/readiness/package-validation.md

**Checkpoint**: User Story 3 preserves unsupported-host fail-closed behavior and package-visible readiness checks without accepting noisy or unsupported performance evidence.

---

## Phase 6: User Story 4 - Publish Reviewer-Readable Lane Evidence (Priority: P2)

**Goal**: Provide one readiness entry point that explains lane facts, excluded evidence, environment limits, prior gates, compatibility impact, package validation, regression evidence, and final claim status.

**Independent Test**: Open the readiness summary and verify that a reviewer can determine lane completeness, applicable scope, excluded evidence, prior P7 gate status, compatibility impact, artifact paths, and final performance claim status from one entry point in under 5 minutes.

### Tests for User Story 4

> Write these tests first and confirm they fail before implementation.

- [X] T036 [P] [US4] Create failing readiness package tests for validation-summary links, lane-ledger summary links, host-fact links, excluded evidence links, unsupported-host zero accepted artifacts, prior P7 gate links, full-validation links, compatibility links, package links, regression links, 5-minute reviewer checklist fields, and `performance-not-accepted` in tests/Rendering.Harness.Tests/Feature161ReadinessPackageTests.fs
- [X] T037 [US4] Add tests/Rendering.Harness.Tests/Feature161ReadinessPackageTests.fs to the compile list before Feature151RenderAnywhereRegressionTests.fs in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T038 [P] [US4] Create failing package compatibility tests for compatibility-ledger.md, package-validation.md, regression-validation.md, validation-summary.md, fsi surface evidence, and Feature161HostLaneReadiness surface notes in tests/Package.Tests/Feature161CompatibilityTests.fs
- [X] T039 [US4] Add tests/Package.Tests/Feature161CompatibilityTests.fs to the compile list before Feature151PackageValidationTests.fs in tests/Package.Tests/Package.Tests.fsproj

### Implementation for User Story 4

- [X] T040 [US4] After T036-T039 are observed failing, render Feature 161 lane-ledger summary, accepted entries, excluded entries, unsupported-host result, prior P7 gate status, full-validation status, compatibility impact, package validation, regression validation, reviewer checklist, and final claim status in tests/Rendering.Harness/Compositor.fs
- [X] T041 [US4] Extend `compositor-readiness --feature 161 --out <dir>` to publish lane-ledger/summary.md, compatibility-ledger.md, package-validation.md, regression-validation.md, full-validation links, fsi links, and validation-summary.md in tests/Rendering.Harness/Cli.fs
- [X] T042 [US4] Populate compatibility impact notes in specs/161-host-performance-lane-ledger/readiness/compatibility-ledger.md, including Feature161HostLaneReadiness public surface details and no runtime rendering behavior change
- [X] T043 [US4] Populate package validation notes in specs/161-host-performance-lane-ledger/readiness/package-validation.md with Rendering.Harness, Testing.Tests, Package.Tests, FSI transcript, and surface-drift outcomes
- [X] T044 [US4] Populate regression preservation evidence in specs/161-host-performance-lane-ledger/readiness/regression-validation.md for Feature 155 correctness, Feature 157 damage-scissored readiness, Feature 158 readback-free timing separation, Feature 159 reuse/promotion readiness, Feature 160 throughput readiness, full-redraw fallback, unsupported-host behavior, package validation, and public-surface drift
- [X] T045 [US4] Populate reviewer closeout summary in specs/161-host-performance-lane-ledger/readiness/validation-summary.md with lane status, release-ready status, host facts, accepted and excluded counts, unsupported-host result, prior gate status, full validation, compatibility, package validation, regression validation, artifact paths, non-generalized lanes, remaining blockers, 5-minute reviewer checklist, and `performance-not-accepted`
- [X] T046 [US4] Re-run and record the compositor host lane FSI authoring transcript from T008 in specs/161-host-performance-lane-ledger/readiness/fsi/compositor-host-lane-authoring.fsx after implementation
- [X] T047 [US4] Re-run and record the host lane readiness helper FSI authoring transcript from T009 in specs/161-host-performance-lane-ledger/readiness/fsi/feature161-host-lane-readiness-authoring.fsx after implementation
- [X] T048 [US4] Run `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature161"` and record the result in specs/161-host-performance-lane-ledger/readiness/package-validation.md

**Checkpoint**: User Story 4 provides the reviewer entry point and package-visible validation coverage for Feature 161 closeout.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate package surface, collect readiness evidence, and close Feature 161 against the quickstart.

- [X] T049 [P] Update FS.GG.UI.Testing surface evidence for Feature161HostLaneReadiness in specs/161-host-performance-lane-ledger/readiness/fsi/FS.GG.UI.Testing.txt after src/Testing/Testing.fsi changes
- [X] T050 [P] Update Rendering.Harness compositor surface evidence for Feature 161 command and readiness signatures in specs/161-host-performance-lane-ledger/readiness/fsi/Rendering.Harness.Compositor.txt after tests/Rendering.Harness/Compositor.fsi changes
- [X] T051 [P] Update Rendering.Harness performance surface evidence for Feature 161 exclusion reason tokens in specs/161-host-performance-lane-ledger/readiness/fsi/Rendering.Harness.Perf.txt after tests/Rendering.Harness/Perf.fsi changes
- [X] T052 Run `dotnet build FS.GG.Rendering.slnx --no-restore` and record the result in specs/161-host-performance-lane-ledger/readiness/package-validation.md
- [X] T053 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature161"` and record the result in specs/161-host-performance-lane-ledger/readiness/package-validation.md
- [X] T054 Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-restore --filter "Feature161"` and record the result in specs/161-host-performance-lane-ledger/readiness/package-validation.md
- [X] T055 Run `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature161"` and record the result in specs/161-host-performance-lane-ledger/readiness/package-validation.md
- [X] T056 Run the lane-scoped performance evidence command from quickstart.md and record accepted, excluded, host-fact, and summary artifacts under specs/161-host-performance-lane-ledger/readiness/lane-ledger/
- [X] T057 Run the unsupported-host validation command from quickstart.md and record zero accepted lane-scoped performance artifacts in specs/161-host-performance-lane-ledger/readiness/lane-ledger/unsupported/README.md
- [X] T058 Run `dotnet test FS.GG.Rendering.slnx --no-restore` and record full validation status, command, duration, and output artifact location in specs/161-host-performance-lane-ledger/readiness/full-validation/validation.md
- [X] T059 Run final `compositor-readiness --feature 161` assembly and verify the reviewer entry point plus 5-minute decision checklist at specs/161-host-performance-lane-ledger/readiness/validation-summary.md
- [X] T060 Update Feature 155, Feature 157, Feature 158, Feature 159, Feature 160, unsupported-host, package, and public-surface preservation evidence in specs/161-host-performance-lane-ledger/readiness/regression-validation.md after final validation
- [X] T061 Update the Feature 161 tracker line and closeout notes in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md with SC-010 closeout evidence and any bounded deferrals

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **US1 Host Facts (Phase 3)**: Depends on Foundational. This is the MVP.
- **US2 Claim Scope (Phase 4)**: Depends on Foundational and can proceed alongside US1 after shared Compositor.fs edit coordination.
- **US3 Fail Closed (Phase 5)**: Depends on US1 host fact classification and US2 lane identity/rejection rules.
- **US4 Reviewer Evidence (Phase 6)**: Depends on US1-US3 summary fields; package compatibility tests can start after Foundational.
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Start after Foundational. No dependency on other stories.
- **US2 (P1)**: Start after Foundational. No dependency on accepted host fact implementation, but final claim rendering uses US1 facts.
- **US3 (P1)**: Start after US1 and US2 define fact completeness and lane rejection behavior.
- **US4 (P2)**: Start readiness/package tests after Foundational; final summary implementation depends on US1-US3 outputs.

### Within Each User Story

- FSI authoring transcripts and failing tests before implementation.
- `.fsi` or package-visible signature changes before `.fs` implementation.
- Pure MVU/status decisions before CLI filesystem publication.
- CLI publication before readiness artifact closeout.
- Story-specific test command before moving to the next priority.

### Parallel Opportunities

- T002, T003, and T004 can run in parallel after T001.
- T006 and T007 can run in parallel with T005 because they touch different signature files.
- T010, T018, T027, T036, and T038 can be drafted in parallel after Foundational if compile-file entries are coordinated.
- T049, T050, and T051 can run in parallel after public surface decisions are known.
- Focused harness, Testing helper, and Package compatibility test execution in T053-T055 can run in parallel once implementation is complete.

---

## Parallel Example: User Story 1

```text
Task: "Create failing host fact completeness, missing fact, contradictory fact, stale fact, unreadable fact, and historical P7 evidence status tests in tests/Rendering.Harness.Tests/Feature161HostLaneFactTests.fs"
Task: "Create and record the pre-implementation Testing helper FSI authoring transcript for the T007 signatures in specs/161-host-performance-lane-ledger/readiness/fsi/feature161-host-lane-readiness-authoring.fsx before adding Testing.fs implementations"
```

## Parallel Example: User Story 2

```text
Task: "Create failing lane claim scope, non-generalized lane, cross-lane aggregation rejection, refresh behavior mismatch, package mismatch, scenario mismatch, policy mismatch, run identity mismatch, environment-limit mismatch, and load-comparability tests in tests/Rendering.Harness.Tests/Feature161LaneLedgerTests.fs"
Task: "Add tests/Rendering.Harness.Tests/Feature161LaneLedgerTests.fs to the compile list before Feature160FocusedLaneTests.fs in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj"
```

## Parallel Example: User Story 3

```text
Task: "Create failing package-visible host lane readiness helper tests for accepted, rejected, fallback-only, environment-limited, blocked, missing facts, zero unsupported artifacts, noisy timing, prior-gate blocked, and overclaimed performance status in tests/Testing.Tests/Feature161HostLaneReadinessTests.fs"
Task: "Add tests/Testing.Tests/Feature161HostLaneReadinessTests.fs to the compile list before Program.fs in tests/Testing.Tests/Testing.Tests.fsproj"
```

## Parallel Example: User Story 4

```text
Task: "Create failing readiness package tests for validation-summary links, lane-ledger summary links, host-fact links, excluded evidence links, unsupported-host zero accepted artifacts, prior P7 gate links, full-validation links, compatibility links, package links, regression links, 5-minute reviewer checklist fields, and `performance-not-accepted` in tests/Rendering.Harness.Tests/Feature161ReadinessPackageTests.fs"
Task: "Create failing package compatibility tests for compatibility-ledger.md, package-validation.md, regression-validation.md, validation-summary.md, fsi surface evidence, and Feature161HostLaneReadiness surface notes in tests/Package.Tests/Feature161CompatibilityTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundational contracts.
3. Complete Phase 3 US1 host fact tests and implementation.
4. Validate US1 with `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature161&HostLaneFact"`.
5. Stop and review host fact evidence before adding claim scoping and fail-closed behavior.

### Incremental Delivery

1. Add US1 host fact capture and prove complete or excluded ledger records.
2. Add US2 claim scoping and prove cross-lane aggregation is impossible.
3. Add US3 fail-closed behavior and package-visible helper validation.
4. Add US4 reviewer package and compatibility evidence.
5. Complete polish validation from quickstart.md and readiness artifacts.

### Parallel Team Strategy

1. One developer owns shared Compositor.fsi/Compositor.fs contracts to avoid F# signature conflicts.
2. A second developer writes harness tests in tests/Rendering.Harness.Tests/.
3. A third developer writes Testing and Package tests in tests/Testing.Tests/ and tests/Package.Tests/.
4. After contracts stabilize, CLI publication and readiness rendering can proceed with file-level coordination.

## Notes

- [P] tasks are safe to parallelize only when their named files do not conflict with active edits.
- Synthetic fixtures are allowed only for rejection/helper tests, must include `Synthetic` in the test name, and must include a source comment explaining why real GL evidence is not used.
- Feature 161 can accept host-lane scoping only for a named lane; the shipped compositor performance claim remains `performance-not-accepted` unless same-profile timing is not noisy, Feature 159 reuse/promotion counters are net-positive, Feature 160 throughput is accepted, and Feature 161 host-lane facts are complete for the claimed lane.
