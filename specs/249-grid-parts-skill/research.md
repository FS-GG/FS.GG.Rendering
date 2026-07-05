# Phase 0 Research: Grid-Parts Skill + Import-and-Adapt Helper Source

All decisions below resolve the Technical Context so no `NEEDS CLARIFICATION` remains for Phase 1. The
feature deliberately mirrors the collision (246) and visibility (247) features; where a decision is
identical in shape, the rationale notes the shared precedent.

## R1 — Delivery mode: package API vs. adaptable source

**Decision**: Ship the **`Edge`/`Vertex`/`GridSpec` parts, the six adjacency conversions, and the pixel
mapping** as **product-owned, adaptable source** (`Grids.fs`) materialized into the game/sample-pack
product; keep the **face and pixel vocabulary** (`Cell` in `FS.GG.UI.Canvas`, `Point`/`Rect` in
`FS.GG.UI.Scene`) as the existing package API it reuses. Do **not** add a new packable library or `.fsi`.

**Rationale**: The face coordinate and the vector/box vocabulary are objective and already frozen —
`Cell` (`{ Col; Row }`, `FS.GG.UI.Canvas`, feature 245) and `Point`/`Rect` (`FS.GG.UI.Scene`, feature
239). The **parts addressing** itself is *per-game policy* the consumer edits (grid origin, cell size,
corner order, a diagonal-edge variant, hex/triangle extension, whether an edge is named from the cell
above or below) — the wrong thing to freeze behind a surface-baselined `.fsi`. The requester said "create
a grids skill like the visibility and collision detection," both of which shipped consumer-owned helper
source. This is exactly the collision R1 / visibility R1 decision applied to grid parts: the shared
vocabulary stays frozen; the opinionated layer (there: response, then the sweep; here: the part-addressing
and pixel mapping) ships as adaptable source.

**Alternatives considered**:
- *New `FS.GG.UI.Grids` package with an `Edge`/`Vertex` API*: rejected — freezes an addressing scheme
  every game overrides, adds a surface baseline and coherent-set member for a thing meant to be edited,
  and contradicts the ask.
- *Extend `Cell`/`Canvas` with `Edge`/`Vertex` and the conversions*: rejected — pollutes the objective
  face-coordinate/broad-phase primitives with opinionated parts policy; still frozen; still can't be
  deleted/adapted. `Cell` today is a face coordinate for `Pathfinding`/`SpatialGrid` only — there is **no**
  `Edge`, `Vertex`, or part-to-part conversion anywhere in the framework. That absence is deliberate; the
  helper fills it in consumer-owned source.

## R2 — Where the skill and source live, and how they register

**Decision**: Skill body at `template/product-skills/fs-gg-grids/SKILL.md` (sibling to `fs-gg-visibility`);
adaptable source at `template/fragments/grids/src/Product/Grids.fs` with a fragment `README.md`. Register
the skill through the **skill-only** path, identical to `fs-gg-collision`/`fs-gg-visibility`:

1. `scripts/generate-skill-manifest.fsx` — add to the `catalog` list **alphabetically** (after
   `fs-gg-game-core`, before `fs-gg-keyboard-input`):
   `"fs-gg-grids", "template/product-skills/fs-gg-grids/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"`.
2. `template/skill-manifest/skill-manifest.json` — regenerated entry (id, sha256 of the body,
   `resolvablePath: .agents/skills/fs-gg-grids/SKILL.md`, `materializes-when: profile in [game, sample-pack]`,
   `supplied-by: template/product-skills/fs-gg-grids/`).
3. `.template.config/template.json` — **two** gated `sources[]`:
   - skill: `condition (profile == "game" || profile == "sample-pack")`, `source template/product-skills/fs-gg-grids/`,
     `target .agents/skills/fs-gg-grids/`, `copyOnly ["**/*"]`.
   - source: `condition (profile == "game" || profile == "sample-pack")`, `source template/fragments/grids/src/`,
     `target src/` — **not** `src/Product/`. The `Product/` segment stays **source-relative** so
     sourceName substitution (`fileRename` `Product` → `<ProductDir>`) rewrites it to
     `src/<ProductDir>/Grids.fs`. (The Feature 246→247 fragment-target fix — see
     [[fragment-target-sourcename-rename]]: an explicit `target: src/Product/` orphans the file.)
