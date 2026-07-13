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
`update`. Advancing the world on a fixed step is [[fs-gg-game-core]]'s job; rendering the result is
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
  `Contact<'T>`/`Resolution<'T>`/`ResponseRule` shapes and `contact`/`collide`/`resolve`/`step`, always
  reached module-qualified as `Collision.*`. This is the game-opinionated **policy** — which body is a
  wall, how bounce is booked — that the framework primitives above deliberately do not decide. Yours to
  edit or delete.

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

## Shoving a unit across a grid — `Resolution.push`

`pushOut`/`slide` are continuous. On a **tile grid** — a knockback stat, a shove, a shockwave — the
primitive is `Resolution.push`: advance from `start` by the per-cell delta `step`, up to `distance`
cells, asking a classifier what each *next* cell does.

The classifier is the whole coupling to your world. It absorbs terrain, occupancy and board bounds
without `Resolution` learning any of them, and it answers with one of **three** states — because two
are not enough:

- `Enter` — move onto it and keep going. Ordinary ground; also lava, which is entered, hurts, and is left.
- `Stop` — move onto it and **halt there**. Water, a chasm: a destination, usually fatal.
- `Block` — cannot enter; halt on the *previous* cell. A wall, a mountain, an occupied cell, off-board.

A binary `blocked` predicate can only say *stop before it* or *walk through it*. It cannot say **enter
it and stop there** — so mark water blocked and your unit halts on dry land; mark it passable and the
unit walks out the far side. Neither is the game. That is why `knockback`, which took exactly such a
predicate, is deprecated.

```fsharp
open FS.GG.Game.Core       // Cell, Resolution

// Shove the target 2 cells along `step`. The lambda is your world; `Resolution` never learns it.
let shove =
    Resolution.push target step 2 (fun cell ->
        if not (board.Contains cell) then Block
        elif board.IsWall cell || board.Occupied cell then Block
        elif board.IsWater cell then Stop
        else Enter)

shove.Final      // the cell it occupies now
shove.Entered    // every cell actually ENTERED, in order, excluding `start`
shove.Outcome    // Completed | Stopped of Cell | Blocked of Cell — why the walk ended, and where
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
(see [[fs-gg-model-swap]]). Change the response rule, add collision layers/masks over `Tag`, or delete
the file if you don't need it: its `Compile` item is `Exists`-guarded, so the build stays green and you
never touch the durable `Product.fsproj`.

## Common pitfalls

- **Consumer geometry records colliding with framework `Point`/`Rect`.** As in [[fs-gg-scene]]: a bare
  `{ X = …; Y = … }` binds to whichever record is in scope last. Reuse the framework `Rect`/`Point`;
  don't define a look-alike bounds/vector type.
- **Reaching for the deprecated `knockback`.** It is `[<Obsolete>]`: a binary predicate cannot express
  a cell that is entered *and* ends the walk, and it discards both the stop reason and the cells crossed.
  `Resolution.push` is the replacement and is strictly more informative —
  `(push start step distance (fun c -> if blocked c then Block else Enter)).Final` *is* the old
  `knockback`. Reach for `push`.
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

- [[fs-gg-game-core]] — the simulation loop (fixed step, RNG, culling, pathfinding, spatial queries)
  that drives the world `Collision.step` resolves each frame.
- [[fs-gg-scene]] — owns the shared `Rect`/`Point` collision operates on; renders the resolved world.
- [[fs-gg-skiaviewer]] — drives the fixed-step loop from the host window.
- [[fs-gg-model-swap]] — classifies `Collision.fs` as replaceable/adaptable source.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- AABB collision + minimum-translation vector background: https://gamedev.stackexchange.com/q/29786
- Fixed-timestep loop background: https://gafferongames.com/post/fix_your_timestep/
