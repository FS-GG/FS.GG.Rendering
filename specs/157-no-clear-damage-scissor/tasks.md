# Tasks: No-Clear Damage-Scissored Render Path

**Input**: Design documents from `specs/157-no-clear-damage-scissor/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by the specification and repository constitution. Write focused tests before implementation tasks where behavior or package-visible surface changes.

**Organization**: Tasks are grouped by user story so each story can be independently implemented and validated.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User-story label, used only inside user-story phases.
- Every task includes exact repository-relative file paths.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish Feature 157 readiness locations, placeholder evidence files, and compile registrations.

- [X] T001 Create Feature 157 readiness directory placeholders in specs/157-no-clear-damage-scissor/readiness/damage/attempts/.gitkeep, specs/157-no-clear-damage-scissor/readiness/damage/fallbacks/.gitkeep, specs/157-no-clear-damage-scissor/readiness/damage/parity/.gitkeep, specs/157-no-clear-damage-scissor/readiness/damage/unsupported/.gitkeep, and specs/157-no-clear-damage-scissor/readiness/fsi/.gitkeep
- [X] T002 [P] Add damage summary placeholders with scenario, attempt, fallback, parity, unsupported-host, and claim-status headings in specs/157-no-clear-damage-scissor/readiness/damage/summary.md and specs/157-no-clear-damage-scissor/readiness/damage/summary.json
- [X] T003 [P] Add readiness closeout placeholders in specs/157-no-clear-damage-scissor/readiness/compatibility-ledger.md, specs/157-no-clear-damage-scissor/readiness/package-validation.md, specs/157-no-clear-damage-scissor/readiness/regression-validation.md, and specs/157-no-clear-damage-scissor/readiness/validation-summary.md
- [X] T004 [P] Add FSI evidence placeholders in specs/157-no-clear-damage-scissor/readiness/fsi/compositor-damage-authoring.fsx, specs/157-no-clear-damage-scissor/readiness/fsi/compositor-damage-authoring.log, specs/157-no-clear-damage-scissor/readiness/fsi/compositor-readiness-authoring.fsx, and specs/157-no-clear-damage-scissor/readiness/fsi/compositor-readiness-authoring.log
- [X] T005 [P] Create stub Feature 157 test modules in tests/SkiaViewer.Tests/Feature157NoClearDamageTests.fs, tests/Rendering.Harness.Tests/Feature157DamageEvidenceTests.fs, tests/Rendering.Harness.Tests/Feature157ReadinessPackageTests.fs, tests/Package.Tests/Feature157CompatibilityTests.fs, and tests/Testing.Tests/Feature157DamageReadinessHelperTests.fs, then register them in tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj, tests/Package.Tests/Package.Tests.fsproj, and tests/Testing.Tests/Testing.Tests.fsproj
- [X] T006 Declare Feature 157 constants, accepted profile `probe-08a47c01`, readiness paths, required scenario ids, fallback scenario ids, and command aliases in tests/Rendering.Harness/Compositor.fsi

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define shared damage-scoped vocabulary, public surfaces, workflow contracts, and command names before user-story implementation.

**Critical**: No user-story implementation should begin until these contracts and expected failing tests can be added against stable names.

- [X] T007 Implement Feature 157 constants, readiness path helpers, accepted profile metadata, required scenario ids, fallback scenario ids, and command aliases in tests/Rendering.Harness/Compositor.fs
- [X] T008 Add package-visible render decision, damage validation, retained-frame, fallback-reason, parity, and attempt diagnostic declarations needed for Feature 157 in src/SkiaViewer/Host/OpenGl.fsi and src/SkiaViewer/CompositorProof.fsi
- [X] T009 Add Feature 157 MVU/effect-boundary and renderer declarations for `Model`, `Msg`, `Effect`, `init`, `update`, attempt records, fallback records, damage summaries, compatibility ledger, package validation, regression validation, readiness summary, and edge interpreter requests in tests/Rendering.Harness/Compositor.fsi
- [X] T010 Add package-visible Feature 157 readiness helper declarations for accepted, fallback-only, rejected, and environment-limited summaries in src/Testing/Testing.fsi
- [X] T011 Add only the necessary viewer-facing diagnostic declarations for Feature 157 fallback, damage area, retained backing, and proof-gate status in src/SkiaViewer/SkiaViewer.fsi and src/SkiaViewer/Host/Diagnostics.fsi
- [X] T012 Add pre-implementation Feature 157 FSI authoring exercises for the declared damage, readiness, and Testing helper surfaces in specs/157-no-clear-damage-scissor/readiness/fsi/compositor-damage-authoring.fsx and specs/157-no-clear-damage-scissor/readiness/fsi/compositor-readiness-authoring.fsx, capture expected failing or type-check logs in the paired .log files, and add transcript coverage expectations in tests/Package.Tests/FsiTranscriptCoverageTests.fs before any Feature 157 .fs implementation bodies
- [X] T013 Add `isFeature157` detection plus `compositor-damage --feature 157` and `compositor-readiness --feature 157` placeholder routing in tests/Rendering.Harness/Cli.fs
- [X] T014 [P] Record Feature 157 real-evidence expectations, synthetic-disclosure rules, unsupported-host check, and focused-test matrix in specs/157-no-clear-damage-scissor/readiness/validation-summary.md

**Checkpoint**: Feature 157 names, signatures, command routes, MVU/effect boundary, and validation expectations are stable enough for failing tests.

---

## Phase 3: User Story 1 - Use Damage-Scoped Repaint Only When Safe (Priority: P1) - MVP

**Goal**: Select the no-clear damage-scoped render path only when the current run has accepted same-profile proof, trusted retained backing, valid damage, available resources, and passing parity evidence.

**Independent Test**: Run representative current-host frame updates and verify damage-scoped repaint is selected only when every safety gate passes, untouched pixels are preserved, and damaged pixels update.

### Tests for User Story 1

Write these tests first and verify they fail before implementation.

- [X] T015 [US1] Add failing eligibility-gate tests for accepted proof, same host profile, current run identity, retained backing, valid damage, resource availability, and parity readiness in tests/SkiaViewer.Tests/Feature157NoClearDamageTests.fs
- [X] T016 [P] [US1] Add failing accepted-scenario contract tests for `damage/static-preserved`, `damage/localized-update`, `damage/movement-old-new`, `damage/scroll-shifted`, and `damage/nested-retained` in tests/Rendering.Harness.Tests/Feature157DamageEvidenceTests.fs
- [X] T017 [US1] Run the failing US1 focused tests and record expected failures in specs/157-no-clear-damage-scissor/readiness/damage/attempts/validation.md

### Implementation for User Story 1

- [X] T018 [US1] Implement Feature 157 proof-gate matching, proof freshness, and host-profile diagnostics in src/SkiaViewer/CompositorProof.fs
- [X] T019 [US1] Implement retained frame state tracking for previous frame id, run id, host profile, framebuffer size, backing kind, validity status, and resource diagnostics in src/SkiaViewer/Host/OpenGl.fs
- [X] T020 [US1] Implement valid damage union normalization, framebuffer-coordinate clamping, no-visible-change handling, and accepted scissor area reporting in src/SkiaViewer/Host/OpenGl.fs
- [X] T021 [US1] Implement the no-clear `DirectToSwapchain` branch that skips full-frame clear, restores or trusts retained backing, clips repaint to validated damage, flushes, presents, and refreshes retained backing identity in src/SkiaViewer/Host/OpenGl.fs
- [X] T022 [US1] Implement Feature 157 diagnostics for render decision, damage area, retained backing, proof gate, fallback reason, and parity status in src/SkiaViewer/Host/Diagnostics.fs and src/SkiaViewer/SkiaViewer.fs
- [X] T023 [US1] Implement accepted-attempt records, scenario inventory, preserved-pixel evidence fields, damaged-pixel evidence fields, and parity-result fields in tests/Rendering.Harness/Compositor.fs
- [X] T024 [US1] Wire `compositor-damage --feature 157` parsing for `--attempt-count`, `--scenario`, `--out`, and feature aliases in tests/Rendering.Harness/Cli.fs
- [X] T025 [US1] Implement the Feature 157 edge interpreter for accepted-host probing, accepted proof loading, retained backing capture, damage-scoped rendering, full-redraw parity rendering, parity comparison, and artifact path collection in tests/Rendering.Harness/Cli.fs
- [X] T026 [US1] Generate capable-host damage attempt evidence for at least three fresh attempts across five scenarios under specs/157-no-clear-damage-scissor/readiness/damage/attempts/ and specs/157-no-clear-damage-scissor/readiness/damage/parity/
- [X] T027 [US1] Refresh the Feature 157 damage authoring transcript and command log after US1 implementation in specs/157-no-clear-damage-scissor/readiness/fsi/compositor-damage-authoring.fsx and specs/157-no-clear-damage-scissor/readiness/fsi/compositor-damage-authoring.log, preserving the pre-implementation FSI shape evidence from T012
- [X] T028 [US1] Run US1 focused tests and the accepted-host damage quickstart command, then record accepted, fallback-only, rejected, or environment-limited results in specs/157-no-clear-damage-scissor/readiness/damage/attempts/validation.md

**Checkpoint**: User Story 1 is independently testable and demonstrates safe damage-scoped repaint only when every eligibility gate passes.

---

## Phase 4: User Story 2 - Fall Back for Unsafe or Invalid Damage (Priority: P1)

**Goal**: Fail closed to full redraw for invalid damage, missing retained backing, unsupported hosts, resource failures, proof mismatches, and parity mismatches, with reviewer-visible reasons and zero accepted partial-redraw artifacts.

**Independent Test**: Feed invalid damage, missing retained backing, unsupported-host facts, resource failures, and parity mismatches, then verify every case takes full redraw and explains why.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T029 [US2] Add failing invalid-damage tests for empty visible change, out-of-bounds rectangles, stale damage, duplicated damage, incomplete movement damage, ambiguous damage, and full-frame invalidation in tests/SkiaViewer.Tests/Feature157NoClearDamageTests.fs
- [X] T030 [P] [US2] Add failing fallback scenario tests for missing retained backing, resource failure, unsupported host, proof rejection, parity mismatch, and zero accepted partial-redraw artifacts in tests/Rendering.Harness.Tests/Feature157DamageEvidenceTests.fs
- [X] T031 [P] [US2] Add failing package-visible readiness helper rejection tests for fallback-only, rejected, and environment-limited damage summaries in tests/Testing.Tests/Feature157DamageReadinessHelperTests.fs
- [X] T032 [US2] Run the failing US2 focused tests and record expected failures in specs/157-no-clear-damage-scissor/readiness/damage/fallbacks/validation.md

### Implementation for User Story 2

- [X] T033 [US2] Implement damage validation categories `empty-visible-change`, `out-of-bounds`, `stale`, `duplicated`, `incomplete`, `ambiguous`, and `full-frame-invalidation` in src/SkiaViewer/Host/OpenGl.fs
- [X] T034 [US2] Implement full-redraw fallback decisions for missing proof, stale proof, cross-profile proof, synthetic-only proof, missing retained backing, stale retained backing, resized backing, resource failure, and unsupported host in src/SkiaViewer/Host/OpenGl.fs
- [X] T035 [US2] Implement parity-mismatch rejection and future-attempt quarantine until fresh proof and retained backing are present in src/SkiaViewer/Host/OpenGl.fs
- [X] T036 [US2] Implement fallback attempt records, primary fallback reason selection, zero accepted artifact counting, and environment-limited status mapping in tests/Rendering.Harness/Compositor.fs
- [X] T037 [US2] Implement fallback scenario execution, unsupported-host execution, resource-failure simulation, parity-mismatch fixtures, and fallback artifact writing in tests/Rendering.Harness/Cli.fs
- [X] T038 [US2] Implement Feature 157 Testing helper behavior for accepted, fallback-only, rejected, and environment-limited readiness summaries in src/Testing/Testing.fs
- [X] T039 [US2] Generate rejection-only fallback outputs under specs/157-no-clear-damage-scissor/readiness/damage/fallbacks/ and unsupported-host outputs under specs/157-no-clear-damage-scissor/readiness/damage/unsupported/
- [X] T040 [US2] Add `Synthetic` test names and `// SYNTHETIC:` comments for rejection-only fixtures in tests/SkiaViewer.Tests/Feature157NoClearDamageTests.fs, tests/Rendering.Harness.Tests/Feature157DamageEvidenceTests.fs, and tests/Testing.Tests/Feature157DamageReadinessHelperTests.fs
- [X] T041 [US2] Run US2 focused tests and record fail-closed fallback coverage in specs/157-no-clear-damage-scissor/readiness/damage/fallbacks/validation.md

