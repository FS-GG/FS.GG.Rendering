namespace FS.GG.UI.Controls

/// The slice of a virtualized list currently realized: `FirstIndex`/`Count` within `Total`.
type VisibleRange =
    { FirstIndex: int
      Count: int
      Total: int }

/// State of a virtualizing collection: scroll offset, viewport/row geometry, `SelectedKeys`,
/// and the derived `VisibleRange` keyed by `ControlId`.
type CollectionModel =
    { ControlId: ControlId
      ItemCount: int
      RowHeight: float
      ViewportHeight: float
      ScrollOffset: float
      SelectedKeys: Set<string>
      VisibleRange: VisibleRange
      /// Feature 114 (Phase 6): extra logical rows realized on EACH side of the visible window
      /// (the overscan buffer). Default <c>0</c>, which reproduces today's realized slice
      /// byte-identically (FR-006); a positive value is an opt-in that widens the realized window by
      /// up to <c>2 * Overscan</c> real, edge-clamped adjacent rows (FR-007). Negative values are
      /// clamped to 0.
      Overscan: int
      RecalculationThresholdMs: int }

/// Messages that drive a `CollectionModel`: `ScrollTo`, `SelectKey`/`ToggleKey`, `ReplaceItemCount`.
type CollectionMsg =
    | ScrollTo of float
    | SelectKey of string
    | ToggleKey of string
    | ReplaceItemCount of int

/// Side effect emitted when a collection update shifts the realized window (`VisibleRangeChanged`).
type CollectionEffect =
    | VisibleRangeChanged of VisibleRange

/// Virtualization model for large scrolling lists: `visibleRange`/`init`/`update` over `CollectionModel`.
module Collections =
    /// Compute the realized `VisibleRange` from row height, viewport height, scroll offset, item total,
    /// and an `overscan` buffer (Feature 114, additive trailing parameter). `overscan = 0` reproduces
    /// the pre-feature visible slice byte-identically; a positive `overscan` shifts `FirstIndex` back by
    /// up to `overscan` and widens `Count` by up to `2 * overscan`, edge-clamped so no index is `< 0` or
    /// `>= totalItems`. Negative `overscan` is treated as 0.
    val visibleRange: rowHeight: float -> viewportHeight: float -> scrollOffset: float -> totalItems: int -> overscan: int -> VisibleRange
    /// Build the initial `CollectionModel` for a `controlId` and emit its first `CollectionEffect` list.
    val init: controlId: ControlId -> itemCount: int -> rowHeight: float -> viewportHeight: float -> CollectionModel * CollectionEffect list
    /// Apply a `CollectionMsg` to the `CollectionModel`, returning the next model and any effects.
    val update: msg: CollectionMsg -> model: CollectionModel -> CollectionModel * CollectionEffect list
