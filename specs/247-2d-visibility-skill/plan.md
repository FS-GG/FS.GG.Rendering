# Implementation Plan: 2D Visibility Skill + Import-and-Adapt Helper Source

**Branch**: `247-2d-visibility-skill` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/247-2d-visibility-skill/spec.md`

## Summary

Ship a **2D visibility capability** to generated `game`/`sample-pack` products as two coordinated,
additive deliverables — the same shape as the collision feature (246): no new framework package, no
change to any existing public surface.

1. **A dedicated skill `fs-gg-visibility`** — the authored guidance an agent loads for the whole
   visibility pipeline: the segment (wall) world model over the shared `Point`, broad-phase culling of
   nearby occluders (reusing `SpatialGrid.queryRadius` in `FS.GG.UI.Canvas`), the **angular-sweep
   visibility algorithm** from the Red Blob Games reference
   (<https://www.redblobgames.com/articles/visibility/>) — collect endpoints, sort by angle, sweep the
   nearest crossing segment — and the visibility-polygon output plus its applications (line-of-sight,
   field-of-view, fog-of-war, 2D light/shadow). Registered exactly like the sibling skills
   `fs-gg-collision`/`fs-gg-audio`/`fs-gg-game-core`/`fs-gg-persistence` (manifest catalog +
   `template.json` source + dev-skill roots + wrapper), gated to `profile in [game, sample-pack]`.
2. **An import-and-adapt helper source fragment** — a product-owned, adaptable F# file `Visibility.fs`
   the scaffold materializes into the game/sample-pack product's `src/<ProductDir>/`. It composes the
   existing shared geometry primitives into a per-frame visibility pass and adds the two pieces the
   framework deliberately does *not* freeze into a package — **ray-segment intersection** and the
   **angular sweep** (the direct analogue of collision *response*). The consumer **owns** the copy: edit
   the sight radius, add a field-of-view cone, switch the output from a polygon to a per-cell mask, or
   delete it — the product still builds and no governance gate hard-pins it (the Feature 220 starter-scene
   lesson). This uses the **same third delivery mode** collision introduced — product-owned adaptable
   source alongside package-referenced APIs and the single-instance scaffold starter.

**Technical approach**: additive template + docs work, no `src/` framework library and no new `.fsi`.
The helper reuses `Point`/`Rect`/`Geometry` (Scene, always referenced) and `SpatialGrid` (Canvas,
already referenced on exactly the `game`/`sample-pack` gate), so it needs **no new package reference**.
The helper's `<Compile Include="Visibility.fs" Condition="Exists('Visibility.fs')" />` is added to
`Product.fsproj` under the same profile gate that already carries `WindowOptions.fs`, `Collision.fs`, and
the Canvas reference — profile-gated at scaffold time, `Exists`-guarded at build time so deletion is safe
(FR-007). The sweep is written to be a **pure function of world state** — endpoints ordered by a
**cross-product angular comparator** (no `atan2`) with an integer endpoint-index tiebreak, and
nearest-hit chosen by a sqrt-free parametric distance — so it is replay-deterministic and bit-identical
inside the fixed-step loop this tier already ships (FR-008, FR-011). Shipping is a **Tier 1
template-contract change** (it alters the `fs-gg-ui-template` emitted-file set: a new skill, a new source
file, a new compile item); on release the coherent set bumps and the cross-repo registry/compatibility is
updated publish-before-flip (FR-014), consistent with how the sibling skill features (243/244/246)
released.

> **Standing assumption — no unverified root-cause hypotheses here.** This is greenfield *additive*
> template/skill surface, not a defect fix, so there are no root-cause hypotheses to confirm. The
> "does it actually work end-to-end" obligation is met by (a) a **quickstart** that scaffolds a game
> product, builds it, edits the sight radius, and deletes the file to prove each acceptance scenario,
> and (b) `/speckit-tasks` scheduling an early **generated-product smoke** in the Foundational phase:
> materialize a `game` product, confirm `Visibility.fs` is present + compiles, run its sweep on a source
> with an occluder between it and a target (assert the target is hidden), then delete it and confirm the
> build still succeeds — before the skill prose is finalized.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution: exclusive stack, net10.0 default). The helper
source is ordinary product F#; there is no new framework library.

**Primary Dependencies**: none new. `Visibility.fs` uses only `FS.GG.UI.Scene.Point`/`Rect`/`Geometry`
(always referenced) and `FS.GG.UI.Canvas.SpatialGrid` (already referenced on the game/sample-pack gate).
Skill/manifest tooling: `scripts/generate-skill-manifest.fsx`, `template/lifecycle/materialize-skill-roots.fsx`,
`scripts/check-agent-skill-parity.fsx`, `scripts/validate-lifecycle-template.fsx`.

**Storage**: N/A (pure source + docs; no persistence).

**Testing**: Expecto + FsCheck. New: coherence/materialize gate test
(`tests/Package.Tests/Feature247VisibilitySkillTests.fs`) asserting the manifest/template.json/parity all
agree and the fragment materializes only for game/sample-pack; and a visibility-logic test (determinism +
occlusion correctness + degenerate totality) that compiles the raw `Visibility.fs` body (default
`sourceName` = `Product`) under `tests/Canvas.Tests/` (already references Canvas + Scene). FSI
prelude/quickstart exercises the helper the way a game consumer would. Registry count-bump assertions live
in the existing `Feature231`/`Feature238`/`Feature204`/`Feature219` gate tests (see Project Structure).

**Target Platform**: cross-platform .NET; the helper carries no GL/window/viewer dependency.

**Project Type**: FS.GG.UI template capability (skill + scaffold source fragment) within this framework
repo. No new packable project.

**Performance Goals**: broad-phase cull over `SpatialGrid.queryRadius` limits the sweep to occluders
within the sight radius, avoiding an O(segments) scan of the whole world per source; the sweep is
O(k log k) in the culled endpoint count k. No hot-path allocation beyond the endpoint/vertex lists.

**Constraints**: pure, deterministic, total, **bounded**. **Bit-identical resolved output under
identical inputs** is the load-bearing constraint (FR-008): the angular sort uses a cross-product
comparator (no `atan2` transcendental) with an integer endpoint-index final tiebreak, and nearest-hit
selection uses a sqrt-free parametric distance — no reliance on `Dictionary`/`HashSet` iteration order or
frame-arrival order. Every ray is **bounded** by the sight radius so an unhit ray terminates on the bound,
never loops (FR-011). Reuse the shared `Point`/`Rect`; introduce **no** look-alike point/vector type
(FR-009). The helper must be edit-and-delete safe (FR-007) and never governance-pinned (FR-013).
Additive only — no change to any existing type, signature, behavior, or the default-profile emitted set
beyond the gated additions.

**Scale/Scope**: one new skill (`SKILL.md` + its coherent registration set), one new fragment source file
(`Visibility.fs` + fragment `README.md`), one `Product.fsproj` gated compile item, two new tests, several
registry count-bump edits, one FSI/quickstart transcript, `scaffold-map.md`/model-swap taxonomy updates,
and (on release) the cross-repo template-contract flip. **Unlike collision (246), there is no sibling-skill
trim** — no pre-existing visibility write-up lives in `fs-gg-game-core` to consolidate.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — ✅ Honored *in the applicable sense*. This
  feature adds **no framework public API / `.fsi`** (the helper is product-owned source, not a packed
  library), so there is no new `.fsi` to draft. The analogue is honored: the helper's intended source
  surface (types + functions the consumer receives) is drafted in `contracts/` first, exercised via the
  quickstart/FSI transcript, covered by a determinism/occlusion test that fails before the file exists and
  passes after, then implemented as `Visibility.fs`.
- **II. Visibility Lives in `.fsi`, Not in `.fs`** — ✅ N/A to a product-owned source file with no
  package surface (there is nothing to hide behind an `.fsi`; the consumer owns and reads the whole file).
  No existing `.fsi` changes, so **no surface-area baseline is added or regenerated** — this feature ships
  no new package public surface. (The name of the *domain* is "visibility"; this principle about `.fsi`
  member visibility is unrelated and simply does not apply to a package-less source fragment.)
- **III. Idiomatic Simplicity Is the Default** — ✅ Plain pure F#: shared `Point`/`Rect`, a small
  ray-segment intersection, a cross-product rotational sort, and a linear sweep. Determinism by *design*
  (cross-product comparator + integer tiebreak; sqrt-free parametric distance), not by an exotic feature.
  No custom operators, SRTP, reflection, type providers, or non-trivial computation expressions. **No
  justification-required feature is used.**
- **IV. Elmish/MVU Is the Boundary for Stateful/I-O Workflows** — ✅ N/A by design: the helper is *pure*
  and stateless. It is called from the **consumer's** `update`/`view`; it owns no state and requests no
  effects, so no `Model/Msg/Cmd` boundary applies. This is the intended shape, not an omission.
- **V. Test Evidence Is Mandatory** — ✅ Real evidence: the visibility-logic test fails before
  `Visibility.fs` exists and passes after; determinism is a repeat-run byte-identity property test;
  occlusion is a "target hidden behind a wall / visible with the wall removed" test; the coherence test
  fails on any registry drift. The quickstart proves the delete-safe and edit-changes-behavior scenarios
  on a real generated product. No synthetic evidence.
- **VI. Observability and Safe Failure** — ✅ Pure helper has no I/O to log. "Safe failure" is met by
  **totality** (FR-010): zero-length segment, source on a wall/endpoint, collinear/near-parallel grazing
  ray, coincident endpoints, and empty segment set return documented values (never a throw, never a NaN
  coordinate) — and by build-time **delete safety** (`Condition="Exists(...)"`).
- **Change Classification** — **Tier 1 (contracted change)**: it changes the `fs-gg-ui-template`
  emitted-file contract (new skill, new source file, new compile item) even though it adds **no F#
  package public surface**. Full artifact chain required (spec, plan, contracts, tests, docs, registry
  coherence) plus, on release, the coherent-set bump and cross-repo registry/compatibility flip —
  scheduled by `/speckit-tasks`.

**Result: PASS.** No violations; Complexity Tracking table not required.

## Project Structure

### Documentation (this feature)

```text
specs/247-2d-visibility-skill/
├── plan.md              # This file
├── research.md          # Phase 0 — delivery mode, compile-order + delete-safety, sweep determinism, bound, contract class
├── data-model.md        # Phase 1 — visibility value shapes (Segment/Settings/VisibilityPolygon) + total-function conventions
├── quickstart.md        # Phase 1 — scaffold a game product, build, edit sight radius, delete-and-still-build
├── contracts/           # Phase 1 — the intended helper source surface + the skill/registry entries
│   ├── visibility-helper-source.md   # types + functions the consumer receives in Visibility.fs
│   └── skill-and-registration.md     # SKILL.md shape + the exact gate-enforced registry touch-points/conditions
├── checklists/
│   └── requirements.md  # spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 — /speckit-tasks (NOT created here)
```

### Source Code / template (repository root)

```text
template/product-skills/fs-gg-visibility/
└── SKILL.md                         # NEW — the dedicated visibility skill (segment model→cull→sweep→polygon, applications, footguns, pointer to Visibility.fs, Red Blob Games citation)

