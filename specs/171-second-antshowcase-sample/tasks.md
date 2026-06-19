# Tasks: Second Ant Showcase Sample

**Input**: Design documents from `/specs/171-second-antshowcase-sample/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md, `.specify/memory/constitution.md`

**Tests**: Required. The feature specification and constitution mandate FSI/prelude API-shape evidence, public sample surface-baseline drift tests, coverage, interaction, visual-readiness, review-finding, template, theme-invariance, determinism, documentation-review, and evidence tests.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independent increment where its prerequisites are met.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: Which user story the task supports.
- Every task includes an exact repository-relative file path.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the independent package-consuming sample tree and review artifact locations without changing `samples/AntShowcase`.

- [X] T001 Create the `samples/SecondAntShowcase/` directory tree with `SecondAntShowcase.Core/`, `SecondAntShowcase.App/`, and `SecondAntShowcase.Tests/`.
- [X] T002 Create package-consumer build configuration in `samples/SecondAntShowcase/nuget.config`, `samples/SecondAntShowcase/Directory.Build.props`, and `samples/SecondAntShowcase/Directory.Packages.props` mirroring `samples/AntShowcase/` without `src/` project references.
- [X] T003 Create Core, App, and Tests project files with planned compile order and package references in `samples/SecondAntShowcase/SecondAntShowcase.Core/SecondAntShowcase.Core.fsproj`, `samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj`, and `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`.
- [X] T004 [P] Create sample documentation skeletons in `samples/SecondAntShowcase/README.md`, `samples/SecondAntShowcase/PROVENANCE.md`, and `samples/SecondAntShowcase/coverage-report.md`.
- [X] T005 [P] Create readiness artifact folders and keep files in `specs/171-second-antshowcase-sample/readiness/preferred/`, `specs/171-second-antshowcase-sample/readiness/minimum/`, `specs/171-second-antshowcase-sample/readiness/fsi/`, `specs/171-second-antshowcase-sample/readiness/surface-baselines/`, `specs/171-second-antshowcase-sample/readiness/limitations.md`, and `specs/171-second-antshowcase-sample/readiness/documentation-review.md`.
- [X] T006 Create Expecto entrypoint, shared helpers, FSI surface tests, public surface-baseline tests, and documentation-review tests in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Main.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Tests/VisualTestHelpers.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Tests/FsiSurfaceTests.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Tests/PublicSurfaceTests.fs`, and `samples/SecondAntShowcase/SecondAntShowcase.Tests/DocumentationReviewTests.fs`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the public sample Core contracts, FSI/prelude evidence, sample surface baseline, pure MVU boundary, CLI shell, and Ant guidance traceability required before user-story implementation.

**Critical**: No user story work should begin until this phase has drafted every public Core `.fsi`, recorded the pre-implementation FSI/prelude exercise, created sample surface-baseline evidence, and compiles with intentional failing semantic tests available in later phases.

