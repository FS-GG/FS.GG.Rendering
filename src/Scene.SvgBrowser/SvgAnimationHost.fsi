namespace FS.GG.UI.Scene.SvgBrowser

open System
open FS.GG.UI.Scene

[<RequireQualifiedAccess>]
type SvgMotionPreference =
    | Full
    | Reduced

[<RequireQualifiedAccess>]
type SvgReducedMotionBehavior =
    | Settle
    | Substitute

/// Essential motion stays observable; decorative motion declares its reduced-motion fallback.
type SvgAnimationImportance =
    | Essential
    | Decorative of SvgReducedMotionBehavior

/// One presentation-only clip request bound to an accepted authority revision.
type SvgAnimationRequest =
    {
        Id: string
        AuthorityRevision: uint64
        Clip: ValidatedAnimationClip
        Importance: SvgAnimationImportance
    }

type SvgAnimationPolicyConfig = { MaxDecorativeEffects: int }

[<RequireQualifiedAccess>]
type SvgAnimationRefusal =
    | EmptyEffectId
    | DuplicateEffectId of string
    | StaleAuthorityRevision of expected: uint64 * actual: uint64
    | DecorativeLimitReached of limit: int
    | UnknownEffect of string
    | Disposed

[<RequireQualifiedAccess>]
type SvgAnimationStatus =
    | Running
    | Paused
    | Disposed

/// Accepted presentation state; elapsed clip time and presentation revision never enter authority state.
type SvgAnimationPolicyState

[<RequireQualifiedAccess>]
type SvgAnimationObservation =
    | Start of SvgAnimationRequest
    | Frame of elapsed: TimeSpan
    | Pause
    | Resume
    | Seek of effectId: string * elapsed: TimeSpan
    | ReplaceAuthority of revision: uint64
    | SetMotionPreference of SvgMotionPreference
    | Dispose

[<RequireQualifiedAccess>]
type SvgAnimationEffect =
    | ApplySample of
        effectId: string *
        authorityRevision: uint64 *
        presentationRevision: uint64 *
        sample: AnimationClipSample
    | CueBatch of effectId: string * authorityRevision: uint64 * cues: CueOccurrence list
    | EffectCompleted of effectId: string
    | EffectRefused of SvgAnimationRefusal
    | ScheduleFrame
    | CancelFrame
    | Disposed

[<RequireQualifiedAccess>]
module SvgAnimationPolicy =
    /// Create an empty running policy at the supplied authority revision.
    val initialize:
        config: SvgAnimationPolicyConfig ->
        authorityRevision: uint64 ->
        motionPreference: SvgMotionPreference ->
            SvgAnimationPolicyState

    /// Apply one observation atomically and return ordered host effects.
    val update:
        config: SvgAnimationPolicyConfig ->
        observation: SvgAnimationObservation ->
        state: SvgAnimationPolicyState ->
            SvgAnimationPolicyState * SvgAnimationEffect list

    /// Observe authority revision, presentation revision, lifecycle, motion preference and active count.
    val observe: state: SvgAnimationPolicyState -> uint64 * uint64 * SvgAnimationStatus * SvgMotionPreference * int

type SvgAnimationCallbacks =
    {
        ApplySample: string -> uint64 -> uint64 -> AnimationClipSample -> unit
        DispatchCues: string -> uint64 -> CueOccurrence list -> unit
        Refused: SvgAnimationRefusal -> unit
        Dispose: unit -> unit
    }

type SvgAnimationHostObservation =
    {
        AuthorityRevision: uint64
        PresentationRevision: uint64
        Status: SvgAnimationStatus
        MotionPreference: SvgMotionPreference
        ActiveEffectCount: int
        OwnedListenerCount: int
        ScheduledFrameCount: int
        IsDisposed: bool
    }

/// Disposable requestAnimationFrame host for retained SVG animation samples.
[<Sealed>]
type SvgAnimationHost =
    new:
        callbacks: SvgAnimationCallbacks * config: SvgAnimationPolicyConfig * authorityRevision: uint64 ->
            SvgAnimationHost

    member Start: request: SvgAnimationRequest -> unit
    member Pause: unit -> unit
    member Resume: unit -> unit
    member Seek: effectId: string * elapsed: TimeSpan -> unit
    member ReplaceAuthority: revision: uint64 -> unit
    member SetMotionPreference: preference: SvgMotionPreference -> unit
    member Observe: unit -> SvgAnimationHostObservation
    interface IDisposable

[<RequireQualifiedAccess>]
module SvgAnimationHost =
    val defaultConfig: SvgAnimationPolicyConfig
