/// Pong — continuous ball/paddle motion with a `Tick`-driven `Step` loop (US3, data-model.md).
/// Distinct from the grid games: the ball moves in floats, not cells. The serve direction is
/// drawn from the seeded `Prng`; the first side to reach the target score ends the match. The
/// right paddle tracks perfectly while the left paddle tracks slowly, so the match reaches its
/// terminal state in a bounded number of steps (SC-007). Pure MVU.
module SampleApps.Core.Games.Pong

open System
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open SampleApps.Core
open SampleApps.Core.Prng
open SampleApps.Core.Evidence

let private courtW = 40.0
let private courtH = 20.0
let private paddleH = 5.0
let private targetScore = 3
let private leftSpeed = 0.35  // the left paddle tracks slowly, so it eventually misses

type Model =
    { BallX: float
      BallY: float
      VX: float
      VY: float
      LeftY: float
      RightY: float
      Rng: Prng
      ScoreL: int
      ScoreR: int
      Over: bool
      Started: bool
      StepTimer: float }

/// The rally integrates one physics step this often (ms of real time) — paced so the ball is
/// playable rather than flying a step per frame.
let stepIntervalMs = 33.0

type Dir =
    | Up
    | Down

type Msg =
    | MoveLeft of Dir
    | Step
    | Tick of float
    | Restart

let private clamp lo hi v = max lo (min hi v)

/// Serve from the centre toward the left, with a PRNG-chosen vertical direction.
let private serve (rng: Prng): float * float * float * float * Prng =
    let r, rng' = nextBelow 2 rng
    let vy = if r = 0 then 0.6 else -0.6
    courtW / 2.0, courtH / 2.0, -1.4, vy, rng'

let init (rng: Prng): Model =
    let bx, by, vx, vy, rng' = serve rng
    { BallX = bx
      BallY = by
      VX = vx
      VY = vy
      LeftY = courtH / 2.0 - paddleH / 2.0
      RightY = courtH / 2.0 - paddleH / 2.0
      Rng = rng'
      ScoreL = 0
      ScoreR = 0
      Over = false
      Started = false
      StepTimer = 0.0 }

let private within (paddleY: float) (ballY: float): bool =
    ballY >= paddleY && ballY <= paddleY + paddleH

/// One physics step: integrate the ball, bounce the walls, run the paddle AI, and score when
/// a side is missed.
let private step (model: Model): Model =
    // paddle AI: right tracks perfectly, left tracks slowly (so it misses fast balls).
    let target = model.BallY - paddleH / 2.0
    let rightY = clamp 0.0 (courtH - paddleH) target
    let leftY =
        let diff = target - model.LeftY
        model.LeftY + clamp -leftSpeed leftSpeed diff
        |> clamp 0.0 (courtH - paddleH)

    let mutable bx = model.BallX + model.VX
    let mutable by = model.BallY + model.VY
    let mutable vx = model.VX
    let mutable vy = model.VY

    if by < 0.0 then
        by <- 0.0
        vy <- -vy
    elif by > courtH then
        by <- courtH
        vy <- -vy

    let mutable scoreL = model.ScoreL
    let mutable scoreR = model.ScoreR
    let mutable rng = model.Rng

    if bx <= 0.0 then
        if within leftY by then
            bx <- 0.0
            vx <- -vx
        else
            scoreR <- scoreR + 1
            let nbx, nby, nvx, nvy, r = serve rng
            bx <- nbx; by <- nby; vx <- nvx; vy <- nvy; rng <- r
    elif bx >= courtW then
        if within rightY by then
            bx <- courtW
            vx <- -vx
        else
            scoreL <- scoreL + 1
            let nbx, nby, nvx, nvy, r = serve rng
            bx <- nbx; by <- nby; vx <- nvx; vy <- nvy; rng <- r

    { model with
        BallX = bx
        BallY = by
        VX = vx
        VY = vy
        LeftY = leftY
        RightY = rightY
        Rng = rng
        ScoreL = scoreL
        ScoreR = scoreR
        Over = (max scoreL scoreR) >= targetScore }

let update (msg: Msg) (model: Model): Model =
    match msg with
    | Restart ->
        if model.Over then { init model.Rng with Started = true }
        elif not model.Started then { model with Started = true }
        else model
    | _ when model.Over || not model.Started -> model
    | Tick dt ->
        let t = model.StepTimer + dt
        if t >= stepIntervalMs then step { model with StepTimer = t - stepIntervalMs } else { model with StepTimer = t }
    | Step -> step model
    | MoveLeft Up -> { model with LeftY = clamp 0.0 (courtH - paddleH) (model.LeftY - 1.0) }
    | MoveLeft Down -> { model with LeftY = clamp 0.0 (courtH - paddleH) (model.LeftY + 1.0) }

let mapKey (key: ViewerKey) (pressed: bool): Msg option =
    if not pressed then
        None
    else
        match key with
        | Enter -> Some Restart
        | ArrowUp -> Some(MoveLeft Up)
        | ArrowDown -> Some(MoveLeft Down)
        | _ -> None

let tick (dt: TimeSpan): Msg option = Some(Tick dt.TotalMilliseconds)

