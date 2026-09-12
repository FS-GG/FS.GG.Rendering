module SvgFoundationBrowserFixture

open System
open Browser.Dom
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open FS.GG.UI.Scene
open FS.GG.UI.Scene.SvgBrowser

let color red green blue = { Red = red; Green = green; Blue = blue; Alpha = 255uy }
let sceneOf nodes = { Nodes = nodes }
let rectangle x y width height value = SceneNode.Rectangle((x, y, width, height), value)

let alpha revisionWidth =
    { Id = "alpha"
      Selectable = true
      AccessibleLabel = "Alpha unit"
      Content = sceneOf [ rectangle 10.0 10.0 revisionWidth 20.0 (color 230uy 80uy 60uy) ] }

let beta =
    { Id = "beta"
      Selectable = true
      AccessibleLabel = "Beta unit"
      Content = sceneOf [ SceneNode.Circle({ X = 65.0; Y = 20.0 }, 10.0, color 60uy 140uy 230uy) ] }

let overlay =
    { Id = "label"
      Selectable = false
      AccessibleLabel = "Scene label"
      Content = sceneOf [ SceneNode.SizedText((4.0, 92.0), "SVG foundation", 8.0, color 30uy 30uy 30uy) ] }

let retained revision alphaWidth =
    { RootId = "svg-foundation-root"
      Revision = revision
      Camera = { PanX = 20.0; PanY = 10.0; Zoom = 2.0 }
      Layers =
        [ { Id = "units"; Visible = true; Objects = [ alpha alphaWidth; beta ] }
          { Id = "overlay"; Visible = true; Objects = [ overlay ] } ] }

let options =
    { Width = 200.0
      Height = 120.0
      AccessibleLabel = "SVG foundation scene"
      WheelZoomFactor = 1.25 }

let container: HTMLElement = unbox (document.getElementById("fixture"))
let transitions = ResizeArray<RetainedInteractionResult>()
let mutable host: SvgBrowserHost option = None
let documentContainer: HTMLElement = unbox (document.getElementById("document-fixture"))
let duplicateContainer: HTMLElement = unbox (document.getElementById("duplicate-fixture"))
let mutable documentHost: SvgDocumentBrowserHost option = None
let performanceContainer: HTMLElement = unbox (document.getElementById("performance-fixture"))
let mutable performanceHost: SvgDocumentBrowserHost option = None
let mutable performanceKind = "ordinary"
let mutable performanceSelection = 0
let mutable performanceCameraStep = 0
let mutable performanceChangedObject = -1

let errorName = function
    | None -> null
    | Some(RetainedInteractionError.StaleRevision _) -> "stale-revision"
    | Some(RetainedInteractionError.NonIncreasingRevision _) -> "non-increasing-revision"
    | Some _ -> "other"

let stateObject () =
    let value = host.Value.State
    createObj [
        "revision" ==> value.Scene.Revision
        "selected" ==> (value.SelectedObjectId |> Option.toObj)
        "focused" ==> (value.FocusedObjectId |> Option.toObj)
        "captured" ==> (value.CapturedPointerId |> Option.map box |> Option.toObj)
        "camera" ==> createObj [ "panX" ==> value.Scene.Camera.PanX; "panY" ==> value.Scene.Camera.PanY; "zoom" ==> value.Scene.Camera.Zoom ]
    ]

let resultObject (result: RetainedInteractionResult) =
    createObj [ "error" ==> errorName result.Error; "state" ==> stateObject() ]

let mount () =
    host |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    transitions.Clear()
    match SvgBrowser.mount container options (retained 1 20.0) transitions.Add with
    | Error error -> failwithf "mount failed: %A" error
    | Ok value -> host <- Some value

let dispose () =
    host |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    let disposedState = stateObject ()
    host <- None
    disposedState

let mountDocumentFixture () =
    documentHost |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    match SvgBrowser.mountDocument documentContainer "gallery-browser" PortableDocumentFixture.document with
    | Error error -> failwithf "document mount failed: %A" error
    | Ok value -> documentHost <- Some value

