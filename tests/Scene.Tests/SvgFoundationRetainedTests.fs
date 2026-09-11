module SceneCapability.SvgFoundationRetainedTests

open Expecto
open FS.GG.UI.Scene

let private black = { Red = 0uy; Green = 0uy; Blue = 0uy; Alpha = 255uy }

let private paint =
    { Fill = Some black
      Stroke = None
      Opacity = 1.0
      Antialias = true
      BlendMode = BlendMode.SrcOver
      Shader = None
      ColorFilter = ColorFilter.NoColorFilter
      MaskFilter = MaskFilter.NoMaskFilter
      ImageFilter = ImageFilter.NoImageFilter
      PathEffect = PathEffect.NoPathEffect }

let private objectValue id selectable content : SemanticSceneObject =
    { Id = id
      Selectable = selectable
      AccessibleLabel = "object " + id
      Content = content }

let private retained revision camera objects : RetainedScene =
    { RootId = "root"
      Revision = revision
      Camera = camera
      Layers =
        [ { Id = "world"
            Visible = true
            Objects = objects } ] }

let private camera = { PanX = 10.5; PanY = -4.25; Zoom = 2.0 }

let private gridFixture revision =
    retained revision camera
        [ objectValue "cell-2-3" true { Nodes = [ SceneNode.PaintedRectangle({ X = 20.0; Y = 30.0; Width = 10.0; Height = 10.0 }, paint) ] }
          objectValue "path-2-3" false { Nodes = [ SceneNode.Line({ X = 25.0; Y = 35.0 }, { X = 35.0; Y = 35.0 }, paint) ] }
          objectValue "player-red" true { Nodes = [ SceneNode.Circle({ X = 25.0; Y = 35.0 }, 4.0, black); SceneNode.Text((22.0, 37.0), "R", black) ] } ]

let private freeFixture revision =
    let curve =
        { Commands =
            [ PathCommand.MoveTo { X = 0.25; Y = 1.5 }
              PathCommand.CubicTo({ X = 3.25; Y = 2.75 }, { X = 5.5; Y = -1.25 }, { X = 8.75; Y = 4.5 }) ]
          FillType = PathFillType.Winding }
    retained revision camera
        [ objectValue "orbit" true
              { Nodes =
                  [ SceneNode.Translate((0.5, 0.75), { Nodes = [ SceneNode.Path(curve, paint) ] })
                    SceneNode.FilledEllipse({ X = 7.125; Y = 3.25; Width = 2.5; Height = 2.5 }, black)
                    SceneNode.SizedText((0.25, 8.5), "orbit", 1.25, black) ] } ]

let private interaction scene =
    match SvgRetained.tryCreateInteraction scene with
    | Ok state -> state
    | Error error -> failtestf "fixture should initialize: %A" error

type private ModelCommand =
    | ModelSelect of expectedRevision: int * objectId: string
    | ModelReplace of candidateRevision: int * availableIds: string list * camera: SvgCamera
    | ModelClearSelection of expectedRevision: int
    | ModelFocusNext of expectedRevision: int
    | ModelFocusPrevious of expectedRevision: int
    | ModelSetCamera of expectedRevision: int * camera: SvgCamera
    | ModelCapturePointer of expectedRevision: int * pointerId: int
    | ModelReleasePointer of expectedRevision: int * pointerId: int

type private ModelState =
    { Revision: int
      Selected: string option
      Focused: string option
      Camera: SvgCamera
      Captured: int option
      AvailableIds: string list }

let private validModelCamera value =
    System.Double.IsFinite value.PanX
    && System.Double.IsFinite value.PanY
    && System.Double.IsFinite value.Zoom
    && value.Zoom > 0.0

let private moveModelFocus direction state =
    match state.AvailableIds with
    | [] -> None
    | available ->
        let current = state.Focused |> Option.bind (fun id -> available |> List.tryFindIndex ((=) id))
        let index =
            match direction, current with
            | 1, Some value -> (value + 1) % available.Length
            | -1, Some value -> (value + available.Length - 1) % available.Length
            | 1, None -> 0
            | _, None -> available.Length - 1
            | _ -> 0
        Some available[index]

