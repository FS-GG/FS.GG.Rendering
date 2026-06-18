# Tasks: Compositor Proof Interpreter

**Input**: Design documents from `/specs/153-compositor-proof-interpreter/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Required. The specification mandates independent testing for each user story, and the constitution requires failing-first tests for behavior-changing Tier 1 work.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independent increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User-story label, used only inside user-story phases.
- Every task includes exact repository-relative file paths.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare durable Feature 153 readiness locations and validation placeholders.

- [X] T001 Create Feature 153 readiness directory placeholders in specs/153-compositor-proof-interpreter/readiness/live-proof/attempts/.gitkeep, specs/153-compositor-proof-interpreter/readiness/live-proof/unsupported/.gitkeep, and specs/153-compositor-proof-interpreter/readiness/fsi/.gitkeep
- [X] T002 [P] Add live proof evidence index for capable-host attempts and unsupported-host output in specs/153-compositor-proof-interpreter/readiness/live-proof/README.md
- [X] T003 [P] Add proof-set decision placeholder describing accepted, fallback-gated, failed, and environment-limited statuses in specs/153-compositor-proof-interpreter/readiness/proof-set.md
- [X] T004 [P] Add validation summary placeholder for proof-set status, fallback status, remaining parity and timing gates, and reviewer links in specs/153-compositor-proof-interpreter/readiness/validation-summary.md
- [X] T005 [P] Add compatibility ledger placeholder for Tier 1 public API, diagnostics, fallback, readiness vocabulary, and package impact in specs/153-compositor-proof-interpreter/readiness/compatibility-ledger.md
- [X] T006 [P] Add package validation evidence placeholder in specs/153-compositor-proof-interpreter/readiness/package-validation.md
- [X] T007 [P] Add focused regression validation evidence placeholder in specs/153-compositor-proof-interpreter/readiness/regression-validation.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Draft public and observable contracts before implementation, preserving the required Spec -> FSI -> semantic tests -> implementation order.

**Critical**: No user-story implementation should begin until these contract surfaces, command routes, and package-shaped FSI transcript coverage are sketched and expected to fail before implementation.

- [X] T008 Draft Feature 153 live proof attempt, frame artifact, proof method, selected proof-set, and readiness tokens in src/SkiaViewer/CompositorProof.fsi
- [X] T009 Draft Feature 153 OpenGL host detection, presentation, readback, artifact capture, and environment-limit contracts in src/SkiaViewer/Host/OpenGl.fsi
- [X] T010 Draft Feature 153 viewer-host interpreter contract for live proof effects in src/SkiaViewer/Host/Viewer.fsi
- [X] T011 Draft Feature 153 harness constants, proof attempt summaries, proof-set decision, and readiness renderer contracts in tests/Rendering.Harness/Compositor.fsi
- [X] T012 Draft Feature 153 live harness execution contract for capable and unsupported host runs in tests/Rendering.Harness/Live.fsi
- [X] T013 Draft Feature 153 consumer readiness helper contract for package-visible validation changes in src/Testing/Testing.fsi and add matching package-shaped FSI transcript coverage assertions in tests/Package.Tests/FsiTranscriptCoverageTests.fs before any .fs implementation changes
- [X] T014 Register Feature 153 CLI feature detection and placeholder command branches in tests/Rendering.Harness/Cli.fs
- [X] T015 Add Feature 153 test Compile entries to tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj, tests/Package.Tests/Package.Tests.fsproj, and tests/Testing.Tests/Testing.Tests.fsproj

**Checkpoint**: Public contracts, harness contracts, and command routes are ready for failing tests.

---

## Phase 3: User Story 1 - Produce Real Live Proof Attempts (Priority: P1, MVP)

**Goal**: Run real host-backed proof attempts that capture sentinel and damage-frame evidence, validate artifact quality, and classify each attempt as accepted, failed, or environment-limited.

**Independent Test**: Run the live proof on a capable host and verify that each attempt records host identity, proof method, sentinel and damage artifacts, freshness, artifact quality, pixel observations, and classification.

### Tests for User Story 1

Write these tests first and verify they fail before implementation.

- [X] T016 [P] [US1] Add failing proof attempt MVU transition and artifact-quality tests in tests/SkiaViewer.Tests/Feature153LiveProofInterpreterTests.fs
- [X] T017 [P] [US1] Add failing capable-host profile, sentinel frame, damage frame, damaged-sample, and undamaged-sample tests in tests/SkiaViewer.Tests/Feature153LiveProofHostTests.fs
- [X] T018 [P] [US1] Add failing live proof command artifact tests for `compositor-live-proof --feature 153 --attempt-count 1` in tests/Rendering.Harness.Tests/Feature153LiveProofEvidenceTests.fs
- [X] T019 [US1] Run failing US1 focused tests and record the expected failures in specs/153-compositor-proof-interpreter/readiness/live-proof/validation.md

### Implementation for User Story 1

- [X] T020 [US1] Implement Feature 153 attempt state, frame artifact, proof method, sample observation, and artifact quality records in src/SkiaViewer/CompositorProof.fs
- [X] T021 [US1] Implement pure attempt `init`, `update`, `classifyObservations`, freshness, quality, and fail-closed reason logic in src/SkiaViewer/CompositorProof.fs
- [X] T022 [US1] Implement capable-host profile detection, sentinel presentation, damage presentation, readback sampling, and artifact capture effects in src/SkiaViewer/Host/OpenGl.fs
- [X] T023 [US1] Integrate live proof interpreter effect execution through viewer host boundaries in src/SkiaViewer/Host/Viewer.fs and src/SkiaViewer/SkiaViewer.fs
- [X] T024 [US1] Implement harness live proof attempt orchestration, attempt-count handling, and per-attempt artifact directory writing in tests/Rendering.Harness/Live.fs
- [X] T025 [US1] Implement Feature 153 live proof Markdown rendering for attempts and artifact quality in tests/Rendering.Harness/Compositor.fs
- [X] T026 [US1] Wire `compositor-live-proof --feature 153 --attempt-count <n> --out <dir>` command dispatch in tests/Rendering.Harness/Cli.fs
- [X] T027 [US1] Generate capable-host or environment-limited US1 proof evidence in specs/153-compositor-proof-interpreter/readiness/live-proof/attempts/README.md and specs/153-compositor-proof-interpreter/readiness/live-proof/validation.md

**Checkpoint**: User Story 1 is independently testable and each proof attempt has reviewable live evidence or a fail-closed reason.

---

## Phase 4: User Story 2 - Preserve Unsupported-Host Safety (Priority: P1)

**Goal**: Keep unsupported or unavailable presentation environments explicit, environment-limited or failed, and non-accepting with zero accepted partial-redraw artifacts.

**Independent Test**: Run the proof with no capable presentation environment and verify that it records the blocking cause, completes under 2 minutes, and accepts no partial-redraw artifacts.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T028 [P] [US2] Add failing unsupported-host, missing-display, missing-renderer, permission, timeout, under-2-minute elapsed-time, and readback-limited classifier tests in tests/SkiaViewer.Tests/Feature153LiveProofSimulationTests.fs
- [X] T029 [P] [US2] Add failing unsupported-host harness command tests for unset DISPLAY, WAYLAND_DISPLAY, and XDG_SESSION_TYPE in tests/Rendering.Harness.Tests/Feature153LiveProofEvidenceTests.fs
- [X] T030 [P] [US2] Add failing zero-accepted partial-redraw readiness helper tests in tests/Testing.Tests/Feature153ReadinessHelperTests.fs

### Implementation for User Story 2

- [X] T031 [US2] Implement unsupported display, missing GL renderer, readback failure, permission, and timeout classification in src/SkiaViewer/Host/OpenGl.fs
- [X] T032 [US2] Ensure unsupported or unavailable host attempts emit `environment-limited` or `failed` and never `accepted` in src/SkiaViewer/CompositorProof.fs
- [X] T033 [US2] Implement unsupported-host evidence writing and zero accepted artifact summaries in tests/Rendering.Harness/Live.fs and tests/Rendering.Harness/Compositor.fs
- [X] T034 [US2] Wire unsupported-host output defaults for `compositor-live-proof --feature 153` in tests/Rendering.Harness/Cli.fs
- [X] T035 [US2] Implement consumer-visible unsupported-host readiness helper behavior in src/Testing/Testing.fs
- [X] T036 [US2] Generate unsupported-host evidence in specs/153-compositor-proof-interpreter/readiness/live-proof/unsupported/README.md and record command output in specs/153-compositor-proof-interpreter/readiness/live-proof/unsupported/validation.md

**Checkpoint**: User Story 2 is independently testable and unsupported hosts cannot accept partial redraw.

---

## Phase 5: User Story 3 - Aggregate the Three-Run Proof Set (Priority: P2)

**Goal**: Produce one proof-set decision over exactly three fresh matching capable-host attempts, failing closed for missing, stale, mixed, mismatched, or non-accepting evidence.

**Independent Test**: Run three proof attempts on the same capable host profile and verify that the proof set accepts only when all three attempts match and individually pass.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T037 [P] [US3] Add failing exact-three proof-set acceptance and fewer-than-three fallback tests in tests/SkiaViewer.Tests/Feature153LiveProofSimulationTests.fs
- [X] T038 [P] [US3] Add failing host-profile, proof-method, stale, mixed-verdict, and four-attempt selected-trio tests in tests/SkiaViewer.Tests/Feature153LiveProofInterpreterTests.fs
- [X] T039 [P] [US3] Add failing proof-set Markdown, selected-attempt, and artifact-link tests in tests/Rendering.Harness.Tests/Feature153LiveProofEvidenceTests.fs

### Implementation for User Story 3

- [X] T040 [US3] Extend proof-set signature with explicit selected attempt ids, freshness window, status reasons, and diagnostics in src/SkiaViewer/CompositorProof.fsi
- [X] T041 [US3] Implement exact three-attempt proof-set evaluation, selected-trio identity, extra-attempt handling, and fail-closed reason priority in src/SkiaViewer/CompositorProof.fs
- [X] T042 [US3] Implement proof-attempt loading, matching, aggregation, and `proof-set.md` rendering in tests/Rendering.Harness/Compositor.fs
- [X] T043 [US3] Wire proof-set aggregation into `compositor-readiness --feature 153 --out <dir>` in tests/Rendering.Harness/Cli.fs
- [X] T044 [US3] Generate or refresh aggregate proof-set evidence in specs/153-compositor-proof-interpreter/readiness/proof-set.md
- [X] T045 [US3] Record US3 validation outcome and missing, limited, or accepted reasons in specs/153-compositor-proof-interpreter/readiness/live-proof/validation.md

**Checkpoint**: User Story 3 is independently testable and proof-set acceptance names exactly three fresh matching attempts.

---

## Phase 6: User Story 4 - Publish Reviewable Proof Readiness (Priority: P3)

**Goal**: Publish one reviewable readiness entry point that links live attempts, proof-set status, unsupported-host behavior, fallback status, compatibility impact, package validation, and remaining parity and timing gates.

**Independent Test**: Review the readiness package and confirm that it links proof attempts, proof-set status, unsupported-host behavior, fallback status, compatibility, package validation, and remaining gates from one place.

### Tests for User Story 4

Write these tests first and verify they fail before implementation.

- [X] T046 [P] [US4] Add failing readiness summary aggregation tests in tests/Rendering.Harness.Tests/Feature153ReadinessPackageTests.fs
- [X] T047 [P] [US4] Add failing compatibility ledger and package validation tests in tests/Package.Tests/Feature153CompatibilityLedgerTests.fs
- [X] T048 [P] [US4] Add failing Feature 153 consumer readiness helper tests in tests/Testing.Tests/Feature153ReadinessHelperTests.fs
- [X] T049 [US4] Extend the foundational Feature 153 FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs only for US4-specific public readiness deltas not already covered by T013

### Implementation for User Story 4

- [X] T050 [US4] Implement Feature 153 readiness summary renderer linking attempts, proof set, unsupported-host evidence, fallback status, compatibility, package validation, and regression evidence in tests/Rendering.Harness/Compositor.fs
- [X] T051 [US4] Implement Feature 153 compatibility ledger renderer for public API, diagnostics, fallback, readiness vocabulary, and migration impact in tests/Rendering.Harness/Compositor.fs
- [X] T052 [US4] Wire `compositor-readiness --feature 153 --out <dir>` to write validation-summary.md, compatibility-ledger.md, proof-set.md, package-validation.md, and regression-validation.md in tests/Rendering.Harness/Cli.fs
- [X] T053 [US4] Implement or update public readiness file discovery and evidence status helpers in src/Testing/Testing.fs
- [X] T054 [US4] Add Feature 153 public authoring transcript and run log in specs/153-compositor-proof-interpreter/readiness/fsi/compositor-proof-interpreter-authoring.fsx and specs/153-compositor-proof-interpreter/readiness/fsi/compositor-proof-interpreter-authoring.log
- [X] T055 [US4] Refresh affected public surface baselines in readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt and readiness/surface-baselines/FS.GG.UI.Testing.txt
- [X] T056 [US4] Write compatibility ledger content in specs/153-compositor-proof-interpreter/readiness/compatibility-ledger.md
- [X] T057 [US4] Write final readiness summary in specs/153-compositor-proof-interpreter/readiness/validation-summary.md
- [X] T058 [US4] Record package validation in specs/153-compositor-proof-interpreter/readiness/package-validation.md

**Checkpoint**: User Story 4 publishes a single proof-readiness decision without implying partial-redraw or performance acceptance.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation alignment, synthetic disclosure, package checks, and regression proof.

- [X] T059 [P] Update the originating P7 report status for Feature 153 live proof interpreter readiness in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md
- [X] T060 [P] Review and disclose every synthetic Feature 153 test or artifact in tests/SkiaViewer.Tests/Feature153LiveProofSimulationTests.fs and specs/153-compositor-proof-interpreter/readiness/validation-summary.md, verifying `Synthetic` appears in each synthetic test name, a `// SYNTHETIC:` use-site comment names the reason, and the PR description has a synthetic-evidence list
- [X] T061 Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature153 --no-build` and record the result in specs/153-compositor-proof-interpreter/readiness/regression-validation.md
- [X] T062 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature153 --no-build` and record the result in specs/153-compositor-proof-interpreter/readiness/regression-validation.md
- [X] T063 Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature153 --no-build` and `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature153 --no-build`, then record results in specs/153-compositor-proof-interpreter/readiness/package-validation.md
- [X] T064 Run unsupported-host quickstart command and record elapsed time, under-2-minute status, zero accepted artifacts, and result in specs/153-compositor-proof-interpreter/readiness/live-proof/unsupported/validation.md
- [X] T065 Run capable-host quickstart command when a usable OpenGL presentation host is available, or record the environment limitation in specs/153-compositor-proof-interpreter/readiness/live-proof/attempts/validation.md
- [X] T066 Run `dotnet test FS.GG.Rendering.slnx --no-restore` and record the result in specs/153-compositor-proof-interpreter/readiness/regression-validation.md
- [X] T067 Run `./fake.sh build -t PackageSurfaceCheck` and `./fake.sh build -t PackLocal`, or record the absent `fake.sh` tooling limitation in specs/153-compositor-proof-interpreter/readiness/package-validation.md
- [X] T068 Verify final readiness links, zero accepted partial-redraw claims for unsupported hosts, fallback-gated parity language, and no performance claim in specs/153-compositor-proof-interpreter/readiness/validation-summary.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks user-story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational; MVP live proof attempt interpreter.
- **User Story 2 (Phase 4)**: Depends on Foundational and can use unsupported-host fixtures; final readiness links to US1 evidence when available.
- **User Story 3 (Phase 5)**: Depends on Foundational and uses attempts from US1 for real accepted proof-set decisions.
- **User Story 4 (Phase 6)**: Depends on the desired proof, unsupported-host, and proof-set evidence from US1-US3.
- **Polish (Phase 7)**: Depends on all selected user stories.

### User Story Dependencies

- **US1 (P1)**: No story dependency after Foundational; this is the MVP.
- **US2 (P1)**: No story dependency after Foundational for unsupported-host classification; final readiness should include US1 evidence if it exists.
- **US3 (P2)**: Can be implemented with fixtures after Foundational; accepted proof-set evidence requires US1 capable-host attempts.
- **US4 (P3)**: Requires whichever US1-US3 evidence exists to publish a truthful readiness decision.

### Dependency Graph

```text
Phase 1 Setup
  -> Phase 2 Foundational
      -> US1 Real Live Proof Attempts (MVP)
      -> US2 Unsupported-Host Safety
      -> US3 Three-Run Proof Set with fixtures
