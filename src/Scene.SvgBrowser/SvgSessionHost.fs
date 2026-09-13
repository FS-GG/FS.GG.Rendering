namespace FS.GG.UI.Scene.SvgBrowser

open System
open System.Collections.Generic
open Browser.Dom
open Browser.Types
open Fable.Core

[<RequireQualifiedAccess>]
type SvgSessionStatus = Running | Paused | Recovering | Disposed

type SvgSessionPolicyConfig = { SuspensionMilliseconds: float }

type SvgSessionPolicyState =
    { Generation: uint64
      Status: SvgSessionStatus
      LastTimestampMilliseconds: float option
      ProjectionPending: bool
      ProjectionQueued: bool
      LastProjectionRevision: uint64 option }

[<RequireQualifiedAccess>]
type SvgSessionPolicyObservation =
    | Frame of timestampMilliseconds: float
    | DemandProjection
    | CompleteProjection of generation: uint64 * revision: uint64
    | Pause | Resume | StepOnce | Reset | Replace | Suspend | Dispose

[<RequireQualifiedAccess>]
type SvgSessionPolicyEffect =
    | AdvanceElapsed of microseconds: uint64
    | PauseAuthority | ResumeAuthority | StepAuthority | ResetAuthority
    | RequestRecovery of generation: uint64
    | RequestProjection of generation: uint64
    | ApplyProjection of revision: uint64
    | ProjectionDemandCoalesced of generation: uint64
    | ProjectionRejected of expectedGeneration: uint64 * actualGeneration: uint64 * revision: uint64
    | ProjectionRevisionRejected of acceptedRevision: uint64 * candidateRevision: uint64
    | CancelGeneration of generation: uint64
    | GenerationReplaced of generation: uint64
    | Disposed

