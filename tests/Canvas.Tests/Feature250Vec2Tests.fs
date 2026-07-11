module Canvas.Tests.Feature250Vec2Tests

// Feature 250 (US1): the collision-safe Vec2 helper source (template/fragments/vec2/src/Product/
// Vec2.fs) is compiled here (literal `namespace AppRoot`, the default sourceName). Vec2's labels
// (Vx/Vy) reuse NONE of Scene.Point (X,Y) / Scene.Rect (X,Y,Width,Height); toPoint/toRect cross into
// the shared scene vocabulary. All real pure computation — no synthetic evidence. Covers the
// data-model laws: interop (FR-009), totals (FR-001/FR-002), and determinism (SC-002 support).

open Expecto
open FsCheck
open FS.GG.UI.Scene
open AppRoot
open AppRoot.Geometry

// The algebraic laws (commutativity, identity) are stated over the finite reals a game model actually
// carries; FsCheck also generates NaN/inf, for which the guard returns true (the ops are documented
// total on non-finite input, but equality laws under NaN don't hold, so those cases are out of scope).
let private finite x = not (System.Double.IsNaN x) && not (System.Double.IsInfinity x)

[<Tests>]
let tests =
    testList
        "Feature 250 Vec2 collision-safe helper (US1)"
        [

          // --- interop laws (FR-009) ---------------------------------------------------------------
          test "toPoint copies the components straight into Scene.Point" {
              let p = toPoint (vec2 3.0 -4.0)
              Expect.equal p { X = 3.0; Y = -4.0 } "toPoint = { X = Vx; Y = Vy }"
          }

          test "toRect is a centered AABB of the given size (size case, FR-002)" {
              let r = toRect (vec2 10.0 20.0) 8.0 6.0
              Expect.equal r { X = 6.0; Y = 17.0; Width = 8.0; Height = 6.0 } "centered on the vector"
          }

          test "toRect treats a negative size as its magnitude (total)" {
              let r = toRect (vec2 0.0 0.0) -8.0 -6.0
              Expect.equal r { X = -4.0; Y = -3.0; Width = 8.0; Height = 6.0 } "abs of size, no inverted rect"
          }

          // --- sim-space interop (#446): the SECOND vocabulary, FS.GG.Game.Core --------------------
          // The collision/visibility helpers the same scaffold ships speak Game.Core's Point/Rect, not
          // Scene's. These crossings are what let a Vec2-modelled product call them at all. The laws
          // mirror the scene crossings above, because the two vocabularies are label-identical.
          test "toSimPoint copies the components straight into a Game.Core.Point" {
              let p = toSimPoint (vec2 3.0 -4.0)
              Expect.equal (p.X, p.Y) (3.0, -4.0) "toSimPoint = (X = Vx, Y = Vy)"
          }

          test "ofSimPoint inverts toSimPoint (a round trip through sim space is the identity)" {
              Check.One(
                  Config.QuickThrowOnFailure.WithMaxTest 500,
                  fun (x: float) (y: float) ->
                      not (finite x && finite y)
                      || ofSimPoint (toSimPoint (vec2 x y)) = vec2 x y)
          }

          test "toSimRect is a centered AABB of the given size (the sim twin of toRect)" {
              let r = toSimRect (vec2 10.0 20.0) 8.0 6.0
              Expect.equal (r.X, r.Y, r.Width, r.Height) (6.0, 17.0, 8.0, 6.0) "centered on the vector"
          }

          test "toSimRect treats a negative size as its magnitude (total)" {
              let r = toSimRect (vec2 0.0 0.0) -8.0 -6.0
              Expect.equal (r.X, r.Y, r.Width, r.Height) (-4.0, -3.0, 8.0, 6.0) "abs of size, no inverted rect"
          }

          // The return leg of toSimRect. `Collision.resolve` separates a pair by MOVING the body's
          // Bounds, so a model that stores positions as Vec2 gets its new position back as a Rect.
          test "ofSimRectCenter recovers the centre of a Game.Core.Rect" {
              let r = toSimRect (vec2 10.0 20.0) 8.0 6.0
              Expect.equal (ofSimRectCenter r) (vec2 10.0 20.0) "ofSimRectCenter inverts toSimRect's centering"
          }

          // Left inverse only UP TO ROUNDING, and only over a sane coordinate range: the round trip is
          // `(x - w/2) + w/2`, which is not exactly invertible in binary floating point, and at the far
          // end of the double range `x - w/2` can overflow to infinity outright. Both bounds below are
          // therefore load-bearing — an unbounded absolute-tolerance version of this property is false,
          // and FsCheck finds the counterexample immediately.
          test "ofSimRectCenter is toSimRect's left inverse (up to rounding) over a game-sized range" {
              let inRange (v: float) = finite v && abs v <= 1.0e6

              Check.One(
                  Config.QuickThrowOnFailure.WithMaxTest 500,
                  fun (x: float) (y: float) (w: float) (h: float) ->
                      not (inRange x && inRange y && inRange w && inRange h)
                      || (let back = ofSimRectCenter (toSimRect (vec2 x y) w h)
                          let tol v size = 1.0e-9 * (1.0 + abs v + abs size)
                          abs (back.Vx - x) <= tol x w && abs (back.Vy - y) <= tol y h))
          }

          // The two crossings must not disagree: same numbers, distinct (nominally) types. A swapped
          // component in one of them would show up here and nowhere else.
          test "the sim crossing agrees componentwise with the scene crossing" {
              let v = vec2 -2.5 7.25
              let sp = toSimPoint v
              let rp = toPoint v
              Expect.equal (sp.X, sp.Y) (rp.X, rp.Y) "same coordinates in both vocabularies"

              let sr = toSimRect v 4.0 2.0
              let rr = toRect v 4.0 2.0
              Expect.equal
                  (sr.X, sr.Y, sr.Width, sr.Height)
                  (rr.X, rr.Y, rr.Width, rr.Height)
                  "same centered AABB in both vocabularies"
          }

          // --- algebraic laws ----------------------------------------------------------------------
          test "add identity: add v zero = v" {
              let v = vec2 2.5 -7.0
              Expect.equal (add v zero) v "zero is the additive identity"
          }

          test "add is commutative over finite vectors" {
              Check.One(
                  Config.QuickThrowOnFailure.WithMaxTest 500,
                  fun (ax: float) (ay: float) (bx: float) (by: float) ->
                      not (finite ax && finite ay && finite bx && finite by)
                      || add (vec2 ax ay) (vec2 bx by) = add (vec2 bx by) (vec2 ax ay))
          }

          test "scale by 1 is identity; scale by 0 is zero" {
              let v = vec2 -3.0 9.0
              Expect.equal (scale 1.0 v) v "scale 1 = id"
              Expect.equal (scale 0.0 v) zero "scale 0 = zero"
          }

          test "sub is add of the negation" {
              let a = vec2 5.0 1.0
              let b = vec2 2.0 -3.0
              Expect.equal (sub a b) (add a (scale -1.0 b)) "a - b = a + (-b)"
          }

          // --- clamp totality (FR-001, keep an entity inside a bound) -------------------------------
          test "clamp keeps a vector inside [lo, hi] per component" {
              let lo = vec2 0.0 0.0
              let hi = vec2 100.0 50.0
              Expect.equal (clamp lo hi (vec2 -10.0 200.0)) (vec2 0.0 50.0) "clamped to bounds"
              Expect.equal (clamp lo hi (vec2 30.0 25.0)) (vec2 30.0 25.0) "in-range unchanged"
          }

          test "clamp is total on a degenerate bound (lo > hi): low bound wins, no throw" {
              let r = clamp (vec2 10.0 10.0) (vec2 0.0 0.0) (vec2 5.0 5.0)
              Expect.equal r (vec2 10.0 10.0) "degenerate bound clamps to lo without crashing"
          }

          // --- determinism (SC-002 support) --------------------------------------------------------
          test "every op is byte-identical on a repeated fixed scenario" {
              let step () =
                  let p = vec2 3.0 4.0
                  let v = vec2 -1.5 2.0
                  let p' = clamp zero (vec2 640.0 480.0) (add p (scale 0.5 v))
                  toPoint p', toRect p' 12.0 12.0
              Expect.equal (step ()) (step ()) "identical inputs -> identical outputs"
          } ]
