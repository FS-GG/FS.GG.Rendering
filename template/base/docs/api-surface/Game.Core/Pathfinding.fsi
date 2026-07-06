namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// An integer grid coordinate — the atom over which walkability, neighbours, and paths are expressed.
/// Distinct from `FS.GG.Game.Core.Point` (float): a `Cell` is a discrete tile index, not a continuous
/// position. Structural equality gives a stable identity for the frontier/visited bookkeeping and, with
/// `(Col, Row)` ordering, the total tie-break order that keeps paths byte-identical (no hash-set
/// iteration-order leakage).
[<Struct>]
type Cell = { Col: int; Row: int }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// Movement neighbourhood for grid pathfinding.
type Neighbourhood =
    /// 4-connected: N/E/S/W only, no diagonals. Each move costs 1 (`bfs`) / 10 (`astar`).
    | FourWay
    /// 8-connected: orthogonals plus diagonals. A diagonal is allowed only when both shared orthogonal
    /// neighbours are walkable (no corner-cutting). Orthogonal move costs 10, diagonal 14 (integer
    /// √2-scaled; never a float, so equal-cost ties can never leak through floating-point equality).
    | EightWay

/// Public contract module exposed by the FS.GG.Game.Core package.
/// Deterministic grid pathfinding over a caller-supplied walkability predicate. The framework holds no
/// map: the predicate `isWalkable` (a pure `Cell -> bool`) IS the map, so pathfinding works over an
/// unbounded integer cell space. Both functions are pure and **bit-identical across runs and
/// platforms** for identical inputs — the frontier is ordered by a total integer key `(f, h, Col, Row)`
/// and costs are integers, so there is no `Dictionary`/`HashSet` iteration-order or floating-point
/// tie-break leakage (safe to run inside a deterministic-replay simulation `update`).
[<RequireQualifiedAccess>]
module Pathfinding =

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// A* shortest path from `start` to `goal` over walkable cells. Returns `Some path` where `path` is
    /// the cell sequence **including both `start` and `goal`** (a single-element `[start]` when
    /// `start = goal` and `start` is walkable), or `None` when no walkable path exists. Total on
    /// degenerate input: a non-walkable `start` or `goal` yields `None`; the search is bounded by
    /// `maxVisited` (the maximum number of cells expanded) so an unreachable or unbounded goal
    /// terminates with `None` rather than searching forever. `maxVisited <= 0` yields `None`.
    val astar:
        neighbourhood: Neighbourhood ->
        maxVisited: int ->
        isWalkable: (Cell -> bool) ->
        start: Cell ->
        goal: Cell ->
            Cell list option

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Breadth-first (unweighted) shortest path from `start` to `goal`, same walkability predicate,
    /// neighbourhood, `maxVisited` bound, endpoint-inclusion, and determinism guarantee as `astar`.
    /// Equivalent to `astar` when every move costs the same; offered for callers who want the simplest
    /// hop-count path without the A* heuristic. Under `EightWay`, applies the same no-corner-cutting
    /// rule (diagonal only when both shared orthogonals are walkable).
    val bfs:
        neighbourhood: Neighbourhood ->
        maxVisited: int ->
        isWalkable: (Cell -> bool) ->
        start: Cell ->
        goal: Cell ->
            Cell list option
