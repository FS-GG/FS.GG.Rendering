---
name: fs-gg-visibility
description: Compute 2D visibility in a generated FS.GG.UI product — the angular-sweep visibility polygon (line-of-sight, field-of-view, fog-of-war, 2D lighting) over an adaptable helper you own, reusing Point/SpatialGrid.
---

# 2D Visibility Capability

## Scope

Use this skill for **2D visibility** in a game/sim product: answering *"what can be seen from here?"* —
the region visible from a viewpoint given a set of wall segments (the **visibility polygon**), plus the
point-to-point **line-of-sight** query built on the same core. It is the workhorse behind line-of-sight,
field-of-view, fog-of-war, and 2D light/shadow. The geometry vocabulary reuses the framework primitives;
the ray-segment intersection and the **angular sweep** are game-opinionated and ship as **adaptable
source you own** (`src/<ProductDir>/Visibility.fs`), not a frozen package. Everything here is pure,
total, deterministic, and bounded — safe to call from a replayed `update`/`view`. The algorithm is the
classic angular sweep from the Red Blob Games reference (see **Sources**). Advancing the world on a fixed
step is [[fs-gg-game-core]]'s job; rendering the polygon is [[fs-gg-scene]]'s. This skill materializes
for the `game` and `sample-pack` profiles.

## Public Contract

The geometry vocabulary you consume is bundled framework surface; the visibility layer is your own
product source:

- `docs/api-surface/Game.Core/Primitives.fsi` — the sim `Point`/`Rect` (positions, ray directions, hit
  vertices, the bound box), with the `Geometry` helpers in `docs/api-surface/Game.Core/Geometry.fsi`.
  Shipped in `FS.GG.Game.Core` (`game`/`sample-pack` profiles).
- `docs/api-surface/Game.Core/SpatialGrid.fsi` — the uniform `SpatialGrid` for broad-phase culling of
  nearby occluders (`build`/`query`/`queryRadius`). Also `FS.GG.Game.Core` (`game`/`sample-pack`).
- `src/<ProductDir>/Visibility.fs` — **product-owned, adaptable** source: the `Segment`/`Settings`/
  `VisibilityPolygon` shapes and `raySegment`/`isVisible`/`polygon`. Yours to edit or delete.

All entry points are **total**: degenerate inputs return a documented value, they never throw or emit a
NaN coordinate.

## The world model

An occluder is a `Segment` — a wall between two shared `Point`s (`{ A; B }`). `Segment` is the one
concept the shared vocabulary lacks (`Point` is a location, `Rect` an AABB); it is a **pair of shared
`Point`s**, deliberately not a look-alike vector type. A zero-length segment occludes nothing. Build your
wall list from your world each frame (tile edges, polygon boundaries, dynamic blockers).

## Broad-phase cull

Don't ray-test every wall in the world. `polygon` buckets segment endpoints once with `SpatialGrid` and
culls to the occluders inside the sight **bound box** (`source ± Settings.Radius`) — reusing the
framework broad-phase, no hand-rolled bucketing. `Settings.Radius` is a single knob: it is the cull
region **and** the ray bound, so the two can never disagree.

```fsharp
open FS.GG.Game.Core       // Point, Rect, Geometry, SpatialGrid
// Visibility lives in your product's own namespace (Visibility.fs).

let walls =
    [ { A = { X = 5.0; Y = -5.0 }; B = { X = 5.0; Y = 5.0 } }
      // ...more wall segments from your world
    ]

let poly = Visibility.polygon { Radius = 200.0; CellSize = 32.0 } source walls
```

## The angular sweep

`polygon` implements the Red Blob Games sweep: collect the occluder corners, shoot a ray at each corner
(and one either side, to slip past it), keep the **nearest** wall hit per ray, and order the hits into a
closed ring around the source. Rays that strike no wall terminate on the bound-box edges (added as
synthetic walls), so the polygon is always finite and closed.

**Determinism is by design, not `atan2`.** The sweep orders hits with a **cross-product angular
comparator** (half-plane + cross-product sign) and a squared-distance-then-integer-index tiebreak, and
picks the nearest hit by the sqrt-free parametric `t`. No transcendental, no hash iteration — identical
inputs yield a byte-identical polygon across runs and platforms (safe under replay).

