# Implementation Plan: FS.GG.UI Grid Simulation Primitives (Pathfinding + Spatial Grid)

**Branch**: `245-grid-sim-primitives` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/245-grid-sim-primitives/spec.md`

## Summary

Ship two small, pure, additive public helpers to `FS.GG.UI.Canvas` so grid-based game/simulation consumers stop re-rolling them — the *grid-shaped* tier that follows feature 239's `Rng`/`FixedStep`:

1. **`Pathfinding`** — deterministic grid routing in `namespace FS.GG.UI.Canvas` over a caller-supplied walkability predicate: `astar` and `bfs`, each selectable `FourWay`/`EightWay`, returning `Cell list option` (`Some` = start..goal inclusive, `None` = no path). Determinism is a *behavioral contract* (FR-003): **integer** move costs (no floating-point tie hazard) and a **total order over cells** as the frontier tie-break, so identical inputs yield a byte-identical path across runs/platforms. Bounded by a `maxVisited` cap so an unreachable goal terminates (FR-005).
2. **`SpatialGrid`** — a uniform spatial partition in `namespace FS.GG.UI.Canvas`, built once from a cell size and positioned items and queried by `Rect` and by radius: `build`, `query`, `queryRadius`. Reuses `FS.GG.UI.Scene.Point`/`Rect` (and 239's `Geometry`) rather than introducing look-alike vocabulary; returns items in deterministic **insertion order**.

**Technical approach**: additive-only, mirroring 239. Both modules land in the **Canvas** package — the deterministic fixed-timestep sim tier that already ships `Rng`/`FixedStep` and references only `Scene`, so consumers get the primitives without pulling in rendering/viewer/layout (FR-012). Each new module follows the repo's Spec→FSI→Semantic-Tests→Implementation order, gets a curated `.fsi`, a regenerated `FS.GG.UI.Canvas` surface-area baseline, Expecto + FsCheck tests (determinism is a property test: same inputs ⇒ identical path/results), an FSI prelude transcript, and product-skill/`fs-gg-game-core` doc updates. Shipping is a **Tier 1 / contract-change**: on release, bump the FS.GG.UI coherent set and (publish-before-flip) update `registry/dependencies.yml` + `docs/registry/compatibility.md` in `FS-GG/.github` (FR-014).

> **Standing assumption — no unverified root-cause hypotheses here.** This is greenfield *additive* surface, not a defect fix, so there are no root-cause hypotheses to confirm. The "does it actually work end-to-end" obligation is met by (a) an FSI prelude transcript that calls the packed public surface the way a grid-game consumer would (route a path across a small walled grid; splash-query a bucketed item set), and (b) `/speckit-tasks` scheduling an early **consumer-shaped smoke** in the Foundational phase: exercise `astar`/`bfs`/`build`/`query` through their `.fsi` from an FSI script before building out the property-test suite.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution: exclusive stack, net10.0 default).

**Primary Dependencies**: none new. `Pathfinding` uses only `System` primitives + the new `Cell` value. `SpatialGrid` uses `FS.GG.UI.Scene.Point`/`Rect` and 239's `Geometry` (Canvas already references Scene). No new package reference; Canvas stays viewer/layout-free.

**Storage**: N/A (pure functions / immutable value structures, no persistence).

**Testing**: Expecto + FsCheck (property tests), `Microsoft.NET.Test.Sdk` + `YoloDev.Expecto.TestSdk`. New tests: `tests/Canvas.Tests/PathfindingTests.fs`, `tests/Canvas.Tests/SpatialGridTests.fs`. Reflection/surface gate: `tests/Package.Tests/SurfaceAreaTests.fs`. FSI transcript under `scripts/*-prelude.fsx`.

**Target Platform**: cross-platform .NET library surface; no GL/window/viewer dependency (FR-012 — consumable standalone).

**Project Type**: F# class-library package within the FS.GG.UI product (`FS.GG.UI.Canvas`).

**Performance Goals**: `astar`/`bfs` are O(E log V) over visited cells, bounded by `maxVisited`; `SpatialGrid.build` is O(n), `query`/`queryRadius` are O(cells touched + candidates). No hot-path allocation beyond the frontier/bucket structures; struct `Cell`.

**Constraints**: pure, deterministic, total (degenerate inputs return documented values, never throw). **Bit-identical output under identical inputs** is the load-bearing constraint (FR-003/FR-008): no reliance on `Dictionary`/`HashSet` iteration order, no floating-point cost ties. Additive public surface only — no change to any existing type, signature, or behavior.

**Scale/Scope**: two modules; four new source files (`Pathfinding.fsi/.fs`, `SpatialGrid.fsi/.fs`) + two test files + one baseline regeneration + doc/skill edits + (on release) the registry/compatibility contract-change.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — ✅ Honored. Tasks author each `.fsi` first (contracts in `contracts/`), exercise it via an FSI prelude, write Expecto/FsCheck semantic tests against the public surface, then implement the `.fs`.
- **II. Visibility Lives in `.fsi`, Not in `.fs`** — ✅ Each new public module ships a curated `.fsi`; `SpatialGrid<'T>` is made **opaque** via the `.fsi` (representation hidden), no `private`/`internal`/`public` modifiers on top-level `.fs` bindings (a gate test enforces this). Surface-area baseline `readiness/surface-baselines/FS.GG.UI.Canvas.txt` regenerated via `scripts/refresh-surface-baselines.fsx` and committed.
- **III. Idiomatic Simplicity Is the Default** — ✅ Plain pure F#: a struct `Cell`, a `Neighbourhood` DU, standard-library data structures for the frontier. Determinism is achieved by *design* (integer costs + total cell order), not by an exotic feature. No custom operators, SRTP, reflection, type providers, or non-trivial computation expressions. **No justification-required feature is used.**
- **IV. Elmish/MVU Is the Boundary for Stateful/I-O Workflows** — ✅ N/A by design: both helpers are *pure* and stateless (no I/O, retries, or background work). They are meant to be called from a **consumer's** `update`; the helpers own no state and request no effects, so no `Model/Msg/Cmd` boundary applies (FR-010). This is the intended shape, not an omission.
- **V. Test Evidence Is Mandatory** — ✅ Every helper gets tests that fail before and pass after (the modules don't exist yet). Determinism is a real property test (repeat-run byte-identity), not synthetic. All evidence is real (pure functions, no GL/IO).
- **VI. Observability and Safe Failure** — ✅ Pure helpers have no I/O to log. "Safe failure" is met by **totality**: degenerate inputs (start=goal, blocked/out-of-bound endpoints, unreachable goal, non-positive cell size, empty items, zero-area/zero-radius query) return documented values rather than throwing; conventions documented in `.fsi` and `research.md`.
- **Change Classification** — **Tier 1 (contracted change)**: adds public API surface **and** is a versioned cross-repo contract-change (FR-014). Full artifact chain required (spec, plan, `.fsi`, baselines, tests, docs) plus, on release, the registry/compatibility flip and coherent-set bump — all scheduled.

**Result: PASS.** No violations; Complexity Tracking table not required.

## Project Structure

### Documentation (this feature)

```text
specs/245-grid-sim-primitives/
├── plan.md              # This file
├── research.md          # Phase 0 — placement, A*/BFS determinism, cost convention, query contract
├── data-model.md        # Phase 1 — Cell/Neighbourhood/SpatialGrid value shapes + total-function conventions
├── quickstart.md        # Phase 1 — how to validate the two helpers end-to-end (FSI + tests)
├── contracts/           # Phase 1 — the intended .fsi signatures
│   ├── Pathfinding.fsi
│   └── SpatialGrid.fsi
├── checklists/
│   └── requirements.md  # spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 — /speckit-tasks (NOT created here)
```

### Source Code (repository root)

```text
src/Canvas/                      # FS.GG.UI.Canvas (deterministic sim tier; refs Scene only)
├── Rng.fsi / Rng.fs             # existing (239): value-type PRNG
├── FixedStep.fsi / FixedStep.fs # existing (239): accumulator drain
├── Pathfinding.fsi              # NEW — Cell, Neighbourhood, astar/bfs contract
├── Pathfinding.fs               # NEW — deterministic A*/BFS (integer cost + total cell order)
├── SpatialGrid.fsi              # NEW — opaque SpatialGrid<'T>, build/query/queryRadius contract
└── SpatialGrid.fs               # NEW — uniform-grid bucketing over Scene.Point/Rect (added to Canvas.Lib.fsproj)

