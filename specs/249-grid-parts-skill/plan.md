# Implementation Plan: Grid-Parts Skill + Import-and-Adapt Helper Source

**Branch**: `249-grid-parts-skill` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/249-grid-parts-skill/spec.md`

## Summary

Ship a **grid-parts capability** to generated `game`/`sample-pack` products as two coordinated,
additive deliverables — the same shape as the collision (246) and visibility (247) features: no new
framework package, no change to any existing public surface.

1. **A dedicated skill `fs-gg-grids`** — the authored guidance an agent loads whenever a task involves
   grid **edges**, **corners**, boundaries, autotiling, or snapping: the parts vocabulary of a square
   grid — **faces** (cells/tiles), **edges** (the shared boundary between two faces), and **vertices**
   (the corners where edges meet) — from the Red Blob Games references "Parts of a grid"
   (<https://www.redblobgames.com/grids/parts/>) and "Grid edges"
   (<https://www.redblobgames.com/grids/edges/>); the **one canonical coordinate per part**; the six
   **part-to-part adjacency conversions**; the **pixel mapping** to/from `Point`/`Rect`; and the
   applications (edge-walls, autotiling / marching-squares over vertices, region borders, cursor
   snapping). Registered exactly like the sibling skills
   `fs-gg-collision`/`fs-gg-visibility`/`fs-gg-audio`/`fs-gg-game-core`/`fs-gg-persistence` (manifest
   catalog + `template.json` source + dev-skill roots + wrapper), gated to
   `profile in [game, sample-pack]`.
2. **An import-and-adapt helper source fragment** — a product-owned, adaptable F# file `Grids.fs` the
   scaffold materializes into the game/sample-pack product's `src/<ProductDir>/`. It **reuses** the
   shared `FS.GG.UI.Canvas.Cell` (the **face**) and `FS.GG.UI.Scene.Point`/`Rect` (pixels) — no
   look-alike types — and **adds only the parts the shared vocabulary genuinely lacks**: an `Edge`
   (orientation + col/row, one canonical name per boundary), a `Vertex` (a grid corner), a `GridSpec`
   (cell size + origin pixel policy), the **six adjacency conversions**, and the **pixel mapping**
   (cell rect/center, vertex point, edge segment/midpoint, and the inverse pixel→cell lookup). The
   consumer **owns** the copy: move the origin, add a diagonal-edge variant, reorder the corners, extend
   toward hex, or delete it — the product still builds and no governance gate hard-pins it (the Feature
   220 starter-scene lesson). This uses the **same third delivery mode** collision introduced —
   product-owned adaptable source alongside package-referenced APIs and the single-instance scaffold
   starter.

**Technical approach**: additive template + docs work, no `src/` framework library and no new `.fsi`.
The helper reuses `Cell` (Canvas, already referenced on exactly the `game`/`sample-pack` gate) and
`Point`/`Rect` (Scene, always referenced), so it needs **no new package reference**. The helper's
`<Compile Include="Grids.fs" Condition="Exists('Grids.fs')" />` is added to `Product.fsproj` under the
same profile gate that already carries `WindowOptions.fs`, `Collision.fs`, `Visibility.fs`, and the
Canvas reference — profile-gated at scaffold time, `Exists`-guarded at build time so deletion is safe
(FR-007). The part-addressing is **pure integer arithmetic** — no floating-point tie-break, no
`Dictionary`/`HashSet` iteration, no `atan2`/`sqrt` — so parts are replay-deterministic and bit-identical
inside the fixed-step loop this tier already ships (FR-008); the pixel mapping is straight-line float
arithmetic **guarded against non-finite / non-positive input** (non-finite / ≤0 `CellSize` → fallback
`1.0`; non-finite origin/coordinate → `0.0`) so it is total and never emits a NaN coordinate (FR-010).
Shipping is a **Tier 1 template-contract change** (it alters the `fs-gg-ui-template` emitted-file set: a
new skill, a new source file, a new compile item); on release the coherent set bumps and the cross-repo
registry/compatibility is updated publish-before-flip (FR-013), consistent with how the sibling skill
features (243/244/246/247) released.

> **Standing assumption — no unverified root-cause hypotheses here.** This is greenfield *additive*
> template/skill surface, not a defect fix, so there are no root-cause hypotheses to confirm. The
> "does it actually work end-to-end" obligation is met by (a) a **quickstart** that scaffolds a game
> product, builds it, edits the grid origin, and deletes the file to prove each acceptance scenario,
> and (b) `/speckit-tasks` scheduling an early **generated-product smoke** in the Foundational phase:
> materialize a `game` product, confirm `Grids.fs` is present + compiles, run a cell → edges/corners
> conversion and a `cellAt (cellCenter c)` round-trip, then delete it and confirm the build still
> succeeds — before the skill prose is finalized.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution: exclusive stack, net10.0 default). The helper
source is ordinary product F#; there is no new framework library.

**Primary Dependencies**: none new. `Grids.fs` uses only `FS.GG.UI.Canvas.Cell` (already referenced on
the game/sample-pack gate) and `FS.GG.UI.Scene.Point`/`Rect` (always referenced). Skill/manifest tooling:
`scripts/generate-skill-manifest.fsx`, `template/lifecycle/materialize-skill-roots.fsx`,
`scripts/check-agent-skill-parity.fsx`, `scripts/validate-lifecycle-template.fsx`.

**Storage**: N/A (pure source + docs; no persistence).

**Testing**: Expecto + FsCheck. New: coherence/materialize gate test
(`tests/Package.Tests/Feature249GridsSkillTests.fs`) asserting the manifest/template.json/parity all agree
and the fragment materializes only for game/sample-pack; and a grid-parts test (adjacency round-trip +
pixel round-trip + determinism + degenerate totality) that compiles the raw `Grids.fs` body (default
`sourceName` = `Product`) under `tests/Canvas.Tests/` (already references Canvas + Scene). FSI
prelude/quickstart exercises the helper the way a game consumer would. Registry count-bump assertions live
in the existing `Feature231`/`Feature238`/`Feature204`/`Feature219` gate tests (see Project Structure).

**Target Platform**: cross-platform .NET; the helper carries no GL/window/viewer dependency.

**Project Type**: FS.GG.UI template capability (skill + scaffold source fragment) within this framework
repo. No new packable project.

**Performance Goals**: the adjacency conversions are O(1) integer record construction (a fixed-length
list per call); the pixel mapping is O(1) float arithmetic. No hot-path allocation beyond the small
fixed-length part lists. No broad-phase or per-frame scan is introduced — grid-parts addressing is direct
arithmetic, not a search.

**Constraints**: pure, deterministic, total. **Bit-identical output under identical inputs** is the
load-bearing constraint (FR-008): the part-addressing is **integer arithmetic** with a fixed, documented
list order per conversion — no floating-point tie-break, no `Dictionary`/`HashSet` iteration, no
`atan2`/`sqrt`; the pixel arithmetic is non-finite-guarded. Two further round-trip invariants are
load-bearing: **adjacency round-trip** — every edge/corner a cell reports reports that cell back
(`edgeCells`/`vertexCells`, FR-009) — and **pixel round-trip** — `cellAt (cellCenter c) = c` (FR-010).
Reuse the shared `Cell`/`Point`/`Rect`; introduce **no** look-alike cell/point type — `Edge`/`Vertex` are
new *parts*, not re-rolls (FR-006). The helper must be edit-and-delete safe (FR-007) and never
governance-pinned. Additive only — no change to any existing type, signature, behavior, or the
default-profile emitted set beyond the gated additions.

**Scale/Scope**: one new skill (`SKILL.md` + its coherent registration set), one new fragment source file
(`Grids.fs` + fragment `README.md`), one `Product.fsproj` gated compile item, two new tests, several
registry count-bump edits, one FSI/quickstart transcript, `scaffold-map.md`/model-swap taxonomy updates,
and (on release) the cross-repo template-contract flip. **Like visibility (247), there is no sibling-skill
trim** — no pre-existing grid-parts write-up lives anywhere to consolidate (the existing `Cell` is a
pathfinding face coordinate only; there is no `Edge`/`Vertex` or part-to-part conversion in the
framework).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — ✅ Honored *in the applicable sense*. This
  feature adds **no framework public API / `.fsi`** (the helper is product-owned source, not a packed
  library), so there is no new `.fsi` to draft. The analogue is honored: the helper's intended source
  surface (types + functions the consumer receives) is drafted in `contracts/` first, exercised via the
  quickstart/FSI transcript, covered by an adjacency/pixel round-trip test that fails before the file
  exists and passes after, then implemented as `Grids.fs`.
- **II. Visibility Lives in `.fsi`, Not in `.fs`** — ✅ N/A to a product-owned source file with no
  package surface (there is nothing to hide behind an `.fsi`; the consumer owns and reads the whole file).
  No existing `.fsi` changes, so **no surface-area baseline is added or regenerated** — this feature ships
  no new package public surface. (This principle about `.fsi` member visibility is unrelated to the
  *domain* name "grids" and simply does not apply to a package-less source fragment.)
- **III. Idiomatic Simplicity Is the Default** — ✅ Plain pure F#: shared `Cell`/`Point`/`Rect`, two
  small new records (`Edge`/`Vertex`) and a `GridSpec`, six integer-arithmetic adjacency conversions, and
  a handful of float pixel maps. Determinism by *design* (integer part-addressing; fixed list order),
  not by an exotic feature. No custom operators, SRTP, reflection, type providers, or non-trivial
  computation expressions. **No justification-required feature is used.**
- **IV. Elmish/MVU Is the Boundary for Stateful/I-O Workflows** — ✅ N/A by design: the helper is *pure*
  and stateless. It is called from the **consumer's** `update`/`view`; it owns no state and requests no
  effects, so no `Model/Msg/Cmd` boundary applies. This is the intended shape, not an omission.
- **V. Test Evidence Is Mandatory** — ✅ Real evidence: the grid-parts test fails before `Grids.fs`
  exists and passes after; adjacency round-trip is an FsCheck property (every edge/corner a cell reports
  reports that cell back); pixel round-trip is a `cellAt (cellCenter c) = c` property; determinism is a
  repeat-run byte-identity check; the coherence test fails on any registry drift. The quickstart proves
  the delete-safe and edit-changes-behavior scenarios on a real generated product. No synthetic evidence.
- **VI. Observability and Safe Failure** — ✅ Pure helper has no I/O to log. "Safe failure" is met by
  **totality** (FR-010): non-finite / non-positive `CellSize` and non-finite point coordinates return
  documented values (fallback cell size `1.0`; axis coordinate `0.0`) — never a throw, never a NaN
  coordinate — and by build-time **delete safety** (`Condition="Exists(...)"`).
- **Change Classification** — **Tier 1 (contracted change)**: it changes the `fs-gg-ui-template`
  emitted-file contract (new skill, new source file, new compile item) even though it adds **no F#
  package public surface**. Full artifact chain required (spec, plan, contracts, tests, docs, registry
  coherence) plus, on release, the coherent-set bump and cross-repo registry/compatibility flip —
  scheduled by `/speckit-tasks`.

**Result: PASS.** No violations; Complexity Tracking table not required. (Contrast feature 245, which
*did* add framework package public surface and therefore a surface-area baseline; this feature adds none.)

## Project Structure

### Documentation (this feature)

```text
specs/249-grid-parts-skill/
├── plan.md              # This file
├── research.md          # Phase 0 — delivery mode, compile-order + delete-safety, part-addressing determinism, round-trips, vocabulary reuse, contract class
├── data-model.md        # Phase 1 — grid-parts value shapes (EdgeOrientation/Edge/Vertex/GridSpec) + reused Cell/Point/Rect + total-function conventions
├── quickstart.md        # Phase 1 — scaffold a game product, build, edit origin/cell size, delete-and-still-build
├── contracts/           # Phase 1 — the intended helper source surface + the skill/registry entries
│   ├── grids-helper-source.md         # types + functions the consumer receives in Grids.fs
│   └── skill-and-registration.md      # SKILL.md shape + the exact gate-enforced registry touch-points/conditions
├── checklists/
│   └── requirements.md  # spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 — /speckit-tasks
```

### Source Code / template (repository root)

```text
template/product-skills/fs-gg-grids/
└── SKILL.md                         # NEW — the dedicated grid-parts skill (parts vocabulary→canonical coords→adjacency→pixel-map, applications, footguns, pointer to Grids.fs, two Red Blob Games citations)

