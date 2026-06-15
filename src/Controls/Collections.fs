namespace FS.GG.UI.Controls

type VisibleRange =
    { FirstIndex: int
      Count: int
      Total: int }

type CollectionModel =
    { ControlId: ControlId
      ItemCount: int
      RowHeight: float
      ViewportHeight: float
      ScrollOffset: float
      SelectedKeys: Set<string>
      VisibleRange: VisibleRange
      Overscan: int
      RecalculationThresholdMs: int }

type CollectionMsg =
    | ScrollTo of float
    | SelectKey of string
    | ToggleKey of string
    | ReplaceItemCount of int

type CollectionEffect =
    | VisibleRangeChanged of VisibleRange

module Collections =
    let visibleRange rowHeight viewportHeight scrollOffset totalItems overscan =
        if totalItems <= 0 || rowHeight <= 0.0 || viewportHeight <= 0.0 then
            { FirstIndex = 0; Count = 0; Total = max 0 totalItems }
        else
            // The overscan-0 visible slice (byte-identical to the pre-114 computation).
            let first = int (max 0.0 scrollOffset / rowHeight) |> min (totalItems - 1)
            let visible = int (ceil (viewportHeight / rowHeight)) + 1
            let count = min visible (totalItems - first)
            // Feature 114 (Phase 6, FR-007): widen the slice symmetrically by `n` rows each side,
            // edge-clamped so no index is `< 0` or `>= totalItems`. `n = 0` returns the slice above
            // unchanged (`first' = first`, `count' = count`), so default overscan is byte-identical.
            let n = max 0 overscan
            let first' = max 0 (first - n)
            let last' = min (totalItems - 1) (first + count - 1 + n)
            { FirstIndex = first'; Count = last' - first' + 1; Total = totalItems }

    let init controlId itemCount rowHeight viewportHeight =
        let range = visibleRange rowHeight viewportHeight 0.0 itemCount 0

        { ControlId = controlId
          ItemCount = itemCount
          RowHeight = rowHeight
          ViewportHeight = viewportHeight
          ScrollOffset = 0.0
          SelectedKeys = Set.empty
          VisibleRange = range
          Overscan = 0
          RecalculationThresholdMs = 16 },
        [ VisibleRangeChanged range ]

    let withRange model scrollOffset itemCount =
        let range = visibleRange model.RowHeight model.ViewportHeight scrollOffset itemCount model.Overscan
        { model with ScrollOffset = max 0.0 scrollOffset; ItemCount = itemCount; VisibleRange = range }, [ VisibleRangeChanged range ]

    let update msg model =
        match msg with
        | ScrollTo offset -> withRange model offset model.ItemCount
        | SelectKey key -> { model with SelectedKeys = Set.singleton key }, []
        | ToggleKey key ->
            let selected =
                if model.SelectedKeys.Contains key then
                    model.SelectedKeys.Remove key
                else
                    model.SelectedKeys.Add key

            { model with SelectedKeys = selected }, []
        | ReplaceItemCount count -> withRange model model.ScrollOffset (max 0 count)
