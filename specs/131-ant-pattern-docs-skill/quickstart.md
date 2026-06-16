# Quickstart / Validation Guide: F6 Ant pattern docs + skill

How to validate feature 131 end-to-end. All steps are headless and deterministic — no GL, display, or network. Run from repo root.

## Prerequisites

- The repo builds clean in Release (`dotnet build -c Release` → 0 warnings/0 errors).
- F1–F5 are merged (they are: feature 130 / `26aa4cf`), so the public token taxonomy, central resolver, and `wcag`/`ant` policy exist.

## V1 — The coverage check fails before the docs exist (red)

Author `tests/Controls.Tests/Feature131AntPatternDocsTests.fs` first, before writing the docs. Run:

```
dotnet test tests/Controls.Tests --filter "131"
```

**Expected**: the suite **fails** — `Family_coverage_is_bijective` reports the catalog categories with no pattern doc; template/skill cases fail too. This is the Constitution-V "fails before, passes after" evidence.

## V2 — Pattern docs satisfy family coverage (green)

Add one `docs/product/ant-design/patterns/<family>.md` per `Catalog.categories` value, each with valid front-matter and a `refs` block. Re-run V1's command.

**Expected**: `Family_coverage_is_bijective` and `Pattern_docs_have_required_refs` pass; reference-resolution cases pass for the refs written so far.

## V3 — Every reference resolves

Confirm no dangling references:

```
dotnet test tests/Controls.Tests --filter "131"
```

**Expected**: `All_control_refs_resolve`, `All_resolver_refs_resolve`, `All_token_refs_resolve`, `All_policy_refs_resolve`, `All_doc_links_resolve`, `No_unknown_ref_prefixes` all pass. To prove the guard bites, temporarily change one `token:` ref to a non-existent member and re-run — the test MUST fail naming that ref — then revert.

## V4 — Enterprise recipes present and marked groundwork

Add the six `docs/product/ant-design/templates/{workbench,list,detail,form,result,exception}.md`, each `status: groundwork`. Re-run.

**Expected**: `Each_template_recipe_present_once_and_groundwork` passes.

## V5 — Skill validates as advisory

Add `.claude/skills/fs-gg-ant-design/SKILL.md` with repo-standard front-matter, the pattern-doc links, the public-seam refs, and the no-React/DOM + no-per-theme-fork reminders. Re-run.

**Expected**: `Skill_is_advisory_and_reminds_layering` and `All_doc_links_resolve` pass. Full `131` suite green.

## V8 — Ant semantic-parts declared and mapped (FR-011 / SC-008)

Each pattern doc must declare its Ant semantic parts. Confirm via the same `131` filter:

```
dotnet test tests/Controls.Tests --filter "131"
```

**Expected**: `Pattern_docs_declare_semantic_parts` passes — every pattern doc carries ≥1 well-shaped `part:<Component>/<partName>` ref plus the companion `control:`/`token:`/`resolver:` refs (which resolve), and `No_unknown_ref_prefixes` accepts the `part` prefix. To prove the guard bites, temporarily malform a `part:` ref (e.g. drop the `/` → `part:Buttonicon`) and re-run — the test MUST fail naming that doc/ref — then revert. The central hub `docs/product/ant-design/reference/ant-llms-sources.md` (cataloging the three Ant LLM files + the curated snapshot) is the cited source for the part vocabulary; the check verifies it exists + lists all three files + is cited by skill/README (`Upstream_source_hub_is_central`) but never reads it for `part:` resolution (network-free, shape-only). Editorial accuracy of each part→region mapping is a **review** item, not asserted here.

## V9 — Central Ant source-of-truth hub (FR-012 / SC-009)

Confirm the three Ant LLM files are catalogued centrally and cited:

```
dotnet test tests/Controls.Tests --filter "131"
```

**Expected**: `Upstream_source_hub_is_central` passes — `docs/product/ant-design/reference/ant-llms-sources.md` exists, its text names all three files (`llms.txt`, `llms-full.txt`, `llms-semantic.md`), and both `SKILL.md` and `README.md` carry a `doc:` ref resolving to the hub. Repo-wide pointers (the `docs/product/` index, `CLAUDE.md`, and key existing skills) are a review item, not asserted here.

## V6 — Zero public-surface / token-value delta (Tier-2 proof)

```
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff --stat tests/surface-baselines/
dotnet fsi scripts/generate-design-tokens.fsx --check
```

**Expected**: `git diff` over `tests/surface-baselines/` is **empty** (no public surface changed — the test file and docs add no public API), and the token `--check` reports no drift (no token value changed). Confirms SC-005. *(Recall: `refresh-surface-baselines.fsx` always rewrites; verify via `git diff`, not exit code.)*

## V7 — Full suite stays green

```
dotnet test -c Release
```

**Expected**: 0 failures across all projects; skip count unchanged from the pre-feature baseline (F6 adds no GL/env-gated tests). Confirms SC-007.

## Acceptance summary

| Validation | Success criterion |
|---|---|
| V2 | SC-001 (family coverage) |
| V3 | SC-002 (references resolve) |
| V4 | SC-003 (six recipes, groundwork) |
| V5 | FR-004/FR-005 (advisory skill), SC-006 (a reader can apply a pattern from docs+skill alone) |
| V8 | FR-011 (semantic-parts enumerated + mapped), SC-008 (part mapping complete; companion refs resolve) |
| V9 | FR-012 (central source-of-truth hub), SC-009 (3 files catalogued + cited centrally) |
| V6 | SC-005 (zero surface/token delta) |
| V7 | SC-007 (full suite green) |
| Review | SC-004 (no React/DOM as implementation requirement) |
