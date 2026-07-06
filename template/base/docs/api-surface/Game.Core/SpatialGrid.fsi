namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A uniform spatial partition of positioned items, built once and queried many times. The
/// representation is **opaque** (hidden in the `.fsi`): callers build with `SpatialGrid.build` and read
/// with `SpatialGrid.query`/`queryRadius`. Immutable and pure — no shared mutable state — so it is safe
/// to hold inside a `Model`. Items are bucketed by `FS.GG.Game.Core.Point` position at a fixed cell size;
/// bucketing is an internal broad-phase acceleration only — every query returns the **exact** set of
/// items in the region (no false positives and no false negatives), in the **insertion order** they were
/// supplied to `build`, so results are fully deterministic.
type SpatialGrid<'T>

/// Public contract module exposed by the FS.GG.Game.Core package.
/// Build and query a `SpatialGrid`. Reuses `FS.GG.Game.Core.Point`/`Rect` (and the `Geometry`
/// containment helper) rather than a look-alike geometry vocabulary. Pure and deterministic throughout:
/// identical items in identical order with an identical query yield an identical result list.
[<RequireQualifiedAccess>]
module SpatialGrid =

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Build a grid bucketing each `(position, item)` at `cellSize`. Insertion order is preserved for
    /// deterministic query results. Total on degenerate input: an empty `items` yields an empty grid; a
    /// non-positive or non-finite `cellSize` falls back to a single bucket (queries still return exact
    /// results by filtering, just without spatial acceleration) rather than throwing or dividing by zero.
    val build: cellSize: float -> items: seq<Point * 'T> -> SpatialGrid<'T>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Every item whose position lies inside `region` (inclusive of the region edges, matching
    /// `Geometry.containsPoint`), in insertion order. A zero-area `region` returns items exactly on that
    /// degenerate rect under the same edge convention. Never throws.
    val query: region: Rect -> grid: SpatialGrid<'T> -> 'T list

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Every item whose position lies within `radius` of `center` (inclusive: distance ≤ `radius`, using
    /// squared-distance comparison so there is no per-item square root and no boundary rounding drift),
    /// in insertion order. A non-positive or non-finite `radius` returns only items coincident with
    /// `center` (radius 0) / an empty list, never throws.
    val queryRadius: center: Point -> radius: float -> grid: SpatialGrid<'T> -> 'T list
