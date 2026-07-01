# Implementation Plan: fs-gg-ui template emits UI skills to the provider-owned tree only (drop the `.claude/skills/` mirror)

**Branch**: `229-drop-claude-skills-mirror` | **Date**: 2026-07-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/229-drop-claude-skills-mirror/spec.md`

## Summary

Per **ADR-0011** (Accepted 2026-07-01), a scaffolded product's three agent-skill roots (`.claude/skills/`, `.codex/skills/`, `.agents/skills/`) are interchangeable and must each carry the byte-identical **union** of all skills; the **`fsgg-sdd` orchestrator** owns that mirror (SDD#57, `fsgg-sdd 0.4.0`). The consequence for this repo (ADR-0011 §4): the `fs-gg-ui` template **drops its `.claude/` UI-skill emission** and writes UI skills into `.agents/skills/` **only** — under **every** lifecycle. This unblocks the Rendering half of [FS.GG.Templates#47](https://github.com/FS-GG/FS.GG.Templates/issues/47) (`scaffold.providerWroteSddTree`) tracked here as [FS.GG.Rendering#42](https://github.com/FS-GG/FS.GG.Rendering/issues/42).

This **supersedes Feature 228**, which *gated* the 9 per-profile `.claude/skills/fs-gg-*/` product-skill sources to `lifecycle == "spec-kit"` (removing them under `sdd`/`none`, **keeping** them under `spec-kit`). ADR-0011 §3 makes provider confinement **unconditional**: those 9 `.claude/skills/` sources are **removed outright**, `spec-kit` included. Under `spec-kit` the template no longer mirrors UI skills into `.claude/skills/` (its Feature 228 FR-003 "spec-kit byte-identical" invariant is deliberately reversed).

**Technical approach**: content + configuration + test-logic correction — **no `src/**` change, no `.fsi`/public-surface change, no dependency**. The change is:
1. **Delete** the 9 per-profile `.claude/skills/fs-gg-*/` product-skill `sources` rows in `.template.config/template.json` (the matching `.agents/skills/fs-gg-*/` rows stay untouched).
2. **Correct the two repo-owned gates** (Feature 219 emission matrix, Feature 204 lifecycle audit) — they currently encode Feature 228's "`.claude/skills/` product mirror is spec-kit-gated" shape and will *fail* on the corrected template; they must instead assert the ADR-0011 invariant "**no** product-skill source targets `.claude/skills/`/`.codex/skills/` under any lifecycle."
3. **Update the validation script** (`scripts/validate-lifecycle-template.fsx`) — its classifier, workspace floor, `gated-condition:` report line, and a new `spec-kit` `claude-product-skills=0` observation — and regenerate the readiness report both gates read.
4. **Re-release** the `fs-gg-ui-template` coherent set (version bump + local pack) so the change is consumable under publish-before-flip (FR-008).

> **Standing-assumption note.** This is a template/config + test change; deterministic tests can pass while the produced artifact is wrong (Feature 175). The observable behaviour is *scaffold file placement*, so the "app to drive" is the **scaffold path**: `/speckit-tasks` MUST schedule an early live scaffold (before the edits) confirming the pre-fix `.claude/skills/` UI-skill leak under `spec-kit`, and a matching after-observation confirming `.claude/skills/` holds **zero** UI skills under `spec-kit`/`sdd`/`none` while `.agents/skills/` is intact. The existing Feature 204/219 gates currently **assert the leaky (spec-kit-gated) shape**, so they are part of the fix surface, not just the verification.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (test edits only); JSON (`.template.config/template.json`); F# script (`scripts/validate-lifecycle-template.fsx`). No product/runtime code.

**Primary Dependencies**: Existing repo-owned Expecto gates in `tests/Package.Tests/` — `Feature204LifecycleTemplateTests.fs` (3-category gating audit + lifecycle report), `Feature219EmitFrameworkSkillsTests.fs` (per-profile emission matrix + surface assertion), and the env-gated live-scaffold script `scripts/validate-lifecycle-template.fsx` that self-provisions `specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md`. No new dependency.

**Storage**: N/A.

**Testing**: Expecto (`tests/Package.Tests`, entry `Program.fs`). No new test project. Verification = corrected Feature 204/219 gates (they must encode the ADR-0011 invariant) **plus** an env-gated live scaffold observation under `sdd`/`none`/`spec-kit` recorded under `specs/229-drop-claude-skills-mirror/readiness/`.

**Target Platform**: N/A (template/package content delivered to scaffolded products).

**Project Type**: Template/package content within the F# rendering framework repo.

**Performance Goals**: N/A. **Constraints**: the provider-owned `.agents/skills/` set MUST NOT shrink under any lifecycle; `sdd` and `none` MUST produce identical skill-tree output; the fix MUST remove **every** `.claude/skills/…` target (9 product-skill rows + base mirror + sample/feedback) while leaving the `.agents/skills/` siblings, the base `.claude/` workspace row, and `.codex/` (never written) untouched; the base `fs-gg-project` workspace skill is exempt from the UI-product count; `spec-kit`'s `.claude/skills/` UI skills are deliberately removed (supersedes Feature 228 — note GV-3 still holds because it compares explicit-`spec-kit` vs the no-flag default of the *same* template, which stays byte-identical).

**Scale/Scope**: ~13 source-row deletions in `template.json` (9 product-skill + base mirror + sample + feedback); 2 test-logic corrections (Feature 219 surface assertion → `.agents`-only; Feature 204 classifier violations + universal `.claude/skills/` guard + GV-2 floor + report string); 1 script update (classifier/guard + workspace floor + `gated-condition:` line + `claudeProductSkillCount` excludes `fs-gg-project` + new `spec-kit` claude-product observation) + regenerated readiness artifact; live scaffold evidence; 1 template version bump + local pack. No skill files added/removed, no catalog row change, no parity-report regen (no skill added/removed).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* — **PASS.**

- **Change Classification** — **Tier 2 (internal/content correction).** No public F# API surface, no `.fsi`, no new dependency. The change **does** alter observable *scaffold-emission* behaviour under `spec-kit` (the `.claude/skills/` UI-skill mirror disappears there too), but that moves the template into compliance with **ADR-0011** (accepted) and the existing `fs-gg-ui-template` contract's provider-confinement requirement — it implements an accepted decision, it does not change a contract. Disclosed explicitly here because the existing Feature 204/219 gates encoded the Feature 228 (pre-ADR) shape and are corrected as part of the fix, and because this reverses Feature 228 FR-003 for the product mirror. *(Alternative reading: Tier 1 via "alters observable behaviour covered by existing specs." Rejected because there is no public F# surface, `.fsi`, baseline, dependency, or contract text to change — the Tier 1 artifact chain is N/A by construction, and the work implements an accepted ADR. The version bump is a re-release of unchanged package **contents' delivery vehicle**, required by issue #42's DoD, not a public-API contract change.)*
- **I. Spec → FSI → Semantic Tests → Implementation** — **N/A by construction.** No `.fs`/`.fsi` module; there is no public surface to draft in FSI. The applicable verification is the corrected repo-owned gates plus a live scaffold observation, scheduled by `/speckit-tasks`.
- **II. Visibility lives in `.fsi`** — N/A; no product module touched (only Expecto test bodies + a validation script).
- **III. Idiomatic Simplicity** — Honored: the fix **removes** JSON rows and **simplifies** the test classifiers (the surface-discrimination branch added in Feature 228 collapses back to "product skills → `.agents/skills/` only"). No new abstraction, operator, SRTP, reflection, or computation expression.
- **IV. Elmish/MVU boundary** — N/A; no stateful/I-O workflow added.
- **V. Test Evidence** — The two gates that currently assert the Feature 228 shape are corrected to fail on the pre-fix template and pass on the fixed one; real scaffold evidence (before/after, per lifecycle) is recorded, not synthetic. No assertion is weakened to green a build — the emission invariant is made *stricter* (no `.claude/skills/` product source at all, vs "spec-kit-gated").
- **VI. Observability & Safe Failure** — N/A; no runtime path. (The SDD boundary check that surfaces the defect lives in the SDD repo.)
- **Controls/layout layering constraint** — N/A; no controls/layout code.
- **Local Skills are advisory** — Honored: no skill is added, removed, or turned into a gate; only scaffold *placement* of existing skills changes (they stop being duplicated into `.claude/skills/`).

**Re-check after design**: unchanged — the design deletes JSON rows + corrects test logic + regenerates evidence + bumps the template version; the gate remains PASS.

## Project Structure

### Documentation (this feature)

```text
specs/229-drop-claude-skills-mirror/
├── spec.md              # Feature specification (Tier 2 declared)
├── plan.md              # This file
├── research.md          # Phase 0 — ADR-0011 mandate, leak surface, gate-conflict, version-bump decision
├── data-model.md        # Phase 1 — source-map categories, corrected per-lifecycle×surface emission matrix
├── quickstart.md        # Phase 1 — runnable verification recipe (gates + live sdd/none/spec-kit scaffold)
├── contracts/
│   ├── scaffold-emission-contract.md   # what lands per (lifecycle × surface × profile) after the fix
│   └── gate-assertion-contract.md      # the invariant the corrected gates must encode
├── checklists/          # requirements.md (spec quality) — already written
└── readiness/           # Evidence: leak-repro (before, spec-kit), fixed-scaffold (after) for sdd/none/spec-kit,
                         #   agents-tree-intact, gate transcripts, success-criteria (produced in /implement)
