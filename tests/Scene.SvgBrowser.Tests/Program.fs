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
    host <- None

let api =
    createObj [
        "mount" ==> fun () -> mount(); stateObject()
        "dispose" ==> fun () -> dispose()
        "state" ==> fun () -> stateObject()
        "replaceCurrent" ==> fun () -> host.Value.Dispatch(RetainedInteractionMessage.ReplaceScene(retained 2 28.0)) |> resultObject
        "replaceStale" ==> fun () -> host.Value.Dispatch(RetainedInteractionMessage.ReplaceScene(retained 1 35.0)) |> resultObject
        "hit" ==> fun x y -> host.Value.HitTest({ X = x; Y = y }) |> Option.toObj
        "scenePoint" ==> fun x y -> SvgRetained.tryToScenePoint host.Value.State.Scene.Camera { X = x; Y = y } |> Option.map (fun point -> createObj [ "x" ==> point.X; "y" ==> point.Y ]) |> Option.toObj
        "zoomAt" ==> fun x y zoom -> host.Value.ZoomAt({ X = x; Y = y }, zoom) |> resultObject
        "panBy" ==> fun x y -> host.Value.PanBy({ X = x; Y = y }) |> resultObject
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
    ]

[<Emit("window.svgFoundation = $0")>]
let expose (_api: obj) : unit = jsNative

mount()
expose api
