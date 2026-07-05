# Contract: Collision Helper Source (`Collision.fs`)

This is **product-owned adaptable source**, not a frozen package `.fsi`. The "contract" is the shape the
consumer receives on scaffold and the invariants the shipped default guarantees — all of which the
consumer may then edit. Types/fields are defined in [../data-model.md](../data-model.md).

## Namespace / module

Materialized into `src/<ProductDir>/Collision.fs` with `sourceName` substitution (`Product` by default):

```fsharp
namespace <ProductName>          // e.g. namespace Product

open FS.GG.UI.Scene              // Rect, Point, Geometry
open FS.GG.UI.Canvas             // SpatialGrid

/// Product-owned collision helper — YOURS to adapt. Detection reuses Geometry/SpatialGrid;
/// the response rule below is the line to edit. Delete this file freely if you don't need it.
module Collision =
    ...
```

## Intended surface (what the consumer gets)

> Signatures are the *default* the consumer receives; they are editable, not surface-baselined.

```fsharp
type Body<'T>       = { Bounds: Rect; Tag: 'T }
type Contact<'T>    = { A: Body<'T>; B: Body<'T>; Penetration: Point; Depth: float }
type Resolution<'T> = { A: Body<'T>; B: Body<'T>; Applied: Point; Restitution: float }

type ResponseRule =
    | SeparateEqually
    | PushFirst
    | PushSecond
    | Slide                                 // 50/50 separation, restitution 0.0
    | Bounce of restitutionPercent: int     // 50/50 separation + recorded restitution (0..1) for the consumer's velocity step

/// Narrow-phase: the minimum-translation contact between two bodies, or None when they do not
/// overlap on positive area (edge/corner touch is NOT a contact — strict edges, matching Geometry).
val contact : a: Body<'T> -> b: Body<'T> -> Contact<'T> option

/// Broad-phase + narrow-phase: every colliding pair among `bodies`, index-ordered and deterministic.
/// Uses SpatialGrid at `cellSize` to avoid the O(n²) scan; total on empty/singleton input.
val collide : cellSize: float -> bodies: Body<'T> list -> Contact<'T> list

/// Apply the response rule to a contact, returning the separated bodies. Pure; deterministic.
val resolve : rule: ResponseRule -> contact: Contact<'T> -> Resolution<'T>

/// One per-frame pass: detect and resolve, returning resolutions in deterministic pair order.
/// This is the function a consumer typically calls from `update`.
val step : rule: ResponseRule -> cellSize: float -> bodies: Body<'T> list -> Resolution<'T> list
```

## Guaranteed invariants (shipped default)

- **C-1 Reuse, no look-alikes** — operates on the shared `Rect`/`Point`; introduces no bounds/vector
  record. (FR-002, FR-009)
- **C-2 Detection = existing primitives** — narrow-phase is `Geometry`; broad-phase is `SpatialGrid`.
  No hand-rolled AABB, no hand-rolled bucketing. (FR-002)
- **C-3 Reports overlap AND resolution** — `collide` yields contacts (with MTV + depth), `resolve`/`step`
  yield separated bodies — never a bare boolean. (FR-006)
- **C-4 Deterministic** — pair/contact/resolution order is by ascending body index in supplied order;
  no hash-iteration or float-tie in the ordering; identical inputs ⇒ byte-identical output. (FR-008)
- **C-5 Total** — degenerate inputs (empty/singleton set, exact touch, containment, zero-area,
  non-finite) return the documented values in [data-model.md](../data-model.md); never throws. (FR-010)
- **C-6 Edit/delete safe** — the response rule is isolated to `resolve` (the marked edit point); the file
  compiles via an `Exists`-guarded gated `Compile` item, so deleting it still builds. (FR-007)

## Verification

Exercised by `tests/Canvas.Tests/CollisionHelperTests.fs` (adds the raw default body via `<Compile Include>`
— literal `namespace Product`; Canvas.Tests already refs Canvas+Scene): overlap→separation removes overlap;
repeat-run byte-identity; degenerate totals. And end-to-end by [../quickstart.md](../quickstart.md). (The
framework repo has no `tests/Product.Tests/`; that project belongs to the *generated* product and exercises
the helper post-scaffold.)