- [X] T007 Draft the public MVU model/update, demo-state, and Ant theme signatures in `samples/SecondAntShowcase/SecondAntShowcase.Core/Model.fsi`, `samples/SecondAntShowcase/SecondAntShowcase.Core/DemoState.fsi`, and `samples/SecondAntShowcase/SecondAntShowcase.Core/AntTheme.fsi`.
- [X] T008 [P] Draft the public page registry signature in `samples/SecondAntShowcase/SecondAntShowcase.Core/PageRegistry.fsi`.
- [X] T009 [P] Draft the public coverage signature in `samples/SecondAntShowcase/SecondAntShowcase.Core/CoverageMap.fsi`.
- [X] T010 [P] Draft the public interaction contract signature in `samples/SecondAntShowcase/SecondAntShowcase.Core/InteractionContracts.fsi`.
- [X] T011 [P] Draft the public visual review finding signature in `samples/SecondAntShowcase/SecondAntShowcase.Core/ReviewFindings.fsi`.
- [X] T012 [P] Draft the public visual target and readiness signatures in `samples/SecondAntShowcase/SecondAntShowcase.Core/VisualConfig.fsi` and `samples/SecondAntShowcase/SecondAntShowcase.Core/VisualReadinessWorkflow.fsi`.
- [X] T013 Draft the public evidence, shell, pages, and templates signatures; record the FSI/prelude authoring transcript; and create the initial per-module sample surface baseline in `samples/SecondAntShowcase/SecondAntShowcase.Core/Evidence.fsi`, `samples/SecondAntShowcase/SecondAntShowcase.Core/Shell.fsi`, `samples/SecondAntShowcase/SecondAntShowcase.Core/Pages.fsi`, `samples/SecondAntShowcase/SecondAntShowcase.Core/Templates.fsi`, `specs/171-second-antshowcase-sample/readiness/fsi/README.md`, `specs/171-second-antshowcase-sample/readiness/fsi/second-ant-showcase-authoring.fsx`, and `specs/171-second-antshowcase-sample/readiness/surface-baselines/SecondAntShowcase.Core.txt`.
- [X] T014 Implement compiling Core stubs for the drafted signatures in `samples/SecondAntShowcase/SecondAntShowcase.Core/Model.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Core/DemoState.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Core/AntTheme.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Core/PageRegistry.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Core/CoverageMap.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Core/InteractionContracts.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Core/ReviewFindings.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Core/VisualConfig.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Core/VisualReadinessWorkflow.fs`, and `samples/SecondAntShowcase/SecondAntShowcase.Core/Evidence.fs`.
- [X] T015 Implement compiling sample scene stubs in `samples/SecondAntShowcase/SecondAntShowcase.Core/Shell.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Core/Pages.fs`, and `samples/SecondAntShowcase/SecondAntShowcase.Core/Templates.fs`.
- [X] T016 Implement CLI dispatch skeleton and diagnostics helpers in `samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs` and `samples/SecondAntShowcase/SecondAntShowcase.App/Diagnostics.fs`.
- [X] T017 Implement App edge stubs for interactive, evidence, and visual readiness modes in `samples/SecondAntShowcase/SecondAntShowcase.App/Interactive.fs`, `samples/SecondAntShowcase/SecondAntShowcase.App/Evidence.fs`, and `samples/SecondAntShowcase/SecondAntShowcase.App/VisualReadiness.fs`.
- [X] T018 Document local Ant Design source links and no React/DOM layering rules in `samples/SecondAntShowcase/PROVENANCE.md` using `docs/product/ant-design/reference/ant-llms-sources.md` and `docs/product/ant-design/README.md`.

**Checkpoint**: Foundation compiles, sample projects exist, public Core surfaces are declared through `.fsi`, FSI/prelude and sample surface-baseline evidence exist, and CLI stubs route the required commands.

---

## Phase 3: User Story 1 - Explore Every Control As A Live Ant Sample (Priority: P1) MVP

**Goal**: Maintainers can open the second showcase, browse all catalog pages, see every current control exactly once, and observe visible state changes for interactive controls.

**Independent Test**: Run the coverage and interaction tests, then run `coverage` and `list` from the App; verify every current catalog control appears exactly once and every interactive control has a visible interaction contract or display-only reason.

### Tests for User Story 1

Write these tests first and confirm they fail against the foundational stubs.

- [X] T019 [P] [US1] Add coverage bijection tests for missing, duplicated, and unknown controls in `samples/SecondAntShowcase/SecondAntShowcase.Tests/CoverageTests.fs`.
- [X] T020 [P] [US1] Add page registry tests for 13 catalog pages, six template placeholders, stable page ids, and two-action reachability in `samples/SecondAntShowcase/SecondAntShowcase.Tests/CoverageTests.fs`.
- [X] T021 [P] [US1] Add interaction contract tests for every interactive control and display-only reason coverage in `samples/SecondAntShowcase/SecondAntShowcase.Tests/InteractionTests.fs`.
- [X] T022 [P] [US1] Add visible state transition tests for buttons, entry, numeric/date controls, sliders/rating, selections, navigation, overlays, feedback, data collections, charts, graphs, and custom surfaces in `samples/SecondAntShowcase/SecondAntShowcase.Tests/InteractionTests.fs`.

### Implementation for User Story 1

