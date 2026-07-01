# Implementation Plan: fs-gg-ui template must not write UI skills into orchestrator-owned skill trees

**Branch**: `228-fix-scaffold-skill-leak` | **Date**: 2026-07-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/228-fix-scaffold-skill-leak/spec.md`

## Summary

The `fs-gg-ui` template emits its per-profile UI product skills into **both** `.agents/skills/fs-gg-*/` (correct, provider-owned) **and** `.claude/skills/fs-gg-*/` (an intrusion into the SDD orchestrator-owned tree). Under `--lifecycle sdd` (`fsgg-sdd scaffold --provider rendering …`, e.g. `new-sdd-fullstack`), SDD's post-write boundary check rejects the `.claude/skills/` writes with `scaffold.providerWroteSddTree` (severity `error`), the scaffold returns `blocked`/`providerFailed`, and the full-stack script aborts before the governance-overlay and `doctor` steps ([FS-GG/FS.GG.Templates#47](https://github.com/FS-GG/FS.GG.Templates/issues/47), contract `fs-gg-ui-template`). The item's only board blocker (SDD#55) is Done, so it is unblocked; the fix lives in **this** repo's `.template.config/template.json`.

**Technical approach**: content + configuration + test-logic correction — **no `src/**` change, no `.fsi`/public-surface change, no dependency, no version bump**. The 9 per-profile `.claude/skills/fs-gg-*/` sources are the only ones that leak: they are `profile`-gated but **missing** the `lifecycle == "spec-kit"` clause that the base `.claude/` re-emit, the `.agents/skills/`→`.claude/skills/` base mirror, and the sample-pack/feedback skills already carry. The fix adds `&& lifecycle == "spec-kit"` to those 9 conditions, so `.claude/skills/` UI-skill copies emit **only** in the standalone Spec Kit lane; the 9 matching `.agents/skills/fs-gg-*/` sources stay profile-gated (provider surface, all lifecycles). `.codex/skills/` is never written by the template, so no `.codex` change is needed.

> **Standing-assumption note.** This is a template/config + test change. Deterministic tests can pass while the produced artifact is wrong (Feature 175: Feature 35's symbology skill was authored but never shipped). The observable behaviour here is *scaffold file placement*, so the "app to drive" is the **scaffold path**: `/speckit-tasks` MUST schedule an early live scaffold under `--lifecycle sdd` (before the test edits) that confirms the leak on the current template, and a matching after-observation that confirms `.claude/skills/` has zero UI skills and `.agents/skills/` is intact. The existing Feature 204/219 gates currently **assert the leaky shape**, so they are part of the fix surface, not just the verification.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (test edits only); JSON (`.template.config/template.json`); F# script (`scripts/validate-lifecycle-template.fsx`). No product/runtime code.

**Primary Dependencies**: Existing repo-owned Expecto gates in `tests/Package.Tests/` — `Feature204LifecycleTemplateTests.fs` (3-category gating audit + lifecycle report), `Feature219EmitFrameworkSkillsTests.fs` (per-profile emission matrix + both-surface assertion), and the env-gated live-scaffold script `scripts/validate-lifecycle-template.fsx` that self-provisions `specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md`. No new dependency.

**Storage**: N/A.

**Testing**: Expecto (`tests/Package.Tests`, entry `Program.fs`). No new test project. Verification = corrected Feature 204/219 gates (they must encode the fixed invariant) **plus** an env-gated live scaffold observation under `sdd`/`none`/`spec-kit` recorded under `specs/228-fix-scaffold-skill-leak/readiness/`.

**Target Platform**: N/A (template/package content delivered to scaffolded products).

**Project Type**: Template/package content within the F# rendering framework repo.

**Performance Goals**: N/A. **Constraints**: `spec-kit` output MUST stay byte-identical to today (Feature 204 GV-3); the provider-owned `.agents/skills/` set MUST NOT shrink under any lifecycle; `sdd` and `none` MUST produce identical skill-tree output; the fix MUST touch only the 9 `.claude/skills/` product-skill sources (not the `.agents/skills/` siblings, not `.codex`).

**Scale/Scope**: 9 one-clause edits in `template.json`; 2 test-logic corrections (Feature 219 per-source lifecycle assertion → surface-specific; Feature 204 classifier + GV-2 floors); 1 report-generator update + regenerated readiness artifact; 1 new `.claude`-absent-under-sdd observation; live scaffold evidence. No skill files added/removed, no catalog row change, no parity-report regen (no skill added).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* — **PASS.**

- **Change Classification** — **Tier 2 (internal/content correction).** No public F# API surface, no `.fsi`, no new dependency, no cross-package/version change. The change **does** alter observable *scaffold-emission* behaviour under `sdd`/`none` (the `.claude/skills/` UI-skill mirror disappears), but that moves the template into compliance with the **existing** `fs-gg-ui-template` contract (FR-008/FR-011: a provider MUST NEVER write into SDD-owned trees) rather than changing a contract — the coordination board labels #47 `bug`, not `contract-change`. Disclosed explicitly here because the existing Feature 204/219 gates encoded the pre-compliance shape and are corrected as part of the fix. *(Alternative reading: Tier 1 via "alters observable behaviour covered by existing specs." Rejected because there is no public F# surface, `.fsi`, baseline, dependency, or contract text to change — the Tier 1 artifact chain is N/A by construction, and the work is a compliance bug fix.)*
- **I. Spec → FSI → Semantic Tests → Implementation** — **N/A by construction.** No `.fs`/`.fsi` module; there is no public surface to draft in FSI. The applicable verification is the corrected repo-owned gates plus a live scaffold observation, scheduled by `/speckit-tasks`.
- **II. Visibility lives in `.fsi`** — N/A; no product module touched (only Expecto test bodies + a validation script).
- **III. Idiomatic Simplicity** — Honored: the fix is additive JSON clauses mirroring an existing pattern; the test edits reuse the existing classifier shape. No new abstraction, operator, SRTP, reflection, or computation expression.
- **IV. Elmish/MVU boundary** — N/A; no stateful/I-O workflow added.
- **V. Test Evidence** — The two gates that currently assert the leak are corrected to fail on the pre-fix template and pass on the fixed one; real scaffold evidence (before/after) is recorded, not synthetic. No assertion is weakened to green a build — the emission invariant is made *more* precise (surface-specific), not looser.
- **VI. Observability & Safe Failure** — N/A; no runtime path. (The SDD boundary check that surfaces the defect lives in the SDD repo.)
- **Controls/layout layering constraint** — N/A; no controls/layout code.
- **Local Skills are advisory** — Honored: no skill is added, removed, or turned into a gate; only scaffold *placement* of existing skills changes.

**Re-check after design**: unchanged — the design adds only JSON clauses + test-logic corrections + regenerated evidence; the gate remains PASS.

## Project Structure

### Documentation (this feature)

```text
specs/228-fix-scaffold-skill-leak/
├── spec.md              # Feature specification (Tier 2 declared)
├── plan.md              # This file
├── research.md          # Phase 0 — gate discriminator, leak surface, test-conflict, guard design
├── data-model.md        # Phase 1 — source-map categories, per-lifecycle×surface emission matrix
├── quickstart.md        # Phase 1 — runnable verification recipe (gates + live sdd/none/spec-kit scaffold)
├── contracts/
│   ├── scaffold-emission-contract.md   # what lands per (lifecycle × surface × profile)
│   └── gate-assertion-contract.md      # the invariant the corrected gates must encode
├── checklists/          # requirements.md (spec quality)
└── readiness/           # Evidence: leak-repro (before), fixed-scaffold (after) for sdd/none/spec-kit,
                         #   agents-tree-intact, gate transcripts, success-criteria (produced in /implement)
