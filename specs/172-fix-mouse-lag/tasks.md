# Tasks: Fix Mouse Interaction Lag

**Input**: Design documents from `/specs/172-fix-mouse-lag/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/responsiveness-evidence.md, quickstart.md

**Tests**: Included. The feature specification and constitution require automated semantic tests plus accepted visible-session evidence for this Tier 1 observable behavior change.

**Organization**: Tasks are grouped by user story so pointer responsiveness, accepted evidence, and regression preservation can be implemented and verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and does not depend on an incomplete task
- **[Story]**: User story label for story-specific tasks only
- Every task names the exact file or directory it changes or records evidence into

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare feature-local evidence locations and capture the current baseline before implementation.

- [X] T001 Create the readiness directory skeleton under `specs/172-fix-mouse-lag/readiness/` with `responsiveness/`, `visual-preferred/`, `visual-minimum/`, and `logs/` subdirectories
- [X] T002 [P] Capture the current responsiveness CLI behavior in `specs/172-fix-mouse-lag/readiness/baseline-responsiveness.md` by running the existing `samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj` responsiveness command
- [X] T003 [P] Capture the pre-change automated test baseline in `specs/172-fix-mouse-lag/readiness/baseline-tests.md` for `tests/Controls.Tests/Controls.Tests.fsproj`, `tests/Elmish.Tests/Elmish.Tests.fsproj`, `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj`, and `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared action/evidence contracts and readiness notes used by all user stories.

**Critical**: No user story work should begin until this phase is complete.

- [X] T004 Update the interaction review contract surface in `samples/SecondAntShowcase/SecondAntShowcase.Core/InteractionContracts.fsi` with action type, input kind, and stable display-only exclusion fields needed by all-interactive responsiveness planning
- [X] T005 Implement the interaction review contract metadata in `samples/SecondAntShowcase/SecondAntShowcase.Core/InteractionContracts.fs` for every entry in `InteractionContracts.all`
- [X] T006 [P] Add responsiveness review session, action, evidence record, summary, and visual-regression evidence types to `samples/SecondAntShowcase/SecondAntShowcase.Core/Evidence.fsi`
- [X] T007 Implement the responsiveness evidence JSON/Markdown helpers and the 100 ms p95 / 150 ms max budget constants in `samples/SecondAntShowcase/SecondAntShowcase.Core/Evidence.fs`
- [X] T008 [P] Document the intended Tier 1 surface impact and any no-breaking-API decision in `specs/172-fix-mouse-lag/readiness/package-surface.md`
- [X] T009 [P] Add shared responsiveness test fixtures for temp directories, JSON parsing, and interaction action lookup in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172ResponsivenessFixtures.fs`
- [X] T010 Register the shared responsiveness fixture file in `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`

**Checkpoint**: Shared contracts, fixtures, and readiness notes exist; user story implementation can begin.

---

## Phase 3: User Story 1 - Pointer actions feel immediate in the showcase (Priority: P1)

**Goal**: Representative clicks, navigation actions, selections, overlays, and value-changing drags produce visible feedback quickly enough that the live showcase no longer feels laggy.

**Independent Test**: Run the US1 automated tests, then open the showcase in a visible desktop session and confirm representative mouse actions visibly respond within the 100 ms / 150 ms target.

### Tests for User Story 1

- [X] T011 [P] [US1] Add failing viewer queue tests for discrete pointer priority, move coalescing, queue-depth recording, and first presented-frame timing in `tests/SkiaViewer.Tests/Feature172PointerQueueTests.fs`
- [X] T012 [P] [US1] Register the viewer queue test file in `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj`
- [X] T013 [P] [US1] Add failing Elmish retained-routing tests for click, drag, navigation, adjacent controls, nested controls, dense controls, and outside/re-enter drag behavior in `tests/Elmish.Tests/Feature172InteractiveResponsivenessTests.fs`
- [X] T014 [P] [US1] Register the Elmish retained-routing test file in `tests/Elmish.Tests/Elmish.Tests.fsproj`
- [X] T015 [P] [US1] Add failing sample tests proving representative pointer actions exist for every interactive family in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172PointerActionTests.fs`
- [X] T016 [US1] Register the sample pointer action test file in `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`

