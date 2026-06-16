namespace FS.GG.UI.Controls

// Feature 133 (D2C.1): authoring modules for the net-new generic chart controls. The render geometry
// lives in Control.fs (theme-role-driven schematics); these modules are the thin typed front door —
// a kind-string `Control.create` plus the chart's data-attribute helper, exactly like `LineChart`.
// The data attributes mirror `ChartAttrs` (Charts.fs) — `series`/`values` ride `UntypedValue`,
// `nodes` rides `StringListValue`, a scalar gauge `value` rides `FloatValue`.

module AreaChart =
    let create attrs = Control.create "area-chart" attrs
    let series (values: ChartSeries list) = Attr.create "series" Data (UntypedValue values)

module ColumnChart =
    let create attrs = Control.create "column-chart" attrs
    let series (values: ChartSeries list) = Attr.create "series" Data (UntypedValue values)

module Histogram =
    let create attrs = Control.create "histogram" attrs
    let values (values: ChartPoint list) = Attr.create "values" Data (UntypedValue values)

module BoxPlot =
    let create attrs = Control.create "box-plot" attrs
    let series (values: ChartSeries list) = Attr.create "series" Data (UntypedValue values)

module Heatmap =
    let create attrs = Control.create "heatmap" attrs
    let values (values: ChartPoint list) = Attr.create "values" Data (UntypedValue values)

module RadarChart =
    let create attrs = Control.create "radar-chart" attrs
    let values (values: ChartPoint list) = Attr.create "values" Data (UntypedValue values)

module RoseChart =
    let create attrs = Control.create "rose-chart" attrs
    let values (values: ChartPoint list) = Attr.create "values" Data (UntypedValue values)

module WaterfallChart =
    let create attrs = Control.create "waterfall-chart" attrs
    let values (values: ChartPoint list) = Attr.create "values" Data (UntypedValue values)

module FunnelChart =
    let create attrs = Control.create "funnel-chart" attrs
    let values (values: ChartPoint list) = Attr.create "values" Data (UntypedValue values)

module GaugeChart =
    let create attrs = Control.create "gauge-chart" attrs
    let value (fraction: float) = Attr.create "value" Content (FloatValue fraction)

module SankeyDiagram =
    let create attrs = Control.create "sankey-diagram" attrs
    let nodes (values: string list) = Attr.create "nodes" Data (StringListValue values)

module ChordDiagram =
    let create attrs = Control.create "chord-diagram" attrs
    let nodes (values: string list) = Attr.create "nodes" Data (StringListValue values)

module Treemap =
    let create attrs = Control.create "treemap" attrs
    let values (values: ChartPoint list) = Attr.create "values" Data (UntypedValue values)

module Sunburst =
    let create attrs = Control.create "sunburst" attrs
    let values (values: ChartPoint list) = Attr.create "values" Data (UntypedValue values)
