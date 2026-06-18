# Tasks: Compositor Proof Acceptance

**Input**: Design documents from `/specs/154-compositor-proof-acceptance/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Required. The specification includes independent testing for each user story, and the constitution requires failing-first tests for behavior-changing Tier 1 work.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independent increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User-story label, used only inside user-story phases.
- Every task includes exact repository-relative file paths.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare Feature 154 readiness locations and initial evidence files without changing runtime behavior.

- [X] T001 Create Feature 154 readiness directory placeholders in specs/154-compositor-proof-acceptance/readiness/live-proof/attempts/.gitkeep, specs/154-compositor-proof-acceptance/readiness/live-proof/unsupported/.gitkeep, specs/154-compositor-proof-acceptance/readiness/parity/.gitkeep, specs/154-compositor-proof-acceptance/readiness/timing/.gitkeep, and specs/154-compositor-proof-acceptance/readiness/fsi/.gitkeep
- [X] T002 [P] Add live proof evidence index for capable-host attempts and unsupported-host output in specs/154-compositor-proof-acceptance/readiness/live-proof/README.md
- [X] T003 [P] Add same-profile parity corpus index with the ten required scenario ids in specs/154-compositor-proof-acceptance/readiness/parity/README.md
- [X] T004 [P] Add timing evidence index with threshold/noise policy placeholders in specs/154-compositor-proof-acceptance/readiness/timing/README.md
- [X] T005 [P] Add validation summary placeholder for proof-set status, parity status, timing status, fallback status, artifact links, and limitations in specs/154-compositor-proof-acceptance/readiness/validation-summary.md
- [X] T006 [P] Add compatibility ledger placeholder for public API, diagnostics, fallback, readiness vocabulary, package, docs, and migration impact in specs/154-compositor-proof-acceptance/readiness/compatibility-ledger.md
- [X] T007 [P] Add package and regression validation placeholders in specs/154-compositor-proof-acceptance/readiness/package-validation.md and specs/154-compositor-proof-acceptance/readiness/regression-validation.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Declare Feature 154 observable surfaces, command routing, and validation wiring before story implementation.

**Critical**: No user-story implementation should begin until these contracts and expected failing tests can be added against stable names.

- [X] T008 Add Feature 154 constants, readiness paths, target host profiles, parity scenario ids, timing tiers, renderer signatures, and MVU/effect-boundary contracts (`Model`, `Msg`, `Effect`, `init`, `update`, edge interpreter) in tests/Rendering.Harness/Compositor.fsi
- [X] T009 Add only the necessary Feature 154 proof-set or readiness public deltas to src/SkiaViewer/CompositorProof.fsi, preserving the Feature 153 proof vocabulary
- [X] T010 Add Feature 154 package-visible readiness helper declarations in src/Testing/Testing.fsi
- [X] T011 Add Feature 154 CLI feature detection stubs for `--feature 154`, `feature154`, and `154-compositor-proof-acceptance` in tests/Rendering.Harness/Cli.fs
- [X] T012 [P] Add Feature 154 FSI transcript path helpers and expected coverage hooks for public proof, parity, timing, readiness, helper, and MVU/effect authoring in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T013 [P] Add failing Feature 154 MVU/effect-boundary semantic coverage tests for pure `update` transitions and edge interpreter effects in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T014 [P] Add Feature 154 validation plan describing real-evidence, synthetic-disclosure, unsupported-host, parity, timing, package, regression, MVU/effect-boundary, and per-story test compile-registration gates in specs/154-compositor-proof-acceptance/readiness/validation-plan.md

**Checkpoint**: Public contracts, harness contracts, command names, MVU/effect-boundary expectations, and per-story test registration policy are ready for failing tests.

---

## Phase 3: User Story 1 - Accept a Three-Run Capable-Host Proof Set (Priority: P1, MVP)

**Goal**: Accept only exactly three fresh matching live proof attempts from one capable host profile and proof method, while failing closed for unsupported, stale, synthetic, mismatched, incomplete, or failed-pixel evidence.

**Independent Test**: Run three live proof attempts on the same capable host profile and verify that the proof set accepts only when all selected attempts are fresh, matching, non-synthetic, decodable, non-blank, and individually accepted.

### Tests for User Story 1

Write these tests first and verify they fail before implementation.

- [X] T015 [P] [US1] Add failing exact-three accepted proof-set, selected-attempt id, freshness-window, host-profile, and proof-method tests in tests/SkiaViewer.Tests/Feature154ProofSetAcceptanceTests.fs plus capable-host live proof tests in tests/SkiaViewer.Tests/Feature154LiveProofHostTests.fs, and register both files in tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T016 [P] [US1] Add failing Synthetic rejection tests for stale, missing, blank, undecodable, synthetic-only, incomplete, damaged-pixel, and undamaged-preservation failures in tests/SkiaViewer.Tests/Feature154SyntheticRejectionTests.fs and register it in tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T017 [P] [US1] Add failing live proof command and artifact tests for `compositor-live-proof --feature 154 --attempt-count 3` in tests/Rendering.Harness.Tests/Feature154ProofAcceptanceTests.fs and register it in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T018 [US1] Run the failing US1 focused tests and record expected failures in specs/154-compositor-proof-acceptance/readiness/live-proof/validation.md

### Implementation for User Story 1

- [X] T019 [US1] Implement Feature 154 constants, readiness path helpers, target host profiles, and proof-set artifact path helpers in tests/Rendering.Harness/Compositor.fs
- [X] T020 [US1] Implement Feature 154 proof-set acceptance rendering for selected attempts, artifact quality, host profile, proof method, freshness, and fail-closed reasons in tests/Rendering.Harness/Compositor.fs
- [X] T021 [US1] Wire Feature 154 live proof output defaults for attempts and unsupported-host directories in tests/Rendering.Harness/Cli.fs
- [X] T022 [US1] Wire `compositor-live-proof --feature 154 --attempt-count <n> --out <dir>` to write Feature 154 proof.md, limitations.md, attempts/README.md, and unsupported/README.md in tests/Rendering.Harness/Cli.fs
- [X] T023 [US1] Adjust exact-three proof-set evaluation or reason priority only where Feature 154 tests expose gaps in src/SkiaViewer/CompositorProof.fs
- [X] T024 [US1] Add Feature 154 proof-set authoring transcript and expected PASS log in specs/154-compositor-proof-acceptance/readiness/fsi/compositor-proof-acceptance-authoring.fsx and specs/154-compositor-proof-acceptance/readiness/fsi/compositor-proof-acceptance-authoring.log
- [X] T025 [US1] Generate or refresh capable-host and unsupported-host proof evidence in specs/154-compositor-proof-acceptance/readiness/live-proof/attempts/README.md, specs/154-compositor-proof-acceptance/readiness/live-proof/unsupported/README.md, and specs/154-compositor-proof-acceptance/readiness/proof-set.md
- [X] T026 [US1] Run US1 focused tests and proof quickstart commands, then record accepted, failed, or environment-limited proof-set outcome in specs/154-compositor-proof-acceptance/readiness/live-proof/validation.md

**Checkpoint**: User Story 1 is independently testable and the proof-set gate accepts only exactly three fresh matching capable-host attempts.

---

## Phase 4: User Story 2 - Prove Same-Profile Damage-Scoped Parity (Priority: P1)

**Goal**: Run the representative damage-scoped parity corpus on the accepted proof host profile and accept only scenarios whose final visible output matches the full-redraw reference, with safe fallback reasons for non-accepted scenarios.

**Independent Test**: Run the parity corpus on the accepted host profile and verify accepted parity or fallback decisions for localized update, no-change, movement, overlap, edge clipping, resize, full invalidation, invalid damage, unsupported host, and resource failure.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T027 [P] [US2] Add failing same-profile parity corpus renderer tests for all ten required scenario ids in tests/Rendering.Harness.Tests/Feature154ParityCorpusTests.fs and register it in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T028 [P] [US2] Add failing cross-profile, stale proof-set, missing proof-set, failed parity, and safe fallback gate tests in tests/SkiaViewer.Tests/Feature154DamageScopedParityTests.fs and register it in tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T029 [P] [US2] Add failing retained damage and full-redraw fallback diagnostic tests for parity scenarios in tests/Controls.Tests/Feature154DamageParityPlanTests.fs and register it in tests/Controls.Tests/Controls.Tests.fsproj
- [X] T030 [P] [US2] Add failing Elmish compositor diagnostic tests for proof/parity fallback and scissor candidate suppression in tests/Elmish.Tests/Feature154CompositorMetricsTests.fs and register it in tests/Elmish.Tests/Elmish.Tests.fsproj
- [X] T031 [US2] Run the failing US2 focused tests and record expected failures in specs/154-compositor-proof-acceptance/readiness/parity/validation.md

### Implementation for User Story 2

- [X] T032 [US2] Implement Feature 154 parity scenario ids, same-profile proof-set gate checks, and parity verdict formatting in tests/Rendering.Harness/Compositor.fs
- [X] T033 [US2] Implement `renderFeature154ParityReport` with full-redraw reference, damage-scoped output, fallback reasons, and cross-profile stale-evidence rejection text in tests/Rendering.Harness/Compositor.fs
- [X] T034 [US2] Wire `compositor-parity --feature 154 --out <dir>` to write Feature 154 parity evidence under specs/154-compositor-proof-acceptance/readiness/parity in tests/Rendering.Harness/Cli.fs
- [X] T035 [US2] Reuse or extend retained damage and fallback diagnostics needed by Feature 154 parity tests in src/Controls/Diagnostics.fsi and src/Controls/Diagnostics.fs
- [X] T036 [US2] Reuse or extend Elmish compositor diagnostics needed by Feature 154 parity fallback tests in src/Controls.Elmish/ControlsElmish.fsi and src/Controls.Elmish/ControlsElmish.fs
- [X] T037 [US2] Run US2 focused tests and parity quickstart command, then record accepted, fallback, failed, or environment-limited parity corpus outcome in specs/154-compositor-proof-acceptance/readiness/parity/validation.md and specs/154-compositor-proof-acceptance/readiness/parity/README.md

**Checkpoint**: User Story 2 is independently testable and partial redraw remains fallback-gated unless proof-set acceptance and same-profile parity acceptance are both current.

---

## Phase 5: User Story 3 - Decide the Live Performance Claim (Priority: P2)

**Goal**: Publish a same-profile timing decision that accepts, rejects, or marks inconclusive any performance claim using a declared threshold/noise policy and comparable live measurements.

**Independent Test**: Measure representative live scenarios with declared policy and verify that readiness accepts a benefit only when same-profile evidence satisfies the policy.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T038 [P] [US3] Add failing timing policy, five-scenario, five-repetition, same-profile, accepted, rejected, and inconclusive decision tests in tests/Rendering.Harness.Tests/Feature154TimingDecisionTests.fs and register it in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T039 [P] [US3] Add failing package-visible timing readiness helper tests for no accepted claim on missing, noisy, incomplete, cross-profile, environment-limited, or non-beneficial evidence in tests/Testing.Tests/Feature154ReadinessHelperTests.fs and register it in tests/Testing.Tests/Testing.Tests.fsproj
- [X] T040 [US3] Run the failing US3 focused tests and record expected failures in specs/154-compositor-proof-acceptance/readiness/timing/validation.md

### Implementation for User Story 3

- [X] T041 [US3] Implement Feature 154 timing policy, scenario count, repetition count, same-profile matching, context-only evidence labels, and timing decision formatting in tests/Rendering.Harness/Compositor.fs
- [X] T042 [US3] Wire `compositor-timing --feature 154 --tier damage --scenario-count <n> --repetitions <n> --out <dir>` parsing and output in tests/Rendering.Harness/Cli.fs
- [X] T043 [US3] Implement package-visible timing readiness helper behavior for accepted, rejected, inconclusive, and context-only timing decisions in src/Testing/Testing.fs
- [X] T044 [US3] Generate or refresh timing decision evidence in specs/154-compositor-proof-acceptance/readiness/timing/README.md and specs/154-compositor-proof-acceptance/readiness/timing/timing-damage.md
- [X] T045 [US3] Run US3 focused tests and timing quickstart command, then record accepted, rejected, or inconclusive timing outcome in specs/154-compositor-proof-acceptance/readiness/timing/validation.md

**Checkpoint**: User Story 3 is independently testable and no performance claim is accepted without declared same-profile live timing evidence.

---

## Phase 6: User Story 4 - Publish the Final P7 Readiness Verdict (Priority: P3)

**Goal**: Publish one reviewable readiness package that states whether P7 live partial redraw is accepted, failed, environment-limited, or fallback-gated, with proof, parity, timing, fallback, compatibility, package, regression, and limitation evidence linked from one summary.

**Independent Test**: Open the readiness summary and confirm it links the selected proof set, same-profile parity corpus, timing decision, fallback status, unsupported-host regression, compatibility impact, and remaining limitations.

### Tests for User Story 4

Write these tests first and verify they fail before implementation.

- [X] T046 [P] [US4] Add failing final readiness package tests for proof, parity, timing, fallback, selected attempts, host profile, artifacts, limitations, status tokens, and the under-5-minute reviewer summary check in tests/Rendering.Harness.Tests/Feature154ReadinessPackageTests.fs and register it in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T047 [P] [US4] Add failing compatibility ledger and public drift tests for Feature 154 readiness, diagnostics, fallback behavior, and package-facing claims in tests/Package.Tests/Feature154CompatibilityLedgerTests.fs and register it in tests/Package.Tests/Package.Tests.fsproj
- [X] T048 [P] [US4] Add failing Feature 154 FSI transcript coverage assertions for proof, parity, timing, readiness, and helper authoring in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T049 [P] [US4] Add failing final readiness helper tests for accepted, failed, environment-limited, and fallback-gated summaries in tests/Testing.Tests/Feature154ReadinessHelperTests.fs and register it in tests/Testing.Tests/Testing.Tests.fsproj
- [X] T050 [US4] Run the failing US4 focused tests and record expected failures in specs/154-compositor-proof-acceptance/readiness/validation-summary.md

### Implementation for User Story 4

- [X] T051 [US4] Implement Feature 154 validation summary renderer linking proof-set, selected attempts, host profile, parity, timing, fallback, artifact locations, compatibility, and limitations in tests/Rendering.Harness/Compositor.fs
- [X] T052 [US4] Implement Feature 154 compatibility ledger, package validation, and regression validation renderers in tests/Rendering.Harness/Compositor.fs
- [X] T053 [US4] Wire `compositor-readiness --feature 154 --out <dir>` to write validation-summary.md, compatibility-ledger.md, proof-set.md, package-validation.md, and regression-validation.md in tests/Rendering.Harness/Cli.fs
- [X] T054 [US4] Implement package-visible final readiness summary helper behavior in src/Testing/Testing.fs
- [X] T055 [US4] Add Feature 154 final-readiness authoring transcript and run log in specs/154-compositor-proof-acceptance/readiness/fsi/compositor-readiness-authoring.fsx and specs/154-compositor-proof-acceptance/readiness/fsi/compositor-readiness-authoring.log
- [X] T056 [US4] Refresh affected public surface baselines in readiness/surface-baselines/FS.GG.UI.Controls.txt, readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt, readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt, and readiness/surface-baselines/FS.GG.UI.Testing.txt
- [X] T057 [US4] Write final compatibility ledger content in specs/154-compositor-proof-acceptance/readiness/compatibility-ledger.md
- [X] T058 [US4] Write final package and regression validation content in specs/154-compositor-proof-acceptance/readiness/package-validation.md and specs/154-compositor-proof-acceptance/readiness/regression-validation.md
- [X] T059 [US4] Run US4 focused tests and readiness quickstart command, then write final P7 verdict in specs/154-compositor-proof-acceptance/readiness/validation-summary.md

**Checkpoint**: User Story 4 publishes one reviewable P7 readiness verdict without hiding fallback, unsupported-host, timing, compatibility, package, or regression limits.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation alignment, synthetic disclosure, package checks, and broad regression evidence.

- [X] T060 [P] Update the originating P7 report status for Feature 154 proof acceptance in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md
- [X] T061 [P] Review and disclose every synthetic Feature 154 test or artifact in tests/SkiaViewer.Tests/Feature154SyntheticRejectionTests.fs and specs/154-compositor-proof-acceptance/readiness/validation-summary.md, verifying `Synthetic` appears in each synthetic test name and each use site has a `// SYNTHETIC:` reason
- [X] T062 Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature154 --no-build` and record the result in specs/154-compositor-proof-acceptance/readiness/regression-validation.md
- [X] T063 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature154 --no-build` and record the result in specs/154-compositor-proof-acceptance/readiness/regression-validation.md
- [X] T064 Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature154 --no-build` and `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature154 --no-build`, then record results in specs/154-compositor-proof-acceptance/readiness/regression-validation.md
- [X] T065 Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature154 --no-build` and `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature154 --no-build`, then record results in specs/154-compositor-proof-acceptance/readiness/package-validation.md
- [X] T066 Run unsupported-host quickstart command and record elapsed time, under-2-minute status, zero accepted artifacts, and result in specs/154-compositor-proof-acceptance/readiness/live-proof/unsupported/validation.md
- [X] T067 Run capable-host proof, parity, and timing quickstart commands when a usable OpenGL presentation host is available, or record the environment limitation in specs/154-compositor-proof-acceptance/readiness/live-proof/attempts/validation.md, specs/154-compositor-proof-acceptance/readiness/parity/validation.md, and specs/154-compositor-proof-acceptance/readiness/timing/validation.md
- [X] T068 Run `dotnet test FS.GG.Rendering.slnx --no-restore` and record the result in specs/154-compositor-proof-acceptance/readiness/regression-validation.md
- [X] T069 Run package surface and pack-local validation, or record absent tooling limitations, in specs/154-compositor-proof-acceptance/readiness/package-validation.md
- [X] T070 Verify final readiness links, accepted/fallback status, selected proof attempts, parity corpus, timing claim, zero unsupported-host accepted artifacts, compatibility ledger, package validation, remaining limitations, and under-5-minute reviewer determination evidence in specs/154-compositor-proof-acceptance/readiness/validation-summary.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks user-story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational; MVP proof-set acceptance.
- **User Story 2 (Phase 4)**: Depends on Foundational and requires an accepted proof set for real accepted parity, but can be developed with fixtures.
- **User Story 3 (Phase 5)**: Depends on Foundational and requires accepted proof plus parity for accepted performance claims, but can be developed with fixtures.
- **User Story 4 (Phase 6)**: Depends on selected proof, parity, and timing evidence from US1-US3 to publish the final verdict.
- **Polish (Phase 7)**: Depends on all selected user stories.

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational; this is the MVP and the proof prerequisite for real parity acceptance.
- **US2 (P1)**: Can start after Foundational with fixtures; accepted same-profile parity requires US1 proof-set acceptance.
- **US3 (P2)**: Can start after Foundational with fixtures; an accepted performance claim requires US1 and US2 acceptance.
- **US4 (P3)**: Requires the actual or explicitly limited evidence from US1, US2, and US3.

