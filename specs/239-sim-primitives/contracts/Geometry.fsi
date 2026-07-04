// CONTRACT SKETCH for src/Scene/Geometry.fsi — the intended public surface (Phase 1).
// Conventions (research D2): intersects = strict (<,>) so edge/corner touch is NOT overlap;
// contains / containsPoint = inclusive (>=,<=). All functions are pure and total (NaN-safe).
namespace FS.GG.UI.Scene

/// Public contract module exposed by this FS.GG.UI package.
/// Axis-aligned bounding-box helpers over the shared Scene `Rect`/`Point` — the collision,
/// containment, and centering surface `docs/product.md` recommends consumers reuse.
[<RequireQualifiedAccess>]
module Geometry =

    /// True when two rectangles overlap on a positive area. Edge- or corner-touching
    /// rectangles (zero-area overlap) are NOT considered intersecting (strict convention).
    val intersects: a: Rect -> b: Rect -> bool

    /// True when `inner` lies entirely within `outer`, inclusive of shared edges.
    val contains: outer: Rect -> inner: Rect -> bool

    /// True when `point` lies within `rect`, inclusive of the low/high edges.
    val containsPoint: rect: Rect -> point: Point -> bool

    /// The geometric center of a rectangle.
    val center: rect: Rect -> Point

    /// Build a rectangle centered on `center` with the given width/height.
    /// Round-trips with `center`: `center (ofCenter c w h) = c`.
    val ofCenter: center: Point -> width: float -> height: float -> Rect

    /// True when the swept path of `moving` displaced by `velocity` overlaps `target`
    /// at any point along the sweep — detects fast projectiles that would tunnel through
    /// a thin `target` within one step. Superset of `intersects` at both endpoints.
    val sweptIntersects: moving: Rect -> velocity: Point -> target: Rect -> bool
