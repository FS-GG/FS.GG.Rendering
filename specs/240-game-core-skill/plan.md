# Implementation Plan: `fs-gg-game-core` — product skill for simulation patterns

**Branch**: `240-game-core-skill` | **Date**: 2026-07-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/240-game-core-skill/spec.md`

## Summary

Add a 13th product skill, `fs-gg-game-core`, that teaches a game/sim consumer of FS.GG.UI to reach for
the simulation primitives shipped in Feature 239 — `FixedStep.drain` (fixed-timestep march), the
value-type `Rng` threaded through the MVU `Model` (determinism), and `Geometry` (AABB + swept collision,
and culling as an `intersects`/`containsPoint` test against the visible `Rect`) — instead of re-rolling
them by hand (Space Invaders feedback §5.6, #73).

**Technical approach — additive, docs/packaging only.** No F# source, no `.fsi`, no surface-area baseline
changes: the library surface is Feature 239's and is consumed verbatim. The work is a new canonical
`SKILL.md` body plus the skill-union wiring that every product skill already uses:

1. **Body** — `template/product-skills/fs-gg-game-core/SKILL.md`, in the sibling skills' voice, covering
   the four patterns and naming only real Feature-239 public members.
2. **One `template.json` source** — `template/product-skills/fs-gg-game-core/` → `.agents/skills/fs-gg-game-core/`,
   `copyOnly: ["**/*"]`, condition `(profile == "game" || profile == "sample-pack")`. This single
   profile-gated (no `lifecycle` clause) source drives **both** lanes: the spec-kit materialize step and
   the sdd framework-emit path (Feature219 re-derives sdd emission from exactly these sources).
3. **Generator catalog** — one entry in `scripts/generate-skill-manifest.fsx`, then regenerate
   `template/skill-manifest/skill-manifest.json` to 13 entries (adds `materializes-when` + `supplied-by`
   for the new id; the twelve prior entries stay byte-identical).
4. **Tests** — move the five interlocking Package.Tests rosters/counts from 12/9/9 → 13/10/10 in lockstep,
   add a surface-referenced check (every member the body names exists in the packed `Scene`/`Canvas` `.fsi`).
5. **Docs** — cross-link the skill from `template/base/docs/product.md`.

> **Standing assumption — no unverified root-cause hypotheses.** This is additive packaging surface, not a
> defect fix, so there is no root-cause map to confirm. The "does it actually work end-to-end" obligation
> is met by (a) a compilable consumer snippet in the body exercised against the packed Feature-239 `.fsi`
> (the FSI-audience analog of Principle I), and (b) `/speckit-tasks` scheduling an early **scaffold smoke**
> in the Foundational phase: regenerate the manifest `--check`, run the skill Package.Tests, and confirm a
> `profile=game` scaffold emits `.agents/skills/fs-gg-game-core/SKILL.md` (byte-equal to source) while a
> `profile=app` scaffold does not.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; the deliverable is Markdown (`SKILL.md`) + JSON
(`skill-manifest.json`) + a `template.json` source + F# test edits. The regenerator
`scripts/generate-skill-manifest.fsx` is an `fsi` script.

**Primary Dependencies**: none new. The skill body *documents* Feature 239's `FS.GG.UI.Scene.Geometry`
and `FS.GG.UI.Canvas.Rng`/`FixedStep`; it adds no code dependency.

**Storage**: N/A.

**Testing**: Expecto Package.Tests — `Feature219EmitFrameworkSkillsTests`,
`Feature225ProductSkillVocabularyTests`, `Feature231SkillManifestTests`,
`Feature238SkillMaterializesWhenTests`, and a currency check `Feature224SkillCatalogCurrencyTests`.
Validation also uses the `generate-skill-manifest.fsx --check` mode and the packed
`template/base/docs/api-surface/{Scene,Canvas}` `.fsi` for the surface-referenced check.

**Target Platform**: cross-platform; the skill materializes into a generated product's `.agents/skills/`.

**Project Type**: template / product-skill packaging within the FS.GG.UI product.

**Performance Goals**: N/A (static content).

**Constraints**: additive and byte-preserving for the twelve existing skills; `schemaVersion` stays `1`;
the new `materializes-when` string is identical in `template.json`, the generator catalog, and the
manifest (single source of truth, Feature 238 FR-006); the body names only members that exist in the
Feature-239 `.fsi`.

**Scale/Scope**: one new skill body; one `template.json` source; one generator-catalog entry; one manifest
regeneration (+1 row); five test files touched; one product-doc cross-link.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — ✅ Honored, adapted for a docs/packaging feature.
  The `.fsi`-sketch step maps to **contracts/** (the `SKILL.md` content contract + the manifest-entry
  contract); the FSI-exercise step maps to a **compilable consumer snippet** in the body validated against
  the packed Feature-239 `.fsi` (SC-004); semantic tests are the Package.Tests updated to fail before /
  pass after; implementation is writing the body + wiring. No new `.fs`/`.fsi` is authored.
- **II. Visibility Lives in `.fsi`** — ✅ N/A: no F# public surface is added or changed. No surface-area
  baseline changes (the Feature-239 baselines already cover the members the body cites).
- **III. Idiomatic Simplicity** — ✅ The wiring reuses the existing product-skill pattern (one profile-gated
  `copyOnly` source, one catalog tuple). No justification-required feature is used. Test edits are plain
  list/count updates.
- **IV. Elmish/MVU Is the Boundary** — ✅ The feature ships no runtime behavior. Where the body *advises*
  state (RNG), FR-003 requires the guidance itself be MVU-shaped: thread the value-type `Rng` through the
  consumer's `Model`, never a mutable `System.Random` — reinforcing the boundary, not crossing it.
- **V. Test Evidence Is Mandatory** — ✅ The five Package.Tests fail before (roster/count mismatch, stale
  digest) and pass after. All evidence is real (deterministic manifest/scaffold checks, no GL/IO, no
  synthetic fixtures).
- **VI. Observability and Safe Failure** — ✅ N/A (no runtime paths). The "safe failure" analog is the
  digest + surface-referenced tests failing loudly on body drift or a dangling member reference.
- **Change Classification** — **Tier 1 (contracted change)**: it extends the skill-manifest catalog (a
  cross-repo-consumed contract, `.github#164`) and the profile→skill emission matrix. Full artifact chain
  required — spec, plan, manifest regeneration, test evidence, docs — **except** `.fsi`/surface-area
  baselines, which are untouched because no F# public surface changes (recorded here per the Tier-1 rule).

