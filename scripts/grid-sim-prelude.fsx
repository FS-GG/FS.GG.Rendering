// Feature 245 — consumer-shaped FSI smoke for the grid sim primitives.
// Drives Pathfinding + SpatialGrid through the packed public surface exactly as a grid-game consumer
// would. Run after a build: `dotnet fsi scripts/grid-sim-prelude.fsx`.

#I "../src/Canvas/bin/Debug/net10.0"
#r "FS.GG.UI.Scene.dll"
#r "FS.GG.UI.Canvas.dll"

open FS.GG.UI.Canvas
open FS.GG.UI.Scene

// A 5x5 grid with a wall at Col=2 for Rows 0..3, leaving a gap at (2,4).
let blocked = set [ for r in 0..3 -> (2, r) ]

let walkable (c: Cell) =
    c.Col >= 0
    && c.Col <= 4
    && c.Row >= 0
    && c.Row <= 4
    && not (blocked.Contains(c.Col, c.Row))

let start = { Col = 0; Row = 0 }
let goal = { Col = 4; Row = 0 }

let path = Pathfinding.astar EightWay 1000 walkable start goal
printfn "astar path      = %A" path
printfn "deterministic   = %b" (path = Pathfinding.astar EightWay 1000 walkable start goal)
printfn "never in wall   = %b" (path |> Option.map (List.forall (fun c -> not (blocked.Contains(c.Col, c.Row)))) |> Option.defaultValue false)
printfn "bfs path len    = %A" (Pathfinding.bfs FourWay 1000 walkable start goal |> Option.map List.length)

// Splash query: bucket some enemies, ask who is within radius 3 of a blast at (10,10).
let enemies = [ { X = 10.0; Y = 11.0 }, "a"; { X = 20.0; Y = 20.0 }, "b"; { X = 12.0; Y = 9.0 }, "c" ]
let grid = SpatialGrid.build 4.0 enemies
printfn "splash r=3      = %A" (SpatialGrid.queryRadius { X = 10.0; Y = 10.0 } 3.0 grid) // ["a"; "c"]
printfn "rect 15x15      = %A" (SpatialGrid.query { X = 0.0; Y = 0.0; Width = 15.0; Height = 15.0 } grid) // ["a"; "c"]
