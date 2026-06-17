# Tasks: Compositor Damage Redraw

**Input**: Design documents from `/specs/147-compositor-damage-redraw/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required. The specification declares mandatory user scenarios, test evidence, full-redraw oracle parity, real host proof or environment-limited disclosure, performance probes, package surface checks, and readiness evidence.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified as an independently testable increment.

**Current status (2026-06-18)**: The completed items represent the deterministic proof contract,
damage/scissor policy, derived diagnostics, harness commands, package coverage, readiness artifacts,
and documentation slice. Remaining unchecked items require live OpenGL sentinel/readback proof,
SceneRenderer/SkiaViewer full-redraw fallback integration, full content/placement tracking, snapshot
composition, and real host timing evidence before P7 can claim shipped compositor performance.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create readiness locations and baseline evidence files before implementation begins.

- [x] T001 Create readiness placeholder files in specs/147-compositor-damage-redraw/readiness/present-proof/.gitkeep, specs/147-compositor-damage-redraw/readiness/parity/.gitkeep, and specs/147-compositor-damage-redraw/readiness/perf/.gitkeep
- [x] T002 Create the compatibility ledger skeleton in specs/147-compositor-damage-redraw/readiness/compatibility-ledger.md
- [x] T003 Create the validation summary skeleton in specs/147-compositor-damage-redraw/readiness/validation-summary.md
- [x] T004 [P] Create the compositor corpus and threshold inventory in specs/147-compositor-damage-redraw/readiness/corpus.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared corpus, threshold, artifact, and assertion scaffolding that all compositor stories use.

**Critical**: No user story implementation should start until this phase is complete, including
recorded target host profiles, stable corpus IDs, accepted thresholds, snapshot resource budgets,
and skipped environment categories.

- [x] T005 Draft shared compositor corpus identifiers, target host profiles, tier names, accepted thresholds, snapshot budget values, and artifact path contracts in tests/Rendering.Harness/Compositor.fsi without depending on Evidence or Perf modules
- [x] T006 Implement shared compositor corpus identifiers, target host profiles, tier names, accepted thresholds, snapshot budget values, deterministic scenario IDs, and artifact path helpers in tests/Rendering.Harness/Compositor.fs
- [x] T007 Add tests/Rendering.Harness/Compositor.fsi and tests/Rendering.Harness/Compositor.fs to tests/Rendering.Harness/Rendering.Harness.fsproj before Evidence.fsi and before Perf.fsi, preserving compile order Compositor -> Evidence -> Perf -> Cli
- [x] T008 [P] Add reusable Feature147 assertion helpers for host profiles, parity verdicts, performance thresholds, and readiness paths in tests/Rendering.Harness/TestAssertions.fs
- [x] T009 [P] Create Feature147 compatibility ledger test scaffolding in tests/Package.Tests/Feature147CompatibilityLedgerTests.fs and add it to tests/Package.Tests/Package.Tests.fsproj before Tests.fs
- [x] T010 [P] Record exact target host profiles, scenario IDs, corpus membership, accepted thresholds, snapshot resource budgets, and skipped environment categories in specs/147-compositor-damage-redraw/readiness/corpus.md

**Checkpoint**: Shared target host profiles, scenario IDs, readiness paths, thresholds, snapshot
budgets, skipped-environment categories, and compatibility scaffolding exist.

---

## Phase 3: User Story 1 - Prove Partial Redraw Is Safe (Priority: P1) MVP

**Goal**: Maintainers can run a present-path proof that reports passed, failed, or environment-limited for the active host profile before any partial redraw readiness is accepted.

**Independent Test**: Run the present-path proof against capable, fresh-clearing, unsupported, stale, and host-mismatched profiles; only a passed matching proof can enable partial redraw readiness.

### Public Surface for User Story 1

- [x] T011 [US1] Define HostProfile, PresentProof, proof verdicts, readiness validation, MVU Model/Msg/Effect, init, update, and interpreter contracts in src/SkiaViewer/CompositorProof.fsi

### Tests for User Story 1

- [x] T012 [P] [US1] Add CompositorProof FSI transcript coverage for host profiles, verdicts, readiness validation, init, and update in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [x] T013 [US1] Add present-path proof MVU, capable-host, fresh-clearing, unsupported-observation, stale-proof, host-mismatch, and deterministic scenario tests in tests/SkiaViewer.Tests/Feature147PresentPathProofTests.fs
- [x] T014 [P] [US1] Add harness proof artifact and verdict formatting tests in tests/Rendering.Harness.Tests/Feature147CompositorEvidenceTests.fs
- [x] T015 [US1] Add tests/SkiaViewer.Tests/Feature147PresentPathProofTests.fs and tests/Rendering.Harness.Tests/Feature147CompositorEvidenceTests.fs to tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 1

- [x] T016 [US1] Add src/SkiaViewer/CompositorProof.fsi and src/SkiaViewer/CompositorProof.fs to src/SkiaViewer/SkiaViewer.fsproj after PresentMode.fs and before Host/Diagnostics.fsi
- [x] T017 [US1] Implement host profile detection, proof identity, proof freshness, and readiness validation in src/SkiaViewer/CompositorProof.fs
- [x] T018 [US1] Implement PresentProof Model, Msg, Effect, init, and pure update transitions in src/SkiaViewer/CompositorProof.fs
- [ ] T019 [US1] Implement the GL sentinel/damage proof interpreter, scissored second-frame draw, readback observation, and failure classification in src/SkiaViewer/Host/OpenGl.fs and src/SkiaViewer/CompositorProof.fs
- [x] T020 [US1] Add the compositor-present-proof harness command, output argument parsing, and readiness/present-proof artifact writing in tests/Rendering.Harness/Compositor.fs and tests/Rendering.Harness/Cli.fs
- [x] T021 [US1] Record present proof artifact schema, accepted host facts, and environment-limited disclosure rules in specs/147-compositor-damage-redraw/readiness/present-proof/README.md

**Checkpoint**: User Story 1 can be validated with `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature147PresentPathProof` and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-present-proof --out specs/147-compositor-damage-redraw/readiness/present-proof`.

