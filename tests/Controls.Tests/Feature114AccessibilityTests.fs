module Feature114AccessibilityTests

// Feature 114 (US3, FR-012, SC-005) — accessibility metadata for a virtualized control reports the
// TOTAL logical item count and the CURRENT focused position (the index within that total), computed
// from the LOGICAL model (row count + focused row's logical index), independent of how many rows are
// materialized. Non-collection controls report `Collection = None`, so at-rest a11y for existing
// controls is byte-identical.

open Expecto
open FS.GG.UI.Controls

let private columns: DataGridColumn list =
    [ { Key = "name"; Header = "Name"; Width = 200.0; ColumnType = TextColumn } ]

let private rowsUpTo (n: int) : DataGridRow list =
    [ for r in 0 .. n - 1 ->
          { Key = sprintf "r%d" r
            Cells = [ { RowKey = sprintf "r%d" r; ColumnKey = "name"; Value = sprintf "Row %d" r } ] } ]

let private collectionOf (control: Control<'msg>) : CollectionPosition option =
    control.Accessibility |> Option.bind (fun a -> a.Collection)

[<Tests>]
let tests =
    testList "Feature 114 accessibility total + position (US3, SC-005)" [

        test "a virtualized DataGrid reports TotalItems = logical row count, independent of materialization (FR-012)" {
            let total = 10000
            // a narrow realized window (11 rows) over 10000 logical rows.
            let vr = Collections.visibleRange 24.0 240.0 0.0 total 0
            let grid = DataGrid.create columns [ DataGrid.rows (rowsUpTo total); DataGrid.visibleRange vr ]

            match collectionOf grid with
            | Some c ->
                Expect.equal c.TotalItems total "TotalItems is the full logical count (10000), not the materialized slice"
                Expect.isTrue (vr.Count < total) "precondition: only a small window is materialized"
            | None -> failtest "a virtualized DataGrid must report a Collection position"
        }

        test "FocusedIndex is the focused row's LOGICAL index (from FocusedCell.RowKey) (FR-012)" {
            let total = 10000
            let cell = { RowKey = "r4200"; ColumnKey = "name" }
            let grid =
                DataGrid.create columns [
                    DataGrid.rows (rowsUpTo total)
                    DataGrid.visibleRange (Collections.visibleRange 24.0 240.0 0.0 total 0)
                    DataGrid.focusedCell (Some cell) ]

            match collectionOf grid with
            | Some c -> Expect.equal c.FocusedIndex (Some 4200) "FocusedIndex is the logical index of the focused row key, even though it is offscreen"
            | None -> failtest "expected a Collection position"
        }

        test "FocusedIndex is None when nothing is focused" {
            let total = 500
            let grid = DataGrid.create columns [ DataGrid.rows (rowsUpTo total) ]
            match collectionOf grid with
            | Some c -> Expect.equal c.FocusedIndex None "no focus => FocusedIndex None"
            | None -> failtest "expected a Collection position"
        }

        test "a non-collection control reports Collection = None (at-rest a11y byte-identical)" {
            let button = Button.create [ Button.text "ok" ]
            Expect.equal (collectionOf button) None "Button carries no Collection position"

            let label = TextBlock.create [ TextBlock.text "hello" ]
            Expect.equal (collectionOf label) None "TextBlock carries no Collection position"
        }
    ]
