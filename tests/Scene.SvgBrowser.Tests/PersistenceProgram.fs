module SvgPersistenceBrowserFixture

open System
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open FS.GG.UI.Scene.SvgBrowser

let mutable host: BrowserPersistenceHost option = None
let events = ResizeArray<BrowserPersistenceEvent>()
let mutable archive = ""

let family = function
    | "project" -> BrowserStorageFamily.ProjectDocument
    | "assets" -> BrowserStorageFamily.AssetManifest
    | "save" -> BrowserStorageFamily.GameSave
    | "workspace" -> BrowserStorageFamily.WorkspacePreferences
    | value -> invalidArg (nameof value) $"unknown family: {value}"

let familyName = function
    | BrowserStorageFamily.ProjectDocument -> "project"
    | BrowserStorageFamily.AssetManifest -> "assets"
    | BrowserStorageFamily.GameSave -> "save"
    | BrowserStorageFamily.WorkspacePreferences -> "workspace"

let eventObject = function
    | BrowserPersistenceEvent.Ready -> createObj [ "kind" ==> "ready" ]
    | BrowserPersistenceEvent.Persisted operation ->
        createObj [ "kind" ==> "persisted"; "generation" ==> float operation.Generation; "operation" ==> float operation.Operation ]
    | BrowserPersistenceEvent.Loaded(key, value) ->
        createObj [
            "kind" ==> "loaded"
            "family" ==> familyName key.Family
            "slot" ==> key.Slot
            "value" ==>
                (value
                 |> Option.map (fun item ->
                     createObj [ "schema" ==> item.SchemaVersion; "hash" ==> item.PayloadHash; "payload" ==> item.Payload ])
                 |> Option.toObj)
        ]
    | BrowserPersistenceEvent.Exported value ->
        createObj [ "kind" ==> "exported"; "archive" ==> value ]
    | BrowserPersistenceEvent.Imported operation ->
        createObj [ "kind" ==> "imported"; "generation" ==> float operation.Generation; "operation" ==> float operation.Operation ]
    | BrowserPersistenceEvent.Failed(operation, failure) ->
        let diagnostic =
            match failure with
            | BrowserPersistenceFailure.InvalidRequest value -> "invalid:" + value
            | BrowserPersistenceFailure.QuotaExceeded -> "quota"
            | BrowserPersistenceFailure.DatabaseError value -> "database:" + value
            | BrowserPersistenceFailure.StaleGeneration -> "stale"
            | BrowserPersistenceFailure.Disposed -> "disposed"
        createObj [
            "kind" ==> "failed"
            "generation" ==> (operation |> Option.map (fun value -> box (float value.Generation)) |> Option.toObj)
            "operation" ==> (operation |> Option.map (fun value -> box (float value.Operation)) |> Option.toObj)
            "failure" ==> diagnostic
        ]
    | BrowserPersistenceEvent.Disposed -> createObj [ "kind" ==> "disposed" ]

let onEvent event =
    events.Add event
    match event with
    | BrowserPersistenceEvent.Exported value -> archive <- value
    | _ -> ()

let mount databaseName maxPayloadCharacters =
    host |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    events.Clear()
    archive <- ""
    host <-
        Some(
            new BrowserPersistenceHost(
                { DatabaseName = databaseName
                  MaxPayloadCharacters = maxPayloadCharacters
                  MaxArchiveCharacters = maxPayloadCharacters * 8 },
                onEvent))

let observation () =
    let value = host.Value.Observe()
    createObj [
        "ready" ==> value.IsReady
        "generation" ==> float value.Generation
        "pending" ==> value.PendingRequestCount
        "disposed" ==> value.IsDisposed
        "events" ==> (events |> Seq.map eventObject |> Seq.toArray)
        "archive" ==> archive
    ]

let api =
    createObj [
        "mount" ==> fun (databaseName: string) (maximum: int) -> mount databaseName maximum
        "observe" ==> fun () -> observation()
        "persist" ==> fun (familyId: string) (slot: string) (schema: int) (hash: string) (payload: string) (generation: int) (operation: int) ->
            host.Value.Persist(
                { Generation = uint64 generation; Operation = uint64 operation },
                { Key = { Family = family familyId; Slot = slot }; SchemaVersion = schema; PayloadHash = hash; Payload = payload })
        "load" ==> fun (familyId: string) (slot: string) -> host.Value.Load({ Family = family familyId; Slot = slot })
        "exportArchive" ==> fun () -> host.Value.ExportArchive()
        "importArchive" ==> fun (value: string) (generation: int) (operation: int) ->
            host.Value.ImportArchive({ Generation = uint64 generation; Operation = uint64 operation }, value)
        "dispose" ==> fun () -> (host.Value :> IDisposable).Dispose(); observation()
    ]

[<Emit("window.svgPersistence = $0")>]
let expose (_api: obj) : unit = jsNative

expose api
