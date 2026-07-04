# Tasks: FS.GG.UI Simulation Primitives

**Input**: Design documents from `specs/239-sim-primitives/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (all present)

**Tests**: REQUIRED — Constitution Principle V ("Test Evidence Is Mandatory") and the spec's explicit acceptance scenarios. Each story follows the constitutional order **`.fsi` → failing semantic tests → implement `.fs`** (Principle I).

**Organization**: grouped by user story (US1 Geometry P1, US2 Rng P2, US3 FixedStep P3), each independently implementable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2 / US3

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: capture the no-regression baseline and confirm the ground the three modules land on.

- [X] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/239-sim-primitives/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set so pre-existing reds are known now, not mistaken for regressions at merge)
- [X] T002 [P] Confirm build ground: `dotnet build src/Scene/Scene.fsproj` and `dotnet build src/Canvas/Canvas.Lib.fsproj` are green on the branch, and record the exact `<Compile>` insertion points — `Geometry.fsi/.fs` after `Types` in `src/Scene/Scene.fsproj`; `Rng.fsi/.fs` and `FixedStep.fsi/.fs` after `Loop` in `src/Canvas/Canvas.Lib.fsproj`; new test files before `Program.fs` in each `*.Tests.fsproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: prove the API *shape* is right and wire the compile seams before any module is implemented.

**⚠️ CRITICAL**: no user-story implementation begins until this phase completes.

> **Early smoke — adapted for pure library primitives (STANDING requirement, honored not skipped).** These three helpers have no GUI/viewer to drive, so the "drive the real app and observe" obligation is met by an **FSI shape-smoke**: exercise the intended public signatures through F# Interactive exactly as a consumer would (the `quickstart.md` snippets), with throwaway bodies, *before* committing real implementations across three modules. This is the Principle I "sketch in FSI, validated by use" step and is the honest early-evidence analog of the live smoke. The plan's decisions (SplitMix64, closed-form drain, strict/inclusive AABB) are treated as unverified until the shapes run in FSI.

