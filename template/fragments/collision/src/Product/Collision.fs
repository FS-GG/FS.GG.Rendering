namespace AppRoot

open FS.GG.Game.Core // ADR-0022 P5: Point/Rect + Geometry + SpatialGrid now live in the FS.GG.Game.Core bottom layer (moved from FS.GG.UI.Scene/.Canvas)

/// Product-owned collision helper — THIS FILE IS YOURS TO ADAPT.
///
/// Detection reuses the framework primitives (no hand-rolled AABB, no look-alike geometry type):
///   * narrow-phase overlap uses `FS.GG.UI.Scene.Geometry` on the shared `Rect`/`Point`;
///   * broad-phase pruning uses `FS.GG.UI.Canvas.SpatialGrid`.
/// The *response* rule (`resolve`, below) is the game-opinionated part — edit it, add collision
/// layers, or delete this whole file (the build stays green: its compile item is `Exists`-guarded).
///
/// Everything here is pure, total, and deterministic: pairs are formed in ascending body-index order
/// and the response math is sqrt-free (the swept narrow-phase uses a single deterministic IEEE `sqrt`
/// for the impact distance), so identical inputs yield byte-identical output across runs and platforms
/// — safe to call from a replayed `update`.
module Collision =

    /// A collidable thing: its axis-aligned bounds at the START of the step, the per-step displacement
    /// (`velocity × dt`) it travels this step, plus a caller-supplied identity/layer payload. A wall (or
    /// any body at rest) has `Velocity = { X = 0.0; Y = 0.0 }` and behaves exactly as a static body; give
    /// a fast mover its real per-step displacement so the swept pass (`collide`/`step`) tests the whole
    /// path `Bounds → Bounds + Velocity` and cannot tunnel it clean through a thin target in one step.
    /// `Tag` is generic (like `SpatialGrid<'T>`) so you never define a look-alike record just to
    /// carry an id — avoiding the consumer-vs-consumer `.Pos`/`.Id` inference footgun.
    type Body<'T> = { Bounds: Rect; Velocity: Point; Tag: 'T }

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

    /// The moving narrow-phase: a `Contact` when `a` and `b` overlap at the start of the step OR when
    /// `a`'s swept path — `Bounds → Bounds + Velocity`, taken relative to `b`'s own motion — crosses `b`
    /// during the step. The second case is exactly the tunnelling a static overlap test misses: a body
    /// moving faster than the target is thick is in front of it before the step and behind it after, so a
    /// point test at either end reports a clean miss on a pair that did collide. Defers to `contact` for
    /// an existing overlap (identical MTV); for a pure crossing it advances `a` to first contact along its
    /// path (the minimum translation that stops it AT `b`'s surface rather than past it). Total (NaN-safe)
    /// and deterministic. `collide`/`step` use THIS, not the bare `contact`.
    let sweptContact (a: Body<'T>) (b: Body<'T>) : Contact<'T> option =
        match contact a b with
        | Some _ as existing -> existing
        | None ->
            // Collapse `b`'s motion into `a`'s frame so a single swept test suffices; for the common
            // projectile-vs-wall case `b` is at rest and this is just `a`'s own displacement.
            let d = { X = a.Velocity.X - b.Velocity.X; Y = a.Velocity.Y - b.Velocity.Y }
            if not (Geometry.sweptIntersects a.Bounds d b.Bounds) then
                None
            else
                // First contact via the Minkowski-expanded segment cast: grow `b` by `a`'s extents and
                // cast `a`'s centre along its relative path; the swept box overlaps `b` exactly when this
                // segment meets the expanded box. `T ∈ [0, 1]` is the fraction of the step at first touch.
                let halfW = finiteDim a.Bounds.Width / 2.0
                let halfH = finiteDim a.Bounds.Height / 2.0
                let expanded =
                    { X = b.Bounds.X - halfW
                      Y = b.Bounds.Y - halfH
                      Width = b.Bounds.Width + finiteDim a.Bounds.Width
                      Height = b.Bounds.Height + finiteDim a.Bounds.Height }
                let c0 = Geometry.center a.Bounds
                let c1 = { X = c0.X + d.X; Y = c0.Y + d.Y }
                match Geometry.segmentAabbHit c0 c1 expanded with
                | Some hit ->
                    // Advance `a` from its start to first contact: the minimum translation that leaves it
                    // touching `b`'s near face instead of tunnelled past it.
                    let penetration = { X = d.X * hit.T; Y = d.Y * hit.T }
                    let depth = sqrt (penetration.X * penetration.X + penetration.Y * penetration.Y)
                    Some { A = a; B = b; Penetration = penetration; Depth = depth }
                | None ->
                    // `sweptIntersects` was true but the centre segment never crosses INTO the expanded
                    // box from outside — only a zero-area edge/corner graze at an endpoint reaches here,
                    // which the strict-edge convention (`contact`) already treats as no contact.
                    None

    /// Broad-phase (SpatialGrid) + swept narrow-phase over every body pair, returned in ascending
    /// (i, j) index order so the result is fully deterministic. `cellSize` tunes the grid; the query
    /// region is expanded by the largest body half-extent AND the largest per-step displacement so no
    /// overlap — including one a fast body only touches mid-sweep — is missed (exact, no false
    /// negatives). Total on empty/singleton input (returns `[]`).
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
            // A fast body's swept path can reach a cell its centre never occupies, so the query region
            // must also grow by the largest per-step displacement — otherwise the grid prunes away the
            // very tunnelling pair the swept narrow-phase exists to catch. The L∞ (max-axis) bound keeps
            // this sqrt-free; both bodies of a pair can move, hence 2×. Over-covering only adds candidate
            // pairs (the narrow-phase stays exact), never a false negative.
            let maxDisp =
                (0.0, arr)
                ||> Array.fold (fun acc b ->
                    max acc (max (abs (finiteDim b.Velocity.X)) (abs (finiteDim b.Velocity.Y))))
            let pad = maxHalf + 2.0 * maxDisp
            let grid =
                SpatialGrid.build cellSize [ for i in 0 .. arr.Length - 1 -> Geometry.center arr.[i].Bounds, i ]
            [ for i in 0 .. arr.Length - 1 do
                  let bi = arr.[i].Bounds
                  let region =
                      { X = bi.X - pad
                        Y = bi.Y - pad
                        Width = bi.Width + 2.0 * pad
                        Height = bi.Height + 2.0 * pad }
                  for j in SpatialGrid.query region grid do
                      if j > i then
                          match sweptContact arr.[i] arr.[j] with
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

    /// One per-frame pass: detect every collision over each body's swept step (so a fast mover cannot
    /// tunnel a thin target) and resolve it under `rule`, in deterministic pair order. This is the
    /// function most games call from `update`. A single swept pass per frame; for dense stacking, call it
    /// again on the resolved bodies or add your own iteration.
    let step (rule: ResponseRule) (cellSize: float) (bodies: Body<'T> list) : Resolution<'T> list =
        collide cellSize bodies |> List.map (resolve rule)
