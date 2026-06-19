# Tasks: Shared Visual Readiness Tooling

**Input**: Design documents from `/specs/164-shared-visual-readiness/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/visual-readiness-api.md`, `contracts/visual-readiness-artifacts.md`, `quickstart.md`

**Tests**: Required. The feature specification marks user scenarios and testing as mandatory, and the constitution requires failing semantic tests before behavior-changing implementation.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently. Public API work follows the repo rule: `Testing.fsi` surface first, semantic tests second, `Testing.fs` implementation third.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the Feature 164 test and evidence scaffolding without changing behavior.

- [X] T001 Create the Feature 164 Expecto test module shell in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`
- [X] T002 Add `Feature164VisualReadinessTests.fs` before `Program.fs` in `tests/Testing.Tests/Testing.Tests.fsproj`
- [X] T003 [P] Create the implementation evidence log placeholder in `specs/164-shared-visual-readiness/readiness/validation-log.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the public Testing package contract and design evidence that every story builds on.

**Critical**: No user story implementation begins until the additive public surface is drafted in the `.fsi`.

- [X] T004 Draft `VisualSize`, `VisualTheme`, `VisualPage`, `VisualCaptureTarget`, `VisualCaptureStatus`, `VisualCaptureArtifact`, `VisualCaptureRecord`, `VisualReviewerSeverity`, `VisualReviewerClassification`, `VisualReviewerValidationResult`, `VisualContactSheet`, `VisualReadinessStatus`, `VisualReadinessReport`, `VisualSummarySectionUpdate`, `VisualCaptureMatrix`, `VisualCompleteness`, `VisualReviewerClassifications`, `VisualReadiness`, and `VisualReadinessMarkdown` signatures in `src/Testing/Testing.fsi`
- [X] T005 Record the FSI surface sketch and no-new-dependency confirmation in `specs/164-shared-visual-readiness/readiness/testing-api-fsi.md`

**Checkpoint**: Foundation ready. Story tests can now be written against the drafted package surface.

---

## Phase 3: User Story 1 - Validate Screenshot Evidence Consistently (Priority: P1)

**Goal**: A sample can declare pages, themes, sizes, and output locations, then get deterministic targets and capture classifications for complete, missing, wrong-size, undecodable, degraded, and blocked artifacts.

**Independent Test**: A small representative fixture produces 12 targets for 3 pages x 2 themes x 2 sizes and classifies complete, missing, wrong-size, undecodable, degraded, and stale artifacts without sample-specific logic.

### Tests for User Story 1

Write these tests first and verify they fail before implementing `Testing.fs` behavior.

- [X] T006 [US1] Add failing deterministic target matrix expansion tests for 3 pages x 2 themes x 2 sizes in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`
- [X] T007 [US1] Add failing duplicate page/theme/size/target id, duplicate relative output path, and escaping relative path rejection tests in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`
- [X] T008 [US1] Add `Synthetic` PNG fixture helpers with explicit synthetic comments for complete, wrong-size, corrupt, and zero-byte artifacts in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`
- [X] T009 [US1] Add failing completeness classification and minimal capture-summary tests for complete, missing, wrong-size, undecodable, and zero-byte screenshots in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`
- [X] T010 [US1] Add failing degraded-reason and stale-artifact diagnostic tests in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`

### Implementation for User Story 1

- [X] T011 [US1] Implement deterministic matrix expansion, duplicate id/path detection, and safe relative path validation in `src/Testing/Testing.fs`
- [X] T012 [US1] Implement PNG completeness validation with byte count, SHA-256 content identity, decoded dimensions, minimal capture-summary counts, and actionable diagnostics in `src/Testing/Testing.fs`
- [X] T013 [US1] Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature164` and record the US1 result in `specs/164-shared-visual-readiness/readiness/us1-completeness.md`

**Checkpoint**: User Story 1 is independently testable and delivers the shared screenshot evidence validator.

---

## Phase 4: User Story 2 - Require Reviewer Classification Before Acceptance (Priority: P1)

**Goal**: Reviewer classification templates, parsing, diagnostics, and readiness decisions require explicit review records and block accepted readiness on blocking defects.

