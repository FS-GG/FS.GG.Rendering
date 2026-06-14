module Feature114OffscreenTests

// Feature 114 (US3, FR-009/FR-010/FR-011, SC-004/SC-005) — focus, selection, and accessibility remain
// correct across the visible/offscreen boundary. Selection/focus are properties of the LOGICAL item
// (a row key/index), so they are addressable on an offscreen row without materializing it. The realized
// window RELOCATES to a target (via `ScrollRowsTo`, the index-based relocation primitive — research
// decision (d)); it never EXPANDS to span the path, so the FR-003 bound (`materialized <= V + 2*overscan`)
// holds at all times. Dispatch outcomes for already-materialized rows stay byte-identical (FR-016).

open Expecto
open FS.Skia.UI.Controls

let private columns: DataGridColumn list =
    [ { Key = "name"; Header = "Name"; Width = 200.0; ColumnType = TextColumn } ]

let private rowsUpTo (n: int) : DataGridRow list =
    [ for r in 0 .. n - 1 ->
          { Key = sprintf "r%d" r
            Cells = [ { RowKey = sprintf "r%d" r; ColumnKey = "name"; Value = sprintf "Row %d" r } ] } ]

let rec private rowKeys (c: Control<'msg>) : string list =
    let here = if c.Kind = "data-grid-row" then Option.toList c.Key else []
    here @ (c.Children |> List.collect rowKeys)

// The realized row keys for a model's current window over `total` rows.
let private realizedKeys (total: int) (model: DataGridModel) : string list =
    DataGrid.create columns [ DataGrid.rows (rowsUpTo total); DataGrid.visibleRange model.VisibleRange ] |> rowKeys

let private total = 10000
let private rowHeight = 24.0
let private viewport = 240.0
let private visibleCount = 11

// A 10000-row grid with overscan N whose window starts at the top (rows r0..r10 realized).
let private gridWithOverscan (n: int) : DataGridModel =
    let model, _ = DataGrid.init "orders" columns total rowHeight viewport
    { model with Overscan = n }

[<Tests>]
let tests =
    testList "Feature 114 offscreen addressability + boundary nav (US3, SC-004/SC-005)" [

        test "selecting an offscreen row records it logically WITHOUT materializing the path (FR-010, SC-004)" {
            let model = gridWithOverscan 0
            let selected, effects = DataGrid.update (SelectRow "r9999") model

            Expect.equal selected.SelectedRows (Set.singleton "r9999") "the offscreen row is selected on the logical model"
            Expect.exists effects (function DataGridSelectionChanged [ "r9999" ] -> true | _ -> false) "selection emits its effect"

            // selection is logical: the realized window is unchanged and bounded; the offscreen row and
            // every intervening row are NOT materialized.
            Expect.equal selected.VisibleRange model.VisibleRange "selection does not move or expand the realized window"
            let keys = realizedKeys total selected
            Expect.isTrue (keys.Length <= visibleCount) "materialized count stays bounded by V (no path materialization)"
            Expect.isFalse (List.contains "r9999" keys) "the selected offscreen row is NOT materialized"
            Expect.isFalse (List.contains "r5000" keys) "no intervening row is materialized"
        }

        test "toggling an offscreen row updates the logical selection set without materializing it (FR-010)" {
            let model = gridWithOverscan 0
            let on, _ = DataGrid.update (ToggleRow "r8000") model
            Expect.isTrue (on.SelectedRows.Contains "r8000") "toggle on records the offscreen row"
            let off, _ = DataGrid.update (ToggleRow "r8000") on
            Expect.isFalse (off.SelectedRows.Contains "r8000") "toggle off clears it — pure logical state, no materialization"
            Expect.isFalse (List.contains "r8000" (realizedKeys total off)) "the toggled offscreen row is never materialized"
        }

        test "focusing an offscreen cell records it logically without materializing the path (FR-009)" {
            let model = gridWithOverscan 0
            let cell = { RowKey = "r7777"; ColumnKey = "name" }
            let focused, effects = DataGrid.update (FocusCell(Some cell)) model

            Expect.equal focused.FocusedCell (Some cell) "the offscreen cell is focused on the logical model"
            Expect.exists effects (function DataGridFocusChanged(Some c) -> c = cell | _ -> false) "focus emits its effect"
            Expect.isFalse (List.contains "r7777" (realizedKeys total focused)) "the focused offscreen row is not materialized by FocusCell alone"
        }

        test "relocating to an offscreen target brings it into the window — relocate, do NOT expand (FR-009, O4)" {
            let n = 3
            let model = gridWithOverscan n
            let targetIndex = 9000
            let relocated, _ = DataGrid.update (ScrollRowsTo targetIndex) model

            let keys = realizedKeys total relocated
            Expect.isTrue (List.contains (sprintf "r%d" targetIndex) keys) "the relocated window materializes the target row"
            // the window MOVED (its first index jumped near the target) ...
            Expect.isTrue (relocated.VisibleRange.FirstIndex >= targetIndex - 2 * n) "window relocated to (near) the target"
            // ... and stayed BOUNDED — it did not expand to span r0..r9000.
            Expect.isTrue (relocated.VisibleRange.Count <= visibleCount + 2 * n) "window stays bounded by V + 2N after relocation (relocate, not expand)"
            Expect.isFalse (List.contains "r0" keys) "the path from the old position is NOT materialized"
        }

        test "boundary-crossing relocation lands on the correct next LOGICAL row and advances the window (FR-011, SC-005)" {
            let n = 0
            let model = gridWithOverscan n
            // window starts realizing r0 .. r(V-1); move focus just past the last realized row.
            let lastRealized = model.VisibleRange.FirstIndex + model.VisibleRange.Count - 1
            let nextLogical = lastRealized + 1

            let advanced, _ = DataGrid.update (ScrollRowsTo nextLogical) model
            let keys = realizedKeys total advanced

            Expect.isTrue (List.contains (sprintf "r%d" nextLogical) keys) "the next logical row across the boundary is now realized"
            Expect.isTrue (advanced.VisibleRange.FirstIndex > model.VisibleRange.FirstIndex) "the realized window advanced forward"
            Expect.isTrue (advanced.VisibleRange.Count <= visibleCount + 2 * n) "the advanced window stays bounded (it advanced, did not grow)"
        }

        test "dispatch outcome for an already-materialized (visible) row is byte-identical to pre-feature (FR-016)" {
            // selecting a VISIBLE row: SelectedRows changes, the window is untouched — exactly as before.
            let model = gridWithOverscan 0
            let selected, effects = DataGrid.update (SelectRow "r3") model
            Expect.equal selected.SelectedRows (Set.singleton "r3") "visible-row selection records the key"
            Expect.equal selected.VisibleRange model.VisibleRange "visible-row selection leaves the window unchanged (byte-identical)"
            Expect.exists effects (function DataGridSelectionChanged [ "r3" ] -> true | _ -> false) "visible-row selection emits the same effect as before"
        }
    ]