let private modelStep command state =
    match command with
    | ModelReplace(candidate, available, replacementCamera) when candidate <= state.Revision -> state, Some "non-increasing"
    | ModelReplace(_, _, replacementCamera) when not (validModelCamera replacementCamera) -> state, Some "invalid-scene"
    | ModelReplace(candidate, available, replacementCamera) ->
        { state with
            Revision = candidate
            AvailableIds = available
            Camera = replacementCamera
            Selected = state.Selected |> Option.filter (fun id -> List.contains id available)
            Focused = state.Focused |> Option.filter (fun id -> List.contains id available) }, None
    | ModelSelect(expected, _)
    | ModelClearSelection expected
    | ModelFocusNext expected
    | ModelFocusPrevious expected
    | ModelSetCamera(expected, _)
    | ModelCapturePointer(expected, _)
    | ModelReleasePointer(expected, _) when expected <> state.Revision -> state, Some "stale"
    | ModelSelect(_, objectId) when not (List.contains objectId state.AvailableIds) -> state, Some "unknown"
    | ModelSelect(_, objectId) -> { state with Selected = Some objectId; Focused = Some objectId }, None
    | ModelClearSelection _ -> { state with Selected = None }, None
    | ModelFocusNext _ -> { state with Focused = moveModelFocus 1 state }, None
    | ModelFocusPrevious _ -> { state with Focused = moveModelFocus -1 state }, None
    | ModelSetCamera(_, nextCamera) when not (validModelCamera nextCamera) -> state, Some "invalid-camera"
    | ModelSetCamera(_, nextCamera) -> { state with Camera = nextCamera }, None
    | ModelCapturePointer(_, pointerId) -> { state with Captured = Some pointerId }, None
    | ModelReleasePointer(_, pointerId) when state.Captured <> Some pointerId -> state, Some "pointer-not-captured"
    | ModelReleasePointer _ -> { state with Captured = None }, None

let private realStep command model =
    let objects =
        [ objectValue "a" true Scene.empty; objectValue "b" true Scene.empty ]
        |> List.filter (fun value -> List.contains value.Id model.AvailableIds)
    let sourceScene = retained model.Revision model.Camera objects
    let source =
        { interaction sourceScene with
            SelectedObjectId = model.Selected
            FocusedObjectId = model.Focused
            CapturedPointerId = model.Captured }
    let message =
        match command with
        | ModelSelect(expected, id) -> RetainedInteractionMessage.Select(expected, id)
        | ModelReplace(candidate, available, replacementCamera) ->
            let replacement =
                [ objectValue "a" true Scene.empty; objectValue "b" true Scene.empty ]
                |> List.filter (fun item -> List.contains item.Id available)
            RetainedInteractionMessage.ReplaceScene(retained candidate replacementCamera replacement)
        | ModelClearSelection expected -> RetainedInteractionMessage.ClearSelection expected
        | ModelFocusNext expected -> RetainedInteractionMessage.FocusNext expected
        | ModelFocusPrevious expected -> RetainedInteractionMessage.FocusPrevious expected
        | ModelSetCamera(expected, nextCamera) -> RetainedInteractionMessage.SetCamera(expected, nextCamera)
        | ModelCapturePointer(expected, pointerId) -> RetainedInteractionMessage.CapturePointer(expected, pointerId)
        | ModelReleasePointer(expected, pointerId) -> RetainedInteractionMessage.ReleasePointer(expected, pointerId)
    SvgRetained.update message source

let private projectRealState (actual: RetainedInteractionState) =
    { Revision = actual.Scene.Revision
      Selected = actual.SelectedObjectId
      Focused = actual.FocusedObjectId
      Camera = actual.Scene.Camera
      Captured = actual.CapturedPointerId
      AvailableIds = actual.Scene.Layers |> List.collect _.Objects |> List.map _.Id }

let private projectRealError = function
    | None -> None
    | Some(RetainedInteractionError.StaleRevision _) -> Some "stale"
    | Some(RetainedInteractionError.UnknownObject _) -> Some "unknown"
    | Some(RetainedInteractionError.NonIncreasingRevision _) -> Some "non-increasing"
    | Some RetainedInteractionError.InvalidCamera -> Some "invalid-camera"
    | Some(RetainedInteractionError.InvalidScene _) -> Some "invalid-scene"
    | Some(RetainedInteractionError.PointerNotCaptured _) -> Some "pointer-not-captured"
    | Some other -> Some(sprintf "unexpected:%A" other)