- [X] T023 [US1] Implement catalog page records, stable ids, control assignments, and navigation metadata for 13 catalog pages in `samples/SecondAntShowcase/SecondAntShowcase.Core/PageRegistry.fs`.
- [X] T024 [US1] Implement live coverage comparison against `FS.GG.UI.Controls.Catalog.supportedControls` in `samples/SecondAntShowcase/SecondAntShowcase.Core/CoverageMap.fs`.
- [X] T025 [US1] Implement deterministic demo state values, options, rows, series, graph data, overlay state, validation state, and feedback state in `samples/SecondAntShowcase/SecondAntShowcase.Core/DemoState.fs`.
- [X] T026 [US1] Implement `Model`, `Msg`, `init`, and pure `update` paths for catalog navigation and control interactions in `samples/SecondAntShowcase/SecondAntShowcase.Core/Model.fs`.
- [X] T027 [US1] Implement interaction contract definitions and scripted actions for all catalog interaction families in `samples/SecondAntShowcase/SecondAntShowcase.Core/InteractionContracts.fs`.
- [X] T028 [US1] Implement Ant-styled catalog page renderers with representative content for all assigned controls in `samples/SecondAntShowcase/SecondAntShowcase.Core/Pages.fs`.
- [X] T029 [US1] Implement the sample shell with navigation, current-page indication, appearance toggle placeholder, and visible review/status area in `samples/SecondAntShowcase/SecondAntShowcase.Core/Shell.fs`.
- [X] T030 [US1] Implement the shipped Ant light and Ant dark theme resolver access in `samples/SecondAntShowcase/SecondAntShowcase.Core/AntTheme.fs`.
- [X] T031 [US1] Implement `coverage`, `list`, and catalog `interactive` CLI behavior with actionable diagnostics in `samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs`, `samples/SecondAntShowcase/SecondAntShowcase.App/Interactive.fs`, and `samples/SecondAntShowcase/SecondAntShowcase.App/Diagnostics.fs`.
- [X] T032 [US1] Update the current generated coverage summary in `samples/SecondAntShowcase/coverage-report.md`.

**Checkpoint**: User Story 1 works independently: catalog coverage is clean, catalog pages render with representative content, and interaction tests prove visible state changes.

---

## Phase 4: User Story 2 - Verify Ant Design Visual Fidelity Iteratively (Priority: P1)

**Goal**: Reviewers can generate and classify visual targets for Ant light and Ant dark at both accepted sizes, record Ant Design findings, and keep acceptance blocked until zero unresolved findings remain.

**Independent Test**: Run visual-readiness and review-finding tests, then generate preferred and minimum readiness summaries; verify 76 targets are represented when all 19 pages exist, limitations are disclosed, and unresolved findings block acceptance.

### Tests for User Story 2

Write these tests first and confirm they fail against the current implementation.

- [X] T033 [P] [US2] Add required visual target matrix tests for all pages, `antLight`, `antDark`, `1600x1000`, and `1280x800` in `samples/SecondAntShowcase/SecondAntShowcase.Tests/VisualReadinessTests.fs`.
- [X] T034 [P] [US2] Add visual finding lifecycle tests for `open`, `fixed`, `reviewed`, and `closed` transitions in `samples/SecondAntShowcase/SecondAntShowcase.Tests/ReviewFindingTests.fs`.
- [X] T035 [P] [US2] Add unresolved finding gate tests for open, fixed-but-unreviewed, reviewed-but-unclosed blocking, malformed, and missing-classification records in `samples/SecondAntShowcase/SecondAntShowcase.Tests/ReviewFindingTests.fs`.
- [X] T036 [P] [US2] Add Ant Design fidelity checks for local source references, palette roles, spacing rhythm, typography hierarchy, contrast, clipping, overlap, alignment, and state coverage in `samples/SecondAntShowcase/SecondAntShowcase.Tests/AntDesignFidelityTests.fs`.

### Implementation for User Story 2

