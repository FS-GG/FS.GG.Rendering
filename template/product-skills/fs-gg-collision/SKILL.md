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

The detection signatures you consume are bundled framework surfaces; the response layer is your own
product source:

- `docs/api-surface/Scene/Scene.fsi` — the `Geometry` module (box overlap / containment / swept /
  centering) on the shared `Rect`/`Point`. Shipped in `FS.GG.UI.Scene` (referenced by every profile).
- `docs/api-surface/Canvas/SpatialGrid.fsi` — the uniform `SpatialGrid` for broad-phase bucketing and
  range/splash queries. Shipped in `FS.GG.UI.Canvas` (`game`/`sample-pack` profiles).
- `src/<ProductDir>/Collision.fs` — **product-owned, adaptable** source: the `Body`/`Contact`/
  `Resolution`/`ResponseRule` shapes and `contact`/`collide`/`resolve`/`step`. Yours to edit or delete.

All detection helpers are **total**: degenerate inputs return a documented value, they never throw.

## Detection (narrow-phase)

`Geometry` operates on the shared `Rect`/`Point` — no hand-rolled AABB, no duplicate bounds record.

- `Geometry.intersects a b` — box-vs-box overlap on positive area (edge/corner touching is **not** an
  intersection: strict edges).
- `Geometry.sweptIntersects moving velocity target` — for a fast projectile that would **tunnel** a
  thin target in one step; tests the whole swept path, not just the endpoints.
- `Geometry.containsPoint` / `contains` — inclusive of shared edges (containment, culling).

The helper's `Collision.contact a b` builds on `intersects` and returns the **minimum-translation
vector** (which way + how far to separate) and overlap depth — a `Contact`, not a bare boolean.

## Broad-phase

Don't test every body against every other (O(n²)). `Collision.collide` buckets bodies once with
`SpatialGrid` and only narrow-phase-tests near pairs — expanding each query region by the largest body
half-extent so no overlap is missed (**exact**, no false negatives). Pairs come back in ascending
`(i, j)` insertion-index order, so the result is deterministic.

```fsharp
open FS.GG.UI.Scene       // Rect, Point
// Collision lives in your product's own namespace (Collision.fs).

let bodies =
    [ { Bounds = { X = 0.0; Y = 0.0; Width = 10.0; Height = 10.0 }; Tag = playerId }
      // ...enemies, bullets, walls — Tag is any id/layer payload you choose
    ]

let contacts = Collision.collide 32.0 bodies      // cellSize tunes the grid
```

## Response

`resolve` is the game-opinionated part — **this is the line to edit.** It turns a `Contact` into a
`Resolution` (the separated bodies + the displacement applied):

- `SeparateEqually` — split the push 50/50 (both bodies move).
- `PushFirst` / `PushSecond` — one body is a wall; the other takes the full push.
- `Slide` — 50/50 separation, no recorded restitution.
- `Bounce restitutionPercent` — 50/50 separation plus a normalized restitution (integer percent, so
  two equal bounces never tie-break through floating-point) the consumer folds into its own velocity
  step. (Velocity integration itself is *your* job — the helper only separates.)

```fsharp
// One per-frame pass: detect + resolve, deterministic pair order.
let resolutions = Collision.step Collision.SeparateEqually 32.0 bodies
// Fold each resolution's separated bodies back into your Model.
```

## The adaptable helper

`Collision.fs` is **yours** — a small, readable file classified *replaceable* in the scaffold map
(see [[fs-gg-model-swap]]). Change the response rule, add collision layers/masks over `Tag`, or delete
the file if you don't need it: its `Compile` item is `Exists`-guarded, so the build stays green and you
never touch the durable `Product.fsproj`.

## Common pitfalls

- **Consumer geometry records colliding with framework `Point`/`Rect`.** As in [[fs-gg-scene]]: a bare
  `{ X = …; Y = … }` binds to whichever record is in scope last. Reuse the framework `Rect`/`Point`;
  don't define a look-alike bounds/vector type.
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

`Geometry` is in `FS.GG.UI.Scene`; `SpatialGrid` is in `FS.GG.UI.Canvas` (referenced only on the
`game`/`sample-pack` profiles). `Collision.fs` is **product-owned source with no backing package**. Keep
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
