module Canvas.Tests.GridsHelperTests

// Feature 249 (US1): the import-and-adapt grid-parts helper source (template/fragments/grids/
// src/Product/Grids.fs) is compiled here (literal `namespace Product`, the default sourceName). Faces
// reuse FS.GG.UI.Canvas.Cell and pixels reuse FS.GG.UI.Scene.Point/Rect; the Edge/Vertex parts, the six
// adjacency conversions, and the pixel mapping are the added layer. All real pure computation — no
// synthetic evidence. Covers FR-008 (determinism), FR-009 (adjacency round-trips), FR-010 (totality),
// and the SC-004 pixel round-trip.

open Expecto
open FsCheck
open FS.GG.UI.Scene
open FS.GG.UI.Canvas
open Product

let private cell col row : Cell = { Col = col; Row = row }
let private spec size ox oy : Grids.GridSpec = { CellSize = size; Origin = { X = ox; Y = oy } }

[<Tests>]
let tests =
    testList
        "Feature 249 Grids helper (US1, FR-008/009/010)"
        [

          // --- shape / counts ------------------------------------------------------------------
          test "a cell has exactly four edges and four corners" {
              let c = cell 3 2
              Expect.equal (Grids.cellEdges c |> List.length) 4 "four edges"
              Expect.equal (Grids.cellCorners c |> List.length) 4 "four corners"
          }

          test "an edge separates exactly two cells and has exactly two vertices" {
              for e in Grids.cellEdges (cell 3 2) do
                  Expect.equal (Grids.edgeCells e |> List.length) 2 "two cells"
                  Expect.equal (Grids.edgeVertices e |> List.length) 2 "two vertices"
          }

          test "a vertex touches exactly four cells and four edges" {
              for v in Grids.cellCorners (cell 3 2) do
                  Expect.equal (Grids.vertexCells v |> List.length) 4 "four cells"
                  Expect.equal (Grids.vertexEdges v |> List.length) 4 "four edges"
          }

          // --- adjacency round-trips (FR-009): the conversions are mutually consistent ----------
          test "each of a cell's edges reports that cell among the two it separates" {
              Check.One(
                  Config.QuickThrowOnFailure.WithMaxTest 500,
                  fun (col: int) (row: int) ->
                      let c = cell col row
                      Grids.cellEdges c |> List.forall (fun e -> Grids.edgeCells e |> List.contains c)
              )
          }

          test "each of a cell's corners reports that cell among the four faces around it" {
              Check.One(
                  Config.QuickThrowOnFailure.WithMaxTest 500,
                  fun (col: int) (row: int) ->
                      let c = cell col row
                      Grids.cellCorners c |> List.forall (fun v -> Grids.vertexCells v |> List.contains c)
              )
          }

          test "each of an edge's vertices reports that edge among the four meeting there" {
              Check.One(
                  Config.QuickThrowOnFailure.WithMaxTest 500,
                  fun (col: int) (row: int) (vertical: bool) ->
                      let e: Grids.Edge =
                          { Col = col
                            Row = row
                            Orientation = (if vertical then Grids.Vertical else Grids.Horizontal) }

                      Grids.edgeVertices e |> List.forall (fun v -> Grids.vertexEdges v |> List.contains e)
              )
          }

          test "canonical edge naming: the two cells an edge separates are distinct and orthogonally adjacent" {
              for e in Grids.cellEdges (cell 3 2) do
                  match Grids.edgeCells e with
                  | [ a; b ] ->
                      Expect.notEqual a b "two distinct cells"
                      Expect.equal (abs (a.Col - b.Col) + abs (a.Row - b.Row)) 1 "the two cells are orthogonally adjacent"
                  | other -> failtestf "expected two cells, got %A" other
          }

          // --- pixel mapping + inverse ----------------------------------------------------------
          test "cellRect places the cell at origin + col*size and is size-square" {
              let r = Grids.cellRect (spec 32.0 10.0 20.0) (cell 3 2)
              Expect.floatClose Accuracy.high r.X (10.0 + 3.0 * 32.0) "x"
              Expect.floatClose Accuracy.high r.Y (20.0 + 2.0 * 32.0) "y"
              Expect.floatClose Accuracy.high r.Width 32.0 "w"
              Expect.floatClose Accuracy.high r.Height 32.0 "h"
          }

          test "cellCenter is the centre of cellRect" {
              let s = spec 32.0 10.0 20.0
              let c = cell 3 2
              let r = Grids.cellRect s c
              let ctr = Grids.cellCenter s c
              Expect.floatClose Accuracy.high ctr.X (r.X + r.Width * 0.5) "cx"
              Expect.floatClose Accuracy.high ctr.Y (r.Y + r.Height * 0.5) "cy"
          }

          test "cellAt is the inverse of cellCenter for every cell (SC-004 pixel round-trip)" {
              let s = spec 32.0 10.0 20.0

              Check.One(
                  Config.QuickThrowOnFailure.WithMaxTest 500,
                  fun (col: int) (row: int) ->
                      let c = cell col row
                      Grids.cellAt s (Grids.cellCenter s c) = c
              )
          }

          test "a cell's top-left corner maps to the cellRect's top-left pixel" {
              let s = spec 32.0 10.0 20.0
              let c = cell 3 2
              let r = Grids.cellRect s c
              let p = Grids.vertexPoint s (Grids.cellCorners c |> List.head)
              Expect.floatClose Accuracy.high p.X r.X "tl x = rect x"
              Expect.floatClose Accuracy.high p.Y r.Y "tl y = rect y"
          }

          test "edgeSegment endpoints equal the pixel positions of the edge's two vertices" {
              let s = spec 32.0 10.0 20.0

              for e in Grids.cellEdges (cell 3 2) do
                  let a, b = Grids.edgeSegment s e

                  match Grids.edgeVertices e with
                  | [ va; vb ] ->
                      let pa, pb = Grids.vertexPoint s va, Grids.vertexPoint s vb
                      Expect.floatClose Accuracy.high a.X pa.X "a.x"
                      Expect.floatClose Accuracy.high a.Y pa.Y "a.y"
                      Expect.floatClose Accuracy.high b.X pb.X "b.x"
                      Expect.floatClose Accuracy.high b.Y pb.Y "b.y"
                  | other -> failtestf "expected two vertices, got %A" other
          }

          // --- determinism (SC-004) -------------------------------------------------------------
          test "the conversions are byte-identical across repeated calls" {
              let c = cell 7 -4
              Expect.equal (Grids.cellEdges c) (Grids.cellEdges c) "edges stable"
              Expect.equal (Grids.cellCorners c) (Grids.cellCorners c) "corners stable"
              let s = spec 32.0 10.0 20.0
              Expect.equal (Grids.cellCenter s c) (Grids.cellCenter s c) "center stable"
          }

          // --- totality on degenerate GridSpec (FR-010, SC-008) ---------------------------------
          test "a non-positive / non-finite cell size falls back and never yields a NaN pixel" {
              let bad = [ spec 0.0 0.0 0.0; spec -5.0 0.0 0.0; spec nan 0.0 0.0; spec infinity 0.0 0.0 ]

              for s in bad do
                  let r = Grids.cellRect s (cell 2 3)

                  Expect.isTrue
                      (System.Double.IsFinite r.X && System.Double.IsFinite r.Y && r.Width > 0.0)
                      "finite rect with a positive fallback size"

                  let ctr = Grids.cellCenter s (cell 2 3)
                  Expect.isTrue (System.Double.IsFinite ctr.X && System.Double.IsFinite ctr.Y) "finite center"
          }

          test "cellAt is total on a non-finite point" {
              let s = spec 32.0 0.0 0.0
              Expect.equal (Grids.cellAt s { X = nan; Y = 100.0 }).Col 0 "nan x -> col 0"
              Expect.equal (Grids.cellAt s { X = 100.0; Y = infinity }).Row 0 "inf y -> row 0"
          } ]
