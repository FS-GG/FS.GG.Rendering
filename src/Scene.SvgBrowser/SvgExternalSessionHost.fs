namespace FS.GG.UI.Scene.SvgBrowser

open System
open System.Collections.Generic
open Browser.Dom
open Browser.Types
open Fable.Core

[<RequireQualifiedAccess>]
type SvgExternalSessionStatus =
    | Disconnected
    | Connected
    | Disposed

[<RequireQualifiedAccess>]
type SvgExternalCompletionFailure =
    | Cancelled
    | Lost
    | CallbackFailed

[<RequireQualifiedAccess>]
type SvgExternalPresentationOutcome =
    | Applied of epoch: string * revision: uint64
    | Rejected of reason: string
    | Failed of SvgExternalCompletionFailure

type SvgExternalSessionState =
    {
        MountGeneration: uint64
        Epoch: string option
        Status: SvgExternalSessionStatus
        AcceptedRevision: uint64 option
        AcquisitionPending: bool
        PresentationQueued: bool
        LastOutcome: SvgExternalPresentationOutcome option
    }

[<RequireQualifiedAccess>]
type SvgExternalSessionObservation =
    | BindEpoch of epoch: string
    | Disconnect
    | InvalidatePresentation
    | DemandPresentation
    | CompletePresentation of mountGeneration: uint64 * epoch: string * revision: uint64
    | FailAcquisition of mountGeneration: uint64 * epoch: string * failure: SvgExternalCompletionFailure
    | PresentationCallbackFailed of epoch: string * revision: uint64
    | Dispose

[<RequireQualifiedAccess>]
type SvgExternalSessionEffect =
    | CancelAcquisition of mountGeneration: uint64
    | RequestPresentation of mountGeneration: uint64 * epoch: string
    | ApplyPresentation of epoch: string * revision: uint64
    | PresentationCoalesced of mountGeneration: uint64 * epoch: string
    | PresentationRejected of reason: string
    | AcquisitionFailed of SvgExternalCompletionFailure
    | EpochBound of mountGeneration: uint64 * epoch: string * preservedBaseline: bool
    | Disconnected
    | GenerationExhausted
    | Disposed

