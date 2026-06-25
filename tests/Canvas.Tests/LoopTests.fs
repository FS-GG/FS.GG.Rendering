module Canvas.Tests.LoopTests

// Feature 191 (US3, T033, C4/FR-009/FR-011, SC-006): the fixed-timestep loop is deterministic, runs
// `floor((acc + clamp frameTime)/dt)` whole steps, clamps a runaway frame to 0.25s, and never reads a
// wall clock. dt values are negative powers of two so the accumulator arithmetic is exact (no FP drift).

open Expecto
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
    ]
