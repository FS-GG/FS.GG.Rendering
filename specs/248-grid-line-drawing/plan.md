# Implementation Plan: Grid Line-Drawing Skill + Import-and-Adapt Helper Source

**Branch**: `248-grid-line-drawing` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/248-grid-line-drawing/spec.md`

## Summary

Ship a **grid line-drawing capability** to generated `game`/`sample-pack` products as two coordinated,
additive deliverables — the same shape as collision (246) and visibility (247): no new framework package,
no change to any existing public surface.

1. **A dedicated skill `fs-gg-line-drawing`** — the authored guidance an agent loads for the whole
   line-drawing capability: the `Cell` grid model (reusing the integer `Cell` from `FS.GG.UI.Canvas`,
   feature 245), the **Bresenham cell line**, the **supercover** (no-diagonal-gap) variant, and a grid
   **line-of-sight** query over a `Cell -> bool` transparency predicate (reusing the `Pathfinding`
   predicate convention) — from the Red Blob Games reference
   (<https://www.redblobgames.com/grids/line-drawing/>). Registered exactly like the sibling skills
   `fs-gg-collision`/`fs-gg-visibility`/`fs-gg-game-core`/`fs-gg-audio` (manifest catalog + `template.json`
   source + dev-skill wrappers), gated to `profile in [game, sample-pack]`.
2. **An import-and-adapt helper source fragment** — a product-owned, adaptable F# file `LineDrawing.fs`
   the scaffold materializes into the game/sample-pack product's `src/<ProductDir>/`. It composes the
   existing shared `Cell` grid coordinate into a per-call cell walk and adds the piece the framework
   deliberately does *not* freeze into a package — the **integer Bresenham walk** (`line`), the
   **supercover** walk, and the **`lineOfSight`** convenience (the direct analogue of collision *response*
   and the visibility *sweep*). The consumer **owns** the copy: switch thin→supercover, add stop-at-first-
   blocked LOS over their own map, cap the line length, or delete it — the product still builds and no
   governance gate hard-pins it (the Feature 220 starter-scene lesson). This uses the **same third delivery
   mode** collision introduced — product-owned adaptable source alongside package-referenced APIs and the
   single-instance scaffold starter.

**Technical approach**: additive template + docs work, no `src/` framework library and no new `.fsi`.
The helper reuses `Cell` (`FS.GG.UI.Canvas`, already referenced on exactly the `game`/`sample-pack`
gate), so it needs **no new package reference**. The helper's
`<Compile Include="LineDrawing.fs" Condition="Exists('LineDrawing.fs')" />` is added to `Product.fsproj`
under the same profile gate that already carries `WindowOptions.fs`, `Collision.fs`, `Visibility.fs`, and
the Canvas reference — profile-gated at scaffold time, `Exists`-guarded at build time so deletion is safe
(FR-007). The walk is written to be a **pure function of its two endpoints** — **integer Bresenham** with
no floating-point interpolation (so there is no rounding-mode drift), no `Dictionary`/`HashSet` iteration
order dependence, and no frame-arrival order dependence — so it is replay-deterministic and bit-identical
inside the fixed-step loop this tier already ships (FR-008). A cell line between two `Cell`s is inherently
finite (bounded by the endpoint separation), so — unlike a visibility ray — no extra bound is required
(FR-011). Shipping is a **Tier 1 template-contract change** (it alters the `fs-gg-ui-template` emitted-file
set: a new skill, a new source file, a new compile item); on release the coherent set bumps and the
cross-repo registry/compatibility is updated publish-before-flip (FR-014), consistent with how the sibling
skill features (243/244/246/247) released.

> **Standing assumption — no unverified root-cause hypotheses here.** This is greenfield *additive*
> template/skill surface, not a defect fix, so there are no root-cause hypotheses to confirm. The
> "does it actually work end-to-end" obligation is met by (a) a **quickstart** that scaffolds a game
> product, builds it, switches thin→supercover, and deletes the file to prove each acceptance scenario,
> and (b) `/speckit-tasks` scheduling an early **generated-product smoke** in the Foundational phase:
> materialize a `game` product, confirm `LineDrawing.fs` is present + compiles, run its walk on two cells
> (assert connectivity + endpoints) and a LOS query across a blocked cell (assert hidden), then delete it
> and confirm the build still succeeds — before the skill prose is finalized.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution: exclusive stack, net10.0 default). The helper
source is ordinary product F#; there is no new framework library.

**Primary Dependencies**: none new. `LineDrawing.fs` uses only `FS.GG.UI.Canvas.Cell` (already referenced
on the game/sample-pack gate). Skill/manifest tooling: `scripts/generate-skill-manifest.fsx`,
`template/lifecycle/materialize-skill-roots.fsx`, `scripts/check-agent-skill-parity.fsx`,
`scripts/validate-lifecycle-template.fsx`.

**Storage**: N/A (pure source + docs; no persistence).

**Testing**: Expecto + FsCheck. New: coherence/materialize gate test
(`tests/Package.Tests/Feature248LineDrawingSkillTests.fs`) asserting the manifest/template.json/parity all
agree and the fragment materializes only for game/sample-pack; and a line-logic test (determinism +
connectivity/endpoints + supercover no-gap + LOS blocked/clear + degenerate/all-octant totality) that
compiles the raw `LineDrawing.fs` body (default `sourceName` = `Product`) under `tests/Canvas.Tests/`
(already references Canvas). FSI prelude/quickstart exercises the helper the way a game consumer would.
Registry count-bump assertions live in the existing `Feature231`/`Feature238`/`Feature204`/`Feature219`
gate tests (see Project Structure).

**Target Platform**: cross-platform .NET; the helper carries no GL/window/viewer dependency.

**Project Type**: FS.GG.UI template capability (skill + scaffold source fragment) within this framework
repo. No new packable project.

**Performance Goals**: the walk is O(k) in the endpoint separation k (the number of cells on the line);
no hot-path allocation beyond the result list. No broad-phase needed (a line is inherently local).

**Constraints**: pure, deterministic, total, **bounded**. **Bit-identical output under identical
endpoints** is the load-bearing constraint (FR-008): the walk is **integer Bresenham** — no
floating-point interpolation, no rounding mode, no transcendental — and does not rely on
`Dictionary`/`HashSet` iteration order or frame-arrival order. Every line is **bounded** by the endpoint
separation so the walk always terminates (FR-011). Reuse the shared `Cell`; introduce **no** look-alike
grid-coordinate type (FR-009). The helper must be edit-and-delete safe (FR-007) and never governance-pinned
(FR-013). Additive only — no change to any existing type, signature, behavior, or the default-profile
emitted set beyond the gated additions.

**Scale/Scope**: one new skill (`SKILL.md` + its coherent registration set), one new fragment source file
(`LineDrawing.fs` + fragment `README.md`), one `Product.fsproj` gated compile item, two new tests, several
registry count-bump edits, one FSI/quickstart transcript, `scaffold-map.md`/model-swap taxonomy updates,
and (on release) the cross-repo template-contract flip. **Like visibility (247), there is no sibling-skill
trim** — no pre-existing line-drawing write-up lives in `fs-gg-game-core` to consolidate.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — ✅ Honored *in the applicable sense*. This
  feature adds **no framework public API / `.fsi`** (the helper is product-owned source, not a packed
  library), so there is no new `.fsi` to draft. The analogue is honored: the helper's intended source
  surface (types + functions the consumer receives) is drafted in `contracts/` first, exercised via the
  quickstart/FSI transcript, covered by a determinism/connectivity/LOS test that fails before the file
  exists and passes after, then implemented as `LineDrawing.fs`.
- **II. Visibility Lives in `.fsi`, Not in `.fs`** — ✅ N/A to a product-owned source file with no package
  surface (there is nothing to hide behind an `.fsi`; the consumer owns and reads the whole file). No
  existing `.fsi` changes, so **no surface-area baseline is added or regenerated** — this feature ships no
  new package public surface.
- **III. Idiomatic Simplicity Is the Default** — ✅ Plain pure F#: the shared `Cell`, a small integer
  Bresenham walk, a supercover walk, and a predicate fold. Determinism by *design* (integer arithmetic, no
  float), not by an exotic feature. No custom operators, SRTP, reflection, type providers, or non-trivial
  computation expressions. **No justification-required feature is used.**
- **IV. Elmish/MVU Is the Boundary for Stateful/I-O Workflows** — ✅ N/A by design: the helper is *pure*
  and stateless. It is called from the **consumer's** `update`/`view`; it owns no state and requests no
  effects, so no `Model/Msg/Cmd` boundary applies. This is the intended shape, not an omission.
- **V. Test Evidence Is Mandatory** — ✅ Real evidence: the line-logic test fails before `LineDrawing.fs`
  exists and passes after; determinism is a repeat-run byte-identity property test; connectivity/endpoints
  and supercover-no-gap are structural properties; LOS is a "target hidden behind a wall tile / visible
  with it removed" test; the coherence test fails on any registry drift. The quickstart proves the
  delete-safe and edit-changes-behavior scenarios on a real generated product. No synthetic evidence.
- **VI. Observability and Safe Failure** — ✅ Pure helper has no I/O to log. "Safe failure" is met by
  **totality** (FR-010): start-equals-goal, axis-aligned/diagonal lines, every octant, and an always-false/
  always-true predicate return documented values (never a throw) — and by build-time **delete safety**
  (`Condition="Exists(...)"`).
- **Change Classification** — **Tier 1 (contracted change)**: it changes the `fs-gg-ui-template`
  emitted-file contract (new skill, new source file, new compile item) even though it adds **no F# package
  public surface**. Full artifact chain required (spec, plan, contracts, tests, docs, registry coherence)
  plus, on release, the coherent-set bump and cross-repo registry/compatibility flip — scheduled by
  `/speckit-tasks`.

**Result: PASS.** No violations; Complexity Tracking table not required.

## Project Structure

### Documentation (this feature)

```text
specs/248-grid-line-drawing/
├── plan.md              # This file
├── research.md          # Phase 0 — delivery mode, compile-order + delete-safety, Bresenham determinism, bound, contract class
├── data-model.md        # Phase 1 — line-drawing value shapes (Cell reuse, cell-list output) + total-function conventions
├── quickstart.md        # Phase 1 — scaffold a game product, build, switch thin→supercover, delete-and-still-build
├── contracts/           # Phase 1 — the intended helper source surface + the skill/registry entries
│   ├── linedrawing-helper-source.md   # types + functions the consumer receives in LineDrawing.fs
│   └── skill-and-registration.md      # SKILL.md shape + the exact gate-enforced registry touch-points/conditions
├── checklists/
│   └── requirements.md  # spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 — /speckit-tasks
```

### Source Code / template (repository root)

```text
template/product-skills/fs-gg-line-drawing/
└── SKILL.md                         # NEW — the dedicated line-drawing skill (Cell model→Bresenham→supercover→LOS, applications, footguns, pointer to LineDrawing.fs, Red Blob Games citation)