let documentError = function
    | Ok() -> null
    | Error(SvgDocumentBrowserError.DuplicateMountNamespace value) -> "duplicate-mount-namespace:" + value
    | Error(SvgDocumentBrowserError.InvalidDocument issues) ->
        issues |> List.map (fun issue -> $"{issue.Code}:{issue.Location}") |> String.concat ","

let invalidDocument () =
    let duplicate = PortableDocumentFixture.document.Children.Head
    { PortableDocumentFixture.document with Children = [ duplicate; duplicate ] }

let reorderedDocument () =
    { PortableDocumentFixture.document with
        Children = PortableDocumentFixture.document.Children |> List.rev }

let changedDocument () =
    let replace (element: SvgElement) =
        if element.Id = "luminance-element" then
            { element with Transform = SvgAffine.translate 3.0 0.0 }
        else element
    { PortableDocumentFixture.document with Children = PortableDocumentFixture.document.Children |> List.map replace }

let performanceDocument kind selection cameraStep changedObject =
    let objectCount = if kind = "dense" then 200 elif kind = "gallery" then 0 else 100
    let point x y = { X = x; Y = y }
    let rect x y width height = { X = x; Y = y; Width = width; Height = height }
    let color red green blue = { Red = red; Green = green; Blue = blue; Alpha = 255uy }
    let gradient =
        { Geometry = SvgGradientGeometry.Linear(point 0.0 0.0, point 1.0 1.0)
          Units = SvgCoordinateUnits.ObjectBoundingBox
          Transform = SvgAffine.identity
          Spread = SvgSpreadMethod.Pad
          Stops =
            [ { Offset = 0.0; Color = color 36uy 116uy 214uy; StopOpacity = 1.0 }
              { Offset = 1.0; Color = color 38uy 190uy 132uy; StopOpacity = 1.0 } ]
          InheritFrom = None }
    let leaf id scene =
        { Id = id
          SemanticId = None
          Visible = true
          Transform = SvgAffine.identity
          ClipId = None
          MaskId = None
          Presentation = None
          Content = SvgElementContent.SceneLeaf scene }
    let symbolChild = leaf "shared-symbol-dot" { Nodes = [ SceneNode.Circle(point 3.0 3.0, 3.0, color 250uy 190uy 40uy) ] }
    let pathPaint =
        { Fill = Some(color 30uy 70uy 130uy)
          Stroke = None
          Opacity = 1.0
          Antialias = true
          BlendMode = BlendMode.SrcOver
          Shader = None
          ColorFilter = ColorFilter.NoColorFilter
          MaskFilter = MaskFilter.NoMaskFilter
          ImageFilter = ImageFilter.NoImageFilter
          PathEffect = PathEffect.NoPathEffect }
    let galleryGradient index =
        { gradient with
            Stops =
                [ { Offset = 0.0; Color = color (byte (40 + index % 180)) 90uy 180uy; StopOpacity = 1.0 }
                  { Offset = 1.0; Color = color 30uy (byte (60 + index % 180)) 120uy; StopOpacity = 1.0 } ] }
    let definitions =
        (if kind = "gallery" then
             [ for index in 0 .. 63 ->
                   { Id = $"gallery-gradient-{index}"
                     Content = SvgDefinitionContent.Gradient(galleryGradient index) } ]
         else [ { Id = "shared-gradient"; Content = SvgDefinitionContent.Gradient gradient } ])
        @ [ { Id = "shared-symbol"; Content = SvgDefinitionContent.Symbol(Some(rect 0.0 0.0 6.0 6.0), [ symbolChild ]) } ]
        @ (if kind = "dense" then
              [ for index in 0 .. 9 do
                    yield { Id = $"dense-clip-{index}"; Content = SvgDefinitionContent.Clip(SvgCoordinateUnits.UserSpaceOnUse, [ SvgClipShape.Rectangle(rect 0.0 0.0 1000.0 1000.0) ]) }
                    let maskChild = leaf $"dense-mask-child-{index}" { Nodes = [ SceneNode.Rectangle((0.0, 0.0, 1.0, 1.0), color 255uy 255uy 255uy) ] }
                    yield { Id = $"dense-mask-{index}"; Content = SvgDefinitionContent.Mask(SvgCoordinateUnits.ObjectBoundingBox, rect 0.0 0.0 1.0 1.0, SvgMaskKind.Alpha, [ maskChild ]) } ]
           elif kind = "gallery" then
              [ for index in 0 .. 31 do
                    let maskChild = leaf $"gallery-mask-child-{index}" { Nodes = [ SceneNode.Rectangle((0.0, 0.0, 1.0, 1.0), color 255uy 255uy 255uy) ] }
                    yield { Id = $"gallery-mask-{index}"; Content = SvgDefinitionContent.Mask(SvgCoordinateUnits.ObjectBoundingBox, rect 0.0 0.0 1.0 1.0, SvgMaskKind.Alpha, [ maskChild ]) } ]
           else [])
    let pathAt x y =
        { Commands =
            PathCommand.MoveTo(point x y)
            :: [ for segment in 1 .. 12 -> PathCommand.LineTo(point (x + float segment * 2.0) (y + float (segment % 3))) ]
          FillType = PathFillType.Winding }
    let objectElement index =
        let column = index % 20
        let row = index / 20
        let x = 5.0 + float column * 47.0
        let y = 8.0 + float row * 45.0
        let selected = index = selection
        let shapeWidth = if index = changedObject then 24.0 else 22.0
        let presentation =
            { FillSource = Some(SvgPaintSource.Definition "shared-gradient")
              StrokeStyle = None
              OverallOpacity = if selected then 0.55 else 1.0
              FillRule = PathFillType.Winding }
        let shape = leaf $"object-{index}-shape" { Nodes = [ SceneNode.Rectangle((x, y, shapeWidth, 15.0), color 70uy 130uy 220uy) ] }
        let path = leaf $"object-{index}-path" { Nodes = [ SceneNode.Path(pathAt x (y + 18.0), pathPaint) ] }
        let label = leaf $"object-{index}-label" { Nodes = [ SceneNode.SizedText((x, y + 34.0), $"Unit {index}", 8.0, color 25uy 25uy 25uy) ] }
        let symbol =
            { leaf $"object-{index}-symbol" { Nodes = [] } with
                Transform = SvgAffine.translate (x + 28.0) y
                Presentation = Some presentation
                Content = SvgElementContent.SymbolInstance("shared-symbol", Some(rect 0.0 0.0 6.0 6.0)) }
        { Id = $"object-{index}"
          SemanticId = Some($"semantic:object-{index}")
          Visible = true
          Transform = SvgAffine.identity
          ClipId = if kind = "dense" && index < 10 then Some($"dense-clip-{index}") else None
          MaskId = if kind = "dense" && index >= 10 && index < 20 then Some($"dense-mask-{index - 10}") else None
          Presentation = Some presentation
          Content = SvgElementContent.Group [ shape; path; label; symbol ] }
    let objects = [ for index in 0 .. objectCount - 1 -> objectElement index ]
    let galleryChildren =
        if kind <> "gallery" then [] else
        let galleryPath index =
            let x = float (index % 20) * 48.0
            let y = float (index / 20) * 35.0
            { Commands = PathCommand.MoveTo(point x y) :: [ for segment in 1 .. 256 -> PathCommand.LineTo(point (x + float segment * 0.12) (y + float (segment % 7) * 0.25)) ]
              FillType = PathFillType.Winding }
        let paths =
            List.init 200 (fun index ->
                { leaf $"gallery-path-{index}" { Nodes = [ SceneNode.Path(galleryPath index, pathPaint) ] } with
                    MaskId = if index < 32 then Some($"gallery-mask-{index}") else None
                    Presentation = Some { FillSource = Some(SvgPaintSource.Definition $"gallery-gradient-{index % 64}"); StrokeStyle = None; OverallOpacity = 1.0; FillRule = PathFillType.Winding } })
        let texts =
            List.init 100 (fun index ->
                leaf $"gallery-text-{index}" { Nodes = [ SceneNode.SizedText((float (index % 20) * 48.0, 365.0 + float (index / 20) * 12.0), $"G{index}", 8.0, color 20uy 20uy 20uy) ] })
        let symbols =
            List.init 100 (fun index ->
                { leaf $"gallery-symbol-{index}" { Nodes = [] } with
                    Transform = SvgAffine.translate (float (index % 20) * 48.0) (430.0 + float (index / 20) * 10.0)
                    Content = SvgElementContent.SymbolInstance("shared-symbol", Some(rect 0.0 0.0 6.0 6.0)) })
        paths @ texts @ symbols
    let layers =
        if kind = "gallery" then
            [ { Id = "gallery-layer"; SemanticId = None; Visible = true; Transform = SvgAffine.identity; ClipId = None; MaskId = None; Presentation = None; Content = SvgElementContent.Group galleryChildren } ]
        else
            objects
            |> List.chunkBySize (objectCount / 4)
            |> List.mapi (fun index children ->
                { Id = $"layer-{index}"
                  SemanticId = None
                  Visible = true
                  Transform = SvgAffine.identity
                  ClipId = None
                  MaskId = None
                  Presentation = None
                  Content = SvgElementContent.Group children })
    let overlay =
        if kind = "dense" then
            let overlayPaint =
                { pathPaint with
                    Fill = Some(color 0uy 0uy 0uy)
                    Stroke = Some { Width = 1.0; Cap = StrokeCap.Butt; Join = StrokeJoin.Miter; Miter = 4.0 } }
            [ leaf "selection-overlay" { Nodes = [ SceneNode.PaintedRectangle(rect 1.0 1.0 995.0 445.0, overlayPaint) ] } ]
        else []
    let camera =
        { Id = "camera"
          SemanticId = None
          Visible = true
          Transform = SvgAffine.translate (float cameraStep * 0.25) (float cameraStep * -0.125)
          ClipId = None
          MaskId = None
          Presentation = None
          Content = SvgElementContent.Group(layers @ overlay) }
    { Schema = SvgDocument.schema
      Id = $"preview-a-{kind}"
      ViewBox = rect 0.0 0.0 1000.0 500.0
      Definitions = definitions
      Children = [ camera ] }