**Result: PASS.** No violations; Complexity Tracking table omitted.

## Project Structure

### Documentation (this feature)

```text
specs/240-game-core-skill/
├── plan.md              # This file
├── research.md          # Phase 0 — profile scoping, emission-lane, tier, surface-referenced-check decisions
├── data-model.md        # Phase 1 — the skill-manifest entry + the SKILL.md content model
├── quickstart.md        # Phase 1 — how to validate end-to-end (regen --check, tests, scaffold smoke)
├── contracts/
│   ├── skill-body.md    # Phase 1 — the fs-gg-game-core SKILL.md content contract (sections, cited members)
│   └── manifest-entry.md# Phase 1 — the 13th skill-manifest entry contract
├── checklists/
│   └── requirements.md  # spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 — /speckit-tasks (NOT created here)
```

### Source Code (repository root)

```text
template/product-skills/fs-gg-game-core/
└── SKILL.md                         # NEW — canonical body (fixed-step / RNG / collision / culling)

.template.config/template.json       # +1 source: product-skills/fs-gg-game-core → .agents/skills/, (game||sample-pack)

scripts/generate-skill-manifest.fsx  # +1 catalog entry (kept sorted asc by id)
template/skill-manifest/skill-manifest.json   # regenerated: 12 → 13 entries (new row only)

# Packaging — make Canvas consumable on the simulation profiles (FR-011/FR-012)
template/base/Directory.Packages.props        # +pin FS.GG.UI.Canvas (gated game||sample-pack)
template/base/src/Product/Product.fsproj      # +PackageReference FS.GG.UI.Canvas (gated game||sample-pack)
template/base/docs/api-surface/Canvas/        # NEW — Elements/FixedStep/Loop/Rng .fsi (verbatim from src/Canvas)
template/base/docs/api-surface/Scene/Scene.fsi# refreshed: + the Geometry module (stale since R8 rebrand)

tests/Package.Tests/
├── Feature231SkillManifestTests.fs         # catalog 12 → 13 (+ "12 entries" comment) + surface-referenced check
├── Feature238SkillMaterializesWhenTests.fs # catalog 12 → 13 (condition auto-derived from template.json)
├── Feature219EmitFrameworkSkillsTests.fs   # game & sample-pack rows gain fs-gg-game-core; sources 9 → 10
├── Feature225ProductSkillVocabularyTests.fs# expectedProductSkillIds 9 → 10 (+ vocabulary check on new body)
├── Feature224SkillCatalogCurrencyTests.fs  # verify the real new id resolves (no catalog-doc dangle)
└── Feature209VersionCoherenceTests.fs      # templateExpected += FS.GG.UI.Canvas (11 → 12-member pin manifest)

template/base/docs/product.md        # cross-link the collision/RNG/fixed-step guidance to fs-gg-game-core
```

> **Scope note (approved 2026-07-04).** The skill was originally scoped as docs-only. Implementation
> revealed `FS.GG.UI.Canvas` (home of `Rng`/`FixedStep`) is not wired into generated products, so a
> skill advising those APIs would dangle. The owner approved **wiring Canvas into the `game`/`sample-pack`
> product template** (FR-011/FR-012). This makes it a **product-package contract change** for those two
> profiles — the exact-equality pin manifest in `Feature209VersionCoherenceTests` is the gate that moves.

**Structure Decision**: Reuse the established product-skill mechanism exactly — a single profile-gated
`copyOnly` `template.json` source plus a generator-catalog tuple — rather than any new machinery. The skill
lives under `template/product-skills/` beside its twelve siblings; the manifest stays the single generated
catalog. Emission is deliberately scoped to the simulation profiles `game` and `sample-pack` (not `app`),
because a generic app is not a simulation host (spec Edge Cases / FR-006).

## Complexity Tracking

*No Constitution Check violations — table intentionally omitted.*
