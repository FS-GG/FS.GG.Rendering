---
description: "Task list for Ant Design Controls Showcase (G3)"
---

# Tasks: Ant Design Controls Showcase — Ant restyle + enterprise templates (G3)

**Input**: Design documents from `specs/135-antd-controls-showcase/`

**Prerequisites**: plan.md, spec.md, research.md (R1–R8), data-model.md, contracts/ (cli, page-registry,
enterprise-templates, evidence-record), quickstart.md (V0–V8)

**Tests**: INCLUDED — required by Constitution Principle V and named explicitly in plan.md (Expecto suites).
Write each story's tests first; they MUST fail before that story's implementation.

**Organization**: Tasks grouped by user story. All paths are under `samples/AntShowcase/` unless noted.
This is a **pure `FS.GG.UI.*` package consumer** — no `src/` references (FR-015). Precedent for every
module is the shipped G1 sample at `samples/ControlsGallery/`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1–US5 (user story phases only)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: The feed precondition + the sample's 3-project skeleton.

- [x] T001 Refresh the local NuGet feed (quickstart V0 / research R1): run `dotnet pack FS.GG.Rendering.slnx -c Release` (writes to `src/<proj>/bin/Release/`, **not** the feed), then copy the packages into the feed — `find src -path '*/bin/Release/FS.GG.UI.*.0.1.0-preview.1.nupkg' -exec cp {} ~/.local/share/nuget-local/ \;` — then `dotnet nuget locals global-packages --clear`; verify `FS.GG.UI.Themes.AntDesign.0.1.0-preview.1.nupkg` exists and the packed `FS.GG.UI.Controls` dll references a net-new Ant control (e.g. `segmented`). **Blocks all build/restore. ✅ DONE 2026-06-17.**
- [x] T002 Create `samples/AntShowcase/` tree + consumer config copied from `samples/ControlsGallery/`: `nuget.config`, `Directory.Build.props` (net10.0, `FS0078`-as-error, `IsPackable=false`), `Directory.Packages.props` (`ManagePackageVersionsCentrally=false`).
- [x] T003 [P] Create `AntShowcase.Core/AntShowcase.Core.fsproj` (`OutputType=Library`) with `PackageReference`s: `FS.GG.UI.Themes.AntDesign`, `FS.GG.UI.Controls`, `FS.GG.UI.Color`, `FS.GG.UI.Scene`, `FS.GG.UI.Controls.Elmish`, `FS.GG.UI.SkiaViewer`, `FS.GG.UI.DesignSystem`, `FS.GG.UI.KeyboardInput` (all `0.1.0-preview.1`) — **no** `Themes.Default`. Add ordered `<Compile>` items per plan structure.
- [x] T004 [P] Create `AntShowcase.App/AntShowcase.App.fsproj` (`OutputType=Exe`): `ProjectReference` Core; `PackageReference` `FS.GG.UI.Controls.Elmish`, `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Testing`.
- [x] T005 [P] Create `AntShowcase.Tests/AntShowcase.Tests.fsproj` + `AntShowcase.Tests/Main.fs` (Expecto `runTestsInAssemblyWithCLIArgs`): `ProjectReference` Core; `PackageReference` `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`, `FS.GG.UI.SkiaViewer`, `Expecto`, `Microsoft.NET.Test.Sdk`, `YoloDev.Expecto.TestSdk`. **Outside** `FS.GG.Rendering.slnx`.
- [x] T006 Add `artifacts/ant-showcase/` to the repo-root `.gitignore` (transient per-page evidence output).

**Checkpoint**: All three projects restore from the local feed and build empty.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core types + Ant theme + evidence record that every story depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T007 [P] Create `AntShowcase.Core/Model.fs`: `PageKind` (`Catalog`/`Template`), `Page`, `AntShowcaseModel`, `AntShowcaseMsg`, `PageMsg`, `CoverageResult`, `FormPhase`, `FormState` — per data-model.md §1–§6.
- [x] T008 [P] Create `AntShowcase.Core/AntTheme.fs`: `resolve : ThemeMode -> Theme` mapping `Light→AntTheme.antLight`, `Dark→AntTheme.antDark` from `FS.GG.UI.Themes.AntDesign` (research R3); a `defaultTheme` for build-time `Theme` needs.
- [x] T009 Create `AntShowcase.Core/DemoState.fs`: `DemoState` record + `seed` value — base interactive fields only (text/numeric/buttons/selection); per-control + form content added in US1/US2. (depends T007)
- [x] T010 Create `AntShowcase.Core/Evidence.fs`: package-only per-page evidence record + `run.json`/`state.txt`/`summary.md` writers ported from `samples/ControlsGallery/ControlsGallery.Core/Evidence.fs`, with non-empty `NotAuthoritativeFor` (contracts/evidence-record.md). (depends T007)

