# Implementation Plan: De-leak Product Skill Vocabulary

**Branch**: `225-deleak-product-skill-vocab` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/225-deleak-product-skill-vocab/spec.md`

## Summary

The 7 shipped product skills (`template/product-skills/fs-gg-*/SKILL.md`) carry **good bodies with
leaky framing**: framework-repo evidence process (`refresh-local-feed-and-samples.fsx`,
`package-feed` proof, `specs/*/readiness/` + `.gitignore` allowlisting, concurrent-test
`BaseOutputPath`), unconditional `specs/<feature>/feedback/` references in every skill's "Persistent
problems" block, and framework feature/spec-number stamps in prose ("Feature 168", "feature
199/200", "spec-196"). This feature **reframes** that prose into product-author language —
preserving every lesson, capability, and evidence intent — and adds a **repo-owned leak guard** that
scans the whole shipped product-skill set and fails the build if any of the three leak classes
reappears. It is package-content only: no skill behavior/capability changes, no skill added/removed,
no consumer catalog touched (that is sibling #36 / Feature 224). Delivery rides a future
`FS.GG.UI.Template` republish + the downstream pin bump (FS-GG/FS.GG.Templates#8).

> **Standing assumption — the produced-package surface is unverified until a real scaffold confirms it.**
> The premise of US2 (these 7 skills reach `app`/`game`/`sdd`/`none` lifecycles, not just spec-kit)
> and the guard's discovery surface (it must enumerate exactly the skills a product carries) are
> provisional until confirmed against the real produced surface. `/speckit-tasks` MUST schedule an
> **early produced-surface verification** in the Foundational phase (before any prose edit) that
> enumerates the shipped product-skill set the way the guard will — `SkillParity` discovery filtered
> to `template/product-skills` — and confirms it is exactly the 7 expected skills. Do not build the
> de-leak edits on an unverified surface assumption.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (repo standard).

**Primary Dependencies**: Expecto (tests); the in-repo `Rendering.Harness` skill tool
(`tools/Rendering.Harness/SkillParity.fs`) for skill discovery — reused, not extended, for the
guard's enumeration. No new dependencies. The shipped artifacts are the 7
`template/product-skills/fs-gg-*/SKILL.md` Markdown bodies (`dotnet new` template content).

**Storage**: N/A — Markdown skill-body content plus test code only. No persistence.

**Testing**: Expecto. New guard test in `tests/Package.Tests/` (sibling of
`Feature224SkillCatalogCurrencyTests.fs`), reusing `SkillParity.discoverDefaultSurfaces` /
`inventorySkills` / `defaultRequest`. The existing wrapper-vs-canonical parity tests under
`tests/Rendering.Harness.Tests/` (`Feature168*`, `Feature223SymbologyParityTests.fs`) MUST stay
green (FR-006). The env-gated lifecycle validator
(`FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx`) is the
real-scaffold evidence path for the standing-assumption produced-surface run.

**Target Platform**: Produced FS.GG.UI product packages (`FS.GG.UI.Template`); the guard runs in the
framework repo's `dotnet test` lane (Linux/CI), no GL surface required.

**Project Type**: F# library + `dotnet new` template product (desktop-app framework). Single repo.

**Performance Goals**: N/A — the leak guard is a build-time correctness gate over ~7 small Markdown
files, not a hot path.

**Constraints**: Tier 1 contracted change to the `fs-gg-ui-template` package content surface. Skills
ship **across lifecycles** (Feature 219 / #30): the de-leak must make lifecycle-specific paths
**conditional**, never simply delete the spec-kit path. Every edit is reframing, not removal
(FR-004 / SC-004) — the wrapper-vs-canonical parity invariant (FR-006) bounds how the prose may
change. Reaches consumers only as package content via a republished `FS.GG.UI.Template` + the
FS-GG/FS.GG.Templates#8 pin bump (FR-008).

**Scale/Scope**: 7 shipped skill bodies (4 carry leak blocks needing rewrite — testing, ui-widgets,
skiaviewer, symbology; 3 carry only the Class-B feedback line — elmish, keyboard-input, scene), one
new guard test, zero new product/runtime code. ~12 leak-token sites total (see research R0).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | ✅ | Spec done; the guard's discovery surface is exercised through the loaded `SkillParity` public API and asserted by an Expecto test that reds on an injected leak and greens on the corrected set (SC-005). |
| II. Visibility in `.fsi` | ✅ (conditional) | Preferred design keeps the scan self-contained in the `Package.Tests` test consuming **existing** public `SkillParity` discovery → no new public surface, no `.fsi`/baseline delta (mirrors Feature 224). If a helper is genuinely promoted into `Rendering.Harness`, its `.fsi` **and** the surface-area baseline are updated in the same change (ordinary Tier 1). |
| III. Idiomatic Simplicity | ✅ | Reuse `defaultRequest`/`discoverDefaultSurfaces`/`inventorySkills`; plain regex token-extraction + per-line findings over `entry.Content`. No SRTP/reflection/new CE/custom operators. |
| IV. Elmish/MVU boundary | ✅ N/A | Pure validation over file content; no stateful or I/O workflow. |
| V. Test Evidence Mandatory | ✅ | Failing-before/passing-after: inject each of the three leak classes into a synthetic skill body → guard reds; real corrected set → green (SC-005). Real produced-surface evidence via an actual scaffold for the standing-assumption run, not a fixture. |
| VI. Observability & Safe Failure | ✅ | Guard failure names the offending **skill id, leak class, matched token, and file:line** (FR-007). No silent pass; no swallowed match. |
| Change Classification | ✅ Tier 1 | Touches the `fs-gg-ui-template` consumer content surface and adds a gate; requires content edits, test evidence, and (only if a public helper is added) `.fsi` + baseline updates. Cross-repo coherence per FR-009. |

**Result: PASS** (no unjustified violations). The only conditional is II — handled by steering the
scan into a self-contained `Package.Tests` test (no new public surface), exactly as Feature 224 did;
see research R3.

## Project Structure

### Documentation (this feature)

```text
specs/225-deleak-product-skill-vocab/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — leak inventory, conditional-feedback recognition, guard home, parity safety, cross-repo
├── data-model.md        # Phase 1 — entities: product skill, leak token (3 classes), lifecycle, leak guard / finding
├── quickstart.md        # Phase 1 — runnable validation (produced-surface run; guard reds-before/greens-after; parity stays green)
├── contracts/
│   └── leak-guard-check.md   # Phase 1 — the leak-guard contract (scan surface, three leak classes, conditional rule, failure shape)
└── checklists/
    └── requirements.md  # Spec quality checklist (from /speckit-specify), if present
