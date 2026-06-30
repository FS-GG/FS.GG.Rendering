# Phase 1 Data Model — Replaceable Game Starter Scene

The "entities" here are template/scaffold constructs and the generated product's MVU types, not a
persistence schema.

## 1. Game starter seam (developer-owned, replaceable)

The minimal Pong-style MVU the developer replaces. Lives in the game `//#if (profile == "game")`
branch of `Model.fs` / `View.fs`.

### `Model` (game branch)
| Field | Type | Meaning | Validation / invariant |
|---|---|---|---|
| `Ball` | center + velocity (floats) | moving ball | stays inside playfield after `update` (clamped/bounced) |
| `LeftPaddle` / `RightPaddle` | position (float) | paddles | clamped to playfield height |
| `Score` | int (or per-side ints) | running score | monotonic per scoring event |
| `Playfield` | size (width/height) | bounds | drives layout regions; matches `view` extent |
| `TickCount` | int | frames advanced | increments on `Tick` |
| `LastInput` | key option | last paddle key | diagnostic only |

> **Record-label collision**: Scene point/rect literals use `X`/`Y`/`Width`/`Height`. The game
> model MUST avoid bare same-labeled records or qualify/annotate them (`fs-gg-scene` pitfall;
> scaffold-map pre-design pointer).

### `Msg` (game branch)
`Tick` | paddle moves (e.g. `MovePaddle of side * direction`) | `ViewerInput of ViewerKey * bool`
| `NoOp`. Mapped from keyboard via `EvidenceCommands.mapKey`.

### Transitions (`update`, pure)
- `Tick` → integrate ball by velocity; bounce off top/bottom walls and paddles; on miss, update
  `Score` and re-serve; `TickCount + 1`. No effects (or a single product-owned host command).
- paddle move → clamp paddle within playfield; record `LastInput`.
- `NoOp` → identity.

`init () : Model * AdapterCommand<Msg>` returns the served initial state; `subscriptions`/`tick`
(≥16ms) drive motion (constitution IV — stateful workflow through the MVU boundary).

### `view : Model -> SceneNode` (game branch)
A `Group` of `Scene` primitives: playfield border `Rectangle`, ball `Rectangle`, two paddle
`Rectangle`s, and a score HUD `Text`. Rendered live by `Viewer.runApp` via `generatedHost.View`.

## 2. Durable governance spine (model-agnostic; re-point only)

| File | Role | Game-branch action |
|---|---|---|
| `Program.fs` | host/CLI entry; default launch = `Viewer.runApp` (game) | keep; ensure game launch reachable (`game \|\| sample-pack` group on `runApp`) |
| `EvidenceCommands.fs` | `generatedHost`, `--scene-evidence`/`SceneEvidence.render` (`deterministic-scene`), evidence commands | re-point `view`/`initialModel`; keep command surface + must-survive tokens; `interactiveHost` stays `app`-only |
| `LayoutEvidence.fs` | HUD + gameplay region bounds evidence | re-point HUD→score strip, gameplay→playfield; keep region/overlap/measurement tokens |
| `WindowOptions.fs` | window-option parsing/diagnostics | conditional-only: include `game` in the package-gated branch; re-confirm defaults |
| `GovernanceTests.fs` | source-text scans of structure/evidence/discoverability | keep; game `//#else` assertions made satisfiable by skeleton **and** a Pong swap; never edited by a consumer |
| `BehaviorTests.fs` | replaceable scaffold-behavior tests | rewrite game `//#else` to drive ball/paddle/score/tick; controls/app test unchanged |

**Must-survive source tokens (game branch, per scaffold-map):** `--scene-evidence`,
`SceneEvidence.render`, `RendererMode = "deterministic-scene"`, `Viewer.runApp viewerOptions
generatedHost`, and the `visualEvidenceGuidance` honesty vocabulary.

## 3. Product profile / family (scaffold selector)

| Profile | Family | Starter | Status this feature |
|---|---|---|---|
| `game` (**new**) | game/rendering | minimal Pong skeleton (default) | NEW — default for the provider |
| `app` | controls | controls showcase | KEPT as explicit option (FR-006); re-described |
| `headless-scene` | headless | deterministic scene | UNCHANGED (FR-007) |
| `governed` | headless+testing | governed scene + Testing validation | UNCHANGED (FR-007) |
| `sample-pack` | samples | controls model + `runApp` launch + samples | UNCHANGED / byte-identical (FR-007) |

`capabilities.yml` registers `game` in the relevant `profiles:` lists (scene, skiaviewer, elmish,
keyboard-input, layout, controls — and `full-governance` if that capability declares a `profiles:`
list, since `game.yml`'s capability set includes it). `game.yml` mirrors `app.yml`'s capability set
(`scene, skiaviewer, elmish, keyboard-input, layout, controls, full-governance`,
`validationCommands: [Dev, Test, Verify]`).

## 4. Scaffold map (durable contract document)

`scaffold-map.md` must match the **real** game-starter edit set: replaceable =
`<ProductDir>/Model.fs`, `<ProductDir>/View.fs`, `tests/Product.Tests/BehaviorTests.fs`;
re-point = `LayoutEvidence.fs`, `EvidenceCommands.fs`; durable-untouched =
`WindowOptions.fs`, `Product.fsproj`, `Program.fs`, `GovernanceTests.fs`. No undocumented coupling
may force edits beyond this set (FR-005 / SC-003 / SC-005).

> `WindowOptions.fs` / `Product.fsproj` are conditional-only authoring changes (game threaded into
> their gates once — §2); post-instantiation they carry no model-field reads, so the developer does
> not touch them on a swap. They are durable-**untouched** at swap time, not re-point.

## 5. `fs-gg-ui-template` contract (cross-repo surface)

The versioned template surface consumed by SDD (scaffold-provider) and Templates (governance).
Contract delta this feature: a new `game` profile, the game/rendering default starter family, and
the relaxed (family-agnostic) governance entrypoint assertion. See
[contracts/fs-gg-ui-template-contract.md](./contracts/fs-gg-ui-template-contract.md).
