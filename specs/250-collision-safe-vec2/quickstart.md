# Quickstart: Collision-Safe Vec2/Position in the Model Template

**Feature**: 250-collision-safe-vec2 · **Plan**: [plan.md](./plan.md) · **Contracts**: [contracts/vec2-surface.md](./contracts/vec2-surface.md)

Runnable validation scenarios that prove the feature end-to-end. Implementation details live in `data-model.md` /
`contracts/` and the tasks; this is the run/verify guide. Paths use `template/base/src/Product/` (the generated tree
names it `src/<ProjectName>/`, module `Product.*`/`AppRoot.*`).

## Prerequisites

- .NET `net10.0` SDK; local NuGet feed at `~/.local/share/nuget-local/` with the coherent `FS.GG.UI.*` set
  (`FS.GG.UI.Scene`, `FS.GG.UI.Canvas`, viewer/controls) — the game profile references `FS.GG.UI.Canvas` for
  `FixedStep.drain`.
- The `FS.GG.UI.Template` package built/installed from this branch (`dotnet new install`), or the template composition
  harness (`tests/composition`).

## Scenario A — Reproduce the trap on today's template (fail-before, plan early-repro gate / C4)

1. Scaffold a **game** product: `dotnet new fsgg-ui --profile game -n Repro1` (or via the composition harness).
2. In `src/Repro1/Model.fs`, add a record with the *natural* names an author reaches for:
   ```fsharp
   type Enemy = { X: float; Y: float; Width: float; Height: float }
   ```
   and give `Model` an `Enemies: Enemy list`.
3. `dotnet build src/Repro1`.

**Expected (bug present)**: a wall of `FS3566`/`FS0039` errors originating in the **durable** `LayoutEvidence.fs`
(which the author never touched) — the bare `{ X = …; Y = …; Width = …; Height = … }` `Rect` literals mis-resolve to
`Enemy`. This is the mechanism the feature removes.

## Scenario B — The fix: a fresh game model built on `Vec2` compiles clean (US1 / C2 / C3)

1. Scaffold a game product from the **feature branch** template: `dotnet new fsgg-ui --profile game -n Game1`.
2. Confirm `src/Game1/Vec2.fs` (module `AppRoot.Geometry`) is present and compiled before `Model.fs`
   (`Product.fsproj`: `<Compile Include="Vec2.fs" Condition="Exists('Vec2.fs')" />`).
3. Add the author's own entities **using the safe vocabulary** (no `X`/`Y`/`Width`/`Height` labels):
   ```fsharp
   open AppRoot.Geometry
   type Enemy = { Pos: Vec2; Velocity: Vec2 }
   // draw/measure with Geometry.toPoint / Geometry.toRect
   ```
4. Leave `LayoutEvidence.fs` untouched. Run `dotnet build src/Game1` then `dotnet test`.

**Expected**: build exits 0 with **no** `FS3566`/`FS0039` from `LayoutEvidence.fs`; tests pass. The zero-label-overlap
assertion (C2) is green.

## Scenario C — Accumulator + `stepSim` on `Tick` (US2 / C5)

1. In the shipped `src/Game1/Model.fs`, confirm `Model` carries `SimAccumulator: float` and entity `Pos`/`Velocity`
   as `Geometry.Vec2`.
2. Confirm `update` handles `Tick` via `let struct(steps, acc') = FixedStep.drain interval frameTime model.SimAccumulator`
   and runs a pure `stepSim` `steps` times.
3. Drive a scripted `frameTime` sequence twice and compare: `dotnet test --filter stepSim`.

**Expected**: the ball stays inside `Playfield` after each step (`Geometry.clamp`); identical inputs yield
byte-identical model states (determinism / replay-safe). `toPoint`/`toRect` satisfy the data-model laws.

## Scenario D — Durable spine + starter swap stay green (FR-005 / C3 / Decision 6)

1. On the fresh `Game1`, run `dotnet test` — the durable `GovernanceTests.fs` (six-file order + `hud-region` /
   `gameplay-region` / `measurement-mode` / `overlap` tokens, `RendererMode = "deterministic-scene"`) passes,
   unedited.
2. Perform a **starter swap**: replace `Model.fs`/`View.fs` with a `Vec2`-based author model; re-point only the
   model-field reads in `LayoutEvidence.fs`/`EvidenceCommands.fs` (tokens preserved).

**Expected**: governance + evidence tests stay green across the swap (as in the feature-220 swap evidence).

## Scenario E — Non-game profiles unchanged (FR-010 / SC-006 / C6)

1. Scaffold `--profile app` and `--profile governed` from the feature branch.
2. Diff generated output against the pre-change baseline / run the per-profile golden + composition checks.

**Expected**: byte-identical output; no `Vec2.fs`/`FS.GG.UI.Canvas` in those profiles; governance posture unchanged.

## Scenario F — Guidance surfaces the pitfall (US3 / C7)

- Read `src/Game1/Model.fs` comment at the model-editing site and the `fs-gg-project` model-swap guidance: both name
  the `Scene`-label collision, name `Geometry.Vec2` as the default, and state the rule.
- `template/base/docs/scaffold-map.md` lists `Vec2.fs` in the replaceable "adaptable helper you own" set.

## Done when

Scenarios A (repro), B (fix builds clean), C (accumulator/stepSim), D (durable+swap green), E (non-game byte-identical),
and F (guidance) all pass, and the `FS.GG.UI.Template` republish carries the change (board #138 → epic #137).
