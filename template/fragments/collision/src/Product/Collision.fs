namespace AppRoot

open FS.GG.UI.Scene
open FS.GG.UI.Canvas

/// Product-owned collision helper — THIS FILE IS YOURS TO ADAPT.
///
/// Detection reuses the framework primitives (no hand-rolled AABB, no look-alike geometry type):
///   * narrow-phase overlap uses `FS.GG.UI.Scene.Geometry` on the shared `Rect`/`Point`;
///   * broad-phase pruning uses `FS.GG.UI.Canvas.SpatialGrid`.
/// The *response* rule (`resolve`, below) is the game-opinionated part — edit it, add collision
/// layers, or delete this whole file (the build stays green: its compile item is `Exists`-guarded).
///
/// Everything here is pure, total, and deterministic: pairs are formed in ascending body-index order
/// and the response math is sqrt-free, so identical inputs yield byte-identical output across runs and
/// platforms — safe to call from a replayed `update`.
module Collision =

    /// A collidable thing: its axis-aligned bounds plus a caller-supplied identity/layer payload.
    /// `Tag` is generic (like `SpatialGrid<'T>`) so you never define a look-alike record just to
    /// carry an id — avoiding the consumer-vs-consumer `.Pos`/`.Id` inference footgun.
    type Body<'T> = { Bounds: Rect; Tag: 'T }

    /// A detected overlap between two bodies and how to separate them (pure detection result).
    /// `A` is always the lower-index body of the pair, `B` the higher — a stable, total order.
    /// `Penetration` is the minimum-translation vector that pushes `A` off `B` (push `B` off `A`
    /// with its negation). `Depth` is the overlap along the MTV axis (>= 0).
    type Contact<'T> = { A: Body<'T>; B: Body<'T>; Penetration: Point; Depth: float }

    /// Post-response state for a contact. `Applied` is the displacement given to `A` (for the
    /// consumer's own velocity/response bookkeeping). `Restitution` is a normalized bounce factor
    /// (0.0..1.0) the consumer can fold into its velocity step — the helper itself only separates.
    type Resolution<'T> = { A: Body<'T>; B: Body<'T>; Applied: Point; Restitution: float }

    /// How overlapping bodies separate. THIS is the policy to edit per game.
    type ResponseRule =
        /// Split the minimum-translation 50/50 — both bodies move (default).
        | SeparateEqually
        /// The FIRST body takes the full push; the second is immovable (a wall).
        | PushFirst
        /// The SECOND body takes the full push; the first is immovable (a wall).
        | PushSecond
        /// 50/50 separation with no recorded restitution (slide along the surface).
        | Slide
        /// 50/50 separation plus a recorded restitution (`restitutionPercent`, clamped to 0..100)
        /// for the consumer's velocity reflection — kept as an integer percent so two equal-strength
        /// bounces can never tie-break through floating-point equality.
        | Bounce of restitutionPercent: int

    let private finiteDim (v: float) = if System.Double.IsFinite v then v else 0.0

    /// Narrow-phase: the minimum-translation contact between two bodies, or `None` when they do not
    /// overlap on positive area. Edge-/corner-touching is NOT a contact (strict edges — this defers
    /// to `Geometry.intersects`). Total: non-finite bounds never overlap and never throw.
    let contact (a: Body<'T>) (b: Body<'T>) : Contact<'T> option =
        if not (Geometry.intersects a.Bounds b.Bounds) then
            None
        else
            let overlapX =
                (min (a.Bounds.X + a.Bounds.Width) (b.Bounds.X + b.Bounds.Width))
                - (max a.Bounds.X b.Bounds.X)
            let overlapY =
                (min (a.Bounds.Y + a.Bounds.Height) (b.Bounds.Y + b.Bounds.Height))
                - (max a.Bounds.Y b.Bounds.Y)
            // Require a strictly positive overlap area. A zero-extent body sits *strictly inside* a
            // larger one (so `intersects` is true) yet the overlap is a zero-area line — not a contact,
            // matching the strict-edge convention. NaN overlaps also fail this guard (NaN <= 0 is false,
            // but NaN comparisons on both sides collapse to no-contact) so non-finite bounds are total.
            if not (overlapX > 0.0) || not (overlapY > 0.0) then
                None
            else

            let ca = Geometry.center a.Bounds
            let cb = Geometry.center b.Bounds
            // Separate along the axis of LEAST penetration; ties resolve to X (deterministic).
            let penetration, depth =
                if overlapX <= overlapY then
                    let sign = if ca.X <= cb.X then -1.0 else 1.0   // push A off B
                    { X = sign * overlapX; Y = 0.0 }, overlapX
                else
                    let sign = if ca.Y <= cb.Y then -1.0 else 1.0
                    { X = 0.0; Y = sign * overlapY }, overlapY
            Some { A = a; B = b; Penetration = penetration; Depth = depth }

    /// Broad-phase (SpatialGrid) + narrow-phase over every body pair, returned in ascending
    /// (i, j) index order so the result is fully deterministic. `cellSize` tunes the grid; the
    /// query region is expanded by the largest body half-extent so no overlap is missed (exact —
    /// no false negatives). Total on empty/singleton input (returns `[]`).
    let collide (cellSize: float) (bodies: Body<'T> list) : Contact<'T> list =
        match bodies with
        | []
        | [ _ ] -> []
        | _ ->
            let arr = List.toArray bodies
            let maxHalf =
                (0.0, arr)
                ||> Array.fold (fun acc b -> max acc (max (finiteDim b.Bounds.Width) (finiteDim b.Bounds.Height)))
                |> fun d -> d / 2.0
            let grid =
                SpatialGrid.build cellSize [ for i in 0 .. arr.Length - 1 -> Geometry.center arr.[i].Bounds, i ]
            [ for i in 0 .. arr.Length - 1 do
                  let bi = arr.[i].Bounds
                  let region =
                      { X = bi.X - maxHalf
                        Y = bi.Y - maxHalf
                        Width = bi.Width + 2.0 * maxHalf
                        Height = bi.Height + 2.0 * maxHalf }
                  for j in SpatialGrid.query region grid do
                      if j > i then
                          match contact arr.[i] arr.[j] with
                          | Some c -> yield c
                          | None -> () ]

    /// Apply the response rule to a contact, returning the separated bodies. Pure and deterministic.
    /// EDIT THIS to change how your game resolves overlaps.
    let resolve (rule: ResponseRule) (c: Contact<'T>) : Resolution<'T> =
        let move (b: Body<'T>) (d: Point) =
            { b with Bounds = { b.Bounds with X = b.Bounds.X + d.X; Y = b.Bounds.Y + d.Y } }
        let p = c.Penetration
        let half = { X = p.X / 2.0; Y = p.Y / 2.0 }
        let negate (v: Point) = { X = -v.X; Y = -v.Y }
        match rule with
        | SeparateEqually
        | Slide -> { A = move c.A half; B = move c.B (negate half); Applied = half; Restitution = 0.0 }
        | Bounce pct ->
            let restitution = float (max 0 (min 100 pct)) / 100.0
            { A = move c.A half; B = move c.B (negate half); Applied = half; Restitution = restitution }
        | PushFirst -> { A = move c.A p; B = c.B; Applied = p; Restitution = 0.0 }
        | PushSecond -> { A = c.A; B = move c.B (negate p); Applied = { X = 0.0; Y = 0.0 }; Restitution = 0.0 }

    /// One per-frame pass: detect every collision and resolve it under `rule`, in deterministic pair
    /// order. This is the function most games call from `update`. A single positional pass per frame;
    /// for dense stacking, call it again on the resolved bodies or add your own iteration.
    let step (rule: ResponseRule) (cellSize: float) (bodies: Body<'T> list) : Resolution<'T> list =
        collide cellSize bodies |> List.map (resolve rule)