**Checkpoint**: Core types + theme + evidence compile; ready for story work.

---

## Phase 3: User Story 1 - Browse every control in the Ant visual language (Priority: P1) 🎯 MVP

**Goal**: All 96 catalog controls rendered under the Ant theme across 13 navigable family pages, with a
passing coverage bijection.

**Independent Test**: `... -- coverage` → `96/96 mapped, 0 unreferenced, 0 duplicated`; walk pages in
`interactive`; `CoverageTests` + `PageRenderTests` green.

### Tests for User Story 1 ⚠️ (write first, must fail)

- [x] T011 [P] [US1] `AntShowcase.Tests/CoverageTests.fs`: assert `CoverageMap.check` is clean over `Catalog`-kind pages and that assigned ids == `Catalog.supportedControls` (96), zero unreferenced/duplicated (FR-003/SC-001).
- [x] T012 [P] [US1] `AntShowcase.Tests/PageRenderTests.fs`: assert every `Catalog` page's `view seed` produces a non-empty `Control` tree with no exception, under `AntTheme.resolve Light` (FR-001/FR-004); additionally assert **every page (Catalog + Template) is reachable in ≤2 nav actions** — i.e. each `PageRegistry.all` entry is a single direct selection in the nav rail (SC-008).

### Implementation for User Story 1

- [x] T013 [US1] Extend `AntShowcase.Core/DemoState.fs` with representative seeded content for **all** families incl. net-new Ant primitives (timeline/steps/collapse/segmented/rate/pagination/breadcrumb/tag/alert/card/avatar/list/tree/grid/chart) — literal constants only (research R5). (depends T009)
- [x] T014 [US1] Create `AntShowcase.Core/Pages.fs`: the **13 family pages** composing all 96 controls per contracts/page-registry.md (each `DemoState -> Control<AntShowcaseMsg>`). (depends T013, T008)
- [x] T015 [US1] Create `AntShowcase.Core/PageRegistry.fs`: `all = familyPages` tagged `Catalog` (templates appended in US2). (depends T014)
- [x] T016 [US1] Create `AntShowcase.Core/CoverageMap.fs`: `check`/`isClean`/`summary` over `Catalog`-kind pages vs `Catalog.supportedControls` (research R4; port from G1). (depends T015)
- [x] T017 [US1] Create `AntShowcase.Core/Shell.fs`: Ant-themed app bar + left nav rail (lists `PageRegistry.all`) + scrolling content (current page `view`) + status strip. (depends T015, T008)
- [x] T018 [US1] Create `AntShowcase.Core/Host.fs`: `InteractiveAppHost` bridge — `Init` (first page, `Light`, `seed`), pure `Update` (`Model.update`, effects `[]`), `View=Shell.view`, `Theme=AntTheme.resolve`. (depends T017)
- [x] T019 [US1] Create `AntShowcase.App/Program.fs` + `AntShowcase.App/Interactive.fs`: CLI `list` + `coverage` + `interactive [<page-id>]` (`runInteractiveApp`, GL-gated) per contracts/cli.md. (depends T018, T016)
- [x] T020 [US1] Create `samples/AntShowcase/coverage-report.md`: committed 96-control → family-page map (FR-003). (depends T016)

**Checkpoint**: MVP — browse all 96 controls Ant-styled; coverage green. Shippable on its own.

---

## Phase 4: User Story 2 - Demonstrate the Ant enterprise page templates (Priority: P2)

**Goal**: Six enterprise template pages (workbench/list/detail/form/result/exception) composed only of
catalog controls, with working form validation.

**Independent Test**: `TemplateTests` — each template composed of catalog controls; form rejects invalid /
succeeds on valid (FR-005/FR-006/SC-002/SC-009).

### Tests for User Story 2 ⚠️ (write first, must fail)

