namespace FS.GG.UI.Scene

type SvgCamera =
    { PanX: float
      PanY: float
      Zoom: float }

type SemanticSceneObject =
    { Id: string
      Selectable: bool
      AccessibleLabel: string
      Content: Scene }

type RetainedSceneLayer =
    { Id: string
      Visible: bool
      Objects: SemanticSceneObject list }

type RetainedScene =
    { RootId: string
      Revision: int
      Camera: SvgCamera
      Layers: RetainedSceneLayer list }

type SvgAdapterIssue =
    { ObjectId: string option
      NodePath: int list
      Reason: string }

[<RequireQualifiedAccess>]
type SvgAdapterResult =
    | Rendered of RetainedScene
    | Unsupported of SvgAdapterIssue list
    | Invalid of SvgAdapterIssue list

[<RequireQualifiedAccess>]
type RetainedInteractionMessage =
    | ReplaceScene of RetainedScene
    | Select of expectedRevision: int * objectId: string
    | ClearSelection of expectedRevision: int
    | FocusNext of expectedRevision: int
    | FocusPrevious of expectedRevision: int
    | SetCamera of expectedRevision: int * camera: SvgCamera
    | CapturePointer of expectedRevision: int * pointerId: int
    | ReleasePointer of expectedRevision: int * pointerId: int

[<RequireQualifiedAccess>]
type RetainedInteractionError =
    | StaleRevision of expected: int * actual: int
    | NonIncreasingRevision of candidate: int * actual: int
    | UnknownObject of string
    | ObjectNotSelectable of string
    | InvalidCamera
    | InvalidScene of SvgAdapterIssue list
    | UnsupportedScene of SvgAdapterIssue list
    | PointerNotCaptured of int

type RetainedInteractionState =
    { Scene: RetainedScene
      SelectedObjectId: string option
      FocusedObjectId: string option
      CapturedPointerId: int option }

type RetainedInteractionResult =
    { State: RetainedInteractionState
      Error: RetainedInteractionError option }

