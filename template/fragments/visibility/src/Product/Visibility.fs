namespace AppRoot

open FS.GG.Game.Core // ADR-0022 P5: Point/Rect now live in the FS.GG.Game.Core bottom layer (moved from FS.GG.UI.Scene/.Canvas)

/// Product-owned 2D-visibility helper — THIS FILE IS YOURS TO ADAPT.
///
/// The geometry vocabulary reuses the framework primitives (no hand-rolled point/vector type):
///   * positions, ray directions and hit vertices are the shared `FS.GG.Game.Core.Point`;
///   * the sight bound and cull region are the shared `FS.GG.Game.Core.Rect`.
/// The ray-segment intersection and the *angular sweep* (`polygon`, below) are the game-opinionated
/// part the framework deliberately does not freeze into a package — edit the sight radius, cone the
/// field of view, swap the polygon output for a per-cell mask, or delete this whole file (the build
/// stays green: its compile item is `Exists`-guarded).
///
/// Everything here is pure, total, and deterministic: endpoints are ordered by a perp-dot (the 2-D
/// cross, `ax*by - ay*bx`) angular comparator (NO `atan2`) with an integer-index tiebreak, and
/// nearest-hit uses a sqrt-free parametric distance, so identical inputs yield byte-identical output
/// across runs and platforms — safe to call from a replayed `update`/`view`.
///
/// Algorithm reference: https://www.redblobgames.com/articles/visibility/
module Visibility =

    /// A wall / occluder: a line segment between two shared `Point`s. The one domain concept the shared
    /// vocabulary lacks (`Point` is a location, `Rect` an AABB) — deliberately NOT a look-alike vector
    /// type. A zero-length segment (`A = B`) occludes nothing.
    type Segment = { A: Point; B: Point }

    /// The editable knob. THIS is the policy to tune per game.
    /// `Radius` is the half-extent of the square sight bound centred on the source: it doubles as the
    /// ray **bound** (an unhit ray terminates on the `source ± Radius` box) AND the cull region, so the
    /// two can never disagree — one radius, one sight box.
    type Settings = { Radius: float }

    /// The visible region from a source: an ordered, closed, counter-clockwise ring of hit points,
    /// bounded by `Radius`. `Source` is the viewpoint it was computed from.
    type VisibilityPolygon = { Source: Point; Vertices: Point list }

    // A tiny angular nudge (linearised rotation) used to shoot rays just past each corner so the sweep
    // captures the walls that begin/end there. Pure arithmetic — no transcendental, so it stays
    // deterministic. Small enough not to skip a real corner, large enough to clear float noise.
    let private nudge = 1e-5

    let private isFinitePoint (p: Point) =
        System.Double.IsFinite p.X && System.Double.IsFinite p.Y

    let private isFiniteSeg (s: Segment) = isFinitePoint s.A && isFinitePoint s.B

    let private sqLen (v: Point) = v.X * v.X + v.Y * v.Y

    /// The portion of the segment `a`→`b` lying inside `rect`, or `None` when it misses entirely.
    /// Liang–Barsky slab clip of the parametric segment against the four half-planes — sqrt-free and
    /// transcendental-free, so it is exact and deterministic. Edge contact counts as a touch
    /// (inclusive, matching `Geometry.containsPoint`), and clips to a zero-length segment.
    ///
    /// This is the cull, and it must never answer `None` for a segment that touches: a dropped occluder
    /// is a wall the viewpoint sees straight through. It is also the *trim*: the returned endpoints are
    /// the occluder's crossings of the bound, which the sweep needs as aim points — a wall spanning the
    /// bound has both its real endpoints outside it, so nothing would otherwise aim at the corners the
    /// visible region turns on. A segment meeting `rect` at a single point clips to a zero-length pair;
    /// it occludes nothing, and `polygon` drops it.
    let private clipSegmentToRect (rect: Rect) (a: Point) (b: Point) : (Point * Point) option =
        let dx = b.X - a.X
        let dy = b.Y - a.Y

        // Narrow the surviving parameter window `[t0, t1]` by one half-plane `p * t <= q`. A `p` of zero
        // means the segment runs parallel to that slab, so it survives only where it already lies inside
        // it (`q >= 0`); an empty window `(1, 0)` is absorbing, so a rejected segment stays rejected.
        // Struct tuples, so narrowing the window itself allocates nothing.
        let inline clip (p: float) (q: float) (struct (t0, t1)) =
            if p = 0.0 then
                if q < 0.0 then struct (1.0, 0.0) else struct (t0, t1)
            else
                let r = q / p
                if p < 0.0 then struct (max t0 r, t1) else struct (t0, min t1 r)

        let window =
            struct (0.0, 1.0)
            |> clip (-dx) (a.X - rect.X)
            |> clip dx (rect.X + rect.Width - a.X)
            |> clip (-dy) (a.Y - rect.Y)
            |> clip dy (rect.Y + rect.Height - a.Y)

        let struct (t0, t1) = window

        if t0 > t1 then
            None
        else
            Some({ X = a.X + t0 * dx; Y = a.Y + t0 * dy }, { X = a.X + t1 * dx; Y = a.Y + t1 * dy })

    /// Nearest ray-segment hit: the point struck and the parametric distance `t >= 0` along the ray
    /// `origin + t*dir`, or `None` when the ray is parallel to / points away from the segment, misses
    /// it, or any input is non-finite. Sqrt-free (parametric) — the shared intersection core.
    /// Total: never throws, never returns a NaN coordinate.
    let raySegment (origin: Point) (dir: Point) (seg: Segment) : (Point * float) option =
        if not (isFinitePoint origin && isFinitePoint dir && isFiniteSeg seg) then
            None
        else
            let ex = seg.B.X - seg.A.X
            let ey = seg.B.Y - seg.A.Y
            // denom = dir × segDir. Zero ⇒ parallel (also covers a zero-length segment: ex = ey = 0).
            let denom = dir.X * ey - dir.Y * ex
            if denom = 0.0 then
                None
            else
                let wx = seg.A.X - origin.X
                let wy = seg.A.Y - origin.Y
                let t = (wx * ey - wy * ex) / denom // (W × segDir) / (dir × segDir)
                let u = (wx * dir.Y - wy * dir.X) / denom // (W × dir) / (dir × segDir)
                if t >= 0.0 && u >= 0.0 && u <= 1.0 then
                    Some({ X = origin.X + t * dir.X; Y = origin.Y + t * dir.Y }, t)
                else
                    None

    /// Point-to-point line-of-sight: is `target` visible from `source` with no segment strictly between
    /// them? Built on `raySegment` (exact — no broad-phase cull). Total on empty/degenerate input.
    let isVisible (source: Point) (target: Point) (segments: Segment list) : bool =
        let dir = { X = target.X - source.X; Y = target.Y - source.Y }
        if not (isFinitePoint source && isFinitePoint target) then
            false
        elif sqLen dir = 0.0 then
            true // the source can always see itself
        else
            // `target` sits at t = 1 along `dir`; a blocker lies strictly between (0 < t < 1).
            segments
            |> List.exists (fun s ->
                match raySegment source dir s with
                | Some(_, t) -> t > 1e-9 && t < 1.0 - 1e-9
                | None -> false)
            |> not

    // The four edges of the axis-aligned bound box `source ± radius`, as synthetic walls, so a ray that
    // hits no real occluder still terminates on the bound (FR-011) and the polygon is always closed.
    let private boundEdges (source: Point) (radius: float) : Segment list * Point list =
        let x0, y0 = source.X - radius, source.Y - radius
        let x1, y1 = source.X + radius, source.Y + radius
        let tl = { X = x0; Y = y0 }
        let tr = { X = x1; Y = y0 }
        let br = { X = x1; Y = y1 }
        let bl = { X = x0; Y = y1 }
        [ { A = tl; B = tr }; { A = tr; B = br }; { A = br; B = bl }; { A = bl; B = tl } ], [ tl; tr; br; bl ]

    // Nearest hit of a single ray against every candidate segment (deterministic: List.fold keeps the
    // first minimum in list order on a `t` tie). `None` only if the ray hits nothing (guarded away by
    // the ever-present bound edges).
    let private nearestHit (source: Point) (dir: Point) (segs: Segment list) : Point option =
        (None, segs)
        ||> List.fold (fun best s ->
            match raySegment source dir s with
            | None -> best
            | Some(p, t) ->
                match best with
                | Some(_, bt) when bt <= t -> best
                | _ -> Some(p, t))
        |> Option.map fst

    // Total rotational order of points around `source`, computed from perp-dot terms only (no `atan2`):
    // half-plane first, then perp-dot sign, then squared distance, then the supplied integer index.
    let private angleCompare (source: Point) (a: Point * int) (b: Point * int) : int =
        let pa, ia = a
        let pb, ib = b
        let va = { X = pa.X - source.X; Y = pa.Y - source.Y }
        let vb = { X = pb.X - source.X; Y = pb.Y - source.Y }
        // half = 0 for angles in [0, π) (upper, incl. +x axis), 1 for [π, 2π): a consistent CCW start.
        let half (v: Point) = if v.Y < 0.0 || (v.Y = 0.0 && v.X < 0.0) then 1 else 0
        let ha, hb = half va, half vb
        if ha <> hb then
            compare ha hb
        else
            let cross = va.X * vb.Y - va.Y * vb.X
            if cross > 0.0 then -1
            elif cross < 0.0 then 1
            else
                let c = compare (sqLen va) (sqLen vb)
                if c <> 0 then c else compare ia ib

    /// The full visibility polygon via angular sweep: cull to the occluders touching the `Radius` bound
    /// box, shoot a ray at every occluder corner (and one either side of it), keep the nearest hit per
    /// ray, and order the hits into a closed CCW ring. Pure and deterministic. This is the function most
    /// games call from `update`/`view`.
    let polygon (settings: Settings) (source: Point) (segments: Segment list) : VisibilityPolygon =
        // Total on a bad radius: fall back to a minimal positive bound rather than throwing.
        let radius =
            if System.Double.IsFinite settings.Radius && settings.Radius > 0.0 then
                settings.Radius
            else
                1.0

        if not (isFinitePoint source) then
            { Source = source; Vertices = [] }
        else

        // Drop non-finite and zero-length occluders up front (total; they can never occlude).
        let real =
            segments
            |> List.filter (fun s -> isFiniteSeg s && sqLen { X = s.B.X - s.A.X; Y = s.B.Y - s.A.Y } > 0.0)

        // Cull to the occluders that actually touch the sight bound box, by an exact segment-vs-box test.
        // NOT a `SpatialGrid` bucket: the grid indexes *points*, so bucketing a segment by its endpoints
        // drops a wall that spans the box with both ends outside it — the viewpoint then sees straight
        // through it. Long walls are the common case, not the corner case, so this test is exact.
        //
        // Each survivor is also *trimmed* to the bound, which is what puts an aim point at the crossing.
        // Trimming cannot change which rays an occluder blocks: the bound box is convex and contains the
        // source, so a ray leaves it exactly once, and the ever-present bound edges already hit it there
        // — any occluder point beyond the crossing loses the nearest-hit comparison to that edge.
        // A wall grazing a corner clips to zero length, occludes nothing, and is dropped.
        let boundRect =
            { X = source.X - radius
              Y = source.Y - radius
              Width = 2.0 * radius
              Height = 2.0 * radius }

        let culled =
            real
            |> List.choose (fun s ->
                match clipSegmentToRect boundRect s.A s.B with
                | Some(a, b) when sqLen { X = b.X - a.X; Y = b.Y - a.Y } > 0.0 -> Some { A = a; B = b }
                | _ -> None)

        let bEdges, bCorners = boundEdges source radius
        let allSegs = culled @ bEdges

        // Aim points: every clipped-occluder endpoint (i.e. its bound crossings) plus the bound corners.
        let aimPoints = [ for s in culled do
                              yield s.A
                              yield s.B
                          yield! bCorners ]

        // For each aim point cast three rays (at it, and nudged either side) to slip past corners.
        let rays =
            [ for p in aimPoints do
                  let d = { X = p.X - source.X; Y = p.Y - source.Y }
                  if sqLen d > 0.0 then
                      yield d
                      yield { X = d.X - nudge * d.Y; Y = d.Y + nudge * d.X }
                      yield { X = d.X + nudge * d.Y; Y = d.Y - nudge * d.X } ]

        let hits =
            rays
            |> List.choose (fun d -> nearestHit source d allSegs)
            |> List.indexed
            |> List.map (fun (i, p) -> p, i)

        let ordered =
            hits
            |> List.sortWith (angleCompare source)
            |> List.map fst

        { Source = source; Vertices = ordered }
