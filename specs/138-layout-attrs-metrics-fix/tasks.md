# Tasks: Layout Attributes and Metrics Green

**Input**: Design documents from `/specs/138-layout-attrs-metrics-fix/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/layout-authoring-and-metrics.md, quickstart.md

**Tests**: Required by the feature specification. Write the listed tests before implementation and confirm they fail for the missing behavior.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independent increment after the shared foundation is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User story label from spec.md: US1, US2, US3, US4.
- Every task includes exact repository file paths.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Register Feature 138 test modules in the ordered F# project files before story work begins.

- [X] T001 Register placeholder Feature 138 Controls test modules in `tests/Controls.Tests/Controls.Tests.fsproj`, `tests/Controls.Tests/Feature138LayoutAttributesTests.fs`, and `tests/Controls.Tests/Feature138ShellChromeTests.fs`
- [X] T002 [P] Register placeholder Feature 138 Elmish metrics test module in `tests/Elmish.Tests/Elmish.Tests.fsproj` and `tests/Elmish.Tests/Feature138TextMetricsTests.fs`
- [X] T003 [P] Register placeholder Feature 138 incremental layout test module in `tests/Layout.Tests/Layout.Tests.fsproj` and `tests/Layout.Tests/Feature138IncrementalLayoutTests.fs`

---

## Phase 2: Foundational (Blocking Prerequisite)

**Purpose**: Establish the shared attribute-name vocabulary used by public builders, layout lowering, invalidation, and tests.

**CRITICAL**: No user story work should begin until this phase is complete.

- [X] T004 Add canonical layout attribute keys and the `spacing` compatibility alias in `src/Controls/Internal/AttrKeys.fs`

**Checkpoint**: Attribute names are centralized and ready for story implementation.

---

## Phase 3: User Story 1 - Author Flex Layout Values Directly (Priority: P1) MVP

**Goal**: Consumers can author padding, margin, gap, alignment, flex growth/shrink/basis, and min/max constraints through the public Controls surface, and those values affect measured bounds while omitted values preserve compatibility geometry.

**Independent Test**: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature138LayoutAttributes` shows authored values affect bounds, explicit zero overrides compatibility defaults, and no-authored-value examples keep prior bounds.

### Tests for User Story 1

- [X] T005 [US1] Add public builder signatures and XML docs for `gap`, `alignItems`, `alignSelf`, `justifyContent`, `flexGrow`, `flexShrink`, `flexBasis`, `minWidth`, `minHeight`, `maxWidth`, and `maxHeight` in `src/Controls/Attributes.fsi`
- [X] T006 [US1] Add failing public surface and FSI usage tests for all new `Attr` builders in `tests/Controls.Tests/Feature138LayoutAttributesTests.fs`
- [X] T007 [US1] Add failing authored-bounds tests for padding, margin, gap, alignment, flex grow, flex shrink, flex basis, and min/max clamps in `tests/Controls.Tests/Feature138LayoutAttributesTests.fs`
- [X] T008 [US1] Add failing compatibility tests for omitted values, authored default values, explicit zero values, and last-writer-wins behavior in `tests/Controls.Tests/Feature138LayoutAttributesTests.fs`

### Implementation for User Story 1

- [X] T009 [US1] Implement canonical layout builders using the shared keys and `LayoutAlign` values in `src/Controls/Attributes.fs`
- [X] T010 [US1] Preserve or lower typed spacing through the gap compatibility path in `src/Controls/Widgets/Primitives.fs` and `src/Controls/Widgets/Containers.fs`
- [X] T011 [US1] Project authored layout attributes into `LayoutIntent` fields while preserving omitted compatibility defaults and explicit zero overrides in `src/Controls/Control.fs`
- [X] T012 [US1] Update the intentional public API additions in `tests/surface-baselines/FS.GG.UI.Controls.txt`
- [X] T013 [US1] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature138LayoutAttributes` and resolve failures in `tests/Controls.Tests/Feature138LayoutAttributesTests.fs`

**Checkpoint**: US1 is functional and independently testable through Controls tests.

---

## Phase 4: User Story 2 - Incremental Layout Knows Geometry-Affecting Values (Priority: P1)

**Goal**: Every newly supported geometry-driving attribute invalidates incremental layout when changed or removed, while visual-only changes stay out of geometry invalidation.

**Independent Test**: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature101LayoutDriftGuard` and `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature138IncrementalLayout` prove dirty-set coverage, visual-only exclusion, removal invalidation, and incremental/full equivalence.

### Tests for User Story 2

- [X] T014 [US2] Extend the failing drift corpus for all canonical layout names, `spacing`, and visual-only exclusions in `tests/Controls.Tests/Feature101LayoutDriftGuardTests.fs`
- [X] T015 [US2] Add failing changed-value and removed-value invalidation tests for padding, margin, gap, alignment, flex, and min/max names in `tests/Controls.Tests/Feature101LayoutDriftGuardTests.fs`
- [X] T016 [P] [US2] Add failing full-layout versus incremental-layout equivalence tests for each new authored layout value in `tests/Layout.Tests/Feature138IncrementalLayoutTests.fs`

