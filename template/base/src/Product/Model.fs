module AppRoot.Model

open System
//#if (profile == "governed" || profile == "headless-scene")
open FS.GG.UI.Scene

type Model =
    { Name: string
      RenderCount: int }

type Msg =
    | Rendered
    | NoOp

let initialModel =
    { Name = "Product"
      RenderCount = 0 }

let update msg model =
    match msg with
    | Rendered -> { model with RenderCount = model.RenderCount + 1 }, []
    | NoOp -> model, []

//#else
//#if (profile == "game")
// ============================================================================================
// GAME family — minimal, replaceable Pong-style starter (feature 220).
//
//   REPLACE ME. This Model/Msg/update is the developer-owned game seam. Swap in your own game
//   by editing Model.fs + View.fs + tests/AppRoot.Tests/BehaviorTests.fs (plus the documented
//   field re-points in LayoutEvidence.fs / EvidenceCommands.fs). The durable governance spine
//   (GovernanceTests.fs, Program.fs, WindowOptions.fs) never calls update/view, so it keeps
//   passing across the swap — see docs/scaffold-map.md.
//
//   Collision-safe positions (feature 250, fs-gg-scene pitfall): a game record that names its
//   fields X/Y/Width/Height collides with FS.GG.UI.Scene.Point/Rect — because the durable
//   LayoutEvidence.fs opens BOTH Scene and this model, its bare `{ X=…; Y=…; Width=…; Height=… }`
//   Rect literals then mis-resolve to YOUR record (a wall of errors in a file you must not touch,
//   surfacing only after a whole model is written). So DON'T put X/Y/Width/Height labels on a game
//   record while Scene is open. Use the collision-safe `Geometry.Vec2` (Vx/Vy) for positions and
//   velocities, and `Geometry.toPoint`/`toRect` to cross into the scene. See Vec2.fs and the
//   `fs-gg-model-swap` / `fs-gg-game-core` skills.
// ============================================================================================
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Canvas // FixedStep.drain — the fixed-timestep accumulator drain
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Controls.Elmish.Authoring // Cmd.none / Sub.none (Elmish-convention no-ops for `[]`)
open AppRoot.Geometry // Vec2 + vec2/add/scale/clamp/toPoint/toRect (collision-safe positions)

type Ball = { Pos: Vec2; Velocity: Vec2 }

type PaddleSide =
    | LeftSide
    | RightSide

type PaddleDirection =
    | PaddleUp
    | PaddleDown

type Model =
    { Ball: Ball
      LeftPaddleY: float
      RightPaddleY: float
      PaddleHeight: float
      LeftScore: int
      RightScore: int
      Playfield: Vec2 // width = Playfield.Vx, height = Playfield.Vy (no Width/Height labels)
      SimAccumulator: float // seconds carried between Ticks for the fixed-step drain
      TickCount: int
      LastInput: ViewerKey option }

type Msg =
    | Tick of dtSeconds: float
    | MovePaddle of PaddleSide * PaddleDirection
    | ViewerInput of ViewerKey * isDown: bool
    | NoOp

// Kept model-agnostic so the durable LayoutEvidence spine validates the skeleton AND a swap.
type GeneratedLayoutValidationFailureClass =
    | MissingLayoutFacts
    | OverlappingLayoutBounds

type GeneratedLayoutValidationResult =
    { Accepted: bool
      FailureClass: GeneratedLayoutValidationFailureClass option
      Diagnostics: string list }

let playfieldWidth = 640.0
let playfieldHeight = 480.0
let paddleHeight = 96.0
let paddleSpeed = 24.0
let ballRadius = 6.0
let leftPaddleX = 16.0
let rightPaddleX = playfieldWidth - 24.0
let paddleThickness = 8.0

// One fixed simulation step is 1/60 s. `update` on `Tick dt` accumulates the host's real elapsed
// time and drains WHOLE steps out of it (FixedStep.drain), so the sim advances deterministically
// regardless of the host frame rate — the classic fixed-timestep accumulator.
let simInterval = 1.0 / 60.0

let private servedBall =
    { Pos = vec2 (playfieldWidth / 2.0) (playfieldHeight / 2.0)
      Velocity = vec2 5.0 3.0 }

let initialModel =
    { Ball = servedBall
      LeftPaddleY = (playfieldHeight - paddleHeight) / 2.0
      RightPaddleY = (playfieldHeight - paddleHeight) / 2.0
      PaddleHeight = paddleHeight
      LeftScore = 0
      RightScore = 0
      Playfield = vec2 playfieldWidth playfieldHeight
      SimAccumulator = 0.0
      TickCount = 0
      LastInput = None }

