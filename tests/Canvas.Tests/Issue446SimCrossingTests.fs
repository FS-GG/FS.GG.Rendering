module Canvas.Tests.Issue446SimCrossingTests

// FS-GG/FS.GG.Rendering#446 — the game scaffold's `Geometry.Vec2` must be able to reach the helpers
// that the SAME scaffold ships. `Collision` and `Visibility` are Game.Core-exclusive (their Point/Rect
// come from FS.GG.Game.Core), while `Vec2` only crossed into FS.GG.UI.Scene (`toPoint`/`toRect`). So a
// product that followed the scaffold's own instruction to model positions as `Vec2` could not call
// them without hand-writing the bridge — the exact crossing a game makes on its first tick.
// `toSimPoint` / `ofSimPoint` / `toSimRect` are that bridge, now shipped in the fragment.
//
// THE SHAPE OF THIS FILE IS THE ASSERTION. It models a product's entities with `Geometry.Vec2`, drives
// the real shipped `Collision` / `Visibility` / `SpatialGrid` helpers, and reads every result back into
// `Vec2` — WITHOUT `open FS.GG.Game.Core` anywhere, and without one bare Game.Core record literal. That
// is precisely what a consumer could not do before. If a crossing were missing, this file would not
// compile; if one were wrong (a swapped or dropped component), the numeric expectations below fail.
//
// That last clause is load-bearing and easy to get wrong, so it is worth stating why the fixtures look
// the way they do. Swapping `Vx`/`Vy` inside `toSimPoint` reflects every point about y = x, and the
// helpers are reflection-EQUIVARIANT: reflect the whole configuration and every distance, overlap and
// sightline is preserved. So a symmetric fixture — zero velocities, an eye at the origin, a radial
// query — reports SUCCESS under a swapped crossing. The fixtures here are asymmetric, and they assert
// coordinates that travelled through a crossing (a velocity via `Body.Velocity`, the viewpoint via
// `polygon.Source`), which is the only thing the reflection actually moves.
//
// It is also the worked example the three repos lacked: how to construct a Game.Core value in a file
// that must not `open` Game.Core, via the `SimPoint`/`SimRect` abbreviations plus a return-type
// annotation. All real pure computation — no synthetic evidence.

open Expecto
open AppRoot
open AppRoot.Geometry

// A product entity, modelled the way the scaffold instructs: position and velocity are `Vec2`, never
// a record carrying X/Y labels of its own (that is the collision-safety rule Vec2 exists to enforce).
type private Entity =
    { Pos: Vec2
      Vel: Vec2
      Size: float
      Id: int }

let private entity px py vx vy size id =
    { Pos = vec2 px py
      Vel = vec2 vx vy
      Size = size
      Id = id }

// Cross into the simulation vocabulary using SHIPPED HELPERS ONLY. Note what is absent: no
// `open FS.GG.Game.Core`, and no `{ X = …; Y = … }` literal. The return-type annotation names the
// target record, and `toSimRect`/`toSimPoint` build it.
let private bodyOf (e: Entity) : Collision.Body<int> =
    { Bounds = toSimRect e.Pos e.Size e.Size
      Velocity = toSimPoint e.Vel
      Tag = e.Id }

let private segOf (a: Vec2) (b: Vec2) : Visibility.Segment =
    { A = toSimPoint a; B = toSimPoint b }

let private sight (radius: float) : Visibility.Settings = { Radius = radius }

