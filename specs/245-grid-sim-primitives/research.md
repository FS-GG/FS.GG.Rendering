# Phase 0 Research — Grid Simulation Primitives

All decisions below resolve the spec's deferred conventions (spec *Assumptions*) into concrete, documented choices. No open `NEEDS CLARIFICATION` remains.

## D1 — Placement: both modules in `FS.GG.UI.Canvas`

- **Decision**: Add `Pathfinding` and `SpatialGrid` to the **Canvas** package (`src/Canvas/`), `namespace FS.GG.UI.Canvas`, alongside 239's `Rng`/`FixedStep`.
- **Rationale**: Canvas is the deterministic fixed-timestep sim tier and already references only `Scene` (no viewer/layout/Skia), satisfying FR-012 "consumable standalone". These are the *next* sim primitives, so they belong with the sibling sim primitives, not in a new package (constitution: "dependencies minimized", "primitives are distinct layers"). `SpatialGrid` reuses `Scene.Point`/`Rect` + 239's `Geometry.containsPoint` (Canvas → Scene reference already exists).
- **Alternatives considered**: (a) a new `FS.GG.UI.Sim` package — rejected: premature fragmentation, no dependency reason to split. (b) Pathfinding in Scene — rejected: pathfinding is not geometry vocabulary and needs no Scene type; only `SpatialGrid` touches `Point`/`Rect`.

## D2 — Determinism mechanism (FR-003/FR-008): integer cost + total cell order

- **Decision**: The A* frontier is a priority ordered by the **integer** tuple `(f, h, Col, Row)` — total f-cost, then heuristic (classic "prefer closer to goal" tie-break), then a **total order over cells** (`Col` then `Row`). All costs and the heuristic are integers. `cameFrom`/`gScore` bookkeeping uses `Map<Cell, _>` (ordered, deterministic) — never `Dictionary`/`HashSet`.
- **Rationale**: Two independent ties would otherwise leak non-determinism: (i) floating-point cost equality (`√2` diagonal) and (ii) container iteration order. Integer costs kill (i); a total order on the priority key kills (ii) — the frontier pop is uniquely determined, so the reconstructed path is byte-identical across runs/platforms. This is the load-bearing acceptance requirement (SC-001) and directly mirrors 239's `Rng` replay-determinism discipline.
- **Alternatives considered**: (a) `System.Collections.Generic.PriorityQueue` with float priority — rejected: float ties + unspecified equal-priority dequeue order. (b) tie-break by insertion order — rejected: insertion order is itself neighbour-enumeration-dependent and less obviously total than `(Col, Row)`.

## D3 — Move-cost convention (FR-002)

- **Decision**: `FourWay` — every move costs 1 in `bfs`, 10 in `astar` (see D4 for why scaled). `EightWay` — orthogonal 10, diagonal **14** (integer √2 ≈ 1.41421 × 10, truncated). Heuristic: `FourWay` → Manhattan × 10; `EightWay` → octile distance `10·(dx+dy) + (14−2·10)·min(dx,dy)` = `10·max + 4·min` form, i.e. `14·min + 10·(max−min)`. Heuristic is admissible (never overestimates) so A* returns a true shortest path.
- **Rationale**: The classic 10/14 integer weighting is the standard way to get √2 diagonals without floats, preserving D2's bit-identical guarantee. Admissible heuristic guarantees optimality (acceptance scenario 1).
- **Alternatives considered**: uniform diagonal cost 10 (Chebyshev) — rejected: makes diagonal and orthogonal equal, producing visually "blocky" non-shortest euclidean paths; 14 matches consumer expectation for tower-defense/RTS routing.

## D4 — `bfs` vs `astar` cost scale

- **Decision**: `bfs` is unweighted (each move = 1 hop, FIFO frontier, no heuristic) and returns a minimum-**hop-count** path; `astar` uses the 10/14 weighted costs of D3. Both share `Cell`, `Neighbourhood`, `maxVisited`, endpoint-inclusion, and the D2 determinism guarantee.
- **Rationale**: FR-004 asks for a breadth-first *unweighted* mode "in addition to A*". Keeping BFS genuinely unweighted (not A* with a zero heuristic) gives the simplest hop-count answer consumers expect, while `astar` gives cost-optimal diagonal-aware routing. Under `EightWay`, BFS still honours the no-corner-cutting rule (D5) but treats a diagonal as one hop.

