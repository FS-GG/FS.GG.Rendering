# Feature Specification: 2D Visibility Skill + Import-and-Adapt Helper Source

**Feature Branch**: `247-2d-visibility-skill`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "add a skill for 2d visibility using https://www.redblobgames.com/articles/visibility/ as reference. add supporting source code, same as with collision detection"

## Context (why this feature, in plain terms)

A developer who scaffolds a **game** (or **sample-pack**) product from the FS.GG.UI template already
gets the low-level geometry pieces they need to *describe* a 2D world — the shared `Point`/`Rect`
vector types and axis-aligned box helpers (`Geometry` in `FS.GG.UI.Scene`), plus broad-phase bucketing
of positioned items (`SpatialGrid` in `FS.GG.UI.Canvas`). What they do **not** get is the game-shaped
layer that answers *"what can be seen from here?"* — the **2D visibility** computation that turns a
viewpoint plus a set of wall segments into the region visible from that point (the *visibility
polygon*), the workhorse behind line-of-sight, field-of-view, fog-of-war, and 2D light/shadow effects.

This is the classic angular-sweep visibility algorithm described in the Red Blob Games reference
(<https://www.redblobgames.com/articles/visibility/>): collect the wall-segment endpoints, sort them by
angle around the source, sweep a ray through them tracking the nearest segment it currently crosses, and
emit the polygon of nearest hit points. It is the natural sibling of collision detection — both are
per-frame geometry passes over a set of world bodies — and it is exactly the kind of opinionated,
per-game code a consumer needs to **edit** (change the light radius, add soft edges, output a mask vs a
polygon), not merely call from a frozen package.

Today that guidance ships **nowhere**: there is no visibility skill, and no visibility source. Developers
who want line-of-sight or 2D lighting hand-roll the whole angular sweep, including the one part the
framework deliberately does not freeze into a package — **ray-segment intersection and the angular
sweep itself**.

This feature delivers two things, mirroring the collision-detection feature (246):

1. A dedicated **2D visibility skill** (`fs-gg-visibility`) that a coding agent loads when the task is
   visibility / line-of-sight / field-of-view / 2D lighting — covering the segment world model,
   broad-phase culling of nearby walls, the angular-sweep algorithm, the visibility-polygon output, and
   the applications — instead of hand-deriving the algorithm from scratch.
2. A **helper source support fragment** the scaffold materializes as **product-owned, adaptable
   source** — visibility code the consumer imports into their product and edits freely (like the
   replaceable starter scene), covering the segment world model, broad-phase culling (reusing
   `SpatialGrid`), ray-segment intersection, and the angular sweep that builds the visibility polygon.
   This uses the **same third delivery mode** collision introduced — product-owned adaptable source
   alongside package-referenced APIs and the single-instance scaffold starter.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - I scaffold a game and get visibility source I own and can adapt (Priority: P1)

A developer scaffolds a **game** product and finds a small, readable 2D-visibility helper file already
in their product source tree (not behind a package boundary). Given a viewpoint and a set of wall
segments it computes the visibility polygon — the region visible from that point — reusing the existing
shared `Point` type and broad-phase `SpatialGrid` to cull far-away walls. The developer edits it
directly — change the sight radius, add a field-of-view cone, switch the output from a polygon to a
per-cell visible/hidden mask, delete what they do not need — the same way they edit their `Model`/`View`.
Nothing forces them to keep the framework's default behavior.

**Why this priority**: This is the concrete artifact the user asked for ("add supporting source code,
same as with collision detection"). Without it the feature delivers only prose. It is independently
valuable: a developer can build line-of-sight / 2D lighting into a game immediately.

**Independent Test**: Scaffold a game product; confirm the visibility helper source is present and
compiles as part of the product; feed it a viewpoint and a few wall segments and observe a visibility
polygon that correctly excludes areas behind walls; edit the sight radius and observe the visible region
change; delete the file entirely and confirm the product still builds and no governance/test gate fails
because of its absence.

**Acceptance Scenarios**:

1. **Given** a freshly scaffolded game product, **When** the developer inspects the product source
   tree, **Then** an adaptable 2D-visibility helper source file is present, is owned by the product (not
   a package reference), and compiles with the rest of the product.
2. **Given** a viewpoint and a set of wall segments with an occluder between the viewpoint and a target
   region, **When** the helper's visibility pass runs, **Then** it produces a **visibility polygon**
   (an ordered boundary of nearest-hit points) that includes the unobstructed area and excludes the
   region hidden behind the occluder — not merely a yes/no answer.
3. **Given** the developer edits a visibility parameter (e.g. the sight radius or a field-of-view angle),
   **When** they rebuild and re-run, **Then** the computed visible region changes accordingly with no
   framework edit required.
4. **Given** the developer deletes the visibility helper source, **When** they build and run the product
   gates, **Then** the product still builds and no gate hard-fails solely because the helper was
   removed.

---

### User Story 2 - A coding agent loads a dedicated 2D-visibility skill (Priority: P1)

When the task is "add line-of-sight / a light source / fog-of-war to my game," a coding agent working in
a scaffolded game/sample-pack product loads a single, focused **2D visibility** skill. It explains the
whole pipeline — the segment world model (reusing the shared `Point`), broad-phase culling of nearby
walls (reusing `SpatialGrid`), the angular-sweep algorithm (endpoint collection, sort by angle,
sweep tracking the nearest crossing segment), and the visibility-polygon output — points at the
adaptable helper source as the starting point, cites the Red Blob Games reference for the algorithm, and
lists the visibility-specific footguns (determinism of the angle sort under ties, collinear/near-parallel
ray edge cases, reusing the shared `Point` instead of a look-alike vector type, O(n²) when walls are not
culled).

**Why this priority**: The user explicitly asked for a "skill for 2d visibility." The skill is how the
capability becomes *discoverable and correctly applied* by an agent; it is independently testable and
valuable even before the source fragment is polished.

**Independent Test**: In a scaffolded game product, confirm the visibility skill materializes for the
game/sample-pack profiles, is absent for profiles that exclude it, and its guidance references the
existing primitives (`Point`/`SpatialGrid`), the Red Blob Games reference, and the adaptable helper
source rather than duplicating unrelated content.

**Acceptance Scenarios**:

1. **Given** a game or sample-pack product, **When** the skill catalog is materialized, **Then** the
   `fs-gg-visibility` skill is present and resolvable.
2. **Given** a profile that does not include visibility (e.g. a non-game app or headless-scene profile),
   **When** the skill catalog is materialized, **Then** the visibility skill is **not** materialized.
3. **Given** the visibility skill body, **When** it is read, **Then** it covers the segment world model,
   broad-phase culling, the angular sweep, and the polygon output; names the existing `Point`/`SpatialGrid`
   surfaces it reuses; cites the Red Blob Games visibility reference; and points at the adaptable helper
   source as the entry point.

---

### User Story 3 - The skill catalog and swap guidance stay coherent (Priority: P2)

The new visibility skill and its helper fragment take their place cleanly alongside the sibling skills
(collision, game-core, audio, persistence): the capability catalog, skill manifest, template scaffold
sources/conditions, skill reference doc, and dev-skill roots all agree, and the scaffold's swap/adapt
guidance lists the visibility helper as consumer-owned replaceable source. An agent or developer reading
the catalog finds visibility as one authoritative, discoverable capability with no drift.

**Why this priority**: Prevents registry drift and an orphaned or undiscoverable capability. Valuable but
dependent on US2 existing first; a correct-but-not-yet-cross-referenced interim state is tolerable, so
this is P2.

**Independent Test**: Run the repo's skill/capability coherence gates after the skill is added; confirm
they pass with zero drift and that the visibility helper appears in the scaffold's swap/adapt file
taxonomy as consumer-owned replaceable source.

**Acceptance Scenarios**:

1. **Given** the added visibility skill, **When** the coherence gates run, **Then** the capability
   catalog, skill manifest, template source/condition, skill reference doc, and dev-skill roots all
   agree (0 drift failures).
2. **Given** the scaffold's swap/adapt guidance, **When** it is read, **Then** the visibility helper
   source is listed as consumer-owned replaceable source, consistent with the collision helper and the
   starter-scene precedent.

---

### Edge Cases

- **Profile gating**: The skill and the helper source must materialize only for the profiles that
  include visibility (game / sample-pack) and be absent otherwise — no orphaned visibility source in an
  app or headless-scene product.
- **Replaceability without governance pin**: Following the feature 220 starter-scene lesson (and the 246
  collision helper), no generated governance/acceptance test may *hard-assert the presence or exact
  content* of the helper source — otherwise a developer who adapts or deletes it fails a gate for doing
  the documented thing.
- **Determinism of the sweep**: The angular sweep sorts endpoints by angle around the source. Endpoints
  at an identical angle (shared corners, collinear walls) must resolve by a deterministic tiebreak so the
  emitted polygon is a pure function of world state — identical inputs yield an identical polygon across
  runs and platforms, with no reliance on hash-container iteration order or frame-arrival order.
- **Degenerate geometry**: Zero-length segments, a source lying exactly on a wall or on an endpoint,
  collinear/near-parallel rays grazing a segment, duplicate/coincident endpoints, and an empty segment
  set must produce documented, total results (e.g. an empty or full-circle polygon) — never a throw or a
  NaN-poisoned coordinate.
- **Vector vocabulary clash**: The helper must reuse the shared `Point` and existing primitives rather
  than introducing a look-alike point/vector type that collides with the consumer's own records (the
  documented consumer-vs-framework and consumer-vs-consumer `.Pos` footguns).
- **Adapt-and-own drift**: Because the source is copied into the product, later framework improvements do
  not reach an already-scaffolded product automatically. The consumer owns the copy; this trade-off must
  be stated so it is a deliberate choice, not a surprise.
- **Catalog coherence**: Adding a skill touches multiple registries (capability catalog, skill manifest,
  template sources/conditions, skill reference doc, dev-skill roots and their mirror). All must agree or
  the coherence gates fail.
- **Unbounded vs bounded visibility**: The algorithm needs a bound (a bounding box or sight radius) so
  rays that hit no wall still terminate. The helper must document and apply a bound (reusing the sight
  radius for the broad-phase cull) rather than looping or returning an infinite ray.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a new dedicated 2D-visibility skill (`fs-gg-visibility`) that
  guides the full pipeline — segment world model, broad-phase culling, the angular sweep, and the
  visibility-polygon output — for a generated game/sample-pack product, citing the Red Blob Games
  visibility reference for the algorithm.
- **FR-002**: The visibility skill MUST reuse and reference the existing shared primitives — the
  `Point` vector type and box helpers (`FS.GG.UI.Scene`) and broad-phase bucketing (`SpatialGrid`,
  `FS.GG.UI.Canvas`, including `queryRadius` for radius culling) — rather than introducing duplicate or
  look-alike geometry vocabulary.
- **FR-003**: The system MUST materialize the visibility skill for the profiles that include it (game and
  sample-pack) and MUST NOT materialize it for profiles that exclude it.
- **FR-004**: The system MUST provide a **helper source support fragment** that the scaffold materializes
  as **product-owned, adaptable source** (compiled as part of the product, not consumed as a package
  reference) into a game/sample-pack product.
- **FR-005**: The helper source MUST cover the full visibility pass: a segment (wall) world model over
  the shared `Point`, broad-phase culling of nearby segments (over `SpatialGrid`), **ray-segment
  intersection**, and the **angular sweep** that emits the visibility polygon.
- **FR-006**: The helper source MUST produce a **visibility polygon** (an ordered boundary of the visible
  region) as its result — the region visible from the source — not merely a point-to-point line-of-sight
  boolean (though a point-visible query MAY be offered as a convenience built on the same intersection
  core).
- **FR-007**: The consumer MUST be able to freely edit, extend, or delete the helper source; the product
  MUST still build and MUST NOT fail any generated governance/acceptance gate **solely** because the
  helper source was changed or removed (the feature 220 / 246 non-hard-pin rule).
- **FR-008**: The visibility computation MUST be deterministic — a pure function of world state (source,
  segments, bound), with a stable tiebreak for equal-angle endpoints and no dependence on hash-container
  iteration order or frame-arrival order — so identical inputs yield an identical visibility polygon
  across runs and platforms.
- **FR-009**: The helper source and skill MUST reuse the shared `Point`/`Rect` types and MUST NOT
  introduce a competing point/vector/segment type that shadows the shared vocabulary; guidance MUST call
  out the consumer-vs-framework and consumer-vs-consumer geometry-naming footguns.
- **FR-010**: All visibility entry points the consumer calls MUST be **total** — degenerate inputs
  (zero-length segment, source on a wall or endpoint, collinear/near-parallel grazing ray, coincident
  endpoints, empty segment set) return a documented value rather than throwing or emitting a NaN
  coordinate.
- **FR-011**: The visibility computation MUST be **bounded** — rays that strike no wall terminate at a
  documented bound (a bounding rectangle or sight radius, reusing the same radius as the broad-phase
  cull) so the polygon is always finite and closed.
- **FR-012**: Adding the visibility skill MUST keep every skill/capability registry coherent — the
  capability catalog, the skill manifest (id + digest + materialize condition + source), the template
  scaffold source/condition, the skill reference doc, and the dev-skill roots plus their materialized
  mirror — so the repo's coherence gates pass.
- **FR-013**: The visibility helper fragment MUST be classified in the scaffold's file taxonomy as
  **replaceable/adaptable** (consumer-owned), consistent with the collision helper and model-swap
  classification, and MUST be reachable from the scaffold's swap/adapt guidance so a consumer knows it is
  theirs to change.
- **FR-014**: If the set of files the template emits is a versioned cross-repo contract, shipping this
  feature MUST update the cross-repo dependency/compatibility registry accordingly on release
  (publish-before-flip), consistent with how the prior additive template/skill features (243/244/246)
  were released.

### Key Entities *(include if feature involves data)*

- **Visibility skill (`fs-gg-visibility`)**: The authored guidance capability — scope, the reused
  primitive surfaces, the segment→cull→angular-sweep→polygon pipeline, the Red Blob Games reference,
  footguns, and a pointer to the adaptable helper source. Registered in the capability catalog and skill
  manifest; materializes for game/sample-pack.
- **Visibility helper source fragment**: Product-owned, adaptable source materialized into a
  game/sample-pack product. Reuses `Point`/`SpatialGrid`; adds ray-segment intersection and the angular
  sweep. Consumer-owned, replaceable, no framework package backing, not hard-pinned by any gate.
- **Wall segment (occluder)**: A line segment between two shared `Point`s that blocks sight. The input
  world model the sweep consumes; culled by the broad-phase before the sweep.
- **Visibility polygon**: What the helper produces per invocation — an ordered boundary of nearest-hit
  points describing the region visible from the source, bounded by the sight radius / bounding box — as
  opposed to a bare line-of-sight boolean.
- **Existing primitives (reused, not re-created)**: the shared `Point`/`Rect` and `Geometry`
  (`FS.GG.UI.Scene`) for the vector/box vocabulary; `SpatialGrid` (`FS.GG.UI.Canvas`, incl. `queryRadius`)
  for broad-phase culling of nearby segments.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer who scaffolds a game product can produce a visibly correct visibility result
  (a light/sight region that is blocked by walls) by editing **only the provided helper source** — with
  **no** added package reference and **no** framework edit — in under 15 minutes.
- **SC-002**: 2D-visibility guidance is discoverable as **exactly one** dedicated skill that cites the
  Red Blob Games reference and reuses the existing `Point`/`SpatialGrid` surfaces (no re-derived
  algorithm buried in an unrelated skill).
- **SC-003**: The helper source can be fully edited or deleted by the consumer with **zero** governance
  or acceptance gates failing on that basis alone.
- **SC-004**: The visibility polygon is replay-deterministic — across repeated runs on the same inputs,
  the emitted polygon is byte-identical (0 divergences), including for inputs with equal-angle endpoints.
- **SC-005**: The shipped helper source and skill introduce **zero** re-rolled point / vector / bounds
  types — 100% of the vector/box vocabulary reuses the existing `Point`/`Rect`/`Geometry`/`SpatialGrid`
  surfaces.
- **SC-006**: The visibility skill materializes for **100%** of game/sample-pack scaffolds and **0%** of
  profiles that exclude it.
- **SC-007**: After the skill is added, **all** repo skill/capability coherence gates pass (0 registry
  drift failures).
- **SC-008**: All documented degenerate inputs (empty segment set, source on a wall/endpoint,
  zero-length and collinear segments) return a total, documented result with **zero** throws or NaN
  coordinates.

## Assumptions

- The two deliverables are (a) a new product skill and (b) a scaffold-materialized, product-owned source
  fragment — the same shape confirmed for collision (246). The helper is *not* a new frozen package API;
  the shared `Point`/`Rect`/`Geometry`/`SpatialGrid` continue to ship as the existing package surfaces,
  which the fragment reuses.
- Visibility scope is the **angular-sweep visibility polygon** from the Red Blob Games reference —
  segment world model, broad-phase cull, ray-segment intersection, and the sweep. A point-to-point
  line-of-sight query built on the same intersection core is in scope as a convenience. Soft
  shadows/penumbra, full dynamic lightmaps/rendering, and 3D visibility are **out of scope** for this
  feature; the helper emits the polygon and leaves rendering/lighting to the consumer.
- The framework does not today ship ray-segment intersection or angle math (`Geometry` is AABB-only);
  that math is exactly the part the helper adds and the framework deliberately does not freeze — the
  direct analogue of collision *response* in feature 246.
- The new skill is standalone (`fs-gg-visibility`) and materializes for the same profiles collision does
  (game and sample-pack). Unlike collision (which trimmed a duplicate section out of `fs-gg-game-core`),
  there is no existing visibility write-up to consolidate, so no sibling-skill trim is required.
- The helper fragment follows the established "consumer-owned, replaceable, not governance-pinned"
  precedent set by the game starter scene (220), the collision helper (246), and the model-swap file
  taxonomy.
- Determinism of the sweep is a first-class requirement because the visibility computation is expected to
  be usable inside the deterministic fixed-step simulation loop this product tier already ships.
- Release may require a cross-repo template-contract update if the emitted file set is a versioned
  contract; the classification (Tier 1 contract-change vs local) is confirmed during `/speckit-plan`.

## Dependencies

- Existing shared geometry primitives: `Point`/`Rect`/`Geometry` (`FS.GG.UI.Scene`, feature 239) and
  `SpatialGrid` incl. `queryRadius` (`FS.GG.UI.Canvas`, feature 245) — reused, not modified.
- The collision-detection feature (246) as the pattern precedent for a skill + import-and-adapt helper
  source pair.
- The skill materialization pipeline and registries: capability catalog, skill manifest + generator,
  template scaffold sources/conditions, skill reference doc, and the dev-skill-root materialize/parity
  tooling (features 224 / 231 / 238 / 230).
- The scaffold file taxonomy and swap/adapt guidance (model-swap skill; features 220 / 242) that governs
  consumer-owned replaceable source.
- On release: the cross-repo dependency/compatibility registry in `FS-GG/.github` if the template's
  emitted-file contract version changes.
- External reference (design source only, not a code dependency): Red Blob Games, "2D Visibility"
  (<https://www.redblobgames.com/articles/visibility/>).