**Checkpoint**: User Story 2 is independently testable and no unsafe or ambiguous input can produce accepted damage-scoped output.

---

## Phase 5: User Story 3 - Publish Reviewable Correctness Evidence (Priority: P2)

**Goal**: Publish one reviewer-facing evidence package that links accepted attempts, rejected attempts, fallback reasons, scenario coverage, host profile, parity, artifact paths, compatibility impact, and final readiness status.

**Independent Test**: Open specs/157-no-clear-damage-scissor/readiness/validation-summary.md and verify a reviewer can trace accepted runs, rejected runs, scenario coverage, host profile, fallback reasons, parity results, and remaining performance limitations from one entry point.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T042 [US3] Add failing readiness package tests for required files, accepted attempt links, fallback links, damage status counts, parity links, unsupported-host result, artifact paths, final status, and `performance-not-accepted` in tests/Rendering.Harness.Tests/Feature157ReadinessPackageTests.fs
- [X] T043 [P] [US3] Add failing compatibility ledger, package validation, public-surface drift, and performance-claim boundary tests in tests/Package.Tests/Feature157CompatibilityTests.fs
- [X] T044 [P] [US3] Add failing Feature 157 FSI transcript assertions for damage authoring, readiness authoring, and Testing helpers in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T045 [US3] Run the failing US3 focused tests and record expected failures in specs/157-no-clear-damage-scissor/readiness/validation-summary.md

