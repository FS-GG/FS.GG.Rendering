# Tasks: Same-Profile Timing Evidence

**Input**: Design documents from `specs/156-same-profile-timing/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by the specification and repository constitution. Write focused tests before implementation tasks where the task changes behavior.

**Organization**: Tasks are grouped by user story so each story can be independently validated.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User-story label, used only inside user-story phases.
- Every task includes exact repository-relative file paths.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish Feature 156 readiness locations, placeholder evidence files, and compile registrations.

- [X] T001 Create Feature 156 readiness directory placeholders in specs/156-same-profile-timing/readiness/timing/scenarios/.gitkeep, specs/156-same-profile-timing/readiness/timing/raw/.gitkeep, specs/156-same-profile-timing/readiness/timing/unsupported/.gitkeep, and specs/156-same-profile-timing/readiness/fsi/.gitkeep
- [X] T002 [P] Add timing summary placeholder with policy, scenario table, artifact, and claim-status headings in specs/156-same-profile-timing/readiness/timing/summary.md
- [X] T003 [P] Add readiness closeout placeholders in specs/156-same-profile-timing/readiness/compatibility-ledger.md, specs/156-same-profile-timing/readiness/package-validation.md, specs/156-same-profile-timing/readiness/regression-validation.md, and specs/156-same-profile-timing/readiness/validation-summary.md
- [X] T004 [P] Add FSI evidence placeholders in specs/156-same-profile-timing/readiness/fsi/compositor-performance-authoring.fsx, specs/156-same-profile-timing/readiness/fsi/compositor-performance-authoring.log, specs/156-same-profile-timing/readiness/fsi/compositor-readiness-authoring.fsx, and specs/156-same-profile-timing/readiness/fsi/compositor-readiness-authoring.log
- [X] T005 [P] Create stub Feature 156 test modules in tests/Rendering.Harness.Tests/Feature156TimingEvidenceTests.fs, tests/Rendering.Harness.Tests/Feature156ReadinessPackageTests.fs, tests/SkiaViewer.Tests/Feature156CompositorTimingTests.fs, tests/Package.Tests/Feature156CompatibilityTests.fs, and tests/Testing.Tests/Feature156TimingHelperTests.fs, then register them in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj, tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, tests/Package.Tests/Package.Tests.fsproj, and tests/Testing.Tests/Testing.Tests.fsproj
- [X] T006 Declare Feature 156 constants, accepted profile `probe-08a47c01`, readiness paths, required scenario ids, and policy id in tests/Rendering.Harness/Compositor.fsi

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define shared timing evidence vocabulary, public surfaces, and command names before user-story implementation.

**Critical**: No user-story implementation should begin until these contracts and expected failing tests can be added against stable names.

- [X] T007 Implement Feature 156 constants, readiness path helpers, accepted profile metadata, required scenario ids, and policy id in tests/Rendering.Harness/Compositor.fs
- [X] T008 Add Feature 156 timing sample, distribution, policy, verdict, and percentile helper declarations in tests/Rendering.Harness/Perf.fsi
- [X] T009 Add Feature 156 MVU/effect-boundary and renderer declarations for `Model`, `Msg`, `Effect`, `init`, `update`, scenario reports, timing summary, compatibility ledger, package validation, regression validation, readiness summary, and edge interpreter requests in tests/Rendering.Harness/Compositor.fsi
- [X] T010 Add package-visible timing helper declarations for Feature 156 summary/verdict assertions in src/Testing/Testing.fsi
- [X] T011 Add only the necessary timing-path selection or proof-overhead declarations for Feature 156 in src/SkiaViewer/SkiaViewer.fsi and src/SkiaViewer/CompositorProof.fsi
- [X] T012 [P] Add Feature 156 FSI transcript coverage expectations for timing policy, summary, readiness, and Testing helpers in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T013 Add `isFeature156` detection and `compositor-performance --feature 156` placeholder routing in tests/Rendering.Harness/Cli.fs
- [X] T014 [P] Record the Feature 156 validation plan, real-evidence expectations, synthetic-disclosure rule, unsupported-host check, and focused-test matrix in specs/156-same-profile-timing/readiness/validation-plan.md

**Checkpoint**: Feature 156 names, signatures, command route, MVU/effect boundary, and validation expectations are stable enough for failing tests.

---

## Phase 3: User Story 1 - Compare Timing on One Accepted Host Profile (Priority: P1) - MVP

**Goal**: Collect comparable full-redraw and damage-scoped timing samples on Feature 155 accepted host profile `probe-08a47c01`.

**Independent Test**: Run `compositor-performance --feature 156 --profile probe-08a47c01 --policy same-profile-live-threshold-v2 --warmup 3 --repetitions 5` and verify at least five required scenarios contain both measured paths, warmup counts, sample counts, p50, p95, p99, confidence decision, and artifact paths.

### Tests for User Story 1

Write these tests first and verify they fail before implementation.

- [X] T015 [US1] Add failing command-contract, option-default, required-scenario, warmup, repetition, and distribution-field tests in tests/Rendering.Harness.Tests/Feature156TimingEvidenceTests.fs
- [X] T016 [P] [US1] Add failing full-redraw versus damage-scoped timing path selection tests in tests/SkiaViewer.Tests/Feature156CompositorTimingTests.fs
- [X] T017 [US1] Run the failing US1 focused tests and record expected failures in specs/156-same-profile-timing/readiness/timing/scenarios/validation.md

### Implementation for User Story 1

- [X] T018 [US1] Implement Feature 156 sample distribution types, finite-duration validation, p50, p95, and p99 calculations in tests/Rendering.Harness/Perf.fs
- [X] T019 [US1] Implement `same-profile-live-threshold-v2` noise-band calculation and positive-scenario rule in tests/Rendering.Harness/Perf.fs
- [X] T020 [US1] Implement Feature 156 required scenario definitions and scenario/path report renderers in tests/Rendering.Harness/Compositor.fs
- [X] T021 [US1] Implement Feature 156 timing workflow `init` and pure `update` transitions for host profile binding, policy declaration, warmup, path measurement, scenario evaluation, and summary publication in tests/Rendering.Harness/Compositor.fs
- [X] T022 [US1] Implement required public timing-path selection or proof-overhead support in src/SkiaViewer/SkiaViewer.fs and src/SkiaViewer/CompositorProof.fs
- [X] T023 [US1] Wire `compositor-performance --feature 156` parsing for `--profile`, `--policy`, `--warmup`, `--repetitions`, `--scenario`, `--json`, and `--out` in tests/Rendering.Harness/Cli.fs
- [X] T024 [US1] Implement the Feature 156 edge interpreter for host probing, full-redraw warmup, damage-scoped warmup, path measurement, raw CSV/JSON output, and artifact paths in tests/Rendering.Harness/Cli.fs
- [X] T025 [US1] Generate same-profile timing evidence for `timing/localized-update`, `timing/no-change`, `timing/movement-old-new`, `timing/overlap`, and `timing/edge-clipping` under specs/156-same-profile-timing/readiness/timing/scenarios/ and specs/156-same-profile-timing/readiness/timing/raw/
- [X] T026 [US1] Add Feature 156 timing authoring transcript and command log in specs/156-same-profile-timing/readiness/fsi/compositor-performance-authoring.fsx and specs/156-same-profile-timing/readiness/fsi/compositor-performance-authoring.log
- [X] T027 [US1] Run US1 focused tests and timing quickstart command, then record accepted, rejected, or environment-limited results in specs/156-same-profile-timing/readiness/timing/scenarios/validation.md

**Checkpoint**: User Story 1 is independently testable and produces comparable same-profile timing evidence for the five required scenarios.

---

## Phase 4: User Story 2 - Reject Noisy, Mixed, or Incomplete Evidence (Priority: P1)

**Goal**: Fail closed for cross-profile, stale, unreadable, duplicated, incomplete, noisy, environment-limited, limited, or non-beneficial timing evidence.

**Independent Test**: Feed the evaluator cross-profile samples, missing repetitions, noisy distributions, duplicate artifacts, unreadable raw samples, readback-dominated measurements, and non-beneficial results, then verify each case rejects with a reviewer-visible reason and cannot support a positive timing decision.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T028 [US2] Add failing cross-profile, mixed-run, incomplete-sample, noisy, non-beneficial, limited-overhead, and environment-limited policy tests in tests/Rendering.Harness.Tests/Feature156TimingEvidenceTests.fs
- [X] T029 [P] [US2] Add failing package-visible timing helper rejection tests in tests/Testing.Tests/Feature156TimingHelperTests.fs
- [X] T030 [P] [US2] Add failing stale, duplicated, missing, and unreadable artifact package tests in tests/Rendering.Harness.Tests/Feature156ReadinessPackageTests.fs
- [X] T031 [US2] Run the failing US2 focused tests and record expected failures in specs/156-same-profile-timing/readiness/timing/rejection-validation.md

### Implementation for User Story 2

- [X] T032 [US2] Implement same-profile validation for host profile, display environment, renderer identity, package version, scenario definition, and run identity in tests/Rendering.Harness/Compositor.fs
- [X] T033 [US2] Implement fail-closed verdict mapping for `noisy`, `non-beneficial`, `incomplete`, `rejected`, `limited`, and `environment-limited` evidence in tests/Rendering.Harness/Compositor.fs
- [X] T034 [US2] Implement raw sample artifact freshness, readability, duplication, and path-link validation in tests/Rendering.Harness/Cli.fs
- [X] T035 [US2] Implement Feature 156 Testing helper behavior for accepted, rejected, noisy, non-beneficial, incomplete, limited, and environment-limited summaries in src/Testing/Testing.fs
- [X] T036 [US2] Generate rejection-only fixture outputs and scenario reports under specs/156-same-profile-timing/readiness/timing/scenarios/ and specs/156-same-profile-timing/readiness/timing/raw/
- [X] T037 [US2] Add `Synthetic` test names and `// SYNTHETIC:` comments for rejection-only fixtures in tests/Rendering.Harness.Tests/Feature156TimingEvidenceTests.fs, tests/Rendering.Harness.Tests/Feature156ReadinessPackageTests.fs, and tests/Testing.Tests/Feature156TimingHelperTests.fs
- [X] T038 [US2] Run US2 focused tests and record fail-closed coverage in specs/156-same-profile-timing/readiness/timing/rejection-validation.md

