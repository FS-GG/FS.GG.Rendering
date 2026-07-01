# Contract: the invariants the corrected gates MUST encode

The two repo-owned Expecto gates and the validation script currently encode the **Feature 228** shape (`.claude/skills/` product mirror is spec-kit-gated). Feature 229 corrects them to encode the **ADR-0011** invariant. Each corrected assertion MUST fail on the pre-fix (post-228) template and pass on the fixed template.

## Feature219EmitFrameworkSkillsTests.fs

**G-EMIT matrix test** (`expectedFrameworkSkills`, `emittedFor`) — **unchanged**: the `.agents/skills/` per-profile emission set is not affected (those sources have no `lifecycle` clause).

**G-EMIT surface test** (currently "framework skill sources are profile-gated; `.claude/skills/` mirror is spec-kit-only", L144-164) — **corrected** to:

- `sources.Length >= 9` (was `>= 18`).
- Every product-skill source carries a `profile ==` predicate (unchanged).
- Every product-skill source targets `.agents/skills/` — assert `s.Target.StartsWith ".agents/skills/"` for **all** sources; assert **no** source target starts with `.claude/skills/` or `.codex/skills/`.
- The `.agents/skills/` sources are **not** `spec-kit`-gated (unchanged sub-check).
- Replace the "each id emits under BOTH `.agents/skills/` and `.claude/skills/`" block with: each distinct id emits under `.agents/skills/`, and **no** id emits under `.claude/skills/`.
- Rename the test + update the comment to cite ADR-0011 §3/§4 (provider confined to `.agents/skills/`; orchestrator owns the mirror).

## Feature204LifecycleTemplateTests.fs

**`gatedSourceAudit`** (L102-144) — **corrected classifier + universal guard**:

- `isFrameworkSkill = source under template/product-skills/ && target under .agents/skills/` (unchanged).
- **Violation A**: a `template/product-skills/` source whose target is **not** under `.agents/skills/` is a hard violation — `"product-skill <src> -> <tgt> must target .agents/skills/ only (ADR-0011)"`.
- **Violation B (universal full-confinement guard)**: **any** source whose target starts with `.claude/skills/` is a hard violation — `"no template source may target .claude/skills/ (ADR-0011 full confinement)"`. This catches a re-added base mirror or sample/feedback row, not just product-skill rows. (Post-fix there are none, so `violations` stays empty.)
- Drop the classifier comment that routes the `.claude/skills/` product mirror into lifecycle-workspace.

**GV-2** (L163-177):

- Floors: `framework >= 9` (unchanged), `workspace >= 6` (was `>= 15`), `product >= 3` (unchanged).
- Expected report string updated to the new `gated-condition:` wording (below).

**GV-4 / GV-5** (L188-226) — **unchanged assertions**, refreshed comments: `sdd/<p>: claude-product-skills=0` and `none/<p>: claude-product-skills=0` still hold; update the "Feature 228" attribution to ADR-0011/#42.

**GV-3** (L180-184) — **unchanged**: explicit `spec-kit` == no-flag default stays byte-identical.

## scripts/validate-lifecycle-template.fsx

- Gating audit (L149-176): mirror the Feature 204 classifier correction **and the universal `.claude/skills/` guard**; `workspaceChecked >= 6` (was `>= 15`); drop "incl. `.claude/skills/` product mirror" from the assert messages.
- `claudeProductSkillCount`: exclude `fs-gg-project` (the base workspace skill) so the count is UI-product-only and reads 0 under every lifecycle.
- `gated-condition:` report line (L438): reworded (below).
- New observation: add `SpecKitClaudeProductSkills` to the verdict struct; emit `spec-kit/<profile>: claude-product-skills=0`; assert `claudeProductSkillCount def = 0` in `validateProfileLive` (the default/spec-kit scaffold).

## The new `gated-condition:` string (Feature 204 GV-2 expected == fsx emitted)

```
gated-condition: lifecycle-workspace sources carry lifecycle == "spec-kit"; framework product-skill sources target .agents/skills/ only (ADR-0011: providers never write .claude/skills/ or .codex/skills/) and are profile-gated, lifecycle-independent
```

(The exact string is defined once in the fsx and asserted verbatim by Feature 204 GV-2 — they MUST match byte-for-byte.)

## Live evidence (FR-009 / SC-005)

Under `FS_GG_RUN_LIFECYCLE_VALIDATION=1`, the regenerated report MUST record, for each covered profile:
- `spec-kit/<p>: claude-product-skills=0` (new),
- `sdd/<p>: claude-product-skills=0`, `none/<p>: claude-product-skills=0` (unchanged),
- `sdd/<p>: framework-skills-present=ok`, `none/<p>: framework-skills-present=ok` (unchanged),
- `result: pass`.
