# Implementation Plan: fs-gg-layout consumer product-skill (app + game profiles)

**Branch**: `227-layout-product-skill` | **Date**: 2026-07-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/227-layout-product-skill/spec.md`

## Summary

Close the last app/game consumer-capability gap ([FS-GG/FS.GG.Rendering#39](https://github.com/FS-GG/FS.GG.Rendering/issues/39), epic #34): a scaffolded `app`/`game` product **references and runs** the Yoga-backed Layout capability (the `LayoutEvidence` HUD/gameplay-region spine in `template/base/src/Product/`) but ships **no** consumer skill teaching it — the only `fs-gg-layout` is the framework-authoring skill the `sdd`/`none` lifecycles gate out. This feature adds **one** thin, shipped, consumer-facing `fs-gg-layout` skill (with its `fs-gg-product-layout` wrapper pair), wires it onto the controls/interaction-bearing profiles (`app`, `game`) exactly as `fs-gg-styling`/`fs-gg-ui-widgets` are wired, and joins it to every repo-owned enumeration that tracks the shipped product-skill set.

**Technical approach**: content + configuration only — **no `src/**` change, no version bump** (mirrors Feature 226). The skill body documents the *consumer slice* of layout (compute HUD + gameplay/content regions, split the screen responsively by output size, keep an active item inside the gameplay region, and the `LayoutEvidence` shape the starter already uses) and explicitly bounds out the framework layout-engine internals (owned upstream by `src/Layout/skill/SKILL.md`). Delivery is wired by mirroring the verified `fs-gg-styling` gating in `.template.config/template.json` and verified by an actual scaffold observation, not by assumption.

> **Standing-assumption note (content-only feature).** This feature ships no runtime code, so the "real app" to drive for the early-live-smoke-run is the **scaffold path**: produce `app`/`game` products from the template and observe the emitted skill set (before authoring and after wiring). The Feature 175 lesson — deterministic tests pass while the produced artifact is wrong (Feature 35's symbology skill was authored but never shipped) — applies directly to delivery and is why the gate is confirmed empirically, not assumed from a green test.

## Technical Context

**Language/Version**: N/A for behavior — content/config only. Touched assets: Markdown (`SKILL.md` ×3), JSON (`.template.config/template.json`), one Markdown catalog row, and F# **test-data** edits in three existing Package.Tests gates (no new module, no public surface).

**Primary Dependencies**: Existing repo-owned gates — the skill-parity harness (`tools/Rendering.Harness/SkillParity.fs`, dynamic discovery), the Feature 224 catalog-currency test, the Feature 225 product-skill leak guard, and the Feature 219 / Feature 204 emission-matrix tests. No new dependency introduced.

**Storage**: N/A.

**Testing**: No new test project. Verification rides the *existing* gates (skill-parity, catalog currency, leak guard, emission matrix) plus a live scaffold observation and hand-reads recorded under `specs/227-layout-product-skill/readiness/`.

**Target Platform**: N/A (template/package content delivered to scaffolded products).

**Project Type**: Template/package content within the F# rendering framework repo.

**Performance Goals**: N/A. **Constraints**: Must stay the consumer slice (FR-002) — no layout-engine internals, no `.fsi`/surface-baseline authoring; must pass the Feature 225 leak guard from the first draft (no `Feature N`/`spec-N`/framework-evidence tokens); gated on product surface, not lifecycle (FR-003).

**Scale/Scope**: One skill body + one wrapper pair + two `sources` entries + one catalog row + one leak-guard backstop id + three test-floor/matrix edits + one regenerated parity report + one cross-link. Shipped product-skill set grows **8 → 9**; framework-skill source count **16 → 18**.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* — **PASS.**

- **Change Classification** — **Tier 2 (internal/content change).** No public API surface, no new dependency, no cross-package/contract change, no change to observable behavior of existing specs. Tier 1 artifacts (`.fsi`, surface-area baselines) are **not** required and are left untouched.
- **I. Spec → FSI → Semantic Tests → Implementation** — **N/A by construction.** Adds no `.fs`/`.fsi` module and no behavior; there is no public surface to draft in FSI. The applicable verification is the existing repo-owned content gates plus a live scaffold observation, which `/speckit-tasks` will schedule.
- **II. Visibility lives in `.fsi`** — N/A; no module touched (the only F# edits are to test-data lists/floors, not a public surface).
- **III. Idiomatic Simplicity** — Honored: one thin skill mirroring existing siblings; no new abstraction, operator, SRTP, reflection, or computation expression.
- **IV. Elmish/MVU boundary** — N/A; no stateful/I-O workflow added.
- **V. Test Evidence** — No behavior to test; the feature is gated by the *existing* parity/catalog/leak/matrix checks (now asserted to include the new skill) plus disclosed scaffold evidence. No synthetic evidence introduced.
- **VI. Observability & Safe Failure** — N/A; no runtime path.
- **Controls/layout layering constraint** — Respected: the skill documents *consuming* the layout surface the starter exposes; it does not fork the layout engine or reimplement region math beyond what the starter ships.
- **Local Skills are advisory** — Honored: the new skill is an advisory aid, not a gate.

**Re-check after design**: unchanged — the design adds only content/config, so the gate remains PASS.

## Project Structure

### Documentation (this feature)

```text
specs/227-layout-product-skill/
├── spec.md              # Feature specification (Tier 2 declared)
├── plan.md              # This file
├── research.md          # Phase 0 — resolved unknowns (scope, gating, wrapper, gate map)
├── data-model.md        # Phase 1 — the entities: skill body, wrapper, emission matrix, catalog, guard
├── quickstart.md        # Phase 1 — the runnable verification recipe (gates + scaffold observation)
├── contracts/           # Phase 1 — the consumer-content contract + gate-assertion contract
├── checklists/          # requirements.md (spec quality)
└── readiness/           # Evidence: naming, wiring-map, scaffold-before/after, layout-surface,
                         #   us1/us3 reads, gate transcripts, success-criteria (produced in /implement)
```

### Source Code (repository root) — verified touch-points

Every path below is confirmed against the shipped `fs-gg-styling` (Feature 226) diff, not assumed:

```text
template/product-skills/fs-gg-layout/SKILL.md              # NEW canonical skill body (US1)
.agents/skills/fs-gg-product-layout/SKILL.md               # NEW wrapper (routes to body)
.claude/skills/fs-gg-product-layout/SKILL.md               # NEW wrapper (routes to body)
.template.config/template.json                             # EDIT: two gated `sources` entries (app|game → .agents + .claude)
template/base/docs/skillist-reference.md                   # EDIT: catalog row (Feature 224 currency)
tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs  # EDIT: +fs-gg-layout in expectedProductSkillIds backstop
tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs  # EDIT: +fs-gg-layout on app+game rows; source floor 16→18
tests/Package.Tests/Feature204LifecycleTemplateTests.fs    # EDIT: framework-source floor 16→18
docs/reports/skills-parity.md                              # REGEN: canonical/wrapper counts (harness output, not hand-edited)
template/product-skills/fs-gg-scene/SKILL.md               # EDIT: cross-link to fs-gg-layout (discoverability; scheduled in Polish — tasks.md T018)
CLAUDE.md                                                  # EDIT: SPECKIT plan-pointer (agent-context step)
```

Discovery in `tools/Rendering.Harness/SkillParity.fs` is **dynamic** (no hardcoded canonical list to edit); the two human-maintained enumerations the new skill must join are the Feature 224 catalog and the Feature 225 backstop, and the two matrix floors (Feature 219 + Feature 204). `docs/reports/skills-parity.md` is **regenerated** by the harness (`skill-parity` CLI / `scripts/check-agent-skill-parity.fsx`), never hand-edited.

**Structure Decision**: Content/config only — no `src/**` change. The new skill lives beside the existing `template/product-skills/fs-gg-*` skills and is delivered through the `.agents/skills/` + `.claude/skills/` template-vendored pair plus the repo-root `fs-gg-product-layout` wrapper alias, exactly as the other shipped skills are.

### Resolved during planning (see research.md)

- **Profile gating** — `(profile == "app" || profile == "game")`, taken from the **real** `fs-gg-styling`/`fs-gg-ui-widgets` wiring, not the spec's provisional reading. Scene-only profiles (`headless-scene`, `governed`, `sample-pack`) do **not** force the skill in (FR-003 / SC-002).
- **Wrapper is in scope** — `fs-gg-product-layout` wrapper pair is required for parity: all 8 shipped product-skills carry a matching `fs-gg-product-*` wrapper, the catalog states "each also ships a wrapper alias," and Feature 226 added `fs-gg-product-styling`. Adding a canonical skill without a wrapper would surface a skill-parity `MissingWrapper` finding.
- **Cross-link target** — `fs-gg-scene` is the layout render target and app/game already relate scene↔layout; `fs-gg-ui-widgets` already owns generated layout-*control* examples, so the primary cross-link is scene↔layout, with a `Related` pointer from `fs-gg-ui-widgets` if the leak/parity read shows an asymmetry. Final link direction is confirmed against the shipped skills during authoring, not fixed here.

## Complexity Tracking

> No Constitution Check violations. No complexity to justify. (Section intentionally empty.)
