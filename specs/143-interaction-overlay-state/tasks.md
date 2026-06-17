# Tasks: Interaction Overlay State (Feature 143)

**Input**: Design documents from `/specs/143-interaction-overlay-state/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/interaction-overlay-state.md, quickstart.md

**Tests**: Required. The feature specification makes semantic, replay, parity, compatibility, and reference-flow verification mandatory.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independently reviewable increment.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare compile-order, test-project, and readiness evidence slots before feature work begins.

- [X] T001 Add `src/Controls/OverlayState.fsi` and `src/Controls/OverlayState.fs` compile entries before `ControlRuntime` in `src/Controls/Controls.fsproj`
- [X] T002 [P] Add `Feature143OverlayFixtures.fs`, `Feature143OverlayStateContractTests.fs`, `Feature143InteractionOverlayStateTests.fs`, `Feature143OverlayHitTestTests.fs`, `Feature143ClosedStateCompatibilityTests.fs`, `Feature143OverlayFocusTests.fs`, `Feature143OverlayModalTests.fs`, `Feature143OverlayModalHitTests.fs`, `Feature143OverlayReplayTests.fs`, and `Feature143ReferenceDatePickerTests.fs` compile entries before `Program.fs` in `tests/Controls.Tests/Controls.Tests.fsproj`
- [X] T003 [P] Add `Feature143OverlayDispatchTests.fs` and `Feature143OverlayParityTests.fs` compile entries before `Program.fs` in `tests/Elmish.Tests/Elmish.Tests.fsproj`
- [X] T004 [P] Add `Feature143OverlayKeyboardTests.fs` compile entry before `Program.fs` in `tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj`
- [X] T005 [P] Add `Feature143OverlayRenderingTests.fs` compile entry before `Program.fs` in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`
- [X] T006 [P] Add `Feature143DatePickerFlowTests.fs` compile entry before `Main.fs` in `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`
- [X] T007 [P] Create the Feature143 readiness index in `specs/143-interaction-overlay-state/readiness/feature143-readiness.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the `.fsi`-first overlay coordinator contract, shared diagnostics, and fixture support that all user stories need.

**Critical**: No user-story implementation should start until this phase is complete.

- [X] T008 [P] Create failing overlay coordinator contract tests for init, finite surface kinds, effect ordering, and invalid-message diagnostics in `tests/Controls.Tests/Feature143OverlayStateContractTests.fs`
- [X] T009 [P] Create or update the public FSI transcript for `OverlayState.init` and `OverlayState.update` in `scripts/controls-prelude.fsx`
- [X] T010 Draft the overlay coordinator public/package signature with `TransientSurfaceKind`, `OverlayState`, `OverlayMsg`, and `OverlayEffect` in `src/Controls/OverlayState.fsi`
- [X] T011 Implement the pure, total overlay coordinator reducer and stable stack ordering in `src/Controls/OverlayState.fs`
- [X] T012 Add overlay diagnostic codes for missing anchors, stale focus targets, blocked dismissals, disabled triggers, no-fit placements, and duplicate dispatch prevention in `src/Controls/Diagnostics.fsi` and `src/Controls/Diagnostics.fs`
- [X] T013 Extend the runtime bridge to carry overlay model/effects without taking ownership of product state in `src/Controls/ControlRuntime.fsi` and `src/Controls/ControlRuntime.fs`
- [X] T014 [P] Create reusable Feature143 surface, anchor, focus, and replay fixtures in `tests/Controls.Tests/Feature143OverlayFixtures.fs`
- [X] T015 Run `dotnet restore FS.GG.Rendering.slnx` and `dotnet build FS.GG.Rendering.slnx`, then record the foundation result in `specs/143-interaction-overlay-state/readiness/foundation-build.md`

**Checkpoint**: Overlay coordinator contract, diagnostics, fixtures, and build wiring are ready for user-story work.

---

## Phase 3: User Story 1 - Open and Dismiss Anchored Transient Surfaces (Priority: P1) MVP

**Goal**: Supported transient surfaces open from enabled triggers, anchor near their trigger, paint and hit-test above normal content, dismiss topmost-first, and preserve closed-state compatibility.

**Independent Test**: Run scripted pointer and keyboard activation against all eight surface categories and verify open state, anchor evidence, topmost hit priority, dismissal behavior, and closed-state output.

### Tests for User Story 1

- [X] T016 [P] [US1] Add failing open/dismiss coverage for menu, context menu, split-button menu, combo dropdown, auto-complete, date-picker, color palette, and modal trigger surfaces in `tests/Controls.Tests/Feature143InteractionOverlayStateTests.fs`
- [X] T017 [P] [US1] Add failing topmost layer and hit-test priority coverage for open overlays above in-flow content in `tests/Controls.Tests/Feature143OverlayHitTestTests.fs`
- [X] T018 [P] [US1] Add failing closed-state compatibility coverage for supported transient controls in `tests/Controls.Tests/Feature143ClosedStateCompatibilityTests.fs`

### Implementation for User Story 1

- [X] T019 [US1] Implement open, dismiss, anchor-changed, anchor-removed, and topmost-only dismissal transitions in `src/Controls/OverlayState.fs`
- [ ] T020 [US1] Add transient surface metadata and product-owned visibility dispatch mapping in `src/Controls/Control.fsi` and `src/Controls/Control.fs`
- [ ] T021 [US1] Add layer and portal anchor evidence helpers that reuse Feature140 ordering in `src/Controls/Composition.fsi` and `src/Controls/Composition.fs`
- [ ] T022 [US1] Route topmost hit decisions and outside pointer dismissal through overlay state in `src/Controls/Pointer.fsi` and `src/Controls/Pointer.fs`
- [ ] T023 [US1] Add menu, context-menu, and split-button transient metadata in `src/Controls/Widgets/Buttons.fsi` and `src/Controls/Widgets/Buttons.fs`
- [ ] T024 [P] [US1] Add combo dropdown and auto-complete transient metadata in `src/Controls/DataEntry2.fsi` and `src/Controls/DataEntry2.fs`
- [ ] T025 [P] [US1] Add date-picker calendar and color-picker palette transient metadata in `src/Controls/Interactive2.fsi` and `src/Controls/Interactive2.fs`
- [X] T026 [US1] Record open/dismiss, anchor, topmost hit, and closed-state evidence in `specs/143-interaction-overlay-state/readiness/us1-open-dismiss.md`

**Checkpoint**: User Story 1 is independently usable as the MVP transient-surface open/dismiss slice.

---

## Phase 4: User Story 2 - Operate Open Surfaces With Keyboard and Focus (Priority: P1)

**Goal**: Open surfaces can be operated by keyboard, keep focus inside the active surface while appropriate, recover focus after dismissal, and dispatch each selection or command exactly once.

**Independent Test**: Exercise keyboard scripts for opening, traversing, selecting, cancelling, and reopening representative surfaces; verify focus scope, focus recovery, and product dispatch counts.

### Tests for User Story 2

- [X] T027 [P] [US2] Add failing keyboard traversal, focus entry, focus recovery, and stale-target tests in `tests/Controls.Tests/Feature143OverlayFocusTests.fs`
- [X] T028 [P] [US2] Add failing overlay key-normalization and traversal-key evidence tests in `tests/KeyboardInput.Tests/Feature143OverlayKeyboardTests.fs`
- [X] T029 [P] [US2] Add failing exactly-once product dispatch tests for overlay selections and commands in `tests/Elmish.Tests/Feature143OverlayDispatchTests.fs`

### Implementation for User Story 2

- [ ] T030 [US2] Add overlay `FocusScope`, initial-focus, traversal, and recovery helpers in `src/Controls/Focus.fsi` and `src/Controls/Focus.fs`
- [X] T031 [US2] Implement `KeyRouted`, `SelectionCompleted`, and `FocusTargetRemoved` overlay transitions in `src/Controls/OverlayState.fs`
- [ ] T032 [US2] Emit focus requests, state-change requests, and exactly-once product dispatch effects in `src/Controls/ControlRuntime.fsi` and `src/Controls/ControlRuntime.fs`
- [ ] T033 [US2] Interpret overlay focus and product-dispatch effects from keyboard routing in `src/Controls.Elmish/ControlsElmish.fsi` and `src/Controls.Elmish/ControlsElmish.fs`
- [ ] T034 [US2] Wire auto-complete keyboard selection and focus recovery metadata in `src/Controls/DataEntry2.fsi` and `src/Controls/DataEntry2.fs`
- [ ] T035 [US2] Wire calendar keyboard navigation, date confirmation, cancellation, and focus recovery metadata in `src/Controls/Interactive2.fsi` and `src/Controls/Interactive2.fs`
- [X] T036 [US2] Record keyboard navigation, focus recovery, stale-target, and duplicate-dispatch evidence in `specs/143-interaction-overlay-state/readiness/us2-keyboard-focus.md`

**Checkpoint**: User Story 2 is independently testable for keyboard and focus behavior on open surfaces.

---

## Phase 5: User Story 3 - Enforce Modal Focus and Dismissal Rules (Priority: P1)

**Goal**: Modal overlays trap keyboard traversal, block covered content, apply dismissal policy before lower routing, and recover focus correctly for nested surfaces.

**Independent Test**: Run modal and non-modal overlay scripts side by side and verify modal focus trapping, lower-layer blocking, nested dismissal recovery, and non-interactive tooltip/toast non-capture behavior.

### Tests for User Story 3

- [X] T037 [P] [US3] Add failing modal focus-trap, nested-surface, and non-interactive surface tests in `tests/Controls.Tests/Feature143OverlayModalTests.fs`
- [X] T038 [P] [US3] Add failing modal lower-layer pointer/key blocking and pass-through policy tests in `tests/Controls.Tests/Feature143OverlayModalHitTests.fs`

### Implementation for User Story 3

- [X] T039 [US3] Add modal flags, trap modes, parent surface identities, and nested dismissal semantics in `src/Controls/OverlayState.fsi` and `src/Controls/OverlayState.fs`
- [ ] T040 [US3] Implement modal focus cycling and parent-or-trigger focus recovery in `src/Controls/Focus.fsi` and `src/Controls/Focus.fs`
- [ ] T041 [US3] Implement modal pointer/key blocking and pass-through-after-dismissal policy in `src/Controls/Pointer.fsi` and `src/Controls/Pointer.fs`
- [ ] T042 [US3] Add dialog-like modal overlay metadata and dismissal policy wiring in `src/Controls/Widgets/Overlay.fsi` and `src/Controls/Widgets/Overlay.fs`
- [ ] T043 [US3] Emit blocked-dismissal and lower-layer-blocking diagnostics/effects in `src/Controls/ControlRuntime.fsi` and `src/Controls/ControlRuntime.fs`
- [X] T044 [US3] Record modal trap, lower-layer blocking, nested dismissal, and non-modal evidence in `specs/143-interaction-overlay-state/readiness/us3-modal-rules.md`

**Checkpoint**: User Story 3 is independently testable for modal and nested overlay policy.

---

## Phase 6: User Story 4 - Keep Overlay State Deterministic and Auditable (Priority: P1)

**Goal**: Equivalent scripts produce deterministic state logs, product dispatches, diagnostics, focus evidence, hit decisions, and direct/retained/cache parity evidence.

**Independent Test**: Replay representative overlay scripts across direct rendering, cold retained rendering, warm retained rendering, cache-enabled mode, and cache-disabled mode; compare replay logs and visual/hit evidence.

### Tests for User Story 4

- [X] T045 [P] [US4] Add failing byte-identical replay determinism tests for three repeated overlay scripts in `tests/Controls.Tests/Feature143OverlayReplayTests.fs`
- [X] T046 [P] [US4] Add failing direct, retained, cache-enabled, and cache-disabled overlay routing parity tests in `tests/Elmish.Tests/Feature143OverlayParityTests.fs`
- [ ] T047 [P] [US4] Add failing rendering harness tests for overlay visual order, hit order, and unsupported-host disclosure in `tests/Rendering.Harness.Tests/Feature143OverlayRenderingTests.fs`

### Implementation for User Story 4

- [X] T048 [US4] Add `InteractionReplayLog`, stable transition evidence, and deterministic log projection APIs in `src/Controls/OverlayState.fsi` and `src/Controls/OverlayState.fs`
- [ ] T049 [US4] Emit topmost hit, focus, and dismissal evidence from routing helpers in `src/Controls/Pointer.fsi`, `src/Controls/Pointer.fs`, `src/Controls/Focus.fsi`, and `src/Controls/Focus.fs`
- [ ] T050 [US4] Thread overlay state and evidence through direct, retained, cache-enabled, and cache-disabled host routing in `src/Controls.Elmish/ControlsElmish.fsi` and `src/Controls.Elmish/ControlsElmish.fs`
- [ ] T051 [US4] Preserve retained reuse and cache behavior for unchanged overlay state in `src/Controls/RetainedRender.fsi` and `src/Controls/RetainedRender.fs`
- [X] T052 [US4] Add no-fit placement, missing-anchor, stale-focus, blocked-dismissal, and duplicate-dispatch diagnostics in `src/Controls/Diagnostics.fsi` and `src/Controls/Diagnostics.fs`
- [X] T053 [US4] Add replay comparison helpers and fixture corpus coverage for at least 100 overlay-state scenes in `tests/Controls.Tests/Feature143OverlayReplayTests.fs`
- [X] T054 [US4] Record replay determinism and cache/direct/retained parity evidence in `specs/143-interaction-overlay-state/readiness/us4-determinism-audit.md`
- [X] T055 [US4] Record rendering harness visual proof or unsupported-host limitations in `specs/143-interaction-overlay-state/readiness/us4-rendering-harness.md`

**Checkpoint**: User Story 4 is independently testable for deterministic replay and parity evidence.

---

## Phase 7: User Story 5 - Demonstrate the Reference Date Picker Flow (Priority: P2)

**Goal**: AntShowcase demonstrates a complete date-picker open, navigate, select, dismiss, focus-recover, and no-stale-overlay flow with readiness evidence.

**Independent Test**: Run the reference date-picker script and verify initial closed state, open state, navigation, exactly-once date selection, dismissal, focus recovery, no stale overlay content, and evidence output.

### Tests for User Story 5

- [X] T056 [P] [US5] Add failing Controls reference date-picker flow tests in `tests/Controls.Tests/Feature143ReferenceDatePickerTests.fs`
- [X] T057 [P] [US5] Add failing AntShowcase date-picker flow tests in `samples/AntShowcase/AntShowcase.Tests/Feature143DatePickerFlowTests.fs`

### Implementation for User Story 5

- [ ] T058 [US5] Add date-picker scripted open, navigate, select, dismiss, and focus-recovery interactions in `samples/AntShowcase/AntShowcase.Core/Scripts.fs`
- [ ] T059 [US5] Update the AntShowcase date-picker page and demo state for the reference overlay flow in `samples/AntShowcase/AntShowcase.Core/Pages.fs` and `samples/AntShowcase/AntShowcase.Core/DemoState.fs`
- [ ] T060 [US5] Emit reference flow evidence from showcase core in `samples/AntShowcase/AntShowcase.Core/Evidence.fs`
- [ ] T061 [US5] Wire app-level evidence collection for the reference overlay flow in `samples/AntShowcase/AntShowcase.App/Evidence.fs`
- [X] T062 [US5] Update Feature143 package or local source references for showcase tests in `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`
- [X] T063 [US5] Record reference date-picker flow evidence in `specs/143-interaction-overlay-state/readiness/us5-reference-date-picker.md`
- [ ] T064 [US5] Document the reference date-picker validation command and evidence path in `samples/AntShowcase/README.md`

**Checkpoint**: User Story 5 demonstrates the end-to-end reference consumer flow.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, baselines, compatibility notes, validation logs, and scope review needed for readiness.

- [ ] T065 [P] Update controls authoring compatibility guidance for product-owned visibility and overlay messages in `src/Controls/README.md`
- [ ] T066 [P] Update host routing and overlay effect interpretation guidance in `src/Controls.Elmish/README.md`
- [ ] T067 [P] Update validation commands if final test names differ from the design draft in `specs/143-interaction-overlay-state/quickstart.md`
- [X] T068 Run `dotnet restore FS.GG.Rendering.slnx` and record the result in `specs/143-interaction-overlay-state/readiness/validation-log.md`
- [X] T069 Run `dotnet build FS.GG.Rendering.slnx` and record the result in `specs/143-interaction-overlay-state/readiness/validation-log.md`
- [X] T070 Run focused Feature143 Controls, Elmish, and KeyboardInput tests and record the result in `specs/143-interaction-overlay-state/readiness/validation-log.md`
- [X] T071 Run Feature140, Feature141, Feature142, and PublicSurface regression tests and record the result in `specs/143-interaction-overlay-state/readiness/validation-log.md`
- [X] T072 Run `dotnet fsi scripts/refresh-surface-baselines.fsx` and update intentional public surface deltas in `tests/surface-baselines/FS.GG.UI.Controls.txt` and `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt`
- [X] T073 [P] Record public surface impact, migration guidance, and versioning rationale in `specs/143-interaction-overlay-state/readiness/public-surface-and-versioning.md`
- [X] T074 [P] Record closed-state, pixel, golden, diagnostic, and interaction-log baseline deltas in `specs/143-interaction-overlay-state/readiness/baseline-disclosure-ledger.md`
- [X] T075 [P] Record scope exclusions for portable serialization, browser rendering, compositor promotion, damage-scissored presentation, intrinsic layout, text editing, selection editing, and widget-catalog redesign in `specs/143-interaction-overlay-state/readiness/scope-review.md`
- [X] T076 Run rendering harness and AntShowcase validation when host support is available, then record proof or limitations in `specs/143-interaction-overlay-state/readiness/validation-log.md`
- [X] T077 Complete the readiness index with links to all evidence records in `specs/143-interaction-overlay-state/readiness/feature143-readiness.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user-story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational; MVP open/dismiss slice.
- **User Story 2 (Phase 4)**: Depends on Foundational; can proceed alongside US1 with coordination on shared `OverlayState`, `ControlRuntime`, and widget files.
- **User Story 3 (Phase 5)**: Depends on Foundational; can proceed alongside US1/US2 with coordination on shared focus and pointer files.
- **User Story 4 (Phase 6)**: Tests and harness work can start after Foundational; final completion depends on the behavior implemented by US1, US2, and US3.
- **User Story 5 (Phase 7)**: Depends on US1, US2, US3, and US4 because it proves the full reference date-picker flow.
- **Polish (Phase 8)**: Depends on all desired user stories for the release scope.