---

## Phase 4: User Story 2 - Repaint Only Damaged Regions Without Changing Pixels (Priority: P1)

**Goal**: Maintainers can enable damage-scissored redraw after a passed proof; changed regions are repainted, untouched regions are preserved, and every accepted frame matches the full-redraw oracle.

**Independent Test**: Render the damage corpus with full redraw and proof-gated scissored redraw, then verify visual parity, union-area diagnostics, full-frame invalidation fallback, and scissor state reset.

### Public Surface for User Story 2

- [ ] T022 [US2] Define damage region, union area, full-frame invalidation, fallback reason, scissor metrics, and diagnostics additions in src/Controls/RetainedRender.fsi, src/Controls/Diagnostics.fsi, and src/Controls.Elmish/ControlsElmish.fsi

### Tests for User Story 2

- [x] T023 [P] [US2] Add RetainedRender/Diagnostics FSI semantic coverage plus localized update, overlapping damage, full-frame invalidation, empty-idle damage, and union-area tests in tests/Controls.Tests/Feature147DamageUnionTests.fs
- [x] T024 [P] [US2] Add proof-gated scissor redraw, full-redraw oracle parity, fallback, frame-edge damage, and scissor reset tests in tests/SkiaViewer.Tests/Feature147ScissorRedrawTests.fs
- [ ] T025 [US2] Add compositor-parity harness evidence tests and SkiaViewer/ControlsElmish FSI transcript coverage for localized, scrolling, resize, theme, overlapping damage, unsupported proof, and parity failure cases in tests/Rendering.Harness.Tests/Feature147CompositorEvidenceTests.fs and tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [x] T026 [US2] Add tests/Controls.Tests/Feature147DamageUnionTests.fs and tests/SkiaViewer.Tests/Feature147ScissorRedrawTests.fs to tests/Controls.Tests/Controls.Tests.fsproj and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj before Program.fs

### Implementation for User Story 2

- [x] T027 [US2] Implement clipped damage rectangles, deduplication, union area, full-frame invalidation causes, and deterministic damage evidence in src/Controls/RetainedRender.fs
- [x] T028 [US2] Implement damage and fallback diagnostics in src/Controls/Diagnostics.fs and expose deterministic frame metrics in src/Controls.Elmish/ControlsElmish.fs
- [ ] T029 [US2] Implement proof-gated scissor setup, damage-union coverage, no-clear scissored draws, and scissor reset around rendering in src/SkiaViewer/SceneRenderer.fs and src/SkiaViewer/Host/OpenGl.fs
- [ ] T030 [US2] Implement full-redraw fallback paths and user-visible fallback diagnostics for missing, stale, failed, host-mismatched, disabled, or full-frame invalidation cases in src/SkiaViewer/SkiaViewer.fs and src/SkiaViewer/SkiaViewer.fsi
- [x] T031 [US2] Add the compositor-parity harness command, full-redraw oracle comparison, parity output, and readiness/parity artifact writing in tests/Rendering.Harness/Compositor.fs and tests/Rendering.Harness/Cli.fs
- [x] T032 [US2] Record damage parity schema, corpus coverage, fallback categories, and scissor-state reset evidence expectations in specs/147-compositor-damage-redraw/readiness/parity/README.md

