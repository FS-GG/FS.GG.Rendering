# Phase 1 Data Model — Grid Simulation Primitives

Two pure value modules in `FS.GG.UI.Canvas`. No persistence, no mutable state. All functions are total (documented values for degenerate inputs, never throw).

## Types

### `Cell` (new, transparent struct)

```
[<Struct>] type Cell = { Col: int; Row: int }
```

- Integer grid coordinate; the atom of walkability, neighbours, and paths.
- **Distinct from** `FS.GG.UI.Scene.Point` (float pixel position) — a discrete tile index (FR-011: no look-alike). Consumers map `Cell ↔ Point` themselves (e.g. `Point = { X = float Col * tileW; Y = float Row * tileH }`).
- Structural equality gives frontier/visited identity; `(Col, Row)` gives the total tie-break order (D2).

### `Neighbourhood` (new, transparent DU)

```
type Neighbourhood = FourWay | EightWay
```

- `FourWay`: N/E/S/W; move cost 1 (bfs) / 10 (astar).
- `EightWay`: + diagonals; orthogonal 10, diagonal 14 (integer, D3). Diagonal permitted only when both shared orthogonals are walkable (no corner-cut, D5).

### `SpatialGrid<'T>` (new, opaque)

```
[<Sealed>] type SpatialGrid<'T>      // representation hidden in .fsi (D9)
```

- Internal (`.fs` only): a `Map<struct(int*int), int list>` from cell key to indices into an ordered `('T)[]`/`(Point*'T)[]` of items in insertion order, plus the `cellSize`. The index indirection preserves insertion order in results (D7) and keeps buckets small.
- Immutable; built once by `build`, read by `query`/`queryRadius`.

## Function conventions (totality table)

| Function | Signature (curried) | Normal result | Degenerate handling |
|---|---|---|---|
| `Pathfinding.astar` | `Neighbourhood → int → (Cell→bool) → Cell → Cell → Cell list option` | `Some [start; …; goal]`, cost-optimal | `start`/`goal` non-walkable → `None`; `start=goal` (walkable) → `Some [start]`; unreachable or `>maxVisited` expansions → `None`; `maxVisited≤0` → `None` |
| `Pathfinding.bfs` | same shape | `Some` min-hop path | same as `astar` |
| `SpatialGrid.build` | `float → seq<Point*'T> → SpatialGrid<'T>` | bucketed grid, insertion order retained | empty items → empty grid; `cellSize≤0`/non-finite → single bucket (D8) |
| `SpatialGrid.query` | `Rect → SpatialGrid<'T> → 'T list` | exact items in rect (inclusive edges), insertion order | zero-area rect → items on that rect; empty grid → `[]` |
| `SpatialGrid.queryRadius` | `Point → float → SpatialGrid<'T> → 'T list` | exact items within `radius` (dist²≤r²), insertion order | `radius≤0`/non-finite → items at `center` only / `[]` |

## Invariants

- **INV-1 (determinism)**: For fixed inputs, `astar`/`bfs` return a byte-identical `Cell list option`, and `query`/`queryRadius` return a byte-identical `'T list`, on every run and platform (no `Dictionary`/`HashSet` iteration-order or float-tie dependence). *Tested by repeat-run byte-identity property tests.*
- **INV-2 (optimality)**: `astar` returns a cost-minimal path under the D3 cost model (admissible heuristic); `bfs` returns a hop-minimal path. *Tested against hand-computed small grids.*
- **INV-3 (no false negative / no false positive)**: `query`/`queryRadius` return exactly the items in the region — every in-region item present, no out-of-region item present. *Tested against brute-force filter over the same items.*
- **INV-4 (purity/additivity)**: no existing public type/signature/behavior changes; both modules are new additive surface with no per-game logic; predicate and item positions are caller-supplied. *Tested by the surface-area baseline diff + additivity assertion.*
- **INV-5 (totality)**: every degenerate row above returns its documented value, never throws. *Tested explicitly per row.*
