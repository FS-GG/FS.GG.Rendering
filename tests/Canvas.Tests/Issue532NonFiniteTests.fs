module Canvas.Tests.Issue532NonFiniteTests

// FS-GG/FS.GG.Rendering#532 — `Vec2.fs`'s header used to promise "straight-line float arithmetic
// guarded against non-finite input (never throws, never yields NaN silently)". It had no finite guard
// at all, so the file stated the exact opposite of what it did, in the one sentence a consumer reads to
// decide whether they need their own guards. The resolution was (b): make the DOC true — these helpers
// propagate non-finite input, and sanitising it is the product's job — plus an opt-in `isFinite` seam.
//
// Writing THIS file is what turned up the sting in the tail: `clamp` is the one helper that does NOT
// propagate. Every comparison against a NaN is false, so `max`/`min` fall through and a NaN component
// lands on `lo` — the file's most guard-LOOKING function silently snaps a broken position to the corner
// of the bound. A blanket "everything here propagates" would have been a fresh false claim of exactly
// the kind #532 exists to kill, so the header now names `clamp` as a trap and the suite pins it.
//
// THIS FILE IS WHY THE DOC CANNOT DRIFT BACK. The false claim survived because nothing tested the
// non-finite path: `Feature250Vec2Tests` states its algebraic laws over a `finite` precondition, and its
// own comment ASSUMES "the ops are documented total on non-finite input" — so the only suite over this
// file filtered out the exact inputs the claim was about. Every test below asserts the behaviour the
// header now documents, so if a future edit re-introduces a silent guard (a NaN quietly becoming 0.0),
// these fail rather than the doc going quietly false again.
//
// The load-bearing test is the last one, and it is the whole reason this is not a doc nit: an unguarded
// NaN position does not throw and does not crash the step. It makes `Collision` STOP SEEING THE ENTITY —
// which is a bug that reads, from the player's seat, as walking through walls. All real pure
// computation — no synthetic evidence.

open Expecto
open FS.GG.UI.Scene
open AppRoot
open AppRoot.Geometry

// `nan` and `infinity` are FSharp.Core built-ins — no local aliases needed.
let private inf = infinity

// A product entity modelled the way the scaffold instructs (position as `Vec2`, never an X/Y label of
// its own), crossed into the simulation vocabulary exactly as `Issue446SimCrossingTests` does — via
// `toSimRect`/`toSimPoint` plus the return-type annotation, with no `open FS.GG.Game.Core` in sight.
// The crossing is the point: this is the path a real product's NaN travels down.
let private bodyAt (center: Vec2) (size: float) (tag: int) : Collision.Body<int> =
    { Bounds = toSimRect center size size
      Velocity = toSimPoint zero
      Tag = tag }

