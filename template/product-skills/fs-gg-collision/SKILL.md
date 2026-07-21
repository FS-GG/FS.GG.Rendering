---
name: fs-gg-collision
description: Detect and resolve collisions in a generated FS.GG.UI product — broad-phase over SpatialGrid, narrow-phase over Geometry, and an adaptable response layer you own and edit.
---

# Collision (Detection + Response) Capability

## Scope

Use this skill for the **collision** half of a game/sim product: testing which bodies overlap
(broad-phase pruning + narrow-phase AABB), and **resolving** those overlaps (how far and which way
bodies separate, and slide/bounce). Detection reuses the framework primitives; the *response* rule is
game-opinionated and ships as **adaptable source you own** (`src/<ProductDir>/Collision.fs`), not a
frozen package. Everything here is pure, total, and deterministic — safe to call from a replayed
`update`. Advancing the world on a fixed step is [[fs-gg-game:fs-gg-game-core]]'s job; rendering the result is
[[fs-gg-scene]]'s. This skill materializes for the `game` and `sample-pack` profiles.

## Public Contract

The detection **and the low-level response** signatures you consume are bundled framework surfaces; the
game-opinionated *policy* layer on top of them is your own product source:

- `docs/api-surface/Game.Core/Geometry.fsi` — the `Geometry` module (box overlap / containment / swept /
  centering, plus the `*Contact` narrow-phase manifolds) on the sim `Rect`/`Point`. Shipped in
  `FS.GG.Game.Core` (`game`/`sample-pack` profiles).
- `docs/api-surface/Game.Core/Primitives.fsi` — the sim value vocabulary those helpers return and consume:
  `Contact` (the `{ Normal; Depth }` minimum-translation manifold), `Circle`, `ConvexPolygon`, `RayHit`,
  and the impulse-layer `Manifold`. This framework `Contact` is a **detection** value; your product's
  `Collision.Contact<'T>` is a *different*, body-carrying record (last bullet) — see the pitfall.
- `docs/api-surface/Game.Core/SpatialGrid.fsi` — the uniform `SpatialGrid` for broad-phase bucketing and
  range/splash queries. Also `FS.GG.Game.Core` (`game`/`sample-pack` profiles).
- `docs/api-surface/Game.Core/Resolution.fsi` — the framework `Resolution` **module** (bundled at
  Game.Core 0.3.0): `pushOut` (separate along the MTV), `slide` (remove the normal velocity component),
  `push` (discrete grid displacement — see below; `knockback` is its **deprecated** predecessor). Pure,
  total, deterministic per-body transforms — the primitives your `resolve` composes rather than
  re-deriving. This `Resolution` is a **module**; your product's `Collision.Resolution<'T>` is a
  *different*, body-carrying result **record** — see the pitfall.
- `src/<ProductDir>/Collision.fs` — **product-owned, adaptable** source: the generic `Body<'T>`/
  `Contact<'T>`/`Resolution<'T>`/`ResponseRule` shapes and `contact`/`collide`/`resolve`/`step`, plus the
  circular-hitbox movement helpers `slideCircle`/`clampCircleInside`, always reached module-qualified as
  `Collision.*`. This is the game-opinionated **policy** — which body is a wall, how bounce is booked —
  that the framework primitives above deliberately do not decide. Yours to edit or delete.

All detection helpers are **total**: degenerate inputs return a documented value, they never throw.

## Detection (narrow-phase)

`Geometry` operates on the shared `Rect`/`Point` — no hand-rolled AABB, no duplicate bounds record.

- `Geometry.intersects a b` — box-vs-box overlap on positive area (edge/corner touching is **not** an
  intersection: strict edges).
- `Geometry.sweptIntersects moving velocity target` — for a fast projectile that would **tunnel** a
  thin target in one step; tests the whole swept path, not just the endpoints. `Collision.collide`/
  `step` already sweep each body's step for you (from its `Body.Velocity`), so you rarely call this
  one directly — reach for it only in a bespoke, one-off cast outside the per-frame pass.
- `Geometry.containsPoint` / `contains` — inclusive of shared edges (containment, culling).

The helper's `Collision.contact a b` builds on `intersects` and returns the **minimum-translation
vector** (which way + how far to separate) and overlap depth, wrapped with the two bodies — a
`Collision.Contact<'T>`, not a bare boolean. If you only need the manifold (normal + depth) and not the
body pair, `Geometry.aabbContact a b` returns the framework `Contact option` directly (`None` on no
overlap).

## Broad-phase

