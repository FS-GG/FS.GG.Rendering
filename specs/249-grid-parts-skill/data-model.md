# Phase 1 Data Model: Grid-Parts Helper Source

The grid-parts helper introduces **no framework package type**. It works on the shared
`FS.GG.UI.Canvas.Cell` (the **face**) and `FS.GG.UI.Scene.Point`/`Rect` (pixels), and exposes a few small
**product-owned** value shapes inside `Grids.fs` that the consumer sees and may edit. Fields and
conventions below define the intended shape (see
[contracts/grids-helper-source.md](./contracts/grids-helper-source.md) for the exact source surface).

## Reused framework types (not redefined)

- **`Cell`** (`FS.GG.UI.Canvas`) — the **face** coordinate: a grid tile. `{ Col; Row }` (int). Reused as
  the face throughout; **not** re-rolled. (Feature 245.)
- **`Point`** (`FS.GG.UI.Scene`) — a pixel position (cell center, vertex point, edge endpoint/midpoint)
  **and** the `GridSpec.Origin`. `{ X; Y }` (float).
- **`Rect`** (`FS.GG.UI.Scene`) — the pixel AABB of a cell (`cellRect`). `{ X; Y; Width; Height }` (float).

*(There is no `Edge`, `Vertex`, or part-to-part conversion anywhere in the framework — that absence is the
whole point of R1. The helper adds exactly those parts, and nothing else.)*

## Product-owned value shapes (in `Grids.fs`)

### EdgeOrientation
Whether an `Edge` is a horizontal boundary (top/bottom of a cell) or a vertical one (left/right).

| Case | Notes |
|------|-------|
| `Horizontal` | A boundary that runs left→right; separates the cell **above** from the cell **below**. |
| `Vertical` | A boundary that runs top→bottom; separates the cell on the **left** from the cell on the **right**. |

### Edge
A new part — the shared boundary between two adjacent faces. **Not** a look-alike of `Cell`/`Point`; it is
the part the shared vocabulary genuinely lacks. Carries exactly **one canonical coordinate** so two
references to the same boundary are equal records.

| Field | Type | Notes |
|-------|------|-------|
| `Col` | `int` | Column of the edge's canonical coordinate. |
| `Row` | `int` | Row of the edge's canonical coordinate. |
| `Orientation` | `EdgeOrientation` | `Vertical { c; r }` = boundary of cells `(c-1, r)` / `(c, r)`, named from the cell on its right; runs vertex `(c, r)` → `(c, r+1)`. `Horizontal { c; r }` = boundary of cells `(c, r-1)` / `(c, r)`, named from the cell below; runs vertex `(c, r)` → `(c+1, r)`. |

### Vertex
A new part — a grid corner where edges meet. `{ Col; Row }` int, in the corner lattice offset half a cell
from the faces. `(c, r)` is the **top-left corner of cell `(c, r)`**. (Structurally `{ Col; Row }` like
`Cell`, but a *distinct part* with distinct adjacency — deliberately its own type, not `Cell`.)

| Field | Type | Notes |
|-------|------|-------|
| `Col` | `int` | Corner column. |
| `Row` | `int` | Corner row. |

### GridSpec (the editable policy)
The pixel-mapping policy — **this is what the consumer tunes.** The one place the grid's placement in
pixel space is set.

| Field | Type | Notes |
|-------|------|-------|
| `CellSize` | `float` | Side length of a cell in pixels. Non-positive / non-finite → a documented fallback of `1.0` (never throws). |
| `Origin` | `Point` | The pixel position of vertex `(0, 0)` (the top-left corner of cell `(0, 0)`). A non-finite `X`/`Y` falls back to `0.0` on that axis. |

## Intended functions (see the contract for signatures)

**Adjacency — pure integer arithmetic, fixed list order:**

- `cellCorners : Cell -> Vertex list` — the cell's four corners, **TL, TR, BR, BL**.
- `cellEdges : Cell -> Edge list` — the cell's four edges, **top, right, bottom, left**.
- `edgeCells : Edge -> Cell list` — the two faces the edge separates (`Vertical`: left-then-right;
  `Horizontal`: above-then-below).
- `edgeVertices : Edge -> Vertex list` — the two endpoint vertices, start-then-end along the edge's
  natural direction.
