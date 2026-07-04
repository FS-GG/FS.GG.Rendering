namespace FS.GG.UI.Scene

[<RequireQualifiedAccess>]
module Geometry =

    // Strict edges (matches the framework's internal `intersects` at Evidence.fs / Scene.fs /
    // TestingVisual.fs): edge- and corner-touching rectangles have zero-area overlap ⇒ not intersecting.
    // NaN-safe: any NaN operand makes a comparison false, so the whole conjunction is false (no throw).
    let intersects (a: Rect) (b: Rect) : bool =
        a.X < b.X + b.Width
        && a.X + a.Width > b.X
        && a.Y < b.Y + b.Height
        && a.Y + a.Height > b.Y

    // Inclusive edges: `inner` flush against `outer`'s edge is contained.
    let contains (outer: Rect) (inner: Rect) : bool =
        inner.X >= outer.X
        && inner.Y >= outer.Y
        && inner.X + inner.Width <= outer.X + outer.Width
        && inner.Y + inner.Height <= outer.Y + outer.Height

    // Inclusive edges: a point on the low or high edge is contained.
    let containsPoint (rect: Rect) (point: Point) : bool =
        point.X >= rect.X
        && point.X <= rect.X + rect.Width
        && point.Y >= rect.Y
        && point.Y <= rect.Y + rect.Height

    let center (rect: Rect) : Point =
        { X = rect.X + rect.Width / 2.0
          Y = rect.Y + rect.Height / 2.0 }

    let ofCenter (center: Point) (width: float) (height: float) : Rect =
        { X = center.X - width / 2.0
          Y = center.Y - height / 2.0
          Width = width
          Height = height }

    // Swept AABB via Minkowski expansion: grow `target` by `moving`'s extents so `moving` collapses to
    // its min-corner point, then clip the motion segment (point → point+velocity) against the expanded
    // box with the Liang–Barsky slab method. A start/end that overlaps `target` puts the corresponding
    // segment endpoint inside the expanded box, so this is a superset of the static `intersects` at both
    // endpoints while also catching fast projectiles that tunnel through a thin target in one step.
    let sweptIntersects (moving: Rect) (velocity: Point) (target: Rect) : bool =
        let minX = target.X - moving.Width
        let maxX = target.X + target.Width
        let minY = target.Y - moving.Height
        let maxY = target.Y + target.Height
        let px, py = moving.X, moving.Y
        let dx, dy = velocity.X, velocity.Y

        // Clip parameter t ∈ [0,1] against one axis' slab; returns the narrowed (tEnter, tExit) or None.
        let clip p d lo hi (tEnter: float) (tExit: float) : (float * float) option =
            if d = 0.0 then
                if p < lo || p > hi then None else Some(tEnter, tExit)
            else
                let t1 = (lo - p) / d
                let t2 = (hi - p) / d
                let tNear = min t1 t2
                let tFar = max t1 t2
                let e = max tEnter tNear
                let x = min tExit tFar
                if e > x then None else Some(e, x)

        match clip px dx minX maxX 0.0 1.0 with
        | None -> false
        | Some(e, x) ->
            match clip py dy minY maxY e x with
            | None -> false
            | Some(e2, x2) -> e2 <= x2
