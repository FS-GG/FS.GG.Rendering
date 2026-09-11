namespace FS.GG.UI.Scene.SvgBrowser

open System
open System.Collections.Generic
open Browser.Dom
open Browser.Types
open Fable.Core
open FS.GG.UI.Scene

module private Dom =
    [<Literal>]
    let SvgNamespace = "http://www.w3.org/2000/svg"

    [<Emit("$0.focus({ preventScroll: true })")>]
    let focus (_element: Element) : unit = jsNative

    [<Emit("$0.setPointerCapture($1)")>]
    let capturePointer (_element: Element) (_pointerId: int) : unit = jsNative

    [<Emit("$0.releasePointerCapture($1)")>]
    let releasePointer (_element: Element) (_pointerId: int) : unit = jsNative

    [<Emit("$0.hasPointerCapture($1)")>]
    let hasPointerCapture (_element: Element) (_pointerId: int) : bool = jsNative

    [<Emit("document.elementFromPoint($0, $1)?.closest('[data-scene-object-id]')?.getAttribute('data-scene-object-id') ?? null")>]
    let objectIdAtClientPoint (_clientX: float) (_clientY: float) : string option = jsNative

    let create name = document.createElementNS(SvgNamespace, name)

    let set name value (element: Element) = element.setAttribute(name, value)

    let clear (element: Element) = element.innerHTML <- ""

    [<Emit("document.importNode(new DOMParser().parseFromString($0, 'image/svg+xml').documentElement, true)")>]
    let parseExportedSvg (_svg: string) : Element = jsNative

    // Keep the source expression single-evaluation-safe. Fable substitutes Emit arguments
    // textually, so a while expression would reparse an exported document on every test/body.
    [<Emit("Array.from($1.childNodes).forEach(node => $0.appendChild(node))")>]
    let appendChildren (_parent: Element) (_source: Element) : unit = jsNative

    [<Emit("document.fonts.check('16px ' + JSON.stringify($0))")>]
    let fontReady (_family: string) : bool = jsNative

module private Format =
    let number (value: float) = string value

    let color (value: Color) =
        let alpha = float value.Alpha / 255.0
        $"rgba({value.Red},{value.Green},{value.Blue},{number alpha})"

module private Geometry =
    let containsRect (point: Point) (rect: Rect) =
        let minX = min rect.X (rect.X + rect.Width)
        let maxX = max rect.X (rect.X + rect.Width)
        let minY = min rect.Y (rect.Y + rect.Height)
        let maxY = max rect.Y (rect.Y + rect.Height)
        point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY

    let containsEllipse (point: Point) (rect: Rect) =
        let radiusX = abs rect.Width / 2.0
        let radiusY = abs rect.Height / 2.0
        if radiusX = 0.0 || radiusY = 0.0 then false
        else
            let centerX = rect.X + rect.Width / 2.0
            let centerY = rect.Y + rect.Height / 2.0
            let dx = (point.X - centerX) / radiusX
            let dy = (point.Y - centerY) / radiusY
            dx * dx + dy * dy <= 1.0

    let containsLine (point: Point) (startPoint: Point) (endPoint: Point) width =
        let vx = endPoint.X - startPoint.X
        let vy = endPoint.Y - startPoint.Y
        let lengthSquared = vx * vx + vy * vy
        let t =
            if lengthSquared = 0.0 then 0.0
            else max 0.0 (min 1.0 (((point.X - startPoint.X) * vx + (point.Y - startPoint.Y) * vy) / lengthSquared))
        let closestX = startPoint.X + t * vx
        let closestY = startPoint.Y + t * vy
        let dx = point.X - closestX
        let dy = point.Y - closestY
        dx * dx + dy * dy <= max 1.0 (width / 2.0) ** 2.0

    let rec containsScene (point: Point) (scene: Scene) =
        scene.Nodes |> List.rev |> List.exists (containsNode point)

    and containsNode (point: Point) node =
        match node with
        | SceneNode.Empty -> false
        | SceneNode.Group scenes -> scenes |> List.rev |> List.exists (containsScene point)
        | SceneNode.Rectangle((x, y, width, height), _)
        | SceneNode.PaintedRectangle({ X = x; Y = y; Width = width; Height = height }, _) ->
            containsRect point { X = x; Y = y; Width = width; Height = height }
        | SceneNode.Circle(center, radius, _) ->
            let dx = point.X - center.X
            let dy = point.Y - center.Y
            dx * dx + dy * dy <= radius * radius
        | SceneNode.FilledEllipse(rect, _)
        | SceneNode.Ellipse(rect, _) -> containsEllipse point rect
        | SceneNode.Line(startPoint, endPoint, paint) ->
            let width = paint.Stroke |> Option.map (fun stroke -> stroke.Width) |> Option.defaultValue 1.0
            containsLine point startPoint endPoint width
        | SceneNode.Translate((x, y), child) ->
            containsScene { X = point.X - x; Y = point.Y - y } child
        | SceneNode.ColorSpaceNode(ColorSpace.Srgb, child) -> containsScene point child
        | _ -> false

