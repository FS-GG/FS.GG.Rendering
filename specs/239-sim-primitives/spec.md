# Feature Specification: FS.GG.UI Simulation Primitives

**Feature Branch**: `239-sim-primitives`

**Created**: 2026-07-04

**Status**: Draft

**Input**: FS-GG/FS.GG.Rendering#72 (epic FS-GG/.github#165, Space Invaders consumer feedback §3.1–3.3). Add three small, high-reuse, simulation-shaped public helpers to FS.GG.UI so game/sim consumers stop re-implementing them: a public AABB geometry helper on `Rect`, a value-type seeded PRNG, and a fixed-timestep accumulator drain.

## Context

FS.GG.UI is a rendering-first framework. A full SDD-lifecycle consumer build of the Space Invaders TestSpec found the rendering/input/layout surface sufficient and pleasant, but every game/simulation consumer re-rolls the same three *simulation-shaped* helpers because the framework does not ship them:

- The shipped product guidance already **promises** the surface — `docs/product.md` tells consumers "Game entities reuse shared Scene geometry for layout, containment, **collision**" and "Reuse the shared `FS.GG.UI.Scene.Rect` bounds type rather than a look-alike" — but the only geometry helpers that exist (`Scene.Evidence`'s private `intersects`, `TestingVisual`'s private `intersects`/`contains`) are internal and layout/evidence-shaped, not a public collision surface. Consumers must hand-write AABB tests.
- Deterministic-replay acceptance (identical seed + identical input ⇒ identical outcome) forces consumers to embed a mutable `System.Random` inside their MVU `Model`, which is a determinism smell: `{ model with … }` shares the instance, so clone/replay silently diverges and record structural equality no longer implies equal RNG state.
- The fixed-timestep accumulator pattern is documented (in design reports and product guidance) but not shipped, so each consumer hand-rolls the `while` drain and the variable-`dt` clamp.

These gaps are additive and non-architectural: three pure, value-type helpers that make the *already-recommended* patterns real, without introducing any per-game logic.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Public collision geometry on Rect (Priority: P1)

A consumer building a game needs to test whether two on-screen entities overlap (a bullet hits an invader), whether a point or entity is contained in a region (a ship stays inside the play field), and to place an entity by its center. Today they must hand-write these AABB tests or copy the framework's private, layout-shaped `intersects`. This story ships a public geometry helper on the shared `Rect` type so the collision/containment surface the product guidance already promises actually exists.

**Why this priority**: This is the most-cited gap and the one the shipped guidance already advertises as available. It unblocks the core game loop (hit detection) and removes look-alike geometry vocabulary the guidance explicitly warns against. It is independently valuable even if the other two stories never ship.

**Independent Test**: A consumer can compute overlap, containment, center, and center-anchored construction against `Rect` values and observe correct, pure results — verifiable in isolation with unit assertions, no rendering or game loop required.

**Acceptance Scenarios**:

1. **Given** two overlapping rectangles, **When** the consumer tests them for intersection, **Then** the result is "they overlap"; **and given** two disjoint rectangles the result is "they do not overlap".
2. **Given** two rectangles that only touch at an edge or corner (zero-area overlap), **When** the consumer tests intersection, **Then** the result follows a single, documented, consistent boundary convention.
3. **Given** an outer rectangle and an inner rectangle fully inside it, **When** the consumer tests containment, **Then** the result is "contained"; **and** a point inside/outside the rectangle reports contained/not-contained consistently with the same boundary convention.
4. **Given** a rectangle, **When** the consumer asks for its center, **Then** they get the correct center point; **and given** a center point and a size, **When** they construct a rectangle from the center, **Then** the rectangle is centered on that point and round-trips with the center query.
5. **Given** a fast-moving entity that would pass entirely through a thin target within one step (tunneling), **When** the consumer tests the swept path of the moving rectangle against the target, **Then** the overlap is detected even though the start and end positions do not overlap.

---

### User Story 2 - Deterministic value-type PRNG (Priority: P2)

A consumer needs randomness (enemy fire cadence, spawn jitter) that is fully deterministic under replay: the same seed and the same sequence of draws must always produce the same outcome, and cloning the model must clone the RNG state. This story ships a seeded, value-type PRNG whose draw operations return a new state rather than mutating shared state, so it can live inside an immutable MVU `Model` without breaking determinism.

