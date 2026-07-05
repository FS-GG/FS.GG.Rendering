# Tasks: FS.GG.UI Grid Simulation Primitives (Pathfinding + Spatial Grid)

**Input**: Design documents from `specs/245-grid-sim-primitives/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (all present)

**Tests**: REQUIRED — Constitution Principle V ("Test Evidence Is Mandatory") and the spec's explicit acceptance scenarios. Each story follows the constitutional order **`.fsi` → failing semantic tests → implement `.fs`** (Principle I).

**Organization**: grouped by user story (US1 Pathfinding P1, US2 SpatialGrid P2), each independently implementable and testable. Both modules land in `FS.GG.UI.Canvas` (`src/Canvas/`) alongside 239's `Rng`/`FixedStep`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: capture the no-regression baseline and confirm the ground the two modules land on.

- [X] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/245-grid-sim-primitives/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` and records the full red/green set so pre-existing reds are known now, not mistaken for regressions at merge)
- [X] T002 [P] Confirm build ground: `dotnet build src/Canvas/Canvas.Lib.fsproj` is green on the branch, and record the exact `<Compile>` insertion points — `Pathfinding.fsi/.fs` then `SpatialGrid.fsi/.fs` after `FixedStep` in `src/Canvas/Canvas.Lib.fsproj` (SpatialGrid after Pathfinding; both after the existing sim primitives); new test files `PathfindingTests.fs`/`SpatialGridTests.fs` after `FixedStepTests.fs` and before `Program.fs` in `tests/Canvas.Tests/Canvas.Tests.fsproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: prove the API *shape* is right and wire the compile seams before either module is implemented.

**⚠️ CRITICAL**: no user-story implementation begins until this phase completes.

> **Early smoke — adapted for pure library primitives (STANDING requirement, honored not skipped).** These helpers have no GUI/viewer to drive, so the "drive the real app and observe" obligation is met by an **FSI shape-smoke**: exercise the intended public signatures through F# Interactive exactly as a consumer would (the `quickstart.md` §1 snippet), with throwaway bodies, *before* committing real implementations. This is the Principle I "sketch in FSI, validated by use" step and the honest early-evidence analog of the live smoke. The plan's decisions (integer 10/14 cost + total `(f,h,Col,Row)` order; exact `SpatialGrid` queries; `maxVisited` bound) are treated as unverified until the shapes run in FSI.

- [X] T003 Wire the compile seams: copy the two contract signatures from `specs/245-grid-sim-primitives/contracts/` into `src/Canvas/Pathfinding.fsi` and `src/Canvas/SpatialGrid.fsi`; add each `.fsi` + a paired `.fs` with **loud-fail** placeholder bodies (`failwith "245: not yet implemented"` — honest, NOT success-shaped per Feature 237) to `src/Canvas/Canvas.Lib.fsproj` at the recorded insertion points; confirm the project still compiles
- [X] T004 **FSI shape-smoke**: in `dotnet fsi`, load the built Canvas assembly (or a scratch script) and run the `specs/245-grid-sim-primitives/quickstart.md` §1 snippet against *throwaway* implementations of the two signatures; confirm the shapes are ergonomic (`Cell` record construction reads well, `Neighbourhood`/`maxVisited` param order pipelines naturally, `Pathfinding.astar` returns a destructurable `Cell list option`, `SpatialGrid.build` + `queryRadius` thread cleanly with `Scene.Point`/`Rect`). Record the transcript under `specs/245-grid-sim-primitives/readiness/fsi-shape-smoke.md`. If a signature is awkward, fix the `.fsi` (and `contracts/`) NOW, before implementation
- [X] T005 [P] Confirm the surface-baseline tooling: dry-run `dotnet fsi scripts/refresh-surface-baselines.fsx`, diff, and record the expected additions to `readiness/surface-baselines/FS.GG.UI.Canvas.txt` (`FS.GG.UI.Canvas.Cell`, `FS.GG.UI.Canvas.Neighbourhood` + `+FourWay`/`+EightWay`, `FS.GG.UI.Canvas.Pathfinding`, `FS.GG.UI.Canvas.SpatialGrid\`1`) in the smoke note — do NOT commit the regenerated baseline yet (it'd go red until the real bodies land)

**Checkpoint**: signatures validated in FSI, compile seams in place — the two stories can now proceed independently.

---

## Phase 3: User Story 1 - Deterministic grid pathfinding (Priority: P1) 🎯 MVP

**Goal**: ship `FS.GG.UI.Canvas.Pathfinding` — `Cell`, `Neighbourhood`, and deterministic `astar`/`bfs` over a caller walkability predicate, 4/8-neighbour, `maxVisited`-bounded, endpoint-inclusive, byte-identical output.

**Independent Test**: from FSI or `tests/Canvas.Tests`, route a path across a small walled grid under `FourWay`/`EightWay` for `astar` and `bfs`, get a correct shortest/hop-minimal path (or documented `None`), and confirm a repeat call is byte-identical — no rendering or game loop.

### Tests for User Story 1 (write FIRST, must FAIL before impl)

- [X] T006 [P] [US1] Write `tests/Canvas.Tests/PathfindingTests.fs` (Expecto + FsCheck) and register it in `tests/Canvas.Tests/Canvas.Tests.fsproj` after `FixedStepTests.fs`. Cover: **optimality (INV-2)** — hand-computed shortest path on a small clear grid and around a wall, for `astar` and `bfs`, `FourWay` and `EightWay` (endpoints included; `start=goal`→`[start]`); **8-way cost** — a diagonal-favouring open grid routes diagonally under `EightWay`/`astar` (10/14 cost) but stair-steps under `FourWay`; **no corner-cut (D5)** — a diagonal blocked by two orthogonal walls is not taken; **no-path/bounds (FR-005)** — walled-off goal→`None`, blocked `start`/`goal`→`None`, `maxVisited≤0`→`None`, unreachable goal terminates at the cap; **determinism (INV-1)** — FsCheck: repeat calls byte-identical, and a grid with ≥2 equal-cost routes yields one stable path across runs. Run and confirm they FAIL (module still `failwith`)
- [X] T007 [US1] Finalize `src/Canvas/Pathfinding.fsi` — curated signature with `/// Public contract …` doc comments in the repo `.fsi` style: `[<Struct>] type Cell`, `type Neighbourhood = FourWay | EightWay`, `[<RequireQualifiedAccess>] module Pathfinding` with `astar`/`bfs`, documenting integer 10/14 costs, total `(f,h,Col,Row)` tie-break, no-corner-cut, endpoint-inclusion, and `maxVisited` semantics (research D2–D6)

### Implementation for User Story 1

- [X] T008 [US1] Implement `src/Canvas/Pathfinding.fs` — A* with an integer priority key `(f, h, Col, Row)` over a deterministic frontier (a `Map`/sorted structure, never `Dictionary`/`HashSet`), `gScore`/`cameFrom` as `Map<Cell,_>`; `FourWay` cost 1(bfs)/10(astar), `EightWay` orthogonal 10 / diagonal 14 with the no-corner-cut guard; admissible Manhattan×10 / octile heuristic; `maxVisited` expansion cap → `None`; `bfs` a FIFO unweighted variant sharing neighbour enumeration; path reconstruction includes `start`..`goal`. Total on all degenerate inputs (data-model table). No `private`/`internal`/`public` modifiers (visibility lives in the `.fsi`)
- [X] T009 [US1] `dotnet test tests/Canvas.Tests/Canvas.Tests.fsproj` → all Pathfinding tests green; re-run the `quickstart.md` §1 Pathfinding FSI block and confirm the commented path + determinism expectations

**Checkpoint**: US1 fully functional and independently testable — this is the shippable MVP (and the surface that unblocks FS-GG/FS.GG.Rendering#112).

---

## Phase 4: User Story 2 - Uniform spatial grid for range/splash queries (Priority: P2)

**Goal**: ship `FS.GG.UI.Canvas.SpatialGrid` — an opaque `SpatialGrid<'T>` built from a cell size + positioned `Scene.Point` items, with exact `query` (Rect) / `queryRadius`, deterministic insertion order.

**Independent Test**: build a grid from positioned items, query a rect and a radius, confirm the returned items exactly equal a brute-force filter over the same items in insertion order, and confirm degenerate `cellSize`/`radius` are total — no rendering or loop.

### Tests for User Story 2 (write FIRST, must FAIL before impl)

- [X] T010 [P] [US2] Write `tests/Canvas.Tests/SpatialGridTests.fs` (Expecto + FsCheck) and register it in `tests/Canvas.Tests/Canvas.Tests.fsproj` after `PathfindingTests.fs`. Cover: **exactness (INV-3)** — FsCheck: `query region`/`queryRadius center r` equal a brute-force `List.filter` (`Geometry.containsPoint` / squared-distance ≤ r²) over the same items, no false negatives or positives; **order (INV-1)** — results are in insertion order and byte-identical across repeat builds/queries; **edges** — item exactly on a rect edge / at distance = r is included (inclusive convention); **degenerate (FR-009)** — empty items→`[]`, `cellSize≤0`/non-finite→single-bucket still-exact results, `radius≤0`→center-coincident/`[]`, zero-area rect→items on it. Run and confirm they FAIL
- [X] T011 [US2] Finalize `src/Canvas/SpatialGrid.fsi` — `[<Sealed>] type SpatialGrid<'T>` (opaque, D9) + `[<RequireQualifiedAccess>] module SpatialGrid` with `build`/`query`/`queryRadius` and `/// Public contract …` doc comments documenting exact-results, insertion order, inclusive edges, squared-distance, and degenerate `cellSize`/`radius` totals (research D7–D9). `open FS.GG.UI.Scene`

### Implementation for User Story 2

- [X] T012 [US2] Implement `src/Canvas/SpatialGrid.fs` — internal `Map<struct(int*int), int list>` of item indices into an insertion-ordered array + the `cellSize`; `build` buckets by `floor (coord / cellSize)` (single bucket when `cellSize≤0`/non-finite); `query` gathers candidate buckets overlapping `region` then exact-filters with `Geometry.containsPoint` preserving index order; `queryRadius` gathers buckets overlapping the center±radius box then exact-filters by squared distance. No `sqrt` on the hot path, no access modifiers
- [X] T013 [US2] `dotnet test tests/Canvas.Tests/Canvas.Tests.fsproj` → all SpatialGrid tests green; re-run the `quickstart.md` §1 SpatialGrid FSI block

**Checkpoint**: US1 + US2 both work independently.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: the Tier-1 obligations that span the whole feature — surface baseline, docs/skills, the additivity proof, and the release contract-change. (Grouped here to avoid same-file conflicts across stories: the baseline and skill docs are shared surfaces.)

- [X] T014 Regenerate and commit the surface baseline: `dotnet fsi scripts/refresh-surface-baselines.fsx`, then verify ONLY the expected lines were added to `readiness/surface-baselines/FS.GG.UI.Canvas.txt` (`FS.GG.UI.Canvas.Cell`, `FS.GG.UI.Canvas.Neighbourhood` + `+FourWay`/`+EightWay`, `FS.GG.UI.Canvas.Pathfinding`, `FS.GG.UI.Canvas.SpatialGrid\`1`); run `dotnet test tests/Package.Tests/Package.Tests.fsproj` (SurfaceAreaTests) → green
- [X] T015 [P] Add an FSI prelude transcript `scripts/grid-sim-prelude.fsx` (the `quickstart.md` §1 snippet) exercising `Pathfinding.astar`/`bfs` and `SpatialGrid.build`/`query`/`queryRadius` through the packed public surface as a consumer would; confirm it runs clean against the real assemblies
- [X] T016 [P] Update `template/product-skills/fs-gg-game-core/SKILL.md` (FR-013): replace the "recommended but unshipped" uniform-spatial-grid / hand-rolled-pathfinding guidance with the now-real `FS.GG.UI.Canvas.Pathfinding` (astar/bfs, 4/8-way, deterministic) and `FS.GG.UI.Canvas.SpatialGrid` (build/query/queryRadius) surface, with a short determinism note (integer costs + stable tie-break ⇒ replay-safe). Update the mirror only if the repo's skill-sync step requires it
- [X] T017 Additivity + full validation (SC-005): `dotnet build FS.GG.Rendering.slnx` (every existing consumer compiles unchanged); run `quickstart.md` §1–§4; re-run `dotnet fsi scripts/baseline-tests.fsx` and diff against the T001 baseline to prove no regressions (only the two new test files add green tests)
- [X] T018 [P] Record `research.md` D15/Dijkstra-flow-field deferral (FR-015) is already tracked; confirm no flow-field surface leaked into the baseline (out-of-scope guard), and that FS-GG/FS.GG.Rendering#112 can now reference `Pathfinding`/`SpatialGrid` by name
- [ ] T019 **Release — contract-change, publish-before-flip (FR-014, SEPARATE AUTHORIZED STEP — do not run without go-ahead)**: bump the FS.GG.UI coherent set (the two version-of-truth files + the `v<V>`/`fs-gg-ui/v<V>`/`fs-gg-ui-template/v<V>` tag triple); confirm the package is LIVE on the org feed; then in `FS-GG/.github` flip `registry/dependencies.yml` (`fs-gg-ui-template` version + consuming edge + `updated:`), prepend a dated `registry/CHANGELOG.md` entry, update `docs/registry/compatibility.md`, and validate with `fsgg-sdd registry validate`; re-pin `FS.GG.Templates` `providers/rendering.providers.yml`. See cross-repo-coordination skill "Release a coherent set" + research D10

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (P1)** → no deps, start immediately.
- **Foundational (P2)** → depends on Setup; **blocks both stories** (compile seams + FSI shape validation).
- **US1 Pathfinding (P3)** → depends on Foundational; independent of US2. **MVP.**
- **US2 SpatialGrid (P4)** → depends on Foundational; independent of US1.
- **Polish (P5)** → depends on both stories being implemented (T014/T017 need the real surface); T019 release depends on all prior + explicit human authorization.

### Story independence

US1 and US2 touch disjoint files (`Pathfinding.*`/`PathfindingTests.fs` vs `SpatialGrid.*`/`SpatialGridTests.fs`) and share only the `Canvas.Lib.fsproj`/`Canvas.Tests.fsproj` compile lists (seams wired once in T003/T006/T010). Either can ship alone; US1 is the MVP and the one that unblocks #112.

---

## Parallel Example: after Foundational

```text
# Two independent module streams in parallel (different files):
Stream A (US1): T006 → T007 → T008 → T009   (src/Canvas/Pathfinding.*, tests/Canvas.Tests/PathfindingTests.fs)
Stream B (US2): T010 → T011 → T012 → T013   (src/Canvas/SpatialGrid.*,  tests/Canvas.Tests/SpatialGridTests.fs)
# Converge at T014 (baseline) → T017 (additivity proof) → T019 (release, authorized).
```

---

## Implementation Strategy

### MVP first (User Story 1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (incl. the FSI shape-smoke) → 3. Phase 3 US1 Pathfinding → **STOP & VALIDATE** (Pathfinding independently green + FSI quickstart) → ship as MVP. Pathfinding alone delivers the higher-risk determinism surface and unblocks #112.

### Incremental delivery

Setup + Foundational → US1 (MVP) → US2 → Polish → (authorized) Release. Each story adds value without touching the other's files.

### Notes

- Tier 1: `.fsi` + regenerated baseline (T014) + tests + docs (T016) are all required — do not merge without them.
- Verify each story's tests FAIL before implementing (the loud-fail placeholders from T003 guarantee red).
- Loud-fail placeholders (`failwith`) are a within-branch scaffold only — every one is replaced by a real body by T008/T012; none may survive to merge (Feature 237: no success-shaped stubs, and no stubs at all in the shipped surface).
- **Determinism is the load-bearing property** — the tie-break (integer cost + total `(f,h,Col,Row)` order) and insertion-order results are the whole point (SC-001); property-test them explicitly (repeat-run byte-identity), not just by example.
- T019 (release) is outward-facing (publishes to the org feed) and is the natural human-authorized checkpoint — do not run it without an explicit go-ahead.
- Commit after each task or logical group.