```

### Source Code (repository root) — verified touch-points

Every path/line below is confirmed against the current tree (post-Feature-228):

```text
.template.config/template.json
    # DELETE every `sources` row that targets `.claude/skills/…` (full confinement, ADR-0011 §3):
    #   (a) the 9 per-profile `.claude/skills/fs-gg-*/` product-skill rows (fs-gg-scene, fs-gg-symbology,
    #       fs-gg-skiaviewer, fs-gg-elmish, fs-gg-keyboard-input, fs-gg-ui-widgets, fs-gg-styling,
    #       fs-gg-layout, fs-gg-testing);
    #   (b) the base `.agents/skills/`→`.claude/skills/` mirror row;
    #   (c) the sample-pack `.claude/skills/fs-gg-samples/` and feedback `.claude/skills/fs-gg-feedback-capture/` rows.
    #   KEEP: the 9 matching `.agents/skills/fs-gg-*/` sources; the base `.claude/` WORKSPACE row
    #   (`template/base/.claude/` -> `.claude/`, which carries settings/hooks + the standalone `fs-gg-project`
    #   skill). Net: NO source targets `.claude/skills/…`. `.codex/skills/` is never written.
    # BUMP the template package version (see Versioning below) so the coherent set can re-release (FR-008).

tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs
    # EDIT (G-EMIT surface test, L144-164): flip the surface assertion from "each id emits under BOTH
    #   .agents/skills/ AND .claude/skills/ (mirror spec-kit-gated)" to "each product-skill source targets
    #   .agents/skills/ ONLY; NO product-skill source targets .claude/skills/ or .codex/skills/". Change
    #   `sources.Length >= 18` → `>= 9`. Keep the profile-predicate check and the `.agents` "not spec-kit-
    #   gated" check. Replace the "each id emits under both surfaces" block with "each id emits under
    #   .agents/skills/ and no id emits under .claude/skills/". Update test name + comment (cite ADR-0011).

