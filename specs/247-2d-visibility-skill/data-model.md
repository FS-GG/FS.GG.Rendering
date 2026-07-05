# Phase 1 Data Model: 2D Visibility Helper Source

The visibility helper introduces **no framework package type**. It works on the shared
`FS.GG.UI.Scene.Point`/`Rect` and exposes a few small **product-owned** value shapes inside
`Visibility.fs` that the consumer sees and may edit. Fields and conventions below define the intended
shape (see [contracts/visibility-helper-source.md](./contracts/visibility-helper-source.md) for the exact
source surface).

## Reused framework types (not redefined)

- **`Point`** (`FS.GG.UI.Scene`) — used as a position (source, hit vertices) and as a **vector** (ray
  direction, cross-product operands). `{ X; Y }` (float).
- **`Rect`** (`FS.GG.UI.Scene`) — the axis-aligned **bound** box (`source ± radius`) and the
  `SpatialGrid.query` cull region. `{ X; Y; Width; Height }` (float).
- **`Geometry`** (`FS.GG.UI.Scene`) — `center`, `containsPoint`, `ofCenter` (bound-box helpers). *(No
  ray/segment/angle functions exist — the helper adds those; that absence is the whole point of R1.)*
- **`SpatialGrid<'T>`** (`FS.GG.UI.Canvas`) — broad-phase cull of nearby occluders: `build`, `query`,
  `queryRadius`.

## Product-owned value shapes (in `Visibility.fs`)

### Segment
A wall / occluder: a line segment between two shared `Point`s. The one domain concept the shared
vocabulary genuinely lacks (`Point` is a location, `Rect` an AABB); **not** a look-alike vector type.

| Field | Type | Notes |
|-------|------|-------|
| `A` | `Point` | One endpoint (reuses the shared `Point`; no new vector record). |
| `B` | `Point` | Other endpoint. A zero-length segment (`A = B`) is total: it occludes nothing. |

### Settings (the editable policy)
The knobs the consumer changes. The one place sight range and grid granularity are tuned.

| Field | Type | Notes |
|-------|------|-------|
| `Radius` | `float` | Sight radius. Doubles as the ray **bound** (`source ± Radius` box, FR-011) **and** the `SpatialGrid.queryRadius` broad-phase cull radius, so the two can never disagree. Non-positive/non-finite → a documented minimal bound (never throws). |
| `CellSize` | `float` | `SpatialGrid` cell size for the occluder cull. Non-positive/non-finite falls back to a single bucket (per `SpatialGrid.build`), still exact. |

### VisibilityPolygon
What the sweep produces: the region visible from the source, as a closed ring of hit points.

| Field | Type | Notes |
|-------|------|-------|
| `Source` | `Point` | The viewpoint the polygon was computed from. |
| `Vertices` | `Point list` | Ordered boundary vertices (counter-clockwise), forming a **closed** ring bounded by `Radius`. Each vertex is a nearest-hit point on a wall or on the bound box. Empty segment set ⇒ the four bound-box corners (full visibility to the bound). |

Rationale: emitting an ordered `Point list` (not a bare boolean, FR-006) lets the consumer fill it as a
2D light, rasterize it into a fog-of-war mask, or point-test it — all without re-running the sweep.

## Intended functions (see the contract for signatures)

- `raySegment` — nearest ray-segment hit as a `(Point * float)` (`t ≥ 0`), or `None` when parallel /
  behind / non-finite. Sqrt-free parametric test; the intersection *core* the other functions share.
- `isVisible` — point-to-point line-of-sight convenience (`bool`), built on `raySegment`: is `target`
  reachable from `source` with no segment strictly between them?
- `polygon` — the full **angular sweep**: cull occluders within `Radius` via `SpatialGrid.queryRadius`,
  order their endpoints by the cross-product comparator, sweep the nearest crossing segment per wedge, and
  emit the bounded `VisibilityPolygon`. The function most games call from `update`/`view`.

## Total-function conventions (FR-010)

| Degenerate input | Documented result |
|------------------|-------------------|
| Empty segment set | `VisibilityPolygon` = the four bound-box corners (full visibility to the bound); never throws. |
| Zero-length segment (`A = B`) | Contributes no occlusion (its endpoints add no wedge that blocks); skipped, never a divide-by-zero. |
| Source exactly on a wall | Total: the wall's endpoints still order into the sweep; the polygon degenerates gracefully (no throw, no NaN) rather than producing an infinite ray. |
| Source exactly on an endpoint | Total: squared-distance 0 breaks the angular tie by integer index; the vertex is emitted once. |
| Collinear / near-parallel grazing ray | `raySegment` returns `None` when the parametric denominator is 0 (parallel); no NaN propagates. |
| Coincident endpoints across walls | Ordered deterministically by squared distance then integer index; each contributes at most one sweep vertex. |
| Non-finite coords (NaN/∞) | Filtered by the finiteness guard; no hit, never throws — matches `Geometry`/`SpatialGrid` NaN-safety. |
| Non-positive/non-finite `Radius` | Falls back to a documented minimal bound box; the sweep still totals. |

## Determinism invariants (FR-008)

- Endpoints are ordered by a **cross-product angular comparator** (half-plane + cross-product sign) — no
  `atan2`, so no transcendental last-bit drift.
- Exact angular ties break by **sqrt-free squared distance**, then by the endpoint's **integer index** in
  supplied order — a total order with no float-tie ambiguity.
- Nearest-hit-per-wedge is chosen by the **parametric `t`** (sqrt-free), never a `sqrt`ed length.
- No `Dictionary`/`HashSet` iteration feeds the result order; `SpatialGrid` cull results are already
  insertion-ordered. Identical `(source, segments, Settings)` ⇒ byte-identical `Vertices` across runs and
  platforms.

## Relationships / flow

```text
source : Point,  segments : Segment list,  settings : Settings
   │  broad-phase cull (bound = cull radius = settings.Radius)
   ▼
SpatialGrid.build settings.CellSize [ for s in segments -> midpoint s, s ]
SpatialGrid.queryRadius source settings.Radius  →  candidate occluders (insertion-ordered)
   │  + the four bound-box edges (source ± Radius) as synthetic walls (FR-011)
   ▼
order endpoints by cross-product comparator (integer-index tiebreak)   [no atan2]
   │  sweep: per wedge, nearest crossing segment via raySegment (sqrt-free t)
   ▼
VisibilityPolygon { Source = source; Vertices = <closed CCW ring, bounded by Radius> }
   │  consumer folds it into its Model / fills it as light / rasterizes a fog mask / point-tests it
   ▼
(pure — safe to call from a replayed update/view)
```
