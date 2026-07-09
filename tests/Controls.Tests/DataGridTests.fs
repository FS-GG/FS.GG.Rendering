module ControlsDataGridTests

open System.Diagnostics
open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

// Aliased, not opened: `TestAssertions` publishes a `SceneEvidenceFailureClassification` whose cases
// shadow the `ControlDiagnosticCode` ones this file matches on.
module TestAssertions = Rendering.Harness.TestAssertions

type GridMsg =
    | RowSelected of string

let columns =
    [ { Key = "id"; Header = "Id"; Width = 72.0; ColumnType = TextColumn }
      { Key = "name"; Header = "Name"; Width = 180.0; ColumnType = TextColumn }
      { Key = "amount"; Header = "Amount"; Width = 96.0; ColumnType = NumericColumn } ]

let row index =
    let key = sprintf "row-%05d" index

    { Key = key
      Cells =
        [ { RowKey = key; ColumnKey = "id"; Value = string index }
          { RowKey = key; ColumnKey = "name"; Value = $"Customer {index}" }
          { RowKey = key; ColumnKey = "amount"; Value = string (index * 3) } ] }

[<Tests>]
let dataGridTests =
    testList "Controls DataGrid scalability" [
        test "large DataGrid keeps visible range bounded at ten thousand rows" {
            let stopwatch = Stopwatch.StartNew()
            let model, effects = DataGrid.init "orders" columns 10_000 24.0 240.0
            stopwatch.Stop()
            printfn "datagrid-large-row-init-ms=%d" stopwatch.ElapsedMilliseconds

            Expect.equal model.RowCount 10_000 "DataGrid owns the full product row count"
            Expect.equal model.VisibleRange { FirstIndex = 0; Count = 11; Total = 10_000 } "initial visible range is viewport plus buffer row"
            Expect.equal effects [ DataGridVisibleRangeChanged model.VisibleRange ] "initial visible range effect is emitted"
            Expect.isLessThan stopwatch.ElapsedMilliseconds 100L "10,000 row visible range initialization stays bounded"

            let scrollWatch = Stopwatch.StartNew()
            let scrolled, scrollEffects = DataGrid.update (ScrollRowsTo 9_999) model
            scrollWatch.Stop()
            printfn "datagrid-large-row-scroll-ms=%d" scrollWatch.ElapsedMilliseconds

            Expect.equal scrolled.VisibleRange.FirstIndex 9_999 "scrolling to the final row clamps first index"
            Expect.equal scrolled.VisibleRange.Count 1 "last-page visible range is clamped to remaining rows"
            Expect.equal scrolled.VisibleRange.Total 10_000 "visible range keeps total row count"
            Expect.exists scrollEffects (function DataGridVisibleRangeChanged range -> range = scrolled.VisibleRange | _ -> false) "scroll emits clamped visible range"
            Expect.isLessThan scrollWatch.ElapsedMilliseconds 100L "10,000 row scroll recalculation stays bounded"
        }

        test "large DataGrid selection and focus emit product-owned effects" {
            let model, _ = DataGrid.init "orders" columns 10_000 24.0 240.0
            let selected, selectionEffects = DataGrid.update (SelectRow "row-04200") model

            Expect.equal selected.SelectedRows (Set.singleton "row-04200") "selected row stays in product-owned DataGrid state"
            Expect.exists selectionEffects (function DataGridSelectionChanged [ "row-04200" ] -> true | _ -> false) "selection emits explicit effect"

            let cell = { RowKey = "row-04200"; ColumnKey = "name" }
            let focused, focusEffects = DataGrid.update (FocusCell(Some cell)) selected

            Expect.equal focused.FocusedCell (Some cell) "focused cell stays in product-owned DataGrid state"
            Expect.exists focusEffects (function DataGridFocusChanged(Some focusedCell) -> focusedCell = cell | _ -> false) "focus emits explicit effect"
        }

        test "large DataGrid render uses visible rows instead of all rows or an empty shell" {
            let rows = [ 0 .. 9_999 ] |> List.map row
            let model, _ = DataGrid.init "orders" columns rows.Length 24.0 240.0

            let control =
                DataGrid.create columns [
                    DataGrid.rows rows
                    Attr.create "visibleRange" State (UntypedValue model.VisibleRange)
                ]

            let rendered = Control.render Theme.light control
            let maxNodes = model.VisibleRange.Count * (columns.Length + 1) + columns.Length + 4

            Expect.isGreaterThan rendered.NodeCount model.VisibleRange.Count "render includes visible row/header nodes, not just an empty shell"
            Expect.isLessThanOrEqual rendered.NodeCount maxNodes "render stays bounded by the visible range instead of all 10,000 rows"
            Expect.isEmpty rendered.Diagnostics "valid large DataGrid render has no diagnostics"
        }

        // Issue #175 (AC-3). The keyed child tree `DataGrid.create` builds is what paints a bound grid:
        // two distinct row sets must reach the screen as two distinct scenes. Asserted on the RETAINED
        // path (`renderTree`) — the one a product renders through — and on the values themselves, so a
        // regression that reinstated placeholder data would fail here rather than merely differ.
        test "two distinct row sets render distinct scenes carrying their own values (issue #175)" {
            // `FS.GG.UI.Scene` is not opened here: its `SceneEvidenceFailureClassification` cases shadow
            // the `ControlDiagnosticCode` ones the viewport test below matches on.
            let size: FS.GG.UI.Scene.Size = { Width = 400; Height = 200 }

            let gridOf rows =
                DataGrid.create columns [ DataGrid.rows rows ]
                |> Control.renderTree Theme.light size

            let first = gridOf [ row 1; row 2 ]
            let second = gridOf [ row 3; row 4 ]

            Expect.notEqual first.Scene second.Scene "distinct row sets paint distinct scenes"

            let textsOf (r: ControlRenderResult<'msg>) =
                TestAssertions.renderedText r.Scene |> List.map (fun t -> t.Text) |> Set.ofList

            let firstTexts = textsOf first
            let secondTexts = textsOf second

            Expect.contains firstTexts "Customer 1" "the first grid paints its own bound row values"
            Expect.contains secondTexts "Customer 3" "the second grid paints its own bound row values"
            Expect.isFalse (firstTexts.Contains "Customer 3") "a grid paints only the rows bound to it"

            // The retired hardcoded sample must not reappear from any path.
            for invented in [ "Widget"; "Gadget" ] do
                Expect.isFalse (firstTexts.Contains invented) (sprintf "a bound grid never paints the retired sample datum %s" invented)
        }

        test "invalid DataGrid viewport reports diagnostics through model and effects" {
            let model, effects = DataGrid.init "orders" columns 10_000 0.0 0.0

            Expect.exists model.Diagnostics (fun item -> item.Code = UnsupportedStateCombination || item.Code = UnsupportedEnvironment) "invalid row height or viewport is diagnosed on the model"
            Expect.exists effects (function ReportDataGridDiagnostic item -> item.ControlKind = "data-grid" | _ -> false) "invalid viewport emits a diagnostic effect"
        }
    ]
