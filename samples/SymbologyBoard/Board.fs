module SymbologyBoard.Board

// Feature 193 (M6): the deterministic live board. A pure fixed-timestep simulation advances each approved
// roster symbol across the board (advance + bounce), driven solely by accumulated step time `World.T`; the
// scene interpolates Previous→Current via `Loop.alpha` and overlays each unit's approved `Symbology.animate`
// motion. Nothing reads a wall clock, performs IO, or draws on render-time randomness — a seed + a scripted
// `Tick` sequence reproduces an identical world, scene, and canonical fingerprint every run (FR-003/FR-005).

open FS.GG.UI.Scene
open FS.GG.UI.Canvas
open FS.GG.UI.Symbology
open SymbologyBoard.Roster

/// The nominal fixed simulation step (also the interval the host tick carries).
let dt = 1.0 / 60.0

/// The fixed board extent (board-space units; the symbol radius is `Token.R = 30`).
let BoardWidth = 960.0
let BoardHeight = 600.0

/// One symbol's live placement on the board.
type BoardUnit =
    { Token: Token
      Motion: Motion
      X: float
      Y: float
      Vx: float
      Vy: float }

/// The deterministic simulation state. `T` is the accumulated step phase fed to `Symbology.animate`
/// (never a wall clock).
type World = { Units: BoardUnit list; T: float }

/// The MVU model — `Step` brackets Previous/Current worlds for interpolation (FS.GG.UI.Canvas `Loop`).
type Model = { Step: StepState<World>; Seed: int }

/// MVU messages. `Tick` carries the host-tick elapsed seconds; Point/Key are deferred (D8/FR-014) and
/// would have to be reconstructed deterministically and kept off the evidence path.
type Msg = Tick of float

// Pure per-(seed, index, salt) jitter in [0,1). 32-bit int hashing wraps deterministically across runs and
// platforms, so seeded positions/velocities reproduce exactly (the determinism contract, FR-005/FR-006).
let private jitter (seed: int) (i: int) (salt: int) : float =
    let v = (seed * 73856093) ^^^ ((i + 1) * 19349663) ^^^ (salt * 83492791)
    float (((v % 1000) + 1000) % 1000) / 1000.0

let private seedWorld (seed: int) : World =
    let units =
        roster
        |> List.mapi (fun i u ->
            let token = mapUnit u
            let radius = token.R
            { Token = token
              Motion = motionOf u token
              X = radius + jitter seed i 1 * (BoardWidth - 2.0 * radius)
              Y = radius + jitter seed i 2 * (BoardHeight - 2.0 * radius)
              Vx = (jitter seed i 3 - 0.5) * 2.0 * 90.0
              Vy = (jitter seed i 4 - 0.5) * 2.0 * 90.0 })

    { Units = units; T = 0.0 }

let init (seed: int) : Model =
    { Step = Loop.init (seedWorld seed); Seed = seed }

// Pure fixed-step transition: advance each unit, reflect velocity at the board edges, and clamp the centre
// to [radius, extent-radius] so the symbol stays fully on-board for any dt/velocity (FR-011). Reads only
// (world, dt) — no IO, no wall clock, no randomness.
let private integrate (w: World) (dt: float) : World =
    let stepUnit (bu: BoardUnit) =
        let r = bu.Token.R
        let minX, maxX = r, BoardWidth - r
        let minY, maxY = r, BoardHeight - r
        let nx = bu.X + bu.Vx * dt
        let ny = bu.Y + bu.Vy * dt

        let bx, vx =
            if nx < minX then minX + (minX - nx), -bu.Vx
            elif nx > maxX then maxX - (nx - maxX), -bu.Vx
            else nx, bu.Vx

        let by, vy =
            if ny < minY then minY + (minY - ny), -bu.Vy
            elif ny > maxY then maxY - (ny - maxY), -bu.Vy
            else ny, bu.Vy

        { bu with
            X = min maxX (max minX bx)
            Y = min maxY (max minY by)
            Vx = vx
            Vy = vy }

    { Units = w.Units |> List.map stepUnit; T = w.T + dt }

let update (msg: Msg) (model: Model) : Model =
    match msg with
    | Tick elapsed -> { model with Step = Loop.advance dt integrate elapsed model.Step }

// The sample's OWN position interpolation (Loop.alpha supplies only the [0,1) factor): blend each unit's
// Previous→Current centre so motion is smooth between fixed steps, then place its approved animated symbol
// there. Same roster/order in both worlds ⇒ pairwise map2 is well-defined.
let renderScene (model: Model) : Scene =
    let a = model.Step.Previous
    let b = model.Step.Current
    let t = Loop.alpha dt model.Step

    (a.Units, b.Units)
    ||> List.map2 (fun pa pb ->
        let x = pa.X + (pb.X - pa.X) * t
        let y = pa.Y + (pb.Y - pa.Y) * t
        Symbology.animate pb.Motion { pb.Token with Cx = x; Cy = y } b.T)
    |> Scene.group

/// Deterministic headless evidence: fold a scripted `Tick` sequence from a seed and return the emitted
/// scene's canonical fingerprint. Same seed + same script ⇒ identical fingerprint every run (SC-001); a
/// different seed ⇒ different placements ⇒ different fingerprint (SC-002). ONE call ⇒ ONE fingerprint —
/// the two-run repro CHECK lives in Program.fs (see contracts/cli-contract.md).
let evidence (seed: int) (script: Msg list) : string =
    let final = script |> List.fold (fun m msg -> update msg m) (init seed)
    SceneCodec.packageIdentity (SceneCodec.export (renderScene final)).CanonicalBytes
