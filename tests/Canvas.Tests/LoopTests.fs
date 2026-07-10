module Canvas.Tests.LoopTests

// Feature 191 (US3, T033, C4/FR-009/FR-011, SC-006): the fixed-timestep loop is deterministic, runs
// `floor((acc + clamp frameTime)/dt)` whole steps, clamps a runaway frame to 0.25s, and never reads a
// wall clock. dt values are negative powers of two so the accumulator arithmetic is exact (no FP drift).
//
// `FS.GG.UI.Canvas.Loop` is deprecated (ADR-0104, #269) but still SHIPPED, so it stays under test until
// it is removed at the next Canvas major. Suppressing the deprecation here is the point: these tests
// assert the behaviour consumers still get. The migration target's own suite lives in FS.GG.Game's
// Game.Core.Tests — this file does not duplicate it.
#nowarn "44"

open Expecto
open FsCheck
open FS.GG.UI.Canvas

// integrate counts how many fixed steps ran; the step value (dt) is recorded too.
let private bump (w: int) (_dt: float) = w + 1

[<Tests>]
let tests =
    testList "Feature 191 Loop fixed-timestep (US3, FR-009/FR-011)" [

        test "init seeds Previous = Current and a zero accumulator" {
            let s = Loop.init 7
            Expect.equal s.Current 7 "Current is the seed"
            Expect.equal s.Previous 7 "Previous equals Current at init"
            Expect.equal s.Accumulator 0.0 "accumulator starts empty"
        }

        test "advance runs floor((acc + frameTime)/dt) whole steps and carries the remainder" {
            // dt = 1/16, frameTime = 0.25 (= 4 dt) ⇒ 4 steps, no remainder.
            let s = Loop.advance 0.0625 bump 0.25 (Loop.init 0)
            Expect.equal s.Current 4 "four whole steps ran"
            Expect.equal s.Previous 3 "Previous is the second-to-last world"
            Expect.equal s.Accumulator 0.0 "0.25 / (1/16) leaves no remainder"
            // A carried sub-step: start with 1/32 banked ⇒ 4 steps consume 0.25, 1/32 remains.
            let r = Loop.advance 0.0625 bump 0.25 { Current = 0; Previous = 0; Accumulator = 0.03125 }
            Expect.equal r.Current 4 "the banked remainder does not add a whole step here"
            Expect.equal r.Accumulator 0.03125 "the sub-step remainder is carried forward"
        }

        test "a runaway frameTime is clamped to 0.25s (no spiral of death)" {
            // dt = 1/16, frameTime = 5.0 ⇒ unclamped this is 80 steps; clamped to 0.25 it is exactly 4.
            let s = Loop.advance 0.0625 bump 5.0 (Loop.init 0)
            Expect.equal s.Current 4 "clamp caps the injected time at 0.25s ⇒ four steps, not eighty"
            Expect.equal s.Accumulator 0.0 "0.25s / (1/16) leaves no remainder"
        }

        test "a negative or zero frameTime advances nothing and is clamped to 0" {
            let s = Loop.advance 0.25 bump -3.0 (Loop.init 9)
            Expect.equal s.Current 9 "no steps run for a non-positive frame time"
            Expect.equal s.Accumulator 0.0 "accumulator unchanged"
        }

        test "a non-positive dt is a no-op (guards against divide-by-zero / infinite loop)" {
            let s0 = { Current = 4; Previous = 3; Accumulator = 0.1 }
            Expect.equal (Loop.advance 0.0 bump 1.0 s0) s0 "dt <= 0 returns the state unchanged"
        }

        test "advance is deterministic: identical arguments yield an identical StepState" {
            let run () = Loop.advance 0.125 bump 1.0 (Loop.init 0)
            Expect.equal (run ()) (run ()) "same inputs ⇒ byte-identical StepState (no wall-clock read)"
        }

        test "alpha is Accumulator/dt and lands in [0,1)" {
            let s = { Current = 0; Previous = 0; Accumulator = 0.25 }
            Expect.equal (Loop.alpha 0.5 s) 0.5 "alpha = accumulator / dt"
            // After advance, the accumulator is always < dt, so alpha < 1.
            let advanced = Loop.advance 0.5 bump 1.25 (Loop.init 0)
            let a = Loop.alpha 0.5 advanced
            Expect.isTrue (a >= 0.0 && a < 1.0) "alpha stays in [0,1) after advance"
        }

        // #266: F#'s `min`/`max` propagate NaN, so the old `max 0.0 (min frameTime maxFrameTime)`
        // turned one NaN frame into a NaN accumulator. `acc >= dt` is then false forever and the
        // only writer of `acc` sits inside that loop — the simulation stopped stepping for good.
        // `FixedStep.drain` (FS.GG.Game.Core) already documents these totals; `Loop` now matches.

        test "a NaN frameTime contributes nothing, and the next finite frame steps normally" {
            let poisoned = Loop.advance 0.0625 bump nan (Loop.init 0)
            Expect.equal poisoned.Current 0 "a NaN frame runs no steps"
            Expect.equal poisoned.Accumulator 0.0 "the NaN never reaches the accumulator"
            // The latch: before the fix every later frame was dead too, whatever its frameTime.
            let recovered = Loop.advance 0.0625 bump 0.25 poisoned
            Expect.equal recovered.Current 4 "the loop recovers — four steps on the next good frame"
        }

        test "an infinite frameTime contributes nothing rather than latching or clamping" {
            let up = Loop.advance 0.0625 bump infinity (Loop.init 0)
            Expect.equal up.Current 0 "+infinity is not a 0.25s frame; it contributes nothing"
            Expect.equal up.Accumulator 0.0 "accumulator stays empty"
            let down = Loop.advance 0.0625 bump -infinity (Loop.init 0)
            Expect.equal down.Current 0 "-infinity advances nothing"
            Expect.equal down.Accumulator 0.0 "accumulator stays empty"
        }

        test "a non-finite accumulator on the incoming state is treated as empty" {
            // Defence in depth: a state fabricated (or persisted) with a poisoned accumulator heals.
            let s = Loop.advance 0.0625 bump 0.25 { Current = 0; Previous = 0; Accumulator = nan }
            Expect.equal s.Current 4 "the NaN accumulator is dropped, the 0.25s frame still steps"
            Expect.equal s.Accumulator 0.0 "and the carried remainder is finite"
            let neg = Loop.advance 0.0625 bump 0.0625 { Current = 0; Previous = 0; Accumulator = -5.0 }
            Expect.equal neg.Current 1 "a negative accumulator is empty, not a step debt"
        }

        test "a non-finite dt is a no-op, like a non-positive one" {
            let s0 = { Current = 4; Previous = 3; Accumulator = 0.1 }
            // `nan <= 0.0` is false, so the old guard fell through into the loop.
            Expect.equal (Loop.advance nan bump 1.0 s0) s0 "dt = NaN returns the state unchanged"
            Expect.equal (Loop.advance infinity bump 1.0 s0) s0 "dt = infinity returns the state unchanged"
        }

        test "alpha never returns NaN" {
            let poisoned = { Current = 0; Previous = 0; Accumulator = nan }
            Expect.equal (Loop.alpha 0.5 poisoned) 0.0 "a NaN accumulator interpolates at 0, not NaN"
            let negative = { Current = 0; Previous = 0; Accumulator = -1.0 }
            Expect.equal (Loop.alpha 0.5 negative) 0.0 "a negative accumulator interpolates at 0"
            let good = { Current = 0; Previous = 0; Accumulator = 0.25 }
            Expect.equal (Loop.alpha nan good) 0.0 "a NaN dt yields 0, not NaN"
            Expect.equal (Loop.alpha infinity good) 0.0 "an infinite dt yields 0"
        }

        test "property: any frameTime sequence (non-finite included) keeps Accumulator in [0,dt)" {
            let dt = 0.0625

            Check.One(
                Config.QuickThrowOnFailure.WithMaxTest 500,
                // FsCheck's float generator emits NaN and ±infinity, which is exactly the point here.
                fun (frameTimes: float list) ->
                    let final =
                        frameTimes
                        |> List.fold (fun st ft -> Loop.advance dt bump ft st) (Loop.init 0)

                    let acc = final.Accumulator
                    System.Double.IsFinite acc && acc >= 0.0 && acc < dt
                    && System.Double.IsFinite(Loop.alpha dt final))
        }
    ]