template/fragments/grids/
├── README.md                        # NEW — fragment stub: consumer-owned, adaptable source
└── src/Product/Grids.fs             # NEW — the import-and-adapt helper (sourceName-substituted 'Product')

template/base/src/Product/Product.fsproj           # EDIT — add gated `<Compile Include="Grids.fs" Condition="Exists('Grids.fs')" />`
                                                    #        under (profile == "game" || profile == "sample-pack"), before Model.fs (next to Collision.fs / Visibility.fs)

scripts/generate-skill-manifest.fsx                # EDIT — add fs-gg-grids to the `catalog` list (alphabetically, after fs-gg-game-core, before fs-gg-keyboard-input)
template/skill-manifest/skill-manifest.json        # REGEN — new fs-gg-grids entry (id+sha256+materializes-when+supplied-by)
.template.config/template.json                     # EDIT — two gated sources: skill → .agents/skills/fs-gg-grids/ (copyOnly), fragment → source template/fragments/grids/src/, target src/ (Product/ stays source-relative for fileRename)

template/base/docs/scaffold-map.md                 # EDIT — classify Grids.fs as replaceable/adaptable (consumer-owned)
template/product-skills/fs-gg-model-swap/SKILL.md  # EDIT — add Grids.fs to the "Replaceable — rewrite freely" list (FR-012 swap-guidance reach); retriggers its manifest sha256 → REGEN manifest

