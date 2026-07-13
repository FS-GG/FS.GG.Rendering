---
name: fs-gg-game-core
description: Simulate a generated FS.GG.UI product — deterministic fixed-step loop, seeded RNG, AABB collision, and entity culling.
---

# Game Core (Simulation) Capability

## Scope

Use this skill for the **simulation** half of a game/sim product: advancing world state on a
deterministic fixed timestep, drawing seeded randomness without a shared mutable generator, testing
axis-aligned collisions, and culling off-screen entities. These are pure helpers — they read no
wall-clock and perform no I/O. Rendering the resulting world is `fs-gg-scene`'s job; wiring the loop to
a window is `fs-gg-skiaviewer`'s. This skill materializes for the `game` and `sample-pack` profiles.

## Public Contract

The signatures you consume are bundled with this product:

- `docs/api-surface/Game.Core/Geometry.fsi` — the `Geometry` module (collision / containment / centering),
  on the sim `Rect`/`Point`. Shipped in `FS.GG.Game.Core`, referenced on the `game` and `sample-pack`
  profiles.
- `docs/api-surface/Game.Core/Rng.fsi`, `docs/api-surface/Game.Core/FixedStep.fsi`, and
  `docs/api-surface/Game.Core/Loop.fsi` — the `Rng` value type, the `FixedStep` accumulator drain, and the
  `Loop` double step buffer built on it. Also `FS.GG.Game.Core`, same profiles.
- `docs/api-surface/Game.Core/Pathfinding.fsi` and `docs/api-surface/Game.Core/SpatialGrid.fsi` — deterministic
  grid `Pathfinding` (A*/BFS over a walkability predicate) and the uniform `SpatialGrid` for range/splash
  queries. Also `FS.GG.Game.Core`, same profiles — reuse these instead of hand-rolling BFS/A* or bucketing.

The rest of the simulation substrate ships in the same package, each with a skill that teaches it:
`Resolution` ([[fs-gg-game:fs-gg-collision]]), `Grids` ([[fs-gg-game:fs-gg-grids]]), `Los` ([[fs-gg-game:fs-gg-line-drawing]]), `Visibility`
and `Fov` ([[fs-gg-game:fs-gg-visibility]]), and `Ballistics` ([[fs-gg-game:fs-gg-ballistics]]). Reach for those before writing
your own — they are the authoritative implementations, not starting points to copy.

Every draw returns a `struct` tuple `(value, nextState)` — deconstruct with `let struct(v, next) = …`.
All helpers are **total**: degenerate inputs return a documented value, they never throw.

## Fixed-timestep march

`FixedStep.drain interval frameTime accumulator` returns `struct(stepCount, newAccumulator)` — the whole
number of fixed steps to run this frame and the carried remainder (all in seconds). It is pure: a
scripted `frameTime` sequence reproduces identical results. A stalled frame is clamped to
`defaultMaxFrameTime` (0.25 s) so the loop can't spiral; pass a tighter clamp with
`drainWith maxFrameTime interval frameTime accumulator`.

Stepping on whole intervals is only half the problem. Render frames land *between* steps, so drawing
`Current` directly makes motion stutter at every frame rate that isn't an exact multiple of the sim
rate. `Loop` closes that gap: it keeps the world one step back and hands you the interpolant.

### The double step buffer is the default

> **The double-buffered fixed-step loop (`StepState { Current; Previous; Accumulator }`) is the default
> for any product with a continuously-moving simulation. Stepping the world any other way MUST record
> why in the spec.**

The one-liner that generalizes it: **interpolate when the world moves between ticks; buffer when you
interpolate.** The sanctioned departures, so the rule reads as a rule and not a prohibition:

| Departure | Where | Justification |
|---|---|---|
| Single world + step timer | `Snake.fs`, `Tetris.fs` | **Discrete-grid games.** The world only occupies integer cells; there is nothing between `Previous` and `Current` to show. Interpolating a Tetris piece halfway between two rows is wrong, not smooth. |
| Single world + step timer | `Pong.fs` | Continuous motion — a **weaker** case. Pong would look better double-buffered; it predates the buffer. Legacy, not precedent. |
| No `Previous` at all | headless replay, `Board.evidence` | **Nothing renders.** `Previous` and `alpha` are dead weight in a fold that only fingerprints `Current`. |
| More than two buffers | rollback netcode | Needs a ring of N historical worlds. The sanctioned reason to *widen* the buffer, not narrow it. |
| No accumulator | turn-based games | A turn is a `Msg`, not a tick. |

