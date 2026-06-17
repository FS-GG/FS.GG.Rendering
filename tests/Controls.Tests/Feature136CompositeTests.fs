module Feature136CompositeTests

// Feature 136 (US3): composite-control structure. Pre-fix, a data-grid stacked vertically, menu/combo
// rows collapsed onto a shared baseline in a short box, descriptions ran past their box, a compressed
// qr-code collapsed to blank, and charts overran or crashed on degenerate data. These tests render
// each control through the public `renderTree` and assert the corrected structure.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open Rendering.Harness.TestAssertions

let private theme = Theme.light
let private sz w h : Size = { Width = w; Height = h }
let private render size control = (Control.renderTree theme size control).Scene

let rec private hasClip (s: Scene) =
    s.Nodes
    |> List.exists (function
        | ClipNode _ -> true
        | Group g -> g |> List.exists hasClip
        | Translate(_, s)
        | ColorSpaceNode(_, s)
        | PerspectiveNode(_, s) -> hasClip s
        | _ -> false)

[<Tests>]
let tests =
    testList
        "Feature136 composite controls (US3)"
        [ // T027
          test "data-grid renders columns side-by-side; header cell N aligned with body cell N" {
              let scene = render (sz 240 160) (Control.create "data-grid" [])
              let texts = renderedText scene
              let xOf t = texts |> List.tryFind (fun r -> r.Text = t) |> Option.map (fun r -> r.X)
              // default cells (cols=2): row0 = Name | Qty ; body rows include Widget | 12
              match xOf "Name", xOf "Widget", xOf "Qty", xOf "12" with
              | Some nameX, Some widgetX, Some qtyX, Some twelveX ->
                  Expect.floatClose Accuracy.medium nameX widgetX "header col0 X aligns with body col0 X"
                  Expect.floatClose Accuracy.medium qtyX twelveX "header col1 X aligns with body col1 X"
                  Expect.isTrue (nameX < qtyX) "columns are side-by-side (col0 left of col1)"
              | _ -> failtest "expected the default data-grid cells to render as a table"
          }

          // T028
          test "menu items occupy distinct y-bands (no shared baseline) even in a short box" {
              let menu = Control.create "menu" [ Attr.items [ "Cut"; "Copy"; "Paste"; "Delete"; "Rename" ] ]
              // A short box: the naive box.Height/n would collapse rows onto one baseline.
              let ys =
                  render (sz 160 40) menu
                  |> renderedText
                  |> List.map (fun r -> System.Math.Round(r.Y, 1))
                  |> List.distinct

              Expect.isTrue (ys.Length >= 5) "each of the 5 items has a distinct baseline"
          }

          // T029
          test "descriptions stay within the box; qr-code yields a populated grid in a small box" {
              let box = sz 200 80
              let desc = Control.create "descriptions" [ Attr.items [ "Name"; "Ant"; "Status"; "Active"; "Owner"; "FS" ] ]

              for t in renderedText (render box desc) do
                  Expect.isTrue (t.Y <= float box.Height + 1.0) (sprintf "description '%s' stays within the box" t.Text)

              let qrBounds = drawnBounds (render (sz 24 24) (Control.create "qr-code" []))
              Expect.isNonEmpty qrBounds "qr-code renders a non-empty module grid even in a small box"
          }

          // T030
          test "chart degenerate data is finite-guarded and the body is clipped to its box" {
              let pts =
                  [ { X = 0.0; Y = nan; Label = None }
                    { X = 1.0; Y = infinity; Label = None }
                    { X = 2.0; Y = 5.0; Label = None } ]

              let chart = PieChart.create [ PieChart.values pts ]
              Expect.equal (ControlInternals.chartValues chart).Length 1 "non-finite (NaN/Inf) points are dropped"

              let empty = PieChart.create [ PieChart.values [] ]
              Expect.equal (ControlInternals.chartValues empty) [] "empty chart data yields no points"
              Expect.isTrue (hasClip (render (sz 120 120) empty)) "chart geometry is clipped to its box (no overrun)"
          } ]
