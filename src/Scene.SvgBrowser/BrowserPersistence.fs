namespace FS.GG.UI.Scene.SvgBrowser

open System
open System.Collections.Generic
open Fable.Core

[<RequireQualifiedAccess>]
type BrowserStorageFamily =
    | ProjectDocument
    | AssetManifest
    | GameSave
    | WorkspacePreferences

type BrowserStorageKey =
    {
        Family: BrowserStorageFamily
        Slot: string
    }

[<Struct>]
type BrowserStorageOperationId =
    {
        Generation: uint64
        Operation: uint64
    }

type BrowserStoredValue =
    {
        Key: BrowserStorageKey
        SchemaVersion: int
        PayloadHash: string
        Payload: string
    }

type BrowserArchiveMember =
    {
        Path: string
        Hash: string
        Content: string
    }

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
    let private validPath path =
        not (String.IsNullOrWhiteSpace path)
        && not (path.StartsWith("/", StringComparison.Ordinal))
        && not (path.Contains("\\", StringComparison.Ordinal))
        && (path.Split('/')
            |> Array.forall (fun part -> part <> "" && part <> "." && part <> ".."))

    let validate hashContent requiredPaths archive =
        let ordered = archive.Members |> List.sortBy (fun member' -> member'.Path)
        let issues = ResizeArray<BrowserArchiveIssue>()

        if ordered.IsEmpty then
            issues.Add BrowserArchiveIssue.EmptyArchive

        let seen = HashSet<string>()

        for member' in ordered do
            if not (validPath member'.Path) then
                issues.Add(BrowserArchiveIssue.InvalidPath member'.Path)

            if not (seen.Add member'.Path) then
                issues.Add(BrowserArchiveIssue.DuplicatePath member'.Path)

            let actual = hashContent member'.Content

            if actual <> member'.Hash then
                issues.Add(BrowserArchiveIssue.HashMismatch(member'.Path, member'.Hash, actual))

        let actualPaths = seen |> Set.ofSeq
        let required = requiredPaths |> Set.ofList

        for path in Set.difference required actualPaths do
            issues.Add(BrowserArchiveIssue.MissingMember path)

        for path in Set.difference actualPaths required do
            issues.Add(BrowserArchiveIssue.UnexpectedMember path)

        if issues.Count = 0 then
            Ok { Members = ordered }
        else
            Error(List.ofSeq issues)

type BrowserPersistenceConfig =
    {
        DatabaseName: string
        MaxPayloadCharacters: int
        MaxArchiveCharacters: int
    }

[<RequireQualifiedAccess>]
type BrowserPersistenceFailure =
    | InvalidRequest of string
    | QuotaExceeded
    | DatabaseError of string
    | StaleGeneration
    | Disposed

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
    {
        IsReady: bool
        Generation: uint64
        PendingRequestCount: int
        IsDisposed: bool
    }

module private PersistenceDom =
    [<Emit("({db:null})")>]
    let graph () : obj = jsNative

    [<Emit("(function(g,name,ok,fail){try{const r=indexedDB.open(name,1);r.onupgradeneeded=()=>{const d=r.result;if(!d.objectStoreNames.contains('records'))d.createObjectStore('records',{keyPath:'key'});};r.onsuccess=()=>{g.db=r.result;ok();};r.onerror=()=>fail(String(r.error?.message||'open failed'));r.onblocked=()=>fail('open blocked');}catch(e){fail(String(e?.message||e));}})($0,$1,$2,$3)")>]
    let openDb (_graph: obj) (_name: string) (_ok: unit -> unit) (_fail: string -> unit) : unit = jsNative

    [<Emit("(function(g,key,family,slot,schema,hash,payload,ok,fail){try{const tx=g.db.transaction('records','readwrite');tx.objectStore('records').put({key,family,slot,schemaVersion:schema,payloadHash:hash,payload});tx.oncomplete=ok;tx.onerror=()=>fail(String(tx.error?.message||'transaction failed'));tx.onabort=()=>fail(String(tx.error?.message||'transaction aborted'));}catch(e){fail(String(e?.message||e));}})($0,$1,$2,$3,$4,$5,$6,$7,$8)")>]
    let put
        (_graph: obj)
        (_key: string)
        (_family: string)
        (_slot: string)
        (_schema: int)
        (_hash: string)
        (_payload: string)
        (_ok: unit -> unit)
        (_fail: string -> unit)
        : unit =
        jsNative

    [<Emit("(function(g,key,ok,fail){try{const r=g.db.transaction('records','readonly').objectStore('records').get(key);r.onsuccess=()=>{const x=r.result;ok(x==null?null:JSON.stringify(x));};r.onerror=()=>fail(String(r.error?.message||'read failed'));}catch(e){fail(String(e?.message||e));}})($0,$1,$2,$3)")>]
    let get (_graph: obj) (_key: string) (_ok: string -> unit) (_fail: string -> unit) : unit = jsNative

    [<Emit("(function(g,ok,fail){try{const r=g.db.transaction('records','readonly').objectStore('records').getAll();r.onsuccess=async()=>{try{const rows=r.result.sort((a,b)=>a.key.localeCompare(b.key)),members=[];for(const x of rows){const content=JSON.stringify({family:x.family,slot:x.slot,schemaVersion:x.schemaVersion,payloadHash:x.payloadHash,payload:x.payload});const bytes=await crypto.subtle.digest('SHA-256',new TextEncoder().encode(content));const hash=Array.from(new Uint8Array(bytes),b=>b.toString(16).padStart(2,'0')).join('');members.push({path:'data/'+x.family+'/'+encodeURIComponent(x.slot)+'.json',hash,content});}ok(JSON.stringify({schema:'fsgg.browser-archive/v1',declaredMembers:members.map(x=>x.path),members}));}catch(e){fail(String(e?.message||e));}};r.onerror=()=>fail(String(r.error?.message||'export failed'));}catch(e){fail(String(e?.message||e));}})($0,$1,$2)")>]
    let exportArchive (_graph: obj) (_ok: string -> unit) (_fail: string -> unit) : unit = jsNative

    [<Emit("(async function(g,text,max,ok,fail){try{if(text.length>max)throw Object.assign(new Error('quota'),{name:'QuotaExceededError'});const a=JSON.parse(text);if(!a||a.schema!=='fsgg.browser-archive/v1'||!Array.isArray(a.declaredMembers)||!Array.isArray(a.members)||a.members.length===0)throw new Error('empty or unsupported archive');const declared=new Set(a.declaredMembers);if(declared.size!==a.declaredMembers.length||a.declaredMembers.some(x=>typeof x!=='string'))throw new Error('invalid declared members');const rows=[],seen=new Set();for(const m of a.members){if(!m||typeof m.path!=='string'||typeof m.hash!=='string'||typeof m.content!=='string')throw new Error('malformed member');if(seen.has(m.path))throw new Error('duplicate path '+m.path);seen.add(m.path);const hit=/^data\/(project|assets|save|workspace)\/([^/]+)\.json$/.exec(m.path);if(!hit)throw new Error('invalid path '+m.path);const slot=decodeURIComponent(hit[2]);if(encodeURIComponent(slot)!==hit[2]||slot.includes('/')||slot==='.'||slot==='..')throw new Error('non-canonical path '+m.path);const bytes=await crypto.subtle.digest('SHA-256',new TextEncoder().encode(m.content));const hash=Array.from(new Uint8Array(bytes),b=>b.toString(16).padStart(2,'0')).join('');if(hash!==m.hash)throw new Error('hash mismatch '+m.path);const x=JSON.parse(m.content);if(x.family!==hit[1]||x.slot!==slot||!Number.isInteger(x.schemaVersion)||x.schemaVersion<1||typeof x.payloadHash!=='string'||typeof x.payload!=='string')throw new Error('content mismatch '+m.path);rows.push({key:x.family+':'+x.slot,...x});}for(const path of declared)if(!seen.has(path))throw new Error('missing member '+path);for(const path of seen)if(!declared.has(path))throw new Error('unexpected member '+path);const tx=g.db.transaction('records','readwrite'),s=tx.objectStore('records');s.clear();for(const x of rows)s.put(x);tx.oncomplete=ok;tx.onerror=()=>fail(String(tx.error?.message||'transaction failed'));tx.onabort=()=>fail(String(tx.error?.message||'transaction aborted'));}catch(e){fail((e?.name==='QuotaExceededError'?'quota:':'invalid:')+String(e?.message||e));}})($0,$1,$2,$3,$4)")>]
    let importArchive (_graph: obj) (_json: string) (_max: int) (_ok: unit -> unit) (_fail: string -> unit) : unit =
        jsNative

    [<Emit("(function(g){try{g.db?.close();}finally{g.db=null;}})($0)")>]
    let close (_graph: obj) : unit = jsNative

    [<Emit("JSON.parse($0)")>]
    let parse (_json: string) : obj = jsNative

    [<Emit("$0.family")>]
    let family (_value: obj) : string = jsNative

    [<Emit("$0.slot")>]
    let slot (_value: obj) : string = jsNative

    [<Emit("$0.schemaVersion")>]
    let schema (_value: obj) : int = jsNative

    [<Emit("$0.payloadHash")>]
    let payloadHash (_value: obj) : string = jsNative

    [<Emit("$0.payload")>]
    let payload (_value: obj) : string = jsNative

[<Sealed>]
type BrowserPersistenceHost(config: BrowserPersistenceConfig, onEvent: BrowserPersistenceEvent -> unit) =
    let graph = PersistenceDom.graph ()
    let pending = HashSet<string>()
    let mutable ready = false
    let mutable disposed = false
    let mutable generation = 0UL

    let familyId (family: BrowserStorageFamily) =
        match family with
        | BrowserStorageFamily.ProjectDocument -> "project"
        | BrowserStorageFamily.AssetManifest -> "assets"
        | BrowserStorageFamily.GameSave -> "save"
        | BrowserStorageFamily.WorkspacePreferences -> "workspace"

    let familyOf =
        function
        | "project" -> BrowserStorageFamily.ProjectDocument
        | "assets" -> BrowserStorageFamily.AssetManifest
        | "save" -> BrowserStorageFamily.GameSave
        | _ -> BrowserStorageFamily.WorkspacePreferences

    let storageKeyText (value: BrowserStorageKey) =
        familyId value.Family + ":" + value.Slot

    let validSlot (value: string) =
        not (String.IsNullOrWhiteSpace value)
        && value <> "."
        && value <> ".."
        && not (value.Contains("/", StringComparison.Ordinal))

    let token (id: BrowserStorageOperationId) =
        string id.Generation + ":" + string id.Operation

    let failure (operation: BrowserStorageOperationId option) (diagnostic: string) =
        let value =
            if diagnostic.StartsWith("quota:", StringComparison.OrdinalIgnoreCase) then
                BrowserPersistenceFailure.QuotaExceeded
            elif diagnostic.StartsWith("invalid:", StringComparison.OrdinalIgnoreCase) then
                BrowserPersistenceFailure.InvalidRequest(diagnostic.Substring("invalid:".Length))
            else
                BrowserPersistenceFailure.DatabaseError diagnostic

        onEvent (BrowserPersistenceEvent.Failed(operation, value))

    do
        if String.IsNullOrWhiteSpace config.DatabaseName then
            invalidArg (nameof config) "database name is required"

        if config.MaxPayloadCharacters < 1 then
            invalidArg (nameof config) "payload limit must be positive"

        if config.MaxArchiveCharacters < 1 then
            invalidArg (nameof config) "archive limit must be positive"

        PersistenceDom.openDb
            graph
            config.DatabaseName
            (fun () ->
                if not disposed then
                    ready <- true
                    onEvent BrowserPersistenceEvent.Ready)
            (failure None)

    member _.Persist(operation, value) =
        if disposed then
            onEvent (BrowserPersistenceEvent.Failed(Some operation, BrowserPersistenceFailure.Disposed))
        elif operation.Generation < generation then
            onEvent (BrowserPersistenceEvent.Failed(Some operation, BrowserPersistenceFailure.StaleGeneration))
        elif
            not (validSlot value.Key.Slot)
            || value.SchemaVersion < 1
            || String.IsNullOrWhiteSpace value.PayloadHash
        then
            onEvent (
                BrowserPersistenceEvent.Failed(
                    Some operation,
                    BrowserPersistenceFailure.InvalidRequest "invalid stored value"
                )
            )
        elif value.Payload.Length > config.MaxPayloadCharacters then
            onEvent (BrowserPersistenceEvent.Failed(Some operation, BrowserPersistenceFailure.QuotaExceeded))
        else
            if operation.Generation > generation then
                generation <- operation.Generation
                pending.Clear()

            let t = token operation

            if pending.Add t then
                PersistenceDom.put
                    graph
                    (storageKeyText value.Key)
                    (familyId value.Key.Family)
                    value.Key.Slot
                    value.SchemaVersion
                    value.PayloadHash
                    value.Payload
                    (fun () ->
                        if pending.Remove t && not disposed then
                            onEvent (BrowserPersistenceEvent.Persisted operation))
                    (fun error ->
                        if pending.Remove t && not disposed then
                            failure (Some operation) error)
            else
                onEvent (
                    BrowserPersistenceEvent.Failed(
                        Some operation,
                        BrowserPersistenceFailure.InvalidRequest "duplicate operation"
                    )
                )

    member _.Load(key) =
        if disposed then
            onEvent (BrowserPersistenceEvent.Failed(None, BrowserPersistenceFailure.Disposed))
        elif not (validSlot key.Slot) then
            onEvent (BrowserPersistenceEvent.Failed(None, BrowserPersistenceFailure.InvalidRequest "invalid slot"))
        else
            PersistenceDom.get
                graph
                (storageKeyText key)
                (fun json ->
                    if not disposed then
                        if obj.ReferenceEquals(json, null) then
                            onEvent (BrowserPersistenceEvent.Loaded(key, None))
                        else
                            let x = PersistenceDom.parse json

                            let value =
                                {
                                    Key =
                                        {
                                            Family = familyOf (PersistenceDom.family x)
                                            Slot = PersistenceDom.slot x
                                        }
                                    SchemaVersion = PersistenceDom.schema x
                                    PayloadHash = PersistenceDom.payloadHash x
                                    Payload = PersistenceDom.payload x
                                }

                            onEvent (BrowserPersistenceEvent.Loaded(key, Some value)))
                (fun error ->
                    if not disposed then
                        failure None error)

    member _.ExportArchive() =
        if disposed then
            onEvent (BrowserPersistenceEvent.Failed(None, BrowserPersistenceFailure.Disposed))
        else
            PersistenceDom.exportArchive
                graph
                (fun json ->
                    if not disposed then
                        onEvent (BrowserPersistenceEvent.Exported json))
                (fun error ->
                    if not disposed then
                        failure None error)

    member _.ImportArchive(operation, archiveJson) =
        if disposed then
            onEvent (BrowserPersistenceEvent.Failed(Some operation, BrowserPersistenceFailure.Disposed))
        elif operation.Generation < generation then
            onEvent (BrowserPersistenceEvent.Failed(Some operation, BrowserPersistenceFailure.StaleGeneration))
        else
            if operation.Generation > generation then
                generation <- operation.Generation
                pending.Clear()

            let t = token operation

            if pending.Add t then
                PersistenceDom.importArchive
                    graph
                    archiveJson
                    config.MaxArchiveCharacters
                    (fun () ->
                        if pending.Remove t && not disposed then
                            onEvent (BrowserPersistenceEvent.Imported operation))
                    (fun error ->
                        if pending.Remove t && not disposed then
                            failure (Some operation) error)
            else
                onEvent (
                    BrowserPersistenceEvent.Failed(
                        Some operation,
                        BrowserPersistenceFailure.InvalidRequest "duplicate operation"
                    )
                )

    member _.Observe() =
        {
            IsReady = ready
            Generation = generation
            PendingRequestCount = pending.Count
            IsDisposed = disposed
        }

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                ready <- false

                if generation <> UInt64.MaxValue then
                    generation <- generation + 1UL

                pending.Clear()
                PersistenceDom.close graph
                onEvent BrowserPersistenceEvent.Disposed

[<RequireQualifiedAccess>]
module BrowserPersistenceHost =
    let defaultConfig databaseName =
        {
            DatabaseName = databaseName
            MaxPayloadCharacters = 4 * 1024 * 1024
            MaxArchiveCharacters = 32 * 1024 * 1024
        }