### Implementation for User Story 3

- [X] T046 [US3] Review Feature 157 renderer declarations in tests/Rendering.Harness/Compositor.fsi and add any missing summary-package declarations before implementing renderer bodies in tests/Rendering.Harness/Compositor.fs
- [X] T047 [US3] Implement Feature 157 damage summary, attempt report, fallback report, unsupported-host report, compatibility ledger, package validation, regression validation, and readiness renderers in tests/Rendering.Harness/Compositor.fs
- [X] T048 [US3] Wire `compositor-readiness --feature 157 --out <dir>` to assemble validation-summary.md, compatibility-ledger.md, package-validation.md, regression-validation.md, damage/summary.md, damage/summary.json, attempt reports, fallback reports, parity reports, and unsupported-host links in tests/Rendering.Harness/Cli.fs
- [X] T049 [US3] Emit per-attempt markdown reports, fallback markdown reports, parity markdown reports, and machine-readable summary JSON for Feature 157 in tests/Rendering.Harness/Cli.fs
- [X] T050 [US3] Generate or refresh the reviewer damage summary in specs/157-no-clear-damage-scissor/readiness/damage/summary.md and specs/157-no-clear-damage-scissor/readiness/damage/summary.json
- [X] T051 [US3] Refresh the Feature 157 readiness authoring transcript and command log after US3 implementation in specs/157-no-clear-damage-scissor/readiness/fsi/compositor-readiness-authoring.fsx and specs/157-no-clear-damage-scissor/readiness/fsi/compositor-readiness-authoring.log, preserving the pre-implementation FSI shape evidence from T012
- [X] T052 [US3] Generate or refresh compatibility, package, regression, and validation summaries in specs/157-no-clear-damage-scissor/readiness/compatibility-ledger.md, specs/157-no-clear-damage-scissor/readiness/package-validation.md, specs/157-no-clear-damage-scissor/readiness/regression-validation.md, and specs/157-no-clear-damage-scissor/readiness/validation-summary.md
- [X] T053 [US3] Run US3 focused tests and the readiness quickstart command, then record the reviewer-summary outcome and under-5-minute reviewer determination check in specs/157-no-clear-damage-scissor/readiness/validation-summary.md