### User Story Dependencies

- **US1 (P1)**: First MVP story after Foundational; no dependency on other user stories.
- **US2 (P1)**: No functional dependency on US1, but final keyboard selection behavior should be validated against US1 open/dismiss surfaces.
- **US3 (P1)**: No functional dependency on US1/US2, but final modal policy must be validated with nested/open surfaces.
- **US4 (P1)**: Audits deterministic behavior for US1-US3; complete after those stories are behaviorally green.
- **US5 (P2)**: Reference consumer story; complete after US1-US4.

### Within Each User Story

- Write failing tests first.
- Update `.fsi` signatures before `.fs` implementation.
- Implement pure model/update behavior before host interpretation.
- Add runtime/host routing before widget-specific integration.
- Record readiness evidence before closing the story.

---

## Parallel Opportunities

- Setup tasks T002-T007 can run in parallel after T001 is understood.
- Foundational tests and transcript tasks T008, T009, and T014 can run in parallel.
- Within US1, test tasks T016-T018 can run in parallel, and metadata tasks T024-T025 can run in parallel after the core Control/Pointer contract is known.
- Within US2, test tasks T027-T029 can run in parallel.
- Within US3, test tasks T037-T038 can run in parallel.
- Within US4, test tasks T045-T047 can run in parallel.
- Within US5, test tasks T056-T057 can run in parallel.
- Polish documentation and readiness tasks T065-T067 and T073-T075 can run in parallel.

