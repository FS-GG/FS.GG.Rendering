# Phase 1 Data Model: Grid Line-Drawing

**Feature**: `248-grid-line-drawing` | **Date**: 2026-07-05

The helper introduces **no new type**: the entire capability is expressed over the existing shared `Cell`.
This is the discrete-grid analogue of visibility reusing `Point`/`Rect` — line-drawing reuses `Cell`.

## Reused primitives (not re-created)

| Concept | Type | Source | Role |
|---------|------|--------|------|
| Grid coordinate | `Cell` = `{ Col: int; Row: int }` (struct) | `FS.GG.UI.Canvas` (feature 245) | The atom the line is expressed over; endpoints and every emitted cell. Distinct from float `Point`. |
| Transparency / walkability map | `Cell -> bool` | `Pathfinding` predicate convention (feature 245) | The caller-supplied map `lineOfSight` consults; framework holds no map. |

## Produced values

| Value | Shape | Meaning |
|-------|-------|---------|
| Cell line | `Cell list` | Ordered cells from `start` to `goal`, **both endpoints included**. `line` is thin (diagonal-connected Bresenham); `supercover` visits every touched cell. |
| Line-of-sight | `bool` | `true` when no cell strictly between `start` and `goal` fails the predicate; built on the supercover walk. |

## Total-function conventions (FR-010)

| Input | Result |
|-------|--------|
| `start = goal` | `[start]` (single cell); `lineOfSight` → `true` |
| Horizontal / vertical / pure-diagonal line | The exact axis/diagonal run of cells |
| Any octant, incl. negative `Col`/`Row` deltas | Correct ordered walk (Bresenham handles all 8 octants) |
| Predicate always `false` | `lineOfSight` → `false` when `start ≠ goal` (any interior cell blocks); the walk functions still return the full cell list (they do not consult the predicate) |
| Predicate always `true` | `lineOfSight` → `true` |

## Determinism (FR-008)

- Integer arithmetic only (Bresenham error accumulator); **no** floating-point interpolation, no
  `Math.Round`, no transcendental — no rounding-mode drift.
- No `Dictionary`/`HashSet`; output order is the deterministic walk order (start → goal).
- Pure function of the two endpoints (and, for `lineOfSight`, the predicate): identical inputs → identical
  output across runs and platforms.

## Bound (FR-011)

The emitted cell count is finite, bounded by the endpoint separation (Chebyshev distance for `line`,
Chebyshev + orthogonal crossings for `supercover`). No unbounded walk is possible.
