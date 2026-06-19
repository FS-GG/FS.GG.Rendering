# Tasks: Structured Render/Layout Inspection Metadata

**Input**: Design documents from `/specs/165-render-layout-inspection/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`

**Tests**: Tests are included because the feature specification marks User Scenarios & Testing as mandatory and the constitution requires failing-first semantic tests for Tier 1 public contract changes. Any hardcoded defect corpus, fixture, fake, or canned inspection data used because real render evidence is not yet available MUST carry `Synthetic` in the test name and include a comment at the use site explaining the synthetic fact and reason.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently after the shared public-surface foundation is in place.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches a different file or independent evidence path
- **[Story]**: Maps the task to the user story from `spec.md`
- Every task names the exact file path or paths it changes or writes

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare feature evidence locations and implementation notes without changing package behavior.

- [X] T001 Create inspection readiness directory placeholders in `specs/165-render-layout-inspection/readiness/inspection/.gitkeep`, `specs/165-render-layout-inspection/readiness/inspection/artifacts/.gitkeep`, and `specs/165-render-layout-inspection/readiness/inspection/findings/.gitkeep`
- [X] T002 [P] Create command-output evidence placeholder in `specs/165-render-layout-inspection/readiness/commands/.gitkeep`
- [X] T003 [P] Create implementation notes capturing package boundaries, unsupported-fact policy, and compatibility assumptions in `specs/165-render-layout-inspection/readiness/implementation-notes.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Draft and exercise the public package surfaces and compile-order hooks required before any user story implementation.

**Critical**: No user story implementation should begin until the intended `.fsi` surfaces, API-shape transcript, and test compile entries exist.

- [X] T004 Draft dependency-light visual inspection public types and `VisualInspection` module signatures in `src/Scene/Scene.fsi`
- [X] T005 Draft the Controls `ControlInspection` public module signature in `src/Controls/Inspection.fsi`
- [X] T006 Draft visual inspection validation, readiness, exception, summary, and Markdown public types/modules in `src/Testing/Testing.fsi`
- [X] T007 Exercise the intended Scene, Controls, and Testing public inspection APIs through F# Interactive or an equivalent prelude transcript before `.fs` implementation, and record the result in `specs/165-render-layout-inspection/readiness/commands/fsi-api-shape.md`
- [X] T008 Add `Inspection.fsi` and `Inspection.fs` compile entries immediately after `Control.fs` in `src/Controls/Controls.fsproj`
- [X] T009 Add Feature165 test compile entries in `tests/Scene.Tests/Scene.Tests.fsproj`, `tests/Controls.Tests/Controls.Tests.fsproj`, and `tests/Testing.Tests/Testing.Tests.fsproj`

**Checkpoint**: Public API shape is visible through `.fsi` files, the API-shape exercise is recorded, Controls compile order is defined, and story test files can be added without more project-file edits.

---

## Phase 3: User Story 1 - Detect Text and Layout Defects Deterministically (Priority: P1) - MVP

**Goal**: Produce structured node, region, bounds, text, clip, and ordering facts, then validate seeded text and layout defects without screenshots.

**Independent Test**: Run Feature165 Scene, Controls, and Testing tests over a small defect corpus containing contained text, overflowing text, clipped text, overlapping regions, and correct regions; every seeded defect reports owner, visual region, and rule reason.

### Tests for User Story 1

- [X] T010 [P] [US1] Create failing Scene model tests for stable tokens, unique node/region ids, deterministic ordering, and explicit unsupported facts in `tests/Scene.Tests/Feature165VisualInspectionModelTests.fs`
- [X] T011 [P] [US1] Create failing Controls inspection tests for stable control ids, final bounds, ownership, visual ordering, text facts, and clip facts from `Control.renderTree` in `tests/Controls.Tests/Feature165ControlInspectionLayoutTests.fs`
- [X] T012 [P] [US1] Create failing Testing validation tests for `required-region-present`, `ordinary-regions-disjoint`, `text-contained-in-owner`, `clip-intent-classified`, `identity-stable`, and `visual-order-stable` in `tests/Testing.Tests/Feature165VisualInspectionValidationTests.fs`

### Implementation for User Story 1

- [X] T013 [US1] Implement visual inspection status, severity, measurement, fit, node-kind, paint-role, and surface-role token helpers in `src/Scene/Scene.fs`
- [X] T014 [US1] Implement `VisualInspection` artifact helpers for node uniqueness, region uniqueness, deterministic ordering, unsupported facts, and stable finding ids in `src/Scene/Scene.fs`
- [X] T015 [US1] Implement `ControlInspection.inspect` core traversal over `Control.renderTree`, `ControlRenderResult.Bounds`, and layout tree ids in `src/Controls/Inspection.fs`
- [X] T016 [US1] Implement Controls text-run extraction and fit/clip classification using owner bounds and existing text measurement helpers in `src/Controls/Inspection.fs`
- [X] T017 [US1] Implement layout, text, clip, identity, and visual-order validation rules in `src/Testing/Testing.fs`
- [X] T018 [US1] Record focused US1 test command output in `specs/165-render-layout-inspection/readiness/commands/us1-focused-tests.md`

**Checkpoint**: User Story 1 is independently testable and can block obvious text overflow, clipping, overlap, containment, identity, and ordering defects.

---

## Phase 4: User Story 2 - Validate Paint Coverage and Intentional Exceptions (Priority: P1)

**Goal**: Distinguish complete intentional surfaces from missing paint, accidental clipping, unclassified overlap, and explicitly reviewed overlays or clipped regions.

**Independent Test**: Run validation over cases with a fully painted root, missing background, intentional overlay, unclassified overlap, intentional clipping, and accidental clipping; only explicitly owned and reasoned intentional cases pass.

### Tests for User Story 2

- [X] T019 [P] [US2] Create failing Scene tests for paint coverage facts, clip facts, required-region paint status, and explicit unsupported paint facts in `tests/Scene.Tests/Feature165VisualInspectionPaintTests.fs`
- [X] T020 [P] [US2] Create failing Controls tests for root surface coverage, section coverage, overlay roles, popup roles, scroll clipping, and unsupported transform facts in `tests/Controls.Tests/Feature165ControlInspectionPaintTests.fs`
- [X] T021 [P] [US2] Create failing Testing tests for `required-region-painted`, `overlay-overlap-classified`, valid exceptions, invalid exceptions, unused exceptions, and accidental clipping in `tests/Testing.Tests/Feature165VisualInspectionExceptionTests.fs`

### Implementation for User Story 2

- [X] T022 [US2] Implement Scene paint coverage and clip fact construction/classification helpers in `src/Scene/Scene.fs`
- [X] T023 [US2] Extend `ControlInspection.inspect` to emit root, section, content, overlay, popup, and floating surface paint coverage facts in `src/Controls/Inspection.fs`
- [X] T024 [US2] Extend `ControlInspection.inspect` to classify overlay overlap, popup overlap, scroll clipping, bounded clipping, comparable transformed bounds, and explicitly unsupported transformed bounds in `src/Controls/Inspection.fs`
- [X] T025 [US2] Implement `VisualInspectionException` matching, invalid-exception diagnostics, and unused-exception diagnostics in `src/Testing/Testing.fs`
- [X] T026 [US2] Implement paint coverage, overlay overlap, and intentional clipping validation with accepted, blocked, unsupported, and environment-limited readiness mapping in `src/Testing/Testing.fs`
- [X] T027 [US2] Record focused US2 test command output in `specs/165-render-layout-inspection/readiness/commands/us2-focused-tests.md`

**Checkpoint**: User Story 2 is independently testable and accepts intentional visual exceptions only when owner, affected ids, rule id, and reason are explicit.

---

## Phase 5: User Story 3 - Produce Reviewable Inspection Evidence (Priority: P2)

**Goal**: Produce machine-readable and reviewer-readable summaries that group inspected pages, variants, sizes, regions, findings, unsupported facts, and readiness status.

**Independent Test**: Generate inspection evidence for mixed passing and failing screens, then verify summary grouping, counts, finding links, unsupported facts, caveats, and reproducible context.

### Tests for User Story 3

- [X] T028 [P] [US3] Create failing Testing summary tests for grouping by scope, presentation, size, region, severity, rule, and readiness status in `tests/Testing.Tests/Feature165VisualInspectionSummaryTests.fs`
- [X] T029 [P] [US3] Create failing Testing artifact tests for required Markdown fields, required JSON fields, related visual evidence links, and safe managed-section regeneration in `tests/Testing.Tests/Feature165VisualInspectionArtifactTests.fs`

### Implementation for User Story 3

- [X] T030 [US3] Implement `VisualInspectionReadiness` aggregation for artifacts, validation results, status counts, finding counts, unsupported facts, and caveats in `src/Testing/Testing.fs`
- [X] T031 [US3] Implement `VisualInspectionMarkdown.renderSummary` with inspected scope tables, blocking finding tables, unsupported fact tables, exception tables, and caveats in `src/Testing/Testing.fs`
- [X] T032 [US3] Implement `VisualInspectionMarkdown.renderJson` with contract-required fields and deterministic ordering in `src/Testing/Testing.fs`
- [X] T033 [US3] Implement generated inspection managed-section insertion/update with safe failure on malformed markers in `src/Testing/Testing.fs`
- [X] T034 [US3] Generate representative inspection summary evidence in `specs/165-render-layout-inspection/readiness/inspection/summary.md` and `specs/165-render-layout-inspection/readiness/inspection/summary.json`
- [X] T035 [US3] Record focused US3 test command output in `specs/165-render-layout-inspection/readiness/commands/us3-focused-tests.md`

**Checkpoint**: User Story 3 is independently testable and reviewers can understand inspection scope and blocking findings without reading raw render data.

---

## Phase 6: User Story 4 - Adopt Inspection Incrementally Without Regressing Visual Output (Priority: P3)

**Goal**: Allow sample owners and generated products to adopt inspection partially while preserving screenshot readiness workflows and explicit unsupported/not-inspected statuses.

**Independent Test**: Enable inspection on a representative sample scope while leaving existing screenshot evidence behavior unchanged; unsupported and not-inspected scopes remain visible and are not counted as accepted evidence.

### Tests for User Story 4

- [X] T036 [P] [US4] Create failing Testing adoption tests for partial coverage, not-inspected scopes, not-run scopes, unsupported scopes, environment-limited scopes, and legacy `GeneratedLayoutValidation` compatibility in `tests/Testing.Tests/Feature165VisualInspectionAdoptionTests.fs`
- [X] T037 [P] [US4] Create failing Controls regression tests proving inspection does not change `Control.renderTree` scene output, bounds, diagnostics, event bindings, bound ids, or node count in `tests/Controls.Tests/Feature165ControlInspectionRegressionTests.fs`

### Implementation for User Story 4

- [X] T038 [US4] Implement partial inspection coverage handling that distinguishes inspected, not-inspected, and not-run coverage from accepted, blocked, unsupported, and environment-limited readiness in `src/Testing/Testing.fs`
- [X] T039 [US4] Implement optional related screenshot/visual-readiness evidence links without requiring screenshots for deterministic inspection validation in `src/Testing/Testing.fs`
- [X] T040 [US4] Add compatibility notes for changed `.fsi` files, changed surface baselines, `LayoutEvidenceReport`, `GeneratedLayoutValidation`, and screenshot workflow behavior in `specs/165-render-layout-inspection/readiness/inspection/compatibility.md`
- [X] T041 [US4] Add representative sample inspection artifact evidence in `specs/165-render-layout-inspection/readiness/inspection/artifacts/representative-sample.inspection.json`
- [X] T042 [US4] Record focused US4 test command output in `specs/165-render-layout-inspection/readiness/commands/us4-focused-tests.md`

**Checkpoint**: User Story 4 is independently testable and proves the inspection feature is additive to existing visual-readiness and legacy layout-evidence workflows.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Finish public docs, surface baselines, package evidence, and repository gates.

- [X] T043 [P] Add final XML documentation comments for new public APIs in `src/Scene/Scene.fsi`, `src/Controls/Inspection.fsi`, and `src/Testing/Testing.fsi`
- [X] T044 [P] Update package README usage and compatibility notes in `src/Scene/README.md`, `src/Controls/README.md`, and `src/Testing/README.md`
- [X] T045 Update public surface baselines for changed packages in `readiness/surface-baselines/FS.GG.UI.Scene.txt`, `readiness/surface-baselines/FS.GG.UI.Controls.txt`, and `readiness/surface-baselines/FS.GG.UI.Testing.txt`
- [X] T046 Run `dotnet test tests/Scene.Tests/Scene.Tests.fsproj` and record output in `specs/165-render-layout-inspection/readiness/commands/scene-tests.md`
- [X] T047 Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj` and record output in `specs/165-render-layout-inspection/readiness/commands/controls-tests.md`
- [X] T048 Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj` and record output in `specs/165-render-layout-inspection/readiness/commands/testing-tests.md`
- [X] T049 Run `./fake.sh build -t CapabilityCheck`, `./fake.sh build -t PackageSurfaceCheck`, and `./fake.sh build -t PackLocal`, then record output in `specs/165-render-layout-inspection/readiness/commands/package-gates.md`
- [X] T050 Run `./fake.sh build -t GeneratedProductCheck` and record output in `specs/165-render-layout-inspection/readiness/commands/generated-product-check.md`
- [X] T051 Run the representative inspection validation command with elapsed wall-clock timing and record output in `specs/165-render-layout-inspection/readiness/commands/representative-inspection-timing.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational; suggested MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational; can proceed alongside US1 if work is coordinated, but reuses the shared Scene model and Testing validation surface.
- **User Story 3 (Phase 5)**: Depends on US1 and US2 validation outputs so summaries can cover both layout/text and paint/exception findings.
- **User Story 4 (Phase 6)**: Depends on US1 through US3 so adoption evidence can reference inspection artifacts, summaries, and compatibility behavior.
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: No dependency on other user stories after Foundation.
- **US2 (P1)**: No dependency on US3 or US4 after Foundation; shares core model assumptions with US1.
- **US3 (P2)**: Requires validation and artifact shapes from US1 and US2.
- **US4 (P3)**: Requires summary/status behavior from US3 and regression evidence from US1/US2.

