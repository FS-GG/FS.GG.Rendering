module Canvas.Tests.SpatialGridTests

// Feature 245 (US2): uniform spatial grid for range/splash queries. Exact results (no false
// negatives/positives), deterministic insertion order, total on degenerate cellSize/radius.
// Reuses Scene.Point/Rect + Geometry.containsPoint. All real pure computation.

open Expecto
open FsCheck
open FS.GG.UI.Canvas
open FS.GG.UI.Scene

let private pt x y : Point = { X = x; Y = y }

// Keep generated coordinates/cell size in a realistic range: astronomically large coords are a
// documented out-of-range case exercised by a dedicated example, not the acceleration contract.
let private clamp (v: float) =
    if System.Double.IsNaN v || System.Double.IsInfinity v then 0.0 else max -1.0e6 (min 1.0e6 v)

let private goodCell (cs: float) =
    if System.Double.IsNaN cs || System.Double.IsInfinity cs || cs <= 0.0 then 1.0 else min 1.0e4 (abs cs + 0.5)

// Brute-force oracles the grid must match exactly.
let private bruteRect (region: Rect) (items: (Point * 'T) list) =
    items |> List.filter (fun (p, _) -> Geometry.containsPoint region p) |> List.map snd

let private bruteRadius (center: Point) (r: float) (items: (Point * 'T) list) =
    let r = if System.Double.IsNaN r || System.Double.IsInfinity r then 0.0 else max 0.0 r
    items
    |> List.filter (fun (p, _) ->
        let dx = p.X - center.X
        let dy = p.Y - center.Y
        dx * dx + dy * dy <= r * r)
    |> List.map snd

[<Tests>]
let tests =
    testList "Feature 245 SpatialGrid (US2, FR-006..FR-009)" [

        test "rectangle query returns exactly the contained items, in insertion order" {
            let items = [ pt 1.0 1.0, "a"; pt 50.0 50.0, "b"; pt 2.0 3.0, "c"; pt 4.0 4.0, "d" ]
            let grid = SpatialGrid.build 8.0 items
            let got = SpatialGrid.query { X = 0.0; Y = 0.0; Width = 5.0; Height = 5.0 } grid
            Expect.equal got [ "a"; "c"; "d" ] "exact contents in insertion order (b is far away)"
        }

        test "radius query returns items within distance, squared-distance boundary inclusive" {
            let items = [ pt 10.0 11.0, "a"; pt 20.0 20.0, "b"; pt 12.0 9.0, "c"; pt 13.0 10.0, "d" ]
            let grid = SpatialGrid.build 4.0 items
            let got = SpatialGrid.queryRadius (pt 10.0 10.0) 3.0 grid
            // a: dist 1, c: dist √8≈2.83, d: dist 3 (== radius, inclusive). b far.
            Expect.equal got [ "a"; "c"; "d" ] "within radius incl. the exact-boundary item, insertion order"
        }

        test "empty grid returns empty results" {
            let grid = SpatialGrid.build 4.0 ([]: (Point * string) list)
            Expect.equal (SpatialGrid.query { X = 0.0; Y = 0.0; Width = 100.0; Height = 100.0 } grid) [] "rect"
            Expect.equal (SpatialGrid.queryRadius (pt 0.0 0.0) 10.0 grid) [] "radius"
        }

        test "non-positive cellSize falls back to a single bucket but stays exact" {
            let items = [ pt 1.0 1.0, "a"; pt 90.0 90.0, "b"; pt 2.0 2.0, "c" ]
            for cs in [ 0.0; -4.0 ] do
                let grid = SpatialGrid.build cs items
                Expect.equal
                    (SpatialGrid.query { X = 0.0; Y = 0.0; Width = 5.0; Height = 5.0 } grid)
                    [ "a"; "c" ]
                    (sprintf "cellSize %f still returns exact contents" cs)
        }

        test "radius <= 0 returns only center-coincident items" {
            let items = [ pt 5.0 5.0, "a"; pt 5.0 5.0, "b"; pt 6.0 5.0, "c" ]
            let grid = SpatialGrid.build 2.0 items
            Expect.equal (SpatialGrid.queryRadius (pt 5.0 5.0) 0.0 grid) [ "a"; "b" ] "radius 0 ⇒ coincident only"
            Expect.equal (SpatialGrid.queryRadius (pt 5.0 5.0) -3.0 grid) [ "a"; "b" ] "negative radius ⇒ radius 0"
        }

        test "zero-area rect returns items exactly on that point" {
            let items = [ pt 3.0 3.0, "a"; pt 3.0 4.0, "b" ]
            let grid = SpatialGrid.build 2.0 items
            Expect.equal (SpatialGrid.query { X = 3.0; Y = 3.0; Width = 0.0; Height = 0.0 } grid) [ "a" ] "point-rect hits (3,3)"
        }

        test "repeat builds and queries are byte-identical (determinism)" {
            let items = [ for i in 0..40 -> pt (float (i % 7)) (float (i % 5)), i ]
            let g1 = SpatialGrid.build 3.0 items
            let g2 = SpatialGrid.build 3.0 items
            let region = { X = 0.0; Y = 0.0; Width = 4.0; Height = 4.0 }
            Expect.equal (SpatialGrid.query region g1) (SpatialGrid.query region g2) "same rect result"
            Expect.equal (SpatialGrid.queryRadius (pt 2.0 2.0) 2.5 g1) (SpatialGrid.queryRadius (pt 2.0 2.0) 2.5 g2) "same radius result"
        }

        test "huge (out-of-range) coordinates stay exact via the O(n) fallback" {
            // A cell index past 2^31 would wrap; the impl must fall back to an exact scan there.
            let items = [ pt 1.0e15 1.0e15, "far"; pt 2.0 2.0, "near"; pt 1.0e15 1.0e15, "far2" ]
            let grid = SpatialGrid.build 1.0 items
            let region = { X = 1.0e15 - 1.0; Y = 1.0e15 - 1.0; Width = 2.0; Height = 2.0 }
            Expect.equal (SpatialGrid.query region grid) [ "far"; "far2" ] "exact hit on huge coords, insertion order"
            Expect.equal (SpatialGrid.queryRadius (pt 1.0e15 1.0e15) 1.0 grid) [ "far"; "far2" ] "radius exact on huge coords"
        }

        testCase "rect query equals brute-force filter, in insertion order (FsCheck)" <| fun () ->
            let prop (coords: (float * float) list) (cs: float) (rx: float) (ry: float) (rw: float) (rh: float) =
                let items = coords |> List.mapi (fun i (x, y) -> pt (clamp x) (clamp y), i)
                let grid = SpatialGrid.build (goodCell cs) items
                let region = { X = clamp rx; Y = clamp ry; Width = abs (clamp rw); Height = abs (clamp rh) }
                SpatialGrid.query region grid = bruteRect region items
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 500, prop)

        testCase "radius query equals brute-force filter, in insertion order (FsCheck)" <| fun () ->
            let prop (coords: (float * float) list) (cs: float) (cx: float) (cy: float) (r: float) =
                let items = coords |> List.mapi (fun i (x, y) -> pt (clamp x) (clamp y), i)
                let grid = SpatialGrid.build (goodCell cs) items
                let center = pt (clamp cx) (clamp cy)
                let radius = clamp r
                SpatialGrid.queryRadius center radius grid = bruteRadius center radius items
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 500, prop)
    ]