**Checkpoint**: User Story 2 is independently testable and no weak or mixed evidence can produce a positive timing decision.

---

## Phase 5: User Story 3 - Publish a Reviewable Timing Evidence Package (Priority: P2)

**Goal**: Publish one reviewer-facing timing summary that links scenarios, distributions, artifacts, host identity, noise policy, limitations, compatibility impact, and final performance status.

**Independent Test**: Open specs/156-same-profile-timing/readiness/timing/summary.md and verify a reviewer can find scenario verdicts, full-redraw and damage-scoped distributions, host profile, policy, artifact paths, rejection reasons, overhead disclosure, remaining gates, and `performance-not-accepted` from one entry point.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T039 [US3] Add failing timing summary package tests for required files, scenario table fields, artifact links, rejection reasons, overhead disclosure, remaining gates, and claim status in tests/Rendering.Harness.Tests/Feature156ReadinessPackageTests.fs
- [X] T040 [P] [US3] Add failing compatibility ledger, package validation, public-surface drift, and performance-claim boundary tests in tests/Package.Tests/Feature156CompatibilityTests.fs
- [X] T041 [P] [US3] Add failing Feature 156 FSI transcript assertions for timing policy authoring, readiness authoring, and Testing helpers in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T042 [US3] Run the failing US3 focused tests and record expected failures in specs/156-same-profile-timing/readiness/validation-summary.md