template/fragments/line-drawing/
├── README.md                        # NEW — fragment stub: consumer-owned, adaptable source
└── src/Product/LineDrawing.fs       # NEW — the import-and-adapt helper (sourceName-substituted 'Product')

template/base/src/Product/Product.fsproj           # EDIT — add gated `<Compile Include="LineDrawing.fs" Condition="Exists('LineDrawing.fs')" />`
                                                    #        under (profile == "game" || profile == "sample-pack"), before Model.fs (next to Visibility.fs)

scripts/generate-skill-manifest.fsx                # EDIT — add fs-gg-line-drawing to the `catalog` list (after fs-gg-visibility)
template/skill-manifest/skill-manifest.json        # REGEN — new fs-gg-line-drawing entry (id+sha256+materializes-when+supplied-by)
.template.config/template.json                     # EDIT — two gated sources: skill → .agents/skills/fs-gg-line-drawing/ (copyOnly), fragment → src/<ProductDir>/

template/base/docs/scaffold-map.md                 # EDIT — classify LineDrawing.fs as replaceable/adaptable (consumer-owned)
template/product-skills/fs-gg-model-swap/SKILL.md  # EDIT — add LineDrawing.fs to the "Replaceable — rewrite freely" list (FR-013 swap-guidance reach); retriggers its manifest sha256 → REGEN manifest

