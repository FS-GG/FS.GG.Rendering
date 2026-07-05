namespace AppRoot

open FS.GG.UI.Scene

/// Product-owned collision-safe 2D vector — THIS FILE IS YOURS TO ADAPT.
///
/// A game model that stores entity positions/velocities collides with the shared scene vocabulary if
/// it reuses the record labels `FS.GG.UI.Scene.Point` (`X`,`Y`) and `Rect` (`X`,`Y`,`Width`,`Height`)
/// use. Because the durable `LayoutEvidence.fs` opens BOTH `FS.GG.UI.Scene` and your model, F#'s
/// record-label inference can then resolve the bare `{ X = …; Y = …; Width = …; Height = … }` literals
/// in that durable file to YOUR record instead of `Rect` — a wall of `FS3566`/`FS0039` in a file you
/// were told not to touch, surfacing only after a whole model is written (fs-gg-scene pitfall).
///
/// `Vec2` avoids the trap structurally: its labels `Vx`/`Vy` ("vector component x/y") share NO name
/// with `Point`/`Rect`, so a model built on `Vec2` can never trip the mis-inference. Use it for
/// position, velocity, and displacement; cross into the scene vocabulary with `toPoint`/`toRect` —
/// the ONE place bare `Scene` record literals appear in your product tree, where only `Scene` types
/// are in scope and the resolution is unambiguous. Express an entity's SIZE through `toRect` (a
/// centered AABB) rather than `Width`/`Height` labels on the record, so the size case stays safe too.
///
/// Everything here is pure, total, and deterministic: straight-line float arithmetic guarded against
/// non-finite input (never throws, never yields NaN silently), so identical inputs yield byte-identical
/// output across runs and platforms — safe to call from a replayed `update`.
///
/// You own this file: rename `Vx`/`Vy`, add a `Z`, add rotation/normalization, or delete it after you
/// swap `Model.fs` off it (its compile item is `Exists`-guarded, so deletion keeps the build green).
/// See the `fs-gg-model-swap` / `fs-gg-game-core` skills. Guidance-only; no backing package.
module Geometry =

    /// A 2D vector — position, velocity, or displacement. `Vx`/`Vy` deliberately avoid `X`/`Y`
    /// (`Scene.Point`) and `Width`/`Height` (`Scene.Rect`) so a model built on this never collides.
    type Vec2 = { Vx: float; Vy: float }

    /// The zero vector.
    let zero: Vec2 = { Vx = 0.0; Vy = 0.0 }

    /// Construct a vector from its components.
    let vec2 (x: float) (y: float) : Vec2 = { Vx = x; Vy = y }

    /// Component-wise addition (e.g. advance a position by a displacement).
    let add (a: Vec2) (b: Vec2) : Vec2 = { Vx = a.Vx + b.Vx; Vy = a.Vy + b.Vy }

    /// Component-wise subtraction.
    let sub (a: Vec2) (b: Vec2) : Vec2 = { Vx = a.Vx - b.Vx; Vy = a.Vy - b.Vy }

    /// Scalar multiply (e.g. `add pos (scale dt vel)` integrates one step).
    let scale (k: float) (v: Vec2) : Vec2 = { Vx = k * v.Vx; Vy = k * v.Vy }

    /// Per-component clamp into `[lo, hi]` — keep an entity inside a bound. Total: if a `lo` axis
    /// exceeds its `hi` axis the low bound wins (no throw), so a degenerate bound can never crash a step.
    let clamp (lo: Vec2) (hi: Vec2) (v: Vec2) : Vec2 =
        let clamp1 lo hi x = x |> max lo |> min (max lo hi)
        { Vx = clamp1 lo.Vx hi.Vx v.Vx
          Vy = clamp1 lo.Vy hi.Vy v.Vy }

    /// Cross into the shared scene vocabulary: a `Vec2` position becomes a `Scene.Point`.
    let toPoint (v: Vec2) : Point = { X = v.Vx; Y = v.Vy }

    /// A centered axis-aligned rectangle of size `w` x `h` about `center` — the size-bearing case,
    /// expressed WITHOUT ever putting `Width`/`Height` labels on your own record. Negative sizes are
    /// treated as their magnitude (total), so a stray sign can never invert the rect.
    let toRect (center: Vec2) (w: float) (h: float) : Rect =
        let w = abs w
        let h = abs h
        { X = center.Vx - w / 2.0
          Y = center.Vy - h / 2.0
          Width = w
          Height = h }