[<Tests>]
let tests =
    testList
        "#446 Vec2 -> Game.Core crossings (a Vec2-modelled product reaches the shipped helpers)"
        [

          // --- Collision: the scaffold's own helper, driven from Vec2 ------------------------------
          // NOTE ON THE DATA. Swapping Vx/Vy inside `toSimPoint` reflects EVERY point about y = x, and
          // the helpers are all reflection-equivariant — so a symmetric fixture (zero velocities, an
          // eye at the origin, a radial query) cannot see the swap: the whole configuration reflects
          // together and every distance is preserved. The fixtures below are therefore ASYMMETRIC, and
          // they assert coordinates that travelled THROUGH a crossing, which is the only thing a
          // reflection actually changes.
          test "a Vec2-modelled pair reaches Collision.contact, and the push-out reads back as a Vec2" {
              // Two 10-wide boxes centred 6 apart on X: spans [-5,5] and [1,11] -> a 4-wide overlap.
              let a = entity 0.0 0.0 1.5 -2.5 10.0 1
              let b = entity 6.0 0.0 0.0 0.0 10.0 2

              match Collision.contact (bodyOf a) (bodyOf b) with
              | None -> failtest "expected an overlap for two 10-wide boxes centred 6 apart"
              | Some c ->
                  let push = ofSimPoint c.Penetration // straight back into the model's vocabulary
                  Expect.floatClose Accuracy.high c.Depth 4.0 "least-penetration depth is the 4-wide X overlap"
                  Expect.floatClose Accuracy.high push.Vx -4.0 "A is pushed left off B (negative X)"
                  Expect.floatClose Accuracy.high push.Vy 0.0 "no vertical component for a horizontal overlap"

                  // The velocity crossed into sim space via `toSimPoint` and must survive the trip
                  // componentwise. Asymmetric on purpose: (1.5, -2.5) reflects to (-2.5, 1.5), so a
                  // swapped crossing fails here rather than passing silently.
                  Expect.equal (ofSimPoint c.A.Velocity) (vec2 1.5 -2.5) "A's velocity round-trips through Body.Velocity"
          }

          test "Collision.step separates a Vec2-modelled pair; the moved body reads back as a Vec2" {
              let a = entity 0.0 0.0 0.0 0.0 10.0 1
              let b = entity 6.0 0.0 0.0 0.0 10.0 2

              match Collision.step Collision.SeparateEqually 32.0 [ bodyOf a; bodyOf b ] with
              | [ r ] ->
                  let applied = ofSimPoint r.Applied
                  Expect.floatClose Accuracy.high applied.Vx -2.0 "A takes half of the 4-wide separation"
                  Expect.floatClose Accuracy.high applied.Vy 0.0 "no vertical component"

                  // `resolve` applies the separation by MOVING the body's Bounds, so the post-step
                  // position comes back as a Rect. `ofSimRectCenter` is the return leg that lets a
                  // Vec2 model store it without hand-writing the bridge.
                  let movedA = ofSimRectCenter r.A.Bounds
                  Expect.floatClose Accuracy.high movedA.Vx -2.0 "A's centre moved left by half the overlap"
                  Expect.floatClose Accuracy.high movedA.Vy 0.0 "and not vertically"
              | rs -> failtestf "expected exactly one resolution for one overlapping pair, got %d" (List.length rs)
          }

          // --- SpatialGrid: the fs-gg-game-core skill's example, which did not compile before -------
          test "SpatialGrid.build keys on Vec2 positions through toSimPoint (the fs-gg-game-core example)" {
              let enemies =
                  [ entity 5.0 5.0 0.0 0.0 1.0 1
                    entity 40.0 8.0 0.0 0.0 1.0 2
                    entity 200.0 200.0 0.0 0.0 1.0 3 ]

              // This is the skill's `SpatialGrid.build 32.0 [ for e in enemies -> e.Pos, e.Id ]` line,
              // made to compile the only way it can: the Vec2 position crosses via `toSimPoint`.
              let grid =
                  FS.GG.Game.Core.SpatialGrid.build 32.0 [ for e in enemies -> toSimPoint e.Pos, e.Id ]

              let near =
                  FS.GG.Game.Core.SpatialGrid.queryRadius (toSimPoint (vec2 0.0 0.0)) 50.0 grid
                  |> List.sort

              Expect.equal near [ 1; 2 ] "the two enemies within 50 units are found; the distant one is not"
          }

          // --- Visibility: fog-of-war / line-of-sight, driven from Vec2 -----------------------------
          test "a Vec2-modelled wall occludes a Vec2-modelled target (line of sight)" {
              let eye = vec2 0.0 0.0
              let wall = segOf (vec2 5.0 -5.0) (vec2 5.0 5.0)

              Expect.isFalse
                  (Visibility.isVisible (toSimPoint eye) (toSimPoint (vec2 10.0 0.0)) [ wall ])
                  "the wall at x=5 blocks the sightline to x=10"

              Expect.isTrue
                  (Visibility.isVisible (toSimPoint eye) (toSimPoint (vec2 -10.0 0.0)) [ wall ])
                  "a target on the far side of the eye is unobstructed"
          }

          test "Visibility.polygon's vertices read back into Vec2 and stay inside the sight bound" {
              // The viewpoint is deliberately off-origin and asymmetric: it is the one value here that
              // makes the round trip `Vec2 -> toSimPoint -> poly.Source -> ofSimPoint -> Vec2` observable,
              // and (3, -7) reflects to (-7, 3), so a swapped crossing cannot survive the anchor check.
              let eye = vec2 3.0 -7.0
              let wall = segOf (vec2 8.0 -12.0) (vec2 8.0 -2.0)
              let radius = 20.0

              let poly = Visibility.polygon (sight radius) (toSimPoint eye) [ wall ]
              let vertices = poly.Vertices |> List.map ofSimPoint // back into the model's vocabulary

              Expect.isNonEmpty vertices "the angular sweep produced a visible region"
              Expect.equal (ofSimPoint poly.Source) eye "the polygon is anchored on the Vec2 viewpoint"

              Expect.all
                  vertices
                  (fun v -> abs (v.Vx - eye.Vx) <= radius + 1e-6 && abs (v.Vy - eye.Vy) <= radius + 1e-6)
                  "every vertex lies inside the square sight bound around the eye"
          }

          // --- determinism: the crossing is pure arithmetic, safe in a replayed `update` ------------
          test "a full Vec2 -> sim -> Vec2 round trip is byte-identical across runs" {
              let step () =
                  let bodies = [ bodyOf (entity 0.0 0.0 1.5 -2.5 10.0 1); bodyOf (entity 6.0 0.0 0.0 0.0 10.0 2) ]
                  let resolutions = Collision.step Collision.SeparateEqually 32.0 bodies
                  let pushes = resolutions |> List.map (fun r -> ofSimPoint r.Applied)
                  let moved = resolutions |> List.map (fun r -> ofSimRectCenter r.A.Bounds)
                  // Read the velocities back too, so every crossing this file ships is on the round trip
                  // rather than merely constructed and discarded.
                  let vels = bodies |> List.map (fun b -> ofSimPoint b.Velocity)
                  let poly =
                      Visibility.polygon (sight 20.0) (toSimPoint (vec2 3.0 -7.0)) [ segOf (vec2 8.0 -12.0) (vec2 8.0 -2.0) ]
                  pushes, moved, vels, poly.Vertices |> List.map ofSimPoint

              Expect.equal (step ()) (step ()) "identical inputs -> identical outputs"
          } ]