**Checkpoint**: User Story 2 can be validated with `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature147Damage`, `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature147ScissorRedraw`, and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-parity --out specs/147-compositor-damage-redraw/readiness/parity`.

---

## Phase 5: User Story 3 - Reuse Stable and Moving Content Safely (Priority: P2)

**Goal**: Maintainers can promote stable boundaries, reuse placement-only movement without stale content, and demote churning or unprofitable boundaries.

**Independent Test**: Run stable, moving-only, content-changing, scrolling, and churning sequences; verify promotion, demotion, parity, old/new placement damage, and work-reduction metrics.

### Public Surface for User Story 3

- [ ] T033 [US3] Define compositor boundary, content identity, placement identity, promotion decision, demotion reason, and reuse metric additions in src/Controls/RetainedRender.fsi and src/Controls/Diagnostics.fsi

### Tests for User Story 3

- [x] T034 [P] [US3] Add RetainedRender/Diagnostics FSI semantic coverage plus stable promotion, placement-only movement, content-change rejection, churn demotion, no-benefit demotion, and deterministic decision tests in tests/Controls.Tests/Feature147PromotionReuseTests.fs
- [x] T035 [P] [US3] Add ControlsElmish FSI transcript coverage and compositor frame metric tests for promotion counts, reuse hits and misses, demotion reasons, repeated-work reduction, and overhead reporting in tests/Elmish.Tests/Feature147CompositorMetricsTests.fs and tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [x] T036 [US3] Add tests/Controls.Tests/Feature147PromotionReuseTests.fs and tests/Elmish.Tests/Feature147CompositorMetricsTests.fs to tests/Controls.Tests/Controls.Tests.fsproj and tests/Elmish.Tests/Elmish.Tests.fsproj before Program.fs

### Implementation for User Story 3

- [ ] T037 [US3] Implement separate content identity and placement identity tracking for retained compositor boundaries in src/Controls/RetainedRender.fs
- [x] T038 [US3] Implement stability observation windows, promotion eligibility, expected saved work, measured overhead, keep/reject/demote decisions, and deterministic decision records in src/Controls/RetainedRender.fs
- [ ] T039 [US3] Implement placement-only movement reuse with old/new region damage, content-change stale-output rejection, and promotion diagnostics in src/Controls/RetainedRender.fs and src/Controls/Diagnostics.fs
- [x] T040 [US3] Surface promotion, reuse, demotion, and work-reduction metrics through src/Controls.Elmish/ControlsElmish.fsi and src/Controls.Elmish/ControlsElmish.fs
- [ ] T041 [US3] Extend stable, moving, content-changing, and churning compositor corpus execution in tests/Rendering.Harness/Compositor.fs
- [x] T042 [US3] Record promotion, placement reuse, demotion, and repeated-work reduction evidence expectations in specs/147-compositor-damage-redraw/readiness/parity/promotion.md

**Checkpoint**: User Story 3 can be validated with `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature147Promotion` and `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature147CompositorMetrics`.

---

## Phase 6: User Story 5 - Review Compositor Readiness Evidence (Priority: P2)

**Goal**: Release reviewers can inspect one readiness package connecting proof, parity, promotion, performance, diagnostics, limitations, and compatibility impact.

**Independent Test**: Assemble readiness from representative passed, failed, rejected, skipped, and environment-limited records; reviewers can identify ready, limited, rejected, and skipped tiers within 10 minutes.

### Public Surface for User Story 5

- [ ] T043 [US5] Define readiness package records, tier verdicts, compatibility impact records, MVU Model/Msg/Effect, init, update, and formatter contracts in tests/Rendering.Harness/Compositor.fsi and tests/Rendering.Harness/Evidence.fsi

### Tests for User Story 5

- [ ] T044 [P] [US5] Add Compositor/Evidence FSI transcript coverage and readiness evaluation tests for ready, limited, rejected, skipped, stale, Synthetic, host-mismatched, missing-proof, and failed-parity evidence in tests/Rendering.Harness.Tests/Feature147CompositorReadinessTests.fs and tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [x] T045 [P] [US5] Add compatibility ledger tests for public metric deltas, diagnostics, surface baseline references, release notes, and migration guidance in tests/Package.Tests/Feature147CompatibilityLedgerTests.fs
- [x] T046 [US5] Add tests/Rendering.Harness.Tests/Feature147CompositorReadinessTests.fs to tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 5

- [x] T047 [US5] Implement readiness Model, Msg, Effect, init, pure update transitions, tier verdict evaluation, and failure classification in tests/Rendering.Harness/Compositor.fs
- [ ] T048 [US5] Implement validation-summary and compatibility-ledger formatting, artifact path validation, and limitation disclosure in tests/Rendering.Harness/Evidence.fs
- [x] T049 [US5] Add the compositor-readiness harness command, output argument parsing, and readiness artifact writing in tests/Rendering.Harness/Compositor.fs and tests/Rendering.Harness/Cli.fs
- [x] T050 [US5] Populate ready, limited, rejected, skipped, fallback, proof, parity, performance, and compatibility sections in specs/147-compositor-damage-redraw/readiness/validation-summary.md
- [x] T051 [US5] Populate public metrics, diagnostics, baseline deltas, release notes, migration guidance, and limitation sections in specs/147-compositor-damage-redraw/readiness/compatibility-ledger.md

**Checkpoint**: User Story 5 can be validated with `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature147` and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-readiness --out specs/147-compositor-damage-redraw/readiness`.

