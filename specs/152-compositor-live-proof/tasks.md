# Tasks: Compositor Live Proof Acceptance

**Input**: Design documents from `/specs/152-compositor-live-proof/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Required. The specification mandates independent testing for each user story, and the constitution requires failing-first tests for behavior-changing Tier 1 work.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independent increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User-story label, used only inside user-story phases.
- Every task includes exact repository-relative file paths.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare durable Feature 152 readiness evidence locations and validation placeholders.

- [X] T001 Create readiness directory placeholders in specs/152-compositor-live-proof/readiness/live-proof/.gitkeep, specs/152-compositor-live-proof/readiness/parity/.gitkeep, specs/152-compositor-live-proof/readiness/timing/.gitkeep, and specs/152-compositor-live-proof/readiness/fsi/.gitkeep
- [X] T002 [P] Add live proof evidence index for three capable-host runs and unsupported-host output in specs/152-compositor-live-proof/readiness/live-proof/README.md
- [X] T003 [P] Add live parity evidence index for oracle, scoped, fallback, and environment-limited records in specs/152-compositor-live-proof/readiness/parity/README.md
- [X] T004 [P] Add timing evidence index for scenario repetitions, predeclared threshold/noise policy, snapshot/reuse context-only rules, and claim verdict files in specs/152-compositor-live-proof/readiness/timing/README.md
- [X] T005 [P] Add Feature 152 FSI transcript evidence index in specs/152-compositor-live-proof/readiness/fsi/README.md
- [X] T006 [P] Add package validation evidence placeholder in specs/152-compositor-live-proof/readiness/package-validation.md
- [X] T007 [P] Add focused regression validation evidence placeholder in specs/152-compositor-live-proof/readiness/regression-validation.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Draft public and observable contracts before implementation, preserving the required Spec -> FSI -> semantic tests -> implementation order.

**Critical**: No user-story implementation should begin until these contract surfaces and command routes are sketched.

- [X] T008 Draft Feature 152 live proof run-set, artifact quality, host profile, and readiness tokens in src/SkiaViewer/CompositorProof.fsi
- [X] T009 Draft Feature 152 host capability, scissor/no-clear, readback, and environment-limited proof contract in src/SkiaViewer/Host/OpenGl.fsi
- [X] T010 Draft Feature 152 fallback, damage validity, and proof-gated diagnostic fields in src/Controls/Diagnostics.fsi and src/Controls/RetainedRender.fsi
- [X] T011 Draft Feature 152 frame-level compositor diagnostic fields in src/Controls.Elmish/ControlsElmish.fsi
- [X] T012 Draft Feature 152 consumer readiness helper surface in src/Testing/Testing.fsi
- [X] T013 Draft Feature 152 harness proof, parity, timing, and evidence contracts in tests/Rendering.Harness/Compositor.fsi, tests/Rendering.Harness/Evidence.fsi, and tests/Rendering.Harness/Perf.fsi
- [X] T014 Register placeholder Feature 152 CLI route names for compositor-live-proof, compositor-parity, compositor-timing, and compositor-readiness in tests/Rendering.Harness/Cli.fs

**Checkpoint**: Public contracts and harness route names are ready for failing tests.

---

## Phase 3: User Story 1 - Accept the Live Safety Gate (Priority: P1, MVP)

**Goal**: Accept live partial redraw only after three fresh matching capable-host proof runs, and fail closed for unsupported, stale, blank, synthetic, missing, mismatched, or failed evidence.

**Independent Test**: Run the live proof tests and harness command on a capable host or unsupported environment and verify accepted proof sets require three fresh matching real attempts, while unsupported paths classify as environment-limited with zero accepted artifacts.

### Tests for User Story 1

Write these tests first and verify they fail before implementation.

- [X] T015 [P] [US1] Add failing capable-host live proof run-set tests and Compile entry in tests/SkiaViewer.Tests/Feature152LiveProofTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T016 [US1] Add failing synthetic, unsupported, stale, blank, host-mismatched, and proof-method-mismatched rejection tests and Compile entry in tests/SkiaViewer.Tests/Feature152LiveProofSimulationTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T017 [P] [US1] Add failing live proof evidence aggregation tests and Compile entry in tests/Rendering.Harness.Tests/Feature152LiveProofEvidenceTests.fs and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T018 [P] [US1] Add failing consumer readiness discovery tests for proof package shape and Compile entry in tests/Testing.Tests/Feature152CompositorReadinessTests.fs and tests/Testing.Tests/Testing.Tests.fsproj

### Implementation for User Story 1

- [X] T019 [US1] Implement HostProfile, ProofArtifact, LiveProofAttempt, AcceptedProofSet, and artifact quality validators in src/SkiaViewer/CompositorProof.fs
- [X] T020 [US1] Implement capable-host profile detection, sentinel presentation, damage presentation, scissor/no-clear setup, pixel readback, and environment-limited classification in src/SkiaViewer/Host/OpenGl.fs
- [X] T021 [US1] Implement proof run-set freshness, host/profile matching, proof-method matching, three-run acceptance, and fail-closed reasons in src/SkiaViewer/CompositorProof.fs
- [X] T022 [US1] Implement proof-gated fallback reason mapping for missing, stale, blank, synthetic, failed, limited, host-mismatched, and method-mismatched evidence in src/Controls/Diagnostics.fs and src/Controls/RetainedRender.fs
- [X] T023 [US1] Implement harness live proof orchestration, proof artifact writing, unsupported-host records, and accepted proof-set serialization in tests/Rendering.Harness/Compositor.fs and tests/Rendering.Harness/Evidence.fs
- [X] T024 [US1] Wire `compositor-live-proof --feature 152 --out ...` argument parsing and command dispatch in tests/Rendering.Harness/Cli.fs
- [X] T025 [US1] Generate real or environment-limited proof evidence summaries under specs/152-compositor-live-proof/readiness/live-proof/README.md
- [X] T026 [US1] Run the US1 quickstart proof commands and record the result in specs/152-compositor-live-proof/readiness/live-proof/validation.md

**Checkpoint**: User Story 1 is independently testable and the MVP proof gate can be accepted, failed, or environment-limited without using parity or timing evidence.

---

## Phase 4: User Story 2 - Prove Live Damage-Scoped Visual Parity (Priority: P1)

**Goal**: On the same accepted host profile, prove damage-scoped live output matches the full-redraw oracle for representative frame transitions, while unsafe paths fall back with recorded reasons.

**Independent Test**: Run the parity tests and `compositor-parity` command using an accepted proof fixture or live proof set, then verify every accepted scenario matches the full-redraw oracle and every unsafe scenario records full-redraw fallback or environment-limited status.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T027 [P] [US2] Add failing damage validity, no-change preservation, full invalidation, resize, and fallback reason tests and Compile entry in tests/Controls.Tests/Feature152DamagePlanTests.fs and tests/Controls.Tests/Controls.Tests.fsproj
- [X] T028 [P] [US2] Add failing live damage-scoped redraw parity tests and Compile entry in tests/SkiaViewer.Tests/Feature152DamageScopedRedrawTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T029 [P] [US2] Add failing live parity corpus and oracle evidence tests and Compile entry in tests/Rendering.Harness.Tests/Feature152DamageParityTests.fs and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T030 [P] [US2] Add failing Elmish compositor diagnostic tests for proof, parity, damage, and fallback fields and Compile entry in tests/Elmish.Tests/Feature152CompositorMetricsTests.fs and tests/Elmish.Tests/Elmish.Tests.fsproj

### Implementation for User Story 2

- [X] T031 [US2] Implement accepted-proof gating, damage clipping, no-change handling, full invalidation, invalid damage, and fallback selection in src/Controls/RetainedRender.fs
- [X] T032 [US2] Implement reviewer-visible damage-scoped parity and fallback diagnostics in src/Controls/Diagnostics.fs
- [X] T033 [US2] Implement damage-scoped live rendering path integration with accepted proof profile checks in src/SkiaViewer/SceneRenderer.fs
- [X] T034 [US2] Implement host-profile drift, framebuffer resize, unsupported host, resource failure, and parity-failure fallback routing in src/SkiaViewer/SkiaViewer.fs
- [X] T035 [US2] Implement representative live corpus scenarios, full-redraw oracle generation, scoped artifact generation, and parity comparison in tests/Rendering.Harness/Compositor.fs
- [X] T036 [US2] Wire `compositor-parity --feature 152 --out ...` argument parsing and command dispatch in tests/Rendering.Harness/Cli.fs
- [X] T037 [US2] Implement Controls.Elmish frame diagnostics for proof set id, parity verdict, damage regions, and fallback status in src/Controls.Elmish/ControlsElmish.fs
- [X] T038 [US2] Run the US2 quickstart parity commands and record the result in specs/152-compositor-live-proof/readiness/parity/validation.md

**Checkpoint**: User Story 2 is independently testable with an accepted-proof fixture, and final live acceptance can link same-profile proof and parity evidence.

---

## Phase 5: User Story 3 - Decide the Live Performance Claim (Priority: P2)

**Goal**: Decide whether capable-host timing supports a compositor performance claim, or explicitly reject/mark the claim inconclusive when evidence is incomplete, noisy, environment-limited, or non-beneficial.

**Independent Test**: Run the timing tests and `compositor-timing` command with same-profile proof and parity evidence, then verify accepted claims require at least 5 representative scenarios with at least 5 comparable repetitions per scenario.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T039 [P] [US3] Add failing timing evidence, threshold/noise policy, snapshot/reuse context-only, and performance claim decision tests and Compile entry in tests/Rendering.Harness.Tests/Feature152TimingEvidenceTests.fs and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T040 [P] [US3] Add failing package transcript coverage for Feature 152 timing authoring in tests/Package.Tests/FsiTranscriptCoverageTests.fs

### Implementation for User Story 3

- [X] T041 [US3] Implement TimingEvidence, PerformanceClaimDecision, predeclared threshold/noise policy, warmup, and repetition validation contracts in tests/Rendering.Harness/Perf.fs
- [X] T042 [US3] Implement same-profile full-redraw versus damage-scoped timing collection across at least 5 live scenarios and 5 comparable repetitions in tests/Rendering.Harness/Perf.fs
- [X] T043 [US3] Implement timing verdict classification for accepted-benefit, rejected, inconclusive, environment-limited, noisy, incomplete, host-limited, missing-proof, failed-parity, and non-beneficial cases in tests/Rendering.Harness/Perf.fs
- [X] T044 [US3] Link timing evidence to proof-set, parity, lifecycle, baseline, scoped metrics, and artifact paths, and record snapshot/reuse evidence as context-only unless same-profile live timing exists in tests/Rendering.Harness/Evidence.fs
- [X] T045 [US3] Wire `compositor-timing --feature 152 --tier damage --out ...` argument parsing and command dispatch in tests/Rendering.Harness/Cli.fs
- [X] T046 [US3] Render accepted-claim and no-claim timing summaries in specs/152-compositor-live-proof/readiness/timing/README.md
- [X] T047 [US3] Run the US3 quickstart timing commands and record the result in specs/152-compositor-live-proof/readiness/timing/validation.md

**Checkpoint**: User Story 3 is independently testable and readiness can separate correctness acceptance from the performance claim decision.

---

## Phase 6: User Story 4 - Publish the Final P7 Readiness Decision (Priority: P3)

**Goal**: Publish one reviewable readiness entry point that states whether P7 live partial redraw is accepted, environment-limited, failed, or fallback-gated, with compatibility impact and package validation.

**Independent Test**: Review `validation-summary.md` and package tests to confirm proof, parity, timing, fallback, compatibility, limitations, and regression evidence are linked from one summary with no undocumented public drift.

### Tests for User Story 4

Write these tests first and verify they fail before implementation.

- [X] T048 [P] [US4] Add failing readiness package aggregation tests and Compile entry in tests/Rendering.Harness.Tests/Feature152ReadinessPackageTests.fs and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T049 [P] [US4] Add failing compatibility ledger and package validation tests and Compile entry in tests/Package.Tests/Feature152CompatibilityLedgerTests.fs and tests/Package.Tests/Package.Tests.fsproj
- [X] T050 [P] [US4] Add failing consumer readiness helper status tests and Compile entry in tests/Testing.Tests/Feature152ReadinessHelperTests.fs and tests/Testing.Tests/Testing.Tests.fsproj
- [X] T051 [US4] Add failing Feature 152 FSI transcript coverage assertions in tests/Package.Tests/FsiTranscriptCoverageTests.fs

### Implementation for User Story 4

- [X] T052 [US4] Implement P7 readiness summary discovery, status vocabulary, evidence link validation, and blocking reason helpers in src/Testing/Testing.fs
- [X] T053 [US4] Implement readiness assembly for proof set, parity, timing, fallback, compatibility, limitations, and final status in tests/Rendering.Harness/Evidence.fs
- [X] T054 [US4] Wire `compositor-readiness --feature 152 --out ...` argument parsing and command dispatch in tests/Rendering.Harness/Cli.fs
- [X] T055 [US4] Write compatibility ledger content covering public API, diagnostics, fallback behavior, readiness vocabulary, package surface, and migration impact in specs/152-compositor-live-proof/readiness/compatibility-ledger.md
- [X] T056 [US4] Render final P7 status, evidence links, performance decision, fallback status, limitations, and reviewer path summary in specs/152-compositor-live-proof/readiness/validation-summary.md
- [X] T057 [US4] Add Feature 152 public authoring transcript and run log in specs/152-compositor-live-proof/readiness/fsi/compositor-live-proof-authoring.fsx and specs/152-compositor-live-proof/readiness/fsi/compositor-live-proof-authoring.log
- [X] T058 [US4] Refresh affected public surface baselines in readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt, readiness/surface-baselines/FS.GG.UI.Controls.txt, readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt, and readiness/surface-baselines/FS.GG.UI.Testing.txt
- [X] T059 [US4] Run the US4 quickstart readiness and package validation commands and record the result in specs/152-compositor-live-proof/readiness/package-validation.md

**Checkpoint**: User Story 4 publishes a single final readiness decision with traceable evidence and package validation.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation alignment, synthetic disclosure, and regression proof.

- [X] T060 [P] Update the originating roadmap/report status for P7 live compositor acceptance in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md
- [X] T061 [P] Review and disclose every synthetic Feature 152 test or artifact in tests/SkiaViewer.Tests/Feature152LiveProofSimulationTests.fs and specs/152-compositor-live-proof/readiness/validation-summary.md
- [X] T062 Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature152` and record the result in specs/152-compositor-live-proof/readiness/regression-validation.md
- [X] T063 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature152` and record the result in specs/152-compositor-live-proof/readiness/regression-validation.md
- [X] T064 Run `dotnet test FS.GG.Rendering.slnx --no-restore`, `./fake.sh build -t PackageSurfaceCheck`, and `./fake.sh build -t PackLocal`; record results in specs/152-compositor-live-proof/readiness/package-validation.md
- [X] T065 Verify unsupported-host proof artifacts record zero accepted partial-redraw artifacts in specs/152-compositor-live-proof/readiness/live-proof/unsupported/README.md
- [X] T066 Record focused Feature 149 diagnostics, deterministic readiness, render-anywhere, overlay, text-shaping, layout, package-readiness, and adjacent surface-baseline verdicts as accepted or explicitly limited in specs/152-compositor-live-proof/readiness/regression-validation.md, then refresh the regression link/status in specs/152-compositor-live-proof/readiness/validation-summary.md before final acceptance

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks user-story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational; MVP acceptance gate.
- **User Story 2 (Phase 4)**: Depends on Foundational and can use accepted-proof fixtures, but final acceptance links to User Story 1 evidence.
- **User Story 3 (Phase 5)**: Depends on Foundational for implementation and on User Stories 1 and 2 for a real accepted performance claim.
- **User Story 4 (Phase 6)**: Depends on the desired proof, parity, and timing evidence from User Stories 1-3.
- **Polish (Phase 7)**: Depends on all selected user stories.

### User Story Dependencies

- **US1 (P1)**: No story dependency after Foundational; this is the MVP.
- **US2 (P1)**: Can start after Foundational using fixtures; final readiness requires US1 accepted or environment-limited proof classification.
- **US3 (P2)**: Can start after Foundational using fixtures; accepted performance claims require US1 accepted proof and US2 passed same-profile parity.
- **US4 (P3)**: Requires the evidence produced by US1, US2, and US3 to publish a final decision.

### Dependency Graph

```text
Phase 1 Setup
  -> Phase 2 Foundational
      -> US1 Live Safety Gate (MVP)
      -> US2 Live Parity (fixture-testable after Foundation; final links to US1)
      -> US3 Timing Decision (fixture-testable after Foundation; accepted claim links to US1+US2)
          -> US4 Final Readiness Decision
              -> Phase 7 Polish
