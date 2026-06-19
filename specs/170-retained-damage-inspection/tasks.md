# Tasks: Retained Render Damage Inspection

**Input**: Design documents from `/specs/170-retained-damage-inspection/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`

**Tests**: Included. The feature specification marks user scenarios and testing as mandatory, and the constitution requires semantic test evidence for Tier 1 public-surface changes.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested independently after the shared contract foundation is in place.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and does not depend on incomplete tasks.
- **[Story]**: Maps the task to the user story phase only.
- Every task includes an exact file or directory path.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the feature evidence locations without changing package behavior.

- [X] T001 Create retained-inspection readiness directories at `specs/170-retained-damage-inspection/readiness/retained-inspection/artifacts/` and `specs/170-retained-damage-inspection/readiness/retained-inspection/findings/`
- [X] T002 [P] Create the compatibility artifact scaffold in `specs/170-retained-damage-inspection/readiness/retained-inspection/compatibility.md`
- [X] T003 [P] Create the validation log scaffold in `specs/170-retained-damage-inspection/readiness/retained-inspection/validation-log.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the additive public contract skeletons, FSI-facing semantic tests, and surface baselines required before any story implementation.

**Critical**: No user story implementation should begin until the intended public surfaces, semantic tests, surface baselines, and fail-closed bodies are in place. FSI signatures and API-shape tests come before `.fs` bodies to preserve the constitution-required Spec -> FSI -> semantic tests -> implementation order.

- [X] T004 Add retained/damage evidence signatures for `RetainedInspectionStatus`, `RetainedNodeStatus`, `DamageInspectionStatus`, `RetainedFrameTransition`, `RetainedNodeInspection`, `DamageRegionInspection`, `DamageLocalityFinding`, `IntentionalDamageException`, `RetainedInspectionArtifact`, `RetainedInspectionSummary`, and `module RetainedInspection` in `src/Scene/Scene.fsi`
- [X] T005 Add `RetainedControlInspectionRequest<'msg>`, `RetainedControlTransition<'msg>`, and retained inspection entry-point signatures to `src/Controls/Inspection.fsi`
- [X] T006 Add `RetainedInspectionRule`, `RetainedInspectionValidationCheck`, `RetainedInspectionValidationResult`, `RetainedInspectionSummarySectionUpdate`, `module RetainedInspectionValidation`, `module RetainedInspectionReadiness`, and `module RetainedInspectionMarkdown` signatures in `src/Testing/Testing.fsi`
- [X] T007 Add failing FSI/API-shape semantic tests for the retained/damage signatures and canonical usage flow in `tests/Package.Tests/Feature170RetainedInspectionSurfaceTests.fs`
- [X] T008 Register Feature170 retained inspection surface tests in `tests/Package.Tests/Package.Tests.fsproj`
- [X] T009 Update additive public surface baselines for retained inspection contracts in `tests/surface-baselines/FS.GG.UI.Scene.txt`, `tests/surface-baselines/FS.GG.UI.Controls.txt`, and `tests/surface-baselines/FS.GG.UI.Testing.txt`
- [X] T010 Add matching fail-closed retained/damage type bodies, status token helpers, artifact diagnostics, and normalization stubs in `src/Scene/Scene.fs`
- [X] T011 Add matching fail-closed Controls retained inspection adapter bodies that return explicit unsupported/not-inspected evidence in `src/Controls/Inspection.fs`
- [X] T012 Add matching fail-closed retained validation, readiness aggregation, Markdown, JSON, and managed-section bodies in `src/Testing/Testing.fs`

**Checkpoint**: Additive `.fsi` surfaces exist, FSI/API-shape tests and surface baselines describe the public contract, implementation bodies compile or fail closed, and story tests can be written against stable names.

---

## Phase 3: User Story 1 - Inspect Final Retained Output (Priority: P1) - MVP

**Goal**: A maintainer can inspect a retained-render transition and see stable retained, reused, repainted, shifted, added, removed, unaffected, and unsupported node facts.

**Independent Test**: Run a retained-render inspection fixture with unchanged nodes, changed nodes, shifted nodes, added/removed nodes, and unsupported damage facts; verify stable identities, statuses, bounds, and affected region context.

### Tests for User Story 1

Write these tests first and verify they fail before implementation.

- [X] T013 [P] [US1] Add retained transition classification tests for reused, repainted, shifted, added, removed, unaffected, and unsupported nodes in `tests/Controls.Tests/Feature170RetainedInspectionTests.fs`
- [X] T014 [P] [US1] Add retained artifact normalization, stable identity, first-frame/no-prior, and unsupported-fact tests in `tests/Testing.Tests/Feature170RetainedInspectionArtifactTests.fs`
- [X] T015 [US1] Register User Story 1 Feature170 test files in `tests/Controls.Tests/Controls.Tests.fsproj` and `tests/Testing.Tests/Testing.Tests.fsproj`

### Implementation for User Story 1

- [X] T016 [US1] Implement retained status text tokens, unsupported fact helpers, deterministic sorting, and retained artifact diagnostics in `src/Scene/Scene.fs`
- [X] T017 [US1] Implement retained first-frame and transition artifact construction over the real `RetainedRender.init` and `RetainedRender.step` path in `src/Controls/Inspection.fs`
- [X] T018 [US1] Extract retained node facts with stable public node ids, owner ids, opaque retained identity tokens, status, and affected region ids in `src/Controls/Inspection.fs`
- [X] T019 [US1] Preserve prior/current bounds for shifted, added, removed, and shifted-and-repainted nodes and emit unsupported node diagnostics in `src/Controls/Inspection.fs`
- [X] T020 [US1] Implement retained artifact Markdown and JSON rendering for node status counts, unsupported facts, and related visual evidence in `src/Testing/Testing.fs`

**Checkpoint**: User Story 1 is independently functional when `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter Feature170RetainedInspection` and `dotnet test tests/Testing.Tests/Testing.Tests.fsproj -c Release --filter Feature170RetainedInspectionArtifact` pass.

---

## Phase 4: User Story 2 - Validate Damage Locality (Priority: P1)

**Goal**: A test author can validate localized damage without pixel comparison and can see broad/full-surface dirty regions, shifted nodes, repainted nodes, and unaffected regions separately.

**Independent Test**: Use fixtures with localized damage, full-surface damage, overlapping rectangles, empty damage, shifted layout, and intentional exceptions; verify validation distinguishes accepted, blocked, review-required, unsupported, and not-inspected states.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T021 [P] [US2] Add dirty-region true-union, empty-damage, broad-damage over maximum dirty percentage, full-surface, and visible-percentage tests in `tests/Controls.Tests/Feature170DamageRegionUnionTests.fs`
- [X] T022 [P] [US2] Add retained damage locality validation tests for localized scope, dirty regions outside expected regions, maximum dirty percentage, full-surface blockers, shifted/repainted separation, unsupported/not-inspected facts, stable findings, and intentional exceptions in `tests/Testing.Tests/Feature170DamageLocalityValidationTests.fs`
- [X] T023 [US2] Register User Story 2 Feature170 test files in `tests/Controls.Tests/Controls.Tests.fsproj` and `tests/Testing.Tests/Testing.Tests.fsproj`

### Implementation for User Story 2

- [X] T024 [US2] Implement clipped true-union dirty area, union bounds, visible dirty area, and dirty percentage calculations for `DamageRegionInspection` in `src/Scene/Scene.fs`
- [X] T025 [US2] Map `RetainedRender` dirty rectangles, `WorkReductionRecord` repaint/shift/unaffected counts, and affected node/region ids into `DamageRegionInspection` in `src/Controls/Inspection.fs`
- [X] T026 [US2] Implement retained damage validation rules from `contracts/damage-locality-validation.md`, including dirty regions outside expected regions and scenario-specific maximum dirty percentage, in `src/Testing/Testing.fs`
- [X] T027 [US2] Implement intentional damage exception matching, invalid exception diagnostics, unused exception diagnostics, and accepted-by-exception findings in `src/Testing/Testing.fs`
- [X] T028 [US2] Implement retained inspection readiness aggregation with accepted, blocked, review-required, unsupported, environment-limited, not-inspected, and not-run outcomes in `src/Testing/Testing.fs`
- [X] T029 [US2] Implement retained inspection summary Markdown, summary JSON, reviewer-summary field coverage for dirty area percentage, repainted count, shifted count, and affected regions, and managed marker updates using `<!-- FS.GG RETAINED INSPECTION START -->` and `<!-- FS.GG RETAINED INSPECTION END -->` in `src/Testing/Testing.fs`

**Checkpoint**: User Story 2 is independently functional when `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter Feature170DamageRegionUnion` and `dotnet test tests/Testing.Tests/Testing.Tests.fsproj -c Release --filter Feature170DamageLocalityValidation` pass.

---

## Phase 5: User Story 3 - Move a Visual-Shell Assertion to Structured Evidence (Priority: P2)

**Goal**: AntShowcase uses structured retained inspection evidence for the representative `charts-statistical` full-shell assertion while preserving screenshot readiness counts.

**Independent Test**: Run the selected AntShowcase visual-shell assertion and screenshot readiness parity checks; verify the structured evidence covers preferred size in light and dark themes and screenshot target counts remain 38 preferred and 12 minimum.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T030 [US3] Add AntShowcase structured retained inspection adoption tests for `charts-statistical` preferred light/dark shell evidence and screenshot count preservation in `samples/AntShowcase/AntShowcase.Tests/Feature170VisualInspectionAdoptionTests.fs`
- [X] T031 [US3] Register the User Story 3 Feature170 test file in `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`

### Implementation for User Story 3

- [X] T032 [US3] Add AntShowcase retained inspection evidence record signatures and serialization function signatures in `samples/AntShowcase/AntShowcase.Core/Evidence.fsi`
- [X] T033 [US3] Implement AntShowcase retained inspection evidence serialization and reviewer Markdown helpers in `samples/AntShowcase/AntShowcase.Core/Evidence.fs`
- [X] T034 [US3] Replace the `theme and current page affordances render in full shell` assertion with structured retained inspection evidence checks in `samples/AntShowcase/AntShowcase.Tests/VisualShellTests.fs`
- [X] T035 [US3] Record sample adoption evidence, selected page/theme/size, screenshot count parity, and reviewer value in `specs/170-retained-damage-inspection/readiness/retained-inspection/antshowcase-adoption.md`

**Checkpoint**: User Story 3 is independently functional when `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --filter Feature170VisualInspectionAdoption` passes and the existing `VisualReadiness` filter still reports 38 preferred and 12 minimum targets.

---

## Phase 6: User Story 4 - Run Canonical Validation (Priority: P2)

**Goal**: Contributors can run a documented `retained-inspection` validation lane without relying on a stale wrapper command.

**Independent Test**: Run the lane list/preflight tests and the documented lane command; verify retained inspection, damage locality, harness, and sample adoption checks execute and produce command/status/artifact evidence.

### Tests for User Story 4

Write these tests first and verify they fail before implementation.

- [X] T036 [P] [US4] Add retained-inspection lane catalog, command, artifact path, elapsed-time threshold status, fail-closed status, and unknown-lane diagnostic tests in `tests/Rendering.Harness.Tests/Feature170RetainedInspectionLaneTests.fs`
- [X] T037 [US4] Register the User Story 4 Feature170 harness test file in `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`

### Implementation for User Story 4

- [X] T038 [US4] Add the `retained-inspection` lane definition with focused Controls, Testing, Rendering.Harness, and AntShowcase Feature170 commands in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T039 [US4] Ensure explicit lane selection fails closed for failed, timed-out, no-progress-timeout, canceled, skipped, not-run, environment-limited, and infrastructure-error retained-inspection results in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T040 [US4] Update lane runner list and preflight diagnostics so the canonical command exposes missing prerequisites and unknown lane ids clearly in `scripts/run-validation-lanes.fsx`
- [X] T041 [US4] Document the retained-inspection lane as the maintained validation entry point and state whether it is optional or required in `docs/validation/validation-set.md`
- [X] T042 [US4] Record the canonical command, expected output paths, prerequisite failure behavior, and artifact review notes in `specs/170-retained-damage-inspection/readiness/retained-inspection/validation-log.md`

**Checkpoint**: User Story 4 is independently functional when `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --filter Feature170RetainedInspectionLane` passes and `dotnet fsi scripts/run-validation-lanes.fsx --lane retained-inspection --out specs/170-retained-damage-inspection/readiness/lanes` writes lane evidence.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Surface baselines, compatibility notes, readiness evidence, and final validation.

- [X] T043 [P] Verify additive public surface baselines from T009 remain synchronized after implementation in `tests/surface-baselines/FS.GG.UI.Scene.txt`, `tests/surface-baselines/FS.GG.UI.Controls.txt`, and `tests/surface-baselines/FS.GG.UI.Testing.txt`
- [X] T044 [P] Add Feature170 compatibility tests for additive `VisualInspectionArtifact` behavior, `CompositorDamageReadiness` preservation, and screenshot readiness preservation in `tests/Package.Tests/Feature170CompatibilityTests.fs`
- [X] T045 Register Feature170 package compatibility tests in `tests/Package.Tests/Package.Tests.fsproj`
- [X] T046 Update compatibility notes with changed `.fsi` files, changed baselines, migration impact, `VisualInspectionArtifact` compatibility, `CompositorDamageReadiness` compatibility, and screenshot readiness count status in `specs/170-retained-damage-inspection/readiness/retained-inspection/compatibility.md`
- [X] T047 Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter Feature170` and record command, status, elapsed time, and artifact paths in `specs/170-retained-damage-inspection/readiness/retained-inspection/validation-log.md`
- [X] T048 Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj -c Release --filter Feature170` and record command, status, elapsed time, and artifact paths in `specs/170-retained-damage-inspection/readiness/retained-inspection/validation-log.md`
- [X] T049 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --filter Feature170` and record command, status, elapsed time, and artifact paths in `specs/170-retained-damage-inspection/readiness/retained-inspection/validation-log.md`
- [X] T050 Run `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --filter Feature170` and `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --filter VisualReadiness`, then record structured adoption and screenshot count parity in `specs/170-retained-damage-inspection/readiness/retained-inspection/antshowcase-adoption.md`
- [X] T051 Run `dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release --filter Surface` and record additive surface-baseline evidence in `specs/170-retained-damage-inspection/readiness/retained-inspection/compatibility.md`
- [X] T052 Run `dotnet fsi scripts/run-validation-lanes.fsx --lane retained-inspection --out specs/170-retained-damage-inspection/readiness/lanes` and record lane summary, result JSON, diagnostics, log, retained artifact paths, elapsed time, and pass/review-required status against the under-5-minute target in `specs/170-retained-damage-inspection/readiness/retained-inspection/summary.md`
- [X] T053 Record reviewer-summary walkthrough evidence showing dirty area percentage, repainted node count, shifted node count, and affected visual regions can be identified within the under-2-minute target in `specs/170-retained-damage-inspection/readiness/retained-inspection/summary.md`
- [X] T054 Publish final retained inspection machine-readable summary and retained artifact references in `specs/170-retained-damage-inspection/readiness/retained-inspection/summary.json`
- [X] T055 Publish any blocking or review-required retained damage findings in `specs/170-retained-damage-inspection/readiness/retained-inspection/findings/blocking-findings.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on setup and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on foundational contracts and is the MVP.
- **User Story 2 (Phase 4)**: Depends on foundational contracts. It can proceed in parallel with User Story 1 after Phase 2, but validation will likely consume retained artifact helpers from User Story 1.
- **User Story 3 (Phase 5)**: Depends on foundational contracts and is easiest after User Stories 1 and 2 expose usable retained/damage summaries.
- **User Story 4 (Phase 6)**: Depends on the focused test commands from User Stories 1, 2, and 3.
- **Polish (Final Phase)**: Depends on all desired user stories.

### User Story Dependencies

- **US1 Inspect Final Retained Output (P1)**: Can start after Phase 2. Provides the MVP and reusable retained artifact model.
- **US2 Validate Damage Locality (P1)**: Can start after Phase 2. Uses the same retained/damage contract and can be validated independently with fixtures.
- **US3 Move Visual-Shell Assertion to Structured Evidence (P2)**: Can start after retained inspection and damage summary helpers exist.
- **US4 Run Canonical Validation (P2)**: Can start after the focused commands and sample adoption tests exist.

### Within Each User Story

- Tests come first and should fail before implementation.
- Public `.fsi` contracts, FSI/API-shape tests, and surface baselines come before `.fs` implementation changes.
- Retained/damage model helpers come before Controls adapters.
- Controls adapters come before Testing validation that consumes emitted artifacts.
- Focused tests pass before readiness and lane evidence is recorded.

### Parallel Opportunities

- T002 and T003 can run in parallel after T001.
- T013 and T014 can run in parallel, then T015 registers both files.
- T021 and T022 can run in parallel, then T023 registers both files.
- T036 can run while User Story 3 implementation proceeds, but T038 should wait until focused Feature170 commands are known.
- T043 and T044 can run in parallel during polish.

---

## Parallel Example: User Story 1

```bash
Task: "Add retained transition classification tests for reused, repainted, shifted, added, removed, unaffected, and unsupported nodes in tests/Controls.Tests/Feature170RetainedInspectionTests.fs"
Task: "Add retained artifact normalization, stable identity, first-frame/no-prior, and unsupported-fact tests in tests/Testing.Tests/Feature170RetainedInspectionArtifactTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "Add dirty-region true-union, empty-damage, broad-damage over maximum dirty percentage, full-surface, and visible-percentage tests in tests/Controls.Tests/Feature170DamageRegionUnionTests.fs"
Task: "Add retained damage locality validation tests for localized scope, dirty regions outside expected regions, maximum dirty percentage, full-surface blockers, shifted/repainted separation, unsupported/not-inspected facts, stable findings, and intentional exceptions in tests/Testing.Tests/Feature170DamageLocalityValidationTests.fs"
```

## Parallel Example: Polish

```bash
Task: "Verify additive public surface baselines from T009 remain synchronized after implementation in tests/surface-baselines/FS.GG.UI.Scene.txt, tests/surface-baselines/FS.GG.UI.Controls.txt, and tests/surface-baselines/FS.GG.UI.Testing.txt"
Task: "Add Feature170 compatibility tests for additive VisualInspectionArtifact behavior, CompositorDamageReadiness preservation, and screenshot readiness preservation in tests/Package.Tests/Feature170CompatibilityTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundational `.fsi`, FSI/API-shape test, surface-baseline, and fail-closed `.fs` contract work.
3. Complete Phase 3 User Story 1.
4. Stop and validate retained output inspection independently with the User Story 1 focused commands.

### Incremental Delivery

1. Add US1 retained output inspection and validate stable retained node facts.
2. Add US2 damage locality validation and validate union area, full-surface blockers, and shifted/repainted separation.
3. Add US3 AntShowcase adoption and screenshot count preservation.
4. Add US4 validation-lane entry point and readiness evidence.
5. Finish polish, surface baselines, compatibility notes, and final lane evidence.

### Team Strategy

After Phase 2, one developer can work on Controls retained emission (US1/US2), one can work on Testing validation and summaries (US1/US2), and one can prepare AntShowcase/harness tests (US3/US4). Coordinate changes to shared files `src/Scene/Scene.fs`, `src/Controls/Inspection.fs`, and `src/Testing/Testing.fs`.

## Notes

- No task changes the existing `VisualInspectionArtifact` record shape.
- `Scene` remains dependency-light and must not reference Controls, Layout, Testing, SkiaViewer, SkiaSharp, Yoga, Elmish, or KeyboardInput.
- `Controls` must not depend on Testing.
- `Testing` must not depend on Controls.
- Synthetic fixtures must disclose the synthetic fact at the use site and include `Synthetic` in the test name when real retained evidence is unavailable.