tests/Package.Tests/Feature204LifecycleTemplateTests.fs
    # EDIT (`gatedSourceAudit`, L102-144): a `template/product-skills/` source whose target is NOT under
    #   .agents/skills/ is now a VIOLATION (ADR-0011 §3 provider confinement), not a silently-classified
    #   workspace mirror. Update the classifier comment (drop the ".claude/skills/ product mirror → workspace" note).
    # EDIT (GV-2, L163-177): floors change workspace `>=15`→`>=6` (framework `>=9`, product `>=3` unchanged);
    #   update the expected `gated-condition:` report string to the new wording (no ".claude/skills/ product mirror").
    # EDIT (GV-4/GV-5 comments, L199-204/L221-225): keep the `claude-product-skills=0` assertions (still hold);
    #   refresh the "Feature 228" attribution to cite ADR-0011/#42.
    # (GV-3 L180-184 is UNCHANGED: explicit spec-kit == no-flag default stays byte-identical after the fix.)

scripts/validate-lifecycle-template.fsx
    # EDIT (gating audit, L149-176): mirror the Feature 204 classifier change; workspace floor `>=15`→`>=6`;
    #   update the assert messages (drop "incl. .claude/skills/ product mirror").
    # EDIT (`gated-condition:` line, L438): new wording matching the Feature 204 GV-2 expected string.
    # EDIT (report + struct, L285-458): add a `spec-kit` `claude-product-skills=0` observation
    #   (SpecKitClaudeProductSkills field + `spec-kit/<profile>: claude-product-skills=0` report line),
    #   and assert `claudeProductSkillCount(def) = 0` in `validateProfileLive` (FR-003/SC-002/SC-005 for spec-kit).

specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md
    # REGEN (by the fsx, not hand-edited): the self-provisioned report both gates read.
```

**Versioning (FR-008).** The template package version is bumped as part of the coherent-set re-release. The exact bump + local pack + feed publish is executed by `/speckit-merge` (the repo's release/merge lane, which bumps local packages and packs the local feed); this plan records that a bump is **required** (issue #42 DoD "re-released / published to the org feed"). The registry flip in `FS-GG/.github` and the `FS.GG.Templates` provider re-pin are **cross-repo follow-ons** (publish-before-flip), out of this repo's implement scope, filed/tracked via the cross-repo-coordination protocol.

**Structure Decision**: single-repo template/config change. No new project or module; edits are localized to the template manifest, two existing Package.Tests gates, and the lifecycle-validation script + its generated artifact, plus a template version bump at merge.

## Phase 0 — Research

Consolidated in [research.md](./research.md). Key resolved decisions:

- **R1 — ADR-0011 mandates unconditional provider confinement.** The provider writes `.agents/skills/` only; the orchestrator owns the three-root union mirror (SDD#57). So the `.claude/skills/` UI-skill sources are removed under **every** lifecycle, not gated to `spec-kit`. This is the decisive difference from Feature 228 and resolves the "does spec-kit also drop the mirror?" fork without a `[NEEDS CLARIFICATION]`.
- **R2 — Leak surface = exactly 9 sources (delete, don't gate).** The 9 per-profile `.claude/skills/fs-gg-*/` sources Feature 228 spec-kit-gated. The 9 `.agents/skills/` siblings, the base `.agents/`→`.claude/` mirror (source[5]), and the sample/feedback `.claude/skills/` sources are out of scope (not the ADR-0011 §4 "UI product skills / Feature 219" surface; none fire under `sdd`). `.codex/skills/` is never written.
- **R3 — Existing gates assert the Feature 228 shape.** Feature 219 (each product id emits under both surfaces; `.claude` mirror spec-kit-gated; `sources.Length >= 18`) and Feature 204 (`.claude/skills/` product mirror classified lifecycle-workspace; `workspace >= 15`; `gated-condition:` string names the mirror) must be corrected or they fail on the fixed template. This is the non-obvious core of the work.
- **R4 — Regression guard = the corrected uniform invariant.** After the fix, *every* `template/product-skills/` source targets `.agents/skills/` only; a source targeting `.claude/skills/`/`.codex/skills/` is a hard violation in both gates. This would have caught Feature 227's `.claude/skills/` addition. Add the live `spec-kit` `claude-product-skills=0` observation for defense in depth.
- **R5 — GV-3 survives untouched.** GV-3 asserts explicit-`spec-kit` == no-flag default of the same template. Removing the `.claude/skills/` product sources changes *both* sides equally, so they stay byte-identical; GV-3 does not need to change (it is not a "vs prior release" diff).
- **R6 — Re-release is required (FR-008).** Unlike Feature 228 (merged unreleased), #42's DoD requires the coherent set re-released. The bump + pack rides `/speckit-merge`; the registry flip + Templates re-pin are cross-repo follow-ons under publish-before-flip.

## Phase 1 — Design & Contracts

- [data-model.md](./data-model.md) — the source-map categories, the corrected 3-category classifier (product-skill → `.agents/skills/` only), and the (lifecycle × surface × profile) emission matrix after the fix.
- [contracts/scaffold-emission-contract.md](./contracts/scaffold-emission-contract.md) — what a scaffolded product must contain per lifecycle/surface after the fix.
- [contracts/gate-assertion-contract.md](./contracts/gate-assertion-contract.md) — the exact invariants the corrected Feature 204/219 gates must encode.
- [quickstart.md](./quickstart.md) — the runnable verification recipe (gate runs + live scaffold before/after).
- Agent context: `CLAUDE.md` SPECKIT marker updated to point at this plan.

## Complexity Tracking

*No constitution violations to justify.* The one subtlety — the fix reverses Feature 228's `spec-kit`-keeps-the-mirror invariant — is the intended consequence of the accepted **ADR-0011**, disclosed in the Constitution Check and the spec, not a new complexity.