# Gate-enforced registry coherence (the "easy-to-miss" coherent set — see adding-a-product-skill-touchpoints):
tests/Package.Tests/Feature231SkillManifestTests.fs        # EDIT — add ("fs-gg-line-drawing", ".../SKILL.md") to `canonicalSources`
tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs # EDIT — add the same to `canonicalSources`
tests/Package.Tests/Feature204LifecycleTemplateTests.fs    # EDIT — framework product-skill count 16 → 17
tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs  # EDIT — add fs-gg-line-drawing to the `game` AND `sample-pack` expected sets (+ any .agents source count)
scripts/validate-lifecycle-template.fsx                    # EDIT — `frameworkChecked = 16` → `17`

# Dev wrappers + mirror (Deterministic gate → Rendering.Harness.Tests skill-parity):
.agents/skills/fs-gg-product-line-drawing/SKILL.md  # NEW — Codex-active thin wrapper (name: fs-gg-product-line-drawing, points at canonical)
.claude/skills/fs-gg-product-line-drawing/SKILL.md  # NEW — Claude-active thin wrapper
docs/reports/skills-parity.md                       # REGEN — after `dotnet run --project tools/Rendering.Harness -- skill-parity` (0 findings)

tests/Package.Tests/Feature248LineDrawingSkillTests.fs # NEW — manifest/template.json/parity coherence + profile gating
tests/Canvas.Tests/LineDrawingHelperTests.fs           # NEW — connectivity/endpoints + supercover no-gap + LOS + repeat-run determinism + all-octant totality (compiles raw LineDrawing.fs; Canvas.Tests already refs Canvas)
scripts/line-drawing-prelude.fsx                       # FSI transcript exercising the helper as a game consumer would

