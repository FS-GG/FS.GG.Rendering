// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Los.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// Which grid line a trace walks — the "thin vs thick" policy that decides what happens at a diagonal
/// wall join (`2026-07-05-game-logic-line-of-sight-design.md` §2). Neither is universally correct: the
/// two disagree exactly at corners, and the game's own movement rules pick the winner. If units may
/// move between two diagonally-touching walls, sight should leak there too (`Thin`); otherwise it must
/// not (`Supercover`).
type LineMode =
    /// Integer Bresenham: ~`max(|dx|, |dy|)` cells, diagonal-connected (a step may advance both axes).
    /// **Cuts corners** — the line passes *between* two diagonally-touching cells without entering
    /// either, so a sight-line can leak through a diagonal wall join. Use for a drawn road or beam, or
    /// for deliberately generous sight. Not commutative on its own (see `lineOfSightBy`).
    | Thin
    /// The supercover walk: ~`|dx| + |dy|` cells, strictly 4-connected (each consecutive pair differs
    /// by exactly 1 in exactly one axis). Emits **every** cell the real segment's area touches, so
    /// nothing leaks through a diagonal wall join. The default for sight and for shell travel.
    | Supercover

/// Public contract module exposed by the FS.GG.Game.Core package.
/// Point-to-point grid line-of-sight — the "can A see/hit B?" and "which tiles does the line from A to
/// B cross?" problem, over a caller-supplied opacity predicate. The framework holds no map: the
/// predicate `isTransparent` (a pure `Cell -> bool`) IS the map, exactly as `isWalkable` is for
/// `Pathfinding`, so one map plugs into both.
///
/// This is **LOS, not FOV**: it answers a question about two known cells in `O(distance)`. Do not build
/// a field of view by running `lineOfSight` to every cell in a radius — that is the slow path *and* the
/// buggy path (asymmetric, artifact-ridden vision), the historical roguelike mistake shadowcasting
/// exists to fix.
///
/// Every function is pure, total, and deterministic: the walk is **integer** — no floating-point
/// interpolation, no `atan2`, no `Math.Round`, no transcendental — so identical endpoints yield a
/// byte-identical cell list across runs, platforms, and architectures (safe to call from a
/// deterministic-replay `update`). Deltas are computed in `int64`, so the arithmetic is total across
/// the whole `int` coordinate domain: an `int` subtraction would wrap, `abs Int32.MinValue` would
/// throw, and the doubled Bresenham error term would overflow to negative once the dominant delta
/// reached ~2^30 — stalling an axis and looping forever.
///
/// Promoted from the `line-drawing` product fragment of the (frozen) FS.GG.Rendering game profile,
/// where every game that wanted sight copied it and diverged. Algorithm reference:
/// https://www.redblobgames.com/grids/line-drawing/
[<RequireQualifiedAccess>]
module Los =

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The ordered tiles the straight line from `a` to `b` passes through — a **thin**,
    /// diagonal-connected line (integer Bresenham). Both endpoints are included, `a` first; `a = b`
    /// returns `[a]`. Each consecutive pair differs by at most 1 in each axis (a diagonal step advances
    /// both), so the walk can cut a corner. For sight through walls prefer `supercover`.
    ///
    /// Direction-preserving, and therefore **not commutative**: `line a b` is not in general
    /// `List.rev (line b a)`, because the error-tie break is resolved in a fixed direction. Use
    /// `lineOfSightBy` when you need the symmetry invariant.
    val line: a: Cell -> b: Cell -> Cell list

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The ordered tiles the segment from `a` to `b` *touches* — the **supercover** walk, strictly
    /// 4-connected (each consecutive pair differs by exactly 1 in exactly one axis), so there is **no
    /// diagonal gap** a sight-line could slip through at a wall corner. Both endpoints included, `a`
    /// first; `a = b` returns `[a]`. An exact diagonal crossing (the line through a lattice corner)
    /// resolves by a fixed step-x-first tie-break, which keeps the walk 4-connected rather than cutting
    /// the corner.
    ///
    /// The comparison that picks the next axis is the cross-multiplied `(1 + 2·ix)·ny ? (1 + 2·iy)·nx`
    /// — integer, division-free, and exact for any separation a caller can actually walk: the emitted
    /// list holds one cell per orthogonal step, so memory is exhausted long before the `int64` product
    /// could saturate.
    val supercover: a: Cell -> b: Cell -> Cell list

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// `line` or `supercover`, selected by `mode`. Both endpoints included, `a` first.
    val trace: mode: LineMode -> a: Cell -> b: Cell -> Cell list

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Grid line-of-sight under `Supercover` — the default for sight and for shell travel, because a
    /// `Thin` line leaks diagonally between two touching wall tiles (the design doc's "#1 LOS bug").
    /// Equivalent to `lineOfSightBy Supercover`.
    ///
    /// `true` when no tile strictly between `a` and `b` fails `isTransparent`. The endpoints themselves
    /// are **never tested** — you may look FROM and AT an opaque tile — and `a = b` is `true`. Total on
    /// an always-false or always-true predicate.
    val lineOfSight: isTransparent: (Cell -> bool) -> a: Cell -> b: Cell -> bool

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Grid line-of-sight under an explicit `LineMode`, with the same endpoint rule as `lineOfSight`
    /// (endpoints never tested, `a = b` is `true`).
    ///
    /// **Symmetric by construction, in every mode:** `lineOfSightBy m p a b = lineOfSightBy m p b a`.
    /// The tiles are traced over the *canonical* ordered pair (`min(a, b)` → `max(a, b)` under `Cell`'s
    /// structural `(Col, Row)` order), so both argument orders test one identical cell sequence. This
    /// is the invariant that makes combat fair — without it a unit can shoot one that cannot shoot back
    /// — and `Thin` does not have it for free: its fixed error-tie break visits different intermediate
    /// cells depending on which endpoint the walk starts from, so a wall in a sometimes-visited cell
    /// would block one direction only.
    val lineOfSightBy: mode: LineMode -> isTransparent: (Cell -> bool) -> a: Cell -> b: Cell -> bool
