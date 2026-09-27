// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.16.0 (src/Game.Core/SaveMigration.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Independently versioned persisted-data families.
[<RequireQualifiedAccess>]
type SaveFamily =
    | ProjectDocument
    | AssetManifest
    | GameSave
    | WorkspacePreferences

/// Compatibility identity carried by every persisted value.
type SaveIdentity =
    {
        Family: SaveFamily
        EngineId: string
        EngineVersion: string
        ProfileId: string
        SchemaId: string
        SchemaVersion: int
        ContentHash: string
        AssetHash: string
    }

/// Persisted value plus a caller-defined deterministic payload hash.
type SaveEnvelope<'payload> =
    {
        Identity: SaveIdentity
        PayloadHash: string
        Payload: 'payload
    }

/// Generation-scoped storage operation identity.
[<Struct>]
type SaveOperationId =
    {
        Generation: uint64
        Operation: uint64
    }

/// One adjacent, deterministic schema migration.
type SaveMigrationStep<'payload> =
    {
        FromVersion: int
        ToVersion: int
        Apply: 'payload -> Result<'payload, string>
    }

[<RequireQualifiedAccess>]
type SaveRefusal =
    | InvalidIdentity of string
    | FamilyMismatch of SaveFamily * SaveFamily
    | EngineMismatch of expectedId: string * expectedVersion: string * actualId: string * actualVersion: string
    | ProfileMismatch of string * string
    | SchemaMismatch of string * string
    | ContentHashMismatch of string * string
    | AssetHashMismatch of string * string
    | PayloadHashMismatch of string * string
    | NewerSchema of supported: int * actual: int
    | MissingMigration of fromVersion: int * toVersion: int
    | InvalidMigrationPlan of string
    | MigrationFailed of fromVersion: int * toVersion: int * diagnostic: string
    | OperationSequenceExhausted
    | StaleOperation of expected: SaveOperationId * actual: SaveOperationId
    | OperationFailed of string
    | Inactive

[<RequireQualifiedAccess>]
type SaveMigrationStatus =
    | Idle
    | Migrating
    | Ready
    | Failed of SaveRefusal
    | Disposed

type SaveMigrationState<'payload> =
    {
        Expected: SaveIdentity
        Generation: uint64
        NextOperation: uint64
        Status: SaveMigrationStatus
        LastCommitted: SaveEnvelope<'payload> option
        Pending: (SaveOperationId * SaveEnvelope<'payload>) option
    }

[<RequireQualifiedAccess>]
type SaveMigrationObservation<'payload> =
    | Begin of SaveEnvelope<'payload>
    | StepCommitted of SaveOperationId
    | StepFailed of SaveOperationId * string
    | Replace of SaveEnvelope<'payload>
    | Dispose

[<RequireQualifiedAccess>]
type SaveMigrationEffect<'payload> =
    | PersistStep of SaveOperationId * SaveEnvelope<'payload>
    | Accepted of SaveEnvelope<'payload>
    | GenerationInvalidated of uint64 * uint64
    | Refused of SaveRefusal
    | Disposed

[<RequireQualifiedAccess>]
module SaveMigration =
    val initialize: expected: SaveIdentity -> Result<SaveMigrationState<'payload>, SaveRefusal>

    val update:
        hashPayload: ('payload -> string) ->
        steps: SaveMigrationStep<'payload> list ->
        observation: SaveMigrationObservation<'payload> ->
        state: SaveMigrationState<'payload> ->
            SaveMigrationState<'payload> * SaveMigrationEffect<'payload> list

[<RequireQualifiedAccess>]
type AutosaveStatus =
    | Clean
    | Debouncing
    | Persisting
    | Failed of string
    | Cancelled
    | Disposed

type AutosaveConfig = { DebounceMilliseconds: uint64 }

type AutosaveState<'value> =
    {
        Config: AutosaveConfig
        Generation: uint64
        NextOperation: uint64
        Status: AutosaveStatus
        LastCommitted: 'value option
        Draft: 'value option
        DueAtMilliseconds: uint64 option
        InFlight: (SaveOperationId * 'value) option
    }

[<RequireQualifiedAccess>]
type AutosaveObservation<'value> =
    | Edit of nowMilliseconds: uint64 * value: 'value
    | Tick of nowMilliseconds: uint64
    | Persisted of SaveOperationId
    | PersistFailed of SaveOperationId * string
    | Retry
    | Cancel
    | Replace of 'value option
    | Dispose

[<RequireQualifiedAccess>]
type AutosaveEffect<'value> =
    | Persist of SaveOperationId * 'value
    | Committed of SaveOperationId * 'value
    | GenerationInvalidated of uint64 * uint64
    | Refused of SaveRefusal
    | Disposed

[<RequireQualifiedAccess>]
module Autosave =
    val initialize: AutosaveConfig -> 'value option -> Result<AutosaveState<'value>, SaveRefusal>

    val update:
        AutosaveObservation<'value> -> AutosaveState<'value> -> AutosaveState<'value> * AutosaveEffect<'value> list
