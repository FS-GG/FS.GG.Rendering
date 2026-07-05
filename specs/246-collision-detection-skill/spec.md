# Feature Specification: Collision Detection Skill + Import-and-Adapt Helper Source

**Feature Branch**: `246-collision-detection-skill`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "add collision detection skill and helper source code support project to import and adapt"

## Context (why this feature, in plain terms)

A developer who scaffolds a **game** (or **sample-pack**) product from the FS.GG.UI template already
gets the low-level collision *detection* pieces as frozen, package-referenced API — box-overlap and
swept tests (`Geometry` in `FS.GG.UI.Scene`) and broad-phase bucketing (`SpatialGrid` in
`FS.GG.UI.Canvas`). What they do **not** get is the game-shaped layer that turns those primitives into
a working collision pass: pairing candidates, filtering, and — the part every game re-invents —
**collision response** (how far and which way overlapping bodies separate, and how they slide or bounce).

Today that guidance is a single `## Collision` section buried inside the broader `fs-gg-game-core`
skill, and the response layer ships nowhere at all. Developers hand-roll it (the in-repo Snake sample
hand-rolls self-collision) because a *frozen* package API is the wrong home for opinionated,
per-game response rules that consumers need to **edit**, not merely call.

This feature delivers two things:

1. A dedicated **collision skill** (`fs-gg-collision`) that a coding agent loads when the task is
   collision — detection, broad-phase, and response — instead of scrolling past unrelated fixed-step
   and RNG material in `fs-gg-game-core`.
2. A **helper source support fragment** the scaffold materializes as **product-owned, adaptable
   source** — collision code the consumer imports into their product and edits freely (like the
   replaceable starter scene), covering broad-phase orchestration, narrow-phase detection (reusing the
   existing primitives), and collision response. This introduces a *new, third* delivery mode alongside
   package-referenced APIs and the single-instance scaffold starter.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - I scaffold a game and get collision source I own and can adapt (Priority: P1)

A developer scaffolds a **game** product and finds a small, readable collision helper file already in
their product source tree (not behind a package boundary). It composes the existing detection and
broad-phase primitives into a per-frame collision pass and resolves overlaps (separate + slide/bounce).
The developer edits it directly — change the response rule, add collision layers, delete what they do
not need — the same way they edit their `Model`/`View`. Nothing forces them to keep the framework's
default behavior.