## The visibility polygon

`Visibility.polygon` returns a `VisibilityPolygon` — the `Source` and an ordered, closed, CCW ring of
`Vertices` bounded by `Radius`. It is a **region**, not a boolean: fill it as a 2D light, rasterize it
into a fog-of-war mask, or point-test against it. For a single yes/no query, `Visibility.isVisible
source target walls` is the exact line-of-sight convenience built on the same `raySegment` core.

## Applications

- **Line-of-sight** — `isVisible` (can an enemy see the player?).
- **Field-of-view** — cone the sweep to an angular range (edit `polygon` to clamp ray directions).
- **Fog-of-war** — rasterize the polygon into a visited/visible mask each step.
- **2D lighting / soft shadows** — fill the polygon as a light; layer several sources.

## The adaptable helper

`Visibility.fs` is **yours** — a small, readable file classified *replaceable* in the scaffold map (see
[[fs-gg-model-swap]]). Change the sight radius, cone the FOV, swap the polygon output for a per-cell
mask, or delete the file if you don't need it: its `Compile` item is `Exists`-guarded, so the build
stays green and you never touch the durable `Product.fsproj`.

## Common pitfalls

- **Consumer geometry records colliding with framework `Point`/`Rect`.** As in [[fs-gg-scene]]: a bare
  `{ X = …; Y = … }` binds to whichever record is in scope last. Reuse the framework `Point`/`Rect`;
  don't define a look-alike point/vector type. `Segment` is a pair of shared `Point`s, not a new vector.
- **Sorting the sweep by `atan2`.** The reference article uses `atan2` for clarity, but its last bit can
  differ across runtimes and flip two near-collinear corners — breaking replay determinism. Keep the
  cross-product comparator (it is already the default); only fall back to `atan2` for a purely cosmetic,
  non-replayed light.
- **O(segments) scans without the cull.** Route occluders through `SpatialGrid` (as `polygon` does), not
  a nested loop over every wall.
- **Unbounded rays.** Every ray must terminate on the `source ± Radius` bound; forgetting the bound
  yields an open polygon. `Settings.Radius` drives both the bound and the cull — keep them one value.
- **Deleting `Visibility.fs` and then editing `Product.fsproj`.** You don't need to — the compile item
  is `Exists`-guarded. Leave `Product.fsproj` alone.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned visibility examples (assert an occluder hides a
target and that removing it restores sight; determinism replays; bound/totality cases).

## Evidence

Record visibility evidence (occlusion cases, determinism replays, bound/totality) under this product's
`readiness/` paths. Do not copy framework readiness reports into the product.

## Package Boundary

`Point`/`Rect`/`Geometry` and `SpatialGrid` are in `FS.GG.Game.Core` (referenced only
on the `game`/`sample-pack` profiles). `Visibility.fs` is **product-owned source with no backing
package**. Keep rendering in [[fs-gg-scene]] and host wiring in [[fs-gg-skiaviewer]].

## Generated Product

Build a `Segment` wall list from your world each fixed step, call `Visibility.polygon` from your
`update`/`view`, and hand the polygon to your `View` — fill it as a light, mask it for fog-of-war, or
point-test it for line-of-sight. Pair it with [[fs-gg-collision]] for a full geometry pass.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the Red Blob Games reference), then
community sources. If your product uses Spec Kit, record findings and resolving links under the feature's
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** line and any product-local
`docs/`. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-collision]] — the sibling per-frame geometry pass (detection + response) that shares the
  `Point`/`Rect`/`SpatialGrid` vocabulary.
- [[fs-gg-game-core]] — the simulation loop (fixed step, RNG, culling, pathfinding) that drives the world
  visibility is computed over.
- [[fs-gg-scene]] — owns the shared `Point`/`Rect` visibility operates on; renders the polygon.
- [[fs-gg-skiaviewer]] — drives the fixed-step loop from the host window.
- [[fs-gg-model-swap]] — classifies `Visibility.fs` as replaceable/adaptable source.

## Sources / links

- Red Blob Games, "2D Visibility": https://www.redblobgames.com/articles/visibility/
- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Ray-segment intersection background: https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection
