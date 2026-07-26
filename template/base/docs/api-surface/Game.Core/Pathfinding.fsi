// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Pathfinding.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// Movement neighbourhood for grid pathfinding. (`Cell`, the grid coordinate this operates over, is a
/// shared primitive declared in `Primitives`.)
type Neighbourhood =
    /// 4-connected: N/E/S/W only, no diagonals. Each move costs 1 (`bfs`) / `baseStep` (`astar`).
    | FourWay
    /// 8-connected: orthogonals plus diagonals. A diagonal is allowed only when both shared orthogonal
    /// neighbours are walkable (no corner-cutting). An orthogonal move costs `baseStep`, a diagonal
    /// `baseStep * 14 / 10` (integer √2-scaled; never a float, so equal-cost ties can never leak
    /// through floating-point equality).
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

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// One settled cell of a `reachable` search: what it cost to get here, and which cell we came from.
    /// `CameFrom` is `None` for exactly one cell — the `start` the search was seeded with.
    type Step =
        { /// Total cost of entering this cell from `start`, in the same `baseStep`-scaled units as
          /// `reachableWithin`'s values (`baseStep` per orthogonal step, `baseStep * 14 / 10` per
          /// diagonal, times `cost`) — *not* movement points. See `baseStep`.
          Cost: int
          /// The predecessor on the cheapest route from `start`. `None` only for `start` itself.
          CameFrom: Cell option }

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// The result of a `reachable` search: **two sets, and they are deliberately not the same set.**
    ///
    /// `Steps` is every cell the search settled — *including* cells that may only be passed **through**.
    /// `Endable` is the subset the unit may legally **stop** on. They differ by exactly the caller's
    /// `canEndOn` predicate: a cell occupied by an ally is traversable (so it is in `Steps`) and is not
    /// a legal destination (so it is not in `Endable`).
    ///
    /// `Endable` is the **highlight** set. `Steps` is what **reconstructs a path** (see `pathTo`), and it
    /// must be the *unfiltered* map: a route through an ally passes through a cell that is deliberately
    /// absent from `Endable`. Reconstruct from the filtered set and you dead-end on precisely the routes
    /// `canEndOn` exists to allow.
    type Reach =
        { /// Every settled cell, including pass-through-only ones. The path source.
          Steps: Map<Cell, Step>
          /// The subset of `Steps` that `canEndOn` admitted. The highlight set, and the only set from
          /// which a destination may be offered.
          Endable: Set<Cell> }

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// A* shortest path from `start` to `goal` over walkable cells. Returns `Some path` where `path` is
    /// the cell sequence **including both `start` and `goal`** (a single-element `[start]` when
    /// `start = goal` and `start` is walkable), or `None` when no walkable path exists. Total on
    /// degenerate input: a non-walkable `start` or `goal` yields `None`; the search is bounded by
    /// `maxVisited` (the maximum number of cells expanded) so an unreachable or unbounded goal
    /// terminates with `None` rather than searching forever. `maxVisited <= 0` yields `None`.
    ///
    /// **`isWalkable` is binary, so `astar` minimises `baseStep` distance — *not* terrain cost.** It
    /// takes no `cost` function; it cannot see that one cell is mud and the next is road. If your
    /// terrain has non-uniform cost, this returns the fewest-steps route, which may be arbitrarily more
    /// **expensive** than the cheapest one. Do not use it to path a unit within a movement budget you
    /// costed with `reachableWithin` — the two answer different questions and will disagree. Use
    /// `reachable` + `pathTo`, which cost the highlight and the path in one search.
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

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Multi-source Dijkstra **from the goals outward** — the "Dijkstra map" / integration field. For
    /// every reachable cell it stores the cheapest cost of travelling from that cell to the *nearest*
    /// goal; each goal itself maps to `0`. There is deliberately **no early exit**: when many agents
    /// share a destination, build one field and let each agent roll downhill (see `flowField`) rather
    /// than running `astar` per agent.
    ///
    /// `cost c` is the cost to **enter** cell `c`, and `cost c <= 0` means **impassable** — so
    /// `fun c -> cost c > 0` is exactly the `isWalkable` predicate `astar`/`bfs` take, and one terrain
    /// function drives all of them. A move onto `c` costs the step weight times `cost c` — `baseStep`
    /// orthogonally, `baseStep * 14 / 10` diagonally, the same scale `astar` uses. Hence with
    /// `cost = fun _ -> 1` the value at any cell equals `astar`'s path cost from that cell to the goal.
    ///
    /// Deterministic: integer costs and a total `(distance, Col, Row)` frontier order, so the field is
    /// bit-identical across runs and platforms (no `Map`/`Set` iteration-order or float tie-break
    /// leakage). Total on degenerate input: an empty `goals` list, a `goals` list whose every entry is
    /// impassable, or `maxVisited <= 0` all yield an empty field; impassable goals are skipped, not
    /// thrown. The walk is bounded by `maxVisited` (cells settled) so an unbounded cell space
    /// terminates, and only **settled** cells are returned — a `maxVisited` cut-off yields a partial
    /// field whose values are all final, never unfinalised tentative ones. Costs accumulate in `int64`
    /// internally; a cell whose cost would exceed `Int32.MaxValue` is treated as unreachable rather
    /// than overflowing.
    ///
    /// A **flee field** is this function composed, not a separate primitive: scale a `distanceField` by
    /// a negative coefficient (≈ `-1.2`) and re-relax, then roll downhill. The coefficient is a tuning
    /// constant — game policy, not navigation — so it lives in the caller, not here.
    val distanceField:
        neighbourhood: Neighbourhood ->
        maxVisited: int ->
        cost: (Cell -> int) ->
        goals: Cell list ->
            Map<Cell, int>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The negative gradient of a `distanceField`: maps each cell to its strictly-lowest-valued
    /// neighbour — the next step for an agent rolling downhill toward a goal. Membership in `field` is
    /// the walkability predicate, so the same no-corner-cutting rule applies under `EightWay` (a
    /// diagonal step is offered only when both shared orthogonal neighbours are also in `field`).
    ///
    /// A cell with no strictly-lower neighbour is a **sink** — a goal, or a local minimum — and is
    /// simply **absent** from the result, so `Map.tryFind` returning `None` is how an agent learns it
    /// has arrived (or is stuck). Only a **strictly** lower neighbour is downhill: on a plateau, or
    /// between two adjacent goals, an `<=` rule would emit arrows that cycle. The step is always a
    /// `Cell`, never a float vector: integer logic, float presentation. Deterministic — ties among
    /// equally-low neighbours break on `(value, Col, Row)`. Total: an empty `field` yields an empty map.
    ///
    /// **Pass the same `neighbourhood` that built `field`.** It cannot be checked here, and it is not a
    /// free choice: widening a `FourWay` field to `EightWay` offers diagonal steps whose 14-cost was
    /// never priced into the field, so the agent rolls down a route the field never costed.
    val flowField: neighbourhood: Neighbourhood -> field: Map<Cell, int> -> Map<Cell, Cell>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Dijkstra **forward** from `start`, capped at a movement-point `budget`: every cell reachable for
    /// a total entry cost of at most `budget`, mapped to that cost. `start` itself maps to `0` (its own
    /// cost is not charged — you are already standing on it).
    ///
    /// **This is the cost map, not the move range — it discards its predecessors.** You cannot
    /// reconstruct a path from a `Map<Cell, int>`, and pathing the unit with `astar` instead does not
    /// recover one: `astar` minimises steps, not terrain cost, so it will happily return a route that
    /// overruns the very `budget` this function just enforced. Nor can one `cost` predicate say "path
    /// through an ally, but do not stop on them" — `cost c <= 0` makes a cell neither traversable nor
    /// endable. **For a turn-based-tactics move range, use `reachable`**, which keeps the `CameFrom`
    /// tree and takes a separate `canEndOn`. This function remains the right answer when you want only
    /// the costed set — an AI scoring candidate cells, say, or a threat overlay.
    ///
    /// **`budget` is in `baseStep` units, not movement points — scale it with `budgetFor`** (a raw
    /// `moveRange` yields the start cell alone, silently; see `baseStep`).
    ///
    /// Same `cost` convention as `distanceField` (`cost c` is the cost to enter `c`; `cost c <= 0` is
    /// impassable), same `baseStep` scale, and the same determinism guarantee. Note the
    /// direction differs: `reachableWithin` charges the cell being stepped **onto** as it walks away
    /// from `start`, whereas `distanceField` walks goal-ward. Under a **uniform** cost the two agree
    /// cell-for-cell (`reachableWithin` equals the `distanceField` from `start` filtered by `budget`);
    /// under a **non-uniform** cost they disagree on the values, and `reachableWithin` is always a
    /// **subset** of the field — it is pruned by `budget`, which the field is not.
    ///
    /// `budget` prunes but does not bound: because the framework holds no map, an unbounded `cost`
    /// predicate admits a reachable set that grows quadratically in `budget`. So the walk is bounded by
    /// `maxVisited` (cells settled), exactly as in `astar`/`bfs`/`distanceField`, and only settled cells
    /// are returned. Total: `maxVisited <= 0`, a negative `budget`, or an impassable `start` all yield
    /// an empty map.
    val reachableWithin:
        neighbourhood: Neighbourhood ->
        maxVisited: int ->
        cost: (Cell -> int) ->
        budget: int ->
        start: Cell ->
            Map<Cell, int>
