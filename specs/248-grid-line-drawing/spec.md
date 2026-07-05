# Feature Specification: Grid Line-Drawing Skill + Import-and-Adapt Helper Source

**Feature Branch**: `248-grid-line-drawing`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "add a grid line-drawing skill using
<https://www.redblobgames.com/grids/line-drawing/> as reference; add supporting product-owned adaptable
source, same shape as collision detection (246) and 2D visibility (247)."

## Context (why this feature, in plain terms)

A developer who scaffolds a **game** (or **sample-pack**) product from the FS.GG.UI template already
gets the grid-simulation atoms they need to *reason about a tile world* — the integer `Cell` grid
coordinate, deterministic grid **pathfinding** (A*/BFS over a `Cell -> bool` walkability predicate), and
a uniform **spatial grid** for range/splash queries (all in `FS.GG.UI.Canvas`, feature 245). What they
do **not** get is the discrete-grid analogue of a ray: given two tiles, **which cells does the straight
line between them pass through?** — the workhorse behind tile-based **line-of-sight**, beam/ray attacks,
drawing walls/roads/rivers between two tiles, and moving an agent along a straight path.

This is the classic grid line-drawing problem described in the Red Blob Games reference
(<https://www.redblobgames.com/grids/line-drawing/>): walk the cells between two grid coordinates with
linear interpolation / **Bresenham's algorithm** for the thin (diagonal-connected) cell line, plus the
**supercover** variant that visits *every* cell the line touches (no diagonal gap). It is the **discrete
sibling** of collision detection (246) and 2D visibility (247) — both are per-frame geometry passes over
a *continuous* world of float `Point`s; line-drawing is the same shape over the *discrete* `Cell` grid.
And it is exactly the kind of opinionated, per-game code a consumer needs to **edit** (4- vs 8-connected
line, thin vs supercover, stop-at-first-blocked for line-of-sight), not merely call from a frozen
package.

Today that guidance ships **nowhere**: there is no line-drawing skill, and no line-drawing source.
Developers who want tile line-of-sight or beam attacks hand-roll Bresenham — including the part where a
naive float-lerp implementation drifts under rounding and breaks same-seed replay, which is exactly the
subtle bit consumers get wrong.

This feature delivers two things, mirroring the collision (246) and visibility (247) features:

1. A dedicated **grid line-drawing skill** (`fs-gg-line-drawing`) that a coding agent loads when the task
   is drawing / walking a line across tiles — tile line-of-sight, beams, tile paths — covering the `Cell`
   grid model, the Bresenham cell line, the supercover variant, and the grid line-of-sight query, instead
   of hand-deriving the algorithm from scratch.
2. A **helper source support fragment** the scaffold materializes as **product-owned, adaptable source**
   — line-drawing code the consumer imports into their product and edits freely (like the replaceable
   starter scene), covering the deterministic Bresenham `line`, the `supercover` walk, and a
   `lineOfSight` query over a `Cell -> bool` transparency predicate. This uses the **same third delivery
   mode** collision introduced — product-owned adaptable source alongside package-referenced APIs and the
   single-instance scaffold starter.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - I scaffold a game and get line-drawing source I own and can adapt (Priority: P1)

A developer scaffolds a **game** product and finds a small, readable grid line-drawing helper file
already in their product source tree (not behind a package boundary). Given two grid cells it returns the
ordered list of cells the straight line between them passes through, reusing the existing shared `Cell`
grid type. The developer edits it directly — switch the thin line to the supercover (no-diagonal-gap)
variant, add a stop-at-first-blocked line-of-sight over their own wall map, cap the line length, delete
what they do not need — the same way they edit their `Model`/`View`. Nothing forces them to keep the
framework's default behavior.

**Why this priority**: This is the concrete artifact the user asked for ("add supporting source code,
same as with collision detection"). Without it the feature delivers only prose. It is independently
valuable: a developer can build tile line-of-sight / beam attacks into a game immediately.

**Independent Test**: Scaffold a game product; confirm the line-drawing helper source is present and
compiles as part of the product; feed it two cells and observe the ordered cell line that connects them
(endpoints included, each step adjacent); ask a line-of-sight query across a blocked cell and observe it
report "blocked"; delete the file entirely and confirm the product still builds and no
governance/test gate fails because of its absence.

**Acceptance Scenarios**:

1. **Given** a freshly scaffolded game product, **When** the developer inspects the product source tree,
   **Then** an adaptable grid line-drawing helper source file is present, is owned by the product (not a
   package reference), and compiles with the rest of the product.
2. **Given** two grid cells, **When** the helper's `line` runs, **Then** it produces an **ordered list of
   cells** from start to goal (both endpoints included) in which each consecutive pair is grid-adjacent —
   a *path of tiles*, not merely a distance or a yes/no answer.
3. **Given** a transparency map (a `Cell -> bool` predicate) with a blocking cell between two tiles,
   **When** the helper's line-of-sight query runs, **Then** it reports the target is **not** visible; and
   with the blocker removed it reports the target **is** visible — with no framework edit required.
4. **Given** the developer deletes the line-drawing helper source, **When** they build and run the product
   gates, **Then** the product still builds and no gate hard-fails solely because the helper was removed.

---

### User Story 2 - A coding agent loads a dedicated grid line-drawing skill (Priority: P1)

When the task is "add tile line-of-sight / a beam weapon / draw a wall between two tiles," a coding agent
working in a scaffolded game/sample-pack product loads a single, focused **grid line-drawing** skill. It
explains the whole pipeline — the `Cell` grid model (reusing the shared integer coordinate), the
Bresenham cell line, the supercover (no-diagonal-gap) variant, and the grid line-of-sight query over a
`Cell -> bool` predicate — points at the adaptable helper source as the starting point, cites the Red
Blob Games reference for the algorithm, and lists the line-drawing-specific footguns (float-lerp rounding
drift vs deterministic integer Bresenham, diagonal gaps in a thin line leaking sight through wall
corners, reusing the shared `Cell` instead of a look-alike `(row, col)` record).

**Why this priority**: The user explicitly asked for a "skill for line drawing." The skill is how the
capability becomes *discoverable and correctly applied* by an agent; it is independently testable and
valuable even before the source fragment is polished.

**Independent Test**: In a scaffolded game product, confirm the line-drawing skill materializes for the
game/sample-pack profiles, is absent for profiles that exclude it, and its guidance references the
existing primitives (`Cell`, the `Pathfinding` predicate convention), the Red Blob Games reference, and
the adaptable helper source rather than duplicating unrelated content.

**Acceptance Scenarios**:

1. **Given** a game or sample-pack product, **When** the skill catalog is materialized, **Then** the
   `fs-gg-line-drawing` skill is present and resolvable.
2. **Given** a profile that does not include line-drawing (e.g. a non-game app or headless-scene profile),
   **When** the skill catalog is materialized, **Then** the line-drawing skill is **not** materialized.
3. **Given** the line-drawing skill body, **When** it is read, **Then** it covers the `Cell` grid model,
   the Bresenham cell line, the supercover variant, and the line-of-sight query; names the existing
   `Cell`/`Pathfinding` surfaces it reuses; cites the Red Blob Games line-drawing reference; and points at
   the adaptable helper source as the entry point.

---

### User Story 3 - The skill catalog and swap guidance stay coherent (Priority: P2)

The new line-drawing skill and its helper fragment take their place cleanly alongside the sibling skills
(collision, visibility, game-core, audio, persistence): the capability catalog, skill manifest, template
scaffold sources/conditions, skill reference doc, and dev-skill roots all agree, and the scaffold's
swap/adapt guidance lists the line-drawing helper as consumer-owned replaceable source. An agent or
developer reading the catalog finds line-drawing as one authoritative, discoverable capability with no
drift.

**Why this priority**: Prevents registry drift and an orphaned or undiscoverable capability. Valuable but
dependent on US2 existing first; a correct-but-not-yet-cross-referenced interim state is tolerable, so
this is P2.

**Independent Test**: Run the repo's skill/capability coherence gates after the skill is added; confirm
they pass with zero drift and that the line-drawing helper appears in the scaffold's swap/adapt file
taxonomy as consumer-owned replaceable source.

**Acceptance Scenarios**:

1. **Given** the added line-drawing skill, **When** the coherence gates run, **Then** the capability
   catalog, skill manifest, template source/condition, skill reference doc, and dev-skill roots all agree
   (0 drift failures).
2. **Given** the scaffold's swap/adapt guidance, **When** it is read, **Then** the line-drawing helper
   source is listed as consumer-owned replaceable source, consistent with the collision/visibility helpers
   and the starter-scene precedent.

---

### Edge Cases

- **Profile gating**: The skill and the helper source must materialize only for the profiles that include
  line-drawing (game / sample-pack) and be absent otherwise — no orphaned line-drawing source in an app
  or headless-scene product.
- **Replaceability without governance pin**: Following the feature 220 starter-scene lesson (and the
  246/247 helpers), no generated governance/acceptance test may *hard-assert the presence or exact content*
  of the helper source — otherwise a developer who adapts or deletes it fails a gate for doing the
  documented thing.
- **Determinism of the line**: The cell line must be a pure function of its two endpoints, computed with
  integer arithmetic (Bresenham) rather than floating-point interpolation, so identical endpoints yield an
  identical, byte-identical cell list across runs and platforms — with no dependence on rounding mode,
  hash-container iteration order, or frame-arrival order (safe to run inside a deterministic-replay
  simulation `update`).
- **Degenerate geometry**: A line whose start equals its goal returns the single start cell; a purely
  horizontal, vertical, or diagonal line, and lines in every octant (including negative directions), all
  return a total, documented result — never a throw or a divergent ordering.
- **Thin-line diagonal gaps**: A thin (Bresenham) line steps diagonally, so two diagonally adjacent cells
  can be "connected" with a gap at their shared corner. For line-of-sight through walls this can leak
  sight through a diagonal wall join; the helper offers the **supercover** variant (visits every touched
  cell) and documents which to use for sight.
- **Vector vocabulary clash**: The helper must reuse the shared `Cell` (and the `Pathfinding`
  `Cell -> bool` predicate convention) rather than introducing a look-alike `(row, col)` / grid-position
  type that collides with the consumer's own records. `Cell` is discrete (integer tile index), distinct
  from the float `Point` — the helper must not conflate the two.
- **Adapt-and-own drift**: Because the source is copied into the product, later framework improvements do
  not reach an already-scaffolded product automatically. The consumer owns the copy; this trade-off must be
  stated so it is a deliberate choice, not a surprise.
- **Catalog coherence**: Adding a skill touches multiple registries (capability catalog, skill manifest,
  template sources/conditions, skill reference doc, dev-skill roots and their mirror). All must agree or
  the coherence gates fail.
- **Line length / bound**: A line between two cells is inherently finite (bounded by the Chebyshev
  distance between the endpoints), so — unlike an unbounded visibility ray — no separate radius bound is
  needed; the helper documents that the cell count is bounded by the endpoint separation.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a new dedicated grid line-drawing skill (`fs-gg-line-drawing`) that
  guides the full capability — the `Cell` grid model, the Bresenham cell line, the supercover variant, and
  the grid line-of-sight query — for a generated game/sample-pack product, citing the Red Blob Games
  line-drawing reference for the algorithm.
- **FR-002**: The line-drawing skill MUST reuse and reference the existing shared primitives — the integer
  `Cell` grid coordinate and the `Pathfinding` `Cell -> bool` walkability/transparency predicate
  convention (`FS.GG.UI.Canvas`, feature 245) — rather than introducing duplicate or look-alike grid
  vocabulary, and MUST call out that `Cell` (discrete) is distinct from the float `Point`.
- **FR-003**: The system MUST materialize the line-drawing skill for the profiles that include it (game and
  sample-pack) and MUST NOT materialize it for profiles that exclude it.
- **FR-004**: The system MUST provide a **helper source support fragment** that the scaffold materializes
  as **product-owned, adaptable source** (compiled as part of the product, not consumed as a package
  reference) into a game/sample-pack product.
- **FR-005**: The helper source MUST cover the full line-drawing capability: a deterministic Bresenham
  `line` (ordered cells between two `Cell`s), a `supercover` variant that visits every touched cell (no
  diagonal gap), and a `lineOfSight` query over a `Cell -> bool` predicate.
- **FR-006**: The helper source MUST produce, for `line`/`supercover`, an **ordered list of `Cell`s** (the
  path of tiles from start to goal, both endpoints included) as its result — the tiles the line passes
  through — not merely a distance or a bare boolean (though the `lineOfSight` boolean convenience is built
  on the same walk).
- **FR-007**: The consumer MUST be able to freely edit, extend, or delete the helper source; the product
  MUST still build and MUST NOT fail any generated governance/acceptance gate **solely** because the helper
  source was changed or removed (the feature 220 / 246 / 247 non-hard-pin rule).
- **FR-008**: The line-drawing computation MUST be deterministic — a pure function of its two endpoints,
  computed with integer arithmetic (Bresenham), with no dependence on floating-point rounding mode,
  hash-container iteration order, or frame-arrival order — so identical endpoints yield an identical cell
  list across runs and platforms.
- **FR-009**: The helper source and skill MUST reuse the shared `Cell` type and MUST NOT introduce a
  competing grid-coordinate/`(row, col)` type that shadows the shared vocabulary; guidance MUST call out
  the `Cell`-vs-`Point` (discrete-vs-float) and consumer-vs-framework grid-naming footguns.
- **FR-010**: All line-drawing entry points the consumer calls MUST be **total** — degenerate inputs
  (start equals goal, purely horizontal/vertical/diagonal lines, every octant including negative
  directions, an always-false or always-true predicate) return a documented value rather than throwing.
- **FR-011**: The line-drawing computation MUST be **bounded** — the returned cell count is finite,
  bounded by the separation between the two endpoints (the line is inherently finite; no unbounded walk).
- **FR-012**: Adding the line-drawing skill MUST keep every skill/capability registry coherent — the
  capability catalog, the skill manifest (id + digest + materialize condition + source), the template
  scaffold source/condition, the skill reference doc, and the dev-skill roots plus their materialized
  mirror — so the repo's coherence gates pass.
- **FR-013**: The line-drawing helper fragment MUST be classified in the scaffold's file taxonomy as
  **replaceable/adaptable** (consumer-owned), consistent with the collision/visibility helpers and
  model-swap classification, and MUST be reachable from the scaffold's swap/adapt guidance so a consumer
  knows it is theirs to change.
- **FR-014**: If the set of files the template emits is a versioned cross-repo contract, shipping this
  feature MUST update the cross-repo dependency/compatibility registry accordingly on release
  (publish-before-flip), consistent with how the prior additive template/skill features (243/244/246/247)
  were released.

### Key Entities *(include if feature involves data)*

- **Line-drawing skill (`fs-gg-line-drawing`)**: The authored guidance capability — scope, the reused
  primitive surfaces, the `Cell` grid model → Bresenham line → supercover → line-of-sight pipeline, the
  Red Blob Games reference, footguns, and a pointer to the adaptable helper source. Registered in the
  capability catalog and skill manifest; materializes for game/sample-pack.
- **Line-drawing helper source fragment**: Product-owned, adaptable source materialized into a
  game/sample-pack product. Reuses `Cell`; adds the Bresenham `line`, the `supercover` walk, and the
  `lineOfSight` query. Consumer-owned, replaceable, no framework package backing, not hard-pinned by any
  gate.
- **Cell (reused, not re-created)**: The integer grid coordinate `{ Col; Row }` (`FS.GG.UI.Canvas`,
  feature 245) — the atom the line is expressed over. Distinct from the float `Point`.
- **Cell line / supercover**: What the helper produces per invocation — an ordered list of `Cell`s from
  start to goal (both endpoints included). `line` is the thin, diagonal-connected Bresenham walk;
  `supercover` visits every cell the segment touches.
- **Transparency predicate (`Cell -> bool`)**: The same shape as `Pathfinding`'s `isWalkable` — the
  caller-supplied map the `lineOfSight` query consults; the framework holds no map.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer who scaffolds a game product can produce a visibly correct tile line-of-sight
  result (sight blocked by a wall tile, restored when it is removed) by editing **only the provided helper
  source** — with **no** added package reference and **no** framework edit — in under 15 minutes.
- **SC-002**: Grid line-drawing guidance is discoverable as **exactly one** dedicated skill that cites the
  Red Blob Games reference and reuses the existing `Cell`/`Pathfinding` surfaces (no re-derived algorithm
  buried in an unrelated skill).
- **SC-003**: The helper source can be fully edited or deleted by the consumer with **zero** governance or
  acceptance gates failing on that basis alone.
- **SC-004**: The cell line is replay-deterministic — across repeated runs on the same endpoints, the
  emitted cell list is byte-identical (0 divergences), including for lines in every octant and along the
  axes/diagonals.
- **SC-005**: The shipped helper source and skill introduce **zero** re-rolled grid-coordinate / `(row,
  col)` types — 100% of the grid vocabulary reuses the existing `Cell` surface.
- **SC-006**: The line-drawing skill materializes for **100%** of game/sample-pack scaffolds and **0%** of
  profiles that exclude it.
- **SC-007**: After the skill is added, **all** repo skill/capability coherence gates pass (0 registry
  drift failures).
- **SC-008**: All documented degenerate inputs (start equals goal, axis-aligned and diagonal lines, every
  octant, always-false/always-true predicate) return a total, documented result with **zero** throws.

## Assumptions

- The two deliverables are (a) a new product skill and (b) a scaffold-materialized, product-owned source
  fragment — the same shape confirmed for collision (246) and visibility (247). The helper is *not* a new
  frozen package API; the shared `Cell`/`Pathfinding`/`SpatialGrid` continue to ship as the existing
  package surfaces, which the fragment reuses.
- Line-drawing scope is the **grid Bresenham line, the supercover variant, and a grid line-of-sight query**
  from the Red Blob Games line-drawing reference. Anti-aliased / fractional line coverage, thick brush
  strokes wider than the supercover, and continuous (non-grid) ray casting are **out of scope**; the helper
  emits cell lists / a LOS boolean and leaves rendering to the consumer.
- The framework ships the discrete `Cell` and the `Pathfinding` predicate convention (feature 245) but no
  cell-line walk; that walk is exactly the part the helper adds and the framework deliberately does not
  freeze — the direct analogue of collision *response* (246) and the visibility *sweep* (247).
- The new skill is standalone (`fs-gg-line-drawing`) and materializes for the same profiles collision and
  visibility do (game and sample-pack). Unlike collision (which trimmed a duplicate section out of
  `fs-gg-game-core`), there is no existing line-drawing write-up to consolidate, so no sibling-skill trim
  is required.
- The helper fragment follows the established "consumer-owned, replaceable, not governance-pinned"
  precedent set by the game starter scene (220), the collision helper (246), the visibility helper (247),
  and the model-swap file taxonomy.
- Determinism of the line is a first-class requirement because the computation is expected to be usable
  inside the deterministic fixed-step simulation loop this product tier already ships — hence integer
  Bresenham, not float interpolation.
- Release may require a cross-repo template-contract update if the emitted file set is a versioned contract;
  the classification (Tier 1 contract-change vs local) is confirmed during `/speckit-plan`.

## Dependencies

- Existing shared grid primitives: the integer `Cell` and `Pathfinding` (`Cell -> bool` predicate
  convention) in `FS.GG.UI.Canvas` (feature 245) — reused, not modified.
- The collision-detection (246) and 2D-visibility (247) features as the pattern precedent for a skill +
  import-and-adapt helper source pair.
- The skill materialization pipeline and registries: capability catalog, skill manifest + generator,
  template scaffold sources/conditions, skill reference doc, and the dev-skill-root materialize/parity
  tooling (features 224 / 231 / 238 / 230).
- The scaffold file taxonomy and swap/adapt guidance (model-swap skill; features 220 / 242) that governs
  consumer-owned replaceable source.
- On release: the cross-repo dependency/compatibility registry in `FS-GG/.github` if the template's
  emitted-file contract version changes.
- External reference (design source only, not a code dependency): Red Blob Games, "Line Drawing on a Grid"
  (<https://www.redblobgames.com/grids/line-drawing/>).