let private sc = 12.0           // court-units → pixels
let private headerPx = 38.0
let private courtPxW = courtW * sc
let private courtPxH = courtH * sc
let private paddlePxW = 8.0

/// Paint the court, both paddles, and the ball as filled `SceneNode.Rectangle`s (origin 0,0).
let private boardScene (model: Model): Scene =
    let bg = Scene.rectangle (0.0, 0.0, courtPxW, courtPxH) (Colors.rgb 30uy 41uy 59uy)
    let midline = Scene.rectangle (courtPxW / 2.0 - 1.0, 0.0, 2.0, courtPxH) (Colors.rgb 51uy 65uy 85uy)
    let leftPaddle = Scene.rectangle (0.0, model.LeftY * sc, paddlePxW, paddleH * sc) (Colors.rgb 59uy 130uy 246uy)
    let rightPaddle = Scene.rectangle (courtPxW - paddlePxW, model.RightY * sc, paddlePxW, paddleH * sc) (Colors.rgb 239uy 68uy 68uy)
    let ball = Scene.rectangle (model.BallX * sc - 5.0, model.BallY * sc - 5.0, 10.0, 10.0) (Colors.rgb 250uy 204uy 21uy)
    Scene.group [ bg; midline; leftPaddle; rightPaddle; ball ]

let private phaseLabel (model: Model): string =
    if not model.Started then "READY" elif model.Over then "MATCH OVER" else "rally"

let private overlay (model: Model): Scene list =
    let banner (line1: string) (line2: string): Scene list =
        let bx, by, bw, bh = 20.0, headerPx + courtPxH / 2.0 - 40.0, courtPxW - 40.0, 80.0
        [ Scene.rectangle (bx, by, bw, bh) (Colors.rgba 15uy 23uy 42uy 235uy)
          Scene.sizedText (bx + 16.0, by + 32.0) line1 22.0 (Colors.rgb 226uy 232uy 240uy)
          Scene.sizedText (bx + 16.0, by + 62.0) line2 15.0 (Colors.rgb 148uy 163uy 184uy) ]
    if not model.Started then banner "PONG" "Press ENTER to start"
    elif model.Over then banner "MATCH OVER" "Press ENTER to play again"
    else []

/// The full evidence/live frame: a HUD score over the court (real `Scene` graphics) plus a
/// Ready / match-over overlay.
let renderScene (_size: Size) (model: Model): Scene =
    let title =
        Scene.sizedText
            (8.0, 26.0)
            (sprintf "PONG   %d : %d   %s" model.ScoreL model.ScoreR (phaseLabel model))
            20.0
            (Colors.rgb 226uy 232uy 240uy)
    Scene.group
        ([ Scene.rectangle (0.0, 0.0, courtPxW, headerPx + courtPxH) (Colors.rgb 17uy 24uy 39uy)
           title
           Scene.translate 0.0 headerPx (boardScene model) ]
         @ overlay model)

/// Control tree for the deterministic golden + coverage; visible pixels come from `renderScene`.
let view (_size: Size) (model: Model): Control<Msg> =
    let status =
        Label.create
            [ Label.text (sprintf "PONG   %d : %d   %s" model.ScoreL model.ScoreR (phaseLabel model))
              Attr.width courtPxW ]
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children [ status; Harness.canvas "pong-court" courtPxW courtPxH (fun () -> boardScene model) ] ]

// --- evidence wiring ----------------------------------------------------------------

let deriveOutcome (model: Model): ExpectedOutcome =
    { Kind = "game"
      Values =
        [ "terminal", (if model.Over then "match-over" else "rally")
          "scoreL", string model.ScoreL
          "scoreR", string model.ScoreR ] }

let private noMods: KeyModifiers = { Ctrl = false; Alt = false; Shift = false; Meta = false }
let private stepTick: FrameInput<Msg> = FrameInput.Tick(TimeSpan.FromMilliseconds stepIntervalMs)

/// A seeded script: start the match, then run the rally clock until a side reaches the target
/// score (terminal).
let script: FrameInput<Msg> list =
    [ FrameInput.Key(Enter, noMods) ] @ [ for _ in 0 .. 299 -> stepTick ] @ [ FrameInput.Idle ]

/// Pinned literal — filled from the deterministic seed-7 run.
let expected: ExpectedOutcome =
    { Kind = "game"
      Values = [ "terminal", "match-over"; "scoreL", "0"; "scoreR", "3" ] }

let private hostFor (seedValue: int) (mode) (accent) =
    Harness.host (fun () -> init (seed seedValue)) update view mapKey (fun _ -> None) tick (SampleTheme.resolve mode accent)

let recordAt (seedValue: int): SampleEvidenceRecord =
    Harness.recordFor "pong" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue

let entry: Harness.SampleEntry =
    { Id = "pong"
      Family = "game"
      Title = "Pong"
      Controls = [ "stack"; "label"; "custom-control" ]
      Inputs = [ "keyboard"; "timing-step" ]
      RunEvidence = fun seedValue outDir -> Harness.evidenceForScene renderScene "pong" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue outDir
      Interactive = fun _ -> Harness.runInteractiveScene "Pong — Sample Apps" { Width = int courtPxW; Height = int (headerPx + courtPxH) } (fun () -> init (seed 7)) update renderScene mapKey tick
      Outcome = expected }
