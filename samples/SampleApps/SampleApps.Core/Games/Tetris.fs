/// Tetris — the MVP game (US1, data-model.md, research R3/R4/R6). A `Tick`-driven gravity
/// loop over a pure MVU core: time advances only through injected `Tick` deltas mapped to a
/// `Gravity` step, the 7-bag order is drawn from the seeded `Prng`, and `update` is pure
/// (Principle IV). The seeded script hard-drops pieces until the stack reaches the top, so
/// the run reaches its terminal `game-over` within a bounded number of steps (SC-007).
module SampleApps.Core.Games.Tetris

open System
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open SampleApps.Core
open SampleApps.Core.Prng
open SampleApps.Core.Evidence

let private width = 10
let private height = 20

/// An active tetromino: a piece kind (0..6), its rotation (0..3), and top-left origin.
type Piece = { Kind: int; Rot: int; Row: int; Col: int }

/// The MVU model. `Board` is a flat `row*width+col` grid (0 = empty, kind+1 = colour). The
/// `Prng` lives in the model so the 7-bag refill stays referentially transparent. `Started`
/// gates play (a Ready screen until the player starts); `DropTimer` accumulates real frame
/// time so gravity drops one row every `dropIntervalMs`, not once per frame.
type Model =
    { Board: int[]
      Active: Piece
      Bag: int list
      Rng: Prng
      Score: int
      Cleared: int
      Over: bool
      Started: bool
      DropTimer: float }

/// Gravity advances one row this often (ms of real time); a frame tick only drops a row once
/// the accumulator crosses it, so the fall is paced rather than once-per-frame.
let dropIntervalMs = 600.0

type Msg =
    | Left
    | Right
    | RotateCW
    | SoftDrop
    | HardDrop
    | Gravity
    | Tick of float    // ms elapsed this frame
    | Restart          // start from the Ready screen, or restart after game over

// --- tetromino geometry -----------------------------------------------------------

/// The bounding-box size for a piece kind (I uses 4, O uses 2, the rest 3).
let private boxSize (kind: int): int =
    match kind with
    | 0 -> 4 // I
    | 1 -> 2 // O
    | _ -> 3

/// The base (rotation-0) cells of a piece kind within its bounding box.
let private baseCells (kind: int): (int * int) list =
    match kind with
    | 0 -> [ 1, 0; 1, 1; 1, 2; 1, 3 ]      // I
    | 1 -> [ 0, 0; 0, 1; 1, 0; 1, 1 ]      // O
    | 2 -> [ 0, 1; 1, 0; 1, 1; 1, 2 ]      // T
    | 3 -> [ 0, 1; 0, 2; 1, 0; 1, 1 ]      // S
    | 4 -> [ 0, 0; 0, 1; 1, 1; 1, 2 ]      // Z
    | 5 -> [ 0, 0; 1, 0; 1, 1; 1, 2 ]      // J
    | _ -> [ 0, 2; 1, 0; 1, 1; 1, 2 ]      // L

/// Rotate a cell clockwise once within an N×N box: (r,c) -> (c, N-1-r).
let private rotateCW1 (n: int) (r: int, c: int): int * int = c, n - 1 - r

/// The cells of a piece kind at a rotation, within its box.
let private rotatedCells (kind: int) (rot: int): (int * int) list =
    let n = boxSize kind
    let mutable cells = baseCells kind
    for _ in 1 .. (rot % 4) do
        cells <- cells |> List.map (rotateCW1 n)
    cells

/// The absolute board cells a piece occupies.
let private pieceCells (p: Piece): (int * int) list =
    rotatedCells p.Kind p.Rot |> List.map (fun (r, c) -> p.Row + r, p.Col + c)

/// Whether a piece collides with the walls, floor, or settled blocks. Cells above the top
/// edge (row < 0) are allowed (spawn headroom).
let private collides (board: int[]) (p: Piece): bool =
    pieceCells p
    |> List.exists (fun (r, c) ->
        c < 0 || c >= width || r >= height || (r >= 0 && board.[r * width + c] <> 0))

// --- 7-bag --------------------------------------------------------------------------