# On release (Tier 1 template-contract change, publish-before-flip) — in FS-GG/.github:
registry/dependencies.yml            # fs-gg-ui-template contract version + consuming edge bumped
registry/CHANGELOG.md                # one dated newest-first entry
docs/registry/compatibility.md       # dependency-graph + versioned-contracts row + coherence row
```

**Structure Decision**: Deliver as a **skill + scaffold-source fragment pair**, not a framework package —
identical to collision (246) and visibility (247). The shared grid vocabulary (`Cell`/`Pathfinding`/
`SpatialGrid`) already ships as package API; the missing piece is the cell-line walk, which is game-shaped
code the consumer edits (thin vs supercover, cap the length, custom LOS), so it belongs in **consumer-owned
adaptable source**, not a frozen `.fsi`. The skill registers exactly like the sibling skill-only
capabilities; the source ships via a new `template/fragments/line-drawing/` (modeled on
`template/fragments/visibility/`) with a profile-gated, `Exists`-guarded compile item so it materializes
only for game/sample-pack and stays edit-and-delete safe. Neither `capabilities.yml` nor
`skillist-reference.md` is touched — those enumerate only package/fragment-backed *package* capabilities /
a curated subset respectively, and this skill (like `fs-gg-collision`/`fs-gg-visibility`) registers through
the manifest/template/dev-root path instead (confirmed against the visibility precedent, whose skill is
**absent** from both files).

## Complexity Tracking

*No Constitution Check violations — table intentionally omitted.*
