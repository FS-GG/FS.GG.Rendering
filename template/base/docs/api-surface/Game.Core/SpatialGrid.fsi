// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/SpatialGrid.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A uniform spatial partition of positioned items, built once and queried many times. The
/// representation is **opaque** (hidden in the `.fsi`): callers build with `SpatialGrid.build` or
/// `SpatialGrid.buildBounds` and read with `SpatialGrid.query`/`queryRadius`/`queryBounds`. Immutable and
/// pure — no shared mutable state — so it is safe to hold inside a `Model`. Bucketing is an internal
/// broad-phase acceleration only — every query returns the **exact** set of items in the region (no false
/// positives and no false negatives), in the **insertion order** they were supplied at build, so results
/// are fully deterministic.
///
/// An item is bucketed either by its `FS.GG.Game.Core.Point` position (`build`) or by every cell its
/// `FS.GG.Game.Core.Rect` extent touches (`buildBounds`). The two are one rule, not two: `build` files a
/// position as a zero-size extent at itself.
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
    /// Build a grid bucketing each `(bounds, item)` under **every cell its `bounds` touches** at
    /// `cellSize`, rather than the single cell one position falls in. This is what lets `queryBounds` ask
    /// only for an item's own extent: a caller of `build` must otherwise dilate every query by the largest
    /// extent in the whole world, and one large item then destroys the acceleration for every other.
    ///
    /// The item's *position*, as `query`/`queryRadius` read it, is the **minimum corner** of its `bounds`.
    ///
    /// Insertion order is preserved for deterministic query results, and an item reached through several
    /// of its cells is returned once. Total on degenerate input, on the same terms as `build`: a
    /// non-positive or non-finite `cellSize` falls back to a single bucket, and an item whose `bounds` are
    /// non-finite — or span more cells than the grid will file one item under — keep exact results by
    /// becoming a candidate for *every* query. Such an item loses only **its own** spatial acceleration;
    /// every other item keeps theirs.
    val buildBounds: cellSize: float -> items: seq<Rect * 'T> -> SpatialGrid<'T>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Every item whose position lies inside `region` (inclusive of the region edges, matching
    /// `Geometry.containsPoint`), in insertion order. A zero-area `region` returns items exactly on that
    /// degenerate rect under the same edge convention. Never throws.
    val query: region: Rect -> grid: SpatialGrid<'T> -> 'T list

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Every item whose **extent** overlaps `region`, decided by `Geometry.intersects` and so inheriting
    /// its **strict-edge convention: two rects that merely touch do not overlap.** In insertion order,
    /// without duplicates. Never throws.
    ///
    /// Well-defined on a grid from either constructor: on a `build` grid an item's extent is the zero-size
    /// rect at its position, so `queryBounds` returns exactly the items strictly inside `region` — the
    /// strict-edge counterpart of `query`'s inclusive one, not a synonym for it.
    val queryBounds: region: Rect -> grid: SpatialGrid<'T> -> 'T list

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Every item whose position lies within `radius` of `center` (inclusive: distance ≤ `radius`, using
    /// squared-distance comparison so there is no per-item square root and no boundary rounding drift),
    /// in insertion order. A non-positive or non-finite `radius` returns only items coincident with
    /// `center` (radius 0) / an empty list, never throws.
    val queryRadius: center: Point -> radius: float -> grid: SpatialGrid<'T> -> 'T list