let keyName key = ViewerKeyboard.toKeyId key

let private clampPaddle model y =
    y |> max 0.0 |> min (model.Playfield.Vy - model.PaddleHeight)

let movePaddle side direction model =
    let delta =
        match direction with
        | PaddleUp -> -paddleSpeed
        | PaddleDown -> paddleSpeed

    match side with
    | LeftSide -> { model with LeftPaddleY = clampPaddle model (model.LeftPaddleY + delta) }
    | RightSide -> { model with RightPaddleY = clampPaddle model (model.RightPaddleY + delta) }

// Keyboard → paddle moves. W/S drive the left paddle; Up/Down the right paddle. Replace this
// mapping when you swap in your own game (EvidenceCommands.mapKey wraps it as ViewerInput).
//
// HOST INPUT BOUNDARY — the game family's default host is KEYBOARD-ONLY (feature 139).
// This starter launches through `Viewer.runApp` over `GeneratedAppHost` (see Program.fs), whose
// only input seam is `MapKey: ViewerKey -> bool -> _`. `ViewerKey` has NO mouse/pointer case — a
// key press arrives at the host as `DispatchInput of ViewerKey * isDown`, which is what maps to
// `ViewerInput` below. So a mouse-aimed control scheme (e.g. twin-stick WASD + mouse aim) CANNOT be
// wired here: there is no mouse to read at this site. Reading the mouse requires the pointer-aware
// interactive host — `InteractiveAppHost` driven by `Controls.Elmish.runInteractiveApp`, which adds
// a `MapPointer: ViewerPointerInput -> Size -> _ -> _` seam (the same host the `app`/controls family
// uses in Program.fs). Moving onto it is a durable, governance-scanned host-wiring change in
// Program.fs, not an edit at this mapping — decide your control scheme with that in mind.
let private paddleForKey key =
    match key with
    | Letter 'W' -> Some(LeftSide, PaddleUp)
    | Letter 'S' -> Some(LeftSide, PaddleDown)
    | ArrowUp -> Some(RightSide, PaddleUp)
    | ArrowDown -> Some(RightSide, PaddleDown)
    | _ -> None

// Pure fixed step: integrate the ball by one step, bounce off the top/bottom walls and the paddles,
// score and re-serve on a miss. Positions/velocities are `Vec2`, advanced with `add`/`scale`; the
// ball always stays inside the playfield after the step. This is your `stepSim` — edit it freely.
let stepSim model =
    let ball = model.Ball
    let next = add ball.Pos ball.Velocity // one unit step (dt folded into velocity units)

    let velocityY, clampedY =
        if next.Vy < ballRadius then -ball.Velocity.Vy, ballRadius
        elif next.Vy > model.Playfield.Vy - ballRadius then -ball.Velocity.Vy, model.Playfield.Vy - ballRadius
        else ball.Velocity.Vy, next.Vy

    let withinLeftPaddle = clampedY >= model.LeftPaddleY && clampedY <= model.LeftPaddleY + model.PaddleHeight
    let withinRightPaddle = clampedY >= model.RightPaddleY && clampedY <= model.RightPaddleY + model.PaddleHeight

    if next.Vx < leftPaddleX + paddleThickness + ballRadius then
        if withinLeftPaddle then
            { model with
                Ball =
                    { Pos = vec2 (leftPaddleX + paddleThickness + ballRadius) clampedY
                      Velocity = vec2 (abs ball.Velocity.Vx) velocityY } }
        else
            { model with
                RightScore = model.RightScore + 1
                Ball = servedBall }
    elif next.Vx > rightPaddleX - ballRadius then
        if withinRightPaddle then
            { model with
                Ball =
                    { Pos = vec2 (rightPaddleX - ballRadius) clampedY
                      Velocity = vec2 (-(abs ball.Velocity.Vx)) velocityY } }
        else
            { model with
                LeftScore = model.LeftScore + 1
                Ball = servedBall }
    else
        { model with
            Ball =
                { ball with
                    Pos = vec2 next.Vx clampedY
                    Velocity = vec2 ball.Velocity.Vx velocityY } }