US1 capable-host attempts -> US3 accepted proof-set evidence
US1/US2/US3 selected evidence -> US4 Reviewable Proof Readiness
US4 -> Phase 7 Polish
```

### Within Each User Story

- Tests first, and they should fail before implementation.
- `.fsi`, contract changes, and package-shaped FSI transcript coverage before `.fs` bodies for public or observable surfaces.
- Pure model/update and validators before edge interpreters and CLI wiring.
- Evidence rendering after command behavior exists.
- Story validation command and readiness artifact update last.

## Parallel Opportunities

- Setup evidence tasks T002-T007 can run in parallel after T001.
- Foundational signature and FSI transcript tasks T008-T013 can be split by package, then T014-T015 wire command and test registration.
- US1 test tasks T016-T018 can run in parallel; T019 records the failing baseline after those tests exist.
- US1 implementation tasks T020-T022 can be split between pure proof classification and OpenGL host capture; T023-T027 sequence integration and evidence output.
- US2 test tasks T028-T030 can run in parallel because they touch separate test concerns.
- US3 test tasks T037-T039 can run in parallel because they touch distinct proof-set behavior areas.
- US4 test tasks T046-T048 can run in parallel; T049 should follow T047 because it extends package transcript coverage.
- Polish tasks T059-T060 can run in parallel with final validation preparation; T068 must run after readiness files exist.

## Parallel Example: User Story 1

```bash
Task: "Add failing proof attempt MVU transition and artifact-quality tests in tests/SkiaViewer.Tests/Feature153LiveProofInterpreterTests.fs"
Task: "Add failing capable-host profile and pixel sample tests in tests/SkiaViewer.Tests/Feature153LiveProofHostTests.fs"
Task: "Add failing live proof command artifact tests in tests/Rendering.Harness.Tests/Feature153LiveProofEvidenceTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "Add failing unsupported-host classifier tests in tests/SkiaViewer.Tests/Feature153LiveProofSimulationTests.fs"
Task: "Add failing unsupported-host harness command tests in tests/Rendering.Harness.Tests/Feature153LiveProofEvidenceTests.fs"
Task: "Add failing zero-accepted partial-redraw helper tests in tests/Testing.Tests/Feature153ReadinessHelperTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "Add failing exact-three proof-set acceptance tests in tests/SkiaViewer.Tests/Feature153LiveProofSimulationTests.fs"
Task: "Add failing selected-trio and mismatch tests in tests/SkiaViewer.Tests/Feature153LiveProofInterpreterTests.fs"
Task: "Add failing proof-set Markdown tests in tests/Rendering.Harness.Tests/Feature153LiveProofEvidenceTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "Add failing readiness summary aggregation tests in tests/Rendering.Harness.Tests/Feature153ReadinessPackageTests.fs"
Task: "Add failing compatibility ledger tests in tests/Package.Tests/Feature153CompatibilityLedgerTests.fs"
Task: "Add failing consumer readiness helper tests in tests/Testing.Tests/Feature153ReadinessHelperTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Stop and validate live proof attempts independently with the US1 focused tests and quickstart proof command.
5. Publish the attempt evidence as accepted, failed, or environment-limited without claiming partial redraw.

### Incremental Delivery

1. Complete Setup and Foundational contracts.
2. Add US1 live proof attempts and validate attempt classifications.
3. Add US2 unsupported-host safety and validate zero accepted artifacts.
4. Add US3 proof-set aggregation and validate exact three-run acceptance rules.
5. Add US4 readiness publication and validate compatibility, package, and regression evidence.

### Parallel Team Strategy

1. Team completes Setup and Foundational contracts together.
2. Once Foundational is done, one developer can work US1 live attempts, another can work US2 unsupported-host safety, and another can start US3 proof-set logic with fixtures.
3. US4 starts after evidence shapes from US1-US3 are stable, then publishes the readiness package.

## Notes

- [P] tasks use different files or distinct evidence files and can run in parallel once prerequisites are met.
- [Story] labels map tasks to user stories for traceability.
- Feature 153 does not enable partial redraw by default and does not accept a compositor performance claim.
- Synthetic tests are rejection-path evidence only and must be disclosed in test names, `// SYNTHETIC:` use-site comments, readiness notes, and the PR description.
- Unsupported-host runs are valid regression evidence but cannot satisfy proof-set acceptance.