**Why this priority**: This is the concrete artifact the user asked for ("helper source code support
project to import and adapt"). Without it the feature delivers only prose. It is independently
valuable: a developer can build a game with working collision response immediately.

**Independent Test**: Scaffold a game product; confirm the collision helper source is present and
compiles as part of the product; edit the response rule and observe overlapping bodies separate
differently; delete the file entirely and confirm the product still builds and no governance/test gate
fails because of its absence.

**Acceptance Scenarios**:

1. **Given** a freshly scaffolded game product, **When** the developer inspects the product source
   tree, **Then** an adaptable collision helper source file is present, is owned by the product (not a
   package reference), and compiles with the rest of the product.
2. **Given** two overlapping bodies passed through the helper's collision pass, **When** the pass runs,
   **Then** it reports the overlap **and** produces a resolution (a separation vector / minimum
   translation and post-response state) rather than only a boolean.
3. **Given** the developer edits the response rule (e.g. bounce vs slide, or a restitution value),
   **When** they rebuild and re-run, **Then** the observed separation behavior changes accordingly with
   no framework edit required.
4. **Given** the developer deletes the collision helper source, **When** they build and run the product
   gates, **Then** the product still builds and no gate hard-fails solely because the helper was
   removed.

---

### User Story 2 - A coding agent loads a dedicated collision skill (Priority: P1)

When the task is "add collision to my game," a coding agent working in a scaffolded game/sample-pack
product loads a single, focused **collision** skill. It explains the whole pipeline — narrow-phase
detection (reusing `Geometry`), broad-phase candidate pairing (reusing `SpatialGrid`), and response
(penetration/minimum-translation, separation, slide/bounce, restitution) — points at the adaptable
helper source as the starting point, and lists the collision-specific footguns (determinism of the
response, consumer-vs-framework geometry clashes, O(n²) pair scans).

**Why this priority**: The user explicitly asked for a "collision detection skill." The skill is how
the capability becomes *discoverable and correctly applied* by an agent; it is independently testable
and valuable even before the source fragment is polished.

**Independent Test**: In a scaffolded game product, confirm the collision skill materializes for the
game/sample-pack profiles, is absent for profiles that exclude collision, and its guidance references
the existing primitives and the adaptable helper source rather than duplicating unrelated fixed-step/RNG
content.

**Acceptance Scenarios**:

1. **Given** a game or sample-pack product, **When** the skill catalog is materialized, **Then** the
   `fs-gg-collision` skill is present and resolvable.
2. **Given** a profile that does not include collision (e.g. a non-game app or headless-scene profile),
   **When** the skill catalog is materialized, **Then** the collision skill is **not** materialized.
3. **Given** the collision skill body, **When** it is read, **Then** it covers detection, broad-phase,
   and response, names the existing `Geometry`/`SpatialGrid` surfaces it reuses, and points at the
   adaptable helper source as the entry point.

---

### User Story 3 - One source of truth: game-core points at the collision skill (Priority: P2)

The existing `fs-gg-game-core` skill no longer carries a duplicated collision write-up. Its `Collision`
material is trimmed to a short pointer at the new collision skill, so an agent (or developer) reading
either skill is sent to exactly one authoritative place for collision guidance, and the two skills
cannot drift apart.

**Why this priority**: Prevents two competing collision write-ups. Valuable but dependent on US2
existing first; a duplicated-but-correct interim state is tolerable, so this is P2.

**Independent Test**: Read `fs-gg-game-core`; confirm its collision content is a pointer to
`fs-gg-collision` and that the detailed detection/broad-phase/response guidance now lives only in the
new skill.

**Acceptance Scenarios**:

1. **Given** the updated `fs-gg-game-core` skill, **When** its collision section is read, **Then** it
   references `fs-gg-collision` for the detailed guidance rather than repeating it.
2. **Given** both skills, **When** their collision guidance is compared, **Then** the authoritative
   detection/broad-phase/response content appears in exactly one skill.

---

### Edge Cases

- **Profile gating**: The skill and the helper source must materialize only for the profiles that
  include collision (game / sample-pack) and be absent otherwise — no orphaned collision source in an
  app or headless-scene product.
- **Replaceability without governance pin**: Following the feature 220 starter-scene lesson, no
  generated governance/acceptance test may *hard-assert the presence or exact content* of the helper
  source — otherwise a developer who adapts or deletes it fails a gate for doing the documented thing.
- **Determinism of response**: Collision response involves separation math that can use fractional
  values. Response run inside a simulated/replayed step must be a pure function of world state so
  identical inputs yield identical resolved positions across runs and platforms — no reliance on
  iteration order of a hash-based container or on frame-arrival order.
- **Geometry vocabulary clash**: The helper must reuse the shared `Rect`/`Point` and existing
  primitives rather than introducing a look-alike bounds/vector type that collides with the consumer's
  own records (the documented consumer-vs-framework and consumer-vs-consumer `.Pos` footguns).
- **Adapt-and-own drift**: Because the source is copied into the product, later framework improvements
  do not reach an already-scaffolded product automatically. The consumer owns the copy; this trade-off
  must be stated so it is a deliberate choice, not a surprise.
- **Catalog coherence**: Adding a skill touches multiple registries (capability catalog, skill
  manifest, template sources/conditions, skill reference doc, dev-skill roots and their mirror). All
  must agree or the coherence gates fail.
- **Degenerate collision inputs**: Zero-area bodies, exactly touching edges, fully-contained bodies, and
  empty candidate sets must produce documented, total results (never a throw).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a new dedicated collision skill (`fs-gg-collision`) that guides
  collision **detection, broad-phase, and response** for a generated game/sample-pack product.
- **FR-002**: The collision skill MUST reuse and reference the existing collision primitives — box
  overlap/containment/swept tests (`Geometry`, `FS.GG.UI.Scene`) and broad-phase bucketing
  (`SpatialGrid`, `FS.GG.UI.Canvas`) — rather than introducing duplicate or look-alike detection
  vocabulary.
- **FR-003**: The system MUST materialize the collision skill for the profiles that include collision
  (game and sample-pack) and MUST NOT materialize it for profiles that exclude collision.
- **FR-004**: The system MUST provide a **helper source support fragment** that the scaffold
  materializes as **product-owned, adaptable source** (compiled as part of the product, not consumed as
  a package reference) into a game/sample-pack product.
- **FR-005**: The helper source MUST cover the full collision pass: broad-phase candidate pairing
  (over `SpatialGrid`), narrow-phase detection (over `Geometry`), and **collision response** —
  penetration / minimum-translation, separation, and a slide/bounce (restitution) rule.
- **FR-006**: The helper source MUST report overlaps **and** produce a resolution (separation vector +
  post-response state), not merely a boolean detection result.
- **FR-007**: The consumer MUST be able to freely edit, extend, or delete the helper source; the
  product MUST still build and MUST NOT fail any generated governance/acceptance gate **solely** because
  the helper source was changed or removed (the feature 220 non-hard-pin rule).
- **FR-008**: Collision response invoked within a simulated/replayed step MUST be deterministic — a pure
  function of world state, with no dependence on hash-container iteration order or frame-arrival order —
  so identical inputs yield identical resolved output across runs and platforms.
- **FR-009**: The helper source and skill MUST reuse the shared `Rect`/`Point` types and MUST NOT
  introduce a competing bounds/vector type; guidance MUST call out the consumer-vs-framework and
  consumer-vs-consumer geometry-naming footguns.
- **FR-010**: All collision detection/response entry points the consumer calls MUST be **total** —
  degenerate inputs (zero-area body, exactly-touching edges, fully-contained body, empty candidate set)
  return a documented value rather than throwing.
- **FR-011**: The existing `fs-gg-game-core` skill's collision content MUST be reduced to a pointer at
  `fs-gg-collision`, leaving the authoritative detection/broad-phase/response guidance in exactly one
  place.
- **FR-012**: Adding the collision skill MUST keep every skill/capability registry coherent — the
  capability catalog, the skill manifest (id + digest + materialize condition + source), the template
  scaffold source/condition, the skill reference doc, and the dev-skill roots plus their materialized
  mirror — so the repo's coherence gates pass.
- **FR-013**: The collision helper fragment MUST be classified in the scaffold's file taxonomy as
  **replaceable/adaptable** (consumer-owned), consistent with the model-swap classification, and MUST be
  reachable from the scaffold's swap/adapt guidance so a consumer knows it is theirs to change.
- **FR-014**: If the set of files the template emits is a versioned cross-repo contract, shipping this
  feature MUST update the cross-repo dependency/compatibility registry accordingly on release
  (publish-before-flip), consistent with how prior additive template/skill features were released.

### Key Entities *(include if feature involves data)*

- **Collision skill (`fs-gg-collision`)**: The authored guidance capability — scope, the reused
  primitive surfaces, the detection→broad-phase→response pipeline, footguns, and a pointer to the
  adaptable helper source. Registered in the capability catalog and skill manifest; materializes for
  game/sample-pack.
- **Collision helper source fragment**: Product-owned, adaptable source materialized into a
  game/sample-pack product. Reuses `Geometry`/`SpatialGrid`; adds the response layer. Consumer-owned,
  replaceable, no framework package backing, not hard-pinned by any gate.
- **Collision pass result**: What the helper produces per invocation — the set of colliding pairs and,
  per collision, a resolution (separation/minimum-translation vector and post-response state) — as
  opposed to a bare boolean.
- **Existing detection primitives (reused, not re-created)**: `Geometry` (`FS.GG.UI.Scene`) for
  narrow-phase overlap/swept/containment; `SpatialGrid` (`FS.GG.UI.Canvas`) for broad-phase bucketing.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer who scaffolds a game product can produce visibly working collision response
  (overlapping bodies separate and slide/bounce) by editing **only the provided helper source** — with
  **no** added package reference and **no** framework edit — in under 15 minutes.
- **SC-002**: Collision guidance is discoverable as **exactly one** dedicated skill, and
  `fs-gg-game-core` contains **zero** duplicated collision write-ups (only a pointer).
- **SC-003**: The helper source can be fully edited or deleted by the consumer with **zero** governance
  or acceptance gates failing on that basis alone.
- **SC-004**: Collision response is replay-deterministic — across repeated runs on the same inputs,
  resolved positions are byte-identical (0 divergences).
- **SC-005**: The shipped helper source and skill introduce **zero** re-rolled AABB / bounds / vector
  types — 100% of detection and broad-phase reuse the existing `Geometry`/`SpatialGrid` surfaces.
- **SC-006**: The collision skill materializes for **100%** of game/sample-pack scaffolds and **0%** of
  profiles that exclude collision.
- **SC-007**: After the skill is added, **all** repo skill/capability coherence gates pass (0 registry
  drift failures).

## Assumptions

- The two deliverables are (a) a new product skill and (b) a scaffold-materialized, product-owned source
  fragment — confirmed with the requester. The helper is *not* a new frozen package API; detection and
  broad-phase continue to ship as the existing `Geometry`/`SpatialGrid` package surfaces, which the
  fragment reuses.
- Collision scope is **detection + broad-phase + response** (penetration/minimum-translation,
  separation, slide/bounce/restitution) — confirmed. A full integrated physics step (velocity/position
  integration, friction) is **out of scope** for this feature; the helper resolves overlaps and leaves
  motion integration to the consumer.
- The new skill is standalone (`fs-gg-collision`), and `fs-gg-game-core` is trimmed to point at it —
  confirmed. The collision skill materializes for the same profiles game-core does today (game and
  sample-pack).
- The helper fragment follows the established "consumer-owned, replaceable, not governance-pinned"
  precedent set by the game starter scene (feature 220) and the model-swap file taxonomy.
- Determinism of the response layer is a first-class requirement because collision resolution is
  expected to run inside the deterministic fixed-step simulation loop this product tier already ships.
- Release may require a cross-repo template-contract update if the emitted file set is a versioned
  contract; the classification (Tier 1 contract-change vs local) is confirmed during `/speckit-plan`.

## Dependencies

- Existing collision-detection primitives: `Geometry` (`FS.GG.UI.Scene`, feature 239) and `SpatialGrid`
  (`FS.GG.UI.Canvas`, feature 245) — reused, not modified.
- The skill materialization pipeline and registries: capability catalog, skill manifest + generator,
  template scaffold sources/conditions, skill reference doc, and the dev-skill-root materialize/parity
  tooling (features 224 / 231 / 238 / 230).
- The scaffold file taxonomy and swap/adapt guidance (model-swap skill; features 220 / 242) that governs
  consumer-owned replaceable source.
- On release: the cross-repo dependency/compatibility registry in `FS-GG/.github` if the template's
  emitted-file contract version changes.
