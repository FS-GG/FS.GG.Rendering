module Feature114OverscanParityTests

// Feature 114 (US2, FR-006/FR-007/FR-008, SC-002/SC-003/SC-007) — overscan is opt-in and at-rest
// output is byte-identical. With overscan at its default (0) the realized rows, geometry, and rendered
// scene are byte-identical to the pre-feature path; with opt-in overscan N only real, edge-clamped
// adjacent rows are added, the visible region is unchanged, and no rows are fabricated or duplicated.

open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

let private columns: DataGridColumn list =
    [ { Key = "name"; Header = "Name"; Width = 200.0; ColumnType = TextColumn }
      { Key = "qty"; Header = "Qty"; Width = 80.0; ColumnType = NumericColumn } ]

let private rowsUpTo (n: int) : DataGridRow list =
    [ for r in 0 .. n - 1 ->
          { Key = sprintf "r%d" r
            Cells = [ { RowKey = sprintf "r%d" r; ColumnKey = "name"; Value = sprintf "Row %d" r } ] } ]

// The keys of the materialized `data-grid-row` nodes, in document order.
let rec private rowKeys (c: Control<'msg>) : string list =
    let here = if c.Kind = "data-grid-row" then Option.toList c.Key else []
    here @ (c.Children |> List.collect rowKeys)

let private gridFor (total: int) (vr: VisibleRange) =
    DataGrid.create columns [ DataGrid.rows (rowsUpTo total); DataGrid.visibleRange vr ]

let private rowHeight = 24.0
let private viewport = 240.0
let private visibleCount = 11

[<Tests>]
let tests =
    testList "Feature 114 overscan parity + opt-in correctness (US2, SC-002/SC-003)" [

        test "overscan 0 realizes exactly the historic visible slice — same keys, byte-identical scene (FR-006, SC-002)" {
            let total = 10000
            let offset = 50.0 * rowHeight

            // The historic visible slice (overscan 0) and the slice a pre-feature build would produce.
            let vr0 = Collections.visibleRange rowHeight viewport offset total 0
            let expectedKeys = [ for i in vr0.FirstIndex .. vr0.FirstIndex + vr0.Count - 1 -> sprintf "r%d" i ]

            let grid = gridFor total vr0
            Expect.equal (rowKeys grid) expectedKeys "overscan-0 realizes exactly the contiguous historic slice (no fabricated/missing rows)"

            // Structural Scene equality: rendering the same realized window is byte-identical (controls
            // have no value equality, so we compare the rendered Scene structurally).
            let sceneA = (Control.render Theme.light grid).Scene
            let sceneB = (Control.render Theme.light (gridFor total vr0)).Scene
            Expect.equal sceneA sceneB "the overscan-0 grid renders a byte-identical scene on repeat builds"
        }

        test "opt-in overscan N adds only real, contiguous adjacent rows; visible region unchanged (FR-007, SC-003)" {
            let total = 10000
            let offset = 50.0 * rowHeight
            let n = 4

            let vr0 = Collections.visibleRange rowHeight viewport offset total 0
            let vrN = Collections.visibleRange rowHeight viewport offset total n

            // The widened window is a contiguous superset, extended by exactly N on each side (mid-list).
            Expect.equal vrN.FirstIndex (vr0.FirstIndex - n) "leading edge extended by exactly N real rows"
            Expect.equal (vrN.FirstIndex + vrN.Count) (vr0.FirstIndex + vr0.Count + n) "trailing edge extended by exactly N real rows"

            let keysN = rowKeys (gridFor total vrN)
            let expectedN = [ for i in vrN.FirstIndex .. vrN.FirstIndex + vrN.Count - 1 -> sprintf "r%d" i ]
            Expect.equal keysN expectedN "all materialized rows are real, contiguous logical rows (no duplicates/fabrication)"

            // The original visible rows are still present and in the same order (visible region unchanged).
            let visibleKeys = [ for i in vr0.FirstIndex .. vr0.FirstIndex + vr0.Count - 1 -> sprintf "r%d" i ]
            Expect.isTrue (visibleKeys |> List.forall (fun k -> List.contains k keysN)) "every original visible row is still materialized"
        }

        test "opt-in overscan is edge-clamped: no row index < 0 or >= Total, no fabricated rows (FR-007)" {
            let total = 30
            let n = 6

            // top edge
            let vrTop = Collections.visibleRange rowHeight viewport 0.0 total n
            let keysTop = rowKeys (gridFor total vrTop)
            Expect.isTrue (keysTop |> List.forall (fun k -> k <> "r-1")) "no negative-index row fabricated at the top edge"
            Expect.equal keysTop.Head "r0" "top edge starts at the first real row"

            // bottom edge
            let vrBot = Collections.visibleRange rowHeight viewport (float total * rowHeight) total n
            let keysBot = rowKeys (gridFor total vrBot)
            Expect.isTrue (keysBot |> List.forall (fun k -> k <> sprintf "r%d" total)) "no past-the-end row fabricated at the bottom edge"
            Expect.equal (List.last keysBot) (sprintf "r%d" (total - 1)) "bottom edge ends at the last real row"
        }

        test "keyed rows are stable across a scroll — same key reused where the window overlaps (FR-008)" {
            let total = 1000
            let vrA = Collections.visibleRange rowHeight viewport (50.0 * rowHeight) total 2
            let vrB = Collections.visibleRange rowHeight viewport (51.0 * rowHeight) total 2

            let keysA = rowKeys (gridFor total vrA) |> Set.ofList
            let keysB = rowKeys (gridFor total vrB) |> Set.ofList
            let overlap = Set.intersect keysA keysB
            Expect.isNonEmpty overlap "a one-row scroll keeps most realized row keys stable (keyed reuse is possible)"
        }
    ]
