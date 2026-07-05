// Feature 249 — consumer-shaped FSI smoke for the import-and-adapt grid-parts helper.
// Loads the raw fragment source (the file a game product receives as src/<ProductDir>/Grids.fs)
// and drives it exactly as a game consumer would. Run after a build:
//   dotnet fsi scripts/grids-parts-prelude.fsx

#I "../src/Canvas/bin/Debug/net10.0"
#r "FS.GG.UI.Scene.dll"
#r "FS.GG.UI.Canvas.dll"
#load "../template/fragments/grids/src/Product/Grids.fs"

open FS.GG.UI.Scene
open FS.GG.UI.Canvas
open Product

let c: Cell = { Col = 3; Row = 2 }
let spec: Grids.GridSpec = { CellSize = 32.0; Origin = { X = 0.0; Y = 0.0 } }

// The parts of cell (3,2): four corners, four edges.
printfn "cell (3,2) corners = %A" (Grids.cellCorners c)
printfn "cell (3,2) edges   = %A" (Grids.cellEdges c)

// Adjacency round-trip: each edge of the cell reports the cell back among the two it separates.
let roundTrip = Grids.cellEdges c |> List.forall (fun e -> Grids.edgeCells e |> List.contains c)
printfn "edges round-trip to the cell = %b" roundTrip

// Corner round-trip: each corner reports the cell back among the four faces around it.
let cornerTrip = Grids.cellCorners c |> List.forall (fun v -> Grids.vertexCells v |> List.contains c)
printfn "corners round-trip to cell   = %b" cornerTrip

// Pixel mapping + inverse: snap the cell's centre pixel back to the cell.
let centre = Grids.cellCenter spec c
printfn "cell centre pixel  = %A" centre
printfn "cellAt(centre) = c = %b" (Grids.cellAt spec centre = c)

// An edge as a drawable pixel segment (a fence on the cell's top boundary).
let top = Grids.cellEdges c |> List.head
printfn "top-edge segment   = %A" (Grids.edgeSegment spec top)

// Determinism: identical inputs -> identical parts.
printfn "deterministic      = %b" (Grids.cellEdges c = Grids.cellEdges c)

// Totality: a degenerate GridSpec never throws or yields a NaN pixel.
let bad: Grids.GridSpec = { CellSize = 0.0; Origin = { X = nan; Y = 0.0 } }
let r = Grids.cellRect bad c
printfn "degenerate spec safe = %b" (System.Double.IsFinite r.X && r.Width > 0.0)
