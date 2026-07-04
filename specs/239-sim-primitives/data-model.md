# Phase 1 Data Model: FS.GG.UI Simulation Primitives

Pure value types and their invariants. No persistence, no state machines — the only "state transitions" are the functional threading of the `Rng` value and the `FixedStep` accumulator, both of which are plain value-in / value-out.

## Existing types reused (no change)

| Type | Location | Shape |
|---|---|---|
| `Rect` | `src/Scene/Types.fs` | `{ X: float; Y: float; Width: float; Height: float }` |
| `Point` | `src/Scene/Types.fs` | `{ X: float; Y: float }` |
| `Size` | `src/Scene/Types.fs` | `{ Width: int; Height: int }` |

`Geometry.ofCenter` needs a float extent (a game entity's half-size is fractional), and `Size` is `int`. To avoid introducing a look-alike type, `ofCenter` takes explicit `width: float` and `height: float` (or a `Point` used as a `(w,h)` extent is rejected as confusing). Decision: `ofCenter: center: Point -> width: float -> height: float -> Rect`. No new type introduced.

## New value type: `Rng` (namespace `FS.GG.UI.Canvas`)

```
[<Struct>] type Rng = { State: uint64 }
```

- **Invariant**: identity is the `State` value; two `Rng` with equal `State` are equal and produce identical continuations (record structural equality ⇒ equal RNG state — SC-002).
- **Immutable / value type**: `[<Struct>]` so it lives inline in a consumer's `Model` with no shared reference; `{ model with Rng = r' }` copies the value.
- **Threading**: every draw is `Rng -> struct('value * Rng)` — returns the drawn value and the *next* state; the input `Rng` is unchanged and still reproduces its own next draw.
- **Seeding**: `ofSeed: uint64 -> Rng` runs one SplitMix64 mixing step off the raw seed so a "weak" seed (e.g. `0UL`) still yields a non-degenerate first draw (edge case in spec).

Draw ranges (documented conventions):
- `nextFloat: Rng -> struct(float * Rng)` → value in `[0.0, 1.0)` (0 inclusive, 1 exclusive).
- `nextInt: lo: int -> hi: int -> Rng -> struct(int * Rng)` → value in `[lo, hi]` **inclusive** on both ends (game-natural: "roll 1..6"). Degenerate: `lo = hi` ⇒ `lo`; `lo > hi` ⇒ `lo` (documented, non-throwing).
- `split: Rng -> struct(Rng * Rng)` → two independent generators derived from the current state (for sub-streams).

## New value: `FixedStep` drain result (namespace `FS.GG.UI.Canvas`)

No new named type — the result is `struct(int * float)`:
- `stepCount: int` — the whole number of fixed steps to run this frame; always `>= 0`.
- `newAccumulator: float` — carried leftover seconds, always in `[0.0, interval)` for a positive interval.

Invariants (the property tests assert these):
- **Non-negativity**: `stepCount >= 0` for every input.
- **Conservation**: `newAccumulator = (accumulator + clampedFrameTime) - float stepCount * interval`, and `0 <= newAccumulator < interval` when `interval > 0`.
- **Clamp bound**: `stepCount <= floor((accumulator + maxFrameTime) / interval)` — a single stalled frame cannot produce unbounded catch-up (FR-010). Default `maxFrameTime = 0.25`.
- **Degenerate**: `interval <= 0` ⇒ `struct(0, accumulator)`; `frameTime <= 0` ⇒ `struct(0, accumulator)`.
- **Purity/determinism**: output depends only on arguments (no wall-clock) — a scripted `frameTime` sequence reproduces identical `(stepCount, newAccumulator)` pairs every run.

## `Geometry` — no new types

Operates entirely on `Rect`/`Point`. Function results are `bool`, `Point`, or `Rect`. Conventions (from research D2):
- `intersects a b`: strict `<`/`>` (edge/corner touch ⇒ `false`).
- `contains outer inner` and `containsPoint rect p`: inclusive `>=`/`<=`.
- `center r -> Point` and `ofCenter c w h -> Rect` round-trip: `center (ofCenter c w h) = c`.
- `sweptIntersects moving velocity target`: overlap of the moving rect's swept path (start → start+velocity) with `target`; a **superset** of the static test at both endpoints (if either endpoint intersects, the swept test is `true`), so fast projectiles that tunnel through a thin target are still detected (US1 scenario 5).

## Cross-entity relationships

None. All three helpers are independent; a consumer composes them (e.g. `Rng` drives spawn positions, `Geometry` tests the resulting collisions, `FixedStep` paces the whole `update`). No shared state, no ordering constraints between the modules.