```

### Source Code (repository root) — verified touch-points

Every path/line below is confirmed against the current tree (not assumed):

```text
.template.config/template.json
    # EDIT: append ` && lifecycle == "spec-kit"` to the condition of the 9 per-profile
    #   `.claude/skills/fs-gg-*/` product-skill sources (targets: fs-gg-scene, fs-gg-symbology,
    #   fs-gg-skiaviewer, fs-gg-elmish, fs-gg-keyboard-input, fs-gg-ui-widgets, fs-gg-styling,
    #   fs-gg-layout, fs-gg-testing). The 9 matching `.agents/skills/fs-gg-*/` sources are UNCHANGED.

tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs
    # EDIT (G-EMIT, ~L138-155): the per-source lifecycle assertion becomes surface-specific —
    #   `.agents/skills/`-targeted product sources MUST NOT be spec-kit-gated; `.claude/skills/`-targeted
    #   product sources MUST be spec-kit-gated. Keep `sources.Length >= 18`, the profile predicate, and the
    #   "each id emits under BOTH surfaces" structural check (both source rows still exist). Update the comment.

tests/Package.Tests/Feature204LifecycleTemplateTests.fs
    # EDIT (`gatedSourceAudit`, ~L118-138): a product-skill source targeting `.claude/skills/` is
    #   classified LIFECYCLE-WORKSPACE (must carry spec-kit clause), not FRAMEWORK; only `.agents/skills/`
    #   product-skill sources are FRAMEWORK (profile-gated, lifecycle-independent).
    # EDIT (GV-2, ~L162-169): floors change framework `>=18`→`>=9`, workspace `>=6`→`>=15`; update the
    #   expected `gated-condition:` report string to the new wording.