### Implementation for User Story 3

- [X] T043 [US3] Review Feature 156 renderer declarations in tests/Rendering.Harness/Compositor.fsi and add any missing summary-package declarations before implementing renderer bodies
- [X] T044 [US3] Implement Feature 156 timing summary, scenario report, compatibility ledger, package validation, regression validation, and readiness renderers in tests/Rendering.Harness/Compositor.fs
- [X] T045 [US3] Wire `compositor-readiness --feature 156 --out <dir>` to assemble validation-summary.md, compatibility-ledger.md, package-validation.md, regression-validation.md, timing/summary.md, scenario reports, raw samples, and unsupported-host links in tests/Rendering.Harness/Cli.fs
- [X] T046 [US3] Emit per-scenario markdown reports and raw sample links for Feature 156 in tests/Rendering.Harness/Cli.fs
- [X] T047 [US3] Generate or refresh the reviewer timing summary in specs/156-same-profile-timing/readiness/timing/summary.md
- [X] T048 [US3] Add Feature 156 readiness authoring transcript and command log in specs/156-same-profile-timing/readiness/fsi/compositor-readiness-authoring.fsx and specs/156-same-profile-timing/readiness/fsi/compositor-readiness-authoring.log
- [X] T049 [US3] Generate or refresh compatibility, package, regression, and validation summaries in specs/156-same-profile-timing/readiness/compatibility-ledger.md, specs/156-same-profile-timing/readiness/package-validation.md, specs/156-same-profile-timing/readiness/regression-validation.md, and specs/156-same-profile-timing/readiness/validation-summary.md
- [X] T050 [US3] Run US3 focused tests and readiness quickstart command, then record the reviewer-summary outcome and under-5-minute reviewer determination check in specs/156-same-profile-timing/readiness/validation-summary.md

