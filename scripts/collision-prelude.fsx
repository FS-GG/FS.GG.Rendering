// Feature 246 — consumer-shaped FSI smoke for the import-and-adapt collision helper.
// Loads the raw fragment source (the file a game product receives as src/<ProductDir>/Collision.fs)
// and drives it exactly as a game consumer would. Run after a build:
//   dotnet fsi scripts/collision-prelude.fsx

#I "../src/Canvas/bin/Debug/net10.0"
#r "FS.GG.UI.Scene.dll"
#r "FS.GG.UI.Canvas.dll"
#load "../template/fragments/collision/src/Product/Collision.fs"

open FS.GG.UI.Scene
open Product

let body x y w h tag : Collision.Body<string> =
    { Bounds = { X = x; Y = y; Width = w; Height = h }; Tag = tag }

// Two overlapping bodies + a far-away one. Broad-phase should only pair the overlapping two.
let world =
    [ body 0.0 0.0 10.0 10.0 "player"
      body 6.0 0.0 10.0 10.0 "wall"
      body 100.0 100.0 8.0 8.0 "faraway" ]

let contacts = Collision.collide 16.0 world
printfn "contacts        = %A" (contacts |> List.map (fun c -> c.A.Tag, c.B.Tag, c.Depth))

let resolutions = Collision.step Collision.SeparateEqually 16.0 world
printfn "separated pair  = %A" (resolutions |> List.map (fun r -> r.A.Tag, r.A.Bounds.X, r.B.Tag, r.B.Bounds.X))

// Determinism: identical inputs -> identical output.
printfn "deterministic   = %b" (Collision.collide 16.0 world = Collision.collide 16.0 world)

// After separation the pair no longer intersects (touching is not overlap).
let sep = resolutions.Head
printfn "overlap removed = %b" (not (Geometry.intersects sep.A.Bounds sep.B.Bounds))

// A wall response: PushSecond keeps the first body fixed.
let wallResp = Collision.resolve Collision.PushSecond contacts.Head
printfn "wall fixed      = %b" (wallResp.A.Bounds = (List.head world).Bounds)
