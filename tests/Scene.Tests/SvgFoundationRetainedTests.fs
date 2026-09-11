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
    | ModelReplace of candidateRevision: int * availableIds: string list

type private ModelState =
    { Revision: int
      Selected: string option }

let private modelStep command state =
    match command with
    | ModelSelect(expected, objectId) when expected = state.Revision && (objectId = "a" || objectId = "b") ->
        { state with Selected = Some objectId }, None
    | ModelSelect(expected, _) when expected <> state.Revision -> state, Some "stale"
    | ModelSelect _ -> state, Some "unknown"
    | ModelReplace(candidate, available) when candidate > state.Revision ->
        { Revision = candidate; Selected = state.Selected |> Option.filter (fun id -> List.contains id available) }, None
    | ModelReplace _ -> state, Some "non-increasing"

let private realStep command model =
    let objects = [ objectValue "a" true Scene.empty; objectValue "b" true Scene.empty ]
    let sourceScene = retained model.Revision camera objects
    let source = { interaction sourceScene with SelectedObjectId = model.Selected }
    let message =
        match command with
        | ModelSelect(expected, id) -> RetainedInteractionMessage.Select(expected, id)
        | ModelReplace(candidate, available) ->
            let selected = objects |> List.filter (fun item -> List.contains item.Id available)
            RetainedInteractionMessage.ReplaceScene(retained candidate camera selected)
    SvgRetained.update message source

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

        test "bounded revision-selection model corresponds to the real reducer" {
            let states =
                [ for revision in 0 .. 2 do
                    for selected in [ None; Some "a"; Some "b" ] do
                        { Revision = revision; Selected = selected } ]
            let commands =
                [ for expected in 0 .. 2 do
                    for target in [ "a"; "b"; "missing" ] do
                        ModelSelect(expected, target)
                  for candidate in 0 .. 3 do
                    for available in [ []; [ "a" ]; [ "b" ]; [ "a"; "b" ] ] do
                        ModelReplace(candidate, available) ]

            for state in states do
                for command in commands do
                    let expectedState, expectedError = modelStep command state
                    let actual = realStep command state
                    let actualState =
                        { Revision = actual.State.Scene.Revision
                          Selected = actual.State.SelectedObjectId }
                    let actualError =
                        match actual.Error with
                        | None -> None
                        | Some(RetainedInteractionError.StaleRevision _) -> Some "stale"
                        | Some(RetainedInteractionError.UnknownObject _) -> Some "unknown"
                        | Some(RetainedInteractionError.NonIncreasingRevision _) -> Some "non-increasing"
                        | Some other -> Some(sprintf "unexpected:%A" other)
                    Expect.equal actualState expectedState $"real reducer state corresponds for {state} / {command}"
                    Expect.equal actualError expectedError $"real reducer outcome corresponds for {state} / {command}"
        }

        test "bounded witness detects a reducer mutant that ignores stale revisions" {
            let state = { Revision = 2; Selected = Some "a" }
            let command = ModelSelect(1, "b")
            let expected, _ = modelStep command state
            let mutant = { state with Selected = Some "b" }
            Expect.notEqual mutant expected "the stale-revision mutation is distinguished by the model"

            let actual = realStep command state
            Expect.equal actual.State.SelectedObjectId expected.Selected "the production reducer matches the model witness"
            Expect.isSome actual.Error "the production reducer rejects the stale command"
        }
    ]
