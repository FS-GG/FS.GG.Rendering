---
name: fs-gg-grids
description: Work with the parts of a grid in a generated FS.GG.UI product — faces (cells), edges, and vertices, their adjacency conversions and pixel mapping — over an adaptable helper you own, reusing Cell/Point/Rect.
---

# Grid-Parts Capability

## Scope

Use this skill for the **parts of a grid** in a game/sim product: not routing over cells
([[fs-gg-game:fs-gg-game-core]]'s `Pathfinding`) or spatial hashing (`SpatialGrid`), but the geometry *vocabulary*
of the grid itself — its **faces** (cells/tiles), **edges** (the shared boundaries between two faces),
and **vertices** (the corners where edges meet), how to give each part one canonical name, how to
convert between the parts, and how to map each part to and from pixels. It is the workhorse behind
edge-walls (a fence on a cell boundary), autotiling / marching-squares (keyed off corners), region
borders, and cursor/point snapping. The face and pixel vocabulary reuses the framework primitives; the
`Edge`/`Vertex` parts and the adjacency conversions are game-opinionated and ship as **adaptable source
you own** (`src/<ProductDir>/Grids.fs`), not a frozen package. Everything here is pure, total, and
deterministic — safe to call from a replayed `update`/`view`. The parts vocabulary is the classic
square-grid model from the Red Blob Games references (see **Sources**). This skill materializes for the
`game` and `sample-pack` profiles.

## Public Contract

The face and pixel vocabulary you consume is bundled framework surface; the parts layer is your own
product source:

- `docs/api-surface/Game.Core/Pathfinding.fsi` — the shared `Cell` (`{ Col; Row }`), the grid **face**.
  Shipped in `FS.GG.Game.Core` (`game`/`sample-pack`). Reused as-is; **not** re-rolled.
- `docs/api-surface/Scene/Scene.fsi` — the shared `Point`/`Rect` (pixel positions, cell boxes, edge
  endpoints). Shipped in `FS.GG.UI.Scene` (every profile).
- `src/<ProductDir>/Grids.fs` — **product-owned, adaptable** source: the `EdgeOrientation`/`Edge`/
  `Vertex`/`GridSpec` shapes, the six adjacency conversions, and the pixel mapping. Yours to edit or
  delete.

All entry points are **total**: degenerate inputs return a documented value, they never throw or emit a
NaN coordinate.

## The parts model

A square grid is three kinds of part, each with **one canonical coordinate**:

- **Face** — the existing shared `Cell` (`{ Col; Row }`). A tile. Reused, never re-created.
- **Edge** — the shared boundary between two adjacent faces. The one concept the shared vocabulary lacks,
  added as `Edge` (`{ Col; Row; Orientation }`). An edge borders two cells and could be named from
  either — so the helper picks **one canonical name**: a `Vertical` edge `{c,r}` is the boundary of
  cells `(c-1,r)`/`(c,r)`; a `Horizontal` edge `{c,r}` the boundary of `(c,r-1)`/`(c,r)`. Two references
  to the same boundary are therefore equal.
- **Vertex** — a corner where edges meet, added as `Vertex` (`{ Col; Row }`). `(c,r)` is the top-left
  corner of cell `(c,r)`; the corner lattice is offset half a cell from the faces.

`Edge`/`Vertex` are genuinely **new parts**, not look-alike re-rolls of `Cell`/`Point` — reuse `Cell`
for faces and `Point`/`Rect` for pixels (see **Common pitfalls**).

## Adjacency — the parts relationship table

Six pure, integer conversions walk between the parts. Each returns a fixed, documented order and is
**mutually consistent** — every edge or corner a cell reports, reports that cell back:

```fsharp
open FS.GG.Game.Core       // Cell
// Grids lives in your product's own namespace (Grids.fs).

let c : Cell = { Col = 3; Row = 2 }

let corners = Grids.cellCorners c    // 4 Vertices: TL, TR, BR, BL
let edges   = Grids.cellEdges c      // 4 Edges: top, right, bottom, left

// Round-trip: the two cells each of c's edges separates includes c itself.
let touching = Grids.edgeCells edges.[0]      // [ {Col=3;Row=1}; {Col=3;Row=2} ] — c is one
let ends     = Grids.edgeVertices edges.[0]   // the edge's two endpoint Vertices
let around   = Grids.vertexCells corners.[0]  // the 4 faces meeting at that corner — c is one
let spokes   = Grids.vertexEdges corners.[0]  // the 4 edges meeting there: up, right, down, left
```

- `cellCorners c` / `cellEdges c` — a face's four corners / four edges.
- `edgeCells e` / `edgeVertices e` — the two faces an edge separates / its two endpoint vertices.
- `vertexCells v` / `vertexEdges v` — the four faces / four edges meeting at a corner.

## Pixel mapping

A `GridSpec` (`{ CellSize; Origin }`) places the grid in pixel space; the mapping reuses the shared
`Point`/`Rect` — no hand-rolled bounds record. `Origin` is the pixel position of vertex `(0,0)`.

```fsharp
open FS.GG.UI.Scene       // Point, Rect
let spec = { Grids.CellSize = 32.0; Grids.Origin = { X = 0.0; Y = 0.0 } }

let box    = Grids.cellRect spec c        // Rect — draw the tile
let mid    = Grids.cellCenter spec c      // Point — place a sprite / label
let a, b   = Grids.edgeSegment spec edges.[0]  // two Points — stroke a fence on the boundary
let corner = Grids.vertexPoint spec corners.[0]

// Inverse — snap a cursor to the grid:
let hovered = Grids.cellAt spec { X = mouseX; Y = mouseY }
```

`cellAt` is the floor-based inverse of `cellCenter`/`cellRect`: `cellAt spec (cellCenter spec c) = c` for
every cell. A point exactly on a boundary belongs to the cell to its right/below.

## Applications

- **Edge-walls / fences** — put a blocker on an `Edge` (a boundary), not inside a cell; `edgeSegment`
  gives the two `Point`s to stroke, and `edgeCells` the two tiles it separates for movement rules.
- **Autotiling / marching-squares** — key a tile's sprite off its four corners (`cellCorners`) or a
  region's occupancy at each `Vertex` (`vertexCells`).
- **Region borders** — the outline of a filled region is the set of edges with exactly one filled
  neighbour (`edgeCells`); walk them into a boundary loop.
- **Snapping** — `cellAt` snaps a pixel to a cell; `vertexPoint`/`cellCenter` snap back to pixels.

## The adaptable helper

`Grids.fs` is **yours** — a small, readable file classified *replaceable* in the scaffold map (see
[[fs-gg-game:fs-gg-model-swap]]). Move the grid origin, add a diagonal-edge orientation, reorder the corners, extend
the scheme toward hex/triangle grids, or delete the file if you don't need it: its `Compile` item is
`Exists`-guarded, so the build stays green and you never touch the durable `Product.fsproj`.

## Common pitfalls

- **Re-rolling `Cell`/`Point` instead of reusing them.** As in [[fs-gg-scene]]: a bare `{ Col = …; Row =
  … }` binds to whichever record is in scope last — and `Cell` and `Vertex` are structurally identical.
  Annotate at the boundary (`let faceOf (c: Cell) = …`) and reuse the shared `Cell` for faces and
  `Point`/`Rect` for pixels. `Edge`/`Vertex` are the *only* new parts — don't add a look-alike tile/point
  type.
- **Giving an edge two names.** An edge borders two cells; naming it from "whichever cell I'm iterating"
  makes the same boundary compare unequal to itself. Keep the one canonical `Edge` coordinate (already the
  default) so a `Set<Edge>` de-dupes correctly.
- **Confusing edge orientation.** A `Vertical` edge is a *left/right* boundary (it runs vertically); a
  `Horizontal` edge a *top/bottom* one. `cellEdges` returns them top, right, bottom, left — index
  accordingly.
- **Off-by-one between faces and corners.** The corner lattice is offset half a cell: vertex `(c,r)` is
  the *top-left* corner of cell `(c,r)`, so cell `(c,r)`'s bottom-right corner is vertex `(c+1,r+1)`. Use
  the conversions rather than hand-indexing.
- **Non-finite `GridSpec`.** A zero/negative/NaN `CellSize` falls back to `1.0` and a non-finite origin to
  `0.0` (total, never a NaN pixel) — but that is a *safety net*, not a valid grid; validate your `GridSpec`
  upstream.
- **Deleting `Grids.fs` and then editing `Product.fsproj`.** You don't need to — the compile item is
  `Exists`-guarded. Leave `Product.fsproj` alone.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned grid-parts examples (assert the adjacency
round-trips — every edge/corner of a cell reports that cell back; `cellAt (cellCenter c) = c`;
determinism replays; totality on degenerate `GridSpec`).

## Evidence

Record grid-parts evidence (adjacency round-trips, pixel round-trip, determinism replays, totality) under
this product's `readiness/` paths. Do not copy framework readiness reports into the product.

## Package Boundary

`Cell` is in `FS.GG.Game.Core` (referenced only on the `game`/`sample-pack` profiles); `Point`/`Rect` are
in `FS.GG.UI.Scene`. `Grids.fs` is **product-owned source with no backing package**. Keep rendering in
[[fs-gg-scene]] and host wiring in [[fs-gg-skiaviewer]].

## Generated Product

Build your world over `Cell` faces, use `Grids` to address the edges and corners between them each fixed
step, and hand the pixel geometry (`cellRect`/`edgeSegment`/`vertexPoint`) to your `View` as a `Scene`.
Pair it with [[fs-gg-collision]] and [[fs-gg-visibility]] for a full geometry pass, and with
[[fs-gg-game:fs-gg-game-core]]'s `Pathfinding`/`SpatialGrid` for routing and range queries over the same cells.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the Red Blob Games references), then
community sources. If your product uses Spec Kit, record findings and resolving links under the feature's
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** line and any product-local
`docs/`. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-game:fs-gg-game-core]] — the simulation loop (fixed step, RNG, culling) plus `Pathfinding`/`SpatialGrid`,
  which route and bucket over the same `Cell` faces this skill addresses the parts of.
- [[fs-gg-collision]] — the sibling per-frame geometry pass over the shared `Point`/`Rect`/`SpatialGrid`.
- [[fs-gg-visibility]] — the sibling angular-sweep visibility pass over the shared vocabulary.
- [[fs-gg-scene]] — owns the shared `Point`/`Rect` the pixel mapping produces; renders the grid.
- [[fs-gg-skiaviewer]] — drives the fixed-step loop from the host window.
- [[fs-gg-game:fs-gg-model-swap]] — classifies `Grids.fs` as replaceable/adaptable source.

## Sources / links

- Red Blob Games, "Parts of a grid": https://www.redblobgames.com/grids/parts/
- Red Blob Games, "Grid edges": https://www.redblobgames.com/grids/edges/
- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
