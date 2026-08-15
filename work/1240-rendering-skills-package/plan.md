---
schemaVersion: 1
workId: 1240-rendering-skills-package
title: Rendering Skills Package
stage: plan
changeTier: tier1
status: planned
sourceSpec: work/1240-rendering-skills-package/spec.md
sourceClarifications: work/1240-rendering-skills-package/clarifications.md
sourceChecklist: work/1240-rendering-skills-package/checklist.md
publicOrToolFacingImpact: true
---

# Rendering Skills Package Plan

Prose status: planned

## Source Snapshot
- spec: work/1240-rendering-skills-package/spec.md sha256:a934c9abb5b32aed6761ff0b033401e10fa2db2a6b1c97e3f6f5ffe4cf2bc016 schemaVersion:1
- clarifications: work/1240-rendering-skills-package/clarifications.md sha256:8daa43f68f81b90cab3dbc3fe80c09f4404a01baf561c4215ff1f2318f15b3df schemaVersion:1
- checklist: work/1240-rendering-skills-package/checklist.md sha256:0bc2f06785d65e799389a65dd92e804669a50f3491efad6aef82d3149b81c800 schemaVersion:1

## Plan Scope
- Work item 1240-rendering-skills-package is planned from the current specification, clarification, and checklist facts.
- Requirement count: 6.
- Clarification decision count: 1.
- Checklist result count: 6.

## Plan Decisions
- PD-001 [AC-001] [AC-003] [FR-001] complete: Derive the packed set from the committed producer manifest `template/skill-manifest/skill-manifest.json` rather than from a directory glob over `template/product-skills/`. The manifest already declares all 18 rows with a per-row `supplied-by` path, so a glob would silently miss the three rows sourced elsewhere. The delivered predicate is `scope == "product"` — one expression, and the only place the set is decided.
- PD-002 [AC-001] [FR-002] complete: Take the digest of record from the producer manifest, which `.github`'s `registry/skills.yml` is itself reconciled from ("registry = manifest = bytes"). The stager recomputes each `SKILL.md` digest from the source bytes and refuses to stage on any mismatch, so a stale manifest cannot be packed.
- PD-003 [AC-001] [FR-003] complete: Compute the digest as SHA-256 over the body's UTF-8 bytes with a UTF-8 BOM stripped and CRLF folded to LF, matching `scripts/generate-skill-manifest.fsx` and the vendored `Fsgg.SkillMirror`. FS.GG.Game's Python helper folds no line endings; copying it verbatim would disagree with this repository's own generator on a CRLF checkout, so the fold is added here deliberately.
- PD-004 [AC-002] [FR-004] complete: Make the verify gate assert the verdict of the same content-addressed function it uses to pass, then assert that verdict flips under a one-line mutation. Asserting merely that "a digest changed" would be tautological, because any appended byte changes a SHA-256.
- PD-005 [AC-003] [FR-005] complete: Pack each row's whole source directory, not only its `SKILL.md`. Three of the 18 rows carry sidecars the body depends on — `fs-gg-symbology` and `fs-gg-symbol-design` carry `reference.fsx` plus a `reference/` note, and `fs-gg-feedback-report` carries `scripts/feedback-tool.fsx` and `scripts/FeedbackReportTool.fs`, which is the very tool whose absence started this investigation. Shipping bodies without them would satisfy the letter of the digest requirement while delivering a skill that references files the tree does not have, which ADR-0014 clause 4 forbids.
- PD-006 [AC-004] [FR-006] complete: Add no `sources[]` row, `condition`, or `copyOnly` change to `.template.config/template.json`. This package is an additional channel; the template's emission is left byte-for-byte as it is, and the existing `Feature231SkillManifestTests` and `template-base-skill-union` gates continue to hold it.
- PD-007 [AC-001] [FR-001] complete: Keep `src/FS.GG.Rendering.Skills/` out of `FS.GG.Rendering.slnx`, as FS.GG.Game keeps its own skills project out of `FS.GG.Game.slnx`. The project compiles nothing; admitting it to the solution would put it in the path of `dotnet build`/`dotnet test`, the api-surface baseline sweep, and the framework-set version pin, none of which apply to a content-only package.
- PD-008 [AC-001] [FR-003] complete: Version the package on its own cadence in its own `<Version>`, starting at `0.1.0`, decoupled from both `<FsGgUiVersion>` (the framework set) and the `FS.GG.UI.Template` tag. Release on a `skills/v*` tag whose value the release workflow asserts against the evaluated `<Version>`, so the tag is a coherence check and never the source of truth.

