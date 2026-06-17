# Tasks: Overlay Host Widget Integration

**Input**: Design documents from `/specs/144-overlay-host-widget-integration/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/overlay-host-widget-integration.md`, `quickstart.md`

**Tests**: Included because the feature specification requires mandatory test evidence for the Tier 1 interaction/package change.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independently testable increment.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the feature evidence area before public-contract work starts.

- [X] T001 Create the Feature 144 readiness index in `specs/144-overlay-host-widget-integration/readiness/README.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish FSI-first contracts and reusable fixtures that all story phases depend on.

**Critical**: No user story implementation should start until this phase is complete.

- [X] T002 [P] Draft the transient widget metadata, activation request, routing frame, and product dispatch public contracts in `src/Controls/Control.fsi`
- [X] T003 [P] Draft the overlay runtime bridge effect interpretation contract in `src/Controls/ControlRuntime.fsi`
- [X] T004 [P] Draft overlay-aware pointer decision contracts in `src/Controls/Pointer.fsi`
- [X] T005 [P] Draft focus recovery and modal traversal contracts in `src/Controls/Focus.fsi`
- [X] T006 [P] Draft overlay-aware host routing contracts in `src/Controls.Elmish/ControlsElmish.fsi`
- [X] T007 [P] Create failing Controls public-FSI semantic tests for transient metadata, runtime effects, pointer decisions, and focus contracts in `tests/Controls.Tests/Feature144FsiSemanticTests.fs` and add the compile entry in `tests/Controls.Tests/Controls.Tests.fsproj`
- [X] T008 [P] Create failing Controls.Elmish public-FSI semantic tests for overlay host routing contracts in `tests/Elmish.Tests/Feature144FsiSemanticTests.fs` and add the compile entry in `tests/Elmish.Tests/Elmish.Tests.fsproj`
- [X] T009 [P] Create shared eight-category overlay surface fixtures in `tests/Controls.Tests/Feature144OverlayFixtures.fs`
- [X] T010 [P] Add the shared Controls overlay fixture compile entry for `tests/Controls.Tests/Feature144OverlayFixtures.fs` in `tests/Controls.Tests/Controls.Tests.fsproj`
- [X] T011 [P] Create shared product-dispatch overlay scripts in `tests/Elmish.Tests/Feature144OverlayDispatchFixtures.fs`
- [X] T012 [P] Add the shared Elmish overlay dispatch fixture compile entry for `tests/Elmish.Tests/Feature144OverlayDispatchFixtures.fs` in `tests/Elmish.Tests/Elmish.Tests.fsproj`
- [X] T013 Record the approved FSI-first contract sketch and any public-surface impact in `specs/144-overlay-host-widget-integration/readiness/fsi-design.md`
- [X] T014 Record synthetic fixture disclosure requirements, including `Synthetic` test names, `// SYNTHETIC:` use-site comments, and PR/readiness listing rules, in `specs/144-overlay-host-widget-integration/readiness/synthetic-evidence.md`

**Checkpoint**: Public signatures and shared fixtures are ready; story work can begin.

---

## Phase 3: User Story 1 - Expose Transient Widget Behavior (Priority: P1) (MVP)

**Goal**: Supported transient controls disclose complete metadata for open, placement, dismissal, focus, and selection while preserving product-owned state.

**Independent Test**: Inspect public authoring paths for all eight supported surface categories and verify enabled controls expose metadata, disabled triggers do not open, missing metadata fails readiness, and closed-state output remains compatible.

### Tests for User Story 1

- [X] T015 [P] [US1] Create failing metadata coverage tests for all eight supported categories in `tests/Controls.Tests/Feature144TransientMetadataTests.fs` and add the compile entry in `tests/Controls.Tests/Controls.Tests.fsproj`
- [X] T016 [P] [US1] Create failing disabled-trigger and missing-metadata readiness tests in `tests/Controls.Tests/Feature144TransientMetadataFailureTests.fs` and add the compile entry in `tests/Controls.Tests/Controls.Tests.fsproj`
- [X] T017 [P] [US1] Create failing closed-state compatibility tests for transient controls in `tests/Controls.Tests/Feature144ClosedStateCompatibilityTests.fs` and add the compile entry in `tests/Controls.Tests/Controls.Tests.fsproj`

