# Implementation Plan: Emit Framework Skills On Every Lifecycle (Skills Follow the Product, Not the Lifecycle)

**Branch**: `219-emit-framework-skills` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/219-emit-framework-skills/spec.md`

## Summary

Make the framework's product-usage skills (`fs-gg-*`) **follow the product profile, not the lifecycle choice**. Today every `template/product-skills/fs-gg-*` source in `.template.config/template.json` carries `… && lifecycle == "spec-kit"`, so the recommended SDD scaffold path (`lifecycle=sdd`) and the `none` path emit **zero** skills — breaking the platform's "deliver framework knowledge to agents through vendored skills" thesis (board item FS-GG/FS.GG.Rendering#30, epic .github#74). The fix is to **drop the `lifecycle` clause from the 6 wired framework product-skill source pairs** (they remain profile-gated), wire the present-but-orphaned `fs-gg-symbology` source, and make the `docs/skillist-reference.md` catalog non-dangling.

The non-obvious cost — surfaced by reading the code, not the issue — is that this is **not a pure `template.json` edit**: the **Feature 204 lifecycle gate** (`scripts/validate-lifecycle-template.fsx` + `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`) hard-asserts the exact invariant *"every `source` whose target is under `.agents`/`.claude`/`.specify` carries `lifecycle == "spec-kit"`"*. Decoupling the framework skills (whose targets are `.agents/skills/fs-gg-*` and `.claude/skills/fs-gg-*`) deliberately violates that invariant, so the gate must be **amended** to recognize a third source category — *framework product-skill* (lifecycle-independent, profile-gated) — distinct from the *lifecycle-workspace* set (spec-kit-gated) and the *product* set (ungated). This amendment is the bulk of the engineering; the `template.json` change itself is one clause removed from 12 sources plus 2 added.

> **Standing assumption — root-cause hypotheses are unverified until the scaffold is run.**
> This feature's "app" is the **template-instantiation path** (`dotnet new fs-gg-ui` per `lifecycle × profile`), not the Skia viewer. The central claim — "dropping the lifecycle clause makes `sdd`/`none` emit the profile-appropriate `fs-gg-*` skills while `spec-kit` stays byte-identical (the skills already arrive there via the blanket `.agents/skills/` copy *and* the per-skill overwrite)" — is provisional until proven by a **real `dotnet new`** across the 3×4 lifecycle×profile matrix with byte-diffs. `/speckit-tasks` MUST front-load the live `FS_GG_RUN_LIFECYCLE_VALIDATION=1` scaffold matrix in the Foundational phase (confirm `spec-kit` diff-vs-today = none, and `sdd`/`none` now carry `.agents/skills/fs-gg-*`) **before** building the catalog/symbology/registry work on top of it. A green env-free verdict-core says nothing about what `dotnet new` actually emits (cf. Feature 175/204: deterministic checks pass while the real path differs).

## Technical Context

**Language/Version**: No application language change. The edited artifacts are: `.template.config/template.json` (dotnet-template JSON), `scripts/validate-lifecycle-template.fsx` (F# `dotnet fsi` script), `tests/Package.Tests/Feature204LifecycleTemplateTests.fs` (Expecto test, F# `net10.0`), `docs/skillist-reference.md` emission wiring, and the cross-repo registry YAML in `FS-GG/.github`. No `.fs`/`.fsi` **product** surface is added, removed, or changed.

**Primary Dependencies**: The dotnet-template engine's `sources`/`condition`/`modifiers` evaluation; the existing Feature 204 validator + report-gate pattern (`readiness/lifecycle-template-validation.md`, `FS_GG_RUN_LIFECYCLE_VALIDATION=1`); `FS.GG.TestSupport` (`RepositoryRoot`); the `speckit-merge` version-bump/release flow; and the `FS-GG/.github` `registry/dependencies.yml` + `docs/registry/compatibility.md` projection.

**Storage**: N/A. The "state" is the set of files a scaffold emits per `lifecycle × profile`, the validator's report, and the registry entry.

**Testing**: Evidence is the **Feature 204 gate, amended** — its env-free verdict-core (re-derives the gating classification straight from `template.json`) plus its env-gated **live `dotnet new` matrix** (`FS_GG_RUN_LIFECYCLE_VALIDATION=1`, 3 lifecycles × 4 profiles + byte-diffs) — augmented by a new `Feature219EmitFrameworkSkillsTests` asserting the **positive** facts (framework `fs-gg-*` skills present under `sdd`/`none` per profile; catalog references all resolve; symbology status resolved). No assertion is weakened: the Feature 204 contract is *re-specified*, not relaxed, and the new positive assertions are added failing-first.

**Target Platform**: The dotnet-template instantiation path on any `net10.0` host; the org GitHub Packages feed only insofar as a new coherent-set version is published to expose the change (release-cadence, owned by the merge flow). No GL/viewer involvement.

**Project Type**: Single repo; template-emission + cross-repo-coordination feature (no `src/` change).

**Performance Goals**: N/A (instantiation is event-driven).

**Constraints**:
- **No-regression on `spec-kit`** — every `lifecycle=spec-kit × profile` scaffold MUST stay byte-for-byte identical to pre-change (FR-004 / SC-003). The framework skills already land under `spec-kit` (blanket `.agents/skills/` copy *then* per-skill overwrite); removing the lifecycle clause must not change that path.
- **Profile mapping preserved exactly** (FR-002): app → {scene, skiaviewer, elmish, keyboard-input, ui-widgets}; headless-scene → {scene}; governed → {scene, testing}; sample-pack → {scene, skiaviewer, elmish} — unchanged; lifecycle no longer narrows it.
- **Lifecycle workspace stays gated** (FR-003): the `speckit-*` command skills, the blanket `.agents/skills/` repo copy, `.specify/`, the constitution, and the generated agent-context tree (`CLAUDE.md`/`AGENTS.md`) remain `spec-kit`-only.
- **Surface-additive** — no template **parameter** is added/removed/renamed; the change is additive for `sdd`/`none` and a no-op for `spec-kit` (FR-001/004).
- **No half-landing** — the `template.json` edit, the Feature 204 gate amendment, the catalog fix, and the symbology decision MUST land together; a skills-emit change that reds the Feature 204 gate is not "done."

**Scale/Scope**: 12 source clauses edited (6 framework skills × 2 destinations) + 2 sources added (symbology × 2 destinations) + 1 catalog-emission change in `template.json`; the validator script's classification function + thresholds; the Feature 204 test's mirror + GV-2/GV-4/GV-5 expectations; a new positive test file; one registry-note update in `FS-GG/.github`; and the closure of #30 + its board item.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Change Classification — Tier 1 (contracted change).** The feature changes the observable scaffold output of the `fs-gg-ui-template` cross-repo contract (what `dotnet new fs-gg-ui` emits for a given `lifecycle × profile`) and refines the registry's documented `lifecycle` gating. Per ADR-0001 it MUST update the registry (`FS-GG/.github` `registry/dependencies.yml` + compatibility projection) as part of resolution (FR-009). It is **additive and parameter-surface-neutral**: no symbol/parameter is added/removed/renamed, and the `spec-kit` path is byte-identical.

**No F# product public-surface impact.** No `.fs`/`.fsi` product module is added/removed/changed. The validator (`.fsx` script) and the Feature 204 test (`.fs` test) are edited, but neither is a packaged public module:
- **Principle I (Spec → FSI → Semantic Tests → Implementation)** — N/A for new product surface; there is no new public API. The "design through use" here is the consumer's `dotnet new fs-gg-ui --lifecycle sdd` invocation + `find … SKILL.md`, validated live (quickstart).
- **Principle II (Visibility in `.fsi`)** and surface-area baselines — N/A; no product module surface changes, so no baseline updates and no surface-drift implications.
- **Principle IV (Elmish/MVU boundary)** — N/A; no stateful F# workflow is added (the template engine evaluates `condition` expressions; the validator is a straight-line script).
- **Principle V (Test Evidence Is Mandatory)** — satisfied by **real** evidence: the amended Feature 204 live `dotnet new` matrix + the new positive test, authored failing-first. The env-free verdict-core keeps CI deterministic; the live proof is `FS_GG_RUN_LIFECYCLE_VALIDATION`-gated and **disclosed** as such (provenance line), not faked. No assertion is weakened — the Feature 204 invariant is *re-specified* (three categories instead of two) with its rationale in this plan.
- **Principle VI (Observability and Safe Failure)** — preserved: the validator's verdict-core `failwith`s loudly on any mis-gated source; the report is only written when the core passes. This feature adds no silent path.

**Engineering constraints** — no new dependency; no product code; `net10.0` unchanged; package identity stays `FS.GG.UI.*`; skills stay **advisory** (vendoring more of them adds no gate, blocks no build — consistent with the constitution's "Local Skills" section). The change touches only template wiring + its validation gate + the registry. **Gate: PASS — no violations; Complexity Tracking left empty.**

## Project Structure

### Documentation (this feature)

```text
specs/219-emit-framework-skills/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 — decisions: decouple mechanism, gate-amendment shape, catalog fix, symbology, registry delta
├── data-model.md        # Phase 1 — entities: source-category taxonomy, lifecycle×profile→skill-set matrix, state transitions
├── quickstart.md        # Phase 1 — runnable live-validation guide (dotnet new matrix / no-regression / skills-present / catalog / gate)
├── contracts/
│   └── fs-gg-ui-template-skill-emission.md   # the contract delta: skill emission follows profile, not lifecycle
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

