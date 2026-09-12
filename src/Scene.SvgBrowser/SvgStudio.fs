namespace FS.GG.UI.Scene.SvgBrowser

open System
open System.Globalization
open Browser.Dom
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open FS.GG.UI.Scene

type SvgStudioOptions = { MountNamespace: string; AccessibleLabel: string; WorkerFactory: (unit -> obj) option }
type SvgStudioObservation = { Revision: int; SelectionCount: int; OwnedListenerCount: int; ActiveGesture: string option; WorkerInFlight: bool }

module private StudioInterop =
    [<Emit("URL.createObjectURL(new Blob([Uint8Array.from(atob($0), c => c.charCodeAt(0))], {type:'font/woff2'}))")>]
    let fontUrl (_base64: string) : string = jsNative
    [<Emit("new FontFace($0, \"url(\" + $1 + \") format('woff2')\", {weight:'400'})")>]
    let fontFace (_family: string) (_url: string) : obj = jsNative
    [<Emit("$0.load().then(face => { document.fonts.add(face); $1(); }, error => $2(String(error?.message ?? error)))")>]
    let loadFont (_face: obj) (_ready: unit -> unit) (_failed: string -> unit) : unit = jsNative
    [<Emit("document.fonts.delete($0)")>]
    let removeFont (_face: obj) : unit = jsNative
    [<Emit("URL.revokeObjectURL($0)")>]
    let revokeUrl (_url: string) : unit = jsNative
    [<Emit("$0.terminate()")>]
    let terminate (_worker: obj) : unit = jsNative
    [<Emit("$0.postMessage($1)")>]
    let postMessage (_worker: obj) (_message: obj) : unit = jsNative
    [<Emit("$0.onmessage = $1")>]
    let onMessage (_worker: obj) (_handler: MessageEvent -> unit) : unit = jsNative
    [<Emit("$0.onerror = $1")>]
    let onError (_worker: obj) (_handler: Event -> unit) : unit = jsNative
    [<Emit("JSON.stringify($0).length")>]
    let encodedSize (_value: obj) : int = jsNative
    [<Emit("$0.data")>]
    let data (_event: MessageEvent) : obj = jsNative
    [<Emit("$0.operationId")>]
    let operationId (_value: obj) : string = jsNative
    [<Emit("$0.acceptedRevision")>]
    let revision (_value: obj) : int = jsNative
    [<Emit("$0.inputContentHash")>]
    let hash (_value: obj) : string = jsNative
    [<Emit("$0.contours.map(c => c.map(p => ({ X:p[0], Y:p[1] })))")>]
    let contours (_value: obj) : Point list list = jsNative
    [<Emit("$0.error ?? null")>]
    let error (_value: obj) : string option = jsNative

[<Sealed>]
type SvgFontResourceHost internal (resource: SvgFontResource) =
    let url = StudioInterop.fontUrl resource.Base64
    let face = StudioInterop.fontFace resource.Family url
    let mutable ready = false
    let mutable diagnostic = None
    let mutable disposed = false
    do StudioInterop.loadFont face (fun () -> if not disposed then ready <- true) (fun error -> if not disposed then diagnostic <- Some error)
    member _.Ready = ready
    member _.Diagnostic = diagnostic
    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                StudioInterop.removeFont face
                StudioInterop.revokeUrl url

[<Sealed>]
type SvgGeometryWorkerHost(factory: unit -> obj) =
    let mutable worker: obj option = None
    let mutable timeout: float option = None
    let finish () =
        timeout |> Option.iter window.clearTimeout
        timeout <- None
        worker |> Option.iter StudioInterop.terminate
        worker <- None
    member _.InFlight = worker.IsSome
    member _.Start(prepared, onResult, onError) =
        if worker.IsSome then Error "one geometry operation is already in flight; requests are not queued"
        elif prepared.EncodedRequest.Length > 1048576 then Error "encoded geometry request exceeds 1 MiB"
        else
            let value = factory()
            worker <- Some value
            StudioInterop.onMessage value (fun event ->
                let payload = StudioInterop.data event
                if StudioInterop.encodedSize payload > 1048576 then finish(); onError "encoded geometry result exceeds 1 MiB"
                else
                    match StudioInterop.error payload with
                    | Some message -> finish(); onError message
                    | None ->
                        let result = { OperationId=StudioInterop.operationId payload; AcceptedRevision=StudioInterop.revision payload; InputContentHash=StudioInterop.hash payload; Contours=StudioInterop.contours payload }
                        finish(); onResult result)
            StudioInterop.onError value (fun _ -> finish(); onError "geometry worker failed")
            timeout <- Some(window.setTimeout((fun () -> finish(); onError "geometry worker exceeded the 2 second deadline"), 2000))
            StudioInterop.postMessage value (JS.JSON.parse prepared.EncodedRequest)
            Ok ()
    member _.Cancel() = finish()
    interface IDisposable with member _.Dispose() = finish()