### Implementation for User Story 1

- [X] T018 [US1] Implement transient metadata storage, extraction, validation, and activation-request helpers in `src/Controls/Control.fs`
- [X] T019 [US1] Implement menu and context-menu metadata lowering in `src/Controls/Widgets/Navigation.fsi` and `src/Controls/Widgets/Navigation.fs`
- [X] T020 [US1] Implement split-button menu metadata lowering in `src/Controls/Widgets/Buttons.fsi` and `src/Controls/Widgets/Buttons.fs`
- [X] T021 [US1] Implement combo dropdown metadata lowering in `src/Controls/Widgets/CollectionsWidgets.fsi` and `src/Controls/Widgets/CollectionsWidgets.fs`
- [X] T022 [US1] Implement auto-complete suggestion metadata lowering in `src/Controls/DataEntry2.fsi` and `src/Controls/DataEntry2.fs`
- [X] T023 [US1] Implement date-picker calendar metadata lowering in `src/Controls/Widgets/Pickers.fsi` and `src/Controls/Widgets/Pickers.fs`
- [X] T024 [US1] Implement color-picker palette metadata lowering in `src/Controls/Widgets/Pickers.fsi` and `src/Controls/Widgets/Pickers.fs`
- [X] T025 [US1] Implement dialog modal metadata lowering in `src/Controls/Widgets/Overlay.fsi` and `src/Controls/Widgets/Overlay.fs`
- [X] T026 [US1] Update supported transient category metadata in `src/Controls/catalog.yml`
- [X] T027 [US1] Extend eight-category enabled, disabled, empty, and missing-anchor fixtures in `tests/Controls.Tests/Feature144OverlayFixtures.fs`
- [X] T028 [US1] Record metadata coverage, disabled-trigger behavior, and closed-state compatibility evidence in `specs/144-overlay-host-widget-integration/readiness/metadata-coverage.md`

**Checkpoint**: User Story 1 is independently testable through Controls.Tests and readiness metadata coverage.

---

## Phase 4: User Story 2 - Route Pointer, Keyboard, and Focus Through Overlay State (Priority: P1)

**Goal**: Open transient surfaces receive topmost pointer, keyboard, and focus routing before covered content, with deterministic dismissal, modal blocking, pass-through, and exactly-once dispatch.

**Independent Test**: Run scripted pointer and keyboard interactions against representative surfaces and compare open state, focus state, dismissal reason, topmost hit target, product messages, diagnostics, and replay output across repeated runs.

### Tests for User Story 2

- [X] T029 [P] [US2] Create failing topmost, outside-dismiss, modal-blocking, and pass-through pointer tests in `tests/Controls.Tests/Feature144OverlayPointerRoutingTests.fs` and add the compile entry in `tests/Controls.Tests/Controls.Tests.fsproj`
- [X] T030 [P] [US2] Create failing focus entry, modal traversal, stale-target, and recovery tests in `tests/Controls.Tests/Feature144OverlayFocusRoutingTests.fs` and add the compile entry in `tests/Controls.Tests/Controls.Tests.fsproj`
- [X] T031 [P] [US2] Create failing retained/direct host routing parity tests in `tests/Elmish.Tests/Feature144OverlayHostRoutingTests.fs` and add the compile entry in `tests/Elmish.Tests/Elmish.Tests.fsproj`
- [X] T032 [P] [US2] Create failing Escape, Tab, activation, navigation, and selection keyboard evidence tests in `tests/KeyboardInput.Tests/Feature144OverlayKeyboardEvidenceTests.fs` and add the compile entry in `tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj`

### Implementation for User Story 2

