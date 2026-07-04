namespace FS.GG.UI.Canvas

[<RequireQualifiedAccess>]
module FixedStep =

    // Same spiral-of-death cap as Loop.advance — one clamp for the whole Canvas package.
    let defaultMaxFrameTime = 0.25

    let drainWith
        (maxFrameTime: float)
        (interval: float)
        (frameTime: float)
        (accumulator: float)
        : struct (int * float) =
        // Totality: the documented invariants (steps >= 0, 0 <= newAccumulator < interval) must hold for
        // EVERY input. A single non-finite value (a NaN `dt` from a first-frame or timer glitch) must
        // never poison the accumulator and permanently wedge the loop, and a nonsense accumulator must
        // never yield a negative step count — so each operand is sanitized before the arithmetic.
        let acc =
            if System.Double.IsFinite accumulator && accumulator > 0.0 then accumulator else 0.0

        if not (System.Double.IsFinite interval) || interval <= 0.0 then
            struct (0, acc)
        else
            // A non-finite frame contributes nothing; a runaway frame is capped by maxFrameTime (itself
            // sanitized), so catch-up can never spiral.
            let cap =
                if System.Double.IsFinite maxFrameTime && maxFrameTime > 0.0 then maxFrameTime else 0.0

            let frame = if System.Double.IsFinite frameTime then max 0.0 frameTime else 0.0
            let total = acc + min cap frame
            // Closed-form whole steps (no drain loop); cap at Int32.MaxValue so a pathologically tiny
            // interval can never wrap the count negative.
            let stepsF = floor (total / interval)
            let steps = if stepsF >= 2147483647.0 then 2147483647 else int stepsF
            struct (steps, total - float steps * interval)

    let drain (interval: float) (frameTime: float) (accumulator: float) : struct (int * float) =
        drainWith defaultMaxFrameTime interval frameTime accumulator