```

### Source Code (repository root)

```text
template/product-skills/                 # EDIT — the 7 shipped product-skill bodies (reframe leaks)
├── fs-gg-testing/SKILL.md               #   Class A (evidence block, all 5 tokens) + Class B (feedback)
├── fs-gg-ui-widgets/SKILL.md            #   Class A (refresh/package-feed) + Class B
├── fs-gg-skiaviewer/SKILL.md            #   Class A (refresh/package-feed) + Class B
├── fs-gg-symbology/SKILL.md             #   Class C (feature 199/200, spec-196 prose stamps) + Class B
├── fs-gg-elmish/SKILL.md                #   Class B only
├── fs-gg-keyboard-input/SKILL.md        #   Class B only
└── fs-gg-scene/SKILL.md                 #   Class B only

tools/Rendering.Harness/
├── SkillParity.fs / .fsi                # REUSE discovery (defaultRequest/discoverDefaultSurfaces/
                                         #   inventorySkills) — NOT edited unless a helper is promoted (then .fsi+baseline)

tests/
├── Package.Tests/
│   └── Feature225ProductSkillVocabularyTests.fs   # NEW — leak guard: scan shipped set, 3 classes, reds-before/greens-after
└── Rendering.Harness.Tests/
    └── Feature168*/Feature223SymbologyParityTests.fs  # UNCHANGED — must stay green (FR-006)
```

**Structure Decision**: Single-repo F# framework + `dotnet new` template. The change is localized to
the 7 shipped Markdown bodies under `template/product-skills/` plus one new Expecto guard under
`tests/Package.Tests/`. The skill enumerator `tools/Rendering.Harness/SkillParity.fs` is **reused**
(the same `defaultRequest`/`discoverDefaultSurfaces`/`inventorySkills` trio Feature 224 consumes),
not re-implemented and — preferentially — not extended. No product/runtime F# changes; no
`.template.config/template.json` edit (the 7 skills are already wired across profiles).

## Complexity Tracking

> No constitution violations require justification. The sole conditional (Principle II, if a public
> `SkillParity` helper is promoted) is handled by the ordinary Tier 1 procedure — `.fsi` + surface
> baseline updated in the same change — and is avoided entirely by the preferred self-contained-test
> design (research R3). Table intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