- [X] T033 [US2] Implement overlay-aware pointer decision logic and evidence emission in `src/Controls/Pointer.fs`
- [X] T034 [US2] Implement focus recovery, modal trap traversal, stale-target recovery, and diagnostics in `src/Controls/Focus.fs`
- [X] T035 [US2] Implement overlay effect interpretation and exactly-once runtime dispatch records in `src/Controls/ControlRuntime.fs`
- [X] T036 [US2] Route retained pointer samples through overlay decisions before lower content in `src/Controls.Elmish/ControlsElmish.fs`
- [X] T037 [US2] Route Escape, Tab, activation, navigation, selection, and fallback keys through overlay state in `src/Controls.Elmish/ControlsElmish.fs`
- [X] T038 [US2] Add or confirm normalized overlay key evidence for Escape, Tab, Enter, Space, and arrow keys in `src/KeyboardInput/KeyboardInput.fsi` and `src/KeyboardInput/KeyboardInput.fs`
- [X] T039 [US2] Record pointer, keyboard, focus, modal blocking, pass-through, and exactly-once dispatch evidence in `specs/144-overlay-host-widget-integration/readiness/routing.md`

**Checkpoint**: User Story 2 is independently testable through Controls.Tests, Elmish.Tests, and KeyboardInput.Tests.

---

## Phase 5: User Story 3 - Preserve Product-Owned Visibility and Compatibility (Priority: P1)

**Goal**: Runtime interaction emits product-visible requests without silently mutating product-owned open, selected, value, or focus state, and compatibility impact is documented.

**Independent Test**: Run compatibility fixtures for explicit open-state controls and verify state-change requests are product-visible, closed-state output stays compatible, and migration guidance covers every intentional public contract or baseline change.

### Tests for User Story 3

- [X] T040 [P] [US3] Create failing product-owned open and close request tests in `tests/Elmish.Tests/Feature144ProductOwnedVisibilityTests.fs` and add the compile entry in `tests/Elmish.Tests/Elmish.Tests.fsproj`
- [X] T041 [P] [US3] Create failing selection, command, focus, and duplicate-dispatch tests in `tests/Elmish.Tests/Feature144ProductDispatchTests.fs` and add the compile entry in `tests/Elmish.Tests/Elmish.Tests.fsproj`
- [X] T042 [P] [US3] Create failing public-surface and compatibility contract tests in `tests/Controls.Tests/Feature144CompatibilityContractTests.fs` and add the compile entry in `tests/Controls.Tests/Controls.Tests.fsproj`

### Implementation for User Story 3

- [X] T043 [US3] Preserve product-owned visibility by keeping open and selected state out of `ControlRuntimeModel` in `src/Controls/ControlRuntime.fs`
- [X] T044 [US3] Map open, close, selection, command, focus, and diagnostic overlay effects to product-visible messages in `src/Controls.Elmish/ControlsElmish.fs`
- [X] T045 [US3] Update product-owned state XML documentation for transient widget authoring contracts in `src/Controls/Widgets/Buttons.fsi`, `src/Controls/Widgets/CollectionsWidgets.fsi`, `src/Controls/Widgets/Navigation.fsi`, `src/Controls/Widgets/Overlay.fsi`, and `src/Controls/Widgets/Pickers.fsi`
- [X] T046 [US3] Update the Controls public surface baseline when metadata contracts change in `tests/surface-baselines/FS.GG.UI.Controls.txt`
- [X] T047 [US3] Update the Controls.Elmish public surface baseline when host routing contracts change in `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt`
- [X] T048 [US3] Write compatibility impact, migration guidance, and versioning rationale in `specs/144-overlay-host-widget-integration/readiness/compatibility.md`
- [X] T049 [US3] Record closed-state visual, hit-test, diagnostic, and authoring compatibility evidence in `specs/144-overlay-host-widget-integration/readiness/closed-state-compatibility.md`

**Checkpoint**: User Story 3 is independently testable through product-owned visibility tests, dispatch tests, and public-surface validation.

---

## Phase 6: User Story 4 - Demonstrate the Live Reference Date Picker Flow (Priority: P2)

**Goal**: AntShowcase date picker proves a complete live flow: closed initial state, open, navigate, select, dismiss, focus recover, no stale overlay, and evidence output.

