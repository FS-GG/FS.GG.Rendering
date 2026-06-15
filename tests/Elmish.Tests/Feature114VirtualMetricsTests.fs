module Feature114VirtualMetricsTests

// Feature 114 (US4, FR-013/FR-014, SC-006) — the virtualization contract is observable as the
// deterministic `FrameMetrics.VirtualItemsMaterialized` / `VirtualItemsTotal` counts produced on the
// `ControlsElmish.Perf.runScript` render path. A frame that builds a virtualized DataGrid records a
// materialized count bounded by `visible + 2*overscan` and a logical total equal to RowCount; a frame
// that evaluates no virtualized control reports 0/0; multiple grids in a frame aggregate; the
// materialized count does NOT scale with the total across 100/1000/10000.

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

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

let private twoGridView (a: int) (b: int) (model: int) : Control<Msg> =
    Stack.create
        [ Stack.orientation (if model % 2 = 0 then "vertical" else "horizontal")
          Stack.children
              [ DataGrid.create columns [ DataGrid.rows (gridRows a) ] |> Control.withKey "gridA"
                DataGrid.create columns [ DataGrid.rows (gridRows b) ] |> Control.withKey "gridB" ] ]

let private buttonView (model: int) : Control<Msg> =
    Stack.create [ Stack.children [ Button.create [ Button.text (sprintf "n%d" model); Button.onClick Bump ] |> Control.withKey "b" ] ]

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

// The create-time fallback realizes `min rows.Length 30` rows (no explicit visible range supplied),
// so the materialized count is 30 for any grid of >= 30 rows — the bound the metric makes observable.
let private fallbackVisible = 30

[<Tests>]
let tests =
    testList "Feature 114 virtual metrics (US4, FR-013/FR-014, SC-006)" [

        test "a frame building a virtualized grid reports materialized <= visible and total = RowCount (FR-013)" {
            // [key; key]: frame 1 seeds via init (0/0), frame 2 re-renders through the retained step.
            let frames = runWith (gridView 10000) [ key (); key () ]
            let f = frames.[1]
            Expect.isTrue (f.VirtualItemsMaterialized <= fallbackVisible) "materialized count is bounded by the realized window"
            Expect.equal f.VirtualItemsTotal 10000 "total equals the logical RowCount (10000)"
            Expect.isTrue (f.VirtualItemsMaterialized < f.VirtualItemsTotal) "the window is far smaller than the logical total"
        }

        test "materialized count does NOT scale with total across 100/1000/10000 (FR-014, SC-006)" {
            let materialized total = (runWith (gridView total) [ key (); key () ]).[1].VirtualItemsMaterialized
            let totals = (runWith (gridView 100) [ key (); key () ]).[1].VirtualItemsTotal,
                         (runWith (gridView 1000) [ key (); key () ]).[1].VirtualItemsTotal,
                         (runWith (gridView 10000) [ key (); key () ]).[1].VirtualItemsTotal

            Expect.allEqual [ materialized 100; materialized 1000; materialized 10000 ] (materialized 10000) "materialized count identical across totals — does not scale"
            Expect.equal totals (100, 1000, 10000) "the logical total DOES scale with the data"
        }

        test "a frame that evaluates no virtualized control reports 0 / 0 (FR-013)" {
            let frames = runWith buttonView [ key (); key () ]
            let f = frames.[1]
            Expect.equal f.VirtualItemsMaterialized 0 "no virtualized control => materialized 0"
            Expect.equal f.VirtualItemsTotal 0 "no virtualized control => total 0"
        }

        test "an idle frame reports 0 / 0 (no step runs)" {
            // frame 2 is Idle: no render step runs, so the per-frame virtual tally stays cleared.
            let frames = runWith (gridView 10000) [ key (); FrameInput.Idle ]
            let f = frames.[1]
            Expect.equal f.VirtualItemsMaterialized 0 "idle frame => materialized 0"
            Expect.equal f.VirtualItemsTotal 0 "idle frame => total 0"
        }

        test "multiple virtualized controls in one frame AGGREGATE the counts (FR-013)" {
            let frames = runWith (twoGridView 1000 2000) [ key (); key () ]
            let f = frames.[1]
            Expect.equal f.VirtualItemsTotal 3000 "total aggregates across both grids (1000 + 2000)"
            Expect.isTrue (f.VirtualItemsMaterialized <= 2 * fallbackVisible) "materialized aggregates across both grids, still bounded"
            Expect.isTrue (f.VirtualItemsMaterialized > fallbackVisible) "materialized reflects BOTH grids' windows, not one"
        }
    ]
