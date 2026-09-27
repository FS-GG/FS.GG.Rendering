// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.16.0 (src/Game.Core/SessionRuntime.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Lifecycle state of a portable deterministic session runtime.
[<RequireQualifiedAccess>]
type SessionRuntimeStatus =
    | Running
    | Paused
    | Disposed

/// Fixed-step and bounded catch-up policy. Time is supplied as integer microseconds by the host.
type SessionRuntimeConfig =
    {
        StepMicroseconds: uint64
        MaxCatchUpSteps: uint32
    }

/// A precise refusal which leaves the previously accepted runtime state unchanged.
[<RequireQualifiedAccess>]
type SessionRuntimeRefusal =
    | InvalidStepMicroseconds of uint64
    | InvalidMaxCatchUpSteps of uint32
    | InvalidEnvelope of SessionContractIssue list
    | WrongSession of expected: string * actual: string
    | StaleInputSequence of accepted: uint64 * candidate: uint64
    | IncompatibleSnapshot of SessionCompatibilityIssue list
    | ContractFailure of SessionFailure
    | RuntimeDisposed

/// Pure owner observations. Browser clocks, workers and transports remain effect interpreters.
[<RequireQualifiedAccess>]
type SessionRuntimeObservation<'input, 'snapshot> =
    | AdmitInput of SessionInput<'input>
    | AdvanceElapsed of microseconds: uint64
    | Pause
    | Resume
    | StepOnce
    | Reset
    | Restore of SessionSnapshot<'snapshot>
    | Dispose

/// Ordered results emitted by an accepted or refused observation.
[<RequireQualifiedAccess>]
type SessionRuntimeEffect<'projection> =
    | InputAccepted of inputId: string * sequence: uint64
    | Advanced of stepCount: uint64
    | CatchUpClamped of droppedMicroseconds: uint64
    | StatusChanged of SessionRuntimeStatus
    | ProjectionReady of SessionProjection<'projection>
    | Refused of SessionRuntimeRefusal
    | Disposed

/// Runtime-owned state around one product contract. Product state is never inspected or copied.
type SessionRuntimeState<'state, 'snapshot> =
    {
        SessionId: string
        Compatibility: SessionCompatibility
        Config: SessionRuntimeConfig
        Status: SessionRuntimeStatus
        Current: 'state
        InitialSnapshot: SessionSnapshot<'snapshot>
        AccumulatorMicroseconds: uint64
        LastInputSequence: uint64 option
    }

/// Pure fixed-step session execution over a product-supplied <c>SessionContract</c>.
[<RequireQualifiedAccess>]
module SessionRuntime =
    /// Validate the bounded clock policy and initialize one session plus its reset snapshot.
    val initialize:
        config: SessionRuntimeConfig ->
        contract: SessionContract<'configuration, 'state, 'input, 'projection, 'snapshot> ->
        request: SessionInitialization<'configuration> ->
            Result<SessionRuntimeState<'state, 'snapshot>, SessionRuntimeRefusal>

    /// Apply one observation. Every refusal preserves the previously accepted state byte-for-byte.
    val update:
        contract: SessionContract<'configuration, 'state, 'input, 'projection, 'snapshot> ->
        observation: SessionRuntimeObservation<'input, 'snapshot> ->
        state: SessionRuntimeState<'state, 'snapshot> ->
            SessionRuntimeState<'state, 'snapshot> * SessionRuntimeEffect<'projection> list

    /// Read the current product projection without mutating runtime state.
    val project:
        contract: SessionContract<'configuration, 'state, 'input, 'projection, 'snapshot> ->
        state: SessionRuntimeState<'state, 'snapshot> ->
            SessionProjection<'projection>

    /// Read a restorable product snapshot without mutating runtime state.
    val snapshot:
        contract: SessionContract<'configuration, 'state, 'input, 'projection, 'snapshot> ->
        state: SessionRuntimeState<'state, 'snapshot> ->
            SessionSnapshot<'snapshot>
