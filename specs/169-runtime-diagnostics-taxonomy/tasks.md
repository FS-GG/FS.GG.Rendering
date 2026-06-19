# Tasks: Runtime Diagnostics Taxonomy

**Input**: Design documents from `/specs/169-runtime-diagnostics-taxonomy/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included. The feature specification marks user-scenario testing as mandatory, and the constitution requires behavior-changing code to have failing tests before implementation. Any fixture, hardcoded diagnostic, in-memory substitute, or other synthetic evidence MUST disclose `// SYNTHETIC:` at the use site, include `Synthetic` in the affected test name, and be listed in readiness/PR evidence.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- Source packages: `src/<Package>/`
- Repository tests: `tests/<Package>.Tests/`
- Rendering harness: `tests/Rendering.Harness/`
- Sample consumer: `samples/AntShowcase/`
- Feature readiness evidence: `specs/169-runtime-diagnostics-taxonomy/readiness/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the package and test-project scaffolding needed by all stories.

- [X] T001 Add `FS.GG.UI.Diagnostics` project metadata in `src/Diagnostics/Diagnostics.fsproj` and register `src/Diagnostics/Diagnostics.fsproj` in `FS.GG.Rendering.slnx`
- [X] T002 [P] Add package purpose, dependency policy, and no-telemetry notes in `src/Diagnostics/README.md`
- [X] T003 Add `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` with Expecto package references and register `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` in `FS.GG.Rendering.slnx`
- [X] T004 Add Expecto entry point in `tests/Diagnostics.Tests/Program.fs`
- [X] T005 [P] Create readiness output skeleton in `specs/169-runtime-diagnostics-taxonomy/readiness/.gitkeep`
- [X] T006 [P] Create public API semantic-check script stub in `scripts/diagnostics-prelude.fsx`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define the shared public contract and project references before any user story implementation.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T007 Draft the public taxonomy, aggregation, readiness, artifact, and console signatures in `src/Diagnostics/Diagnostics.fsi`
- [X] T008 Add public API semantic checks in `scripts/diagnostics-prelude.fsx` that exercise the `src/Diagnostics/Diagnostics.fsi` surface for severity labels, categories, aggregation, readiness status, artifact rendering, and console rendering before any `src/Diagnostics/Diagnostics.fs` body work
- [X] T009 Run `dotnet fsi scripts/diagnostics-prelude.fsx` after T007 and record the expected pre-implementation failure plus FSI feedback in `specs/169-runtime-diagnostics-taxonomy/readiness/feature169-tests.md`
- [X] T010 Add compile-only implementations that match the signatures in `src/Diagnostics/Diagnostics.fs`
- [X] T011 Add `src/Diagnostics/Diagnostics.fsi` and `src/Diagnostics/Diagnostics.fs` compile entries to `src/Diagnostics/Diagnostics.fsproj`
- [X] T012 Add `ProjectReference` edges to `src/Diagnostics/Diagnostics.fsproj` in `src/Controls/Controls.fsproj`, `src/SkiaViewer/SkiaViewer.fsproj`, `src/Controls.Elmish/Controls.Elmish.fsproj`, `src/Testing/Testing.fsproj`, and `tests/Rendering.Harness/Rendering.Harness.fsproj`
- [X] T013 Add diagnostics package references to repository test projects in `tests/Controls.Tests/Controls.Tests.fsproj`, `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj`, `tests/Elmish.Tests/Elmish.Tests.fsproj`, `tests/Testing.Tests/Testing.Tests.fsproj`, `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`, and `tests/Package.Tests/Package.Tests.fsproj`
- [X] T014 [P] Add shared mixed-diagnostic fixtures in `tests/Diagnostics.Tests/Feature169Fixtures.fs`, with `// SYNTHETIC:` disclosure comments or a documented real-evidence source for every hardcoded diagnostic value
- [X] T015 Register `tests/Diagnostics.Tests/Feature169Fixtures.fs` before other Feature 169 test files in `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj`
- [X] T016 Inventory existing Controls, SkiaViewer, Controls.Elmish, sample, and validation-lane diagnostic messages and classify any rename, reclassification, or newly blocking behavior in `specs/169-runtime-diagnostics-taxonomy/readiness/migration-notes.md` before adapter tests or mapping implementations begin

**Checkpoint**: Foundation ready. User story implementation can now begin in priority order or in parallel where dependencies allow.

---

## Phase 3: User Story 1 - Understand Sample Runtime Diagnostics (Priority: P1) MVP

**Goal**: Sample and runtime diagnostics are classified, grouped by severity and category, and blockers are separated from expected environment/backend-cost information.

