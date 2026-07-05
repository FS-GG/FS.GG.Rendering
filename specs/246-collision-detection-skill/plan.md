# Implementation Plan: Collision Detection Skill + Import-and-Adapt Helper Source

**Branch**: `246-collision-detection-skill` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/246-collision-detection-skill/spec.md`

## Summary

Ship a **collision capability** to generated `game`/`sample-pack` products as two coordinated,
additive deliverables — no new framework package, no change to any existing public surface:

1. **A dedicated skill `fs-gg-collision`** — the authored guidance an agent loads for the whole
   collision pipeline: narrow-phase detection (reusing `Geometry` in `FS.GG.UI.Scene`), broad-phase
   candidate pairing (reusing `SpatialGrid` in `FS.GG.UI.Canvas`), and **collision response**
   (penetration / minimum-translation, separation, slide/bounce restitution). Registered exactly like
   the sibling skills `fs-gg-audio`/`fs-gg-game-core`/`fs-gg-persistence` (manifest catalog +
   `template.json` source + `skillist-reference.md` + dev-skill roots), gated to `profile in [game,
   sample-pack]`. The existing `fs-gg-game-core` `## Collision` section is trimmed to a pointer at the
   new skill so collision guidance has exactly one authoritative home.
2. **An import-and-adapt helper source fragment** — a product-owned, adaptable F# file
   `Collision.fs` the scaffold materializes into the game/sample-pack product's `src/<ProductDir>/`.
   It composes the existing detection primitives into a per-frame collision pass and adds the
   response layer the framework deliberately does *not* freeze into a package. The consumer **owns**
   the copy: edit the response rule, add layers, or delete it — the product still builds and no
   governance gate hard-pins it (the Feature 220 starter-scene lesson). This is a **new, third**
   delivery mode alongside package-referenced APIs and the single-instance scaffold starter.

**Technical approach**: additive template + docs work, no `src/` framework library and no new `.fsi`.
The helper reuses `Geometry`/`Rect`/`Point` (Scene, already referenced on every profile) and
`SpatialGrid` (Canvas, already referenced on exactly the `game`/`sample-pack` gate), so it needs **no
new package reference**. The helper's `<Compile Include="Collision.fs" Condition="Exists('Collision.fs')" />`
is added to `Product.fsproj` under the same profile gate that already carries `WindowOptions.fs` and the
Canvas reference — profile-gated at scaffold time, `Exists`-guarded at build time so deletion is safe
(FR-007). Response math (fractional separation vectors) is written to be a **pure function of world
state** — no hash-container iteration order, no frame-arrival dependence — so it is replay-deterministic
inside the fixed-step loop this tier already ships (FR-008). Shipping is a **Tier 1 template-contract
change** (it alters the `fs-gg-ui-template` emitted-file set: a new skill, a new source file, a new
compile item); on release the coherent set bumps and the cross-repo registry/compatibility is updated
publish-before-flip (FR-014), consistent with how the sibling skill features (243/244) released.

> **Standing assumption — no unverified root-cause hypotheses here.** This is greenfield *additive*
> template/skill surface, not a defect fix, so there are no root-cause hypotheses to confirm. The
> "does it actually work end-to-end" obligation is met by (a) a **quickstart** that scaffolds a game
> product, builds it, edits the response rule, and deletes the file to prove each acceptance scenario,
> and (b) `/speckit-tasks` scheduling an early **generated-product smoke** in the Foundational phase:
> materialize a `game` product, confirm `Collision.fs` is present + compiles, run its collision pass on
> two overlapping bodies, then delete it and confirm the build still succeeds — before the skill prose
> is finalized.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution: exclusive stack, net10.0 default). The helper
source is ordinary product F#; there is no new framework library.

**Primary Dependencies**: none new. `Collision.fs` uses only `FS.GG.UI.Scene.Geometry`/`Rect`/`Point`
(always referenced) and `FS.GG.UI.Canvas.SpatialGrid` (already referenced on the game/sample-pack gate).
Skill/manifest tooling: `scripts/generate-skill-manifest.fsx`, `template/lifecycle/materialize-skill-roots.fsx`,
`scripts/check-agent-skill-parity.fsx`.

**Storage**: N/A (pure source + docs; no persistence).

