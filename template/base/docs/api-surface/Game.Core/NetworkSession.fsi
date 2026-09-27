// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.16.0 (src/Game.Core/NetworkSession.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// A client identity bound to one session and one opaque reconnect token.
type NetworkClientBinding =
    {
        SessionId: string
        ClientId: string
        ReconnectToken: string
    }

/// One transport-delivered semantic input. The inner input remains the Game session contract.
type NetworkInput<'input> =
    {
        Binding: NetworkClientBinding
        Input: SessionInput<'input>
    }

/// An input assigned its deterministic server acceptance order.
type AcceptedNetworkInput<'input> =
    {
        AcceptedOrder: uint64
        ClientId: string
        Input: SessionInput<'input>
    }

/// Portable admission state. Construction is available only through <c>NetworkAdmission.create</c>.
type NetworkAdmissionState<'input> =
    private | NetworkAdmissionState of
        string *
        NetworkClientBinding list *
        Map<string, uint64> *
        AcceptedNetworkInput<'input> list

[<RequireQualifiedAccess>]
type NetworkAdmissionIssue =
    | MissingSessionId
    | MissingClientId
    | MissingReconnectToken of clientId: string
    | DuplicateClientId of string
    | ClientAlreadyBound of string
    | WrongSession of expected: string * actual: string
    | ClientNotBound of string
    | ReconnectTokenMismatch of clientId: string
    | InvalidInput of SessionContractIssue list
    | InputSessionMismatch of bindingSession: string * inputSession: string
    | DuplicateInputSequence of clientId: string * sequence: uint64
    | StaleInputSequence of clientId: string * accepted: uint64 * candidate: uint64
    | PayloadRefused of code: string

/// What a reconnecting client needs relative to the current accepted revision.
[<RequireQualifiedAccess>]
type NetworkResyncDecision =
    | Current of revision: uint64
    | ReplaySuffix of afterRevision: uint64 * currentRevision: uint64
    | FullSnapshot of currentRevision: uint64 * reason: string
    | ClientAhead of clientRevision: uint64 * currentRevision: uint64

[<RequireQualifiedAccess>]
module NetworkAdmission =
    val create:
        sessionId: string ->
        bindings: NetworkClientBinding list ->
            Result<NetworkAdmissionState<'input>, NetworkAdmissionIssue list>

    /// Add one runtime-issued client binding without changing accepted history or sequence cursors.
    val bind:
        binding: NetworkClientBinding ->
        state: NetworkAdmissionState<'input> ->
            Result<NetworkAdmissionState<'input>, NetworkAdmissionIssue list>

    /// Retire a client binding and its sequence cursor while preserving the accepted room history.
    val unbind: clientId: string -> state: NetworkAdmissionState<'input> -> NetworkAdmissionState<'input>

    /// Validate identity, token, monotonic sequence and product payload before assigning the next order.
    val admit:
        validatePayload: ('input -> Result<unit, string>) ->
        candidate: NetworkInput<'input> ->
        state: NetworkAdmissionState<'input> ->
            Result<NetworkAdmissionState<'input> * AcceptedNetworkInput<'input>, NetworkAdmissionIssue>

    val accepted: state: NetworkAdmissionState<'input> -> AcceptedNetworkInput<'input> list
    val lastSequence: clientId: string -> state: NetworkAdmissionState<'input> -> uint64 option

    /// Canonical length-prefixed accepted-stream text, independent of transport arrival representation.
    val canonicalText: encodeInput: ('input -> string) -> state: NetworkAdmissionState<'input> -> string

    /// Select a bounded replay suffix when retained history is available; otherwise require a full snapshot.
    val resync:
        retainedAfterRevision: uint64 option ->
        clientRevision: uint64 ->
        currentRevision: uint64 ->
            NetworkResyncDecision
