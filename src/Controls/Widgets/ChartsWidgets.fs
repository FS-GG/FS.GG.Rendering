namespace FS.Skia.UI.Controls.Typed

open FS.Skia.UI.Controls

type LineChartProps<'msg> =
    { Id: ControlId option
      Series: ChartSeries list
      OnSelected: (string -> 'msg) option }

type BarChartProps<'msg> =
    { Id: ControlId option
      Series: ChartSeries list
      OnSelected: (string -> 'msg) option }

type PieChartProps<'msg> =
    { Id: ControlId option
      Values: ChartPoint list
      OnSelected: (string -> 'msg) option }

type ScatterPlotProps<'msg> =
    { Id: ControlId option
      Series: ChartSeries list
      OnSelected: (string -> 'msg) option }

type GraphViewProps<'msg> =
    { Id: ControlId option
      Nodes: string list
      OnSelected: (string -> 'msg) option }

// File-private lowering helpers. Charts/graph reuse the existing `ChartSeries`/
// `ChartPoint` data types and lower to the dedicated legacy `*.create` in
// Charts.fsi. Hidden by absence from ChartsWidgets.fsi.
module ChartLowering =
    let eventAttrs (onSelected: (string -> 'msg) option) : Attr<'msg> list =
        match onSelected with
        | Some map -> [ WidgetLowering.onString "onSelected" map ]
        | None -> []

module LineChart =
    let defaults: LineChartProps<'msg> = { Id = None; Series = []; OnSelected = None }

    let view (props: LineChartProps<'msg>) : Widget<'msg> =
        FS.Skia.UI.Controls.LineChart.create
            (FS.Skia.UI.Controls.LineChart.series props.Series
             :: ChartLowering.eventAttrs props.OnSelected)
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module BarChart =
    let defaults: BarChartProps<'msg> = { Id = None; Series = []; OnSelected = None }

    let view (props: BarChartProps<'msg>) : Widget<'msg> =
        FS.Skia.UI.Controls.BarChart.create
            (FS.Skia.UI.Controls.BarChart.series props.Series
             :: ChartLowering.eventAttrs props.OnSelected)
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module PieChart =
    let defaults: PieChartProps<'msg> = { Id = None; Values = []; OnSelected = None }

    let view (props: PieChartProps<'msg>) : Widget<'msg> =
        FS.Skia.UI.Controls.PieChart.create
            (FS.Skia.UI.Controls.PieChart.values props.Values
             :: ChartLowering.eventAttrs props.OnSelected)
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module ScatterPlot =
    let defaults: ScatterPlotProps<'msg> = { Id = None; Series = []; OnSelected = None }

    let view (props: ScatterPlotProps<'msg>) : Widget<'msg> =
        FS.Skia.UI.Controls.ScatterPlot.create
            (FS.Skia.UI.Controls.ScatterPlot.series props.Series
             :: ChartLowering.eventAttrs props.OnSelected)
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module GraphView =
    let defaults: GraphViewProps<'msg> = { Id = None; Nodes = []; OnSelected = None }

    let view (props: GraphViewProps<'msg>) : Widget<'msg> =
        FS.Skia.UI.Controls.GraphView.create
            (FS.Skia.UI.Controls.GraphView.nodes props.Nodes
             :: ChartLowering.eventAttrs props.OnSelected)
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl
