namespace FS.GG.UI.Canvas

/// **DEPRECATED (ADR-0104, Rendering#269).** Superseded by `FS.GG.Game.Core.StepState`. The double
/// step buffer is a *simulation* primitive: `Current`/`Previous` are two simulated worlds, not two
/// render states. It is retired at the next `FS.GG.UI.Canvas` major. Prefer `FS.GG.Game.Core`.
///
/// Feature 191 (US3, C4/FR-009/FR-011): the fixed-timestep accumulator state. `Current`/`Previous`
/// bracket the latest two simulated worlds (render interpolates between them with `Loop.alpha`);
/// `Accumulator` carries the unspent sub-step time. Deterministic: a `StepState` is a pure value.
type StepState<'world> =
    { Current: 'world
      Previous: 'world
      Accumulator: float }

/// **DEPRECATED (ADR-0104, Rendering#269). Use `FS.GG.Game.Core.Loop` instead.**
///
/// `advance` contains no rendering: it is `FS.GG.Game.Core.FixedStep.drain` plus a fold that retains
/// the previous world, so it belongs in the BCL-only bottom layer where a headless deterministic
/// simulation can reach it (ADR-0022 §2). Keeping a second accumulator here already cost the org one
/// divergence — `FixedStep.drain` was hardened against non-finite input while this copy propagated
/// `NaN` and froze the simulation permanently (#266), and the hardened one was not the one products
/// used. `FS.GG.Game.Core.Loop` is that one accumulator, built on `FixedStep.drain`.
///
/// `FS.GG.Game.Core.Loop` is deliberately **not** re-exported from here: that would grow a
/// `FS.GG.UI.Canvas` → `FS.GG.Game.Core` package edge for one type and three functions (ADR-0104).
/// Depend on `FS.GG.Game.Core` directly — the `game` and `sample-pack` profiles, the only two that
/// materialize `FS.GG.UI.Canvas`, already pin it.
///
/// Retired at the next `FS.GG.UI.Canvas` major. It carries no `[<Obsolete>]` yet because the
/// replacement ships in no published `FS.GG.Game.Core` (it landed after `v0.2.0`); the attribute lands
/// with the release that makes the migration target reachable.
///
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
    /// A non-finite input can never poison the loop: a non-positive or non-finite `dt` returns `state`
    /// unchanged; a non-positive or non-finite `frameTime` contributes nothing; a non-finite or negative
    /// `Accumulator` is treated as empty. Given a `state` produced by `init` or `advance`, the returned
    /// `Accumulator` is finite and in `[0, dt)`.
    val advance:
        dt: float ->
        integrate: ('world -> float -> 'world) ->
        frameTime: float ->
        state: StepState<'world> ->
            StepState<'world>

    /// Interpolation factor for rendering between `Previous` and `Current` (`Accumulator / dt`); in
    /// `[0,1)` for any `state` produced by `init` or `advance`, whose `Accumulator` is `< dt`.
    /// Never `NaN`: a non-positive or non-finite `dt` yields `0.0`, as does a non-finite or negative
    /// `Accumulator`.
    val alpha: dt: float -> state: StepState<'world> -> float
