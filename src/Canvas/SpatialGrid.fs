namespace FS.GG.UI.Canvas

open FS.GG.UI.Scene

// Representation is hidden by the .fsi (opaque SpatialGrid<'T>). `Items` holds every (position, item) in
// insertion order; `Buckets` maps a cell key to the ascending item indices in that cell — the index
// indirection is what preserves insertion order in query results.
type SpatialGrid<'T> =
    { CellSize: float
      Items: (Point * 'T)[]
      Buckets: Map<struct (int * int), int list> }

[<RequireQualifiedAccess>]
module SpatialGrid =

    // A non-positive or non-finite cell size collapses to one bucket, keeping every operation total.
    let private cellSizeValid (cellSize: float) : bool =
        cellSize > 0.0
        && not (System.Double.IsNaN cellSize)
        && not (System.Double.IsInfinity cellSize)

    let private finite (v: float) : bool =
        not (System.Double.IsNaN v) && not (System.Double.IsInfinity v)

    // Cell index of a coordinate. Matches `keyOf` exactly so build/query bucket the same way.
    let private cellIndex (cellSize: float) (v: float) : int = int (floor (v / cellSize))

    let private keyOf (cellSize: float) (p: Point) : struct (int * int) =
        if cellSizeValid cellSize then
            struct (cellIndex cellSize p.X, cellIndex cellSize p.Y)
        else
            struct (0, 0)

    // Candidate item indices (ascending → insertion order) for the axis-aligned box [minX,maxX]×[minY,maxY].
    // Common case (small query relative to the populated grid): enumerate only the box's cells. Fallbacks
    // that keep the function total and bounded: a single bucket for an invalid cell size, and a full O(n)
    // exact scan whenever the box would span more cells than there are buckets — which also absorbs
    // non-finite bounds (a `float` cell estimate of `infinity` trips the same branch, so no billion-cell
    // enumeration and no throw). The exact per-item filter downstream is authoritative either way.
    let private candidateIndices
        (minX: float)
        (minY: float)
        (maxX: float)
        (maxY: float)
        (grid: SpatialGrid<'T>)
        : int list =
        let cs = grid.CellSize

        let idxs =
            if not (cellSizeValid cs) then
                Map.tryFind (struct (0, 0)) grid.Buckets |> Option.defaultValue []
            elif not (finite minX && finite minY && finite maxX && finite maxY) then
                [ 0 .. grid.Items.Length - 1 ]
            else
                // float cell bounds — never overflow (huge ⇒ infinity), so the decision is safe.
                let minCf, maxCf = floor (minX / cs), floor (maxX / cs)
                let minRf, maxRf = floor (minY / cs), floor (maxY / cs)
                let boxCells = (maxCf - minCf + 1.0) * (maxRf - minRf + 1.0)
                // Enumerating cells is only correct while cell indices stay inside int range: past 2^31
                // `int (floor …)` wraps and breaks the monotonicity that guarantees no false negative.
                // Cap well below 2^31; anything larger (or a box wider than the grid) uses the exact
                // O(n) scan, which never consults bucket keys and so is correct at any magnitude.
                let safe = abs minCf < 1.0e9 && abs maxCf < 1.0e9 && abs minRf < 1.0e9 && abs maxRf < 1.0e9

                if not safe || boxCells > float (Map.count grid.Buckets) then
                    [ 0 .. grid.Items.Length - 1 ]
                else
                    let minC, maxC = int minCf, int maxCf
                    let minR, maxR = int minRf, int maxRf

                    [ for c in minC..maxC do
                          for r in minR..maxR do
                              match Map.tryFind (struct (c, r)) grid.Buckets with
                              | Some v -> yield! v
                              | None -> () ]

        idxs |> List.sort |> List.distinct

    let build (cellSize: float) (items: seq<Point * 'T>) : SpatialGrid<'T> =
        let arr = Seq.toArray items

        let buckets =
            arr
            |> Array.mapi (fun i (p, _) -> (keyOf cellSize p, i))
            |> Array.fold
                (fun m (k, i) ->
                    let existing = Map.tryFind k m |> Option.defaultValue []
                    Map.add k (i :: existing) m)
                Map.empty
            // indices were prepended, so reverse each bucket back to ascending (insertion) order
            |> Map.map (fun _ idxs -> List.rev idxs)

        { CellSize = cellSize
          Items = arr
          Buckets = buckets }

    let query (region: Rect) (grid: SpatialGrid<'T>) : 'T list =
        candidateIndices region.X region.Y (region.X + region.Width) (region.Y + region.Height) grid
        |> List.choose (fun i ->
            let (p, item) = grid.Items.[i]
            if Geometry.containsPoint region p then Some item else None)

    let queryRadius (center: Point) (radius: float) (grid: SpatialGrid<'T>) : 'T list =
        let r =
            if not (finite radius) then 0.0 else max 0.0 radius

        let r2 = r * r

        candidateIndices (center.X - r) (center.Y - r) (center.X + r) (center.Y + r) grid
        |> List.choose (fun i ->
            let (p, item) = grid.Items.[i]
            let dx = p.X - center.X
            let dy = p.Y - center.Y
            if dx * dx + dy * dy <= r2 then Some item else None)
