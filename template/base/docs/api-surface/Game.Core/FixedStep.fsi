namespace FS.GG.Game.Core

/// Public contract module exposed by the FS.GG.Game.Core package.
/// Fixed-timestep accumulator drain: given a fixed `interval`, an elapsed `frameTime`, and a carried
/// `accumulator` (all in SECONDS), returns the whole number of fixed steps to run this frame and the
/// new carried accumulator. Pure and deterministic — no wall-clock read — so a scripted `frameTime`
/// sequence reproduces identical results. The lower-level primitive a stateful game loop drives.
[<RequireQualifiedAccess>]
module FixedStep =

    /// Public contract value exposed by the FS.GG.Game.Core package.
    /// Default spiral-of-death clamp in seconds (0.25). A single stalled frame runs at most
    /// `floor((accumulator + 0.25) / interval)` steps.
    val defaultMaxFrameTime: float

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Drain with the default clamp (`defaultMaxFrameTime`). Returns `struct(stepCount, newAccumulator)`
    /// with `stepCount >= 0` and `0 <= newAccumulator < interval` for `interval > 0`. Total on every
    /// input: a non-positive or non-finite `interval` yields `struct(0, accumulator)`; a non-positive or
    /// non-finite `frameTime` contributes nothing; a non-finite or negative `accumulator` is treated as
    /// empty (so a stray NaN `dt` can never poison the loop); `stepCount` is capped at `Int32.MaxValue`.
    val drain: interval: float -> frameTime: float -> accumulator: float -> struct (int * float)

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Drain with an explicit spiral-of-death clamp `maxFrameTime` (seconds), e.g. a tighter `0.05`.
    val drainWith:
        maxFrameTime: float ->
        interval: float ->
        frameTime: float ->
        accumulator: float ->
            struct (int * float)
