// See skill: fs-gg-scene
namespace FS.GG.UI.Scene

open System

/// Declarative motion for FS.GG.UI (feature 073). A bounded, additive slice:
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

/// Public contract module exposed by this FS.GG.UI package.
module Easing =
    /// Maps normalized progress `t` to eased progress. Input `t` is clamped to
    /// `[0,1]` before the curve, so out-of-domain samples yield the endpoint.
    val apply: easing: Easing -> t: float -> float
    /// The documented default curve when easing is unspecified (FR-003) =
    /// `EaseInOut`.
    val Default: Easing

/// Public contract module exposed by this FS.GG.UI package.
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

/// Public contract module exposed by this FS.GG.UI package.
module Tween =
    /// Normalized, eased, clamped progress in `[0,1]`. `Duration ≤ 0` ⇒ `1.0`
    /// (no divide-by-zero).
    val progress: elapsed: TimeSpan -> tween: Tween<'a> -> float
    /// The interpolated value at a time sample, using the caller-supplied
    /// interpolant for `'a`. Monotone in elapsed per its easing.
    val sample: interp: ('a -> 'a -> float -> 'a) -> elapsed: TimeSpan -> tween: Tween<'a> -> 'a

/// Public contract module exposed by this FS.GG.UI package.
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
    /// Samples the `Color` tween at the given time (`None` when unset). `applyAt` composes only
    /// opacity + transform onto the scene — the frozen wire format has no scene-wide tint node — so
    /// the animated colour is surfaced here for consumers to drive their own recolouring rather than
    /// being silently dropped (P6 / R5).
    val sampleColor: elapsed: TimeSpan -> animation: Animation -> Color option
    /// Pure sampling: produce the target scene transformed for the given time
    /// sample. Identity-at-rest rule (R5): when the sampled opacity is `1.0` and
    /// the sampled transform is identity, returns the target scene's node
    /// unwrapped (byte-identical to static); a non-identity transform lowers to
    /// a `PerspectiveNode`. Composes opacity + transform only; the `Color` tween is
    /// sampled via `sampleColor`, not composited here (P6 / R5).
    val applyAt: elapsed: TimeSpan -> animation: Animation -> target: Scene -> SceneNode
    /// Samples the animation at explicit time points for deterministic evidence.
    val sampleFrames: times: TimeSpan list -> animation: Animation -> target: Scene -> Scene list
    /// True when every present tween has `elapsed ≥ Duration`; drives redraw
    /// gating.
    val isSettled: elapsed: TimeSpan -> animation: Animation -> bool

/// Public contract module exposed by this FS.GG.UI package.
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

/// An untrusted animation clip accepted only through `AnimationClip.validate`.
type AnimationClip =
    { Id: string
      Duration: TimeSpan
      Tracks: (ClipProperty * ClipKeyframe list) list
      Cues: AnimationCue list
      Loop: ClipLoop }

/// A pure clip sample at an explicit elapsed time.
type AnimationClipSample =
    { LocalTime: TimeSpan
      Iteration: int
      Direction: ClipDirection
      Values: Map<ClipProperty, ClipValue>
      Cues: CueOccurrence list
      IsComplete: bool }

/// A presentation cue identified independently from its payload so live playback can deduplicate it.
type AnimationCue =
    { Id: string
      Time: TimeSpan
      Payload: string }

/// Controls event-cue delivery independently from visual sampling. Seek and pause never emit historical cues.
type ClipCueMode =
    | LiveAdvance of previousElapsed: TimeSpan
    | Seek
    | Paused

/// The direction of the current clip iteration.
type ClipDirection =
    | Forward
    | Reverse

/// A located reason an animation clip could not be accepted.
type ClipIssue =
    | EmptyClipId
    | InvalidDuration
    | EmptyTracks
    | DuplicateTrack of ClipProperty
    | EmptyCustomProperty
    | EmptyPathIdentity
    | MissingKeyframes of ClipProperty
    | InvalidKeyframeTime of ClipProperty * int
    | InvalidKeyframeValue of ClipProperty * int
    | MismatchedKeyframeValue of ClipProperty * int
    | IncompatiblePathTopology of ClipProperty
    | InvalidLoopIterations of int
    | EmptyCueId of int
    | DuplicateCueId of string
    | InvalidCueTime of string
    | UnorderedCues

/// One keyframe at an absolute clip-local time. `EasingToNext` shapes the following segment.
type ClipKeyframe =
    { Time: TimeSpan
      Value: ClipValue
      EasingToNext: Easing }

/// Bounded playback behavior. Iterations count complete forward or reverse passes and must be 1–10,000.
type ClipLoop =
    | Once
    | Repeat of iterations: int
    | PingPong of iterations: int

/// A property addressed by a portable animation clip. Named scalar and path targets let a product
/// bind its own presentation properties without extending the engine vocabulary.
type ClipProperty =
    | PositionX
    | PositionY
    | RotationDegrees
    | ScaleX
    | ScaleY
    | Opacity
    | Color
    | CustomScalar of string
    | PathMorph of string

/// A typed keyframe value. A track accepts one value shape determined by its `ClipProperty`.
type ClipValue =
    | ScalarValue of float
    | ColorValue of Color
    | PathValue of (float * float) list

/// One cue occurrence, including the loop iteration needed for stable deduplication.
type CueOccurrence =
    { Cue: AnimationCue
      Iteration: int
      Direction: ClipDirection }

/// A clip that passed structural, numeric, topology and playback-bound validation.
type ValidatedAnimationClip = private ValidatedAnimationClip of AnimationClip

/// Validation and deterministic sampling for portable clips. The module owns no clock, renderer or callback.
[<RequireQualifiedAccess>]
module AnimationClip =
    /// Sample visual values and, for `LiveAdvance`, cue occurrences in `(previousElapsed, elapsed]`.
    val sample: elapsed: TimeSpan -> cueMode: ClipCueMode -> clip: ValidatedAnimationClip -> AnimationClipSample
    /// Validate identities, duration, loop bounds, track types, keyframe ordering and path topology atomically.
    val validate: clip: AnimationClip -> Result<ValidatedAnimationClip, ClipIssue list>
    /// Recover the immutable source value of a validated clip.
    val value: clip: ValidatedAnimationClip -> AnimationClip
