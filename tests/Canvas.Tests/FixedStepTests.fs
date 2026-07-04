module Canvas.Tests.FixedStepTests

// Feature 239 (US3): pure fixed-timestep accumulator drain. `drain interval frameTime acc` returns
// struct(steps, newAcc). Deterministic, clamps a runaway frame (default 0.25s = Loop.advance's cap),
// total on degenerate input. dt values are negative powers of two so accumulator arithmetic is exact.

open Expecto
open FsCheck
open FS.GG.UI.Canvas

[<Tests>]
let tests =
    testList "Feature 239 FixedStep drain (US3, FR-009/FR-010)" [

        test "default clamp matches Loop.advance (0.25s)" {
            Expect.equal FixedStep.defaultMaxFrameTime 0.25 "one canonical clamp across the Canvas package"
        }

        test "an exact multiple of the interval runs that many whole steps with no remainder" {
            // interval = 1/16, frameTime = 0.25 = 4 intervals ⇒ 4 steps, remainder 0.
            let struct (steps, rem) = FixedStep.drain 0.0625 0.25 0.0
            Expect.equal steps 4 "four whole steps"
            Expect.equal rem 0.0 "no remainder"
        }

        test "a carried accumulator is included in the drain" {
            // 1/32 banked + 0.25 frame at interval 1/16 ⇒ still 4 steps, 1/32 carried.
            let struct (steps, rem) = FixedStep.drain 0.0625 0.25 0.03125
            Expect.equal steps 4 "the banked sub-step does not add a whole step"
            Expect.equal rem 0.03125 "the remainder is carried forward"
        }

        test "a sub-interval frame runs zero steps and grows the accumulator" {
            let struct (steps, rem) = FixedStep.drain 0.0625 0.03125 0.0
            Expect.equal steps 0 "not enough time for a step"
            Expect.equal rem 0.03125 "accumulator grows by the frame time"
        }

        test "a runaway frame is clamped (no spiral of death)" {
            // interval 1/16, frameTime 5.0 ⇒ unclamped 80 steps; clamped to 0.25 ⇒ exactly 4.
            let struct (steps, _) = FixedStep.drain 0.0625 5.0 0.0
            Expect.equal steps 4 "clamp caps injected time at 0.25s ⇒ 4 steps, not 80"
        }

        test "a non-positive frame time contributes nothing new (a sub-interval accumulator is preserved)" {
            // acc 1/32 < interval 1/16, so with no new time no step can run and the accumulator is kept.
            Expect.equal (FixedStep.drain 0.0625 -3.0 0.03125) (struct (0, 0.03125)) "negative dt ⇒ no steps, accumulator preserved"
            Expect.equal (FixedStep.drain 0.0625 0.0 0.03125) (struct (0, 0.03125)) "zero dt ⇒ no steps, accumulator preserved"
        }

        test "a non-positive frame still drains a pre-banked accumulator that already holds ≥ one interval" {
            // acc 0.1 > interval 1/16: even with zero new time the banked step must run (correct fixed-step).
            let struct (steps, rem) = FixedStep.drain 0.0625 0.0 0.1
            Expect.equal steps 1 "the already-banked interval drains"
            Expect.floatClose Accuracy.high rem 0.0375 "carrying the sub-step remainder"
        }

        test "a non-positive interval is a no-op (no divide-by-zero / infinite steps)" {
            Expect.equal (FixedStep.drain 0.0 1.0 0.2) (struct (0, 0.2)) "interval = 0 ⇒ struct(0, acc)"
            Expect.equal (FixedStep.drain -0.5 1.0 0.2) (struct (0, 0.2)) "interval < 0 ⇒ struct(0, acc)"
        }

        test "drain is deterministic for identical arguments" {
            let run () = FixedStep.drain 0.125 1.0 0.0
            Expect.equal (run ()) (run ()) "same inputs ⇒ identical struct(steps, rem)"
        }

        test "drainWith uses an explicit tighter clamp than the 0.25 default" {
            // interval 1/16; frameTime 1.0. Default clamp 0.25 ⇒ 4 steps. Explicit 0.05 ⇒ floor(0.05/0.0625)=0.
            let struct (dfltSteps, _) = FixedStep.drain 0.0625 1.0 0.0
            let struct (tightSteps, _) = FixedStep.drainWith 0.05 0.0625 1.0 0.0
            Expect.equal dfltSteps 4 "default 0.25 clamp"
            Expect.equal tightSteps 0 "0.05 clamp caps below one interval ⇒ zero steps"
        }

        testCase "conservation + bounds: newAcc = (acc + clamp dt) - steps*interval ∈ [0, interval) (FsCheck ≥1000)"
        <| fun () ->
            let prop (i: int) (f: int) (a: int) =
                let interval = float (1 + abs (i % 64)) / 64.0 // (0, 1]
                let frameTime = float (f % 500) / 100.0 // may be negative or huge
                let acc = float (abs (a % 64)) / 64.0
                let struct (steps, newAcc) = FixedStep.drain interval frameTime acc
                let clamped = min FixedStep.defaultMaxFrameTime (max 0.0 frameTime)
                let expected = (acc + clamped) - float steps * interval
                steps >= 0
                && abs (newAcc - expected) < 1e-9
                && newAcc >= -1e-9
                && newAcc < interval + 1e-9
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 1000, prop)

        testCase "clamp bound: steps never exceed floor((acc + maxClamp)/interval) (FsCheck ≥1000)"
        <| fun () ->
            let prop (i: int) (f: int) (a: int) =
                let interval = float (1 + abs (i % 64)) / 64.0
                let frameTime = float (f % 100000) // deliberately huge
                let acc = float (abs (a % 64)) / 64.0
                let struct (steps, _) = FixedStep.drain interval frameTime acc
                let bound = int (floor ((acc + FixedStep.defaultMaxFrameTime) / interval))
                steps <= bound
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 1000, prop)
    ]
