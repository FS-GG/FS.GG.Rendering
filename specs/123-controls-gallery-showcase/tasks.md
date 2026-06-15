---
description: "Task list for Controls Gallery Showcase (Light/Dark)"
---

# Tasks: Controls Gallery Showcase (Light/Dark)

**Input**: Design documents from `/specs/123-controls-gallery-showcase/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUDED — Constitution Principle V mandates test evidence, and each user
story carries an Independent Test plus explicit checks (FR-003 coverage, SC-002
determinism, SC-003 theme invariance, SC-004 degrade-and-disclose).

**Organization**: Tasks are grouped by user story for independent implementation and
testing. The sample lives in a standalone `samples/ControlsGallery/` tree consuming
the packed `FS.GG.UI.*` packages (no `src/` project references).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1–US4 (Setup/Foundational/Polish have no story label)

## Path Conventions

- Core (pure): `samples/ControlsGallery/ControlsGallery.Core/`
- App (edge/exe): `samples/ControlsGallery/ControlsGallery.App/`
- Tests (Expecto): `samples/ControlsGallery/ControlsGallery.Tests/`
- Evidence output: `artifacts/controls-gallery/<seed>/<page-id>/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Scaffold the standalone consumer tree and wire it to the local NuGet feed.

- [X] T001 Create the `samples/ControlsGallery/` directory tree (Core, App, Tests subfolders) per plan.md Project Structure
- [X] T002 [P] Add `samples/ControlsGallery/nuget.config` pointing the local feed at `~/.local/share/nuget-local/` (research R1)
- [X] T003 [P] Create `samples/ControlsGallery/ControlsGallery.Core/ControlsGallery.Core.fsproj` (net10.0, `IsPackable=false`) with `PackageReference` to `FS.GG.UI.Controls` and `FS.GG.UI.Color` (versions `0.1.0-preview.1`)
- [X] T004 [P] Create `samples/ControlsGallery/ControlsGallery.App/ControlsGallery.App.fsproj` (net10.0, `OutputType=Exe`, `IsPackable=false`) with `ProjectReference` to Core and `PackageReference` to `FS.GG.UI.Controls.Elmish`, `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Testing`
- [X] T005 [P] Create `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` (net10.0) with Expecto + `Microsoft.NET.Test.Sdk` + `YoloDev.Expecto.TestSdk`, `ProjectReference` to Core, and `PackageReference` to `FS.GG.UI.Controls`
- [X] T006 Verify package restore + empty build of all three projects against the local feed (`dotnet build` each) — confirms the public-consumer path resolves (SC-005)

**Checkpoint**: Projects compile empty against packed `FS.GG.UI.*` packages.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core types, the page registry (single source of truth), and base theme
resolution that every user story depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T007 Define core types in `samples/ControlsGallery/ControlsGallery.Core/Model.fs`: `GalleryPage`, `DemoState`, `CoverageResult`, `GalleryModel`, `GalleryMsg` (per data-model.md; pure records/DUs, no `update` body yet)
- [X] T008 Author the page registry `Core.Pages.all` in `samples/ControlsGallery/ControlsGallery.Core/Pages.fs` — the 10 `GalleryPage` entries with `Id`/`Index`/`Title`/`Family`/`ControlIds` exactly per contracts/page-registry.md (52 ids across 10 pages); `Build` stubbed
- [X] T009 Implement base theme resolution in `samples/ControlsGallery/ControlsGallery.Core/GalleryTheme.fs`: Light/Dark via `Theming.resolve`/`Theming.toTheme` over the slate neutral base, plus Indigo (primary) and Teal (secondary) accent `Color` literals (research R5)

**Checkpoint**: Registry + types + base theme available; stories can begin.

---

## Phase 3: User Story 1 - Browse every control across a navigable gallery (Priority: P1) 🎯 MVP

**Goal**: A windowed gallery with a 10-page nav rail that renders all 52 controls with
seeded content, plus an automated coverage check proving the 52→10 bijection.