- [x] T021 [P] [US2] `AntShowcase.Tests/TemplateTests.fs`: for each of the 6 templates, assert every rendered node maps to a known `Catalog` id (no bespoke types); assert form `FormSubmitted` invalid → `Invalid` with `validation-message` nodes and **no** success `result`; valid → `Submitted` with a `result` node.

### Implementation for User Story 2

- [x] T022 [US2] Extend `AntShowcase.Core/DemoState.fs` with `FormState` seed + template demo data (workbench rows, list items+pagination, detail descriptions, result/exception states). (depends T013)
- [x] T023 [US2] Create `AntShowcase.Core/Templates.fs`: the 6 template pages per contracts/enterprise-templates.md, composed from catalog controls. (depends T022, T014)
- [x] T024 [US2] Extend `AntShowcase.Core/Model.fs` `update`: pure form transitions `Editing`/`Invalid errors`/`Submitted` (validation: empty Name, malformed Email, `Agree=false`) per data-model.md §5a. (depends T007)
- [x] T025 [US2] Update `AntShowcase.Core/PageRegistry.fs`: append `Templates.all` tagged `Template` (`ControlIds=[]`, exempt from the bijection — research R2). (depends T023, T015)
- [x] T026 [US2] Update `samples/AntShowcase/coverage-report.md` with the 6 template-pages section. (depends T025)

**Checkpoint**: Templates render populated; form validation behaves; coverage still green (templates exempt).

---

## Phase 5: User Story 3 - Switch between Ant light and dark (Priority: P2)

**Goal**: Runtime antLight↔antDark toggle; identical tree/a11y, only visuals differ; no theme-id branching.

**Independent Test**: `ThemeInvarianceTests` — same page under both variants has identical control tree +
accessibility metadata (FR-008/SC-003).

### Tests for User Story 3 ⚠️ (write first, must fail)

- [x] T027 [P] [US3] `AntShowcase.Tests/ThemeInvarianceTests.fs`: render representative pages under `antLight` and `antDark`; assert identical control-tree shape + accessibility metadata; assert no control inspects theme identity.

### Implementation for User Story 3

- [x] T028 [US3] Add `ToggleMode` handling to `Model.update` + an app-bar light/dark toggle control in `Shell.fs`. (depends T017, T007)
- [x] T029 [US3] Update `Host.fs` to re-resolve `Theme = AntTheme.resolve model.Mode` on mode change. (depends T018, T028)

**Checkpoint**: Toggle restyles the whole shell + controls; parity test green.

---

## Phase 6: User Story 4 - Deterministic, disclosed per-page evidence headlessly (Priority: P3)

**Goal**: `evidence --seed N` produces byte-identical, disclosed per-page records; degrades cleanly with no
display/GL.

**Independent Test**: run twice same seed → byte-identical; no-GL host → exit 0 + disclosed degrade
(FR-010/FR-011/FR-013/SC-004/SC-005).

### Tests for User Story 4 ⚠️ (write first, must fail)

- [x] T030 [P] [US4] `AntShowcase.Tests/DeterminismTests.fs`: run each page's seeded script via `Perf.runScript` twice; assert byte-identical `run.json`/`state.txt`.
- [x] T031 [P] [US4] `AntShowcase.Tests/DegradeTests.fs`: on a no-GL path assert `ProvesScreenshot=false`, non-empty `UnsupportedHostReason`, non-hanging success.

### Implementation for User Story 4

- [x] T032 [US4] Create `AntShowcase.Core/Scripts.fs`: per-page deterministic `FrameInput<AntShowcaseMsg> list` (no clock/RNG); the form page drives an invalid-then-valid sequence (research R7). (depends T015, T024)
- [x] T033 [US4] Create `AntShowcase.App/Evidence.fs`: headless `Perf.runScript` + `Viewer.captureScreenshotEvidence` + per-page record writer (depends T010, T032).
- [x] T034 [US4] Extend `AntShowcase.App/Program.fs`: `evidence --seed <N> [--page <id>]` dispatch + degrade-and-disclose exit handling per contracts/cli.md. (depends T033)

**Checkpoint**: Evidence reproducible byte-for-byte; clean disclosed skip on no-GL.

---

## Phase 7: User Story 5 - Exercise pointer and keyboard interaction (Priority: P3)

**Goal**: Interactive controls respond to seeded pointer/keyboard input per a documented contract.

**Independent Test**: `InteractionTests` — seeded input → visible state change; display-only controls exempt
(FR-014).

### Tests for User Story 5 ⚠️ (write first, must fail)

- [x] T035 [P] [US5] `AntShowcase.Tests/InteractionTests.fs`: drive seeded `Key`/`Pointer` inputs through `update` per contracts/interaction-contract.md; assert the documented visible state change for one representative control of each interactive family; assert a display-only (exempt) control renders unchanged.

### Implementation for User Story 5

- [x] T036 [US5] Extend `AntShowcase.Core/Host.fs` `mapKey`/`mapPointer` to route activation/selection/edit inputs to `PageMsg` per contracts/interaction-contract.md (input-mapping table + per-family behavior). (depends T018)

**Checkpoint**: Seeded interactions produce visible state changes.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [x] T037 [P] Write `samples/AntShowcase/PROVENANCE.md`: `FS.Skia.UI.*`→`FS.GG.UI.*` rebrand + template-recipe source disclosure (FR-017).
- [x] T038 [P] Write `samples/AntShowcase/README.md`: Ant-restyle rationale, the V0 feed-refresh precondition, two modes, how to run.
- [x] T039 [P] Optional ADR under `docs/product/decisions/`: new-sample-vs-extend-G1 decision (research R8).
- [x] T040 Run quickstart.md V0–V8 end-to-end; capture any disclosed degrade; confirm `dotnet test` suite green.
- [x] T041 [P] Final pass: confirm no `private`/`internal`/`public` modifiers on top-level bindings (Principle II, `FS0078`-clean) and no `Themes.Default` reference crept in.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (P1)** → **Foundational (P2)** → **User Stories (P3–P7)** → **Polish (P8)**.
- **T001 (feed refresh) blocks everything** — nothing restores without it.
- US1 (P3) is the MVP and must precede US2–US5 in practice (they extend its registry/state/host), though
  US3/US4/US5 are independently testable once US1's foundation exists.

### User story dependencies

- **US1 (P1)**: needs Foundational only. Delivers the MVP.
- **US2 (P2)**: extends US1's `PageRegistry`/`DemoState`/`Model.update` (templates + form).
- **US3 (P2)**: extends US1's `Shell`/`Host`/`Model` (mode toggle). Independent of US2.
- **US4 (P3)**: needs US1's registry + US2's form transitions (for the form script); App-side evidence.
- **US5 (P3)**: extends US1's `Host`. Independent of US2–US4.

### Within each story

- Tests first (must fail) → models/state → page/template builders → registry/coverage → shell/host → App CLI.
- F# compile order in the fsproj must follow: Model → AntTheme → DemoState → Pages → Templates →
  PageRegistry → CoverageMap → Scripts → Shell → Host → Evidence.

---

## Parallel Opportunities

- **Setup**: T003/T004/T005 (three fsprojs) in parallel after T002.
- **Foundational**: T007/T008 in parallel (different files).
- **Per story**: each story's test task(s) [P] can be authored in parallel with each other before that
  story's implementation begins.
- **Polish**: T037/T038/T039/T041 in parallel.

### Parallel example: User Story 1

```bash
# Tests first (parallel):
Task: "CoverageTests.fs over Catalog pages == 96, 0 unref/dup"
Task: "PageRenderTests.fs every Catalog page renders populated"
```

---

## Implementation Strategy

### MVP first (User Story 1 only)

1. Phase 1 Setup (esp. **T001 feed refresh**).
2. Phase 2 Foundational.
3. Phase 3 US1 → **STOP & VALIDATE**: `coverage` green, all 96 controls browsable under the Ant theme.
4. Demo the Ant-styled gallery (the headline G3 payoff).

### Incremental delivery

US1 (MVP) → US2 (templates) → US3 (light/dark) → US4 (evidence/CI path) → US5 (interaction) → Polish.
Each story is an independently testable increment; coverage stays green throughout.

---

## Notes

- 41 tasks total. Sample is a consumer (no `src/` refs, `IsPackable=false`); both product drift gates stay
  untouched (SC-007).
- Every module has a shipped G1 precedent at `samples/ControlsGallery/` — port, don't reinvent.
- Commit after each task or logical group. Verify story tests fail before implementing.