### Implementation for User Story 1

- [X] T017 [US1] If the viewer public timing or queue surface changes, update `src/SkiaViewer/SkiaViewer.fsi` before changing `src/SkiaViewer/SkiaViewer.fs`
- [X] T018 [US1] Fix the viewer input drain and latency hot path in `src/SkiaViewer/SkiaViewer.fs` so discrete pointer inputs drain before non-input ticks, coalesced movement is accounted for, and the first visible presented frame is timed; if the public surface changed, refresh `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt`
- [X] T019 [US1] If the adapter public timing contribution or retained-routing surface changes, update `src/Controls.Elmish/ControlsElmish.fsi` before changing `src/Controls.Elmish/ControlsElmish.fs`
- [X] T020 [US1] Fix retained pointer routing and same-frame state application in `src/Controls.Elmish/ControlsElmish.fs` so clicked, dragged, adjacent, nested, and dense controls update before catch-up frames; if the public surface changed, refresh `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt`
- [X] T021 [US1] Extend representative pointer script generation in `samples/SecondAntShowcase/SecondAntShowcase.Core/Scripts.fs` for click, navigate, select, open-close, and drag/value-change actions derived from `InteractionContracts.all`
- [X] T022 [US1] Run the US1 validation commands and record results in `specs/172-fix-mouse-lag/readiness/us1-pointer-responsiveness.md` for `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj`, `tests/Elmish.Tests/Elmish.Tests.fsproj`, and `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`

**Checkpoint**: Pointer interactions are functional and testable independently of the accepted evidence packaging.

---

## Phase 4: User Story 2 - Responsiveness evidence is accepted, not inferred (Priority: P2)

**Goal**: The responsiveness command produces a visible-session evidence package with machine-readable records, summaries, coverage, budgets, and fail-closed behavior.

**Independent Test**: Run the documented light and dark `responsiveness --all-interactive --require-live --json` commands in a visible session and inspect `summary.json`, `summary.md`, `records.jsonl`, and `environment.md`.

### Tests for User Story 2

- [X] T023 [P] [US2] Add failing CLI parser and contract tests for `--all-interactive`, `--page`, mutual exclusion, `--json`, and exit codes 0/2/3/4/5 in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172ResponsivenessCliContractTests.fs`
- [X] T024 [P] [US2] Add failing evidence shape tests for `records.jsonl`, `summary.json`, budgets, coverage, relative paths, and sample review fields in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172ResponsivenessEvidenceTests.fs`
- [X] T025 [P] [US2] Add failing fail-closed tests for no visible surface, hidden visible session, throttled visible session, missing presentation boundary, headless substitute, low-precision timestamp, and write failure in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172ResponsivenessFailClosedTests.fs`
- [X] T026 [US2] Register the US2 responsiveness test files in `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`

### Implementation for User Story 2

- [X] T027 [US2] Extend `Request` parsing in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs` to support `--all-interactive`, keep `--page <page-id>`, reject invalid combinations with exit code 2, and preserve `--require-live` and `--json`
- [X] T028 [US2] Build the responsiveness action plan from `InteractionContracts.all` and display-only reasons in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T029 [US2] Add the live visible-session measurement runner in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs` using the viewer responsiveness sink and a measured presentation boundary
- [X] T030 [US2] Write records, summaries, environment diagnostics, and display-only exclusions through `samples/SecondAntShowcase/SecondAntShowcase.Core/Evidence.fs` and `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T031 [US2] Enforce acceptance budgets, all-family coverage, continuous drag acceptance, hidden/throttled/unreliable-session environment limitations, and exit codes in `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`
- [X] T032 [US2] Update responsiveness command usage text in `samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs`
- [X] T033 [US2] Update maintainer-facing sample instructions for the responsiveness command in `samples/SecondAntShowcase/README.md`
- [X] T034 [US2] Run the US2 CLI/evidence tests and record results in `specs/172-fix-mouse-lag/readiness/us2-responsiveness-evidence.md` for `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`
- [X] T035 [US2] Run light and dark visible responsiveness evidence commands and write accepted or blocked artifacts under `specs/172-fix-mouse-lag/readiness/responsiveness/`

**Checkpoint**: Responsiveness is accepted only by measured visible-session evidence or reported as blocked/environment-limited.

