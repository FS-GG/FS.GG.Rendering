// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.16.0 (src/Game.Core/SessionOperations.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Transport-neutral destination for a session operation.
[<RequireQualifiedAccess>]
type SessionOperationTarget =
    | LocalWorker
    | AuthoritativeServer

/// Generation-scoped identity assigned by the portable coordinator.
[<Struct>]
type SessionOperationId =
    {
        Generation: uint64
        Operation: uint64
    }

/// Bounded ownership policy for one operation generation.
type SessionOperationConfig =
    {
        Target: SessionOperationTarget
        MaxPendingRequired: uint32
    }

/// Lifecycle of the current generation.
[<RequireQualifiedAccess>]
type SessionOperationStatus =
    | Active
    | Cancelled
    | Failed of SessionFailure
    | Disposed

/// A required request. Its order is independent of transport completion order.
type SessionRequiredDispatch<'request> =
    {
        Id: SessionOperationId
        Order: uint64
        Target: SessionOperationTarget
        Payload: 'request
    }

/// A replaceable projection request. At most one is owned at a time.
type SessionProjectionDispatch =
    {
        Id: SessionOperationId
        Target: SessionOperationTarget
    }

/// Pure observations from a host or its local-worker/server interpreter.
[<RequireQualifiedAccess>]
type SessionOperationObservation<'request, 'record, 'projection> =
    | EnqueueRequired of 'request
    | DemandProjection
    | CompleteRequired of SessionOperationId * 'record
    | CompleteProjection of SessionOperationId * revision: uint64 * 'projection
    | Fail of SessionOperationId * SessionFailure
    | Replace
    | Cancel
    | Dispose

/// Precise refusals. A refused observation preserves the coordinator state.
[<RequireQualifiedAccess>]
type SessionOperationRefusal =
    | InvalidMaxPendingRequired of uint32
    | RequiredCapacityReached of uint32
    | StaleGeneration of expected: uint64 * actual: uint64
    | UnknownOrCompletedOperation of SessionOperationId
    | UnexpectedReplyKind of SessionOperationId
    | StaleProjectionRevision of accepted: uint64 * candidate: uint64
    | GenerationExhausted
    | OperationSequenceExhausted
    | CoordinatorInactive of SessionOperationStatus

/// Ordered instructions and commits emitted by the coordinator.
[<RequireQualifiedAccess>]
type SessionOperationEffect<'request, 'record, 'projection> =
    | DispatchRequired of SessionRequiredDispatch<'request>
    | DispatchProjection of SessionProjectionDispatch
    | RequiredCommitted of order: uint64 * 'record
    | ProjectionCommitted of revision: uint64 * 'projection
    | ProjectionDemandCoalesced of SessionOperationId
    | GenerationInvalidated of previous: uint64 * current: uint64 * SessionOperationStatus
    | Refused of SessionOperationRefusal

/// Portable generation, ordering and backpressure state. Payloads remain product-owned.
type SessionOperationState<'record> =
    {
        Config: SessionOperationConfig
        Generation: uint64
        Status: SessionOperationStatus
        NextOperation: uint64
        NextRequiredOrder: uint64
        NextRequiredCommit: uint64
        PendingRequired: Map<uint64, uint64>
        BufferedRequired: Map<uint64, 'record>
        PendingProjection: uint64 option
        ProjectionDemandQueued: bool
        LastProjectionRevision: uint64 option
    }

/// Pure coordinator shared by local-worker and authoritative-server interpreters.
[<RequireQualifiedAccess>]
module SessionOperations =
    /// Create generation zero after validating its bounded required-record capacity.
    val initialize: SessionOperationConfig -> Result<SessionOperationState<'record>, SessionOperationRefusal>

    /// Apply one host/interpreter observation and emit transport-neutral work or commits.
    val update:
        SessionOperationObservation<'request, 'record, 'projection> ->
        SessionOperationState<'record> ->
            SessionOperationState<'record> * SessionOperationEffect<'request, 'record, 'projection> list
