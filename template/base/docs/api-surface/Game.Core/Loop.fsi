// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Loop.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// The double step buffer: the world as of the last completed fixed step (`Current`), the world one
/// step behind it (`Previous`), and the sub-step time carried into the next frame (`Accumulator`,
/// seconds). Renderers interpolate `Previous → Current` by `Loop.alpha`, so motion is smooth at any
/// frame rate while the simulation only ever advances by whole fixed steps.
///
/// Structural equality is the world's: a scripted `frameTime` sequence over a deterministic
/// `integrate` reproduces an identical `StepState` chain, which is what makes replay a value
/// comparison rather than a tolerance check.
type StepState<'world> =
    { Current: 'world
      Previous: 'world
      Accumulator: float }

/// Public contract module exposed by the FS.GG.Game.Core package.
/// The fixed-step double-buffered loop, built on `FixedStep.drain` — one hardened accumulator in the
/// org, not two. `advance` drains whole steps and keeps the world from before the last one.
///
/// **The double step buffer is the default** for any product with a continuously-moving simulation.
/// Stepping the world any other way MUST record why in the spec. The rule that generalizes the
/// sanctioned departures: *interpolate when the world moves between ticks; buffer when you
/// interpolate.* A discrete-grid game (Snake, Tetris) has nothing to show between `Previous` and
/// `Current` and may carry one world and a step timer; a headless replay that only fingerprints
/// `Current` may carry no `Previous` at all; rollback netcode may *widen* the buffer to a ring of N
/// worlds. Continuous motion with a single buffer is a defect, not a departure.
///
/// Three things stay non-negotiable regardless of buffering, because they are the three ways to lose
/// replay: never feed `alpha` back into the simulation, never step with a variable `dt`, and never
/// read a wall clock below the effect interpreter.
[<RequireQualifiedAccess>]
module Loop =

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Seed the buffer from a starting world. `Previous = Current` and the accumulator is empty, so
    /// `alpha` is `0.0` and the first frame renders the world exactly as given.
    val init: world: 'world -> StepState<'world>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Advance by the whole fixed steps that `frameTime` (seconds) affords, integrating each with
    /// `integrate world dt`. `Previous` is the world before the **last** step that ran, so a frame
    /// that runs several steps still brackets exactly the interval the renderer interpolates across;
    /// a frame that runs none leaves both worlds untouched.
    ///
    /// The drain — and with it the `FixedStep.defaultMaxFrameTime` spiral-of-death clamp and the
    /// totality — is `FixedStep.drain`'s, not a second copy. So this is total on every input: a
    /// non-finite or non-positive `dt` runs no steps; a non-finite or non-positive `frameTime`
    /// contributes no time; a non-finite or negative `Accumulator` is treated as empty rather than
    /// propagated, and the returned `Accumulator` is always finite and in `[0, dt)` for `dt > 0`. A
    /// `NaN` can therefore never enter the buffer and wedge the loop.
    ///
    /// The clamp bounds injected *time*, not work: a pathologically small `dt` still affords
    /// `floor((Accumulator + 0.25) / dt)` calls to `integrate`. Choose `dt` as a sim rate, not a
    /// resolution knob.
    val advance:
        dt: float -> integrate: ('world -> float -> 'world) -> frameTime: float -> state: StepState<'world> ->
            StepState<'world>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The render interpolant in `[0.0, 1.0]`: how far the presentation clock sits between `Previous`
    /// and `Current`. A **presentation** value — feeding it back into the simulation makes the sim
    /// frame-rate dependent and destroys replay.
    ///
    /// Never returns `NaN` or a value outside `[0, 1]`, for any `dt` and any hand-built `StepState`:
    /// a non-finite or non-positive `dt`, and a non-finite or negative `Accumulator`, all yield `0.0`.
    val alpha: dt: float -> state: StepState<'world> -> float