module private Render =
    let append (parent: Element) (child: Element) = parent.appendChild(child) |> ignore

    let setRect (rect: Rect) element =
        Dom.set "x" (Format.number rect.X) element
        Dom.set "y" (Format.number rect.Y) element
        Dom.set "width" (Format.number rect.Width) element
        Dom.set "height" (Format.number rect.Height) element

    let setPaint isLine (paint: Paint) element =
        let color = paint.Fill |> Option.map Format.color |> Option.defaultValue "none"
        Dom.set "opacity" (Format.number paint.Opacity) element
        match paint.Stroke with
        | Some stroke ->
            Dom.set "fill" (if isLine then "none" else color) element
            Dom.set "stroke" color element
            Dom.set "stroke-width" (Format.number stroke.Width) element
            Dom.set "stroke-linecap" (match stroke.Cap with StrokeCap.Butt -> "butt" | StrokeCap.Round -> "round" | StrokeCap.Square -> "square") element
            Dom.set "stroke-linejoin" (match stroke.Join with StrokeJoin.Miter -> "miter" | StrokeJoin.RoundJoin -> "round" | StrokeJoin.Bevel -> "bevel") element
            Dom.set "stroke-miterlimit" (Format.number stroke.Miter) element
        | None ->
            Dom.set "fill" (if isLine then "none" else color) element
            if isLine then Dom.set "stroke" color element

    let pathData (path: PathSpec) =
        path.Commands
        |> List.map (function
            | PathCommand.MoveTo point -> $"M {Format.number point.X} {Format.number point.Y}"
            | PathCommand.LineTo point -> $"L {Format.number point.X} {Format.number point.Y}"
            | PathCommand.QuadTo(control, point) -> $"Q {Format.number control.X} {Format.number control.Y} {Format.number point.X} {Format.number point.Y}"
            | PathCommand.CubicTo(control1, control2, point) -> $"C {Format.number control1.X} {Format.number control1.Y} {Format.number control2.X} {Format.number control2.Y} {Format.number point.X} {Format.number point.Y}"
            | PathCommand.Close -> "Z"
            | PathCommand.ArcTo _ -> "")
        |> String.concat " "

    let rec scene parent (value: Scene) = value.Nodes |> List.iter (node parent)

    and node parent value =
        let add name configure =
            let element = Dom.create name
            configure element
            append parent element
        match value with
        | SceneNode.Empty -> ()
        | SceneNode.Group scenes ->
            add "g" (fun group -> scenes |> List.iter (scene group))
        | SceneNode.Rectangle((x, y, width, height), color) ->
            add "rect" (fun element ->
                setRect { X = x; Y = y; Width = width; Height = height } element
                Dom.set "fill" (Format.color color) element)
        | SceneNode.PaintedRectangle(rect, paint) ->
            add "rect" (fun element -> setRect rect element; setPaint false paint element)
        | SceneNode.Circle(center, radius, color) ->
            add "circle" (fun element ->
                Dom.set "cx" (Format.number center.X) element
                Dom.set "cy" (Format.number center.Y) element
                Dom.set "r" (Format.number radius) element
                Dom.set "fill" (Format.color color) element)
        | SceneNode.FilledEllipse(rect, color) ->
            add "ellipse" (fun element ->
                Dom.set "cx" (Format.number (rect.X + rect.Width / 2.0)) element
                Dom.set "cy" (Format.number (rect.Y + rect.Height / 2.0)) element
                Dom.set "rx" (Format.number (abs rect.Width / 2.0)) element
                Dom.set "ry" (Format.number (abs rect.Height / 2.0)) element
                Dom.set "fill" (Format.color color) element)
        | SceneNode.Ellipse(rect, paint) ->
            add "ellipse" (fun element ->
                Dom.set "cx" (Format.number (rect.X + rect.Width / 2.0)) element
                Dom.set "cy" (Format.number (rect.Y + rect.Height / 2.0)) element
                Dom.set "rx" (Format.number (abs rect.Width / 2.0)) element
                Dom.set "ry" (Format.number (abs rect.Height / 2.0)) element
                setPaint false paint element)
        | SceneNode.Line(startPoint, endPoint, paint) ->
            add "line" (fun element ->
                Dom.set "x1" (Format.number startPoint.X) element
                Dom.set "y1" (Format.number startPoint.Y) element
                Dom.set "x2" (Format.number endPoint.X) element
                Dom.set "y2" (Format.number endPoint.Y) element
                setPaint true paint element)
        | SceneNode.Path(path, paint) ->
            add "path" (fun element ->
                Dom.set "d" (pathData path) element
                Dom.set "fill-rule" (match path.FillType with PathFillType.Winding -> "nonzero" | PathFillType.EvenOdd -> "evenodd") element
                setPaint false paint element)
        | SceneNode.Text((x, y), text, color) ->
            add "text" (fun element ->
                Dom.set "x" (Format.number x) element
                Dom.set "y" (Format.number y) element
                Dom.set "fill" (Format.color color) element
                element.textContent <- text)
        | SceneNode.SizedText((x, y), text, size, color) ->
            add "text" (fun element ->
                Dom.set "x" (Format.number x) element
                Dom.set "y" (Format.number y) element
                Dom.set "font-size" (Format.number size) element
                Dom.set "fill" (Format.color color) element
                element.textContent <- text)
        | SceneNode.Translate((x, y), child) ->
            add "g" (fun group ->
                Dom.set "transform" $"translate({Format.number x} {Format.number y})" group
                scene group child)
        | SceneNode.ColorSpaceNode(ColorSpace.Srgb, child) -> scene parent child
        | _ -> ()

    let documentScene mountNamespace parent (value: Scene) =
        let document =
            { Schema = SvgDocument.schema
              Id = "retained-document"
              ViewBox = { X = 0.0; Y = 0.0; Width = 1.0; Height = 1.0 }
              Definitions = []
              Children =
                [ { Id = "content"
                    SemanticId = None
                    Visible = true
                    Transform = SvgAffine.identity
                    ClipId = None
                    MaskId = None
                    Presentation = None
                    Content = SvgElementContent.SceneLeaf value } ] }
        match SvgDocument.exportSvg mountNamespace document with
        | Error issues -> failwithf "validated retained Scene could not export: %A" issues
        | Ok svg -> Dom.appendChildren parent (Dom.parseExportedSvg svg)