### Within Each User Story

- Write failing tests first.
- Implement or extend Scene dependency-light model behavior before package-specific adapters.
- Implement Controls extraction before Testing validation rules that depend on extracted facts.
- Record focused test evidence before moving to the next checkpoint.

### Parallel Opportunities

- T002 and T003 can run in parallel with T001.
- US1 test tasks T010, T011, and T012 can run in parallel.
- US2 test tasks T019, T020, and T021 can run in parallel.
- US3 test tasks T028 and T029 can run in parallel.
- US4 test tasks T036 and T037 can run in parallel.
- Polish documentation tasks T043 and T044 can run in parallel.
- Full package tests T046, T047, and T048 can run in parallel after implementation compiles.
- T051 runs after the representative inspection command is documented and implementation compiles.

---

## Parallel Examples

### User Story 1

```bash
Task: "Create failing Scene model tests for stable tokens, unique node/region ids, deterministic ordering, and explicit unsupported facts in tests/Scene.Tests/Feature165VisualInspectionModelTests.fs"
Task: "Create failing Controls inspection tests for stable control ids, final bounds, ownership, visual ordering, text facts, and clip facts from Control.renderTree in tests/Controls.Tests/Feature165ControlInspectionLayoutTests.fs"
Task: "Create failing Testing validation tests for required-region-present, ordinary-regions-disjoint, text-contained-in-owner, clip-intent-classified, identity-stable, and visual-order-stable in tests/Testing.Tests/Feature165VisualInspectionValidationTests.fs"
```