- [X] T037 [US2] Implement accepted review sizes, theme aliases, required target ids, and target validation in `samples/SecondAntShowcase/SecondAntShowcase.Core/VisualConfig.fs`.
- [X] T038 [US2] Implement visual-readiness workflow status aggregation, environment-limited target records, and summary computation in `samples/SecondAntShowcase/SecondAntShowcase.Core/VisualReadinessWorkflow.fs`.
- [X] T039 [US2] Implement visual finding records, lifecycle transitions, unresolved counts, reviewer classification checks, and close rules in `samples/SecondAntShowcase/SecondAntShowcase.Core/ReviewFindings.fs`.
- [X] T040 [US2] Implement `visual-readiness` and `review-findings` CLI commands with non-hanging live-display degradation in `samples/SecondAntShowcase/SecondAntShowcase.App/VisualReadiness.fs` and `samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs`.
- [X] T041 [US2] Apply Ant palette role, spacing, typography, state, and pattern-reference metadata to catalog demonstrations in `samples/SecondAntShowcase/SecondAntShowcase.Core/Pages.fs`.
- [X] T042 [US2] Write preferred-size readiness artifact generation to `specs/171-second-antshowcase-sample/readiness/preferred/`.
- [X] T043 [US2] Write minimum-size readiness artifact generation to `specs/171-second-antshowcase-sample/readiness/minimum/`.
- [X] T044 [US2] Write visual review summaries and finding ledgers to `specs/171-second-antshowcase-sample/readiness/visual-review-summary.md`, `specs/171-second-antshowcase-sample/readiness/visual-review-summary.json`, and `specs/171-second-antshowcase-sample/readiness/visual-findings.md`.

**Checkpoint**: User Story 2 works independently for the current registry: visual target generation, limitation disclosure, and finding lifecycle gates are enforced.

---

## Phase 5: User Story 3 - Demonstrate Complete Enterprise Ant Page Patterns (Priority: P2)

**Goal**: Developers can inspect six realistic Ant-style enterprise templates that compose showcased controls and include meaningful state transitions.

**Independent Test**: Run template tests and interact with each template page; verify workbench, list, detail, form, result, and exception pages are populated, composed from known controls, and at least five of six include meaningful interactions.

### Tests for User Story 3

Write these tests first and confirm they fail against the current implementation.

- [X] T045 [P] [US3] Add template presence, known composed-control ids, and populated seed data tests in `samples/SecondAntShowcase/SecondAntShowcase.Tests/TemplateTests.fs`.
- [X] T046 [P] [US3] Add form invalid-submit and valid-submit behavior tests in `samples/SecondAntShowcase/SecondAntShowcase.Tests/TemplateTests.fs`.
- [X] T047 [P] [US3] Add list filtering, selection, pagination, and exception recovery behavior tests in `samples/SecondAntShowcase/SecondAntShowcase.Tests/TemplateTests.fs`.

### Implementation for User Story 3

- [X] T048 [US3] Implement workbench, list, detail, form, result, and exception template page definitions in `samples/SecondAntShowcase/SecondAntShowcase.Core/Templates.fs`.
- [X] T049 [US3] Add the six template pages to navigation order with `ControlIds = []` for coverage in `samples/SecondAntShowcase/SecondAntShowcase.Core/PageRegistry.fs`.
- [X] T050 [US3] Add template-specific state for filtering, selection, pagination, form validation, success result, and exception recovery in `samples/SecondAntShowcase/SecondAntShowcase.Core/DemoState.fs`.
- [X] T051 [US3] Add pure template update messages and transitions in `samples/SecondAntShowcase/SecondAntShowcase.Core/Model.fs`.
- [X] T052 [US3] Add template interaction contracts and evidence script steps in `samples/SecondAntShowcase/SecondAntShowcase.Core/InteractionContracts.fs`.
- [X] T053 [US3] Include template pages in `list`, `interactive`, and visual-readiness command output in `samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs`.

**Checkpoint**: User Story 3 works independently: all six enterprise templates render as populated Ant-style compositions with tested behavior.

---

## Phase 6: User Story 4 - Switch Ant Light And Ant Dark Without Behavior Drift (Priority: P2)

**Goal**: Users can switch appearances while preserving page selection, entered values, selections, expanded state, overlays, and validation state.

