namespace AppRoot

open FS.GG.Game.Core // ADR-0022 P5: Cell (and Pathfinding's walkability shape) now live in the FS.GG.Game.Core bottom layer (moved from FS.GG.UI.Canvas)

/// Product-owned grid line-drawing helper — THIS FILE IS YOURS TO ADAPT.
///
/// The grid vocabulary reuses the framework primitive (no hand-rolled `(row, col)` type):
///   * every endpoint and every emitted tile is the shared `FS.GG.UI.Canvas.Cell` (an integer grid
///     coordinate — a discrete tile index, deliberately NOT the float `FS.GG.UI.Scene.Point`);
///   * `lineOfSight` consults a `Cell -> bool` transparency map — the SAME shape as
///     `FS.GG.UI.Canvas.Pathfinding`'s `isWalkable` predicate, so your map plugs into both.
/// The cell walk itself is the game-opinionated part the framework deliberately does not freeze into a
/// package — switch the thin line for the supercover, cap the length for a limited-range beam, make LOS
/// stop-and-report the first blocker, or delete this whole file (the build stays green: its compile item
/// is `Exists`-guarded).
///
/// Everything here is pure, total, and deterministic: the walk is **integer Bresenham** — no
/// floating-point interpolation, no `Math.Round`, no transcendental — so identical endpoints yield a
/// byte-identical cell list across runs and platforms (safe to call from a replayed `update`/`view`).
/// A line between two cells is inherently finite (bounded by the endpoint separation), so the walk
/// always terminates.
///
/// Algorithm reference: https://www.redblobgames.com/grids/line-drawing/
module LineDrawing =

    /// The ordered tiles the straight line from `a` to `b` passes through — a **thin**,
    /// diagonal-connected line (integer Bresenham). Both endpoints are included; `a = b` returns `[a]`.
    /// Each consecutive pair differs by at most 1 in each axis (a diagonal step advances both). Use this
    /// for drawing a road/beam; for sight through walls prefer `supercover` (a thin line can slip through
    /// a diagonal wall join). Pure, total, deterministic; integer-only.
    let line (a: Cell) (b: Cell) : Cell list =
        // Deltas and the error term are `int64` so the arithmetic stays total across the WHOLE `int`
        // coordinate domain: an `int` subtraction could wrap, `abs Int32.MinValue` throws, and `2 * err`
        // overflows to negative once the dominant delta reaches ~2^30 (which would stall the axis and
        // loop forever). Promoting to `int64` removes all three — still pure integer arithmetic, still
        // byte-identical, and the walk always terminates. (x/y stay `int`: they only ever step by ±1.)
        let dx = abs (int64 b.Col - int64 a.Col)
        let dy = abs (int64 b.Row - int64 a.Row)
        let sx = if a.Col < b.Col then 1 else -1
        let sy = if a.Row < b.Row then 1 else -1
        let acc = ResizeArray<Cell>()
        let mutable x = a.Col
        let mutable y = a.Row
        // err is the Bresenham decision variable (dx - dy). All arithmetic is integer, so there is no
        // rounding-mode drift — the same endpoints always produce the same steps.
        let mutable err = dx - dy
        let mutable go = true
        while go do
            acc.Add { Col = x; Row = y }
            if x = b.Col && y = b.Row then
                go <- false
            else
                let e2 = 2L * err
                if e2 > -dy then
                    err <- err - dy
                    x <- x + sx
                if e2 < dx then
                    err <- err + dx
                    y <- y + sy
        List.ofSeq acc

    /// The ordered tiles the segment from `a` to `b` *touches* — the **supercover** walk, strictly
    /// 4-connected (each consecutive pair differs by exactly 1 in exactly one axis), so there is **no
    /// diagonal gap** a sight-line could slip through at a wall corner. Both endpoints included;
    /// `a = b` returns `[a]`. This is the variant `lineOfSight` walks. Pure, total, deterministic;
    /// integer-only (exact diagonal crossings resolve by a fixed step-x-first tiebreak).
    let supercover (a: Cell) (b: Cell) : Cell list =
        // Deltas and the tiebreak perp-dot (the 2-D cross) are `int64` for the same full-`int`-domain
        // totality as `line` (wrap-free subtraction, `abs` that can't throw, and `(1 + 2*ix)*ny` that
        // can't overflow for any sane tile range). Still pure integer arithmetic; x/y only step by ±1.
        let dx = int64 b.Col - int64 a.Col
        let dy = int64 b.Row - int64 a.Row
        let nx = abs dx
        let ny = abs dy
        let sx = if dx > 0L then 1 else -1
        let sy = if dy > 0L then 1 else -1
        let acc = ResizeArray<Cell>()
        let mutable x = a.Col
        let mutable y = a.Row
        acc.Add { Col = x; Row = y }
        // ix / iy count the orthogonal steps already taken toward b. At each step choose the axis whose
        // next half-cell boundary the line crosses first: compare (0.5 + ix)/nx with (0.5 + iy)/ny,
        // cross-multiplied to stay in integers ⇒ (1 + 2*ix)*ny vs (1 + 2*iy)*nx. An exact tie (the line
        // passes through a lattice corner) steps in x first — a deterministic choice that keeps the walk
        // 4-connected rather than cutting the corner diagonally.
        let mutable ix = 0L
        let mutable iy = 0L
        while ix < nx || iy < ny do
            let cmp = (1L + 2L * ix) * ny - (1L + 2L * iy) * nx
            if iy >= ny || (ix < nx && cmp <= 0L) then
                x <- x + sx
                ix <- ix + 1L
            else
                y <- y + sy
                iy <- iy + 1L
            acc.Add { Col = x; Row = y }
        List.ofSeq acc

    /// Grid line-of-sight: `true` when no tile strictly between `a` and `b` fails `isTransparent`
    /// (a `Cell -> bool` map — the same shape as `Pathfinding`'s walkability predicate; the framework
    /// holds no map, your predicate IS the map). Walks the `supercover` tiles so sight cannot leak
    /// through a diagonal wall join. The endpoints themselves are never tested (you can look FROM and AT
    /// an opaque tile). `a = b` returns `true`. Total on an always-false / always-true predicate.
    let lineOfSight (isTransparent: Cell -> bool) (a: Cell) (b: Cell) : bool =
        supercover a b
        |> List.forall (fun c -> c = a || c = b || isTransparent c)
