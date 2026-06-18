# Tasks: Separate Proof Readback From Timing

**Input**: Design documents from `specs/158-separate-proof-timing/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by the feature specification and repository constitution. Write focused tests before implementation tasks where behavior, `.fsi` surface, package validation, or readiness semantics change.

**Organization**: Tasks are grouped by user story so each story can be independently implemented and validated.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User-story label, used only inside user-story phases.
- Every task includes exact repository-relative file paths.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish Feature 158 readiness locations, placeholder evidence files, and compile registrations.

- [X] T001 Create Feature 158 readiness directory placeholders in specs/158-separate-proof-timing/readiness/timing/scenarios/.gitkeep, specs/158-separate-proof-timing/readiness/timing/raw/.gitkeep, specs/158-separate-proof-timing/readiness/timing/excluded/.gitkeep, specs/158-separate-proof-timing/readiness/timing/unsupported/.gitkeep, specs/158-separate-proof-timing/readiness/proof-probes/.gitkeep, specs/158-separate-proof-timing/readiness/fsi/.gitkeep, and specs/158-separate-proof-timing/readiness/surface-baselines/.gitkeep
- [X] T002 [P] Add initial timing evidence placeholders with measurement-policy, included-sample, excluded-sample, unsupported-host, and claim-status headings in specs/158-separate-proof-timing/readiness/timing/summary.md and specs/158-separate-proof-timing/readiness/timing/summary.json
- [X] T003 [P] Add closeout placeholders for compatibility, package validation, regression validation, and reviewer summary in specs/158-separate-proof-timing/readiness/compatibility-ledger.md, specs/158-separate-proof-timing/readiness/package-validation.md, specs/158-separate-proof-timing/readiness/regression-validation.md, and specs/158-separate-proof-timing/readiness/validation-summary.md
- [X] T004 [P] Add FSI authoring placeholders in specs/158-separate-proof-timing/readiness/fsi/compositor-performance-authoring.fsx, specs/158-separate-proof-timing/readiness/fsi/compositor-performance-authoring.log, specs/158-separate-proof-timing/readiness/fsi/compositor-readiness-authoring.fsx, and specs/158-separate-proof-timing/readiness/fsi/compositor-readiness-authoring.log
- [X] T005 [P] Add proof/probe placeholder indexes in specs/158-separate-proof-timing/readiness/proof-probes/README.md and specs/158-separate-proof-timing/readiness/timing/excluded/README.md
- [X] T006 [P] Register Feature 158 harness test modules tests/Rendering.Harness.Tests/Feature158MeasurementPolicyTests.fs and tests/Rendering.Harness.Tests/Feature158ReadinessPackageTests.fs in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T007 [P] Register Feature 158 package compatibility test module tests/Package.Tests/Feature158CompatibilityTests.fs, plus tests/Testing.Tests/Feature158TimingSeparationHelperTests.fs and tests/SkiaViewer.Tests/Feature158TimingProbeTests.fs only if Testing or SkiaViewer public helper tests are justified, in the owning .fsproj files

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define shared Feature 158 vocabulary, public surfaces, workflow contracts, command names, and failing semantic tests before user-story implementation.

**Critical**: No user-story implementation should begin until these contracts and expected failing tests are added and run against stable names.

- [X] T008 Declare Feature 158 constants, readiness paths, accepted profile `probe-08a47c01`, policy id `readback-free-timing-v1`, required timing scenario ids, and command aliases in tests/Rendering.Harness/Compositor.fsi
- [X] T009 Declare Feature 158 measurement workflow contracts for timing samples, excluded samples, proof/probe evidence, timing summaries, readiness summaries, MVU model, messages, effects, renderers, and status tokens in tests/Rendering.Harness/Compositor.fsi
- [X] T010 Declare measurement-policy, inclusion-status, exclusion-reason, sample-classification, and accepted-set helper signatures in tests/Rendering.Harness/Perf.fsi
- [X] T011 Add only justified package-visible Feature 158 helper declarations for measurement policy, exclusion reasons, readiness status, or probe evidence in src/Testing/Testing.fsi and src/SkiaViewer/SkiaViewer.fsi; if no helper is justified, record the no-new-helper decision in specs/158-separate-proof-timing/readiness/compatibility-ledger.md
- [X] T012 Add pre-implementation Feature 158 FSI authoring exercises in specs/158-separate-proof-timing/readiness/fsi/compositor-performance-authoring.fsx and specs/158-separate-proof-timing/readiness/fsi/compositor-readiness-authoring.fsx, capture expected type-check or failing logs in the paired .log files, and add transcript coverage expectations in tests/Package.Tests/FsiTranscriptCoverageTests.fs for any public or observable surface
- [X] T013 Add failing foundational semantic tests for FSI-visible Feature 158 constants, policy/status tokens, MVU `init`/`update` transitions, emitted effects, and CLI command contracts in tests/Rendering.Harness.Tests/Feature158MeasurementPolicyTests.fs and tests/Rendering.Harness.Tests/Feature158ReadinessPackageTests.fs; add package-facing semantic tests only for justified public helpers
- [X] T014 Run the failing foundational semantic tests and FSI authoring exercises, then record expected failures in specs/158-separate-proof-timing/readiness/regression-validation.md and specs/158-separate-proof-timing/readiness/fsi/
- [X] T015 Implement compile-safe Feature 158 constants, readiness path helpers, rejected/incomplete/environment-limited stubs, and placeholder routing for `compositor-performance --feature 158`, `compositor-performance --feature 158 --probe-readback`, and `compositor-readiness --feature 158` in tests/Rendering.Harness/Compositor.fs, tests/Rendering.Harness/Perf.fs, tests/Rendering.Harness/Cli.fs, src/Testing/Testing.fs only if Testing helpers were declared, and src/SkiaViewer/SkiaViewer.fs only if SkiaViewer helpers were declared

**Checkpoint**: Feature 158 names, signatures, command routes, MVU/effect boundary, and failing validation expectations are stable before implementation stubs or story behavior.

---

## Phase 3: User Story 1 - Measure Performance Without Forced Proof Readback (Priority: P1) - MVP

**Goal**: Collect representative performance timing samples whose accepted measured interval excludes validation readback while contaminated, missing-policy, and unverifiable samples are rejected with stable reasons.

**Independent Test**: Run the representative timing lane and verify every accepted timing sample declares `readback-free` or `readback-outside-measurement`, proof readback is outside the accepted measured sample set, and the shipped performance claim remains `performance-not-accepted`.

### Tests for User Story 1

Write these tests first and verify they fail before implementation.

- [X] T016 [US1] Add failing measurement-policy classifier tests for `readback-free`, `readback-outside-measurement`, `probe-readback-included`, `unverified`, `missing`, and `proof-readback-in-measured-interval` in tests/Rendering.Harness.Tests/Feature158MeasurementPolicyTests.fs
- [X] T017 [US1] Add failing timing-command contract tests for `compositor-performance --feature 158`, `--policy readback-free-timing-v1`, five required scenarios, raw sample fields, excluded-sample fields, and `performance-not-accepted` in tests/Rendering.Harness.Tests/Feature158MeasurementPolicyTests.fs
- [X] T018 [US1] Run the failing US1 focused tests and record expected failures in specs/158-separate-proof-timing/readiness/timing/excluded/validation.md

### Implementation for User Story 1

- [X] T019 [US1] Implement measurement-policy, inclusion-status, exclusion-reason, token parsing, token rendering, and sample classification helpers in tests/Rendering.Harness/Perf.fs
- [X] T020 [US1] Implement accepted timing set construction, finite non-negative duration validation, distribution preservation, and proof/probe exclusion checks in tests/Rendering.Harness/Perf.fs
- [X] T021 [US1] Implement Feature 158 timing sample, excluded sample, scenario report, accepted timing set, timing summary, and status records in tests/Rendering.Harness/Compositor.fs
- [X] T022 [US1] Implement same-profile, same-run, same-display, same-renderer, same-package, and same-scenario validation for Feature 158 summaries in tests/Rendering.Harness/Compositor.fs
- [X] T023 [US1] Implement Feature 158 raw sample CSV and JSON writing with policy, host profile, scenario id, inclusion status, exclusion reason, duration, and artifact path fields in tests/Rendering.Harness/Cli.fs
- [X] T024 [US1] Implement readback-free render/present timing collection that keeps screenshot/proof readback outside the accepted measured interval in tests/Rendering.Harness/Cli.fs
- [X] T025 [US1] Wire `compositor-performance --feature 158` parsing for `--out`, `--profile`, `--policy`, `--warmup`, `--repetitions`, `--scenario`, and `--json` in tests/Rendering.Harness/Cli.fs
- [X] T026 [US1] Implement Feature 158 scenario report, timing summary, excluded-sample report, unsupported-host report, and optional summary JSON renderers in tests/Rendering.Harness/Compositor.fs
- [X] T027 [US1] Run the US1 timing quickstart command and record included samples, excluded samples, required scenario coverage, and final measurement-separation status in specs/158-separate-proof-timing/readiness/timing/summary.md and specs/158-separate-proof-timing/readiness/timing/scenarios/validation.md

**Checkpoint**: User Story 1 is independently testable and delivers the MVP readback-free timing lane without accepting a shipped compositor performance claim.

---

## Phase 4: User Story 2 - Keep Proof Readback Available as an Explicit Probe (Priority: P1)

**Goal**: Preserve screenshot/readback proof and explicit probe paths while ensuring every readback-included proof or probe sample is labelled and excluded from performance acceptance.

**Independent Test**: Run an explicit probe path and confirm readback artifacts are produced, labelled as proof or probe evidence, linked from readiness, and excluded from accepted timing with `probe-run-excluded`.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T028 [US2] Add failing explicit probe exclusion tests for `probe-readback-included`, `probe-run-excluded`, proof/probe artifact links, and zero accepted performance samples in tests/Rendering.Harness.Tests/Feature158MeasurementPolicyTests.fs
- [X] T029 [P] [US2] If SkiaViewer public helpers are justified, add failing viewer-facing timing/probe token tests for package-visible measurement policy and probe classification helpers in tests/SkiaViewer.Tests/Feature158TimingProbeTests.fs; otherwise add a no-viewer-helper assertion to tests/Package.Tests/Feature158CompatibilityTests.fs
- [X] T030 [US2] Run the failing US2 focused tests and record expected failures in specs/158-separate-proof-timing/readiness/proof-probes/validation.md

### Implementation for User Story 2

- [X] T031 [US2] Implement Feature 158 proof/probe evidence, explicit probe run, readback artifact, probe exclusion, and failed-proof-readback records in tests/Rendering.Harness/Compositor.fs
- [X] T032 [US2] Implement `compositor-performance --feature 158 --probe-readback` parsing, default output resolution, and zero-performance-acceptance status in tests/Rendering.Harness/Cli.fs
- [X] T033 [US2] Implement explicit probe readback capture, proof/probe artifact publication, and host-profile diagnostics using the existing proof capture path in tests/Rendering.Harness/Cli.fs
- [X] T034 [US2] Implement probe exclusion report rendering for `probe-run-excluded`, `failed-proof-readback`, and cross-profile proof/probe evidence in tests/Rendering.Harness/Compositor.fs
- [X] T035 [US2] Wire `compositor-performance --feature 158 --probe-readback` proof/probe evidence publication under specs/158-separate-proof-timing/readiness/proof-probes/ in tests/Rendering.Harness/Cli.fs
- [X] T036 [US2] If SkiaViewer public helpers were justified and declared, implement the package-visible Feature 158 measurement policy and probe classification helpers in src/SkiaViewer/SkiaViewer.fs; otherwise document no viewer helper surface in specs/158-separate-proof-timing/readiness/compatibility-ledger.md
- [X] T037 [US2] Run the explicit probe quickstart command and record probe artifacts, exclusion reason, host profile, and zero accepted performance samples in specs/158-separate-proof-timing/readiness/proof-probes/README.md and specs/158-separate-proof-timing/readiness/timing/excluded/probe-run-excluded.md

**Checkpoint**: User Story 2 is independently testable and proof readback remains available without contaminating accepted timing.

---

## Phase 5: User Story 3 - Publish Comparable Readiness Evidence (Priority: P2)

**Goal**: Publish one reviewer-facing evidence package that distinguishes readback-free timing, proof/probe evidence, host profile, scenario coverage, exclusions, Feature 156 comparison, compatibility impact, and final claim status.

**Independent Test**: Open specs/158-separate-proof-timing/readiness/validation-summary.md and verify a reviewer can determine measurement policy, included samples, excluded samples, proof/probe links, host profile, Feature 156 comparison, and final performance claim status from one entry point.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T038 [US3] Add failing readiness package tests for required files, required summary fields, reviewer checklist fields, included timing links, excluded-sample links, proof/probe links, unsupported-host result, Feature 156 comparison, and `performance-not-accepted` in tests/Rendering.Harness.Tests/Feature158ReadinessPackageTests.fs
- [X] T039 [P] [US3] If Testing public helpers are justified, add failing package-visible readiness helper tests for accepted, rejected, fallback-only, and environment-limited measurement-separation summaries in tests/Testing.Tests/Feature158TimingSeparationHelperTests.fs; otherwise add a no-Testing-helper assertion to tests/Package.Tests/Feature158CompatibilityTests.fs
- [X] T040 [P] [US3] Add failing compatibility ledger, package validation, FSI transcript, public-surface drift, and performance-claim boundary tests in tests/Package.Tests/Feature158CompatibilityTests.fs
- [X] T041 [US3] Run the failing US3 focused tests and record expected failures in specs/158-separate-proof-timing/readiness/validation-summary.md

### Implementation for User Story 3

- [X] T042 [US3] If Testing public helpers were justified and declared, implement Feature 158 Testing helper records, status tokens, validation rules, and diagnostics in src/Testing/Testing.fs; otherwise document no Testing helper surface in specs/158-separate-proof-timing/readiness/compatibility-ledger.md
- [X] T043 [US3] Implement Feature 158 timing summary, validation summary, reviewer checklist, compatibility ledger, package validation, regression validation, unsupported-host, and Feature 156 comparison renderers in tests/Rendering.Harness/Compositor.fs
- [X] T044 [US3] Wire `compositor-readiness --feature 158 --out <dir>` to assemble timing, excluded, proof/probe, unsupported-host, compatibility, package, regression, FSI, and validation-summary files in tests/Rendering.Harness/Cli.fs
- [X] T045 [US3] Implement Feature 156 comparison classification as `supersedes`, `confirms`, or `contextualizes` based on same-profile timing status in tests/Rendering.Harness/Cli.fs and tests/Rendering.Harness/Compositor.fs
- [X] T046 [US3] Generate Feature 158 FSI authoring scripts and logs during readiness assembly in specs/158-separate-proof-timing/readiness/fsi/compositor-performance-authoring.fsx, specs/158-separate-proof-timing/readiness/fsi/compositor-performance-authoring.log, specs/158-separate-proof-timing/readiness/fsi/compositor-readiness-authoring.fsx, and specs/158-separate-proof-timing/readiness/fsi/compositor-readiness-authoring.log
- [X] T047 [US3] Emit machine-readable Feature 158 timing summary JSON with policy, host profile, included samples, excluded reasons, proof/probe links, unsupported-host status, and claim status in specs/158-separate-proof-timing/readiness/timing/summary.json
- [X] T048 [US3] Refresh package and FSI transcript expectations for accepted Feature 158 public surface changes in tests/Package.Tests/FsiTranscriptCoverageTests.fs and tests/Package.Tests/Feature158CompatibilityTests.fs, or assert no new package-visible helper surface when none is justified
- [X] T049 [US3] Run the readiness quickstart command and record reviewer-entry links, reviewer checklist completion result, under-5-minute inspection evidence, Feature 156 comparison, and final claim status in specs/158-separate-proof-timing/readiness/validation-summary.md

**Checkpoint**: User Story 3 publishes a comparable readiness package with all claims and limitations traceable from one summary.

---

## Phase 6: User Story 4 - Preserve Existing Safety Boundaries (Priority: P2)

**Goal**: Preserve Feature 155 correctness readiness, Feature 157 damage-scissored readiness, Feature 156 noisy timing context, full-redraw fallback, unsupported-host fail-closed behavior, package validation, and public compatibility boundaries.

**Independent Test**: Run focused proof, timing, unsupported-host, package, and compatibility checks before closeout and verify any consumer-visible drift is intentional and documented.

### Tests for User Story 4

Write these tests first and verify they fail before implementation.

- [X] T050 [US4] Add failing Feature 155 proof preservation, Feature 156 timing context, Feature 157 damage readiness preservation, and final claim boundary tests in tests/Rendering.Harness.Tests/Feature158ReadinessPackageTests.fs
- [X] T051 [P] [US4] Add failing unsupported-host tests for `environment-limited`, under-2-minute completion, zero accepted proof artifacts, and zero accepted performance artifacts in tests/Rendering.Harness.Tests/Feature158MeasurementPolicyTests.fs
- [X] T052 [P] [US4] Add failing public-surface baseline, compatibility ledger, package validation, and no-undocumented-drift tests in tests/Package.Tests/Feature158CompatibilityTests.fs
- [X] T053 [US4] Run the failing US4 focused tests and record expected failures in specs/158-separate-proof-timing/readiness/regression-validation.md

### Implementation for User Story 4

- [X] T054 [US4] Implement unsupported-host and unavailable-presentation fail-closed behavior for Feature 158 timing, probe, and readiness commands in tests/Rendering.Harness/Cli.fs
- [X] T055 [US4] Enforce cross-profile, display-environment, renderer-identity, package-version, run-identity, scenario-definition, and failed-proof-readback rejection in tests/Rendering.Harness/Perf.fs and tests/Rendering.Harness/Compositor.fs
- [X] T056 [US4] Preserve and render Feature 155 proof references, Feature 157 damage readiness references, Feature 156 noisy timing context, full-redraw fallback status, and remaining Feature 159 and Feature 161 gates in tests/Rendering.Harness/Compositor.fs
- [X] T057 [US4] Refresh public surface baselines when `.fsi` changes occur by updating tests/surface-baselines/FS.GG.UI.Testing.txt and tests/surface-baselines/FS.GG.UI.SkiaViewer.txt, mirroring accepted deltas into specs/158-separate-proof-timing/readiness/surface-baselines/FS.GG.UI.Testing.txt and specs/158-separate-proof-timing/readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt, or documenting no public surface drift in specs/158-separate-proof-timing/readiness/compatibility-ledger.md
- [X] T058 [US4] Update the Feature 158 tracker status, measurement-separation outcome, and remaining Feature 159 and Feature 161 performance gates in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md
- [X] T059 [US4] Run unsupported-host validation with display variables unset and record elapsed time, `environment-limited` status, zero accepted proof artifacts, and zero accepted performance artifacts in specs/158-separate-proof-timing/readiness/timing/unsupported/README.md and specs/158-separate-proof-timing/readiness/timing/unsupported/validation.md
- [X] T060 [US4] Run focused Feature 155, Feature 156, Feature 157, and Feature 158 regression checks and record correctness, timing, fallback, package, compatibility, and unsupported-host preservation evidence in specs/158-separate-proof-timing/readiness/regression-validation.md

**Checkpoint**: User Story 4 proves measurement separation does not weaken proof, fallback, unsupported-host, package, or public-surface boundaries.

---

## Phase 7: Polish & Validation

**Purpose**: Final validation, package evidence, quickstart evidence, and task closeout.

- [X] T061 Run `dotnet build FS.GG.Rendering.slnx --no-restore` and record the result in specs/158-separate-proof-timing/readiness/package-validation.md
- [X] T062 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-build --filter "Feature158"` and record the result in specs/158-separate-proof-timing/readiness/regression-validation.md
- [X] T063 Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-build --filter "Feature158"` and record the result in specs/158-separate-proof-timing/readiness/regression-validation.md
- [X] T064 Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-build --filter "Feature158"` and `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-build --filter "Feature158"` then record results in specs/158-separate-proof-timing/readiness/package-validation.md
- [X] T065 Run `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 158 --out specs/158-separate-proof-timing/readiness/timing --policy readback-free-timing-v1 --warmup 3 --repetitions 5 --json` and record timing artifacts in specs/158-separate-proof-timing/readiness/timing/summary.md
- [X] T066 Run `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 158 --probe-readback --out specs/158-separate-proof-timing/readiness/timing` and record proof/probe links in specs/158-separate-proof-timing/readiness/proof-probes/README.md
- [X] T067 Run `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-readiness --feature 158 --out specs/158-separate-proof-timing/readiness` and record final package links in specs/158-separate-proof-timing/readiness/validation-summary.md
- [X] T068 Run `env -u DISPLAY -u WAYLAND_DISPLAY dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 158 --out specs/158-separate-proof-timing/readiness/timing/unsupported` and record under-2-minute unsupported-host evidence in specs/158-separate-proof-timing/readiness/timing/unsupported/validation.md
- [X] T069 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-build --filter "Feature155|Feature156|Feature157|Feature158"` and record focused P7 regression results in specs/158-separate-proof-timing/readiness/regression-validation.md
- [X] T070 Run `dotnet test FS.GG.Rendering.slnx --no-restore`, run `git diff --check`, record final validation in specs/158-separate-proof-timing/readiness/regression-validation.md, and mark completed tasks in specs/158-separate-proof-timing/tasks.md after selected evidence is recorded

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user-story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational and is the MVP readback-free timing lane.
- **User Story 2 (Phase 4)**: Depends on Foundational; can be developed after US1 contracts and final validation depends on US1 timing acceptance rules.
- **User Story 3 (Phase 5)**: Depends on US1 and US2 evidence shapes to publish the complete readiness package.
- **User Story 4 (Phase 6)**: Depends on US1 command routing and US3 readiness assembly for unsupported-host, compatibility, and regression evidence.
- **Polish (Phase 7)**: Depends on all selected user stories.

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational; delivers the MVP measurement-separation path.
- **US2 (P1)**: Can start after Foundational with probe fixtures; final proof/probe exclusion validation depends on US1 classifier behavior.
- **US3 (P2)**: Requires US1 timing evidence and US2 proof/probe evidence to publish a comparable package.
- **US4 (P2)**: Requires US1 command routing and US3 package links to prove safety and compatibility preservation.

### Dependency Graph

```text
Phase 1 Setup
  -> Phase 2 Foundational
      -> US1 Readback-Free Timing (MVP)
      -> US2 Explicit Proof/Probe Readback