type SvgBrowserOptions =
    { Width: float
      Height: float
      AccessibleLabel: string
      WheelZoomFactor: float }

[<RequireQualifiedAccess>]
type SvgBrowserMountError =
    | InvalidScene of RetainedInteractionError
    | InvalidOptions of string
    | InvalidDocument of SvgDocumentIssue list

[<RequireQualifiedAccess>]
type SvgDocumentBrowserError =
    | InvalidDocument of SvgDocumentIssue list
    | DuplicateMountNamespace of string

type SvgBrowserFontObservation =
    { DefinitionId: string
      Family: string
      Ready: bool
      Diagnostic: string option }

type SvgBrowserObservation =
    { RootId: string
      Revision: int
      LayerCount: int
      ObjectCount: int
      SvgNodeCount: int
      OwnedListenerCount: int
      ScheduledFrameCount: int }

module private DocumentRegistry =
    let activeNamespaces = HashSet<string>()

[<Sealed>]
type SvgDocumentBrowserHost internal
    (container: HTMLElement,
     mountNamespace: string,
     initialDocument: SvgDocument,
     initialSvg: string,
     initialRoot: Element) =

    let mutable documentValue = initialDocument
    let mutable exportedSvg = initialSvg
    let mutable root = initialRoot
    let mutable disposed = false

    let fontObservations () =
        documentValue.Definitions
        |> List.choose (fun definition ->
            match definition.Content with
            | SvgDefinitionContent.Font font ->
                let ready = Dom.fontReady font.Family
                Some
                    { DefinitionId = definition.Id
                      Family = font.Family
                      Ready = ready
                      Diagnostic = if ready then None else Some $"font-unavailable:{definition.Id}:{font.Family}" }
            | _ -> None)

    member _.Root = root
    member _.MountNamespace = mountNamespace
    member _.Document = documentValue
    member _.ExportedSvg = exportedSvg
    member _.ObserveFonts() = fontObservations ()
    member _.Replace(document: SvgDocument) =
        if disposed then invalidOp "The SVG document browser host is disposed."
        match SvgDocument.exportSvg mountNamespace document with
        | Error issues -> Error(SvgDocumentBrowserError.InvalidDocument issues)
        | Ok candidateSvg ->
            // Parsing happens only after portable validation/export succeeds, and replacement is the
            // final operation so invalid candidates cannot partially mutate the live document.
            let candidateRoot = Dom.parseExportedSvg candidateSvg
            container.replaceChild(candidateRoot, root) |> ignore
            root <- candidateRoot
            documentValue <- document
            exportedSvg <- candidateSvg
            Ok()

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                if root.parentNode = container then container.removeChild(root) |> ignore
                DocumentRegistry.activeNamespaces.Remove mountNamespace |> ignore
                disposed <- true