---

## Parallel Example: User Story 1

```bash
Task: "T016 [P] [US1] Add failing open/dismiss coverage in tests/Controls.Tests/Feature143InteractionOverlayStateTests.fs"
Task: "T017 [P] [US1] Add failing topmost layer and hit-test priority coverage in tests/Controls.Tests/Feature143OverlayHitTestTests.fs"
Task: "T018 [P] [US1] Add failing closed-state compatibility coverage in tests/Controls.Tests/Feature143ClosedStateCompatibilityTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "T027 [P] [US2] Add failing keyboard traversal and focus tests in tests/Controls.Tests/Feature143OverlayFocusTests.fs"
Task: "T028 [P] [US2] Add failing overlay key-normalization tests in tests/KeyboardInput.Tests/Feature143OverlayKeyboardTests.fs"
Task: "T029 [P] [US2] Add failing exactly-once dispatch tests in tests/Elmish.Tests/Feature143OverlayDispatchTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "T037 [P] [US3] Add failing modal focus and nested-surface tests in tests/Controls.Tests/Feature143OverlayModalTests.fs"
Task: "T038 [P] [US3] Add failing modal blocking tests in tests/Controls.Tests/Feature143OverlayModalHitTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "T045 [P] [US4] Add failing replay determinism tests in tests/Controls.Tests/Feature143OverlayReplayTests.fs"
Task: "T046 [P] [US4] Add failing routing parity tests in tests/Elmish.Tests/Feature143OverlayParityTests.fs"
Task: "T047 [P] [US4] Add failing rendering harness tests in tests/Rendering.Harness.Tests/Feature143OverlayRenderingTests.fs"
```