[<RequireQualifiedAccess>]
module SvgSessionPolicy =
    let private validate config =
        if Double.IsNaN config.SuspensionMilliseconds
           || Double.IsInfinity config.SuspensionMilliseconds
           || config.SuspensionMilliseconds <= 0.0 then
            invalidArg (nameof config) "suspension threshold must be finite and positive"

    let initialize config =
        validate config
        { Generation = 0UL
          Status = SvgSessionStatus.Running
          LastTimestampMilliseconds = None
          ProjectionPending = false
          ProjectionQueued = false
          LastProjectionRevision = None }

    let private demand state =
        if state.ProjectionPending then
            { state with ProjectionQueued = true },
            [ SvgSessionPolicyEffect.ProjectionDemandCoalesced state.Generation ]
        else
            { state with ProjectionPending = true },
            [ SvgSessionPolicyEffect.RequestProjection state.Generation ]

    let private invalidate nextStatus recovery state =
        if state.Generation = UInt64.MaxValue then invalidOp "SVG session generation space is exhausted."
        let previous = state.Generation
        let next =
            { state with
                Generation = previous + 1UL
                Status = nextStatus
                LastTimestampMilliseconds = None
                ProjectionPending = false
                ProjectionQueued = false }
        let effects = [ SvgSessionPolicyEffect.CancelGeneration previous ]
        next, if recovery then effects @ [ SvgSessionPolicyEffect.PauseAuthority; SvgSessionPolicyEffect.RequestRecovery next.Generation ] else effects

    let update config observation state =
        validate config
        if state.Status = SvgSessionStatus.Disposed then state, []
        else
            match observation with
            | SvgSessionPolicyObservation.Frame timestamp when state.Status <> SvgSessionStatus.Running -> state, []
            | SvgSessionPolicyObservation.Frame timestamp when Double.IsNaN timestamp || Double.IsInfinity timestamp -> state, []
            | SvgSessionPolicyObservation.Frame timestamp ->
                match state.LastTimestampMilliseconds with
                | None -> { state with LastTimestampMilliseconds = Some timestamp }, []
                | Some previous when timestamp <= previous -> state, []
                | Some previous ->
                    let elapsed = timestamp - previous
                    if elapsed >= config.SuspensionMilliseconds then invalidate SvgSessionStatus.Recovering true state
                    else
                        let microseconds = uint64 (Math.Round(elapsed * 1000.0, MidpointRounding.AwayFromZero))
                        let timed = { state with LastTimestampMilliseconds = Some timestamp }
                        let requested, requestEffects = demand timed
                        requested, SvgSessionPolicyEffect.AdvanceElapsed microseconds :: requestEffects
            | SvgSessionPolicyObservation.DemandProjection -> demand state
            | SvgSessionPolicyObservation.CompleteProjection(generation, revision) when generation <> state.Generation ->
                state, [ SvgSessionPolicyEffect.ProjectionRejected(state.Generation, generation, revision) ]
            | SvgSessionPolicyObservation.CompleteProjection(_, revision) ->
                match state.LastProjectionRevision with
                | Some accepted when revision <= accepted ->
                    state, [ SvgSessionPolicyEffect.ProjectionRevisionRejected(accepted, revision) ]
                | _ when not state.ProjectionPending ->
                    state, [ SvgSessionPolicyEffect.ProjectionRejected(state.Generation, state.Generation, revision) ]
                | _ ->
                    let accepted =
                        { state with
                            ProjectionPending = state.ProjectionQueued
                            ProjectionQueued = false
                            LastProjectionRevision = Some revision }
                    let effects = [ SvgSessionPolicyEffect.ApplyProjection revision ]
                    if accepted.ProjectionPending then accepted, effects @ [ SvgSessionPolicyEffect.RequestProjection accepted.Generation ]
                    else accepted, effects
            | SvgSessionPolicyObservation.Pause when state.Status = SvgSessionStatus.Running ->
                let next, effects = invalidate SvgSessionStatus.Paused false state
                next, effects @ [ SvgSessionPolicyEffect.PauseAuthority ]
            | SvgSessionPolicyObservation.Pause -> state, []
            | SvgSessionPolicyObservation.Resume when state.Status <> SvgSessionStatus.Running ->
                { state with Status = SvgSessionStatus.Running; LastTimestampMilliseconds = None }, [ SvgSessionPolicyEffect.ResumeAuthority ]
            | SvgSessionPolicyObservation.Resume -> state, []
            | SvgSessionPolicyObservation.StepOnce when state.Status = SvgSessionStatus.Paused ->
                let next, effects = demand state
                next, SvgSessionPolicyEffect.StepAuthority :: effects
            | SvgSessionPolicyObservation.StepOnce -> state, []
            | SvgSessionPolicyObservation.Reset ->
                let next, effects = invalidate state.Status false state
                let requested, requestEffects = demand next
                requested, effects @ [ SvgSessionPolicyEffect.ResetAuthority ] @ requestEffects
            | SvgSessionPolicyObservation.Replace ->
                let next, effects = invalidate SvgSessionStatus.Running false state
                next, effects @ [ SvgSessionPolicyEffect.GenerationReplaced next.Generation ]
            | SvgSessionPolicyObservation.Suspend when state.Status = SvgSessionStatus.Running ->
                invalidate SvgSessionStatus.Recovering true state
            | SvgSessionPolicyObservation.Suspend -> state, []
            | SvgSessionPolicyObservation.Dispose ->
                { state with Status = SvgSessionStatus.Disposed; LastTimestampMilliseconds = None; ProjectionPending = false; ProjectionQueued = false },
                [ SvgSessionPolicyEffect.CancelGeneration state.Generation; SvgSessionPolicyEffect.Disposed ]

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

type SvgSessionHostObservation =
    { Generation: uint64
      Status: SvgSessionStatus
      LastProjectionRevision: uint64 option
      OwnedListenerCount: int
      ScheduledFrameCount: int
      OwnedRequestCount: int
      IsDisposed: bool }

module private SessionDom =
    [<Emit("requestAnimationFrame($0)")>]
    let requestFrame (_callback: float -> unit) : float = jsNative
    [<Emit("cancelAnimationFrame($0)")>]
    let cancelFrame (_token: float) : unit = jsNative
    [<Emit("document.hidden")>]
    let hidden () : bool = jsNative

[<Sealed>]
type SvgSessionHost<'projection>(callbacks: SvgSessionCallbacks<'projection>, config: SvgSessionPolicyConfig) =
    let listeners = ResizeArray<EventTarget * string * (Event -> unit)>()
    let mutable state = SvgSessionPolicy.initialize config
    let mutable frame: float option = None
    let mutable pendingPayload: (uint64 * uint64 * 'projection) option = None

    let listen (target: EventTarget) (name: string) (handler: Event -> unit) =
        target.addEventListener(name, handler)
        listeners.Add(target, name, handler)

    let interpret effects =
        for effect in effects do
            match effect with
            | SvgSessionPolicyEffect.AdvanceElapsed value -> callbacks.AdvanceElapsed value
            | SvgSessionPolicyEffect.PauseAuthority -> callbacks.Pause()
            | SvgSessionPolicyEffect.ResumeAuthority -> callbacks.Resume()
            | SvgSessionPolicyEffect.StepAuthority -> callbacks.StepOnce()
            | SvgSessionPolicyEffect.ResetAuthority -> callbacks.Reset()
            | SvgSessionPolicyEffect.RequestRecovery generation -> callbacks.RequestRecovery generation
            | SvgSessionPolicyEffect.RequestProjection generation -> callbacks.RequestProjection generation
            | SvgSessionPolicyEffect.ApplyProjection revision ->
                match pendingPayload with
                | Some(generation, candidate, projection) when generation = state.Generation && candidate = revision ->
                    pendingPayload <- None
                    callbacks.ApplyProjection revision projection
                | _ -> ()
            | SvgSessionPolicyEffect.CancelGeneration generation -> pendingPayload <- None; callbacks.CancelGeneration generation
            | SvgSessionPolicyEffect.GenerationReplaced generation -> callbacks.Replace generation
            | SvgSessionPolicyEffect.Disposed -> callbacks.Dispose()
            | SvgSessionPolicyEffect.ProjectionDemandCoalesced _
            | SvgSessionPolicyEffect.ProjectionRejected _
            | SvgSessionPolicyEffect.ProjectionRevisionRejected _ -> ()

    let apply observation =
        let next, effects = SvgSessionPolicy.update config observation state
        state <- next
        interpret effects

    let cancelScheduled () =
        frame |> Option.iter SessionDom.cancelFrame
        frame <- None

    let rec schedule () =
        if state.Status = SvgSessionStatus.Running && frame.IsNone then
            frame <- Some(SessionDom.requestFrame (fun timestamp ->
                frame <- None
                apply (SvgSessionPolicyObservation.Frame timestamp)
                schedule ()))

    let suspend () =
        cancelScheduled ()
        apply SvgSessionPolicyObservation.Suspend

    do
        listen (window :> EventTarget) "blur" (fun _ -> suspend ())
        listen (document :> EventTarget) "visibilitychange" (fun _ -> if SessionDom.hidden() then suspend ())
        schedule ()

    member _.Pause() = cancelScheduled (); apply SvgSessionPolicyObservation.Pause
    member _.Resume() = apply SvgSessionPolicyObservation.Resume; schedule ()
    member _.StepOnce() = apply SvgSessionPolicyObservation.StepOnce
    member _.Reset() = apply SvgSessionPolicyObservation.Reset
    member _.Replace() = cancelScheduled (); apply SvgSessionPolicyObservation.Replace; schedule ()
    member _.DemandProjection() = apply SvgSessionPolicyObservation.DemandProjection
    member _.CompleteProjection(generation, revision, projection) =
        pendingPayload <- Some(generation, revision, projection)
        apply (SvgSessionPolicyObservation.CompleteProjection(generation, revision))
        match pendingPayload with
        | Some(g, r, _) when g = generation && r = revision -> pendingPayload <- None
        | _ -> ()
    member _.Observe() =
        { Generation = state.Generation
          Status = state.Status
          LastProjectionRevision = state.LastProjectionRevision
          OwnedListenerCount = if state.Status = SvgSessionStatus.Disposed then 0 else listeners.Count
          ScheduledFrameCount = if frame.IsSome then 1 else 0
          OwnedRequestCount = if state.ProjectionPending then 1 else 0
          IsDisposed = state.Status = SvgSessionStatus.Disposed }
    interface IDisposable with
        member _.Dispose() =
            if state.Status <> SvgSessionStatus.Disposed then
                cancelScheduled ()
                apply SvgSessionPolicyObservation.Dispose
                for target, name, handler in listeners do target.removeEventListener(name, handler)
                listeners.Clear()

[<RequireQualifiedAccess>]
module SvgSessionHost =
    let defaultConfig = { SuspensionMilliseconds = 1000.0 }