---

## Phase 5: User Story 3 - Existing showcase fixes remain intact (Priority: P3)

**Goal**: The mouse-lag fix does not regress opaque backgrounds, Ant-like navigation, mapped-control coverage, visual readiness, or slider click/drag behavior.

**Independent Test**: Run visual readiness, review findings, coverage, slider interaction tests, and sample package-consuming tests after US1/US2 changes.

### Tests for User Story 3

- [X] T036 [P] [US3] Add failing visual regression tests for opaque backgrounds and Ant-like navigation rail status in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172VisualRegressionTests.fs`
- [X] T037 [P] [US3] Add failing slider click/drag regression tests for value-changing pointer behavior in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172SliderRegressionTests.fs`
- [X] T038 [P] [US3] Add failing coverage regression tests for mapped controls, interactive-family coverage, and display-only exclusions in `samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172CoverageRegressionTests.fs`
- [X] T039 [US3] Register the US3 regression test files in `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`

### Implementation for User Story 3

- [X] T040 [US3] Preserve visual-readiness alpha, navigation, contact-sheet, and reviewer finding statuses in `samples/SecondAntShowcase/SecondAntShowcase.App/VisualReadiness.fs`
- [X] T041 [US3] Preserve slider, rating, and progress click/drag state updates in `samples/SecondAntShowcase/SecondAntShowcase.Core/DemoState.fs` and visible rendering in `samples/SecondAntShowcase/SecondAntShowcase.Core/Pages.fs`
- [X] T042 [US3] Preserve all-page mapping and explicit display-only exclusions in `samples/SecondAntShowcase/SecondAntShowcase.Core/CoverageMap.fs` and `samples/SecondAntShowcase/SecondAntShowcase.Core/InteractionContracts.fs`
- [X] T043 [US3] Run visual-readiness preferred/minimum and review-findings commands and record results in `specs/172-fix-mouse-lag/readiness/us3-visual-regressions.md`
- [X] T044 [US3] Run the US3 sample regression tests and record results in `specs/172-fix-mouse-lag/readiness/us3-sample-regressions.md` for `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`

**Checkpoint**: Prior showcase fixes remain intact while the responsiveness fix is present.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final documentation, package-surface validation, package-consuming sample refresh, and live review closeout.

- [X] T045 [P] Update final validation command details and exit-code notes in `specs/172-fix-mouse-lag/quickstart.md`
- [X] T046 [P] Refresh final package-surface notes and copied baselines in `specs/172-fix-mouse-lag/readiness/package-surface.md` after running `scripts/refresh-surface-baselines.fsx`
- [X] T047 [P] Write the feature closeout report with accepted, blocked, or failed evidence links in `specs/172-fix-mouse-lag/readiness/closeout-report.md`
- [X] T048 Run the full automated validation package and record results in `specs/172-fix-mouse-lag/readiness/full-validation.md` for `tests/Controls.Tests/Controls.Tests.fsproj`, `tests/Elmish.Tests/Elmish.Tests.fsproj`, `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj`, and `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`
- [X] T049 Refresh the local package feed, rebuild the package-consuming sample, and record results in `specs/172-fix-mouse-lag/readiness/package-consuming-sample.md` for `scripts/refresh-local-feed-and-samples.fsx` and `samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj`
- [X] T050 Review final `summary.json` and `summary.md` outputs and record budget/coverage acceptance in `specs/172-fix-mouse-lag/readiness/responsiveness/summary-review.md`
- [X] T051 Perform the final visible manual pointer review and record whether unchanged lag is resolved in `specs/172-fix-mouse-lag/readiness/manual-review.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion; blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational; recommended MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational and benefits from US1 timing fixes for accepted evidence.
- **User Story 3 (Phase 5)**: Depends on Foundational; can start in parallel with US1/US2 tests, but final regression validation should run after US1 and US2 implementation.
- **Polish (Phase 6)**: Depends on the desired story set being complete.

### User Story Dependencies

- **US1 (P1)**: No dependency on US2/US3; delivers the primary interaction fix.
- **US2 (P2)**: Depends on US1 for accepted timing when live evidence is collected, but its parser, artifact shape, and fail-closed tests can start after Foundation.
- **US3 (P3)**: Regression-focused; can begin after Foundation and must be rerun after US1/US2.

### Within Each User Story

- Write failing tests before implementation.
- Update `.fsi` before `.fs` when a public or sample-core surface changes.
- Implement framework hot-path changes before sample evidence acceptance.
- Record evidence before marking a story checkpoint complete.

---

## Parallel Opportunities

- Setup tasks T002 and T003 can run in parallel after T001 creates the readiness root.
- Foundational tasks T006, T008, and T009 can run in parallel with T004/T005 because they touch different files.
- US1 tests T011, T013, and T015 can be authored in parallel; project-file registration tasks should be serialized by project.
- US2 tests T023, T024, and T025 can be authored in parallel before parser/evidence implementation.
- US3 tests T036, T037, and T038 can be authored in parallel before the regression-preservation implementation.
- Polish tasks T045, T046, and T047 can run in parallel after story implementation is stable.

---

## Parallel Example: User Story 1

```bash
# Author independent failing tests in parallel:
Task: "T011 Add failing viewer queue tests in tests/SkiaViewer.Tests/Feature172PointerQueueTests.fs"
Task: "T013 Add failing Elmish retained-routing tests in tests/Elmish.Tests/Feature172InteractiveResponsivenessTests.fs"
Task: "T015 Add failing sample pointer action tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172PointerActionTests.fs"