[<Sealed>]
type SvgStudioHost(root: HTMLElement, documentHost: SvgDocumentBrowserHost, initial: SvgAuthoringState, initialTool: SvgArtState, workerHost: SvgGeometryWorkerHost option, onChange: SvgAuthoringState -> unit) =
    let mutable state = initial
    let mutable tool = initialTool
    let listeners = ResizeArray<EventTarget * string * (Event -> unit)>()
    let mutable disposed = false
    let sync next =
        state <- next
        documentHost.Replace state.Document |> ignore
        onChange state
    let cancelActive () =
        match state.Preview with
        | Some preview -> SvgAuthoring.cancelPreview preview.TransactionId state |> Result.iter sync
        | None -> ()
    let listen (target: EventTarget) name (handler: Event -> unit) =
        target.addEventListener(name, handler)
        listeners.Add(target, name, handler)
    do
        listen (root :> EventTarget) "keydown" (fun event ->
            let key = event :?> KeyboardEvent
            if key.key = "Escape" then cancelActive())
        for name in [ "pointercancel"; "lostpointercapture" ] do listen (documentHost.Root :> EventTarget) name (fun _ -> cancelActive())
        listen (window :> EventTarget) "blur" (fun _ -> cancelActive())
    member _.Root = root
    member _.State = state
    member _.ToolState = tool
    member _.GeometryWorker = workerHost
    member _.SetSelection elementIds =
        let rec ids elements = elements |> List.collect (fun (element: SvgElement) -> element.Id :: (match element.Content with SvgElementContent.Group children -> ids children | _ -> []))
        let known = ids state.Document.Children |> Set.ofList
        match elementIds |> List.tryFind (known.Contains >> not) with
        | Some missing -> Error(SvgArtError.MissingElement missing)
        | None -> tool <- { tool with Selection = List.distinct elementIds }; Ok ()
    member _.SetCamera camera =
        if not (SvgAffine.isFinite camera) then Error(SvgArtError.InvalidSelection "camera must be finite")
        else tool <- { tool with Camera = camera }; Ok ()
    member _.Preview transaction = SvgAuthoring.preview state.Revision transaction state |> Result.map sync
    member _.CommitGesture transactionId = SvgAuthoring.commitPreview state.Revision transactionId state |> Result.map sync
    member _.CancelGesture transactionId = SvgAuthoring.cancelPreview transactionId state |> Result.map sync
    member _.Undo() = SvgAuthoring.undo state.Revision state |> Result.map sync
    member _.Redo() = SvgAuthoring.redo state.Revision state |> Result.map sync
    member _.ApplyNumericTransform(transactionId, transform) =
        let transaction = { Schema=SvgAuthoring.transactionSchema; Id=transactionId; Operations=[SvgAuthoringOperation.TransformElements(tool.Selection, transform)] }
        SvgAuthoring.commit state.Revision transaction state |> Result.map sync
    member _.Observe() = { Revision=state.Revision; SelectionCount=tool.Selection.Length; OwnedListenerCount=listeners.Count; ActiveGesture=state.Preview |> Option.map _.TransactionId; WorkerInFlight=workerHost |> Option.exists _.InFlight }
    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                listeners |> Seq.iter (fun (target,name,listener) -> target.removeEventListener(name,listener))
                listeners.Clear()
                workerHost |> Option.iter (fun value -> (value :> IDisposable).Dispose())
                (documentHost :> IDisposable).Dispose()
                root.remove()

