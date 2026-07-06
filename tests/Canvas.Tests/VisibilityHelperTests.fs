module Canvas.Tests.VisibilityHelperTests

// Feature 247 (US1): the import-and-adapt 2D-visibility helper source (template/fragments/visibility/
// src/Product/Visibility.fs) is compiled here via the framework test project (its literal
// `namespace AppRoot` is the default sourceName). The geometry vocabulary reuses Point/Rect/SpatialGrid;
// the ray-segment intersection + angular sweep are the added layer. All real pure computation — no
// synthetic evidence. Covers FR-006 (region + occlusion), FR-008 (determinism), FR-010 (totality),
// FR-011 (bounded).

open Expecto
open FS.GG.Game.Core // ADR-0022 P5: the visibility fragment's Point/Rect + SpatialGrid now come from FS.GG.Game.Core
open AppRoot

let private p x y : Point = { X = x; Y = y }
let private seg ax ay bx by : Visibility.Segment = { A = p ax ay; B = p bx by }
let private settings r c : Visibility.Settings = { Radius = r; CellSize = c }

let private withinBound (source: Point) (radius: float) (v: Point) =
    abs (v.X - source.X) <= radius + 1e-6 && abs (v.Y - source.Y) <= radius + 1e-6

[<Tests>]
let tests =
    testList "Feature 247 Visibility helper (US1, FR-006/008/010/011)" [

        // --- ray-segment intersection core -------------------------------------------------------
        test "raySegment hits a wall straight ahead at the expected point and distance" {
            match Visibility.raySegment (p 0.0 0.0) (p 1.0 0.0) (seg 5.0 -5.0 5.0 5.0) with
            | None -> failtest "expected a hit on the wall at x=5"
            | Some(hit, t) ->
                Expect.floatClose Accuracy.high t 5.0 "t is the distance to the wall"
                Expect.floatClose Accuracy.high hit.X 5.0 "hit lands on the wall's x"
                Expect.floatClose Accuracy.high hit.Y 0.0 "hit is level with the ray"
        }

        test "raySegment returns None for a wall behind the ray, off to the side, or collinear" {
            Expect.isNone (Visibility.raySegment (p 0.0 0.0) (p 1.0 0.0) (seg -5.0 -5.0 -5.0 5.0)) "wall behind the origin"
            Expect.isNone (Visibility.raySegment (p 0.0 0.0) (p 1.0 0.0) (seg 5.0 5.0 5.0 15.0)) "wall off to the side (u<0)"
            Expect.isNone (Visibility.raySegment (p 0.0 0.0) (p 1.0 0.0) (seg 5.0 0.0 10.0 0.0)) "wall collinear with the ray (parallel, no NaN)"
        }

        // --- FR-006: occlusion (a region, not just geometry) -------------------------------------
        test "a wall between source and target blocks line of sight; removing it restores it" {
            let wall = [ seg 5.0 -5.0 5.0 5.0 ]
            Expect.isFalse (Visibility.isVisible (p 0.0 0.0) (p 10.0 0.0) wall) "target hidden behind the wall"
            Expect.isTrue (Visibility.isVisible (p 0.0 0.0) (p 10.0 0.0) []) "target visible with no walls"
        }

        test "a wall NOT between source and target does not block" {
            // wall sits beyond the target, so it cannot occlude it
            Expect.isTrue (Visibility.isVisible (p 0.0 0.0) (p 4.0 0.0) [ seg 5.0 -5.0 5.0 5.0 ]) "wall past the target does not block"
        }

        // --- FR-011: the polygon is bounded and closed -------------------------------------------
        test "with no occluders the polygon fills the bound box and every vertex is within Radius" {
            let src = p 0.0 0.0
            let poly = Visibility.polygon (settings 10.0 8.0) src []
            Expect.isNonEmpty poly.Vertices "an unobstructed sweep still yields a bounded ring"
            Expect.all poly.Vertices (withinBound src 10.0) "every vertex lies within the source ± Radius bound"
            Expect.equal poly.Source src "the polygon records its source"
        }

        test "an occluder pulls the polygon in: no vertex sits beyond the wall along the blocked ray" {
            let src = p 0.0 0.0
            let poly = Visibility.polygon (settings 50.0 8.0) src [ seg 5.0 -5.0 5.0 5.0 ]
            // Nothing directly ahead (small |Y|) may extend past the wall at x=5.
            let aheadBeyondWall =
                poly.Vertices |> List.exists (fun v -> abs v.Y < 0.5 && v.X > 5.0 + 1e-6)
            Expect.isFalse aheadBeyondWall "the wall occludes everything directly behind it"
        }

        // --- FR-008: deterministic (byte-identical across repeated runs) --------------------------
        test "polygon is deterministic: repeated runs are byte-identical" {
            let src = p 1.0 2.0
            let walls =
                [ seg 5.0 0.0 5.0 5.0
                  seg 8.0 0.0 8.0 -5.0        // (5,0) and (8,0) share the +x angle -> tie broken by index
                  seg -6.0 3.0 -6.0 -3.0
                  seg 3.0 3.0 9.0 9.0 ]
            let run () = Visibility.polygon (settings 20.0 6.0) src walls
            Expect.equal (run ()) (run ()) "identical inputs -> identical polygon"
        }

        // --- FR-010: total on degenerate input ---------------------------------------------------
        test "empty and zero-length segments are total and never throw" {
            let src = p 0.0 0.0
            Expect.isNonEmpty (Visibility.polygon (settings 10.0 8.0) src []).Vertices "empty set -> bound box polygon"
            Expect.isTrue (Visibility.isVisible src (p 4.0 0.0) [ seg 2.0 0.0 2.0 0.0 ]) "zero-length wall occludes nothing"
            let withZero = Visibility.polygon (settings 10.0 8.0) src [ seg 3.0 3.0 3.0 3.0 ]
            Expect.isNonEmpty withZero.Vertices "a zero-length occluder is filtered, sweep still total"
        }

        test "source on a wall, non-finite input, and a non-positive radius are total" {
            let onWall = Visibility.polygon (settings 10.0 8.0) (p 5.0 0.0) [ seg 5.0 -5.0 5.0 5.0 ]
            Expect.isNotNull (box onWall) "source lying on a wall does not throw"
            let nanSeg = Visibility.polygon (settings 10.0 8.0) (p 0.0 0.0) [ { A = p (0.0 / 0.0) 0.0; B = p 5.0 5.0 } ]
            Expect.isNonEmpty nanSeg.Vertices "a non-finite occluder is filtered; sweep still totals"
            Expect.all nanSeg.Vertices (fun v -> System.Double.IsFinite v.X && System.Double.IsFinite v.Y) "no NaN coordinate leaks into the polygon"
            let nanSource = Visibility.polygon (settings 10.0 8.0) (p (0.0 / 0.0) 0.0) []
            Expect.isEmpty nanSource.Vertices "a non-finite source yields an empty (not throwing) polygon"
            let zeroR = Visibility.polygon (settings 0.0 8.0) (p 0.0 0.0) []
            Expect.isNonEmpty zeroR.Vertices "non-positive radius falls back to a minimal bound, still total"
            Expect.isFalse (Visibility.isVisible (p 0.0 0.0) (p (0.0 / 0.0) 0.0) []) "non-finite target is not visible (no throw)"
        }
    ]