---

## Phase 7: User Story 4 - Use Snapshot Reuse Only When It Pays Off (Priority: P3)

**Goal**: Maintainers can enable a bounded snapshot tier for expensive stable content only when probe evidence shows net benefit, with safe fallback for unsupported or losing cases.

**Independent Test**: Run expensive, simple, churning, over-budget, invalid-resource, and unsupported-host scenarios; verify parity, resource bounds, demotion, and performance thresholds.

### Public Surface for User Story 4

- [ ] T052 [US4] Define snapshot eligibility, resource state, budget, lifecycle diagnostics, and host support additions in src/SkiaViewer/PictureReplayCache.fsi and src/SkiaViewer/SkiaViewer.fsi

### Tests for User Story 4

- [x] T053 [P] [US4] Add snapshot eligibility, simple-scene overhead, churn demotion, no-benefit demotion, and threshold tests with RetainedRender FSI semantic coverage in tests/Controls.Tests/Feature147SnapshotBudgetTests.fs
- [ ] T054 [P] [US4] Add PictureReplayCache/SkiaViewer FSI transcript coverage plus snapshot resource allocation, byte budget, invalidation, refresh, eviction, disposal, unsupported-host, and parity tests in tests/SkiaViewer.Tests/Feature147SnapshotResourceTests.fs and tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [ ] T055 [US4] Add snapshot performance and readiness tests for expensive stable, simple, churning, over-budget, unsupported, Synthetic, and environment-limited cases in tests/Rendering.Harness.Tests/Feature147CompositorReadinessTests.fs
- [x] T056 [US4] Add tests/Controls.Tests/Feature147SnapshotBudgetTests.fs and tests/SkiaViewer.Tests/Feature147SnapshotResourceTests.fs to tests/Controls.Tests/Controls.Tests.fsproj and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj before Program.fs

### Implementation for User Story 4

- [x] T057 [US4] Implement snapshot eligibility inputs, expensive-stable classification, overhead measurement, and demotion decisions in src/Controls/RetainedRender.fs
- [ ] T058 [US4] Implement bounded snapshot resource allocation, byte estimates, content identity validation, refresh, eviction, disposal, and lifecycle diagnostics in src/SkiaViewer/PictureReplayCache.fs
- [ ] T059 [US4] Integrate snapshot composition and lower-tier/full-redraw fallback into src/SkiaViewer/SceneRenderer.fs and src/SkiaViewer/Host/OpenGl.fs
- [ ] T060 [US4] Implement snapshot performance probe signatures, lower-tier comparisons, and resource budget reporting in tests/Rendering.Harness/Perf.fsi and tests/Rendering.Harness/Perf.fs; implement tier verdict input records in tests/Rendering.Harness/Compositor.fsi and tests/Rendering.Harness/Compositor.fs without making Compositor depend on Perf
- [x] T061 [US4] Record snapshot resource budget, lifecycle, unsupported-host, and net-benefit evidence expectations in specs/147-compositor-damage-redraw/readiness/perf/snapshot.md