4. **Gate-enforced coherent set** (see [[adding-a-product-skill-touchpoints]] — several are enforced by
   separate test projects and are easy to miss):
   - `tests/Package.Tests/Feature231SkillManifestTests.fs` **and** `Feature238SkillMaterializesWhenTests.fs`
     — add `("fs-gg-grids", "template/product-skills/fs-gg-grids/SKILL.md")` to the `canonicalSources`
     list in **both**.
   - `tests/Package.Tests/Feature204LifecycleTemplateTests.fs` — framework product-skill count `16 → 17`.
   - `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs` — add `fs-gg-grids` to the `game`
     **and** `sample-pack` `expectedFrameworkSkills` sets (and any `.agents`-only narration/source count).
   - `scripts/validate-lifecycle-template.fsx` — `frameworkChecked = 16 → 17`.
5. **Dev roots + wrapper + mirror** (Deterministic gate → `Rendering.Harness.Tests` skill inventory /
   parity): `.agents/skills/fs-gg-grids/SKILL.md` (canonical, byte-identical to the product-skills body)
   + `.claude/skills/fs-gg-grids/` mirror via `template/lifecycle/materialize-skill-roots.fsx`; **and the
   thin `fs-gg-product-grids` wrapper in both `.agents/` and `.claude/`** (frontmatter
   `name: fs-gg-product-grids` + the canonical description, body pointing at
   `../../../template/product-skills/fs-gg-grids/SKILL.md`; the two differ only by the "Codex-active" vs
   "Claude-active" line). Parity asserted by `scripts/check-agent-skill-parity.fsx` /
   `dotnet run --project tools/Rendering.Harness -- skill-parity` (0 findings) → regenerate
   `docs/reports/skills-parity.md`.

**Rationale**: `capabilities.yml` lists only package/fragment-backed *package* capabilities, and
`skillist-reference.md` is a **curated subset** — the collision (246) and visibility (247) skills are in
**neither**, so `fs-gg-grids` follows the same path and touches **neither** file. The `template.json` gate
condition MUST be *semantically equal* to the manifest `materializes-when` (Feature 238 enforces), so both
use the same `profile ∈ {game, sample-pack}` expression. The current framework product-skill count is
**16** (visibility, 247, was the 16th on the same gate); grids is the **17th**.

**Alternatives considered**:
- *Skill under the fragment (`template/fragments/grids/skill/`, like `fs-gg-samples`)*: workable but the
  grids skill is a first-class capability sibling of `fs-gg-collision`/`fs-gg-visibility`, so
  `product-skills/` is the consistent home; `supplied-by` can point at either, so this is purely
  organizational.
- *Add a `capabilities.yml` / `skillist-reference.md` entry*: rejected — matches the
  collision/visibility/game-core precedent (skill-only capabilities appear in neither).

## R3 — Compile-order entry and delete-safety

**Decision**: Add one gated compile item to `template/base/src/Product/Product.fsproj`:

```xml
<!--#if (profile == "game" || profile == "sample-pack") -->
<Compile Include="Grids.fs" Condition="Exists('Grids.fs')" />
<!--#endif -->
```

Place it in the same game/sample-pack region as `Collision.fs`/`Visibility.fs`, **before** `Model.fs` (the
helper is consumed by the model/update/view, not by the re-export spine).

**Rationale**: identical mechanism to collision R3 / visibility R3. `Product.fsproj` already gates compile
items with the C-style `<!--#if (profile == "game" || profile == "sample-pack") -->` preprocessor
(`WindowOptions.fs`, `Collision.fs`, `Visibility.fs`, the Canvas reference), so profile-gated
*materialization* is established — the file and its compile line appear only for game/sample-pack (FR-003).
The `Condition="Exists('Grids.fs')"` MSBuild guard makes **deletion** safe (FR-007): remove the file and
the compile item silently drops, so the product still builds with no dangling `Compile Include` — while
`Product.fsproj` stays a "durable — do not touch" file. The governance compile-order scan anchors on the
literal `Compile Include="X.fs"` substring (commit 3fdcf63), which the conditioned item still satisfies, so
the scan is unaffected.

**Alternatives considered**: shipping in the ungated base tree (would materialize into app/headless too,
violating FR-003); an unconditioned `<Compile Include>` (deleting the file would break the build,
violating FR-007); wildcard globbing (breaks the governed explicit compile order). All rejected — same as
collision/visibility R3.

## R4 — Part-addressing determinism (the load-bearing decision)

**Decision**: The part conversions are **pure integer arithmetic** returning fixed-length lists in a
**fixed, documented order**. Determinism rules baked into `Grids.fs`:

- **Integer part-addressing — no floating-point anywhere in the adjacency layer.** `cellCorners`,
  `cellEdges`, `edgeCells`, `edgeVertices`, `vertexCells`, `vertexEdges` are `int` add/subtract on
  `Col`/`Row` only. There is no float tie-break, no `atan2`, no `sqrt`, and no distance comparison to
  order the parts — so there is **no** last-bit drift surface at all (unlike the visibility sweep, whose
  determinism hinged on avoiding `atan2`; grid parts avoid floats in the addressing entirely).
- **Fixed list order per conversion.** Each list is emitted in one documented order: `cellCorners`
  TL/TR/BR/BL, `cellEdges` top/right/bottom/left, `edgeCells` the two faces in ascending order
  (left-then-right for `Vertical`, above-then-below for `Horizontal`), `edgeVertices` start-then-end along
  the edge's natural direction, `vertexCells` TL/TR/BR/BL, `vertexEdges` up/right/down/left. Never iterate
  a `Dictionary`/`HashSet`.
- **One canonical name per edge.** An edge borders two cells and could be addressed from either; the
  scheme fixes exactly one name: `Edge Vertical { Col; Row }` is the boundary of cells `(Col-1, Row)` /
  `(Col, Row)` (named from the cell on its **right**); `Edge Horizontal { Col; Row }` is the boundary of
  cells `(Col, Row-1)` / `(Col, Row)` (named from the cell **below**). So two references to the same
  boundary are structurally equal `Edge` records — record equality settles it, no normalization pass.
- **Pixel mapping is straight-line, non-finite-guarded float arithmetic.** `cellRect`/`cellCenter`/
  `vertexPoint`/`edgeSegment`/`edgeMidpoint`/`cellAt` are `Origin + coord * CellSize` (and a `floor`
  inverse), with the guards from R6. No transcendental, so the float output is bit-identical across
  platforms for a given `GridSpec`.

**Rationale**: The helper is expected to run inside the `FS.GG.UI.Canvas` fixed-step loop, whose value is
replay-identical simulation (Feature 239/245). Integer addressing is *trivially* replay-deterministic;
the only float surface (the pixel maps) is straight-line arithmetic with no ordering or tie-break, so it
too is byte-identical. This is a **stronger** determinism posture than visibility's, because grid parts
have no angular sort to make deterministic — the addressing is direct arithmetic. Note the Red Blob Games
"Parts of a grid" reference presents several equivalent naming conventions; the FS.GG helper picks **one
canonical convention** (documented above) and holds it, which is what makes the parts composable.

**Alternatives considered**:
- *Address edges by the pair of cells they separate (an unordered `Cell*Cell`)*: rejected — a pair has
  two orderings and no single canonical form, breaking equality and the round-trip; the orientation +
  col/row scheme gives exactly one name.
- *Float-derived corner ordering (sort corners by angle from the cell center)*: rejected — reintroduces a
  float tie-break for no benefit; the fixed TL/TR/BR/BL order is deterministic and matches the pixel
  mapping.

## R5 — Round-trip invariants (FR-009, FR-010)

**Decision**: Two round-trip properties are load-bearing and tested as FsCheck properties:

- **Adjacency round-trip (FR-009).** Every edge a cell reports reports that cell back, and every corner
  a cell reports reports that cell among its faces: for all `c`, `cellEdges c |> List.forall (fun e ->
  edgeCells e |> List.contains c)` and `cellCorners c |> List.forall (fun v -> vertexCells v |>
  List.contains c)`. The canonical naming (R4) is exactly what makes this hold: a cell's *right* edge is
  `Edge Vertical { Col+1; Row }`, whose `edgeCells` are `(Col, Row)` and `(Col+1, Row)` — the original
  cell is the left of the pair. Symmetrically for corners via `vertexCells`.
- **Pixel round-trip (FR-010).** `cellAt (cellCenter spec c) = c` for every cell and every valid
  `GridSpec` — the pixel mapping and its floor-based inverse agree. `cellCenter` places the point at
  `Origin + (col + 0.5) * CellSize`; `cellAt` floors `(p - Origin) / CellSize`, so the center of cell `c`
  floors back to `c`. `cellAt` is documented as floor-based, so a point exactly on a boundary belongs to
  the cell to its right/below — a deliberate, stated tie rule (not an ambiguity).

**Rationale**: These two round-trips are what let an agent *compose* the conversions without surprise
(spec Edge Cases "Adjacency round-trips" and Acceptance 2/3). They are cheap to state and cheap to test,
and they are the concrete meaning of "mutually consistent" (FR-009) and "the pixel mapping and its inverse
agree" (FR-010 / US1 Acceptance 3).

**Alternatives considered**: asserting only the six conversions individually — rejected: it would leave
their *mutual consistency* (the property an agent actually relies on) unverified.

## R6 — Vocabulary reuse (no look-alike cell/point) and totality

**Decision**: `Grids.fs` reuses the shared `FS.GG.UI.Canvas.Cell` as the **face** and
`FS.GG.UI.Scene.Point`/`Rect` as the **pixel** vocabulary. It adds only the genuinely-new parts:
`EdgeOrientation` (`Horizontal | Vertical`), `Edge = { Col; Row; Orientation }`, `Vertex = { Col; Row }`,
and `GridSpec = { CellSize; Origin }`. **No** new cell/point/vector/bounds record is introduced. The pixel
mapping is **total**: a non-finite or non-positive `CellSize` falls back to `1.0`; a non-finite
origin/coordinate falls back to `0.0`; `cellAt` maps a non-finite axis coordinate to `0` — so no NaN ever
escapes and nothing throws (FR-010).

