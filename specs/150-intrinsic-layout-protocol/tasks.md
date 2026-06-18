# Tasks: Intrinsic Layout Protocol

**Input**: Design documents from `/specs/150-intrinsic-layout-protocol/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required. The specification declares mandatory user scenarios, failing-first semantic
tests, full/incremental parity, ScrollViewer extent validation, cache invalidation evidence, package
surface validation, and readiness artifacts.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified as
an independently testable increment. Public or observable surfaces follow the repository rule:
`.fsi` first, semantic/FSI tests next, implementation after that.

**Current task status (2026-06-18 08:08 CEST)**: 37/58 tasks are complete for the bounded first
intrinsic-layout slice. Unchecked tasks may have scaffolding or focused tests in place, but remain
open until the full representative corpus, evaluator-internal cache reuse, broad regression evidence,
and final pack/readiness acceptance are complete.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Feature150 readiness locations and reviewable evidence skeletons before
implementation starts.

- [X] T001 Create Feature150 readiness files in specs/150-intrinsic-layout-protocol/readiness/validation-summary.md, specs/150-intrinsic-layout-protocol/readiness/compatibility-ledger.md, specs/150-intrinsic-layout-protocol/readiness/scrollviewer-validation.md, specs/150-intrinsic-layout-protocol/readiness/intrinsic-cache-validation.md, and specs/150-intrinsic-layout-protocol/readiness/full-incremental-parity.md
- [X] T002 Populate the initial validation command checklist from specs/150-intrinsic-layout-protocol/quickstart.md into specs/150-intrinsic-layout-protocol/readiness/validation-summary.md
- [X] T003 Create a Feature150 local run log section for accepted, failed, skipped, and environment-limited results in specs/150-intrinsic-layout-protocol/readiness/validation-summary.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared Feature150 fixtures and compile-order wiring used by all user
stories.

**Critical**: No user story implementation should start until this phase is complete.

- [ ] T004 Add shared Feature150 layout corpus builders matching the representative layout corpus defined in specs/150-intrinsic-layout-protocol/spec.md for constrained roots, measured leaves, intrinsic-capable nodes, invalid constraints, dynamic content, and diagnostic assertions in tests/Layout.Tests/Feature150Fixtures.fs
- [ ] T005 [P] Add shared Feature150 Controls scroll fixtures for fixed viewport, smaller/exact/overflowing content, dynamic content, nested scroll, clipped parent, layered parent, and bounds assertions in tests/Controls.Tests/Feature150ScrollFixtures.fs
- [X] T006 Add tests/Layout.Tests/Feature150Fixtures.fs and tests/Controls.Tests/Feature150ScrollFixtures.fs compile entries to tests/Layout.Tests/Layout.Tests.fsproj and tests/Controls.Tests/Controls.Tests.fsproj before dependent Feature150 test files

**Checkpoint**: Shared fixtures compile and user story tests can reference stable Feature150
helpers.

---

## Phase 3: User Story 1 - Measure Layout Through a Clear Contract (Priority: P1) MVP

**Goal**: Layout participants receive explicit constraints and return deterministic measured sizes,
child placements, cache dependency evidence, and diagnostics.

**Independent Test**: Run the representative layout corpus defined in `spec.md` through full layout and verify bounded,
deterministic sizes and placements for repeated equivalent inputs, including invalid constraints.

### Public Surface for User Story 1

- [X] T007 [US1] Draft Feature150 constraint, normalized identity, measurement request, measured result, child placement, intrinsic dependency, and diagnostic code contracts in src/Layout/Types.fsi and src/Layout/Layout.fsi

### Tests for User Story 1

- [X] T008 [P] [US1] Add FSI transcript coverage for Feature150 Layout constraints, measured results, child placements, intrinsic dependencies, and diagnostics in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [ ] T009 [P] [US1] Add layout protocol tests for finite, unbounded, zero-sized, very large, and contradictory constraints in tests/Layout.Tests/Feature150IntrinsicProtocolTests.fs
- [ ] T010 [P] [US1] Add deterministic measurement, child placement identity, repeated evaluation, and single-measure-per-pass tests in tests/Layout.Tests/Feature150MeasureDeterminismTests.fs
- [ ] T011 [P] [US1] Add invalid constraint, degradation, unsupported intrinsic, fallback bounds, and no-misleading-result diagnostic tests in tests/Layout.Tests/Feature150LayoutDiagnosticsTests.fs
- [X] T012 [US1] Add tests/Layout.Tests/Feature150IntrinsicProtocolTests.fs, tests/Layout.Tests/Feature150MeasureDeterminismTests.fs, and tests/Layout.Tests/Feature150LayoutDiagnosticsTests.fs to tests/Layout.Tests/Layout.Tests.fsproj before Program.fs

### Implementation for User Story 1

- [X] T013 [US1] Implement constraint normalization, accepted measured size validation, child placement records, intrinsic dependency records, and new diagnostic codes in src/Layout/Types.fs and src/Layout/Layout.fs
- [X] T014 [US1] Wire the Yoga-backed evaluator to produce Feature150 measured results and placement evidence while preserving Layout.evaluate, Layout.evaluateIncremental, Layout.renderComputed, and hit-test source compatibility in src/Layout/Layout.fs
- [ ] T015 [US1] Implement single-measure-per-pass accounting and deterministic duplicate-measure diagnostics in src/Layout/Layout.fs
- [X] T016 [US1] Document the constraints-down and sizes-up layout contract, diagnostics, and compatibility expectations in src/Layout/README.md

**Checkpoint**: User Story 1 can be validated with `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150IntrinsicProtocol` and public FSI transcript coverage in `tests/Package.Tests/FsiTranscriptCoverageTests.fs`.

---

## Phase 4: User Story 2 - Size Scrollable Content From Intrinsics (Priority: P1)

**Goal**: ScrollViewer derives content extent and scroll ranges from the layout/intrinsic contract
instead of inspecting rendered descendant bounds.

**Independent Test**: Place empty, smaller, exact-fit, overflowing, nested, clipped, layered, text,
dynamic, and invalid-intrinsic content in a fixed viewport and verify accepted scroll extents.

### Public Surface for User Story 2

- [X] T017 [US2] Extend ScrollViewport and ScrollViewer diagnostic contracts for content width, content height, max horizontal offset, max vertical offset, extent source, and fallback diagnostics in src/Controls/Control.fsi and src/Controls/Diagnostics.fsi
- [X] T018 [US2] Draft Layout intrinsic query functions and result contracts needed by ScrollViewer in src/Layout/Types.fsi and src/Layout/Layout.fsi

### Tests for User Story 2

- [X] T019 [P] [US2] Add FSI transcript coverage for ScrollViewport content extent, max offsets, extent source, and diagnostic fallback in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [ ] T020 [P] [US2] Add ScrollViewer extent tests for empty, smaller-than-viewport, exact-fit, barely overflowing, substantially overflowing, nested scroll, clipped parent, layered parent, text/content natural size, dynamic content change, and invalid intrinsic fallback cases in tests/Controls.Tests/Feature150ScrollViewerExtentTests.fs
- [ ] T021 [P] [US2] Add ScrollViewer diagnostic tests for unavailable intrinsic capability, rejected intrinsic result, measured fallback extent, contradictory extent, and insufficient dependency evidence in tests/Controls.Tests/Feature150LayoutDiagnosticsTests.fs
- [ ] T022 [P] [US2] Add default layout compatibility tests proving fixed viewport bounds and unrelated surrounding layout remain equivalent after intrinsic content changes in tests/Controls.Tests/Feature150LayoutCompatibilityTests.fs
- [X] T023 [US2] Add tests/Controls.Tests/Feature150ScrollViewerExtentTests.fs, tests/Controls.Tests/Feature150LayoutDiagnosticsTests.fs, and tests/Controls.Tests/Feature150LayoutCompatibilityTests.fs to tests/Controls.Tests/Controls.Tests.fsproj before Program.fs

### Implementation for User Story 2

- [X] T024 [US2] Implement intrinsic query evaluation for natural width and natural height under explicit cross-axis constraints in src/Layout/Types.fs and src/Layout/Layout.fs
- [ ] T025 [US2] Thread intrinsic capability, content identity, layout-affecting dependency keys, and extent metadata from Controls lowering into the LayoutNode tree in src/Controls/Control.fs
- [X] T026 [US2] Replace the descendant-bounds content-height walk in Control.scrollViewport with layout/intrinsic extent calculation in src/Controls/Control.fs
- [X] T027 [US2] Implement ScrollViewer extent source, max offset clamping, fallback diagnostics, and public ScrollViewport shape in src/Controls/Control.fs and src/Controls/Diagnostics.fs
- [ ] T028 [US2] Update scroll-viewer widget lowering and docs for intrinsic content extent in src/Controls/Widgets/Containers.fsi, src/Controls/Widgets/Containers.fs, and src/Controls/README.md
- [X] T029 [US2] Record ScrollViewer validation cases, commands, verdicts, limitations, and accepted extent evidence in specs/150-intrinsic-layout-protocol/readiness/scrollviewer-validation.md

**Checkpoint**: User Story 2 can be validated with `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature150ScrollViewerExtent` and `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature150LayoutDiagnostics`.

---

## Phase 5: User Story 3 - Preserve Incremental and Full Layout Parity (Priority: P2)

**Goal**: Measured and intrinsic cache reuse preserves full/incremental equivalence for bounds,
placements, scroll extents, and diagnostics.

**Independent Test**: Run cold full layout, warm incremental layout, and changed-input incremental
layout over the representative layout corpus defined in `spec.md` and compare all reported results and diagnostics.

### Public Surface for User Story 3

- [X] T030 [US3] Draft measured cache entry, intrinsic cache entry, layout input key, child dependency key, result identity, cache revision, reuse verdict, and stale-cache diagnostic contracts in src/Layout/Types.fsi and src/Layout/Layout.fsi
- [X] T031 [US3] Draft deterministic layout and intrinsic metric fields or diagnostics exposed to Controls.Elmish consumers in src/Controls.Elmish/ControlsElmish.fsi

### Tests for User Story 3

- [ ] T032 [P] [US3] Add intrinsic cache tests for query identity determinism, accepted cache hits, partial-key misses, unsupported queries, and stale measured or intrinsic entry rejection in tests/Layout.Tests/Feature150IntrinsicCacheTests.fs
- [ ] T033 [P] [US3] Add invalidation tests covering at least constraints or viewport changes, content/measure callback changes, LayoutIntent changes, visibility changes, child insertion/removal/reorder, intrinsic dependency changes, and cache revision changes in tests/Layout.Tests/Feature150IntrinsicInvalidationTests.fs
- [ ] T034 [P] [US3] Add full/incremental parity tests over the representative layout corpus for cold full, warm incremental, changed subtree, intrinsic-driven container size, ScrollViewer extent, diagnostics, and unchanged sibling preservation in tests/Layout.Tests/Feature150FullIncrementalParityTests.fs
- [ ] T035 [P] [US3] Add Controls.Elmish metrics tests for deterministic layout work, intrinsic query work, cache hit/miss counts, invalidation counts, and pure update boundaries in tests/Elmish.Tests/Feature150LayoutMetricsTests.fs
- [X] T036 [US3] Add tests/Layout.Tests/Feature150IntrinsicCacheTests.fs, tests/Layout.Tests/Feature150IntrinsicInvalidationTests.fs, tests/Layout.Tests/Feature150FullIncrementalParityTests.fs, and tests/Elmish.Tests/Feature150LayoutMetricsTests.fs to tests/Layout.Tests/Layout.Tests.fsproj and tests/Elmish.Tests/Elmish.Tests.fsproj before Program.fs

### Implementation for User Story 3

- [ ] T037 [US3] Implement deterministic measured and intrinsic cache key construction over participant id, constraints or query identity, content identity, LayoutIntent, visibility, child order, child dependency keys, and revision in src/Layout/Types.fs and src/Layout/Layout.fs
- [ ] T038 [US3] Implement measured and intrinsic cache reuse, miss, stale rejection, dependency diagnostics, and safe cache-hit evidence in src/Layout/Layout.fs
- [ ] T039 [US3] Extend incremental invalidation to recompute when constraints, viewport, content, measure callback, LayoutIntent, visibility, child order, intrinsic dependency, or cache revision inputs change in src/Layout/Layout.fs
- [X] T040 [US3] Implement Controls.Elmish layout/intrinsic metric projection without executing I/O inside update logic in src/Controls.Elmish/ControlsElmish.fs
- [X] T041 [US3] Record cache/invalidation command output, at least five accepted invalidation categories, stale rejection evidence, and limitations in specs/150-intrinsic-layout-protocol/readiness/intrinsic-cache-validation.md
- [X] T042 [US3] Record the representative full/incremental parity corpus, bounds, placements, scroll extents, diagnostics, commands, verdicts, and limitations in specs/150-intrinsic-layout-protocol/readiness/full-incremental-parity.md

**Checkpoint**: User Story 3 can be validated with `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150IntrinsicCache`, `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150FullIncrementalParity`, and `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature150LayoutMetrics`.

---

## Phase 6: User Story 4 - Publish Layout Readiness and Compatibility Evidence (Priority: P3)

**Goal**: Package consumers and reviewers can inspect public contract impact, compatibility
changes, diagnostics, limitations, and readiness evidence from one bounded package.

**Independent Test**: Review the readiness package and package contract validation to confirm public
behavior, intentional compatibility changes, and deferred layout work are explicit.

### Public Surface for User Story 4

- [X] T043 [US4] Define layout readiness report, readiness status, evidence link, compatibility delta, limitation, and validation helper contracts in src/Testing/Testing.fsi

### Tests for User Story 4

- [X] T044 [P] [US4] Add package compatibility ledger tests for public API deltas, diagnostic deltas, surface baseline references, release notes, migration guidance, limitations, and prior rendering evidence regressions in tests/Package.Tests/Feature150CompatibilityLedgerTests.fs
- [X] T045 [P] [US4] Add consumer readiness helper tests for accepted, incomplete, failed, skipped, environment-limited, synthetic-only, compatibility-blocked, and missing-evidence states in tests/Testing.Tests/Feature150ReadinessHelperTests.fs
- [X] T046 [P] [US4] Extend public FSI transcript coverage for final Layout, Controls, Controls.Elmish, and Testing Feature150 surfaces in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T047 [US4] Add tests/Package.Tests/Feature150CompatibilityLedgerTests.fs and tests/Testing.Tests/Feature150ReadinessHelperTests.fs to tests/Package.Tests/Package.Tests.fsproj and tests/Testing.Tests/Testing.Tests.fsproj before Program.fs

### Implementation for User Story 4

- [ ] T048 [US4] Implement layout readiness report parsing, evidence discovery, status aggregation, compatibility delta validation, limitation validation, and diagnostics in src/Testing/Testing.fs
- [X] T049 [US4] Populate public surface changes, behavior changes, diagnostic changes, compatibility verdict, release notes draft, migration guidance, and limitations in specs/150-intrinsic-layout-protocol/readiness/compatibility-ledger.md
- [X] T050 [US4] Populate final readiness status, linked evidence, accepted/failed/incomplete classifications, limitations, and under-10-minute review path in specs/150-intrinsic-layout-protocol/readiness/validation-summary.md
- [X] T051 [US4] Update consumer documentation for layout protocol usage, ScrollViewer extent, cache invalidation, diagnostics, compatibility, and readiness helpers in src/Layout/README.md, src/Controls/README.md, and src/Testing/README.md

**Checkpoint**: User Story 4 can be validated with `dotnet fsi scripts/refresh-surface-baselines.fsx`, `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature150`, and `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature150`.

---

## Phase 7: Polish and Cross-Cutting Concerns

**Purpose**: Complete Tier 1 baselines, quickstart validation, prior-evidence regression checks,
package validation, and final readiness records.

- [X] T052 [P] Refresh public surface baselines with scripts/refresh-surface-baselines.fsx and verify intentional Feature150 deltas in tests/surface-baselines/FS.GG.UI.Layout.txt, tests/surface-baselines/FS.GG.UI.Controls.txt, tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt, and tests/surface-baselines/FS.GG.UI.Testing.txt
- [X] T053 Run Layout protocol, diagnostic, intrinsic cache, and full/incremental parity validation and record command output in specs/150-intrinsic-layout-protocol/readiness/validation-summary.md using tests/Layout.Tests/Layout.Tests.fsproj
- [X] T054 Run ScrollViewer extent, layout diagnostics, and layout compatibility validation and record command output in specs/150-intrinsic-layout-protocol/readiness/scrollviewer-validation.md using tests/Controls.Tests/Controls.Tests.fsproj
- [X] T055 Run Controls.Elmish layout metric validation and record command output in specs/150-intrinsic-layout-protocol/readiness/intrinsic-cache-validation.md using tests/Elmish.Tests/Elmish.Tests.fsproj
- [X] T056 Run package surface, FSI transcript, compatibility ledger, and Testing helper validation and record command output in specs/150-intrinsic-layout-protocol/readiness/validation-summary.md using tests/Package.Tests/Package.Tests.fsproj and tests/Testing.Tests/Testing.Tests.fsproj
- [ ] T057 Run focused retained rendering, overlay, render-anywhere, text-shaping, compositor-readiness, disabled-cache, and default layout regression validation and record outcomes in specs/150-intrinsic-layout-protocol/readiness/validation-summary.md using FS.GG.Rendering.slnx
- [ ] T058 Run full solution build/test and local package validation, then record pack readiness and feed output in specs/150-intrinsic-layout-protocol/readiness/validation-summary.md using FS.GG.Rendering.slnx and ~/.local/share/nuget-local/

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **US1 Layout Protocol (Phase 3)**: Depends on Foundational and is the MVP.
- **US2 ScrollViewer Intrinsics (Phase 4)**: Depends on US1 public measurement and diagnostic contracts.
- **US3 Cache and Parity (Phase 5)**: Depends on US1 measurement contracts and US2 intrinsic extent path for scroll parity.
- **US4 Readiness (Phase 6)**: Depends on US1 and US2 for minimum reviewable readiness, and depends on US3 for accepted full/incremental parity claims.
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational and has no dependency on other user stories.
- **US2 (P1)**: Requires US1 Layout constraints, measurement, intrinsic query, and diagnostics contracts.
- **US3 (P2)**: Requires US1 measurement evidence and US2 intrinsic ScrollViewer extent behavior.
- **US4 (P3)**: Requires evidence from US1, US2, and US3 before claiming accepted P8 readiness.

### Within Each User Story

- Write tests first and confirm they fail for missing behavior.
- Draft `.fsi` public signatures before semantic/FSI transcript tests and before `.fs` implementation bodies.
- Update `.fsproj` compile ordering whenever new F# files are added.
- Keep `src/Layout` independent of Controls, SkiaViewer, KeyboardInput, charts, and harness projects.
- Record readiness evidence before marking any acceptance status as ready.

---

## Parallel Opportunities

- T004 and T005 can run in parallel because they write different test fixture files.
- US1 tests T008, T009, T010, and T011 can run in parallel after T007; T012 is the compile-order wiring task.
- US2 tests T019, T020, T021, and T022 can run in parallel after T017 and T018; implementation T024 and T025 can proceed in parallel before T026 replaces the ScrollViewer extent source.
- US3 tests T032, T033, T034, and T035 can run in parallel after T030 and T031; implementation T037 and T040 touch different packages and can proceed in parallel once contract names are stable.
- US4 tests T044, T045, and T046 can run in parallel after T043; implementation T048 and T049 touch different boundaries and can proceed in parallel before final summary T050.
- T052 can run in parallel with documentation review once public contracts are stable.

---

## Parallel Example: User Story 1

```bash
Task: "T008 Add FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
Task: "T009 Add constraint tests in tests/Layout.Tests/Feature150IntrinsicProtocolTests.fs"
Task: "T010 Add deterministic measurement tests in tests/Layout.Tests/Feature150MeasureDeterminismTests.fs"
Task: "T011 Add diagnostic tests in tests/Layout.Tests/Feature150LayoutDiagnosticsTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "T019 Add ScrollViewport FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
Task: "T020 Add ScrollViewer extent tests in tests/Controls.Tests/Feature150ScrollViewerExtentTests.fs"
Task: "T021 Add ScrollViewer diagnostic tests in tests/Controls.Tests/Feature150LayoutDiagnosticsTests.fs"
Task: "T022 Add default layout compatibility tests in tests/Controls.Tests/Feature150LayoutCompatibilityTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "T032 Add intrinsic cache tests in tests/Layout.Tests/Feature150IntrinsicCacheTests.fs"
Task: "T033 Add invalidation tests in tests/Layout.Tests/Feature150IntrinsicInvalidationTests.fs"
Task: "T034 Add full/incremental parity tests in tests/Layout.Tests/Feature150FullIncrementalParityTests.fs"
Task: "T035 Add Controls.Elmish metrics tests in tests/Elmish.Tests/Feature150LayoutMetricsTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "T044 Add compatibility ledger tests in tests/Package.Tests/Feature150CompatibilityLedgerTests.fs"
Task: "T045 Add readiness helper tests in tests/Testing.Tests/Feature150ReadinessHelperTests.fs"
Task: "T046 Extend FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Stop and validate `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150IntrinsicProtocol`.
5. Review the public `.fsi` shape before implementing US2.

### Incremental Delivery

1. Complete Setup and Foundational work.
2. Add US1 Layout protocol and validate it independently.
3. Add US2 ScrollViewer extent and validate it independently.
4. Add US3 cache/parity and validate it independently.
5. Add US4 readiness evidence and package validation.
6. Run Polish validation and update final readiness records.

### Parallel Team Strategy

With multiple contributors:

1. Team completes Setup and Foundational fixtures together.
2. After US1 public contracts stabilize, one contributor implements ScrollViewer extent, another implements cache/parity tests, and another prepares readiness/package validation.
3. Integrate only after each story's independent tests pass and readiness artifacts name the evidence.

---

## Notes

- [P] tasks write different files or can proceed without depending on incomplete task output.
- [US1], [US2], [US3], and [US4] map directly to the user stories in specs/150-intrinsic-layout-protocol/spec.md.
- All public changes are Tier 1: update `.fsi`, semantic/FSI tests, implementation, surface baselines, compatibility notes, and readiness evidence.
- Synthetic or fixture-only evidence must be named explicitly and cannot replace accepted parity, compatibility, or ScrollViewer evidence.
- Avoid layout package dependency drift: src/Layout may reference Scene and Yoga.Net only.