No `src/` changes. Files this feature edits or reads:

```text
.template.config/template.json                         # EDIT — drop `&& lifecycle == "spec-kit"` from the 6 framework product-skill source pairs (12 sources); ADD fs-gg-symbology source pair (FR-007); make docs/skillist-reference.md emission non-dangling (FR-005/006)
template/product-skills/fs-gg-symbology/                # READ — the orphaned skill to wire (has SKILL.md + reference.fsx)
template/base/docs/skillist-reference.md               # READ/EDIT-WIRING — the catalog whose emission must stop dangling
scripts/validate-lifecycle-template.fsx                # EDIT — verifyGatedSources(): add the framework-product-skill category (target under .agents/.claude AND source under template/product-skills/ → NOT lifecycle-gated, profile-gated); adjust thresholds; update live-run gatedAbsent/diff classification so .agents/skills/fs-gg-* counts as product, not gated
tests/Package.Tests/Feature204LifecycleTemplateTests.fs # EDIT — mirror gatedSourceAudit() to the 3-category model; update GV-2 counts; update GV-4/GV-5 sdd/none expectations (framework skills now PRESENT under .agents/skills); keep GV-3 (spec-kit byte-identical) intact
tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs # NEW — positive facts: sdd/none emit profile-appropriate fs-gg-* SKILL.md; catalog refs all resolve; symbology status resolved
specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md # REGENERATED by the validator (gitignored readiness artifact)

# Cross-repo (FS-GG/.github), as the contract-change landing point:
registry/dependencies.yml                              # EDIT — fs-gg-ui-template.parameters.lifecycle notes: framework product-skills emit under ALL lifecycles (profile-gated); "Gates … .agents/ …" refined to exclude .agents/skills/fs-gg-*
docs/registry/compatibility.md                         # EDIT — projection of the above
```

**Structure Decision**: This is a template-emission + validation-gate + cross-repo-coordination feature, not a product-code feature — there is no module to place. Work is (1) decouple the framework product-skill sources from `lifecycle` in `template.json` (keep profile gating), (2) amend the Feature 204 validator + test to a three-category gating model so the gate stays honest under the new invariant, (3) wire `fs-gg-symbology` or record it as intentionally-unvendored (FR-007), (4) make `docs/skillist-reference.md` non-dangling (FR-005/006), (5) add positive `dotnet new` evidence that `sdd`/`none` now carry the skills while `spec-kit` stays byte-identical, and (6) update the `FS-GG/.github` registry + projection and close #30 / advance its board item.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.
