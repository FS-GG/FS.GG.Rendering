# Phase 1 Data Model: Collision-Safe Vec2/Position in the Model Template

**Feature**: 250-collision-safe-vec2 · **Plan**: [plan.md](./plan.md) · **Research**: [research.md](./research.md)

Entities here are **product-template values** (module `AppRoot.Geometry` in `Vec2.fs`) and the starter `Model` deltas.
No framework/library types change.

---

## `Vec2` (new — the collision-safe vector)

```fsharp
namespace AppRoot

open FS.GG.UI.Scene   // Point, Rect — the ONLY records constructed by bare label in this file

/// Product-owned collision-safe 2D vector — THIS FILE IS YOURS TO ADAPT.
/// Used for position, velocity, and displacement. Field labels `Vx`/`Vy` deliberately DO NOT reuse
/// `Scene.Point` (X,Y) or `Scene.Rect` (X,Y,Width,Height), so a model built on this can never trip the
/// record-label mis-inference that poisons the durable LayoutEvidence.fs (see fs-gg-scene pitfall).
module Geometry =
    type Vec2 = { Vx: float; Vy: float }
```

| Field | Type | Meaning | Rule |
| --- | --- | --- | --- |
| `Vx` | `float` | X component (position/velocity/displacement) | finite in normal use; ops are total on non-finite input (see laws) |
| `Vy` | `float` | Y component | finite in normal use; total on non-finite input |

**Invariant (SC-002 / FR-001)**: `{ "Vx"; "Vy" } ∩ ({ "X"; "Y" } ∪ { "X"; "Y"; "Width"; "Height" }) = ∅`.
This is the single load-bearing property; a generated-product test asserts it (see contract).

---

## `Geometry` module surface (pure, total, deterministic)

| Function | Signature | Notes |
| --- | --- | --- |
| `vec2` | `float -> float -> Vec2` | smart constructor (`vec2 x y = { Vx = x; Vy = y }`) |
| `zero` | `Vec2` | `{ Vx = 0.0; Vy = 0.0 }` |
| `add` | `Vec2 -> Vec2 -> Vec2` | component add |
| `sub` | `Vec2 -> Vec2 -> Vec2` | component subtract |
| `scale` | `float -> Vec2 -> Vec2` | scalar multiply (used by `stepSim`: `add pos (scale dt vel)`) |
| `clamp` | `min: Vec2 -> max: Vec2 -> Vec2 -> Vec2` | per-component clamp (keep entity inside a bound) |
| `toPoint` | `Vec2 -> Point` | `fun v -> { X = v.Vx; Y = v.Vy }` — the crossing into `Scene` |
| `toRect` | `center: Vec2 -> w: float -> h: float -> Rect` | centered AABB; covers the size case (FR-002) |

**Laws** (asserted by tests; constitution V — real evidence, deterministic):
1. `toPoint (vec2 x y) = { X = x; Y = y }` (round-trips the components into `Scene.Point`).
2. `add a zero = a`; `add a b = add b a`; `scale 1.0 v = v`; `scale 0.0 v = zero`.
3. `toRect c w h` is centered: its `X = c.Vx - w/2`, `Y = c.Vy - h/2`, `Width = w`, `Height = h`.
4. Every function is **total**: non-finite (`nan`/`inf`) or negative inputs never throw (mirrors `FixedStep.drain`'s
   total posture); `clamp` with `min ≤ max` returns a value in `[min, max]` per component.
5. Determinism: identical inputs → byte-identical outputs (integer/float straight-line arithmetic, no hashing, no
   wall-clock) — safe inside a replayed `update`.

---

## Starter `Model` deltas (game family, replaceable)

Before (excerpt — bare component floats, ad-hoc collision avoidance):

```fsharp
type Ball = { CenterX: float; CenterY: float; VelocityX: float; VelocityY: float }
type Model = { Ball: Ball; PlayfieldWidth: float; PlayfieldHeight: float; TickCount: int; … }
```

After (expressed in `Vec2`; adds the accumulator):

```fsharp
open AppRoot.Geometry

type Ball = { Pos: Vec2; Velocity: Vec2 }
type Model =
    { Ball: Ball
      Playfield: Vec2          // width = Playfield.Vx, height = Playfield.Vy (no Width/Height labels)
      SimAccumulator: float    // seconds carried between Ticks (FixedStep.drain)
      LeftPaddleY: float; RightPaddleY: float; PaddleHeight: float
      LeftScore: int; RightScore: int
      TickCount: int; LastInput: ViewerKey option }
```

- No record in `AppRoot.Model` declares any of `X`/`Y`/`Width`/`Height` (playfield extent is carried as a `Vec2`, not a
  `Rect`) → the durable `LayoutEvidence.fs` bare-label literals resolve unambiguously to `Scene.Rect`.
- `PaddleY`/`PaddleHeight`/scores stay scalar (they never collided).

### State transitions (unchanged shape, re-expressed)

| Msg | Transition |
| --- | --- |
| `Tick` | `let struct(steps, acc') = FixedStep.drain interval frameTime model.SimAccumulator` → apply pure `stepSim` `steps` times → `{ model' with SimAccumulator = acc'; TickCount = TickCount + 1 }` |
| `MovePaddle (side, dir)` | unchanged (scalar paddle Y) |
| `ViewerInput (key, isDown)` | unchanged mapping; `LastInput = Some key` |
| `NoOp` | identity |

`stepSim` (pure) integrates `Ball.Pos = add Pos (scale dt Velocity)`, bounces (negate the relevant `Velocity`
component), scores/re-serves, and `clamp`s the ball inside `Playfield` — the current `stepBall` logic over `Vec2`.

---

## Durable re-point deltas (keep file + tokens; re-point fields only)

| File | Field reads change | Tokens/surface preserved |
| --- | --- | --- |
| `LayoutEvidence.fs` | `model.Ball.CenterX/CenterY` → `model.Ball.Pos.Vx/Vy` (via `Geometry`); `model.PlayfieldWidth/Height` → `model.Playfield.Vx/Vy`; active-item bounds via `toRect`/`toPoint` | `HudRegion`/`GameplayRegion`/`TextBounds`/`GameplayBounds`, `hud-region=present`/`gameplay-region=present`/`measurement-mode=approximate`, `NoLayoutOverlap`, compile order |
| `EvidenceCommands.fs` | same position re-point where it renders the starter scene | `RendererMode = "deterministic-scene"`, command surface, tokens |
| `View.fs` | draw ball/paddles from `Vec2` (`toPoint`) | scene-text tokens the behavior tests read |

Non-game `Model` (`app`/`governed`/`headless-scene`: `Name`/`RenderCount`/pages) is **untouched** → byte-identical
(FR-010 / SC-006).
