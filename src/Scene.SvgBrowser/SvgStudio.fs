namespace FS.GG.UI.Scene.SvgBrowser

open System
open System.Globalization
open Browser.Dom
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open FS.GG.UI.Scene

type SvgStudioOptions =
    {
        MountNamespace: string
        AccessibleLabel: string
        WorkerFactory: (unit -> obj) option
    }

type SvgStudioObservation =
    {
        Revision: int
        SelectionCount: int
        OwnedListenerCount: int
        ActiveGesture: string option
        WorkerInFlight: bool
    }

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
    let contours (_value: obj) : Point array array = jsNative

    [<Emit("$0.error ?? null")>]
    let error (_value: obj) : string option = jsNative

[<Sealed>]
type SvgFontResourceHost internal (resource: SvgFontResource) =
    let url = StudioInterop.fontUrl resource.Base64
    let face = StudioInterop.fontFace resource.Family url
    let mutable ready = false
    let mutable diagnostic = None
    let mutable disposed = false

    do
        StudioInterop.loadFont
            face
            (fun () ->
                if not disposed then
                    ready <- true)
            (fun error ->
                if not disposed then
                    diagnostic <- Some error)

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
        if worker.IsSome then
            Error "one geometry operation is already in flight; requests are not queued"
        elif prepared.EncodedRequest.Length > 1048576 then
            Error "encoded geometry request exceeds 1 MiB"
        else
            let value = factory ()
            worker <- Some value

            StudioInterop.onMessage value (fun event ->
                let payload = StudioInterop.data event

                if StudioInterop.encodedSize payload > 1048576 then
                    finish ()
                    onError "encoded geometry result exceeds 1 MiB"
                else
                    match StudioInterop.error payload with
                    | Some message ->
                        finish ()
                        onError message
                    | None ->
                        let contours =
                            StudioInterop.contours payload |> Array.map Array.toList |> Array.toList

                        let result =
                            {
                                OperationId = StudioInterop.operationId payload
                                AcceptedRevision = StudioInterop.revision payload
                                InputContentHash = StudioInterop.hash payload
                                Contours = contours
                            }

                        finish ()
                        onResult result)

            StudioInterop.onError value (fun _ ->
                finish ()
                onError "geometry worker failed")

            timeout <-
                Some(
                    window.setTimeout (
                        (fun () ->
                            finish ()
                            onError "geometry worker exceeded the 2 second deadline"),
                        2000
                    )
                )

            StudioInterop.postMessage value (JS.JSON.parse prepared.EncodedRequest)
            Ok()

    member _.Cancel() = finish ()

    interface IDisposable with
        member _.Dispose() = finish ()