**Independent Test**: Start from representative active states, switch light to dark and dark to light, and verify only visual treatment changes.

### Tests for User Story 4

Write these tests first and confirm they fail against the current implementation.

- [X] T054 [P] [US4] Add theme-switch preservation tests for current page, entered values, selections, expanded state, overlays, and validation state in `samples/SecondAntShowcase/SecondAntShowcase.Tests/ThemeInvarianceTests.fs`.
- [X] T055 [P] [US4] Add theme-invariant interaction script tests for representative catalog and template pages in `samples/SecondAntShowcase/SecondAntShowcase.Tests/ThemeInvarianceTests.fs`.

### Implementation for User Story 4

- [X] T056 [US4] Implement appearance state, aliases, and theme-switch messages in `samples/SecondAntShowcase/SecondAntShowcase.Core/Model.fs`.
- [X] T057 [US4] Preserve control values, expanded state, validation state, overlays, template state, and current page during theme switching in `samples/SecondAntShowcase/SecondAntShowcase.Core/DemoState.fs`.
- [X] T058 [US4] Thread the selected Ant appearance through shell, catalog pages, and template pages in `samples/SecondAntShowcase/SecondAntShowcase.Core/Shell.fs`, `samples/SecondAntShowcase/SecondAntShowcase.Core/Pages.fs`, and `samples/SecondAntShowcase/SecondAntShowcase.Core/Templates.fs`.
- [X] T059 [US4] Implement `interactive [page-id] --theme light|dark` parsing and unknown-theme diagnostics in `samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs` and `samples/SecondAntShowcase/SecondAntShowcase.App/Interactive.fs`.

**Checkpoint**: User Story 4 works independently: switching themes preserves behavior and state while restyling the rendered sample.

---

## Phase 7: User Story 5 - Produce Honest Repeatable Review Evidence (Priority: P3)

**Goal**: Maintainers can run deterministic evidence commands that disclose what was proven, what was limited by the environment, and whether visual acceptance remains blocked.

**Independent Test**: Run the representative evidence command twice with the same seed and compare stable fields; verify limitation disclosures are explicit and environment-limited runs never claim accepted visual fidelity.

### Tests for User Story 5

Write these tests first and confirm they fail against the current implementation.

- [X] T060 [P] [US5] Add deterministic representative evidence tests for stable page sets, script steps, outcomes, coverage, visual target ids, limitations, and summaries in `samples/SecondAntShowcase/SecondAntShowcase.Tests/DeterminismTests.fs`.
- [X] T061 [P] [US5] Add evidence artifact schema and limitation disclosure tests in `samples/SecondAntShowcase/SecondAntShowcase.Tests/DeterminismTests.fs`.
- [X] T062 [P] [US5] Add no-live-display degradation and under-30-second completion tests for evidence and visual-readiness commands in `samples/SecondAntShowcase/SecondAntShowcase.Tests/VisualReadinessTests.fs`.

### Implementation for User Story 5

- [X] T063 [US5] Implement deterministic evidence summary shaping, stable field comparison, limitation classification, and synthetic evidence flags in `samples/SecondAntShowcase/SecondAntShowcase.Core/Evidence.fs`.
- [X] T064 [US5] Implement filesystem evidence writing for coverage, interactions, summaries, command log, and limitations in `samples/SecondAntShowcase/SecondAntShowcase.App/Evidence.fs`.
- [X] T065 [US5] Implement `evidence --seed <int> [--out <dir>] [--page <page-id>]` CLI parsing and diagnostics in `samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs`.
- [X] T066 [US5] Write aggregate evidence outputs to `specs/171-second-antshowcase-sample/readiness/evidence-summary.md`, `specs/171-second-antshowcase-sample/readiness/evidence-summary.json`, `specs/171-second-antshowcase-sample/readiness/coverage.md`, `specs/171-second-antshowcase-sample/readiness/interaction-review.md`, `specs/171-second-antshowcase-sample/readiness/limitations.md`, and `specs/171-second-antshowcase-sample/readiness/command-log.md`.

**Checkpoint**: User Story 5 works independently: representative evidence is repeatable for the same seed and honest about environment limits.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Validate the whole sample, document review status, and protect existing sample behavior.