// Fixed-timestep advance: fold the host's real elapsed `dt` into the carried accumulator, drain the
// whole number of `simInterval` steps out of it, and run `stepSim` that many times. `FixedStep.drain`
// is a pure FS.GG.UI.Canvas primitive (no wall-clock read), so a scripted `dt` sequence replays
// byte-identically. This is the accumulator + stepSim pattern — the shape most games want on Tick.
let private advanceSim dtSeconds model =
    let struct (steps, accumulator) = FixedStep.drain simInterval dtSeconds model.SimAccumulator

    let stepped =
        // mutable: a single unaliased accumulator over a fixed step count is plainer than a fold here.
        let mutable m = model
        for _ in 1..steps do
            m <- stepSim m
        m

    { stepped with
        SimAccumulator = accumulator
        TickCount = model.TickCount + 1 }

let init () : Model * AdapterCommand<Msg> = initialModel, Cmd.none

let update msg model : Model * AdapterCommand<Msg> =
    match msg with
    | Tick dtSeconds -> advanceSim dtSeconds model, Cmd.none
    | MovePaddle(side, direction) -> movePaddle side direction model, Cmd.none
    | ViewerInput(key, isDown) ->
        let moved =
            if isDown then
                match paddleForKey key with
                | Some(side, direction) -> movePaddle side direction model
                | None -> model
            else
                model

        { moved with LastInput = Some key }, Cmd.none
    | NoOp -> model, Cmd.none

let subscriptions _ : AdapterSubscription<Msg> list = Sub.none

//#else
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Controls.Elmish.Authoring // Cmd.none / Sub.none (Elmish-convention no-ops for `[]`)
open FS.GG.UI.DesignSystem
open FS.GG.UI.Themes.Default
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

type Model =
    { Name: string
      CanSave: bool
      Page: Page
      Interactions: int
      ContentColumn: int
      ContentRow: int
      ItemCount: int
      Step: int
      TickCount: int
      NextLabel: string
      LastInput: ViewerKey option
      InputDiagnostics: InputFlowDiagnostic list
      Revenue: ChartSeries list
      GridColumns: DataGridColumn list
      GridRows: DataGridRow list
      RichIntro: RichTextBlock }

and Page =
    | Home
    | Browse
    | Detail
    | Settings
    | Summary

and InputFlowDiagnostic =
    { InputValue: string
      RawKey: string option
      Direction: string
      Page: string
      ExpectedTransition: string
      Flow: string }

type Msg =
    | NameChanged of string
    | SaveRequested
    | GridSelectionChanged of string
    | ViewerInput of ViewerKey * isDown: bool
    | ViewerKeyEventReceived of ViewerKeyEvent
    | Tick
    | Navigated of Page
    | RuntimeMsg of ControlRuntimeMsg
    | NoOp

type GeneratedLayoutValidationFailureClass =
    | MissingLayoutFacts
    | OverlappingLayoutBounds

type GeneratedLayoutValidationResult =
    { Accepted: bool
      FailureClass: GeneratedLayoutValidationFailureClass option
      Diagnostics: string list }

let revenueSeries =
    [ { Name = "Revenue"
        Points =
          [ { X = 0.0; Y = 12.0; Label = Some "Q1" }
            { X = 1.0; Y = 18.0; Label = Some "Q2" }
            { X = 2.0; Y = 15.0; Label = Some "Q3" }
            { X = 3.0; Y = 24.0; Label = Some "Q4" } ] } ]

let gridColumns =
    [ { Key = "name"; Header = "Name"; Width = 160.0; ColumnType = TextColumn }
      { Key = "amount"; Header = "Amount"; Width = 96.0; ColumnType = NumericColumn } ]

let gridColumnsAttr: Attr<Msg> =
    DataGrid.columns gridColumns

let gridRows =
    [ let row key name amount =
          { Key = key
            Cells =
              [ { RowKey = key; ColumnKey = "name"; Value = name }
                { RowKey = key; ColumnKey = "amount"; Value = amount } ] }

      row "row-1" "North" "120"
      row "row-2" "South" "98"
      row "row-3" "West" "141" ]

let richIntro =
    let baseStyle = RichText.defaultStyle Theme.light
    let accent =
        { baseStyle with
            Weight = Bold
            Foreground = Theme.light.Accent }

    { RichText.block [ RichText.run "Product-owned " accent; RichText.run "Controls guidance" baseStyle ] with
        MaxWidth = Some 360.0
        Clip = true }

