namespace FS.Skia.UI.Elmish

open FS.Skia.UI.Scene
open FS.Skia.UI.SkiaViewer

type ElmishAdapterModel<'model> =
    { UserModel: 'model
      Scene: SceneNode
      Viewer: ViewerModel }

type ElmishAdapterMsg<'msg> =
    | UserMsg of 'msg
    | ViewerMsg of ViewerMsg

type ElmishAdapterEffect<'msg> =
    | DispatchUser of 'msg
    | DispatchViewer of ViewerEffect

module ElmishAdapter =
    let init viewerOptions userModel scene =
        let viewer, effects = Viewer.init viewerOptions

        { UserModel = userModel
          Scene = scene
          Viewer = viewer },
        (effects |> List.map DispatchViewer)

    let update render msg model =
        match msg with
        | UserMsg userMsg -> model, [ DispatchUser userMsg ]
        | ViewerMsg viewerMsg ->
            let viewer, effects = Viewer.update viewerMsg model.Viewer
            let next = { model with Viewer = viewer; Scene = render model.UserModel }
            next, (effects |> List.map DispatchViewer)