**Rationale**: FR-006 and the documented consumer-vs-framework / consumer-vs-consumer `.Pos` footguns: a
second `{ Col; Row }`- or `{ X; Y }`-shaped type invites the bare-record-inference bug, so the helper
never re-rolls one. `Edge`/`Vertex` are **new parts** the shared vocabulary genuinely lacks (`Cell` is a
face; `Vertex` is a corner in a lattice offset half a cell; `Edge` is a boundary with an orientation), not
competing coordinate types. Reusing `Cell`/`Point`/`Rect` also lets the helper feed `SpatialGrid`/
`Geometry`/the scene directly with no conversion. The totality guards mirror `Geometry`/`SpatialGrid`
NaN-safety and are exactly the degenerate-input contract (FR-010 / SC-008). The skill's `## Common
pitfalls` restates the geometry/coordinate-clash footgun (as `fs-gg-scene`/`fs-gg-collision`/
`fs-gg-visibility` already do), plus the grid-specific ones (two names for one edge; confusing edge
orientation; off-by-one corner/cell indexing).

**Alternatives considered**: a dedicated `GridCell`/`Coord` record shadowing `Cell` — rejected per above;
`Vertex` deliberately does **not** reuse `Cell` even though both are `{ Col; Row }` ints, because a vertex
is a *corner in the offset lattice*, a distinct part with distinct adjacency — conflating them would break
the round-trip semantics. `EdgeOrientation`/`Edge`/`Vertex` are the minimal new part vocabulary.

## R7 — Change classification and cross-repo contract

**Decision**: **Tier 1 template-contract change** (no F# package public surface). On release: bump the
FS.GG.UI coherent set and, publish-before-flip, update `registry/dependencies.yml`,
`registry/CHANGELOG.md`, and `docs/registry/compatibility.md` in `FS-GG/.github` for the
`fs-gg-ui-template` contract; confirm exact edges through the `cross-repo-coordination` skill.

**Rationale**: identical to collision R6 / visibility R7. The set of files the template emits (skills +
product source) is the `fs-gg-ui-template` contract that generated products and the SDD scaffold-provider
consume; adding a skill + a materialized source file + a compile item changes it. Sibling skill additions
(243 audio, 244 persistence, 246 collision, 247 visibility) took the same path. No surface-area baseline
is added because no packed public API changes.

**Alternatives considered**: *Tier 2 (local)* — rejected: the emitted-file set is cross-repo observable,
so treating it as local would risk an incoherent registry.

## R8 — Testing the adaptable source (which is a template file)

**Decision**: Two tests, mirroring collision R7 / visibility R8.
1. `tests/Package.Tests/Feature249GridsSkillTests.fs` — coherence: the `catalog` entry, the regenerated
   `skill-manifest.json` digest, the two `template.json` sources, the dev-root/wrapper/mirror parity, and
   the materialize condition (exactly `profile ∈ {game, sample-pack}` — present for those, absent
   otherwise) all agree. Mirrors `Feature247VisibilitySkillTests`.
2. `tests/Canvas.Tests/GridsHelperTests.fs` — logic: the raw
   `template/fragments/grids/src/Product/Grids.fs` (literal `namespace Product`, the default `sourceName`)
   is added via `<Compile Include>` and compiles unmodified into `Canvas.Tests`, which already references
   `FS.GG.UI.Canvas` + `FS.GG.UI.Scene`; assert (a) **adjacency round-trip** — every edge/corner a cell
   reports reports that cell back (`edgeCells`/`vertexCells`), and `edgeCells`/`edgeVertices` each return
   exactly two, `vertexCells`/`vertexEdges` exactly four (FR-009); (b) **pixel round-trip** —
   `cellAt (cellCenter spec c) = c` and `vertexPoint`/`edgeMidpoint` land where the fixed order says
   (FR-010); (c) **determinism** — repeat-run byte-identity of every conversion's output and the pixel
   maps on a fixed scenario (FR-008); (d) **totality** — non-finite / non-positive `CellSize` and
   non-finite point coordinates return the documented fallbacks (`1.0` / `0.0` / cell `0` on an axis),
   never throwing, never a NaN coordinate (FR-010). FsCheck drives (a)/(b)/(d) over random cells, edges,
   vertices, and specs.

**Home for the logic test**: `tests/Product.Tests/` does **not** exist in this framework repo — it is the
*generated* product's project. The framework-side logic test therefore lives in `tests/Canvas.Tests/`
(references already cover the helper's dependencies — `Cell` from Canvas, `Point`/`Rect` from Scene),
while the generated product's own `Product.Tests` exercises it after scaffolding. Mirrors
collision/visibility R7/R8 exactly.

**Rationale**: gives real fail-before/pass-after evidence (Constitution V) for a deliverable that is a
template file rather than a packed library; mirrors `Feature247VisibilitySkillTests` (coherence) +
`VisibilityHelperTests` (logic).

**Alternatives considered**: only a coherence test — rejected: it would leave the adjacency/pixel logic
(and its round-trip/determinism/totality contracts) unverified.
