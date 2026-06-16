---
description: "Task list for Ant Design Charts adoption (D2C.1)"
---

# Tasks: Ant Design Charts adoption (D2C.1)

**Input**: Design documents from `/specs/133-ant-design-charts/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: INCLUDED — the spec mandates them (FR-004 honesty check, FR-006 parity test, FR-005 contract
families) and constitution Principle I requires Spec → FSI → Semantic Tests → Implementation. Test tasks
are written to fail first, before the implementation they cover.

**Organization**: Tasks are grouped by user story (US1–US5) to enable independent implementation and
testing. The plan's internal phasing (P-A…P-D) maps to these stories.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1–US5 (Setup/Foundational/Polish carry no story label)
- Exact file paths are included in each task

## Path Conventions

Multi-project F# solution (`FS.GG.Rendering.slnx`). Controls live in `src/Controls/`; tests in
`tests/Controls.Tests/`; surface baselines in `tests/surface-baselines/`; Ant docs under
`docs/product/ant-design/`; decision records under `docs/product/decisions/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the ground the feature builds on; no behaviour change.

- [X] T001 Verify repo builds green and feature 132 is present: `dotnet build -c Debug`, and confirm `FS.GG.UI.Themes.AntDesign` exposes `AntTheme.antLight` (the reused Ant theme) in `src/Themes.AntDesign/`.
- [X] T002 [P] Locate and document the chart-styling seam: the geometry helpers `lineGeom`/`barGeom`/`pieGeom`/`scatterGeom`/`graphGeom` and `faithfulContent`/`richFamilies` in `src/Controls/Control.fs`, and the `palette`/`colorAt` theme-role helpers — confirm the existing five charts already colour their primary series from `theme.Accent` (input for US1).
- [X] T003 [P] Confirm the existing-precedent test trio compiles and runs as the authoring template: `tests/Controls.Tests/Feature132CoverageMatrixTests.fs`, `Feature132NewControlContractTests.fs`, `Feature132ThemeParityTests.fs`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Provenance + the pinned snapshot source that US2/US3 disposition against.

**⚠️ CRITICAL**: US2 (matrix) and US3 (net-new set) reference the pinned Ant Charts overview list created here.

- [X] T004 Add an **Ant Charts overview snapshot** section to the central reference hub `docs/product/ant-design/reference/ant-llms-sources.md` — the hub owns the `2026-06-16` retrieval date; record the Ant Charts overview families (statistical / relational / hierarchical / geo-flow / general) with no fabricated upstream version label (FR-011, research R6).
- [X] T005 Encode the **pinned Ant Charts overview entry list** (one canonical name per overview entry, grouped by family) as a shared fixture/list inside `tests/Controls.Tests/Feature133ChartCoverageMatrixTests.fs` — this is the snapshot the honesty check compares the matrix against (H1/H6).

**Checkpoint**: Snapshot pinned — user stories can proceed.

---

## Phase 3: User Story 1 - Existing charts render in Ant's visual language (Priority: P1) 🎯 MVP

**Goal**: The five existing chart controls (line/bar/pie/scatter/graph) render Ant-divergently under
`AntTheme` through theme roles, with the Default render path byte-identical (plan P-A).

**Independent Test**: Render the existing chart controls under Default vs Ant; Ant differs visibly
(palette/axis/grid/legend/mark colour), behaviour + accessibility identical, no Ant-specific chart type,
Default output unchanged byte-for-byte.

### Tests for User Story 1 (write first, must FAIL) ⚠️

- [X] T006 [US1] Create `tests/Controls.Tests/Feature133ChartParityTests.fs` scoped to the **existing five charts**: assert (a) Default vs Ant resolve ≥1 divergent visual property (mark/axis/grid/legend colour), (b) data/layout/accessibility identical across themes, (c) no chart control reads theme identity. Register the file in `tests/Controls.Tests/Controls.Tests.fsproj`. Run → confirm it FAILS/compiles-red before implementation.
- [X] T007 [P] [US1] Add a Default-byte-identical assertion (SC-004/FR-002) for the existing five charts in `Feature133ChartParityTests.fs`. Reference source: since T008 leaves the existing charts' Default-path geometry untouched (no change to the series≥2 literal palette), the assertion compares the Ant-unselected render to a scene captured from the **current `main` Default render** (snapshot the pre-edit `Control.render Themes.Default.Theme.light` output as the fixture, or assert structural identity to the unchanged geometry). Confirm it FAILS first if any Default-path change leaks.

### Implementation for User Story 1