scripts/validate-lifecycle-template.fsx
    # EDIT: update the `gated-condition:` line it emits; add a per-(sdd|none)×profile observation proving
    #   `.claude/skills/` holds ZERO product skills (extend the `.agents/skills/`-only `frameworkSkillCount`
    #   with a `.claude/skills/` product-mirror count). Keep `.agents/skills/` framework-present=ok.

specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md
    # REGEN (by the fsx, not hand-edited): the self-provisioned report both gates read.
```

**Structure Decision**: single-repo template/config change. No new project or module; edits are localized to the template manifest, two existing Package.Tests gates, and the lifecycle-validation script + its generated artifact.

## Phase 0 — Research

Consolidated in [research.md](./research.md). Key resolved decisions:

- **R1 — Gate discriminator = `lifecycle == "spec-kit"`** (not `lifecycle != "sdd"`). Mirrors every other `.claude/`-targeted source; makes `none` align to `sdd` per the lifecycle contract ("`none` = same template-level output as `sdd`"). `none` losing its `.claude/skills/` copies is a deliberate correction.
- **R2 — Leak surface = exactly 9 sources.** The 9 per-profile `.claude/skills/fs-gg-*/` sources; the 9 `.agents/skills/` siblings and the already-gated base/sample/feedback sources are out of scope. `.codex/skills/` is never written.
- **R3 — Existing gates assert the leak.** Feature 219 (each product source non-spec-kit-gated + both surfaces) and Feature 204 (`.claude/skills/` product sources classified framework, no spec-kit clause) must be corrected, or they fail on the fixed template. This is the non-obvious core of the work.
- **R4 — Regression guard = the corrected uniform invariant.** After the fix, *every* `.claude/skills/` source is spec-kit-gated; Feature 204's workspace rule (once the classifier is corrected) enforces "no product skill escapes to `.claude/skills/` outside the standalone lane," which would have caught Feature 227's addition. No separate test needed; add the live `.claude`-absent observation for defense in depth.
- **R5 — Live scaffold observation.** The template is installed and packable; `scripts/validate-lifecycle-template.fsx` already scaffolds `sdd`/`none`/`spec-kit` (env-gated) — reuse it for the before/after evidence (FR-008).

## Phase 1 — Design & Contracts

- [data-model.md](./data-model.md) — the source-map categories (provider-skill vs workspace-mirror), the corrected 3-category classifier, and the (lifecycle × surface × profile) emission matrix.
- [contracts/scaffold-emission-contract.md](./contracts/scaffold-emission-contract.md) — what a scaffolded product must contain per lifecycle/surface.
- [contracts/gate-assertion-contract.md](./contracts/gate-assertion-contract.md) — the exact invariants the corrected Feature 204/219 gates must encode.
- [quickstart.md](./quickstart.md) — the runnable verification recipe (gate runs + live scaffold before/after).
- Agent context: `CLAUDE.md` SPECKIT marker updated to point at this plan.

## Complexity Tracking

*No constitution violations to justify.* The one subtlety — the fix changes observable scaffold output under `sdd`/`none` — is a compliance correction to the existing `fs-gg-ui-template` contract, disclosed in the Constitution Check, not a new complexity.