### Dependency Graph

```text
Phase 1 Setup
  -> Phase 2 Foundational
      -> US1 Three-Run Proof Set (MVP)
      -> US2 Same-Profile Parity with fixtures
      -> US3 Timing Decision with fixtures
US1 accepted proof set -> US2 accepted same-profile parity
US1 + US2 accepted evidence -> US3 accepted performance claim eligibility
US1/US2/US3 selected evidence -> US4 Final P7 Readiness Verdict
US4 -> Phase 7 Polish
```

### Within Each User Story

- Tests first, and they should fail before implementation.
- New Feature 154 test files are registered in the owning `.fsproj` by the same task that creates the file.
- `.fsi`, contract changes, and package-shaped FSI transcript coverage before `.fs` bodies for public or observable surfaces.
- Pure validators and renderers before edge interpreters and CLI wiring.
- Command behavior before durable readiness artifact generation.
- Story validation command and readiness artifact update last.

## Parallel Opportunities

- Setup evidence tasks T002-T007 can run in parallel after T001.
- Foundational tasks T009-T014 can be split by package after T008 establishes harness names and MVU/effect contracts.
- US1 test tasks T015-T017 can run in parallel; T018 records the failing baseline after those tests exist.
- US1 implementation can split T019-T020 harness formatting from T023 proof-set gaps, then T021-T022 sequence CLI wiring.
- US2 test tasks T027-T030 can run in parallel because they touch different test projects.
- US2 implementation can split T032-T034 harness work from T035-T036 diagnostics work.
- US3 test tasks T038-T039 can run in parallel because they touch separate test projects.
- US4 test tasks T046-T049 can run in parallel because they touch separate test files.
- Polish tasks T060-T061 can run in parallel with final validation preparation; T070 must run after readiness files exist.

