---
name: fs-gg-elmish
description: Work on Elmish adapter contracts and generated product Elmish wiring.
---

# Elmish Capability

## Scope

Owns `src/Elmish/`, Elmish adapter tests, `template/fragments/elmish/`, and generated product Elmish entry points.

## Public Contract

The supported API lives in `src/Elmish/Elmish.fsi`. Surface changes require `readiness/surface-baselines/FS.GG.UI.Elmish.txt`.

## Build Commands

Run `./fake.sh build -t CapabilityCheck`, `./fake.sh build -t DependencyReport`, and `./fake.sh build -t PackLocal`.

## Test Commands

Run `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj` and `./fake.sh build -t GeneratedProductCheck`.

## Evidence

Record transition and effect evidence under the active feature readiness
package-surface reports when adapter behavior changes. Stable public surface
baselines live under `readiness/surface-baselines/`.

## Package Boundary

Keep `Model`, `Msg`, `Effect`, `init`, and `update` pure. Native viewer I/O belongs to SkiaViewer interpreter code.

## Generated Product

Products that select Elmish receive Scene and SkiaViewer prerequisites plus this skill.

## Runnable example

Open the package namespace and initialize the adapter over a pure user model:

```fsharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Elmish

let options = { Title = "elmish"; InitialSize = { Width = 320; Height = 240 } }
let render (count: int) = Text((10.0, 20.0), sprintf "count=%d" count, Colors.white)

let model, _effects = ElmishAdapter.init options 0 (render 0)
let next, _ = ElmishAdapter.update render (UserMsg 1) model
printfn "user model = %d" next.UserModel
```

## Canonical arcade game-loop conventions

Every arcade demo (Asteroids, Breakout, …) re-derives the same deterministic
`update`-side primitives. Capture them as canonical MVU conventions here rather
than re-implementing them per game. Each is a pure function of the model, so it
lives inside `update`, never in the interpreter.

**Shipped helper: deterministic seeded RNG (`FS.GG.UI.Canvas.Rng`).**
As of feature 239, the thrice-re-implemented seeded RNG is **shipped real API** — a
value-type SplitMix64 generator. Use it instead of ambient `System.Random` so your
`update` stays pure and replayable. Thread the `Rng` value through your `Model`:

```fsharp
open FS.GG.UI.Canvas

// in init: seed once (same seed ⇒ identical replayable stream on any platform)
let model0 = { model with Rng = Rng.ofSeed 42UL }

// in update: thread the state — no ambient System.Random, no wall-clock
let struct (spawnColumn, rng') = Rng.nextInt 0 (boardColumns - 1) model.Rng
{ model with Rng = rng' (* … place the entity at spawnColumn … *) }
```

`ofSeed`/`nextInt`/`nextFloat`/`split` are pure `Rng -> struct(value, Rng)`. Because
`Rng` is a `[<Struct>]` value, carrying it in the model keeps the whole simulation
deterministic and replayable — structural model equality implies equal RNG state
(a prerequisite for deterministic-replay evidence).

Two of the three loop primitives below are now **shipped `FS.GG.UI` API** (feature 239);
the third (paddle rebound) remains a documented per-game convention.

1. **Fixed-step accumulator — shipped as `FS.GG.UI.Canvas.FixedStep.drain`.** Decouple
   simulation from frame cadence: `drain interval frameTime accumulator` returns
   `struct(steps, newAccumulator)`, clamping a long stall (debugger pause, GC) so it can
   never spiral into hundreds of catch-up steps. Pure in `update`; the only input is the
   elapsed time carried on the tick `Msg`.
   ```fsharp
   open FS.GG.UI.Canvas
   let struct (steps, acc') = FixedStep.drain (1.0/120.0) elapsed model.Accumulator
   let m' = List.fold (fun s _ -> advance s) model [ 1 .. steps ]
   { m' with Accumulator = acc' }
   ```
   (For a tighter catch-up cap than the 0.25 s default, use `FixedStep.drainWith maxFrameTime`.)
2. **Collision — shipped AABB in `FS.GG.UI.Scene.Geometry`.** `Geometry.intersects`
   (box-vs-box, strict edges), `Geometry.contains`/`Geometry.containsPoint` (inclusive),
   and `Geometry.sweptIntersects moving velocity target` (catches a fast projectile that
   would tunnel through a thin target in one step). Then resolve **at most one** reflection
   per fixed step, choosing the axis by the **smaller normalized penetration** (penetration
   ÷ extent) so a corner hit flips exactly one component, not both — reflect by negating
   that one velocity component and pushing the body out by the penetration depth. (The
   reflection resolution is per-game convention; the overlap tests are shipped.)
3. **Paddle-rebound angle with a `|Dy|` floor.** Map the ball's contact offset
   from the paddle centre to a horizontal velocity, but clamp `|Dy|` to a minimum
   so the ball can never settle into a purely-horizontal loop:
   `dy = sign dy * max minDy (abs dy)` after the rebound.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-skiaviewer]] provides the `ViewerModel`/`ViewerMsg` this adapter wraps.
- [[fs-gg-scene]] supplies the `SceneNode` the render function produces.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Fable.Elmish (the Elmish architecture this adapter follows): https://elmish.github.io/elmish/
