module Feature114OverscanTests

// Feature 114 (US1, FR-001/FR-002/FR-003/FR-004/FR-007) — a large repeated control materializes only
// its visible window plus overscan. These tests reach the public `Collections.visibleRange` overscan
// model and the `DataGrid` realized-window materialization (the count of `data-grid-row` nodes a built
// grid produces). The headline guarantee: the materialized count is bounded by `visible + 2*overscan`
// and does NOT scale with the total logical row count.

open Expecto
open FS.GG.UI.Controls

// Count the materialized `data-grid-row` nodes in a built grid (the realized window).
let rec private countKind (kind: string) (c: Control<'msg>) : int =
    (if c.Kind = kind then 1 else 0) + (c.Children |> List.sumBy (countKind kind))

let private columns: DataGridColumn list =
    [ { Key = "name"; Header = "Name"; Width = 200.0; ColumnType = TextColumn }
      { Key = "qty"; Header = "Qty"; Width = 80.0; ColumnType = NumericColumn } ]

let private rowsUpTo (n: int) : DataGridRow list =
    [ for r in 0 .. n - 1 ->
          { Key = sprintf "r%d" r
            Cells = [ { RowKey = sprintf "r%d" r; ColumnKey = "name"; Value = sprintf "Row %d" r } ] } ]

// Build a grid over `total` rows showing the realized window `vr`, and count materialized rows.
let private materializedRows (total: int) (vr: VisibleRange) : int =
    let grid = DataGrid.create columns [ DataGrid.rows (rowsUpTo total); DataGrid.visibleRange vr ]
    countKind "data-grid-row" grid

// Row geometry used across the suite: 24px rows in a 240px viewport => 11 visible rows
// (ceil(240/24)+1). Scrolling to a mid-list offset puts the visible window away from both edges.
let private rowHeight = 24.0
let private viewport = 240.0
let private visibleCount = 11

[<Tests>]
let tests =
    testList "Feature 114 overscan + bounded materialization (US1, SC-001)" [

        test "realized window does NOT scale with total: identical Count at 100/1000/10000 (FR-003, SC-001)" {
            // A mid-list scroll offset so the window is away from the top/bottom edges at every total.
            let offset = 50.0 * rowHeight

            let counts =
                [ 100; 1000; 10000 ]
                |> List.map (fun total -> (Collections.visibleRange rowHeight viewport offset total 0).Count)

            Expect.allEqual counts visibleCount "overscan-0 realized Count is identical (=V) across totals — does not scale"
        }

        test "with overscan N a mid-list window realizes exactly V + 2N real rows (FR-007)" {
            let offset = 50.0 * rowHeight
            let n = 5

            for total in [ 100; 1000; 10000 ] do
                let vr = Collections.visibleRange rowHeight viewport offset total n
                Expect.equal vr.Count (visibleCount + 2 * n) (sprintf "total %d: Count = V + 2N when away from edges" total)
                Expect.isTrue (vr.Count <= visibleCount + 2 * n) "bounded by V + 2N"
                // the materialized data-grid-row nodes equal the realized Count (bounded, non-scaling).
                Expect.equal (materializedRows total vr) vr.Count (sprintf "total %d: materialized rows == realized Count" total)
        }

        test "materialized count is identical across totals at a fixed overscan (non-scaling, SC-001)" {
            let offset = 50.0 * rowHeight
            let n = 3

            let materialized =
                [ 100; 1000; 10000 ]
                |> List.map (fun total ->
                    let vr = Collections.visibleRange rowHeight viewport offset total n
                    materializedRows total vr)

            Expect.allEqual materialized (visibleCount + 2 * n) "materialized count bounded by V+2N and identical across totals"
        }

        test "a grid whose total fits the window realizes the WHOLE set, transparently (FR-004)" {
            let n = 4
            let total = 8 // total <= V + 2N => the whole set is realized

            let vr = Collections.visibleRange rowHeight viewport 0.0 total n
            Expect.equal vr.Count total "Count == Total when the grid fits visible + 2*overscan (transparent)"
            Expect.equal vr.FirstIndex 0 "the whole set starts at index 0"
            Expect.equal (materializedRows total vr) total "every logical row materializes when the grid fits"
        }

        test "realized window is edge-clamped at the top (no index < 0, FR-002/FR-007)" {
            let n = 5
            // scroll offset 0 => first visible index 0; overscan cannot pull below 0.
            let vr = Collections.visibleRange rowHeight viewport 0.0 10000 n
            Expect.equal vr.FirstIndex 0 "top edge: FirstIndex clamped at 0, never negative"
            Expect.isTrue (vr.Count <= visibleCount + 2 * n) "still bounded by V + 2N at the edge"
            // only the leading visible window plus the trailing overscan exist (no rows before 0).
            Expect.equal vr.Count (visibleCount + n) "top edge: V + N (the leading overscan is clamped away)"
        }

        test "realized window is edge-clamped at the bottom (no index >= Total, FR-002/FR-007)" {
            let n = 5
            let total = 10000
            // scroll far past the end; ScrollRowsTo-style clamp keeps the window inside [0, Total).
            let offset = float total * rowHeight
            let vr = Collections.visibleRange rowHeight viewport offset total n
            Expect.isTrue (vr.FirstIndex >= 0) "FirstIndex non-negative"
            Expect.isTrue (vr.FirstIndex + vr.Count <= total) "bottom edge: no realized index >= Total"
            Expect.isTrue (vr.Count <= visibleCount + 2 * n) "still bounded by V + 2N at the edge"
        }

        test "overscan default (0) is byte-identical to the pre-feature visible slice (FR-006)" {
            // The overscan-0 result reproduces the historic formula: first = floor(scroll/rowHeight)
            // clamped to total-1; visible = ceil(viewport/rowHeight)+1; Count = min visible (total-first).
            for offset in [ 0.0; 17.0 * rowHeight; 9999.0 * rowHeight ] do
                let total = 10000
                let first = int (max 0.0 offset / rowHeight) |> min (total - 1)
                let visible = int (ceil (viewport / rowHeight)) + 1
                let expected = { FirstIndex = first; Count = min visible (total - first); Total = total }
                Expect.equal (Collections.visibleRange rowHeight viewport offset total 0) expected "overscan 0 == historic slice"
        }

        test "negative overscan is treated as 0 (clamped on the way in)" {
            let offset = 50.0 * rowHeight
            let total = 1000
            Expect.equal
                (Collections.visibleRange rowHeight viewport offset total -7)
                (Collections.visibleRange rowHeight viewport offset total 0)
                "negative overscan clamps to 0"
        }
    ]