**Why this priority**: It removes a genuine correctness hazard (shared mutable `System.Random` in an immutable model) that silently breaks replay/clone determinism and defeats structural equality. It is second because collision (P1) is required for a game loop to exist at all, whereas deterministic randomness is required for a game loop to be *reproducible*.

**Independent Test**: A consumer can seed the generator, draw a sequence of integers and floats, and confirm that (a) the same seed reproduces the same sequence exactly, (b) advancing the state returns a new value rather than mutating the old one, and (c) two copies of a given state produce identical continuations — all without any rendering or game loop.

**Acceptance Scenarios**:

1. **Given** a generator seeded with a fixed value, **When** the consumer draws a sequence of numbers, **Then** repeating from the same seed produces a byte-for-byte identical sequence.
2. **Given** a generator state, **When** the consumer draws the next value, **Then** they receive both the drawn value and a new generator state, and the original state is unchanged and still reproduces its own next value.
3. **Given** an integer draw bounded to a range, **When** the consumer requests it, **Then** the value falls within the requested bounds under the documented inclusivity convention, uniformly enough to be usable; **and** a float draw falls within its documented range (e.g. 0 inclusive to 1 exclusive).
4. **Given** two products that copy the same generator state into their models and then apply the same inputs, **When** they run, **Then** their random-driven outcomes are identical (replay/clone determinism holds).

---

### User Story 3 - Fixed-timestep accumulator drain (Priority: P3)

A consumer advancing a simulation needs to decouple simulation rate from frame rate: given a variable wall-clock delta each frame, run a whole number of fixed-size simulation steps and carry the remainder, while clamping a pathologically large delta so a stalled frame cannot trigger a runaway "spiral of death" of catch-up steps. This story ships a single pure helper that, given the fixed interval, the elapsed delta, and the carried accumulator, returns how many fixed steps to run and the new accumulator remainder.

**Why this priority**: It standardizes an already-documented pattern and removes hand-rolled `while`-drain bugs, but a consumer can still ship a correct (if less smooth) loop without it, so it is the lowest of the three.

**Independent Test**: A consumer can feed a sequence of deltas into the helper and confirm the step count and carried remainder match the fixed-timestep accumulator semantics, including the large-delta clamp — verifiable purely with assertions.

**Acceptance Scenarios**:

1. **Given** a fixed interval and an accumulator plus delta that together cover exactly N intervals, **When** the consumer drains, **Then** they get N steps and a remainder consistent with the leftover time.
2. **Given** a delta smaller than one interval, **When** the consumer drains, **Then** they get zero steps and the accumulator grows by the delta.
3. **Given** a very large delta (e.g. a long stall), **When** the consumer drains, **Then** the number of steps is bounded by the documented clamp rather than unbounded catch-up.
4. **Given** the same interval and a sequence of deltas, **When** two consumers drain the same sequence, **Then** they produce identical step counts and remainders (the helper is pure and deterministic).

---

### Edge Cases

- **Degenerate rectangles**: zero-width/zero-height or negative-size rectangles — behavior under intersection/containment must be documented and consistent (not throw).
- **Boundary/touching geometry**: edge- and corner-touching rectangles resolve under one stated convention (inclusive vs exclusive), applied uniformly across intersects/contains.
- **PRNG range degeneracy**: an integer range whose low equals high, or low greater than high — documented, non-throwing behavior.
- **PRNG seed degeneracy**: a zero or otherwise "weak" seed must still yield a usable, non-degenerate sequence.
- **FixedStep non-positive interval**: a zero or negative interval must not divide-by-zero or loop unboundedly.
- **FixedStep negative or zero delta**: yields zero steps and updates the accumulator correctly without producing a negative step count.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The framework MUST expose a public geometry helper, operating on the shared `FS.GG.UI.Scene.Rect` type, that reports whether two rectangles intersect.
- **FR-002**: The geometry helper MUST report whether a rectangle contains another rectangle, and whether a rectangle contains a point.
- **FR-003**: The geometry helper MUST return the center point of a rectangle, and MUST construct a rectangle from a center point and a size, such that the two operations round-trip.
- **FR-004**: The geometry helper MUST provide a swept/segment overlap test that detects overlap between a moving rectangle's path and a target rectangle even when the start and end positions do not overlap (fast-projectile tunneling).
- **FR-005**: All geometry operations MUST be pure (no side effects, no shared mutable state) and MUST apply a single, documented boundary convention (inclusive vs exclusive edges) consistently across intersection and containment.
- **FR-006**: The framework MUST expose a public seeded pseudo-random generator whose state is a value (not a reference to shared mutable state), safe to store inside an immutable model.
- **FR-007**: The generator MUST support creation from a seed, drawing a bounded integer, and drawing a float in a documented range; each draw MUST return the drawn value together with the next generator state, leaving the input state unchanged.
- **FR-008**: Identical seed plus identical sequence of draw operations MUST always produce an identical sequence of values (deterministic replay), and copying a generator state MUST produce an independent generator that yields the identical continuation (clone determinism).
- **FR-009**: The framework MUST expose a pure fixed-timestep drain helper that, given a fixed interval, an elapsed delta, and a carried accumulator, returns the whole number of fixed steps to run and the new accumulator remainder.
- **FR-010**: The fixed-timestep helper MUST clamp an excessively large elapsed delta to a documented maximum so the step count cannot grow unbounded from a single stalled frame.
- **FR-011**: All three helpers MUST be additive public surface on FS.GG.UI — no existing public type, signature, or behavior may change — and MUST contain no per-game logic (no entities, scores, waves, or game rules).
- **FR-012**: The new public surface MUST be reflected in the repository's public-API stubs (so the honest-public-API stub gate stays green) and in the shipped product documentation (`docs/product.md` / the relevant product skill), replacing the "promised but internal" collision guidance with the now-real surface.
- **FR-013**: Each helper MUST be usable by a consumer without pulling in rendering, viewer, or layout machinery (they are simulation primitives, consumable standalone).