# Gate-enforced registry coherence (the "easy-to-miss" coherent set — see [[adding-a-product-skill-touchpoints]]):
tests/Package.Tests/Feature231SkillManifestTests.fs        # EDIT — add ("fs-gg-grids", ".../SKILL.md") to `canonicalSources`
tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs # EDIT — add the same to `canonicalSources`
tests/Package.Tests/Feature204LifecycleTemplateTests.fs    # EDIT — framework product-skill count 16 → 17
tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs  # EDIT — add fs-gg-grids to the `game` AND `sample-pack` expected sets (+ narration/source count)
scripts/validate-lifecycle-template.fsx                    # EDIT — `frameworkChecked = 16` → `17`

# Dev roots + wrapper + mirror (Deterministic gate → Rendering.Harness.Tests skill-parity):
.agents/skills/fs-gg-grids/SKILL.md                 # NEW (dev root) — canonical body (byte-identical to product-skills body)
.claude/skills/fs-gg-grids/SKILL.md                 # NEW (mirror) — via materialize-skill-roots.fsx
.agents/skills/fs-gg-product-grids/SKILL.md         # NEW — Codex-active thin wrapper (name: fs-gg-product-grids, points at canonical)
.claude/skills/fs-gg-product-grids/SKILL.md         # NEW — Claude-active thin wrapper
docs/reports/skills-parity.md                       # REGEN — after `dotnet run --project tools/Rendering.Harness -- skill-parity` (0 findings)

