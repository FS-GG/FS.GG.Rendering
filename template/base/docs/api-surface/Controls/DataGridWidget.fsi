// See skill: fs-gg-ui-widgets
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
    /// Builds the initial `DataGridModel` for `controlId` from its columns and
    /// row/viewport metrics, with the first visible-range effects.
    val init: controlId: ControlId -> columns: DataGridColumn list -> rowCount: int -> rowHeight: float -> viewportHeight: float -> DataGridModel * DataGridEffect list
    /// Applies a `DataGridMsg` to `model`, returning the next state and any
    /// `DataGridEffect`s the change produces.
    val update: msg: DataGridMsg -> model: DataGridModel -> DataGridModel * DataGridEffect list
    /// Lowers structurally equal to the legacy `DataGrid.create` attrs for the current model state.
    val view: props: DataGridProps<'msg> -> model: DataGridModel -> Widget<'msg>
