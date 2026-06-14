namespace FS.Skia.UI.Controls

type DataGridColumnType =
    | TextColumn
    | NumericColumn
    | BooleanColumn
    | CustomColumn of string

type DataGridColumn =
    { Key: string
      Header: string
      Width: float
      ColumnType: DataGridColumnType }

type DataGridCell =
    { RowKey: string
      ColumnKey: string
      Value: string }

type DataGridRow =
    { Key: string
      Cells: DataGridCell list }

type DataGridSortDirection =
    | Ascending
    | Descending

type DataGridSort =
    { ColumnKey: string
      Direction: DataGridSortDirection }

type DataGridFocusedCell =
    { RowKey: string
      ColumnKey: string }

type DataGridModel =
    { ControlId: ControlId
      Columns: DataGridColumn list
      RowCount: int
      RowHeight: float
      ViewportHeight: float
      VisibleRange: VisibleRange
      Overscan: int
      SelectedRows: Set<string>
      FocusedCell: DataGridFocusedCell option
      Sort: DataGridSort option
      FilterText: string option
      Diagnostics: ControlDiagnostic list }

type DataGridMsg =
    | ScrollRowsTo of int
    | SelectRow of string
    | ToggleRow of string
    | FocusCell of DataGridFocusedCell option
    | SortBy of string
    | ApplyFilter of string option
    | ReplaceRowCount of int

type DataGridEffect =
    | DataGridVisibleRangeChanged of VisibleRange
    | DataGridSelectionChanged of string list
    | DataGridFocusChanged of DataGridFocusedCell option
    | DataGridSortChanged of DataGridSort option
    | DataGridFilterChanged of string option
    | ReportDataGridDiagnostic of ControlDiagnostic