[<Sealed>]
type SvgStudioHost
    (
        root: HTMLElement,
        documentHost: SvgDocumentBrowserHost,
        initial: SvgAuthoringState,
        initialTool: SvgArtState,
        initialWorkspace: SvgWorkspaceState,
        workerHost: SvgGeometryWorkerHost option,
        onChange: SvgAuthoringState -> unit
    ) =
    let mutable state = initial
    let mutable tool = initialTool
    let mutable workspace = initialWorkspace
    let listeners = ResizeArray<EventTarget * string * (Event -> unit)>()
    let mutable disposed = false

    let rec documentIds elements =
        elements
        |> List.collect (fun (element: SvgElement) ->
            element.Id
            :: (match element.Content with
                | SvgElementContent.Group children -> documentIds children
                | _ -> []))

    let applyCamera () =
        let camera = tool.Camera

        documentHost.Root.setAttribute (
            "style",
            $"transform-origin:0 0;transform:matrix({camera.A},{camera.B},{camera.C},{camera.D},{camera.E},{camera.F})"
        )

    let sync next =
        state <- next

        let displayed =
            next.Preview
            |> Option.map (fun preview -> preview.Candidate.Document)
            |> Option.defaultValue next.Document

        documentHost.Replace displayed |> ignore
        let known = documentIds next.Document.Children |> Set.ofList

        tool <-
            { tool with
                Selection = tool.Selection |> List.filter known.Contains
            }

        applyCamera ()
        onChange state

    let cancelActive () =
        match state.Preview with
        | Some preview -> SvgAuthoring.cancelPreview preview.TransactionId state |> Result.iter sync
        | None -> ()

    let show selector visible =
        match root.querySelector selector with
        | null -> ()
        | element ->
            if visible then
                element.removeAttribute ("hidden")
            else
                element.setAttribute ("hidden", "")

    let syncWorkspaceDom () =
        match root.querySelector ("[data-fsgg-workspace-mode-status]") with
        | null -> ()
        | element -> element.textContent <- $"Mode: {workspace.Mode}"

        for mode in [ "Create"; "Arrange"; "Play"; "Review" ] do
            match root.querySelector ($"[data-fsgg-workspace-mode='{mode}']") with
            | null -> ()
            | element ->
                element.setAttribute ("aria-pressed", (string (string workspace.Mode = mode)).ToLowerInvariant())

        show
            "[data-fsgg-workspace-overlay='palette']"
            (match workspace.Overlay with
             | Some(SvgWorkspaceOverlay.CommandPalette _) -> true
             | _ -> false)

        show
            "[data-fsgg-workspace-overlay='help']"
            (match workspace.Overlay with
             | Some(SvgWorkspaceOverlay.PossibleInputHelp _) -> true
             | _ -> false)

        show
            "[data-fsgg-workspace-overlay='rebind']"
            (match workspace.Overlay with
             | Some(SvgWorkspaceOverlay.RebindCommand _) -> true
             | _ -> false)

    let transitionWorkspace message =
        let next, effects = SvgWorkspace.update message workspace
        workspace <- next
        syncWorkspaceDom ()

        effects
        |> List.iter (function
            | SvgWorkspaceEffect.RequestFocus target ->
                let local =
                    match target with
                    | "workspace.command-palette" ->
                        root.querySelector ("[data-fsgg-focus-target='workspace.command-palette']")
                    | "workspace.possible-input-help" ->
                        root.querySelector ("[data-fsgg-focus-target='workspace.possible-input-help']")
                    | "workspace.rebind" -> root.querySelector ("[data-fsgg-focus-target='workspace.rebind']")
                    | _ -> null

                if isNull local then
                    match document.getElementById target with
                    | null -> ()
                    | element -> element.focus ()
                else
                    (local :?> HTMLElement).focus()
            | _ -> ())

        effects

    let listen (target: EventTarget) name (handler: Event -> unit) =
        target.addEventListener (name, handler)
        listeners.Add(target, name, handler)

    do
        syncWorkspaceDom ()

        listen (root :> EventTarget) "keydown" (fun event ->
            let key = event :?> KeyboardEvent

            if key.key = "Escape" then
                match workspace.Overlay with
                | Some _ -> transitionWorkspace SvgWorkspaceMessage.CloseOverlay |> ignore
                | None -> cancelActive ())

        for name in [ "pointercancel"; "lostpointercapture" ] do
            listen (documentHost.Root :> EventTarget) name (fun _ -> cancelActive ())

        listen (window :> EventTarget) "blur" (fun _ -> cancelActive ())

    member _.Root = root
    member _.State = state
    member _.ToolState = tool
    member _.WorkspaceState = workspace
    member _.UpdateWorkspace message = transitionWorkspace message
    member _.GeometryWorker = workerHost

    member _.SetSelection elementIds =
        let known = documentIds state.Document.Children |> Set.ofList

        match elementIds |> List.tryFind (known.Contains >> not) with
        | Some missing -> Error(SvgArtError.MissingElement missing)
        | None ->
            tool <-
                { tool with
                    Selection = List.distinct elementIds
                }

            Ok()

    member _.SetCamera camera =
        let determinant = camera.A * camera.D - camera.B * camera.C

        if not (SvgAffine.isFinite camera) || abs determinant < 0.000000001 then
            Error(SvgArtError.InvalidSelection "camera must be finite and invertible")
        else
            tool <- { tool with Camera = camera }
            applyCamera ()
            Ok()

    member _.Pick(screenPoint: Point) =
        let camera = tool.Camera
        let determinant = camera.A * camera.D - camera.B * camera.C

        if not (SvgAffine.isFinite camera) || abs determinant < 0.000000001 then
            Error(SvgArtError.InvalidSelection "camera must be finite and invertible")
        else
            let x = screenPoint.X - camera.E
            let y = screenPoint.Y - camera.F

            let local =
                {
                    X = (camera.D * x - camera.C * y) / determinant
                    Y = (-camera.B * x + camera.A * y) / determinant
                }

            Ok(documentHost.HitTestBounds local)

    member _.Preview transaction =
        SvgAuthoring.preview state.Revision transaction state |> Result.map sync

    member _.CommitGesture transactionId =
        SvgAuthoring.commitPreview state.Revision transactionId state |> Result.map sync

    member _.CancelGesture transactionId =
        SvgAuthoring.cancelPreview transactionId state |> Result.map sync

    member _.Undo() =
        SvgAuthoring.undo state.Revision state |> Result.map sync

    member _.Redo() =
        SvgAuthoring.redo state.Revision state |> Result.map sync

    member _.CommitDocument(transactionId, candidate) =
        let transaction =
            {
                Schema = SvgAuthoring.transactionSchema
                Id = transactionId
                Operations = [ SvgAuthoringOperation.ReplaceDocument candidate ]
            }

        SvgAuthoring.commit state.Revision transaction state |> Result.map sync

    member _.SetGrid(transactionId, grid) =
        let metadata = { state.Metadata with Grid = grid }

        let transaction =
            {
                Schema = SvgAuthoring.transactionSchema
                Id = transactionId
                Operations = [ SvgAuthoringOperation.ReplaceSceneMetadata metadata ]
            }

        SvgAuthoring.commit state.Revision transaction state |> Result.map sync

    member _.ApplyNumericTransform(transactionId, transform) =
        let transaction =
            {
                Schema = SvgAuthoring.transactionSchema
                Id = transactionId
                Operations = [ SvgAuthoringOperation.TransformElements(tool.Selection, transform) ]
            }

        SvgAuthoring.commit state.Revision transaction state |> Result.map sync

    member _.CommitGeometry(prepared, result) =
        if prepared.Request.AcceptedRevision <> state.Revision then
            Error(
                SvgArtError.InvalidInput
                    [
                        {
                            Code = "stale-geometry-result"
                            Location = "/acceptedRevision"
                            Message = "geometry result no longer names the accepted authoring revision"
                        }
                    ]
            )
        else
            SvgGeometry.transaction result prepared state.Document
            |> Result.bind (fun transaction ->
                match SvgAuthoring.commit state.Revision transaction state with
                | Ok accepted ->
                    sync accepted
                    Ok()
                | Error error -> Error(SvgArtError.InvalidSelection(sprintf "%A" error)))

    member _.Observe() =
        {
            Revision = state.Revision
            SelectionCount = tool.Selection.Length
            OwnedListenerCount = listeners.Count
            ActiveGesture = state.Preview |> Option.map _.TransactionId
            WorkerInFlight = workerHost |> Option.exists _.InFlight
        }

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true

                listeners
                |> Seq.iter (fun (target, name, listener) -> target.removeEventListener (name, listener))

                listeners.Clear()
                workerHost |> Option.iter (fun value -> (value :> IDisposable).Dispose())
                (documentHost :> IDisposable).Dispose()
                root.remove ()

