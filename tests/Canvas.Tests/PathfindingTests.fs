module Canvas.Tests.PathfindingTests

// Feature 245 (US1): deterministic grid pathfinding (A* / BFS) over a walkability predicate.
// 4/8-neighbour, integer 10/14 costs, total (f,h,Col,Row) tie-break, no corner-cutting, maxVisited
// bound, endpoint-inclusive. All real pure computation.

open Expecto
open FsCheck
open FS.GG.UI.Canvas

// Walkability over a bounded w×h grid minus a blocked set (the predicate IS the map).
let private gridWalkable (w: int) (h: int) (blocked: Set<int * int>) (c: Cell) =
    c.Col >= 0
    && c.Col < w
    && c.Row >= 0
    && c.Row < h
    && not (blocked.Contains(c.Col, c.Row))

// A returned path must: start at `start`, end at `goal`, be all-walkable, and step only between
// adjacent (≤1 in each axis) cells.
let private validPath (isWalkable: Cell -> bool) (start: Cell) (goal: Cell) (path: Cell list) =
    match path with
    | [] -> false
    | _ ->
        List.head path = start
        && List.last path = goal
        && List.forall isWalkable path
        && path
           |> List.pairwise
           |> List.forall (fun (a, b) ->
               abs (a.Col - b.Col) <= 1 && abs (a.Row - b.Row) <= 1 && (a.Col <> b.Col || a.Row <> b.Row))