US1 + US2 -> US3 Comparable Readiness Evidence
US1 + US3 -> US4 Safety Boundary Preservation
US1/US2/US3/US4 -> Phase 7 Polish
```

### Within Each User Story

- Tests first, and they should fail before implementation.
- New Feature 158 test files are registered in the owning `.fsproj` before focused test execution.
- `.fsi`, public contracts, pre-implementation FSI authoring transcripts, and FSI transcript expectations come before `.fs` bodies for public or observable surfaces.
- Stateful or I/O-bearing behavior must add pure `Model`/`Msg`/`update` transition tests and edge interpreter/effect tests before implementation.
- Pure policy classifiers, workflow updates, and renderers come before CLI edge interpreters.
- Command behavior comes before durable readiness artifact generation.
- Story validation command and readiness artifact update are the final tasks in each story.

## Parallel Opportunities

- Setup tasks T002-T007 can run in parallel after T001 creates readiness directories.
- Foundational tasks T009-T011 can run in parallel after T008 establishes Feature 158 constants; T012-T014 run after the relevant `.fsi` declarations and before T015 implementation stubs or routing.
- US1 classifier tests and US1 command tests are in one file and should stay sequential, but US1 implementation can split Perf classification work in T019-T020 from Compositor summary work in T021-T022 before converging on CLI work in T023-T025.
- US2 tests T028-T029 can run in parallel because they touch different test projects or package-compatibility assertions.
- US2 implementation can split proof/probe modeling in T031 from CLI probe routing in T032-T033 and conditional viewer token implementation or no-helper documentation in T036.
- US3 tests T038-T040 can run in parallel because they touch different test files or package-compatibility assertions.
- US3 implementation can split conditional Testing helper work or no-helper documentation in T042 from Compositor renderer work in T043 and package transcript work in T048 before converging on readiness assembly in T044-T047.
- US4 tests T050-T052 can run in parallel because they touch different test files.
- Polish validation tasks T062-T064 can run in parallel after T061 build succeeds; T067-T070 must run after readiness files exist.

## Parallel Example: User Story 2

```bash
Task: "Add failing explicit probe exclusion tests for probe-readback-included, probe-run-excluded, proof/probe artifact links, and zero accepted performance samples in tests/Rendering.Harness.Tests/Feature158MeasurementPolicyTests.fs"
Task: "If SkiaViewer public helpers are justified, add failing viewer-facing timing/probe token tests in tests/SkiaViewer.Tests/Feature158TimingProbeTests.fs; otherwise add a no-viewer-helper assertion to tests/Package.Tests/Feature158CompatibilityTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "If Testing public helpers are justified, add failing package-visible readiness helper tests in tests/Testing.Tests/Feature158TimingSeparationHelperTests.fs; otherwise add a no-Testing-helper assertion to tests/Package.Tests/Feature158CompatibilityTests.fs"
Task: "Add failing compatibility ledger, package validation, FSI transcript, public-surface drift, and performance-claim boundary tests in tests/Package.Tests/Feature158CompatibilityTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "Add failing unsupported-host tests for environment-limited, under-2-minute completion, zero accepted proof artifacts, and zero accepted performance artifacts in tests/Rendering.Harness.Tests/Feature158MeasurementPolicyTests.fs"
Task: "Add failing public-surface baseline, compatibility ledger, package validation, and no-undocumented-drift tests in tests/Package.Tests/Feature158CompatibilityTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Stop and validate: run the focused US1 tests and `compositor-performance --feature 158` timing lane.
5. Review the timing summary to confirm accepted samples are readback-free and the shipped performance claim remains `performance-not-accepted`.

