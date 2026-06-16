# Contract: net-new generic chart controls in `FS.GG.UI.Controls`

Net-new chart controls are **generic and theme-agnostic** (no Ant naming, no theme branching). Each is
added as: a `catalog.yml` entry → regenerated `Catalog.fs` GENERATED row → a curated `.fsi` → a `.fs`
body, grouped into the new `Charts2` module pair, plus a geometry case in `Control.fs`.

## Per-control requirements

- **R1 (catalog parity)**: the control appears in `catalog.yml` with `id`, `category` (`chart`/`graph`),
  `module`, required/common attributes, the standard 8 `visualStates`, `accessibility` metadata,
  `events`, `supportStatus: supported` — same schema as the existing chart rows. `Catalog.fs` regenerated.
- **R2 (`.fsi` first)**: a curated signature exists before the `.fs`; visibility lives in the `.fsi`.
  Module shape follows the existing chart controls (e.g. `LineChart`):

  ```fsharp
  module AreaChart =
      val create: attrs: Attr<'msg> list -> Control<'msg>
      val series: ChartSeries list -> Attr<'msg>   // + chart-specific data helpers
  ```

- **R3 (dual-theme render)**: renders coherently under `Themes.Default` (neutral) and `Themes.AntDesign`
  (Ant-styled). All appearance differences come from the theme-role-derived palette + resolver/tokens,
  not the control.
- **R4 (no fork)**: the control never reads theme identity to alter behaviour (the chart parity test
  enforces this).
- **R5 (state)**: charts are pure render + data attributes (parent owns the data); no `Model`/`Msg`/
  `Effect`, no ad-hoc internal mutable state.
- **R6 (test families)**: passes the chart-control families — Catalog / Semantic / Accessibility /
  Rendering (dual-theme) — same as every existing chart control.

## Candidate net-new chart control ids (finalized in P-B against the matrix)

`area-chart`, `column-chart`, `histogram`, `box-plot`, `heatmap`, `radar-chart`, `rose-chart`,
`waterfall-chart`, `funnel-chart`, `gauge-chart`, `sankey-diagram`, `chord-diagram`, `treemap`,
`sunburst`.

> The exact net-new vs composition split is decided when the matrix is authored (P-B). Rule: add a
> primitive only when a composition of existing chart controls cannot express the chart cleanly.
> Combo/dual-axis/stacked-grouped variants may resolve to `composition`; geo/map charts to
> `not-applicable` (no geospatial dependency, FR-008).

## Surface-baseline expectation

`tests/surface-baselines/FS.GG.UI.Controls.txt` regenerated — the only new rows are the net-new chart
control modules under `Charts2`. No incidental surface leaks (a baseline diff review is part of the
change). No new package baseline (the Ant chart styling reuses `Themes.AntDesign`).