# Then implement in dependency order:
Task: "T017 Update src/SkiaViewer/SkiaViewer.fsi first if the viewer public timing or queue surface changes"
Task: "T018 Fix viewer input drain and latency hot path in src/SkiaViewer/SkiaViewer.fs"
Task: "T019 Update src/Controls.Elmish/ControlsElmish.fsi first if the adapter public timing or retained-routing surface changes"
Task: "T020 Fix retained pointer routing and same-frame state application in src/Controls.Elmish/ControlsElmish.fs"
Task: "T021 Extend representative pointer script generation in samples/SecondAntShowcase/SecondAntShowcase.Core/Scripts.fs"
```

## Parallel Example: User Story 2

```bash
# Author independent failing evidence tests in parallel:
Task: "T023 Add CLI parser and contract tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172ResponsivenessCliContractTests.fs"
Task: "T024 Add evidence shape tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172ResponsivenessEvidenceTests.fs"
Task: "T025 Add fail-closed tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172ResponsivenessFailClosedTests.fs"

# Then implement parser, action planning, live measurement, and artifact writing in Responsiveness.fs/Evidence.fs.
```

## Parallel Example: User Story 3

```bash
# Author independent regression tests in parallel:
Task: "T036 Add visual regression tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172VisualRegressionTests.fs"
Task: "T037 Add slider click/drag regression tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172SliderRegressionTests.fs"
Task: "T038 Add coverage regression tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature172CoverageRegressionTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 through T022.
3. Stop and validate that visible pointer interactions no longer feel delayed.
4. Proceed to US2 only after the hot path is demonstrably responsive.

### Incremental Delivery

1. Setup + Foundation establish shared contracts and fixtures.
2. US1 fixes the live pointer interaction path.
3. US2 converts the result into accepted visible-session evidence.
4. US3 proves previous showcase fixes stayed intact.
5. Polish records the final evidence and package-consuming validation.

### Validation Commands

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase
dotnet nuget locals global-packages --clear
dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release --no-incremental
dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme light --all-interactive --require-live --out specs/172-fix-mouse-lag/readiness/responsiveness --json
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme dark --all-interactive --require-live --out specs/172-fix-mouse-lag/readiness/responsiveness --json
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out specs/172-fix-mouse-lag/readiness/visual-preferred
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --out specs/172-fix-mouse-lag/readiness/visual-minimum
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- review-findings --out specs/172-fix-mouse-lag/readiness --fail-on-unresolved
```

---

## Notes

- Public framework surface changes must update `.fsi` first and refresh matching files under `tests/surface-baselines/`.
- Headless deterministic responsiveness output is substitute evidence only; it can guard shape but cannot accept the feature.
- Display-only controls must be explicitly excluded with reasons and must not count as failed interactions.
- Exit code 4 means blocked or environment-limited evidence; exit code 5 means measured evidence failed a responsiveness budget.
