namespace FS.GG.UI.Scene.SvgBrowser

open System

/// Independent browser storage families shared with the Game save authority.
[<RequireQualifiedAccess>]
type BrowserStorageFamily = ProjectDocument | AssetManifest | GameSave | WorkspacePreferences

type BrowserStorageKey = { Family: BrowserStorageFamily; Slot: string }

[<Struct>]
type BrowserStorageOperationId = { Generation: uint64; Operation: uint64 }

type BrowserStoredValue =
    { Key: BrowserStorageKey
      SchemaVersion: int
      PayloadHash: string
      Payload: string }

type BrowserArchiveMember = { Path: string; Hash: string; Content: string }
type BrowserArchive = { Members: BrowserArchiveMember list }

[<RequireQualifiedAccess>]
type BrowserArchiveIssue =
    | EmptyArchive
    | InvalidPath of string
    | DuplicatePath of string
    | MissingMember of string
    | UnexpectedMember of string
    | HashMismatch of path: string * expected: string * actual: string

[<RequireQualifiedAccess>]
module BrowserArchive =
    /// Validate the complete archive in stable path order before any host transaction begins.
    val validate:
        hashContent: (string -> string) ->
        requiredPaths: string list ->
        archive: BrowserArchive -> Result<BrowserArchive, BrowserArchiveIssue list>

type BrowserPersistenceConfig =
    { DatabaseName: string
      MaxPayloadCharacters: int
      MaxArchiveCharacters: int }

[<RequireQualifiedAccess>]
type BrowserPersistenceFailure = InvalidRequest of string | QuotaExceeded | DatabaseError of string | StaleGeneration | Disposed

[<RequireQualifiedAccess>]
type BrowserPersistenceEvent =
    | Ready
    | Persisted of BrowserStorageOperationId
    | Loaded of BrowserStorageKey * BrowserStoredValue option
    | Exported of string
    | Imported of BrowserStorageOperationId
    | Failed of BrowserStorageOperationId option * BrowserPersistenceFailure
    | Disposed

type BrowserPersistenceObservation =
    { IsReady: bool
      Generation: uint64
      PendingRequestCount: int
      IsDisposed: bool }

/// Disposable IndexedDB interpreter. Imports validate every member and hash before one atomic replacement.
[<Sealed>]
type BrowserPersistenceHost =
    new: config: BrowserPersistenceConfig * onEvent: (BrowserPersistenceEvent -> unit) -> BrowserPersistenceHost
    member Persist: operation: BrowserStorageOperationId * value: BrowserStoredValue -> unit
    member Load: key: BrowserStorageKey -> unit
    member ExportArchive: unit -> unit
    member ImportArchive: operation: BrowserStorageOperationId * archiveJson: string -> unit
    member Observe: unit -> BrowserPersistenceObservation
    interface IDisposable

[<RequireQualifiedAccess>]
module BrowserPersistenceHost =
    val defaultConfig: databaseName: string -> BrowserPersistenceConfig
