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

- `docs/api-surface/Scene/Scene.fsi` — the `Geometry` module (collision / containment / centering), on
  the shared `Rect`/`Point`. Shipped in `FS.GG.UI.Scene` (already referenced by every profile).
- `docs/api-surface/Canvas/Rng.fsi` and `docs/api-surface/Canvas/FixedStep.fsi` — the `Rng` value type
  and the `FixedStep` accumulator drain. Shipped in `FS.GG.UI.Canvas`, referenced on the `game` and
  `sample-pack` profiles.
- `docs/api-surface/Canvas/Pathfinding.fsi` and `docs/api-surface/Canvas/SpatialGrid.fsi` — deterministic
  grid `Pathfinding` (A*/BFS over a walkability predicate) and the uniform `SpatialGrid` for range/splash
  queries. Also `FS.GG.UI.Canvas`, same profiles — reuse these instead of hand-rolling BFS/A* or bucketing.

Every draw returns a `struct` tuple `(value, nextState)` — deconstruct with `let struct(v, next) = …`.
All helpers are **total**: degenerate inputs return a documented value, they never throw.

## Fixed-timestep march

`FixedStep.drain interval frameTime accumulator` returns `struct(stepCount, newAccumulator)` — the whole
number of fixed steps to run this frame and the carried remainder (all in seconds). It is pure: a
scripted `frameTime` sequence reproduces identical results. A stalled frame is clamped to
`defaultMaxFrameTime` (0.25 s) so the loop can't spiral; pass a tighter clamp with
`drainWith maxFrameTime interval frameTime accumulator`.

```fsharp
open FS.GG.UI.Canvas

// Run world-updates at a fixed 60 Hz regardless of render cadence.
let stepHz = 1.0 / 60.0

let advance (dt: float) (accumulator: float) (world: 'w) (stepWorld: 'w -> 'w) : 'w * float =
    let struct (steps, carry) = FixedStep.drain stepHz dt accumulator
    // mutable: single unaliased accumulator over a fixed step count — plainer than a fold.
    let mutable w = world
    for _ in 1 .. steps do
        w <- stepWorld w
    w, carry
```

## RNG determinism

`Rng` is a value type (`{ State: uint64 }`) — a SplitMix64 generator you **store in your MVU `Model`**
and thread through `update`. Because two `Rng` with equal state are equal and produce identical
continuations, structural equality of a `Model` implies equal RNG state, so replay and clone stay
deterministic. Never put a mutable `System.Random` in the `Model` — it breaks determinism and structural
equality.

```fsharp
open FS.GG.UI.Canvas

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

`Geometry` operates on the shared `Rect`/`Point` — no hand-rolled AABB, no duplicate bounds record.

- `Geometry.intersects a b` — box-vs-box overlap on positive area (edge/corner touching is **not** an
  intersection: strict edges).
- `Geometry.containsPoint rect point` / `Geometry.contains outer inner` — inclusive of shared edges.
- `Geometry.sweptIntersects moving velocity target` — for a fast projectile that would **tunnel** a thin
  target in one step; tests the whole swept path, not just the endpoints.
- `Geometry.center rect` / `Geometry.ofCenter center w h` — round-trip centering.

```fsharp
open FS.GG.UI.Scene

let bullet : Rect = Geometry.ofCenter { X = 100.0; Y = 40.0 } 2.0 6.0
let vel : Point = { X = 0.0; Y = 900.0 }            // fast — use the swept test
let enemy : Rect = { X = 90.0; Y = 300.0; Width = 24.0; Height = 8.0 }

let hit = Geometry.sweptIntersects bullet vel enemy
```

## Culling

Keep only the entities whose bounds meet the visible region — an `intersects` (or `containsPoint`)
test against the gameplay `Rect`, reusing the same shared type:

```fsharp
open FS.GG.UI.Scene

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
open FS.GG.UI.Canvas

let blocked = set [ (2, 0); (2, 1); (2, 2) ]                     // a wall your Model owns
let walkable (c: Cell) = c.Col >= 0 && c.Col < 8 && c.Row >= 0 && c.Row < 8 && not (blocked.Contains(c.Col, c.Row))

let route = Pathfinding.astar EightWay 4096 walkable { Col = 0; Row = 0 } { Col = 7; Row = 3 }
```

## Spatial queries (range / splash)

`SpatialGrid` buckets positioned items once and answers "what is near here" without an O(n²) scan — the
uniform grid the perf guidance recommends, now shipped. Built from a cell size and `(Scene.Point * 'item)`
pairs; queries are **exact** (no false positives/negatives) and return items in **insertion order**.

- `SpatialGrid.build cellSize items` — bucket once (hold the grid in your `Model` or rebuild per frame).
- `SpatialGrid.query region grid` — items inside a `Rect` (broad-phase collision / on-screen set).
- `SpatialGrid.queryRadius center radius grid` — items within a radius (splash damage / proximity).

```fsharp
open FS.GG.UI.Canvas
open FS.GG.UI.Scene

let grid = SpatialGrid.build 32.0 [ for e in enemies -> e.Pos, e.Id ]
let splashed = SpatialGrid.queryRadius blast 48.0 grid            // ids to damage
```

## Grid-sim recipe

The primitives above compose into the loop a grid game actually runs each fixed step. Nothing here reads
the clock or touches shared state, so the whole `stepWorld` stays pure and replay-identical — which is the
point of keeping the `Rng`, the grid, and the effect table *in the `Model`*.

A tower-defense-shaped step, start to finish:

```fsharp
open FS.GG.UI.Canvas
open FS.GG.UI.Scene

// 1. Route each creep across the map. The blocked set lives in the Model; the predicate IS the map, so
//    recompute a path only when the walls change — not every step.
let walkable (c: Cell) =
    c.Col >= 0 && c.Col < cols && c.Row >= 0 && c.Row < rows && not (walls.Contains c)
let path = Pathfinding.astar FourWay 8192 walkable spawn goal

// 2. Bucket creeps once per step; each tower then asks "who is in range?" — no O(towers × creeps) scan.
let grid = SpatialGrid.build cellPx [ for cr in creeps -> cr.Pos, cr ]
let inRange (tower: Tower) = SpatialGrid.queryRadius tower.Pos tower.Range grid
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

`Geometry` is in `FS.GG.UI.Scene`; `Rng`/`FixedStep`/`Pathfinding`/`SpatialGrid` are in `FS.GG.UI.Canvas`
(referenced only on the `game`/`sample-pack` profiles). Canvas depends only on Scene — the simulation
primitives pull in no viewer, layout, or widget machinery. Keep rendering in `fs-gg-scene` and host wiring
in `fs-gg-skiaviewer`.

## Generated Product

Thread the `Rng` through your `Model`, drive `update` on the `FixedStep.drain` step count, run collision
with `Geometry`, cull against the visible `Rect`, then hand the surviving world to your `View` as a
`Scene`.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the driven library's own reference), then
community sources. If your product uses Spec Kit, record findings and resolving links under the feature's
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** line and any product-local
`docs/`. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-scene]] — build the `Scene` the simulated world renders into; owns the shared `Rect`/`Point`.
- [[fs-gg-skiaviewer]] — drive the fixed-step loop from the host window.
- [[fs-gg-layout]] — compute the gameplay region (the visible `Rect`) entities are culled against.
- [[fs-gg-keyboard-input]] — map input to the `Msg` values your `update` steps the world with.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SplitMix64 (the RNG algorithm): https://prng.di.unimi.it/splitmix64.c
- Fixed-timestep loop background: https://gafferongames.com/post/fix_your_timestep/
