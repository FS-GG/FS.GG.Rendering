namespace FS.Skia.UI.Controls.Typed

open FS.Skia.UI.Controls

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
        FS.Skia.UI.Controls.DataGrid.init props.Id props.Columns props.Rows.Length props.RowHeight props.ViewportHeight

    let update (msg: DataGridMsg) (model: DataGridModel) : DataGridModel * DataGridEffect list =
        FS.Skia.UI.Controls.DataGrid.update msg model

    let view (props: DataGridProps<'msg>) (model: DataGridModel) : Widget<'msg> =
        let attrs =
            [ yield FS.Skia.UI.Controls.DataGrid.rows props.Rows
              yield FS.Skia.UI.Controls.DataGrid.visibleRange model.VisibleRange
              yield FS.Skia.UI.Controls.DataGrid.selectedRows model.SelectedRows
              yield FS.Skia.UI.Controls.DataGrid.focusedCell model.FocusedCell
              match props.OnSelectionChanged with
              | Some map ->
                  yield
                      Attr.onWith "onSelected" (fun event ->
                          event.Payload
                          |> Option.map (fun value -> [ value ])
                          |> Option.defaultValue []
                          |> map)
              | None -> () ]

        FS.Skia.UI.Controls.DataGrid.create props.Columns attrs
        |> Control.withKey props.Id
        |> Widget.ofControl