```

### Within Each User Story

- Tests first, and they should fail before implementation.
- `.fsi` and contract changes before `.fs` bodies for public or observable surfaces.
- Pure model/update and validators before edge interpreters and CLI wiring.
- Evidence rendering after command behavior exists.
- Story validation command and readiness artifact update last.

## Parallel Opportunities

- Setup evidence index tasks T002-T007 can run in parallel after T001.
- Foundational FSI sketches T008-T013 can be split by package, then T014 wires CLI placeholders.
- US1 test tasks T015, T017, and T018 can run in parallel; T016 shares the SkiaViewer test project file with T015 and should be sequenced.
- US2 test tasks T027-T030 can run in parallel because they touch separate test projects.
- US3 test tasks T039 and T040 can run in parallel.
- US4 test tasks T048-T050 can run in parallel; T051 shares Package.Tests context and should be sequenced after T049.
- Polish tasks T060 and T061 can run in parallel with final validation once readiness output exists; T066 must run after T062-T064 so final acceptance includes focused regression verdicts.

## Parallel Example: User Story 1

```bash
Task: "Add failing capable-host live proof run-set tests in tests/SkiaViewer.Tests/Feature152LiveProofTests.fs"
Task: "Add failing live proof evidence aggregation tests in tests/Rendering.Harness.Tests/Feature152LiveProofEvidenceTests.fs"
Task: "Add failing consumer readiness discovery tests in tests/Testing.Tests/Feature152CompositorReadinessTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "Add failing damage validity and fallback tests in tests/Controls.Tests/Feature152DamagePlanTests.fs"
Task: "Add failing live damage-scoped redraw parity tests in tests/SkiaViewer.Tests/Feature152DamageScopedRedrawTests.fs"
Task: "Add failing live parity corpus tests in tests/Rendering.Harness.Tests/Feature152DamageParityTests.fs"
Task: "Add failing Elmish compositor diagnostic tests in tests/Elmish.Tests/Feature152CompositorMetricsTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "Add failing timing evidence and performance claim tests in tests/Rendering.Harness.Tests/Feature152TimingEvidenceTests.fs"
Task: "Add failing package transcript coverage for Feature 152 timing authoring in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "Add failing readiness package aggregation tests in tests/Rendering.Harness.Tests/Feature152ReadinessPackageTests.fs"
Task: "Add failing compatibility ledger tests in tests/Package.Tests/Feature152CompatibilityLedgerTests.fs"
Task: "Add failing consumer readiness helper status tests in tests/Testing.Tests/Feature152ReadinessHelperTests.fs"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete US1 tests and implementation.
3. Run the US1 proof quickstart commands.
4. Stop and validate whether the environment produced accepted, failed, or environment-limited proof evidence.

