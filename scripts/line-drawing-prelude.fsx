// Feature 248 — consumer-shaped FSI smoke for the import-and-adapt line-drawing helper.
// Loads the raw fragment source (the file a game product receives as src/<ProductDir>/LineDrawing.fs)
// and drives it exactly as a game consumer would. Run after a build:
//   dotnet fsi scripts/line-drawing-prelude.fsx

#I "../src/Canvas/bin/Debug/net10.0"
#r "FS.GG.UI.Scene.dll"
#r "FS.GG.UI.Canvas.dll"
#load "../template/fragments/line-drawing/src/Product/LineDrawing.fs"

open FS.GG.UI.Canvas
open AppRoot

let a: Cell = { Col = 0; Row = 0 }
let b: Cell = { Col = 5; Row = 2 }

// The thin Bresenham cell line: ordered tiles a..b, endpoints included, each step adjacent.
let tiles = LineDrawing.line a b
printfn "line a..b                  = %A" tiles
printfn "endpoints included         = %b" (List.head tiles = a && List.last tiles = b)
printfn "every step adjacent        = %b"
    (tiles |> List.pairwise |> List.forall (fun (p, q) -> abs (p.Col - q.Col) <= 1 && abs (p.Row - q.Row) <= 1))

// The supercover walk: strictly 4-connected (no diagonal gap) — the variant sight walks.
let cover = LineDrawing.supercover a b
printfn "supercover 4-connected     = %b"
    (cover |> List.pairwise |> List.forall (fun (p, q) -> abs (p.Col - q.Col) + abs (p.Row - q.Row) = 1))

// Grid line-of-sight: a wall tile blocks sight; removing it restores it.
let wall: Cell = { Col = 3; Row = 1 }
printfn "target hidden behind wall  = %b" (not (LineDrawing.lineOfSight (fun c -> c <> wall) a b))
printfn "target visible, no wall    = %b" (LineDrawing.lineOfSight (fun _ -> true) a b)

// Determinism: identical endpoints -> byte-identical cell list.
printfn "deterministic              = %b" (LineDrawing.line a b = tiles)
