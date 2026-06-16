# 0007. Ant Design Charts adoption — design language only (D2C.1)

**Status**: accepted
**Date**: 2026-06-16

## Decision

Extend the framework's chart controls toward the **Ant Design Charts** catalog, adopting Ant Design
Charts **as a design language only** — a machine-checked chart-type coverage matrix plus a token/visual
mapping over the repo's own chart controls. **No AntV (G2/G6/L7), React, or JS charting/geospatial
dependency is added**; charts render through the existing Skia + F# chart-control path. This is a
**Tier 1** change: new public chart controls in `FS.GG.UI.Controls` (new `.fsi`, grown surface
baseline, this decision record) — but **no new package** (the Ant chart styling rides the existing
`FS.GG.UI.Themes.AntDesign` theme from feature 132 / decision
[0006](./0006-antdesign-theme-and-new-controls.md)).

Four sub-decisions:

1. **Existing charts diverge through theme roles, opt-in, Default byte-identical.** The five existing
   chart controls (`line-chart`/`bar-chart`/`pie-chart`/`scatter-plot`/`graph-view`) already colour
   their primary series from `theme.Accent` and their axis/grid/legend from `theme.Foreground`/
   `Muted`/`Background` — so under `AntTheme` they render Ant-divergently with **no** change to their
   Default render path (their literal categorical palette for series ≥2 is untouched). SC-004 holds by
   construction; the parity test proves no chart branches on theme identity.

2. **Net-new charts are generic and theme-agnostic.** Fourteen high-value Ant Charts types with no
   repo analog are added to `FS.GG.UI.Controls` as generic kind-string controls (authored exactly like
   `LineChart`), grouped into one new `Charts2` module pair. Their render geometry is a pure schematic
   built from existing `Scene` primitives, coloured from a **theme-role-derived palette**
   (`[Accent; Danger; Success; Warning; Muted; Foreground]`) — a pure function of role *values*, never
   `Theme.Name`. State is parent-owned via data attributes; no `Model`/`Msg`/`Effect`, no internal
   mutable state (Constitution IV).

3. **"Maximal" is kept honest by a machine-checked coverage matrix.** Every Ant Charts overview entry
   (pinned 27-entry snapshot, retrieved 2026-06-16) gets exactly one row in
   `docs/product/ant-design/coverage/ant-chart-coverage.md` with a disposition of `existing` /
   `net-new` / `composition` / `not-applicable`. The honesty check
   `Feature133ChartCoverageMatrixTests` fails on any missing row, dangling chart-control/token
   reference, blank disposition, or missing rationale. Disposition totals: **5 existing, 14 net-new,
   4 composition, 4 not-applicable**. Geo/flow charts are `not-applicable` (they need a
   geospatial/map-tile dependency this feature forbids); combo/dual-axis/stacked-grouped/bullet are
   `composition` over existing charts.

4. **No token-value change; no charting dependency.** The net-new chart palette helper is additive and
   theme-role-derived (it adds nothing to any token store), so the design-token-drift gate stays
   green. A dependency guard (`Feature133ChartCoverageMatrixTests`) scans `Charts2.fs*` +
   `Controls.fsproj` and fails on any `antv`/`/g2`/`/g6`/`/l7`/`react`/`d3-`/`charting` match.

## Public surface delta

**New public chart controls in `FS.GG.UI.Controls` (14 net-new ids, module `Charts2`):**

- `AreaChart` (`area-chart`), `ColumnChart` (`column-chart`), `Histogram` (`histogram`),
  `BoxPlot` (`box-plot`), `Heatmap` (`heatmap`), `RadarChart` (`radar-chart`),
  `RoseChart` (`rose-chart`), `WaterfallChart` (`waterfall-chart`), `FunnelChart` (`funnel-chart`),
  `GaugeChart` (`gauge-chart`), `SankeyDiagram` (`sankey-diagram`), `ChordDiagram` (`chord-diagram`),
  `Treemap` (`treemap`), `Sunburst` (`sunburst`).

Each has a catalog row (`catalog.yml` + the GENERATED rows in `Catalog.fs`), a curated `Charts2.fsi`,
and is rendered through the kind-string dispatch in `Control.fs`'s `faithfulContent`. The
`FS.GG.UI.Controls` surface baseline is regenerated + committed in this change, growing **only** by the
`Charts2` chart modules. **No new package baseline** (the Ant chart styling reuses `Themes.AntDesign`).

## Ant snapshot

The Ant Charts overview and tokens trace to the central reference hub
[`../ant-design/reference/ant-llms-sources.md`](../ant-design/reference/ant-llms-sources.md) (Ant
Charts overview snapshot section), whose **snapshot retrieval date `2026-06-16`** is the single owner
of provenance. Ant Design Charts is a distinct AntV-based product from the Ant Design components the
hub already covers; it publishes no upstream version label in its LLM docs, so none is restated here.

## Rationale

A theme-role-styling + net-new-generic-charts split (rather than a theme-only or composition-only
coverage, both declined) is the only route to maximal Ant-Charts coverage without per-theme chart
forks. The coverage matrix + honesty check make the breadth auditable; the chart parity test proves
the layering invariant ("one chart set, many themes; no chart branches on theme identity"); the
dependency guard proves the "design language, not a charting engine" posture.

## Consequences

- Consumers get visibly Ant-styled charts by selecting `AntTheme.antLight`/`antDark`, with no chart
  code changes; with the Ant theme unselected, the existing charts render byte-identically to before.
- The standard control set grows from 82 to 96 supported controls (14 net-new charts).
- Geo/flow chart families remain deferred (`not-applicable`) until a geospatial dependency is
  separately justified; combo/stacked variants are expressed by composition.
