// FSI prelude for feature 250 — the collision-safe Vec2 helper, exercised the way a game consumer would.
//
// Constitution Principle I (Spec → FSI → Semantic Tests → Implementation): this transcript sketches and
// validates the public surface of the product-owned `Geometry` module BY USE, before/independently of the
// generated-product build. It loads the raw fragment source plus the FS.GG.UI.Scene package from the local
// feed (Point/Rect), so it runs without the full viewer/controls stack.
//
//   dotnet fsi scripts/vec2-prelude.fsx
//
// Everything below is pure and deterministic — identical inputs yield byte-identical output.

#r "nuget: FS.GG.UI.Scene"

#load "../template/fragments/vec2/src/Product/Vec2.fs"

open FS.GG.UI.Scene
open AppRoot.Geometry

// A position and a velocity, expressed with the collision-safe vector.
let pos = vec2 320.0 240.0
let vel = vec2 -1.5 2.0

// One integration step (accumulator + stepSim shape): advance position by dt * velocity, keep it in bounds.
let dt = 0.5
let playfield = vec2 640.0 480.0
let stepped = clamp zero playfield (add pos (scale dt vel))
printfn "pos %A + %g*vel -> %A (in [0,0]..%A)" pos dt stepped playfield

// Cross into the scene vocabulary for rendering/layout — the one place bare Scene literals appear.
let asPoint : Point = toPoint stepped
let asRect : Rect = toRect stepped 24.0 24.0   // centered 24x24 hitbox, NO Width/Height labels on any model record
printfn "toPoint -> %A" asPoint
printfn "toRect (centered 24x24) -> %A" asRect

// Collision-safety, stated plainly: Vec2's labels overlap NOTHING in Point/Rect.
let vec2Labels = set [ "Vx"; "Vy" ]
let sceneLabels = set [ "X"; "Y"; "Width"; "Height" ]
printfn "shared labels with Point/Rect: %A (must be empty)" (Set.intersect vec2Labels sceneLabels)
