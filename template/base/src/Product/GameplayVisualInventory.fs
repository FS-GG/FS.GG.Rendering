module AppRoot.GameplayVisualInventory

//#if (profile == "game")
// Runtime-owned visual inventory and registry. This is deliberately independent of
// tests/Product.Tests/element-visuals.catalog: the catalog describes dispositions, while this module
// declares the gameplay elements that MUST receive one and supplies the projection the real View uses.

open FS.GG.UI.Scene
open Microsoft.FSharp.Reflection
open AppRoot.Model

type GameplayVisualElement =
    | Ball
    | LeftPaddle
    | RightPaddle
    | Score
    | Playfield

let all =
    FSharpType.GetUnionCases typeof<GameplayVisualElement>
    |> Array.map (fun case -> FSharpValue.MakeUnion(case, [||]) :?> GameplayVisualElement)
    |> Array.toList

let elementId =
    function
    | Ball -> "Ball"
    | LeftPaddle -> "LeftPaddle"
    | RightPaddle -> "RightPaddle"
    | Score -> "Score"
    | Playfield -> "Playfield"

type VisualBinding =
    { Element: GameplayVisualElement
      Handle: string
      RequiredStates: (string * Model) list
      Project: Model -> Scene }

type RuntimeProjection =
    { Element: GameplayVisualElement
      Handle: string
      Scene: Scene }

let private foreground: Color =
    { Red = 240uy
      Green = 240uy
      Blue = 240uy
      Alpha = 255uy }

let private accent: Color =
    { Red = 120uy
      Green = 200uy
      Blue = 255uy
      Alpha = 255uy }

let private playfieldFill: Color =
    { Red = 18uy
      Green = 22uy
      Blue = 30uy
      Alpha = 255uy }

let private stepped = stepSim initialModel
let private movedLeft = movePaddle LeftSide PaddleDown initialModel
let private scored = { initialModel with LeftScore = initialModel.LeftScore + 1 }

let private binding =
    function
    | Ball ->
        { Element = Ball
          Handle = "scene/ball"
          RequiredStates = [ "initial", initialModel; "advanced", stepped ]
          Project =
            fun model ->
                let ball = model.Ball.Pos
                { Nodes = [ Rectangle((ball.Vx - 6.0, ball.Vy - 6.0, 12.0, 12.0), accent) ] } }
    | LeftPaddle ->
        { Element = LeftPaddle
          Handle = "scene/left-paddle"
          RequiredStates = [ "initial", initialModel; "moved", movedLeft ]
          Project =
            fun model ->
                { Nodes = [ Rectangle((16.0, model.LeftPaddleY, 8.0, model.PaddleHeight), foreground) ] } }
    | RightPaddle ->
        { Element = RightPaddle
          Handle = "scene/right-paddle"
          RequiredStates = [ "initial", initialModel ]
          Project =
            fun model ->
                { Nodes =
                    [ Rectangle(
                          (model.Playfield.Vx - 24.0, model.RightPaddleY, 8.0, model.PaddleHeight),
                          foreground
                      ) ] } }
    | Score ->
        { Element = Score
          Handle = "scene/score"
          RequiredStates = [ "initial", initialModel; "scored", scored ]
          Project =
            fun model ->
                { Nodes =
                    [ Text(
                          (model.Playfield.Vx / 2.0 - 28.0, 28.0),
                          $"{model.LeftScore} : {model.RightScore}",
                          foreground
                      ) ] } }
    | Playfield ->
        { Element = Playfield
          Handle = "scene/playfield"
          RequiredStates = [ "initial", initialModel ]
          Project =
            fun model ->
                { Nodes = [ Rectangle((0.0, 0.0, model.Playfield.Vx, model.Playfield.Vy), playfieldFill) ] } }

let bindings = all |> List.map binding

let registeredBindings =
    bindings
    |> List.map (fun item -> elementId item.Element, item.Handle)

let representativeModels =
    [ initialModel; stepped; movedLeft; scored ]

let project (model: Model) : RuntimeProjection list =
    bindings
    |> List.map (fun item ->
        { Element = item.Element
          Handle = item.Handle
          Scene = item.Project model })
//#endif
