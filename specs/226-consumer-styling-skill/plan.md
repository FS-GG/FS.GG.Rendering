# Implementation Plan: Consumer Theming/Styling Product Skill

**Branch**: `226-consumer-styling-skill` | **Date**: 2026-07-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/226-consumer-styling-skill/spec.md`

## Summary

Close the consumer styling capability gap (FS-GG/FS.GG.Rendering#38): a scaffolded
FS.GG.UI product ships seven product skills but none teach a product author how to
theme or style their product. This feature adds **one** thin, shipped, consumer-facing
styling skill (`fs-gg-styling`, wrapper `fs-gg-product-styling`) that teaches the
*consume-a-style* slice only — pick/apply a theme, set a control's style variant and
style class, apply a resolved style to a control — wires it to ship on the
controls-bearing product profiles exactly as `fs-gg-ui-widgets` is wired, enumerates it
in the repo-owned parity/leak/catalog surfaces, and cross-links it from `fs-gg-ui-widgets`.

**Technical approach**: content + configuration only. No runtime/framework code changes.
The skill body is authored to the *real* consumer-reachable styling surface (confirmed in
Foundational T005) and to pass the repo-owned leak guard (Feature 225). Delivery is wired
by mirroring the verified `fs-gg-ui-widgets` gating in `.template.config/template.json`
(Foundational T003) and verified by an actual scaffold (T004/T017), not by assumption.

> **Standing-assumption note (adapted for a content-only feature).** This feature ships
> no runtime code, so the "real app" to drive for the early-live-smoke-run is the
> **scaffold path**: produce a product from the template and observe the shipped skill set
> before authoring (T004) and after wiring (T017). The Feature 175 lesson — deterministic
> tests pass while the produced artifact is wrong (a sibling skill was authored but never
> shipped) — applies directly to delivery (US2) and is why the gate is confirmed empirically.

## Technical Context

**Language/Version**: N/A for behavior — content/config only. Touched assets are Markdown
(`SKILL.md`), JSON (`.template.config/template.json`), one F# test-data list edit
(`expectedProductSkillIds` in `tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs`).

**Primary Dependencies**: Existing repo-owned checks — skill-parity engine
(`tools/Rendering.Harness/SkillParity.fs`, dynamic discovery), Feature 225 leak guard,
Feature 224 catalog-currency test. No new dependency is introduced.

**Storage**: N/A.

**Testing**: No new test project. Verification rides the *existing* gates
(skill-parity, leak guard, catalog currency) plus a live scaffold observation and
hand-reads recorded under `specs/226-consumer-styling-skill/readiness/`.

**Target Platform**: N/A (template/package content delivered to scaffolded products).

**Project Type**: Template/package content within the F# rendering framework repo.

**Performance Goals**: N/A.

**Constraints**: Must stay the consumer slice (FR-002/FR-003) — no token-source, no
`StyleResolver` internals, no surface-baseline authoring; must pass the leak guard from
the start (FR-004); must be gated on product surface, not lifecycle (FR-007).

**Scale/Scope**: One skill body + one wrapper pair + one `sources` wiring + one catalog
row + one backstop-list entry + one cross-link. Shipped product-skill set grows 7 → 8.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* — **PASS.**

- **Change Classification** — Declared **Tier 2 (internal/content change)** in spec.md.
  No public API surface, no new dependency, no inter-project/package contract change, no
  change to observable behavior of existing specs. Tier 1 artifacts (`.fsi`,
  surface-area baselines) are therefore **not** required and are left untouched (FR-010).
- **I. Spec → FSI → Semantic Tests → Implementation** — **N/A by construction.** This
  feature adds no `.fs`/`.fsi` module and no behavior; there is no public surface to draft
  in FSI. The applicable verification is the existing repo-owned content checks and a live
  scaffold observation (T004/T017), which the tasks schedule.
- **II. Visibility lives in `.fsi`** — N/A; no module touched (the only F# edit is to a
  test-data list, not a public surface).
- **III. Idiomatic Simplicity** — Honored: one thin skill mirroring existing siblings; no
  new abstraction, operator, SRTP, reflection, or computation expression.
- **IV. Elmish/MVU boundary** — N/A; no stateful/I-O workflow added.
- **V. Test Evidence** — No behavior change to test; the feature is gated by the *existing*
  parity/leak/catalog checks (now asserted to include the new skill) plus disclosed
  scaffold evidence. No synthetic evidence introduced.
- **VI. Observability & Safe Failure** — N/A; no runtime path.
- **Controls/themes layering constraint** — Respected: the skill documents *consuming*
  one semantic control set's styling; it does not fork controls per theme and does not add
  a design-specific kit.
- **Local Skills are advisory** — Honored: the new skill is an advisory aid, not a gate.

**Re-check after design**: unchanged — the design adds only content/config, so the gate
remains PASS.

## Project Structure

### Documentation (this feature)

```text
specs/226-consumer-styling-skill/
├── spec.md              # Feature specification (Tier 2 declared)
├── plan.md              # This file
├── tasks.md             # Task list (grounded in spec.md + verified wiring map)
├── checklists/          # Authoring checklist(s)
└── readiness/           # Evidence: naming, wiring-map, scaffold-before/after,
                         #   styling-surface, us1/us3 reads, baselines, success-criteria
```

### Source Code (repository root) — verified touch-points

```text
template/product-skills/fs-gg-styling/SKILL.md          # NEW canonical skill body (US1)
.agents/skills/fs-gg-product-styling/SKILL.md           # NEW wrapper (routes to body)
.claude/skills/fs-gg-product-styling/SKILL.md           # NEW wrapper (routes to body)
.template.config/template.json                          # EDIT: two gated `sources` entries
template/base/docs/skillist-reference.md                # EDIT: catalog row (Feature 224)
tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs  # EDIT: expectedProductSkillIds
template/product-skills/fs-gg-ui-widgets/SKILL.md       # EDIT: cross-link pointer (US3)
```

Discovery is dynamic in `tools/Rendering.Harness/SkillParity.fs` (no hardcoded list to
edit there); the two human-maintained enumerations the new skill must join are the
Feature 225 backstop list and the Feature 224 catalog.

**Structure Decision**: Content/config only — no `src/**` change. The new skill lives
beside the existing `template/product-skills/fs-gg-*` skills and is delivered through the
`.agents/skills/` + `.claude/skills/` wrapper pair exactly as the other shipped skills are.

### Profile gating (resolved during planning)

The controls-bearing profile set is taken from the **real** `fs-gg-ui-widgets` wiring, not
from the spec's provisional "default reading." Verified gating is
`(profile == "app" || profile == "game")`. The spec Assumptions list `sample-pack` as a
*possible* third controls-bearing profile; the live wiring map (T003) does **not** gate
`fs-gg-ui-widgets` to `sample-pack`, so `fs-gg-styling` mirrors app+game only. Scene-only
profiles (`headless-scene`, `governed`) do not force the skill in (FR-006), and both are
exercised in the scaffold checks.

## Complexity Tracking

> No Constitution Check violations. No complexity to justify. (Section intentionally empty.)
