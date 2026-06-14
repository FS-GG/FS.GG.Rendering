namespace FS.Skia.UI.Controls

open System

// `ChartPoint` / `ChartSeries` are defined in Types.fs (feature 080) so the renderer in
// Control.fs — which compiles before this file — can read them. The authoring modules stay
// here.

module ChartAttrs =
    let finite values =
        values |> List.filter Double.IsFinite

    let seriesValues (values: ChartSeries list) =
        values |> List.collect (fun series -> series.Points |> List.map _.Y) |> finite

    let pointValues (values: ChartPoint list) =
        values |> List.map _.Y |> finite

    let series (values: ChartSeries list) = Attr.create "series" Data (UntypedValue values)
    let points (values: ChartPoint list) = Attr.create "values" Data (UntypedValue values)
    let nodes (values: string list) = Attr.create "nodes" Data (StringListValue values)

module LineChart =
    let create attrs = Control.create "line-chart" attrs
    let series values = ChartAttrs.series values

module BarChart =
    let create attrs = Control.create "bar-chart" attrs
    let series values = ChartAttrs.series values

module PieChart =
    let create attrs = Control.create "pie-chart" attrs
    let values values = ChartAttrs.points values

module ScatterPlot =
    let create attrs = Control.create "scatter-plot" attrs
    let series values = ChartAttrs.series values

module GraphView =
    let create attrs = Control.create "graph-view" attrs
    let nodes values = ChartAttrs.nodes values
