namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

type DataGridProps<'msg> =
    { Id: ControlId
      Columns: DataGridColumn list
      Rows: DataGridRow list
      RowHeight: float
      ViewportHeight: float
      SelectedRows: Set<string>
      OnSelectionChanged: (string list -> 'msg) option }

module DataGrid =
    let defaults (controlId: ControlId) : DataGridProps<'msg> =
        { Id = controlId
          Columns = []
          Rows = []
          RowHeight = 24.0
          ViewportHeight = 240.0
          SelectedRows = Set.empty
          OnSelectionChanged = None }

    let init (props: DataGridProps<'msg>) : DataGridModel * DataGridEffect list =
        FS.GG.UI.Controls.DataGrid.init props.Id props.Columns props.Rows.Length props.RowHeight props.ViewportHeight

    let update (msg: DataGridMsg) (model: DataGridModel) : DataGridModel * DataGridEffect list =
        FS.GG.UI.Controls.DataGrid.update msg model

    let view (props: DataGridProps<'msg>) (model: DataGridModel) : Widget<'msg> =
        let attrs =
            [ yield FS.GG.UI.Controls.DataGrid.rows props.Rows
              yield FS.GG.UI.Controls.DataGrid.visibleRange model.VisibleRange
              yield FS.GG.UI.Controls.DataGrid.selectedRows model.SelectedRows
              yield FS.GG.UI.Controls.DataGrid.focusedCell model.FocusedCell
              match props.OnSelectionChanged with
              | Some map ->
                  // Feature 184 (US3): the moved cell now arrives typed as `Nav = MovedCell(row, col)`.
                  // Report the cell as "row:col" (zero-based indices). NOTE: this refines the pre-184
                  // contract, which reported the concatenated row/column KEYS ("rowKey:colKey") — the
                  // keys are not recoverable from the typed indices (maintainer-approved behavior change).
                  yield
                      Attr.onWith "onSelected" (fun event ->
                          ControlEvent.navCell event
                          |> Option.map (fun (row, col) -> [ sprintf "%d:%d" row col ])
                          |> Option.defaultValue []
                          |> map)
              | None -> () ]

        FS.GG.UI.Controls.DataGrid.create props.Columns attrs
        |> Control.withKey props.Id
        |> Widget.ofControl