**Independent Test**: Reviewer templates for a known target matrix can be left pending, filled with no defects, filled with minor/major defects, or filled with one blocking defect, and the readiness decision changes accordingly.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T014 [US2] Add failing reviewer template generation tests with one row per required target in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`
- [X] T015 [US2] Add failing reviewer parser tests for missing, duplicate, malformed, unknown-target, pending, minor, major, and blocking rows in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`
- [X] T016 [US2] Add failing readiness decision tests for pending review, blocking reviewer defect, all-clear acceptance gates, and the default rule that no accepted exceptions exist unless explicitly supplied in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`

### Implementation for User Story 2

- [X] T017 [US2] Implement reviewer Markdown template generation, parsing, severity normalization, and diagnostics in `src/Testing/Testing.fs`
- [X] T018 [US2] Implement readiness evaluation for capture status, missing reviewer rows, duplicate reviewer rows, blocking reviewer defects, and explicit accepted-exception inputs in `src/Testing/Testing.fs`
- [X] T019 [US2] Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature164` and record the US2 result in `specs/164-shared-visual-readiness/readiness/us2-reviewer-gate.md`

**Checkpoint**: User Stories 1 and 2 together provide the P1 acceptance gate: complete screenshots plus required human review.

---

## Phase 5: User Story 3 - Produce Reviewable Contact Sheets and Summaries (Priority: P2)

**Goal**: Maintainers can produce reviewer-friendly contact sheet metadata and human/machine summaries with clear counts, paths, statuses, caveats, diagnostics, and readiness outcome.

**Independent Test**: A mixed-status fixture yields deterministic contact sheet ordering, target labels, status counts, artifact links, caveats, diagnostics, and machine-readable readiness fields without parsing prose.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T020 [US3] Add failing contact sheet metadata tests for deterministic target ordering, visible target labels, missing target ids, theme grouping, and size grouping in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`
- [X] T021 [US3] Add failing Markdown and JSON summary tests for target counts, status counts, reviewer state, contact sheet paths, caveats, diagnostics, and overall readiness in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`

### Implementation for User Story 3

- [X] T022 [US3] Implement contact sheet metadata aggregation, visible reviewer label data, and report status counts in `src/Testing/Testing.fs`
- [X] T023 [US3] Implement generated Markdown and machine-readable JSON summary rendering for visual readiness reports in `src/Testing/Testing.fs`
- [X] T024 [US3] Document shared visual readiness report, contact sheet metadata, and summary helper usage in `src/Testing/README.md`
- [X] T025 [US3] Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature164` and record the US3 result in `specs/164-shared-visual-readiness/readiness/us3-reporting.md`

**Checkpoint**: User Story 3 is independently testable and produces reviewer-readable and machine-checkable readiness outputs.

---

## Phase 6: User Story 4 - Preserve Manual Readiness Notes (Priority: P2)

**Goal**: Regenerating visual readiness summaries updates only the managed generated section or fails safely, preserving manual notes byte-for-byte outside the generated area.

**Independent Test**: A summary with manual sections before and after the generated section can be regenerated at least three times with identical manual text, while malformed markers fail without writing.

### Tests for User Story 4

Write these tests first and verify they fail before implementation.

- [X] T026 [US4] Add failing managed-section regeneration tests that preserve manual content before and after generated visual readiness content in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`
- [X] T027 [US4] Add failing missing-marker insertion and malformed-marker safe-failure tests for multiple, reversed, and one-sided markers in `tests/Testing.Tests/Feature164VisualReadinessTests.fs`

### Implementation for User Story 4