[<RequireQualifiedAccess>]
module SvgExternalSessionPolicy =
    let initialize () =
        {
            MountGeneration = 0UL
            Epoch = None
            Status = SvgExternalSessionStatus.Disconnected
            AcceptedRevision = None
            AcquisitionPending = false
            PresentationQueued = false
            LastOutcome = None
        }

    let private reject reason state =
        { state with
            LastOutcome = Some(SvgExternalPresentationOutcome.Rejected reason)
        },
        [ SvgExternalSessionEffect.PresentationRejected reason ]

    let private retire state =
        if state.MountGeneration = UInt64.MaxValue then
            None, [ SvgExternalSessionEffect.GenerationExhausted ]
        else
            let effects =
                if state.AcquisitionPending then
                    [ SvgExternalSessionEffect.CancelAcquisition state.MountGeneration ]
                else
                    []

            Some
                { state with
                    MountGeneration = state.MountGeneration + 1UL
                    AcquisitionPending = false
                    PresentationQueued = false
                },
            effects

    let private demand state =
        match state.Status, state.Epoch with
        | SvgExternalSessionStatus.Connected, Some epoch when state.AcquisitionPending ->
            { state with PresentationQueued = true },
            [ SvgExternalSessionEffect.PresentationCoalesced(state.MountGeneration, epoch) ]
        | SvgExternalSessionStatus.Connected, Some epoch ->
            { state with AcquisitionPending = true },
            [ SvgExternalSessionEffect.RequestPresentation(state.MountGeneration, epoch) ]
        | _ -> reject "not-connected" state

    let private drain state effects =
        if state.PresentationQueued then
            let cleared =
                { state with
                    AcquisitionPending = false
                    PresentationQueued = false
                }

            let requested, requestEffects = demand cleared
            requested, effects @ requestEffects
        else
            { state with
                AcquisitionPending = false
            },
            effects

    let update observation state =
        if state.Status = SvgExternalSessionStatus.Disposed then
            state, []
        else
            match observation with
            | SvgExternalSessionObservation.BindEpoch epoch when String.IsNullOrWhiteSpace epoch ->
                reject "invalid-epoch" state
            | SvgExternalSessionObservation.BindEpoch epoch ->
                match retire state with
                | None, effects -> state, effects
                | Some retired, effects ->
                    let preserved = retired.Epoch = Some epoch

                    { retired with
                        Epoch = Some epoch
                        Status = SvgExternalSessionStatus.Connected
                        AcceptedRevision = if preserved then retired.AcceptedRevision else None
                        LastOutcome = None
                    },
                    effects
                    @ [
                        SvgExternalSessionEffect.EpochBound(retired.MountGeneration, epoch, preserved)
                    ]
            | SvgExternalSessionObservation.Disconnect ->
                match retire state with
                | None, effects -> state, effects
                | Some retired, effects ->
                    { retired with
                        Status = SvgExternalSessionStatus.Disconnected
                    },
                    effects @ [ SvgExternalSessionEffect.Disconnected ]
            | SvgExternalSessionObservation.InvalidatePresentation ->
                match retire state with
                | None, effects -> state, effects
                | Some retired, effects -> retired, effects
            | SvgExternalSessionObservation.DemandPresentation -> demand state
            | SvgExternalSessionObservation.CompletePresentation(generation, epoch, revision) ->
                if generation <> state.MountGeneration then
                    reject "stale-generation" state
                elif state.Epoch <> Some epoch then
                    reject "stale-epoch" state
                elif not state.AcquisitionPending then
                    reject "unsolicited-completion" state
                else
                    match state.AcceptedRevision with
                    | Some accepted when revision <= accepted ->
                        let rejected =
                            { state with
                                LastOutcome = Some(SvgExternalPresentationOutcome.Rejected "non-increasing-revision")
                            }

                        drain rejected [ SvgExternalSessionEffect.PresentationRejected "non-increasing-revision" ]
                    | _ ->
                        let accepted =
                            { state with
                                AcceptedRevision = Some revision
                                LastOutcome = Some(SvgExternalPresentationOutcome.Applied(epoch, revision))
                            }

                        drain accepted [ SvgExternalSessionEffect.ApplyPresentation(epoch, revision) ]
            | SvgExternalSessionObservation.FailAcquisition(generation, epoch, failure) ->
                if
                    generation <> state.MountGeneration
                    || state.Epoch <> Some epoch
                    || not state.AcquisitionPending
                then
                    reject "stale-acquisition-failure" state
                else
                    let failed =
                        { state with
                            LastOutcome = Some(SvgExternalPresentationOutcome.Failed failure)
                        }

                    drain failed [ SvgExternalSessionEffect.AcquisitionFailed failure ]
            | SvgExternalSessionObservation.PresentationCallbackFailed(epoch, revision) ->
                if state.Epoch = Some epoch && state.AcceptedRevision = Some revision then
                    { state with
                        LastOutcome =
                            Some(SvgExternalPresentationOutcome.Failed SvgExternalCompletionFailure.CallbackFailed)
                    },
                    [
                        SvgExternalSessionEffect.AcquisitionFailed SvgExternalCompletionFailure.CallbackFailed
                    ]
                else
                    reject "stale-presentation-callback" state
            | SvgExternalSessionObservation.Dispose ->
                let effects =
                    if state.AcquisitionPending then
                        [ SvgExternalSessionEffect.CancelAcquisition state.MountGeneration ]
                    else
                        []

                { state with
                    Status = SvgExternalSessionStatus.Disposed
                    AcquisitionPending = false
                    PresentationQueued = false
                },
                effects @ [ SvgExternalSessionEffect.Disposed ]

type SvgExternalSessionCallbacks<'projection> =
    {
        RequestPresentation: uint64 -> string -> unit
        ApplyPresentation: string -> uint64 -> 'projection -> unit
        CancelAcquisition: uint64 -> unit
        EpochBound: uint64 -> string -> bool -> unit
        Disconnected: unit -> unit
        Dispose: unit -> unit
    }

type SvgExternalSessionHostObservation =
    {
        State: SvgExternalSessionState
        OwnedListenerCount: int
        OwnedRequestCount: int
        IsDisposed: bool
    }