**Independent Test**: Run the representative mixed fixture and verify every diagnostic is grouped by category and severity, blocker presence is reported, and expected environment/backend-cost diagnostics remain visible but non-blocking.

### Tests for User Story 1

Write these tests first and verify they fail before implementation.

- [X] T017 [P] [US1] Add mixed-fixture classification and summary tests in `tests/Diagnostics.Tests/Feature169ClassificationTests.fs`, including stable reviewer-facing severity label and action-guidance assertions
- [X] T018 [P] [US1] Add repeated backend-cost aggregation tests for 100 identical diagnostics in `tests/Diagnostics.Tests/Feature169AggregationTests.fs`
- [X] T019 [P] [US1] Add Controls diagnostic adapter mapping tests in `tests/Controls.Tests/Feature169RuntimeDiagnosticMappingTests.fs`
- [X] T020 [P] [US1] Add SkiaViewer host diagnostic adapter mapping tests in `tests/SkiaViewer.Tests/Feature169HostDiagnosticMappingTests.fs`
- [X] T021 [P] [US1] Add Controls.Elmish adapter diagnostic mapping tests in `tests/Elmish.Tests/Feature169AdapterDiagnosticMappingTests.fs`
- [X] T022 [US1] Add stream-origin classification coverage for environment warnings emitted on stdout/stderr or separate runtime streams in `tests/Diagnostics.Tests/Feature169ClassificationTests.fs`
- [X] T023 [US1] Register US1 test files in `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj`, `tests/Controls.Tests/Controls.Tests.fsproj`, `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj`, and `tests/Elmish.Tests/Elmish.Tests.fsproj`

### Implementation for User Story 1

- [X] T024 [US1] Implement `DiagnosticSeverity`, `DiagnosticCategory`, token rendering, `source`, `context`, and `create` helpers in `src/Diagnostics/Diagnostics.fs`
- [X] T025 [US1] Implement stable fingerprint generation and `aggregate` occurrence grouping in `src/Diagnostics/Diagnostics.fs`
- [X] T026 [US1] Implement `summarize` count derivation, blocker counts, unclassified counts, and grouped summary construction in `src/Diagnostics/Diagnostics.fs`
- [X] T027 [US1] Implement compact grouped console rendering for classified summaries in `src/Diagnostics/Diagnostics.fs`
- [X] T028 [US1] Add stream-origin fields or context mapping needed to preserve stdout/stderr/runtime stream origin in `src/Diagnostics/Diagnostics.fs`
- [X] T029 [US1] Add `toRuntimeDiagnostic` signature for controls diagnostics in `src/Controls/Diagnostics.fsi`
- [X] T030 [US1] Implement Controls severity/category/source mapping in `src/Controls/Diagnostics.fs`
- [X] T031 [US1] Add `toRuntimeDiagnostic` signature for host diagnostics in `src/SkiaViewer/Host/Diagnostics.fsi`
- [X] T032 [US1] Implement SkiaViewer host severity/category/source mapping in `src/SkiaViewer/Host/Diagnostics.fs`
- [X] T033 [US1] Add `adapterDiagnosticToRuntimeDiagnostic` signature in `src/Controls.Elmish/ControlsElmish.fsi`
- [X] T034 [US1] Implement Controls.Elmish adapter diagnostic mapping in `src/Controls.Elmish/ControlsElmish.fs`
- [X] T035 [US1] Add sample diagnostics command implementation in `samples/AntShowcase/AntShowcase.App/Diagnostics.fs`
- [X] T036 [US1] Register `samples/AntShowcase/AntShowcase.App/Diagnostics.fs` before `Program.fs` in `samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj`
- [X] T037 [US1] Add `FS.GG.UI.Diagnostics` sample package reference in `samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj` and `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`

**Checkpoint**: User Story 1 is independently testable with diagnostics package tests plus Controls, SkiaViewer, Elmish adapter mapping tests.

---

## Phase 4: User Story 2 - Gate Readiness by Blocker Status (Priority: P1)

**Goal**: Readiness decisions consume typed diagnostics so informational diagnostics remain visible, true blockers block readiness, and unclassified diagnostics require review.