### Incremental Delivery

1. Complete Setup and Foundational contracts.
2. Add US1 readback-free timing and validate it independently.
3. Add US2 explicit probe readback and validate proof/probe exclusion independently.
4. Add US3 comparable readiness package and validate the reviewer entry point.
5. Add US4 safety-boundary preservation and validate focused regressions.
6. Complete Phase 7 validation and closeout evidence.

### Parallel Team Strategy

1. Team completes Setup and Foundational work together.
2. After Foundational work is complete, one developer can own US1 timing policy, one can own US2 proof/probe exclusion, and one can prepare US3 package tests.
3. US3 readiness assembly waits for US1 and US2 evidence shapes.
4. US4 regression and compatibility work starts once US1 command routing and US3 package links exist.

## Notes

- [P] tasks use different files or independent validation outputs.
- [US1], [US2], [US3], and [US4] labels map directly to the prioritized user stories in specs/158-separate-proof-timing/spec.md.
- Synthetic fixtures are rejection-only and must include `Synthetic` in the test name plus a `// SYNTHETIC:` source comment.
- Public API changes must be designed in `.fsi`, exercised through FSI evidence, covered by semantic tests, and reflected in surface baselines or documented as no drift.
- Feature 158 can accept measurement separation, but `performance-not-accepted` remains until later Feature 159 and Feature 161 gates are satisfied.
