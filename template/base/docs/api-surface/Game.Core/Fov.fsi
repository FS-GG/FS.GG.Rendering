// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Fov.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract module exposed by the FS.GG.Game.Core package.
/// Deterministic grid field of view by **symmetric shadowcasting** (Ford/Milazzo) over a
/// caller-supplied transparency predicate. The framework holds no map: the predicate
/// `isTransparent` (a pure `Cell -> bool`) IS the map, so field of view works over an unbounded
/// integer cell space — the same predicate shape as `Pathfinding.astar`'s `isWalkable`, so one
/// terrain plugs into both.
///
/// **Opacity is not walkability.** This module consults opacity only. A cell may be transparent and
/// unwalkable (a chasm) or opaque and walkable (a secret door); model them as two independent
/// predicates, never one "wall" flag.
///
/// **Do not build field of view by testing line of sight to every cell in radius.** That is
/// `O(radius^3)` and produces asymmetric, artifact-ridden vision. Symmetric shadowcasting is
/// `O(cells in radius)` and gets symmetry structurally.
///
/// Pure and **bit-identical across runs and platforms** for identical inputs: the row-scan slope
/// window is carried as exact integer rationals (cross-multiplied compares, never a float), the
/// radius test is an integer squared distance, and the result is a `Set<Cell>` ordered by the total
/// `(Col, Row)` cell order — so there is no floating-point rounding, and no `HashSet`
/// iteration-order leakage (safe inside a deterministic-replay simulation `update`).
[<RequireQualifiedAccess>]
module Fov =

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The set of cells visible from `origin` within `radius`, given `isTransparent` (`true` = you can
    /// see through this cell). Uses symmetric shadowcasting: floors are modelled as centre points and
    /// walls as inscribed diamonds, which is what buys symmetry and expansive walls together.
    ///
    /// **Symmetry (the property that distinguishes this from a ray per cell):** for any two
    /// *transparent* cells `a` and `b` within `radius` of each other,
    /// `b ∈ fov isTransparent a radius  ⇔  a ∈ fov isTransparent b radius`.
    ///
    /// The guarantee is stated over transparent cells because **walls are deliberately asymmetric**:
    /// a visible wall is revealed (you see its near face) even though it blocks everything beyond it,
    /// so `b` may be an opaque cell in `fov a` while `a ∉ fov b`. This is the "expansive walls" rule —
    /// it is what makes a convex room show all of its walls rather than a ragged fringe — and it is a
    /// documented part of this contract, not an artifact.
    ///
    /// Shape is a **Euclidean disc**: a cell is clipped unless `dCol^2 + dRow^2 <= radius^2` (exact
    /// integer squared distance, so no jagged-circle rounding). Clipping applies to what is *revealed*
    /// only — an opaque cell outside the disc still casts its shadow, so shrinking the radius never
    /// reveals a cell that a larger radius hid.
    ///
    /// Total on degenerate input. `origin` is always visible when `radius >= 0`, **even when `origin`
    /// itself is opaque** (you occupy the cell you stand in); `radius = 0` therefore yields exactly
    /// `Set.singleton origin`, and `radius < 0` yields `Set.empty`. An always-opaque `isTransparent`
    /// yields `origin` plus its surrounding walls that fall inside the disc, and terminates — the scan
    /// is bounded by `radius`, never by the map.
    val fov: isTransparent: (Cell -> bool) -> origin: Cell -> radius: int -> Set<Cell>