[<Tests>]
let tests =
    testList "SVG foundation retained scene" [
        test "grid and continuous fixtures preserve stable identities and camera meaning" {
            for fixture in [ gridFixture 7; freeFixture 7 ] do
                match SvgRetained.project fixture with
                | SvgAdapterResult.Rendered projected ->
                    Expect.equal projected.RootId fixture.RootId "root identity is unchanged"
                    Expect.equal (projected.Layers |> List.map _.Id) (fixture.Layers |> List.map _.Id) "layer identities and order are unchanged"
                    Expect.equal
                        (projected.Layers |> List.collect _.Objects |> List.map _.Id)
                        (fixture.Layers |> List.collect _.Objects |> List.map _.Id)
                        "semantic identities and order are unchanged"
                    let source = { X = 2.25; Y = -1.5 }
                    let screen = SvgRetained.toScreenPoint projected.Camera source
                    Expect.equal screen { X = 15.0; Y = -7.25 } "screen = pan + zoom * scene"
                    Expect.equal (SvgRetained.tryToScenePoint projected.Camera screen) (Some source) "camera inverse preserves continuous coordinates"
                | other -> failtestf "fixture should be rendered by the portable adapter: %A" other
        }

        test "unsupported nodes and non-finite inputs are distinct explicit outcomes" {
            let unsupported = retained 0 camera [ objectValue "points" true { Nodes = [ SceneNode.Points([], paint) ] } ]
            match SvgRetained.project unsupported with
            | SvgAdapterResult.Unsupported [ issue ] ->
                Expect.equal issue.ObjectId (Some "points") "unsupported result identifies its semantic object"
                Expect.equal issue.NodePath [ 0 ] "unsupported result identifies its node path"
            | other -> failtestf "expected explicit unsupported result, got %A" other

            let invalid = retained 0 camera [ objectValue "bad" true { Nodes = [ SceneNode.Circle({ X = nan; Y = 0.0 }, 1.0, black) ] } ]
            match SvgRetained.project invalid with
            | SvgAdapterResult.Invalid issues ->
                Expect.isTrue (issues |> List.exists (fun issue -> issue.ObjectId = Some "bad")) "invalid result identifies its semantic object"
            | other -> failtestf "expected explicit invalid result, got %A" other
        }

        test "revision replacement preserves valid selection and rejects stale mutation" {
            let initial = interaction (gridFixture 4)
            let selected = SvgRetained.update (RetainedInteractionMessage.Select(4, "player-red")) initial
            Expect.isNone selected.Error "current revision selection applies"
            let replacement = gridFixture 5
            let replaced = SvgRetained.update (RetainedInteractionMessage.ReplaceScene replacement) selected.State
            Expect.equal replaced.State.SelectedObjectId (Some "player-red") "stable identity preserves selection across a newer revision"

            let stale = SvgRetained.update (RetainedInteractionMessage.Select(4, "cell-2-3")) replaced.State
            Expect.equal stale.State replaced.State "stale command cannot mutate state"
            Expect.equal stale.Error (Some(RetainedInteractionError.StaleRevision(4, 5))) "stale revision is observable"
        }

        test "bounded retained-interaction model corresponds to every public reducer action" {
            let states =
                [ for revision in 0 .. 1 do
                    for available in [ []; [ "a" ]; [ "b" ]; [ "a"; "b" ] ] do
                        let identities = None :: (available |> List.map Some)
                        for selected in identities do
                            for focused in identities do
                                for captured in [ None; Some 7 ] do
                                    { Revision = revision
                                      Selected = selected
                                      Focused = focused
                                      Camera = camera
                                      Captured = captured
                                      AvailableIds = available } ]
            let commands =
                [ for expected in 0 .. 2 do
                    for target in [ "a"; "b"; "missing" ] do
                        ModelSelect(expected, target)
                    ModelClearSelection expected
                    ModelFocusNext expected
                    ModelFocusPrevious expected
                    ModelSetCamera(expected, { PanX = 3.0; PanY = -2.0; Zoom = 4.0 })
                    ModelSetCamera(expected, { PanX = 3.0; PanY = -2.0; Zoom = 0.0 })
                    ModelCapturePointer(expected, 7)
                    ModelReleasePointer(expected, 7)
                    ModelReleasePointer(expected, 8)
                  for candidate in 0 .. 3 do
                    for available in [ []; [ "a" ]; [ "b" ]; [ "a"; "b" ] ] do
                        ModelReplace(candidate, available, { PanX = 1.0; PanY = -1.0; Zoom = 2.0 })
                        ModelReplace(candidate, available, { PanX = 1.0; PanY = -1.0; Zoom = 0.0 }) ]

            for state in states do
                for command in commands do
                    let expectedState, expectedError = modelStep command state
                    let actual = realStep command state
                    Expect.equal (projectRealState actual.State) expectedState $"real reducer state corresponds for {state} / {command}"
                    Expect.equal (projectRealError actual.Error) expectedError $"real reducer outcome corresponds for {state} / {command}"
        }

        test "replacement retains surviving selection and focus and clears removed identities" {
            let initial =
                { Revision = 0
                  Selected = Some "b"
                  Focused = Some "a"
                  Camera = camera
                  Captured = Some 9
                  AvailableIds = [ "a"; "b" ] }
            let retainedExpected, _ = modelStep (ModelReplace(1, [ "a"; "b" ], camera)) initial
            let retainedActual = realStep (ModelReplace(1, [ "a"; "b" ], camera)) initial
            Expect.equal (projectRealState retainedActual.State) retainedExpected "surviving identities and capture are retained"

            let clearedExpected, _ = modelStep (ModelReplace(1, [ "a" ], camera)) initial
            let clearedActual = realStep (ModelReplace(1, [ "a" ], camera)) initial
            Expect.equal clearedExpected.Selected None "removed selection is cleared by the model"
            Expect.equal clearedExpected.Focused (Some "a") "surviving focus remains"
            Expect.equal (projectRealState clearedActual.State) clearedExpected "production replacement matches retention rules"
        }

        test "mutant controls distinguish stale acceptance, mapping, retention, focus, camera, and capture" {
            let state =
                { Revision = 0
                  Selected = Some "a"
                  Focused = None
                  Camera = camera
                  Captured = Some 7
                  AvailableIds = [ "a"; "b" ] }

            let staleState = { state with Revision = 2; Focused = Some "a" }
            let staleExpected, _ = modelStep (ModelSelect(1, "b")) staleState
            Expect.notEqual { staleState with Selected = Some "b"; Focused = Some "b" } staleExpected "stale acceptance mutant is killed"

            let clearExpected, _ = modelStep (ModelClearSelection 0) state
            let mappedToFocus, _ = modelStep (ModelFocusNext 0) state
            Expect.notEqual mappedToFocus clearExpected "incorrect action mapping mutant is killed"

            let retainedExpected, _ = modelStep (ModelReplace(1, [ "a"; "b" ], camera)) { state with Focused = Some "b" }
            Expect.notEqual { retainedExpected with Selected = None; Focused = None } retainedExpected "replacement retention mutant is killed"

            let previousExpected, _ = modelStep (ModelFocusPrevious 0) state
            let nextMutant, _ = modelStep (ModelFocusNext 0) state
            Expect.notEqual nextMutant previousExpected "focus direction mutant is killed"

            let nextCamera = { PanX = 3.0; PanY = 4.0; Zoom = 5.0 }
            let cameraExpected, _ = modelStep (ModelSetCamera(0, nextCamera)) state
            let cameraMutant = { state with Camera = { state.Camera with Zoom = nextCamera.Zoom } }
            Expect.notEqual cameraMutant cameraExpected "camera field mapping mutant is killed"

            let captureExpected, captureError = modelStep (ModelReleasePointer(0, 8)) state
            Expect.equal captureError (Some "pointer-not-captured") "mismatched release remains observable"
            Expect.notEqual { captureExpected with Captured = None } captureExpected "capture ownership mutant is killed"
        }
    ]