- [X] T008 [US1] In `src/Controls/Control.fs`, confirm/extend `lineGeom`/`barGeom`/`pieGeom`/`scatterGeom`/`graphGeom` so axis/grid/legend/background read `theme.Foreground`/`theme.Muted`/`theme.Background` and the primary series reads `theme.Accent` — pure functions of `theme`, **no `theme.Name` branch** (FR-001/FR-007). Do NOT alter the existing literal categorical palette for series ≥2 (preserves Default bytes, SC-004/R2).
- [X] T009 [US1] Run `Feature133ChartParityTests.fs` (existing-charts scope) → green; verify the Default-byte-identical assertion (T007) passes (existing charts' Default render unchanged).

**Checkpoint**: MVP — existing charts visibly Ant-styled, opt-in, Default unchanged. Shippable on its own.

---

## Phase 4: User Story 2 - Maximal, honest coverage via a coverage matrix (Priority: P1)

**Goal**: A coverage matrix dispositions every Ant Charts overview entry, guarded by an automated honesty
check (plan P-B). Finalizes the net-new list for US3.

**Independent Test**: Exactly one matrix row per overview entry with a valid disposition; the honesty
check fails on any missing row, blank disposition, or dangling chart-control/token reference.

### Tests for User Story 2 (write first, must FAIL) ⚠️

- [X] T010 [US2] Implement the honesty check in `tests/Controls.Tests/Feature133ChartCoverageMatrixTests.fs` (extends the fixture from T005): parse `docs/product/ant-design/coverage/ant-chart-coverage.md` and assert H1 no-gaps, H2 valid disposition, H3 no dangling `repoControls` id (must exist in `Catalog`), H4 no dangling `tokenEntries` (must exist in `FS.GG.UI.DesignSystem` public surface via reflection over `DesignTokensExt`/`DesignTokens`, per research R5), H5 rationale present for `composition`/`not-applicable`, H6 zero un-dispositioned. Register in `Controls.Tests.fsproj`. Run → FAILS (matrix doc absent).

### Implementation for User Story 2

- [X] T011 [US2] Author `docs/product/ant-design/coverage/ant-chart-coverage.md` — header records the hub source + `2026-06-16` retrieval date (no fabricated version); one table row per pinned overview entry with columns `antChart | antCategory | disposition | repoControls | tokenEntries | rationale`; dispositions: `existing` (line/bar/pie/scatter/graph), `net-new` (the ~14 from research R3), `composition` (combo/dual-axis/stacked-grouped/bullet), `not-applicable` (geo/map/flow — rationale: geospatial dependency forbidden, FR-008). Add the per-disposition count summary line at the foot.
- [X] T012 [US2] Run `Feature133ChartCoverageMatrixTests.fs` → green; reconcile the foot-summary counts with the pinned snapshot-list size (SC-001/SC-002). The finalized `net-new` set is the input scope for US3.

**Checkpoint**: Matrix is honest and machine-checked; net-new control list frozen.

---

## Phase 5: User Story 3 - Net-new generic chart controls (Priority: P1)

**Goal**: Add the ~14 generic, theme-agnostic chart controls the matrix marks `net-new`
(`area-chart`, `column-chart`, `histogram`, `box-plot`, `heatmap`, `radar-chart`, `rose-chart`,
`waterfall-chart`, `funnel-chart`, `gauge-chart`, `sankey-diagram`, `chord-diagram`, `treemap`,
`sunburst`), each shaped like the existing chart controls (plan P-C).

**Independent Test**: Each net-new control is cataloged, renders coherently under BOTH themes from the
theme-role palette, carries accessibility metadata, passes the chart-control test families, with no
theme-identity branch.

### `.fsi` first (constitution Principle I/II)

- [X] T013 [US3] Create the curated signature `src/Controls/Charts2.fsi` declaring **one module per `net-new` row frozen by the matrix in T012** (provisional candidate set, ~14: `AreaChart`, `ColumnChart`, `Histogram`, `BoxPlot`, `Heatmap`, `RadarChart`, `RoseChart`, `WaterfallChart`, `FunnelChart`, `GaugeChart`, `SankeyDiagram`, `ChordDiagram`, `Treemap`, `Sunburst` — drop any the matrix reclassified to `composition`/`not-applicable`, per contract "split decided in P-B"). Each module: `val create: Attr<'msg> list -> Control<'msg>` plus chart-specific data helpers (`series`/`values`/`items`), per contract R2. Add `Charts2.fsi`/`Charts2.fs` to `src/Controls/Controls.fsproj` (after `Charts.fs`).

### Tests for User Story 3 (write first, must FAIL) ⚠️

- [X] T014 [US3] Create `tests/Controls.Tests/Feature133NewChartControlContractTests.fs` — a parameterized suite over **exactly the net-new ids frozen in T012** (the same set T013 declares — keep this list and `Charts2.fsi` in lock-step) asserting the chart-control families per contract R1/R3/R5/R6: Catalog registration, Semantic shape, Accessibility metadata, dual-theme Rendering (coherent under Default and Ant), and no `Model`/`Msg`/internal mutable state. Register in `Controls.Tests.fsproj`. Run → FAILS (controls absent).

### Implementation for User Story 3

- [X] T015 [US3] Add a `chartPalette: Theme -> Color list` pure helper (theme-role-derived: `[Accent; Danger; Success; Warning; Muted; Foreground]`) for the net-new charts in `src/Controls/Control.fs` — co-located with the chart geometry it feeds (single home; not duplicated in `Charts2.fs`) — theme-identity-blind (function of role *values*, not `Theme.Name`), per data-model §3. No inline hex/size at use sites.
- [X] T016 [US3] Implement the 14 chart bodies in `src/Controls/Charts2.fs` (`Control.create "<kind>" attrs` + data-attribute helpers), exactly like `LineChart` — pure render + parent-owned data, no theme branch (contract R4/R5).
- [X] T017 [US3] Wire render geometry in `src/Controls/Control.fs`: add each new kind to `richFamilies` and a geometry case in `faithfulContent` built from existing `Scene` primitives (`rectangle`/`path`/`circle`/`line`/`textRun`/`arc`), colouring via `chartPalette`/theme roles (research R4). Cover the edge cases: the defined empty/single-point placeholder state, and the **unknown-chart-kind** path resolving to a defined visible fallback so the resolver stays total (spec Edge Cases) — add a test asserting the unknown-kind fallback does not crash and renders the placeholder.
- [X] T018 [US3] Add `catalog.yml` rows for all 14 ids in `src/Controls/catalog.yml` (`category` `chart`/`graph`, `module` `Charts2`, minimal `requiredAttributes`, standard 8 `visualStates`, accessibility role + nameSource + stateMetadata + keyboard, `events` none, `supportStatus: supported`), then regenerate the GENERATED rows in `src/Controls/Catalog.fs` (hand-maintained per memory `catalog-no-generator`).
- [X] T019 [US3] Run `Feature133NewChartControlContractTests.fs` → all 14 green; confirm catalog/semantic/accessibility/rendering pass under both themes.

**Checkpoint**: All net-new charts exist, cataloged, dual-theme, contract-green.

---

## Phase 6: User Story 4 - "One chart set, many themes" parity proven (Priority: P2)

**Goal**: Harden the parity test to span every chart family (existing + net-new) and fail on any
theme-identity branch (plan P-D).

**Independent Test**: Parity test over a tree covering each family asserts contract-identity +
visual-divergence and passes; fails on any theme-identity branch.

### Tests for User Story 4 (extend existing, must FAIL before impl complete) ⚠️

- [X] T020 [US4] Extend `tests/Controls.Tests/Feature133ChartParityTests.fs` so the chart tree spans **every** chart family incl. the 14 net-new: assert behaviour/accessibility identical and ≥1 resolved visual property divergent under Default vs Ant (FR-006/SC-007), and a negative case that FAILS if a chart control reads theme identity (FR-007). Run → confirm coverage gaps surface before they pass.

### Implementation for User Story 4

- [X] T021 [US4] Resolve any divergence/contract-identity failures surfaced by T020 by routing the offending appearance through theme roles in `src/Controls/Control.fs` (never a `Theme.Name` branch); re-run `Feature133ChartParityTests.fs` → green across all families.

**Checkpoint**: The no-per-theme-chart-fork invariant is machine-proven for every family.

---

## Phase 7: User Story 5 - No charting-engine dependency (Priority: P3)

**Goal**: Prove the feature adds no JS/React/AntV/G2/G6/L7/geospatial dependency (plan US5).

**Independent Test**: Inspect project files/dependency graph; confirm no new charting/runtime package and
charts render through the existing Skia + F# path.

### Tests for User Story 5 (write first, must FAIL if a dep leaks) ⚠️

- [X] T022 [P] [US5] Add a dependency-guard assertion (Expecto test or inspection) that scans `src/Controls/Charts2.fs*` and `src/Controls/Controls.fsproj` for `antv|/g2|/g6|/l7|react|d3-|charting` and asserts zero matches (FR-008/SC-006); place in `tests/Controls.Tests/Feature133ChartCoverageMatrixTests.fs` or a small `Feature133NoChartDependencyTests.fs` registered in `Controls.Tests.fsproj`. Run → green (must stay green).

---

## Phase 8: Polish & Cross-Cutting Concerns (Tier-1 discipline + provenance)

**Purpose**: Land the Tier-1 surface/provenance artifacts and run the drift gates (FR-009/FR-010).

- [X] T023 Regenerate the Controls surface baseline: `dotnet fsi scripts/refresh-surface-baselines.fsx`, then `git diff --stat tests/surface-baselines/` — confirm `tests/surface-baselines/FS.GG.UI.Controls.txt` grows **only** by the net-new `Charts2` chart modules; no other baseline churn; no new package baseline (contract surface-baseline expectation).
- [X] T024 [P] Author the Tier-1 decision record `docs/product/decisions/0007-antdesign-charts-adoption.md` (next after 0006): design-language-only posture (no AntV/React/JS dep), net-new public chart-control surface delta, chosen Ant Charts snapshot via the hub, and the no-token-value-change / opt-in / no-fork / no-charting-dependency guarantees (FR-010, data-model §4).
- [X] T025 [P] Run the design-token-drift gate: `dotnet test tests/Controls.Tests -c Debug` (`DesignTokenParity`) — confirm zero existing token value changed; any new chart token entries are additive (FR-009/SC-005).
- [X] T026 Run the full validation per `quickstart.md` §1–§6: render under Ant vs Default, all three Feature133 suites green, baseline + token gates green, no-dependency grep clean — confirm SC-001…SC-007 all met. Also confirm the **charts-only scope** (FR-012): review `git diff --stat` and the regenerated baselines show changes confined to chart controls/geometry/docs/tests — no non-chart component surface changed, and the feature builds on feature 132 (`Themes.AntDesign`) already present.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS US2/US3 (they consume the pinned snapshot from T004/T005).
- **US1 (Phase 3)**: Depends only on Setup — independent of the pinned-list work; the MVP.
- **US2 (Phase 4)**: Depends on Foundational (T005 fixture).
- **US3 (Phase 5)**: Depends on Foundational; scope frozen by US2's matrix (T012). `.fsi` (T013) before tests (T014) before bodies/geometry (T016–T018).
- **US4 (Phase 6)**: Depends on US1 (parity file from T006) and benefits from US3 controls existing (full-family tree).
- **US5 (Phase 7)**: Depends on US3 (the `Charts2.fs`/proj it scans).
- **Polish (Phase 8)**: Depends on US3 (baseline) + all stories for the final gate run.

### Within Each User Story

- Tests are written and FAIL before implementation (constitution Principle I).
- `.fsi` before `.fs` (Principle II); catalog row before regenerated `Catalog.fs`.
- Geometry/palette before contract green.

### Parallel Opportunities

- T002, T003 (Setup) run in parallel.
- After Setup: **US1 (Phase 3) can run fully in parallel with Foundational+US2** — US1 touches the existing five charts' geometry and its own parity test; it does not need the pinned list.
- T024, T025 (Polish) run in parallel (different files); T023 must precede the final T026 gate.
- Within US3, T015 (palette helper) and T013 (`.fsi`) are independent; the 14 catalog rows can be drafted alongside the `.fsi`. Bodies (T016) and geometry (T017) both touch shared files (`Charts2.fs`, `Control.fs`) so they are **not** mutually parallel.

---

## Parallel Example: Setup + early MVP

```bash
# Setup tasks in parallel:
Task: "Locate chart-styling seam in src/Controls/Control.fs"           # T002
Task: "Confirm Feature132 precedent trio compiles in tests/Controls.Tests/"  # T003

# Then US1 (MVP) proceeds while Foundational+US2 are staffed separately:
Task: "Write Feature133ChartParityTests.fs (existing-charts scope), confirm red"  # T006
Task: "Add Default-byte-identical assertion for existing charts"                  # T007
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → 2. (US1 needs no Foundational) → 3. Phase 3 US1 → **STOP & VALIDATE**: existing charts
   visibly Ant-styled, opt-in, Default byte-identical. Shippable MVP (plan P-A).

### Incremental Delivery

1. Setup → US1 (MVP, P-A) → demo Ant-styled existing charts.
2. Foundational + US2 (P-B) → matrix honest + net-new list frozen.
3. US3 (P-C) → net-new controls land, baseline grows.
4. US4 (P-D) → full-family parity proven; US5 dependency guard; Polish (decision record, drift gates, quickstart).

### Suggested MVP Scope

**User Story 1** alone — proves the "design language, not a charting engine" approach over the controls
that already exist, before any net-new chart type.

---

## Notes

- [P] = different files, no incomplete-task dependency.
- Charts are pure render + parent-owned data — no Elmish `Model`/`Msg`, no internal mutable state (Principle IV).
- The central guardrail: appearance flows through theme roles/tokens; any `Theme.Name` branch fails the parity test (FR-007).
- Commit after each task or logical group; the `after_tasks` git hook is offered below.
- Verify each test fails before implementing it.
