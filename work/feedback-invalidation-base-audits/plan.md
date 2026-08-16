---
schemaVersion: 1
workId: feedback-invalidation-base-audits
title: Feedback Invalidation Base Audits
stage: plan
changeTier: tier1
status: planned
sourceSpec: work/feedback-invalidation-base-audits/spec.md
sourceClarifications: work/feedback-invalidation-base-audits/clarifications.md
sourceChecklist: work/feedback-invalidation-base-audits/checklist.md
publicOrToolFacingImpact: true
---

# Feedback Invalidation Base Audits Plan

Prose status: planned

## Source Snapshot
- spec: work/feedback-invalidation-base-audits/spec.md sha256:1e71605736ebd1a3417acb9974513658a11399e570417677758e525dbde61835 schemaVersion:1
- clarifications: work/feedback-invalidation-base-audits/clarifications.md sha256:b507b1cf3a69a136d6d90f9a344affef063efe3847ebf5b0533ce0efbc6963b3 schemaVersion:1
- checklist: work/feedback-invalidation-base-audits/checklist.md sha256:4506132422b4ff3c38669d720994a2b19276c93ae7e6aaa55ab7b25a49bb7acd schemaVersion:1

## Plan Scope
- Change only `FeedbackReportTool.fs`, `feedback-tool.fsx`, the skill wording in
  `template/feedback-report/skill/SKILL.md`, the generated digest that binds that
  wording in `template/skill-manifest/skill-manifest.json`, the producer tests in
  `tests/Package.Tests/FeatureFeedbackReportSkillTests.fs`, and this SDD package.
- Preserve the audit/report JSON schema, the `validate` command, and the public
  behavior of `findInvalidatedAuditBindings` and `changedPathsFromNameStatus`.
- Requirement count: 5.
- Clarification decision count: 3.
- Checklist result count: 5.

## Plan Decisions
- PD-001 [AC-001] [AC-002] [AC-004] [FR-001] complete: Separate the *index* from
  the *scan*. Introduce `IndexedAudit` (workspace-relative path plus either the
  document's exact bytes in the indexed tree or the fail-closed reason they could
  not be read) and `AuditIndex` (a named subject, its audits, and any enumeration
  error). Re-express the existing scan as `findInvalidatedAuditBindingsIn` over an
  `AuditIndex`, and supply two producers: `workingTreeAuditIndex` (today's
  `Directory.EnumerateFiles` behavior) and `baseTreeAuditIndex`, which reads
  `git ls-tree -r -z --name-only <base> -- feedback/audits` and then
  `git cat-file blob <base>:<path>` for each `*.audit.json` entry through the
  library's existing private `runGit`. `checkInvalidationBetweenRefs` composes
  `baseTreeAuditIndex <base>` with the changed set from
  `git diff --name-status --find-renames --find-copies <base> <head>`, so the
  index and the changed set share one left-hand side by construction.
- PD-002 [AC-001] [FR-002] complete: In `--base/--head` mode consult no
  working-tree audit at all. A candidate-only audit is absent from the base tree
  and therefore absent from the index, so it cannot select itself. This is the
  whole of the defect's repair; nothing filters it out afterwards, because a
  post-hoc filter would be a second place for the two views to disagree.