template/fragments/visibility/
├── README.md                        # NEW — fragment stub: consumer-owned, adaptable source
└── src/Product/Visibility.fs        # NEW — the import-and-adapt helper (sourceName-substituted 'Product')

template/base/src/Product/Product.fsproj           # EDIT — add gated `<Compile Include="Visibility.fs" Condition="Exists('Visibility.fs')" />`
                                                    #        under (profile == "game" || profile == "sample-pack"), before Model.fs (next to Collision.fs)

scripts/generate-skill-manifest.fsx                # EDIT — add fs-gg-visibility to the `catalog` list (after fs-gg-ui-widgets)
template/skill-manifest/skill-manifest.json        # REGEN — new fs-gg-visibility entry (id+sha256+materializes-when+supplied-by)
.template.config/template.json                     # EDIT — two gated sources: skill → .agents/skills/fs-gg-visibility/ (copyOnly), fragment → src/<ProductDir>/

template/base/docs/scaffold-map.md                 # EDIT — classify Visibility.fs as replaceable/adaptable (consumer-owned)
template/product-skills/fs-gg-model-swap/SKILL.md  # EDIT — add Visibility.fs to the "Replaceable — rewrite freely" list (FR-013 swap-guidance reach); retriggers its manifest sha256 → REGEN manifest

