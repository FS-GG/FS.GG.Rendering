// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Geometry.fsi); regenerate when $(FsGgGameVersion) moves.
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
    /// documented degenerate — `Normal = (1, 0)`, `Depth = a.Radius + b.Radius`. NB the sibling producer
    /// `Physics.manifold` makes the OPPOSITE call on a circle–circle pair with coincident centres (returns
    /// no manifold rather than an invented normal): this primitive guarantees a total `Contact` on every
    /// overlap, picking a deterministic fallback direction so a caller always gets separation guidance,
    /// whereas the solver refuses to feed a physically-absent normal (and its zero-length warm-start key)
    /// into the impulse pass. Two deliberate, documented answers to the same degenerate — not a discrepancy.
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
    /// overlap (the minimum translation vector magnitude) and `Contact.Normal` is that axis oriented
    /// along the nearer exit: on the MTV axis the pair can separate by pushing `a` either way, and the
    /// normal points along whichever of the two signed exit distances is shorter, so translating `a` by
    /// `−Normal × Depth` is the least-penetration separation (equivalently, the a→b direction — the
    /// shorter exit always moves `a` away from `b`). No centroid is computed. An equal-minimum overlap
    /// on more than one axis resolves to the first in the a-then-b
    /// generation order (byte-deterministic). Pure and total: a polygon with fewer than 3 vertices, a
    /// zero-area polygon, or any NaN coordinate yields `None` without throwing.
    val polygonContact: a: ConvexPolygon -> b: ConvexPolygon -> Contact option

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Narrow-phase convex-polygon contact POINTS — what `polygonContact` cannot give you. SAT returns
    /// a separating axis and a depth; the points where the pair actually touches need reference-face
    /// selection plus Sutherland–Hodgman clipping of the incident face. An angular impulse needs them
    /// (the lever arms from each centre of mass), and so does most-overlap-first stacking.
    ///
    /// Additive: `Contact` and `polygonContact` are unchanged, and `Resolution` still consumes them.
    ///
    /// `Normal` and `Depth` come from the same SAT scan `polygonContact` uses, so for all inputs
    /// `polygonManifold a b |> ValueOption.isSome` agrees with `polygonContact a b |> Option.isSome`
    /// (same strict-edge convention: a touch is not a contact), and on a hit the two agree bit for bit
    /// on `Normal` and `Depth` — including the a-then-b generation-order tie-break, and so including
    /// its documented divergence from `aabbContact`'s X-bias on an exactly-equal penetration.
    /// `A`/`B` are the argument positions `0`/`1` (so `A < B`); `Normal` points from `a` toward `b`.
    ///
    /// The *reference face* is whichever face of `a` or `b` the other polygon penetrates least — a
    /// directed face query, so it is always a real face and its penetration is exactly `Depth`. The
    /// *incident face* is the face of the other polygon most anti-parallel to it. Clipping the incident
    /// face to the reference face's side planes yields the contact.
    ///
    /// `Points` holds `PointCount` ∈ {1, 2} contact points — 2 for a face-on-face contact, 1 when a
    /// vertex pokes into a face. Each lies exactly on the boundary of the *incident* polygon (it is a
    /// point of its clipped face) and at signed distance within `[−Depth, 0]` of the *reference*
    /// polygon's contacting face, hence within `Depth` of that polygon's boundary too: the pair met
    /// there, to within the penetration the manifold reports. Points that clip to the same position
    /// collapse to one.
    ///
    /// `FeatureId` identifies which pair of faces produced the contact and is stable across ticks for
    /// an unmoving pair — the warm-start cache key (see `Manifold`). Opaque: compare, do not decode.
    ///
    /// Deterministic tie-breaks, fixed for byte-identical output, all of them the same first-wins rule
    /// `polygonContact` applies to its axis scan: an exact tie in penetration between a face of `a` and
    /// one of `b` (parallel faces — two axis-aligned boxes, say) makes `a`'s the reference; within one
    /// polygon, an exact tie between faces resolves to the first in vertex order; and the incident face
    /// is tie-broken the same way.
    ///
    /// Winding: `ConvexPolygon`'s CCW convention (an input assumption, not runtime-enforced) is
    /// load-bearing HERE in a way it is not for `polygonContact`. `Normal` and `Depth` come from the
    /// SAT scan, which orients the MTV by the projection exit and is winding-agnostic — a CW-wound
    /// polygon still yields the correct `Normal`/`Depth` (and so still agrees with `polygonContact`).
    /// The contact *points* do NOT survive the same abuse: reference- and incident-face selection
    /// argmax over each face's OUTWARD normal, which is outward only for a CCW ring — a CW ring flips
    /// every edge normal inward, so a CW polygon selects the wrong reference/incident faces and its
    /// `Points`, `PointCount`, and `FeatureId` (the warm-start key) are unreliable. This is a
    /// wrong-answer limitation, not a totality violation: a CW input still returns without throwing.
    /// The trap is that `Physics` tolerates either winding for mass and circle-vs-poly contacts, so a
    /// CW body is accepted with correct mass yet corrupted poly-vs-poly points — feed CCW rings (as
    /// `obbPolygon` does by construction) if you consume `Points`/`FeatureId`.
    ///
    /// Pure and total: a polygon with fewer than 3 vertices, a zero-area polygon, or any NaN
    /// coordinate yields `ValueNone` without throwing, exactly as `polygonContact` yields `None`.
    val polygonManifold: a: ConvexPolygon -> b: ConvexPolygon -> Manifold voption

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Segment-cast against a convex polygon by half-plane clipping (the slab method with one half-plane
    /// per edge instead of one per axis). Returns `Some hit` at the first crossing INTO the polygon from
    /// outside with parameter `T ∈ [0,1]`, and `None` on a miss, a segment starting inside, or a polygon
    /// behind the segment. `Point = p0 + (p1 − p0)·T` lies on the polygon boundary and `Normal` is the
    /// outward unit normal of the ENTERED edge — the value that identifies which face was struck.
    /// Strict edges, matching `aabbContact`/`polygonContact`: where a zero-AREA overlap is not a contact,
    /// a zero-LENGTH chord is not a hit — so a graze that clips a single vertex is NOT a hit. A segment
    /// collinear with an edge IS a hit (its chord has positive length), reporting the edge it crossed to
    /// reach the boundary. The clipped vertex is the sole divergence in the RULE from `segmentAabbHit`,
    /// which reports that graze as a hit; otherwise, for a `rotation = 0` `obbPolygon`, `segmentPolygonHit`
    /// agrees with `segmentAabbHit` on `T` and `Point`. (A segment endpoint lying exactly on the boundary
    /// can still flip either way between the two, because a `Rect` edge `X + Width` and the corresponding
    /// corner `centre + halfExtent` need not be the same double — the two shapes differ, not the two
    /// rules.) A corner ENTRY (adjacent edges sharing the maximal entry
    /// parameter, to within a `1e-9` tolerance on the dimensionless `T` that keeps the choice out of the
    /// hands of floating-point rounding) resolves to the FIRST such edge in vertex order — deterministic,
    /// and distinct from `segmentAabbHit`'s X-face tie-break, exactly as `polygonContact`'s tie normal is
    /// distinct from `aabbContact`'s. Pure and total: fewer than 3 vertices, a zero-area polygon, a
    /// zero-length segment, or any NaN coordinate yields `None`.
    val segmentPolygonHit: p0: Point -> p1: Point -> poly: ConvexPolygon -> RayHit option

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// True when the swept path of `moving` displaced by `velocity` overlaps `target` anywhere along
    /// the sweep — detects a fast projectile that would tunnel through a thin `target` within one
    /// step. A superset of `intersects` at both sweep endpoints.
    val sweptIntersects: moving: Rect -> velocity: Point -> target: Rect -> bool
