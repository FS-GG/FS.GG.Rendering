// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.16.0 (src/Game.Core/SessionContract.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Versioned identity shared by session initialization and snapshots.
/// This is portable contract data and has no renderer or transport dependency.
type SessionCompatibility =
    {
        ContractVersion: int
        EngineId: string
        EngineVersion: string
        ProfileId: string
        SchemaId: string
        SchemaVersion: int
    }

/// A precise reason why two session identities cannot exchange a snapshot.
[<RequireQualifiedAccess>]
type SessionCompatibilityIssue =
    | ContractVersion of expected: int * actual: int
    | EngineId of expected: string * actual: string
    | EngineVersion of expected: string * actual: string
    | ProfileId of expected: string * actual: string
    | SchemaId of expected: string * actual: string
    | SchemaVersion of expected: int * actual: int

/// A malformed value at the portable session boundary.
[<RequireQualifiedAccess>]
type SessionContractIssue =
    | InvalidContractVersion of int
    | MissingEngineId
    | MissingEngineVersion
    | MissingProfileId
    | MissingSchemaId
    | InvalidSchemaVersion of int
    | MissingSessionId
    | MissingInputId

/// Explicit support classification for the surface shipped by this package.
[<RequireQualifiedAccess>]
type SessionSupport =
    /// Game.Core supplies contract values and signatures only. It supplies no M5 session runtime.
    | ContractEnvelopeOnly
    /// Game.Core supplies the portable bounded fixed-step session reducer.
    | PortableRuntime

/// Input to a product-provided session initializer.
type SessionInitialization<'configuration> =
    {
        SessionId: string
        Compatibility: SessionCompatibility
        Configuration: 'configuration
    }

/// One admitted semantic intent. `InputId` is a stable namespaced product identity, not a device token.
type SessionInput<'input> =
    {
        SessionId: string
        InputId: string
        Sequence: uint64
        Value: 'input
    }

/// One caller-observed deterministic advancement boundary.
type SessionAdvance =
    {
        SessionId: string
        /// Number of whole deterministic steps the product implementation should apply.
        StepCount: uint64
    }

/// A read-only projection of accepted session state.
type SessionProjection<'projection> =
    {
        SessionId: string
        Revision: uint64
        Value: 'projection
    }

/// A restorable value bound to the exact engine/profile/schema identity that wrote it.
type SessionSnapshot<'snapshot> =
    {
        SessionId: string
        Revision: uint64
        Compatibility: SessionCompatibility
        Value: 'snapshot
    }

/// Product-owned failure data returned through the generic contract.
type SessionFailure = { Code: string; Message: string }

/// The complete portable shape a product implements to expose a deterministic session.
/// Game.Core does not execute, host, persist, or transport these operations.
type SessionContract<'configuration, 'state, 'input, 'projection, 'snapshot> =
    {
        Initialize: SessionInitialization<'configuration> -> Result<'state, SessionFailure>
        AdmitInput: SessionInput<'input> -> 'state -> Result<'state, SessionFailure>
        Advance: SessionAdvance -> 'state -> Result<'state, SessionFailure>
        Project: 'state -> SessionProjection<'projection>
        Snapshot: 'state -> SessionSnapshot<'snapshot>
        Restore: SessionSnapshot<'snapshot> -> Result<'state, SessionFailure>
    }

/// Validation and exact compatibility checks for session identities.
[<RequireQualifiedAccess>]
module SessionCompatibility =
    /// Validate required identifiers and positive format versions in deterministic field order.
    val validate: value: SessionCompatibility -> SessionContractIssue list
    /// Compare every compatibility axis in deterministic field order.
    val compare: expected: SessionCompatibility -> actual: SessionCompatibility -> SessionCompatibilityIssue list
    /// True only when every compatibility axis is exactly equal.
    val isCompatible: expected: SessionCompatibility -> actual: SessionCompatibility -> bool

/// Validation for portable envelopes. Product-owned payloads are not inspected.
[<RequireQualifiedAccess>]
module SessionEnvelope =
    val validateInitialization: value: SessionInitialization<'configuration> -> SessionContractIssue list
    val validateInput: value: SessionInput<'input> -> SessionContractIssue list
    val validateAdvance: value: SessionAdvance -> SessionContractIssue list
    val validateProjection: value: SessionProjection<'projection> -> SessionContractIssue list
    val validateSnapshot: value: SessionSnapshot<'snapshot> -> SessionContractIssue list

/// Stable metadata for this package's support level.
[<RequireQualifiedAccess>]
module SessionSupport =
    val current: SessionSupport
    val id: support: SessionSupport -> string