module private ExternalSessionDom =
    [<Emit("document.hidden")>]
    let hidden () : bool = jsNative

[<Sealed>]
type SvgExternalSessionHost<'projection>(callbacks: SvgExternalSessionCallbacks<'projection>) =
    let listeners = ResizeArray<EventTarget * string * (Event -> unit)>()
    let mutable state = SvgExternalSessionPolicy.initialize ()
    let mutable pendingPayload: (uint64 * string * uint64 * 'projection) option = None

    let listen (target: EventTarget) name handler =
        target.addEventListener (name, handler)
        listeners.Add(target, name, handler)

    let rec apply observation =
        let next, effects = SvgExternalSessionPolicy.update observation state
        state <- next

        for effect in effects do
            match effect with
            | SvgExternalSessionEffect.RequestPresentation(generation, epoch) ->
                try
                    callbacks.RequestPresentation generation epoch
                with _ ->
                    apply (
                        SvgExternalSessionObservation.FailAcquisition(
                            generation,
                            epoch,
                            SvgExternalCompletionFailure.CallbackFailed
                        )
                    )
            | SvgExternalSessionEffect.ApplyPresentation(epoch, revision) ->
                match pendingPayload with
                | Some(generation, payloadEpoch, payloadRevision, projection) when
                    generation = state.MountGeneration
                    && payloadEpoch = epoch
                    && payloadRevision = revision
                    ->
                    pendingPayload <- None

                    try
                        callbacks.ApplyPresentation epoch revision projection
                    with _ ->
                        apply (SvgExternalSessionObservation.PresentationCallbackFailed(epoch, revision))
                | _ -> ()
            | SvgExternalSessionEffect.CancelAcquisition generation ->
                pendingPayload <- None
                callbacks.CancelAcquisition generation
            | SvgExternalSessionEffect.EpochBound(generation, epoch, preserved) ->
                callbacks.EpochBound generation epoch preserved
            | SvgExternalSessionEffect.Disconnected -> callbacks.Disconnected()
            | SvgExternalSessionEffect.Disposed -> callbacks.Dispose()
            | SvgExternalSessionEffect.PresentationCoalesced _
            | SvgExternalSessionEffect.PresentationRejected _
            | SvgExternalSessionEffect.AcquisitionFailed _
            | SvgExternalSessionEffect.GenerationExhausted -> ()

    let invalidate () =
        apply SvgExternalSessionObservation.InvalidatePresentation

    do
        listen (window :> EventTarget) "blur" (fun _ -> invalidate ())

        listen (document :> EventTarget) "visibilitychange" (fun _ ->
            if ExternalSessionDom.hidden () then
                invalidate ())

    member _.BindEpoch epoch =
        apply (SvgExternalSessionObservation.BindEpoch epoch)

    member _.Disconnect() =
        apply SvgExternalSessionObservation.Disconnect

    member _.InvalidatePresentation() = invalidate ()

    member _.DemandPresentation() =
        apply SvgExternalSessionObservation.DemandPresentation

    member _.CompletePresentation(mountGeneration, epoch, revision, projection) =
        pendingPayload <- Some(mountGeneration, epoch, revision, projection)
        apply (SvgExternalSessionObservation.CompletePresentation(mountGeneration, epoch, revision))

        match pendingPayload with
        | Some(g, e, r, _) when g = mountGeneration && e = epoch && r = revision -> pendingPayload <- None
        | _ -> ()

    member _.FailAcquisition(mountGeneration, epoch, failure) =
        apply (SvgExternalSessionObservation.FailAcquisition(mountGeneration, epoch, failure))

    member _.Observe() =
        {
            State = state
            OwnedListenerCount =
                if state.Status = SvgExternalSessionStatus.Disposed then
                    0
                else
                    listeners.Count
            OwnedRequestCount = if state.AcquisitionPending then 1 else 0
            IsDisposed = state.Status = SvgExternalSessionStatus.Disposed
        }

    interface IDisposable with
        member _.Dispose() =
            if state.Status <> SvgExternalSessionStatus.Disposed then
                apply SvgExternalSessionObservation.Dispose

                for target, name, handler in listeners do
                    target.removeEventListener (name, handler)

                listeners.Clear()