**Checkpoint**: User Story 4 can be validated with `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature147Snapshot`, `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature147Snapshot`, and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-perf --tier snapshot --out specs/147-compositor-damage-redraw/readiness/perf`.

---

## Phase 8: Polish and Cross-Cutting Concerns

**Purpose**: Complete Tier 1 readiness, docs, surface baselines, full quickstart validation, and package proof.

- [x] T062 [P] Update compositor behavior, diagnostics, limitations, and migration notes in src/Controls/README.md and src/SkiaViewer/README.md
- [x] T063 [P] Update compatibility impact, public metric changes, baseline references, release notes, and known limitations in specs/147-compositor-damage-redraw/readiness/compatibility-ledger.md
- [x] T064 Refresh public surface baselines with scripts/refresh-surface-baselines.fsx and verify intentional Feature147 deltas in tests/surface-baselines/FS.GG.UI.Controls.txt, tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt, tests/surface-baselines/FS.GG.UI.SkiaViewer.txt, and tests/surface-baselines/FS.GG.UI.Testing.txt
- [x] T065 Run present-path proof validation and record command results, host profile verdicts, and environment limitations in specs/147-compositor-damage-redraw/readiness/validation-summary.md using tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj and tests/Rendering.Harness/Rendering.Harness.fsproj
- [x] T066 Run damage-scissored redraw parity validation and record corpus coverage, parity verdicts, fallback reasons, and scissor reset evidence in specs/147-compositor-damage-redraw/readiness/validation-summary.md using tests/Controls.Tests/Controls.Tests.fsproj, tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, and tests/Rendering.Harness/Rendering.Harness.fsproj
- [x] T067 Run promotion and placement reuse validation and record repeated-work reduction, demotion, and parity results in specs/147-compositor-damage-redraw/readiness/validation-summary.md using tests/Controls.Tests/Controls.Tests.fsproj and tests/Elmish.Tests/Elmish.Tests.fsproj
- [x] T068 Run damage, promotion, and snapshot performance probes and record threshold results in specs/147-compositor-damage-redraw/readiness/validation-summary.md using tests/Rendering.Harness/Rendering.Harness.fsproj
- [x] T069 Run readiness package and compatibility ledger validation and record ready, limited, rejected, and skipped tier status in specs/147-compositor-damage-redraw/readiness/validation-summary.md using tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj and tests/Package.Tests/Package.Tests.fsproj
- [x] T070 Run full solution, package surface, and pack validation and record final results in specs/147-compositor-damage-redraw/readiness/validation-summary.md using FS.GG.Rendering.slnx, tests/Package.Tests/Package.Tests.fsproj, and ~/.local/share/nuget-local/

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **US1 Prove Partial Redraw Is Safe (Phase 3)**: Depends on Foundational and is the MVP.
- **US2 Repaint Only Damaged Regions (Phase 4)**: Depends on US1 proof gating.
- **US3 Reuse Stable and Moving Content (Phase 5)**: Depends on US2 damage/fallback metrics and can use the same full-redraw oracle.
- **US5 Review Readiness Evidence (Phase 6)**: Depends on US1 and US2 evidence formats; can treat later tiers as skipped or limited until US3 and US4 are complete.
- **US4 Snapshot Reuse (Phase 7)**: Depends on US3 promotion decisions and US5 readiness verdict plumbing.
- **Polish (Phase 8)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational and has no dependency on other user stories.
- **US2 (P1)**: Requires US1 present-path proof and readiness validation.
- **US3 (P2)**: Requires US2 damage records, fallback reasons, and oracle parity path.
- **US5 (P2)**: Requires US1 and US2 evidence contracts; extends naturally as US3 and US4 evidence appears.
- **US4 (P3)**: Requires US3 promotion boundaries and US5 tier verdict plumbing.

### Within Each User Story

- Write tests first and confirm they fail for missing behavior.
- Draft `.fsi` public signatures before semantic/FSI transcript tests and before `.fs` implementation bodies.
- Update `.fsproj` compile ordering whenever new F# files are added.
- Preserve full-redraw oracle parity before accepting performance evidence.
- Record readiness evidence before claiming a tier as ready.
- Keep `tests/Rendering.Harness` dependencies flowing from Compositor contracts to Evidence and Perf,
  with `Cli.fs` orchestrating modules that should not depend on each other directly.

---

## Parallel Opportunities

- T004 can run in parallel with T002 and T003 because it writes specs/147-compositor-damage-redraw/readiness/corpus.md.
- T008, T009, and T010 can run in parallel after T005 to T007 establish shared harness names.
- US1 tests T012 and T014 can run in parallel after T011; T013 and T015 are sequential because T015 wires the new test files.
- US2 tests T023 and T024 can run in parallel after T022; T025 extends an existing harness test file and should be sequenced with T014.
- US3 tests T034 and T035 can run in parallel after T033; implementation T037 to T040 should stay sequential because they modify the same retained-render and Elmish metrics surfaces.
- US5 tests T044 and T045 can run in parallel after T043; T047 to T049 should stay sequential because they share tests/Rendering.Harness/Compositor.fs and tests/Rendering.Harness/Cli.fs.
- US4 tests T053 and T054 can run in parallel after T052; T055 should run after T044 because it extends the readiness test file.
- Polish tasks T062 and T063 can run in parallel after public surfaces stabilize.

---

## Parallel Example: User Story 1

```bash
Task: "T012 Add CompositorProof FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
Task: "T014 Add harness proof artifact tests in tests/Rendering.Harness.Tests/Feature147CompositorEvidenceTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "T023 Add damage union tests in tests/Controls.Tests/Feature147DamageUnionTests.fs"
Task: "T024 Add scissor redraw parity tests in tests/SkiaViewer.Tests/Feature147ScissorRedrawTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "T034 Add promotion reuse tests in tests/Controls.Tests/Feature147PromotionReuseTests.fs"
Task: "T035 Add compositor metric tests in tests/Elmish.Tests/Feature147CompositorMetricsTests.fs"
```

## Parallel Example: User Story 5

```bash
Task: "T044 Add readiness evaluation tests in tests/Rendering.Harness.Tests/Feature147CompositorReadinessTests.fs"
Task: "T045 Add compatibility ledger tests in tests/Package.Tests/Feature147CompatibilityLedgerTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "T053 Add snapshot budget tests in tests/Controls.Tests/Feature147SnapshotBudgetTests.fs"
Task: "T054 Add snapshot resource tests in tests/SkiaViewer.Tests/Feature147SnapshotResourceTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundational shared corpus and artifact helpers.
3. Complete Phase 3 User Story 1 `.fsi`, tests, implementation, harness command, and present-proof evidence.
4. Stop and validate US1 independently before enabling scissored redraw.

