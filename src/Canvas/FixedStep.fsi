namespace FS.GG.UI.Canvas

/// Public contract module exposed by this FS.GG.UI package.
/// Fixed-timestep accumulator drain: given a fixed `interval`, an elapsed `frameTime`, and a carried
/// `accumulator` (all in SECONDS, matching `Loop.advance`), returns the whole number of fixed steps to
/// run this frame and the new carried accumulator. Pure and deterministic — no wall-clock read — so a
/// scripted `frameTime` sequence reproduces identical results. The lower-level primitive beneath the
/// stateful `Loop.advance`.
[<RequireQualifiedAccess>]
module FixedStep =

    /// Public contract value exposed by this FS.GG.UI package.
    /// Default spiral-of-death clamp in seconds (0.25) — the same cap `Loop.advance` uses. A single
    /// stalled frame runs at most `floor((accumulator + 0.25) / interval)` steps.
    val defaultMaxFrameTime: float

    /// Public contract function exposed by this FS.GG.UI package.
    /// Drain with the default clamp (`defaultMaxFrameTime`). Returns `struct(stepCount, newAccumulator)`
    /// with `stepCount >= 0` and `0 <= newAccumulator < interval` for `interval > 0`. Total on
    /// degenerate input: `interval <= 0` yields `struct(0, accumulator)`; `frameTime <= 0` contributes
    /// nothing and yields `struct(0, accumulator)`.
    val drain: interval: float -> frameTime: float -> accumulator: float -> struct (int * float)

    /// Public contract function exposed by this FS.GG.UI package.
    /// Drain with an explicit spiral-of-death clamp `maxFrameTime` (seconds), e.g. a tighter `0.05`.
    val drainWith:
        maxFrameTime: float ->
        interval: float ->
        frameTime: float ->
        accumulator: float ->
            struct (int * float)
