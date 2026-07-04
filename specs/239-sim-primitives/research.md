# Phase 0 Research: FS.GG.UI Simulation Primitives

All decisions below resolve the "NEEDS CLARIFICATION"-class unknowns from the plan's Technical Context. Each is a planning choice the spec explicitly deferred (see spec Assumptions).

## D1 — Module placement

**Decision**: `Geometry` → `src/Scene/` (`namespace FS.GG.UI.Scene`); `Rng` and `FixedStep` → `src/Canvas/` (`namespace FS.GG.UI.Canvas`).

**Rationale**:
- `Geometry` operates on `Rect`/`Point`, both defined in `src/Scene/Types.fs`. Placing it in Scene means it introduces **no new geometry vocabulary** (FR-011) and directly delivers the "reuse the shared `FS.GG.UI.Scene.Rect`" surface that `template/base/docs/product.md` already advertises. Scene is dependency-light and pure — geometry fits without pulling anything in.
- `Canvas` (`FS.GG.UI.Canvas`, description: "…deterministic fixed-timestep game loop") already ships the stateful `Loop.advance`/`StepState` and references **only** Scene. `FixedStep.drain` is the lower-level, stateless primitive underneath `Loop.advance`; co-locating keeps the fixed-timestep concept (and its clamp constant) in one package. `Rng` is the companion determinism primitive for the same sim/game audience.

**Alternatives considered**:
- *All three in Scene*: rejected — `Rng`/`FixedStep` are simulation-runtime concepts, not scene geometry; putting them in Scene pollutes its vocabulary and splits the fixed-timestep story from the existing `Loop`.
- *A new `FS.GG.UI.Sim` package*: rejected — adds a package/baseline/test-project/skill for ~three tiny modules; violates "dependencies/layers minimized." Canvas already is the sim/game-runtime layer.

## D2 — AABB edge conventions (Geometry)

**Decision**: `intersects` uses **strict** inequalities (`<` / `>`) — rectangles that only touch at an edge/corner do **not** intersect. `contains` (rect-in-rect) and point-containment use **inclusive** inequalities (`>=` / `<=`) — a shape flush against the boundary *is* contained.

**Rationale**: This is the exact, consistent convention already used by every internal helper in the repo, so the public helper matches observed behavior and existing tests:
- `src/Scene/Evidence.fs:164` `private intersects` — strict.
- `src/Scene/Scene.fs:452` `classifyPlacement` — strict `intersects` + inclusive `inside`, side by side.
- `src/Testing/TestingVisual.fs:787` — strict `intersects`, inclusive `contains`.

Documented in each `.fsi` and in `data-model.md`. Point containment treats the rectangle as the half-open region under the same inclusive-low / inclusive-high reading used by `inside` (documented so consumers know edge behavior).

**Alternatives considered**: inclusive `intersects` (touching = overlap) — rejected; contradicts existing repo behavior and would make edge-adjacent HUD/gameplay rectangles register spurious collisions.

## D3 — Degenerate-input totality

**Decision**: All helpers are **total** — they return documented values instead of throwing.
- Geometry on a zero/negative-size `Rect`: computed with the same plain strict/inclusive inequalities as the existing repo helpers — no zero-area special-casing. A degenerate rect touching only the boundary does not `intersects` (strict); one lying strictly interior does (the documented consequence of the strict formula, not a separate rule). NaN inputs yield `false` (comparisons are false), never exceptions.
- `Rng.nextInt lo hi` with `lo = hi` returns `lo`; with `lo > hi` returns `lo` (documented, non-throwing) and advances state.
- `FixedStep.drain` with interval `<= 0` returns `struct(0, accumulator)` (no divide-by-zero, no unbounded loop). With `frameTime <= 0` it adds nothing and returns `struct(0, accumulator)` — never a negative step count.

**Rationale**: Principle VI "safe failure" for pure helpers = totality; consumers embedding these in a hot `update` loop must never get a surprise exception from a stray NaN or a paused-frame `dt`.

## D4 — RNG algorithm

**Decision**: **SplitMix64** — 64-bit state, the well-known `state += 0x9E3779B97F4A7C15; z = state; z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9; z = (z ^ (z >> 27)) * 0x94D049BB133111EB; z ^ (z >> 31)` finalizer. `ofSeed` seeds the state; `nextInt`/`nextFloat` derive a value and return the advanced state; `split` derives an independent seed from the current state.

**Rationale**:
- Value-type by construction (state is a single `uint64` in a struct), so it drops straight into an immutable `Model` and record structural equality implies equal RNG state (SC-002).
- Deterministic and trivially reproducible from a seed (SC-002 / FR-008).
- **Splittable** (the issue asked for "splittable/seeded"): SplitMix64 is the canonical splittable generator; `split` is one extra draw, no extra machinery.
- Better statistical distribution than a bare LCG for uniform `nextInt`/`nextFloat`.

