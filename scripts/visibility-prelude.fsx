// Feature 247 — consumer-shaped FSI smoke for the import-and-adapt visibility helper.
// Loads the raw fragment source (the file a game product receives as src/<ProductDir>/Visibility.fs)
// and drives it exactly as a game consumer would. Run after a build:
//   dotnet fsi scripts/visibility-prelude.fsx

#I "../src/Canvas/bin/Debug/net10.0"
#r "FS.GG.UI.Scene.dll"
#r "FS.GG.UI.Canvas.dll"
#load "../template/fragments/visibility/src/Product/Visibility.fs"

open FS.GG.UI.Scene
open AppRoot

let source: Point = { X = 0.0; Y = 0.0 }
let target: Point = { X = 10.0; Y = 0.0 }

// A single wall segment sitting directly between the source and the target.
let wall: Visibility.Segment list = [ { A = { X = 5.0; Y = -5.0 }; B = { X = 5.0; Y = 5.0 } } ]

// Line-of-sight: hidden behind the wall, visible once the wall is gone.
printfn "target hidden behind wall = %b" (not (Visibility.isVisible source target wall))
printfn "target visible, no wall   = %b" (Visibility.isVisible source target [])

// The visibility polygon: a bounded, closed ring of hit points around the source.
let poly = Visibility.polygon { Radius = 20.0; CellSize = 8.0 } source wall
printfn "polygon vertex count      = %d" (List.length poly.Vertices)
printfn "every vertex within bound = %b"
    (poly.Vertices |> List.forall (fun v -> abs v.X <= 20.0 + 1e-6 && abs v.Y <= 20.0 + 1e-6))

// Determinism: identical inputs -> byte-identical polygon.
printfn "deterministic             = %b"
    (Visibility.polygon { Radius = 20.0; CellSize = 8.0 } source wall = poly)
