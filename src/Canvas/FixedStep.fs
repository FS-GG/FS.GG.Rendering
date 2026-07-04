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
        if interval <= 0.0 then
            // No positive interval ⇒ cannot form a step; carry the accumulator unchanged.
            struct (0, accumulator)
        else
            // Clamp the frame to [0, maxFrameTime] so a stalled frame cannot spiral, then take whole
            // steps by closed-form floor division and carry the sub-step remainder (no drain loop).
            let clamped = min maxFrameTime (max 0.0 frameTime)
            let total = accumulator + clamped
            let steps = int (floor (total / interval))
            struct (steps, total - float steps * interval)

    let drain (interval: float) (frameTime: float) (accumulator: float) : struct (int * float) =
        drainWith defaultMaxFrameTime interval frameTime accumulator
