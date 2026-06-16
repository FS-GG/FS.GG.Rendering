# Feature Specification: Ant Design Charts adoption (D2C.1)

**Feature Branch**: `133-ant-design-charts`

**Created**: 2026-06-16

**Status**: Draft

**Input**: User description: "continue with charts"

## Context

This is the **charts follow-up** recorded alongside the D2.1 widened-component-coverage feature
(132, FR-019) and captured in the active implementation plan as **Phase D2-Charts / task D2C.1**
(`docs/reports/2026-06-15-11-34-missing-features-implementation-plan.md`, §7.4b). It extends the
framework's existing chart controls toward the Ant Design Charts catalog, adopting Ant Design Charts
**as a design language only** — a chart-type catalog plus a token/visual mapping over the repo's own
chart controls and the Ant-derived token taxonomy. There is **no JS/React/AntV charting dependency**;
charts continue to render through the existing Skia + F# chart-control path. It is sequenced after
D2.1 (the concrete Ant theme, `FS.GG.UI.Themes.AntDesign`) and reuses that theme + the
`StyleResolver`/token seams — no new theme package and no chart-control forks.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - App author renders charts in Ant's visual language without forking chart controls (Priority: P1) 🎯 MVP

An app author selects the Ant theme and the existing chart controls (line/bar/pie/scatter/graph)
render in Ant Design Charts' visual language — Ant brand palette, axis/grid/legend styling, mark
colours — sourced entirely from the Ant-derived token taxonomy through the shared resolver/token
seams. Opt-in; with the Ant theme unselected, chart output is byte-identical to today.

**Why this priority**: This is the standalone MVP — it delivers visible Ant-styled charts over the
controls that already exist, proving the "design language, not a charting engine" approach before any
net-new chart type is added.

**Independent Test**: Render a tree of the existing chart controls under Default vs the Ant theme;
confirm (a) the Ant render differs visibly (palette/axis/grid/legend/mark colour), (b) behaviour and
accessibility are identical, (c) no chart control type is Ant-specific, (d) Default output unchanged.

**Acceptance Scenarios**:

1. **Given** a `line-chart` authored with sample series, **When** it is resolved under the Ant theme,
   **Then** its mark/axis/legend colours derive from Ant token entries and differ from the Default
   render, with the same data, layout behaviour, and accessibility metadata.
2. **Given** the same chart with the Ant theme **unselected**, **When** it is rendered, **Then** the
   output is byte-identical to the pre-feature output (opt-in, no regression).

---

### User Story 2 - Maximal, honest Ant Charts coverage via a coverage matrix (Priority: P1)

A maintainer opens a chart-type coverage matrix that dispositions **every** Ant Design Charts overview
entry (statistical, relational, hierarchical, and geo/flow families) as `existing` / `net-new` /
`composition` / `not-applicable`, guarded by an automated honesty check so "maximal coverage" stays
truthful against the live chart-control and token surface.

**Why this priority**: Charts span dozens of types; without an enumerable, machine-checked matrix the
breadth claim is unverifiable and drifts silently. The matrix also finalizes the net-new chart-control
list for US3.

**Independent Test**: Open the matrix; confirm exactly one row per Ant Charts overview entry with a
valid disposition; the honesty check fails on any missing row, blank disposition, or dangling
chart-control/token reference.

**Acceptance Scenarios**:

1. **Given** the pinned Ant Charts overview snapshot list, **When** the honesty check runs, **Then** it
   fails if any entry lacks a matrix row, any row lacks a valid disposition, or any covered row names a
   chart control absent from the catalog or a token entry absent from the design-system surface.
2. **Given** a covered row, **When** validated, **Then** its referenced chart-control id(s) and token
   entries all resolve against the live public surface.

---

### User Story 3 - Net-new generic chart controls fill the Ant Charts gaps (Priority: P1)

The generic, theme-agnostic chart controls the Ant Charts overview needs but the library lacks (e.g.
area, column, histogram, box-plot, radar, funnel, gauge, heatmap, treemap, sunburst, sankey, …) are
added, each cataloged and passing the same chart-control test families as the existing chart controls;
styled by both themes, theme-aware in neither.

