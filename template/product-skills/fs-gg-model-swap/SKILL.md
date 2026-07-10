---
name: fs-gg-model-swap
description: Swap the scaffold's starter model for your own in a generated FS.GG.UI product — the re-export spine, durable vs replaceable vs re-point files, and the swap footguns.
---

# Model-Swap Capability

## Scope

Use this skill when you replace the generated product's **starter model** (the minimal,
replaceable Pong-style state machine) with your own game/UI model. The scaffold is built
so the swap touches only a handful of files and every governance/evidence scan stays green
across it. Read `docs/scaffold-map.md` first — it is the source of truth for *what survives*;
this skill is the *how-to* for the swap itself.

## The re-export spine — why a swap is a 4-file change

`Program.fs` is the host/CLI entry point. It does **not** call your model directly through
long qualified paths; instead it **re-exports** the model/host surface by name at the top:

```fsharp
// in Program.fs — the seam that decouples the large host file from your model
type Model = Product.Model.Model
type Msg   = Product.Model.Msg
let initialModel  = Product.Model.initialModel
let update        = Product.Model.update
let view          = Product.View.view
let tick          = Product.EvidenceCommands.tick
let generatedHost = Product.EvidenceCommands.generatedHost
```

Everything below those bindings (the entrypoint, the evidence-command dispatch, the window
diagnostics) refers to the **local** `update` / `view` / `generatedHost` names. So as long as
your swapped model keeps those source-module members **present and same-shaped**
(`Product.Model.update : Model -> Msg -> Model`, `Product.View.view : …`,
`Product.EvidenceCommands.generatedHost : …`), `Program.fs` and `WindowOptions.fs` compile
**unchanged** — you never touch them. That is the whole reason a model swap is a ~4-file edit,
not a rewrite of the host.

> The contract you must not break: **keep the re-exported names alive in their source modules,
> with the same types.** Rename `Product.Model.update` or change its arity and the spine breaks
> and the swap grows to touch `Program.fs`.

## Which files you touch (durable vs replaceable vs re-point)

From `docs/scaffold-map.md`, three classes — you only edit the last two:

- **Replaceable — rewrite freely** (they define/return the starter model):
  - `src/<ProductDir>/Model.fs` — the starter `Model`/`Msg`/`update`. Your state machine goes here.
    Entity positions/velocities are the collision-safe `Geometry.Vec2`; `stepSim` advances them via the
    `FixedStep.drain` accumulator on `Tick` (see [[fs-gg-game-core]]).
  - `src/<ProductDir>/Vec2.fs` *(game / sample-pack)* — the collision-safe vector helper (`Geometry.Vec2`,
    `Vx`/`Vy`, `toPoint`/`toRect`). **Use it for your own positions/sizes so you never reuse `Scene`'s
    `X`/`Y`/`Width`/`Height` labels** (see the record-label pitfall below). Rename/extend it freely; but
    because the shipped starter `Model.fs` depends on it, delete it only together with (or after) swapping
    the starter model off it — then its `Exists`-guarded compile item keeps the build green.
  - `src/<ProductDir>/View.fs` — the starter `view : Model -> SceneNode`. Your rendering goes here.
  - `src/<ProductDir>/Collision.fs` *(game / sample-pack)* — the adaptable collision helper
    (see [[fs-gg-collision]]). Edit the response rule, add layers, or delete it entirely: its compile
    item is `Exists`-guarded, so the build stays green and `Product.fsproj` stays durable.
  - `src/<ProductDir>/Visibility.fs` *(game / sample-pack)* — the adaptable 2D-visibility helper
    (see [[fs-gg-visibility]]). Edit the sight radius, cone the field of view, swap the polygon for a
    fog-of-war mask, or delete it entirely: its compile item is `Exists`-guarded, so the build stays
    green and `Product.fsproj` stays durable.
  - `src/<ProductDir>/Grids.fs` *(game / sample-pack)* — the adaptable grid-parts helper
    (see [[fs-gg-grids]]). Edit the edge/vertex addressing, move the grid origin, extend it toward hex
    grids, or delete it entirely: its compile item is `Exists`-guarded, so the build stays green and
    `Product.fsproj` stays durable.
  - `src/<ProductDir>/LineDrawing.fs` *(game / sample-pack)* — the adaptable grid line-drawing helper
    (see [[fs-gg-line-drawing]]). Switch the thin line for the supercover, cap the length for a
    limited-range beam, or delete it entirely: its compile item is `Exists`-guarded, so the build stays
    green and `Product.fsproj` stays durable.
  - `tests/Product.Tests/BehaviorTests.fs` — the replaceable behaviour tests that drive the
    starter's `view`/`update`/host directly. Rewrite these to drive **your** model.
- **Re-point — keep the file + its scanned tokens, re-aim the model-field reads**:
  - `src/<ProductDir>/LayoutEvidence.fs` — HUD/gameplay region bounds. Re-point the region
    computations at your own layout; keep the evidence tokens.
  - `src/<ProductDir>/EvidenceCommands.fs` — the deterministic `SceneEvidence.render` command
    and the host wiring (`generatedHost`, `tick`, `mapKey`, `viewerOptions`). Re-point it at
    your own `view`/`update`; keep the command surface and tokens.
- **Durable — do not touch**: `Program.fs`, `WindowOptions.fs`, `Product.fsproj`,
  `tests/Product.Tests/GovernanceTests.fs`. The re-export spine keeps these compiling.

