module Feature080ExtractionTests

// Feature 080 (US? foundation, FR-002 / T005) — failing-first extraction test.
//
// Root cause (Control.fs:159): `chartValues` matched only `float list`/`float array`/
// `FloatValue`, but the typed chart controls store `UntypedValue(ChartSeries list)` under
// "series" and `UntypedValue(ChartPoint list)` under "values". So a chart built through the
// real typed front door yielded `[]` — every chart preview drew nothing and collapsed to a
// label-on-a-box. These tests assert the structured points are extracted with X/Y/Label
// preserved; they are RED against the pre-fix matcher (yields `[]`) and GREEN after T006.
//
// `ControlInternals` is internal to FS.GG.UI.Controls and reachable here via the
// `InternalsVisibleTo("Controls.Tests")` item in Controls.fsproj (same access path the
// reconciler tests use).

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed

[<Tests>]
let chartValuesExtractionTests =
    testList "Feature 080 chartValues extraction (T005, FR-002)" [
        test "line-chart from a typed ChartSeries yields all points with X/Y preserved" {
            let series: ChartSeries list =
                [ { Name = "Sales"
                    Points =
                      [ { X = 0.0; Y = 3.0; Label = None }
                        { X = 1.0; Y = 7.0; Label = None }
                        { X = 2.0; Y = 5.0; Label = None }
                        { X = 3.0; Y = 9.0; Label = None } ] } ]

            let control = LineChart.view { LineChart.defaults with Series = series } |> Widget.toControl
            let points = ControlInternals.chartValues control

            Expect.equal (List.length points) 4 "chartValues yields all four series points (pre-fix: [])"
            Expect.equal (points |> List.map (fun p -> p.Y)) [ 3.0; 7.0; 5.0; 9.0 ] "Y values preserved in order"
            Expect.equal (points |> List.map (fun p -> p.X)) [ 0.0; 1.0; 2.0; 3.0 ] "X values preserved in order"
        }

        test "bar-chart from a typed ChartSeries yields its points" {
            let series: ChartSeries list =
                [ { Name = "Q"; Points = [ { X = 0.0; Y = 2.0; Label = None }; { X = 1.0; Y = 6.0; Label = None } ] } ]

            let control = BarChart.view { BarChart.defaults with Series = series } |> Widget.toControl
            let points = ControlInternals.chartValues control
            Expect.equal (points |> List.map (fun p -> p.Y)) [ 2.0; 6.0 ] "bar magnitudes preserved"
        }

        test "pie-chart from typed ChartPoint values yields its points with labels" {
            let values: ChartPoint list =
                [ { X = 0.0; Y = 30.0; Label = Some "A" }
                  { X = 1.0; Y = 50.0; Label = Some "B" }
                  { X = 2.0; Y = 20.0; Label = Some "C" } ]

            let control = PieChart.view { PieChart.defaults with Values = values } |> Widget.toControl
            let points = ControlInternals.chartValues control

            Expect.equal (points |> List.map (fun p -> p.Y)) [ 30.0; 50.0; 20.0 ] "pie slice magnitudes preserved"
            Expect.equal (points |> List.choose (fun p -> p.Label)) [ "A"; "B"; "C" ] "pie slice labels preserved"
        }

        test "flat float-list fallback still extracts (legacy authoring)" {
            // The legacy untyped path may still carry a flat float list; the fix keeps that
            // fallback, mapping each value to a ChartPoint with X = index.
            let control =
                LineChart.create [ Attr.create "series" Data (UntypedValue([ 1.0; 4.0; 9.0 ])) ]

            let points = ControlInternals.chartValues control
            Expect.equal (points |> List.map (fun p -> p.Y)) [ 1.0; 4.0; 9.0 ] "flat-list fallback preserved"
            Expect.equal (points |> List.map (fun p -> p.X)) [ 0.0; 1.0; 2.0 ] "flat-list X defaults to index"
        }
    ]
