# Tasks: HarfBuzz Text Shaping

**Input**: Design documents from `/specs/142-harfbuzz-text-shaping/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/harfbuzz-text-shaping.md`, `quickstart.md`

**Tests**: Included. The feature specification requires automated fixture, parity, fallback, cache, retained-rendering, surface, and readiness evidence.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested independently after the shared setup and foundational phases.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other ready tasks because it touches different files and does not depend on incomplete work
- **[Story]**: Maps the task to the user story from `spec.md`
- All task descriptions include exact repository paths

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add dependency, project-file, fixture, and readiness scaffolding needed by all stories.

- [X] T001 Add `SkiaSharp.HarfBuzz` version `4.147.0-preview.3.1` to `Directory.Packages.props`
- [X] T002 Add the `SkiaSharp.HarfBuzz` package reference to `src/SkiaViewer/SkiaViewer.fsproj`
- [X] T003 [P] Add `Feature142ShapedTextTests.fs`, `Feature142RunItemizationTests.fs`, `Feature142ShapedTextDeterminismTests.fs`, and `Feature142PureFallbackCompatibilityTests.fs` compile entries to `tests/Scene.Tests/Scene.Tests.fsproj`
- [X] T004 [P] Add `Feature142HarfBuzzShapingTests.fs`, `Feature142FallbackDiagnosticsTests.fs`, and `Feature142SurfaceAndDependencyTests.fs` compile entries to `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj`
- [X] T005 [P] Add `Feature142ControlsTextShapingTests.fs` and `Feature142TextCacheParityTests.fs` compile entries to `tests/Controls.Tests/Controls.Tests.fsproj`
- [X] T006 [P] Add `Feature142TextMetricsTests.fs` compile entry to `tests/Elmish.Tests/Elmish.Tests.fsproj`
- [X] T007 [P] Add `TextShapingFixtures.fsi`, `TextShapingFixtures.fs`, `TextShapingOracle.fsi`, `TextShapingOracle.fs`, `TextShapingParity.fsi`, and `TextShapingParity.fs` compile entries to `tests/Rendering.Harness/Rendering.Harness.fsproj`
- [X] T008 [P] Add `Feature142TextFixtureCorpusTests.fs` and `Feature142BaselineLedgerTests.fs` compile entries to `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`
- [X] T009 [P] Create readiness evidence stubs in `specs/142-harfbuzz-text-shaping/readiness/validation-log.md`, `specs/142-harfbuzz-text-shaping/readiness/measure-draw-parity.md`, `specs/142-harfbuzz-text-shaping/readiness/fallback-diagnostics.md`, `specs/142-harfbuzz-text-shaping/readiness/cache-retained-parity.md`, `specs/142-harfbuzz-text-shaping/readiness/pure-fallback.md`, `specs/142-harfbuzz-text-shaping/readiness/surface-baseline.md`, `specs/142-harfbuzz-text-shaping/readiness/dependency-boundary.md`, `specs/142-harfbuzz-text-shaping/readiness/baseline-disclosure-ledger.md`, `specs/142-harfbuzz-text-shaping/readiness/package-surface.md`, and `specs/142-harfbuzz-text-shaping/readiness/scope-review.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish public signatures, fixture primitives, and contract guards before user-story implementation.

**CRITICAL**: No user story implementation begins until these `.fsi` sketches, shared test fixtures, and
FSI/prelude shape-validation transcript exist.

- [X] T010 Design the dependency-light shaped text result, run, glyph, metrics, provider evidence, fallback disclosure, and fingerprint surface in `src/Scene/Scene.fsi`
- [X] T011 [P] Design HarfBuzz provider, shaped-result builder, provider status, and fallback diagnostic signatures in `src/SkiaViewer/Fonts.fsi`
- [X] T012 [P] Design install, clear, status, fallback report, and diagnostic readback signatures for shaped text in `src/SkiaViewer/SkiaViewer.fsi`
- [X] T013 [P] Design shaped cache key, shaped reuse evidence, and retained text metrics signatures in `src/Controls/RetainedRender.fsi`
- [X] T014 [P] Define at least 40 text fixture records across eight categories, including Latin kerning, ligatures, combining marks, RTL, mixed-direction, emoji or symbol fallback, Arabic contextual forms, Devanagari conjuncts, Thai mark or vowel positioning, and newline-control cases, in `tests/Rendering.Harness/TextShapingFixtures.fsi` and `tests/Rendering.Harness/TextShapingFixtures.fs`
- [X] T015 [P] Define measure/draw parity, diagnostics coverage, and deterministic fingerprint assertion helpers in `tests/Rendering.Harness/TextShapingOracle.fsi` and `tests/Rendering.Harness/TextShapingOracle.fs`
- [X] T016 [P] Define direct, cold retained, warm retained, cache-enabled, cache-disabled, shaping-enabled, and pure-fallback parity capture helpers in `tests/Rendering.Harness/TextShapingParity.fsi` and `tests/Rendering.Harness/TextShapingParity.fs`
- [X] T017 Add a Scene dependency-boundary guard proving `src/Scene/Scene.fsproj` does not reference SkiaSharp, HarfBuzzSharp, SkiaViewer, Controls, Elmish, Yoga, Silk.NET, or native host packages in `tests/Scene.Tests/Feature142ShapedTextTests.fs`, and record an F# Interactive/prelude transcript that exercises the new public shaped-text signatures before `.fs` implementation in `specs/142-harfbuzz-text-shaping/readiness/dependency-boundary.md`

**Checkpoint**: Public signatures and shared fixtures are ready; tests can now be added story-by-story before implementation.

---

## Phase 3: User Story 1 - Measure and Draw From One Shaped Result (Priority: P1) MVP

**Goal**: Text measurement, drawing, fingerprints, and fallback behavior derive from one shaped result in shaping-enabled mode while preserving no-provider fallback.

**Independent Test**: Render Latin kerning, ligature, combining mark, RTL, mixed-direction, emoji, Arabic, Devanagari, and Thai fixtures with the provider installed; assert measured advance and drawn advance differ by no more than one pixel and that provider-cleared fallback remains deterministic.

### Tests for User Story 1

- [X] T018 [P] [US1] Add failing Scene tests for shaped result metrics, glyph advances, fingerprints, and pure fallback compatibility in `tests/Scene.Tests/Feature142ShapedTextTests.fs`
- [X] T019 [P] [US1] Add failing SkiaViewer tests for HarfBuzz provider install, shaped glyph output, and measure-vs-draw advance parity in `tests/SkiaViewer.Tests/Feature142HarfBuzzShapingTests.fs`
- [X] T020 [P] [US1] Add failing Controls tests proving labels, buttons, text blocks, data values, and rich text consume shaped metrics in `tests/Controls.Tests/Feature142ControlsTextShapingTests.fs`

### Implementation for User Story 1

- [X] T021 [US1] Implement shaped text records, fallback-compatible construction, deterministic fingerprinting, and shaped metrics projection in `src/Scene/Scene.fs`
- [X] T022 [US1] Implement HarfBuzz-backed shaping provider, shaped request handling, aggregate metrics, and provider evidence in `src/SkiaViewer/Fonts.fs`
- [X] T023 [US1] Update glyph drawing to paint shaped glyph IDs and positions instead of reshaping or drawing the original string for successful shaped results in `src/SkiaViewer/SceneRenderer.fs`
- [X] T024 [US1] Wire shaped provider install, clear, and status into the viewer text edge in `src/SkiaViewer/SkiaViewer.fs`
- [X] T025 [US1] Route control text, rich text, and fitted text measurement through the shaped metrics seam in `src/Controls/Control.fs`
- [X] T026 [US1] Invalidate shaped evidence when text, family, size, weight, direction, or provider availability changes in `src/Controls/RetainedRender.fs`
- [X] T027 [US1] Record User Story 1 focused validation commands and results in `specs/142-harfbuzz-text-shaping/readiness/measure-draw-parity.md`

**Checkpoint**: User Story 1 is independently testable as the MVP.

---

## Phase 4: User Story 2 - Render International Text With Actionable Fallback Evidence (Priority: P1)

**Goal**: International text, fallback fonts, missing glyphs, and mixed-direction runs render predictably and produce actionable diagnostics.

**Independent Test**: Exercise the 40-case fixture corpus plus negative missing-glyph fixtures and verify run itemization, fallback decisions, missing-glyph disclosure, direction evidence, cluster mapping, and diagnostic coverage.

### Tests for User Story 2

- [X] T028 [P] [US2] Add failing fixture corpus tests for RTL, mixed-direction, combining marks, emoji, symbols, complex scripts, and negative missing-glyph cases in `tests/Rendering.Harness.Tests/Feature142TextFixtureCorpusTests.fs`
- [X] T029 [P] [US2] Add failing SkiaViewer diagnostics tests for substituted fonts, missing glyphs, provider failure, and unsupported bidi disclosure in `tests/SkiaViewer.Tests/Feature142FallbackDiagnosticsTests.fs`
- [X] T030 [P] [US2] Add failing Scene tests for run itemization, fallback disclosure data, clusters, and source text ranges in `tests/Scene.Tests/Feature142RunItemizationTests.fs`

### Implementation for User Story 2

- [X] T031 [US2] Implement run itemization data, direction/script evidence, cluster storage, and fallback disclosure aggregation in `src/Scene/Scene.fs`
- [X] T032 [US2] Implement font fallback segmentation, resolved face identity, missing-glyph detection, and negative-fixture diagnostics in `src/SkiaViewer/Fonts.fs`
- [X] T033 [US2] Implement deterministic RTL and mixed-direction single-line run ordering with unsupported bidi diagnostics in `src/SkiaViewer/Fonts.fs`
- [X] T034 [US2] Expose provider, fallback, and missing-glyph diagnostics through the text readback API in `src/SkiaViewer/SkiaViewer.fs`
- [X] T035 [US2] Complete the 40-case fixture corpus with expected categories, directions, fallback expectations, missing-glyph expectations, and single-line newline-control expectations in `tests/Rendering.Harness/TextShapingFixtures.fs`
- [X] T036 [US2] Implement corpus oracle checks for bounds, advances, clusters, diagnostics, and deterministic newline-control handling in `tests/Rendering.Harness/TextShapingOracle.fs`
- [X] T037 [US2] Record fixture coverage and diagnostic coverage evidence in `specs/142-harfbuzz-text-shaping/readiness/fallback-diagnostics.md`

**Checkpoint**: User Story 2 is independently testable against international and negative fixture evidence.

---

## Phase 5: User Story 3 - Preserve Rendering Parity, Caches, and Determinism (Priority: P1)

**Goal**: Direct, retained, cache-enabled, cache-disabled, and repeated-run paths produce equivalent shaped text output, metrics, diagnostics, fingerprints, and reuse evidence.

**Independent Test**: Run text-heavy scenes through direct, first-frame retained, warm retained, cache-enabled, cache-disabled, shaping-enabled, and pure fallback modes and compare metrics, diagnostics, fingerprints, and output evidence.

### Tests for User Story 3

- [X] T038 [P] [US3] Add failing Controls cache-enabled versus cache-disabled shaped text parity tests in `tests/Controls.Tests/Feature142TextCacheParityTests.fs`
- [X] T039 [P] [US3] Add failing Elmish tests for direct, cold retained, warm retained, at least 100 text-heavy fixture or generated scenes, and repeated warm frame text metrics in `tests/Elmish.Tests/Feature142TextMetricsTests.fs`
- [X] T040 [P] [US3] Add failing Scene tests for byte-stable shaped fingerprints and diagnostics across three repeated runs in `tests/Scene.Tests/Feature142ShapedTextDeterminismTests.fs`

### Implementation for User Story 3

- [X] T041 [US3] Extend retained text cache keys with provider availability, provider version bucket, direction, script, language if present, resolved face, fallback outcome, and shaping feature flags in `src/Controls/RetainedRender.fs`
- [X] T042 [US3] Implement bounded shaped result cache entries with hit, miss, fresh, reused, and stale-prevented evidence in `src/Controls/RetainedRender.fs`
- [X] T043 [US3] Add a shaped-cache disabled bypass path that still emits equivalent output, metrics, diagnostics, and fingerprints in `src/Controls/RetainedRender.fs`
- [X] T044 [US3] Feed shaped text fingerprints, diagnostics, and reuse counters into retained frame metrics in `src/Controls.Elmish/ControlsElmish.fsi` and `src/Controls.Elmish/ControlsElmish.fs`
- [X] T045 [US3] Ensure full render and retained render paths hash the same shaped text evidence in `src/Controls/Control.fs`
- [X] T046 [US3] Implement direct, cold retained, warm retained, cache-enabled, cache-disabled, shaping-enabled, and pure-fallback parity capture for at least 100 text-heavy fixture or generated scenes in `tests/Rendering.Harness/TextShapingParity.fs`
- [X] T047 [US3] Record cache-on/off, direct/cold/warm retained, deterministic fingerprint, no-stale-reuse, no more than one fresh shaped result per unique unchanged text input, and new-shaping-versus-pre-existing cache or retained limitation classification evidence in `specs/142-harfbuzz-text-shaping/readiness/cache-retained-parity.md`

**Checkpoint**: User Story 3 is independently testable for cache and retained-rendering parity.

---

## Phase 6: User Story 4 - Keep Pure Fallback and Baseline Changes Auditable (Priority: P2)

**Goal**: Pure fallback remains compatible, public surface and dependency changes are auditable, and every intentional shaped baseline delta has a ledger entry.

**Independent Test**: Run provider-cleared fallback verification and surface/dependency checks; confirm zero pure fallback baseline changes and complete disclosure for intentional shaping-enabled deltas.

### Tests for User Story 4

- [X] T048 [P] [US4] Add failing pure fallback zero-baseline tests for provider-absent and provider-cleared paths in `tests/Scene.Tests/Feature142PureFallbackCompatibilityTests.fs`
- [X] T049 [P] [US4] Add failing package surface and dependency boundary tests for Scene and SkiaViewer package contracts in `tests/SkiaViewer.Tests/Feature142SurfaceAndDependencyTests.fs`
- [X] T050 [P] [US4] Add failing baseline ledger validation tests for intentional pixel, diagnostic, dependency, and surface deltas in `tests/Rendering.Harness.Tests/Feature142BaselineLedgerTests.fs`

### Implementation for User Story 4

- [X] T051 [US4] Preserve byte-compatible no-provider fallback measurement, drawing, fingerprints, and diagnostics in `src/Scene/Scene.fs`
- [X] T052 [US4] Preserve provider-absent and provider-cleared fallback rendering behavior in `src/SkiaViewer/Fonts.fs`
- [X] T053 [US4] Implement provider clear, provider status, native asset failure, and shaping failure diagnostics in `src/SkiaViewer/SkiaViewer.fs`
- [X] T054 [US4] Record public surface baseline diff or zero-diff evidence in `specs/142-harfbuzz-text-shaping/readiness/surface-baseline.md`
- [X] T055 [US4] Document the HarfBuzz dependency, maintenance owner, versioning rationale, and migration impact in `docs/reports/dependencies.md`
- [X] T056 [US4] Record every intentional pixel, golden, diagnostic, dependency, or public surface delta in `specs/142-harfbuzz-text-shaping/readiness/baseline-disclosure-ledger.md`
- [X] T057 [US4] Record pure fallback zero-baseline evidence in `specs/142-harfbuzz-text-shaping/readiness/pure-fallback.md`
- [X] T058 [US4] Confirm no implementation work was added for portable serialization, browser rendering, overlay interaction state, compositor promotion, damage-scissored presentation, intrinsic layout, caret, selection, or text editing in `specs/142-harfbuzz-text-shaping/readiness/scope-review.md`

**Checkpoint**: User Story 4 is independently testable for fallback compatibility and auditability.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation, package checks, and roadmap status updates.

- [X] T059 [P] Add final XML documentation for new public shaped text and provider signatures in `src/Scene/Scene.fsi`, `src/SkiaViewer/Fonts.fsi`, and `src/SkiaViewer/SkiaViewer.fsi`
- [X] T060 [P] Update validation commands and environment-limitation notes in `specs/142-harfbuzz-text-shaping/quickstart.md`
- [X] T061 Run `dotnet restore FS.GG.Rendering.slnx` and `dotnet build FS.GG.Rendering.slnx`, then record results in `specs/142-harfbuzz-text-shaping/readiness/validation-log.md`
- [X] T062 Run `dotnet test tests/Scene.Tests/Scene.Tests.fsproj`, `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj`, `dotnet test tests/Controls.Tests/Controls.Tests.fsproj`, and `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj`, then record results in `specs/142-harfbuzz-text-shaping/readiness/validation-log.md`
- [X] T063 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` and record fixture corpus results in `specs/142-harfbuzz-text-shaping/readiness/measure-draw-parity.md`
- [X] T064 Run `./fake.sh build -t CapabilityCheck`, `./fake.sh build -t PackageSurfaceCheck`, and `./fake.sh build -t PackLocal`, then record results, explicit tooling limitations, and new-shaping-versus-pre-existing package-surface limitation classification in `specs/142-harfbuzz-text-shaping/readiness/package-surface.md`
- [X] T065 Verify central package pinning and absence of Scene references to SkiaSharp, HarfBuzzSharp, SkiaViewer, Controls, Elmish, Yoga, Silk.NET, or native host packages, then record evidence in `specs/142-harfbuzz-text-shaping/readiness/dependency-boundary.md`
- [X] T066 Update Feature 142/P4 text shaping completion status and evidence links in `docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 and blocks every user story.
- **User Story 1 (Phase 3)**: Depends on Phase 2; this is the MVP.
- **User Story 2 (Phase 4)**: Depends on Phase 2 and can proceed in parallel with US1 after the shared signatures exist, but final corpus results benefit from US1 shaping implementation.
- **User Story 3 (Phase 5)**: Depends on Phase 2 and can proceed in parallel with US1/US2 after the shared signatures exist, but final parity evidence requires shaped output from US1 and diagnostics from US2.
- **User Story 4 (Phase 6)**: Depends on Phase 2 and can proceed in parallel for fallback/surface tests, but final disclosure evidence requires US1-US3 deltas.
- **Polish (Phase 7)**: Depends on all selected user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Core MVP; no dependency on other stories after Phase 2.
- **US2 (P1)**: Independent diagnostics and fixture story; final assertions use the provider from US1.
- **US3 (P1)**: Independent cache/parity story; final assertions use shaped results from US1 and diagnostics from US2.
- **US4 (P2)**: Independent fallback/audit story; final ledger is completed after intentional deltas from US1-US3 are known.

### Within Each User Story

- Tests are written first and should fail before implementation.
- `.fsi` signatures from Phase 2 stay ahead of `.fs` implementation.
- Scene data and pure fallbacks come before SkiaViewer shaping code.
- SkiaViewer shaping code comes before Controls/retained integration.
- Readiness evidence is recorded before a story checkpoint is considered complete.

### Parallel Opportunities

- Setup tasks T003-T009 can run in parallel after T001-T002 are understood.
- Foundational tasks T011-T016 can run in parallel after T010 establishes the Scene-shaped data vocabulary; T017 follows the public signatures and shared fixture primitives.
- US1 tests T018-T020 can run in parallel.
- US2 tests T028-T030 can run in parallel.
- US3 tests T038-T040 can run in parallel.
- US4 tests T048-T050 can run in parallel.
- Different P1 stories can be staffed in parallel after Phase 2, with final evidence reconciled in priority order.

---

## Parallel Example: User Story 1

```bash
Task: "Add failing Scene tests for shaped result metrics, glyph advances, fingerprints, and pure fallback compatibility in tests/Scene.Tests/Feature142ShapedTextTests.fs"
Task: "Add failing SkiaViewer tests for HarfBuzz provider install, shaped glyph output, and measure-vs-draw advance parity in tests/SkiaViewer.Tests/Feature142HarfBuzzShapingTests.fs"
Task: "Add failing Controls tests proving labels, buttons, text blocks, data values, and rich text consume shaped metrics in tests/Controls.Tests/Feature142ControlsTextShapingTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "Add failing fixture corpus tests for RTL, mixed-direction, combining marks, emoji, symbols, complex scripts, and negative missing-glyph cases in tests/Rendering.Harness.Tests/Feature142TextFixtureCorpusTests.fs"
Task: "Add failing SkiaViewer diagnostics tests for substituted fonts, missing glyphs, provider failure, and unsupported bidi disclosure in tests/SkiaViewer.Tests/Feature142FallbackDiagnosticsTests.fs"
Task: "Add failing Scene tests for run itemization, fallback disclosure data, clusters, and source text ranges in tests/Scene.Tests/Feature142RunItemizationTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "Add failing Controls cache-enabled versus cache-disabled shaped text parity tests in tests/Controls.Tests/Feature142TextCacheParityTests.fs"
Task: "Add failing Elmish tests for direct, cold retained, warm retained, and repeated warm frame text metrics in tests/Elmish.Tests/Feature142TextMetricsTests.fs"
Task: "Add failing Scene tests for byte-stable shaped fingerprints and diagnostics across three repeated runs in tests/Scene.Tests/Feature142ShapedTextDeterminismTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "Add failing pure fallback zero-baseline tests for provider-absent and provider-cleared paths in tests/Scene.Tests/Feature142PureFallbackCompatibilityTests.fs"
Task: "Add failing package surface and dependency boundary tests for Scene and SkiaViewer package contracts in tests/SkiaViewer.Tests/Feature142SurfaceAndDependencyTests.fs"
Task: "Add failing baseline ledger validation tests for intentional pixel, diagnostic, dependency, and surface deltas in tests/Rendering.Harness.Tests/Feature142BaselineLedgerTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 public signatures and shared fixtures.
3. Complete Phase 3 User Story 1.
4. Stop and validate `tests/Scene.Tests/Scene.Tests.fsproj`, `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj`, and `tests/Controls.Tests/Controls.Tests.fsproj`.
5. Record MVP evidence in `specs/142-harfbuzz-text-shaping/readiness/measure-draw-parity.md`.