tests/Canvas.Tests/PathfindingTests.fs   # NEW — correctness + determinism (repeat-run byte-identity) + degenerate totals
tests/Canvas.Tests/SpatialGridTests.fs   # NEW — no-false-negative + deterministic order + degenerate totals

readiness/surface-baselines/
└── FS.GG.UI.Canvas.txt          # regenerated (+ FS.GG.UI.Canvas.Cell, .Neighbourhood[+cases], .Pathfinding, .SpatialGrid`1)

template/product-skills/fs-gg-game-core/SKILL.md   # updated: grid guidance points at the now-real Pathfinding/SpatialGrid
scripts/*-prelude.fsx            # FSI transcript exercising the two helpers as a consumer would

# On release (contract-change, publish-before-flip) — in FS-GG/.github:
registry/dependencies.yml        # fs-gg-ui-template contract version + consuming edge bumped
registry/CHANGELOG.md            # one dated newest-first entry
docs/registry/compatibility.md   # dependency-graph + versioned-contracts row + coherence row
```

**Structure Decision**: Extend the existing **Canvas** package rather than introduce a new project (constitution: "Dependencies are minimized"; "primitives are distinct layers"). Both modules belong in Canvas — the package whose stated purpose is the deterministic fixed-timestep sim tier and which already ships the sibling `Rng`/`FixedStep`. `Pathfinding` introduces a grid-`Cell` value (integer coordinate, distinct from Scene's float `Point`) it owns; `SpatialGrid` reuses Scene's `Point`/`Rect` and 239's `Geometry` for its exact tests (Canvas already references Scene) so it introduces no look-alike geometry vocabulary (FR-011). Canvas references only Scene, so consumers get the primitives without rendering/viewer/layout machinery (FR-012).

## Complexity Tracking

*No Constitution Check violations — table intentionally omitted.*
