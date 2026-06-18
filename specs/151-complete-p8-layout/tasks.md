# Tasks: Complete P8 Layout Acceptance

**Input**: Design documents from `/specs/151-complete-p8-layout/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required. The specification declares mandatory user scenarios, acceptance evidence,
full/incremental parity, regression validation, package validation, and final readiness artifacts.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified as
an independently testable increment. Public or observable surface changes follow the repository
rule: `.fsi` first, semantic/FSI tests next, implementation after that.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Feature151 readiness locations and reviewable evidence skeletons before
implementation starts.

- [X] T001 Create Feature151 readiness artifact skeletons in specs/151-complete-p8-layout/readiness/validation-summary.md, specs/151-complete-p8-layout/readiness/corpus-validation.md, specs/151-complete-p8-layout/readiness/scrollviewer-validation.md, specs/151-complete-p8-layout/readiness/reuse-validation.md, specs/151-complete-p8-layout/readiness/full-incremental-parity.md, specs/151-complete-p8-layout/readiness/regression-evidence.md, specs/151-complete-p8-layout/readiness/compatibility-ledger.md, specs/151-complete-p8-layout/readiness/package-validation.md, and specs/151-complete-p8-layout/readiness/limitations.md
- [X] T002 Populate the validation command checklist from specs/151-complete-p8-layout/quickstart.md into specs/151-complete-p8-layout/readiness/validation-summary.md and specs/151-complete-p8-layout/readiness/package-validation.md
- [X] T003 [P] Create the representative layout corpus ledger template in specs/151-complete-p8-layout/readiness/corpus-validation.md with required case ids, expected bounds, placements, diagnostics, verdict, and evidence path columns
- [X] T004 [P] Create the ScrollViewer corpus ledger template in specs/151-complete-p8-layout/readiness/scrollviewer-validation.md with the 11 required content cases, viewport, content extent, max offset, extent source, diagnostics, and verdict columns
- [X] T005 [P] Create the regression and compatibility classification templates in specs/151-complete-p8-layout/readiness/regression-evidence.md, specs/151-complete-p8-layout/readiness/compatibility-ledger.md, and specs/151-complete-p8-layout/readiness/limitations.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared Feature151 fixtures, compile-order wiring, and public-surface policy
used by every user story.

**Critical**: No user story implementation should start until this phase is complete.

- [X] T006 Create shared Feature151 layout corpus builders for constrained roots, measured leaves, intrinsic-capable content, invalid constraints, dynamic content, visibility changes, child insertion/removal/reorder, and diagnostic assertions in tests/Layout.Tests/Feature151CorpusFixtures.fs
- [X] T007 [P] Create shared Feature151 ScrollViewer corpus builders for empty, smaller-than-viewport, exact-fit, barely overflowing, substantially overflowing, nested scroll, clipped parent, layered parent, text/content natural size, dynamic content change, and invalid intrinsic fallback cases in tests/Controls.Tests/Feature151ScrollViewerFixtures.fs
- [X] T008 [P] Create shared Feature151 readiness fixture helpers for required files, status vocabulary, evidence links, compatibility deltas, package verdicts, and limitation records in tests/Testing.Tests/Feature151ReadinessFixtures.fs
- [X] T009 Add Feature151 fixture compile entries to tests/Layout.Tests/Layout.Tests.fsproj, tests/Controls.Tests/Controls.Tests.fsproj, and tests/Testing.Tests/Testing.Tests.fsproj before dependent Feature151 test files
- [X] T010 Review Feature150 layout, intrinsic, ScrollViewer, Controls.Elmish, and Testing surfaces for Feature151 contract gaps and record the no-change or required-delta decision in specs/151-complete-p8-layout/readiness/compatibility-ledger.md
- [X] T011 Draft any required Feature151 public contract deltas in src/Layout/Types.fsi, src/Layout/Layout.fsi, src/Controls/Control.fsi, src/Controls/Diagnostics.fsi, src/Controls.Elmish/ControlsElmish.fsi, and src/Testing/Testing.fsi, then add matching package-shaped FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs before any .fs implementation changes

**Checkpoint**: Shared fixtures compile, public-surface decisions, signature drafts, and FSI
transcript coverage are recorded, and user story tests can reference stable Feature151 helpers.

---

## Phase 3: User Story 1 - Prove the Representative Layout Corpus (Priority: P1) MVP

**Goal**: Exercise the full representative layout and ScrollViewer corpus so P8 acceptance is based
on breadth rather than focused smoke coverage.

**Independent Test**: Run the accepted representative corpus and verify every case records expected
bounds, placements, scroll extents, diagnostics, and readiness status.

### Tests for User Story 1

Write these tests first and confirm they fail for missing corpus behavior or missing ledger data.

- [X] T012 [P] [US1] Add representative layout corpus tests for finite roots, zero/very small/very large constraints, measured leaves, intrinsic-capable content, empty/single-child containers, deep nesting, dynamic content, and child order changes in tests/Layout.Tests/Feature151RepresentativeCorpusTests.fs
- [X] T013 [P] [US1] Add invalid, contradictory, unsupported intrinsic, fallback, duplicate-measurement, and no-misleading-result diagnostic tests in tests/Layout.Tests/Feature151DiagnosticsTests.fs
- [X] T014 [P] [US1] Add ScrollViewer corpus tests for empty, smaller-than-viewport, exact-fit, barely overflowing, substantially overflowing, nested scroll, clipped parent, layered parent, text/content natural size, dynamic content change, and invalid intrinsic fallback cases in tests/Controls.Tests/Feature151ScrollViewerCorpusTests.fs
- [X] T015 [P] [US1] Add Layout protocol tests proving ScrollViewer accepted extents come from Layout.contentExtent and intrinsic evidence rather than rendered descendant bounds in tests/Layout.Tests/Feature151ScrollLayoutProtocolTests.fs
- [X] T016 [US1] Add Feature151 US1 test compile entries to tests/Layout.Tests/Layout.Tests.fsproj and tests/Controls.Tests/Controls.Tests.fsproj before Program.fs

### Implementation for User Story 1

- [X] T017 [US1] Complete expected bounds, child placements, scroll extents, diagnostics, and verdict data for every US1 corpus case in tests/Layout.Tests/Feature151CorpusFixtures.fs and tests/Controls.Tests/Feature151ScrollViewerFixtures.fs
- [X] T018 [US1] Implement missing deterministic layout, intrinsic extent, fallback, and diagnostic behavior exposed by the US1 corpus in src/Layout/Layout.fs and src/Layout/Types.fs
- [X] T019 [US1] Implement missing ScrollViewer viewport, content extent, max offset, extent source, and fallback diagnostic behavior exposed by the US1 corpus in src/Controls/Control.fs and src/Controls/Diagnostics.fs
- [X] T020 [US1] Record accepted, failed, skipped, blocked, and environment-limited US1 corpus verdicts in specs/151-complete-p8-layout/readiness/corpus-validation.md and specs/151-complete-p8-layout/readiness/scrollviewer-validation.md
- [X] T021 [US1] Document representative corpus scope, ScrollViewer extent expectations, diagnostics, and unsupported/fallback behavior in src/Layout/README.md and src/Controls/README.md

**Checkpoint**: User Story 1 can be validated with `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151RepresentativeCorpus`, `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151Diagnostics`, and `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature151ScrollViewerCorpus`.

---

## Phase 4: User Story 2 - Accept Measured and Intrinsic Cache Reuse (Priority: P1)

**Goal**: Accept measured and intrinsic layout reuse only when dependency keys match, while
preserving full, cold incremental, warm incremental, changed-input incremental, and disabled-cache
equivalence.

**Independent Test**: Evaluate cold, warm, and changed-input layout runs over the corpus and verify
accepted reuse, required misses, stale rejection, and full/incremental equivalence.

### Tests for User Story 2

Write these tests first and confirm they fail for missing cache evidence or unsafe reuse behavior.

- [X] T022 [P] [US2] Add measured reuse tests for participant id, entry kind, normalized constraints, content identity, measurement behavior, LayoutIntent, visibility, child order, child dependency keys, revision, accepted hits, misses, and stale rejection in tests/Layout.Tests/Feature151MeasuredReuseTests.fs
- [X] T023 [P] [US2] Add intrinsic reuse tests for query identity, participant id, intrinsic axis, cross-axis constraint, layout input key, intrinsic dependency identity, revision, unsupported query, contradictory result, accepted hits, misses, and stale rejection in tests/Layout.Tests/Feature151IntrinsicReuseTests.fs
- [X] T024 [P] [US2] Add full/cold/warm/changed incremental parity tests for bounds, placements, scroll extents, diagnostics, result identities, accepted reuse, rejected reuse, and changed geometry in tests/Layout.Tests/Feature151FullIncrementalParityTests.fs
- [X] T025 [US2] Add duplicate measurement, insufficient dependency evidence, stale cache, rejected intrinsic, fallback, and disabled-cache diagnostics to tests/Layout.Tests/Feature151DiagnosticsTests.fs
- [X] T026 [P] [US2] Add Controls disabled-cache parity tests covering ScrollViewer extent, default layout compatibility, diagnostics, and work metrics in tests/Controls.Tests/Feature151DisabledCacheParityTests.fs
- [X] T027 [US2] Add Feature151 US2 test compile entries to tests/Layout.Tests/Layout.Tests.fsproj and tests/Controls.Tests/Controls.Tests.fsproj before Program.fs

### Implementation for User Story 2

- [X] T028 [US2] Implement deterministic measured and intrinsic dependency-key construction over constraints, viewport, content identity, measurement behavior, layout-affecting inputs, visibility, child order, intrinsic dependencies, and revision in src/Layout/Types.fs and src/Layout/Layout.fs
- [X] T029 [US2] Implement measured and intrinsic cache hit, miss, stale rejection, unsupported query, contradictory result, duplicate measurement, and reviewer-visible diagnostic behavior in src/Layout/Layout.fs
- [X] T030 [US2] Implement full, cold incremental, warm incremental, changed-input incremental, and disabled-cache parity behavior for accepted corpus cases in src/Layout/Layout.fs and src/Controls/Control.fs
- [X] T031 [US2] Record measured reuse, intrinsic reuse, stale rejection, disabled-cache, and duplicate-measurement verdicts in specs/151-complete-p8-layout/readiness/reuse-validation.md
- [X] T032 [US2] Record full, cold incremental, warm incremental, changed-input incremental, and disabled-cache parity verdicts in specs/151-complete-p8-layout/readiness/full-incremental-parity.md

**Checkpoint**: User Story 2 can be validated with `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151MeasuredReuse`, `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151IntrinsicReuse`, `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151FullIncrementalParity`, and `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature151DisabledCacheParity`.

---

## Phase 5: User Story 3 - Run Broad Regression Evidence (Priority: P2)

**Goal**: Prove completing P8 does not regress retained rendering, default layout, overlay state,
render-anywhere, text shaping, compositor readiness, disabled-cache parity, public surface
compatibility, package compatibility, or full solution validation.

**Independent Test**: Run the agreed regression set and confirm every accepted result is linked from
the P8 readiness summary, with failures or environment limits classified as blockers or limitations.

### Tests for User Story 3

Write these tests first and confirm they fail for missing regression classification or unexpected
behavioral deltas.

- [X] T033 [P] [US3] Add default layout compatibility and broad Controls regression tests for retained/default layout parity, surrounding layout stability, overlay state, and disabled-cache behavior in tests/Controls.Tests/Feature151LayoutCompatibilityTests.fs
- [X] T034 [P] [US3] Add Controls.Elmish layout regression metric tests for deterministic layout work, intrinsic work, cache hits, cache misses, stale rejection, invalidation counts, and pure update boundaries in tests/Elmish.Tests/Feature151LayoutRegressionMetricsTests.fs
- [X] T035 [P] [US3] Add render-anywhere regression evidence tests for package/protocol compatibility and P8 classification fields in tests/Rendering.Harness.Tests/Feature151RenderAnywhereRegressionTests.fs
- [X] T036 [P] [US3] Add text-shaping regression evidence tests that classify accepted, skipped, synthetic-only, or environment-limited output without claiming new text behavior in tests/Rendering.Harness.Tests/Feature151TextShapingRegressionTests.fs
- [X] T037 [P] [US3] Add compositor-readiness regression evidence tests that classify P7 environment-limited output without claiming new partial-redraw acceptance in tests/Rendering.Harness.Tests/Feature151CompositorReadinessRegressionTests.fs
- [X] T038 [P] [US3] Add retained rendering regression tests for viewer retained rendering behavior, default layout compatibility, and environment-limited viewer diagnostics in tests/SkiaViewer.Tests/Feature151RetainedRenderingRegressionTests.fs
- [X] T039 [US3] Add Feature151 US3 test compile entries to tests/Controls.Tests/Controls.Tests.fsproj, tests/Elmish.Tests/Elmish.Tests.fsproj, tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj, and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj before Program.fs
- [X] T040 [P] [US3] Add package compatibility ledger tests for public API deltas, diagnostic deltas, surface baseline references, migration guidance, limitations, and prior evidence links in tests/Package.Tests/Feature151CompatibilityLedgerTests.fs
- [X] T041 [P] [US3] Add package validation tests for full solution result classification, local pack output, template pack output, local feed path, and package readiness verdicts in tests/Package.Tests/Feature151PackageValidationTests.fs
- [X] T042 [US3] Add Feature151 package test compile entries to tests/Package.Tests/Package.Tests.fsproj before Tests.fs

### Implementation for User Story 3

- [X] T043 [US3] Fix or explicitly classify retained rendering, default layout, overlay, disabled-cache, and Controls regression deltas in src/Controls/Control.fs and tests/Controls.Tests/Feature151LayoutCompatibilityTests.fs
- [X] T044 [US3] Fix or explicitly classify layout metric and pure-update regressions in src/Controls.Elmish/ControlsElmish.fs and tests/Elmish.Tests/Feature151LayoutRegressionMetricsTests.fs
- [X] T045 [US3] Fix or explicitly classify render-anywhere, text-shaping, compositor-readiness, and retained-viewer evidence deltas in tests/Rendering.Harness.Tests/Feature151RenderAnywhereRegressionTests.fs, tests/Rendering.Harness.Tests/Feature151TextShapingRegressionTests.fs, tests/Rendering.Harness.Tests/Feature151CompositorReadinessRegressionTests.fs, and tests/SkiaViewer.Tests/Feature151RetainedRenderingRegressionTests.fs
- [X] T046 [US3] Record broad regression command output, accepted verdicts, pre-existing unrelated failures, environment-limited checks, synthetic-only checks, blockers, and reviewer-visible rationale in specs/151-complete-p8-layout/readiness/regression-evidence.md

**Checkpoint**: User Story 3 can be validated with the focused regression commands from specs/151-complete-p8-layout/quickstart.md and by reviewing specs/151-complete-p8-layout/readiness/regression-evidence.md.

---

## Phase 6: User Story 4 - Publish Final P8 Readiness (Priority: P3)

**Goal**: Package consumers and maintainers can review one readiness package that states final P8
status, supporting evidence, compatibility impact, package readiness, limitations, and follow-up
scope.

**Independent Test**: Review the readiness package in under 10 minutes and confirm it links corpus,
cache/reuse, parity, ScrollViewer, compatibility, regression, full solution, package, and limitation
evidence.

### Tests for User Story 4

Write these tests first and confirm they fail for missing readiness files, invalid statuses, missing
links, or compatibility-blocking deltas.

- [X] T047 [P] [US4] Add readiness helper tests for required Feature151 files, status vocabulary, evidence links, missing evidence, compatibility-blocked evidence, limitations, and final P8 status aggregation in tests/Testing.Tests/Feature151ReadinessHelperTests.fs
- [X] T048 [P] [US4] Verify the foundational public FSI transcript coverage and extend it only for any US4 readiness-helper Testing surface deltas in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T049 [US4] Add Feature151 US4 test compile entries to tests/Testing.Tests/Testing.Tests.fsproj and tests/Package.Tests/Package.Tests.fsproj before Program.fs

### Implementation for User Story 4

- [X] T050 [US4] Implement or extend readiness file discovery, status aggregation, evidence link validation, compatibility delta validation, limitation validation, and package verdict diagnostics in src/Testing/Testing.fs
- [X] T051 [US4] Populate consumer-visible layout behavior changes, diagnostic changes, public API deltas, migration guidance, surface baseline references, and compatibility verdicts in specs/151-complete-p8-layout/readiness/compatibility-ledger.md
- [X] T052 [US4] Populate full solution, package surface, package test, local source pack, template pack, local feed, failed, skipped, and environment-limited verdicts in specs/151-complete-p8-layout/readiness/package-validation.md
- [X] T053 [US4] Populate environment limits, synthetic-only disclosures, pre-existing unrelated failures, non-accepted checks, and follow-up scope in specs/151-complete-p8-layout/readiness/limitations.md
- [X] T054 [US4] Populate the final P8 status, blockers, limitations, compatibility impact, package readiness, and links to every required evidence artifact in specs/151-complete-p8-layout/readiness/validation-summary.md
- [X] T055 [US4] Update final P8 consumer documentation and readiness helper guidance in src/Layout/README.md, src/Controls/README.md, src/Testing/README.md, and docs/validation/surface-baseline-review.md
- [X] T056 [US4] Update the radical rendering report P8/R3b status and follow-up scope after Feature151 readiness is classified in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md

**Checkpoint**: User Story 4 can be validated with `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature151Readiness`, `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature151`, and a review of specs/151-complete-p8-layout/readiness/validation-summary.md.

---

## Phase 7: Polish and Cross-Cutting Concerns

**Purpose**: Complete Tier 1 baselines, dependency-boundary checks, quickstart validation, full
solution validation, package proof, and final consistency review.

- [X] T057 [P] Refresh public surface baselines with scripts/refresh-surface-baselines.fsx and verify intentional Feature151 deltas or no-op output in tests/surface-baselines/FS.GG.UI.Layout.txt, tests/surface-baselines/FS.GG.UI.Controls.txt, tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt, and tests/surface-baselines/FS.GG.UI.Testing.txt
- [X] T058 Run restore, build, representative corpus, ScrollViewer corpus, cache reuse, parity, regression, surface, and Testing helper quickstart commands from specs/151-complete-p8-layout/quickstart.md and record outcomes in specs/151-complete-p8-layout/readiness/validation-summary.md
- [X] T059 Run full solution validation with FS.GG.Rendering.slnx and record accepted, failed, skipped, unrelated, or environment-limited results in specs/151-complete-p8-layout/readiness/package-validation.md
- [X] T060 Run local source package and template package validation with FS.GG.Rendering.slnx, .template.package/FS.GG.UI.Template.fsproj, and ~/.local/share/nuget-local/ and record pack evidence in specs/151-complete-p8-layout/readiness/package-validation.md
- [X] T061 Verify Layout dependency boundaries and package graph drift for src/Layout/Layout.fsproj, Directory.Packages.props, and docs/reports/dependencies.md before final P8 acceptance
- [X] T062 Perform final readiness consistency review across specs/151-complete-p8-layout/readiness/validation-summary.md, specs/151-complete-p8-layout/readiness/corpus-validation.md, specs/151-complete-p8-layout/readiness/scrollviewer-validation.md, specs/151-complete-p8-layout/readiness/reuse-validation.md, specs/151-complete-p8-layout/readiness/full-incremental-parity.md, specs/151-complete-p8-layout/readiness/regression-evidence.md, specs/151-complete-p8-layout/readiness/compatibility-ledger.md, specs/151-complete-p8-layout/readiness/package-validation.md, and specs/151-complete-p8-layout/readiness/limitations.md

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **US1 Representative Corpus (Phase 3)**: Depends on Foundational and is the MVP.
- **US2 Cache Reuse and Parity (Phase 4)**: Depends on US1 corpus fixtures and layout/ScrollViewer expected outputs.
- **US3 Broad Regression Evidence (Phase 5)**: Depends on US1 and US2 behavior before accepted regression claims are meaningful.
- **US4 Final Readiness (Phase 6)**: Depends on US1, US2, and US3 evidence before accepted P8 readiness can be claimed.
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational and has no dependency on other user stories.
- **US2 (P1)**: Requires US1 corpus fixtures and expected geometry to compare full/incremental parity.
- **US3 (P2)**: Requires US1 and US2 behavior before broad regression results can be classified against final P8 behavior.
- **US4 (P3)**: Requires evidence from US1, US2, and US3 before publishing final accepted readiness.

### Within Each User Story

- Write tests first and confirm they fail for missing behavior or missing evidence.
- Draft `.fsi` public signatures before semantic/FSI transcript tests and before `.fs` implementation bodies when a public delta is required.
- Update `.fsproj` compile ordering whenever new F# files are added.
- Keep `src/Layout` independent of Controls, SkiaViewer, KeyboardInput, charts, viewer, and harness projects.
- Record readiness evidence before marking any readiness category as accepted.
- Classify failed, skipped, synthetic-only, environment-limited, and unrelated results before final P8 acceptance.

---

## Parallel Opportunities

- T003, T004, and T005 can run in parallel because they write different readiness files.
- T006, T007, and T008 can run in parallel because they create fixtures in different test projects.
- US1 tests T012, T013, T014, and T015 can run in parallel after T006 and T007; T016 is compile-order wiring.
- US2 tests T022, T023, T024, and T026 can run in parallel after T006 and T007; T025 shares the US1 diagnostics file and should follow T013.
- US3 tests T033, T034, T035, T036, T037, T038, T040, and T041 can run in parallel after US1 and US2 behavior is stable; T039 and T042 are compile-order wiring.
- US4 tests T047 and T048 can run in parallel after T011 and the readiness artifact skeletons exist.
- T057 can run in parallel with documentation review once public contract deltas are stable.

---

## Parallel Example: User Story 1

```bash
Task: "T012 Add representative layout corpus tests in tests/Layout.Tests/Feature151RepresentativeCorpusTests.fs"
Task: "T013 Add diagnostic tests in tests/Layout.Tests/Feature151DiagnosticsTests.fs"
Task: "T014 Add ScrollViewer corpus tests in tests/Controls.Tests/Feature151ScrollViewerCorpusTests.fs"
Task: "T015 Add Scroll layout protocol tests in tests/Layout.Tests/Feature151ScrollLayoutProtocolTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "T022 Add measured reuse tests in tests/Layout.Tests/Feature151MeasuredReuseTests.fs"
Task: "T023 Add intrinsic reuse tests in tests/Layout.Tests/Feature151IntrinsicReuseTests.fs"
Task: "T024 Add full/incremental parity tests in tests/Layout.Tests/Feature151FullIncrementalParityTests.fs"
Task: "T026 Add disabled-cache parity tests in tests/Controls.Tests/Feature151DisabledCacheParityTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "T033 Add Controls regression tests in tests/Controls.Tests/Feature151LayoutCompatibilityTests.fs"
Task: "T034 Add Elmish regression metrics tests in tests/Elmish.Tests/Feature151LayoutRegressionMetricsTests.fs"
Task: "T035 Add render-anywhere regression tests in tests/Rendering.Harness.Tests/Feature151RenderAnywhereRegressionTests.fs"
Task: "T036 Add text-shaping regression tests in tests/Rendering.Harness.Tests/Feature151TextShapingRegressionTests.fs"
Task: "T037 Add compositor-readiness regression tests in tests/Rendering.Harness.Tests/Feature151CompositorReadinessRegressionTests.fs"
Task: "T038 Add retained rendering regression tests in tests/SkiaViewer.Tests/Feature151RetainedRenderingRegressionTests.fs"
Task: "T040 Add compatibility ledger tests in tests/Package.Tests/Feature151CompatibilityLedgerTests.fs"
Task: "T041 Add package validation tests in tests/Package.Tests/Feature151PackageValidationTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "T047 Add readiness helper tests in tests/Testing.Tests/Feature151ReadinessHelperTests.fs"
Task: "T048 Verify foundational FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Stop and validate the representative layout and ScrollViewer corpus independently.
5. Review readiness/corpus-validation.md and readiness/scrollviewer-validation.md before starting cache acceptance.