let replacePerformance () =
    performanceHost.Value.Replace(performanceDocument performanceKind performanceSelection performanceCameraStep performanceChangedObject)
    |> documentError

let mountPerformance kind =
    performanceHost |> Option.iter (fun value -> (value :> IDisposable).Dispose())
    performanceKind <- kind
    performanceSelection <- 0
    performanceCameraStep <- 0
    performanceChangedObject <- -1
    let candidate = performanceDocument kind performanceSelection performanceCameraStep performanceChangedObject
    match SvgBrowser.mountDocument performanceContainer "preview-a-performance" candidate with
    | Error error -> failwithf "performance mount failed: %A" error
    | Ok value ->
        performanceHost <- Some value
        value.Root.setAttribute("width", "800")
        value.Root.setAttribute("height", "400")

let excessivePerformanceDocument () =
    let before = performanceHost.Value.Root
    let candidate = performanceDocument performanceKind performanceSelection performanceCameraStep performanceChangedObject
    let empty index =
        { Id = $"excessive-{index}"
          SemanticId = None
          Visible = true
          Transform = SvgAffine.identity
          ClipId = None
          MaskId = None
          Presentation = None
          Content = SvgElementContent.SceneLeaf { Nodes = [] } }
    let excessive = { candidate with Children = [ for index in 0 .. SvgDocument.defaultLimits.MaxNodes -> empty index ] }
    let error = performanceHost.Value.Replace excessive |> documentError
    createObj [ "error" ==> error; "rootRetained" ==> obj.ReferenceEquals(before, performanceHost.Value.Root) ]

