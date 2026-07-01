# Phase 0 Research: fix scaffold skill-tree leak

Resolves the unknowns behind the fix. No `[NEEDS CLARIFICATION]` remained after spec; the open scope decision (which lifecycles keep `.claude/skills/`) is resolved below in R1.

## R1 — Gating discriminator

**Decision**: Gate the leaking sources on **`lifecycle == "spec-kit"`** (append `&& lifecycle == "spec-kit"` to the existing `profile` predicate), not on `lifecycle != "sdd"`.

**Rationale**:
- Every other `.claude/`-targeted source in `template.json` is already `lifecycle == "spec-kit"`-gated: the base `.claude/` re-emit (`template/base/.claude/` → `.claude/`), the base agent-skill mirror (`.agents/skills/` → `.claude/skills/`), and the sample-pack/feedback skills (`… && lifecycle == "spec-kit"`). The 9 per-profile `.claude/skills/fs-gg-*/` sources are the **only** `.claude/`-targeted sources missing the clause — an omission, not a design.
- The `lifecycle` symbol contract (`template.json` `symbols.lifecycle`) states `sdd` and `none` both "suppress the gated lifecycle set" and `none` = "**same template-level output as `sdd`**." A `lifecycle != "sdd"` gate would leave `none` still emitting `.claude/skills/` copies — inconsistent with its own contract. `lifecycle == "spec-kit"` makes `sdd` and `none` identical, as required.

**Consequence (intended)**: `none` stops emitting `.claude/skills/` UI-skill copies. This is a deliberate correction, not a regression — `none` was leaking too; it simply had no orchestrator boundary-check to catch it. Only `spec-kit` (the standalone lane with no external lifecycle owner) keeps the `.claude/skills/` mirror.

**Alternatives considered**:
- `lifecycle != "sdd"` — rejected (leaves `none` inconsistent with the lifecycle contract; two behaviours where the contract says one).
- Drop `.claude/skills/` product copies under **all** lifecycles (delete the 9 sources) — rejected: regresses the standalone `spec-kit` product (Claude Code discovers skills via `.claude/skills/` when no SDD fan-out exists) and breaks Feature 204 GV-3 "spec-kit byte-identical to today."
- Also suppress `.agents/skills/` under `sdd`/`none` — rejected: `.agents/skills/` is the provider-owned canonical tree; #47's on-disk evidence shows `.agents/skills/` with exactly the profile's skills as the **correct** state, and the SDD orchestrator's skill fan-out reads `.agents/skills/` to re-supply the consumer trees.

## R2 — Leak surface

**Decision**: Exactly **9** sources change — the per-profile `.claude/skills/fs-gg-*/` product-skill copies for: `fs-gg-scene`, `fs-gg-symbology`, `fs-gg-skiaviewer`, `fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-ui-widgets`, `fs-gg-styling`, `fs-gg-layout`, `fs-gg-testing`.

**Rationale**: Enumerated `template.json` `sources[]` by target. The `.claude/skills/fs-gg-*/` sources carry only a `profile` predicate (e.g. `(profile == "app" || profile == "game")`); the `.agents/skills/fs-gg-*/` siblings carry the same predicate and are the provider surface (kept). The base agent-skill mirror and sample/feedback skills already carry the spec-kit clause. `.codex/skills/` appears **0** times in `template.json` — the template never writes it (matching #47's evidence that `.codex/skills/` had no leak), so no `.codex` change.

**Profile → leaked-skill count** (matches #47's diagnostic of 8 files for `game`): `app`/`game` = 8 (scene, symbology, skiaviewer, elmish, keyboard-input, ui-widgets, styling, layout); `headless-scene` = 2 (scene, symbology); `governed` = 3 (scene, symbology, testing); `sample-pack` = 4 (scene, symbology, skiaviewer, elmish). `fs-gg-testing` leaks only under `governed`.

## R3 — The existing gates assert the leaky shape (the non-obvious core)

**Decision**: The fix **must** correct `Feature219EmitFrameworkSkillsTests.fs` and `Feature204LifecycleTemplateTests.fs`; they are part of the fix surface, not passive verification.

**Rationale** (verified against the test source):
- `Feature219` G-EMIT (L140-155) iterates **every** `template/product-skills/` source and asserts (L145) `Condition` does **not** contain the spec-kit clause, and (L147-154) each id emits under both `.agents/skills/` and `.claude/skills/`. Adding the spec-kit clause to the 9 `.claude/skills/` sources fails L145.
- `Feature204` `gatedSourceAudit` (L118-138) classifies any source with `source` under `template/product-skills/` as **framework** (regardless of target) and flags a spec-kit clause as a violation (L127-128); GV-2 (L162) floors `framework >= 18` (9 skills × 2 surfaces). The fix would produce 9 violations and drop the framework count.

**Correction**:
- `Feature219`: make the L145 lifecycle assertion **surface-specific** — `.agents/skills/` product sources MUST NOT be spec-kit-gated; `.claude/skills/` product sources MUST be spec-kit-gated. Keep the `>= 18` count, the profile predicate, and the "both surfaces exist" structural check.
- `Feature204`: in `gatedSourceAudit`, route a product-skill source whose **target** is under `.claude/skills/` to the **lifecycle-workspace** bucket (must carry spec-kit clause); keep only `.agents/skills/` product-skill sources in **framework**. Update GV-2 floors: `framework >= 9`, `workspace >= 15`. Update the expected `gated-condition:` report string.

This is not weakening — the emission invariant becomes *more* precise (it now distinguishes the provider surface from the workspace mirror), and it encodes the cross-repo contract (provider never writes SDD-owned trees) that Feature 219/204 previously contradicted.

## R4 — Regression guard (US3 / FR-006)

**Decision**: The corrected Feature 204 classifier **is** the static regression guard; add a live `.claude/skills/`-absent observation under `sdd`/`none` for defense in depth. No brand-new test file.

**Rationale**: After the fix, *every* `.claude/skills/`-targeted source is spec-kit-gated, so Feature 204's uniform workspace rule ("`.claude`-targeted ⇒ must carry spec-kit clause") fails if any future feature re-adds an un-gated `.claude/skills/` copy — exactly the Feature 227 pattern (each new skill added an un-gated `.claude/skills/` row). The live observation (`.claude/skills/` product-skill count == 0 under `sdd`/`none`) proves the *scaffolded artifact*, closing the Feature 175 gap (green test ≠ correct artifact).

## R5 — Live scaffold observation (FR-008)

**Decision**: Reuse `scripts/validate-lifecycle-template.fsx` (`scaffold`, `frameworkSkillCount`) under `FS_GG_RUN_LIFECYCLE_VALIDATION=1` to capture before/after evidence for `sdd`, `none`, and `spec-kit`; record transcripts under `specs/228-fix-scaffold-skill-leak/readiness/`.

**Rationale**: The template is installed (`dotnet new list fs-gg-ui` resolves) and packed locally; the script already instantiates `dotnet new fs-gg-ui --profile <p> --lifecycle <l>` into a temp dir and inspects the emitted trees. Extend its skill-count helper from `.agents/skills/`-only to also count `.claude/skills/` product skills so the report records `sdd/<p>: claude-product-skills=0` (new) alongside `framework-skills-present=ok` (unchanged). The regenerated report is the artifact both gates read, so regenerating it is both the evidence and the gate refresh.

**Open item for `/speckit-tasks`**: determine whether `--emit-report` alone regenerates the readiness artifact or whether the live-scaffold env var is required; schedule the regeneration step accordingly (and the before-observation must run against the *pre-fix* template).