Three things stay non-negotiable regardless of buffering, because they are the three ways to lose
replay: **never feed `alpha` back into the simulation**, never step with a variable `dt`, and never read
a wall clock below the effect interpreter.

```fsharp
open FS.GG.Game.Core

// Run world-updates at a fixed 60 Hz regardless of render cadence.
let simInterval = 1.0 / 60.0

// `integrate` is your game step: world -> dt -> world.
let stepped = Loop.advance simInterval integrate dtSeconds model.Sim

// ...and at draw time, lerp the two worlds by how far the presentation clock sits between them.
let t = Loop.alpha simInterval stepped        // in [0, 1], never NaN
let shown = lerpWorld stepped.Previous stepped.Current t
```

`Loop.advance` delegates the drain — and with it the 0.25 s clamp and the totality — to
`FixedStep.drain`, so a `NaN` frame time can never enter the accumulator and freeze the sim. `Previous`
is the world before the **last** step, so a frame that catches up several steps still brackets exactly
the interval you interpolate across. Seed with `Loop.init world`.

Reach for `FixedStep.drain` directly only when you have taken one of the departures above — a
discrete-grid game or a headless replay, where there is no `Previous` worth keeping.

The scaffolded `game` starter ships the drain shape: `Model.SimAccumulator` carries the remainder,
`update` on `Tick dtSeconds` calls `FixedStep.drain simInterval dtSeconds` and runs `stepSim` that many
times, and the host feeds the real elapsed time in (`EvidenceCommands.tick`). Edit `stepSim` to be your
game step; if it moves anything continuously, carry a `StepState` instead of a bare accumulator.

## Collision-safe positions (`Geometry.Vec2`)

Store entity positions/velocities in the scaffold's **collision-safe** `Geometry.Vec2` (`src/<ProductDir>/Vec2.fs`),
**not** a record you name with `X`/`Y`/`Width`/`Height`. Those labels collide with `FS.GG.UI.Scene.Point`/`Rect`,
and because the durable `LayoutEvidence.fs` opens both `Scene` and your model, the collision surfaces as a wall of
errors in a file you must not touch — only after a whole model is written (see [[fs-gg-game:fs-gg-model-swap]]). `Vec2` uses
`Vx`/`Vy` (zero overlap) and crosses into the scene with `toPoint`/`toRect` (express a size via `toRect`, never
`Width`/`Height` labels). This is also the cheapest time to plan your *own* records' labels so two of them don't
share one (the consumer-vs-consumer inference footgun below).

## RNG determinism

`Rng` is a value type (`{ State: uint64 }`) — a SplitMix64 generator you **store in your MVU `Model`**
and thread through `update`. Because two `Rng` with equal state are equal and produce identical
continuations, structural equality of a `Model` implies equal RNG state, so replay and clone stay
deterministic. Never put a mutable `System.Random` in the `Model` — it breaks determinism and structural
equality.

```fsharp
open FS.GG.Game.Core

type Model = { Rng: Rng; Score: int }               // RNG state lives IN the model

let init (seed: uint64) = { Rng = Rng.ofSeed seed; Score = 0 }

// Draw and write the ADVANCED generator back into the model — never reuse the input Rng.
let spawnLane (model: Model) : int * Model =
    let struct (lane, rng') = Rng.nextInt 0 9 model.Rng
    lane, { model with Rng = rng' }

// `split` derives an independent sub-stream (e.g. per-entity) without disturbing the main one.
let forkStream (model: Model) : Rng * Model =
    let struct (child, rng') = Rng.split model.Rng
    child, { model with Rng = rng' }
```

## Collision

Collision detection **and** response now have a dedicated skill — see **[[fs-gg-game:fs-gg-collision]]**. It covers
narrow-phase (`Geometry` box/circle/polygon contacts and segment casts on the shared `Rect`/`Point`),
broad-phase over `SpatialGrid`, and the `Resolution` response layer — which `FS.GG.Game.Core` keeps
deliberately separate from detection. Reach for it instead of hand-rolling AABB or a duplicate bounds
record.