### Implementation for User Story 2

- [X] T017 [US2] Expand `ControlInternals.layoutAffectingAttrNames` to match the keys read by `toLayout` in `src/Controls/Control.fs`
- [X] T018 [US2] Update retained dirty-set handling for changed and removed layout attributes in `src/Controls/RetainedRender.fs`
- [X] T019 [US2] Align the behavioral drift probes with the final `toLayout` read set in `tests/Controls.Tests/Feature101LayoutDriftGuardTests.fs`
- [X] T020 [US2] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature101LayoutDriftGuard` and `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature138IncrementalLayout` and resolve failures in `src/Controls/Control.fs`

**Checkpoint**: US2 is functional and independently testable through dirty-set and incremental-layout tests.

---

## Phase 5: User Story 3 - Shell Chrome Can Be Pinned Without Special-Case Fixes (Priority: P2)

**Goal**: A shell screen can pin header, footer, and navigation chrome while flexible content receives the remaining bounded space using public authored layout values.

**Independent Test**: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature138ShellChrome` proves fixed chrome and bounded content at the validated `640x480` and `400x300` viewport sizes.

### Tests for User Story 3

- [X] T021 [US3] Add failing shell chrome tests for fixed header, fixed footer, fixed navigation, and flexible content at `640x480` and `400x300` in `tests/Controls.Tests/Feature138ShellChromeTests.fs`
- [X] T022 [US3] Add failing shell chrome tests for tall content staying bounded while chrome remains visible at `640x480` and `400x300` in `tests/Controls.Tests/Feature138ShellChromeTests.fs`

### Implementation for User Story 3

- [X] T023 [US3] Update typed stack and container lowering so shell fixtures can express fixed chrome and flexible content through public attributes in `src/Controls/Widgets/Primitives.fs` and `src/Controls/Widgets/Containers.fs`
- [X] T024 [US3] Apply shell-chrome flex pinning fixes for `flexShrink = 0`, `flexGrow`, width/height, and min/max lowering in `src/Controls/Control.fs`
- [X] T025 [US3] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature138ShellChrome` and resolve failures in `tests/Controls.Tests/Feature138ShellChromeTests.fs`

**Checkpoint**: US3 is functional and independently testable through shell-chrome Controls tests.

---

## Phase 6: User Story 4 - Text-Cache Metrics Are Truthful Before Refactors (Priority: P2)

**Goal**: Frame metrics distinguish cold text measurement, warm prior-frame reuse, style-only frames, and idle frames deterministically.

**Independent Test**: `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature138TextMetrics` and the retained text-cache tests prove cold hits are zero, warm hits are nonzero, style-only/idle work is zero, and repeated scripts are byte-identical.

### Tests for User Story 4

- [X] T026 [US4] Update internal metric-window contract docs and signatures for frame-start resident key accounting in `src/Controls/RetainedRender.fsi`
- [X] T027 [US4] Add failing retained-render metric tests proving same-frame duplicate text does not count as a cold-frame hit in `tests/Controls.Tests/Feature117TextCacheTests.fs`
- [X] T028 [P] [US4] Add failing public `FrameMetrics` sequence tests for cold, warm, style-only, idle, and repeated captures in `tests/Elmish.Tests/Feature138TextMetricsTests.fs`

### Implementation for User Story 4

- [X] T029 [US4] Implement frame-start resident key snapshots and prior-frame hit classification in `src/Controls/RetainedRender.fs`
- [X] T030 [US4] Preserve same-frame text-cache reuse while recording same-frame inserts as misses in `src/Controls/RetainedRender.fs`
- [X] T031 [US4] Propagate corrected `WorkReduction` text-cache counts through the deterministic metrics path in `src/Controls.Elmish/ControlsElmish.fs`
- [X] T032 [US4] Update public `FrameMetrics` XML docs to define text-cache hit as prior-frame reuse in `src/Controls.Elmish/ControlsElmish.fsi`
- [X] T033 [US4] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature117TextCache` and `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature138TextMetrics` and resolve failures in `src/Controls/RetainedRender.fs`

**Checkpoint**: US4 is functional and independently testable through retained-render and Elmish metrics tests.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate the full focused scope, public surface, documentation, and broad preflight.