- PD-003 [AC-002] [AC-003] [FR-003] complete: Because the index is the base tree
  and never the disk, an audit that is present at base keeps guarding its cited
  paths even when the candidate deletes, renames or rewrites the audit file. That
  closes the corruption route the issue names ("renaming, deleting, or rewriting
  the durable audit would make the product gate green") as a consequence of the
  same change rather than as an extra rule.
- PD-004 [AC-005] [AC-006] [FR-004] complete: Fail closed on three distinct
  inputs with three distinct diagnostics — `could not read the audit index at
  <ref>` for a non-zero `ls-tree`, `unreadable audit <path>` for a non-zero
  `cat-file`, and the existing `malformed audit <path>` family for JSON and schema
  faults. `AuditIndex.errors` flows into `AuditInvalidationCheck.errors`, which
  `feedback-tool.fsx` already turns into a non-zero exit, so an index that could
  not be built can never present as an empty index.
- PD-005 [AC-007] [FR-005] complete: Add `subject` to `AuditIndex` and to
  `AuditInvalidationCheck`, and print it on both the pass and the fail line —
  `audit index: base ref origin/main` or `audit index: the working tree`. Update
  the `check-invalidation` usage line and the skill's "Commit-time audit
  invalidation check" section to state the same subject in the same words, and
  regenerate `template/skill-manifest/skill-manifest.json` so its recorded SHA256
  for `fs-gg-feedback-report` matches the edited `SKILL.md`.

## Contract Impact
- PC-001 [PD-001] [PD-005] command report: `feedback-tool.fsx check-invalidation`
  keeps its exit contract (0 clean, 1 on any invalidation or any error) and its
  per-binding diagnostic fields, adds the indexed subject to its verdict lines,
  and changes which audits `--base/--head` indexes. `--changed` is unchanged in
  behavior and gains only the explicit subject.
- PC-002 [PD-001] library surface: `FeedbackReportTool.fs` adds `IndexedAudit`,
  `AuditIndex`, `workingTreeAuditIndex`, `baseTreeAuditIndex`,
  `findInvalidatedAuditBindingsIn`, `changedPathsBetween` and
  `checkInvalidationBetweenRefs`, and adds a `subject` field to
  `AuditInvalidationCheck`. `findInvalidatedAuditBindings` and
  `changedPathsFromNameStatus` keep their signatures and behavior.
- PC-003 [PD-001] process decoding: `runGit` pins stdout/stderr to UTF-8. The
  index now carries audit JSON through a pipe, and finding ids in this repository's
  own audits are non-ASCII (`§4.1`), so an inherited console encoding would decide
  whether an audit parses. Existing `runGit` callers read only hex SHAs and are
  unaffected either way.

## Verification Obligations
- VO-001 [PD-001] [PD-002] [PD-003] [PC-001] semanticTest: A real temporary Git
  repository, built by the test itself, in which a base commit carries one audit
  and the candidate commit adds a second. Assert that changing the base audit's
  cited path fails, that changing only the candidate audit's cited path passes,
  and that deleting the base audit in the candidate does not clear the failure.
  Source inspection is not evidence here: the defect was invisible in source
  review of the fsx and only shows against real refs.
- VO-002 [PD-004] [PC-001] semanticTest: Unresolvable-ref, unreadable-blob and
  malformed-base-audit cases each assert their own diagnostic text and a non-empty
  error list, over the same real repository.
- VO-003 [PD-005] [PC-001] semanticTest: Assert the subject string on both index
  producers and that the skill-manifest digest recomputes to the edited SKILL.md
  (the latter is already `Feature231SkillManifestTests`' standing gate).
- VO-004 [PD-001] gateInversion: Every gate added or modified is inverted once by
  breaking its subject, plus one non-vacuity leg for any gate whose subject is a
  collection, and the exact mutation and observed red are recorded.

## Performance Intent
No performance intent is declared for this work item.

## Migration Posture
- PM-001 [PC-001] compatible: No audit or report is rewritten. A candidate that
  was failing solely because of its own unmerged audit starts passing, which is
  the intended repair; no candidate that was passing starts failing, because the
  base-tree index is a subset of the working-tree index for every audit that is
  genuinely merged and the only additions are error paths that previously did not
  fail closed at all.
- PM-002 [PC-002] compatible: `AuditInvalidationCheck` gains a field. Every
  consumer in this repository constructs it only inside `FeedbackReportTool.fs`
  and reads it positionally nowhere.

## Generated View Impact
- GV-001 [PD-001] workModel: `readiness/feedback-invalidation-base-audits/work-model.json`
  is the readiness projection of this package's authored sources. Regenerate it
  after the implementation lands so the PR proves the authoring records and the
  generated view describe the same digests; a stale one is reported as
  `staleGeneratedView` rather than silently accepted.
- GV-002 [PD-005] skillManifest: `template/skill-manifest/skill-manifest.json` is a
  generated projection over the canonical `SKILL.md` bodies. It is regenerated with
  `dotnet fsi scripts/generate-skill-manifest.fsx` in this PR, and
  `Feature231SkillManifestTests` reds if it is not.

## Accepted Deferrals
No accepted plan deferrals recorded.

## Planning Findings
No blocking planning findings recorded.

## Advisory Notes
- Optional Governance pointers remain compatibility facts only.
- `FS-GG/.github#2659` covers a different root cause in the same producer file (an
  exception ledger declared but never consumed). It is deliberately not touched
  here; the two stories must not be merged.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd tasks --work feedback-invalidation-base-audits`.
