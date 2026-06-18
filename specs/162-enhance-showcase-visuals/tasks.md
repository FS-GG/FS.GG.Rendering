# Tasks: AntShowcase Visual Overhaul

**Input**: Design documents from `/specs/162-enhance-showcase-visuals/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by the feature specification and constitution. Story tests should be written
first and observed failing before implementation.

**Implementation note (2026-06-18)**: The Feature 162 test files and validation gates are
implemented and passing, but this task run did not preserve separate failing-first output for the
new tests. Full-solution test validation was attempted and recorded in
`readiness/full-validation/validation.md`; `Controls.Tests` did not complete before cancellation.

**Organization**: Tasks are grouped by user story so shell containment, catalog readability,
template polish, visual evidence, and accepted-size/theme behavior can be implemented and tested
independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on
  incomplete tasks.
- **[Story]**: User story label from spec.md.
- Every task names the exact file or readiness path to change.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare Feature 162 readiness locations and package-only validation records.

- [X] T001 Create the Feature 162 readiness directory guide in specs/162-enhance-showcase-visuals/readiness/README.md with visual-evidence, minimum-size, package-feed, compatibility, regression, full-validation, and validation-summary locations from plan.md
- [X] T002 [P] Create the preferred visual evidence directory guide in specs/162-enhance-showcase-visuals/readiness/visual-evidence/README.md with light, dark, completeness, contact sheet, reviewer-defects, summary.md, and summary.json expectations
- [X] T003 [P] Create the minimum-size evidence directory guide in specs/162-enhance-showcase-visuals/readiness/minimum-size/README.md with the 1280x800 representative page and theme expectations from quickstart.md
- [X] T004 [P] Create the package-feed validation ledger in specs/162-enhance-showcase-visuals/readiness/package-feed.md with build, pack, local-feed, AntShowcase build, list, coverage, and focused-test sections
- [X] T005 [P] Create the compatibility ledger in specs/162-enhance-showcase-visuals/readiness/compatibility-ledger.md with a no-public-surface-change default and a lower-level package change escalation section
- [X] T006 [P] Create the regression validation ledger in specs/162-enhance-showcase-visuals/readiness/regression-validation.md with existing AntShowcase coverage, determinism, interaction, feedback, template, theme, and overlay test sections

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared visual configuration, layout descriptors, and compile-order hooks before
story implementation.

**CRITICAL**: No user story implementation should begin until these shared files and project-file
entries are in place.

- [X] T007 [P] Create accepted-size, canonical theme id (`antLight`/`antDark`), CLI theme alias, visual-readiness status, and page-selection constants in samples/AntShowcase/AntShowcase.Core/VisualConfig.fsi and samples/AntShowcase/AntShowcase.Core/VisualConfig.fs
- [X] T008 [P] Create shell region, bounds, navigation label, overflow policy, and containment helper types in samples/AntShowcase/AntShowcase.Core/ShellLayout.fsi and samples/AntShowcase/AntShowcase.Core/ShellLayout.fs
- [X] T009 [P] Create page density, section layout, large demonstration region, transient-surface policy, and minimum-size representative mappings in samples/AntShowcase/AntShowcase.Core/PageProfiles.fsi and samples/AntShowcase/AntShowcase.Core/PageProfiles.fs
- [X] T010 Update compile order for VisualConfig.fsi/fs, ShellLayout.fsi/fs, and PageProfiles.fsi/fs before Shell.fs and Pages.fs in samples/AntShowcase/AntShowcase.Core/AntShowcase.Core.fsproj
- [X] T011 [P] Create the pure visual-readiness workflow boundary with Model, Msg, Effect, init, and update stubs in samples/AntShowcase/AntShowcase.Core/VisualReadinessWorkflow.fsi and samples/AntShowcase/AntShowcase.Core/VisualReadinessWorkflow.fs
- [X] T012 Create the visual-readiness app-edge command module signature/stub in samples/AntShowcase/AntShowcase.App/VisualReadiness.fsi and samples/AntShowcase/AntShowcase.App/VisualReadiness.fs, then update VisualReadinessWorkflow.fsi/fs compile order in samples/AntShowcase/AntShowcase.Core/AntShowcase.Core.fsproj and VisualReadiness.fsi/fs compile order before Program.fs in samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj
- [X] T013 Create shared visual test helpers in samples/AntShowcase/AntShowcase.Tests/VisualTestHelpers.fs and wire VisualTestHelpers.fs before CoverageTests.fs in samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj

**Checkpoint**: Shared visual configuration, layout descriptors, page profiles, and project-file
entries are ready for user story tests and implementation.

---

## Phase 3: User Story 1 - Browse Every Showcase Page Without Shell Collisions (Priority: P1) MVP

**Goal**: Top bar, navigation rail, content, feedback, and status regions stay readable and
separate on every page at accepted sizes.

**Independent Test**: Capture or render every page at the preferred accepted size and verify that
navigation text, top-bar controls, content, feedback, and status text do not overlap another shell
region.

### Tests for User Story 1

> Write these tests first and confirm they fail before implementation.

- [X] T014 [P] [US1] Create failing shell-region, navigation-label containment, content-region containment, feedback/status containment, visible page/theme affordance discoverability, and all-page preferred-size render tests in samples/AntShowcase/AntShowcase.Tests/VisualShellTests.fs
- [X] T015 [US1] Wire VisualShellTests.fs into the compile order before Main.fs in samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj

### Implementation for User Story 1

- [X] T016 [US1] After T014-T015 are observed failing, implement preferred and minimum shell region calculations, nav label shortening, and disjoint-bounds validation helpers in samples/AntShowcase/AntShowcase.Core/ShellLayout.fs
- [X] T017 [US1] Refactor appBar, navRail, content, feedbackSection, statusStrip, and view to render bounded painted shell regions with visible current-page, navigation, and theme-switch affordances in samples/AntShowcase/AntShowcase.Core/Shell.fs
- [X] T018 [US1] Update interactive launch size to the preferred inspection size and preserve explicit minimum-size documentation in samples/AntShowcase/AntShowcase.App/Interactive.fs
- [X] T019 [US1] Update existing page render assertions to render every catalog and template page at 1600x1000 through the full shell and assert current page/theme affordance visibility in samples/AntShowcase/AntShowcase.Tests/PageRenderTests.fs
- [X] T020 [US1] Run the shell-focused AntShowcase tests and record command, status, and output location in specs/162-enhance-showcase-visuals/readiness/package-feed.md

**Checkpoint**: User Story 1 is independently testable through VisualShell/PageRender tests and is
the MVP baseline for visual inspection.

---

## Phase 4: User Story 2 - Inspect Every Control Family in a Readable Presentation (Priority: P1)

**Goal**: Every catalog page presents controls in readable sections, with large or dense controls
given dedicated regions that do not overpaint neighboring sections.

**Independent Test**: Review each catalog page screenshot and confirm every section label and
demonstrated control is readable, contained inside its intended section, and not overpainted.

### Tests for User Story 2

> Write these tests first and confirm they fail before implementation.

- [X] T021 [P] [US2] Create failing catalog-section readability, page-profile coverage, large-region allocation, transient-baseline policy, and live-catalog bijection tests in samples/AntShowcase/AntShowcase.Tests/VisualPageTests.fs
- [X] T022 [US2] Wire VisualPageTests.fs into the compile order before Main.fs in samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj

### Implementation for User Story 2

- [X] T023 [US2] After T021-T022 are observed failing, implement page visual profiles for every catalog page in samples/AntShowcase/AntShowcase.Core/PageProfiles.fs
- [X] T024 [US2] Recompose display, cards/media, buttons, input, selection, layout, navigation, overlays, feedback, and data sections with bounded labels and readable spacing in samples/AntShowcase/AntShowcase.Core/Pages.fs
- [X] T025 [US2] Recompose chart, graph, calendar, data-grid, collection, media, drawer, overlay, and multi-selector demonstrations into large or high-density regions in samples/AntShowcase/AntShowcase.Core/Pages.fs
- [X] T026 [US2] Add readable missing-asset placeholders and deterministic representative demo values for visual pages in samples/AntShowcase/AntShowcase.Core/DemoState.fs
- [X] T027 [US2] Extend catalog coverage and page-render assertions to require a visual profile for every catalog page in samples/AntShowcase/AntShowcase.Tests/CoverageTests.fs
- [X] T028 [US2] Run the VisualPage and Coverage tests and record command, status, and output location in specs/162-enhance-showcase-visuals/readiness/package-feed.md

**Checkpoint**: User Story 2 is independently testable through VisualPage/Coverage tests and all
catalog controls remain mapped exactly once.

---

## Phase 5: User Story 3 - Review Polished Enterprise Template Pages (Priority: P2)

**Goal**: The six enterprise template pages read as credible Ant-styled workflow examples with
clear hierarchy, alignment, primary action, and realistic density.

**Independent Test**: Capture the six template pages and verify that a first-time viewer can
identify page purpose, primary action, main content, and supporting content.

### Tests for User Story 3

> Write these tests first and confirm they fail before implementation.

- [X] T029 [P] [US3] Create failing template hierarchy, primary action, form alignment, result balance, exception recovery, and catalog-only composition tests in samples/AntShowcase/AntShowcase.Tests/VisualTemplateTests.fs
- [X] T030 [US3] Wire VisualTemplateTests.fs into the compile order before Main.fs in samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj

### Implementation for User Story 3

- [X] T031 [US3] After T029-T030 are observed failing, recompose the workbench, list, detail, and form templates with clear page title, primary action, main content, supporting content, and realistic density in samples/AntShowcase/AntShowcase.Core/Templates.fs
- [X] T032 [US3] Recompose the result and exception templates with balanced outcome message, recovery action, supporting details, and bounded content regions in samples/AntShowcase/AntShowcase.Core/Templates.fs
- [X] T033 [US3] Add deterministic template content for workbench rows, list rows, detail facts, form defaults, result details, and exception recovery hints in samples/AntShowcase/AntShowcase.Core/DemoState.fs
- [X] T034 [US3] Extend existing template tests for catalog-only composition and form validation after the visual template rewrite in samples/AntShowcase/AntShowcase.Tests/TemplateTests.fs
- [X] T035 [US3] Run the VisualTemplate and Template tests and record command, status, and output location in specs/162-enhance-showcase-visuals/readiness/package-feed.md

**Checkpoint**: User Story 3 is independently testable through VisualTemplate/Template tests and
all six template pages remain reachable.

---

## Phase 6: User Story 4 - Validate Visual Readiness With Complete Screenshot Evidence (Priority: P2)

**Goal**: Maintainers can run a visual evidence pass that captures every page in both Ant themes,
checks screenshot completeness, publishes contact sheets, and blocks readiness until reviewer
defect classification is present and non-critical.

**Independent Test**: Run the visual evidence pass twice with the same inputs and verify complete
per-page screenshots, contact sheets, automated completeness results, and reviewer-defect gating.

### Tests for User Story 4

> Write these tests first and confirm they fail before implementation.

- [X] T036 [P] [US4] Create failing visual-readiness pure workflow transition, app-edge interpreter, CLI parsing, output-tree, screenshot matrix, completeness, contact-sheet, stale-screenshot cleanup, degraded-capture disclosure, reviewer-rubric template, missing-classification blocking, and critical-defect blocking tests in samples/AntShowcase/AntShowcase.Tests/VisualEvidenceTests.fs
- [X] T037 [US4] Wire VisualEvidenceTests.fs into the compile order before Main.fs in samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj

### Implementation for User Story 4

- [X] T038 [US4] After T036-T037 are observed failing, add visual screenshot, completeness, defect classification, contact sheet, and readiness summary record serializers in samples/AntShowcase/AntShowcase.Core/Evidence.fsi and samples/AntShowcase/AntShowcase.Core/Evidence.fs, with Evidence.fsi immediately before Evidence.fs in samples/AntShowcase/AntShowcase.Core/AntShowcase.Core.fsproj
- [X] T039 [US4] Implement pure `visual-readiness` Model, Msg, Effect, init, update, canonical theme alias resolution, theme/page expansion, and environment-limited status decisions in samples/AntShowcase/AntShowcase.Core/VisualReadinessWorkflow.fs
- [X] T040 [US4] Implement `visual-readiness --seed --size --themes --pages --out` parsing, workflow effect interpretation, per-page per-theme screenshot capture at the requested size using Viewer.captureScreenshotEvidence, and stale file cleanup in samples/AntShowcase/AntShowcase.App/VisualReadiness.fs
- [X] T041 [US4] Implement automated screenshot completeness validation for missing, degraded, stale, undecodable, and wrong-size images in samples/AntShowcase/AntShowcase.App/VisualReadiness.fs
- [X] T042 [US4] Implement per-theme contact sheet generation and stable page ordering for visual evidence in samples/AntShowcase/AntShowcase.App/VisualReadiness.fs
- [X] T043 [US4] Implement reviewer-defects.md rubric template generation, reviewer defect parsing, severity/class readiness blocking, and lower-level limitation reporting in samples/AntShowcase/AntShowcase.App/VisualReadiness.fs
- [X] T044 [US4] Add `visual-readiness` and `visual-readiness --summarize` usage, dispatch, invalid-argument handling, and exit-code behavior in samples/AntShowcase/AntShowcase.App/Program.fs
- [X] T045 [US4] Run the VisualEvidence and Degrade tests and record command, status, and output location in specs/162-enhance-showcase-visuals/readiness/package-feed.md

**Checkpoint**: User Story 4 is independently testable through VisualEvidence/Degrade tests and
cannot mark visual readiness accepted from incomplete screenshots or missing reviewer review.

---

## Phase 7: User Story 5 - Keep the Showcase Usable Across Accepted Sizes and Themes (Priority: P3)

**Goal**: The showcase remains readable at the preferred and minimum accepted sizes, and both Ant
light and Ant dark paint complete intentional shell and content surfaces.

**Independent Test**: Capture representative dense and template pages at 1600x1000 and 1280x800 in
both themes and verify content containment, scrolling/responsive behavior, and complete surfaces.

### Tests for User Story 5

> Write these tests first and confirm they fail before implementation.

- [X] T046 [P] [US5] Create failing preferred-size, minimum-size representative, canonical antLight/antDark surface, CLI light/dark alias, theme-state preservation, and no-theme-fork tests in samples/AntShowcase/AntShowcase.Tests/VisualReadinessTests.fs
- [X] T047 [US5] Wire VisualReadinessTests.fs into the compile order before Main.fs in samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj

### Implementation for User Story 5

- [X] T048 [US5] After T046-T047 are observed failing, finalize accepted size declarations, minimum representative page ids, canonical supported theme ids, CLI alias mapping, and screenshot-count derivation in samples/AntShowcase/AntShowcase.Core/VisualConfig.fs
- [X] T049 [US5] Ensure canonical antLight and antDark shell/content surface roles are fully painted and never expose large unplanned black regions in samples/AntShowcase/AntShowcase.Core/Shell.fs
- [X] T050 [US5] Implement minimum-size overflow, scrolling, wrapping, and responsive section behavior for dense and large pages in samples/AntShowcase/AntShowcase.Core/PageProfiles.fs
- [X] T051 [US5] Extend existing theme invariance tests for current-page preservation, page-state preservation, and no per-theme control forks at accepted sizes in samples/AntShowcase/AntShowcase.Tests/ThemeInvarianceTests.fs
- [X] T052 [US5] Run the VisualReadiness and ThemeInvariance tests and record command, status, and output location in specs/162-enhance-showcase-visuals/readiness/package-feed.md

**Checkpoint**: User Story 5 is independently testable through VisualReadiness/ThemeInvariance
tests and documents both accepted sizes.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Refresh package evidence, capture visual artifacts, record reviewer classification,
and close the readiness package.

- [X] T053 Run `dotnet build FS.GG.Rendering.slnx -c Release --no-restore` and record command, status, and output location in specs/162-enhance-showcase-visuals/readiness/package-feed.md
- [X] T054 Run `dotnet pack FS.GG.Rendering.slnx -c Release --no-build`, copy packed FS.GG.UI packages to the local feed, clear global packages, and record the result in specs/162-enhance-showcase-visuals/readiness/package-feed.md
- [X] T055 Run AntShowcase build, list, and coverage commands from quickstart.md and record page count, catalog count, and coverage status in specs/162-enhance-showcase-visuals/readiness/package-feed.md
- [X] T056 Run `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --filter "Coverage|PageRender|ThemeInvariance|Template|Interaction|Feedback|Degrade|Visual"` and record the result in specs/162-enhance-showcase-visuals/readiness/package-feed.md
- [X] T057 Capture preferred visual evidence with `visual-readiness --seed 1 --size 1600x1000 --themes light,dark`, verify the summary resolves aliases to `antLight,antDark`, and store artifacts under specs/162-enhance-showcase-visuals/readiness/visual-evidence/
- [X] T058 Capture minimum-size representative visual evidence with `visual-readiness --seed 1 --size 1280x800 --themes light,dark --pages data-collections,charts-statistical,charts-advanced,feedback-status,tpl-form,tpl-exception`, verify the summary resolves aliases to `antLight,antDark`, and store artifacts under specs/162-enhance-showcase-visuals/readiness/minimum-size/
- [X] T059 Record reviewer defect rubric classification for every preferred-size screenshot in specs/162-enhance-showcase-visuals/readiness/visual-evidence/reviewer-defects.md, including severity, class, affected page/theme, readiness impact, reviewer, timestamp, and notes
- [X] T060 Run visual-readiness summary assembly and write final summary links in specs/162-enhance-showcase-visuals/readiness/validation-summary.md
- [X] T061 Record no public package surface changed, or document any lower-level `.fsi` and surface-baseline changes, in specs/162-enhance-showcase-visuals/readiness/compatibility-ledger.md
- [X] T062 Record existing AntShowcase coverage, determinism, interaction, feedback, template, theme-invariance, and Feature143/144/145 overlay regression results in specs/162-enhance-showcase-visuals/readiness/regression-validation.md
- [X] T063 Run `dotnet test FS.GG.Rendering.slnx --no-restore` and record full validation command, status, duration, and output location in specs/162-enhance-showcase-visuals/readiness/full-validation/validation.md
- [X] T064 Update usage, accepted size, visual-readiness command, and evidence honesty documentation in samples/AntShowcase/README.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **US1 Shell Collisions (Phase 3)**: Depends on Foundational. This is the MVP.
- **US2 Catalog Readability (Phase 4)**: Depends on Foundational and benefits from US1 shell
  containment.
- **US3 Template Polish (Phase 5)**: Depends on Foundational and benefits from US1 shell
  containment.
- **US4 Visual Evidence (Phase 6)**: Depends on Foundational; final accepted evidence depends on
  US1-US3 visual fixes.
- **US5 Sizes and Themes (Phase 7)**: Depends on Foundational; final status depends on US1 and US2
  containment behavior.
- **Polish (Phase 8)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Start after Foundational. No dependency on other stories.
- **US2 (P1)**: Start after Foundational. Can begin in parallel with US1 if edits to Pages.fs and
  Shell.fs are coordinated.
- **US3 (P2)**: Start after Foundational. Can begin in parallel with US2 because it primarily
  touches Templates.fs and DemoState.fs.
- **US4 (P2)**: Start after Foundational for CLI and evidence infrastructure; acceptance evidence
  should be captured after US1-US3.
- **US5 (P3)**: Start after Foundational for size/theme tests; final behavior should be validated
  after US1-US4.

### Within Each User Story

- Tests must be written first and observed failing before implementation.
- Shared records and pure decisions before App-edge filesystem or screenshot effects.
- Core visual composition before screenshot evidence capture.
- Story-specific focused tests before moving to the next priority.
- Readiness evidence publication after focused tests pass.

### Parallel Opportunities

- T002-T006 can run in parallel after T001.
- T007-T009 and T011 can run in parallel because they create different files.
- T014, T021, T029, T036, and T046 can be drafted in parallel after Foundational if test project
  compile-order edits are coordinated.
- US2 and US3 can proceed in parallel after US1 shell region contracts are stable.
- T053-T056 can run after implementation, and T057-T058 can run in parallel on a capable screenshot
  host if output directories are separate.

---

## Parallel Example: User Story 1

```text
Task: "Create failing shell-region, navigation-label containment, content-region containment, feedback/status containment, and all-page preferred-size render tests in samples/AntShowcase/AntShowcase.Tests/VisualShellTests.fs"
Task: "Update existing page render assertions to render every catalog and template page at 1600x1000 through the full shell in samples/AntShowcase/AntShowcase.Tests/PageRenderTests.fs"
```

## Parallel Example: User Story 2

```text
Task: "Create failing catalog-section readability, page-profile coverage, large-region allocation, transient-baseline policy, and live-catalog bijection tests in samples/AntShowcase/AntShowcase.Tests/VisualPageTests.fs"
Task: "Add readable missing-asset placeholders and deterministic representative demo values for visual pages in samples/AntShowcase/AntShowcase.Core/DemoState.fs"
```

## Parallel Example: User Story 3

```text
Task: "Create failing template hierarchy, primary action, form alignment, result balance, exception recovery, and catalog-only composition tests in samples/AntShowcase/AntShowcase.Tests/VisualTemplateTests.fs"
Task: "Add deterministic template content for workbench rows, list rows, detail facts, form defaults, result details, and exception recovery hints in samples/AntShowcase/AntShowcase.Core/DemoState.fs"
```

## Parallel Example: User Story 4

```text
Task: "Create failing visual-readiness pure workflow transition, app-edge interpreter, CLI parsing, output-tree, screenshot matrix, completeness, contact-sheet, stale-screenshot cleanup, degraded-capture disclosure, reviewer-rubric template, missing-classification blocking, and critical-defect blocking tests in samples/AntShowcase/AntShowcase.Tests/VisualEvidenceTests.fs"
Task: "Add visual-readiness and visual-readiness --summarize usage, dispatch, invalid-argument handling, and exit-code behavior in samples/AntShowcase/AntShowcase.App/Program.fs"
```

## Parallel Example: User Story 5

```text
Task: "Create failing preferred-size, minimum-size representative, canonical antLight/antDark surface, CLI light/dark alias, theme-state preservation, and no-theme-fork tests in samples/AntShowcase/AntShowcase.Tests/VisualReadinessTests.fs"
Task: "Extend existing theme invariance tests for current-page preservation, page-state preservation, and no per-theme control forks at accepted sizes in samples/AntShowcase/AntShowcase.Tests/ThemeInvarianceTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundational visual configuration and project-file entries.
3. Complete Phase 3 US1 shell containment tests and implementation.
4. Validate US1 with the focused VisualShell/PageRender tests.
5. Stop and review the all-page shell containment result before catalog and evidence work.

