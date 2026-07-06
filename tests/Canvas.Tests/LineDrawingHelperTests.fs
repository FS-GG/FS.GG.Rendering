module Canvas.Tests.LineDrawingHelperTests

// Feature 248 (US1): the import-and-adapt grid line-drawing helper source (template/fragments/
// line-drawing/src/Product/LineDrawing.fs) is compiled here via the framework test project (its literal
// `namespace AppRoot` is the default sourceName). The grid vocabulary reuses the shared `Cell`; the
// Bresenham `line`, the `supercover` walk, and the `lineOfSight` query are the added layer. All real pure
// computation — no synthetic evidence. Covers FR-006 (LOS), FR-008 (determinism), FR-010 (totality),
// FR-011 (bounded / connected).

open Expecto
open FS.GG.Game.Core // ADR-0022 P5: the line-drawing fragment's Cell now comes from FS.GG.Game.Core
open AppRoot

let private c col row : Cell = { Col = col; Row = row }

/// Endpoints spanning every octant around the origin (incl. axes, diagonals, negative deltas).
let private targets =
    [ for col in -4 .. 4 do
          for row in -4 .. 4 -> c col row ]

[<Tests>]
let tests =
    testList "Feature 248 LineDrawing helper (US1, FR-006/008/010/011)" [

        // --- endpoints + connectivity (a connected path of tiles, not a distance) ----------------
        test "line includes both endpoints and every step is grid-adjacent (<=1 per axis)" {
            for b in targets do
                let ln = LineDrawing.line (c 0 0) b
                Expect.equal (List.head ln) (c 0 0) "line starts at a"
                Expect.equal (List.last ln) b (sprintf "line ends at b=%A" b)
                ln
                |> List.pairwise
                |> List.iter (fun (p, q) ->
                    Expect.isTrue
                        (p <> q && abs (p.Col - q.Col) <= 1 && abs (p.Row - q.Row) <= 1)
                        (sprintf "line step %A->%A is adjacent and non-repeating (b=%A)" p q b))
        }

        test "supercover includes both endpoints and is strictly 4-connected (no diagonal gap)" {
            for b in targets do
                let sc = LineDrawing.supercover (c 0 0) b
                Expect.equal (List.head sc) (c 0 0) "supercover starts at a"
                Expect.equal (List.last sc) b (sprintf "supercover ends at b=%A" b)
                sc
                |> List.pairwise
                |> List.iter (fun (p, q) ->
                    Expect.equal
                        (abs (p.Col - q.Col) + abs (p.Row - q.Row))
                        1
                        (sprintf "supercover step %A->%A differs by exactly 1 in exactly one axis (b=%A)" p q b))
        }

        // --- FR-006: line-of-sight over a Cell -> bool transparency map ---------------------------
        test "a wall cell between source and target blocks line of sight; removing it restores it" {
            let a = c 0 0
            let b = c 6 2
            let wall = c 3 1 // sits on the supercover walk between a and b
            Expect.isFalse (LineDrawing.lineOfSight (fun x -> x <> wall) a b) "target hidden behind the wall tile"
            Expect.isTrue (LineDrawing.lineOfSight (fun _ -> true) a b) "target visible with no walls"
        }

        test "the endpoints themselves are never tested (you can look FROM and AT an opaque tile)" {
            let a = c 0 0
            let b = c 6 2
            Expect.isTrue (LineDrawing.lineOfSight (fun x -> x <> b) a b) "an opaque target tile is still visible"
            Expect.isTrue (LineDrawing.lineOfSight (fun x -> x <> a) a b) "standing on an opaque tile still sees out"
        }

        // --- FR-008: deterministic (byte-identical across repeated runs, every octant) ------------
        test "line and supercover are deterministic: repeated runs are byte-identical" {
            for b in targets do
                Expect.equal (LineDrawing.line (c 0 0) b) (LineDrawing.line (c 0 0) b) "line identical on replay"
                Expect.equal (LineDrawing.supercover (c 0 0) b) (LineDrawing.supercover (c 0 0) b) "supercover identical on replay"
        }

        // --- FR-010 / FR-011: total + bounded on degenerate input --------------------------------
        test "start = goal returns the single start cell and is total" {
            Expect.equal (LineDrawing.line (c 2 3) (c 2 3)) [ c 2 3 ] "line a=a -> [a]"
            Expect.equal (LineDrawing.supercover (c 2 3) (c 2 3)) [ c 2 3 ] "supercover a=a -> [a]"
            Expect.isTrue (LineDrawing.lineOfSight (fun _ -> false) (c 2 3) (c 2 3)) "lineOfSight a=a -> true even with an all-opaque map"
        }

        test "axis-aligned and pure-diagonal lines are exact and never throw" {
            Expect.equal (LineDrawing.line (c 0 0) (c 3 0)) [ c 0 0; c 1 0; c 2 0; c 3 0 ] "horizontal run"
            Expect.equal (LineDrawing.line (c 0 0) (c 0 3)) [ c 0 0; c 0 1; c 0 2; c 0 3 ] "vertical run"
            Expect.equal (LineDrawing.line (c 0 0) (c 3 3)) [ c 0 0; c 1 1; c 2 2; c 3 3 ] "diagonal run (thin, diagonal-connected)"
            // a negative-delta line is the reverse-direction mirror, still total
            Expect.equal (List.head (LineDrawing.line (c 0 0) (c -3 -2))) (c 0 0) "negative-delta line starts at a"
            Expect.equal (List.last (LineDrawing.line (c 0 0) (c -3 -2))) (c -3 -2) "negative-delta line ends at b"
        }

        test "an always-true and always-false predicate are both total" {
            Expect.isTrue (LineDrawing.lineOfSight (fun _ -> true) (c 0 0) (c 5 5)) "clear map -> visible"
            Expect.isFalse (LineDrawing.lineOfSight (fun _ -> false) (c 0 0) (c 5 5)) "fully opaque interior -> not visible"
        }

        test "the emitted cell count is bounded by the endpoint separation (finite)" {
            for b in targets do
                let cheb = max (abs b.Col) (abs b.Row)
                let ln = LineDrawing.line (c 0 0) b
                Expect.equal (List.length ln) (cheb + 1) (sprintf "line length is Chebyshev+1 for b=%A" b)
                let sc = LineDrawing.supercover (c 0 0) b
                // supercover is 4-connected: length is the Manhattan step count + 1
                Expect.equal (List.length sc) (abs b.Col + abs b.Row + 1) (sprintf "supercover length is Manhattan+1 for b=%A" b)
        }

        test "large coordinates stay total and correct (delta/error arithmetic is int64, not int32)" {
            // A dominant delta this large would have overflowed 32-bit `2*err` (stalling the walk into an
            // infinite loop) had the arithmetic stayed int32; big enough to prove the int64 promotion,
            // small enough to enumerate quickly. The truly degenerate billion-cell spans are total by the
            // same promotion but too large to walk in a unit test.
            let a = c 0 0
            let b = c 100003 7
            let ln = LineDrawing.line a b
            Expect.equal (List.head ln) a "line starts at a"
            Expect.equal (List.last ln) b "line ends at b (no stall / overflow)"
            Expect.equal (List.length ln) (100003 + 1) "line length is Chebyshev+1 at scale"
            let sc = LineDrawing.supercover a b
            Expect.equal (List.last sc) b "supercover ends at b"
            Expect.equal (List.length sc) (100003 + 7 + 1) "supercover length is Manhattan+1 at scale"
            sc
            |> List.pairwise
            |> List.iter (fun (p, q) ->
                Expect.equal (abs (p.Col - q.Col) + abs (p.Row - q.Row)) 1 "supercover stays strictly 4-connected at scale")
        }
    ]