### Incremental Delivery

1. Deliver US1 to establish the live safety gate.
2. Deliver US2 to prove same-profile live parity and fallback behavior.
3. Deliver US3 to decide the performance claim without blocking correctness acceptance.
4. Deliver US4 to publish the single readiness decision and compatibility package.
5. Complete Phase 7 to update reports, disclose synthetic evidence, and record regression/package validation.

### Parallel Team Strategy

1. Complete Setup and Foundational together.
2. Split tests by package: SkiaViewer, Controls, Elmish, Rendering.Harness, Testing, Package.
3. Keep implementation ownership aligned to package boundaries: `SkiaViewer` for host proof/readback, `Controls` for damage/fallback policy, `Controls.Elmish` for adapter diagnostics, `Rendering.Harness` for orchestration/evidence, and `Testing` for consumer helpers.
4. Merge by evidence gate: US1 proof, US2 parity, US3 timing, then US4 readiness.

## Notes

- Feature 149 deterministic readiness and Feature 151 P8 layout acceptance are baseline evidence, not reopened scope.
- Synthetic tests are allowed for rejection paths only and must remain visibly disclosed.
- Environment-limited evidence must never count as accepted partial-redraw or performance evidence.
- Accepted performance claims require same-profile proof, parity, and timing evidence; correctness acceptance can still ship with a rejected or inconclusive performance claim.
