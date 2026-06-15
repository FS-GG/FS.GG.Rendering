module Audit_Virtualization

// AUDIT (feature 006-verify-imported-mechanisms) — virtualization mechanism.
//   * T007 sanity: the `FrameMetrics.VirtualItemsMaterialized` / `VirtualItemsTotal` counters are reachable.
//   * T034 US3 effectiveness: a collection LARGER than the viewport materializes a window bounded by the
//     viewport need, NOT the logical total — `VirtualMaterialized << VirtualTotal`, and the materialized
//     count does NOT scale as the total grows 100 -> 1000 -> 10000. If materialized ~= total (everything
//     realized) that is a FINDING (the mechanism is a no-op). Driven through the real
//     `ControlsElmish.Perf.runScript` retained `step` path (mirrors Feature114VirtualMetricsTests).

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish

type private Msg = Bump

let private size: Size = { Width = 1024; Height = 768 }
let private noMods = ViewerKeyboard.noModifiers
let private key () = FrameInput.Key(Enter, noMods)

let private columns: DataGridColumn list =
    [ { Key = "name"; Header = "Name"; Width = 200.0; ColumnType = TextColumn }
      { Key = "qty"; Header = "Qty"; Width = 80.0; ColumnType = NumericColumn } ]

let private gridRows (rowCount: int) : DataGridRow list =
    [ for r in 0 .. rowCount - 1 ->
          { Key = sprintf "r%d" r
            Cells =
              [ { RowKey = sprintf "r%d" r; ColumnKey = "name"; Value = sprintf "Row %d" r }
                { RowKey = sprintf "r%d" r; ColumnKey = "qty"; Value = string (r % 100) } ] } ]

// A grid wrapped in a stack whose orientation toggles on model parity, so frame 2 re-renders the grid
// through the retained `step` (where the virtualization counts are tallied).
let private gridView (rowCount: int) (model: int) : Control<Msg> =
    Stack.create
        [ Stack.orientation (if model % 2 = 0 then "vertical" else "horizontal")
          Stack.children [ DataGrid.create columns [ DataGrid.rows (gridRows rowCount) ] |> Control.withKey "grid" ] ]

let private runWith (view: int -> Control<Msg>) (script: FrameInput<Msg> list) : FrameMetrics list =
    let host: InteractiveAppHost<int, Msg> =
        { Init = fun () -> 0, []
          Update = fun Bump model -> model + 1, []
          View = fun _ model -> view model
          Theme = Theme.light
          MapKey = fun k _ -> match k with | Enter -> Some Bump | _ -> None
          MapPointer = fun _ -> None
          Tick = fun _ -> None
          MapKeyChord = fun _ _ -> None
          OnFrameMetrics = ignore
          Diagnostics = Viewer.defaultDiagnostics }

    ControlsElmish.Perf.runScript host size script

// The create-time fallback realizes `min rows.Length 30` rows (no explicit visible range supplied).
let private fallbackVisible = 30

// Frame 1 seeds via init (0/0); frame 2 re-renders the grid through the retained step.
let private virtualOf (rowCount: int) : FrameMetrics = (runWith (gridView rowCount) [ key (); key () ]).[1]

[<Tests>]
let tests =
    testList "Audit virtualization mechanism (T007 / T034 US3)" [

        // ---- T007 sanity --------------------------------------------------------------------------
        test "Audit: virtualization counters reachable — VirtualItemsMaterialized/Total touchable (T007)" {
            let f = virtualOf 1000
            Expect.isTrue (f.VirtualItemsTotal > 0) "a virtualized grid surfaces a positive logical total (counters reachable)"
            Expect.isTrue (f.VirtualItemsMaterialized >= 0) "the materialized counter is reachable"
        }

        // ---- T034 US3 effectiveness ---------------------------------------------------------------
        test "Audit: a 10000-row grid materializes a viewport-bounded window, NOT the total (T034, effectiveness)" {
            let f = virtualOf 10000
            Expect.equal f.VirtualItemsTotal 10000 "the logical total equals the full collection (10000)"
            Expect.isTrue (f.VirtualItemsMaterialized <= fallbackVisible)
                (sprintf "materialized (%d) is bounded by the realized window (<= %d), not the total" f.VirtualItemsMaterialized fallbackVisible)

            // FINDING gate: if materialized ~= total, virtualization is a no-op. Require a large margin.
            let ratio = float f.VirtualItemsMaterialized / float f.VirtualItemsTotal
            Expect.isLessThan ratio 0.05
                (sprintf "materialized %d / total %d = %.4f — a small fraction (no-op would be ~1.0, a FINDING)" f.VirtualItemsMaterialized f.VirtualItemsTotal ratio)
            Expect.isTrue (f.VirtualItemsMaterialized < f.VirtualItemsTotal) "materialized is strictly below the total"
        }

        test "Audit: the materialized window does NOT scale with the total across 100/1000/10000 (T034, discriminating)" {
            let m100 = (virtualOf 100).VirtualItemsMaterialized
            let m1000 = (virtualOf 1000).VirtualItemsMaterialized
            let m10000 = (virtualOf 10000).VirtualItemsMaterialized
            let t100 = (virtualOf 100).VirtualItemsTotal
            let t1000 = (virtualOf 1000).VirtualItemsTotal
            let t10000 = (virtualOf 10000).VirtualItemsTotal

            // Totals scale with the data; the materialized window stays flat. A non-virtualizing impl
            // (materialized == total) would make these equal and FAIL.
            Expect.equal (t100, t1000, t10000) (100, 1000, 10000) "the logical total DOES scale with the data"
            Expect.equal m100 m10000 "materialized count is identical at 100 and 10000 rows — the window does not scale"
            Expect.equal m1000 m10000 "materialized count is identical at 1000 and 10000 rows — the window does not scale"
            Expect.notEqual m10000 t10000 "DISCRIMINATING: materialized != total (an all-realized impl would equal it)"
        }
    ]