**Independent Test**: Evaluate fixtures with only informational diagnostics, warning diagnostics, a readiness blocker, an unclassified diagnostic, and accepted exceptions. Verify status and caveats match the diagnostic content without parsing console prose.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T038 [P] [US2] Add readiness status rule tests in `tests/Diagnostics.Tests/Feature169ReadinessTests.fs`
- [X] T039 [P] [US2] Add Testing helper readiness wrapper tests in `tests/Testing.Tests/Feature169RuntimeDiagnosticsReadinessTests.fs`
- [X] T040 [P] [US2] Add validation-lane typed diagnostics tests in `tests/Rendering.Harness.Tests/Feature169ValidationDiagnosticsTests.fs`
- [X] T041 [US2] Register US2 test files in `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj`, `tests/Testing.Tests/Testing.Tests.fsproj`, and `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`

### Implementation for User Story 2

- [X] T042 [US2] Implement ordered readiness evaluation rules for missing classification, invalid exceptions, blockers, review-required diagnostics, environment-limited status, and accepted status in `src/Diagnostics/Diagnostics.fs`
- [X] T043 [US2] Implement diagnostic exception matching, expiration checks, and visible exception counts in `src/Diagnostics/Diagnostics.fs`
- [X] T044 [US2] Add runtime diagnostic readiness helper signatures in `src/Testing/Testing.fsi`
- [X] T045 [US2] Implement runtime diagnostic readiness helper wrappers in `src/Testing/Testing.fs`
- [X] T046 [US2] Add typed diagnostic readiness fields or references to validation lane signatures in `tests/Rendering.Harness/ValidationLanes.fsi`
- [X] T047 [US2] Derive validation lane readiness from `DiagnosticSummary.Status` instead of parsed diagnostic prose in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T048 [US2] Add diagnostics-lane CLI selection and output wiring in `tests/Rendering.Harness/Cli.fs`

**Checkpoint**: User Story 2 is independently testable through diagnostics readiness tests and validation-lane tests.

---

## Phase 5: User Story 3 - Review Structured Diagnostic Artifacts (Priority: P2)

**Goal**: Maintainers can review durable JSON, Markdown, and JSONL diagnostic artifacts with counts, sources, categories, severities, blocker status, repeated-message counts, and recommended actions.

**Independent Test**: Generate artifacts from the mixed fixture and verify a validation lane can compute category counts, severity counts, blocker status, and repeated-message totals from structured artifacts without parsing prose.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T049 [P] [US3] Add `diagnostics-summary.json` contract tests in `tests/Diagnostics.Tests/Feature169ArtifactTests.fs`
- [X] T050 [P] [US3] Add Markdown reviewer summary tests in `tests/Diagnostics.Tests/Feature169ArtifactMarkdownTests.fs`, including checks that blocker status, category counts, severity counts, and accepted exceptions are visible without reading raw records so SC-005 can be completed within 2 minutes
- [X] T051 [P] [US3] Add JSONL individual-record artifact tests in `tests/Diagnostics.Tests/Feature169JsonLinesTests.fs`
- [X] T052 [P] [US3] Add artifact write failure tests in `tests/Diagnostics.Tests/Feature169ArtifactFailureTests.fs`
- [X] T053 [P] [US3] Add validation-lane artifact consumption tests in `tests/Rendering.Harness.Tests/Feature169DiagnosticArtifactTests.fs`
- [X] T054 [P] [US3] Add stale prior-run blocker artifact isolation tests in `tests/Diagnostics.Tests/Feature169ArtifactTests.fs`
- [X] T055 [US3] Register US3 test files in `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` and `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`

### Implementation for User Story 3

- [X] T056 [US3] Extend artifact rendering and writer signatures for JSON, Markdown, and JSONL in `src/Diagnostics/Diagnostics.fsi`
- [X] T057 [US3] Implement `renderJson` with `runtime-diagnostics-v1` schema tokens in `src/Diagnostics/Diagnostics.fs`
- [X] T058 [US3] Implement `renderMarkdown` reviewer summary output in `src/Diagnostics/Diagnostics.fs`
- [X] T059 [US3] Implement JSONL rendering for individual `RuntimeDiagnostic` records in `src/Diagnostics/Diagnostics.fs`
- [X] T060 [US3] Implement filesystem artifact writer failure handling that emits developer-action diagnostics in `src/Diagnostics/Diagnostics.fs`
- [X] T061 [US3] Implement run-identifier, clean-write, or overwrite semantics that prevent prior-run blocker artifacts from contaminating later clean summaries in `src/Diagnostics/Diagnostics.fs`
- [X] T062 [US3] Link diagnostic artifact paths from validation lane summaries in `tests/Rendering.Harness/ValidationLanes.fs`
- [X] T063 [US3] Write fixture artifact evidence to `specs/169-runtime-diagnostics-taxonomy/readiness/diagnostics-fixture-summary.json` and `specs/169-runtime-diagnostics-taxonomy/readiness/diagnostics-fixture-summary.md`

