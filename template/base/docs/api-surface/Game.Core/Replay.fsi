// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.16.0 (src/Game.Core/Replay.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// One accepted semantic operation in a replay. Device events and wall-clock time never enter this log.
[<RequireQualifiedAccess>]
type ReplayEventKind<'input> =
    | Input of SessionInput<'input>
    | Advance of stepCount: uint64

/// An accepted operation paired with the canonical product-state digest immediately after it.
type ReplayEvent<'input> =
    {
        Index: uint64
        Kind: ReplayEventKind<'input>
        StateDigest: string
    }

/// A restorable point after <c>NextEventIndex</c> accepted operations.
type ReplayCheckpoint<'snapshot> =
    {
        NextEventIndex: uint64
        Snapshot: SessionSnapshot<'snapshot>
        StateDigest: string
    }

/// Portable recording data. Product codecs own input and snapshot payload serialization.
type ReplayRecording<'input, 'snapshot> =
    {
        FormatVersion: int
        SessionId: string
        Compatibility: SessionCompatibility
        InitialSnapshot: SessionSnapshot<'snapshot>
        InitialStateDigest: string
        Events: ReplayEvent<'input> list
        Checkpoints: ReplayCheckpoint<'snapshot> list
    }

/// A malformed or incompatible recording that is refused before semantic execution.
[<RequireQualifiedAccess>]
type ReplayIssue =
    | UnsupportedFormatVersion of int
    | InvalidSession of SessionContractIssue list
    | WrongSession of expected: string * actual: string
    | IncompatibleSnapshot of SessionCompatibilityIssue list
    | InvalidEventIndex of expected: uint64 * actual: uint64
    | MissingStateDigest of eventIndex: uint64 option
    | InvalidAdvance of eventIndex: uint64
    | StaleInputSequence of eventIndex: uint64 * accepted: uint64 * candidate: uint64
    | InvalidCheckpointIndex of uint64
    | CheckpointDigestMismatch of checkpointIndex: uint64 * expected: string * actual: string
    | TargetBeyondRecording of target: uint64 * eventCount: uint64

/// First point where replayed product state differs from its recorded canonical digest.
type ReplayDivergence =
    {
        EventIndex: uint64
        ExpectedDigest: string
        ActualDigest: string
    }

/// Bounded replay result. Cancellation returns an immutable resume cursor and the last accepted state.
[<RequireQualifiedAccess>]
type ReplayRunOutcome<'state> =
    | Completed of nextEventIndex: uint64 * state: 'state
    | Cancelled of nextEventIndex: uint64 * state: 'state
    | Diverged of ReplayDivergence
    | ContractRefused of eventIndex: uint64 * SessionFailure

/// Construction helpers for a recording captured from accepted session operations.
[<RequireQualifiedAccess>]
module ReplayRecorder =
    val create:
        initialSnapshot: SessionSnapshot<'snapshot> ->
        initialStateDigest: string ->
            Result<ReplayRecording<'input, 'snapshot>, ReplayIssue list>

    val appendInput:
        input: SessionInput<'input> ->
        stateDigest: string ->
        recording: ReplayRecording<'input, 'snapshot> ->
            Result<ReplayRecording<'input, 'snapshot>, ReplayIssue list>

    val appendAdvance:
        stepCount: uint64 ->
        stateDigest: string ->
        recording: ReplayRecording<'input, 'snapshot> ->
            Result<ReplayRecording<'input, 'snapshot>, ReplayIssue list>

    val addCheckpoint:
        snapshot: SessionSnapshot<'snapshot> ->
        stateDigest: string ->
        recording: ReplayRecording<'input, 'snapshot> ->
            Result<ReplayRecording<'input, 'snapshot>, ReplayIssue list>

/// Validation, exact seek and cancellation-safe execution over the product's real session contract.
[<RequireQualifiedAccess>]
module Replay =
    val validate: recording: ReplayRecording<'input, 'snapshot> -> ReplayIssue list

    val seek:
        contract: SessionContract<'configuration, 'state, 'input, 'projection, 'snapshot> ->
        stateDigest: ('state -> string) ->
        shouldCancel: (uint64 -> bool) ->
        targetEventCount: uint64 ->
        recording: ReplayRecording<'input, 'snapshot> ->
            Result<ReplayRunOutcome<'state>, ReplayIssue list>

/// Canonical length-prefixed export text, stable across .NET and Fable for equal product encoders.
[<RequireQualifiedAccess>]
module ReplayExport =
    val canonicalText:
        encodeInput: ('input -> string) ->
        encodeSnapshot: ('snapshot -> string) ->
        recording: ReplayRecording<'input, 'snapshot> ->
            Result<string, ReplayIssue list>
