module ControlsFeature133NewChartControlContractTests

// Feature 133 (D2C.1) — the net-new chart-control contract suite (US3, contract R1/R3/R5/R6).
// Parameterized over EXACTLY the 14 net-new chart ids frozen by the coverage matrix (kept in
// lock-step with `Charts2.fsi`), exercising the chart-control families every existing chart passes —
// Catalog, Semantic, Accessibility, and a DUAL-THEME Rendering (Default neutral + AntDesign
// Ant-styled) — plus the "pure render, no Model/Msg state" invariant (R5).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

module DefaultTheme = FS.GG.UI.Themes.Default.Theme
module Ant = FS.GG.UI.Themes.AntDesign.AntTheme

let private pts : ChartPoint list =
    [ { X = 0.0; Y = 3.0; Label = Some "a" }
      { X = 1.0; Y = 7.0; Label = Some "b" }
      { X = 2.0; Y = 5.0; Label = Some "c" }
      { X = 3.0; Y = 9.0; Label = Some "d" }
      { X = 4.0; Y = 4.0; Label = Some "e" } ]

let private signedPts : ChartPoint list =
    [ { X = 0.0; Y = 5.0; Label = Some "open" }
      { X = 1.0; Y = 3.0; Label = Some "up" }
      { X = 2.0; Y = -2.0; Label = Some "down" }
      { X = 3.0; Y = 4.0; Label = Some "up" } ]

let private series : ChartSeries list = [ { Name = "S1"; Points = pts } ]
let private nodes = [ "Alpha"; "Beta"; "Gamma"; "Delta" ]

// Every net-new chart id with a minimally-attributed instance. The kind string IS the catalog id.
let private netNewCharts : (string * Control<obj>) list =
    [ "area-chart", AreaChart.create [ AreaChart.series series ]
      "column-chart", ColumnChart.create [ ColumnChart.series series ]
      "histogram", Histogram.create [ Histogram.values pts ]
      "box-plot", BoxPlot.create [ BoxPlot.series series ]
      "heatmap", Heatmap.create [ Heatmap.values pts ]
      "radar-chart", RadarChart.create [ RadarChart.values pts ]
      "rose-chart", RoseChart.create [ RoseChart.values pts ]
      "waterfall-chart", WaterfallChart.create [ WaterfallChart.values signedPts ]
      "funnel-chart", FunnelChart.create [ FunnelChart.values pts ]
      "gauge-chart", GaugeChart.create [ GaugeChart.value 0.65 ]
      "sankey-diagram", SankeyDiagram.create [ SankeyDiagram.nodes nodes ]
      "chord-diagram", ChordDiagram.create [ ChordDiagram.nodes nodes ]
      "treemap", Treemap.create [ Treemap.values pts ]
      "sunburst", Sunburst.create [ Sunburst.values pts ] ]

let private rowsById = Catalog.supportedControls |> List.map (fun r -> r.Id, r) |> Map.ofList

let private standardStates =
    set [ "normal"; "disabled"; "hover"; "pressed"; "focused"; "selected"; "validation"; "loading" ]

let private mkEvent kind : ControlEvent =
    { Kind = kind; ControlId = None; Origin = ControlEventOrigin.Pointer; Nav = None }

[<Tests>]
let feature133NewChartControlContractTests =
    testList "Feature 133 net-new chart control contract (R1/R3/R5/R6)" [

        test "exactly the 14 frozen net-new chart ids are exercised (lock-step with Charts2.fsi)" {
            Expect.equal (List.length netNewCharts) 14 "the suite covers the 14 net-new chart controls"
        }

        // --- Catalog family -------------------------------------------------------------------
        testList "Catalog" [
            for (id, _) in netNewCharts do
                test (sprintf "%s has a complete, supported catalog row" id) {
                    match Map.tryFind id rowsById with
                    | None -> failtestf "%s is missing from Catalog.supportedControls" id
                    | Some row ->
                        Expect.isFalse (System.String.IsNullOrWhiteSpace row.Purpose) (sprintf "%s has a purpose" id)
                        Expect.equal (Set.ofList row.VisualStates) standardStates (sprintf "%s declares the standard 8 visual states" id)
                        Expect.equal row.SupportStatus "supported" (sprintf "%s is supported" id)
                        Expect.equal row.Owner "controls" (sprintf "%s is Controls-owned" id)
                        Expect.contains (Catalog.categories ()) row.Category (sprintf "%s category is a known catalog category" id)
                }
        ]

        // --- Semantic family ------------------------------------------------------------------
        testList "Semantic" [
            for (id, ctrl) in netNewCharts do
                test (sprintf "%s authors with the expected kind and is diagnostic-clean" id) {
                    Expect.equal ctrl.Kind id (sprintf "%s control carries kind '%s'" id id)
                    Expect.isGreaterThan (Control.count ctrl) 0 (sprintf "%s has at least one node" id)
                    let errors = Control.diagnostics ctrl |> List.filter (fun d -> d.Severity = Error)
                    Expect.isEmpty errors (sprintf "%s authors with no Error diagnostics" id)
                }
        ]

        // --- Accessibility family -------------------------------------------------------------
        testList "Accessibility" [
            for (id, _) in netNewCharts do
                test (sprintf "%s advertises accessibility role + state metadata" id) {
                    let row = rowsById.[id]
                    Expect.isFalse (System.String.IsNullOrWhiteSpace row.Accessibility.Role) (sprintf "%s has an accessibility role" id)
                    Expect.isNonEmpty row.Accessibility.StateMetadata (sprintf "%s reports state metadata" id)
                    Expect.isFalse (System.String.IsNullOrWhiteSpace row.Accessibility.KeyboardOperation) (sprintf "%s documents keyboard operation" id)
                }
        ]

        // --- Rendering family (DUAL THEME, R3) ------------------------------------------------
        testList "Rendering (dual theme)" [
            for (id, ctrl) in netNewCharts do
                test (sprintf "%s renders coherently under BOTH Default and AntDesign" id) {
                    for (tname, theme) in [ "Default", DefaultTheme.light; "AntDesign", Ant.antLight ] do
                        let rendered = Control.render theme ctrl
                        Expect.isEmpty rendered.Diagnostics (sprintf "%s renders with no diagnostics under %s" id tname)
                        Expect.isGreaterThan rendered.NodeCount 0 (sprintf "%s has a non-empty render tree under %s" id tname)
                        let evidence = Scene.renderReadbackEvidence { Width = 320; Height = 160 } rendered.Scene
                        Expect.isNonEmpty evidence.DeterministicHash (sprintf "%s produces deterministic render evidence under %s" id tname)
                }
        ]

        // --- State invariant (R5): charts are pure render + data — no event wiring ------------
        testList "Pure render (no Model/Msg state)" [
            for (id, ctrl) in netNewCharts do
                test (sprintf "%s dispatches nothing (presentational, parent owns data)" id) {
                    Expect.isEmpty (Control.dispatch (mkEvent "click") ctrl) (sprintf "%s has no event handlers" id)
                }
        ]
    ]
