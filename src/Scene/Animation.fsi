namespace FS.Skia.UI.Scene

open System

/// Declarative motion for FS.Skia.UI (feature 073). A bounded, additive slice:
/// an author declares — as data against an existing `Scene` — that opacity, an
/// affine transform, and/or color should travel from a start value to a target
/// value over a duration shaped by a named easing curve. Sampling is a **pure**
/// function of an explicit `TimeSpan`, so identical inputs and identical time
/// samples always produce byte-identical output. A deliberate identity-at-rest
/// lowering makes a settled animation byte-identical to the static render of the
/// same widget.

/// The named easing curves. Endpoints are pinned for every case
/// (`Easing.apply e 0.0 = 0.0`, `Easing.apply e 1.0 = 1.0`).
type Easing =
    | Linear
    | EaseIn
    | EaseOut
    | EaseInOut

/// An affine 2D transform expressed with motion-specific labels (deliberately
/// NOT Scene's `X`/`Y`/`Width`/`Height`, to avoid bare-literal inference
/// collisions). Identity is `TranslateX/Y = 0`, `ScaleX/Y = 1`,
/// `RotationDegrees = 0`.
type Transform =
    { TranslateX: float
      TranslateY: float
      ScaleX: float
      ScaleY: float
      RotationDegrees: float }

/// One declared property motion from `Start` to `End` over `Duration`, shaped by
/// `Easing`. `Easing` and `Duration` are mandatory fields (no omitted-field
/// defaulting).
type Tween<'a> =
    { Start: 'a
      End: 'a
      Duration: TimeSpan
      Easing: Easing }

/// The author-declared, sample-as-data motion applied to a target `Scene`. Each
/// property is optional; an absent property is treated as its identity.
type Animation =
    { Opacity: Tween<float> option
      Transform: Tween<Transform> option
      Color: Tween<Color> option }

/// Stateful retargeting value held by the author in their own model. All
/// transitions are pure (Principle IV); the framework owns no hidden mutable
/// animation registry.
///
/// `Interp` carries the per-`'a` interpolant supplied at `create` so that the
/// interp-free `advance` / `value` signatures (data-model.md) can recompute
/// `Current` for the generic `'a`. (Resolves the contract's internal
/// inconsistency between `create` taking `interp` and the 6-field record having
/// nowhere to store it — see `readiness/package-surface-expectations.md`.)
type AnimationState<'a> =
    { Current: 'a
      Start: 'a
      Target: 'a
      Elapsed: TimeSpan
      Duration: TimeSpan
      Easing: Easing
      Interp: 'a -> 'a -> float -> 'a }

/// Public contract module exposed by this FS.Skia.UI package.
module Easing =
    /// Maps normalized progress `t` to eased progress. Input `t` is clamped to
    /// `[0,1]` before the curve, so out-of-domain samples yield the endpoint.
    val apply: easing: Easing -> t: float -> float
    /// The documented default curve when easing is unspecified (FR-003) =
    /// `EaseInOut`.
    val Default: Easing

/// Public contract module exposed by this FS.Skia.UI package.
module Transform =
    /// The all-identity transform (no translate, unit scale, no rotation).
    val identity: Transform
    /// True when the transform is the identity value.
    val isIdentity: transform: Transform -> bool
    /// Per-field linear interpolation between two transforms.
    val lerp: a: Transform -> b: Transform -> t: float -> Transform
    /// Composes translate ∘ rotate ∘ scale into the existing 3×3
    /// `PerspectiveTransform` (`M31 = M32 = 0`, `M33 = 1`).
    val toPerspectiveTransform: transform: Transform -> PerspectiveTransform

/// Public contract module exposed by this FS.Skia.UI package.
module Tween =
    /// Normalized, eased, clamped progress in `[0,1]`. `Duration ≤ 0` ⇒ `1.0`
    /// (no divide-by-zero).
    val progress: elapsed: TimeSpan -> tween: Tween<'a> -> float
    /// The interpolated value at a time sample, using the caller-supplied
    /// interpolant for `'a`. Monotone in elapsed per its easing.
    val sample: interp: ('a -> 'a -> float -> 'a) -> elapsed: TimeSpan -> tween: Tween<'a> -> 'a

/// Public contract module exposed by this FS.Skia.UI package.
module Animation =
    /// Linear interpolation between two floats (the `'a = float` interpolant
    /// passed to `Tween.sample` / `AnimationState.create`). Sibling of
    /// `lerpColor` and `Transform.lerp`. Lives here because a namespace cannot
    /// hold a bare value.
    val lerpFloat: a: float -> b: float -> t: float -> float
    /// Per-RGBA-byte (rounded) linear interpolation between two colors (the
    /// `'a = Color` interpolant). A flat binding rather than a `Color` submodule
    /// because the `Color` type already occupies that name in the namespace.
    val lerpColor: a: Color -> b: Color -> t: float -> Color
    /// The no-op animation: every property absent. `applyAt` over `empty`
    /// returns the target unwrapped at every time sample.
    val empty: Animation
    /// Pure sampling: produce the target scene transformed for the given time
    /// sample. Identity-at-rest rule (R5): when the sampled opacity is `1.0` and
    /// the sampled transform is identity, returns the target scene's node
    /// unwrapped (byte-identical to static); a non-identity transform lowers to
    /// a `PerspectiveNode`.
    val applyAt: elapsed: TimeSpan -> animation: Animation -> target: Scene -> SceneNode
    /// Samples the animation at explicit time points for deterministic evidence.
    val sampleFrames: times: TimeSpan list -> animation: Animation -> target: Scene -> Scene list
    /// True when every present tween has `elapsed ≥ Duration`; drives redraw
    /// gating.
    val isSettled: elapsed: TimeSpan -> animation: Animation -> bool

/// Public contract module exposed by this FS.Skia.UI package.
module AnimationState =
    /// Initial state: `Current = Start = Target = initial`, `Elapsed = 0`. The
    /// `interp` argument is the per-`'a` interpolant (`lerpFloat` / `Color.lerp`
    /// / `Transform.lerp`).
    val create: interp: ('a -> 'a -> float -> 'a) -> initial: 'a -> duration: TimeSpan -> easing: Easing -> AnimationState<'a>
    /// Adds the delta to `Elapsed` (capped at `Duration`) and recomputes
    /// `Current` via easing `Start`→`Target`.
    val advance: delta: TimeSpan -> state: AnimationState<'a> -> AnimationState<'a>
    /// Retargets from the currently displayed value: `Start = Current`,
    /// `Target = newTarget`, `Elapsed = 0` — continues from the displayed value,
    /// no snap-back (FR-005).
    val retarget: newTarget: 'a -> state: AnimationState<'a> -> AnimationState<'a>
    /// Returns the currently displayed value (`Current`).
    val value: state: AnimationState<'a> -> 'a
    /// True while the transition is still in flight
    /// (`Elapsed < Duration && Current <> Target`).
    val isActive: state: AnimationState<'a> -> bool when 'a: equality
