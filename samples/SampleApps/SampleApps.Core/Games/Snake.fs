/// Snake — a grid game with a directional turn + a `Tick`-driven `Advance` loop (US3,
/// data-model.md). Food placement is drawn from the seeded `Prng`; running into a wall or the
/// snake's own body is the terminal state. Pure MVU; time advances only through injected ticks.
module SampleApps.Core.Games.Snake

open System
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open SampleApps.Core
open SampleApps.Core.Prng
open SampleApps.Core.Evidence

let private gridW = 16
let private gridH = 16

type Dir =
    | Up
    | Down
    | Left
    | Right

/// The model. `Snake` is head-first; `Food` is the current pellet; the `Prng` reseeds food.
/// `Started` gates play (Ready screen); `StepTimer` accumulates real frame time so the snake
/// advances every `stepIntervalMs` rather than once per frame.
type Model =
    { Snake: (int * int) list
      Dir: Dir
      Food: int * int
      Rng: Prng
      Score: int
      Over: bool
      Started: bool
      StepTimer: float }

/// The snake advances one cell this often (ms of real time).
let stepIntervalMs = 150.0

type Msg =
    | Turn of Dir
    | Advance
    | Tick of float
    | Restart

let private delta (dir: Dir): int * int =
    match dir with
    | Up -> 0, -1
    | Down -> 0, 1
    | Left -> -1, 0
    | Right -> 1, 0

/// Reversing onto your own neck is not a legal turn.
let private opposite (a: Dir) (b: Dir): bool =
    match a, b with
    | Up, Down
    | Down, Up
    | Left, Right
    | Right, Left -> true
    | _ -> false

/// Place food on a free cell, drawn from the PRNG (retries until it misses the snake).
let private placeFood (snake: (int * int) list) (rng: Prng): (int * int) * Prng =
    let occupied = Set.ofList snake
    let rec loop (r: Prng) (fuel: int): (int * int) * Prng =
        let x, r1 = nextBelow gridW r
        let y, r2 = nextBelow gridH r1
        if fuel <= 0 || not (occupied.Contains(x, y)) then (x, y), r2 else loop r2 (fuel - 1)
    loop rng 64