Don't test every body against every other (O(n²)). `Collision.collide` buckets bodies once with
`SpatialGrid` and narrow-phase-tests near pairs with the **swept** contact (so a fast mover cannot
tunnel) — expanding each query region by the largest body half-extent **and the largest per-step
displacement**, so no overlap is missed, including one a fast body only touches mid-sweep (**exact**,
no false negatives). Pairs come back in ascending `(i, j)` insertion-index order, so the result is
deterministic.

```fsharp
open FS.GG.Game.Core       // Rect, Point, Geometry, SpatialGrid
// Collision lives in your product's own namespace (Collision.fs).

let bodies =
    [ { Bounds = { X = 0.0; Y = 0.0; Width = 10.0; Height = 10.0 }; Velocity = { X = 0.0; Y = 0.0 }; Tag = playerId }
      // ...enemies, bullets, walls — Velocity is this step's displacement (velocity × dt); a wall (or any
      // body at rest) is { X = 0.0; Y = 0.0 }. Tag is any id/layer payload you choose.
    ]

let contacts = Collision.collide 32.0 bodies      // cellSize tunes the grid
```

## Broadphase combat resolution (pierce + prune)

`Collision.collide` above is **body-vs-body**: it finds *mutual* overlap pairs and hands them to `resolve`
— the right tool when two bodies separate (player vs wall, two crates). **Combat** is a different shape
over the same broad-phase, and it is the one every combat milestone re-derives: each shot or hazard asks
`SpatialGrid.queryRadius` "*who am I hitting?*", spends a limited **pierce budget** over the enemies it
overlaps, applies damage, and **prunes** the dead — no separation, no `Contact`. So it consumes
`SpatialGrid` directly rather than bending `collide`/`resolve` into a damage loop.

The broad-phase is dilated and confirmed the same way `collide` does it: `queryRadius` reads each enemy's
**position**, so dilate the query by the largest enemy radius, then confirm the true circle/circle overlap
in a sqrt-free narrow phase. The inline boolean below is enough for "did it hit"; when you need the
manifold (knockback direction, penetration depth) reach for the packaged narrow phase —
`Geometry.circleContact` for circle/circle, or `Geometry.circleAabbContact` for a circle/AABB target. HP
and pierce stay **integer**, and every step is a pure fold — no mutation, no iteration order leaking in —
so the whole pass is replay-identical.

```fsharp
open FS.GG.Game.Core       // Point, Geometry, SpatialGrid

type Enemy = { Id: int; Pos: Point; Radius: float; Hp: int }
type Shot  = { Pos: Point; Radius: float; Damage: int; Pierce: int }   // Pierce = enemies still hittable

// One fixed step: build → queryRadius per shot → overlap → apply damage + spend pierce → prune.
let resolveCombat (enemies: Enemy list) (shots: Shot list) : Enemy list * Shot list =
    // 1. BUILD once — bucket this step's enemies. `queryRadius` files them in insertion order, so every
    //    query below is deterministic. `SpatialGrid.build : float -> seq<Point * 'T> -> SpatialGrid<'T>`.
    let grid = SpatialGrid.build 32.0 [ for e in enemies -> e.Pos, e ]
    let maxEnemyR = (0.0, enemies) ||> List.fold (fun m e -> max m e.Radius)

    // 2. Walk shots in list order; each spends its pierce budget over the enemies it actually overlaps,
    //    in the grid's insertion order. Accumulate damage per enemy id — never mutate an enemy in place.
    let struct (damage, keptShots) =
        (struct (Map.empty, []), shots)
        ||> List.fold (fun (struct (dmg, kept)) shot ->
            // BROAD: candidates whose POSITION is within (shot + max enemy) radius.
            //   `SpatialGrid.queryRadius : Point -> float -> SpatialGrid<'T> -> 'T list`.
            let candidates = SpatialGrid.queryRadius shot.Pos (shot.Radius + maxEnemyR) grid
            // NARROW: true circle/circle overlap, sqrt-free; take only as many as pierce allows.
            let hits =
                candidates
                |> List.filter (fun e ->
                    let dx, dy = e.Pos.X - shot.Pos.X, e.Pos.Y - shot.Pos.Y
                    let reach = shot.Radius + e.Radius
                    dx * dx + dy * dy <= reach * reach)
                |> List.truncate shot.Pierce                       // spend the pierce budget in order
            let dmg' =
                (dmg, hits)
                ||> List.fold (fun m e -> Map.add e.Id (defaultArg (Map.tryFind e.Id m) 0 + shot.Damage) m)
            let shot' = { shot with Pierce = shot.Pierce - hits.Length }
            struct (dmg', if shot'.Pierce > 0 then shot' :: kept else kept))   // drop spent shots here

    // 3. PRUNE — subtract accumulated damage, drop enemies at/below 0 HP. Spent shots already fell out.
    let survivors =
        enemies
        |> List.choose (fun e ->
            let hp = e.Hp - defaultArg (Map.tryFind e.Id damage) 0
            if hp > 0 then Some { e with Hp = hp } else None)

    survivors, List.rev keptShots
```