module SvgStudio =
    let mount (container: HTMLElement) (options: SvgStudioOptions) (initialState: SvgAuthoringState) onChange =
        match SvgBrowser.mountDocument container options.MountNamespace initialState.Document with
        | Error error -> Error error
        | Ok documentHost ->
            let root: HTMLElement = document.createElement("section")
            root.className <- "fsgg-svg-studio"
            root.setAttribute("aria-label", options.AccessibleLabel)
            let toolbar: HTMLElement = document.createElement("div")
            toolbar.setAttribute("role", "toolbar")
            toolbar.setAttribute("aria-label", "SVG art tools")
            let buttons = ResizeArray<string * HTMLElement>()
            for name in [ "Rectangle"; "Ellipse"; "Polygon"; "Path"; "Undo"; "Redo" ] do
                let button: HTMLElement = document.createElement("button")
                button.setAttribute("type", "button")
                button.textContent <- name
                button.setAttribute("aria-label", name)
                toolbar.appendChild button |> ignore
                buttons.Add(name, button)
            let properties: HTMLElement = document.createElement("fieldset")
            properties.setAttribute("aria-label", "Selected element properties")
            let translateLabel: HTMLElement = document.createElement("label")
            translateLabel.textContent <- "Translate X"
            let translateInput: HTMLInputElement = document.createElement("input") :?> HTMLInputElement
            translateInput.setAttribute("inputmode", "decimal")
            translateInput.setAttribute("aria-label", "Translate X")
            translateLabel.appendChild translateInput |> ignore
            let applyTranslation: HTMLElement = document.createElement("button")
            applyTranslation.textContent <- "Apply translation"
            applyTranslation.setAttribute("type", "button")
            applyTranslation.setAttribute("aria-label", "Apply translation")
            let selection: HTMLElement = document.createElement("output")
            selection.id <- "fsgg-svg-studio-selection"
            selection.setAttribute("aria-label", "Current selection")
            selection.textContent <- "No selected elements"
            let points: HTMLElement = document.createElement("ol")
            points.setAttribute("aria-label", "Path points and handles")
            let status: HTMLElement = document.createElement("div")
            status.id <- "fsgg-svg-studio-status"
            status.setAttribute("role", "status")
            status.setAttribute("aria-live", "polite")
            status.setAttribute("aria-atomic", "true")
            properties.appendChild translateLabel |> ignore
            properties.appendChild applyTranslation |> ignore
            properties.appendChild selection |> ignore
            properties.appendChild points |> ignore
            properties.appendChild status |> ignore
            root.appendChild toolbar |> ignore
            root.appendChild properties |> ignore
            buttons
            |> Seq.iter (fun (name, button) ->
                if name = "Rectangle" then
                    button.setAttribute("aria-describedby", "fsgg-svg-studio-selection fsgg-svg-studio-status"))
            applyTranslation.setAttribute("aria-describedby", "fsgg-svg-studio-selection fsgg-svg-studio-status")
            container.insertBefore(root, documentHost.Root) |> ignore
            let worker = options.WorkerFactory |> Option.map (fun factory -> new SvgGeometryWorkerHost(factory))
            let host = new SvgStudioHost(root, documentHost, initialState, SvgArt.initialState, worker, onChange)
            let announce (message: string) = status.textContent <- message
            let choose (id: string) (label: string) =
                host.SetSelection [id] |> ignore
                selection.textContent <- "Selected element: " + id
                announce (label + " created and selected")
            let create (name: string) =
                let id = name.ToLowerInvariant() + "-" + string (host.State.Revision + 1)
                let primitive =
                    match name with
                    | "Rectangle" -> SvgArtPrimitive.Rectangle {X=8.0;Y=8.0;Width=24.0;Height=16.0}
                    | "Ellipse" -> SvgArtPrimitive.Ellipse {X=8.0;Y=8.0;Width=24.0;Height=16.0}
                    | "Polygon" -> SvgArtPrimitive.Polygon [{X=8.0;Y=24.0};{X=20.0;Y=8.0};{X=32.0;Y=24.0}]
                    | _ -> SvgArtPrimitive.Path {Commands=[PathCommand.MoveTo {X=8.0;Y=24.0};PathCommand.QuadTo({X=20.0;Y=0.0},{X=32.0;Y=24.0});PathCommand.Close];FillType=PathFillType.Winding}
                match SvgArt.create id primitive SvgDocument.defaultPresentation host.State.Document with
                | Error error -> announce (sprintf "Validation error: %A" error)
                | Ok candidate ->
                    let transaction = {Schema=SvgAuthoring.transactionSchema;Id="create-"+id;Operations=[SvgAuthoringOperation.ReplaceDocument candidate]}
                    match host.Preview transaction |> Result.bind (fun () -> host.CommitGesture transaction.Id) with
                    | Ok () -> choose id name
                    | Error error -> announce (sprintf "Validation error: %A" error)
            buttons |> Seq.iter (fun (name, button) ->
                button.addEventListener("click", fun _ ->
                    match name with
                    | "Undo" -> host.Undo() |> Result.iter (fun () -> selection.textContent <- "No selected elements"; announce "Undo completed")
                    | "Redo" -> host.Redo() |> Result.iter (fun () -> announce "Redo completed")
                    | _ -> create name))
            applyTranslation.addEventListener("click", fun _ ->
                match Double.TryParse(translateInput.value, NumberStyles.Float, CultureInfo.InvariantCulture) with
                | true, value when not (Double.IsNaN value || Double.IsInfinity value) && not host.ToolState.Selection.IsEmpty ->
                    match host.ApplyNumericTransform("translate-x-" + string (host.State.Revision + 1), SvgAffine.translate value 0.0) with
                    | Ok () -> announce "Translation applied to current selection"
                    | Error error -> announce (sprintf "Validation error: %A" error)
                | _ -> announce "Validation error: enter a finite translation for the current selection")
            Ok host

    let activateFont resource =
        match SvgResourceInterchange.notoSansLatin400 resource.Base64 with
        | Ok accepted when accepted = resource -> Ok(new SvgFontResourceHost(resource))
        | Ok _ -> Error [{Code="unapproved-font-manifest";Location="/font";Message="font metadata does not match the verified Noto Sans resource"}]
        | Error issues -> Error issues
