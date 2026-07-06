module Canvas.Tests.CollisionHelperTests

// Feature 246 (US1): the import-and-adapt collision helper source (template/fragments/collision/
// src/Product/Collision.fs) is compiled here via the framework test project (its literal
// `namespace AppRoot` is the default sourceName). Detection reuses Geometry/SpatialGrid; the response
// layer separates overlaps deterministically. All real pure computation — no synthetic evidence.

open Expecto
open FS.GG.Game.Core // ADR-0022 P5: the collision fragment's Point/Rect + Geometry/SpatialGrid now come from FS.GG.Game.Core
open AppRoot

let private body x y w h tag : Collision.Body<int> =
    { Bounds = { X = x; Y = y; Width = w; Height = h }; Tag = tag }

[<Tests>]
let tests =
    testList "Feature 246 Collision helper (US1, FR-002/006/008/010)" [

        // --- FR-006: reports overlap AND a resolution, not a bare boolean -------------------------
        test "overlapping pair yields a contact with positive depth and a minimum-translation vector" {
            let a = body 0.0 0.0 10.0 10.0 1
            let b = body 6.0 0.0 10.0 10.0 2       // 4-wide overlap on X
            match Collision.contact a b with
            | None -> failtest "expected a contact for overlapping bodies"
            | Some c ->
                Expect.isGreaterThan c.Depth 0.0 "depth is positive"
                Expect.floatClose Accuracy.high c.Depth 4.0 "least-penetration depth is the 4-wide X overlap"
                Expect.floatClose Accuracy.high c.Penetration.X -4.0 "A is pushed left off B (negative X)"
                Expect.floatClose Accuracy.high c.Penetration.Y 0.0 "no vertical component for a horizontal overlap"
        }

        test "resolve SeparateEqually removes the overlap (bodies no longer intersect)" {
            let a = body 0.0 0.0 10.0 10.0 1
            let b = body 6.0 0.0 10.0 10.0 2
            match Collision.contact a b with
            | None -> failtest "expected a contact"
            | Some c ->
                let r = Collision.resolve Collision.SeparateEqually c
                Expect.isFalse (Geometry.intersects r.A.Bounds r.B.Bounds) "separated pair does not overlap (touching is not intersect)"
                Expect.floatClose Accuracy.high r.Applied.X -2.0 "A takes half the MTV"
        }

        test "PushFirst moves only the first body; PushSecond moves only the second" {
            let a = body 0.0 0.0 10.0 10.0 1
            let b = body 6.0 0.0 10.0 10.0 2
            let c = (Collision.contact a b).Value
            let rf = Collision.resolve Collision.PushFirst c
            Expect.equal rf.B.Bounds b.Bounds "PushFirst leaves the second body fixed"
            Expect.notEqual rf.A.Bounds a.Bounds "PushFirst moves the first body"
            let rs = Collision.resolve Collision.PushSecond c
            Expect.equal rs.A.Bounds a.Bounds "PushSecond leaves the first body fixed"
            Expect.notEqual rs.B.Bounds b.Bounds "PushSecond moves the second body"
        }

        test "Bounce records a clamped restitution; Slide records none" {
            let c = (Collision.contact (body 0.0 0.0 10.0 10.0 1) (body 6.0 0.0 10.0 10.0 2)).Value
            Expect.floatClose Accuracy.high (Collision.resolve (Collision.Bounce 50) c).Restitution 0.5 "50% -> 0.5"
            Expect.floatClose Accuracy.high (Collision.resolve (Collision.Bounce 250) c).Restitution 1.0 "over-100% clamps to 1.0"
            Expect.floatClose Accuracy.high (Collision.resolve Collision.Slide c).Restitution 0.0 "slide records no restitution"
        }

        // --- FR-008: deterministic (byte-identical across repeated runs) --------------------------
        test "collide is deterministic: repeated runs are byte-identical" {
            let bodies =
                [ body 0.0 0.0 10.0 10.0 1
                  body 6.0 2.0 10.0 10.0 2
                  body 100.0 100.0 8.0 8.0 3
                  body 103.0 101.0 8.0 8.0 4
                  body 5.0 5.0 4.0 4.0 5 ]
            let run () = Collision.collide 16.0 bodies
            Expect.equal (run ()) (run ()) "identical inputs -> identical contact list"
            let step () = Collision.step Collision.SeparateEqually 16.0 bodies
            Expect.equal (step ()) (step ()) "identical inputs -> identical resolution list"
        }

        test "contacts are emitted in ascending (i, j) index order" {
            // three mutually overlapping bodies -> pairs (1,2),(1,3),(2,3) by insertion index
            let bodies = [ body 0.0 0.0 10.0 10.0 1; body 3.0 0.0 10.0 10.0 2; body 6.0 0.0 10.0 10.0 3 ]
            let pairs = Collision.collide 16.0 bodies |> List.map (fun c -> c.A.Tag, c.B.Tag)
            Expect.equal pairs [ (1, 2); (1, 3); (2, 3) ] "pairs are lower-tag-first, in index order"
        }

        // --- FR-002/006: broad-phase finds every real overlap (no false negatives) ----------------
        test "no false negative: a distant-but-overlapping large body is still found" {
            // A large body whose center is far from a small body it nonetheless overlaps.
            let big = body 0.0 0.0 100.0 100.0 1
            let small = body 95.0 95.0 10.0 10.0 2
            let found = Collision.collide 8.0 [ big; small ] |> List.map (fun c -> c.A.Tag, c.B.Tag)
            Expect.equal found [ (1, 2) ] "the overlap is detected despite far-apart centers"
        }

        // --- FR-010: total on degenerate input ---------------------------------------------------
        test "empty and singleton inputs yield no contacts" {
            Expect.isEmpty (Collision.collide 16.0 ([]: Collision.Body<int> list)) "empty -> []"
            Expect.isEmpty (Collision.collide 16.0 [ body 0.0 0.0 4.0 4.0 1 ]) "singleton -> []"
        }

        test "exactly touching edges are NOT a contact (strict edges)" {
            let a = body 0.0 0.0 10.0 10.0 1
            let b = body 10.0 0.0 10.0 10.0 2      // shares the x=10 edge, zero-area overlap
            Expect.isNone (Collision.contact a b) "edge touch is not an intersection"
        }

        test "a fully contained body produces a contact" {
            let outer = body 0.0 0.0 100.0 100.0 1
            let inner = body 40.0 40.0 10.0 10.0 2
            Expect.isSome (Collision.contact outer inner) "containment overlaps on positive area"
        }

        test "zero-area and non-finite bodies never contact and never throw" {
            let zero = body 5.0 5.0 0.0 10.0 1
            let solid = body 0.0 0.0 20.0 20.0 2
            Expect.isNone (Collision.contact zero solid) "zero-width body has no positive-area overlap"
            let nan = body (0.0 / 0.0) 0.0 10.0 10.0 3
            Expect.isNone (Collision.contact nan solid) "non-finite bounds never overlap"
            // a NaN body mixed with finite overlapping bodies must not poison the finite result
            let mixed = [ nan; body 0.0 0.0 10.0 10.0 4; body 5.0 0.0 10.0 10.0 5 ]
            let found = Collision.collide 16.0 mixed |> List.map (fun c -> c.A.Tag, c.B.Tag)
            Expect.equal found [ (4, 5) ] "finite overlap still found alongside a NaN body"
        }
    ]
