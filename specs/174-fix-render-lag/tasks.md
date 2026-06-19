# Tasks: Fix Render Lag

**Input**: Design documents from `/specs/174-fix-render-lag/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md, contracts/

**Tests**: Included because FR-008 and FR-009 require automated regression and parity coverage.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested independently after the shared foundation is in place.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and does not depend on another incomplete task.
- **[Story]**: User-story label for story phases only.
- Every task names the exact file path or artifact path to edit or validate.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the feature evidence locations, baseline record, and validation entry point before implementation.

- [X] T001 Create the Feature 174 baseline record with exact 2026-06-19 button-click, page-change, and first-frame preparation values, source trace paths, scenario IDs, measurement provenance, and stale-baseline validation notes in specs/174-fix-render-lag/readiness/render-lag/baseline-2026-06-19.md
- [X] T002 Create the readiness validation summary skeleton with required command, artifact, caveat, and status sections in specs/174-fix-render-lag/readiness/validation-summary.md
- [X] T003 [P] Add retained-render shared test fixtures for button-click, page-change, dense content, and metadata parity assertions in tests/Controls.Tests/Feature174RetainedRenderFixtures.fs
- [X] T004 [P] Add SecondAntShowcase shared evidence fixtures for render-lag, responsiveness, and environment-limited assertions in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature174RenderLagFixtures.fs
- [X] T005 Register Feature174RetainedRenderFixtures.fs before Feature 174 controls tests in tests/Controls.Tests/Controls.Tests.fsproj
- [X] T006 Register Feature174RenderLagFixtures.fs before Feature 174 sample tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the internal measurement vocabulary and no-op-safe plumbing that all stories depend on.

**CRITICAL**: No user story implementation should start until this phase is complete.

- [X] T007 Extend the internal retained render signature with metadata work counters and reusable metadata state in src/Controls/RetainedRender.fsi
- [X] T008 [P] Add failing retained metadata work-counter tests for MetadataVisitedNodeCount, BaselineNodeCount, fallback count, reusable metadata state, zero-compatible defaults, and first-frame/shared-bottleneck coverage in tests/Controls.Tests/Feature174RetainedRenderWorkTests.fs
- [X] T009 [P] Add failing frame phase attribution tests for retained-step, paint, compose, and existing public FrameMetrics shape in tests/Elmish.Tests/Feature174FramePhaseTests.fs
- [X] T010 Register Feature174RetainedRenderWorkTests.fs before Feature 174 retained-render implementation tests in tests/Controls.Tests/Controls.Tests.fsproj
- [X] T011 Register Feature174FramePhaseTests.fs before Feature 174 Elmish implementation tests in tests/Elmish.Tests/Elmish.Tests.fsproj
- [X] T012 Add regression coverage for unchanged public surface baselines and no new package dependency assumptions in tests/Controls.Tests/PublicSurfaceTests.fs
- [X] T013 Mirror the new internal retained metadata work counters with zero-compatible defaults for retained-step and shared first-frame preparation paths in src/Controls/RetainedRender.fs
- [X] T014 Preserve existing public FrameMetrics shape while mapping retained-step, paint, and compose timing into existing responsiveness timing helpers in src/Controls.Elmish/ControlsElmish.fs

**Checkpoint**: Internal metadata work records are available, existing public surfaces remain unchanged, and story tests can be added.

---

## Phase 3: User Story 1 - Immediate Control Feedback (Priority: P1) MVP

**Goal**: Button activation and similar small interactions show the follow-up visual frame without visible pause while preserving output and routing.

**Independent Test**: Run the representative button activation scenario after the first frame is visible and verify latency budget, phase attribution, retained work scaling, and visual/interaction parity.

### Tests for User Story 1

Write these tests first and confirm they fail before implementation.

- [X] T015 [P] [US1] Add button-click changed-subtree bounds assertions to the retained work-scaling tests in tests/Controls.Tests/Feature174RetainedRenderWorkTests.fs
- [X] T016 [P] [US1] Add button activation visual, event binding, bound id, diagnostics, hit-test, and accessibility metadata parity tests in tests/Controls.Tests/Feature174RetainedRenderParityTests.fs
- [X] T017 [P] [US1] Add button-click model-changing frame cases to the phase attribution tests in tests/Elmish.Tests/Feature174FramePhaseTests.fs
- [X] T018 [US1] Register Feature174RetainedRenderParityTests.fs before Program.fs in tests/Controls.Tests/Controls.Tests.fsproj

### Implementation for User Story 1

- [X] T019 [US1] Replace repeated full-tree metadata collection for localized retained steps and any shared first-frame preparation bottleneck with retained-node metadata reuse in src/Controls/RetainedRender.fs
- [X] T020 [US1] Count metadata visited nodes, metadata fallback count, recomputed nodes, remeasured nodes, and repainted nodes for button-click retained steps in src/Controls/RetainedRender.fs
- [X] T021 [US1] Ensure retained pointer/key routing consumes reused bounds, event bindings, and bound ids without falling back to full render on normal button-click paths in src/Controls.Elmish/ControlsElmish.fs
- [X] T022 [US1] Update render-lag probe trace output for button-click to expose frame preparation subphase names without changing CLI arguments in samples/SecondAntShowcase/SecondAntShowcase.App/RenderLagProbe.fs

**Checkpoint**: User Story 1 is independently testable with retained work-scaling tests, parity tests, and the button-click render-lag probe.

---

## Phase 4: User Story 2 - Fast Page Navigation (Priority: P2)

**Goal**: Navigation to dense showcase pages completes within budget while stable shell chrome and unchanged retained regions avoid repeated full-scene preparation.

**Independent Test**: Run the representative page-change scenario to text-numeric-input and verify navigation latency budget, retained work scaling, and parity of the destination page.

### Tests for User Story 2

Write these tests first and confirm they fail before implementation.

- [X] T023 [P] [US2] Add page-change retained work-scaling tests for stable shell chrome, unchanged regions, and destination content bounds in tests/Controls.Tests/Feature174PageNavigationWorkTests.fs
- [X] T024 [P] [US2] Add retained parity tests for dense nested content, theme changes, animations, overlays, scroll viewers, data-rich controls, and no-replay-cache pages in tests/Controls.Tests/Feature174PageNavigationParityTests.fs
- [X] T025 [P] [US2] Add SecondAntShowcase page-change visual and interaction parity tests for navigation to text-numeric-input in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature174VisualParityTests.fs
- [X] T026 [US2] Register Feature174PageNavigationWorkTests.fs and Feature174PageNavigationParityTests.fs before Program.fs in tests/Controls.Tests/Controls.Tests.fsproj
- [X] T027 [US2] Register Feature174VisualParityTests.fs before Main.fs in samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj

### Implementation for User Story 2

- [X] T028 [US2] Reuse retained metadata for unchanged shell chrome and retained page regions during page-change retained steps in src/Controls/RetainedRender.fs
- [X] T029 [US2] Guard metadata reuse invalidation for theme, layout, text proof, modifier layer, explicit identity, child ordering, child insertion/removal, and cache boundaries in src/Controls/RetainedRender.fs
- [X] T030 [US2] Keep page-change scenario selection fixed on buttons to text-numeric-input and record destination page facts in samples/SecondAntShowcase/SecondAntShowcase.App/RenderLagProbe.fs
- [X] T031 [US2] Preserve page registry and script behavior for the text-numeric-input navigation path in samples/SecondAntShowcase/SecondAntShowcase.Core/Scripts.fs

**Checkpoint**: User Story 2 is independently testable with retained work-scaling tests, parity tests, and the page-change render-lag probe.

---

## Phase 5: User Story 3 - Reliable Performance Regression Evidence (Priority: P3)

**Goal**: Maintainers get repeatable evidence that reports budgets, phase attribution, parity, baseline comparison, and explicit environment limitations.

**Independent Test**: Run the performance validation workflow for button-click and page-change and verify pass, fail, or environment-limited output for every required scenario.

### Tests for User Story 3

Write these tests first and confirm they fail before implementation.

- [X] T032 [P] [US3] Add render-lag probe artifact contract tests for phase-records.jsonl, summary.json, summary.md, trace.log, stable tokens, and baseline comparison fields in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature174RenderLagProbeTests.fs
- [X] T033 [P] [US3] Add responsiveness budget tests for button-click median/p95, page-change median/p95, preparation reduction percent, first-frame preparation reduction percent, and parity links in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature174ResponsivenessBudgetTests.fs
- [X] T034 [P] [US3] Add fail-closed tests for headless, substitute-only, missing presentation, missing phase attribution, stale baseline, and incomplete artifact cases in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature174EnvironmentLimitedTests.fs
- [X] T035 [P] [US3] Add deterministic responsiveness regression tests for required scenario coverage and timing contribution naming in tests/Elmish.Tests/Feature174ResponsivenessRegressionTests.fs
- [X] T036 [US3] Register Feature174RenderLagProbeTests.fs, Feature174ResponsivenessBudgetTests.fs, and Feature174EnvironmentLimitedTests.fs before Main.fs in samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj
- [X] T037 [US3] Register Feature174ResponsivenessRegressionTests.fs before Program.fs in tests/Elmish.Tests/Elmish.Tests.fsproj

### Implementation for User Story 3

- [X] T038 [US3] Write render-lag phase records and summaries under specs/174-fix-render-lag/readiness/render-lag/ from samples/SecondAntShowcase/SecondAntShowcase.App/RenderLagProbe.fs
- [X] T039 [US3] Add baseline profile id, optimized profile id, preparation reduction percent, first-frame preparation reduction percent, parity status, and caveat links to responsiveness summaries in samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs
- [X] T040 [US3] Preserve Feature 173 records.jsonl, summary.json, summary.md, environment.md, exit codes, and environment-limited behavior in samples/SecondAntShowcase/SecondAntShowcase.Core/ResponsivenessWorkflow.fs
- [X] T041 [US3] Ensure Program dispatch keeps existing responsiveness, render-lag-probe, coverage, evidence, visual-readiness, and review-findings commands source-compatible in samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs

**Checkpoint**: User Story 3 is independently testable with artifact contract tests, budget tests, fail-closed tests, and deterministic scenario coverage.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate the full feature, package-consuming sample, evidence artifacts, and Tier 2 public-surface constraints.

- [X] T042 Run Release build from quickstart.md and record command, exit code, and caveats in specs/174-fix-render-lag/readiness/validation-summary.md
- [X] T043 Run focused framework regression checks from quickstart.md and record Controls, Elmish, and SkiaViewer results in specs/174-fix-render-lag/readiness/validation-summary.md
- [X] T044 Run package-consuming SecondAntShowcase sample checks from quickstart.md and record package feed and sample test results in specs/174-fix-render-lag/readiness/validation-summary.md
- [X] T045 Run headless fail-closed responsiveness validation from quickstart.md and record non-accepted environment-limited evidence in specs/174-fix-render-lag/readiness/responsiveness/<run-id>/environment.md
- [X] T046 Run visible desktop render-lag probes for button-click and page-change from quickstart.md and save traces in specs/174-fix-render-lag/readiness/render-lag/button-click.trace.log and specs/174-fix-render-lag/readiness/render-lag/page-change.trace.log
- [X] T047 Run accepted live responsiveness evidence for light and dark themes from quickstart.md and save outputs in specs/174-fix-render-lag/readiness/responsiveness/<run-id>/summary.json
- [X] T048 Run visual and interaction parity commands from quickstart.md and save outputs in specs/174-fix-render-lag/readiness/visual-parity/preferred/summary.md
- [X] T049 Run public surface check from quickstart.md and record zero-diff status for tests/surface-baselines/ in specs/174-fix-render-lag/readiness/validation-summary.md
- [X] T050 Update final readiness review with baseline values, optimized values, first-frame preparation values, run directories, trace paths, parity paths, caveats, and acceptance status in specs/174-fix-render-lag/readiness/validation-summary.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user-story implementation.
- **User Stories (Phase 3+)**: Depend on Foundational completion.
- **Polish (Phase 6)**: Depends on whichever stories are implemented; final acceptance depends on all stories.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Foundational; no dependency on US2 or US3.
- **User Story 2 (P2)**: Starts after Foundational; no dependency on US1, but final navigation evidence benefits from US1's metadata path.
- **User Story 3 (P3)**: Starts after Foundational; can develop artifact checks independently, but final accepted evidence depends on US1 and US2 performance fixes.

### Within Each User Story

- Tests are written and observed failing before implementation.
- Internal contracts and fixtures come before tests that consume them.
- Retained-render implementation comes before Elmish/sample integration.
- Story-specific probe or evidence wiring comes after the retained path reports required counters.
- Each story reaches its checkpoint before moving to the next priority unless a parallel team is working in separate files.

## Parallel Opportunities

- Setup fixture tasks T003 and T004 can run in parallel.
- Foundational failing-test tasks T008 and T009 can run in parallel after T007; T010 and T011 register them before T013 and T014 implementation work starts.
- US1 test tasks T015, T016, and T017 can run in parallel; T018 registers the parity file before parity test execution.
- US2 test tasks T023, T024, and T025 can run in parallel before T026 and T027 register them.
- US3 test tasks T032, T033, T034, and T035 can run in parallel before T036 and T037 register them.
- After T007 through T014 complete, US1, US2, and US3 test authoring can proceed in parallel by different developers.
- Polish validation tasks that only read artifacts can run in parallel after the corresponding generating commands complete.

## Parallel Example: User Story 1

```text
Task T015: Add button-click changed-subtree assertions in tests/Controls.Tests/Feature174RetainedRenderWorkTests.fs
Task T016: Add button activation parity tests in tests/Controls.Tests/Feature174RetainedRenderParityTests.fs
Task T017: Add button-click frame phase attribution cases in tests/Elmish.Tests/Feature174FramePhaseTests.fs
```

## Parallel Example: User Story 2

```text
Task T023: Add page-change retained work-scaling tests in tests/Controls.Tests/Feature174PageNavigationWorkTests.fs
Task T024: Add page-change retained parity tests in tests/Controls.Tests/Feature174PageNavigationParityTests.fs
Task T025: Add SecondAntShowcase page-change visual and interaction parity tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature174VisualParityTests.fs
```

## Parallel Example: User Story 3

```text
Task T032: Add render-lag probe artifact contract tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature174RenderLagProbeTests.fs
Task T033: Add responsiveness budget tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature174ResponsivenessBudgetTests.fs
Task T034: Add fail-closed environment-limited tests in samples/SecondAntShowcase/SecondAntShowcase.Tests/Feature174EnvironmentLimitedTests.fs
Task T035: Add deterministic responsiveness regression tests in tests/Elmish.Tests/Feature174ResponsivenessRegressionTests.fs
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 for button-click retained work scaling and parity.
3. Validate with Controls.Tests, Elmish.Tests, and the button-click render-lag probe.
4. Stop and review whether median <= 150 ms, p95 <= 250 ms, and non-paint preparation reduction >= 80% are supported by evidence.

### Incremental Delivery

1. Deliver US1 to remove the visible button activation pause.
2. Deliver US2 to cover dense page navigation and stable shell reuse.
3. Deliver US3 to harden artifacts, budgets, fail-closed reporting, and final readiness evidence.
4. Run Phase 6 validation and update validation-summary.md before closeout.

### Tier 2 Guardrails

- Public package surface remains unchanged; any public `.fsi`, package contract, dependency, or intentional behavior change requires Tier 1 reclassification before continuing.
- Environment-limited, substitute, skipped, timed-out, degraded, blocked, or manual-review-pending evidence is never summarized as accepted.
- No new external dependency is introduced for this feature.