A purely additive swap (e.g. adding a model field the re-point files never read) can leave the
re-point files untouched too — then the swap is just `Model.fs` + `View.fs` + `BehaviorTests.fs`.

## Doing the swap — a checklist

1. Rewrite `Model.fs`: your `Model`, `Msg`, `initialModel`, `update`. Keep those names.
2. Rewrite `View.fs`: your `view : Model -> SceneNode` (see [[fs-gg-rendering:fs-gg-scene]] for the primitives).
3. Re-point `LayoutEvidence.fs` and `EvidenceCommands.fs` at the fields your new model exposes,
   preserving every must-survive source-scan token (`SceneEvidence.render`,
   `RendererMode = "deterministic-scene"`, the visual-evidence vocabulary — see `scaffold-map.md`).
4. Rewrite `BehaviorTests.fs` to drive your model.
5. Leave `Program.fs` / `WindowOptions.fs` / `Product.fsproj` / `GovernanceTests.fs` alone.
6. `./fake.sh build -t Dev` then `-t Test` then `-t Verify`. The governance gate proves the
   durable spine survived; the behaviour tests prove your model works.

## Common pitfalls

- **Renaming a re-exported member.** If you rename `Product.Model.update` (or change its
  signature), `Program.fs`'s `let update = Product.Model.update` no longer resolves and the swap
  leaks into the durable host file. Keep the re-exported names and shapes; add new members
  alongside them rather than renaming the spine.
- **The governance substring trap (see the compile-order gate + `scaffold-map.md`).** The
  compile-order scan is anchored to the `<Compile Include="X.fs" />` item form, so an additive
  file whose name embeds one of the six scanned names — a `BulwarkView.fs`, a `GameModel.fs` —
  and even a code comment mentioning `View.fs`/`Model.fs` are **safe**. You do not need to rename
  additive files or scrub comments to avoid the six substrings. (This closed an earlier
  bare-`IndexOf` footgun that flagged `BulwarkView.fs` as "view before model" and broke the build.)
- **Framework-vs-consumer record-label collision — use the shipped `Geometry.Vec2`.** `Scene`
  exposes `Point = { X: float; Y: float }` and `Rect = { X; Y; Width; Height }`. The durable
  `LayoutEvidence.fs` opens **both** `Scene` and your model and builds `Rect` with bare labels, so if
  your model also declares a record with `X`/`Y`/`Width`/`Height`, those bare `{ X = …; Y = …; Width = …;
  Height = … }` literals mis-resolve to **your** record — a wall of errors in a durable file you were told
  not to touch, surfacing only after a whole model is written. The scaffold ships the fix: the
  collision-safe `Geometry.Vec2` (`Vx`/`Vy` — zero overlap with `Point`/`Rect`) in `src/<ProductDir>/Vec2.fs`.
  Build your positions on it and cross into the scene with its `toPoint`/`toRect` (express a size via
  `toRect`, never `Width`/`Height` labels on your record):
  ```fsharp
  open Geometry                                          // Vec2 = { Vx: float; Vy: float } (collision-safe)
  type Enemy = { Pos: Vec2; Velocity: Vec2 }             // NO X/Y/Width/Height labels on your record
  let bounds (e: Enemy) : Rect = toRect e.Pos 24.0 24.0  // centered size, no Width/Height labels
  ```
- **Consumer-vs-consumer record-label collision.** Distinct from the clash above: if two of
  *your own* records share a label (a `Creep` and a `Tower` both carrying `.Pos`/`.Id`/`.Hp`), a
  bare `let posOf x = x.Pos` makes F# infer the *last-declared* record for `x`, so the helper
  silently type-checks against the wrong type. Annotate the parameter — `let posOf (c: Creep) = c.Pos`
  — at every such shared-label access. Plan your record label names up front (see [[fs-gg-game-core]]'s
  grid-sim recipe — it is far cheaper than reworking the model after the inference errors appear).

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` — `GovernanceTests.fs` proves the durable spine survived the swap;
`BehaviorTests.fs` proves your replaced model behaves.

## Evidence

Record swap evidence (before/after governance-scan green, behaviour cases for the new model) under
this product's `readiness/` paths. Do not copy framework readiness reports into the product.

## Package Boundary

A model swap is pure product code — it references only the capability packages your profile already
carries (`fs-gg-scene` for geometry, `fs-gg-skiaviewer` for host wiring, `fs-gg-game-core` for
simulation). It introduces no new package edge; keep host wiring in `fs-gg-skiaviewer`.

## Generated Product

The starter is deliberately minimal so it is cheap to replace. Rewrite `Model.fs`/`View.fs`,
re-point the two evidence files, rewrite `BehaviorTests.fs`, and the re-export spine carries the
durable host across unchanged.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the driven library's own reference),
then community sources. If your product uses Spec Kit, record findings and resolving links under the
feature's `specs/<feature>/feedback/` folder; otherwise record them in this skill's **Sources** line
and any product-local `docs/` location. Offline, the mandate degrades to recording
"research blocked — <why>" rather than hard-failing the phase.

## Related

- [[fs-gg-rendering:fs-gg-scene]] — the `Scene`/`Point`/`Rect` primitives your new `view` builds; owns the
  framework geometry records the collision note is about.
- [[fs-gg-game-core]] — the grid-sim recipe and the consumer-vs-consumer record-label guidance.
- [[fs-gg-rendering:fs-gg-skiaviewer]] — the host boundary the re-exported `generatedHost` drives.
- [[fs-gg-rendering:fs-gg-layout]] — the HUD + gameplay regions the re-pointed `LayoutEvidence.fs` computes.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
