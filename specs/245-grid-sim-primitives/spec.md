# Feature Specification: FS.GG.UI Grid Simulation Primitives (Pathfinding + Spatial Grid)

**Feature Branch**: `245-grid-sim-primitives`

**Created**: 2026-07-05

**Status**: Draft

**Input**: FS-GG/FS.GG.Rendering#110 (epic FS-GG/FS.GG.Rendering#115, TD1 *Bulwark: Tower Defense* field-feedback report `FEEDBACK.md` §3.3, §4.3, §5, Rec #1 — Effort M, High, "unblocks every game profile"). Add two small, high-reuse, deterministic, simulation-shaped public helpers to FS.GG.UI so grid-based game/sim consumers stop re-implementing them: grid **pathfinding** (A*/BFS over a walkability predicate) and a uniform **spatial grid** for O(1)-ish range/splash queries.

## Context

FS.GG.UI is a rendering-first framework. Feature 239 shipped the first tier of simulation primitives — a public `Rect` geometry helper, a value-type seeded PRNG (`Rng`), and a fixed-timestep accumulator (`FixedStep`) — inside `FS.GG.UI.Canvas`. A full charter→ship SDD consumer build of the TD1 *Bulwark: Tower Defense* TestSpec (57/57 tests, 0 synthetic evidence) found the rendering/input/layout/sim surface pleasant but surfaced the **next** re-implemented tier: every grid-based game/simulation consumer re-rolls the same two *grid-shaped* helpers because the framework does not ship them:

- The `fs-gg-game-core` skill's own performance guidance **recommends** bucketing entities into a uniform spatial grid for range/splash queries — but the framework ships no spatial partition, so the TD1 consumer hand-rolled range queries by hand. Every game-profile consumer in the sample matrix (tower-defense, twin-stick, RTS, roguelike) will re-implement the same bucketing.
- Grid pathfinding (routing an enemy/agent over walkable tiles) is a near-universal grid-game need, but the framework ships no pathfinder, so the TD1 consumer hand-rolled BFS/A* over the tile grid — including the deterministic tie-break needed to keep replay bit-identical, which is exactly the subtle part consumers get wrong.

These gaps are additive and non-architectural: two pure, value-shaped helpers that make the *already-recommended* patterns real, without introducing any per-game logic and without touching the viewer/layout/input surfaces. They extend the same `FS.GG.UI.Canvas` sim-primitive tier established by feature 239.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Deterministic grid pathfinding (Priority: P1)

A consumer building a grid-based game needs to route an agent from a start cell to a goal cell across a tile grid, treating some cells as blocked (walls, water, occupied). Today they must hand-write BFS or A* over the grid — and, critically, get the *tie-break* right so that two runs with identical inputs always return the byte-identical path (a determinism-replay acceptance requirement). This story ships a pathfinding helper over a caller-supplied walkability predicate, with 4- and 8-neighbour movement and a stable, documented tie-break, so the pathfinding surface every grid game needs actually exists and is safe to run inside a deterministic simulation.