**Checkpoint**: User Story 3 is independently testable through artifact shape tests and validation-lane artifact consumption tests.

---

## Phase 6: User Story 4 - Keep Console Output Concise by Default (Priority: P3)

**Goal**: Default sample output is compact and grouped, while verbose output and artifacts preserve full diagnostic detail.

**Independent Test**: Run the same fixture in default and verbose modes. Verify default output remains at or below 12 lines for the mixed fixture, verbose output includes detailed records, and artifact write failures appear as developer-action warnings.

### Tests for User Story 4

Write these tests first and verify they fail before implementation.

- [X] T064 [P] [US4] Add default and verbose console rendering tests in `tests/Diagnostics.Tests/Feature169ConsoleTests.fs`
- [X] T065 [P] [US4] Add AntShowcase diagnostics CLI default and verbose tests in `samples/AntShowcase/AntShowcase.Tests/Feature169DiagnosticsCliTests.fs`
- [X] T066 [US4] Register US4 test files in `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` and `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`

### Implementation for User Story 4

- [X] T067 [US4] Enforce the mixed-fixture default console budget in `src/Diagnostics/Diagnostics.fs`
- [X] T068 [US4] Add verbose console output for source, code, category, severity, occurrence count, first/last context, action guidance, and exception id in `src/Diagnostics/Diagnostics.fs`
- [X] T069 [US4] Add `--out`, `--json`, and `--verbose` parsing to `samples/AntShowcase/AntShowcase.App/Diagnostics.fs`
- [X] T070 [US4] Add `diagnostics` usage text and command dispatch in `samples/AntShowcase/AntShowcase.App/Program.fs`
- [X] T071 [US4] Record default and verbose sample output evidence in `specs/169-runtime-diagnostics-taxonomy/readiness/sample-output.md`

**Checkpoint**: User Story 4 is independently testable through console rendering tests and AntShowcase CLI tests.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Surface baselines, semantic checks, package validation, migration notes, and final evidence.

- [X] T072 [P] Refresh `FS.GG.UI.Diagnostics` public surface baseline in `tests/surface-baselines/FS.GG.UI.Diagnostics.txt`
- [X] T073 [P] Refresh adapter and helper public surface baselines in `tests/surface-baselines/FS.GG.UI.Controls.txt`, `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt`, `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt`, and `tests/surface-baselines/FS.GG.UI.Testing.txt`
- [X] T074 [P] Finalize migration notes in `specs/169-runtime-diagnostics-taxonomy/readiness/migration-notes.md`, confirming the T016 inventory is updated with any implementation-time renamed, reclassified, or newly blocking diagnostics
- [X] T075 Run `dotnet fsi scripts/diagnostics-prelude.fsx` and record semantic-check output in `specs/169-runtime-diagnostics-taxonomy/readiness/feature169-tests.md`
- [X] T076 Run focused Feature169 `dotnet test` commands and record results in `specs/169-runtime-diagnostics-taxonomy/readiness/feature169-tests.md`
- [X] T077 Run `dotnet fsi scripts/refresh-surface-baselines.fsx` and package surface tests, then record results in `specs/169-runtime-diagnostics-taxonomy/readiness/validation-log.md`
- [X] T078 Pack updated packages to `~/.local/share/nuget-local/` and record package-feed validation notes in `specs/169-runtime-diagnostics-taxonomy/readiness/validation-log.md`
- [X] T079 Run `dotnet fsi scripts/run-validation-lanes.fsx --out specs/169-runtime-diagnostics-taxonomy/readiness/lanes --include diagnostics` and record results in `specs/169-runtime-diagnostics-taxonomy/readiness/validation-log.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies, can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational. MVP scope.
- **User Story 2 (Phase 4)**: Depends on Foundational and can begin once shared summary types exist; validation-lane integration is easier after US1 summary construction.
- **User Story 3 (Phase 5)**: Depends on US1 summary construction and US2 readiness status rules.
- **User Story 4 (Phase 6)**: Depends on US1 console summary and US3 artifact writer behavior.
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational. No dependency on other user stories.
- **US2 (P1)**: Can start after Foundational, but final validation-lane behavior depends on the `DiagnosticSummary` from US1.
- **US3 (P2)**: Depends on US1 grouped summaries and US2 readiness interpretation.
- **US4 (P3)**: Depends on US1 console renderer and US3 artifact failure diagnostics.

### Within Each User Story

- Tests must be written and observed failing before implementation.
- `.fsi` public signatures and semantic FSI checks must be drafted and exercised before `.fs` bodies.
- Synthetic fixtures, hardcoded diagnostics, in-memory substitutes, or other synthetic evidence must use `Synthetic` test names, `// SYNTHETIC:` use-site disclosure comments, and readiness/PR evidence notes.
- Project-file compile entries must be updated when adding F# source files.
- Core shared package changes come before producer adapters, Testing wrappers, harness integration, and sample consumer wiring.
- Story checkpoint validation must pass before using that story as evidence for the next priority.