### Incremental Delivery

1. Deliver US1 representative corpus and ScrollViewer corpus.
2. Deliver US2 measured/intrinsic cache reuse and full/incremental parity.
3. Deliver US3 broad regression evidence and classifications.
4. Deliver US4 final readiness package and compatibility documentation.
5. Finish Phase 7 validation, package proof, surface baselines, and final readiness consistency review.

### Parallel Team Strategy

After Phase 2, split by package boundary where possible: one contributor on Layout corpus and cache
evidence, one on Controls ScrollViewer and disabled-cache parity, one on broad regression evidence,
and one on Testing/Package/readiness artifacts. Merge only after each story checkpoint passes its
independent validation.

---

## Notes

- `[P]` tasks write different files or can proceed without depending on incomplete task results.
- `[US1]` through `[US4]` labels map directly to the user stories in specs/151-complete-p8-layout/spec.md.
- Tests are required by this feature specification and should fail before the implementation task that satisfies them.
- Public contract changes are allowed only if the corpus exposes a real gap; any delta requires `.fsi`, FSI transcript coverage, semantic/package tests, surface baseline refresh, compatibility notes, and readiness evidence.
- Environment-limited, skipped, failed, synthetic-only, or missing evidence cannot mark P8 accepted unless it is explicitly classified as bounded non-required follow-up.
- Synthetic tests or fixtures must use `Synthetic` in the test name, include a `// SYNTHETIC:` use-site disclosure with reason, and be listed in the PR description per the constitution.