**Testing**: Expecto + FsCheck. New: coherence/materialize gate test (`tests/Package.Tests/Feature246CollisionSkillTests.fs`)
asserting the manifest/template.json/skillist/parity all agree and the fragment materializes only for
game/sample-pack; and a collision-logic test (determinism + response correctness + degenerate totality)
that compiles the raw `Collision.fs` body (default `sourceName` = `Product`) under a test project. FSI
prelude/quickstart exercises the helper the way a game consumer would.

**Target Platform**: cross-platform .NET; the helper carries no GL/window/viewer dependency.

**Project Type**: FS.GG.UI template capability (skill + scaffold source fragment) within this framework
repo. No new packable project.

**Performance Goals**: broad-phase over `SpatialGrid` avoids O(n²) pair scans; the per-frame pass is
O(candidate pairs). No hot-path allocation beyond the pair/result lists.

**Constraints**: pure, deterministic, total. **Bit-identical resolved output under identical inputs**
is the load-bearing constraint (FR-008): response uses fractional separation but no reliance on
`Dictionary`/`HashSet` iteration order or frame-arrival order. Reuse the shared `Rect`/`Point`; introduce
**no** look-alike bounds/vector type (FR-009). The helper must be edit-and-delete safe (FR-007) and never
governance-pinned (FR-013). Additive only — no change to any existing type, signature, behavior, or the
default-profile emitted set beyond the gated additions.

**Scale/Scope**: one new skill (`SKILL.md` + 6 registry touch-points), one new fragment source file
(`Collision.fs` + fragment `README.md`), one `Product.fsproj` gated compile item, one trimmed
`fs-gg-game-core` section, two new tests, one FSI/quickstart transcript, `scaffold-map.md`/model-swap
taxonomy updates, and (on release) the cross-repo template-contract flip.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — ✅ Honored *in the applicable sense*. This
  feature adds **no framework public API / `.fsi`** (the helper is product-owned source, not a packed
  library), so there is no new `.fsi` to draft. The analogue is honored: the helper's intended source
  surface (types + functions the consumer receives) is drafted in `contracts/` first, exercised via the
  quickstart/FSI transcript, covered by a determinism/response test that fails before the file exists and
  passes after, then implemented as `Collision.fs`.
- **II. Visibility Lives in `.fsi`, Not in `.fs`** — ✅ N/A to a product-owned source file with no
  package surface (there is nothing to hide behind an `.fsi`; the consumer owns and reads the whole
  file). No existing `.fsi` changes, so **no surface-area baseline is added or regenerated** — this
  feature ships no new package public surface (contrast Feature 245, which did).
- **III. Idiomatic Simplicity Is the Default** — ✅ Plain pure F#: shared `Rect`/`Point`, a small
  penetration/MTV computation, and a documented response rule. Determinism by *design* (stable pair
  ordering + no float ties in the ordering key), not by an exotic feature. No custom operators, SRTP,
  reflection, type providers, or non-trivial computation expressions. **No justification-required
  feature is used.**
- **IV. Elmish/MVU Is the Boundary for Stateful/I-O Workflows** — ✅ N/A by design: the helper is
  *pure* and stateless. It is called from the **consumer's** `update`; it owns no state and requests no
  effects, so no `Model/Msg/Cmd` boundary applies. This is the intended shape, not an omission.
- **V. Test Evidence Is Mandatory** — ✅ Real evidence: the collision-logic test fails before
  `Collision.fs` exists and passes after; determinism is a repeat-run byte-identity property test; the
  coherence test fails on any registry drift. The quickstart proves the delete-safe and edit-changes-
  behavior scenarios on a real generated product. No synthetic evidence.
- **VI. Observability and Safe Failure** — ✅ Pure helper has no I/O to log. "Safe failure" is met by
  **totality** (FR-010): zero-area body, exactly-touching edges, fully-contained body, empty candidate
  set return documented values, never throw — and by build-time **delete safety** (`Condition="Exists(...)"`).
- **Change Classification** — **Tier 1 (contracted change)**: it changes the `fs-gg-ui-template`
  emitted-file contract (new skill, new source file, new compile item) even though it adds **no F#
  package public surface**. Full artifact chain required (spec, plan, contracts, tests, docs, registry
  coherence) plus, on release, the coherent-set bump and cross-repo registry/compatibility flip —
  scheduled by `/speckit-tasks`.

