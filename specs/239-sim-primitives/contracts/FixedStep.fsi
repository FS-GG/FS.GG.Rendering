// CONTRACT SKETCH for src/Canvas/FixedStep.fsi — the intended public surface (Phase 1).
// Pure, closed-form accumulator drain (research D5). Seconds throughout (matching Loop.advance).
// The lower-level primitive beneath the existing stateful `Loop.advance`.
namespace FS.GG.UI.Canvas

/// Public contract module exposed by this FS.GG.UI package.
/// Fixed-timestep accumulator: given a fixed `interval`, an elapsed `frameTime`, and a carried
/// `accumulator` (all in SECONDS), returns the whole number of fixed steps to run this frame and
/// the new carried accumulator. Pure and deterministic — no wall-clock read.
[<RequireQualifiedAccess>]
module FixedStep =

    /// Default spiral-of-death clamp (seconds) — matches `Loop.advance` (0.25). A single stalled
    /// frame cannot produce more than `floor((accumulator + 0.25) / interval)` steps.
    val defaultMaxFrameTime: float

    /// Drain with the default clamp (`defaultMaxFrameTime`).
    /// `interval <= 0` yields `struct(0, accumulator)`; `frameTime <= 0` yields `struct(0, accumulator)`.
    /// Returns `struct(stepCount, newAccumulator)` with `stepCount >= 0` and
    /// `0 <= newAccumulator < interval` (for `interval > 0`).
    val drain: interval: float -> frameTime: float -> accumulator: float -> struct (int * float)

    /// Drain with an explicit spiral-of-death clamp `maxFrameTime` (seconds), e.g. a tighter 0.05.
    val drainWith:
        maxFrameTime: float -> interval: float -> frameTime: float -> accumulator: float ->
            struct (int * float)