### Incremental Delivery

1. Add US1 shell containment and verify every page has separated shell regions.
2. Add US2 catalog readability and verify catalog coverage remains a clean bijection.
3. Add US3 enterprise templates and verify all six templates remain catalog-composed and readable.
4. Add US4 visual-readiness evidence and verify completeness plus reviewer gating.
5. Add US5 accepted-size/theme hardening and verify preferred/minimum behavior.
6. Complete Phase 8 readiness capture and full validation.

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup and Foundational together.
2. After Foundational:
   - Developer A: US1 shell containment.
   - Developer B: US2 catalog readability after ShellLayout contracts stabilize.
   - Developer C: US3 template polish.
   - Developer D: US4 evidence command and US5 size/theme tests.
3. Each story lands only after its focused tests and readiness notes are current.

## Notes

- `[P]` tasks touch different files and can run in parallel when compile-order edits are
  coordinated.
- `[US#]` labels map tasks to spec user stories.
- Tests are included because the feature spec and constitution require test evidence.
- Avoid public package changes unless a lower-level visual defect cannot be fixed in the sample.
- New reusable sample modules introduced by this feature need companion `.fsi` signatures before
  `.fs` implementation tasks.
- If a lower-level package change becomes necessary, add `.fsi`, semantic tests, surface baseline,
  compatibility, and package validation tasks before implementing the `.fs` body.
