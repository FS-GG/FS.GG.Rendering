module ElmishCapabilityTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Elmish

let private viewerOptions =
    { Title = "Product"
      InitialSize = { Width = 320; Height = 240 }
      PresentMode = ViewerPresentMode.OffscreenReadback
      FrameRateCap = None; LogicalSize = None }

/// A render function whose output is keyed on the user model, so a re-render is observable.
let private render (userModel: int) =
    SceneNode.Text((0.0, 0.0), string userModel, Colors.rgb 0uy 0uy 0uy)

let private viewerModel () = Viewer.init viewerOptions |> fst

[<Tests>]
let tests =
    testList "Elmish adapter contract" [
        test "init maps viewer effects" {
            let scene = Empty
            let _, effects = ElmishAdapter.init viewerOptions 0 scene

            match effects with
            | [ DispatchViewer(OpenWindow(title, size))
                DispatchViewer(ApplyWindowOptions behavior)
                DispatchViewer(EmitDiagnostic diagnostic) ] ->
                Expect.equal title "Product" "viewer title is mapped"
                Expect.equal size { Width = 320; Height = 240 } "viewer size is mapped"
                Expect.equal behavior Viewer.defaultWindowBehavior "viewer startup behavior is mapped"
                Expect.equal diagnostic.Category Startup "startup diagnostic category is mapped"
                Expect.equal diagnostic.Stage (Some Window) "startup diagnostic stage is mapped"
            | other -> failtestf "Expected DispatchViewer OpenWindow, ApplyWindowOptions, and startup diagnostic effects, got %A" other
        }

        test "update forwards a user message without re-rendering the scene" {
            let model = { UserModel = 1; Scene = Empty; Viewer = viewerModel () }
            let next, effects = ElmishAdapter.update render (UserMsg "save") model

            Expect.equal next.Scene Empty "a user message alone does not re-render"
            Expect.equal next.UserModel 1 "the adapter never interprets the user model"

            match effects with
            | [ DispatchUser userMsg ] -> Expect.equal userMsg "save" "the user message is forwarded verbatim"
            | other -> failtestf "Expected a single DispatchUser effect, got %A" other
        }

        test "update re-renders the scene from the user model on a viewer message" {
            let model = { UserModel = 7; Scene = Empty; Viewer = viewerModel () }
            let next, _ = ElmishAdapter.update render (ViewerMsg(FramePresented { Width = 320; Height = 240 })) model

            Expect.equal next.Scene (render 7) "the viewer message re-renders from the current user model"
            Expect.equal next.UserModel 7 "re-rendering does not disturb the user model"
        }

        test "update wraps every viewer effect as DispatchViewer" {
            let model = { UserModel = 0; Scene = Empty; Viewer = viewerModel () }
            let viewerMsg = Render(render 0)
            let _, expected = Viewer.update viewerMsg model.Viewer
            let _, effects = ElmishAdapter.update render (ViewerMsg viewerMsg) model

            Expect.equal effects.Length expected.Length "the adapter neither drops nor invents viewer effects"

            Expect.isTrue
                (effects |> List.forall (function DispatchViewer _ -> true | DispatchUser _ -> false))
                "a viewer message yields only viewer effects"
        }
    ]