Two things keep this deterministic and are easy to lose: process shots in a **total order** (list order
here — never a `Dictionary`/`HashSet`), and truncate the overlaps to the pierce budget *after* the
insertion-ordered `filter`, so which enemies a piercing shot spends itself on is a pure function of build
order, not of arrival. A wide `Damage` fold over one enemy id then survives the prune as a single
subtraction, so two shots landing the same step compose the same way on every replay. Positions here are
the sim `Point`; a product that stores them in the collision-safe `Geometry.Vec2` crosses at the boundary
with its own `simPoint` ([[fs-gg-game:fs-gg-game-core]] *Spatial queries*).

## Response

`resolve` is the game-opinionated part — **this is the line to edit.** It turns a `Collision.Contact<'T>`
into a `Collision.Resolution<'T>` (the separated bodies + the displacement applied). It is the *policy*
that sits on top of the framework `Resolution` primitives (`pushOut`/`slide`, and `push` for grid
displacement) — compose those inside `resolve` rather than re-deriving separation math:

- `SeparateEqually` — split the push 50/50 (both bodies move).
- `PushFirst` / `PushSecond` — one body is a wall; the other takes the full push.
- `Slide` — 50/50 separation, no recorded restitution.
- `Bounce restitutionPercent` — 50/50 separation plus a normalized restitution (integer percent, so
  two equal bounces never tie-break through floating-point) the consumer folds into its own velocity
  step. (`collide`/`step` read each `Body.Velocity` to sweep the step for *detection*; integrating
  velocities into new positions is still *your* job — the helper only separates.)

```fsharp
// One per-frame pass: detect + resolve, deterministic pair order.
let resolutions = Collision.step Collision.SeparateEqually 32.0 bodies
// Fold each resolution's separated bodies back into your Model.
```

## Moving a circular hitbox against walls — `slideCircle`

`Body`/`collide`/`step` above resolve **AABB-body-vs-AABB-body**. A **circular** mover — a player
hitbox, a ball — sliding against static walls is a *different* narrow-phase, so faking the disc as its
bounding box catches on corners and feels wrong. `Collision.slideCircle` is that case: it moves a
`Circle` by a per-step `displacement` (velocity × dt) against **static** AABB `walls`, resolving the X
move and the Y move **independently** so a wall that stops one axis leaves the other free — the "slide
along the wall" feel. Detection reuses the framework `Geometry.circleAabbContact` primitive (clamp
centre to box, squared-distance test) — no hand-rolled circle math, matching `contact`. An optional
`bounds` clamps the final centre inside the playfield (inset by the radius) via `clampCircleInside`.

```fsharp
open FS.GG.Game.Core       // Rect, Point, Circle, Geometry

let walls  = [ { X = 100.0; Y = 0.0; Width = 20.0; Height = 200.0 } ]   // immovable, stable order
let bounds = Some { X = 0.0; Y = 0.0; Width = 640.0; Height = 360.0 }   // or None for no clamp
let player = { Center = { X = 80.0; Y = 50.0 }; Radius = 13.0 }

// velocity × dt for this fixed step; X is blocked by the wall, Y still slides.
let moved = Collision.slideCircle bounds walls player { X = 6.0; Y = 4.0 }
// fold moved.Center back into your Model — the radius is unchanged.
```