### Incremental Delivery

1. Complete Setup and Foundational phases.
2. Deliver US1 present-path proof and validate capable, failed, stale, and environment-limited cases.
3. Deliver US2 damage-scissored redraw only behind passed proof and validate full-redraw oracle parity.
4. Deliver US3 promotion and placement reuse with demotion evidence.
5. Deliver US5 readiness packaging so reviewers can inspect accepted, limited, rejected, and skipped tiers.
6. Deliver US4 snapshot reuse once lower tiers and readiness plumbing are stable.
7. Complete Polish validation, surface baselines, docs, readiness artifacts, full solution tests, and pack output.

### Parallel Team Strategy

With multiple contributors:

1. Pair on Setup and Foundational tasks because they establish shared names and paths.
2. Assign US1 to the host/OpenGL owner and US2 to the retained-render owner after foundational names exist, with US2 waiting to merge until US1 proof gating is available.
3. Assign US3 Controls/Elmish work and US5 harness/readiness work in parallel after US2 evidence formats stabilize.
4. Assign US4 snapshot work after US3 promotion records and US5 verdict plumbing are available.

---

## Notes

- `[P]` tasks write different files and can be worked in parallel after their prerequisites.
- `[US#]` labels map each task to the corresponding user story in spec.md.
- Tests must be written first and observed failing before implementation.
- Public or package-visible surfaces must be drafted in `.fsi` before `.fs` implementation.
- Synthetic or environment-limited evidence must be disclosed in test names, diagnostics, readiness records, use-site comments, and PR descriptions; synthetic tests must carry the token `Synthetic`.
- Do not claim performance benefit until parity, proof, and threshold evidence are present for the active tier.