**Checkpoint**: User Story 3 publishes one reviewable correctness package with all claims and limitations traceable from the summary.

---

## Phase 6: User Story 4 - Preserve Existing Safety Claims and Boundaries (Priority: P2)

**Goal**: Preserve Feature 155 correctness readiness, Feature 156 timing policy, unsupported-host behavior, package validation, public compatibility, and the remaining performance-claim boundary.

**Independent Test**: Run focused correctness, fallback, package, and compatibility checks before closeout and verify any consumer-visible drift is intentional and documented.

### Tests for User Story 4

Write these tests first and verify they fail before implementation.

- [X] T054 [US4] Add failing Feature 155 proof/parity preservation and Feature 156 `performance-not-accepted` boundary tests in tests/Rendering.Harness.Tests/Feature157ReadinessPackageTests.fs
- [X] T055 [P] [US4] Add failing unsupported-host regression tests for environment-limited status, under-2-minute completion, and zero accepted partial-redraw artifacts in tests/Rendering.Harness.Tests/Feature157DamageEvidenceTests.fs
- [X] T056 [P] [US4] Add failing public-surface baseline, compatibility ledger, package validation, and no-undocumented-drift tests in tests/Package.Tests/Feature157CompatibilityTests.fs
- [X] T057 [US4] Run the failing US4 focused tests and record expected failures in specs/157-no-clear-damage-scissor/readiness/regression-validation.md