**Why this priority**: This is the higher-effort, higher-risk half of the gap — the deterministic tie-break is the part consumers most often get subtly wrong (iteration-order and floating-point leakage silently break same-seed replay). It unblocks routing for the core game loop and is independently valuable even if the spatial grid never ships. It is the parent that the `fs-gg-game-core` grid-sim recipe (FS-GG/FS.GG.Rendering#112) is blocked on.

**Independent Test**: A consumer can define a small grid with blocked cells, request a path from start to goal, and observe a correct shortest path (or a documented "no path" result), then re-run the identical request and confirm the returned path is byte-identical — verifiable in isolation with unit assertions, no rendering or game loop required.

**Acceptance Scenarios**:

1. **Given** a grid with a clear corridor between start and goal, **When** the consumer requests a path, **Then** they receive a shortest walkable path from start to goal (a documented convention for whether endpoints are included).
2. **Given** a grid where every route to the goal is blocked, **When** the consumer requests a path, **Then** they receive a documented, non-throwing "no path found" result.
3. **Given** a grid with two or more equal-cost shortest paths, **When** the consumer requests a path twice with identical inputs, **Then** both requests return the byte-identical path (the tie-break is stable and independent of hash/dictionary iteration order).
4. **Given** the same grid, **When** the consumer requests a path under 4-neighbour vs 8-neighbour movement, **Then** each honours its neighbourhood (no diagonal steps under 4-neighbour) and applies the documented per-move cost convention.
5. **Given** a start cell equal to the goal cell, or a start/goal that is itself blocked, **When** the consumer requests a path, **Then** the result follows a documented, non-throwing convention (trivial path / no path).

---

### User Story 2 - Uniform spatial grid for range/splash queries (Priority: P2)

A consumer needs to answer "which entities are near this point / inside this rectangle" every frame — for splash damage, proximity triggers, or broad-phase collision — without scanning every entity (O(n²)). Today they hand-roll the bucketing the `fs-gg-game-core` guidance recommends. This story ships a uniform spatial grid built from a cell size and a set of positioned items, with rectangle and radius queries, so the range-query surface the guidance already recommends actually exists.

**Why this priority**: It removes the most-recommended-but-unshipped performance helper and is required for any game with many interacting entities to scale. It is second because pathfinding (P1) carries the harder determinism contract and gates the downstream recipe issue, whereas the spatial grid is a more mechanical bucket structure; a consumer can ship a small game with a naive scan before adopting it.

**Independent Test**: A consumer can build a grid from a cell size and a list of positioned items, query a rectangle and a radius, and confirm the returned items are exactly those overlapping the query region (no false negatives, and any false positives are documented as broad-phase candidates) — verifiable purely with assertions.

**Acceptance Scenarios**:

1. **Given** a set of positioned items and a cell size, **When** the consumer builds a spatial grid and queries a rectangle, **Then** every item whose position falls in the rectangle is returned.
2. **Given** a built grid, **When** the consumer queries a radius around a center point, **Then** every item within the radius is returned under a documented distance convention.
3. **Given** two consumers that build a grid from the identical items in the identical order and run the identical query, **When** they read the results, **Then** the returned item collections are identical in content and order (the structure is pure and deterministic).
4. **Given** a query region larger than the populated area, or an empty item set, **When** the consumer queries, **Then** they get a documented, non-throwing result (all items / empty).

---

### Edge Cases

- **Pathfinding — unreachable goal**: a fully walled-off goal yields the documented "no path" result, not an exception or an unbounded search.
- **Pathfinding — degenerate endpoints**: start equals goal; start or goal blocked; start or goal outside the caller-defined search bound — each resolves under one documented, non-throwing convention.
- **Pathfinding — tie-break stability**: multiple equal-cost frontier cells must be resolved by a total order over cells (not by open-set/closed-set container iteration order and not by floating-point cost equality), so the path is bit-identical across runs and platforms.
- **Pathfinding — 8-neighbour diagonal cost**: the diagonal move cost convention (uniform vs √2-weighted) is chosen once and documented; corner-cutting past two blocked orthogonal neighbours follows a documented convention.
- **SpatialGrid — non-positive cell size**: a zero or negative cell size must not divide-by-zero or loop unboundedly; documented, non-throwing behavior.
- **SpatialGrid — items on cell boundaries / coincident items**: items exactly on a cell edge or sharing a position resolve under one documented convention and appear in query results deterministically.
- **SpatialGrid — negative / out-of-range coordinates**: items or queries at negative coordinates are handled under a documented convention (no array-index underflow).
- **SpatialGrid — degenerate query**: a zero-area rectangle or zero radius returns a documented result consistent with the boundary convention.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The framework MUST expose a public grid-pathfinding helper that, given a start cell, a goal cell, and a caller-supplied walkability predicate over integer grid cells, returns a walkable path from start to goal or a documented "no path found" result.
- **FR-002**: The pathfinding helper MUST support both 4-neighbour and 8-neighbour movement, selectable by the caller, and MUST apply a single documented per-move cost convention (including the diagonal-move cost under 8-neighbour).
- **FR-003**: The pathfinding helper MUST resolve equal-cost choices by a stable total order over cells (not by hash/dictionary/set iteration order, and not by floating-point tie equality), so that identical inputs always produce a byte-identical path across runs and platforms (deterministic-replay safe).
- **FR-004**: The pathfinding helper MUST offer a breadth-first (unweighted shortest-path) mode in addition to A*, over the same walkability predicate and neighbourhood selection, with the same determinism guarantee.
- **FR-005**: The pathfinding helper MUST handle degenerate inputs (start = goal, start or goal blocked, endpoints outside the caller-defined search bound) under a documented, non-throwing convention, and MUST bound its search so an unreachable goal terminates rather than searching unboundedly.
- **FR-006**: The framework MUST expose a public uniform spatial-grid structure built from a cell size and a set of positioned items, supporting a rectangle query and a radius query that return the items overlapping the query region.
- **FR-007**: The spatial-grid queries MUST NOT produce false negatives (every item genuinely inside the query region is returned); any broad-phase false positives (items in a touched cell but outside the exact region) MUST be documented as such, with a documented convention for whether queries return exact or broad-phase candidate sets.
- **FR-008**: The spatial grid MUST be pure and deterministic: building from identical items in identical order and running an identical query MUST yield an identical result collection in identical order, with no shared mutable state and no reliance on hash-set iteration order.
- **FR-009**: The spatial grid MUST handle degenerate inputs (non-positive cell size, empty item set, boundary/coincident items, negative or out-of-range coordinates, zero-area query) under documented, non-throwing conventions.
- **FR-010**: Both helpers MUST be pure `Model -> Model`-safe value functions (no side effects, no shared mutable state observable to the caller) honoring the MUE boundary, and MUST be usable inside an immutable simulation model.
- **FR-011**: Both helpers MUST be additive public surface on FS.GG.UI (the `FS.GG.UI.Canvas` sim-primitive tier alongside `Rng`/`FixedStep`) — no existing public type, signature, or behavior may change — and MUST contain no per-game logic (no entities, scores, waves, damage, or game rules; the walkability predicate and item positions are supplied by the caller).
- **FR-012**: Each helper MUST be usable by a consumer without pulling in rendering, viewer, layout, or input machinery (they are simulation primitives, consumable standalone), preserving `FS.GG.UI.Canvas`'s zero-viewer/zero-layout footprint.
- **FR-013**: The new public surface MUST carry per-member API documentation (signatures + doc comments) and MUST be reflected in the repository's public-API stubs (so the honest-public-API stub gate stays green) and in the shipped product documentation (the relevant product skill / `fs-gg-game-core` reference), so the "recommended but unshipped" grid guidance points at the now-real surface.
- **FR-014**: Shipping these helpers is a versioned cross-repo **contract-change**: the release MUST bump the FS.GG.UI coherent set and, publish-before-flip, update `registry/dependencies.yml` and `docs/registry/compatibility.md` in `FS-GG/.github` so downstream consumers can pin the version that carries the new surface.
- **FR-015**: A Dijkstra flow-field (all-cells-to-one-goal distance field) is explicitly OUT OF SCOPE for this feature and MUST be recorded as a future RTS-profile primitive, not shipped here.

### Key Entities *(include if feature involves data)*

- **Grid cell (new, value)**: an integer `(column, row)` coordinate identifying a tile; the atom over which walkability, neighbours, and paths are expressed. The helper operates on integer cells and MUST NOT introduce a look-alike of the existing `Point`.
- **Walkability predicate (caller-supplied)**: a pure function from a grid cell to "walkable / blocked" that the caller provides; it, not the framework, encodes the game's map. The framework holds no map state.
- **Path result (new)**: an ordered sequence of grid cells from start to goal (endpoint-inclusion documented), or a documented "no path found" value.
- **Positioned item (caller-supplied)**: an item paired with a position (reusing the existing `FS.GG.UI.Scene.Point`/`Rect` vocabulary rather than a look-alike) that the spatial grid buckets; the item payload is opaque to the framework.
- **Spatial grid (new)**: an immutable structure built from a cell size and positioned items, queried by rectangle and by radius; carries no game semantics, only spatial bucketing.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A grid-game consumer can route an agent across a tile grid using only the shipped pathfinding helper, with zero hand-written BFS/A* code, and obtain byte-identical paths across repeated identical-input runs (deterministic-replay acceptance passes).
- **SC-002**: A consumer can answer per-frame range/splash "what is near here" queries using only the shipped spatial grid, with zero hand-rolled bucketing, and avoid an O(n²) all-pairs scan.
- **SC-003**: Building a comparable next grid-based game reuses these two primitives instead of re-implementing them, measurably reducing the grid-simulation plumbing a consumer must author (the epic's target: field-reported rough edges from TD1 removed for the next game-profile build).
- **SC-004**: The `fs-gg-game-core` grid-sim recipe (FS-GG/FS.GG.Rendering#112) can reference the shipped primitives by name instead of a hand-rolled pattern (this feature unblocks that issue).
- **SC-005**: The change is fully additive — every existing consumer of FS.GG.UI continues to build and pass unchanged, the public-API stub and product-doc currency gates remain green, and the registry/compatibility projection is updated coherently on release (publish-before-flip).

## Assumptions

- The two helpers live in `FS.GG.UI.Canvas` (the sim-primitive tier established by feature 239) as new public modules; the exact module names (`Pathfinding`, `SpatialGrid`) and precise signatures are refined in planning, following the repo's `.fsi` + `.fs` signature-file convention used by `Rng`/`FixedStep`.
- "Pure / value-shaped" means the spatial grid is an immutable value built once and queried functionally; a specific internal representation (array of buckets, map of cells) is an implementation choice for planning, not a spec requirement — the spec requires only that no shared mutable state is exposed and that results are deterministic.
- Pathfinding operates over an *unbounded* integer cell space defined implicitly by the caller's walkability predicate plus an explicit search bound (so an unreachable goal terminates); the framework does not own a fixed-size map array.
- The diagonal-cost convention (uniform 1 vs √2) and the endpoint-inclusion convention for paths are chosen once in planning and documented; where a floating-point cost would otherwise create a tie-break hazard, planning selects an integer or scaled-integer cost to preserve bit-identical determinism.
- The spatial-grid query contract (exact vs broad-phase candidate set) is chosen once in planning and documented; broad-phase (return cell-bucket candidates, caller does the exact test) is the presumed default unless planning finds exact filtering is cheap and clearer.
- No per-game logic, no new external dependencies, and no change to the viewer/layout/input surfaces are in scope.
- "Reflected in product docs" targets the relevant shipped product skill / `fs-gg-game-core` reference; authoring the separate grid-sim consumer recipe is tracked as FS-GG/FS.GG.Rendering#112 and is out of scope here (this feature only makes it referenceable).
- A Dijkstra flow-field is out of scope and is recorded as a future RTS-profile primitive (per FR-015).
