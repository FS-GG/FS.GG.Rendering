# Phase 0 Research: Grid Line-Drawing

**Feature**: `248-grid-line-drawing` | **Date**: 2026-07-05

All decisions below are settled against the collision (246) and visibility (247) precedents; there are no
open unknowns blocking Phase 1.

## R1 — Delivery mode: package API vs product-owned adaptable source

**Decision**: Ship the algorithm as a **product-owned, adaptable source fragment** (`LineDrawing.fs`),
not a frozen package `.fsi`. The shared grid vocabulary (`Cell`, `Pathfinding`, `SpatialGrid`) stays as
existing package surface and is reused.

**Rationale**: The line walk is opinionated, per-game code the consumer edits — thin (Bresenham) vs
supercover, 4- vs 8-connected feel, cap the line length, stop-at-first-blocked line-of-sight over the
consumer's own map. Freezing it into a package would fight the consumer; the "third delivery mode"
(product-owned adaptable source) established by collision (246) and visibility (247) is the exact fit.
The framework already ships the discrete `Cell` and the `Pathfinding` predicate convention (feature 245);
the cell-line walk is deliberately the part left to the consumer — the direct analogue of collision
*response* (246) and the visibility *sweep* (247).

**Alternatives rejected**: (a) a `LineDrawing` package module in `FS.GG.UI.Canvas` — freezes an
opinionated walk the consumer wants to edit and adds public surface for no gain; (b) burying it inside
`fs-gg-game-core` — undiscoverable and not independently deletable.

## R2 — Determinism: integer Bresenham vs float linear interpolation

**Decision**: Compute the cell line with **integer Bresenham** (error-accumulator form), not the
floating-point linear-interpolation-and-round approach the Red Blob Games article presents first.

**Rationale**: The article's lerp form (`lerp(a, b, t)` then `round`) is clearest for teaching but its
last-bit rounding can differ across runtimes and flip a cell on a near-tie, breaking replay determinism
(the same class of hazard as sorting a visibility sweep by `atan2`). Integer Bresenham uses only integer
add/compare, so identical endpoints yield a byte-identical cell list across runs and platforms (FR-008) —
safe inside the deterministic fixed-step loop this tier ships. This is the line-drawing analogue of
visibility's "cross-product comparator, no `atan2`" determinism story.

**Alternatives rejected**: float lerp + round (rounding-mode drift); `Math.Round`-based sampling (same
hazard).

## R3 — Thin line vs supercover (which cells count as "on the line")

**Decision**: Ship **both** `line` (thin, diagonal-connected Bresenham) and `supercover` (visits every
cell the segment touches, including both cells at a corner crossing), and base `lineOfSight` on the
supercover walk by default.

**Rationale**: A thin Bresenham line steps diagonally, leaving a corner gap between two diagonally
adjacent cells. That is fine for drawing a road but *leaks* sight through a diagonal wall join, so
line-of-sight should use the supercover walk. Offering both, and documenting which to use, matches the
article and the acceptance scenarios (US1 #3 uses a blocking cell for LOS).

**Alternatives rejected**: shipping only the thin line (LOS would leak through wall corners); shipping
only supercover (a thin road/beam becomes two-cells-wide at diagonals).

## R4 — Compile order + delete safety

**Decision**: Add `<Compile Include="LineDrawing.fs" Condition="Exists('LineDrawing.fs')" />` inside the
existing `(profile == "game" || profile == "sample-pack")` block in `Product.fsproj`, next to
`Collision.fs`/`Visibility.fs`, **before** `Model.fs` (so `update`/`view` can call it).

**Rationale**: Profile-gated at scaffold time (only game/sample-pack get the line in the `.fsproj`),
`Exists`-guarded at build time (deleting `LineDrawing.fs` drops the compile item and the build stays
green — FR-007). This is exactly how `Visibility.fs` is wired; `Product.fsproj` stays a "durable — do not
touch" file.

## R5 — Fragment source/target rename (the 246 trap)

**Decision**: In `template.json`, the fragment source row uses `source: template/fragments/line-drawing/src/`,
`target: src/` (NO `copyOnly`), so the `Product/` path segment is **source-relative** and gets
`fileRename`d to `<productName>` — landing the file at `src/<ProductDir>/LineDrawing.fs` next to the
renamed project.

**Rationale**: An explicit `target: src/Product/` is NOT sourceName-renamed and orphans the file in a
stray `src/Product/` the renamed project never compiles — the silent bug 246 shipped with and 247 fixed.
Only a real scaffold+build catches it, so the quickstart and a task both exercise a real
`dotnet new` scaffold. (See the `fragment-target-sourcename-rename` note.)

## R6 — Bound

**Decision**: No separate bound is needed. A cell line between two `Cell`s is inherently finite — bounded
by the Chebyshev distance between the endpoints — so the walk always terminates (FR-011). This is simpler
than visibility, which needed a sight radius to terminate unhit rays.

## R7 — Contract classification

**Decision**: **Tier 1 template-contract change.** It alters the `fs-gg-ui-template` emitted-file set (new
skill, new source file, new compile item) but adds **no F# package public surface**. On release, bump the
coherent set and update the cross-repo dependency/compatibility registry publish-before-flip (FR-014) —
identical to how 243/244/246/247 released.

## R8 — Vocabulary reuse

**Decision**: Reuse `Cell` (`FS.GG.UI.Canvas`, feature 245) for the grid coordinate and the `Cell -> bool`
predicate shape (as in `Pathfinding.isWalkable`) for `lineOfSight`. Introduce **no** look-alike
`(row, col)` type (FR-009). Document that `Cell` (discrete integer tile index) is distinct from the float
`Point` — conflating them is the grid analogue of the `Point`/`Rect` record-collision footgun.