### User Story 2

```bash
Task: "Create failing Scene tests for paint coverage facts, clip facts, required-region paint status, and explicit unsupported paint facts in tests/Scene.Tests/Feature165VisualInspectionPaintTests.fs"
Task: "Create failing Controls tests for root surface coverage, section coverage, overlay roles, popup roles, scroll clipping, and unsupported transform facts in tests/Controls.Tests/Feature165ControlInspectionPaintTests.fs"
Task: "Create failing Testing tests for required-region-painted, overlay-overlap-classified, valid exceptions, invalid exceptions, unused exceptions, and accidental clipping in tests/Testing.Tests/Feature165VisualInspectionExceptionTests.fs"
```

### User Story 3

```bash
Task: "Create failing Testing summary tests for grouping by scope, presentation, size, region, severity, rule, and readiness status in tests/Testing.Tests/Feature165VisualInspectionSummaryTests.fs"
Task: "Create failing Testing artifact tests for required Markdown fields, required JSON fields, related visual evidence links, and safe managed-section regeneration in tests/Testing.Tests/Feature165VisualInspectionArtifactTests.fs"
```

### User Story 4

```bash
Task: "Create failing Testing adoption tests for partial coverage, not-inspected scopes, not-run scopes, unsupported scopes, environment-limited scopes, and legacy GeneratedLayoutValidation compatibility in tests/Testing.Tests/Feature165VisualInspectionAdoptionTests.fs"
Task: "Create failing Controls regression tests proving inspection does not change Control.renderTree scene output, bounds, diagnostics, event bindings, bound ids, or node count in tests/Controls.Tests/Feature165ControlInspectionRegressionTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 for US1.
3. Run and record the US1 focused tests in `specs/165-render-layout-inspection/readiness/commands/us1-focused-tests.md`.
4. Stop and validate that structured inspection can catch text overflow, accidental clipping, ordinary overlap, missing region facts, identity churn, and visual-order churn.

### Incremental Delivery

1. Add US1 for deterministic text/layout defect detection.
2. Add US2 for paint coverage, overlay overlap, and intentional exception handling.
3. Add US3 for reviewer-readable and machine-readable evidence.
4. Add US4 for partial adoption, generated-product compatibility, and legacy visual-readiness preservation.
5. Finish Phase 7 gates and readiness artifacts.

### Parallel Team Strategy

1. Complete Setup and Foundation together.
2. Split US1 and US2 test authoring by package once `.fsi` surfaces exist.
3. Implement Scene model helpers first, then split Controls extraction and Testing validation work.
4. Start US3 summary work after the validation result shape stabilizes.
5. Start US4 compatibility work after summary statuses and artifact paths stabilize.

---

## Notes

- Tests are mandatory for this feature because `spec.md` and the constitution require them.
- Public surface changes must be declared in `.fsi` files before `.fs` bodies.
- Intended public APIs must be exercised through F# Interactive or an equivalent prelude transcript before `.fs` body implementation.
- `src/Scene` must remain dependency-light and must not reference Controls, Layout, Testing, SkiaViewer, SkiaSharp, Yoga.Net, Elmish, or KeyboardInput.
- `src/Controls` must not depend on Testing.
- `src/Testing` must not depend on Controls or Layout.
- Unsupported or unavailable inspection facts must be explicit and cannot be counted as accepted deterministic evidence.
- Hardcoded or fake inspection fixtures must follow the constitution's `Synthetic` naming and disclosure rule.
- Existing `LayoutEvidenceReport`, `GeneratedLayoutValidation`, and screenshot visual-readiness workflows remain additive compatibility paths unless a deliberate change is documented.
