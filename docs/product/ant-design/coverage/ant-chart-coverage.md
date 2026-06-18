# Ant Charts coverage matrix (D2C.1)

**Ant source**: the repo Ant reference hub — [`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md) (Ant Charts overview snapshot section).
**Snapshot retrieval date**: `2026-06-16` (owned by the hub; this matrix does not restate an upstream version label — Ant publishes none in its LLM docs).

One row per Ant Design Charts overview entry. `disposition` ∈ {`existing`, `net-new`, `composition`, `not-applicable`}.
`repoControls` ids resolve in `FS.GG.UI.Controls.Catalog`; `tokenEntries` resolve in the `FS.GG.UI.DesignSystem` public token surface.
The honesty check `tests/Controls.Tests/Feature133ChartCoverageMatrixTests.fs` fails on any missing row, dangling control/token reference, blank disposition, or missing rationale.

Ant Design Charts is adopted **as a design language only** — no AntV (G2/G6/L7), React, or JS charting/geospatial dependency. The five existing repo charts plus the net-new generic charts render every covered family through the existing Skia + F# path, coloured from theme roles/tokens.

| antChart | antCategory | disposition | repoControls | tokenEntries | rationale |
|---|---|---|---|---|---|
| Line | Statistical | existing | line-chart | Seed.colorPrimary, Alias.Light.borderDefault | — |
| Area | Statistical | net-new | area-chart | Seed.colorPrimary, Alias.Light.surfaceContainer | new generic filled-area trend chart |
| Column | Statistical | net-new | column-chart | Seed.colorPrimary | new generic vertical-bar chart |
| Bar | Statistical | existing | bar-chart | Seed.colorPrimary | — |
| Pie | Statistical | existing | pie-chart | Seed.colorPrimary | — |
| Scatter | Statistical | existing | scatter-plot | Seed.colorPrimary, Alias.Light.borderDefault | — |
| Histogram | Statistical | net-new | histogram | Seed.colorPrimary | new generic frequency-distribution chart |
| Box Plot | Statistical | net-new | box-plot | Seed.colorPrimary, Alias.Light.textSecondary | new generic distribution-summary chart |
| Heatmap | Statistical | net-new | heatmap | Seed.colorPrimary, Alias.Light.surfaceContainer | new generic intensity-grid chart |
| Radar | Statistical | net-new | radar-chart | Seed.colorPrimary | new generic multi-axis polygon chart |
| Rose | Statistical | net-new | rose-chart | Seed.colorPrimary | new generic polar-area (Nightingale) chart |
| Waterfall | Statistical | net-new | waterfall-chart | Seed.colorSuccess, Seed.colorError | new generic cumulative-delta chart |
| Funnel | Statistical | net-new | funnel-chart | Seed.colorPrimary | new generic stage-conversion chart |
| Dual Axes | Statistical | composition | line-chart, bar-chart | Seed.colorPrimary | two existing charts share an axis frame; no new primitive needed |
| Stacked Column | Statistical | composition | bar-chart, column-chart | Seed.colorPrimary | a data-layering variant of the existing/net-new bar charts; no new primitive |
| Grouped Column | Statistical | composition | bar-chart, column-chart | Seed.colorPrimary | a grouped variant composed from the existing/net-new bar charts |
| Bullet | Statistical | composition | progress-bar, gauge-chart | Seed.colorPrimary | a reference-line gauge composed from existing controls |
| Sankey | Relational | net-new | sankey-diagram | Seed.colorPrimary, Alias.Light.borderDefault | new generic flow diagram |
| Chord | Relational | net-new | chord-diagram | Seed.colorPrimary, Alias.Light.borderDefault | new generic relationship ring |
| Network Graph | Relational | existing | graph-view | Seed.colorPrimary, Alias.Light.textDefault | — |
| Treemap | Hierarchical | net-new | treemap | Seed.colorPrimary, Alias.Light.surfaceContainer | new generic nested-rectangle chart |
| Sunburst | Hierarchical | net-new | sunburst | Seed.colorPrimary | new generic radial-hierarchy chart |
| Choropleth Map | Geo-Flow | not-applicable | — | — | needs a geospatial/map-tile dependency this feature forbids |
| Point Map | Geo-Flow | not-applicable | — | — | needs a geospatial/map-tile dependency this feature forbids |
| Heatmap Map | Geo-Flow | not-applicable | — | — | needs a geospatial/map-tile dependency this feature forbids |
| Flow Map | Geo-Flow | not-applicable | — | — | needs a geospatial/map-tile dependency this feature forbids |
| Gauge | General | net-new | gauge-chart | Seed.colorPrimary, Alias.Light.borderDefault | new generic single-value dial |

## Summary

Total Ant Charts overview entries dispositioned: **27** (zero un-dispositioned — SC-001).

- existing: 5
- net-new: 14
- composition: 4
- not-applicable: 4
