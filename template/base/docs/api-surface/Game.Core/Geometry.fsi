// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.2.0 (src/Game.Core/Geometry.fsi); regenerate when $(FsGgGameVersion) moves.
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
    /// Narrow-phase AABB contact manifold. Returns `Some contact` exactly when `intersects a b`
    /// (positive-area overlap, strict edges) and `None` on touch or gap — so `(aabbContact a b)
    /// |> Option.isSome` agrees with `intersects a b` for all inputs. The `Contact.Normal` points
    /// from `a` toward `b` along the minimum-penetration axis and `Contact.Depth` is that
    /// penetration (the minimum translation vector). Pure and total (NaN-safe). Deterministic
    /// tie-breaks, fixed for byte-identical output: equal penetration on both axes resolves to the
    /// X axis; a zero centre-delta on the chosen axis resolves to the +axis direction.
    val aabbContact: a: Rect -> b: Rect -> Contact option

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Narrow-phase circle–circle contact manifold. Returns `Some contact` exactly when the two
    /// circles overlap on positive area (centre distance strictly less than the radius sum) and
    /// `None` on touch or gap. `Normal` is the unit vector from `a`'s centre toward `b`'s, and
    /// `Depth = (a.Radius + b.Radius) − centreDistance`. The overlap test uses squared distance; the
    /// sole `sqrt` (a correctly-rounded, cross-platform-deterministic IEEE op) builds the manifold on
    /// a hit. Pure and total: a NaN or non-positive radius yields `None`. Coincident centres are the
    /// documented degenerate — `Normal = (1, 0)`, `Depth = a.Radius + b.Radius`.
    val circleContact: a: Circle -> b: Circle -> Contact option

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Narrow-phase circle–AABB contact manifold. Returns `Some contact` exactly when the circle
    /// overlaps the box on positive area, computed by clamping the circle centre to the box and
    /// comparing squared distance to squared radius; `None` otherwise. As with `aabbContact`,
    /// `Normal` points from the circle toward the box and the separating translation of the circle is
    /// `−Normal × Depth`. When the centre lies inside the box, the box encloses the circle, so
    /// `−Normal` is the outward normal of the least-penetration face (equal-penetration tie-break:
    /// X axis, +bias — identical to `aabbContact`) and `Depth` is that penetration plus the radius.
    /// Pure and total (a NaN or non-positive radius yields `None`).
    val circleAabbContact: c: Circle -> box: Rect -> Contact option

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Segment-cast against an AABB by the slab method. Returns `Some hit` at the first crossing INTO
    /// the box from outside with parameter `T ∈ [0,1]`, and `None` on a miss, a segment starting
    /// inside the box, or a box behind the segment. `Point = p0 + (p1 − p0)·T` lies on the box
    /// boundary and `Normal` is the outward unit axis of the entered face. A corner entry (equal
    /// per-axis entry parameter) resolves to the X face. Pure and total: a zero-length segment or any
    /// NaN operand yields `None`.
    val segmentAabbHit: p0: Point -> p1: Point -> box: Rect -> RayHit option

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Segment-cast against a circle via the ray–circle quadratic. Returns `Some hit` at the near root
    /// when it lies in `[0,1]` (the segment enters the circle from outside), and `None` on a miss
    /// (negative discriminant), a degenerate segment, a non-positive radius, or an origin inside the
    /// circle. `Point = p0 + (p1 − p0)·T` lies on the circle and `Normal = (Point − Center) / Radius`
    /// is unit length. Pure and total (NaN or non-positive radius yields `None`).
    val segmentCircleHit: p0: Point -> p1: Point -> c: Circle -> RayHit option

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Build a convex polygon for an oriented bounding box: the four corners of the box centered on
    /// `center` with the given `halfExtents`, rotated by `rotation` radians (counter-clockwise
    /// positive), emitted in CCW winding order. At `rotation = 0` the corners are the axis-aligned box,
    /// so an `obbPolygon` collides through `polygonContact` consistently with `aabbContact`. The result
    /// satisfies the convex/CCW input convention of `ConvexPolygon` by construction.
    val obbPolygon: center: Point -> halfExtents: Point -> rotation: float -> ConvexPolygon

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Narrow-phase convex-polygon contact manifold via the Separating Axis Theorem (SAT). Returns
    /// `Some contact` exactly when the two convex polygons overlap on positive area (every candidate
    /// axis shows a positive projection overlap) and `None` when any axis separates them — a gap or a
    /// touch (zero overlap), matching the strict-edge convention of `aabbContact`. The candidate axes
    /// are the outward edge normals of both polygons (a's in vertex order, then b's), deduplicated so
    /// antiparallel/duplicate directions collapse to one. `Contact.Depth` is the minimum projection
    /// overlap (the minimum translation vector magnitude) and `Contact.Normal` is that axis re-oriented
    /// from `a` toward `b` by the centroid delta, so translating `a` by `−Normal × Depth` separates the
    /// pair. An equal-minimum overlap on more than one axis resolves to the first in the a-then-b
    /// generation order (byte-deterministic). Pure and total: a polygon with fewer than 3 vertices, a
    /// zero-area polygon, or any NaN coordinate yields `None` without throwing.
    val polygonContact: a: ConvexPolygon -> b: ConvexPolygon -> Contact option

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// True when the swept path of `moving` displaced by `velocity` overlaps `target` anywhere along
    /// the sweep — detects a fast projectile that would tunnel through a thin `target` within one
    /// step. A superset of `intersects` at both sweep endpoints.
    val sweptIntersects: moving: Rect -> velocity: Point -> target: Rect -> bool