**Independent Test**: Launch the gallery, walk all pages, confirm every catalog control
is present and rendered; run the coverage check and confirm 0 unreferenced / 0
duplicated (it fails on any drift).

### Tests for User Story 1

- [X] T010 [P] [US1] Coverage test in `samples/ControlsGallery/ControlsGallery.Tests/CoverageTests.fs` — assert `Catalog.supportedControls` (52) maps 1:1 onto the 10 pages, 0 unreferenced, 0 duplicated (FR-003/SC-001); MUST fail until the registry is complete
- [X] T011 [P] [US1] Page-render test in `samples/ControlsGallery/ControlsGallery.Tests/PageRenderTests.fs` — for each of the 10 pages, `Control.renderTree` of its `Build demoState` produces a non-empty tree with no errors (US1 acceptance #1, #3)

### Implementation for User Story 1

- [X] T012 [US1] Implement seeded representative content in `samples/ControlsGallery/ControlsGallery.Core/DemoState.fs` — populate data/collection/chart/tree controls so none renders empty (FR-004)
- [X] T013 [US1] Implement `Core.CoverageMap.check : unit -> CoverageResult` in `samples/ControlsGallery/ControlsGallery.Core/CoverageMap.fs` over `Catalog.supportedControls` + `Pages.all` (contracts/page-registry.md)
- [X] T014 [US1] Implement each page's `Build : DemoState -> Control<GalleryMsg>` in `samples/ControlsGallery/ControlsGallery.Core/Pages.fs` — render the page's grouped controls with demo state (one semantic control set, no per-theme forks)
- [X] T015 [US1] Implement the shell view in `samples/ControlsGallery/ControlsGallery.Core/Shell.fs`: top app bar (title), left nav rail (10 pages), scrolling content region, bottom status strip (FR-001)
- [X] T016 [US1] Implement `init` + `update` for `SelectPage` (page navigation, pure) in `samples/ControlsGallery/ControlsGallery.Core/Model.fs`
- [X] T017 [US1] Wire interactive launch in `samples/ControlsGallery/ControlsGallery.App/Interactive.fs` — build `InteractiveAppHost<GalleryModel,GalleryMsg>` and call `ControlsElmish.runInteractiveApp`, GL-gated via `Viewer.runtimeCapability` (FR-007)
- [X] T018 [US1] Implement CLI dispatch in `samples/ControlsGallery/ControlsGallery.App/Program.fs` for `interactive` and `coverage-check` subcommands (contracts/cli.md), incl. coverage-check exit codes (0 pass / non-zero on drift)

**Checkpoint**: MVP — gallery browses all 52 controls across 10 pages; coverage check passes.

---

## Phase 4: User Story 2 - Switch theme and accent (Priority: P2)

**Goal**: Light/Dark toggle + accent selector restyle the whole gallery cohesively
while behavior and accessibility stay identical across variants.

**Independent Test**: Render the same page under Light, Dark, and each accent; assert
the control tree and accessibility contract are identical while resolved visuals differ.

### Tests for User Story 2

- [X] T019 [P] [US2] Theme-invariance test in `samples/ControlsGallery/ControlsGallery.Tests/ThemeInvarianceTests.fs` — for the same page, control-tree shape + accessibility metadata are identical across Light/Dark × {Indigo,Teal}; only resolved visuals differ (FR-006/SC-003)

### Implementation for User Story 2

- [X] T020 [US2] Expose the accent set (Indigo/Teal) + mode/accent → `Theme` resolution helpers in `samples/ControlsGallery/ControlsGallery.Core/GalleryTheme.fs`
- [X] T021 [US2] Implement `update` for `ToggleTheme` and `SelectAccent` in `samples/ControlsGallery/ControlsGallery.Core/Model.fs` (pure)
- [X] T022 [US2] Add the app-bar theme toggle + accent selector controls and apply the resolved `Theme` in `samples/ControlsGallery/ControlsGallery.Core/Shell.fs` (FR-005)

**Checkpoint**: US1 + US2 both work independently; theme/accent switching is live.

---

## Phase 5: User Story 3 - Produce deterministic, disclosed per-page evidence headlessly (Priority: P3)

**Goal**: Headless evidence mode replays seeded scripts per page, writes byte-identical
per-page records with non-empty disclosures, and degrades cleanly on no-GL hosts.

**Independent Test**: Run headless evidence twice with the same seed and diff for
byte-identity; inspect each record for a non-empty "not authoritative for" disclosure;
run on a no-GL host and confirm a clean disclosed skip (exit 0, no hang).

### Tests for User Story 3

- [X] T023 [P] [US3] Determinism test in `samples/ControlsGallery/ControlsGallery.Tests/DeterminismTests.fs` — two same-seed evidence runs over all pages produce byte-identical `run.json` + `state.txt` (FR-009/SC-002)
- [X] T024 [P] [US3] Degrade-and-disclose test in `samples/ControlsGallery/ControlsGallery.Tests/DegradeTests.fs` — on a no-GL host, `provesScreenshot=false` with a stated reason, `notAuthoritativeFor` non-empty, process exits 0 (FR-010/FR-011/SC-004)

### Implementation for User Story 3

- [X] T025 [US3] Author per-page seeded `FrameInput<GalleryMsg> list` scripts in `samples/ControlsGallery/ControlsGallery.Core/Scripts.fs` — keys/pointers/ticks with injected deltas, no wall-clock/randomness (FR-009)
- [X] T026 [US3] Implement the state-outcome capture in `samples/ControlsGallery/ControlsGallery.App/Evidence.fs` via `ControlsElmish.Perf.runScript` (golden `FrameMetrics` count/bool fields only; exclude `*Duration`)
- [X] T027 [US3] Add screenshot capture in `samples/ControlsGallery/ControlsGallery.App/Evidence.fs` via `Viewer.captureScreenshotEvidence` with `ViewerPresentMode.OffscreenReadback` (FR-008)
- [X] T028 [US3] Implement the `PageEvidenceRecord` writer in `samples/ControlsGallery/ControlsGallery.App/Evidence.fs` — `run.json`/`summary.md`/`state.txt`/`frame.png` under `artifacts/controls-gallery/<seed>/<page-id>/`, every record with non-empty `notAuthoritativeFor` (contracts/evidence-record.md, FR-010)
- [X] T029 [US3] Implement degrade-and-disclose in `samples/ControlsGallery/ControlsGallery.App/Evidence.fs` — gate on `Viewer.runtimeCapability`/`ScreenshotEvidenceResult`, still emit deterministic state, set `provesScreenshot=false` + reason, omit `frame.png`, exit 0 (FR-011/FR-016)
- [X] T030 [US3] Add the `evidence --seed <int> [--out <dir>] [--page <id>]` subcommand + exit codes to `samples/ControlsGallery/ControlsGallery.App/Program.fs` (contracts/cli.md)

**Checkpoint**: Headless CI path produces deterministic, disclosed evidence; degrades cleanly.

---

## Phase 6: User Story 4 - Exercise pointer and keyboard interaction (Priority: P3)

**Goal**: Interactive controls respond visibly to pointer/keyboard per a documented
interaction contract; display-only controls simply render.

**Independent Test**: Drive a seeded pointer/keyboard script against an interactive page
and assert the visible state change matches the documented contract; display-only
controls are exempt.

### Tests for User Story 4

- [X] T031 [P] [US4] Interaction test in `samples/ControlsGallery/ControlsGallery.Tests/InteractionTests.fs` — a seeded script targeting an interactive control yields a visible state change (`ProductModelChanged`/responds-proof); display-only controls (label/separator/badge) are exempt (FR-012)

### Implementation for User Story 4

- [X] T032 [US4] Extend `samples/ControlsGallery/ControlsGallery.Core/DemoState.fs` with interactive per-control state (button press, text/numeric value, checkbox/switch, selection, overlay open)
- [X] T033 [US4] Route control interaction messages through `PageMsg` in `samples/ControlsGallery/ControlsGallery.Core/Model.fs` `update` (pure)
- [X] T034 [US4] Wire control event handlers (`onClick`/`onChanged`/etc.) to `PageMsg` in `samples/ControlsGallery/ControlsGallery.Core/Shell.fs` and the page `Build`s
- [X] T035 [US4] Document the pointer-interaction contract (per interactive family; display-only exemptions) in `samples/ControlsGallery/README.md` (adopted/rebranded per FR-015)

**Checkpoint**: All four user stories independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Provenance, docs, ignore rules, and end-to-end validation.

- [X] T036 [P] Record adoption/rebrand provenance (`FS.Skia.UI.*` → `FS.GG.UI.*`, archived Showcase 01–10 source) in `PROVENANCE.md` (FR-015, research R9)
- [X] T037 [P] Write `samples/ControlsGallery/README.md` — purpose, the two modes, how to run, the public-consumer-path note (SC-005)
- [X] T038 [P] Add `artifacts/controls-gallery/` to `.gitignore`
- [X] T039 Update `tests/Smoke.Tests` so the samples-present contract check runs (no longer `skiptest` for missing samples) — keeps the gallery honest in CI without GL dependence
- [X] T040 Run the quickstart.md validation scenarios end-to-end (coverage, determinism diff, theme invariance, degrade) and confirm outcomes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup — BLOCKS all user stories.
- **User Stories (Phase 3–6)**: all depend on Foundational.
  - US1 (P1) is the MVP and should land first.
  - US2 (P2) depends only on Foundational (uses base theme + shell; integrates with US1 shell).
  - US3 (P3) depends on Foundational + a renderable gallery (US1) to script against.
  - US4 (P3) depends on Foundational + US1 (controls present to interact with); overlaps with US3 scripts.
- **Polish (Phase 7)**: after the desired stories are complete.

### User Story Dependencies

- **US1 (P1)**: Foundational only. No dependency on other stories.
- **US2 (P2)**: Foundational; touches `Shell.fs`/`Model.fs` shared with US1 (sequence after US1 to avoid same-file churn).
- **US3 (P3)**: Foundational + US1 (needs pages to render/script). Largely additive (new `Scripts.fs`, `Evidence.fs`).
- **US4 (P3)**: Foundational + US1; shares `Model.fs`/`Shell.fs`/`DemoState.fs` with US1/US2.

### Within Each User Story

- Tests are written first and MUST fail before implementation (Principle I/V).
- Core (pure) before App (edge); types before functions; `update` before view wiring.

### Parallel Opportunities

- Setup: T002–T005 are [P] (distinct files).
- US1 tests T010/T011 are [P]. US3 tests T023/T024 are [P].
- Foundational T007→T008→T009 are sequential within `Core` files but T008/T009 are
  independent once T007's types exist.
- US3's new files (`Scripts.fs`, `Evidence.fs`) can proceed in parallel with US4's
  doc task once US1 is stable.
- Polish T036/T037/T038 are [P].

---

## Parallel Example: User Story 1

```bash
# Launch US1 tests together (write first, expect failure):
Task: "Coverage test in ControlsGallery.Tests/CoverageTests.fs"
Task: "Page-render test in ControlsGallery.Tests/PageRenderTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1.
4. **STOP and VALIDATE**: browse all 52 controls across 10 pages; coverage check passes.
5. Demo the MVP (living documentation + proof the controls compose).

### Incremental Delivery

1. Setup + Foundational → consumer tree builds against packed packages.
2. US1 → MVP (browse + coverage). 3. US2 → theme/accent. 4. US3 → headless evidence
   (CI path). 5. US4 → interaction. Each story adds value without breaking prior ones.

---

## Notes

- [P] = different files, no incomplete-task dependencies.
- The sample consumes **packed `FS.GG.UI.*` only** — no `src/` project references (FR-013/SC-005).
- Headless `evidence` + `coverage-check` + the deterministic Expecto suites are the
  CI-facing path; interactive mode is GL-gated and advisory (FR-016).
- No wall-clock/randomness in any evidence-affecting path (FR-009).
- Commit after each task or logical group; stop at any checkpoint to validate.