- [X] T028 [US4] Implement managed section update, deterministic insertion, and no-write diagnostics for unsafe marker layouts in `src/Testing/Testing.fs`
- [X] T029 [US4] Add manual-safe summary regeneration guidance and marker examples in `src/Testing/README.md`
- [X] T030 [US4] Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature164` and record the US4 result in `specs/164-shared-visual-readiness/readiness/us4-summary-preservation.md`

**Checkpoint**: User Story 4 is independently testable and closes the manual-summary overwrite risk from the retrospective.

---

## Phase 7: User Story 5 - Adopt the Shared Workflow in AntShowcase First (Priority: P3)

**Goal**: AntShowcase migrates generic visual-readiness workflow behavior to `FS.GG.UI.Testing` while preserving current page registry, theme choices, accepted sizes, screenshot capture, contact sheet PNG composition, reviewer gates, and summary meaning.

**Independent Test**: The migrated AntShowcase preferred-size run validates 38 required captures, the minimum-size run validates 12 required captures, and tests prove AntShowcase no longer owns duplicated generic matrix, reviewer, summary, or readiness-decision rules.

### Tests for User Story 5

Write these tests first and verify they fail before migration.

- [X] T031 [US5] Add `FS.GG.UI.Testing` package reference for shared visual readiness workflow calls in `samples/AntShowcase/AntShowcase.Core/AntShowcase.Core.fsproj`
- [X] T032 [US5] Add `FS.GG.UI.Testing` package reference for shared API parity assertions in `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`
- [X] T033 [P] [US5] Add failing preferred 38-target and minimum 12-target shared matrix parity tests in `samples/AntShowcase/AntShowcase.Tests/VisualReadinessTests.fs`
- [X] T034 [P] [US5] Add failing reviewer gate and generated-summary parity tests for AntShowcase visual evidence in `samples/AntShowcase/AntShowcase.Tests/VisualEvidenceTests.fs`

### Implementation for User Story 5

- [X] T035 [US5] Refactor AntShowcase visual readiness workflow signatures to expose shared-readiness compatible targets and statuses in `samples/AntShowcase/AntShowcase.Core/VisualReadinessWorkflow.fsi`
- [X] T036 [US5] Refactor AntShowcase visual readiness workflow logic to call shared target matrix and readiness evaluation helpers in `samples/AntShowcase/AntShowcase.Core/VisualReadinessWorkflow.fs`
- [X] T037 [US5] Narrow AntShowcase evidence signatures to sample-owned records while removing duplicated generic summary/reviewer contract ownership in `samples/AntShowcase/AntShowcase.Core/Evidence.fsi`
- [X] T038 [US5] Delegate AntShowcase reviewer template and visual summary serialization to `FS.GG.UI.Testing` helpers in `samples/AntShowcase/AntShowcase.Core/Evidence.fs`
- [X] T039 [US5] Refactor the AntShowcase capture flow to pass shared targets, capture records, reviewer classifications, and reports through the visual-readiness CLI in `samples/AntShowcase/AntShowcase.App/VisualReadiness.fs`
- [X] T040 [US5] Preserve the public AntShowcase visual-readiness CLI entry point while documenting the shared helper boundary in `samples/AntShowcase/AntShowcase.App/VisualReadiness.fsi`
- [X] T041 [US5] Keep contact sheet PNG composition sample-owned while writing shared `VisualContactSheet` metadata in `samples/AntShowcase/AntShowcase.App/VisualReadiness.fs`
- [X] T042 [US5] Document migrated preferred-size and minimum-size visual-readiness commands in `samples/AntShowcase/README.md`
- [X] T043 [US5] Run `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Visual"` and record the result in `specs/164-shared-visual-readiness/readiness/us5-antshowcase-tests.md`
- [X] T044 [US5] Run AntShowcase preferred and minimum visual-readiness commands from `quickstart.md` after local packages are packed and record target counts, reviewer state, summary paths, and before/after generic workflow centralization percentage in `specs/164-shared-visual-readiness/readiness/us5-antshowcase-parity.md`

**Checkpoint**: User Story 5 proves the shared workflow against the first adopter without moving sample-specific rendering into the Testing package.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Finish public surface evidence, documentation, package validation, and repository gates.