### Implementation for User Story 4

- [X] T058 [US4] Preserve and render Feature 155 proof/parity baseline references, unsupported-host fail-closed policy, Feature 156 timing status, and remaining Feature 158, Feature 159, Feature 160, and Feature 161 gates in tests/Rendering.Harness/Compositor.fs
- [X] T059 [US4] Implement unsupported-host `environment-limited` execution for `compositor-damage --feature 157` with full-redraw fallback and zero accepted damage-scoped artifacts in tests/Rendering.Harness/Cli.fs
- [X] T060 [US4] Refresh public surface baselines if `.fsi` changes occur by updating tests/surface-baselines/FS.GG.UI.SkiaViewer.txt and tests/surface-baselines/FS.GG.UI.Testing.txt, copying or summarizing the accepted deltas into readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt and readiness/surface-baselines/FS.GG.UI.Testing.txt, or document no public surface drift in specs/157-no-clear-damage-scissor/readiness/compatibility-ledger.md
- [X] T061 [US4] Generate unsupported-host validation notes with elapsed time, environment-limited status, and zero accepted artifacts in specs/157-no-clear-damage-scissor/readiness/damage/unsupported/README.md and specs/157-no-clear-damage-scissor/readiness/damage/unsupported/validation.md
- [X] T062 [US4] Update Feature 157 status, remaining performance gates, and no universal performance claim in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md
- [X] T063 [US4] Run US4 focused tests and record correctness, fallback, package, compatibility, and unsupported-host preservation evidence in specs/157-no-clear-damage-scissor/readiness/regression-validation.md

**Checkpoint**: User Story 4 proves the real damage-scoped path does not weaken existing correctness, fallback, unsupported-host, package, or public-surface boundaries.

---

## Phase 7: Polish & Validation

**Purpose**: Final validation, package evidence, quickstart evidence, and task closeout.