**Result: PASS.** No violations; Complexity Tracking table not required.

## Project Structure

### Documentation (this feature)

```text
specs/246-collision-detection-skill/
├── plan.md              # This file
├── research.md          # Phase 0 — delivery mode, compile-order + delete-safety, response determinism, contract class
├── data-model.md        # Phase 1 — collision value shapes (Body/Contact/Resolution) + total-function conventions
├── quickstart.md        # Phase 1 — scaffold a game product, build, edit response, delete-and-still-build
├── contracts/           # Phase 1 — the intended helper source surface + the skill/registry entries
│   ├── collision-helper-source.md   # types + functions the consumer receives in Collision.fs
│   └── skill-and-registration.md    # SKILL.md shape + the exact registry touch-points/conditions
├── checklists/
│   └── requirements.md  # spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 — /speckit-tasks (NOT created here)
```

### Source Code / template (repository root)

```text
template/product-skills/fs-gg-collision/
└── SKILL.md                         # NEW — the dedicated collision skill (detection→broad→response, footguns, pointer to Collision.fs)

template/fragments/collision/
├── README.md                        # NEW — fragment stub: consumer-owned, adaptable source
└── src/Product/Collision.fs         # NEW — the import-and-adapt helper (sourceName-substituted 'Product')

template/product-skills/fs-gg-game-core/SKILL.md   # EDIT — trim the `## Collision` section to a pointer at fs-gg-collision

template/base/src/Product/Product.fsproj           # EDIT — add gated `<Compile Include="Collision.fs" Condition="Exists('Collision.fs')" />`
                                                    #        under (profile == "game" || profile == "sample-pack")

scripts/generate-skill-manifest.fsx                # EDIT — add fs-gg-collision to the `catalog` list
template/skill-manifest/skill-manifest.json        # REGEN — new fs-gg-collision entry (id+sha256+materializes-when+supplied-by)
.template.config/template.json                     # EDIT — two gated sources: skill → .agents/skills/fs-gg-collision/, fragment → src/<ProductDir>/
template/base/docs/skillist-reference.md           # EDIT — register fs-gg-collision in the full-registry catalog
template/base/docs/scaffold-map.md                 # EDIT — classify Collision.fs as replaceable/adaptable (consumer-owned)
template/product-skills/fs-gg-model-swap/SKILL.md   # EDIT — add Collision.fs to the "Replaceable — rewrite freely" list (FR-013 swap-guidance reach); retriggers its manifest sha256

.agents/skills/fs-gg-collision/SKILL.md            # NEW (dev root) — canonical body; .claude/skills mirror via materialize-skill-roots.fsx

tests/Package.Tests/Feature246CollisionSkillTests.fs   # NEW — manifest/template.json/skillist/parity coherence + profile gating
tests/Canvas.Tests/CollisionHelperTests.fs             # NEW — response correctness + repeat-run determinism + degenerate totality (compiles raw Collision.fs; Canvas.Tests already refs Canvas+Scene)
scripts/*-prelude.fsx                                   # FSI transcript exercising the helper as a game consumer would

# On release (Tier 1 template-contract change, publish-before-flip) — in FS-GG/.github:
registry/dependencies.yml            # fs-gg-ui-template contract version + consuming edge bumped
registry/CHANGELOG.md                # one dated newest-first entry
docs/registry/compatibility.md       # dependency-graph + versioned-contracts row + coherence row
```

**Structure Decision**: Deliver as a **skill + scaffold-source fragment pair**, not a framework
package. Collision *detection* already ships as package API (`Geometry`/`SpatialGrid`); the missing
piece is the game-opinionated *response* layer, which belongs in **consumer-owned adaptable source**,
not a frozen `.fsi`. The skill registers exactly like the sibling skill-only capabilities
(`fs-gg-audio`/`fs-gg-game-core`); the source ships via a new `template/fragments/collision/` (modeled
on how `template/fragments/samples/` ships product content) with a profile-gated, `Exists`-guarded
compile item so it materializes only for game/sample-pack and stays edit-and-delete safe. `capabilities.yml`
is **not** touched — it enumerates only package/fragment-backed *package* capabilities, and this skill,
like `fs-gg-game-core`, is registered through the manifest/template/skillist path instead.

## Complexity Tracking

*No Constitution Check violations — table intentionally omitted.*
