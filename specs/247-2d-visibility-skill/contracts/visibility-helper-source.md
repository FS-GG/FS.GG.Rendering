# Contract: Visibility Helper Source (`Visibility.fs`)

This is **product-owned adaptable source**, not a frozen package `.fsi`. The "contract" is the shape the
consumer receives on scaffold and the invariants the shipped default guarantees — all of which the
consumer may then edit. Types/fields are defined in [../data-model.md](../data-model.md).

## Namespace / module

Materialized into `src/<ProductDir>/Visibility.fs` with `sourceName` substitution (`Product` by default):

```fsharp
namespace <ProductName>          // e.g. namespace Product

open FS.GG.UI.Scene              // Point, Rect, Geometry
open FS.GG.UI.Canvas             // SpatialGrid

/// Product-owned 2D-visibility helper — YOURS to adapt. The geometry vocabulary reuses
/// Point/Rect/Geometry/SpatialGrid; the ray-segment intersection and the angular sweep below are the
/// lines to edit (radius, FOV cone, polygon-vs-mask output). Delete this file freely if you don't need
/// it. Algorithm reference: https://www.redblobgames.com/articles/visibility/
module Visibility =
    ...
```

## Intended surface (what the consumer gets)

> Signatures are the *default* the consumer receives; they are editable, not surface-baselined.

```fsharp
/// A wall / occluder between two shared Points (the one concept Point/Rect don't express).
type Segment  = { A: Point; B: Point }

/// The editable knobs: sight radius (also the ray bound AND the cull radius) + grid cell size.
type Settings = { Radius: float; CellSize: float }

/// The visible region from a source: a closed CCW ring of hit points, bounded by Radius.
type VisibilityPolygon = { Source: Point; Vertices: Point list }

/// Nearest ray-segment hit: the point struck and the parametric distance t (>= 0) along the ray,
/// or None when the ray is parallel to / points away from the segment, or inputs are non-finite.
/// Sqrt-free (parametric) — the shared intersection core. Total: never throws, never returns NaN.
val raySegment : origin: Point -> dir: Point -> seg: Segment -> (Point * float) option

/// Point-to-point line-of-sight convenience: is `target` visible from `source` with no segment
/// strictly between them? Built on `raySegment`. Total on empty/degenerate input.
val isVisible : source: Point -> target: Point -> segments: Segment list -> bool

/// The full visibility polygon via angular sweep: cull occluders within `settings.Radius`
/// (SpatialGrid.queryRadius), order endpoints by a cross-product comparator (no atan2), sweep the
/// nearest crossing segment per wedge, and emit the bounded, closed polygon. Pure; deterministic.
/// This is the function a consumer typically calls from `update`/`view`.
val polygon : settings: Settings -> source: Point -> segments: Segment list -> VisibilityPolygon
```

## Guaranteed invariants (shipped default)

- **V-1 Reuse, no look-alikes** — operates on the shared `Point`/`Rect`; introduces no point/vector/bounds
  record. `Segment` is a pair of shared `Point`s, not a competing vector type. (FR-002, FR-009)
- **V-2 Cull = existing primitive** — broad-phase occluder culling is `SpatialGrid.queryRadius`; no
  hand-rolled bucketing. The intersection + sweep are the only added math (deliberately not in a package —
  `Geometry` is AABB-only). (FR-002, FR-005)
- **V-3 Reports a region, not a boolean** — `polygon` yields an ordered, closed `VisibilityPolygon`;
  `isVisible` is offered only as a point convenience built on the same core. (FR-006)
- **V-4 Deterministic** — endpoint order is a cross-product comparator (no `atan2`) with a squared-distance
  then integer-index tiebreak; nearest-hit uses sqrt-free `t`; no hash iteration; identical inputs ⇒
  byte-identical `Vertices`. (FR-008)
- **V-5 Bounded** — every ray terminates on the `source ± Radius` bound box (reusing the cull radius), so
  an unhit ray never loops and the polygon is always finite and closed. (FR-011)
- **V-6 Total** — degenerate inputs (empty/zero-length segments, source on a wall/endpoint,
  collinear/near-parallel grazing ray, non-finite coords, non-positive radius) return the documented values
  in [data-model.md](../data-model.md); never throws, never emits NaN. (FR-010)
- **V-7 Edit/delete safe** — the editable policy (radius, FOV cone, bound shape, polygon-vs-mask output) is
  isolated to `Settings` + the marked sweep body; the file compiles via an `Exists`-guarded gated
  `Compile` item, so deleting it still builds. (FR-007)

## Verification

Exercised by `tests/Canvas.Tests/VisibilityHelperTests.fs` (adds the raw default body via `<Compile
Include>` — literal `namespace Product`; Canvas.Tests already refs Canvas + Scene): a target behind a wall
is not visible and is visible with the wall removed; the polygon is a closed ring bounded by the radius;
repeat-run byte-identity (including equal-angle endpoints); degenerate totals. And end-to-end by
[../quickstart.md](../quickstart.md). (The framework repo has no `tests/Product.Tests/`; that project
belongs to the *generated* product and exercises the helper post-scaffold.)