/// Draw the next piece kind, refilling + shuffling the 7-bag from the `Prng` when empty.
let private nextFromBag (bag: int list) (rng: Prng): int * int list * Prng =
    match bag with
    | k :: rest -> k, rest, rng
    | [] ->
        let filled, rng' = shuffle [ 0; 1; 2; 3; 4; 5; 6 ] rng
        match filled with
        | k :: rest -> k, rest, rng'
        | [] -> 0, [], rng' // unreachable (the bag is never empty after a shuffle)

/// Spawn the next piece at the top. Sets `Over` when the fresh piece immediately collides.
let private spawn (model: Model): Model =
    let kind, bag, rng = nextFromBag model.Bag model.Rng
    let piece = { Kind = kind; Rot = 0; Row = 0; Col = 3 }
    if collides model.Board piece then
        { model with Active = piece; Bag = bag; Rng = rng; Over = true }
    else
        { model with Active = piece; Bag = bag; Rng = rng }

// --- locking + line clears ----------------------------------------------------------

let private lineScore = [| 0; 100; 300; 500; 800 |]

/// Settle the active piece into the board, clear any full rows, score them, and spawn next.
let private lockAndClear (model: Model): Model =
    let board = Array.copy model.Board
    for r, c in pieceCells model.Active do
        if r >= 0 && r < height && c >= 0 && c < width then
            board.[r * width + c] <- model.Active.Kind + 1
    // surviving rows (top→bottom), then re-pad empties on top.
    let rows = [ for r in 0 .. height - 1 -> Array.sub board (r * width) width ]
    let kept = rows |> List.filter (fun row -> Array.exists (fun v -> v = 0) row)
    let clearedNow = height - List.length kept
    let empties = List.replicate clearedNow (Array.zeroCreate<int> width)
    let newBoard = Array.concat (empties @ kept)
    spawn
        { model with
            Board = newBoard
            Score = model.Score + lineScore.[min clearedNow 4]
            Cleared = model.Cleared + clearedNow }

/// Try to move/rotate the active piece; reject the candidate if it collides.
let private tryUpdate (model: Model) (candidate: Piece): Model =
    if collides model.Board candidate then model else { model with Active = candidate }

/// One gravity step: drop the piece a row, or lock+clear+spawn if it cannot fall.
let private gravity (model: Model): Model =
    let down = { model.Active with Row = model.Active.Row + 1 }
    if collides model.Board down then lockAndClear model else { model with Active = down }

/// Drop straight to the floor and lock.
let private hardDrop (model: Model): Model =
    let mutable p = model.Active
    while not (collides model.Board { p with Row = p.Row + 1 }) do
        p <- { p with Row = p.Row + 1 }
    lockAndClear { model with Active = p }

// --- MVU ----------------------------------------------------------------------------

/// Seed the game: an empty board and the first spawned piece, all randomness from `Prng`.
/// Starts on the Ready screen (`Started = false`) — gravity does not run until the player
/// presses Enter.
let init (rng: Prng): Model =
    let start =
        { Board = Array.zeroCreate (width * height)
          Active = { Kind = 0; Rot = 0; Row = 0; Col = 3 }
          Bag = []
          Rng = rng
          Score = 0
          Cleared = 0
          Over = false
          Started = false
          DropTimer = 0.0 }
    spawn start

/// Pure reducer. On the Ready screen only `Restart` (start) is honoured; a terminal (`Over`)
/// model only honours `Restart` (a fresh seeded game). Gameplay input is otherwise gated to
/// the Playing phase, and gravity is paced by the `Tick` accumulator (bounded loop, SC-007).
let update (msg: Msg) (model: Model): Model =
    match msg with
    | Restart ->
        if model.Over then { init model.Rng with Started = true } // fresh game off the live RNG
        elif not model.Started then { model with Started = true }  // begin from Ready
        else model
    | _ when model.Over || not model.Started -> model              // ignore play until started
    | Tick dt ->
        let t = model.DropTimer + dt
        if t >= dropIntervalMs then gravity { model with DropTimer = t - dropIntervalMs }
        else { model with DropTimer = t }
    | Left -> tryUpdate model { model.Active with Col = model.Active.Col - 1 }
    | Right -> tryUpdate model { model.Active with Col = model.Active.Col + 1 }
    | RotateCW -> tryUpdate model { model.Active with Rot = (model.Active.Rot + 1) % 4 }
    | SoftDrop -> gravity model
    | HardDrop -> hardDrop model
    | Gravity -> gravity model