let api =
    createObj [
        "mount" ==> fun () -> mount(); stateObject()
        "dispose" ==> fun () -> dispose()
        "state" ==> fun () -> stateObject()
        "replaceCurrent" ==> fun () -> host.Value.Dispatch(RetainedInteractionMessage.ReplaceScene(retained 2 28.0)) |> resultObject
        "replaceReordered" ==> fun () ->
            let current = retained 3 28.0
            let reordered =
                { current with
                    Layers =
                        [ { Id = "overlay"; Visible = true; Objects = [ overlay ] }
                          { Id = "units"; Visible = true; Objects = [ beta; alpha 28.0 ] } ] }
            host.Value.Dispatch(RetainedInteractionMessage.ReplaceScene reordered) |> resultObject
        "replaceStale" ==> fun () -> host.Value.Dispatch(RetainedInteractionMessage.ReplaceScene(retained 1 35.0)) |> resultObject
        "hit" ==> fun x y -> host.Value.HitTest({ X = x; Y = y }) |> Option.toObj
        "scenePoint" ==> fun x y -> SvgRetained.tryToScenePoint host.Value.State.Scene.Camera { X = x; Y = y } |> Option.map (fun point -> createObj [ "x" ==> point.X; "y" ==> point.Y ]) |> Option.toObj
        "zoomAt" ==> fun x y zoom -> host.Value.ZoomAt({ X = x; Y = y }, zoom) |> resultObject
        "panBy" ==> fun x y -> host.Value.PanBy({ X = x; Y = y }) |> resultObject
        "capture" ==> fun pointerId -> host.Value.Dispatch(RetainedInteractionMessage.CapturePointer(host.Value.State.Scene.Revision, pointerId)) |> resultObject
        "observe" ==> fun () ->
            let value = host.Value.Observe()
            createObj [
                "revision" ==> value.Revision
                "layers" ==> value.LayerCount
                "objects" ==> value.ObjectCount
                "nodes" ==> value.SvgNodeCount
                "listeners" ==> value.OwnedListenerCount
                "frames" ==> value.ScheduledFrameCount
            ]
        "transitionCount" ==> fun () -> transitions.Count
        "documentExport" ==> fun () -> documentHost.Value.ExportedSvg
        "documentFonts" ==> fun () ->
            documentHost.Value.ObserveFonts()
            |> List.map (fun value -> createObj [ "definitionId" ==> value.DefinitionId; "family" ==> value.Family; "ready" ==> value.Ready; "diagnostic" ==> (value.Diagnostic |> Option.toObj) ])
            |> List.toArray
        "replaceInvalidDocument" ==> fun () -> documentHost.Value.Replace(invalidDocument()) |> documentError
        "replaceReorderedDocument" ==> fun () -> documentHost.Value.Replace(reorderedDocument()) |> documentError
        "replaceChangedDocument" ==> fun () -> documentHost.Value.Replace(changedDocument()) |> documentError
        "replaceOriginalDocument" ==> fun () -> documentHost.Value.Replace(PortableDocumentFixture.document) |> documentError
        "documentHit" ==> fun x y -> documentHost.Value.HitTest({ X = x; Y = y }) |> Option.toObj
        "duplicateDocumentMount" ==> fun () -> SvgBrowser.mountDocument duplicateContainer "gallery-browser" PortableDocumentFixture.document |> Result.map (fun value -> (value :> IDisposable).Dispose()) |> documentError
        "performanceMount" ==> fun kind -> mountPerformance kind
        "performanceSelect" ==> fun index -> performanceSelection <- index; replacePerformance()
        "performanceCamera" ==> fun step -> performanceCameraStep <- step; replacePerformance()
        "performanceRevise" ==> fun index -> performanceChangedObject <- index; replacePerformance()
        "performanceExport" ==> fun () -> performanceHost.Value.ExportedSvg
        "performanceObserve" ==> fun () ->
            createObj [
                "kind" ==> performanceKind
                "objects" ==> (if performanceKind = "dense" then 200 elif performanceKind = "gallery" then 400 else 100)
                "layers" ==> (if performanceKind = "gallery" then 1 else 4)
                "definitions" ==> performanceHost.Value.Document.Definitions.Length
                "nodes" ==> performanceHost.Value.Root.querySelectorAll("*").length + 1
            ]
        "performanceExcessiveDocument" ==> fun () -> excessivePerformanceDocument()
        "performanceDispose" ==> fun () ->
            performanceHost |> Option.iter (fun value -> (value :> IDisposable).Dispose())
            performanceHost <- None
            performanceContainer.querySelectorAll("svg").length
    ]

[<Emit("window.svgFoundation = $0")>]
let expose (_api: obj) : unit = jsNative

mount()
mountDocumentFixture()
expose api