### Parallel Opportunities

- Setup tasks T002, T005, and T006 can run in parallel after T001/T003 ownership is clear.
- Foundational fixture task T014 can run in parallel with project-reference edits T012 and T013.
- US1 test files T017-T022 can be authored in parallel before registration T023.
- US2 test files T038-T040 can be authored in parallel before registration T041.
- US3 test files T049-T054 can be authored in parallel before registration T055.
- US4 test files T064-T065 can be authored in parallel before registration T066.
- Surface baseline tasks T072-T073 and migration notes T074 can run in parallel after implementation stabilizes.

---

## Parallel Example: User Story 1

```bash
Task: "Add mixed-fixture classification and summary tests in tests/Diagnostics.Tests/Feature169ClassificationTests.fs"
Task: "Add repeated backend-cost aggregation tests for 100 identical diagnostics in tests/Diagnostics.Tests/Feature169AggregationTests.fs"
Task: "Add Controls diagnostic adapter mapping tests in tests/Controls.Tests/Feature169RuntimeDiagnosticMappingTests.fs"
Task: "Add SkiaViewer host diagnostic adapter mapping tests in tests/SkiaViewer.Tests/Feature169HostDiagnosticMappingTests.fs"
Task: "Add Controls.Elmish adapter diagnostic mapping tests in tests/Elmish.Tests/Feature169AdapterDiagnosticMappingTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "Add readiness status rule tests in tests/Diagnostics.Tests/Feature169ReadinessTests.fs"
Task: "Add Testing helper readiness wrapper tests in tests/Testing.Tests/Feature169RuntimeDiagnosticsReadinessTests.fs"
Task: "Add validation-lane typed diagnostics tests in tests/Rendering.Harness.Tests/Feature169ValidationDiagnosticsTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "Add diagnostics-summary.json contract tests in tests/Diagnostics.Tests/Feature169ArtifactTests.fs"
Task: "Add Markdown reviewer summary tests in tests/Diagnostics.Tests/Feature169ArtifactMarkdownTests.fs"
Task: "Add JSONL individual-record artifact tests in tests/Diagnostics.Tests/Feature169JsonLinesTests.fs"
Task: "Add artifact write failure tests in tests/Diagnostics.Tests/Feature169ArtifactFailureTests.fs"
Task: "Add validation-lane artifact consumption tests in tests/Rendering.Harness.Tests/Feature169DiagnosticArtifactTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "Add default and verbose console rendering tests in tests/Diagnostics.Tests/Feature169ConsoleTests.fs"
Task: "Add AntShowcase diagnostics CLI default and verbose tests in samples/AntShowcase/AntShowcase.Tests/Feature169DiagnosticsCliTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Stop and validate diagnostics classification, aggregation, adapter mappings, and compact grouped summaries.
5. Use US1 as the MVP for separating expected environment/backend-cost messages from real blockers.

### Incremental Delivery

1. Complete Setup and Foundational work.
2. Add US1 to make sample/runtime diagnostics understandable.
3. Add US2 to gate readiness from typed blocker and review-required status.
4. Add US3 to produce durable JSON, Markdown, and JSONL artifacts.
5. Add US4 to polish default/verbose console behavior for sample users.
6. Complete polish tasks and record readiness evidence.

### Parallel Team Strategy

1. One contributor owns the shared `src/Diagnostics` contract and implementation.
2. Producer owners add Controls, SkiaViewer, and Controls.Elmish adapters after the shared `.fsi` stabilizes.
3. Harness and Testing owners add readiness integration after US2 status rules are available.
4. Sample owner wires AntShowcase CLI behavior after console and artifact functions are available.

## Notes

- [P] tasks use different files or are safe to perform independently before explicit registration tasks.
- Every public `.fs` addition must have a matching `.fsi` surface or stay out of public package scope.
- Runtime packages must not depend on `FS.GG.UI.Testing` for diagnostic emission.
- Unclassified or partially classified diagnostics must fail closed to review-required status.
- Artifact write failures must remain visible as developer-action diagnostics.