- [X] T064 Run `dotnet build FS.GG.Rendering.slnx --no-restore` and record the result in specs/157-no-clear-damage-scissor/readiness/package-validation.md
- [X] T065 Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature157 --no-build` and record the result in specs/157-no-clear-damage-scissor/readiness/regression-validation.md
- [X] T066 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature157 --no-build` and record the result in specs/157-no-clear-damage-scissor/readiness/regression-validation.md
- [X] T067 Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature157 --no-build` and `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature157 --no-build`, then record results in specs/157-no-clear-damage-scissor/readiness/package-validation.md
- [X] T068 Run the accepted-host `compositor-damage --feature 157 --attempt-count 3 --out specs/157-no-clear-damage-scissor/readiness/damage` quickstart command or record host limitation in specs/157-no-clear-damage-scissor/readiness/damage/summary.md
- [X] T069 Run the unsupported-host quickstart command with display variables unset, measure elapsed time, and record under-2-minute and zero-accepted-artifact evidence in specs/157-no-clear-damage-scissor/readiness/damage/unsupported/validation.md
- [X] T070 Run `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-readiness --feature 157 --out specs/157-no-clear-damage-scissor/readiness` and record final package links in specs/157-no-clear-damage-scissor/readiness/validation-summary.md
- [X] T071 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature155|Feature156|Feature157" --no-build` and record focused P7 regression results in specs/157-no-clear-damage-scissor/readiness/regression-validation.md
- [X] T072 Run `dotnet test FS.GG.Rendering.slnx --no-restore` and record the result in specs/157-no-clear-damage-scissor/readiness/regression-validation.md
- [X] T073 Run `dotnet fsi scripts/refresh-surface-baselines.fsx` when public `.fsi` changes occur, verify committed tests/surface-baselines/FS.GG.UI.SkiaViewer.txt and tests/surface-baselines/FS.GG.UI.Testing.txt changes or document no public drift, mirror or summarize intended deltas under readiness/surface-baselines/ when required by release evidence, then run package surface and pack-local validation or record tooling limitations in specs/157-no-clear-damage-scissor/readiness/package-validation.md
- [X] T074 Run `git diff --check` and record whitespace validation in specs/157-no-clear-damage-scissor/readiness/regression-validation.md
- [X] T075 Mark completed tasks in specs/157-no-clear-damage-scissor/tasks.md after all selected validation evidence is recorded, and list every Feature 157 `Synthetic` test or fixture in specs/157-no-clear-damage-scissor/readiness/validation-summary.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user-story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational and is the MVP safe damage-scoped repaint path.
- **User Story 2 (Phase 4)**: Depends on Foundational; can be developed with rejection fixtures but final fallback behavior uses US1 decision primitives.
- **User Story 3 (Phase 5)**: Depends on US1 and US2 evidence shapes to publish a complete review package.
- **User Story 4 (Phase 6)**: Depends on US1 command routing and US3 package assembly for final unsupported-host, compatibility, and regression evidence.
- **Polish (Phase 7)**: Depends on all selected user stories.

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational; delivers the MVP guarded damage-scoped repaint path.
- **US2 (P1)**: Can start after Foundational with fixtures; final rejection coverage depends on US1 render-decision and attempt shapes.
- **US3 (P2)**: Requires US1 accepted attempts and US2 fallback records to publish a reviewable package.
- **US4 (P2)**: Requires US1 command routing and US3 package links to prove safety and compatibility preservation.

### Dependency Graph

```text
Phase 1 Setup
  -> Phase 2 Foundational
      -> US1 Safe Damage-Scoped Repaint (MVP)
      -> US2 Fail-Closed Fallbacks
US1 + US2 -> US3 Reviewable Correctness Evidence
US1 + US3 -> US4 Safety and Compatibility Preservation
US1/US2/US3/US4 -> Phase 7 Polish
```

### Within Each User Story

- Tests first, and they should fail before implementation.
- New Feature 157 test files are registered in the owning `.fsproj` before focused test execution.
- `.fsi`, public contracts, pre-implementation FSI authoring transcripts, and FSI transcript expectations come before `.fs` bodies for public or observable surfaces.
- Pure validators, workflow updates, and renderers come before CLI edge interpreters.
- Command behavior comes before durable readiness artifact generation.
- Story validation command and readiness artifact update are the final tasks in each story.

## Parallel Opportunities