[<Tests>]
let tests =
    testList "Feature 245 Pathfinding (US1, FR-001..FR-005)" [

        test "astar FourWay finds a straight corridor path, endpoints included" {
            let walk = gridWalkable 4 1 Set.empty
            let path = Pathfinding.astar FourWay 1000 walk { Col = 0; Row = 0 } { Col = 3; Row = 0 }
            Expect.equal
                path
                (Some [ { Col = 0; Row = 0 }; { Col = 1; Row = 0 }; { Col = 2; Row = 0 }; { Col = 3; Row = 0 } ])
                "start..goal inclusive straight path"
        }

        test "astar EightWay takes the strictly-cheaper diagonal on an open grid" {
            let walk = gridWalkable 4 4 Set.empty
            let path = Pathfinding.astar EightWay 1000 walk { Col = 0; Row = 0 } { Col = 3; Row = 3 }
            Expect.equal
                path
                (Some [ { Col = 0; Row = 0 }; { Col = 1; Row = 1 }; { Col = 2; Row = 2 }; { Col = 3; Row = 3 } ])
                "diagonal (cost 14) beats stair-stepping (cost 20 per corner)"
        }

        test "astar FourWay cannot use diagonals (longer Manhattan path)" {
            let walk = gridWalkable 4 4 Set.empty
            let path = Pathfinding.astar FourWay 1000 walk { Col = 0; Row = 0 } { Col = 3; Row = 3 }
            Expect.equal (path |> Option.map List.length) (Some 7) "6 orthogonal moves ⇒ 7 cells"
            Expect.isTrue (path |> Option.get |> validPath walk { Col = 0; Row = 0 } { Col = 3; Row = 3 }) "valid"
        }

        test "astar EightWay refuses to cut the corner past a blocked orthogonal (D5)" {
            // (1,0) blocked ⇒ the (0,0)->(1,1) diagonal is refused; must go via (0,1).
            let walk = gridWalkable 3 3 (Set.ofList [ (1, 0) ])
            let path = Pathfinding.astar EightWay 1000 walk { Col = 0; Row = 0 } { Col = 1; Row = 1 }
            Expect.equal
                path
                (Some [ { Col = 0; Row = 0 }; { Col = 0; Row = 1 }; { Col = 1; Row = 1 } ])
                "no corner-cut ⇒ routes around, length 3 not the direct diagonal length 2"
        }

        test "astar routes around a wall with a single gap" {
            // 5×5, wall at Col=2 rows 0..3, gap at (2,4).
            let blocked = Set.ofList [ for r in 0..3 -> (2, r) ]
            let walk = gridWalkable 5 5 blocked
            let start = { Col = 0; Row = 0 }
            let goal = { Col = 4; Row = 0 }
            let path = Pathfinding.astar EightWay 5000 walk start goal
            Expect.isSome path "a path through the gap exists"
            Expect.isTrue (validPath walk start goal (Option.get path)) "valid walkable adjacent path"
            Expect.isFalse (Option.get path |> List.exists (fun c -> blocked.Contains(c.Col, c.Row))) "never enters the wall"
        }

        test "start = goal (walkable) yields the single-cell path" {
            let walk = gridWalkable 3 3 Set.empty
            Expect.equal (Pathfinding.astar FourWay 100 walk { Col = 1; Row = 1 } { Col = 1; Row = 1 }) (Some [ { Col = 1; Row = 1 } ]) "[start]"
            Expect.equal (Pathfinding.bfs EightWay 100 walk { Col = 1; Row = 1 } { Col = 1; Row = 1 }) (Some [ { Col = 1; Row = 1 } ]) "[start]"
        }

        test "a blocked start or goal yields None" {
            let walk = gridWalkable 3 3 (Set.ofList [ (0, 0); (2, 2) ])
            Expect.isNone (Pathfinding.astar FourWay 100 walk { Col = 0; Row = 0 } { Col = 1; Row = 1 }) "blocked start"
            Expect.isNone (Pathfinding.astar FourWay 100 walk { Col = 1; Row = 1 } { Col = 2; Row = 2 }) "blocked goal"
        }

        test "a walled-off goal yields None (bounded search terminates)" {
            // goal (4,5) fully enclosed by a ring of blocked cells (8-connected).
            let ring = Set.ofList [ (3, 4); (4, 4); (5, 4); (3, 5); (5, 5); (3, 6); (4, 6); (5, 6) ]
            let walk = gridWalkable 8 8 ring
            let path = Pathfinding.astar EightWay 5000 walk { Col = 0; Row = 0 } { Col = 4; Row = 5 }
            Expect.isNone path "goal enclosed by a wall ring ⇒ None"
        }

        test "maxVisited <= 0 yields None" {
            let walk = gridWalkable 4 4 Set.empty
            Expect.isNone (Pathfinding.astar FourWay 0 walk { Col = 0; Row = 0 } { Col = 3; Row = 3 }) "zero budget"
            Expect.isNone (Pathfinding.bfs FourWay -1 walk { Col = 0; Row = 0 } { Col = 3; Row = 3 }) "negative budget"
        }

        test "an unreachable-within-budget goal terminates at the cap with None" {
            // large open grid, tiny budget → cannot reach a far goal.
            let walk = gridWalkable 100 100 Set.empty
            Expect.isNone (Pathfinding.astar FourWay 3 walk { Col = 0; Row = 0 } { Col = 99; Row = 99 }) "budget exhausted ⇒ None"
        }

        test "bfs returns a hop-minimal path" {
            let walk = gridWalkable 5 1 Set.empty
            let path = Pathfinding.bfs FourWay 1000 walk { Col = 0; Row = 0 } { Col = 4; Row = 0 }
            Expect.equal (path |> Option.map List.length) (Some 5) "4 hops ⇒ 5 cells"
            Expect.isTrue (path |> Option.get |> validPath walk { Col = 0; Row = 0 } { Col = 4; Row = 0 }) "valid"
        }

        test "two equal-cost routes still yield one stable path (repeat-run byte-identity)" {
            // FourWay (0,0)->(1,1): right-then-up and up-then-right both cost 20.
            let walk = gridWalkable 2 2 Set.empty
            let a = Pathfinding.astar FourWay 100 walk { Col = 0; Row = 0 } { Col = 1; Row = 1 }
            let b = Pathfinding.astar FourWay 100 walk { Col = 0; Row = 0 } { Col = 1; Row = 1 }
            Expect.equal a b "identical inputs ⇒ byte-identical path despite the tie"
            Expect.isTrue (a |> Option.get |> validPath walk { Col = 0; Row = 0 } { Col = 1; Row = 1 }) "and it is a valid path"
        }

        testCase "determinism: repeat calls are byte-identical over random grids (FsCheck)" <| fun () ->
            let prop (blockedRaw: (int * int) list) (sc: int) (sr: int) (gc: int) (gr: int) =
                let blocked = blockedRaw |> List.map (fun (c, r) -> (((abs c) % 8), ((abs r) % 8))) |> Set.ofList
                let walk = gridWalkable 8 8 blocked
                let start = { Col = (abs sc) % 8; Row = (abs sr) % 8 }
                let goal = { Col = (abs gc) % 8; Row = (abs gr) % 8 }
                let p1 = Pathfinding.astar EightWay 2000 walk start goal
                let p2 = Pathfinding.astar EightWay 2000 walk start goal
                // deterministic, and any Some result is a genuinely valid path
                p1 = p2 && (match p1 with Some path -> validPath walk start goal path | None -> true)
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 500, prop)

        testCase "bfs determinism over random grids (FsCheck)" <| fun () ->
            let prop (blockedRaw: (int * int) list) (sc: int) (gc: int) =
                let blocked = blockedRaw |> List.map (fun (c, r) -> (((abs c) % 6), ((abs r) % 6))) |> Set.ofList
                let walk = gridWalkable 6 6 blocked
                let start = { Col = (abs sc) % 6; Row = 0 }
                let goal = { Col = (abs gc) % 6; Row = 5 }
                Pathfinding.bfs FourWay 2000 walk start goal = Pathfinding.bfs FourWay 2000 walk start goal
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 500, prop)
    ]
