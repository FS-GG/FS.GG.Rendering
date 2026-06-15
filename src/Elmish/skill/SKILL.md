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

**Shipped helper: deterministic seeded RNG (`FS.GG.UI.SkillSupport.Random`).**
As of feature 062 (FR-010), the thrice-re-implemented seeded RNG is **shipped real
API** — use it instead of ambient `System.Random` so your `update` stays pure and
replayable. Thread the opaque `RngState` through your `Model`:

```fsharp
open FS.GG.UI.SkillSupport

// in init: seed once (same seed ⇒ identical replayable stream on any platform)
let model0 = { model with Rng = Random.seedRng 42UL }

// in update: thread the state — no ambient System.Random, no wall-clock
let spawnColumn, rng' = Random.nextBelow boardColumns model.Rng
{ model with Rng = rng' (* … place the entity at spawnColumn … *) }
```

`seedRng`/`nextRng`/`nextBelow` are pure `state -> (value, nextState)`; carrying
`RngState` in the model keeps the whole simulation deterministic and replayable
(a prerequisite for deterministic-replay evidence).

The three loop primitives below remain **documented conventions, not shipped
`FS.GG.UI` API** (feature 062 D10/D11 defer them with rationale — not yet at the
3-demo recurrence bar); each documented convention is the spec if a later feature
ships it.

1. **Fixed-step accumulator (deterministic `step` driver).** Decouple simulation
   from frame cadence: accumulate real elapsed time and advance the simulation in
   fixed `1/120 s` steps, capping the steps consumed per tick so a long stall
   (debugger pause, GC) can never spiral into hundreds of catch-up steps. Pure in
   `update`; the only input is the elapsed time carried on the tick `Msg`.
   ```fsharp
   let private dt = 1.0 / 120.0          // fixed simulation step
   let private maxStepsPerTick = 5       // cap catch-up after a stall
   let stepFixed (advance: Model -> Model) (elapsed: float) (m: Model) : Model =
       let acc = m.Accumulator + elapsed
       let steps = min maxStepsPerTick (int (acc / dt))
       let m' = List.fold (fun s _ -> advance s) m [ 1 .. steps ]
       { m' with Accumulator = acc - float steps * dt }
   ```
2. **Collision + single-reflection-per-step.** Use AABB for box-vs-box and
   circle-vs-rect for a ball; resolve **at most one** reflection per fixed step,
   choosing the axis by the **smaller normalized penetration** (penetration ÷
   extent) so a corner hit flips exactly one component, not both. Reflect by
   negating that one velocity component and pushing the body out by the
   penetration depth.
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