[<Sealed>]
type SvgBrowserHost internal
    (container: HTMLElement,
     root: Element,
     viewport: Element,
     initialState: RetainedInteractionState,
     options: SvgBrowserOptions,
     onTransition: RetainedInteractionResult -> unit) =

    let layers = Dictionary<string, Element>()
    let objects = Dictionary<string, Element>()
    let listeners = ResizeArray<string * (Event -> unit)>()
    let mutable state = initialState
    let mutable disposed = false
    let mutable lastPointer: Point option = None

    let selectableObject id =
        state.Scene.Layers
        |> List.collect (fun layer -> layer.Objects)
        |> List.tryFind (fun candidate -> candidate.Id = id && candidate.Selectable)

    let syncInteraction () =
        for KeyValue(id, element) in objects do
            match selectableObject id with
            | Some value ->
                Dom.set "role" "button" element
                Dom.set "tabindex" (if state.FocusedObjectId = Some id then "0" else "-1") element
                Dom.set "aria-label" value.AccessibleLabel element
                Dom.set "aria-selected" (if state.SelectedObjectId = Some id then "true" else "false") element
                Dom.set "data-selected" (if state.SelectedObjectId = Some id then "true" else "false") element
            | None ->
                element.removeAttribute("role")
                element.removeAttribute("tabindex")
                element.removeAttribute("aria-selected")

    let reconcileScene () =
        Dom.set "id" state.Scene.RootId root
        Dom.set "data-scene-root-id" state.Scene.RootId root
        Dom.set "data-scene-revision" (string state.Scene.Revision) root
        let camera = state.Scene.Camera
        Dom.set "transform" $"translate({Format.number camera.PanX} {Format.number camera.PanY}) scale({Format.number camera.Zoom})" viewport

        let wantedLayers = state.Scene.Layers |> List.map (fun layer -> layer.Id) |> Set.ofList
        for id in layers.Keys |> Seq.toArray do
            if not (wantedLayers.Contains id) then
                layers[id].remove()
                layers.Remove id |> ignore

        objects.Clear()
        for layer in state.Scene.Layers do
            let layerElement =
                match layers.TryGetValue layer.Id with
                | true, existing -> existing
                | _ ->
                    let created = Dom.create "g"
                    Dom.set "data-scene-layer-id" layer.Id created
                    layers[layer.Id] <- created
                    created
            Dom.set "display" (if layer.Visible then "inline" else "none") layerElement
            Dom.clear layerElement
            for objectValue in layer.Objects do
                let objectElement = Dom.create "g"
                Dom.set "data-scene-object-id" objectValue.Id objectElement
                Render.documentScene $"{state.Scene.RootId}:{layer.Id}:{objectValue.Id}" objectElement objectValue.Content
                Render.append layerElement objectElement
                objects[objectValue.Id] <- objectElement
            Render.append viewport layerElement
        syncInteraction ()

    let apply message =
        if disposed then invalidOp "The SVG browser host is disposed."
        let result = SvgRetained.update message state
        state <- result.State
        if result.Error.IsNone then reconcileScene ()
        else syncInteraction ()
        onTransition result
        result

    let hitTest screenPoint =
        SvgRetained.tryToScenePoint state.Scene.Camera screenPoint
        |> Option.bind (fun scenePoint ->
            let bounds = root.getBoundingClientRect()
            let clientX = bounds.left + screenPoint.X * bounds.width / options.Width
            let clientY = bounds.top + screenPoint.Y * bounds.height / options.Height
            match Dom.objectIdAtClientPoint clientX clientY |> Option.filter (fun id -> selectableObject id |> Option.isSome) with
            | Some id -> Some id
            | None ->
                state.Scene.Layers
                |> List.rev
                |> List.filter (fun layer -> layer.Visible)
                |> List.tryPick (fun layer ->
                    layer.Objects
                    |> List.rev
                    |> List.tryFind (fun objectValue -> objectValue.Selectable && Geometry.containsScene scenePoint objectValue.Content)
                    |> Option.map (fun objectValue -> objectValue.Id)))

    let localPoint (pointer: PointerEvent) =
        let bounds = root.getBoundingClientRect()
        { X = (pointer.clientX - bounds.left) * options.Width / max 1.0 bounds.width
          Y = (pointer.clientY - bounds.top) * options.Height / max 1.0 bounds.height }

    let addListener name handler =
        root.addEventListener(name, handler)
        listeners.Add(name, handler)

    do
        reconcileScene ()
        let pointerDown (event: Event) =
            let pointer = event :?> PointerEvent
            let pointerId = int pointer.pointerId
            let point = localPoint pointer
            lastPointer <- Some point
            Dom.capturePointer root pointerId
            apply (RetainedInteractionMessage.CapturePointer(state.Scene.Revision, pointerId)) |> ignore
            match hitTest point with
            | Some id -> apply (RetainedInteractionMessage.Select(state.Scene.Revision, id)) |> ignore
            | None -> ()
            event.preventDefault()
        let pointerMove (event: Event) =
            let pointer = event :?> PointerEvent
            let pointerId = int pointer.pointerId
            if state.CapturedPointerId = Some pointerId then
                let point = localPoint pointer
                match lastPointer with
                | Some previous ->
                    let delta = { X = point.X - previous.X; Y = point.Y - previous.Y }
                    let camera = state.Scene.Camera
                    apply (RetainedInteractionMessage.SetCamera(state.Scene.Revision, { camera with PanX = camera.PanX + delta.X; PanY = camera.PanY + delta.Y })) |> ignore
                | None -> ()
                lastPointer <- Some point
        let release (event: Event) =
            let pointer = event :?> PointerEvent
            let pointerId = int pointer.pointerId
            if state.CapturedPointerId = Some pointerId then
                apply (RetainedInteractionMessage.ReleasePointer(state.Scene.Revision, pointerId)) |> ignore
            if Dom.hasPointerCapture root pointerId then Dom.releasePointer root pointerId
            lastPointer <- None
        let keyDown (event: Event) =
            let keyboard = event :?> KeyboardEvent
            let message =
                match keyboard.key with
                | "ArrowRight" | "ArrowDown" -> Some(RetainedInteractionMessage.FocusNext state.Scene.Revision)
                | "ArrowLeft" | "ArrowUp" -> Some(RetainedInteractionMessage.FocusPrevious state.Scene.Revision)
                | "Enter" | " " -> state.FocusedObjectId |> Option.map (fun id -> RetainedInteractionMessage.Select(state.Scene.Revision, id))
                | _ -> None
            match message with
            | Some value ->
                let result = apply value
                result.State.FocusedObjectId |> Option.bind (fun id -> match objects.TryGetValue id with true, element -> Some element | _ -> None) |> Option.iter Dom.focus
                event.preventDefault()
            | None -> ()
        let wheel (event: Event) =
            let value = event :?> WheelEvent
            let anchor =
                let bounds = root.getBoundingClientRect()
                { X = (value.clientX - bounds.left) * options.Width / max 1.0 bounds.width
                  Y = (value.clientY - bounds.top) * options.Height / max 1.0 bounds.height }
            let factor = if value.deltaY < 0.0 then options.WheelZoomFactor else 1.0 / options.WheelZoomFactor
            let targetZoom = state.Scene.Camera.Zoom * factor
            match SvgRetained.tryToScenePoint state.Scene.Camera anchor with
            | Some scenePoint ->
                let camera = { PanX = anchor.X - targetZoom * scenePoint.X; PanY = anchor.Y - targetZoom * scenePoint.Y; Zoom = targetZoom }
                apply (RetainedInteractionMessage.SetCamera(state.Scene.Revision, camera)) |> ignore
                event.preventDefault()
            | None -> ()
        addListener "pointerdown" pointerDown
        addListener "pointermove" pointerMove
        addListener "pointerup" release
        addListener "pointercancel" release
        addListener "keydown" keyDown
        addListener "wheel" wheel

    member _.Root = root
    member _.State = state
    member _.Dispatch message = apply message
    member _.HitTest screenPoint = hitTest screenPoint
    member _.PanBy (delta: Point) =
        let camera = state.Scene.Camera
        apply (RetainedInteractionMessage.SetCamera(state.Scene.Revision, { camera with PanX = camera.PanX + delta.X; PanY = camera.PanY + delta.Y }))
    member _.ZoomAt(anchorScreen, zoom) =
        match SvgRetained.tryToScenePoint state.Scene.Camera anchorScreen with
        | None -> apply (RetainedInteractionMessage.SetCamera(state.Scene.Revision, { state.Scene.Camera with Zoom = zoom }))
        | Some scenePoint ->
            apply (RetainedInteractionMessage.SetCamera(state.Scene.Revision,
                { PanX = anchorScreen.X - zoom * scenePoint.X
                  PanY = anchorScreen.Y - zoom * scenePoint.Y
                  Zoom = zoom }))
    member _.Observe() =
        { RootId = state.Scene.RootId
          Revision = state.Scene.Revision
          LayerCount = state.Scene.Layers.Length
          ObjectCount = state.Scene.Layers |> List.sumBy (fun layer -> layer.Objects.Length)
          SvgNodeCount = root.querySelectorAll("*").length + 1
          OwnedListenerCount = if disposed then 0 else listeners.Count
          ScheduledFrameCount = 0 }

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                for name, handler in listeners do root.removeEventListener(name, handler)
                listeners.Clear()
                if root.parentNode = container then container.removeChild(root) |> ignore
                disposed <- true
                lastPointer <- None

