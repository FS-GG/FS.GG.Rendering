namespace AppRoot

open FS.GG.UI.Scene
open FS.GG.UI.Canvas

/// Product-owned 2D-visibility helper — THIS FILE IS YOURS TO ADAPT.
///
/// The geometry vocabulary reuses the framework primitives (no hand-rolled point/vector type):
///   * positions, ray directions and hit vertices are the shared `FS.GG.UI.Scene.Point`;
///   * the sight bound and cull region are the shared `FS.GG.UI.Scene.Rect`;
///   * broad-phase culling of nearby occluders reuses `FS.GG.UI.Canvas.SpatialGrid`.
/// The ray-segment intersection and the *angular sweep* (`polygon`, below) are the game-opinionated
/// part the framework deliberately does not freeze into a package — edit the sight radius, cone the
/// field of view, swap the polygon output for a per-cell mask, or delete this whole file (the build
/// stays green: its compile item is `Exists`-guarded).
///
/// Everything here is pure, total, and deterministic: endpoints are ordered by a cross-product angular
/// comparator (NO `atan2`) with an integer-index tiebreak, and nearest-hit uses a sqrt-free parametric
/// distance, so identical inputs yield byte-identical output across runs and platforms — safe to call
/// from a replayed `update`/`view`.
///
/// Algorithm reference: https://www.redblobgames.com/articles/visibility/
module Visibility =

    /// A wall / occluder: a line segment between two shared `Point`s. The one domain concept the shared
    /// vocabulary lacks (`Point` is a location, `Rect` an AABB) — deliberately NOT a look-alike vector
    /// type. A zero-length segment (`A = B`) occludes nothing.
    type Segment = { A: Point; B: Point }

    /// The editable knobs. THIS is the policy to tune per game.
    /// `Radius` is the half-extent of the square sight bound centred on the source: it doubles as the
    /// ray **bound** (an unhit ray terminates on the `source ± Radius` box) AND the broad-phase cull
    /// region, so the two can never disagree. `CellSize` tunes the `SpatialGrid` used to cull occluders.
    type Settings = { Radius: float; CellSize: float }

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

    // Total rotational order of points around `source`, computed from cross products only (no `atan2`):
    // half-plane first, then cross-product sign, then squared distance, then the supplied integer index.
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

    /// The full visibility polygon via angular sweep: cull occluders inside the `Radius` bound with
    /// `SpatialGrid`, shoot a ray at every occluder corner (and one either side of it), keep the nearest
    /// hit per ray, and order the hits into a closed CCW ring. Pure and deterministic. This is the
    /// function most games call from `update`/`view`.
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

        // Broad-phase cull: bucket each segment by BOTH endpoints, keep those with an endpoint inside the
        // bound box (reuses SpatialGrid — no hand-rolled bucketing). A chord crossing the box with both
        // ends outside is a documented broad-phase simplification.
        let boundRect =
            { X = source.X - radius
              Y = source.Y - radius
              Width = 2.0 * radius
              Height = 2.0 * radius }

        let indexed = List.indexed real
        let grid =
            SpatialGrid.build settings.CellSize [ for i, s in indexed do
                                                      yield s.A, i
                                                      yield s.B, i ]
        let culledIdx = SpatialGrid.query boundRect grid |> List.distinct |> List.sort
        let culled = [ for i in culledIdx -> real.[i] ]

        let bEdges, bCorners = boundEdges source radius
        let allSegs = culled @ bEdges

        // Aim points: every culled-occluder endpoint plus the bound corners.
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
