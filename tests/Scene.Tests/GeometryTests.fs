module GeometryTests

// Feature 239 (US1): public AABB helpers on the shared Scene Rect/Point. All pure computation.
// Convention under test: `intersects` strict (touching is NOT overlap); `contains`/`containsPoint`
// inclusive of shared edges; `center`/`ofCenter` round-trip; `sweptIntersects` catches tunneling.

open Expecto
open FsCheck
open FS.GG.UI.Scene

let private r x y w h : Rect = { X = x; Y = y; Width = w; Height = h }
let private p x y : Point = { X = x; Y = y }

// Two ints -> a rect with non-negative size, coords/size bounded so overlaps actually happen.
let private rectOf (a: int) (b: int) (c: int) (d: int) : Rect =
    { X = float (a % 50)
      Y = float (b % 50)
      Width = float (abs (c % 50))
      Height = float (abs (d % 50)) }

[<Tests>]
let tests =
    testList "Feature 239 Geometry (US1, FR-001..FR-005)" [

        testList "intersects (strict edges)" [
            test "overlapping rectangles intersect" {
                Expect.isTrue (Geometry.intersects (r 0. 0. 10. 10.) (r 5. 5. 10. 10.)) "overlap ⇒ true"
            }
            test "disjoint rectangles do not intersect" {
                Expect.isFalse (Geometry.intersects (r 0. 0. 10. 10.) (r 20. 20. 5. 5.)) "gap ⇒ false"
            }
            test "edge-touching rectangles do NOT intersect (strict)" {
                Expect.isFalse (Geometry.intersects (r 0. 0. 10. 10.) (r 10. 0. 10. 10.)) "shared edge is zero-area ⇒ false"
            }
            test "corner-touching rectangles do NOT intersect (strict)" {
                Expect.isFalse (Geometry.intersects (r 0. 0. 10. 10.) (r 10. 10. 10. 10.)) "shared corner ⇒ false"
            }
            testCase "intersects is symmetric (FsCheck ≥500 cases)" <| fun () ->
                let prop (a: int) (b: int) (c: int) (d: int) (e: int) (f: int) (g: int) (h: int) =
                    let x = rectOf a b c d
                    let y = rectOf e f g h
                    Geometry.intersects x y = Geometry.intersects y x
                Check.One(Config.QuickThrowOnFailure.WithMaxTest 500, prop)
            test "a zero-area rectangle on a shared edge does not intersect (strict)" {
                // Consistent with the plain strict formula (no zero-area special-casing): a degenerate
                // rect touching only the boundary is not an overlap. (Strictly *interior* it would be —
                // that is the documented consequence of the strict convention, not a separate rule.)
                Expect.isFalse (Geometry.intersects (r 10. 5. 0. 0.) (r 0. 0. 10. 10.)) "on the edge ⇒ false"
            }
            test "NaN coordinates return false, never throw" {
                Expect.isFalse (Geometry.intersects (r nan 0. 10. 10.) (r 0. 0. 10. 10.)) "NaN ⇒ false, total"
            }
        ]

        testList "contains / containsPoint (inclusive edges)" [
            test "outer fully containing inner ⇒ true" {
                Expect.isTrue (Geometry.contains (r 0. 0. 20. 20.) (r 5. 5. 5. 5.)) "inner inside ⇒ true"
            }
            test "inner flush against the outer edge is contained (inclusive)" {
                Expect.isTrue (Geometry.contains (r 0. 0. 20. 20.) (r 0. 0. 20. 20.)) "flush/coincident ⇒ contained"
            }
            test "inner poking outside ⇒ false" {
                Expect.isFalse (Geometry.contains (r 0. 0. 20. 20.) (r 15. 15. 10. 10.)) "spills past edge ⇒ false"
            }
            test "point strictly inside ⇒ true" {
                Expect.isTrue (Geometry.containsPoint (r 0. 0. 10. 10.) (p 5. 5.)) "interior point"
            }
            test "point on the low edge/corner ⇒ true (inclusive)" {
                Expect.isTrue (Geometry.containsPoint (r 0. 0. 10. 10.) (p 0. 0.)) "low corner inclusive"
            }
            test "point on the high edge/corner ⇒ true (inclusive)" {
                Expect.isTrue (Geometry.containsPoint (r 0. 0. 10. 10.) (p 10. 10.)) "high corner inclusive"
            }
            test "point outside ⇒ false" {
                Expect.isFalse (Geometry.containsPoint (r 0. 0. 10. 10.) (p 11. 5.)) "beyond high edge"
            }
        ]

        testList "center / ofCenter" [
            test "center of a known rectangle" {
                Expect.equal (Geometry.center (r 10. 20. 40. 60.)) (p 30. 50.) "center = corner + half-extent"
            }
            testCase "center (ofCenter c w h) = c round-trip (FsCheck ≥500 cases)" <| fun () ->
                let prop (cx: int) (cy: int) (w: int) (h: int) =
                    let c = p (float (cx % 1000)) (float (cy % 1000))
                    let width = float (abs (w % 500))
                    let height = float (abs (h % 500))
                    let got = Geometry.center (Geometry.ofCenter c width height)
                    abs (got.X - c.X) < 1e-9 && abs (got.Y - c.Y) < 1e-9
                Check.One(Config.QuickThrowOnFailure.WithMaxTest 500, prop)
            test "ofCenter produces the requested size" {
                let rect = Geometry.ofCenter (p 100. 50.) 20. 8.
                Expect.equal rect.Width 20. "width preserved"
                Expect.equal rect.Height 8. "height preserved"
            }
        ]

        testList "sweptIntersects (tunneling)" [
            test "a fast projectile tunneling through a thin target is detected" {
                // 2x2 bullet at x=0 moving +100 in x; a 1-wide wall at x=50 — start & end never overlap it.
                let bullet = r 0. 0. 2. 2.
                let wall = r 50. 0. 1. 10.
                Expect.isFalse (Geometry.intersects bullet wall) "start does not overlap"
                Expect.isFalse (Geometry.intersects (r 100. 0. 2. 2.) wall) "end does not overlap"
                Expect.isTrue (Geometry.sweptIntersects bullet (p 100. 0.) wall) "the swept path crosses the wall ⇒ true"
            }
            test "no motion + no overlap ⇒ false" {
                Expect.isFalse (Geometry.sweptIntersects (r 0. 0. 2. 2.) (p 0. 0.) (r 50. 0. 1. 10.)) "still & apart ⇒ false"
            }
            testCase "sweptIntersects is a superset of intersects at both endpoints (FsCheck ≥500)" <| fun () ->
                let prop (a: int) (b: int) (c: int) (d: int) (vx: int) (vy: int) (e: int) (f: int) =
                    let moving = rectOf a b c d
                    let target = rectOf e f (c + 3) (d + 3)
                    let v = p (float (vx % 100)) (float (vy % 100))
                    let movedEnd = { moving with X = moving.X + v.X; Y = moving.Y + v.Y }
                    // if either endpoint overlaps, the sweep must report overlap.
                    if Geometry.intersects moving target || Geometry.intersects movedEnd target then
                        Geometry.sweptIntersects moving v target
                    else
                        true
                Check.One(Config.QuickThrowOnFailure.WithMaxTest 500, prop)
        ]
    ]