# Gate-enforced registry coherence (the "easy-to-miss" coherent set — see [[adding-a-product-skill-touchpoints]]):
tests/Package.Tests/Feature231SkillManifestTests.fs        # EDIT — add ("fs-gg-visibility", ".../SKILL.md") to `canonicalSources`
tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs # EDIT — add the same to `canonicalSources`
tests/Package.Tests/Feature204LifecycleTemplateTests.fs    # EDIT — framework product-skill count 15 → 16
tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs  # EDIT — add fs-gg-visibility to the `game` AND `sample-pack` expected sets (+ any .agents source count)
scripts/validate-lifecycle-template.fsx                    # EDIT — `frameworkChecked = 15` → `16`

# Dev roots + wrapper + mirror (Deterministic gate → Rendering.Harness.Tests skill-parity):
.agents/skills/fs-gg-visibility/SKILL.md            # NEW (dev root) — canonical body (byte-identical to product-skills body)
.claude/skills/fs-gg-visibility/SKILL.md            # NEW (mirror) — via materialize-skill-roots.fsx
.agents/skills/fs-gg-product-visibility/SKILL.md    # NEW — Codex-active thin wrapper (name: fs-gg-product-visibility, points at canonical)
.claude/skills/fs-gg-product-visibility/SKILL.md    # NEW — Claude-active thin wrapper
docs/reports/skills-parity.md                       # REGEN — after `dotnet run --project tools/Rendering.Harness -- skill-parity` (0 findings)