**Checkpoint**: User Story 3 publishes one reviewable timing evidence package with all claims and limitations traceable from the summary.

---

## Phase 6: User Story 4 - Preserve Safe Correctness and Unsupported-Host Behavior (Priority: P2)

**Goal**: Keep Feature 155 correctness acceptance, full-redraw fallback, and unsupported-host fail-closed behavior unchanged while adding Feature 156 timing evidence.

**Independent Test**: Run accepted-host and unsupported-host readiness checks before and after timing collection, then confirm proof/parity correctness remains tied to Feature 155, unsupported hosts record zero accepted performance artifacts, and the shipped P7 performance claim remains `performance-not-accepted`.

### Tests for User Story 4

Write these tests first and verify they fail before implementation.

- [X] T051 [US4] Add failing unsupported-host timing regression tests for environment-limited output, under-2-minute completion, and zero accepted performance artifacts in tests/Rendering.Harness.Tests/Feature156ReadinessPackageTests.fs
- [X] T052 [P] [US4] Add failing correctness-boundary and fallback-preservation tests in tests/SkiaViewer.Tests/Feature156CompositorTimingTests.fs
- [X] T053 [P] [US4] Add failing Feature 155 baseline preservation and shipped-claim boundary tests in tests/Package.Tests/Feature156CompatibilityTests.fs
- [X] T054 [US4] Run the failing US4 focused tests and record expected failures in specs/156-same-profile-timing/readiness/timing/unsupported/validation.md

### Implementation for User Story 4