/// The seeded starting model: a length-3 snake at centre heading right, with a seeded pellet.
/// Starts on the Ready screen (`Started = false`).
let init (rng: Prng): Model =
    let snake = [ 8, 8; 7, 8; 6, 8 ]
    let food, rng' = placeFood snake rng
    { Snake = snake; Dir = Right; Food = food; Rng = rng'; Score = 0; Over = false; Started = false; StepTimer = 0.0 }

/// One movement step: move the head, grow + reseed food on a pellet, terminate on collision.
let private advance (model: Model): Model =
    let hx, hy = List.head model.Snake
    let dx, dy = delta model.Dir
    let nx, ny = hx + dx, hy + dy
    let hitsWall = nx < 0 || nx >= gridW || ny < 0 || ny >= gridH
    let hitsSelf = List.contains (nx, ny) model.Snake
    if hitsWall || hitsSelf then
        { model with Over = true }
    elif (nx, ny) = model.Food then
        let grown = (nx, ny) :: model.Snake
        let food, rng' = placeFood grown model.Rng
        { model with Snake = grown; Food = food; Rng = rng'; Score = model.Score + 1 }
    else
        let moved = (nx, ny) :: (model.Snake |> List.rev |> List.tail |> List.rev)
        { model with Snake = moved }

/// Pure reducer. Ready honours only `Restart` (start); terminal honours only `Restart` (fresh
/// game); play is otherwise gated to Playing and the step is paced by the `Tick` accumulator.
let update (msg: Msg) (model: Model): Model =
    match msg with
    | Restart ->
        if model.Over then { init model.Rng with Started = true }
        elif not model.Started then { model with Started = true }
        else model
    | _ when model.Over || not model.Started -> model
    | Tick dt ->
        let t = model.StepTimer + dt
        if t >= stepIntervalMs then advance { model with StepTimer = t - stepIntervalMs } else { model with StepTimer = t }
    | Turn dir -> if opposite dir model.Dir then model else { model with Dir = dir }
    | Advance -> advance model

let mapKey (key: ViewerKey) (pressed: bool): Msg option =
    if not pressed then
        None
    else
        match key with
        | Enter -> Some Restart
        | ArrowUp -> Some(Turn Up)
        | ArrowDown -> Some(Turn Down)
        | ArrowLeft -> Some(Turn Left)
        | ArrowRight -> Some(Turn Right)
        | _ -> None

let tick (dt: TimeSpan): Msg option = Some(Tick dt.TotalMilliseconds)

let private cellPx = 26.0
let private headerPx = 38.0
let private boardW = float gridW * cellPx
let private boardH = float gridH * cellPx

/// Paint the well + the snake (head brighter than the body) + the food pellet as filled
/// `SceneNode.Rectangle`s (origin 0,0).
let private boardScene (model: Model): Scene =
    let bg = Scene.rectangle (0.0, 0.0, boardW, boardH) (Colors.rgb 30uy 41uy 59uy)
    let cell (x: int) (y: int) (color: Color) =
        Scene.rectangle (float x * cellPx + 1.0, float y * cellPx + 1.0, cellPx - 2.0, cellPx - 2.0) color
    let food = [ let fx, fy = model.Food in cell fx fy (Colors.rgb 239uy 68uy 68uy) ]
    let bodyCells =
        model.Snake
        |> List.mapi (fun i (x, y) ->
            let color = if i = 0 then Colors.rgb 134uy 239uy 172uy else Colors.rgb 34uy 197uy 94uy
            cell x y color)
    Scene.group (bg :: food @ bodyCells)

let private phaseLabel (model: Model): string =
    if not model.Started then "READY" elif model.Over then "CRASHED" else "alive"

let private overlay (model: Model): Scene list =
    let banner (line1: string) (line2: string): Scene list =
        let bx, by, bw, bh = 20.0, headerPx + boardH / 2.0 - 40.0, boardW - 40.0, 80.0
        [ Scene.rectangle (bx, by, bw, bh) (Colors.rgba 15uy 23uy 42uy 235uy)
          Scene.sizedText (bx + 16.0, by + 30.0) line1 18.0 (Colors.rgb 226uy 232uy 240uy)
          Scene.sizedText (bx + 16.0, by + 58.0) line2 13.0 (Colors.rgb 148uy 163uy 184uy) ]
    if not model.Started then banner "SNAKE" "ENTER to start · arrows steer"
    elif model.Over then banner "CRASHED" "ENTER to play again"
    else []

/// The full evidence/live frame: a HUD title over the colored grid (real `Scene` graphics)
/// plus a Ready / crashed overlay.
let renderScene (_size: Size) (model: Model): Scene =
    let title =
        Scene.sizedText
            (8.0, 24.0)
            (sprintf "SNAKE  %d pts  len %d  %s" model.Score (List.length model.Snake) (phaseLabel model))
            14.0
            (Colors.rgb 226uy 232uy 240uy)
    Scene.group
        ([ Scene.rectangle (0.0, 0.0, boardW, headerPx + boardH) (Colors.rgb 17uy 24uy 39uy)
           title
           Scene.translate 0.0 headerPx (boardScene model) ]
         @ overlay model)

/// Control tree for the deterministic golden + coverage (`stack`/`label`/`custom-control`);
/// the visible pixels come from `renderScene`.
let view (_size: Size) (model: Model): Control<Msg> =
    let status =
        Label.create
            [ Label.text (sprintf "SNAKE   score %d   len %d   %s" model.Score (List.length model.Snake) (phaseLabel model))
              Attr.width boardW ]
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children [ status; Harness.canvas "snake-board" boardW boardH (fun () -> boardScene model) ] ]

// --- evidence wiring ----------------------------------------------------------------

let deriveOutcome (model: Model): ExpectedOutcome =
    { Kind = "game"
      Values =
        [ "terminal", (if model.Over then "collision" else "alive")
          "score", string model.Score
          "length", string (List.length model.Snake) ] }

let private noMods: KeyModifiers = { Ctrl = false; Alt = false; Shift = false; Meta = false }
let private stepTick: FrameInput<Msg> = FrameInput.Tick(TimeSpan.FromMilliseconds stepIntervalMs)

/// A seeded script: start the game, then advance right until the snake hits the wall (terminal).
let script: FrameInput<Msg> list =
    [ FrameInput.Key(Enter, noMods) ] @ [ for _ in 0 .. 24 -> stepTick ] @ [ FrameInput.Idle ]

/// Pinned literal — filled from the deterministic seed-7 run.
let expected: ExpectedOutcome =
    { Kind = "game"
      Values = [ "terminal", "collision"; "score", "0"; "length", "3" ] }

let private hostFor (seedValue: int) (mode) (accent) =
    Harness.host (fun () -> init (seed seedValue)) update view mapKey (fun _ -> None) tick (SampleTheme.resolve mode accent)

let recordAt (seedValue: int): SampleEvidenceRecord =
    Harness.recordFor "snake" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue

let entry: Harness.SampleEntry =
    { Id = "snake"
      Family = "game"
      Title = "Snake"
      Controls = [ "stack"; "label"; "custom-control" ]
      Inputs = [ "keyboard"; "timing-step" ]
      RunEvidence = fun seedValue outDir -> Harness.evidenceForScene renderScene "snake" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue outDir
      Interactive = fun _ -> Harness.runInteractiveScene "Snake — Sample Apps" { Width = int boardW; Height = int (headerPx + boardH) } (fun () -> init (seed 7)) update renderScene mapKey tick
      Outcome = expected }