**Why this priority**: Styling the existing five charts cannot reach maximal coverage; the high-value
Ant Charts types with no repo analog must exist as generic controls to close the gap honestly.

**Independent Test**: For each net-new chart control, confirm catalog registration, coherent render
under BOTH themes with sample data, accessibility metadata, and passing chart-control test families;
no Ant-specific branching in the control.

**Acceptance Scenarios**:

1. **Given** a net-new chart control (e.g. `area-chart`) authored with sample data, **When** rendered
   under Default and the Ant theme, **Then** it produces a coherent chart in both, differing only in
   token-sourced appearance, with no theme-identity branch.
2. **Given** any net-new chart control, **When** the chart-control test families run, **Then** it passes
   catalog/semantic/accessibility/rendering checks like every existing chart control.

---

### User Story 4 - "One chart set, many themes" parity is proven (Priority: P2)

A reviewer runs a parity test over a representative tree spanning every chart family (existing and
net-new) and confirms it renders behaviour-identically and visually-divergently under Default vs the
Ant theme, and fails if any chart control branches on theme identity.

**Why this priority**: It machine-proves the central layering invariant for charts (no per-theme chart
forks) and hardens coverage so it is not silently narrowed to the easy chart types.

**Independent Test**: Run the parity test over a chart tree covering each family; confirm it asserts
contract-identity + visual-divergence and passes, and fails on any theme-identity branch.

**Acceptance Scenarios**:

1. **Given** a chart tree spanning every chart family, **When** resolved under Default and Ant,
   **Then** behaviour/accessibility are asserted identical and ≥1 resolved visual property divergent.
2. **Given** a chart control that reads theme identity to branch, **When** the parity test runs,
   **Then** it fails.

---

### User Story 5 - No charting-engine dependency is introduced (Priority: P3)

A maintainer confirms the feature adds **no** JS/React/AntV (G2/G6/L7) charting dependency: charts are
realized purely through the existing Skia + F# chart-control rendering path and the token taxonomy.

**Why this priority**: It guards the adoption posture (design language, not realization mechanism) so
the package graph and build stay dependency-clean.

**Independent Test**: Inspect the dependency graph and project files; confirm no new charting/runtime
package was added and charts render through the existing chart-control path.

**Acceptance Scenarios**:

1. **Given** the feature's project files, **When** the dependency graph is inspected, **Then** no
   AntV/G2/G6/L7/React/JS charting dependency is present.

---

### Edge Cases

- **Empty / single-point series**: every chart control (existing and net-new) renders a defined,
  non-crashing empty/placeholder state.
- **Chart type with no clean primitive**: dispositioned `composition` (e.g. a combo/dual-axis chart as
  a composition of existing charts) with documented rationale, not a faked primitive.
- **Geo / flow charts requiring external map tiles or projections**: dispositioned `not-applicable`
  (or `composition`/deferred) with rationale — no map-tile or geospatial dependency is introduced.
- **Very large series**: rendering remains a deterministic, bounded schematic (no new hot path).
- **Unknown chart kind**: resolves to a defined visible fallback (the resolver stays total).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The existing chart controls MUST render in Ant Design Charts' visual language when the
  Ant theme is selected, with all chart appearance (palette, axis, grid, legend, mark colours) sourced
  from Ant-derived token entries through the shared resolver/token seams — **no chart-control fork**.
- **FR-002**: Ant chart styling MUST be opt-in; with the Ant theme unselected, chart render output is
  byte-identical to the pre-feature output.
- **FR-003**: The feature MUST provide a coverage matrix with exactly one row per Ant Design Charts
  overview entry, each dispositioned `existing` / `net-new` / `composition` / `not-applicable`.
- **FR-004**: An automated honesty check MUST fail on any missing matrix row, blank/invalid
  disposition, or any covered row naming a chart-control id absent from the catalog or a token entry
  absent from the design-system public surface.
- **FR-005**: Net-new chart controls MUST be generic and theme-agnostic, registered in the catalog,
  and pass the same chart-control test families as existing chart controls; they MUST render coherently
  under **both** themes and read theme identity in **neither**.
- **FR-006**: A parity test MUST prove that one chart tree (spanning every chart family) renders
  behaviour/accessibility-identically and with ≥1 divergent resolved visual property under Default vs
  the Ant theme.