/// Map a key to a move. Enter starts/restarts; arrows steer, Up rotates, Down soft-drops,
/// Space hard-drops.
let mapKey (key: ViewerKey) (pressed: bool): Msg option =
    if not pressed then
        None
    else
        match key with
        | Enter -> Some Restart
        | ArrowLeft -> Some Left
        | ArrowRight -> Some Right
        | ArrowUp -> Some RotateCW
        | ArrowDown -> Some SoftDrop
        | Space -> Some HardDrop
        | _ -> None

/// A frame tick carries the elapsed milliseconds; gravity advances only when the accumulator
/// crosses `dropIntervalMs`.
let tick (dt: TimeSpan): Msg option = Some(Tick dt.TotalMilliseconds)

// --- view ---------------------------------------------------------------------------

/// Classic per-kind tetromino colours (I/O/T/S/Z/J/L).
let private pieceColor (kind: int): Color =
    match kind with
    | 0 -> Colors.rgb 45uy 212uy 191uy   // I — cyan
    | 1 -> Colors.rgb 250uy 204uy 21uy   // O — yellow
    | 2 -> Colors.rgb 168uy 85uy 247uy   // T — purple
    | 3 -> Colors.rgb 34uy 197uy 94uy    // S — green
    | 4 -> Colors.rgb 239uy 68uy 68uy    // Z — red
    | 5 -> Colors.rgb 59uy 130uy 246uy   // J — blue
    | _ -> Colors.rgb 249uy 115uy 22uy   // L — orange

let private cellPx = 26.0
let private headerPx = 38.0
let private boardW = float width * cellPx
let private boardH = float height * cellPx

let private cellKind (model: Model) (occupied: Map<int * int, int>) (r: int) (c: int): int option =
    match Map.tryFind (r, c) occupied with
    | Some k -> Some k
    | None -> if model.Board.[r * width + c] <> 0 then Some(model.Board.[r * width + c] - 1) else None

/// Paint just the well + settled/active blocks (origin at 0,0), each a filled
/// `SceneNode.Rectangle` in its piece colour over a dark well.
let private boardScene (model: Model): Scene =
    let occupied =
        pieceCells model.Active
        |> List.choose (fun (r, c) -> if r >= 0 then Some((r, c), model.Active.Kind) else None)
        |> Map.ofList
    let bg = Scene.rectangle (0.0, 0.0, boardW, boardH) (Colors.rgb 30uy 41uy 59uy)
    let blocks =
        [ for r in 0 .. height - 1 do
              for c in 0 .. width - 1 do
                  match cellKind model occupied r c with
                  | Some k ->
                      Scene.rectangle
                          (float c * cellPx + 1.0, float r * cellPx + 1.0, cellPx - 2.0, cellPx - 2.0)
                          (pieceColor k)
                  | None -> () ]
    Scene.group (bg :: blocks)

let private phaseLabel (model: Model): string =
    if not model.Started then "READY" elif model.Over then "GAME OVER" else "playing"

/// A centered overlay banner (used for the Ready and Game-Over screens).
let private overlay (model: Model): Scene list =
    let banner (line1: string) (line2: string): Scene list =
        let bx, by, bw, bh = 10.0, headerPx + boardH / 2.0 - 40.0, boardW - 20.0, 80.0
        [ Scene.rectangle (bx, by, bw, bh) (Colors.rgba 15uy 23uy 42uy 235uy)
          Scene.sizedText (bx + 14.0, by + 30.0) line1 18.0 (Colors.rgb 226uy 232uy 240uy)
          Scene.sizedText (bx + 14.0, by + 58.0) line2 13.0 (Colors.rgb 148uy 163uy 184uy) ]
    if not model.Started then banner "TETRIS" "ENTER to start"
    elif model.Over then banner "GAME OVER" "ENTER to play again"
    else []