module DataGrid =
    let range rowHeight viewportHeight firstRow total overscan =
        Collections.visibleRange rowHeight viewportHeight (float firstRow * rowHeight) total overscan

    let viewportDiagnostics controlId rowHeight viewportHeight =
        [ if rowHeight <= 0.0 then
              yield
                  Diagnostics.create
                      (Some controlId)
                      "data-grid"
                      UnsupportedStateCombination
                      Error
                      "DataGrid rowHeight must be greater than zero."

          if viewportHeight <= 0.0 then
              yield
                  Diagnostics.create
                      (Some controlId)
                      "data-grid"
                      UnsupportedStateCombination
                      Error
                      "DataGrid viewportHeight must be greater than zero." ]

    let withDiagnosticEffects effects diagnostics =
        effects @ (diagnostics |> List.map ReportDataGridDiagnostic)

    let init controlId columns rowCount rowHeight viewportHeight =
        let rowCount = max 0 rowCount
        let visibleRange = range rowHeight viewportHeight 0 rowCount 0
        let diagnostics = viewportDiagnostics controlId rowHeight viewportHeight

        { ControlId = controlId
          Columns = columns
          RowCount = rowCount
          RowHeight = rowHeight
          ViewportHeight = viewportHeight
          VisibleRange = visibleRange
          Overscan = 0
          SelectedRows = Set.empty
          FocusedCell = None
          Sort = None
          FilterText = None
          Diagnostics = diagnostics },
        withDiagnosticEffects [ DataGridVisibleRangeChanged visibleRange ] diagnostics

    let update msg model =
        match msg with
        | ScrollRowsTo firstRow ->
            // Feature 114 (Phase 6, FR-009/FR-011): relocate the realized window so `firstRow` is realized,
            // widened by `model.Overscan` each side. The window RELOCATES (its first index jumps to the
            // clamped target) and stays bounded — it never expands to span the path (FR-003).
            let visibleRange =
                range model.RowHeight model.ViewportHeight (max 0 (min firstRow (max 0 (model.RowCount - 1)))) model.RowCount model.Overscan

            { model with VisibleRange = visibleRange }, [ DataGridVisibleRangeChanged visibleRange ]
        | SelectRow rowKey ->
            { model with SelectedRows = Set.singleton rowKey }, [ DataGridSelectionChanged [ rowKey ] ]
        | ToggleRow rowKey ->
            let selected =
                if model.SelectedRows.Contains rowKey then
                    model.SelectedRows.Remove rowKey
                else
                    model.SelectedRows.Add rowKey

            { model with SelectedRows = selected }, [ DataGridSelectionChanged(Set.toList selected) ]
        | FocusCell cell ->
            { model with FocusedCell = cell }, [ DataGridFocusChanged cell ]
        | SortBy columnKey ->
            if model.Columns |> List.exists (fun column -> column.Key = columnKey) |> not then
                let diagnostic =
                    Diagnostics.create
                        (Some model.ControlId)
                        "data-grid"
                        StaleGeneratedReference
                        Warning
                        $"DataGrid sort column '{columnKey}' does not exist."

                { model with Diagnostics = diagnostic :: model.Diagnostics }, [ ReportDataGridDiagnostic diagnostic ]
            else
                // Feature 108 (US5, FR-015): three-state cycle on the SAME column
                //   None -> Asc -> Desc -> None; a DIFFERENT column restarts at Asc.
                // The clearing transition emits `DataGridSortChanged None`, so the consumer no longer
                // intercepts the third press to clear the sort (SC-008).
                let sort =
                    match model.Sort with
                    | Some current when current.ColumnKey = columnKey ->
                        match current.Direction with
                        | Ascending -> Some { ColumnKey = columnKey; Direction = Descending }
                        | Descending -> None
                    | _ -> Some { ColumnKey = columnKey; Direction = Ascending }

                { model with Sort = sort }, [ DataGridSortChanged sort ]
        | ApplyFilter filterText ->
            { model with FilterText = filterText }, [ DataGridFilterChanged filterText ]
        | ReplaceRowCount count ->
            let count = max 0 count
            let visibleRange = range model.RowHeight model.ViewportHeight model.VisibleRange.FirstIndex count model.Overscan
            { model with RowCount = count; VisibleRange = visibleRange }, [ DataGridVisibleRangeChanged visibleRange ]

    let tryLast (name: string) (attrs: Attr<'msg> list) =
        attrs
        |> List.rev
        |> List.tryFind (fun attr -> attr.Name = name)

    let rowsFrom (attrs: Attr<'msg> list) : DataGridRow list =
        AttrKeys.tryKey AttrKeys.Rows attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | UntypedValue(:? (DataGridRow list) as rows) -> Some rows
            | UntypedValue(:? (DataGridRow array) as rows) -> Some(Array.toList rows)
            | _ -> None)
        |> Option.defaultValue []

    let visibleRangeFrom (rows: DataGridRow list) (attrs: Attr<'msg> list) =
        AttrKeys.tryKey AttrKeys.VisibleRange attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | UntypedValue(:? VisibleRange as visibleRange) -> Some visibleRange
            | _ -> None)
        |> Option.defaultValue { FirstIndex = 0; Count = min rows.Length 30; Total = rows.Length }

    let hasAttr (name: string) (attrs: Attr<'msg> list) =
        attrs |> List.exists (fun attr -> attr.Name = name)

    let cellValue (row: DataGridRow) (column: DataGridColumn) =
        row.Cells
        |> List.tryFind (fun cell -> cell.ColumnKey = column.Key)
        |> Option.map _.Value
        |> Option.defaultValue ""

    let cellControl (row: DataGridRow) (column: DataGridColumn) : Control<'msg> =
        Control.create
            "data-grid-cell"
            [ Attr.text (cellValue row column)
              Attr.create "columnKey" Data (TextValue column.Key)
              Attr.create "rowKey" Data (TextValue row.Key) ]
        |> Control.withKey $"{row.Key}:{column.Key}"

    let headerCell (column: DataGridColumn) : Control<'msg> =
        Control.create
            "data-grid-header-cell"
            [ Attr.text column.Header
              Attr.create "columnKey" Data (TextValue column.Key) ]
        |> Control.withKey $"header:{column.Key}"

    let rowControl (columns: DataGridColumn list) (row: DataGridRow) : Control<'msg> =
        Control.create
            "data-grid-row"
            [ Attr.create "rowKey" Data (TextValue row.Key)
              Attr.children (columns |> List.map (cellControl row)) ]
        |> Control.withKey row.Key

    let visibleRows (rows: DataGridRow list) (visibleRange: VisibleRange) =
        rows
        |> List.skip (max 0 visibleRange.FirstIndex)
        |> List.truncate (max 0 visibleRange.Count)

    let focusedCellFrom (attrs: Attr<'msg> list) : DataGridFocusedCell option =
        AttrKeys.tryKey AttrKeys.FocusedCell attrs
        |> Option.bind (fun attr ->
            match attr.Value with
            | UntypedValue(:? (DataGridFocusedCell option) as fc) -> fc
            | UntypedValue(:? DataGridFocusedCell as cell) -> Some cell
            | _ -> None)

    let create (columns: DataGridColumn list) (attrs: Attr<'msg> list) =
        let rows = rowsFrom attrs
        let visibleRange = visibleRangeFrom rows attrs
        let header = Control.create "data-grid-header" [ Attr.children (columns |> List.map headerCell) ]
        let children = header :: (visibleRows rows visibleRange |> List.map (rowControl columns))

        // Feature 114 (FR-012): report the LOGICAL total + focused position to assistive technology,
        // computed from the logical model (the visible range's `Total` and the focused row's index in the
        // full row set) — NOT the materialized slice. Only the realized window exists as controls, but
        // a11y still describes the true size and position. Non-collection controls keep `Collection = None`.
        let focusedIndex =
            focusedCellFrom attrs
            |> Option.bind (fun cell -> rows |> List.tryFindIndex (fun row -> row.Key = cell.RowKey))

        let collectionMetadata =
            { Accessibility.defaultFor "data-grid" "data-grid" with
                Collection = Some { TotalItems = visibleRange.Total; FocusedIndex = focusedIndex } }

        let attrs =
            attrs
            |> fun attrs -> if AttrKeys.hasKey AttrKeys.Columns attrs then attrs else Attr.create (AttrKeys.nameOf AttrKeys.Columns) Data (UntypedValue columns) :: attrs
            |> fun attrs -> if AttrKeys.hasKey AttrKeys.VisibleRange attrs then attrs else Attr.create (AttrKeys.nameOf AttrKeys.VisibleRange) State (UntypedValue visibleRange) :: attrs
            |> fun attrs -> if AttrKeys.hasKey AttrKeys.Accessibility attrs then attrs else Attr.accessibility collectionMetadata :: attrs

        Control.create "data-grid" (Attr.children children :: attrs)

    let columns (columns: DataGridColumn list) =
        Attr.create (AttrKeys.nameOf AttrKeys.Columns) Data (UntypedValue columns)

    let rows (rows: DataGridRow list) =
        Attr.create (AttrKeys.nameOf AttrKeys.Rows) Data (UntypedValue rows)

    let visibleRange (visibleRange: VisibleRange) =
        Attr.create (AttrKeys.nameOf AttrKeys.VisibleRange) State (UntypedValue visibleRange)

    let selectedRows (selectedRows: Set<string>) =
        Attr.create (AttrKeys.nameOf AttrKeys.SelectedRows) State (UntypedValue selectedRows)

    let focusedCell (focusedCell: DataGridFocusedCell option) =
        let payload =
            match focusedCell with
            | Some cell -> Some cell :> obj
            | None -> "" :> obj

        Attr.create (AttrKeys.nameOf AttrKeys.FocusedCell) State (UntypedValue payload)