- **FR-007**: No chart control may branch on theme identity (the parity test enforces this).
- **FR-008**: The feature MUST NOT add any JS/React/AntV (G2/G6/L7) charting or geospatial dependency;
  charts render through the existing Skia + F# chart-control path only.
- **FR-009**: The feature MUST NOT change any existing design-token value; any genuinely new chart
  token entries are additive (the design-token-drift gate stays green).
- **FR-010**: New public chart modules MUST follow Tier-1 discipline — a curated signature per public
  module, surface baselines regenerated and committed in the same change, and a decision record landed
  in lock-step.
- **FR-011**: The Ant Charts overview snapshot MUST be recorded via the central Ant reference hub
  (which owns the retrieval date); the matrix MUST NOT fabricate an upstream version label.
- **FR-012**: The feature scope MUST be charts only — no non-chart component work — and MUST be
  sequenced after D2.1 (feature 132).

### Key Entities

- **Ant chart-type coverage matrix row**: one Ant Design Charts overview entry → `antChart`,
  `antCategory` (statistical/relational/hierarchical/geo-flow/other), `disposition`, `repoControls`
  (chart-control id(s)), `tokenEntries` (≥1 design-token entry the styling uses), `rationale`
  (required for `composition`/`not-applicable`).
- **Net-new chart control catalog entry**: a generic chart control (id, category `chart`/`graph`,
  module, required/common attributes, standard visual states, accessibility, events, `supported`
  status) shaped exactly like an existing chart control.
- **Ant chart token mapping**: the application of Ant-derived token entries (palette/axis/grid/legend
  /mark roles) to chart geometry through the resolver/token seams — no inline literals at use sites.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of Ant Design Charts overview entries (the pinned snapshot list) have exactly one
  matrix row with a valid disposition — zero un-dispositioned entries.
- **SC-002**: 100% of covered matrix rows reference only chart-control ids that exist in the catalog
  and token entries that exist in the design-system public surface (zero dangling references).
- **SC-003**: 100% of net-new chart controls pass every chart-control test family and render coherently
  under both Default and the Ant theme.
- **SC-004**: With the Ant theme unselected, chart render output is byte-identical to the pre-feature
  reference (no chart fork; opt-in proven).
- **SC-005**: Zero existing design-token values change (design-token-drift gate green).
- **SC-006**: Zero new charting/geospatial (JS/React/AntV/G2/G6/L7) dependencies are introduced.
- **SC-007**: The chart parity test passes — one chart tree, both themes, behaviour/accessibility
  identical and ≥1 visual property divergent — and fails on any theme-identity branch.

## Assumptions

- Ant Design Charts is adopted **as a design language only**: a chart-type catalog + token/visual
  mapping over the repo's existing chart controls and the `DesignTokensExt`/`DesignTokens` taxonomy.
  No AntV (G2/G6/L7), React, or JS charting runtime is used or referenced.
- The Ant chart visual styling rides the **existing** `FS.GG.UI.Themes.AntDesign` theme + the
  `StyleResolver`/token seams (no new theme package); net-new generic chart controls live in
  `FS.GG.UI.Controls` alongside the existing chart controls, consistent with feature 132.
- The Ant Design Charts overview is a distinct product (AntV-based) from the Ant Design components
  covered by the existing reference hub; this feature pins the charts-overview snapshot through the
  same central hub (adding a charts snapshot section if needed), with the hub owning the retrieval
  date. No upstream version label is fabricated.
- "Maximal coverage in one feature" follows the D2.1 precedent: the coverage matrix + honesty check
  preserve honesty while net-new generic chart controls close the high-value gaps; chart types whose
  value does not justify a primitive are dispositioned `composition`, and map/geospatial-dependent
  charts are `not-applicable`/deferred.
- The existing chart controls (`line-chart`, `bar-chart`, `pie-chart`, `scatter-plot`, `graph-view`)
  and the existing Skia + F# chart-control rendering path are reused unchanged in behaviour.
- Scope is charts only and is sequenced after D2.1 (feature 132); enterprise kits (Phase D3) and
  other concrete themes remain out of scope.
