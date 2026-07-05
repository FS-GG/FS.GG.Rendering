# Feature Specification: Grid-Parts Skill + Import-and-Adapt Helper Source

**Feature Branch**: `249-grid-parts-skill`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "create a grids skill like the visibility and collision detection: https://www.redblobgames.com/grids/parts/ https://www.redblobgames.com/grids/edges/"

## Context (why this feature, in plain terms)

A developer who scaffolds a **game** (or **sample-pack**) product from the FS.GG.UI template already
gets the pieces they need to *route over* and *bucket* a grid — deterministic pathfinding over cells and
a uniform spatial grid for range/splash queries (`Pathfinding`/`SpatialGrid` in `FS.GG.UI.Canvas`,
feature 245), plus the shared `Point`/`Rect` vector vocabulary (`FS.GG.UI.Scene`). What they do **not**
get is the game-shaped layer that answers *"what are the **parts** of this grid, and how do they relate?"*
— the grid **edges** (the shared boundaries between two cells) and **vertices** (the corners where edges
meet), how to name each part with one canonical coordinate, how to convert between the parts (a cell's
edges and corners, the two cells an edge separates, the four cells around a vertex), and how to map each
part to and from pixels.

This is exactly the grid vocabulary described in the Red Blob Games references "Parts of a grid"
(<https://www.redblobgames.com/grids/parts/>) and "Grid edges" (<https://www.redblobgames.com/grids/edges/>):
a square grid is made of **faces** (cells/tiles), **edges** (boundaries between adjacent faces), and
**vertices** (corners); each part has a small, canonical coordinate and a fixed set of adjacency
relationships to the other parts. It is the natural sibling of the collision (246) and visibility (247)
features — all three are per-frame geometry layers reusing the shared primitives — and it is exactly the
kind of opinionated, per-game code a consumer needs to **edit** (change the origin, add hex support, walk
edges differently), not merely call from a frozen package.

Today that guidance and that code ship **nowhere**: there is no grids skill, and no grid-parts source.
The existing `Cell` is a face coordinate for pathfinding; there is no `Edge` or `Vertex` value and no
part-to-part conversion anywhere in the framework. Developers who want fence-on-a-boundary walls,
autotiling / marching-squares over vertices, region borders, or robust pixel snapping hand-roll the whole
edge/vertex addressing scheme — including the one part the framework deliberately does not freeze into a
package, the **canonical part coordinates and the adjacency conversions between them**.

This feature delivers two things, mirroring the collision (246) and visibility (247) features:

1. A dedicated **grid-parts skill** (`fs-gg-grids`) that a coding agent loads when the task involves grid
   edges, corners, boundaries, autotiling, or snapping — covering the parts vocabulary (face/edge/vertex),
   the canonical coordinate for each part, the adjacency conversions, and the pixel mapping — instead of
   re-deriving the addressing scheme from scratch, and citing the two Red Blob Games references.
2. A **helper source support fragment** the scaffold materializes as **product-owned, adaptable source**
   — grid-parts code the consumer imports into their product and edits freely (like the collision and
   visibility helpers), covering the `Edge`/`Vertex`/`GridSpec` value shapes, the six part-to-part
   conversions, and the pixel mapping — reusing the existing shared `Cell` (the face) and `Point`/`Rect`
   (pixels). This uses the **same third delivery mode** collision introduced — product-owned adaptable
   source alongside package-referenced APIs and the single-instance scaffold starter.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - I scaffold a game and get grid-parts source I own and can adapt (Priority: P1)

A developer scaffolds a **game** product and finds a small, readable grid-parts helper file already in
their product source tree (not behind a package boundary). It defines the missing grid parts — an `Edge`
(the boundary between two cells) and a `Vertex` (a grid corner) — each with one canonical coordinate,
reusing the existing shared `Cell` for faces and `Point`/`Rect` for pixels. It converts between the parts
(a cell's four edges and four corners, the two cells an edge separates, the two vertices at an edge's
ends, the four cells and four edges around a vertex) and maps every part to/from pixels (a cell's rect and
center, a vertex's point, an edge's endpoint pair and midpoint, and the inverse pixel→cell lookup). The
developer edits it directly — change the grid origin, add a diagonal-edge variant, switch corner ordering,
delete what they do not need — the same way they edit their `Model`/`View`. Nothing forces them to keep the
framework's default behavior.

**Why this priority**: This is the concrete artifact analogous to what the user asked for ("a grids skill
like the visibility and collision detection" — both of which shipped adaptable helper source). Without it
the feature delivers only prose. It is independently valuable: a developer can build edge-walls, region
borders, autotiling, or snapping into a game immediately.

**Independent Test**: Scaffold a game product; confirm the grid-parts helper source is present and
compiles as part of the product; feed it a cell and observe its four edges and four corners; take one of
those edges and observe the two cells it separates (one of which is the original cell); round-trip a cell
through its pixel center back to a cell and get the original; edit the grid origin and observe the pixel
mapping shift; delete the file entirely and confirm the product still builds and no governance/test gate
fails because of its absence.

**Acceptance Scenarios**:

1. **Given** a freshly scaffolded game product, **When** the developer inspects the product source tree,
   **Then** an adaptable grid-parts helper source file is present, is owned by the product (not a package
   reference), and compiles with the rest of the product.
2. **Given** a cell, **When** the helper's conversions run, **Then** they return that cell's four edges
   and four corners in a documented order, and for each returned edge the "cells this edge separates"
   conversion includes the original cell — the adjacency relationships round-trip.
3. **Given** a cell and a grid spec (cell size + origin), **When** the developer maps the cell to its
   pixel center and then maps that pixel back to a cell, **Then** they get the original cell back (the
   pixel mapping and its inverse agree).
4. **Given** the developer edits a grid-parts parameter (e.g. the grid origin or cell size), **When** they
   rebuild and re-run, **Then** the computed pixel positions change accordingly with no framework edit
   required.
5. **Given** the developer deletes the grid-parts helper source, **When** they build and run the product
   gates, **Then** the product still builds and no gate hard-fails solely because the helper was removed.

---

### User Story 2 - A coding agent loads a dedicated grid-parts skill (Priority: P1)

When the task is "put a wall on the boundary between two tiles," "draw the border of a region," "autotile
from the corners," or "snap the cursor to the grid," a coding agent working in a scaffolded game/sample-pack
product loads a single, focused **grid-parts** skill. It explains the whole vocabulary — a square grid is
faces (cells), edges (shared boundaries), and vertices (corners); the canonical coordinate for each part;
the adjacency conversions between the parts; and the pixel mapping — points at the adaptable helper source
as the starting point, cites the two Red Blob Games references, and lists the grid-parts-specific footguns
(introducing a look-alike cell/point type instead of reusing the shared `Cell`/`Point`; giving an edge two
names instead of one canonical coordinate; confusing edge orientation; off-by-one corner/cell indexing).

**Why this priority**: The user explicitly asked for a "grids skill." The skill is how the capability
becomes *discoverable and correctly applied* by an agent; it is independently testable and valuable even
before the source fragment is polished.

**Independent Test**: In a scaffolded game product, confirm the grids skill materializes for the
game/sample-pack profiles, is absent for profiles that exclude it, and its guidance references the existing
primitives (`Cell`/`Point`/`Rect`), the two Red Blob Games references, and the adaptable helper source
rather than duplicating unrelated content.

**Acceptance Scenarios**:

1. **Given** a game or sample-pack product, **When** the skill catalog is materialized, **Then** the
   `fs-gg-grids` skill is present and resolvable.
2. **Given** a profile that does not include grids (e.g. a non-game app or headless-scene profile),
   **When** the skill catalog is materialized, **Then** the grids skill is **not** materialized.
3. **Given** the grids skill body, **When** it is read, **Then** it covers the parts vocabulary
   (face/edge/vertex), the canonical part coordinates, the adjacency conversions, and the pixel mapping;
   names the existing `Cell`/`Point`/`Rect` surfaces it reuses; cites the two Red Blob Games references;
   and points at the adaptable helper source as the entry point.

---

### User Story 3 - The skill catalog and swap guidance stay coherent (Priority: P2)

The new grids skill and its helper fragment take their place cleanly alongside the sibling skills
(collision, visibility, game-core, audio, persistence): the capability catalog, skill manifest, template
scaffold sources/conditions, and dev-skill roots all agree, and the scaffold's swap/adapt guidance lists
the grid-parts helper as consumer-owned replaceable source. An agent or developer reading the catalog finds
grid-parts as one authoritative, discoverable capability with no drift.

**Why this priority**: Prevents registry drift and an orphaned or undiscoverable capability. Valuable but
dependent on US2 existing first; a correct-but-not-yet-cross-referenced interim state is tolerable, so this
is P2.

**Independent Test**: Run the repo's skill/capability coherence gates after the skill is added; confirm they
pass with zero drift and that the grid-parts helper appears in the scaffold's swap/adapt file taxonomy as
consumer-owned replaceable source.

**Acceptance Scenarios**:

1. **Given** the added grids skill, **When** the coherence gates run, **Then** the skill manifest, template
   source/condition, dev-skill roots, and the framework product-skill count all agree (0 drift failures).
2. **Given** the scaffold's swap/adapt guidance, **When** it is read, **Then** the grid-parts helper source
   is listed as consumer-owned replaceable source, consistent with the collision/visibility helpers and the
   starter-scene precedent.

---

### Edge Cases

- **Profile gating**: The skill and the helper source must materialize only for the profiles that include
  grids (game / sample-pack) and be absent otherwise — no orphaned grid-parts source in an app or
  headless-scene product.
- **Replaceability without governance pin**: Following the feature 220 starter-scene lesson (and the 246/247
  helpers), no generated governance/acceptance test may *hard-assert the presence or exact content* of the
  helper source — otherwise a developer who adapts or deletes it fails a gate for doing the documented thing.
- **One canonical name per edge**: A grid edge borders two cells and could be named from either; the helper
  must give each edge exactly one canonical coordinate (orientation + col/row) so two references to the same
  edge are equal, and the "cells this edge separates" conversion returns both neighbours deterministically.
- **Adjacency round-trips**: The part conversions must be mutually consistent — every edge a cell reports
  must report that cell among its two neighbours; every corner a cell reports must report that cell among
  its four surrounding faces — so an agent can compose them without surprise.
- **Degenerate pixel mapping**: A non-finite or non-positive cell size, and non-finite point coordinates,
  must produce documented, total results (a fallback cell size; a documented cell for a non-finite point) —
  never a throw or a NaN-poisoned coordinate.
- **Vector/coordinate vocabulary clash**: The helper must reuse the shared `Cell`/`Point`/`Rect` rather than
  introducing a look-alike cell/point type that collides with the consumer's own records (the documented
  consumer-vs-framework and consumer-vs-consumer naming footguns). `Edge`/`Vertex` are genuinely new parts
  the shared vocabulary lacks, not re-rolls of an existing type.
- **Determinism**: The part addressing is integer arithmetic (no floating-point tie-break) and the pixel
  mapping is straight-line float arithmetic guarded against non-finite input, so identical inputs yield
  identical parts and identical pixels across runs and platforms, with no reliance on hash-container
  iteration order — usable inside the deterministic fixed-step loop this tier already ships.
- **Adapt-and-own drift**: Because the source is copied into the product, later framework improvements do
  not reach an already-scaffolded product automatically. The consumer owns the copy; this trade-off must be
  stated so it is a deliberate choice, not a surprise.
- **Catalog coherence**: Adding a skill touches multiple registries (skill manifest, template
  sources/conditions, dev-skill roots and their mirror, framework product-skill counts). All must agree or
  the coherence gates fail.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a new dedicated grid-parts skill (`fs-gg-grids`) that guides the full
  vocabulary — the face/edge/vertex parts, the canonical coordinate for each part, the adjacency conversions,
  and the pixel mapping — for a generated game/sample-pack product, citing the Red Blob Games "Parts of a
  grid" and "Grid edges" references.
- **FR-002**: The grids skill MUST reuse and reference the existing shared primitives — the `Cell` face
  coordinate (`FS.GG.UI.Canvas`) and the `Point`/`Rect` vector/box vocabulary (`FS.GG.UI.Scene`) — rather
  than introducing duplicate or look-alike coordinate vocabulary; it MUST introduce `Edge` and `Vertex` only
  as the genuinely new parts the shared vocabulary lacks.
- **FR-003**: The system MUST materialize the grids skill for the profiles that include it (game and
  sample-pack) and MUST NOT materialize it for profiles that exclude it.
- **FR-004**: The system MUST provide a **helper source support fragment** that the scaffold materializes as
  **product-owned, adaptable source** (compiled as part of the product, not consumed as a package reference)
  into a game/sample-pack product.
- **FR-005**: The helper source MUST cover the grid-parts layer: the `Edge` (orientation + col/row, one
  canonical name per edge), `Vertex` (grid corner), and `GridSpec` (cell size + origin) value shapes; the
  six adjacency conversions (a cell's edges and corners; an edge's two cells and two vertices; a vertex's
  cells and edges); and the pixel mapping (cell rect/center, vertex point, edge endpoint-pair/midpoint, and
  the inverse pixel→cell lookup).
- **FR-006**: The helper source MUST reuse the shared `Cell` as the **face** and `Point`/`Rect` as the pixel
  vocabulary; the new `Edge`/`Vertex` parts MUST each carry exactly **one canonical integer coordinate** so
  two references to the same part are equal.
- **FR-007**: The consumer MUST be able to freely edit, extend, or delete the helper source; the product MUST
  still build and MUST NOT fail any generated governance/acceptance gate **solely** because the helper source
  was changed or removed (the feature 220 / 246 / 247 non-hard-pin rule).
- **FR-008**: The grid-parts conversions MUST be deterministic — pure functions of their inputs with integer
  part-addressing (no floating-point tie-break) and no dependence on hash-container iteration order — so
  identical inputs yield identical parts and (for a given `GridSpec`) identical pixels across runs and
  platforms.
- **FR-009**: The adjacency conversions MUST be **mutually consistent** — every edge a cell reports must
  report that cell among the two cells it separates, and every corner a cell reports must report that cell
  among the faces around it — so they compose without surprise.
- **FR-010**: All grid-parts entry points the consumer calls MUST be **total** — degenerate inputs
  (non-finite or non-positive cell size, non-finite point coordinates) return a documented value rather than
  throwing or emitting a NaN coordinate.
- **FR-011**: Adding the grids skill MUST keep every skill/capability registry coherent — the skill manifest
  (id + digest + materialize condition + source), the template scaffold source/condition, the dev-skill roots
  plus their materialized mirror, and the framework product-skill counts — so the repo's coherence gates
  pass.
- **FR-012**: The grid-parts helper fragment MUST be classified in the scaffold's file taxonomy as
  **replaceable/adaptable** (consumer-owned), consistent with the collision/visibility helpers and model-swap
  classification, and MUST be reachable from the scaffold's swap/adapt guidance so a consumer knows it is
  theirs to change.
- **FR-013**: If the set of files the template emits is a versioned cross-repo contract, shipping this
  feature MUST update the cross-repo dependency/compatibility registry accordingly on release
  (publish-before-flip), consistent with how the prior additive template/skill features (243/244/246/247)
  were released.

### Key Entities *(include if feature involves data)*

- **Grids skill (`fs-gg-grids`)**: The authored guidance capability — scope, the reused primitive surfaces,
  the parts vocabulary and canonical coordinates, the adjacency conversions, the pixel mapping, the two Red
  Blob Games references, footguns, and a pointer to the adaptable helper source. Registered in the skill
  manifest; materializes for game/sample-pack.
- **Grid-parts helper source fragment**: Product-owned, adaptable source materialized into a game/sample-pack
  product. Reuses `Cell`/`Point`/`Rect`; adds the `Edge`/`Vertex`/`GridSpec` shapes, the adjacency
  conversions, and the pixel mapping. Consumer-owned, replaceable, no framework package backing, not
  hard-pinned by any gate.
- **Face (cell)**: The existing shared `Cell` (`{ Col; Row }`) — a grid tile. Reused, not re-created.
- **Edge**: A new part — the shared boundary between two adjacent faces, named by an orientation
  (horizontal/vertical) plus a col/row so each edge has exactly one canonical coordinate.
- **Vertex**: A new part — a grid corner where edges meet, named by a col/row in the corner lattice.
- **Grid spec**: The pixel-mapping policy — a cell size and an origin — the consumer tunes to place the grid
  in pixel space.
- **Existing primitives (reused, not re-created)**: the shared `Cell` (`FS.GG.UI.Canvas`) for faces; the
  shared `Point`/`Rect` (`FS.GG.UI.Scene`) for the pixel vocabulary.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer who scaffolds a game product can produce a visibly correct grid-parts result (e.g.
  a fence drawn on a cell boundary, or a cursor snapped to a cell) by editing **only the provided helper
  source** — with **no** added package reference and **no** framework edit — in under 15 minutes.
- **SC-002**: Grid-parts guidance is discoverable as **exactly one** dedicated skill that cites the two Red
  Blob Games references and reuses the existing `Cell`/`Point`/`Rect` surfaces (no re-derived addressing
  scheme buried in an unrelated skill).
- **SC-003**: The helper source can be fully edited or deleted by the consumer with **zero** governance or
  acceptance gates failing on that basis alone.
- **SC-004**: The grid-parts conversions are replay-deterministic — across repeated runs on the same inputs,
  the returned parts and pixels are byte-identical (0 divergences); and the adjacency conversions round-trip
  (every reported edge/corner of a cell reports that cell back) for 100% of tested cells.
- **SC-005**: The shipped helper source and skill introduce **zero** re-rolled cell / point / rect types —
  100% of the face/pixel vocabulary reuses the existing `Cell`/`Point`/`Rect` surfaces; `Edge`/`Vertex` are
  the only new part types.
- **SC-006**: The grids skill materializes for **100%** of game/sample-pack scaffolds and **0%** of profiles
  that exclude it.
- **SC-007**: After the skill is added, **all** repo skill/capability coherence gates pass (0 registry drift
  failures).
- **SC-008**: All documented degenerate inputs (non-finite/non-positive cell size, non-finite point) return a
  total, documented result with **zero** throws or NaN coordinates.

## Assumptions

- The two deliverables are (a) a new product skill and (b) a scaffold-materialized, product-owned source
  fragment — the same shape confirmed for collision (246) and visibility (247). The helper is *not* a new
  frozen package API; the shared `Cell`/`Point`/`Rect` continue to ship as the existing package surfaces,
  which the fragment reuses.
- Grid-parts scope is the **square-grid** parts vocabulary from the two Red Blob Games references — faces,
  edges, vertices; their canonical coordinates; the adjacency conversions; and the pixel mapping. Hex/triangle
  grids, the broader coordinate-system series (offset/axial/cube conversions), and pathfinding/spatial-hashing
  (already shipped in 245) are **out of scope**; the helper adds only the missing parts layer and is written so
  a consumer can extend it toward other grid shapes.
- The framework does not today ship an `Edge`/`Vertex` value or any part-to-part conversion; that is exactly
  the part the helper adds and the framework deliberately does not freeze — the direct analogue of collision
  *response* (246) and the visibility *sweep* (247). The existing `Cell` is a face coordinate for
  pathfinding only.
- The new skill is standalone (`fs-gg-grids`) and materializes for the same profiles collision/visibility do
  (game and sample-pack). There is no existing grid-parts write-up to consolidate, so no sibling-skill trim is
  required.
- The helper fragment follows the established "consumer-owned, replaceable, not governance-pinned" precedent
  set by the game starter scene (220), the collision helper (246), and the visibility helper (247).
- Determinism of the conversions is a first-class requirement because the grid-parts computation is expected
  to be usable inside the deterministic fixed-step simulation loop this product tier already ships.
- Release may require a cross-repo template-contract update if the emitted file set is a versioned contract;
  the classification (Tier 1 contract-change vs local) is confirmed during `/speckit-plan`.

## Dependencies

- Existing shared primitives: `Cell` (`FS.GG.UI.Canvas`, feature 245) as the face; `Point`/`Rect`
  (`FS.GG.UI.Scene`, feature 239) as the pixel vocabulary — reused, not modified.
- The collision (246) and visibility (247) features as the pattern precedent for a skill + import-and-adapt
  helper source pair.
- The skill materialization pipeline and registries: skill manifest + generator, template scaffold
  sources/conditions, and the dev-skill-root materialize/parity tooling (features 224 / 231 / 238 / 230).
- The scaffold file taxonomy and swap/adapt guidance (model-swap skill; features 220 / 242) that governs
  consumer-owned replaceable source.
- On release: the cross-repo dependency/compatibility registry in `FS-GG/.github` if the template's
  emitted-file contract version changes.
- External references (design source only, not a code dependency): Red Blob Games, "Parts of a grid"
  (<https://www.redblobgames.com/grids/parts/>) and "Grid edges" (<https://www.redblobgames.com/grids/edges/>).