- [X] T055 [US4] Implement unsupported-host `environment-limited` branch for `compositor-performance --feature 156` with zero accepted artifacts in tests/Rendering.Harness/Cli.fs
- [X] T056 [US4] Preserve and render Feature 155 proof/parity baseline references, correctness status, fallback status, and performance-claim boundary in tests/Rendering.Harness/Compositor.fs
- [X] T057 [US4] Generate unsupported-host timing output and validation notes in specs/156-same-profile-timing/readiness/timing/unsupported/README.md and specs/156-same-profile-timing/readiness/timing/unsupported/validation.md
- [X] T058 [US4] Update the P7 report with Feature 156 timing status, remaining Feature 157, Feature 158, Feature 159, and Feature 161 performance gates, and Feature 160 throughput follow-up status in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md
- [X] T059 [US4] Run US4 focused tests and record correctness/fallback preservation evidence in specs/156-same-profile-timing/readiness/regression-validation.md

**Checkpoint**: User Story 4 proves timing work does not weaken correctness acceptance, fallback, unsupported-host behavior, or shipped performance-claim boundaries.

---

## Phase 7: Polish & Validation

**Purpose**: Final validation, package evidence, quickstart evidence, and task closeout.

- [X] T060 Run `dotnet build FS.GG.Rendering.slnx --no-restore` and record the result in specs/156-same-profile-timing/readiness/package-validation.md
- [X] T061 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature156 --no-build` and record the result in specs/156-same-profile-timing/readiness/regression-validation.md
- [X] T062 Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature156 --no-build` and record the result in specs/156-same-profile-timing/readiness/regression-validation.md
- [X] T063 Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature156 --no-build` and `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature156 --no-build`, then record results in specs/156-same-profile-timing/readiness/package-validation.md
- [X] T064 Run same-profile timing quickstart command or record host limitation in specs/156-same-profile-timing/readiness/timing/summary.md
- [X] T065 Run unsupported-host quickstart command with display variables unset, measure elapsed time, and record under-2-minute and zero-accepted-artifact evidence in specs/156-same-profile-timing/readiness/timing/unsupported/validation.md
- [X] T066 Run `compositor-readiness --feature 156 --out specs/156-same-profile-timing/readiness` and record final package links in specs/156-same-profile-timing/readiness/validation-summary.md
- [X] T067 Run `dotnet test FS.GG.Rendering.slnx --no-restore` and record the result in specs/156-same-profile-timing/readiness/regression-validation.md
- [X] T068 Run `dotnet fsi scripts/refresh-surface-baselines.fsx` when public .fsi changes occur, verify committed surface baselines or document no public drift, then run package surface and pack-local validation or record tooling limitations in specs/156-same-profile-timing/readiness/package-validation.md
- [X] T069 Run `git diff --check` and record whitespace validation in specs/156-same-profile-timing/readiness/regression-validation.md
- [X] T070 Mark completed tasks in specs/156-same-profile-timing/tasks.md after all selected validation evidence is recorded, and list every Feature 156 `Synthetic` test or fixture in the pull request description

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user-story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational and is the MVP timing collection path.
- **User Story 2 (Phase 4)**: Depends on Foundational; can be developed with rejection fixtures but depends on US1 primitives for final integration.
- **User Story 3 (Phase 5)**: Depends on US1 and US2 evidence shapes to publish a complete summary.
- **User Story 4 (Phase 6)**: Depends on US1 command routing and US3 package assembly for final unsupported-host and correctness-boundary evidence.
- **Polish (Phase 7)**: Depends on all selected user stories.

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational; delivers the MVP same-profile timing collection flow.
- **US2 (P1)**: Can start after Foundational with fixtures; final rejection coverage uses US1 distribution and artifact shapes.
- **US3 (P2)**: Requires US1 timing output and US2 rejection reasons to publish a reviewable package.
- **US4 (P2)**: Requires US1 command routing and US3 package links to prove unsupported-host and correctness preservation.

### Dependency Graph

```text
Phase 1 Setup
  -> Phase 2 Foundational
      -> US1 Same-Profile Timing Collection (MVP)
      -> US2 Fail-Closed Evidence Policy
