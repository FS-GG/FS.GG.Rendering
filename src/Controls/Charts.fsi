namespace FS.Skia.UI.Controls

// `ChartPoint` / `ChartSeries` are declared in Types.fsi (feature 080, surface-neutral move).

/// Line-chart control plotting one or more `ChartSeries` as connected lines;
/// author it through the typed `Props` front door.
module LineChart =
    /// Builds a `LineChart` `Control` from the given attributes.
    val create: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the line-chart's `series` data to plot.
    val series: ChartSeries list -> Attr<'msg>

/// Bar-chart control rendering each `ChartSeries` as grouped bars; author it
/// through the typed `Props` front door.
module BarChart =
    /// Builds a `BarChart` `Control` from the given attributes.
    val create: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the bar-chart's `series` data to render as bars.
    val series: ChartSeries list -> Attr<'msg>

/// Pie-chart control rendering `ChartPoint` values as proportional slices;
/// author it through the typed `Props` front door.
module PieChart =
    /// Builds a `PieChart` `Control` from the given attributes.
    val create: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the pie-chart's `values`, each a slice of the whole.
    val values: ChartPoint list -> Attr<'msg>

/// Scatter-plot control rendering each `ChartSeries` as discrete points;
/// author it through the typed `Props` front door.
module ScatterPlot =
    /// Builds a `ScatterPlot` `Control` from the given attributes.
    val create: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the scatter-plot's `series` of points to plot.
    val series: ChartSeries list -> Attr<'msg>

/// Graph-view control rendering a set of named nodes and their relationships;
/// author it through the typed `Props` front door.
module GraphView =
    /// Builds a `GraphView` `Control` from the given attributes.
    val create: Attr<'msg> list -> Control<'msg>
    /// Attribute supplying the graph's `nodes` by name.
    val nodes: string list -> Attr<'msg>