[<Tests>]
let tests =
    testList
        "#532 Vec2 propagates non-finite input — except `clamp`, which swallows a NaN into `lo`"
        [

          // --- the arithmetic propagates (FR: the header's new claim) -------------------------------
          test "add propagates a NaN component rather than guarding it" {
              let r = add (vec2 nan 0.0) (vec2 1.0 2.0)
              Expect.isTrue (System.Double.IsNaN r.Vx) "NaN + 1.0 stays NaN — add does not sanitise"
              Expect.equal r.Vy 2.0 "the finite component is unaffected"
          }

          test "sub propagates a NaN component rather than guarding it" {
              let r = sub (vec2 1.0 nan) (vec2 1.0 2.0)
              Expect.equal r.Vx 0.0 "the finite component is unaffected"
              Expect.isTrue (System.Double.IsNaN r.Vy) "NaN - 2.0 stays NaN — sub does not sanitise"
          }

          test "scale propagates an infinity rather than guarding it" {
              let r = scale inf (vec2 2.0 0.0)
              Expect.isTrue (System.Double.IsInfinity r.Vx) "inf * 2.0 stays infinite — scale does not sanitise"
              // inf * 0.0 is NaN by IEEE-754. Worth pinning: the *finite* component of a vector scaled by
              // an infinity does not merely go infinite, it goes NaN — the scale case that surprises people.
              Expect.isTrue (System.Double.IsNaN r.Vy) "inf * 0.0 = NaN (IEEE-754), not 0.0"
          }

          // --- the crossings carry it into BOTH vocabularies ----------------------------------------
          test "toPoint carries a NaN into the render vocabulary" {
              let p = toPoint (vec2 nan 0.0)
              Expect.isTrue (System.Double.IsNaN p.X) "Scene.Point.X = NaN — the crossing does not sanitise"
          }

          test "toSimPoint carries a NaN into the simulation vocabulary" {
              let p = toSimPoint (vec2 nan 0.0)
              Expect.isTrue (System.Double.IsNaN p.X) "Game.Core.Point.X = NaN — the crossing does not sanitise"
          }

          test "toRect/toSimRect carry a NaN through the size guard (abs nan = nan)" {
              // The centered-AABB helper DOES guard the SIZE (negative sizes become their magnitude), and
              // that `abs` is exactly what makes the position case so easy to misread as guarded. It is
              // not: `abs nan = nan`, and the origin is derived from the centre, so a NaN centre lands in
              // the rect regardless of how well-behaved the size is.
              let r: Rect = toRect (vec2 nan 0.0) 8.0 6.0
              Expect.isTrue (System.Double.IsNaN r.X) "Scene.Rect.X = NaN from a NaN centre"
              Expect.equal r.Width 8.0 "the size is still guarded — only the position propagates"

              let sr = toSimRect (vec2 nan 0.0) 8.0 6.0
              Expect.isTrue (System.Double.IsNaN sr.X) "Game.Core.Rect.X = NaN from a NaN centre"
              Expect.equal sr.Width 8.0 "the size is still guarded — only the position propagates"
          }

          test "a NaN SIZE propagates too — the `abs` guard does not stop it (abs nan = nan)" {
              // The header claims this explicitly, so it is pinned explicitly. `centeredBox` guards the
              // size with `abs`, which makes a NEGATIVE size total — but `abs nan = nan`, so the guard
              // that saves you from a stray sign does nothing about a bad float. Both vocabularies.
              let r: Rect = toRect (vec2 0.0 0.0) nan 6.0
              Expect.isTrue (System.Double.IsNaN r.Width) "Scene.Rect.Width = NaN — abs nan = nan"
              Expect.isTrue (System.Double.IsNaN r.X) "and it reaches the origin too (centre - nan/2)"

              let sr = toSimRect (vec2 0.0 0.0) nan 6.0
              Expect.isTrue (System.Double.IsNaN sr.Width) "Game.Core.Rect.Width = NaN — abs nan = nan"
          }

          // --- clamp is the EXCEPTION, and the header calls it a trap --------------------------------
          test "clamp SWALLOWS a NaN into `lo` — it does not propagate, and that is the trap" {
              // This is the one helper in the file that does not propagate, and it is the most dangerous
              // one precisely because it looks like a guard. Every comparison against a NaN is false, so
              // `max`/`min` fall through and the component lands on `lo`. A position that silently went
              // bad therefore silently SNAPS TO THE CORNER of the bound and keeps playing, rather than
              // staying visibly NaN. If someone ever "fixes" clamp to propagate, this test says so.
              let lo = vec2 -10.0 -10.0
              let hi = vec2 10.0 10.0

              let r = clamp lo hi (vec2 nan 0.0)
              Expect.isFalse (System.Double.IsNaN r.Vx) "clamp does NOT propagate the NaN"
              Expect.equal r.Vx -10.0 "the NaN component falls out as `lo`, not as NaN — the trap"
              Expect.equal r.Vy 0.0 "the finite component clamps normally"

              // An infinity, by contrast, clamps sensibly — so clamp launders exactly ONE of the two
              // non-finite failure modes, which is why it cannot be your finite guard.
              let p = clamp lo hi (vec2 inf System.Double.NegativeInfinity)
              Expect.equal p.Vx 10.0 "+infinity clamps to `hi` (sensible)"
              Expect.equal p.Vy -10.0 "-infinity clamps to `lo` (sensible)"
          }

          // --- the seam the header sends you to ------------------------------------------------------
          test "isFinite is the guard: it rejects NaN and both infinities, and accepts finite vectors" {
              Expect.isTrue (isFinite (vec2 3.0 -4.0)) "a finite vector is finite"
              Expect.isTrue (isFinite zero) "the zero vector is finite"
              Expect.isFalse (isFinite (vec2 nan 0.0)) "a NaN in Vx is caught"
              Expect.isFalse (isFinite (vec2 0.0 nan)) "a NaN in Vy is caught"
              Expect.isFalse (isFinite (vec2 inf 0.0)) "a positive infinity is caught"
              Expect.isFalse (isFinite (vec2 0.0 System.Double.NegativeInfinity)) "a negative infinity is caught"
          }

          // --- WHY IT MATTERS: the silent failure the header now warns about -------------------------
          test "a NaN position silently turns collision OFF — the contact vanishes, nothing throws" {
              // Two bodies that unambiguously overlap: same 10x10 box, centres 4 apart.
              let a = bodyAt (vec2 0.0 0.0) 10.0 1
              let b = bodyAt (vec2 4.0 0.0) 10.0 2

              // The control: with finite positions this pair DOES collide. Without this assertion the
              // test below would pass just as happily against a fixture that never overlapped at all.
              Expect.isSome (Collision.contact a b) "control: the finite pair overlaps and reports a contact"

              // Now poison ONE component of ONE position — a division that was not proven non-zero, a
              // parsed save file, an impulse off a bad delta. This is the whole defect in three lines.
              let poisoned = bodyAt (vec2 nan 0.0) 10.0 1

              // It does not throw. It does not report a contact either. `Collision` documents non-finite
              // bounds as never overlapping (every comparison against a NaN is false), so the entity is
              // simply no longer there as far as collision is concerned — it walks through `b`.
              Expect.isNone
                  (Collision.contact poisoned b)
                  "a NaN position produces NO contact — collision is silently off for that entity"

              // And the guard the header sends you to is what would have caught it at the boundary.
              Expect.isFalse (isFinite (vec2 nan 0.0)) "isFinite rejects the position that made the contact vanish"
          }
        ]
