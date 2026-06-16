# Quickstart / Validation Guide: Ant Design Charts adoption (D2C.1)

Prerequisites: .NET `net10.0` SDK; repo builds green (`dotnet build -c Debug`); feature 132
(`FS.GG.UI.Themes.AntDesign`) present. All commands from repo root.

## 1. Render the existing charts under the Ant theme (US1)

In FSI or a sample, render an existing chart control under the Default vs the Ant theme:

```fsharp
open FS.GG.UI.Controls
open FS.GG.UI.Themes.AntDesign

let chart = LineChart.create [ LineChart.series sampleSeries ]
let d = Control.render FS.GG.UI.Themes.Default.Theme.light chart   // unchanged Default render
let a = Control.render AntTheme.antLight chart                     // Ant brand-blue primary series, etc.
// a's scene differs from d's (≥1 mark/axis colour); d matches today's output byte-for-byte.
```

**Expected**: under `AntTheme.antLight` the primary series + axis/grid/legend colours derive from Ant
token roles and differ from Default; with the Ant theme unselected, the chart render is byte-identical
to today (opt-in, no regression).

## 2. Run the chart-parity test (US4 / SC-007)

```bash
dotnet test tests/Controls.Tests -c Debug   # filter: Feature 133 chart parity
```

**Expected**: one chart tree spanning every chart family (existing + net-new) resolves under Default and
Ant; behaviour/accessibility asserted identical, ≥1 visual property asserted divergent; fails if any
chart control branches on theme identity.

## 3. Run the per-control contract tests for net-new charts (US3 / SC-003)

```bash
dotnet test tests/Controls.Tests -c Debug   # filter: Feature 133 net-new chart contract
```

**Expected**: every net-new chart control passes the Catalog/Semantic/Accessibility/Rendering families;
each renders coherently under both themes from the theme-role-derived palette.

## 4. Run the chart coverage-matrix honesty check (US2 / SC-001, SC-002)

```bash
dotnet test tests/Controls.Tests -c Debug   # filter: Feature 133 chart coverage matrix
```

**Expected**: passes only when every Ant Charts overview entry has a disposition and every covered row
references a chart-control id in `Catalog` and a token entry in the `DesignSystem` surface; fails on any
gap or dangling reference.

## 5. Surface + token drift gates (SC-004, SC-005)

```bash
dotnet build -c Debug
dotnet fsi scripts/refresh-surface-baselines.fsx       # regenerates baselines (Controls grows with new charts)
git diff --stat tests/surface-baselines/                # review: only the net-new chart modules under Charts2
dotnet test tests/Controls.Tests -c Debug               # DesignTokenParity: no token value change
```

**Expected**: `FS.GG.UI.Controls.txt` grows only by the net-new `Charts2` chart modules; no other
baseline churn; no new package baseline; design-token-drift green (no existing token value changed). With
the Ant theme unselected, the existing five charts' rendering is byte-identical.

## 6. No-charting-dependency guard (US5 / SC-006)

```bash
grep -RinE "antv|/g2|/g6|/l7|react|d3-|charting" src/Controls/Charts2.fs* src/Controls/Controls.fsproj
```

**Expected**: no AntV/G2/G6/L7/React/JS charting or geospatial dependency; charts render through the
existing Skia + F# chart-control path only.