- [X] T045 Update the public Testing surface baseline for the additive visual readiness API in `readiness/surface-baselines/FS.GG.UI.Testing.txt`
- [X] T046 [P] Update Feature 164 validation and migration notes in `specs/164-shared-visual-readiness/quickstart.md`
- [X] T047 Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj` and record the full Testing test result in `specs/164-shared-visual-readiness/readiness/testing-tests.md`
- [X] T048 Run `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Visual"` and record the full AntShowcase visual test result in `specs/164-shared-visual-readiness/readiness/antshowcase-visual-tests.md`
- [X] T049 Run `./fake.sh build -t CapabilityCheck` and record the result in `specs/164-shared-visual-readiness/readiness/capability-check.md`
- [X] T050 Run `./fake.sh build -t PackageSurfaceCheck` and record the result in `specs/164-shared-visual-readiness/readiness/package-surface-check.md`
- [X] T051 Run `./fake.sh build -t PackLocal` and record the local package result in `specs/164-shared-visual-readiness/readiness/pack-local.md`
- [X] T052 Run `./fake.sh build -t GeneratedProductCheck` and record the generated-product validation result in `specs/164-shared-visual-readiness/readiness/generated-product-check.md`
- [X] T053 Write the final human validation summary with manual caveats outside generated markers in `specs/164-shared-visual-readiness/readiness/validation-summary.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; starts immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user story implementation.
- **US1 (Phase 3)**: Depends on Foundational; delivers screenshot matrix and completeness validation.
- **US2 (Phase 4)**: Depends on Foundational and benefits from US1 capture status vocabulary; required with US1 before accepted readiness can be claimed.
- **US3 (Phase 5)**: Depends on US1 and US2 report inputs.
- **US4 (Phase 6)**: Depends on US3 summary rendering.
- **US5 (Phase 7)**: Depends on US1 through US4 shared APIs.
- **Polish (Phase 8)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational; no dependency on other stories.
- **US2 (P1)**: Can start after Foundational with synthetic complete captures, but final readiness integration depends on US1 capture status types.
- **US3 (P2)**: Depends on US1 capture records and US2 reviewer/readiness state.
- **US4 (P2)**: Depends on US3 generated Markdown content.
- **US5 (P3)**: Depends on the shared workflow from US1 through US4.

### Within Each User Story

- Public signatures must exist in `src/Testing/Testing.fsi` before tests target them.
- Tests must be written and observed failing before the corresponding implementation tasks.
- Implementation stays in `src/Testing/Testing.fs` for shared package behavior.
- AntShowcase migration must keep rendering, theme selection, page registry, screenshot capture, and contact-sheet PNG composition in sample/app files.
- Each checkpoint should be validated before starting the next priority story.

---

## Parallel Opportunities

- T003 can run in parallel with T001 and T002 because it only creates a readiness log.
- T033 and T034 can run in parallel after T031 and T032 because they edit separate AntShowcase test files.
- T046 can run in parallel with T045 after stories are complete because it edits feature docs while T045 edits the surface baseline.
- Different engineers can work on US3 and US4 tests while US1/US2 implementation stabilizes, but implementation must respect the dependencies above.
- Validation commands in Phase 8 are listed sequentially for clearer evidence, even if local resources allow some to run concurrently.

## Parallel Example: User Story 5

```text
Task: "Add failing preferred 38-target and minimum 12-target shared matrix parity tests in samples/AntShowcase/AntShowcase.Tests/VisualReadinessTests.fs"
Task: "Add failing reviewer gate and generated-summary parity tests for AntShowcase visual evidence in samples/AntShowcase/AntShowcase.Tests/VisualEvidenceTests.fs"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete US1 to prove shared target matrix and screenshot completeness validation.
3. Stop and validate US1 with `specs/164-shared-visual-readiness/readiness/us1-completeness.md`.

US1 is the smallest independently useful increment. Because US2 is also P1, do not claim accepted visual readiness until US2 reviewer gates are implemented and validated.

### Acceptance-Capable P1

1. Complete US1 and US2.
2. Validate the fixture where all captures are complete, every reviewer row is classified, and no blocking defect exists.
3. Validate pending-review and blocking-defect cases before moving to reporting work.

### Incremental Delivery

1. Add US3 reporting so reviewers can inspect contact sheet metadata and summaries.
2. Add US4 safe summary regeneration to protect manual notes.
3. Migrate AntShowcase in US5 and verify preferred/minimum evidence parity.
4. Finish Phase 8 package, surface, and generated-product checks.

## Notes

- `[P]` tasks edit different files and do not depend on incomplete tasks in the same phase.
- `[US1]` through `[US5]` labels map directly to the user stories in `spec.md`.
- Synthetic PNG or malformed Markdown fixtures must include `Synthetic` in the test name and a `SYNTHETIC:` comment at the fixture site.
- The Testing package must not gain new dependencies for this feature; PNG decoding uses the existing SkiaSharp reference.
- Contact sheet PNG composition remains in AntShowcase for Feature 164; the Testing package records and reports contact sheet metadata.
- Accepted exceptions default to none for Feature 164 unless an implementation task explicitly supplies and records an exception input in readiness evidence.