## Contract Impact
- PC-001 [PD-001] package identifier: `FS.GG.Rendering.Skills` is a new public package that other repositories pin. Its packed layout is the contract: `rendering-skills/skill-manifest.json` (byte-identical to the committed producer manifest), `rendering-skills/skills/<id>/**` (each row's source tree), and `build/FS.GG.Rendering.Skills.props` exposing `$(FsggRenderingSkillsContentDir)` as the consumer handle.
- PC-002 [PD-005] delivered payload: the packed payload is each row's whole source directory, so a consumer materializing `fs-gg-feedback-report` receives `scripts/feedback-tool.fsx` alongside the body. Consumers that only mirror `SKILL.md` remain correct; the extra files are additive.
- PC-003 [PD-006] template emission: `.template.config/template.json` is unchanged, so the `fs-gg-ui` template's product-skill, feedback-report and samples emission is contract-identical before and after this change.
- PC-004 [PD-008] release trigger: the tag `skills/v<version>` starts publication of this package alone; it does not participate in the `v*` / `fs-gg-ui-template/v*` tag triple that `release-tags.yml` cuts.

## Verification Obligations
- VO-001 [PD-001] [PC-001] semanticTest: `verify-package.sh` step 1 stages from the committed manifest and asserts the staged set is exactly its 18 `scope: product` rows, and that the staged manifest is byte-identical to the committed one.
- VO-002 [PD-001] [PC-001] semanticTest: `verify-package.sh` step 2 drives the stager against a synthetic checkout carrying one invented row whose `supplied-by` is an out-of-tree path, proving new rows flow from the manifest alone and that an out-of-tree source is not a special case.
- VO-003 [PD-002] [PC-001] semanticTest: `verify-package.sh` step 4 unpacks the built nupkg and re-verifies every packed `SKILL.md` against the manifest digest, so the assertion is made against packaged bytes rather than the working tree.
- VO-004 [PD-004] [PC-001] mutationTest: `verify-package.sh` step 5 appends one line to a packed body and requires the step-4 verdict function to return non-zero and name that row. The exact mutation and the observed failure are recorded in `work/1240-rendering-skills-package/evidence.yml`.
- VO-005 [PD-005] [PC-002] semanticTest: `verify-package.sh` step 3 asserts the nupkg carries every sidecar file present in each row's source directory, so a stager that silently packed only `SKILL.md` reds.
- VO-006 [PD-006] [PC-003] semanticTest: `dotnet fsi scripts/generate-skill-manifest.fsx --check` and the existing `Feature231SkillManifestTests` remain green, and `git diff` reports no change to `.template.config/template.json` or to any file under the three emitted skill trees.
- VO-007 [PD-002] [PC-001] semanticTest: every packed `SKILL.md` digest is additionally compared to the `sha256` `.github`'s `registry/skills.yml` declares for the same row id, closing the loop the item's acceptance 2 names.

## Performance Intent
No performance intent is declared for this work item.

## Migration Posture
- PM-001 [PC-003] compatibility: Nothing is removed or re-gated. A tree created by the `fs-gg-ui` template keeps receiving these skills from the template exactly as before; a tree created by any other provider gains a channel it did not have. No consumer is required to adopt the package for existing behaviour to hold.
- PM-002 [PC-001] sequencing: `.github`'s `registry/dependencies.yml` cannot record the `rendering-skills` contract until the package is live on the feed, because `check-feed-coherence.py` asserts `package-version` is the newest live version unconditionally. That row and the `provider-scoped` to `delivered` flip are therefore owned by `.github#2639` and follow this publication, not precede it.
- PM-003 [PC-004] prerequisite: the first push to nuget.org requires an org-owner-created Trusted Publishing policy bound to this repository, the workflow filename `release-skills.yml`, and the package id `FS.GG.Rendering.Skills`. Until it exists the nuget.org step 401s. This is an external prerequisite of publication, recorded here so it is not discovered at tag time.

## Generated View Impact
- GV-001 [PD-006] workModel: no generated view in this repository changes. `template/skill-manifest/skill-manifest.json` is regenerated only by `scripts/generate-skill-manifest.fsx`, and this change adds no row and edits no body, so `--check` stays green and `.github`'s `registry/skills.yml` needs no reconcile. The package consumes that manifest; it is not a second producer of it.
- GV-002 [PD-001] stagedContent: `obj/skills-staging/` is derived at pack time from the committed manifest and is never committed, so the packed set cannot go stale against the manifest it is derived from.

## Accepted Deferrals
No accepted plan deferrals recorded.

## Planning Findings
No blocking planning findings recorded.

## Advisory Notes
- Optional Governance pointers remain compatibility facts only.
- The item text says "17 under `template/product-skills/`" and names two rows whose source lies outside it. The registry and this repository's own producer manifest both say 15 under `template/product-skills/` and three outside it: `fs-gg-feedback-report`, `fs-gg-samples`, and `fs-gg-project` at `template/base/.agents/skills/fs-gg-project/`. The total of 18 is correct; only the split is misstated. Deriving the set from the manifest makes the split irrelevant to correctness, which is why PD-001 is written that way.
- Publication itself is deliberately not performed inside this change. It is an irreversible external effect and it depends on PM-003, so it is carried as a declared post-merge obligation rather than folded into the pull request.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd tasks --work 1240-rendering-skills-package`.