## Culling

Keep only the entities whose bounds meet the visible region — an `intersects` (or `containsPoint`)
test against the gameplay `Rect`, reusing the same shared type:

```fsharp
open FS.GG.Game.Core

let visible : Rect = { X = 0.0; Y = 0.0; Width = 1280.0; Height = 720.0 }

let onScreen (bounds: Rect list) : Rect list =
    bounds |> List.filter (fun b -> Geometry.intersects b visible)
```

## Pathfinding

`Pathfinding` routes an agent across a tile grid over a **walkability predicate you supply** (`Cell -> bool`
IS the map — the framework holds no grid state). It is **deterministic**: integer move costs and a total
frontier order make the path byte-identical across runs, so it is safe inside a replayed `update` — no
hand-rolled BFS/A*, no tie-break footgun.

- `Pathfinding.astar neighbourhood maxVisited isWalkable start goal` — cost-optimal path, `Some [start;…;goal]`
  (endpoints included) or `None`. `FourWay`/`EightWay`; `EightWay` costs 10 orthogonal / 14 diagonal and
  refuses to cut a wall corner. `maxVisited` bounds the search so an unreachable goal terminates.
- `Pathfinding.bfs …` — same shape, unweighted (hop-minimal) path.

```fsharp
open FS.GG.Game.Core

let blocked = set [ (2, 0); (2, 1); (2, 2) ]                     // a wall your Model owns
let walkable (c: Cell) = c.Col >= 0 && c.Col < 8 && c.Row >= 0 && c.Row < 8 && not (blocked.Contains(c.Col, c.Row))

let route = Pathfinding.astar EightWay 4096 walkable { Col = 0; Row = 0 } { Col = 7; Row = 3 }
```

## Spatial queries (range / splash)

`SpatialGrid` buckets positioned items once and answers "what is near here" without an O(n²) scan — the
uniform grid the perf guidance recommends, now shipped. Built from a cell size and `(Point * 'item)`
pairs (the sim `FS.GG.Game.Core.Point`); queries are **exact** (no false positives/negatives) and return
items in **insertion order**.

- `SpatialGrid.build cellSize items` — bucket once (hold the grid in your `Model` or rebuild per frame).
- `SpatialGrid.query region grid` — items inside a `Rect` (broad-phase collision / on-screen set).
- `SpatialGrid.queryRadius center radius grid` — items within a radius (splash damage / proximity).

`SpatialGrid` buckets by the sim `Point`, but your model stores positions in the collision-safe
`Geometry.Vec2` (`Vx`/`Vy`) — and the scaffold's `Vec2` only crosses into the *scene*
(`toPoint`/`toRect`), so it ships **no** `Vec2 -> Game.Core.Point` crossing. Write the one-liner
yourself; the return annotation is what stops a bare `{ X = …; Y = … }` from binding to
`Scene.Point` (the clash in *Consumer geometry records colliding with framework `Point`/`Rect`*
below).

```fsharp
open FS.GG.Game.Core

// The crossing the scaffold does not ship. `: Point` is load-bearing, not decoration.
let simPoint (v: Geometry.Vec2) : Point = { X = v.Vx; Y = v.Vy }

let grid = SpatialGrid.build 32.0 [ for e in enemies -> simPoint e.Pos, e.Id ]
let splashed = SpatialGrid.queryRadius (simPoint blast) 48.0 grid   // ids to damage
```

## Grid-sim recipe

The primitives above compose into the loop a grid game actually runs each fixed step. Nothing here reads
the clock or touches shared state, so the whole `stepWorld` stays pure and replay-identical — which is the
point of keeping the `Rng`, the grid, and the effect table *in the `Model`*.

A tower-defense-shaped step, start to finish:

```fsharp
open FS.GG.Game.Core

// Positions are stored in the collision-safe `Vec2`, per the storage rule above — so this recipe needs
// the same `simPoint` crossing as *Spatial queries*. `: Point` is load-bearing, not decoration.
type Creep = { Pos: Geometry.Vec2; Hp: int }
type Tower = { Pos: Geometry.Vec2; Range: float }

let simPoint (v: Geometry.Vec2) : Point = { X = v.Vx; Y = v.Vy }

// 1. Route each creep across the map. The blocked set lives in the Model; the predicate IS the map, so
//    recompute a path only when the walls change — not every step.
let walkable (c: Cell) =
    c.Col >= 0 && c.Col < cols && c.Row >= 0 && c.Row < rows && not (walls.Contains c)
let path = Pathfinding.astar FourWay 8192 walkable spawn goal

// 2. Bucket creeps once per step; each tower then asks "who is in range?" — no O(towers × creeps) scan.
//    `SpatialGrid` buckets by the sim `Point`, so cross at the boundary — never hand it a `Vec2`.
let grid = SpatialGrid.build cellPx [ for cr in creeps -> simPoint cr.Pos, cr ]
let inRange (tower: Tower) = SpatialGrid.queryRadius (simPoint tower.Pos) tower.Range grid
```

Two decisions every step then makes — how a hit lands, and how effects pile up.

### Instant vs projectile

- **Instant (hitscan).** Resolve damage the same step you acquire the target: pick from `inRange tower`,
  subtract HP now. Deterministic and cheap, but there is no travel time — a fast creep cannot outrun it.
- **Projectile.** Spawn a moving entity, advance it on the fixed step, and test the hit with
  `Geometry.sweptIntersects` so a fast shot cannot **tunnel** past a thin creep between steps. Costs extra
  Model state and forces a decision on the mid-flight edge case: if the target dies before impact, choose
  *re-acquire nearest* vs *fizzle* — and make that choice a pure function of the world, never of arrival
  order.

Pick instant for hitscan/beam towers; pick projectile when travel time or leading the target is part of
the game feel.

### Deterministic status-effect stacking

Slows, poisons, and buffs must combine the *same way every replay*. Two rules keep them deterministic:

- **Key by effect kind and fold with an explicit policy — don't iterate a `Dictionary`.** Hold active
  effects as a `Map<EffectKind, Effect>` (or a list you sort by a total key before folding). Iterating a
  `Dictionary`/`HashSet` leaks hash order into the result and breaks replay; a `Map` folds in key order.
- **State the stacking policy per kind and keep magnitudes integer.** *Refresh* (reset duration, keep
  magnitude), *stack-additive* (sum magnitude, cap the total), and *strongest-wins* (max magnitude) are all
  valid — but pick one per kind and compute it in integers (e.g. slow as a *percent*, not `0.35`) so two
  equal-strength effects can never tie-break through floating-point equality.

```fsharp
type EffectKind = Slow | Poison
type Effect = { Kind: EffectKind; Magnitude: int; TicksLeft: int }   // integer magnitude — no float ties

// Strongest-wins magnitude, refresh-duration on re-application — one explicit policy, applied in key order.
let applyEffect (incoming: Effect) (active: Map<EffectKind, Effect>) : Map<EffectKind, Effect> =
    match Map.tryFind incoming.Kind active with
    | Some cur ->
        active
        |> Map.add incoming.Kind
            { cur with
                Magnitude = max cur.Magnitude incoming.Magnitude
                TicksLeft = max cur.TicksLeft incoming.TicksLeft }
    | None -> active |> Map.add incoming.Kind incoming

// Age by folding the Map (key-ordered → deterministic), dropping any whose TicksLeft hit 0.
let ageEffects (active: Map<EffectKind, Effect>) : Map<EffectKind, Effect> =
    (Map.empty, active)
    ||> Map.fold (fun acc kind e ->
        let e' = { e with TicksLeft = e.TicksLeft - 1 }
        if e'.TicksLeft > 0 then Map.add kind e' acc else acc)
```

## Common pitfalls

- **Mutable `System.Random` in the `Model`.** Breaks determinism and structural equality; use the
  value-type `Rng` and thread its returned state.
- **Hand-rolled BFS/A* with a non-deterministic tie-break.** Iterating a `Dictionary`/`HashSet` frontier
  leaks iteration order into the path and breaks replay. Use `Pathfinding.astar`/`bfs` — the tie-break is
  a total order over cells, so identical inputs give a byte-identical path.
