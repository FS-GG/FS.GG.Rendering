namespace FS.GG.UI.Canvas

[<Struct>]
type Cell = { Col: int; Row: int }

type Neighbourhood =
    | FourWay
    | EightWay

[<RequireQualifiedAccess>]
module Pathfinding =

    // Fixed step offsets. The enumeration order does not affect A* output (the total (f,h,Col,Row)
    // frontier order decides), and for BFS it is a deterministic, documented order.
    let private orthoOffsets = [ (0, -1); (0, 1); (-1, 0); (1, 0) ]
    let private diagOffsets = [ (-1, -1); (1, -1); (-1, 1); (1, 1) ]

    // Walkable neighbours of `c` with their integer step cost. Orthogonal = 10, diagonal = 14.
    // Under EightWay a diagonal is refused unless BOTH shared orthogonal neighbours are walkable
    // (no corner-cutting).
    let private neighbours (nb: Neighbourhood) (isWalkable: Cell -> bool) (c: Cell) : struct (Cell * int) list =
        let ortho =
            orthoOffsets
            |> List.choose (fun (dx, dy) ->
                let n = { Col = c.Col + dx; Row = c.Row + dy }
                if isWalkable n then Some(struct (n, 10)) else None)

        match nb with
        | FourWay -> ortho
        | EightWay ->
            let diag =
                diagOffsets
                |> List.choose (fun (dx, dy) ->
                    let n = { Col = c.Col + dx; Row = c.Row + dy }
                    let side1 = { Col = c.Col + dx; Row = c.Row }
                    let side2 = { Col = c.Col; Row = c.Row + dy }

                    if isWalkable n && isWalkable side1 && isWalkable side2 then
                        Some(struct (n, 14))
                    else
                        None)

            ortho @ diag

    // Admissible integer heuristic toward `goal`: Manhattan*10 (4-way) / octile (8-way with 10/14).
    let private heuristic (nb: Neighbourhood) (goal: Cell) (c: Cell) : int =
        let dx = abs (c.Col - goal.Col)
        let dy = abs (c.Row - goal.Row)

        match nb with
        | FourWay -> 10 * (dx + dy)
        | EightWay ->
            let lo = min dx dy
            let hi = max dx dy
            14 * lo + 10 * (hi - lo)

    // Walk the cameFrom chain from `goal` back to the start, yielding start..goal inclusive.
    let private reconstruct (cameFrom: Map<Cell, Cell>) (goal: Cell) : Cell list =
        let rec walk acc c =
            match Map.tryFind c cameFrom with
            | Some prev -> walk (c :: acc) prev
            | None -> c :: acc

        walk [] goal

    // Shared guards for the trivial/degenerate cases before a real search runs.
    let private trivial (maxVisited: int) (isWalkable: Cell -> bool) (start: Cell) (goal: Cell) : Cell list option option =
        if maxVisited <= 0 || not (isWalkable start) || not (isWalkable goal) then Some None
        elif start = goal then Some(Some [ start ])
        else None

    let astar
        (neighbourhood: Neighbourhood)
        (maxVisited: int)
        (isWalkable: Cell -> bool)
        (start: Cell)
        (goal: Cell)
        : Cell list option =
        match trivial maxVisited isWalkable start goal with
        | Some result -> result
        | None ->
            let h = heuristic neighbourhood goal
            // Frontier is a Set keyed by the TOTAL integer order (f, h, Col, Row) — Set.minElement is
            // deterministic, so the pop order (hence the path) is bit-identical across runs/platforms.
            let h0 = h start
            let openSet = Set.singleton (h0, h0, start.Col, start.Row)
            let gScore = Map.ofList [ start, 0 ]

            let rec loop
                (openSet: Set<int * int * int * int>)
                (gScore: Map<Cell, int>)
                (cameFrom: Map<Cell, Cell>)
                (expansions: int)
                : Cell list option =
                if Set.isEmpty openSet || expansions >= maxVisited then
                    None
                else
                    let (f, hCur, col, row) = Set.minElement openSet
                    let current = { Col = col; Row = row }
                    let openSet = Set.remove (f, hCur, col, row) openSet

                    if current = goal then
                        Some(reconstruct cameFrom current)
                    else
                        let g = gScore.[current]

                        let (openSet, gScore, cameFrom) =
                            neighbours neighbourhood isWalkable current
                            |> List.fold
                                (fun (os, gs, cf) (struct (n, cost)) ->
                                    let tentative = g + cost

                                    match Map.tryFind n gs with
                                    | Some existing when existing <= tentative -> (os, gs, cf)
                                    | prior ->
                                        let hn = h n
                                        // Drop any stale open entry for n (same h, old g) before re-adding.
                                        let os =
                                            match prior with
                                            | Some oldG -> Set.remove (oldG + hn, hn, n.Col, n.Row) os
                                            | None -> os

                                        let os = Set.add (tentative + hn, hn, n.Col, n.Row) os
                                        (os, Map.add n tentative gs, Map.add n current cf))
                                (openSet, gScore, cameFrom)

                        loop openSet gScore cameFrom (expansions + 1)

            loop openSet gScore Map.empty 0

    let bfs
        (neighbourhood: Neighbourhood)
        (maxVisited: int)
        (isWalkable: Cell -> bool)
        (start: Cell)
        (goal: Cell)
        : Cell list option =
        match trivial maxVisited isWalkable start goal with
        | Some result -> result
        | None ->
            // Amortized-O(1) immutable FIFO queue (front list + reversed back list). Deterministic:
            // enqueue order follows the fixed neighbour offsets, dequeue is FIFO — no hashing on order.
            let rec loop
                (front: Cell list)
                (back: Cell list)
                (cameFrom: Map<Cell, Cell>)
                (visited: Set<Cell>)
                (expansions: int)
                : Cell list option =
                match front with
                | [] ->
                    match List.rev back with
                    | [] -> None
                    | front -> loop front [] cameFrom visited expansions
                | current :: rest ->
                    if expansions >= maxVisited then
                        None
                    elif current = goal then
                        Some(reconstruct cameFrom goal)
                    else
                        let (back, cameFrom, visited) =
                            neighbours neighbourhood isWalkable current
                            |> List.fold
                                (fun (bk, cf, vis) (struct (n, _cost)) ->
                                    if Set.contains n vis then
                                        (bk, cf, vis)
                                    else
                                        (n :: bk, Map.add n current cf, Set.add n vis))
                                (back, cameFrom, visited)

                        loop rest back cameFrom visited (expansions + 1)

            loop [ start ] [] Map.empty (Set.singleton start) 0