**Independent Test**: Run the reference date-picker scenario end to end and verify product state, overlay state, focus state, final closed render, hit-test behavior, and readiness evidence.

### Tests for User Story 4

- [X] T050 [P] [US4] Create failing AntShowcase open, navigate, select, dismiss, and focus-recover tests in `samples/AntShowcase/AntShowcase.Tests/Feature144DatePickerFlowTests.fs` and add the compile entry in `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`
- [X] T051 [P] [US4] Create failing no-stale-overlay final render and hit-test tests in `samples/AntShowcase/AntShowcase.Tests/Feature144DatePickerStaleOverlayTests.fs` and add the compile entry in `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`

### Implementation for User Story 4

- [X] T052 [US4] Wire product-owned date-picker open, selected date, and focus state in `samples/AntShowcase/AntShowcase.Core/DemoState.fs`
- [X] T053 [US4] Add Feature 144 date-picker trigger, navigation, selection, dismissal, and focus recovery script steps in `samples/AntShowcase/AntShowcase.Core/Scripts.fs`
- [X] T054 [US4] Update the AntShowcase date-picker page to publish transient metadata and product-visible overlay requests in `samples/AntShowcase/AntShowcase.Core/Pages.fs`
- [X] T055 [US4] Extend AntShowcase evidence records for replay log, focus transitions, product messages, diagnostics, and no-stale-overlay proof in `samples/AntShowcase/AntShowcase.Core/Evidence.fs`
- [X] T056 [US4] Persist AntShowcase app evidence artifacts into the feature readiness folder in `samples/AntShowcase/AntShowcase.App/Evidence.fs`
- [X] T057 [US4] Record the live reference date-picker evidence bundle in `specs/144-overlay-host-widget-integration/readiness/reference-date-picker.md`

**Checkpoint**: User Story 4 is independently testable through AntShowcase.Tests and the reference readiness evidence.

---

## Phase 7: User Story 5 - Prove Integrated Overlay Rendering and Auditability (Priority: P2)

**Goal**: Integrated overlays produce deterministic replay logs, direct/retained/cache parity, compatible closed-state output, and real visual proof or an explicit unsupported-host limitation.

**Independent Test**: Replay overlay fixture corpus across direct rendering, first retained frame, warm retained frame, cache-enabled mode, cache-disabled mode, and available offscreen visual path, then compare logs, focus, product messages, diagnostics, visible output, and hit order.

### Tests for User Story 5

- [X] T058 [P] [US5] Create failing direct, retained, cache-enabled, and cache-disabled overlay parity tests in `tests/Rendering.Harness.Tests/Feature144OverlayRenderingParityTests.fs` and add the compile entry in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`
- [X] T059 [P] [US5] Create failing three-run replay determinism tests in `tests/Elmish.Tests/Feature144OverlayReplayDeterminismTests.fs` and add the compile entry in `tests/Elmish.Tests/Elmish.Tests.fsproj`
- [X] T060 [P] [US5] Create failing visual proof or unsupported-host limitation tests in `tests/Rendering.Harness.Tests/Feature144OverlayVisualProofTests.fs` and add the compile entry in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`

### Implementation for User Story 5

- [X] T061 [US5] Extend rendering evidence helpers for overlay logs, product messages, hit order, and diagnostics in `tests/Rendering.Harness/Evidence.fsi` and `tests/Rendering.Harness/Evidence.fs`
- [X] T062 [US5] Add representative overlay fixture corpus inputs for at least 100 generated scenes in `tests/Rendering.Harness/Input.fsi` and `tests/Rendering.Harness/Input.fs`
- [X] T063 [US5] Add offscreen visual proof capture or unsupported-host limitation recording in `tests/Rendering.Harness/Live.fsi` and `tests/Rendering.Harness/Live.fs`
- [X] T064 [US5] Expose Feature 140 layer and portal order evidence needed for overlay proof in `src/Controls/Composition.fsi` and `src/Controls/Composition.fs`
- [X] T065 [US5] Record direct, retained, cache-enabled, and cache-disabled parity evidence in `specs/144-overlay-host-widget-integration/readiness/rendering-parity.md`
- [X] T066 [US5] Record real visual proof artifact path or unsupported-host owner, cause, next proof path, and trust rationale in `specs/144-overlay-host-widget-integration/readiness/visual-proof.md`
- [X] T067 [US5] Record scope review confirming no P6/render-anywhere, compositor, intrinsic layout, text, editing, or widget-catalog work in `specs/144-overlay-host-widget-integration/readiness/scope-review.md`