### Incremental Delivery

1. Deliver US1 to make shaped measurement and drawing use one result.
2. Deliver US2 to broaden coverage to international text and diagnostics.
3. Deliver US3 to prove cache, retained-rendering, and deterministic parity.
4. Deliver US4 to lock fallback compatibility and audit all public, dependency, diagnostic, and pixel changes.
5. Finish Phase 7 validation and roadmap status updates.

### Parallel Team Strategy

1. One contributor owns `src/Scene` signatures and deterministic data.
2. One contributor owns `src/SkiaViewer` HarfBuzz/provider/fallback integration.
3. One contributor owns `src/Controls` and `src/Controls.Elmish` cache/retained integration.
4. One contributor owns harness fixtures, parity oracles, and readiness ledgers.
5. All contributors reconcile through the Phase 7 validation artifacts before completion.

---

## Notes

- `[P]` tasks are intentionally limited to different files or isolated setup/test scaffolds.
- Public surface work follows constitution order: `spec.md` and `plan.md`, then `.fsi`, then failing semantic tests, then `.fs` implementation.
- HarfBuzz, SkiaSharp, native assets, and provider lifecycle stay in `src/SkiaViewer`.
- `src/Scene` remains dependency-light and stores shaped text only as stable records and discriminated unions.
- Pure fallback remains the oracle for no-provider and provider-cleared validation.
