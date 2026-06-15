namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

/// Immutable, compiler-checked authoring surface for a line chart. `series` required.
/// Reuses the existing `ChartSeries` data type. `OnSelected = None` lowers to no binding.
type LineChartProps<'msg> =
    { Id: ControlId option
      Series: ChartSeries list
      OnSelected: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a bar chart. `series` required.
type BarChartProps<'msg> =
    { Id: ControlId option
      Series: ChartSeries list
      OnSelected: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a pie chart. `values` required.
type PieChartProps<'msg> =
    { Id: ControlId option
      Values: ChartPoint list
      OnSelected: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a scatter plot. `series` required.
type ScatterPlotProps<'msg> =
    { Id: ControlId option
      Series: ChartSeries list
      OnSelected: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a graph view. `nodes` required.
type GraphViewProps<'msg> =
    { Id: ControlId option
      Nodes: string list
      OnSelected: (string -> 'msg) option }

/// Typed Props front door for the `LineChart` control.
module LineChart =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: LineChartProps<'msg>
    /// Lowers structurally equal to `LineChart.create [ LineChart.series props.Series ]`.
    val view: props: LineChartProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `BarChart` control.
module BarChart =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: BarChartProps<'msg>
    /// Lowers structurally equal to `BarChart.create [ BarChart.series props.Series ]`.
    val view: props: BarChartProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `PieChart` control.
module PieChart =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: PieChartProps<'msg>
    /// Lowers structurally equal to `PieChart.create [ PieChart.values props.Values ]`.
    val view: props: PieChartProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `ScatterPlot` control.
module ScatterPlot =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: ScatterPlotProps<'msg>
    /// Lowers structurally equal to `ScatterPlot.create [ ScatterPlot.series props.Series ]`.
    val view: props: ScatterPlotProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `GraphView` control.
module GraphView =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: GraphViewProps<'msg>
    /// Lowers structurally equal to `GraphView.create [ GraphView.nodes props.Nodes ]`.
    val view: props: GraphViewProps<'msg> -> Widget<'msg>