**Checkpoint**: User Story 5 is independently testable through Rendering.Harness.Tests, Elmish replay tests, and readiness audit records.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Complete validation, documentation, and readiness records after the desired story scope is implemented.

- [X] T068 [P] Update quickstart command outcomes and environment notes in `specs/144-overlay-host-widget-integration/readiness/quickstart-validation.md`
- [X] T069 [P] Update the P5 status and Feature 144 evidence links in `docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md`
- [X] T070 Run `dotnet restore FS.GG.Rendering.slnx` and record restore evidence in `specs/144-overlay-host-widget-integration/readiness/build.md`
- [X] T071 Run `dotnet build FS.GG.Rendering.slnx` and record warnings-as-errors build evidence in `specs/144-overlay-host-widget-integration/readiness/build.md`
- [X] T072 Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature144|Feature143"` and record results in `specs/144-overlay-host-widget-integration/readiness/test-results.md`
- [X] T073 Run `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature144|Feature143"` and record results in `specs/144-overlay-host-widget-integration/readiness/test-results.md`
- [X] T074 Run `dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj --filter "Feature144|Feature143"` and record results in `specs/144-overlay-host-widget-integration/readiness/test-results.md`
- [X] T075 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature144|Feature143"` and record results in `specs/144-overlay-host-widget-integration/readiness/test-results.md`
- [X] T076 Run `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj --filter "Feature144|Feature143|DatePicker"` and record results in `specs/144-overlay-host-widget-integration/readiness/test-results.md`
- [X] T077 Run `dotnet fsi scripts/refresh-surface-baselines.fsx` when public surface changes and record baseline evidence in `specs/144-overlay-host-widget-integration/readiness/surface-baselines.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion; blocks user story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational completion; this is the MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational completion; can begin with overlay fixtures, and final live-widget coverage depends on User Story 1 metadata.
- **User Story 3 (Phase 5)**: Depends on Foundational completion; can begin with overlay fixtures, and compatibility review depends on User Story 1 public surface.
- **User Story 4 (Phase 6)**: Depends on User Stories 1, 2, and 3 for live metadata, routing, and product-owned state.
- **User Story 5 (Phase 7)**: Depends on User Stories 1, 2, and 3 for integrated overlay behavior; visual and corpus proof should include User Story 4 when available.
- **Polish (Phase 8)**: Depends on all selected user stories.

### User Story Dependencies

- **US1 (P1)**: Start after Foundational; no dependency on other stories.
- **US2 (P1)**: Start after Foundational with disclosed synthetic overlay fixtures from T009/T014; complete live-widget validation after US1.
- **US3 (P1)**: Start after Foundational with disclosed synthetic overlay fixtures from T009/T014; complete compatibility readiness after US1 and US2 dispatch paths.
- **US4 (P2)**: Start after US1, US2, and US3.
- **US5 (P2)**: Start after US1, US2, and US3; include US4 in the corpus once US4 is complete.

### Within Each User Story

- Write tests first and confirm they fail before implementation.
- Implement or update `.fsi` contracts before corresponding `.fs` implementations.
- Implement pure Controls contracts before Controls.Elmish host interpretation.
- Implement product-visible dispatch mapping before readiness evidence claims exactly-once behavior.
- Complete each story checkpoint before moving to lower-priority stories unless working in parallel with separate files.

---

## Parallel Execution Examples

### User Story 1

```bash
Task: "T015 Create failing metadata coverage tests for all eight supported categories in tests/Controls.Tests/Feature144TransientMetadataTests.fs"
Task: "T016 Create failing disabled-trigger and missing-metadata readiness tests in tests/Controls.Tests/Feature144TransientMetadataFailureTests.fs"
Task: "T017 Create failing closed-state compatibility tests for transient controls in tests/Controls.Tests/Feature144ClosedStateCompatibilityTests.fs"
```

### User Story 2

```bash
Task: "T029 Create failing topmost, outside-dismiss, modal-blocking, and pass-through pointer tests in tests/Controls.Tests/Feature144OverlayPointerRoutingTests.fs"
Task: "T030 Create failing focus entry, modal traversal, stale-target, and recovery tests in tests/Controls.Tests/Feature144OverlayFocusRoutingTests.fs"
Task: "T031 Create failing retained/direct host routing parity tests in tests/Elmish.Tests/Feature144OverlayHostRoutingTests.fs"
Task: "T032 Create failing Escape, Tab, activation, navigation, and selection keyboard evidence tests in tests/KeyboardInput.Tests/Feature144OverlayKeyboardEvidenceTests.fs"
```

### User Story 3

```bash
Task: "T040 Create failing product-owned open and close request tests in tests/Elmish.Tests/Feature144ProductOwnedVisibilityTests.fs"
Task: "T041 Create failing selection, command, focus, and duplicate-dispatch tests in tests/Elmish.Tests/Feature144ProductDispatchTests.fs"
Task: "T042 Create failing public-surface and compatibility contract tests in tests/Controls.Tests/Feature144CompatibilityContractTests.fs"
```

### User Story 4

```bash
Task: "T050 Create failing AntShowcase open, navigate, select, dismiss, and focus-recover tests in samples/AntShowcase/AntShowcase.Tests/Feature144DatePickerFlowTests.fs"
Task: "T051 Create failing no-stale-overlay final render and hit-test tests in samples/AntShowcase/AntShowcase.Tests/Feature144DatePickerStaleOverlayTests.fs"
```

### User Story 5

```bash
Task: "T058 Create failing direct, retained, cache-enabled, and cache-disabled overlay parity tests in tests/Rendering.Harness.Tests/Feature144OverlayRenderingParityTests.fs"
Task: "T059 Create failing three-run replay determinism tests in tests/Elmish.Tests/Feature144OverlayReplayDeterminismTests.fs"
Task: "T060 Create failing visual proof or unsupported-host limitation tests in tests/Rendering.Harness.Tests/Feature144OverlayVisualProofTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational contracts and shared fixtures.
3. Complete Phase 3: User Story 1 metadata integration.
4. Stop and validate User Story 1 with `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature144|Feature143"`.
5. Record metadata and compatibility evidence before starting host routing.

