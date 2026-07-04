# Implementation Plan: FS.GG.UI Simulation Primitives

**Branch**: `239-sim-primitives` | **Date**: 2026-07-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/239-sim-primitives/spec.md`

## Summary

Ship three small, pure, additive public helpers so game/simulation consumers of FS.GG.UI stop re-rolling them:

1. **`Geometry`** — a public AABB helper in `namespace FS.GG.UI.Scene`, operating on the existing `Rect`/`Point`: `intersects`, `contains` (rect and point), `center`, `ofCenter`, and a swept-overlap test for fast projectiles. Fills the collision surface `template/base/docs/product.md` already advertises but only ships privately.
2. **`Rng`** — a value-type seeded PRNG in `namespace FS.GG.UI.Canvas`: `ofSeed`, `nextInt`, `nextFloat`, `split`; each draw returns `(value, nextState)` and never mutates. Removes the mutable-`System.Random`-in-the-`Model` determinism smell.
3. **`FixedStep`** — a pure fixed-timestep accumulator drain in `namespace FS.GG.UI.Canvas`: `drain interval frameTime accumulator -> struct(int * float)`, plus a `drainWith` variant taking an explicit spiral-of-death clamp. A lower-level primitive underneath the existing `Loop.advance`.

**Technical approach**: additive-only. Two new modules land in the Canvas package (which already *is* the deterministic fixed-timestep game-loop package and references only Scene), one in the Scene package (which owns `Rect`/`Point` and stays dependency-light). Each new module follows the repo's Spec→FSI→Semantic-Tests→Implementation order, gets a curated `.fsi`, a regenerated surface-area baseline, Expecto + FsCheck tests, an FSI prelude exercise, and product-doc/skill updates. All AABB and clamp conventions match existing internal code so nothing observable changes for current consumers.

> **Standing assumption — no unverified root-cause hypotheses here.** This is greenfield *additive* surface, not a defect fix, so there are no root-cause hypotheses to confirm. The "does it actually work end-to-end" obligation is met by (a) FSI prelude transcripts that call the packed public surface the way a consumer would, and (b) `/speckit-tasks` scheduling an early **consumer-shaped smoke** in the Foundational phase: exercise each helper through its `.fsi` from an FSI script before building out the property-test suite, and (optionally) re-point one sample game's hand-rolled PRNG/accumulator at the new helpers to prove real reuse (SC-004).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution: exclusive stack, net10.0 default).

**Primary Dependencies**: none new. Geometry uses only `FS.GG.UI.Scene.Rect`/`Point`. Rng/FixedStep use only `System` primitives. The Scene package must stay dependency-light (gate `SurfaceAreaTests` "Scene package stays dependency-light" forbids Elmish/Silk/SkiaSharp/Yoga/YamlDotNet in `Scene.fsproj`); nothing new is added.

**Storage**: N/A (pure functions, no persistence).

**Testing**: Expecto + FsCheck (property tests), `Microsoft.NET.Test.Sdk` + `YoloDev.Expecto.TestSdk`. New tests: `tests/Scene.Tests/GeometryTests.fs`, `tests/Canvas.Tests/RngTests.fs`, `tests/Canvas.Tests/FixedStepTests.fs`. Reflection/surface gate: `tests/Package.Tests/SurfaceAreaTests.fs`. FSI transcripts under `scripts/*-prelude.fsx`.

**Target Platform**: cross-platform .NET library surface; no GL/window/viewer dependency (FR-013 — consumable standalone).

**Project Type**: F# class-library packages within the FS.GG.UI product (`FS.GG.UI.Scene`, `FS.GG.UI.Canvas`).

**Performance Goals**: helpers are O(1) arithmetic (Geometry, FixedStep) / O(1) per draw (Rng); no hot-path concern beyond "don't allocate gratuitously" — value-type/struct returns.

**Constraints**: pure, deterministic, total (degenerate inputs return documented values, never throw). Additive public surface only — no change to any existing type, signature, or behavior.

**Scale/Scope**: three modules, ~small; six new files (`Geometry.fsi/.fs`, `Rng.fsi/.fs`, `FixedStep.fsi/.fs`) + three test files + two baseline regenerations + doc/skill edits.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — ✅ Honored. Tasks author each `.fsi` first (contracts in `contracts/`), exercise it via an FSI prelude, write Expecto/FsCheck semantic tests against the public surface, then implement the `.fs`.
- **II. Visibility Lives in `.fsi`, Not in `.fs`** — ✅ Each new public module ships a curated `.fsi`; no `private`/`internal`/`public` modifiers on top-level `.fs` bindings (a gate test enforces this). Surface-area baselines `readiness/surface-baselines/FS.GG.UI.Scene.txt` and `FS.GG.UI.Canvas.txt` regenerated via `scripts/refresh-surface-baselines.fsx` and committed.
- **III. Idiomatic Simplicity Is the Default** — ✅ Plain pure F#: functions over classes, `struct` tuple returns, standard-library math. `FixedStep.drain` is closed-form arithmetic (no loop). No custom operators, SRTP, reflection, type providers, or non-trivial computation expressions. FsCheck property tests use the standard FsCheck surface. **No justification-required feature is used.**
- **IV. Elmish/MVU Is the Boundary for Stateful/I-O Workflows** — ✅ N/A by design: all three helpers are *pure* and stateless (no multi-step state, I/O, retries, or background work). The `Rng` value is explicitly designed to be threaded through a **consumer's** MVU `Model`, but the helper itself owns no state and requests no effects, so no `Model/Msg/Cmd` boundary applies. This is the intended shape, not an omission.
- **V. Test Evidence Is Mandatory** — ✅ Every helper gets tests that fail before and pass after (the modules don't exist yet). All evidence is **real** (pure functions, no GL/IO) — no synthetic evidence needed or used.
- **VI. Observability and Safe Failure** — ✅ Pure helpers have no I/O to log. "Safe failure" is met by **totality**: degenerate inputs (zero/negative rects, `low > high` ranges, non-positive interval, negative `dt`) return documented values rather than throwing; conventions documented in `.fsi` and `research.md`.
- **Change Classification** — **Tier 1 (contracted change)**: adds public API surface. Full artifact chain required (spec, plan, `.fsi`, baselines, tests, docs) — all scheduled.

**Result: PASS.** No violations; Complexity Tracking table not required.

## Project Structure

### Documentation (this feature)

```text
specs/239-sim-primitives/
├── plan.md              # This file
├── research.md          # Phase 0 — placement, RNG algorithm, clamp/units decisions
├── data-model.md        # Phase 1 — the value types + total-function conventions
├── quickstart.md        # Phase 1 — how to validate the three helpers end-to-end
├── contracts/           # Phase 1 — the intended .fsi signatures (Geometry/Rng/FixedStep)
│   ├── Geometry.fsi
│   ├── Rng.fsi
│   └── FixedStep.fsi
├── checklists/
│   └── requirements.md  # spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 — /speckit-tasks (NOT created here)
```

### Source Code (repository root)

```text
src/Scene/                       # FS.GG.UI.Scene (dependency-light; owns Rect/Point)
├── Types.fsi / Types.fs         # existing: Rect, Point
├── Geometry.fsi                 # NEW — public AABB contract on Rect/Point
├── Geometry.fs                  # NEW — implementation (added to Scene.fsproj after Types)
└── skill/SKILL.md               # updated: advertise the now-real Geometry surface

src/Canvas/                      # FS.GG.UI.Canvas (deterministic fixed-timestep game loop; refs Scene only)
├── Loop.fsi / Loop.fs           # existing: StepState, Loop.advance/alpha (0.25s clamp)
├── Rng.fsi                      # NEW — value-type seeded PRNG contract
├── Rng.fs                       # NEW — SplitMix64 implementation
├── FixedStep.fsi                # NEW — pure accumulator-drain contract
└── FixedStep.fs                 # NEW — closed-form drain (added to Canvas.Lib.fsproj)

tests/Scene.Tests/GeometryTests.fs      # NEW — Expecto + FsCheck
tests/Canvas.Tests/RngTests.fs          # NEW — determinism + distribution
tests/Canvas.Tests/FixedStepTests.fs    # NEW — conservation + clamp property tests

readiness/surface-baselines/
├── FS.GG.UI.Scene.txt           # regenerated (+ FS.GG.UI.Scene.Geometry)
└── FS.GG.UI.Canvas.txt          # regenerated (+ FS.GG.UI.Canvas.Rng, .FixedStep, .Rng state type)

template/base/docs/product.md    # updated: collision/RNG/fixed-step guidance now points at real API
scripts/*-prelude.fsx            # FSI transcript exercising the three helpers
```

**Structure Decision**: Extend the two existing packages rather than introduce a new project (constitution: "Dependencies are minimized"; "Controls/design-system/primitives are distinct layers"). `Geometry` belongs in **Scene** because it operates on Scene's `Rect`/`Point` and introduces no new geometry vocabulary (FR-011, and product.md's "reuse the shared `Rect`" guidance). `Rng` and `FixedStep` belong in **Canvas**, the package whose stated purpose is the deterministic fixed-timestep game loop and which already ships the stateful `Loop.advance`; `FixedStep.drain` is the lower-level primitive beneath it and `Rng` is the companion determinism primitive. Canvas references only Scene, so consumers get the primitives without pulling in rendering/viewer/layout machinery (FR-013).

## Complexity Tracking

*No Constitution Check violations — table intentionally omitted.*
