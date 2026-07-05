# Phase 1 Data Model: Collision Helper Source

The collision helper introduces **no framework package type**. It works on the shared
`FS.GG.UI.Scene.Rect`/`Point` and exposes a few small **product-owned** value shapes inside
`Collision.fs` that the consumer sees and may edit. Fields and conventions below define the intended
shape (see [contracts/collision-helper-source.md](./contracts/collision-helper-source.md) for the exact
source surface).

## Reused framework types (not redefined)

- **`Rect`** (`FS.GG.UI.Scene`) — a body's axis-aligned bounds. `{ X; Y; Width; Height }` (float).
- **`Point`** (`FS.GG.UI.Scene`) — used both as a position and, in the response layer, as a **vector**
  (separation / minimum-translation). `{ X; Y }` (float).
- **`Geometry`** (`FS.GG.UI.Scene`) — narrow-phase: `intersects`, `containsPoint`, `sweptIntersects`,
  `center`, `ofCenter`.
- **`SpatialGrid<'T>`** (`FS.GG.UI.Canvas`) — broad-phase bucketing: `build`, `query`, `queryRadius`.

## Product-owned value shapes (in `Collision.fs`)

### Body<'T>
A collidable thing: its bounds plus a caller-supplied identity/tag.

| Field | Type | Notes |
|-------|------|-------|
| `Bounds` | `Rect` | AABB in world space (reuses the shared `Rect`; no new bounds type). |
| `Tag` | `'T` | Caller's identity/layer payload (id, kind, layer mask). Generic like `SpatialGrid<'T>`. |

Rationale: carrying identity as `'T` (not a new record) avoids the consumer-vs-consumer `.Pos`/`.Id`
footgun and lets the same value flow into `SpatialGrid.build`.

### Contact<'T>
A detected overlap between two bodies and how to separate them. Pure detection result — no state
mutated.

| Field | Type | Notes |
|-------|------|-------|
| `A` | `Body<'T>` | First body, always the **lower-index** body of the pair (stable order). |
| `B` | `Body<'T>` | Second body, the higher-index body. |
| `Penetration` | `Point` | Minimum-translation vector: the smallest displacement that removes the overlap. Direction points to push `A` off `B` (B off A is the negation). |
| `Depth` | `float` | Overlap depth along the MTV axis (`≥ 0`); `0` only for exact edge/corner touch. |

### Resolution<'T>
The post-response state for a contact under the chosen response rule.

| Field | Type | Notes |
|-------|------|-------|
| `A` | `Body<'T>` | `A` after separation (bounds moved by its share of the MTV). |
| `B` | `Body<'T>` | `B` after separation. |
| `Applied` | `Point` | The separation actually applied to `A` (for the consumer's velocity/response bookkeeping). |
| `Restitution` | `float` | Normalized bounce factor (0.0..1.0) for the consumer's velocity step; 0.0 unless a `Bounce` rule set it. |

### ResponseRule (the editable policy)
The one place the consumer changes behavior. A small DU documenting the built-in options:

- `SeparateEqually` — split the MTV 50/50 between `A` and `B` (both movable).
- `PushFirst` / `PushSecond` — one body is immovable (wall); the other takes the full MTV.
- `Slide` — 50/50 separation, no recorded restitution.
- `Bounce restitutionPercent` — 50/50 separation plus a recorded restitution (integer percent, clamped
  to 0..100, normalized to 0.0..1.0) for the consumer's velocity step. Integer percent avoids float-tie
  surprises. (Velocity integration itself is out of scope — the helper only separates positionally.)

The default emitted `Collision.fs` picks one rule (e.g. `SeparateEqually`) and clearly marks the rule
function as **the line to edit**.

## Total-function conventions (FR-010)

| Degenerate input | Documented result |
|------------------|-------------------|
| Empty body set | Empty contact list; empty resolution list. |
| Single body | No pairs; empty results. |
| Exactly touching edges/corners | **Not** a contact (matches `Geometry.intersects` strict-edge rule): `Depth = 0` cases are excluded. |
| Fully contained body | A contact with MTV along the least-overlap axis; separation pushes it to the nearest edge. |
| Zero-area body (`Width` or `Height` = 0) | Never a positive-area overlap ⇒ no contact (consistent with `intersects`). |
| Non-finite bounds (NaN/∞) | No contact (NaN comparisons are false), never throws — matches `Geometry` NaN-safety. |

## Determinism invariants (FR-008)

- Pairs are formed by ascending body **index** (`i < j`) in supplied order; contact and resolution lists
  are returned in that order.
- No `Dictionary`/`HashSet` iteration feeds the result order; `SpatialGrid` broad-phase results are
  already insertion-ordered.
- Ordering never depends on a float comparison (depth/distance), only on the integer index — so equal
  inputs yield a byte-identical result across runs and platforms.

## Relationships / flow

```text
bodies : Body<'T> list
   │  build broad-phase
   ▼
SpatialGrid.build cellSize [ for b in bodies -> Geometry.center b.Bounds, b ]
   │  query candidate pairs (index-ordered)
   ▼
narrow-phase: Geometry.intersects a.Bounds b.Bounds  →  Contact<'T> (MTV + depth)
   │  apply ResponseRule
   ▼
Resolution<'T> list   (pure; consumer folds these back into its Model)
```