## D5 — Corner-cutting rule (EightWay, edge case)

- **Decision**: A diagonal step from `c` to `c+(±1,±1)` is permitted only when **both** shared orthogonal neighbours (`c+(±1,0)` and `c+(0,±1)`) are walkable. Otherwise the diagonal is not a neighbour.
- **Rationale**: Prevents an agent "slipping" through the corner between two walls — the near-universal expectation for tile games and what the TD1 consumer hand-rolled. Documented in `Neighbourhood.EightWay` doc-comment.
- **Alternatives considered**: allow corner-cutting (simpler) — rejected: produces paths that clip wall corners, a reported rough edge.

## D6 — Search bound (FR-005): `maxVisited`

- **Decision**: An explicit `maxVisited: int` parameter caps the number of cells **expanded** (popped from the frontier). Exceeding it returns `None`. `maxVisited <= 0` returns `None` immediately. A non-walkable `start` or `goal` returns `None` without searching. `start = goal` (walkable) returns `Some [start]`.
- **Rationale**: The cell space is unbounded (the predicate is the only map), so an unreachable goal must be bounded to terminate. An explicit cap — rather than an implicit bounding box — keeps the framework map-agnostic and puts the budget in the caller's hands (they know their grid size). Total, non-throwing (FR-005).
- **Alternatives considered**: infer bounds from first-N walkable cells — rejected: the predicate has no enumerable domain; a caller-supplied cap is honest and simple.

## D7 — SpatialGrid query contract (FR-007): exact results

- **Decision**: `query`/`queryRadius` return the **exact** set of items in the region (point-in-rect via `Geometry.containsPoint`; radius via squared-distance ≤ `radius²`), not raw broad-phase bucket candidates. Bucketing is an internal acceleration; the public result has no false positives and no false negatives. Results are in **insertion order**.
- **Rationale**: Items are single `Point`s, so the exact test is O(1) per candidate — cheap and clearer than pushing the filter onto every caller (spec assumption: "exact if cheap and clearer"). A no-false-negative *and* no-false-positive contract is easier to test and reason about. Squared-distance avoids a per-item `sqrt` and any boundary rounding drift, preserving determinism.
- **Alternatives considered**: return broad-phase candidates (items in touched cells) — rejected: forces every consumer to re-filter, re-introducing the boilerplate this feature removes; only worth it for extended (non-point) item bounds, which are out of scope.

## D8 — Degenerate `cellSize` (edge case)

- **Decision**: A non-positive or non-finite `cellSize` falls back to a **single bucket** holding all items; queries still return exact results (by filtering the one bucket), just without spatial acceleration. Never throws, never divides by zero.
- **Rationale**: Totality (constitution VI). A consumer passing a bad cell size gets correct-but-slow results rather than a crash — the safest failure for a pure helper.

## D9 — Opaque `SpatialGrid<'T>` type

- **Decision**: Expose `SpatialGrid<'T>` as an **opaque** type in the `.fsi` (`[<Sealed>] type SpatialGrid<'T>` with no visible representation). The internal bucket map + ordered item vector stay private to the `.fs`.
- **Rationale**: Constitution II (visibility lives in `.fsi`). Callers never need the representation; hiding it lets the internal layout change without a surface break, and keeps the baseline surface minimal (`FS.GG.UI.Canvas.SpatialGrid\`1`). `Pathfinding`'s `Cell`/`Neighbourhood` are, by contrast, transparent value types the caller constructs directly.

## D10 — Tier / contract-change & release (FR-014)

- **Decision**: Tier 1. On release, follow publish-before-flip: bump the FS.GG.UI coherent set (the two version-of-truth files + the tag triple), confirm the package is live on the feed, then flip `registry/dependencies.yml` (`fs-gg-ui-template` contract version + the consuming edge), prepend a dated `registry/CHANGELOG.md` entry, and update `docs/registry/compatibility.md`. Re-pin `FS.GG.Templates` `providers/rendering.providers.yml`.
- **Rationale**: New public surface = versioned cross-repo contract (cross-repo-coordination skill, "Release a coherent set"). Additive, so no coherence break — consumers opt in by moving their pin. This is scheduled as the final task group and is the natural human-authorized checkpoint (outward-facing, publishes to the org feed).