module SvgStudio =
    let mount (container: HTMLElement) (options: SvgStudioOptions) (initialState: SvgAuthoringState) onChange =
        match SvgBrowser.mountDocument container options.MountNamespace initialState.Document with
        | Error error -> Error error
        | Ok documentHost ->
            let sceneFocus = options.MountNamespace + "--scene"
            let root: HTMLElement = document.createElement ("section")
            root.className <- "fsgg-svg-studio"
            root.setAttribute ("aria-label", options.AccessibleLabel)
            let toolbar: HTMLElement = document.createElement ("div")
            toolbar.setAttribute ("role", "toolbar")
            toolbar.setAttribute ("aria-label", "SVG art tools")
            let buttons = ResizeArray<string * HTMLElement>()

            for name in
                [
                    "Region"
                    "Boundary"
                    "Object"
                    "Rectangle"
                    "Ellipse"
                    "Polygon"
                    "Path"
                    "Group"
                    "Ungroup"
                    "Align left"
                    "Bring forward"
                    "Apply style"
                    "Grid snapping"
                    "Freeform"
                    "Undo"
                    "Redo"
                ] do
                let button: HTMLElement = document.createElement ("button")
                button.setAttribute ("type", "button")
                button.textContent <- name
                button.setAttribute ("aria-label", name)
                toolbar.appendChild button |> ignore
                buttons.Add(name, button)

            let properties: HTMLElement = document.createElement ("fieldset")
            properties.setAttribute ("aria-label", "Selected element properties")
            let translateLabel: HTMLElement = document.createElement ("label")
            translateLabel.textContent <- "Translate X"

            let translateInput: HTMLInputElement =
                document.createElement ("input") :?> HTMLInputElement

            translateInput.setAttribute ("inputmode", "decimal")
            translateInput.setAttribute ("aria-label", "Translate X")
            translateLabel.appendChild translateInput |> ignore
            let applyTranslation: HTMLElement = document.createElement ("button")
            applyTranslation.textContent <- "Apply translation"
            applyTranslation.setAttribute ("type", "button")
            applyTranslation.setAttribute ("aria-label", "Apply translation")
            let selection: HTMLElement = document.createElement ("output")
            selection.id <- "fsgg-svg-studio-selection"
            selection.setAttribute ("aria-label", "Current selection")
            selection.textContent <- "No selected elements"
            let points: HTMLElement = document.createElement ("ol")
            points.setAttribute ("aria-label", "Path points and handles")
            let status: HTMLElement = document.createElement ("div")
            status.id <- "fsgg-svg-studio-status"
            status.setAttribute ("role", "status")
            status.setAttribute ("aria-live", "polite")
            status.setAttribute ("aria-atomic", "true")
            properties.appendChild translateLabel |> ignore
            properties.appendChild applyTranslation |> ignore
            properties.appendChild selection |> ignore
            properties.appendChild points |> ignore
            properties.appendChild status |> ignore
            root.appendChild toolbar |> ignore
            root.appendChild properties |> ignore
            let workspaceToolbar: HTMLElement = document.createElement ("div")
            workspaceToolbar.setAttribute ("role", "toolbar")
            workspaceToolbar.setAttribute ("aria-label", "Workspace controls")
            let workspaceButtons = ResizeArray<string * HTMLElement>()

            for name in [ "Create"; "Arrange"; "Play"; "Review" ] do
                let button: HTMLElement = document.createElement ("button")
                button.setAttribute ("type", "button")
                button.setAttribute ("aria-label", name + " mode")
                button.setAttribute ("data-fsgg-workspace-mode", name)
                button.textContent <- name
                workspaceToolbar.appendChild button |> ignore
                workspaceButtons.Add(name, button)

            let overlayButtons = ResizeArray<string * HTMLElement>()

            for key, label in
                [
                    "palette", "Open command palette"
                    "help", "Open possible input help"
                    "rebind", "Rebind selected command"
                ] do
                let button: HTMLElement = document.createElement ("button")
                button.setAttribute ("type", "button")
                button.setAttribute ("aria-label", label)
                button.textContent <- label
                workspaceToolbar.appendChild button |> ignore
                overlayButtons.Add(key, button)

            let modeStatus: HTMLElement = document.createElement ("output")
            modeStatus.setAttribute ("role", "status")
            modeStatus.setAttribute ("aria-live", "polite")
            modeStatus.setAttribute ("data-fsgg-workspace-mode-status", "")
            workspaceToolbar.appendChild modeStatus |> ignore
            root.appendChild workspaceToolbar |> ignore

            let overlay key label target content =
                let panel: HTMLElement = document.createElement ("section")
                panel.setAttribute ("role", "dialog")
                panel.setAttribute ("aria-modal", "true")
                panel.setAttribute ("aria-label", label)
                panel.setAttribute ("data-fsgg-workspace-overlay", key)
                panel.setAttribute ("data-fsgg-focus-target", target)
                panel.setAttribute ("tabindex", "-1")
                panel.setAttribute ("hidden", "")
                panel.textContent <- content
                root.appendChild panel |> ignore
                panel

            let palette =
                overlay
                    "palette"
                    "Command palette"
                    "workspace.command-palette"
                    "Command palette. Available workspace commands."

            let help =
                overlay
                    "help"
                    "Possible input help"
                    "workspace.possible-input-help"
                    "Possible input help. Shortcuts update with the current mode."

            let rebind =
                overlay
                    "rebind"
                    "Rebind command"
                    "workspace.rebind"
                    "Rebind command. Press a new input. Conflict feedback: no displacement."

            buttons
            |> Seq.iter (fun (name, button) ->
                if name = "Rectangle" then
                    button.setAttribute ("aria-describedby", "fsgg-svg-studio-selection fsgg-svg-studio-status"))

            applyTranslation.setAttribute ("aria-describedby", "fsgg-svg-studio-selection fsgg-svg-studio-status")
            container.insertBefore (root, documentHost.Root) |> ignore
            documentHost.Root.id <- sceneFocus
            documentHost.Root.setAttribute ("tabindex", "0")

            let worker =
                options.WorkerFactory
                |> Option.map (fun factory -> new SvgGeometryWorkerHost(factory))

            let workspace =
                { SvgWorkspace.init SvgWorkspaceMode.Create window.innerWidth with
                    FocusTarget = sceneFocus
                }

            let host =
                new SvgStudioHost(root, documentHost, initialState, SvgArt.initialState, workspace, worker, onChange)

            workspaceButtons
            |> Seq.iter (fun (name, button) ->
                button.addEventListener (
                    "click",
                    fun _ ->
                        let mode =
                            match name with
                            | "Arrange" -> SvgWorkspaceMode.Arrange
                            | "Play" -> SvgWorkspaceMode.Play
                            | "Review" -> SvgWorkspaceMode.Review
                            | _ -> SvgWorkspaceMode.Create

                        host.UpdateWorkspace(SvgWorkspaceMessage.SetMode mode) |> ignore
                ))

            overlayButtons
            |> Seq.iter (fun (key, button) ->
                button.addEventListener (
                    "click",
                    fun _ ->
                        let message =
                            match key with
                            | "help" -> SvgWorkspaceMessage.OpenHelp button.id
                            | "rebind" -> SvgWorkspaceMessage.BeginRebind("workspace.selected", button.id)
                            | _ -> SvgWorkspaceMessage.OpenPalette button.id

                        host.UpdateWorkspace message |> ignore
                ))

            for index, (_, button) in workspaceButtons |> Seq.indexed do
                button.id <- $"{options.MountNamespace}--workspace-mode-{index}"

            for key, button in overlayButtons do
                button.id <- $"{options.MountNamespace}--workspace-{key}"

            let announce (message: string) = status.textContent <- message

            let choose (id: string) (label: string) =
                host.SetSelection [ id ] |> ignore
                selection.textContent <- "Selected element: " + id
                announce (label + " created and selected")

            let create (name: string) =
                let id =
                    name.ToLowerInvariant().Replace(" ", "-")
                    + "-"
                    + string (host.State.Revision + 1)

                let place point =
                    match host.State.Metadata.Grid with
                    | Some grid -> SvgScenePlacement.grid grid point |> Result.defaultValue point
                    | None -> SvgScenePlacement.freeform point |> Result.defaultValue point

                let start = place { X = 8.25; Y = 8.25 }

                let primitive =
                    match name with
                    | "Region"
                    | "Rectangle" ->
                        SvgArtPrimitive.Rectangle
                            {
                                X = start.X
                                Y = start.Y
                                Width = 24.0
                                Height = 16.0
                            }
                    | "Object"
                    | "Ellipse" ->
                        SvgArtPrimitive.Ellipse
                            {
                                X = start.X
                                Y = start.Y
                                Width = 24.0
                                Height = 16.0
                            }
                    | "Boundary"
                    | "Polygon" ->
                        SvgArtPrimitive.Polygon
                            [
                                place { X = 8.25; Y = 24.25 }
                                place { X = 20.25; Y = 8.25 }
                                place { X = 32.25; Y = 24.25 }
                            ]
                    | _ ->
                        SvgArtPrimitive.Path
                            {
                                Commands =
                                    [
                                        PathCommand.MoveTo(place { X = 8.25; Y = 24.25 })
                                        PathCommand.QuadTo(
                                            place { X = 20.25; Y = 0.25 },
                                            place { X = 32.25; Y = 24.25 }
                                        )
                                        PathCommand.Close
                                    ]
                                FillType = PathFillType.Winding
                            }

                match SvgArt.create id primitive SvgDocument.defaultPresentation host.State.Document with
                | Error error -> announce (sprintf "Validation error: %A" error)
                | Ok candidate ->
                    let transaction =
                        {
                            Schema = SvgAuthoring.transactionSchema
                            Id = "create-" + id
                            Operations = [ SvgAuthoringOperation.ReplaceDocument candidate ]
                        }

                    match
                        host.Preview transaction
                        |> Result.bind (fun () -> host.CommitGesture transaction.Id)
                    with
                    | Ok() -> choose id name
                    | Error error -> announce (sprintf "Validation error: %A" error)

            let commitArt (label: string) (result: Result<SvgDocument, SvgArtError>) =
                match result with
                | Error error -> announce (sprintf "Validation error: %A" error)
                | Ok candidate ->
                    match
                        host.CommitDocument(
                            label.ToLowerInvariant().Replace(" ", "-")
                            + "-"
                            + string (host.State.Revision + 1),
                            candidate
                        )
                    with
                    | Ok() -> announce (label + " completed")
                    | Error error -> announce (sprintf "Validation error: %A" error)

            buttons
            |> Seq.iter (fun (name, button) ->
                button.addEventListener (
                    "click",
                    fun _ ->
                        match name with
                        | "Undo" ->
                            host.Undo()
                            |> Result.iter (fun () ->
                                selection.textContent <- "No selected elements"
                                announce "Undo completed")
                        | "Redo" -> host.Redo() |> Result.iter (fun () -> announce "Redo completed")
                        | "Group" ->
                            let id = "group-" + string (host.State.Revision + 1)

                            match SvgArt.group id host.ToolState.Selection host.State.Document with
                            | Ok candidate ->
                                match
                                    host.CommitDocument("group-gesture-" + string (host.State.Revision + 1), candidate)
                                with
                                | Ok() ->
                                    host.SetSelection [ id ] |> ignore
                                    selection.textContent <- "Selected element: " + id
                                    announce "Group completed"
                                | Error error -> announce (sprintf "Validation error: %A" error)
                            | Error error -> announce (sprintf "Validation error: %A" error)
                        | "Ungroup" ->
                            match host.ToolState.Selection with
                            | [ id ] -> commitArt "Ungroup" (SvgArt.ungroup id host.State.Document)
                            | _ -> announce "Validation error: select one group"
                        | "Align left" ->
                            commitArt
                                "Align left"
                                (SvgArt.align SvgArtAlignment.Left host.ToolState.Selection host.State.Document)
                        | "Bring forward" ->
                            match host.ToolState.Selection with
                            | id :: _ ->
                                commitArt
                                    "Bring forward"
                                    (SvgArt.reorder id SvgArtSiblingOrder.Forward host.State.Document)
                            | [] -> announce "Validation error: select an element"
                        | "Apply style" ->
                            let presentation =
                                { SvgDocument.defaultPresentation with
                                    FillSource =
                                        Some(
                                            SvgPaintSource.Solid
                                                {
                                                    Red = 40uy
                                                    Green = 110uy
                                                    Blue = 220uy
                                                    Alpha = 255uy
                                                }
                                        )
                                }

                            commitArt
                                "Apply style"
                                (SvgArt.setPresentation host.ToolState.Selection presentation host.State.Document)
                        | "Grid snapping" ->
                            host.SetGrid(
                                "grid-" + string (host.State.Revision + 1),
                                Some
                                    {
                                        Origin = { X = 3.0; Y = 5.0 }
                                        Step = { X = 8.0; Y = 8.0 }
                                    }
                            )
                            |> Result.iter (fun () -> announce "Grid snapping enabled")
                        | "Freeform" ->
                            host.SetGrid("freeform-" + string (host.State.Revision + 1), None)
                            |> Result.iter (fun () -> announce "Freeform placement enabled")
                        | _ -> create name
                ))

            applyTranslation.addEventListener (
                "click",
                fun _ ->
                    match Double.TryParse(translateInput.value, NumberStyles.Float, CultureInfo.InvariantCulture) with
                    | true, value when
                        not (Double.IsNaN value || Double.IsInfinity value)
                        && not host.ToolState.Selection.IsEmpty
                        ->
                        match
                            host.ApplyNumericTransform(
                                "translate-x-" + string (host.State.Revision + 1),
                                SvgAffine.translate value 0.0
                            )
                        with
                        | Ok() -> announce "Translation applied to current selection"
                        | Error error -> announce (sprintf "Validation error: %A" error)
                    | _ -> announce "Validation error: enter a finite translation for the current selection"
            )

            Ok host

    let activateFont resource =
        match SvgResourceInterchange.notoSansLatin400 resource.Base64 with
        | Ok accepted when accepted = resource -> Ok(new SvgFontResourceHost(resource))
        | Ok _ ->
            Error
                [
                    {
                        Code = "unapproved-font-manifest"
                        Location = "/font"
                        Message = "font metadata does not match the verified Noto Sans resource"
                    }
                ]
        | Error issues -> Error issues
