// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Ballistics.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A projectile in continuous simulation space — a `Position`, a `Velocity` in units per SECOND, and
/// a `TicksRemaining` lifetime measured in whole fixed steps rather than float seconds. The tick
/// count is deliberate: a lifetime carried as `float` seconds accumulates a different rounding error
/// on every machine and drifts a replay apart, while an `int` decrement cannot. Structural equality
/// makes a projectile a golden-testable value.
type Projectile =
    { Position: Point
      Velocity: Point
      TicksRemaining: int }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// The outcome of advancing a `Projectile` by one fixed step — the three terminal states of a round,
/// as a value the caller matches on rather than a mutated flag.
type ProjectileStep =
    /// The round crossed its whole step without meeting an occluder, and is still alive.
    | Flew of Projectile
    /// The round met an occluder. The `RayHit` is reported in the frame of THIS step's segment, so
    /// `T` is the fraction of the step consumed before impact and `Point` is the impact position.
    | Struck of RayHit
    /// The round's lifetime ran out, or its state was not finite. It should be removed.
    | Expired

/// Public contract module exposed by the FS.GG.Game.Core package.
/// Ballistics for projectile weapons: a swept advance that cannot tunnel, a lead/intercept solve, and
/// a splash query with a caller-chosen falloff curve. Pure, total, and deterministic — no wall-clock
/// read, no shared mutable state, no floating-point tie-break — so a scripted `dt` sequence reproduces
/// a byte-identical trajectory.
///
/// The one non-negotiable rule this module exists to enforce: **a fast round is a SEGMENT, never a
/// point.** A 1200 unit/s round advanced by a 1/60 s step moves 20 units, and a naive
/// `containsPoint` test on the new position passes straight through any occluder thinner than that.
/// `step` therefore casts the whole segment `Position → Position + Velocity·dt` at the caller's
/// occluders, and no timestep is small enough to make that unnecessary.
[<RequireQualifiedAccess>]
module Ballistics =

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Advance a projectile by one fixed step of `dt` seconds, casting the swept segment
    /// `Position → Position + Velocity·dt` at the caller's occluders via `cast p0 p1`.
    ///
    /// `cast` is the sole coupling to the world's geometry: supply
    /// `fun p0 p1 -> Geometry.segmentAabbHit p0 p1 wall` for a box, `segmentPolygonHit` for a convex
    /// occluder, or a fold over a `SpatialGrid` broad phase that returns the NEAREST hit (smallest
    /// `T`). Returning anything other than the nearest hit is a caller bug, not an unsoundness here.
    ///
    /// Ordering: lifetime is checked BEFORE motion, so a round with `TicksRemaining <= 0` is `Expired`
    /// and never gets a free final step. A surviving round has `TicksRemaining` decremented by one.
    ///
    /// Total on every input, and hardened against a hostile `cast`. A non-finite `dt` or a
    /// non-positive `dt` advances the round zero distance (the tick is still consumed) rather than
    /// poisoning `Position` with a NaN; a projectile whose `Position` or `Velocity` is already
    /// non-finite is `Expired`, so a NaN can never escape into the world; and a `cast` that reports a
    /// hit with a non-finite `T`, a `T` outside `[0,1]`, or a non-finite `Point` or `Normal` is
    /// treated as a MISS — reflecting a velocity about a NaN normal poisons a world just as
    /// thoroughly as reading a NaN impact point.
    val step: cast: (Point -> Point -> RayHit option) -> dt: float -> projectile: Projectile -> ProjectileStep

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The lead/intercept solve: where to aim a round of scalar `speed` fired from `shooter` so that it
    /// meets a target now at `target` drifting at constant `targetVelocity`. Returns `ValueSome p`,
    /// the interception POINT (aim at it; the firing velocity is `normalise (p - shooter) * speed`), or
    /// `ValueNone` when no interception exists.
    ///
    /// This is the quadratic `|target + targetVelocity·t - shooter| = speed·t` in `t`. The degenerate
    /// cases are the whole reason this is a function and not three lines at the call site, and each
    /// returns `ValueNone` rather than a NaN:
    ///
    /// * the target is faster than the round and running away (no real root, or no positive root);
    /// * `speed` is zero, negative, or non-finite;
    /// * any input coordinate is non-finite.
    ///
    /// When the target's speed exactly equals `speed` the quadratic degenerates to a LINEAR equation;
    /// it is solved as one rather than divided by a zero leading coefficient. When both roots are
    /// positive the SMALLER (earliest interception) is taken; a zero root is accepted (the target is
    /// already at the shooter). The result is a pure function of its arguments, so it is replay-safe.
    val intercept: shooter: Point -> speed: float -> target: Point -> targetVelocity: Point -> Point voption

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Falloff curve: full damage at the centre, `edgeScale` of it at the rim, linear in between.
    /// `linearFalloff 0.5` is the "centre 100%, 50% at the edge" curve the tower-defense spec asks for;
    /// `linearFalloff 0.0` falls to nothing at the rim. `edgeScale` is clamped to `[0,1]`, and a
    /// non-finite `edgeScale` degrades to `0.0`.
    val linearFalloff: edgeScale: float -> (float -> float)

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Falloff curve: `1 / (1 + k·d²)` over the normalised distance `d ∈ [0,1]`. Plays very differently
    /// from `linearFalloff` — most of the damage is concentrated near the centre. `k` is clamped to
    /// non-negative, and a non-finite `k` degrades to `0.0` (a flat curve).
    val inverseSquareFalloff: k: float -> (float -> float)

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Splash: every item within `radius` of `centre`, paired with its damage multiplier under
    /// `falloff`. `falloff` receives the NORMALISED distance `d = distance / radius ∈ [0,1]` and
    /// returns a multiplier; use `linearFalloff` / `inverseSquareFalloff`, or supply your own — the
    /// curve is a balance lever and is deliberately not baked in.
    ///
    /// `position` projects an item onto the point splash measures from, because `SpatialGrid` returns
    /// items and not their positions. It MUST agree with the position the grid was built at; if it does
    /// not, the falloff is computed from a different point than the one that was queried.
    ///
    /// Determinism: the result is in `SpatialGrid` insertion order, and the SET of items returned —
    /// with their multipliers — is independent of that order, because membership is decided by the
    /// exact `distance <= radius` test and never by bucket traversal. A non-positive or non-finite
    /// `radius` returns `[]`. An item whose `position` is non-finite is excluded. A `falloff` that
    /// returns a non-finite multiplier contributes `0.0`, so a hostile curve cannot inject a NaN into
    /// a damage total.
    val splash:
        centre: Point ->
        radius: float ->
        falloff: (float -> float) ->
        position: ('T -> Point) ->
        grid: SpatialGrid<'T> ->
            ('T * float) list