Pure, total (NaN-safe), and deterministic (walls fold in list order). It is a single **move-and-resolve**
step, **not** a swept cast: it lands the disc on a wall's near face only while the moved centre stays in
that wall's near half — always true for a player hitbox against tile-sized walls, whose per-step
displacement is well under the wall thickness. A mover fast enough to overshoot a wall's midline in one
step is a **projectile**, not a hitbox: use the swept `collide`/`step` pass (it reads `Body.Velocity`,
FS.GG.Rendering#290), or call `slideCircle` in sub-steps each no longer than the radius so consecutive
discs overlap. This is the deliberate boundary of the helper, not a silent gap — see the pitfall below.
<!-- skill-refs: closed-ok FS.GG.Rendering#290 — cited as the issue that ESTABLISHED the swept collide/step pass (why a hitbox helper need not sweep), not as somewhere to go. Closed is correct; it stays closed. File-scoped, so it honours the ref in the pitfall below too. -->

## Shoving a unit across a grid — `Resolution.push`

`pushOut`/`slide` are continuous. On a **tile grid** — a knockback stat, a shove, a shockwave — the
primitive is `Resolution.push`: advance from `start` by the per-cell delta `step`, up to `distance`
cells, asking a classifier what each *next* cell does.

The classifier is the whole coupling to your world. It absorbs terrain, occupancy and board bounds
without `Resolution` learning any of them, and it answers with one of **three** `Resolution.CellStep`
states — because two are not enough:

- `Resolution.Enter` — move onto it and keep going. Ordinary ground; also lava, which is entered, hurts, and is left.
- `Resolution.Stop` — move onto it and **halt there**. Water, a chasm: a destination, usually fatal.
- `Resolution.Block` — cannot enter; halt on the *previous* cell. A wall, a mountain, an occupied cell, off-board.

A binary `blocked` predicate can only say *stop before it* or *walk through it*. It cannot say **enter
it and stop there** — so mark water blocked and your unit halts on dry land; mark it passable and the
unit walks out the far side. Neither is the game. That is why `knockback`, which took exactly such a
predicate, is deprecated.

**Qualify the cases.** `Resolution` is `[<RequireQualifiedAccess>]` and `CellStep`/`PushStop` are
declared inside it, so a bare `Enter`/`Block` does not resolve — and you cannot `open Resolution` to
make it (RQA forbids exactly that). Write `Resolution.Enter`, and match on `Resolution.Stopped`.

```fsharp
open FS.GG.Game.Core       // Cell, Resolution

// Shove the target 2 cells along `step`. The lambda is your world; `Resolution` never learns it.
let shove =
    Resolution.push target step 2 (fun cell ->
        if not (board.Contains cell) then Resolution.Block
        elif board.IsWall cell || board.Occupied cell then Resolution.Block
        elif board.IsWater cell then Resolution.Stop
        else Resolution.Enter)

shove.Final      // the cell it occupies now
shove.Entered    // every cell actually ENTERED, in order, excluding `start`

match shove.Outcome with
| Resolution.Completed -> ()                     // all `distance` steps taken; nothing interrupted it
| Resolution.Stopped cell -> drown unit cell     // it ENTERED `cell` and halted there — it occupies it
| Resolution.Blocked cell -> bruise unit cell    // it could NOT enter `cell`; it occupies `shove.Final`
```

`Entered` is what a per-cell terrain tick folds over — a unit shoved across two lava tiles takes the
tick twice — and it is exactly what `knockback` threw away. `Outcome` carries the cell that ended the
walk, so you can attribute collision damage to the obstacle, or drown the unit in the water it landed
in.

**`distance` bounds MEMORY, not just the loop.** `push` is total — it terminates for every `distance` —
but `Entered` accumulates one `Cell` per entered cell, so a `distance` of `Int32.MaxValue` **exhausts
memory** rather than merely running long. The module deliberately does not clamp it: a maximum push
distance is a game-defining parameter, not a module constant. It is 1–3 in every game in the corpus. If
you derive `distance` from content — a knockback stat, a designer's field — **bound it where that
content is authored.**

Pushing several units into one another is order-dependent **by design**: you sequence them, and close
the classifier over the occupancy each push updates.

## The adaptable helper

`Collision.fs` is **yours** — a small, readable file classified *replaceable* in the scaffold map
(see [[fs-gg-game:fs-gg-model-swap]]). Change the response rule, add collision layers/masks over `Tag`, or delete
the file if you don't need it: its `Compile` item is `Exists`-guarded, so the build stays green and you
never touch the durable `Product.fsproj`.

## Common pitfalls

- **Consumer geometry records colliding with framework `Point`/`Rect`.** As in [[fs-gg-scene]]: a bare
  `{ X = …; Y = … }` binds to whichever record is in scope last. Reuse the framework `Rect`/`Point`;
  don't define a look-alike bounds/vector type.
- **Reaching for the deprecated `knockback`.** It is `[<Obsolete>]`: a binary predicate cannot express
  a cell that is entered *and* ends the walk, and it discards both the stop reason and the cells crossed.
  `Resolution.push` is the replacement and is strictly more informative — the old `knockback` is exactly
  `(Resolution.push start step distance (fun c -> if blocked c then Resolution.Block else Resolution.Enter)).Final`.
  Reach for `push`.
- **Writing a bare `Enter`/`Stop`/`Block`.** `Resolution` is `[<RequireQualifiedAccess>]`, so the
  `CellStep` cases declared inside it are only reachable as `Resolution.Enter` — and RQA forbids the
  `open Resolution` that would otherwise import them. A bare case is `error FS0039: The value or
  constructor 'Block' is not defined`, which reads like a missing package and is not one.
- **Framework `Resolution`/`Contact` vs your `Collision.Resolution`/`Contact`.** Game.Core 0.3.0 bundles
  a `Resolution` *module* (`pushOut`/`slide`/`push`) and a `Contact` *manifold* type; your
  `Collision.fs` ships its own generic `Resolution<'T>`/`Contact<'T>` *records*. Same names, different
  things. Reach the framework ones only through their module (`Resolution.slide`, a `Geometry.*Contact`
  return) and your own only through `Collision.` (`Collision.resolve`, `Collision.Contact`); never `open`
  both into one unqualified scope, or the name binds to whichever came last.
- **Two of your own records exposing `.Pos`/`.Id` (consumer-vs-consumer).** Annotate the parameter —
  `let posOf (c: Creep) = c.Pos` — so the helper doesn't silently infer the wrong record. `Body.Tag`
  is generic precisely so you don't need a second id-carrying record.
- **O(n²) proximity scans.** Use `Collision.collide`/`SpatialGrid`, not a nested loop over all bodies.
- **Non-deterministic response.** Keep `resolve` a pure function of the world; don't iterate a
  `Dictionary`/`HashSet` and don't introduce a `sqrt`/normalization into the response math — the
  built-in MTV is sqrt-free so output is byte-identical across runs/platforms (safe under replay).
- **Expecting `step` to fully de-stack a pile in one call.** It is a single positional pass per frame;
  for dense stacking, call it again on the resolved bodies or add your own iteration.
- **Driving a fast PROJECTILE through `slideCircle`.** It is a single move-and-resolve step for a
  player-speed *hitbox*, not a swept cast: a mover that overshoots a wall's midline in one step tunnels.
  A projectile is what the swept `collide`/`step` pass (via `Body.Velocity`, FS.GG.Rendering#290) is for; or sub-step
  `slideCircle` with each chunk no longer than the radius so consecutive discs overlap. Do not raise the
  per-step displacement of a hitbox past its wall thickness and expect a wall to stop it.
- **Deleting `Collision.fs` and then editing `Product.fsproj`.** You don't need to — the compile item
  is `Exists`-guarded. Leave `Product.fsproj` alone.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned collision examples (assert the `Contact`/
`Resolution` your `collide`/`step` produces for representative overlaps; determinism replays).

## Evidence

Record collision evidence (overlap/resolution cases, determinism replays) under this product's
`readiness/` paths. Do not copy framework readiness reports into the product.

## Package Boundary

`Geometry`, `SpatialGrid`, and `Resolution` (plus the sim `Rect`/`Point`/`Contact` in `Primitives`) are in
`FS.GG.Game.Core` (referenced only on the `game`/`sample-pack` profiles). `Collision.fs` is
**product-owned source with no backing package** — the policy layer, not the primitives. Keep
rendering in [[fs-gg-scene]] and host wiring in [[fs-gg-skiaviewer]].

## Generated Product

Build a `Body` list from your world each fixed step, `Collision.step` it under your chosen rule, fold
the resolved bodies back into your `Model`, then hand the world to your `View` as a `Scene`.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the driven library's own reference), then
community sources. If your product uses Spec Kit, record findings and resolving links under the feature's
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** line and any product-local
`docs/`. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-game:fs-gg-game-core]] — the simulation loop (fixed step, RNG, culling, pathfinding, spatial queries)
  that drives the world `Collision.step` resolves each frame.
- [[fs-gg-scene]] — owns the shared `Rect`/`Point` collision operates on; renders the resolved world.
- [[fs-gg-skiaviewer]] — drives the fixed-step loop from the host window.
- [[fs-gg-game:fs-gg-model-swap]] — classifies `Collision.fs` as replaceable/adaptable source.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- AABB collision + minimum-translation vector background: https://gamedev.stackexchange.com/q/29786
- Fixed-timestep loop background: https://gafferongames.com/post/fix_your_timestep/
