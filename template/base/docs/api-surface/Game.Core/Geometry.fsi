namespace FS.GG.Game.Core

/// Public contract module exposed by the FS.GG.Game.Core package.
/// Axis-aligned bounding-box helpers over the sim `Rect`/`Point` — the collision, containment,
/// and centering surface the game simulation uses instead of hand-rolling its own geometry.
/// Extracted from `FS.GG.UI.Scene.Geometry` (ADR-0022 / P0 Option D) and reimplemented BCL-only
/// over `FS.GG.Game.Core.Rect`/`Point`. All functions are pure and total (NaN-safe; degenerate
/// rects never throw). Convention: `intersects` uses strict edges (touching is NOT overlap);
/// `contains`/`containsPoint` are inclusive of shared edges.
[<RequireQualifiedAccess>]
module Geometry =

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// True when two rectangles overlap on a positive area. Edge- or corner-touching rectangles
    /// (zero-area overlap) are NOT considered intersecting (strict `<`/`>` convention).
    val intersects: a: Rect -> b: Rect -> bool

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// True when `inner` lies entirely within `outer`, inclusive of shared edges.
    val contains: outer: Rect -> inner: Rect -> bool

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// True when `point` lies within `rect`, inclusive of the low and high edges.
    val containsPoint: rect: Rect -> point: Point -> bool

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The geometric center of a rectangle.
    val center: rect: Rect -> Point

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Build a rectangle centered on `center` with the given width and height.
    /// Round-trips with `center`: `center (ofCenter c w h) = c`.
    val ofCenter: center: Point -> width: float -> height: float -> Rect

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// True when the swept path of `moving` displaced by `velocity` overlaps `target` anywhere along
    /// the sweep — detects a fast projectile that would tunnel through a thin `target` within one
    /// step. A superset of `intersects` at both sweep endpoints.
    val sweptIntersects: moving: Rect -> velocity: Point -> target: Rect -> bool