/// The full evidence/live frame: a HUD title over the colored block board, plus a Ready /
/// Game-Over overlay. Painted with the public `Scene` primitives (real graphics — not ASCII).
let renderScene (_size: Size) (model: Model): Scene =
    let title =
        Scene.sizedText
            (8.0, 24.0)
            (sprintf "TETRIS  %d pts  %d rows  %s" model.Score model.Cleared (phaseLabel model))
            14.0
            (Colors.rgb 226uy 232uy 240uy)
    Scene.group
        ([ Scene.rectangle (0.0, 0.0, boardW, headerPx + boardH) (Colors.rgb 17uy 24uy 39uy)
           title
           Scene.translate 0.0 headerPx (boardScene model) ]
         @ overlay model)

/// The control tree feeds the deterministic `Perf.runScript` golden + the coverage claim
/// (`stack`/`label`/`custom-control`). The visible pixels come from `renderScene` via the
/// scene path. Coverage: `stack`, `label`, `custom-control` (the painted board surface).
let view (_size: Size) (model: Model): Control<Msg> =
    let status =
        Label.create
            [ Label.text (sprintf "TETRIS   score %d   rows %d   %s" model.Score model.Cleared (phaseLabel model))
              Attr.width boardW ]
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children [ status; Harness.canvas "tetris-board" boardW boardH (fun () -> boardScene model) ] ]

// --- evidence wiring ----------------------------------------------------------------

/// The achieved acceptance outcome derived from the final model (research R6).
let deriveOutcome (model: Model): ExpectedOutcome =
    { Kind = "game"
      Values =
        [ "terminal", (if model.Over then "game-over" else "playing")
          "clearedRows", string model.Cleared
          "score", string model.Score ] }

/// A seeded headless script: nudge left/right a little, then hard-drop, repeatedly, until
/// the stack tops out. Fixed deltas + a fixed structure ⇒ a deterministic frame sequence.
let private noMods: KeyModifiers = { Ctrl = false; Alt = false; Shift = false; Meta = false }
let private press (k: ViewerKey): FrameInput<Msg> = FrameInput.Key(k, noMods)
let private gravityTick: FrameInput<Msg> = FrameInput.Tick(TimeSpan.FromMilliseconds 500.0)

let script: FrameInput<Msg> list =
    // Start the game (leave the Ready screen), then spread successive pieces across all
    // columns (spawn column is 3) and keep dropping until the stack tops out.
    [ yield press Enter
      for i in 0 .. 79 do
        let target = i % 9
        if target < 3 then
            for _ in 1 .. (3 - target) -> press ArrowLeft
        elif target > 3 then
            for _ in 1 .. (target - 3) -> press ArrowRight
        yield press Space
        yield gravityTick ]
    @ [ FrameInput.Idle ]

/// The authored acceptance outcome for seed 7 (pinned literal; asserted by BuildOutcomeTests).
/// The game is fully deterministic in the seed, so this is the stable seed-7 result: the
/// scripted hard-drops top the stack out (the bounded terminal state, SC-007). Real-piece play
/// leaves holes, so this seed clears no full lines — the line-clear/scoring LOGIC is proven
/// separately by the pure-update test (`clears a full row and scores it`). Pinning the literal
/// (not a recomputed value) makes any future behavioural regression fail the build-outcome gate.
let expected: ExpectedOutcome =
    { Kind = "game"
      Values = [ "terminal", "game-over"; "clearedRows", "0"; "score", "0" ] }

let private hostFor (seedValue: int) (mode) (accent) =
    Harness.host (fun () -> init (seed seedValue)) update view mapKey (fun _ -> None) tick (SampleTheme.resolve mode accent)

/// A pure record for the suites (no disk, no GL).
let recordAt (seedValue: int): SampleEvidenceRecord =
    Harness.recordFor "tetris" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue

let entry: Harness.SampleEntry =
    { Id = "tetris"
      Family = "game"
      Title = "Tetris"
      Controls = [ "stack"; "label"; "custom-control" ]
      Inputs = [ "keyboard"; "timing-step" ]
      RunEvidence = fun seedValue outDir -> Harness.evidenceForScene renderScene "tetris" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue outDir
      Interactive = fun _ -> Harness.runInteractiveScene "Tetris — Sample Apps" { Width = int boardW; Height = int (headerPx + boardH) } (fun () -> init (seed 7)) update renderScene mapKey tick
      Outcome = expected }
