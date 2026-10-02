namespace FS.GG.UI.Scene.SvgBrowser

open System

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
    val initialize: unit -> SvgExternalSessionState

    val update:
        observation: SvgExternalSessionObservation ->
        state: SvgExternalSessionState ->
            SvgExternalSessionState * SvgExternalSessionEffect list

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

/// Presentation-only host for state produced by an external authority.
[<Sealed>]
type SvgExternalSessionHost<'projection> =
    new: callbacks: SvgExternalSessionCallbacks<'projection> -> SvgExternalSessionHost<'projection>
    member BindEpoch: epoch: string -> unit
    member Disconnect: unit -> unit
    member InvalidatePresentation: unit -> unit
    member DemandPresentation: unit -> unit

    member CompletePresentation:
        mountGeneration: uint64 * acquisitionId: uint64 * epoch: string * revision: uint64 * projection: 'projection ->
            unit

    member FailAcquisition:
        mountGeneration: uint64 * acquisitionId: uint64 * epoch: string * failure: SvgExternalCompletionFailure -> unit

    member Observe: unit -> SvgExternalSessionHostObservation
    interface IDisposable
