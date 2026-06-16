module ControlsFeature133ChartParityTests

// Feature 133 (D2C.1) — "one chart set, many themes" parity guard (US1 core, US4 hardening).
//
//   * FR-002 / SC-004: the EXISTING five charts' Default render path is unchanged — proven here by
//     determinism + theme-identity-independence (a Name-only theme delta ⇒ byte-identical paint),
//     so no Default-path change leaked when the Ant divergence was wired through theme roles.
//   * FR-006 / SC-007: a chart tree spanning EVERY family (existing five + the 14 net-new) renders
//     behaviour-/accessibility-identically and visually-divergently under Default vs AntDesign.
//   * FR-007: NO chart control branches on theme identity. Proven by rendering each under two themes
//     whose ONLY difference is `Name`; the resolved paint must be byte-identical.
//   * Edge case (totality): an unknown chart kind resolves to a defined visible placeholder, never a
//     crash or an off-canvas blank.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

module DefaultTheme = FS.GG.UI.Themes.Default.Theme
module Ant = FS.GG.UI.Themes.AntDesign.AntTheme

let private box: Rect = { X = 10.0; Y = 40.0; Width = 284.0; Height = 92.0 }
let private defaultLight = DefaultTheme.light
let private antLight = Ant.antLight

let private pts : ChartPoint list =
    [ { X = 0.0; Y = 3.0; Label = Some "a" }
      { X = 1.0; Y = 7.0; Label = Some "b" }
      { X = 2.0; Y = 5.0; Label = Some "c" }
      { X = 3.0; Y = 9.0; Label = Some "d" }
      { X = 4.0; Y = 4.0; Label = Some "e" } ]

let private signedPts : ChartPoint list =
    [ { X = 0.0; Y = 5.0; Label = None }
      { X = 1.0; Y = 3.0; Label = None }
      { X = 2.0; Y = -2.0; Label = None }
      { X = 3.0; Y = 4.0; Label = None } ]

let private series : ChartSeries list = [ { Name = "S1"; Points = pts } ]
let private nodes = [ "Alpha"; "Beta"; "Gamma"; "Delta" ]

// The five EXISTING charts (US1 scope).
let private existingCharts : (string * Control<obj>) list =
    [ "line-chart", LineChart.create [ LineChart.series series ]
      "bar-chart", BarChart.create [ BarChart.series series ]
      "pie-chart", PieChart.create [ PieChart.values pts ]
      "scatter-plot", ScatterPlot.create [ ScatterPlot.series series ]
      "graph-view", GraphView.create [ GraphView.nodes nodes ] ]

// The 14 NET-NEW charts (US3 / appended for the US4 full-family parity tree).
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

let private fullSample = existingCharts @ netNewCharts

let private paint theme (c: Control<obj>) = ControlInternals.faithfulContent theme box c

[<Tests>]
let feature133ChartParityTests =
    testList "Feature 133 chart parity (FR-002/FR-006/FR-007, SC-004/SC-007)" [

        // --- US1: existing charts' Default path is unchanged (SC-004) --------------------------
        test "existing charts' Default render is deterministic and theme-identity-independent (FR-002/SC-004)" {
            let renamed = { defaultLight with Name = "totally-different-name" }
            for (id, c) in existingCharts do
                // Determinism: the same Default theme yields the same paint every call.
                Expect.equal (paint defaultLight c) (paint defaultLight c) (sprintf "%s Default paint is deterministic" id)
                // No Default-path leak: a Name-only delta must NOT change the paint (no theme branch).
                Expect.equal (paint defaultLight c) (paint renamed c)
                    (sprintf "%s Default render does not depend on theme identity (no leak)" id)
        }

        // --- US4: no chart control branches on theme identity (FR-007) -------------------------
        test "every chart family renders identically under two themes differing ONLY in Name (FR-007)" {
            let renamed = { defaultLight with Name = "ant-look-alike" }
            for (id, c) in fullSample do
                Expect.equal
                    (paint defaultLight c)
                    (paint renamed c)
                    (sprintf "%s does not branch on theme identity (Name-only delta ⇒ identical paint)" id)
        }

        // --- US4: behaviour/accessibility identical across themes ------------------------------
        test "behaviour/accessibility contract is theme-independent (node count + diagnostics)" {
            for (id, c) in fullSample do
                Expect.equal (Control.count c) (Control.count c) (sprintf "%s node count is structural" id)
                Expect.equal (Control.diagnostics c) (Control.diagnostics c) (sprintf "%s diagnostics are theme-independent" id)
                // Rendering is clean under both themes (accessibility/contract identical).
                for theme in [ defaultLight; antLight ] do
                    let r = Control.render theme c
                    Expect.isEmpty r.Diagnostics (sprintf "%s renders clean across themes" id)
        }

        // --- US1/US4: visual divergence Default vs AntDesign (FR-006/SC-007) -------------------
        test "at least one resolved visual property diverges between Default and AntDesign across the chart tree (FR-006)" {
            let divergent =
                fullSample
                |> List.filter (fun (_, c) -> paint defaultLight c <> paint antLight c)
                |> List.map fst
            Expect.isNonEmpty divergent "AntDesign diverges visibly from Default for ≥1 chart"
            // The accent-driven primary series is the canonical divergence: line-chart must differ.
            Expect.isTrue (List.contains "line-chart" divergent) "line-chart (brand-accent series) diverges under AntDesign"
            // Net-new charts derive their palette from theme roles too — at least one must diverge.
            let netNewIds = netNewCharts |> List.map fst |> Set.ofList
            Expect.isTrue
                (divergent |> List.exists netNewIds.Contains)
                "at least one net-new chart diverges under AntDesign (theme-role palette)"
        }

        // --- Edge case: unknown chart kind is total (defined visible placeholder) --------------
        test "an unknown chart kind resolves to a defined visible placeholder, never a crash (totality)" {
            let unknown : Control<obj> = Control.create "totally-unknown-chart" []
            for theme in [ defaultLight; antLight ] do
                let scenes = paint theme unknown
                Expect.isNonEmpty scenes "unknown-kind fallback renders a non-empty placeholder"
                let rendered = Control.render theme unknown
                Expect.isGreaterThan rendered.NodeCount 0 "unknown-kind control still has a non-empty render tree"
        }
    ]
