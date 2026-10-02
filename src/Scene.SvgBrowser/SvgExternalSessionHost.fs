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
    | Applied of acquisitionId: uint64 * epoch: string * revision: uint64
    | Rejected of reason: string
    | Failed of failure: SvgExternalCompletionFailure * acquisitionId: uint64 option

type SvgExternalSessionState =
    {
        MountGeneration: uint64
        Epoch: string option
        Status: SvgExternalSessionStatus
        AcceptedRevision: uint64 option
        NextAcquisitionId: uint64
        PendingAcquisitionId: uint64 option
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
    | CompletePresentation of mountGeneration: uint64 * acquisitionId: uint64 * epoch: string * revision: uint64
    | FailAcquisition of
        mountGeneration: uint64 *
        acquisitionId: uint64 *
        epoch: string *
        failure: SvgExternalCompletionFailure
    | PresentationCallbackFailed of acquisitionId: uint64 * epoch: string * revision: uint64
    | Dispose

[<RequireQualifiedAccess>]
type SvgExternalSessionEffect =
    | CancelAcquisition of mountGeneration: uint64 * acquisitionId: uint64 * epoch: string option
    | RequestPresentation of mountGeneration: uint64 * acquisitionId: uint64 * epoch: string
    | ApplyPresentation of mountGeneration: uint64 * acquisitionId: uint64 * epoch: string * revision: uint64
    | PresentationCoalesced of mountGeneration: uint64 * acquisitionId: uint64 * epoch: string
    | PresentationRejected of
        reason: string *
        mountGeneration: uint64 option *
        acquisitionId: uint64 option *
        epoch: string option *
        revision: uint64 option
    | AcquisitionFailed of
        failure: SvgExternalCompletionFailure *
        mountGeneration: uint64 *
        acquisitionId: uint64 *
        epoch: string *
        revision: uint64 option
    | EpochBound of mountGeneration: uint64 * epoch: string * preservedBaseline: bool
    | Disconnected of mountGeneration: uint64
    | GenerationExhausted of mountGeneration: uint64
    | AcquisitionIdExhausted of nextAcquisitionId: uint64
    | Disposed of mountGeneration: uint64

[<RequireQualifiedAccess>]
module SvgExternalSessionPolicy =
    let private optionText formatter =
        function
        | Some value -> $"some:{formatter value}"
        | None -> "none"

    let initialize () =
        {
            MountGeneration = 0UL
            Epoch = None
            Status = SvgExternalSessionStatus.Disconnected
            AcceptedRevision = None
            NextAcquisitionId = 0UL
            PendingAcquisitionId = None
            AcquisitionPending = false
            PresentationQueued = false
            LastOutcome = None
        }

    let private reject reason generation acquisitionId epoch revision state =
        { state with
            LastOutcome = Some(SvgExternalPresentationOutcome.Rejected reason)
        },
        [
            SvgExternalSessionEffect.PresentationRejected(reason, generation, acquisitionId, epoch, revision)
        ]

    let private retire state =
        if state.MountGeneration = UInt64.MaxValue then
            None, [ SvgExternalSessionEffect.GenerationExhausted state.MountGeneration ]
        else
            let effects =
                match state.PendingAcquisitionId with
                | Some acquisitionId ->
                    [
                        SvgExternalSessionEffect.CancelAcquisition(state.MountGeneration, acquisitionId, state.Epoch)
                    ]
                | None -> []

            Some
                { state with
                    MountGeneration = state.MountGeneration + 1UL
                    PendingAcquisitionId = None
                    AcquisitionPending = false
                    PresentationQueued = false
                },
            effects

    let private demand state =
        match state.Status, state.Epoch, state.PendingAcquisitionId with
        | SvgExternalSessionStatus.Connected, Some epoch, Some acquisitionId ->
            { state with PresentationQueued = true },
            [
                SvgExternalSessionEffect.PresentationCoalesced(state.MountGeneration, acquisitionId, epoch)
            ]
        | SvgExternalSessionStatus.Connected, Some _, None when state.NextAcquisitionId = UInt64.MaxValue ->
            state, [ SvgExternalSessionEffect.AcquisitionIdExhausted state.NextAcquisitionId ]
        | SvgExternalSessionStatus.Connected, Some epoch, None ->
            let acquisitionId = state.NextAcquisitionId

            { state with
                NextAcquisitionId = acquisitionId + 1UL
                PendingAcquisitionId = Some acquisitionId
                AcquisitionPending = true
            },
            [
                SvgExternalSessionEffect.RequestPresentation(state.MountGeneration, acquisitionId, epoch)
            ]
        | _ -> reject "not-connected" None None None None state

    let private drain state effects =
        let cleared =
            { state with
                PendingAcquisitionId = None
                AcquisitionPending = false
                PresentationQueued = false
            }

        if state.PresentationQueued then
            let requested, requestEffects = demand cleared
            requested, effects @ requestEffects
        else
            cleared, effects

    let update observation state =
        if state.Status = SvgExternalSessionStatus.Disposed then
            state, []
        else
            match observation with
            | SvgExternalSessionObservation.BindEpoch epoch when String.IsNullOrWhiteSpace epoch ->
                reject "invalid-epoch" None None (Some epoch) None state
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
                    effects @ [ SvgExternalSessionEffect.Disconnected retired.MountGeneration ]
            | SvgExternalSessionObservation.InvalidatePresentation ->
                match retire state with
                | None, effects -> state, effects
                | Some retired, effects -> retired, effects
            | SvgExternalSessionObservation.DemandPresentation -> demand state
            | SvgExternalSessionObservation.CompletePresentation(generation, acquisitionId, epoch, revision) ->
                if generation <> state.MountGeneration then
                    reject
                        $"stale-generation:{state.MountGeneration}:{generation}"
                        (Some generation)
                        (Some acquisitionId)
                        (Some epoch)
                        (Some revision)
                        state
                elif state.Epoch <> Some epoch then
                    reject
                        $"stale-epoch:{optionText id state.Epoch}:{epoch}"
                        (Some generation)
                        (Some acquisitionId)
                        (Some epoch)
                        (Some revision)
                        state
                elif state.PendingAcquisitionId <> Some acquisitionId then
                    reject
                        $"stale-acquisition:{optionText string state.PendingAcquisitionId}:{acquisitionId}"
                        (Some generation)
                        (Some acquisitionId)
                        (Some epoch)
                        (Some revision)
                        state
                elif not state.AcquisitionPending then
                    reject
                        $"unsolicited-completion:{acquisitionId}"
                        (Some generation)
                        (Some acquisitionId)
                        (Some epoch)
                        (Some revision)
                        state
                else
                    match state.AcceptedRevision with
                    | Some accepted when revision <= accepted ->
                        let rejected =
                            { state with
                                LastOutcome =
                                    Some(
                                        SvgExternalPresentationOutcome.Rejected(
                                            $"non-increasing-revision:{accepted}:{revision}"
                                        )
                                    )
                            }

                        drain
                            rejected
                            [
                                SvgExternalSessionEffect.PresentationRejected(
                                    $"non-increasing-revision:{accepted}:{revision}",
                                    Some generation,
                                    Some acquisitionId,
                                    Some epoch,
                                    Some revision
                                )
                            ]
                    | _ ->
                        let accepted =
                            { state with
                                AcceptedRevision = Some revision
                                LastOutcome =
                                    Some(SvgExternalPresentationOutcome.Applied(acquisitionId, epoch, revision))
                            }

                        drain
                            accepted
                            [
                                SvgExternalSessionEffect.ApplyPresentation(generation, acquisitionId, epoch, revision)
                            ]
            | SvgExternalSessionObservation.FailAcquisition(generation, acquisitionId, epoch, failure) ->
                if generation <> state.MountGeneration then
                    reject
                        $"stale-generation:{state.MountGeneration}:{generation}"
                        (Some generation)
                        (Some acquisitionId)
                        (Some epoch)
                        None
                        state
                elif state.Epoch <> Some epoch then
                    reject
                        $"stale-epoch:{optionText id state.Epoch}:{epoch}"
                        (Some generation)
                        (Some acquisitionId)
                        (Some epoch)
                        None
                        state
                elif state.PendingAcquisitionId <> Some acquisitionId then
                    reject
                        $"stale-acquisition:{optionText string state.PendingAcquisitionId}:{acquisitionId}"
                        (Some generation)
                        (Some acquisitionId)
                        (Some epoch)
                        None
                        state
                elif not state.AcquisitionPending then
                    reject
                        $"unsolicited-failure:{acquisitionId}"
                        (Some generation)
                        (Some acquisitionId)
                        (Some epoch)
                        None
                        state
                else
                    let failed =
                        { state with
                            LastOutcome = Some(SvgExternalPresentationOutcome.Failed(failure, Some acquisitionId))
                        }

                    drain
                        failed
                        [
                            SvgExternalSessionEffect.AcquisitionFailed(failure, generation, acquisitionId, epoch, None)
                        ]
            | SvgExternalSessionObservation.PresentationCallbackFailed(acquisitionId, epoch, revision) ->
                if
                    state.Epoch = Some epoch
                    && state.AcceptedRevision = Some revision
                    && state.LastOutcome = Some(SvgExternalPresentationOutcome.Applied(acquisitionId, epoch, revision))
                then
                    { state with
                        LastOutcome =
                            Some(
                                SvgExternalPresentationOutcome.Failed(
                                    SvgExternalCompletionFailure.CallbackFailed,
                                    Some acquisitionId
                                )
                            )
                    },
                    [
                        SvgExternalSessionEffect.AcquisitionFailed(
                            SvgExternalCompletionFailure.CallbackFailed,
                            state.MountGeneration,
                            acquisitionId,
                            epoch,
                            Some revision
                        )
                    ]
                else
                    reject
                        $"stale-presentation-callback:{acquisitionId}:{epoch}:{revision}"
                        (Some state.MountGeneration)
                        (Some acquisitionId)
                        (Some epoch)
                        (Some revision)
                        state
            | SvgExternalSessionObservation.Dispose ->
                let effects =
                    match state.PendingAcquisitionId with
                    | Some acquisitionId ->
                        [
                            SvgExternalSessionEffect.CancelAcquisition(
                                state.MountGeneration,
                                acquisitionId,
                                state.Epoch
                            )
                        ]
                    | None -> []

                { state with
                    Status = SvgExternalSessionStatus.Disposed
                    PendingAcquisitionId = None
                    AcquisitionPending = false
                    PresentationQueued = false
                },
                effects @ [ SvgExternalSessionEffect.Disposed state.MountGeneration ]

type SvgExternalSessionCallbacks<'projection> =
    {
        RequestPresentation: uint64 -> uint64 -> string -> unit
        ApplyPresentation: string -> uint64 -> 'projection -> unit
        CancelAcquisition: uint64 -> uint64 -> unit
        EpochBound: uint64 -> string -> bool -> unit
        Disconnected: unit -> unit
        Dispose: unit -> unit
    }

type SvgExternalSessionHostObservation =
    {
        State: SvgExternalSessionState
        OwnedListenerCount: int
        OwnedRequestCount: int
        CallbackFailureObserved: bool
        CancellationSettlementUnknown: bool
        IsDisposed: bool
    }

module private ExternalSessionDom =
    [<Emit("document.hidden")>]
    let hidden () : bool = jsNative

[<Sealed>]
type SvgExternalSessionHost<'projection>(callbacks: SvgExternalSessionCallbacks<'projection>) =
    let listeners = ResizeArray<EventTarget * string * (Event -> unit)>()
    let mutable state = SvgExternalSessionPolicy.initialize ()

    let mutable pendingPayload: (uint64 * uint64 * string * uint64 * 'projection) option =
        None

    let mutable callbackFailureObserved = false
    let mutable cancellationSettlementUnknown = false

    let listen (target: EventTarget) name handler =
        target.addEventListener (name, handler)
        listeners.Add(target, name, handler)

    let callback action =
        try
            action ()
            true
        with _ ->
            callbackFailureObserved <- true
            false

    let rec apply observation =
        let next, effects = SvgExternalSessionPolicy.update observation state
        state <- next

        for effect in effects do
            match effect with
            | SvgExternalSessionEffect.RequestPresentation(generation, acquisitionId, epoch) ->
                if
                    state.Status = SvgExternalSessionStatus.Connected
                    && state.MountGeneration = generation
                    && state.Epoch = Some epoch
                    && state.PendingAcquisitionId = Some acquisitionId
                then
                    let succeeded =
                        callback (fun () -> callbacks.RequestPresentation generation acquisitionId epoch)

                    if not succeeded then
                        apply (
                            SvgExternalSessionObservation.FailAcquisition(
                                generation,
                                acquisitionId,
                                epoch,
                                SvgExternalCompletionFailure.CallbackFailed
                            )
                        )
            | SvgExternalSessionEffect.ApplyPresentation(generation, acquisitionId, epoch, revision) ->
                if
                    state.Status <> SvgExternalSessionStatus.Disposed
                    && state.MountGeneration = generation
                    && state.Epoch = Some epoch
                    && state.LastOutcome = Some(SvgExternalPresentationOutcome.Applied(acquisitionId, epoch, revision))
                then
                    match pendingPayload with
                    | Some(payloadGeneration, payloadAcquisitionId, payloadEpoch, payloadRevision, projection) when
                        payloadGeneration = generation
                        && payloadAcquisitionId = acquisitionId
                        && payloadEpoch = epoch
                        && payloadRevision = revision
                        ->
                        pendingPayload <- None

                        if not (callback (fun () -> callbacks.ApplyPresentation epoch revision projection)) then
                            apply (
                                SvgExternalSessionObservation.PresentationCallbackFailed(acquisitionId, epoch, revision)
                            )
                    | _ -> ()
            | SvgExternalSessionEffect.CancelAcquisition(generation, acquisitionId, _) ->
                match pendingPayload with
                | Some(payloadGeneration, payloadAcquisitionId, _, _, _) when
                    payloadGeneration = generation && payloadAcquisitionId = acquisitionId
                    ->
                    pendingPayload <- None
                | _ -> ()

                if not (callback (fun () -> callbacks.CancelAcquisition generation acquisitionId)) then
                    cancellationSettlementUnknown <- true
            | SvgExternalSessionEffect.EpochBound(generation, epoch, preserved) ->
                if
                    state.Status = SvgExternalSessionStatus.Connected
                    && state.MountGeneration = generation
                    && state.Epoch = Some epoch
                then
                    callback (fun () -> callbacks.EpochBound generation epoch preserved) |> ignore
            | SvgExternalSessionEffect.Disconnected generation ->
                if
                    state.Status = SvgExternalSessionStatus.Disconnected
                    && state.MountGeneration = generation
                then
                    callback callbacks.Disconnected |> ignore
            | SvgExternalSessionEffect.Disposed generation ->
                if
                    state.Status = SvgExternalSessionStatus.Disposed
                    && state.MountGeneration = generation
                then
                    callback callbacks.Dispose |> ignore
            | SvgExternalSessionEffect.PresentationCoalesced _
            | SvgExternalSessionEffect.PresentationRejected _
            | SvgExternalSessionEffect.AcquisitionFailed _
            | SvgExternalSessionEffect.GenerationExhausted _
            | SvgExternalSessionEffect.AcquisitionIdExhausted _ -> ()

    let removeOwnedListeners () =
        // Failed removals remain in the owned-resource census and are retried by
        // a repeated Dispose instead of being reported as settled.
        for index in listeners.Count - 1 .. -1 .. 0 do
            let target, name, handler = listeners[index]

            try
                target.removeEventListener (name, handler)
                listeners.RemoveAt index
            with _ ->
                callbackFailureObserved <- true

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

    member _.CompletePresentation(mountGeneration, acquisitionId, epoch, revision, projection) =
        pendingPayload <- Some(mountGeneration, acquisitionId, epoch, revision, projection)

        apply (SvgExternalSessionObservation.CompletePresentation(mountGeneration, acquisitionId, epoch, revision))

        match pendingPayload with
        | Some(g, a, e, r, _) when g = mountGeneration && a = acquisitionId && e = epoch && r = revision ->
            pendingPayload <- None
        | _ -> ()

    member _.FailAcquisition(mountGeneration, acquisitionId, epoch, failure) =
        apply (SvgExternalSessionObservation.FailAcquisition(mountGeneration, acquisitionId, epoch, failure))

    member _.Observe() =
        {
            State = state
            OwnedListenerCount = listeners.Count
            OwnedRequestCount = if state.PendingAcquisitionId.IsSome then 1 else 0
            CallbackFailureObserved = callbackFailureObserved
            CancellationSettlementUnknown = cancellationSettlementUnknown
            IsDisposed = state.Status = SvgExternalSessionStatus.Disposed
        }

    interface IDisposable with
        member _.Dispose() =
            if state.Status <> SvgExternalSessionStatus.Disposed then
                try
                    apply SvgExternalSessionObservation.Dispose
                finally
                    removeOwnedListeners ()
            else
                removeOwnedListeners ()