### Key Entities *(include if feature involves data)*

- **Rect (existing, `FS.GG.UI.Scene.Rect`)**: the shared axis-aligned bounds type `{ X; Y; Width; Height }`; the geometry helper operates on this type and MUST NOT introduce a look-alike.
- **Point (existing, `FS.GG.UI.Scene.Point`)**: `{ X; Y }`; used for center queries, center-anchored construction, and point-containment.
- **Rng state (new)**: an immutable value carrying the generator's internal state; produced by seeding and threaded through draws, each of which yields `(value, nextState)`.
- **Fixed-step result (new)**: the pair `(stepCount, remainingAccumulator)` returned by the drain, where `stepCount` is a whole number of fixed steps and `remainingAccumulator` is the carried leftover time.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A game/sim consumer can perform hit-detection, containment, center placement, and swept-overlap using only shipped framework helpers, with zero hand-written or copied AABB code.
- **SC-002**: A consumer can achieve identical-seed / identical-input reproducibility of random-driven outcomes with the RNG state held inside an immutable model, with no mutable `System.Random` embedded anywhere in the model.
- **SC-003**: A consumer can advance a simulation on a fixed timestep using one shipped helper, with no hand-rolled accumulator `while` loop, and a stalled frame produces a bounded number of catch-up steps.
- **SC-004**: Building a comparable next game reuses these three primitives instead of re-implementing them, measurably reducing the simulation-plumbing a consumer must author (the epic's target: a comparable build cut toward ~⅓, savings concentrated pre-implementation).
- **SC-005**: The change is fully additive — every existing consumer of FS.GG.UI continues to build and pass unchanged, and the public-API stub and product-doc currency gates remain green.

## Assumptions

- The three helpers live in FS.GG.UI (the rendering library) as new public modules/functions; the exact module names (`Geometry`, `Rng`, `FixedStep`) and precise signatures are refined in planning, but they attach to the existing `FS.GG.UI.Scene.Rect`/`Point` types rather than introducing new geometry vocabulary.
- "Value-type / pure" means the RNG state is an immutable value threaded functionally; a `struct` representation is an implementation choice for planning, not a spec requirement — the spec requires only that no shared mutable state is exposed.
- The fixed-timestep drain is a stateless computation over `(interval, dt, accumulator)`; interpolation/`alpha` blending is out of scope for this feature (a consumer can compute it from the returned remainder if desired).
- The boundary convention (edge inclusivity) and the RNG's range inclusivity are chosen once in planning to match the framework's existing internal `intersects`/`contains` conventions where reasonable, and documented.
- No per-game logic, no new dependencies, and no change to the viewer/layout/input surfaces are in scope.
- "Reflected in product docs" targets the shipped `template/base/docs/product.md` guidance and the relevant consumer product skill; authoring the separate `fs-gg-game-core` consumer skill is tracked independently (FS-GG/FS.GG.Rendering#73) and is out of scope here.
