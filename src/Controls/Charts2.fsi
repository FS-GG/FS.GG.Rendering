namespace FS.GG.UI.Controls

/// Feature 133 (D2C.1): net-new generic chart controls filling Ant Design Charts overview gaps that
/// the existing five charts (line/bar/pie/scatter/graph) do not cover. Each is generic and
/// theme-agnostic — pure render + data attributes (the parent owns the data); no internal mutable
/// state. They render neutrally under `Themes.Default` and Ant-styled under `Themes.AntDesign`
/// through a theme-role-derived palette, branching on no theme identity. `ChartPoint`/`ChartSeries`
/// are declared in `Types.fsi` (feature 080).

/// Area chart — a filled region under a series outline.
module AreaChart =
    /// Builds an `area-chart` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the area chart's `series` data to plot.
    val series: ChartSeries list -> Attr<'msg>

/// Column chart — vertical categorical bars.
module ColumnChart =
    /// Builds a `column-chart` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the column chart's `series` data to render as columns.
    val series: ChartSeries list -> Attr<'msg>

/// Histogram — adjacent frequency bins over a continuous range.
module Histogram =
    /// Builds a `histogram` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the histogram's bin `values`.
    val values: ChartPoint list -> Attr<'msg>

/// Box plot — a box-and-whisker distribution summary per category.
module BoxPlot =
    /// Builds a `box-plot` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the box plot's `series` of category distributions.
    val series: ChartSeries list -> Attr<'msg>

/// Heatmap — a colour-intensity grid of cell values.
module Heatmap =
    /// Builds a `heatmap` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the heatmap cell `values`.
    val values: ChartPoint list -> Attr<'msg>

/// Radar chart — a multi-axis value polygon.
module RadarChart =
    /// Builds a `radar-chart` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the radar chart's per-axis `values`.
    val values: ChartPoint list -> Attr<'msg>

/// Rose chart — Nightingale polar-area sectors.
module RoseChart =
    /// Builds a `rose-chart` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the rose chart's sector `values`.
    val values: ChartPoint list -> Attr<'msg>

/// Waterfall chart — running cumulative deltas as floating bars.
module WaterfallChart =
    /// Builds a `waterfall-chart` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the waterfall chart's step `values` (signed deltas).
    val values: ChartPoint list -> Attr<'msg>

/// Funnel chart — a centred trapezoid stack of narrowing stages.
module FunnelChart =
    /// Builds a `funnel-chart` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the funnel chart's stage `values`.
    val values: ChartPoint list -> Attr<'msg>

/// Gauge chart — a 180° dial showing a single fraction in [0,1].
module GaugeChart =
    /// Builds a `gauge-chart` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the gauge's `value` as a fraction in [0,1].
    val value: float -> Attr<'msg>

/// Sankey diagram — flow bands between source/target node columns.
module SankeyDiagram =
    /// Builds a `sankey-diagram` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the diagram's `nodes` by name.
    val nodes: string list -> Attr<'msg>

/// Chord diagram — relationships between nodes arranged on a ring.
module ChordDiagram =
    /// Builds a `chord-diagram` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the diagram's `nodes` by name.
    val nodes: string list -> Attr<'msg>

/// Treemap — value-proportional nested rectangles.
module Treemap =
    /// Builds a `treemap` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the treemap tile `values`.
    val values: ChartPoint list -> Attr<'msg>

/// Sunburst — a radial hierarchy of value-proportional ring segments.
module Sunburst =
    /// Builds a `sunburst` `Control` from the given attributes.
    val create: attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the sunburst segment `values`.
    val values: ChartPoint list -> Attr<'msg>
