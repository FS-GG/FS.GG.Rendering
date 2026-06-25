module CanvasDemo.Game

// Feature 191 (US3, T037/T038): a deterministic embedded-canvas mini-game. The simulation is a pure
// fixed-timestep transition advanced by FS.GG.UI.Canvas `Loop.advance`; the scene is composed from
// `Elements`; input arrives as raw pointer/key messages from the `canvas` control. Nothing reads a wall
// clock — a seed + a scripted tick/input sequence reproduces an identical world and scene every run.

open FS.GG.UI.Scene
open FS.GG.UI.Canvas
open FS.GG.UI.Controls
open FS.GG.UI.KeyboardInput

[<Literal>]
let Width = 320.0

[<Literal>]
let Height = 200.0

[<Literal>]
let Radius = 8.0

[<Literal>]
let PaddleW = 64.0

[<Literal>]
let PaddleH = 8.0

/// The nominal fixed simulation step (also the interval the host tick carries).
let dt = 1.0 / 60.0

type World =
    { BallX: float
      BallY: float
      Vx: float
      Vy: float
      PaddleX: float
      Score: int }

type Model =
    { Step: StepState<World>
      /// The paddle's desired centre X — the reconstructed input level state (D7/FR-010).
      PaddleTarget: float
      Seed: int }

type Msg =
    | Tick of float // elapsed seconds carried by the host tick
    | Key of ViewerKey * KeyModifiers
    | Point of PointerSample

let private seedWorld (seed: int) : World =
    { BallX = Width / 2.0
      BallY = Height / 2.0
      Vx = float (40 + seed % 13)
      Vy = float (33 + seed % 7)
      PaddleX = Width / 2.0 - PaddleW / 2.0
      Score = 0 }

let init (seed: int) : Model =
    { Step = Loop.init (seedWorld seed); PaddleTarget = Width / 2.0; Seed = seed }

// Held-input reconstruction pattern (D7/FR-010): `PaddleTarget` is the reconstructed level state. A raw
// ViewerKey carries no up/down, so each arrow key nudges the target and a pointer sets it absolutely; a
// host with a real key-up channel would instead keep a `Set<ViewerKey>` cleared per fixed step (see
// quickstart.md). The simulation `integrate` reads ONLY (world, dt) + the captured target, so it stays a
// pure, deterministic transition.
let private integrate (target: float) (w: World) (dt: float) : World =
    let nx = w.BallX + w.Vx * dt
    let ny = w.BallY + w.Vy * dt
    let vx = if nx < Radius || nx > Width - Radius then -w.Vx else w.Vx
    let paddleX = w.PaddleX + (target - PaddleW / 2.0 - w.PaddleX) * min 1.0 (dt * 8.0)
    let onPaddle = ny > Height - Radius - PaddleH && nx > paddleX && nx < paddleX + PaddleW

    let vy, score =
        if onPaddle then -(abs w.Vy), w.Score + 1
        elif ny > Height - Radius then -w.Vy, w.Score
        else w.Vy, w.Score

    { BallX = w.BallX + vx * dt
      BallY = w.BallY + vy * dt
      Vx = vx
      Vy = vy
      PaddleX = paddleX
      Score = score }

let update (msg: Msg) (model: Model) : Model =
    match msg with
    | Tick elapsed -> { model with Step = Loop.advance dt (integrate model.PaddleTarget) elapsed model.Step }
    | Key(k, _) ->
        let delta =
            match k with
            | ArrowLeft -> -20.0
            | ArrowRight -> 20.0
            | _ -> 0.0

        { model with PaddleTarget = max 0.0 (min Width (model.PaddleTarget + delta)) }
    | Point s -> { model with PaddleTarget = max 0.0 (min Width s.X) }

// The sample's OWN world interpolation (no framework lerp/interpolation API — `Loop.alpha` supplies only
// the factor): blend Previous→Current so rendering is smooth between fixed steps.
let private lerp (a: World) (b: World) (t: float) : World =
    { b with
        BallX = a.BallX + (b.BallX - a.BallX) * t
        BallY = a.BallY + (b.BallY - a.BallY) * t
        PaddleX = a.PaddleX + (b.PaddleX - a.PaddleX) * t }

let renderScene (model: Model) : Scene =
    let w = lerp model.Step.Previous model.Step.Current (Loop.alpha dt model.Step)

    Elements.layer
        [ Elements.at (w.BallX - Radius) (w.BallY - Radius) (Elements.circle Radius (Colors.rgb 60uy 160uy 240uy))
          Elements.at w.PaddleX (Height - PaddleH) (Elements.rect PaddleW PaddleH (Paint.fill (Colors.rgb 230uy 230uy 230uy))) ]

let view (_: Size) (model: Model) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Canvas.create
                    [ Attr.width Width
                      Attr.height Height
                      Canvas.volatile'
                      Canvas.scene (renderScene model)
                      Canvas.onPointer Point
                      Canvas.onKey (fun k m -> Key(k, m)) ]
                |> Control.withKey "play"
                TextBlock.create [ TextBlock.text (sprintf "score %d" model.Step.Current.Score) ]
                |> Control.withKey "score" ] ]

/// Deterministic headless evidence: fold a scripted tick/input sequence from a seed and return the
/// emitted scene's canonical fingerprint. Same seed + same script ⇒ identical fingerprint every run.
let evidence (seed: int) (script: Msg list) : string =
    let final = script |> List.fold (fun m msg -> update msg m) (init seed)
    SceneCodec.packageIdentity (SceneCodec.export (renderScene final)).CanonicalBytes