tests/Package.Tests/Feature249GridsSkillTests.fs       # NEW — manifest/template.json/parity coherence + profile gating
tests/Canvas.Tests/GridsHelperTests.fs                 # NEW — adjacency round-trip + pixel round-trip + determinism + degenerate totality (compiles raw Grids.fs; Canvas.Tests already refs Canvas+Scene)
scripts/grids-parts-prelude.fsx                        # FSI transcript exercising the helper as a game consumer would

# On release (Tier 1 template-contract change, publish-before-flip) — in FS-GG/.github:
registry/dependencies.yml            # fs-gg-ui-template contract version + consuming edge bumped
registry/CHANGELOG.md                # one dated newest-first entry
docs/registry/compatibility.md       # dependency-graph + versioned-contracts row + coherence row
```

**Structure Decision**: Deliver as a **skill + scaffold-source fragment pair**, not a framework package
— identical to collision (246) and visibility (247). The shared grid vocabulary (`Cell` for the face,
`Point`/`Rect` for pixels) already ships as package API; the missing pieces are the `Edge`/`Vertex` parts,
the adjacency conversions, and the pixel mapping — which are game-shaped code the consumer edits (move the
origin, add a diagonal-edge variant, reorder corners, extend to hex), so they belong in **consumer-owned
adaptable source**, not a frozen `.fsi`. The skill registers exactly like the sibling skill-only
capabilities; the source ships via a new `template/fragments/grids/` (modeled on
`template/fragments/visibility/`) with a profile-gated, `Exists`-guarded compile item so it materializes
only for game/sample-pack and stays edit-and-delete safe. Neither `capabilities.yml` nor
`skillist-reference.md` is touched — those enumerate only package/fragment-backed *package* capabilities /
a curated subset respectively, and this skill (like `fs-gg-collision`/`fs-gg-visibility`/`fs-gg-game-core`)
registers through the manifest/template/dev-root path instead (confirmed against the
collision/visibility precedent, whose skills are **absent** from both files).

> **Fragment target note (the Feature 246→247 fix).** The fragment `template.json` source is
> `source: template/fragments/grids/src/`, `target: src/` — **not** `target: src/Product/`. The
> `Product/` path segment must stay **source-relative** so the engine's `fileRename` (sourceName
> substitution `Product` → `<ProductDir>`) rewrites it to `src/<ProductDir>/Grids.fs`. An explicit
> `target: src/Product/` orphans the file in a literal `src/Product/` directory that never compiles.
> (Captured in [[fragment-target-sourcename-rename]].)

## Complexity Tracking

*No Constitution Check violations — table intentionally omitted.*
