module Canvas.Tests.CollisionHelperTests

// Feature 246 (US1): the import-and-adapt collision helper source (template/fragments/collision/
// src/Product/Collision.fs) is compiled here via the framework test project (its literal
// `namespace AppRoot` is the default sourceName). Detection reuses Geometry/SpatialGrid; the response
// layer separates overlaps deterministically. All real pure computation — no synthetic evidence.

open Expecto
open FsCheck
open FS.GG.Game.Core // ADR-0022 P5: the collision fragment's Point/Rect + Geometry/SpatialGrid now come from FS.GG.Game.Core
open AppRoot

// A body at rest (a wall / a static overlap fixture): zero per-step displacement.
let private body x y w h tag : Collision.Body<int> =
    { Bounds = { X = x; Y = y; Width = w; Height = h }
      Velocity = { X = 0.0; Y = 0.0 }
      Tag = tag }

// A body that travels `(vx, vy)` this step — the swept-detection fixtures (#290).
let private moving x y w h vx vy tag : Collision.Body<int> =
    { Bounds = { X = x; Y = y; Width = w; Height = h }
      Velocity = { X = vx; Y = vy }
      Tag = tag }

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

        // --- #290: a moving body is a SEGMENT, never a point — the swept pass does not tunnel ---------

        test "a fast round does not tunnel a thin target (the issue's 1200 u/s example)" {
            // 1200 u/s advanced by one 60 Hz step covers 20 units; a 6-wide target sits in the path.
            let round = moving 0.0 0.0 2.0 2.0 20.0 0.0 1
            let target = body 10.0 -2.0 6.0 6.0 2
            // Neither the start nor the end position overlaps — a point test reports a clean miss on
            // the very pair that collides, exactly the FS.GG.Game.Core.Ballistics red-vs-point property.
            let endRound = { round.Bounds with X = round.Bounds.X + round.Velocity.X }
            Expect.isFalse (Geometry.intersects round.Bounds target.Bounds) "start: the round is in front of the target"
            Expect.isFalse (Geometry.intersects endRound target.Bounds) "end: the round is past the target"
            let hits = Collision.collide 16.0 [ round; target ] |> List.map (fun c -> c.A.Tag, c.B.Tag)
            Expect.equal hits [ (1, 2) ] "the swept pass detects the crossing the point test misses"
        }

        test "resolving a swept hit stops the mover at the wall's near face (first-contact advance)" {
            let round = moving 0.0 0.0 2.0 2.0 20.0 0.0 1
            let wall = body 10.0 -2.0 6.0 6.0 2
            match Collision.step Collision.PushFirst 16.0 [ round; wall ] with
            | [ r ] ->
                Expect.floatClose Accuracy.high (r.A.Bounds.X + r.A.Bounds.Width) wall.Bounds.X
                    "PushFirst advances the round to touch the wall's near face, not past it"
                Expect.equal r.B.Bounds wall.Bounds "the immovable wall does not move"
            | other -> failtestf "expected exactly one resolution, got %d" (List.length other)
        }

        // --- #290 acceptance: any speed, any timestep, any grid — a crossed wall is always detected ---
        // Property (FsCheck): for a body whose step segment crosses a wall, `collide` reports the
        // contact, AND the naive endpoint point test reports a miss — so this property is RED against
        // the pre-fix point-test implementation. Speed (= velocity × dt), start gap, target height band,
        // and the broad-phase cell size all vary; the only fixed premise is that the segment crosses.
        test "property: a body of any speed/timestep collides with a wall its step segment crosses" {
            Check.One(
                Config.QuickThrowOnFailure.WithMaxTest 500,
                fun (startGap: NormalFloat) (extraSpeed: NormalFloat) (yBand: NormalFloat) (cell: NormalFloat) ->
                    let pw, ph = 4.0, 4.0
                    let wall = body 500.0 0.0 6.0 200.0 2
                    // Strictly left of the wall (gap ≥ 0.5 keeps the start endpoint clear of the face).
                    let gap = 0.5 + abs startGap.Get % 100.0
                    let px0 = wall.Bounds.X - gap - pw
                    // Anywhere inside the wall's tall Y span, so the horizontal sweep truly crosses it.
                    let py = wall.Bounds.Y + abs yBand.Get % (wall.Bounds.Height - ph)
                    // Enough displacement to end fully RIGHT of the wall (+1 margin), plus any extra speed.
                    let dx = gap + pw + wall.Bounds.Width + 1.0 + abs extraSpeed.Get % 100000.0
                    let round = moving px0 py pw ph dx 0.0 1
                    let cellSize = 4.0 + abs cell.Get % 500.0

                    let endRound = { round.Bounds with X = round.Bounds.X + dx }
                    let pointTestMisses =
                        not (Geometry.intersects round.Bounds wall.Bounds)
                        && not (Geometry.intersects endRound wall.Bounds)
                    let sweptFinds =
                        Collision.collide cellSize [ round; wall ]
                        |> List.exists (fun c -> c.A.Tag = 1 && c.B.Tag = 2)

                    pointTestMisses && sweptFinds)
        }

        test "a zero-velocity swept pass reduces to the static overlap pass" {
            // With no motion, `collide` must be byte-identical to the pre-#290 point behaviour.
            let bodies = [ body 0.0 0.0 10.0 10.0 1; body 6.0 2.0 10.0 10.0 2; body 100.0 100.0 8.0 8.0 3 ]
            let swept = Collision.collide 16.0 bodies |> List.map (fun c -> c.A.Tag, c.B.Tag)
            Expect.equal swept [ (1, 2) ] "static (zero-velocity) bodies collide exactly as before"
        }

        // --- #890: circle-vs-static-AABB axis-separated sliding sweep (player-hitbox movement) --------
        // The moving-CIRCLE case `Body`/`collide` do NOT cover. `slideCircle` composes the framework
        // `Geometry.circleAabbContact` primitive; all real pure computation, no synthetic evidence.

        let circle cx cy r : Circle = { Center = { X = cx; Y = cy }; Radius = r }

        test "slideCircle with no walls moves the centre by the full displacement" {
            let start = circle 0.0 0.0 13.0
            let out = Collision.slideCircle None [] start { X = 5.0; Y = -3.0 }
            Expect.floatClose Accuracy.high out.Center.X 5.0 "X advances by dx"
            Expect.floatClose Accuracy.high out.Center.Y -3.0 "Y advances by dy"
            Expect.floatClose Accuracy.high out.Radius 13.0 "radius is unchanged"
        }

        test "slideCircle stops on the blocked axis but keeps moving on the free one (slides)" {
            // A tall wall at x∈[100,120]; the circle (r=13) starts left of it at (80,50) and moves
            // right-and-down. X is blocked at the wall face; Y must still advance the full 10 units.
            let wall = { X = 100.0; Y = 0.0; Width = 20.0; Height = 200.0 }
            let start = circle 80.0 50.0 13.0
            let out = Collision.slideCircle None [ wall ] start { X = 25.0; Y = 10.0 }
            Expect.floatClose Accuracy.high (out.Center.X + out.Radius) wall.X
                "the disc's right edge rests on the wall's near face (X blocked, not tunnelled through)"
            Expect.floatClose Accuracy.high out.Center.Y 60.0 "Y slides the full 10 units despite the X block"
            Expect.isNone (Geometry.circleAabbContact out wall) "the resolved circle no longer overlaps the wall"
        }

        test "slideCircle clamps the centre inside bounds (inset by the radius)" {
            let bounds = { X = 0.0; Y = 0.0; Width = 200.0; Height = 200.0 }
            let start = circle 190.0 10.0 13.0
            let out = Collision.slideCircle (Some bounds) [] start { X = 50.0; Y = -50.0 }
            Expect.floatClose Accuracy.high out.Center.X (200.0 - 13.0) "clamped to the right inset"
            Expect.floatClose Accuracy.high out.Center.Y 13.0 "clamped to the top inset"
        }

        test "slideCircle is deterministic: repeated runs are byte-identical" {
            let walls = [ { X = 100.0; Y = 0.0; Width = 20.0; Height = 200.0 }
                          { X = 40.0; Y = 40.0; Width = 10.0; Height = 10.0 } ]
            let bounds = Some { X = 0.0; Y = 0.0; Width = 300.0; Height = 300.0 }
            let start = circle 80.0 50.0 13.0
            let run () = Collision.slideCircle bounds walls start { X = 40.0; Y = 12.0 }
            Expect.equal (run ()) (run ()) "identical inputs -> identical resolved circle"
        }

        test "a fast mover is kept from tunnelling by sub-stepping (each chunk <= the radius)" {
            // 40 units in ONE step overshoots the 20-wide wall's midline and tunnels (documented
            // single-step boundary); folding the same move as four 10-unit chunks (each < r=13) keeps
            // consecutive discs overlapping, so the sweep stops on the wall's near face instead.
            let wall = { X = 100.0; Y = 0.0; Width = 20.0; Height = 200.0 }
            let start = circle 80.0 50.0 13.0
            let stepped =
                [ 1..4 ]
                |> List.fold (fun c _ -> Collision.slideCircle None [ wall ] c { X = 10.0; Y = 0.0 }) start
            Expect.floatClose Accuracy.high (stepped.Center.X + stepped.Radius) wall.X
                "sub-stepped fast move rests on the wall's near face — no tunnel"
        }

        test "slideCircle is total on non-finite displacement and radius (never throws)" {
            let wall = { X = 100.0; Y = 0.0; Width = 20.0; Height = 200.0 }
            let start = circle 80.0 50.0 13.0
            let nan = 0.0 / 0.0
            // A non-finite displacement axis contributes nothing rather than poisoning the centre.
            let out = Collision.slideCircle None [ wall ] start { X = nan; Y = 5.0 }
            Expect.isTrue (System.Double.IsFinite out.Center.X && System.Double.IsFinite out.Center.Y)
                "non-finite dx is dropped; the centre stays finite"
            Expect.floatClose Accuracy.high out.Center.Y 55.0 "the finite Y axis still advances"
            // A NaN radius is a no-contact input, so no wall resolution fires and nothing throws.
            let bad = Collision.slideCircle None [ wall ] (circle 80.0 50.0 nan) { X = 40.0; Y = 0.0 }
            Expect.floatClose Accuracy.high bad.Center.X 120.0 "NaN-radius disc is not resolved (moves freely)"
        }
    ]