let initialModel =
    { Name = "Product"
      CanSave = true
      Page = Home
      Interactions = 0
      ContentColumn = 0
      ContentRow = 0
      ItemCount = 0
      Step = 1
      TickCount = 0
      NextLabel = "Next"
      LastInput = None
      InputDiagnostics = []
      Revenue = revenueSeries
      GridColumns = gridColumns
      GridRows = gridRows
      RichIntro = richIntro }

let pageName page =
    match page with
    | Home -> "home"
    | Browse -> "browse"
    | Detail -> "detail"
    | Settings -> "settings"
    | Summary -> "summary"

let keyName key =
    ViewerKeyboard.toKeyId key

let diagnostic flow raw direction previousPage key expected =
    { InputValue = keyName key
      RawKey = raw
      Direction = direction
      Page = pageName previousPage
      ExpectedTransition = expected
      Flow = flow }

// Neutral, non-game navigation: keys move between application pages and a content-region
// cursor (column/row) over the example controls. Pure transition over the model.
let transitionViewerInput raw direction key isDown model =
    if not isDown then
        { model with LastInput = Some key }, []
    else
        let current = model.Page

        let nextPage, interactions, contentColumn, contentRow, flow, expected =
            match current, key with
            | Home, Enter -> Browse, model.Interactions, model.ContentColumn, model.ContentRow, "home-open", "browse"
            | Home, Letter 'S' -> Settings, model.Interactions, model.ContentColumn, model.ContentRow, "settings-open", "settings"
            | Settings, Enter -> Browse, model.Interactions, model.ContentColumn, model.ContentRow, "settings-apply", "browse"
            | Settings, Escape
            | Settings, Backspace -> Home, model.Interactions, model.ContentColumn, model.ContentRow, "settings-back", "home"
            | Browse, Enter -> Detail, model.Interactions, model.ContentColumn, model.ContentRow, "open-detail", "detail"
            | Browse, ArrowLeft -> Browse, model.Interactions + 1, max 0 (model.ContentColumn - 1), model.ContentRow, "content-move", "browse"
            | Browse, ArrowRight -> Browse, model.Interactions + 1, min 9 (model.ContentColumn + 1), model.ContentRow, "content-move", "browse"
            | Browse, ArrowDown -> Browse, model.Interactions + 1, model.ContentColumn, min 19 (model.ContentRow + 1), "content-move", "browse"
            | Browse, ArrowUp -> Browse, model.Interactions + 1, model.ContentColumn, max 0 (model.ContentRow - 1), "content-move", "browse"
            | Detail, Escape
            | Detail, Backspace -> Browse, model.Interactions, model.ContentColumn, model.ContentRow, "detail-back", "browse"
            | Summary, Enter -> Home, 0, 0, 0, "restart", "home"
            | _ -> current, model.Interactions, model.ContentColumn, model.ContentRow, "ignored", pageName current

        let entry = diagnostic flow raw direction current key expected

        { model with
            Page = nextPage
            Interactions = interactions
            ContentColumn = contentColumn
            ContentRow = contentRow
            LastInput = Some key
            InputDiagnostics = entry :: model.InputDiagnostics },
        []

let dispatchViewerKey event model =
    let key, isDown = ViewerKeyboard.normalizeEvent event
    let direction = if isDown then "down" else "up"
    transitionViewerInput (Some event.RawKey) direction key isDown model

let init () : Model * AdapterCommand<Msg> =
    initialModel, Cmd.none

let update msg model : Model * AdapterCommand<Msg> =
    match msg with
    | NameChanged value -> { model with Name = value }, Cmd.none
    | SaveRequested -> model, [ DispatchHostCommand $"save:{model.Name}" ]
    | GridSelectionChanged _ -> model, Cmd.none
    | ViewerInput(key, isDown) -> transitionViewerInput None (if isDown then "down" else "up") key isDown model
    | ViewerKeyEventReceived event -> dispatchViewerKey event model
    | Tick ->
        if model.Page = Browse then
            { model with
                TickCount = model.TickCount + 1
                ContentRow = if model.ContentRow >= 19 then 0 else model.ContentRow + 1
                ItemCount = model.ItemCount + 1 },
            Cmd.none
        else
            { model with TickCount = model.TickCount + 1 }, Cmd.none
    | Navigated page -> { model with Page = page }, Cmd.none
    | RuntimeMsg _ -> model, Cmd.none
    | NoOp -> model, Cmd.none

let subscriptions _ : AdapterSubscription<Msg> list = Sub.none

//#endif
//#endif
