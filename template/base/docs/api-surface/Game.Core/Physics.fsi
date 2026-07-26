// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Physics.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract module exposed by the FS.GG.Game.Core package.
/// The mini rigid-body engine: a world of bodies, a broad phase, a narrow phase, and a semi-implicit
/// Euler step with a sequential-impulse contact solver, warm started, over bodies that fall asleep once
/// they settle. Fast *circle* movers are swept by speculative contacts (`step` documents the scope and
/// the corner caveat), and `interpolate` blends the double buffer into presentation-only poses on the
/// shortest arc.
///
/// **Mass is derived, not given.** Neither `Material` nor `addBody` carries one: a body's mass is the
/// area of its `Shape` at unit density, and its rotational inertia is taken about its origin, which is
/// therefore its centre of mass. `Static` and `Kinematic` bodies have infinite mass (zero inverse mass)
/// and are unmoved by any impulse.
///
/// Arcade resolution stays the first-class default: `Resolution.pushOut`/`slide`/`knockback` remain what
/// most games use, and `Physics` is opt-in. Neither module knows the other.
///
/// Every type here is scoped to `Physics` rather than promoted alongside `Point`/`Rect`/`Contact`,
/// following the rule `Grids.Edge` and `Visibility.Segment` already follow: `Physics` is their only
/// consumer, and a type moved *into* `Primitives` later is additive where one moved out of it is
/// breaking. Scoping also keeps names as collision-prone as `Static`, `Dynamic`, `Shape` and `Config`
/// out of a namespace that games `open` wholesale.
[<RequireQualifiedAccess>]
module Physics =

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// How the solver is allowed to move a body. `Static` never moves. `Kinematic` is moved by the game,
    /// never by a contact. `Dynamic` is moved by both. Only a `Dynamic` body has finite mass, so a pair
    /// with no `Dynamic` member can never resolve — `pairs` omits it.
    type BodyKind =
        | Static
        | Kinematic
        | Dynamic

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// A body's collision geometry, in body-local coordinates about its position. `SPoly`'s vertices are
    /// offsets from the position and carry `ConvexPolygon`'s convention (convex, CCW-wound) — the same
    /// input assumption `Geometry.polygonContact` makes, checked the same way.
    ///
    /// A degenerate shape — a non-positive or non-finite radius or half-extent, or a ring `Geometry`
    /// would reject as malformed (fewer than 3 vertices, a non-finite coordinate, or zero area) — is a
    /// *no-collision input*, exactly as it is to `Geometry`. `pairs` omits such a body rather than
    /// throwing, so the totality-on-degenerate-input convention holds here too.
    type Shape =
        | SCircle of radius: float
        | SBox of halfExtents: Point
        | SPoly of polygon: ConvexPolygon

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// The contact-response coefficients of a body. `Restitution` is the bounciness `e`; `Friction` is
    /// the Coulomb coefficient `μ`. Both are read by the solver, not by the broad phase.
    type Material = { Restitution: float; Friction: float }

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// Simulation-wide tuning, baked into a `World` at `empty` and never changed thereafter. Baking it in
    /// is what makes `Physics.step : World -> float -> World` *exactly* `Loop.advance`'s `integrate`
    /// contract, with no adapter.
    ///
    /// Iteration counts are `int` and fixed: the solver never "iterates until converged", because a float
    /// convergence tolerance is a replay divergence. `SleepTicks` is an `int` count of consecutive ticks,
    /// never an accumulation of elapsed seconds, for the same reason.
    ///
    /// **Sleeping has three independent off switches**, and any one of them disables it: a non-positive
    /// `SleepTicks` (a body cannot be at rest for zero consecutive ticks), a non-positive `SleepLinearSq`,
    /// or a non-positive `SleepAngular` (no squared speed and no magnitude is below zero). Warm starting
    /// has none — it changes how fast the solver converges, not what it converges to, and is always on.
    type Config =
        /// Uniform acceleration applied to every `Dynamic` body each step.
        { Gravity: Point
          /// Fixed velocity-solver iterations per step. 8 is the usual answer. Warm starting buys roughly
          /// an order of magnitude here: 4 warm iterations settle a box flatter than 10 cold ones.
          VelocityIterations: int
          /// Fixed position-correction iterations per step. 3 is the usual answer.
          PositionIterations: int
          /// Penetration tolerated before positional correction acts (~0.01).
          Slop: float
          /// Baumgarte correction fraction (~0.2).
          Correction: float
          /// Restitution applies only where `|v·n|` exceeds this; below it `e = 0`, or a resting box
          /// jitters forever and never sleeps.
          BounceThreshold: float
          /// A body is a candidate for sleep while its SQUARED linear speed is strictly below this.
          /// Non-positive disables sleeping.
          SleepLinearSq: float
          /// ...and while its angular speed magnitude is strictly below this. Non-positive disables
          /// sleeping. Both must hold on the same tick for the counter to advance.
          SleepAngular: float
          /// Consecutive ticks — not cumulative — a body must spend under both thresholds before it
          /// sleeps. Non-positive disables sleeping. 60 is a second at the usual step rate.
          SleepTicks: int
          /// Cell size handed to the `SpatialGrid` broad phase. A non-positive or non-finite value
          /// degrades to a single bucket — slower, never wrong (`SpatialGrid.build`'s own contract).
          BroadPhaseCellSize: float }

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// A rigid-body simulation world. The representation is **opaque** (hidden in the `.fsi`), exactly as
    /// `SpatialGrid<'T>` is: callers build with `empty`/`addBody` and read through this module, and never
    /// see the interior. Bodies are identified by the `int` index `addBody` returns, which is stable for
    /// the life of the world.
    ///
    /// Immutable at the boundary — every function here takes a `World` and returns a value — so a `World`
    /// is safe to hold inside a `Model`, and safe as the `'world` of `Loop.StepState`.
    ///
    /// Besides body state a `World` carries **cross-tick derived state**: which bodies are asleep, how long
    /// each has been still, the impulse each contact accumulated last tick (the warm-start cache), and the
    /// broad-phase index — each body's world-space bounds and the grid that buckets them, kept current with
    /// the bodies so `pairs` reads it rather than rebuilding one per call. None of it is presentation, none
    /// of it reaches `checksum`, and none of it is meaningful to interpolate. **Two worlds equal in
    /// presentation may differ in this state** — a world that has just woken holds no cache, one that settled
    /// through a different history holds a different one, and a world built by `addBodies` reaches the same
    /// pose as one built by `addBody` through different arrays — so compare worlds with `checksum`, never
    /// structurally.
    type World

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// The presentation pose of one body: where to draw it (`Position` — the body origin `Pos` means) and
    /// how it is turned (`Rotation`, radians). It is the projection a renderer consumes, deliberately NOT a
    /// `World`: it carries no velocity, no inverse mass, and none of the sleep or warm-start cache — none
    /// of which is drawn or blended. `interpolate` produces an array of these, one per body.
    type Transform = { Position: Point; Rotation: float }

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// A world with no bodies, carrying `config` for its lifetime.
    val empty: config: Config -> World

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Append a body and return `struct(index, world')`. The index is the body's identity: it is the
    /// world's previous body count, so indices are dense and ascending in insertion order, and `pairs`
    /// reports pairs in terms of them.
    ///
    /// `position` is the body's origin; `shape` is expressed relative to it. A degenerate `shape` or a
    /// non-finite `position` is accepted and indexed (so later indices do not shift), but the body
    /// collides with nothing — see `Shape`.
    ///
    /// Cost is O(body count): the world's arrays are copied. This is the *build* path, not the step path
    /// — `Physics.step` is the one that must not allocate. Adding many bodies at once is O(N²) this way, a
    /// copy per body; reach for `addBodies` when a caller has a batch, which copies once.
    val addBody:
        kind: BodyKind -> shape: Shape -> material: Material -> position: Point -> world: World -> struct (int * World)

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Append a *batch* of bodies in one pass and return `struct(indices, world')`, where `indices.[k]` is
    /// the identity of the `k`-th body of `bodies` — dense and ascending from the world's previous body
    /// count, exactly the indices the same bodies added one at a time through `addBody` would receive.
    ///
    /// This is the build path for a scene: it copies each of the world's arrays **once for the whole batch**
    /// and rebuilds the broad-phase index once, so loading an N-body world is O(N), where N separate
    /// `addBody` calls are O(N²) — a full copy per body. `addBodies bodies` and folding `addBody` over the
    /// same `bodies` produce worlds with identical `checksum`s and identical `pairs`.
    ///
    /// Every guarantee `addBody` makes holds: it is an append that returns a new value and never mutates
    /// `world`, so the input world is untouched and safe to keep, indices are stable, and each body enters at
    /// rest, unrotated and awake. A degenerate `shape` or non-finite `position` is still accepted, indexed,
    /// and collides with nothing. An empty `bodies` returns `struct([||], world)` — the world unchanged.
    val addBodies: bodies: seq<BodyKind * Shape * Material * Point> -> world: World -> struct (int[] * World)

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The broad phase: every candidate pair, as `struct(a, b)` with `a < b`, **sorted ascending by
    /// `(a, b)` and free of duplicates**. Unsorted pair order is the single largest determinism leak in a
    /// physics step, so the order is part of this contract rather than an accident of the container: the
    /// result is a byte-identical array for a byte-identical world.
    ///
    /// A pair is returned exactly when all three hold:
    ///
    /// - **at least one body is `Dynamic`** — no other pair can ever resolve, so emitting it would only
    ///   buy a narrow-phase call that cannot produce motion;
    /// - **both bodies are collidable** — no degenerate shape, no non-finite position, and no non-finite
    ///   rotation (`Shape`);
    /// - **their world-space AABBs overlap on positive area**, decided by `Geometry.intersects` and so
    ///   inheriting its **strict-edge convention: two boxes that merely touch are not a pair.** That
    ///   agrees with the narrow phase, which likewise reports no contact on a touch.
    ///
    /// The result is therefore *exact over AABBs* — no false positives and no false negatives, the same
    /// promise `SpatialGrid.queryBounds` makes — rather than the loose superset a broad phase is normally
    /// allowed. Narrowing further (are the *shapes*, rather than their boxes, in contact?) is the narrow
    /// phase's job. Exactness holds at **any** magnitude: a body whose extent is too large to bucket
    /// costs **only itself** its spatial acceleration, never its pairs and never the rest of the world's.
    ///
    /// Pure, total and deterministic: identical bodies added in an identical order yield an identical
    /// array. Never throws.
    val pairs: world: World -> struct (int * int)[]

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The narrow phase: the contact between bodies `a` and `b`, or `ValueNone` if they do not touch.
    ///
    /// `A` and `B` are `a` and `b` — the body indices, not `polygonManifold`'s `0`/`1` — and `Normal` is
    /// the unit vector pointing from `a` toward `b`, whichever order they are given in. `Depth` is the
    /// penetration along it, and `Points` holds `PointCount` contact points: up to 2 for a polygon pair
    /// (a face-on-face contact), always 1 where a circle is involved.
    ///
    /// The **strict-edge convention** is `Geometry`'s throughout: a touch is not a contact. Two circles
    /// contact when `d < ra + rb`, never at `d = ra + rb`; polygon pairs defer to `polygonManifold`, which
    /// says the same. This agrees with `pairs`, so a pair that the broad phase refuses can never have had
    /// a contact to lose.
    ///
    /// `FeatureId` identifies the contacting features and is stable across ticks for an unmoving pair. It
    /// is two thirds of the key `step`'s warm-start cache is built on — an unstable one would seed a
    /// contact with an impulse belonging to a different one, and stacks would shake. **Opaque: compare it,
    /// do not decode it.** Its values are disjoint from `polygonManifold`'s for the circle cases, which
    /// have no face pair to name.
    ///
    /// Pure, total and deterministic. An out-of-range or equal index, a degenerate shape, a non-finite
    /// position or rotation, and two exactly coincident circle centres (no direction exists along which to
    /// separate them) all yield `ValueNone` rather than throwing or inventing a normal.
    val manifold: world: World -> a: int -> b: int -> Manifold voption

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Advance the world by `dt`. **This is `Loop.advance`'s `integrate`**: the signature is exactly
    /// `'world -> float -> 'world` because `Config` is baked into the `World` at `empty`, so
    /// `Loop.advance dt Physics.step frameTime state` typechecks with no adapter. Reads `dt` and the
    /// world, and nothing else — never a clock, never `alpha`.
    ///
    /// One step is: broad phase; narrow phase; wake what a moving body touches; integrate velocity under
    /// gravity (semi-implicit Euler); seed each contact with the impulse it accumulated last tick; solve
    /// contact velocities with a sequential-impulse solver over a **fixed** `VelocityIterations`; integrate
    /// position; correct penetration over a fixed `PositionIterations`; then put to sleep whatever has
    /// stopped moving.
    ///
    /// **Continuous collision (speculative).** A body moving more than its own thinnest cross-section in one
    /// step could cross a thin obstacle between its start and end positions, which the discrete phases —
    /// reading only the two endpoints — would miss. When any fast mover exists, `step` casts each such mover
    /// and generates a speculative contact for the impending impact; the solver then refuses to let the pair
    /// close through it, so the mover stops AT the surface rather than through it. `dt` is never subdivided
    /// (substepping would change the step count, diverging a recorded input replay), and the sweep is INERT —
    /// byte-identical to its absence — when no body is fast. **Scope and limits:** the swept mover is a
    /// *circle* (the projectile shape); a fast *polygon* mover is not swept, and rotational CCD is out of
    /// scope. Against a polygon the mover's *centre* is cast at the *bare* face, so a circle clipping a
    /// *corner* without its centre reaching the target gets no speculative contact and can still tunnel that
    /// corner — the discrete phase recovers on the next tick, once real penetration exists.
    ///
    /// **Warm starting.** Each contact point is seeded with the normal and tangent impulse the same contact
    /// — same bodies, same `FeatureId`, same point — accumulated last tick. It is a convergence
    /// accelerator: it does not change what the solver converges *to*, only how fast, so a scene that had
    /// genuinely settled is unaffected bit-for-bit, while one still converging settles sooner. A contact
    /// that is new this tick starts cold, exactly as though the cache did not exist.
    ///
    /// **Sleeping.** A `Dynamic` body that holds both `SleepLinearSq` and `SleepAngular` for `SleepTicks`
    /// CONSECUTIVE ticks stops: its velocity is zeroed, it is no longer integrated, and pairs in which no
    /// body is awake are not even narrow-phased — a settled scene costs the broad phase and nothing else.
    /// A sleeping body is **immovable, not absent**: it has infinite mass for as long as it sleeps, exactly
    /// as a `Static` body does, and things pile up on it rather than through it.
    ///
    /// A sleeper wakes when a body that is **actually moving** touches it. Merely being awake is not
    /// enough, or two bodies resting on one another would wake each other forever; and a `Static` body
    /// never moves, so a floor never disturbs what has settled on it. The consequence to know: a wake
    /// travels at most one body per tick, and only through bodies that visibly move — a sleeper that takes
    /// a load without shifting does not pass it on. Waking a whole pile at once needs solver islands, which
    /// this engine does not have.
    ///
    /// Friction is a tangent impulse clamped to the Coulomb cone `|jt| <= μ·jn`, with `μ` the geometric
    /// mean of the two materials'. Restitution applies only where the approach speed exceeds
    /// `BounceThreshold`; below it `e = 0`, or a resting box jitters forever.
    ///
    /// A contact's coefficients combine from BOTH materials: restitution as the **maximum** of the two
    /// (a floor is normally `0`, and taking the minimum would make every ball dead on every floor), and
    /// friction as the geometric mean `sqrt(μa·μb)` (so one frictionless body slides on anything).
    ///
    /// **Determinism.** Manifolds are sorted by `(A, B, FeatureId)` before solving. Iteration counts are
    /// fixed `int`s — the solver never iterates until converged, because a float convergence tolerance
    /// decides differently on two machines. Every divide and `sqrt` is guarded. Identical worlds stepped
    /// by identical `dt` sequences produce identical worlds, and so identical `checksum`s.
    ///
    /// Total: a non-finite or non-positive `dt` returns the world unchanged, as does an empty world.
    /// A `Static` body never moves; a `Kinematic` body moves under its own velocity and is unmoved by
    /// impulses. Never throws.
    val step: world: World -> dt: float -> World

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Blend the two double-buffered worlds `Loop.advance` keeps — `previous` and `current` — into the
    /// poses to draw this frame, at `Loop.alpha`'s fraction `alpha`. Returns one `Transform` per body of
    /// `current`, in index order.
    ///
    /// **Presentation only.** Only `Pos` and `Rot` interpolate — the state a renderer shows. Velocity and
    /// the solver's cross-tick cache (accumulated impulses, sleep counters) are NOT blended: blending a
    /// warm-start cache is meaningless, and two worlds equal in pose may hold different caches. This is the
    /// same projection `checksum` takes (body state, never cache), for the same reason.
    ///
    /// **Shortest arc.** `Rotation` interpolates the short way around the circle. A body turning through
    /// `π` — `previous ≈ +3.13`, `current ≈ -3.13` — crosses `+π`; it does not unwind the long way through
    /// `0`. The naive `a0 + (a1 - a0)·alpha` gets this wrong and spins a turret backwards at 60 fps.
    ///
    /// **Exact endpoints.** `interpolate 0.0 previous current` is `previous`'s poses and
    /// `interpolate 1.0 previous current` is `current`'s, bit-for-bit — even across the shortest-arc wrap,
    /// where the blended angle at `alpha = 1` would otherwise land a full turn off `current`'s.
    ///
    /// `alpha` is read here, at the presentation boundary, and **never reaches `step`** — feeding it back
    /// into the simulation is the cardinal double-buffer sin. It is clamped to `[0, 1]`, so a non-finite or
    /// out-of-range `alpha` neither throws nor extrapolates past an endpoint. A body in `current` but not
    /// `previous` — one added since the previous buffer was taken — has no prior pose and is returned at
    /// its `current` transform. Total: never throws.
    val interpolate: alpha: float -> previous: World -> current: World -> Transform[]

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// A stable 64-bit hash of every body's `Pos`, `Vel`, `Rot` and `AngVel` — the desync tripwire.
    /// Compare it across a replayed input stream, or against a golden value in a test, to catch the tick
    /// on which two runs diverged.
    ///
    /// **Body state only.** No solver state, no cache, no sleep flag, no contact — those are derived each
    /// step, and a checksum over them would move whenever an optimisation changed how they are stored,
    /// reporting a desync that is not one. A world asleep and a world awake in the same pose hash alike.
    ///
    /// Stable across runs, processes and runtimes: it is FNV-1a over the IEEE-754 bits, not
    /// `GetHashCode`. `-0.0` hashes as `0.0` (they compare equal, so they must hash equal), and every NaN
    /// hashes alike. Float arithmetic is byte-deterministic on a fixed compiler and ISA; a cross-platform
    /// lockstep guarantee needs fixed-point, which is a later, ADR'd decision.
    val checksum: world: World -> uint64