US1 + US2 -> US3 Reviewable Timing Package
US1 + US3 -> US4 Correctness and Unsupported-Host Preservation
US1/US2/US3/US4 -> Phase 7 Polish
```

### Within Each User Story

- Tests first, and they should fail before implementation.
- New Feature 156 test files are registered in the owning `.fsproj` before focused test execution.
- `.fsi`, public contracts, and FSI transcript expectations come before `.fs` bodies for public or observable surfaces.
- Pure validators, distribution calculations, and renderers come before CLI edge interpreters.
- Command behavior comes before durable readiness artifact generation.
- Story validation command and readiness artifact update are the final tasks in each story.

## Parallel Opportunities

- Setup tasks T002-T005 can run in parallel after T001 creates readiness directories.
- Foundational tasks T010, T012, and T014 can run in parallel with harness contract work after T006.
- US1 tests T015 and T016 can run in parallel because they touch different test projects.
- US1 implementation can split T018-T019 timing primitives from T022 SkiaViewer support, then converge on T023-T024 CLI wiring.
- US2 tests T028-T030 can run in parallel because they touch different test files.
- US2 implementation can split T032-T033 evaluator work from T034 CLI artifact validation and T035 Testing helper work.
- US3 tests T039-T041 can run in parallel because they touch different test files.
- US3 implementation can split T044 renderer work from T048 transcript authoring after T043 confirms `.fsi` declarations are complete.
- US4 tests T051-T053 can run in parallel because they touch different test files.
- Polish validation tasks T061-T063 can run in parallel after T060 build succeeds; T066-T070 must run after readiness files exist.

## Parallel Example: User Story 1

```bash
Task: "Add failing command-contract, option-default, required-scenario, warmup, repetition, and distribution-field tests in tests/Rendering.Harness.Tests/Feature156TimingEvidenceTests.fs"
Task: "Add failing full-redraw versus damage-scoped timing path selection tests in tests/SkiaViewer.Tests/Feature156CompositorTimingTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "Add failing cross-profile, mixed-run, incomplete-sample, noisy, non-beneficial, limited-overhead, and environment-limited policy tests in tests/Rendering.Harness.Tests/Feature156TimingEvidenceTests.fs"
Task: "Add failing package-visible timing helper rejection tests in tests/Testing.Tests/Feature156TimingHelperTests.fs"
Task: "Add failing stale, duplicated, missing, and unreadable artifact package tests in tests/Rendering.Harness.Tests/Feature156ReadinessPackageTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "Add failing compatibility ledger, package validation, public-surface drift, and performance-claim boundary tests in tests/Package.Tests/Feature156CompatibilityTests.fs"
Task: "Add failing Feature 156 FSI transcript assertions for timing policy authoring, readiness authoring, and Testing helpers in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "Add failing correctness-boundary and fallback-preservation tests in tests/SkiaViewer.Tests/Feature156CompositorTimingTests.fs"
Task: "Add failing Feature 155 baseline preservation and shipped-claim boundary tests in tests/Package.Tests/Feature156CompatibilityTests.fs"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup and Phase 2 foundational contracts.
2. Complete Phase 3 User Story 1.
3. Stop and validate the MVP with Feature 156 focused Rendering.Harness and SkiaViewer tests plus the same-profile timing quickstart command.
4. Preserve `performance-not-accepted` unless later report-defined performance gates are also present.

### Incremental Delivery

1. Setup + Foundational -> stable Feature 156 names, signatures, command route, and validation plan.
2. US1 -> comparable same-profile timing evidence for five required scenarios.
3. US2 -> fail-closed policy for weak, mixed, incomplete, noisy, limited, or non-beneficial evidence.
4. US3 -> one reviewable timing package and package-facing validation.
5. US4 -> unsupported-host regression, Feature 155 correctness preservation, report update, and final closeout.

### Validation Strategy

1. Run failing-focused tests before each story implementation.
2. Run focused `Feature156` filters after each story.
3. Run quickstart same-profile, unsupported-host, and readiness commands before closeout.
4. Run broad solution tests, package/surface validation, and `git diff --check` before marking tasks complete.
