# SWAP-CHECKLIST — replace the starter model

This is your **model-swap to-do list**. The generated product ships a minimal Pong-style
starter plus a durable governance spine. When you swap in your own game/UI you rewrite only
the replaceable parts; the durable parts keep compiling and keep their source/evidence scans
green across the swap. Read [`docs/scaffold-map.md`](docs/scaffold-map.md) for the
durable-vs-replaceable rationale — this file is the precise symbol-level checklist that
projects it, so you don't have to rediscover the re-points from compiler errors.

> Paths use `<ProductDir>` = `src/<ProjectName>` (your generated tree names it after the
> project, e.g. `src/SpaceInvaders1`). The module name stays `Product.*`.

## 1. Rewrite wholesale (replaceable — these define/call the starter model directly)

- [ ] `<ProductDir>/Model.fs` — the starter `Model`/`Msg`/`update` (the Pong state machine:
      `Ball` = `{ Pos; Velocity }` as collision-safe `Geometry.Vec2`, paddles, scores; `movePaddle`,
      `paddleForKey`, `stepSim` + `advanceSim` (fixed-step `FixedStep.drain` accumulator on `Tick`),
      `keyName`). Replace with your own model — keep positions as `Geometry.Vec2` (Vx/Vy), NOT bare
      `X`/`Y`/`Width`/`Height`, so `LayoutEvidence.fs` never collides (see `Vec2.fs`, `[[fs-gg-game:fs-gg-model-swap]]`).
- [ ] `<ProductDir>/Vec2.fs` — the collision-safe `Geometry.Vec2` helper the starter is built on.
      Yours to adapt (rename `Vx`/`Vy`, add a `Z`) or delete after you swap `Model.fs` off it.
- [ ] `<ProductDir>/View.fs` — the starter `view` (`Model -> SceneNode`) reading `model.Ball.Pos`
      (Vec2), `model.LeftPaddleY`/`RightPaddleY`, `model.PaddleHeight`, `model.Playfield` (Vec2),
      `model.LeftScore`/`RightScore`. Replace with your own view.
- [ ] `tests/Product.Tests/BehaviorTests.fs` — the replaceable scaffold-behavior tests that drive
      the starter's `view`/`update`/`tick`/host directly. Rewrite for your model.

## 2. Keep the file + re-point the model-field reads (durable must-re-point)

Keep these files and every must-survive evidence token they carry, but re-point the
model-field references at your own model. A purely additive swap (you only *add* a field)
leaves them untouched — see §4.

### `<ProductDir>/LayoutEvidence.fs`

- [ ] `activeGameplayBoundsForSize` — reads `model.Ball.Pos.Vx`, `model.Ball.Pos.Vy`,
      `model.Playfield.Vx`, `model.Playfield.Vy` (maps the active item into the gameplay region)
- [ ] `spawnUsesGameplayRegion` — reads `initialModel.Ball`
- [ ] `scoreTextBounds` — reads `model.TickCount`, `model.LeftScore`, `model.RightScore` (HUD text)
- [ ] `layoutEvidenceForSize` — assembles the report from the readers above
- [ ] `movementUsesGameplayRegion` / `collisionUsesGameplayRegion` — read the active-item bounds
- [ ] `validateGeneratedLayout` — validates the assembled report

### `<ProductDir>/EvidenceCommands.fs`

- [ ] `mapKey` — wraps input as `ViewerInput(key, isDown)` (the keyboard→`Msg` seam)
- [ ] `layoutEvidenceCommand` / `sceneEvidence` — render from `initialModel` + `view`

## 3. Leave untouched (durable model-agnostic spine — reads no model field)

- `<ProductDir>/Program.fs` · `<ProductDir>/WindowOptions.fs` ·
  `tests/Product.Tests/GovernanceTests.fs` — pure plumbing; keep compiling across the swap.

## 4. Additive-swap note

If your swap only **adds** a model field (rather than changing the fields the §2 files read),
the durable re-point files need no edit — a purely additive change leaves
`LayoutEvidence.fs` / `EvidenceCommands.fs` untouched.