- Setup tasks T002-T005 can run in parallel after T001 creates readiness directories.
- Foundational tasks T010 and T014 can run in parallel with harness contract work after T006; T012 runs after the relevant `.fsi` declarations from T008-T011 are drafted and before any Feature 157 `.fs` implementation bodies.
- US1 tests T015 and T016 can run in parallel because they touch different test projects.
- US1 implementation can split T018 proof-gate work from T019-T021 OpenGl render-path work and T023 harness evidence modeling, then converge on T024-T025 CLI wiring.
- US2 tests T029-T031 can run in parallel because they touch different test files.
- US2 implementation can split T033-T035 OpenGl fallback work from T036 harness modeling, T037 CLI fixtures, and T038 Testing helpers.
- US3 tests T042-T044 can run in parallel because they touch different test files.
- US3 implementation can split T047 renderer work from T051 transcript refresh after T046 confirms `.fsi` declarations are complete and T012 has already captured pre-implementation FSI shape evidence.
- US4 tests T054-T056 can run in parallel because they touch different test files.
- Polish validation tasks T065-T067 can run in parallel after T064 build succeeds; T070-T075 must run after readiness files exist.

## Parallel Example: User Story 1

```bash
Task: "Add failing eligibility-gate tests for accepted proof, same host profile, current run identity, retained backing, valid damage, resource availability, and parity readiness in tests/SkiaViewer.Tests/Feature157NoClearDamageTests.fs"
Task: "Add failing accepted-scenario contract tests for damage/static-preserved, damage/localized-update, damage/movement-old-new, damage/scroll-shifted, and damage/nested-retained in tests/Rendering.Harness.Tests/Feature157DamageEvidenceTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "Add failing fallback scenario tests for missing retained backing, resource failure, unsupported host, proof rejection, parity mismatch, and zero accepted partial-redraw artifacts in tests/Rendering.Harness.Tests/Feature157DamageEvidenceTests.fs"
Task: "Add failing package-visible readiness helper rejection tests for fallback-only, rejected, and environment-limited damage summaries in tests/Testing.Tests/Feature157DamageReadinessHelperTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "Add failing compatibility ledger, package validation, public-surface drift, and performance-claim boundary tests in tests/Package.Tests/Feature157CompatibilityTests.fs"
Task: "Add failing Feature 157 FSI transcript assertions for damage authoring, readiness authoring, and Testing helpers in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "Add failing unsupported-host regression tests for environment-limited status, under-2-minute completion, and zero accepted partial-redraw artifacts in tests/Rendering.Harness.Tests/Feature157DamageEvidenceTests.fs"
Task: "Add failing public-surface baseline, compatibility ledger, package validation, and no-undocumented-drift tests in tests/Package.Tests/Feature157CompatibilityTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational contracts and command routes.
3. Complete Phase 3: User Story 1 safe damage-scoped repaint.
4. Stop and validate US1 independently with focused SkiaViewer and Rendering.Harness tests plus the accepted-host damage command.

### Incremental Delivery

1. Complete Setup and Foundational work so names, `.fsi` declarations, tests, and command routes are stable.
2. Add US1 to produce the guarded real damage-scoped path and accepted-attempt evidence.
3. Add US2 to harden every unsafe and invalid condition into full-redraw fallback.
4. Add US3 to publish the reviewer-facing correctness evidence package.
5. Add US4 to preserve Feature 155, Feature 156, unsupported-host, package, and public-surface boundaries.
6. Run Phase 7 validation and close out readiness artifacts.

### Parallel Team Strategy

With multiple developers:

1. Complete Phase 1 and Phase 2 together.
2. After Foundational is done, split work by file ownership:
   - Developer A: US1 OpenGl render path and SkiaViewer tests.
   - Developer B: US2 fallback/rejection paths and Testing helpers.
   - Developer C: US3/US4 harness readiness, compatibility, and regression evidence.
3. Merge through the readiness package once accepted and fallback evidence shapes are stable.

## Notes

- [P] tasks touch different files and can be run in parallel once their prerequisites are done.
- [Story] labels map tasks to specific user stories for traceability.
- Keep Feature 157 correctness acceptance profile-bound to `probe-08a47c01` unless a later accepted proof explicitly replaces it.
- The shipped compositor performance claim remains `performance-not-accepted` unless later report-defined gates are also complete.
- Synthetic fixtures are rejection-only and must use `Synthetic` test names plus `// SYNTHETIC:` comments.
- Commit after each task or logical group.