- `vertexCells : Vertex -> Cell list` — the four faces meeting at the vertex, **TL, TR, BR, BL**.
- `vertexEdges : Vertex -> Edge list` — the four edges meeting at the vertex, **up, right, down, left**.

**Pixel mapping — straight-line float arithmetic, non-finite-guarded, total:**

- `cellRect : GridSpec -> Cell -> Rect` — the cell's pixel AABB.
- `cellCenter : GridSpec -> Cell -> Point` — the cell's pixel center.
- `vertexPoint : GridSpec -> Vertex -> Point` — the vertex's pixel position.
- `edgeSegment : GridSpec -> Edge -> Point * Point` — the edge as its two endpoint pixels (stroke this to
  draw a fence/border).
- `edgeMidpoint : GridSpec -> Edge -> Point` — the edge's pixel midpoint.
- `cellAt : GridSpec -> Point -> Cell` — the floor-based inverse of `cellCenter`/`cellRect`: the cell that
  contains a pixel point (a point exactly on a boundary belongs to the cell to its right/below).

## Total-function conventions (FR-010)

| Degenerate input | Documented result |
|------------------|-------------------|
| Non-finite or non-positive `CellSize` | Falls back to `1.0`; every pixel map still totals; no divide-by-zero in `cellAt`. |
| Non-finite `Origin.X` / `Origin.Y` | That axis falls back to `0.0`; no NaN propagates into any `Point`/`Rect`. |
| Non-finite point coordinate into `cellAt` | That axis maps to `0` (a documented cell), never a NaN cell. |
| Any `Cell`/`Edge`/`Vertex` into an adjacency conversion | Pure integer arithmetic — total for all `int` inputs; no partial cases (negative or large indices are valid parts). |
| `edgeSegment` on an `Edge` | `edgeVertices` always yields exactly two, so the segment is always a real endpoint pair; the unreachable branch is still total (degenerates to a zero-length point pair, never throws). |

## Round-trip invariants (FR-009, FR-010)

- **Adjacency round-trip (FR-009).** For every `Cell c`: each edge in `cellEdges c` reports `c` among its
  `edgeCells`, and each corner in `cellCorners c` reports `c` among its `vertexCells`. The canonical edge
  naming (one name per boundary) is what makes this hold and makes two references to the same edge equal.
- **Pixel round-trip (FR-010).** For every `Cell c` and valid `GridSpec spec`:
  `cellAt spec (cellCenter spec c) = c` — the pixel mapping and its floor-based inverse agree.

## Determinism invariants (FR-008)

- The adjacency conversions are **integer arithmetic** with a **fixed, documented list order** per
  conversion — no floating-point tie-break, no `atan2`/`sqrt`, no `Dictionary`/`HashSet` iteration. There
  is no ordering surface to drift.
- The pixel mapping is **straight-line float arithmetic** (`Origin + coord * CellSize`, and a `floor`
  inverse) with no transcendental and no comparison-based ordering — bit-identical across runs and
  platforms for a given `GridSpec`.
- Identical `(part)` ⇒ byte-identical adjacency lists; identical `(spec, part)` ⇒ byte-identical pixels,
  across runs and platforms. Safe to call from a replayed `update`/`view`.

## Relationships / flow

```text
face:  Cell { Col; Row }              (reused — FS.GG.UI.Canvas)
                    │ cellEdges / cellCorners        (integer, fixed order)
                    ▼
edge:  Edge { Col; Row; Orientation }   vertex:  Vertex { Col; Row }
   │ edgeCells (2 faces)  edgeVertices (2 corners)     │ vertexCells (4 faces) vertexEdges (4 edges)
   ▼                                                    ▼
        ── adjacency round-trip: every edge/corner a cell reports reports that cell back (FR-009) ──

pixels (GridSpec { CellSize; Origin }, reusing Point/Rect):
   Cell   ── cellRect / cellCenter ──▶  Rect / Point
   Vertex ── vertexPoint          ──▶  Point
   Edge   ── edgeSegment / edgeMidpoint ──▶  Point*Point / Point
   Point  ── cellAt (floor inverse) ──▶  Cell           [ cellAt (cellCenter c) = c  (FR-010) ]
        (all non-finite-guarded: CellSize→1.0, origin/coord→0.0 — total, no NaN escapes)
   │  consumer folds parts into its Model / strokes edges as fences / autotiles from vertices / snaps cursor
   ▼
(pure — safe to call from a replayed update/view)
```
