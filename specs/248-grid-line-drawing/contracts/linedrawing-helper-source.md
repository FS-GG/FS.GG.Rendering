# Contract: `LineDrawing.fs` helper source surface

**Feature**: `248-grid-line-drawing` | **Date**: 2026-07-05

This is the intended source surface the consumer *receives* in `src/<ProductDir>/LineDrawing.fs` — not a
frozen package `.fsi`. The consumer owns and edits the whole file. Drafted first (Constitution I analogue),
exercised by the quickstart/FSI transcript, covered by `tests/Canvas.Tests/LineDrawingHelperTests.fs`
(fails before the file exists, passes after), then implemented.

```fsharp
namespace Product

open FS.GG.UI.Canvas   // Cell

/// Product-owned grid line-drawing helper — THIS FILE IS YOURS TO ADAPT.
module LineDrawing =

    /// Ordered cells the straight line from `a` to `b` passes through — thin, diagonal-connected
    /// (integer Bresenham). Both endpoints included; `a = b` → `[a]`. Pure, total, deterministic
    /// (integer-only, no float rounding). Bounded by the endpoint separation.
    val line: a: Cell -> b: Cell -> Cell list

    /// Ordered cells the segment from `a` to `b` *touches* — the "supercover" walk with NO diagonal gap
    /// (both cells at a corner crossing are included). Both endpoints included; `a = b` → `[a]`. Use this
    /// (not `line`) for sight through walls so a diagonal wall join cannot leak. Pure/total/deterministic.
    val supercover: a: Cell -> b: Cell -> Cell list

    /// Grid line-of-sight: `true` when no cell strictly between `a` and `b` fails `isTransparent`
    /// (a `Cell -> bool` map, same shape as `Pathfinding`'s walkability predicate). Built on the
    /// supercover walk. `a = b` → `true`. Total on an always-false / always-true predicate.
    val lineOfSight: isTransparent: (Cell -> bool) -> a: Cell -> b: Cell -> bool
```

## Guarantees the tests pin

- **Endpoints**: `List.head (line a b) = a` and `List.last (line a b) = b` (same for `supercover`).
- **Connectivity (`line`)**: each consecutive pair differs by at most 1 in each of `Col`/`Row`.
- **No diagonal gap (`supercover`)**: each consecutive pair differs by exactly 1 in exactly one axis
  (4-connected), so sight cannot slip through a corner.
- **Determinism**: `line a b = line a b` byte-identically across repeated calls; covered for all 8 octants
  and the axis/diagonal degenerate cases.
- **LOS**: with a blocking cell placed on the segment, `lineOfSight` is `false`; with it removed, `true`.
- **Totality**: `a = b`, axis-aligned, diagonal, and negative-delta lines never throw.

## Editable knobs (the "yours to adapt" surface)

- Swap `line` (thin) for `supercover` (thick) at call sites, or vice versa.
- Cap the line length (truncate the returned list) for a limited-range beam.
- Change `lineOfSight` to stop-and-report the first blocked cell, or to permit diagonal squeezes.
- Delete the file entirely — its compile item is `Exists`-guarded, so the build stays green.
