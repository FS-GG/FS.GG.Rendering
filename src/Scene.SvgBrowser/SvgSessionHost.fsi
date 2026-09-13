namespace FS.GG.UI.Scene.SvgBrowser

open System

/// <summary>Lifecycle of the browser-owned clock and projection generation.</summary>
[<RequireQualifiedAccess>]
type SvgSessionStatus =
    | Running
    | Paused
    | Recovering
    | Disposed

/// <summary>Browser boundary policy for suspension detection and elapsed conversion.</summary>
type SvgSessionPolicyConfig =
    { /// <summary>Elapsed host time at or above this value requests explicit recovery.</summary>
      SuspensionMilliseconds: float }

/// <summary>Portable ownership state shared by the .NET and Fable browser adapters.</summary>
type SvgSessionPolicyState =
    { Generation: uint64
      Status: SvgSessionStatus
      LastTimestampMilliseconds: float option
      ProjectionPending: bool
      ProjectionQueued: bool
      LastProjectionRevision: uint64 option }

/// <summary>Observations accepted by the portable browser-boundary reducer.</summary>
[<RequireQualifiedAccess>]
type SvgSessionPolicyObservation =
    | Frame of timestampMilliseconds: float
    | DemandProjection
    | CompleteProjection of generation: uint64 * revision: uint64
    | Pause
    | Resume
    | StepOnce
    | Reset
    | Replace
    | Suspend
    | Dispose

/// <summary>Instructions emitted for the Game runtime and browser resource interpreter.</summary>
[<RequireQualifiedAccess>]
type SvgSessionPolicyEffect =
    | AdvanceElapsed of microseconds: uint64
    | PauseAuthority
    | ResumeAuthority
    | StepAuthority
    | ResetAuthority
    | RequestRecovery of generation: uint64
    | RequestProjection of generation: uint64
    | ApplyProjection of revision: uint64
    | ProjectionDemandCoalesced of generation: uint64
    | ProjectionRejected of expectedGeneration: uint64 * actualGeneration: uint64 * revision: uint64
    | ProjectionRevisionRejected of acceptedRevision: uint64 * candidateRevision: uint64
    | CancelGeneration of generation: uint64
    | GenerationReplaced of generation: uint64
    | Disposed

/// <summary>Pure policy used by the browser host and correspondence tests.</summary>
[<RequireQualifiedAccess>]
module SvgSessionPolicy =
    /// <summary>Creates generation zero in the running state.</summary>
    val initialize: config: SvgSessionPolicyConfig -> SvgSessionPolicyState

    /// <summary>Applies one clock, lifecycle, or projection observation.</summary>
    val update:
        config: SvgSessionPolicyConfig ->
        observation: SvgSessionPolicyObservation ->
        state: SvgSessionPolicyState -> SvgSessionPolicyState * SvgSessionPolicyEffect list

/// <summary>Callbacks that interpret browser policy through the authoritative Game session.</summary>
type SvgSessionCallbacks<'projection> =
    { AdvanceElapsed: uint64 -> unit
      Pause: unit -> unit
      Resume: unit -> unit
      StepOnce: unit -> unit
      Reset: unit -> unit
      RequestRecovery: uint64 -> unit
      RequestProjection: uint64 -> unit
      ApplyProjection: uint64 -> 'projection -> unit
      CancelGeneration: uint64 -> unit
      Replace: uint64 -> unit
      Dispose: unit -> unit }

/// <summary>Resource counters and monotonic projection state for browser evidence.</summary>
type SvgSessionHostObservation =
    { Generation: uint64
      Status: SvgSessionStatus
      LastProjectionRevision: uint64 option
      OwnedListenerCount: int
      ScheduledFrameCount: int
      OwnedRequestCount: int
      IsDisposed: bool }

/// <summary>Disposable requestAnimationFrame interpreter for one Game session.</summary>
[<Sealed>]
type SvgSessionHost<'projection> =
    new: callbacks: SvgSessionCallbacks<'projection> * config: SvgSessionPolicyConfig -> SvgSessionHost<'projection>
    member Pause: unit -> unit
    member Resume: unit -> unit
    member StepOnce: unit -> unit
    member Reset: unit -> unit
    member Replace: unit -> unit
    member DemandProjection: unit -> unit
    member CompleteProjection: generation: uint64 * revision: uint64 * projection: 'projection -> unit
    member Observe: unit -> SvgSessionHostObservation
    interface IDisposable

/// <summary>Defaults for the SVG game-session browser adapter.</summary>
[<RequireQualifiedAccess>]
module SvgSessionHost =
    /// <summary>Suspension policy tuned to distinguish ordinary low cadence from a backgrounded tab.</summary>
    val defaultConfig: SvgSessionPolicyConfig
