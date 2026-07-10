// Feature 246 / #290 — consumer-shaped FSI smoke for the import-and-adapt collision helper.
// Loads the raw fragment source (the file a game product receives as src/<ProductDir>/Collision.fs)
// and drives it exactly as a game consumer would. Run after a build of the Canvas test project
// (which restores the FS.GG.Game.Core closure the re-homed fragment opens):
//   dotnet build tests/Canvas.Tests/Canvas.Tests.fsproj -c Debug
//   dotnet fsi scripts/collision-prelude.fsx

// ADR-0022 P5: the fragment now `open`s FS.GG.Game.Core (Point/Rect + Geometry/SpatialGrid moved out
// of FS.GG.UI.Scene/.Canvas). The Canvas.Tests output carries the full closure, incl. Game.Core.
#I "../tests/Canvas.Tests/bin/Debug/net10.0"
#r "FS.GG.Game.Core.dll"
#load "../template/fragments/collision/src/Product/Collision.fs"

open FS.GG.Game.Core
open AppRoot

// A body at rest (zero per-step displacement) — a wall or a static overlap fixture.
let body x y w h tag : Collision.Body<string> =
    { Bounds = { X = x; Y = y; Width = w; Height = h }
      Velocity = { X = 0.0; Y = 0.0 }
      Tag = tag }

// A body travelling `(vx, vy)` this step — the swept-detection case (#290).
let moving x y w h vx vy tag : Collision.Body<string> =
    { Bounds = { X = x; Y = y; Width = w; Height = h }
      Velocity = { X = vx; Y = vy }
      Tag = tag }

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

// #290: a 1200 u/s round advanced by one 60 Hz step travels 20 units and would tunnel a 6-wide target
// under a point test — its start is in front of the target, its end is behind it. The swept pass still
// catches the crossing, and PushFirst stops the round at the target's near face instead of past it.
let tunnelWorld = [ moving 0.0 0.0 2.0 2.0 20.0 0.0 "round"; body 10.0 -2.0 6.0 6.0 "target" ]
let tunnelHit = Collision.collide 16.0 tunnelWorld
printfn "swept no-tunnel = %b" (tunnelHit |> List.exists (fun c -> c.A.Tag = "round" && c.B.Tag = "target"))

match Collision.step Collision.PushFirst 16.0 tunnelWorld with
| stopped :: _ -> printfn "stops at wall   = %b" (abs (stopped.A.Bounds.X + stopped.A.Bounds.Width - 10.0) < 1e-6)
| [] -> printfn "stops at wall   = false (no contact)"