## Parallel Example: User Story 5

```bash
Task: "T056 [P] [US5] Add failing Controls reference date-picker tests in tests/Controls.Tests/Feature143ReferenceDatePickerTests.fs"
Task: "T057 [P] [US5] Add failing AntShowcase date-picker flow tests in samples/AntShowcase/AntShowcase.Tests/Feature143DatePickerFlowTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational overlay coordinator contract.
3. Complete Phase 3: User Story 1.
4. Validate US1 with `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature143`.
5. Stop and review the MVP before adding keyboard, modal, determinism, or showcase scope.

### Incremental Delivery

1. Deliver Setup + Foundational.
2. Deliver US1 open/dismiss behavior and validate independently.
3. Deliver US2 keyboard/focus behavior and validate independently.
4. Deliver US3 modal policy behavior and validate independently.
5. Deliver US4 replay/parity evidence and validate independently.
6. Deliver US5 reference date-picker flow.
7. Complete Polish readiness records and full quickstart validation.

### Team Parallel Strategy

1. One developer owns `OverlayState` and `ControlRuntime` contract coherence through Phase 2.
2. After Phase 2, separate developers can work on US1 tests/metadata, US2 keyboard/focus, US3 modal policy, and US4 harness/replay scaffolding.
3. Shared files (`src/Controls/OverlayState.fs`, `src/Controls/ControlRuntime.fs`, `src/Controls/Pointer.fs`, `src/Controls/Focus.fs`, and `src/Controls.Elmish/ControlsElmish.fs`) require short-lived integration branches or explicit handoff points.
4. US5 starts after the core behavior is stable enough to demonstrate in AntShowcase.

---

## Notes

- `[P]` tasks touch different files or are safe to run in parallel once their phase prerequisites are met.
- `[US1]` through `[US5]` labels map directly to the five user stories in `spec.md`.
- Every public or package-visible contract change must be designed in `.fsi`, tested semantically, and reflected in surface baselines or readiness evidence.
- Verification limitations and pre-existing failures must be recorded as limitations, not treated as successful Feature143 evidence.