[<RequireQualifiedAccess>]
module SvgRetained =
    let private finite (value: float) =
        not (System.Double.IsNaN value || System.Double.IsInfinity value)

    let private validCamera (camera: SvgCamera) =
        finite camera.PanX && finite camera.PanY && finite camera.Zoom && camera.Zoom > 0.0

    let private issue (objectId: string option) (path: int list) (reason: string) : SvgAdapterIssue =
        { ObjectId = objectId
          NodePath = path
          Reason = reason }

    let private validatePoint objectId path (point: Point) =
        if finite point.X && finite point.Y then []
        else [ issue objectId path "point contains a non-finite coordinate" ]

    let private validateRect objectId path (rect: Rect) =
        if finite rect.X && finite rect.Y && finite rect.Width && finite rect.Height then []
        else [ issue objectId path "rectangle contains a non-finite coordinate" ]

    let private validateColor _objectId _path (_color: Color) : SvgAdapterIssue list = []

    let private validateStroke objectId path (stroke: Stroke) =
        if finite stroke.Width && finite stroke.Miter && stroke.Width >= 0.0 && stroke.Miter >= 0.0 then []
        else [ issue objectId path "stroke width and miter must be finite and non-negative" ]

    let private validatePaint objectId path (paint: Paint) =
        let numeric =
            [ if not (finite paint.Opacity) || paint.Opacity < 0.0 || paint.Opacity > 1.0 then
                  issue objectId path "paint opacity must be finite and between zero and one"
              match paint.Stroke with
              | Some stroke -> yield! validateStroke objectId path stroke
              | None -> () ]

        let unsupported =
            [ if paint.BlendMode <> BlendMode.SrcOver then yield issue objectId path "blend mode is outside the SVG foundation subset"
              match paint.Shader with
              | None
              | Some(Shader.SolidColor _) -> ()
              | Some _ -> yield issue objectId path "gradient shader is outside the SVG foundation subset"
              if paint.ColorFilter <> ColorFilter.NoColorFilter then yield issue objectId path "color filter is outside the SVG foundation subset"
              if paint.MaskFilter <> MaskFilter.NoMaskFilter then yield issue objectId path "mask filter is outside the SVG foundation subset"
              if paint.ImageFilter <> ImageFilter.NoImageFilter then yield issue objectId path "image filter is outside the SVG foundation subset"
              if paint.PathEffect <> PathEffect.NoPathEffect then yield issue objectId path "path effect is outside the SVG foundation subset" ]

        numeric, unsupported

    let rec private validateScene (objectId: string option) (prefix: int list) (scene: Scene) =
        scene.Nodes
        |> List.mapi (fun index node -> validateNode objectId (prefix @ [ index ]) node)
        |> List.fold (fun (invalid, unsupported) (nodeInvalid, nodeUnsupported) -> invalid @ nodeInvalid, unsupported @ nodeUnsupported) ([], [])

    and private validateNode (objectId: string option) (path: int list) (node: SceneNode) =
        let supported invalid unsupported = invalid, unsupported
        let unsupported reason = [], [ issue objectId path reason ]

        match node with
        | SceneNode.Empty -> supported [] []
        | SceneNode.Group scenes ->
            scenes
            |> List.mapi (fun index scene -> validateScene objectId (path @ [ index ]) scene)
            |> List.fold (fun (invalid, rejected) (nextInvalid, nextRejected) -> invalid @ nextInvalid, rejected @ nextRejected) ([], [])
        | SceneNode.Rectangle((x, y, width, height), color) ->
            let invalid = validateRect objectId path { X = x; Y = y; Width = width; Height = height }
            supported (invalid @ validateColor objectId path color) []
        | SceneNode.PaintedRectangle(bounds, paint)
        | SceneNode.Ellipse(bounds, paint) ->
            let invalidPaint, unsupportedPaint = validatePaint objectId path paint
            supported (validateRect objectId path bounds @ invalidPaint) unsupportedPaint
        | SceneNode.Circle(center, radius, color) ->
            let radiusIssue = if finite radius && radius >= 0.0 then [] else [ issue objectId path "circle radius must be finite and non-negative" ]
            supported (validatePoint objectId path center @ radiusIssue @ validateColor objectId path color) []
        | SceneNode.FilledEllipse(bounds, color) ->
            supported (validateRect objectId path bounds @ validateColor objectId path color) []
        | SceneNode.Line(startPoint, endPoint, paint) ->
            let invalidPaint, unsupportedPaint = validatePaint objectId path paint
            supported (validatePoint objectId path startPoint @ validatePoint objectId path endPoint @ invalidPaint) unsupportedPaint
        | SceneNode.Path(pathSpec, paint) ->
            let invalidPaint, unsupportedPaint = validatePaint objectId path paint
            let commandResults =
                pathSpec.Commands
                |> List.mapi (fun index command ->
                    let commandPath = path @ [ index ]
                    match command with
                    | PathCommand.MoveTo point
                    | PathCommand.LineTo point -> validatePoint objectId commandPath point, []
                    | PathCommand.QuadTo(control, point) -> validatePoint objectId commandPath control @ validatePoint objectId commandPath point, []
                    | PathCommand.CubicTo(control1, control2, point) ->
                        validatePoint objectId commandPath control1 @ validatePoint objectId commandPath control2 @ validatePoint objectId commandPath point, []
                    | PathCommand.Close -> [], []
                    | PathCommand.ArcTo _ -> [], [ issue objectId commandPath "ArcTo is outside the SVG foundation subset" ])
                |> List.fold (fun (invalid, rejected) (nextInvalid, nextRejected) -> invalid @ nextInvalid, rejected @ nextRejected) ([], [])
            supported (invalidPaint @ fst commandResults) (unsupportedPaint @ snd commandResults)
        | SceneNode.Text((x, y), _, color) ->
            let invalid = if finite x && finite y then [] else [ issue objectId path "text position contains a non-finite coordinate" ]
            supported (invalid @ validateColor objectId path color) []
        | SceneNode.SizedText((x, y), _, size, color) ->
            let invalid =
                [ if not (finite x && finite y) then issue objectId path "text position contains a non-finite coordinate"
                  if not (finite size) || size <= 0.0 then issue objectId path "text size must be finite and positive" ]
            supported (invalid @ validateColor objectId path color) []
        | SceneNode.Translate((x, y), child) ->
            let invalid = if finite x && finite y then [] else [ issue objectId path "translation contains a non-finite coordinate" ]
            let childInvalid, childUnsupported = validateScene objectId path child
            supported (invalid @ childInvalid) childUnsupported
        | SceneNode.Points _ -> unsupported "Points is outside the SVG foundation subset"
        | SceneNode.Vertices _ -> unsupported "Vertices is outside the SVG foundation subset"
        | SceneNode.Arc _ -> unsupported "Arc is outside the SVG foundation subset"
        | SceneNode.TextRun _ -> unsupported "TextRun is outside the SVG foundation subset"
        | SceneNode.GlyphRun _ -> unsupported "GlyphRun is outside the SVG foundation subset"
        | SceneNode.Image _ -> unsupported "Image is outside the SVG foundation subset"
        | SceneNode.ClipNode _ -> unsupported "clips are outside the SVG foundation subset"
        | SceneNode.RegionNode _ -> unsupported "regions are outside the SVG foundation subset"
        | SceneNode.ColorSpaceNode(ColorSpace.Srgb, child) -> validateScene objectId path child
        | SceneNode.ColorSpaceNode _ -> unsupported "non-sRGB color spaces are outside the SVG foundation subset"
        | SceneNode.PerspectiveNode _ -> unsupported "perspective transforms are outside the SVG foundation subset"
        | SceneNode.PictureNode _ -> unsupported "pictures are outside the SVG foundation subset"
        | SceneNode.Chart _ -> unsupported "charts are outside the SVG foundation subset"
        | SceneNode.CachedSubtree _ -> unsupported "cached subtrees are outside the SVG foundation subset"

    let private duplicateIssues kind ids =
        ids
        |> List.countBy id
        |> List.choose (fun (value, count) ->
            if count > 1 then Some(issue None [] (kind + " id is duplicated: " + value)) else None)

    let project (scene: RetainedScene) =
        let objects = scene.Layers |> List.collect (fun layer -> layer.Objects)
        let identityIssues =
            [ if System.String.IsNullOrWhiteSpace scene.RootId then yield issue None [] "root id must not be blank"
              if scene.Revision < 0 then yield issue None [] "revision must be non-negative"
              if not (validCamera scene.Camera) then yield issue None [] "camera pan must be finite and zoom must be finite and positive"
              for layer in scene.Layers do
                  if System.String.IsNullOrWhiteSpace layer.Id then yield issue None [] "layer id must not be blank"
              for objectValue in objects do
                  if System.String.IsNullOrWhiteSpace objectValue.Id then yield issue None [] "object id must not be blank"
                  if System.String.IsNullOrWhiteSpace objectValue.AccessibleLabel then yield issue (Some objectValue.Id) [] "accessible label must not be blank"
              yield! duplicateIssues "layer" (scene.Layers |> List.map (fun layer -> layer.Id))
              yield! duplicateIssues "object" (objects |> List.map (fun objectValue -> objectValue.Id)) ]

        let invalidNodes, unsupportedNodes =
            objects
            |> List.map (fun objectValue -> validateScene (Some objectValue.Id) [] objectValue.Content)
            |> List.fold (fun (invalid, unsupported) (nextInvalid, nextUnsupported) -> invalid @ nextInvalid, unsupported @ nextUnsupported) ([], [])

        let invalid = identityIssues @ invalidNodes
        if not invalid.IsEmpty then SvgAdapterResult.Invalid invalid
        elif not unsupportedNodes.IsEmpty then SvgAdapterResult.Unsupported unsupportedNodes
        else SvgAdapterResult.Rendered scene

    let toScreenPoint (camera: SvgCamera) (point: Point) : Point =
        { X = camera.PanX + camera.Zoom * point.X
          Y = camera.PanY + camera.Zoom * point.Y }

    let tryToScenePoint (camera: SvgCamera) (point: Point) : Point option =
        if validCamera camera then
            Some
                { X = (point.X - camera.PanX) / camera.Zoom
                  Y = (point.Y - camera.PanY) / camera.Zoom }
        else
            None

    let tryCreateInteraction (scene: RetainedScene) =
        match project scene with
        | SvgAdapterResult.Rendered _ ->
            Ok
                { Scene = scene
                  SelectedObjectId = None
                  FocusedObjectId = None
                  CapturedPointerId = None }
        | SvgAdapterResult.Invalid issues -> Error(RetainedInteractionError.InvalidScene issues)
        | SvgAdapterResult.Unsupported issues -> Error(RetainedInteractionError.UnsupportedScene issues)

    let private reject error (state: RetainedInteractionState) = { State = state; Error = Some error }
    let private accept (state: RetainedInteractionState) = { State = state; Error = None }

    let private selectableObjects (scene: RetainedScene) =
        scene.Layers
        |> List.filter (fun layer -> layer.Visible)
        |> List.collect (fun layer -> layer.Objects)
        |> List.filter (fun objectValue -> objectValue.Selectable)

    let private findObject id (scene: RetainedScene) =
        scene.Layers
        |> List.collect (fun layer -> layer.Objects)
        |> List.tryFind (fun objectValue -> objectValue.Id = id)

    let private requireRevision expected (state: RetainedInteractionState) continuation =
        if expected <> state.Scene.Revision then
            reject (RetainedInteractionError.StaleRevision(expected, state.Scene.Revision)) state
        else
            continuation ()

    let private moveFocus direction (state: RetainedInteractionState) =
        let candidates = selectableObjects state.Scene
        match candidates with
        | [] -> accept { state with FocusedObjectId = None }
        | _ ->
            let currentIndex =
                state.FocusedObjectId
                |> Option.bind (fun current -> candidates |> List.tryFindIndex (fun candidate -> candidate.Id = current))
            let nextIndex =
                match direction, currentIndex with
                | 1, Some index -> (index + 1) % candidates.Length
                | -1, Some index -> (index + candidates.Length - 1) % candidates.Length
                | 1, None -> 0
                | _, None -> candidates.Length - 1
                | _ -> 0
            accept { state with FocusedObjectId = Some candidates[nextIndex].Id }

    let update message (state: RetainedInteractionState) =
        match message with
        | RetainedInteractionMessage.ReplaceScene candidate ->
            if candidate.Revision <= state.Scene.Revision then
                reject (RetainedInteractionError.NonIncreasingRevision(candidate.Revision, state.Scene.Revision)) state
            else
                match project candidate with
                | SvgAdapterResult.Invalid issues -> reject (RetainedInteractionError.InvalidScene issues) state
                | SvgAdapterResult.Unsupported issues -> reject (RetainedInteractionError.UnsupportedScene issues) state
                | SvgAdapterResult.Rendered _ ->
                    let remainsSelectable id =
                        findObject id candidate |> Option.exists (fun objectValue -> objectValue.Selectable)
                    accept
                        { state with
                            Scene = candidate
                            SelectedObjectId = state.SelectedObjectId |> Option.filter remainsSelectable
                            FocusedObjectId = state.FocusedObjectId |> Option.filter remainsSelectable }
        | RetainedInteractionMessage.Select(expected, objectId) ->
            requireRevision expected state (fun () ->
                match findObject objectId state.Scene with
                | None -> reject (RetainedInteractionError.UnknownObject objectId) state
                | Some objectValue when not objectValue.Selectable -> reject (RetainedInteractionError.ObjectNotSelectable objectId) state
                | Some _ -> accept { state with SelectedObjectId = Some objectId; FocusedObjectId = Some objectId })
        | RetainedInteractionMessage.ClearSelection expected ->
            requireRevision expected state (fun () -> accept { state with SelectedObjectId = None })
        | RetainedInteractionMessage.FocusNext expected ->
            requireRevision expected state (fun () -> moveFocus 1 state)
        | RetainedInteractionMessage.FocusPrevious expected ->
            requireRevision expected state (fun () -> moveFocus -1 state)
        | RetainedInteractionMessage.SetCamera(expected, camera) ->
            requireRevision expected state (fun () ->
                if validCamera camera then accept { state with Scene = { state.Scene with Camera = camera } }
                else reject RetainedInteractionError.InvalidCamera state)
        | RetainedInteractionMessage.CapturePointer(expected, pointerId) ->
            requireRevision expected state (fun () -> accept { state with CapturedPointerId = Some pointerId })
        | RetainedInteractionMessage.ReleasePointer(expected, pointerId) ->
            requireRevision expected state (fun () ->
                if state.CapturedPointerId = Some pointerId then accept { state with CapturedPointerId = None }
                else reject (RetainedInteractionError.PointerNotCaptured pointerId) state)