tests/Package.Tests/Feature247VisibilitySkillTests.fs  # NEW — manifest/template.json/parity coherence + profile gating
tests/Canvas.Tests/VisibilityHelperTests.fs            # NEW — occlusion correctness + repeat-run determinism + degenerate totality (compiles raw Visibility.fs; Canvas.Tests already refs Canvas+Scene)
scripts/*-prelude.fsx                                  # FSI transcript exercising the helper as a game consumer would

# On release (Tier 1 template-contract change, publish-before-flip) — in FS-GG/.github:
registry/dependencies.yml            # fs-gg-ui-template contract version + consuming edge bumped
registry/CHANGELOG.md                # one dated newest-first entry
docs/registry/compatibility.md       # dependency-graph + versioned-contracts row + coherence row
```

**Structure Decision**: Deliver as a **skill + scaffold-source fragment pair**, not a framework package
— identical to collision (246). The shared geometry vocabulary (`Point`/`Rect`/`Geometry`/`SpatialGrid`)
already ships as package API; the missing pieces are ray-segment intersection and the angular sweep, which
are game-shaped code the consumer edits (change the radius, cone the FOV, output a mask), so they belong
in **consumer-owned adaptable source**, not a frozen `.fsi`. The skill registers exactly like the
sibling skill-only capabilities; the source ships via a new `template/fragments/visibility/` (modeled on
`template/fragments/collision/`) with a profile-gated, `Exists`-guarded compile item so it materializes
only for game/sample-pack and stays edit-and-delete safe. Neither `capabilities.yml` nor
`skillist-reference.md` is touched — those enumerate only package/fragment-backed *package* capabilities /
a curated subset respectively, and this skill (like `fs-gg-collision`/`fs-gg-game-core`) registers through
the manifest/template/dev-root path instead (confirmed against the collision precedent, whose skill is
**absent** from both files).

## Complexity Tracking

*No Constitution Check violations — table intentionally omitted.*