- [X] T034 Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj` and record the result in `specs/138-layout-attrs-metrics-fix/quickstart.md`
- [X] T035 Run `dotnet test tests/Layout.Tests/Layout.Tests.fsproj` and record the result in `specs/138-layout-attrs-metrics-fix/quickstart.md`
- [X] T036 Run `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj` and record the result in `specs/138-layout-attrs-metrics-fix/quickstart.md`
- [X] T037 Run `./fake.sh build -t PackageSurfaceCheck` and update intentional surface changes in `tests/surface-baselines/FS.GG.UI.Controls.txt`
- [X] T038 Run `./fake.sh build -t VerifyPreflight` and record the result or environment limitation in `specs/138-layout-attrs-metrics-fix/quickstart.md`
- [X] T039 [P] Document the new public layout builders, compatibility defaults, and `spacing` alias behavior in `src/Controls/README.md`
- [X] T040 Review that no new runtime dependencies or renderer refactors were introduced and record the check in `specs/138-layout-attrs-metrics-fix/plan.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 and blocks all user stories.
- **US1 (Phase 3)**: Depends on Phase 2. This is the MVP and should complete before US3.
- **US2 (Phase 4)**: Depends on Phase 2. It can be developed alongside US1 after the shared names exist, but final dirty-set coverage must match US1's `toLayout` read set.
- **US3 (Phase 5)**: Depends on US1 for public authored layout values and on US2 for incremental correctness.
- **US4 (Phase 6)**: Depends on Phase 2 only; it is independent of layout authoring stories.
- **Polish (Phase 7)**: Depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: Start after Phase 2; no dependency on other stories.
- **US2 (P1)**: Start after Phase 2; coordinate with US1 so the dirty-set guard matches `toLayout`.
- **US3 (P2)**: Start after US1; complete after US2 if shell incremental parity is included in validation.
- **US4 (P2)**: Start after Phase 2; no dependency on US1, US2, or US3.

### Within Each User Story

- Draft `.fsi` signatures and contract docs first where the story changes a public or internal signature.
- Write tests and confirm they fail before implementation.
- Implement `.fs` bodies after signatures and failing tests exist.
- Update surface baselines only for intentional public API changes.
- Run the independent test command before moving to the next checkpoint.

---

## Parallel Opportunities

- T002 and T003 can run alongside T001 because they touch different test projects.
- T016 can run alongside T014 and T015 because it lives in `tests/Layout.Tests/Feature138IncrementalLayoutTests.fs`.
- T028 can run alongside T027 because it lives in `tests/Elmish.Tests/Feature138TextMetricsTests.fs`.
- T039 can run alongside final validation command runs because it only edits `src/Controls/README.md`.
- US4 can proceed in parallel with US1/US2 after Phase 2 because text-cache metrics are independent of layout-authoring implementation.

## Parallel Example: User Story 1

```bash
Task: "T006 Add failing public surface and FSI usage tests in tests/Controls.Tests/Feature138LayoutAttributesTests.fs"
Task: "T009 Implement canonical layout builders using the shared keys in src/Controls/Attributes.fs"
```

Note: T009 should wait for T005 and T006 during strict TDD execution; the example shows that builder implementation is isolated from `Control.fs` lowering work.

## Parallel Example: User Story 2

```bash
Task: "T014 Extend the failing drift corpus in tests/Controls.Tests/Feature101LayoutDriftGuardTests.fs"
Task: "T016 Add failing full-layout versus incremental-layout equivalence tests in tests/Layout.Tests/Feature138IncrementalLayoutTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "T021 Add failing shell chrome tests at 640x480 and 400x300 in tests/Controls.Tests/Feature138ShellChromeTests.fs"
Task: "T023 Update typed stack and container lowering in src/Controls/Widgets/Primitives.fs and src/Controls/Widgets/Containers.fs"
```

Note: T023 should wait for the public builder shape from US1 when strict sequencing is required.

## Parallel Example: User Story 4

```bash
Task: "T027 Add retained-render metric tests in tests/Controls.Tests/Feature117TextCacheTests.fs"
Task: "T028 Add public FrameMetrics sequence tests in tests/Elmish.Tests/Feature138TextMetricsTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 (US1).
3. Validate `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature138LayoutAttributes`.
4. Stop and review public API shape, omitted-value compatibility, explicit-zero behavior, and the Controls surface baseline.

### Incremental Delivery

1. Ship US1 to expose and prove authored layout values.
2. Add US2 to make incremental layout invalidation correct for those values.
3. Add US3 to prove real shell chrome layout without special-case fixes.
4. Add US4 to fix public text-cache metrics before renderer refactors use them as evidence.
5. Run Phase 7 validation before readiness sign-off.

### Parallel Team Strategy

1. One developer completes Phase 1 and Phase 2.
2. Developer A works US1 public authoring and bounds tests.
3. Developer B works US2 dirty-set and incremental equivalence tests.
4. Developer C works US4 metrics tests and retained-render accounting.
5. US3 starts after US1 because it consumes the public authoring surface.

---

## Independent Test Criteria Summary

- **US1**: Authored layout values affect bounds, omitted values preserve compatibility bounds, and explicit zero values override compatibility defaults in `tests/Controls.Tests/Feature138LayoutAttributesTests.fs`.
- **US2**: Dirty-set coverage equals the `toLayout` read set, visual-only names remain excluded, removed layout values invalidate, and incremental layout equals full layout in `tests/Controls.Tests/Feature101LayoutDriftGuardTests.fs` and `tests/Layout.Tests/Feature138IncrementalLayoutTests.fs`.
- **US3**: Shell header, footer, navigation, and content region keep requested geometry at `640x480` and `400x300` in `tests/Controls.Tests/Feature138ShellChromeTests.fs`.
- **US4**: Cold/warm/style-only/idle text-heavy scripts report truthful, deterministic `FrameMetrics` through `tests/Elmish.Tests/Feature138TextMetricsTests.fs`.
