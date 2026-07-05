# Contract: Grid-Parts Helper Source (`Grids.fs`)

This is **product-owned adaptable source**, not a frozen package `.fsi`. The "contract" is the shape the
consumer receives on scaffold and the invariants the shipped default guarantees — all of which the
consumer may then edit. Types/fields are defined in [../data-model.md](../data-model.md).

## Namespace / module

Materialized into `src/<ProductDir>/Grids.fs` with `sourceName` substitution (`Product` by default):

```fsharp
namespace <ProductName>          // e.g. namespace Product

open FS.GG.UI.Scene              // Point, Rect
open FS.GG.UI.Canvas             // Cell (the face)

/// Product-owned grid-parts helper — THIS FILE IS YOURS TO ADAPT. Faces reuse the shared
/// Canvas.Cell; pixels reuse the shared Scene.Point/Rect; the Edge/Vertex parts, the six adjacency
/// conversions, and the pixel mapping below are the lines to edit (move the origin, add a diagonal-edge
/// variant, reorder corners, extend to hex). Delete this file freely if you don't need it.
/// References: https://www.redblobgames.com/grids/parts/  and  https://www.redblobgames.com/grids/edges/
module Grids =
    ...
```

## Intended surface (what the consumer gets)

> Signatures are the *default* the consumer receives; they are editable, not surface-baselined.

```fsharp
/// Whether an Edge is a horizontal boundary (top/bottom of a cell) or a vertical one (left/right).
type EdgeOrientation =
    | Horizontal
    | Vertical

/// A grid EDGE — the shared boundary between two adjacent faces. Col/Row + Orientation give each edge
/// exactly ONE canonical name (a Vertical edge is named from the cell on its right; a Horizontal edge
/// from the cell below it), so two references to the same boundary are equal.
type Edge = { Col: int; Row: int; Orientation: EdgeOrientation }

/// A grid VERTEX — a corner where edges meet. (Col, Row) is the top-left corner of cell (Col, Row);
/// the corner lattice is offset by half a cell from the faces.
type Vertex = { Col: int; Row: int }

/// The pixel-mapping policy — THIS is what you tune. CellSize is the side length of a cell in pixels;
/// Origin is the pixel position of vertex (0, 0) (the top-left corner of cell (0, 0)).
type GridSpec = { CellSize: float; Origin: Point }

// --- Adjacency (pure integer arithmetic, fixed list order) ---

/// A cell's four corners, in top-left, top-right, bottom-right, bottom-left order.
val cellCorners  : c: Cell -> Vertex list

/// A cell's four edges, in top, right, bottom, left order.
val cellEdges    : c: Cell -> Edge list

/// The two faces an edge separates (Vertical: left-then-right; Horizontal: above-then-below).
val edgeCells    : e: Edge -> Cell list

/// The two vertices at an edge's ends (start then end along the edge's natural direction).
val edgeVertices : e: Edge -> Vertex list

/// The four faces meeting at a vertex, in top-left, top-right, bottom-right, bottom-left order.
val vertexCells  : v: Vertex -> Cell list

/// The four edges meeting at a vertex, in up, right, down, left order.
val vertexEdges  : v: Vertex -> Edge list

// --- Pixel mapping (straight-line float arithmetic, non-finite-guarded, total) ---

/// The pixel AABB of a cell.
val cellRect     : spec: GridSpec -> c: Cell -> Rect

/// The pixel center of a cell.
val cellCenter   : spec: GridSpec -> c: Cell -> Point

/// The pixel position of a vertex.
val vertexPoint  : spec: GridSpec -> v: Vertex -> Point

/// An edge as its two endpoint pixels (stroke this segment to draw a fence/border).
val edgeSegment  : spec: GridSpec -> e: Edge -> Point * Point

/// The pixel midpoint of an edge.
val edgeMidpoint : spec: GridSpec -> e: Edge -> Point

/// The floor-based inverse of cellCenter/cellRect: the cell containing a pixel point (a point exactly
/// on a boundary belongs to the cell to its right/below). Total on non-finite input.
val cellAt       : spec: GridSpec -> p: Point -> Cell
```

## Guaranteed invariants (shipped default)

- **G-1 Reuse, no look-alikes** — faces reuse the shared `Cell`; pixels reuse the shared `Point`/`Rect`;
  the helper introduces no cell/point/vector/bounds record. `Edge`/`Vertex` are the genuinely-new **parts**
  the shared vocabulary lacks (a boundary with an orientation; a corner in the offset lattice), not
  competing coordinate types. (FR-006, SC-005)
- **G-2 One canonical name per edge** — an edge borders two cells but has exactly one `Edge` value:
  `Vertical { c; r }` = cells `(c-1, r)` / `(c, r)`; `Horizontal { c; r }` = cells `(c, r-1)` / `(c, r)`.
  Two references to the same boundary are structurally-equal records. (FR-006, spec Edge Cases)
- **G-3 Adjacency round-trip** — every edge/corner a cell reports reports that cell back: for all `c`,
  `cellEdges c` ⊂ edges whose `edgeCells` contain `c`, and `cellCorners c` ⊂ vertices whose `vertexCells`
  contain `c`. The conversions compose without surprise. (FR-009)
- **G-4 Fixed list order** — `cellCorners`/`vertexCells` TL/TR/BR/BL; `cellEdges` top/right/bottom/left;
  `vertexEdges` up/right/down/left; `edgeCells` left/right (`Vertical`) or above/below (`Horizontal`);
  `edgeVertices` start→end. Documented and stable — no hash iteration, no float ordering. (FR-008)
- **G-5 Deterministic** — the adjacency layer is pure **integer** arithmetic (no float tie-break, no
  `atan2`/`sqrt`, no `Dictionary`/`HashSet`); the pixel mapping is straight-line float arithmetic; identical
  inputs ⇒ byte-identical output across runs and platforms. (FR-008, SC-004)
- **G-6 Pixel round-trip** — `cellAt spec (cellCenter spec c) = c` for every cell and valid `GridSpec`; the
  pixel mapping and its floor-based inverse agree. (FR-010, US1 Acceptance 3)
- **G-7 Total** — degenerate inputs return documented values: non-finite / non-positive `CellSize` → `1.0`;
  non-finite `Origin`/coordinate → `0.0`; `cellAt` on a non-finite axis → `0`. Never throws, never emits a
  NaN coordinate. (FR-010, SC-008)
- **G-8 Edit/delete safe** — the editable policy (origin, cell size, corner order, edge convention, hex
  extension) is isolated to `GridSpec` + the marked adjacency/pixel bodies; the file compiles via an
  `Exists`-guarded gated `Compile` item, so deleting it still builds. (FR-007)

## Verification

Exercised by `tests/Canvas.Tests/GridsHelperTests.fs` (adds the raw default body via `<Compile Include>` —
literal `namespace Product`; Canvas.Tests already refs Canvas + Scene): FsCheck properties for adjacency
round-trip (`edgeCells`/`vertexCells` report the cell back), fixed list lengths (2 for `edgeCells`/
`edgeVertices`, 4 for `vertexCells`/`vertexEdges`/`cellEdges`/`cellCorners`), pixel round-trip
(`cellAt (cellCenter c) = c`), repeat-run byte-identity (determinism), and degenerate totals
(non-finite/≤0 `CellSize`, non-finite point). And end-to-end by [../quickstart.md](../quickstart.md). (The
framework repo has no `tests/Product.Tests/`; that project belongs to the *generated* product and exercises
the helper post-scaffold.)
