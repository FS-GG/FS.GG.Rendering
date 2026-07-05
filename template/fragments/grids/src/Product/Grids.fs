namespace Product

open FS.GG.UI.Scene
open FS.GG.UI.Canvas

/// Product-owned grid-parts helper — THIS FILE IS YOURS TO ADAPT.
///
/// A square grid is made of three kinds of PARTS (Red Blob Games, "Parts of a grid"):
///   * FACES   — the cells/tiles. Reuses the shared `FS.GG.UI.Canvas.Cell` (`{ Col; Row }`), NOT a
///               look-alike type.
///   * EDGES   — the shared boundary between two adjacent faces. The one part the shared vocabulary
///               lacks, so it is added here as `Edge` — an orientation plus a `Col`/`Row` giving each
///               edge exactly ONE canonical name (Red Blob Games, "Grid edges").
///   * VERTICES — the corners where edges meet. Also new, added here as `Vertex`.
/// Pixels reuse the shared `FS.GG.UI.Scene.Point`/`Rect` (`GridSpec` maps parts to/from pixels).
///
/// The part-addressing (adjacency conversions) is the game-opinionated part the framework deliberately
/// does not freeze into a package — change the origin, add a diagonal-edge variant, reorder the corners,
/// switch to hex, or delete this whole file (the build stays green: its compile item is `Exists`-guarded).
///
/// Everything here is pure, total, and deterministic: the adjacency conversions are integer arithmetic
/// (no floating-point tie-break, no hash iteration), and the pixel mapping is straight-line float
/// arithmetic guarded against non-finite input — so identical inputs yield byte-identical output across
/// runs and platforms, safe to call from a replayed `update`/`view`.
///
/// Canonical coordinates (the whole scheme in four lines):
///   * `Vertex   { Col; Row }`               — the top-left corner of cell `(Col, Row)`.
///   * `Edge Vertical   { Col; Row }`        — boundary of cells `(Col-1, Row)` / `(Col, Row)`;
///                                             runs from vertex `(Col, Row)` down to `(Col, Row+1)`.
///   * `Edge Horizontal { Col; Row }`        — boundary of cells `(Col, Row-1)` / `(Col, Row)`;
///                                             runs from vertex `(Col, Row)` right to `(Col+1, Row)`.
///
/// References: https://www.redblobgames.com/grids/parts/  and  https://www.redblobgames.com/grids/edges/
module Grids =

    /// Whether an `Edge` is a horizontal boundary (top/bottom of a cell) or a vertical one (left/right).
    type EdgeOrientation =
        | Horizontal
        | Vertical

    /// A grid EDGE — the shared boundary between two adjacent faces. `Col`/`Row` plus `Orientation` give
    /// each edge exactly ONE canonical name (a `Vertical` edge is named from the cell on its right; a
    /// `Horizontal` edge from the cell below it), so two references to the same boundary are equal.
    type Edge = { Col: int; Row: int; Orientation: EdgeOrientation }

    /// A grid VERTEX — a corner where edges meet. `(Col, Row)` is the top-left corner of cell `(Col, Row)`;
    /// the corner lattice is offset by half a cell from the faces.
    type Vertex = { Col: int; Row: int }

    /// The pixel-mapping policy — THIS is what you tune. `CellSize` is the side length of a cell in
    /// pixels; `Origin` is the pixel position of vertex `(0, 0)` (the top-left corner of cell `(0, 0)`).
    type GridSpec = { CellSize: float; Origin: Point }

    // ---------------------------------------------------------------------------------------------
    // Adjacency — the "parts" relationship table. Pure integer arithmetic; every list is in a fixed,
    // documented order so results are deterministic. The conversions are mutually consistent: every
    // edge/corner a cell reports, reports that cell back (see `edgeCells`/`vertexCells`).
    // ---------------------------------------------------------------------------------------------

    /// A cell's four corners, in top-left, top-right, bottom-right, bottom-left order.
    let cellCorners (c: Cell) : Vertex list =
        [ { Col = c.Col; Row = c.Row } // TL
          { Col = c.Col + 1; Row = c.Row } // TR
          { Col = c.Col + 1; Row = c.Row + 1 } // BR
          { Col = c.Col; Row = c.Row + 1 } ] // BL

    /// A cell's four edges, in top, right, bottom, left order.
    let cellEdges (c: Cell) : Edge list =
        [ { Col = c.Col; Row = c.Row; Orientation = Horizontal } // top
          { Col = c.Col + 1; Row = c.Row; Orientation = Vertical } // right
          { Col = c.Col; Row = c.Row + 1; Orientation = Horizontal } // bottom
          { Col = c.Col; Row = c.Row; Orientation = Vertical } ] // left

    /// The two faces an edge separates, in ascending order (left-then-right for a `Vertical` edge,
    /// above-then-below for a `Horizontal` one).
    let edgeCells (e: Edge) : Cell list =
        match e.Orientation with
        | Vertical -> [ { Col = e.Col - 1; Row = e.Row }; { Col = e.Col; Row = e.Row } ]
        | Horizontal -> [ { Col = e.Col; Row = e.Row - 1 }; { Col = e.Col; Row = e.Row } ]

    /// The two vertices at an edge's ends (start then end along the edge's natural direction).
    let edgeVertices (e: Edge) : Vertex list =
        match e.Orientation with
        | Vertical -> [ { Col = e.Col; Row = e.Row }; { Col = e.Col; Row = e.Row + 1 } ]
        | Horizontal -> [ { Col = e.Col; Row = e.Row }; { Col = e.Col + 1; Row = e.Row } ]

    /// The four faces meeting at a vertex, in top-left, top-right, bottom-right, bottom-left order.
    let vertexCells (v: Vertex) : Cell list =
        [ { Col = v.Col - 1; Row = v.Row - 1 } // TL
          { Col = v.Col; Row = v.Row - 1 } // TR
          { Col = v.Col; Row = v.Row } // BR
          { Col = v.Col - 1; Row = v.Row } ] // BL

    /// The four edges meeting at a vertex, in up, right, down, left order.
    let vertexEdges (v: Vertex) : Edge list =
        [ { Col = v.Col; Row = v.Row - 1; Orientation = Vertical } // up
          { Col = v.Col; Row = v.Row; Orientation = Horizontal } // right
          { Col = v.Col; Row = v.Row; Orientation = Vertical } // down
          { Col = v.Col - 1; Row = v.Row; Orientation = Horizontal } ] // left

    // ---------------------------------------------------------------------------------------------
    // Pixel mapping — reuse the shared `Point`/`Rect`. Total: a non-finite / non-positive `CellSize`
    // falls back to 1.0, a non-finite origin/coordinate falls back to 0.0, so no NaN ever escapes.
    // ---------------------------------------------------------------------------------------------

    let private safeCellSize (spec: GridSpec) =
        if System.Double.IsFinite spec.CellSize && spec.CellSize > 0.0 then
            spec.CellSize
        else
            1.0

    let private safeOriginX (spec: GridSpec) =
        if System.Double.IsFinite spec.Origin.X then spec.Origin.X else 0.0

    let private safeOriginY (spec: GridSpec) =
        if System.Double.IsFinite spec.Origin.Y then spec.Origin.Y else 0.0

    /// The pixel AABB of a cell.
    let cellRect (spec: GridSpec) (c: Cell) : Rect =
        let s = safeCellSize spec

        { X = safeOriginX spec + float c.Col * s
          Y = safeOriginY spec + float c.Row * s
          Width = s
          Height = s }

    /// The pixel center of a cell.
    let cellCenter (spec: GridSpec) (c: Cell) : Point =
        let s = safeCellSize spec

        { X = safeOriginX spec + (float c.Col + 0.5) * s
          Y = safeOriginY spec + (float c.Row + 0.5) * s }

    /// The pixel position of a vertex.
    let vertexPoint (spec: GridSpec) (v: Vertex) : Point =
        let s = safeCellSize spec

        { X = safeOriginX spec + float v.Col * s
          Y = safeOriginY spec + float v.Row * s }

    /// An edge as its two endpoint pixels (draw a fence/border by stroking this segment).
    let edgeSegment (spec: GridSpec) (e: Edge) : Point * Point =
        match edgeVertices e with
        | [ a; b ] -> vertexPoint spec a, vertexPoint spec b
        | _ ->
            // Unreachable (edgeVertices always yields two), but keep the mapping total.
            let p = vertexPoint spec { Col = e.Col; Row = e.Row }
            p, p

    /// The pixel midpoint of an edge.
    let edgeMidpoint (spec: GridSpec) (e: Edge) : Point =
        let a, b = edgeSegment spec e
        { X = (a.X + b.X) * 0.5; Y = (a.Y + b.Y) * 0.5 }

    /// Inverse of `cellCenter`/`cellRect`: the cell that contains a pixel point (floor-based, so a point
    /// exactly on a boundary belongs to the cell to its right/below). Total on non-finite input — a
    /// non-finite coordinate maps to `0` on that axis rather than emitting a NaN cell.
    let cellAt (spec: GridSpec) (p: Point) : Cell =
        let s = safeCellSize spec

        let col =
            if System.Double.IsFinite p.X then
                int (floor ((p.X - safeOriginX spec) / s))
            else
                0

        let row =
            if System.Double.IsFinite p.Y then
                int (floor ((p.Y - safeOriginY spec) / s))
            else
                0

        { Col = col; Row = row }