### Incremental Delivery

1. Deliver Setup and Foundational contracts.
2. Deliver US1 metadata coverage as the MVP.
3. Deliver US2 routing and input behavior.
4. Deliver US3 product-owned visibility and compatibility.
5. Deliver US4 AntShowcase reference date-picker flow.
6. Deliver US5 rendering, replay, visual proof, and auditability.
7. Complete Phase 8 validation and readiness records.

### Parallel Team Strategy

1. One contributor completes setup and shared FSI contracts.
2. Controls-focused work proceeds on US1 metadata while host-focused work starts US2 tests against fixtures.
3. A compatibility-focused contributor works on US3 tests and readiness records.
4. After US1 through US3 are green, AntShowcase and rendering-harness work can proceed in parallel.

---

## Notes

- `[P]` tasks touch different files and do not depend on incomplete same-phase tasks.
- `[US1]` through `[US5]` labels map directly to the five user stories in `spec.md`.
- Public API changes require `.fsi` updates, semantic tests, surface-baseline updates, migration guidance, and versioning rationale.
- Product-owned visibility remains the source of truth; runtime and host code emit requests instead of mutating product model state.
- Synthetic overlay fixtures must be disclosed with `Synthetic` test names, `// SYNTHETIC:` use-site comments, and an entry in `specs/144-overlay-host-widget-integration/readiness/synthetic-evidence.md`.
- Unsupported offscreen visual hosts must be recorded as limitations with owner, cause, next proof path, and behavioral evidence rationale.