- [X] T067 [P] Update sample usage, local feed refresh, commands, limitations, relation to `samples/AntShowcase`, visual-review status, and the 10-minute maintainer identification checklist in `samples/SecondAntShowcase/README.md`.
- [X] T068 [P] Update Ant source provenance, local pattern docs used, package-consumer proof, and any visual review limitations in `samples/SecondAntShowcase/PROVENANCE.md`.
- [X] T069 Run local feed refresh commands from `specs/171-second-antshowcase-sample/quickstart.md` and record the result in `specs/171-second-antshowcase-sample/readiness/command-log.md`.
- [X] T070 Run `dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release` and record the result in `specs/171-second-antshowcase-sample/readiness/command-log.md`.
- [X] T071 Run `dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release` and record the result in `specs/171-second-antshowcase-sample/readiness/command-log.md`.
- [X] T072 Run `dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- coverage` and update `specs/171-second-antshowcase-sample/readiness/coverage.md`.
- [X] T073 Run `dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- evidence --seed 1 --out specs/171-second-antshowcase-sample/readiness` and update `specs/171-second-antshowcase-sample/readiness/evidence-summary.md`.
- [X] T074 Run `dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out specs/171-second-antshowcase-sample/readiness/preferred` and update `specs/171-second-antshowcase-sample/readiness/visual-review-summary.md`.
- [X] T075 Run `dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --out specs/171-second-antshowcase-sample/readiness/minimum` and update `specs/171-second-antshowcase-sample/readiness/visual-review-summary.md`.
- [X] T076 Classify live visual targets, record any palette, spacing, typography, contrast, clipping, overlap, alignment, state, or Ant conformance findings, and update `specs/171-second-antshowcase-sample/readiness/visual-findings.md`.
- [X] T077 Fix and re-review every unresolved visual finding until `review-findings --fail-on-unresolved` passes, then update `specs/171-second-antshowcase-sample/readiness/visual-findings.md`.
- [X] T078 Verify the full `samples/AntShowcase/` tree remains unchanged by this feature, including README, project files, Core, App, Tests, package configuration, provenance, and coverage report, and record the check in `specs/171-second-antshowcase-sample/readiness/command-log.md`.
- [X] T079 Confirm `git diff --name-only -- src/` is empty for this sample-only feature and record the result in `specs/171-second-antshowcase-sample/readiness/command-log.md`.
- [X] T080 Run the maintainer documentation review for SC-010 and record whether purpose, relation to `samples/AntShowcase`, Ant guidance source, and current visual-review status are identifiable in under 10 minutes in `specs/171-second-antshowcase-sample/readiness/documentation-review.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **US1 (Phase 3)**: Depends on Foundational and is the MVP.
- **US2 (Phase 4)**: Depends on Foundational; full 76-target acceptance depends on US1 catalog pages and US3 template pages.
- **US3 (Phase 5)**: Depends on Foundational and can proceed after US1 page/coverage conventions exist.
- **US4 (Phase 6)**: Depends on US1 and should include US3 pages when templates are present.
- **US5 (Phase 7)**: Depends on US1, US2, and US4 for complete evidence; can start against partial registry once foundational evidence contracts exist.
- **Polish**: Depends on all targeted user stories.

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies after Foundational. Suggested MVP scope.
- **User Story 2 (P1)**: Can build review mechanics after Foundational; final review matrix requires US1 and US3 page registry completion.
- **User Story 3 (P2)**: Builds on the page registry and MVU conventions from US1.
- **User Story 4 (P2)**: Builds on interaction state from US1 and template state from US3.
- **User Story 5 (P3)**: Builds on coverage, interactions, visual readiness, theme invariance, and finding lifecycle.

### Within Each User Story

- Write listed tests first and confirm they fail for the intended reason.
- Draft or update `.fsi` before exposing new public Core functions.
- Record FSI/prelude evidence and surface-baseline expectations before filling `.fs` bodies.
- Implement pure Core state and reducers before App edge interpretation.
- Implement App CLI behavior after Core contracts are testable.
- Update readiness artifacts only after commands produce deterministic output.

---

## Parallel Opportunities

- Setup documentation and readiness folder tasks T004 and T005 can run beside project scaffolding once T001 exists.
- Foundational signature tasks T008 through T012 can run in parallel after T007; T013 follows once the public signature inventory is stable.
- US1 test tasks T019 through T022 can run in parallel because they target different assertions in test files before implementation.
- US2 test tasks T033 through T036 can run in parallel because visual matrix, finding lifecycle, gate checks, and fidelity checks are separate concerns.
- US3 test tasks T045 through T047 can run in parallel before template implementation.
- US4 test tasks T054 and T055 can run in parallel.
- US5 test tasks T060 through T062 can run in parallel.
- Documentation polish tasks T067 and T068 can run in parallel after the implemented command behavior is known.

---

## Parallel Example: User Story 1

```text
Task: "T019 [P] [US1] Add coverage bijection tests for missing, duplicated, and unknown controls in samples/SecondAntShowcase/SecondAntShowcase.Tests/CoverageTests.fs"
Task: "T021 [P] [US1] Add interaction contract tests for every interactive control and display-only reason coverage in samples/SecondAntShowcase/SecondAntShowcase.Tests/InteractionTests.fs"
Task: "T022 [P] [US1] Add visible state transition tests for buttons, entry, numeric/date controls, sliders/rating, selections, navigation, overlays, feedback, data collections, charts, graphs, and custom surfaces in samples/SecondAntShowcase/SecondAntShowcase.Tests/InteractionTests.fs"
```

## Parallel Example: User Story 2

```text
Task: "T033 [P] [US2] Add required visual target matrix tests for all pages, antLight, antDark, 1600x1000, and 1280x800 in samples/SecondAntShowcase/SecondAntShowcase.Tests/VisualReadinessTests.fs"
Task: "T034 [P] [US2] Add visual finding lifecycle tests for open, fixed, reviewed, and closed transitions in samples/SecondAntShowcase/SecondAntShowcase.Tests/ReviewFindingTests.fs"
Task: "T036 [P] [US2] Add Ant Design fidelity checks for local source references, palette roles, spacing rhythm, typography hierarchy, contrast, clipping, overlap, alignment, and state coverage in samples/SecondAntShowcase/SecondAntShowcase.Tests/AntDesignFidelityTests.fs"
```

## Parallel Example: User Story 3

```text
Task: "T045 [P] [US3] Add template presence, known composed-control ids, and populated seed data tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/TemplateTests.fs"
Task: "T046 [P] [US3] Add form invalid-submit and valid-submit behavior tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/TemplateTests.fs"
Task: "T047 [P] [US3] Add list filtering, selection, pagination, and exception recovery behavior tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/TemplateTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational contracts and stubs.
3. Complete Phase 3: User Story 1.
4. Stop and validate with coverage, list, interaction tests, and catalog interactive smoke.

### Incremental Delivery

1. Add US1 to prove every current control is reachable and live.
2. Add US2 to establish visual-readiness and finding lifecycle gates.
3. Add US3 to complete enterprise Ant template coverage.
4. Add US4 to prove light/dark switching preserves behavior.
5. Add US5 to produce deterministic evidence and limitation disclosures.
6. Complete polish validation and visual review closure.

### Validation Commands

```sh
dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release
dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release
dotnet fsi specs/171-second-antshowcase-sample/readiness/fsi/second-ant-showcase-authoring.fsx
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- coverage
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- evidence --seed 1 --out specs/171-second-antshowcase-sample/readiness
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- review-findings --out specs/171-second-antshowcase-sample/readiness --fail-on-unresolved
```

---

## Notes

- Tasks avoid new product controls, new product themes, React, DOM, HTML, CSS, and direct `src/` project references.
- Product public surface changes are not planned; T079 exists only as a guardrail if implementation discovers one is unavoidable.
- Sample public Core surfaces are governed by the feature-local `.fsi`, FSI/prelude, and surface-baseline tasks before `.fs` bodies are completed.
- Environment-limited evidence is useful for automation but cannot be counted as accepted live visual fidelity.
