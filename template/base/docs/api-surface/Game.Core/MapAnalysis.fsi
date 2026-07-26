// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/MapAnalysis.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A validation constraint for `MapAnalysis.validate` (M11). A closed set of the common map-quality rules —
/// `Connected` (all floor is one component), `MinDiameter`/`MaxDiameter` (critical-path length bounds),
/// `MinBorderOpenings` (at least N entrances/exits), and `MaxComponents` (at most N separate floor areas).
type Rule =
    | Connected
    | MinDiameter of int
    | MaxDiameter of int
    | MinBorderOpenings of int
    | MaxComponents of int

/// Public contract type exposed by the FS.GG.Game.Core package.
/// The result of `MapAnalysis.validate` (M11): `Passed` is true with an empty `Failures` iff every `Rule`
/// held; otherwise `Failures` carries one reason string per violated rule, in rule-list order. The measured
/// facts (`Connected`/`ComponentCount`/`Diameter`/`BorderOpenings`) are always populated, so a caller can
/// read them whether or not it passed.
type Report =
    { Passed: bool
      Failures: string list
      Connected: bool
      ComponentCount: int
      Diameter: int
      BorderOpenings: int }

/// Public contract module exposed by the FS.GG.Game.Core package.
/// Producer-agnostic analysis over a map — a `TileMap`, or (like `Pathfinding`) a `Cell -> bool`
/// walkability predicate. The analysis half of the `fs-gg-mapcraft` construction pipeline
/// (produce → analyze → validate): a map-builder asks these questions of a map however it was made —
/// procedurally (`MapGen`), hand-authored, or agent-drawn. Everything is pure, total, and deterministic,
/// and built on `Pathfinding`/`MapGen` so its answers agree with the router's neighbour logic.
///
/// M8 covers reachability & connectivity. Later milestones add chokepoints, path metrics, fairness &
/// validation, and tactical shape.
[<RequireQualifiedAccess>]
module MapAnalysis =

    /// The set of cells reachable from `start` over the `isWalkable` predicate and `neighbourhood`, bounded
    /// by `maxVisited`. Built on `Pathfinding.distanceField`, so it uses the router's exact neighbour logic
    /// (no corner-cutting under `EightWay`) — the returned set is exactly what `Pathfinding.bfs` can reach.
    /// A non-walkable `start` yields the empty set. Argument order matches `Pathfinding.bfs`.
    val reachable:
        neighbourhood: Neighbourhood -> maxVisited: int -> isWalkable: (Cell -> bool) -> start: Cell -> Set<Cell>

    /// The `Floor` cells of `map` **not** reachable from `from` under `neighbourhood`, in row-major order —
    /// the "did I strand a region?" answer. When `from` is not a `Floor` cell, every `Floor` cell is
    /// stranded (nothing is reachable from a wall). Total.
    val stranded: neighbourhood: Neighbourhood -> from: Cell -> map: TileMap -> Cell list

    /// Whether every `Floor` cell of `map` forms a single connected component under `neighbourhood`. A map
    /// with no `Floor` is connected (vacuously). Total.
    val isConnected: neighbourhood: Neighbourhood -> map: TileMap -> bool

    /// The number of connected `Floor` components of `map` under `neighbourhood` — the count of
    /// `MapGen.regions` (which is the component list itself). Total; an empty-floor map has 0 components.
    val componentCount: neighbourhood: Neighbourhood -> map: TileMap -> int

    /// The `Floor` cells on the map's outer ring (`col = 0`, `col = Width-1`, `row = 0`, or `row = Height-1`),
    /// in row-major order — a map's entrances/exits, where it meets the outside. Neighbourhood-independent.
    /// Total.
    val borderOpenings: map: TileMap -> Cell list

    /// The `Floor` cells with exactly one `Floor` neighbour under `neighbourhood` (no corner-cutting under
    /// `EightWay`), in row-major order — the map's dead-ends. Total.
    val deadEnds: neighbourhood: Neighbourhood -> map: TileMap -> Cell list

    /// The chokepoints of `map` under `neighbourhood` — exactly the `Floor` cells whose removal increases the
    /// floor component count (graph articulation points / cut vertices), in row-major order. Implemented with
    /// **iterative** DFS (Tarjan), so a long single-cell-wide corridor cannot overflow the stack — total on
    /// any shape.
    val articulationPoints: neighbourhood: Neighbourhood -> map: TileMap -> Cell list

    /// The eccentricity of `cell` — the maximum **unweighted hop distance** (BFS, each `neighbourhood` step =
    /// 1) from `cell` to any `Floor` cell reachable from it. 0 when `cell` is not `Floor` or has no reachable
    /// neighbour. This is a topological hop count, distinct from `Pathfinding.distanceField`'s `baseStep`/√2
    /// movement cost. Total.
    val isolation: neighbourhood: Neighbourhood -> map: TileMap -> cell: Cell -> int

    /// The diameter of `map` under `neighbourhood` — the longest shortest-path (in unweighted hops) between
    /// any two `Floor` cells in the same component, i.e. the map's critical-path length. Equals the maximum
    /// over all `Floor` cells of `isolation`. 0 for an empty-floor or single-cell map. O(V²); a build/validate
    /// -time metric, not a per-tick one. Total.
    val diameter: neighbourhood: Neighbourhood -> map: TileMap -> int

    /// The minimum and mean nearest-neighbour **Manhattan** distance of `points` (a geometric spread measure
    /// of a point set, independent of any map). Fewer than two points ⇒ `struct (0, 0.0)`. Total.
    val spacing: points: Cell list -> struct (int * float)

    /// Each `spawn` that can reach a `resource` mapped to its nearest-resource **hop distance** (BFS over the
    /// map's corner-cut-aware adjacency under `neighbourhood`) — a spread of these values tells a designer
    /// whether spawns are treated fairly. A spawn that reaches no resource is omitted. Total.
    val fairness: spawns: Cell list -> resources: Cell list -> neighbourhood: Neighbourhood -> map: TileMap -> Map<Cell, int>

    /// The fraction of `Floor` cells within `radius` hops of some cell in `points` (BFS over the map's
    /// corner-cut-aware adjacency). 0.0 when there is no floor or no point. Total.
    val coverage: neighbourhood: Neighbourhood -> map: TileMap -> points: Cell list -> radius: int -> float

    /// Run `rules` against `map` under `neighbourhood` and return a `Report` — the keystone of the
    /// produce → analyze → **validate** → re-produce loop. `Passed` is true with empty `Failures` iff every
    /// rule holds; otherwise one reason string per violated rule in rule-list order. The measured facts are
    /// always populated. Total; deterministic.
    val validate: rules: Rule list -> neighbourhood: Neighbourhood -> map: TileMap -> Report

    /// The **static** exposure of `map` (M12): each `Floor` cell mapped to the number of *other* `Floor` cells
    /// that can see it, via the caller's `hasLos: Cell -> Cell -> bool` oracle. High exposure is open killing
    /// ground; low exposure is sheltered. A property of geometry alone, computable with no units present —
    /// distinct from `Ai.threatField` (the dynamic, enemy-keyed answer). Under a symmetric `hasLos`, exposure
    /// is symmetric. O(V²); a build/analyze-time metric. Total (given a total `hasLos`).
    val exposureMap: hasLos: (Cell -> Cell -> bool) -> map: TileMap -> Map<Cell, int>

    /// The cover of `map` (M12): each `Floor` cell mapped to the number of its 8 neighbouring positions that
    /// are `Wall` or off-map (0..8) — the directions it is protected from. Needs no line-of-sight oracle.
    /// Total.
    val coverMap: map: TileMap -> Map<Cell, int>

    /// The killzones of `map` (M12): the canonical `(a, b)` pairs (`a < b`) of `Floor` cells that can see each
    /// other (`hasLos a b`) and are at least `minLength` apart in **Chebyshev** (king-move) distance — the
    /// long open sightlines a designer wants to break up or defend. Returned in `(a, b)` order. Total (given a
    /// total `hasLos`).
    val killzones: hasLos: (Cell -> Cell -> bool) -> minLength: int -> map: TileMap -> (Cell * Cell) list