- **O(n²) proximity scans.** Don't test every entity against every other; `SpatialGrid.query`/`queryRadius`
  buckets them for range/splash lookups.
- **Ignoring the returned generator.** `Rng.nextInt`/`nextFloat`/`split` return `struct(value, next)`;
  drawing again from the *input* `Rng` repeats the value. Always keep `next`.
- **Unbounded accumulator (spiral of death).** Don't run `while accumulator >= interval` by hand; let
  `FixedStep.drain` clamp the catch-up, or pass a tighter `drainWith` clamp.
- **Consumer geometry records colliding with framework `Point`/`Rect`.** As in `fs-gg-scene`: if your
  product defines its own `{ X; Y }` record, F# binds a bare `{ X = …; Y = … }` to whichever record is
  in scope last. Annotate the type or qualify fields at the boundary and convert into the framework type.
- **Two of *your own* records exposing `.Pos` (consumer-vs-consumer).** Distinct from the
  framework-vs-consumer clash above: if both a `Creep` and a `Tower` record carry a `.Pos`, a bare
  `let posOf x = x.Pos` makes F# infer the *last-declared* record for `x`, so the helper silently
  type-checks against the wrong type. Annotate the parameter — `let posOf (c: Creep) = c.Pos` — at every
  such `.Pos` (or `.Id`, `.Hp`, …) access shared by two consumer records.
- **`=` read as a named argument inside a call/tuple.** In argument position, `Some (id, dmg, id = primary.Id)`
  parses `id = primary.Id` as the *named argument* `id`, not the boolean equality you meant (and usually
  fails to compile with a confusing message). Wrap the equality in parens to force the comparison:
  `Some (id, dmg, (id = primary.Id))`.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned simulation examples.

## Evidence

Record simulation evidence (determinism replays, collision/culling cases) under this product's
`readiness/` paths. Do not copy framework readiness reports into the product.

## Package Boundary

`Geometry`, `Rng`, `FixedStep`, `Loop`, `Pathfinding`, and `SpatialGrid` (plus the sim `Point`/`Rect`/`Cell`) all
live in `FS.GG.Game.Core` (referenced only on the `game`/`sample-pack` profiles). `FS.GG.Game.Core` is the
BCL-only bottom layer — it depends on nothing and pulls in no viewer, layout, or widget machinery. Keep
rendering in `fs-gg-scene` and host wiring in `fs-gg-skiaviewer`.

## Generated Product

Thread the `Rng` through your `Model`, carry a `Loop.StepState` and drive `update` with `Loop.advance`, run
collision with `Geometry`, cull against the visible `Rect`, then hand the world your `View` interpolates by
`Loop.alpha` on as a `Scene`.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the driven library's own reference), then
community sources. If your product uses Spec Kit, record findings and resolving links under the feature's
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** line and any product-local
`docs/`. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-game:fs-gg-collision]] — detect and resolve collisions: the `Geometry` narrow phase, a `SpatialGrid` broad
  phase, and the `Resolution` response layer kept deliberately separate from detection.
- [[fs-gg-game:fs-gg-grids]] — the grid's geometry *vocabulary*: faces, edges, vertices, and the pixel↔cell map.
- [[fs-gg-game:fs-gg-line-drawing]] — `Los`: the Bresenham/supercover cell walk and discrete grid line-of-sight.
- [[fs-gg-game:fs-gg-visibility]] — `Visibility`'s continuous angular-sweep polygon and `Fov`'s symmetric shadowcasting.
- [[fs-gg-game:fs-gg-ballistics]] — swept projectile advance, lead solves, and splash over the same `Geometry` casts.
- [[fs-gg-rendering:fs-gg-scene]] — build the `Scene` the simulated world renders into.
- [[fs-gg-rendering:fs-gg-skiaviewer]] — drive the fixed-step loop from the host window.
- [[fs-gg-rendering:fs-gg-layout]] — compute the gameplay region (the visible `Rect`) entities are culled against.
- [[fs-gg-rendering:fs-gg-keyboard-input]] — map input to the `Msg` values your `update` steps the world with.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SplitMix64 (the RNG algorithm): https://prng.di.unimi.it/splitmix64.c
- Fixed-timestep loop background: https://gafferongames.com/post/fix_your_timestep/