**Prior art / cleanup**: `samples/SampleApps/SampleApps.Core/Prng.fs` ships a **sample-local** MMIX LCG (`{ State: uint64 }`, `state -> (value, nextState)`) — the same value-type shape, confirming the ergonomics. It stays as-is for now; optionally a follow-up re-points the sample games at the shipped `Rng` (proves SC-004). Also note `src/Elmish/skill/SKILL.md` (lines ~62–78) references a **non-existent** `FS.GG.UI.SkillSupport.Random` module ("as of feature 062") — stale/aspirational; this feature makes a real PRNG exist and that stale skill text should be corrected (tracked as a doc task).

**Alternatives considered**: reuse the sample's MMIX LCG (weaker distribution, not splittable); `System.Random` (mutable/reference — the exact smell we're removing); xoshiro256 (larger 256-bit state, more than needed for a value-in-`Model` primitive).

## D5 — FixedStep signature, units, and clamp

**Decision**:
- Unify on **seconds** (float) for `interval`, `frameTime`, and `accumulator`, matching the existing `Loop.advance` convention (`dt = 1.0/60.0`, clamp `<= 0.25`) and the design reports. The consumer's shorthand `intervalMs` maps to `interval` in seconds; documented in the `.fsi`. Rationale: a ms/seconds split inside one package (`Loop` in seconds, `FixedStep` in ms) is exactly the silent unit-mismatch bug this feature exists to remove.
- Signature: `drain: interval: float -> frameTime: float -> accumulator: float -> struct(int * float)` returning `struct(stepCount, newAccumulator)`. Closed-form: `let t = accumulator + clamp frameTime; let steps = int (floor (t / interval)); struct(steps, t - float steps * interval)` — no `while` loop needed (Principle III: plainest code).
- **Clamp**: default spiral-of-death clamp = **0.25s**, reusing the established `Loop.advance` constant so the Canvas package has one clamp, not two. A second entry point `drainWith: maxFrameTime: float -> interval: float -> frameTime: float -> accumulator: float -> struct(int * float)` lets a consumer pass an explicit tighter clamp (e.g. the 0.05s the consumer report suggested) without changing the default.

**Rationale**: honors the requested `(interval, dt, accumulator) -> (steps, remainder)` shape and the "clamp the large delta" requirement (FR-010) while keeping one canonical default consistent with the package's existing loop. The explicit-clamp variant preserves the consumer's ability to choose 0.05s.

**Open decision surfaced to the maintainer**: the default clamp (0.25s, repo precedent) vs. the consumer feedback's 0.05s. Defaulting to 0.25s + offering `drainWith` is the reconciliation; a maintainer who prefers 0.05s as the default can say so before `/speckit-tasks`.

**Alternatives considered**: milliseconds throughout (rejected — clashes with `Loop`'s seconds); a `while`-drain implementation (rejected — closed-form is simpler and equally clear); no explicit-clamp variant (rejected — loses the consumer's requested control).

## D6 — Surface baselines & gate mechanics

**Decision**: after adding each module, regenerate the affected baseline with `scripts/refresh-surface-baselines.fsx` and commit the updated `readiness/surface-baselines/FS.GG.UI.Scene.txt` and `FS.GG.UI.Canvas.txt`. The `tests/Package.Tests/SurfaceAreaTests.fs` gate asserts set-equality between the baseline and the reflected assembly, so an un-regenerated baseline fails CI — this is the intended Tier-1 tripwire, not an obstacle.

**Rationale**: matches Principle II and the honest-public-API discipline (Feature 237): the new helpers must do real work (SplitMix64 draws, real arithmetic — no success-shaped stubs) and appear in the baselines. Modules reflect as `FS.GG.UI.Scene.Geometry`, `FS.GG.UI.Canvas.Rng`, `FS.GG.UI.Canvas.FixedStep` (the `Module` suffix is stripped by the generator); the `Rng` state type reflects as its own entry.

## D7 — Docs & skills to update (FR-012)

**Decision**: update `template/base/docs/product.md` (replace "promised but internal" collision text at lines 20/77 with the now-real `Geometry` surface; add short RNG-determinism and fixed-step notes) and `src/Scene/skill/SKILL.md` (the `fs-gg-scene` source, not the `.claude/skills` mirror). There is **no** dedicated Canvas skill, so Rng/FixedStep guidance lives in `product.md` (+ the stale `src/Elmish/skill/SKILL.md` Random reference gets corrected). Authoring the separate `fs-gg-game-core` consumer skill is **out of scope** (FS-GG/FS.GG.Rendering#73).

**Rationale**: matches spec Assumptions; keeps edits at the skill *source* of truth so the materialization stays coherent.