## Parallel Example: User Story 1

```bash
Task: "Add failing exact-three accepted proof-set and live-host tests, then register them in tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj"
Task: "Add failing Synthetic rejection tests, then register them in tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj"
Task: "Add failing live proof command and artifact tests, then register them in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj"
```

## Parallel Example: User Story 2

```bash
Task: "Add failing same-profile parity corpus renderer tests, then register them in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj"
Task: "Add failing cross-profile and stale proof-set gate tests, then register them in tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj"
Task: "Add failing retained damage and fallback diagnostic tests, then register them in tests/Controls.Tests/Controls.Tests.fsproj"
Task: "Add failing Elmish compositor diagnostic tests, then register them in tests/Elmish.Tests/Elmish.Tests.fsproj"
```

## Parallel Example: User Story 3

```bash
Task: "Add failing timing policy and decision tests, then register them in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj"
Task: "Add failing package-visible timing readiness helper tests, then register them in tests/Testing.Tests/Testing.Tests.fsproj"
```

## Parallel Example: User Story 4

```bash
Task: "Add failing final readiness package tests, then register them in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj"
Task: "Add failing compatibility ledger and public drift tests, then register them in tests/Package.Tests/Package.Tests.fsproj"
Task: "Add failing Feature 154 FSI transcript coverage assertions in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
Task: "Add failing final readiness helper tests, then register them in tests/Testing.Tests/Testing.Tests.fsproj"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Stop and validate the exact-three proof-set gate independently with US1 focused tests and the live proof quickstart command.
5. Publish proof evidence as accepted, failed, or environment-limited without claiming partial redraw until US2 passes.

### Incremental Delivery

1. Complete Setup and Foundational contracts.
2. Add US1 proof-set acceptance and validate exact-three capable-host behavior.
3. Add US2 same-profile parity and validate scoped redraw versus full-redraw reference.
4. Add US3 timing decision and validate accepted, rejected, or inconclusive performance claim output.
5. Add US4 final readiness package and validate public compatibility, package, and regression evidence.

### Parallel Team Strategy

1. Team completes Setup and Foundational contracts together.
2. Once Foundational is done, one developer can work US1 proof acceptance, another can work US2 parity with fixtures, and another can work US3 timing with fixtures.
3. US4 starts after evidence shapes from US1-US3 are stable, then publishes the readiness package.

## Notes

- [P] tasks use different files or distinct evidence files and can run in parallel once prerequisites are met.
- [Story] labels map tasks to user stories for traceability.
- Feature 154 does not accept partial redraw without both proof-set acceptance and same-profile parity acceptance.
- Feature 154 does not accept a performance claim unless same-profile timing evidence satisfies the declared policy.
- Synthetic tests are rejection-path evidence only and must be disclosed in test names, `// SYNTHETIC:` use-site comments, readiness notes, and the PR description.
- Unsupported-host runs are valid regression evidence but cannot satisfy proof, parity, or timing acceptance.
