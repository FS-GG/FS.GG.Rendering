namespace FS.GG.UI.Canvas

/// Feature 191 (US3, C4/FR-009/FR-011): the fixed-timestep accumulator state. `Current`/`Previous`
/// bracket the latest two simulated worlds (render interpolates between them with `Loop.alpha`);
/// `Accumulator` carries the unspent sub-step time. Deterministic: a `StepState` is a pure value.
type StepState<'world> =
    { Current: 'world
      Previous: 'world
      Accumulator: float }

/// Feature 191 (US3, C4): a deterministic fixed-timestep game loop (Glenn Fiedler's accumulator).
/// Every function's output depends ONLY on its arguments — no wall-clock read — so a seed + a scripted
/// `frameTime` sequence reproduces an identical `StepState` every run (FR-011, SC-006).
[<RequireQualifiedAccess>]
module Loop =

    /// Seed a `StepState` from an initial world (`Previous = Current`, `Accumulator = 0`).
    val init: world: 'world -> StepState<'world>

    /// Advance the simulation by whole fixed steps.
    /// `dt` — fixed step seconds (e.g. `1.0/60.0`).
    /// `integrate` — pure `'world -> dt -> 'world` simulation step.
    /// `frameTime` — elapsed seconds since the last advance; clamped to `<= 0.25` (spiral-of-death guard).
    /// Runs `floor((Accumulator + clamp frameTime) / dt)` steps, carrying the remainder in `Accumulator`.
    val advance:
        dt: float ->
        integrate: ('world -> float -> 'world) ->
        frameTime: float ->
        state: StepState<'world> ->
            StepState<'world>

    /// Interpolation factor in [0,1) for rendering between `Previous` and `Current` (`Accumulator / dt`).
    val alpha: dt: float -> state: StepState<'world> -> float