- [X] T003 Wire the compile seams: copy the three contract signatures from `specs/239-sim-primitives/contracts/` into `src/Scene/Geometry.fsi`, `src/Canvas/Rng.fsi`, `src/Canvas/FixedStep.fsi`; add each `.fsi` + a paired `.fs` with **loud-fail** placeholder bodies (`failwith "239: not yet implemented"` — honest, NOT success-shaped per Feature 237) to `src/Scene/Scene.fsproj` and `src/Canvas/Canvas.Lib.fsproj`; confirm both projects still compile
- [X] T004 **FSI shape-smoke**: in `dotnet fsi`, load the two built assemblies (or a scratch script) and run every snippet from `specs/239-sim-primitives/quickstart.md` §3 against *throwaway* implementations of the three signatures; confirm the shapes are ergonomic (struct-tuple destructuring reads well, `Rng` threads cleanly, `ofCenter`/`center` round-trip is natural). Record the transcript under `specs/239-sim-primitives/readiness/fsi-shape-smoke.md`. If a signature is awkward, fix the `.fsi` (and `contracts/`) NOW, before implementation
- [X] T005 [P] Confirm the surface-baseline tooling: dry-run `dotnet fsi scripts/refresh-surface-baselines.fsx`, diff, and record the expected additions (`FS.GG.UI.Scene.Geometry`; `FS.GG.UI.Canvas.Rng`, `FS.GG.UI.Canvas.FixedStep`, `Rng` state type) in the smoke note — do NOT commit the regenerated baselines yet (they'd go red until the real bodies land)

**Checkpoint**: signatures validated in FSI, compile seams in place — the three stories can now proceed independently.

---

## Phase 3: User Story 1 - Public collision geometry on Rect (Priority: P1) 🎯 MVP

**Goal**: ship `FS.GG.UI.Scene.Geometry` — public AABB `intersects`/`contains`/`containsPoint`/`center`/`ofCenter`/`sweptIntersects` on the shared `Rect`/`Point`.

**Independent Test**: from FSI or `tests/Scene.Tests`, compute overlap, containment, center, center-anchored construction, and swept overlap on `Rect` values and get correct, pure, total results — no rendering or game loop.

### Tests for User Story 1 (write FIRST, must FAIL before impl)

- [X] T006 [P] [US1] Write `tests/Scene.Tests/GeometryTests.fs` (Expecto + FsCheck) and register it in `tests/Scene.Tests/Scene.Tests.fsproj` before `Program.fs`. Cover: `intersects` true for overlap / false for disjoint / **false for edge- & corner-touch** (strict); `contains` inclusive of shared edges; `containsPoint` inclusive on low/high edges; `center (ofCenter c w h) = c` round-trip (FsCheck); `sweptIntersects` detects a fast projectile tunneling through a thin target AND is a superset of `intersects` at both sweep endpoints (FsCheck); degenerate/NaN rects never throw and return `false`. Run and confirm they FAIL (module still `failwith`)

### Implementation for User Story 1

- [X] T007 [US1] Finalize `src/Scene/Geometry.fsi` — curated signature with `/// Public contract …` doc comments in the repo `.fsi` style, `[<RequireQualifiedAccess>] module Geometry`, documenting the strict-`intersects` / inclusive-`contains` convention (research D2)
- [X] T008 [US1] Implement `src/Scene/Geometry.fs` — strict `intersects` (`<`/`>`), inclusive `contains`/`containsPoint` (`>=`/`<=`) matching `Evidence.fs:164`/`Scene.fs:452`, `center`/`ofCenter`, and `sweptIntersects` (broad-phase swept-AABB: expand `target` by `moving`'s extents and test the velocity segment, or min/max-time slab test). No `private`/`internal`/`public` modifiers (visibility lives in the `.fsi`)
- [X] T009 [US1] `dotnet test tests/Scene.Tests/Scene.Tests.fsproj` → all Geometry tests green; re-run the `quickstart.md` §3 Geometry FSI block and confirm each commented expectation

**Checkpoint**: US1 fully functional and independently testable — this is the shippable MVP.

---

## Phase 4: User Story 2 - Deterministic value-type PRNG (Priority: P2)

**Goal**: ship `FS.GG.UI.Canvas.Rng` — a `[<Struct>]` SplitMix64 generator (`ofSeed`/`nextInt`/`nextFloat`/`split`), every draw returning `(value, nextState)` and never mutating.

**Independent Test**: seed, draw a sequence, confirm same-seed reproducibility, input-state immutability, in-range draws, and that two copies of a state produce identical continuations — no rendering or loop.

### Tests for User Story 2 (write FIRST, must FAIL before impl)

- [X] T010 [P] [US2] Write `tests/Canvas.Tests/RngTests.fs` (Expecto + FsCheck) and register it in `tests/Canvas.Tests/Canvas.Tests.fsproj` before `Program.fs`. Cover: identical seed ⇒ byte-identical sequence; a draw leaves the input `Rng` unchanged (purity) and it reproduces its own next draw; `nextInt lo hi` stays in `[lo,hi]` inclusive incl. degenerate `lo=hi`→`lo`, `lo>hi`→`lo`; `nextFloat` in `[0.0,1.0)`; `split` yields two generators with differing streams; structural equality of `Rng` ⇒ identical continuation (SC-002). Run and confirm they FAIL
- [X] T011 [US2] Author `src/Canvas/Rng.fsi` — `[<Struct>] type Rng = { State: uint64 }` + `[<RequireQualifiedAccess>] module Rng` with the four `val`s and doc comments

### Implementation for User Story 2

- [X] T012 [US2] Implement `src/Canvas/Rng.fs` — SplitMix64 (`ofSeed` mixes the raw seed once so `0UL` is non-degenerate; `nextFloat` from the top 53 bits into `[0,1)`; `nextInt` inclusive-range mapping; `split` derives an independent seed via one extra mix). No access modifiers
- [X] T013 [US2] `dotnet test tests/Canvas.Tests/Canvas.Tests.fsproj` → all Rng tests green; re-run the `quickstart.md` §3 Rng FSI block

**Checkpoint**: US1 + US2 both work independently.

---

## Phase 5: User Story 3 - Fixed-timestep accumulator drain (Priority: P3)

**Goal**: ship `FS.GG.UI.Canvas.FixedStep` — pure closed-form `drain interval frameTime accumulator -> struct(int*float)` + explicit-clamp `drainWith` + `defaultMaxFrameTime` (0.25 s).

**Independent Test**: feed a delta sequence and confirm step counts + carried remainder match the accumulator semantics, including the spiral-of-death clamp — pure assertions only.

### Tests for User Story 3 (write FIRST, must FAIL before impl)

- [X] T014 [P] [US3] Write `tests/Canvas.Tests/FixedStepTests.fs` (Expecto + FsCheck) and register it in `tests/Canvas.Tests/Canvas.Tests.fsproj` before `Program.fs`. Cover: exact-N-intervals ⇒ N steps + expected remainder; sub-interval delta ⇒ 0 steps, accumulator grows; **huge delta ⇒ step count bounded by the clamp** (not unbounded); conservation `newAcc = (acc + clamp dt) - steps*interval` with `0 ≤ newAcc < interval` (FsCheck); `stepCount ≥ 0` always; degenerate `interval ≤ 0` and `frameTime ≤ 0` ⇒ `struct(0, accumulator)`; determinism over a scripted sequence; `drainWith 0.05` clamps tighter than the 0.25 default. Run and confirm they FAIL
- [X] T015 [US3] Author `src/Canvas/FixedStep.fsi` — `[<RequireQualifiedAccess>] module FixedStep` with `defaultMaxFrameTime`, `drain`, `drainWith`, documenting **seconds** units (research D5) and the 0.25 s default clamp reused from `Loop.advance`

### Implementation for User Story 3

- [X] T016 [US3] Implement `src/Canvas/FixedStep.fs` — closed-form: `let t = accumulator + min maxFrameTime (max 0.0 frameTime); if interval <= 0.0 then struct(0, accumulator) else let steps = int (floor (t / interval)) in struct(steps, t - float steps * interval)`; `drain` = `drainWith defaultMaxFrameTime`. No loop, no access modifiers
- [X] T017 [US3] `dotnet test tests/Canvas.Tests/Canvas.Tests.fsproj` → all FixedStep tests green; re-run the `quickstart.md` §3 FixedStep FSI block

**Checkpoint**: all three user stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: the Tier-1 obligations that span the whole feature — surface baselines, docs/skills, and the additivity proof. (Grouped here to avoid same-file conflicts across stories: both baselines and `product.md` are shared surfaces.)

- [ ] T018 Regenerate and commit the surface baselines: `dotnet fsi scripts/refresh-surface-baselines.fsx`, then verify only the expected lines were added to `readiness/surface-baselines/FS.GG.UI.Scene.txt` (`FS.GG.UI.Scene.Geometry`) and `readiness/surface-baselines/FS.GG.UI.Canvas.txt` (`FS.GG.UI.Canvas.Rng`, `FS.GG.UI.Canvas.FixedStep`, `Rng` state type); run `dotnet test tests/Package.Tests/Package.Tests.fsproj` (SurfaceAreaTests) → green
- [X] T019 [P] Update `template/base/docs/product.md` (FR-012): replace the "promised but internal" collision guidance (~lines 20 & 77) with the real `FS.GG.UI.Scene.Geometry` surface; add a short deterministic-`Rng` note (RNG state lives in the immutable `Model`; no `System.Random`) and a `FixedStep.drain` fixed-timestep note
- [X] T020 [P] Update the skill sources (NOT the `.claude/skills` mirror): `src/Scene/skill/SKILL.md` (`fs-gg-scene`) to advertise `Geometry` under its Public Contract section, and correct the stale `FS.GG.UI.SkillSupport.Random` reference in `src/Elmish/skill/SKILL.md` to point at `FS.GG.UI.Canvas.Rng`
- [X] T021 Additivity + full validation (SC-005): `dotnet build FS.GG.Rendering.slnx` (every existing consumer compiles unchanged); run `quickstart.md` §1–§5; re-run `dotnet fsi scripts/baseline-tests.fsx` and diff against the T001 baseline to prove no regressions (only the three new test files add green tests)
- [ ] T022 [P] (Optional — DEFERRED as a follow-up; feature complete without it) — SC-004 real-reuse proof) Re-point one sample game's hand-rolled PRNG (`samples/SampleApps/SampleApps.Core/Prng.fs` consumers under `samples/.../Games/*.fs`) at `FS.GG.UI.Canvas.Rng` and confirm `samples/SampleApps/SampleApps.Tests/DeterminismTests.fs` still passes — a consumer consuming the shipped primitive instead of re-rolling it

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (P1)** → no deps, start immediately.
- **Foundational (P2)** → depends on Setup; **blocks all stories** (compile seams + FSI shape validation).
- **User Stories (P3–P5)** → all depend on Foundational; then **independent** of each other (US1 touches only Scene; US2/US3 touch only Canvas and *different* files — `Rng.*` vs `FixedStep.*` — and *different* test files). Can run in parallel.
- **Polish (P6)** → depends on all desired stories complete (baselines/docs reflect whatever shipped).

### Within each story

- `.fsi` and failing tests before implementation; implementation before the green-run task.

### Parallel opportunities

- T002, T005 [P] in their phases.
- **After Foundational, all three stories run in parallel** — US1 (Scene) ⟂ US2 (Rng) ⟂ US3 (FixedStep), no shared files until Polish.
- The three test-authoring tasks (T006, T010, T014) are [P] across stories.
- In Polish, T019 and T020 [P] (docs vs skills, different files); T018 and T021 are sequential gates.

---

## Parallel Example: after Foundational

```text
# Three independent module streams in parallel (different packages/files):
Stream A (US1): T006 → T007 → T008 → T009   (src/Scene/Geometry.*, tests/Scene.Tests/GeometryTests.fs)
Stream B (US2): T010 → T011 → T012 → T013   (src/Canvas/Rng.*,       tests/Canvas.Tests/RngTests.fs)
Stream C (US3): T014 → T015 → T016 → T017   (src/Canvas/FixedStep.*, tests/Canvas.Tests/FixedStepTests.fs)
# Converge at T018 (baselines) → T021 (additivity proof).
```

---

## Implementation Strategy

### MVP first (User Story 1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (incl. the FSI shape-smoke) → 3. Phase 3 US1 Geometry → **STOP & VALIDATE** (Geometry independently green + FSI quickstart) → ship as MVP. Geometry alone delivers the most-cited, already-advertised surface.

### Incremental delivery

Setup + Foundational → US1 (MVP, ship) → US2 → US3 → Polish. Each story adds value without touching the others' files.

### Notes

- Tier 1: `.fsi` + regenerated baselines (T018) + tests + docs (T019/T020) are all required — do not merge without them.
- Verify each story's tests FAIL before implementing (the loud-fail placeholders from T003 guarantee red).
- Loud-fail placeholders (`failwith`) are a within-branch scaffold only — every one is replaced by a real body by T008/T012/T016; none may survive to merge (Feature 237: no success-shaped stubs, and no stubs at all in the shipped surface).
- Commit after each task or logical group.
