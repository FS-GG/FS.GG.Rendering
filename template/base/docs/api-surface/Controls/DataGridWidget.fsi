namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

/// Immutable, compiler-checked authoring surface for a data grid. Reuses the
/// existing `DataGridModel`/`Msg`/`Effect` — no parallel state type (FR-006).
type DataGridProps<'msg> =
    { Id: ControlId
      Columns: DataGridColumn list
      Rows: DataGridRow list
      RowHeight: float
      ViewportHeight: float
      SelectedRows: Set<string>
      OnSelectionChanged: (string list -> 'msg) option }

/// Typed Props front door for the `DataGrid` control.
module DataGrid =
    /// Authoring defaults for the given required `Id`.
    val defaults: controlId: ControlId -> DataGridProps<'msg>
    /// Delegates to `DataGrid.init` — initial model + effects equal the existing control.
    val init: props: DataGridProps<'msg> -> DataGridModel * DataGridEffect list
    /// Delegates to `DataGrid.update` — pure transition, no I/O.
    val update: msg: DataGridMsg -> model: DataGridModel -> DataGridModel * DataGridEffect list
    /// Lowers structurally equal to the legacy `DataGrid.create` attrs for the current model state.
    val view: props: DataGridProps<'msg> -> model: DataGridModel -> Widget<'msg>
