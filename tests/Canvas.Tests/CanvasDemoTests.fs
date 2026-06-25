module Canvas.Tests.CanvasDemoTests

// Feature 191 (US3, T034, FR-014/SC-006): a seeded simulation built from Elements + Loop reproduces an
// identical world, emitted Scene, and scene fingerprint across two independent headless runs. This is
// the same deterministic pattern the runnable samples/CanvasDemo uses; here it is exercised purely
// (no GL, no wall clock) so the reproducibility guarantee is asserted, not assumed.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Canvas

// A tiny bouncing-ball world: position + velocity in a 100x100 box, reflected at the walls.
type private Ball = { X: float; Y: float; Vx: float; Vy: float }

let private box = 100.0
let private radius = 4.0

let private integrate (b: Ball) (dt: float) : Ball =
    let nx = b.X + b.Vx * dt
    let ny = b.Y + b.Vy * dt
    let vx = if nx < radius || nx > box - radius then -b.Vx else b.Vx
    let vy = if ny < radius || ny > box - radius then -b.Vy else b.Vy
    { X = b.X + vx * dt; Y = b.Y + vy * dt; Vx = vx; Vy = vy }

// Deterministic seed → initial world (a seed maps to a starting velocity; no RNG, fully reproducible).
let private seedWorld (seed: int) : Ball =
    { X = 50.0; Y = 50.0; Vx = float (10 + seed % 7); Vy = float (8 + seed % 5) }

let private render (b: Ball) : Scene =
    Elements.layer [ Elements.at (b.X - radius) (b.Y - radius) (Elements.circle radius (Colors.rgb 60uy 160uy 240uy)) ]

// A scripted frame-time sequence (seconds elapsed per host tick) — the only external input.
let private script = [ 0.016; 0.020; 0.013; 0.250; 0.016; 0.016; 0.009; 0.030 ]

let private run (seed: int) : StepState<Ball> * Scene * string =
    let dt = 1.0 / 60.0
    let final = script |> List.fold (fun st ft -> Loop.advance dt integrate ft st) (Loop.init (seedWorld seed))
    let scene = render final.Current
    let identity = SceneCodec.packageIdentity (SceneCodec.export scene).CanonicalBytes
    final, scene, identity

[<Tests>]
let tests =
    testList "Feature 191 seeded-sample reproducibility (US3, FR-014/SC-006)" [

        test "same seed + scripted inputs ⇒ identical world, Scene, and fingerprint across two runs" {
            let w1, s1, f1 = run 3
            let w2, s2, f2 = run 3
            Expect.equal w1 w2 "identical final StepState"
            Expect.equal s1 s2 "byte-identical emitted Scene"
            Expect.equal f1 f2 "identical scene fingerprint"
        }

        test "a different seed diverges (the simulation is genuinely seed-driven, not constant)" {
            let _, _, f1 = run 3
            let _, _, f2 = run 4
            Expect.notEqual f1 f2 "different seeds ⇒ different fingerprints"
        }

        test "the runaway 0.25s tick in the script is clamped (the world stays finite and in-box)" {
            let final, _, _ = run 1
            Expect.isTrue (final.Current.X > 0.0 && final.Current.X < box) "x stays inside the box"
            Expect.isTrue (final.Current.Y > 0.0 && final.Current.Y < box) "y stays inside the box"
        }
    ]
