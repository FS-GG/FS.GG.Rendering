namespace FS.GG.UI.Controls

/// How a `DataGridColumn` renders and interprets its cells — `TextColumn`,
/// `NumericColumn`, `BooleanColumn`, or a `CustomColumn` named by its tag.
type DataGridColumnType =
    | TextColumn
    | NumericColumn
    | BooleanColumn
    | CustomColumn of string

/// A grid column definition: its stable `Key`, displayed `Header`, pixel
/// `Width`, and `ColumnType` controlling cell rendering.
type DataGridColumn =
    { Key: string
      Header: string
      Width: float
      ColumnType: DataGridColumnType }

/// A single grid cell addressed by `RowKey` and `ColumnKey`, carrying its
/// rendered string `Value`.
type DataGridCell =
    { RowKey: string
      ColumnKey: string
      Value: string }

/// A grid row identified by `Key`, holding its `Cells` in column order.
type DataGridRow =
    { Key: string
      Cells: DataGridCell list }

/// Direction of a column sort — `Ascending` or `Descending`.
type DataGridSortDirection =
    | Ascending
    | Descending

/// The active sort: which column (`ColumnKey`) is ordered and in which
/// `Direction`.
type DataGridSort =
    { ColumnKey: string
      Direction: DataGridSortDirection }

/// The currently focused cell, located by `RowKey` and `ColumnKey` for
/// keyboard navigation.
type DataGridFocusedCell =
    { RowKey: string
      ColumnKey: string }

/// The full virtualized data-grid state: identity, `Columns`, `RowCount`,
/// row/viewport metrics, the visible window, selection, focus, sort, filter,
/// and accumulated `Diagnostics`.
type DataGridModel =
    { ControlId: ControlId
      Columns: DataGridColumn list
      RowCount: int
      RowHeight: float
      ViewportHeight: float
      VisibleRange: VisibleRange
      /// Feature 114 (Phase 6): extra logical rows realized on EACH side of the visible window.
      /// Default <c>0</c> (byte-identical to today's slice, FR-006); a positive value is an opt-in
      /// widening the realized window by up to <c>2 * Overscan</c> real, edge-clamped adjacent rows
      /// (FR-007). The window always stays bounded (<c>materialized &lt;= visible + 2 * Overscan</c>) — a
      /// scroll/relocation moves the window, it never expands it.
      Overscan: int
      SelectedRows: Set<string>
      FocusedCell: DataGridFocusedCell option
      Sort: DataGridSort option
      FilterText: string option
      Diagnostics: ControlDiagnostic list }

/// Messages driving a `DataGridModel` through `update`: scroll, row
/// select/toggle, cell focus, sort, filter, and row-count replacement.
type DataGridMsg =
    | ScrollRowsTo of int
    | SelectRow of string
    | ToggleRow of string
    | FocusCell of DataGridFocusedCell option
    | SortBy of string
    | ApplyFilter of string option
    | ReplaceRowCount of int

/// Outbound notifications a `DataGridModel` emits when its visible range,
/// selection, focus, sort, or filter changes, plus diagnostic reports.
type DataGridEffect =
    | DataGridVisibleRangeChanged of VisibleRange
    | DataGridSelectionChanged of string list
    | DataGridFocusChanged of DataGridFocusedCell option
    | DataGridSortChanged of DataGridSort option
    | DataGridFilterChanged of string option
    | ReportDataGridDiagnostic of ControlDiagnostic

/// Virtualized data-grid control: an MVU `init`/`update` core plus attribute
/// builders for authoring a grid against the typed `Props` front door.
module DataGrid =
    /// Builds the initial `DataGridModel` for `controlId` from its columns and
    /// row/viewport metrics, with the first visible-range effects.
    val init: controlId: ControlId -> columns: DataGridColumn list -> rowCount: int -> rowHeight: float -> viewportHeight: float -> DataGridModel * DataGridEffect list
    /// Applies a `DataGridMsg` to `model`, returning the next state and any
    /// `DataGridEffect`s the change produces.
    val update: msg: DataGridMsg -> model: DataGridModel -> DataGridModel * DataGridEffect list
    /// Authors a data-grid `Control` over the given `columns` and `attrs` — the
    /// legacy builder behind the typed `Props` front door.
    val create: columns: DataGridColumn list -> attrs: Attr<'msg> list -> Control<'msg>
    /// Attribute setting the grid's `columns` definition on a data-grid control.
    val columns: columns: DataGridColumn list -> Attr<'msg>
    /// Attribute supplying the grid's `rows` of cell data to a data-grid control.
    val rows: rows: DataGridRow list -> Attr<'msg>
    /// Attribute pinning the grid's `visibleRange` — the virtualized window of
    /// rows currently rendered.
    val visibleRange: visibleRange: VisibleRange -> Attr<'msg>
    /// Attribute marking the set of `selectedRows` (by row key) on a data-grid
    /// control.
    val selectedRows: selectedRows: Set<string> -> Attr<'msg>
    /// Attribute setting the grid's `focusedCell` for keyboard navigation, or
    /// `None` to clear focus.
    val focusedCell: focusedCell: DataGridFocusedCell option -> Attr<'msg>