[<RequireQualifiedAccess>]
module SvgBrowser =
    let mountDocument (container: HTMLElement) mountNamespace document =
        if DocumentRegistry.activeNamespaces.Contains mountNamespace then
            Error(SvgDocumentBrowserError.DuplicateMountNamespace mountNamespace)
        else
            match SvgDocument.exportSvg mountNamespace document with
            | Error issues -> Error(SvgDocumentBrowserError.InvalidDocument issues)
            | Ok svg ->
                let root = Dom.parseExportedSvg svg
                container.appendChild(root) |> ignore
                DocumentRegistry.activeNamespaces.Add mountNamespace |> ignore
                Ok(new SvgDocumentBrowserHost(container, mountNamespace, document, svg, root))

    let mount (container: HTMLElement) (options: SvgBrowserOptions) (scene: RetainedScene) onTransition =
        match SvgRetained.tryCreateInteraction scene with
        | Error error -> Error(SvgBrowserMountError.InvalidScene error)
        | Ok state ->
            let finite value = not (Double.IsNaN value || Double.IsInfinity value)
            if not (finite options.Width && finite options.Height && finite options.WheelZoomFactor)
               || options.Width <= 0.0 || options.Height <= 0.0
               || String.IsNullOrWhiteSpace options.AccessibleLabel || options.WheelZoomFactor <= 1.0 then
                Error(SvgBrowserMountError.InvalidOptions "width and height must be finite and positive; label must be nonblank; wheel zoom factor must be finite and greater than one")
            else
                match SvgDocument.ofRetainedScene { X = 0.0; Y = 0.0; Width = options.Width; Height = options.Height } scene with
                | Error issues -> Error(SvgBrowserMountError.InvalidDocument issues)
                | Ok _ ->
                    let root = Dom.create "svg"
                    Dom.set "width" (Format.number options.Width) root
                    Dom.set "height" (Format.number options.Height) root
                    Dom.set "viewBox" $"0 0 {Format.number options.Width} {Format.number options.Height}" root
                    Dom.set "role" "application" root
                    Dom.set "aria-label" options.AccessibleLabel root
                    Dom.set "tabindex" "0" root
                    let viewport = Dom.create "g"
                    Dom.set "data-scene-viewport" "true" viewport
                    root.appendChild(viewport) |> ignore
                    container.appendChild(root) |> ignore
                    Ok(new SvgBrowserHost(container, root, viewport, state, options, onTransition))
