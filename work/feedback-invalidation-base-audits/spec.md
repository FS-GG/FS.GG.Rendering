---
schemaVersion: 1
workId: feedback-invalidation-base-audits
title: Feedback Invalidation Base Audits
stage: specify
changeTier: tier1
status: specified
publicOrToolFacingImpact: true
---

# Feedback Invalidation Base Audits Specification

Prose status: specified

## User Value
A worker can repair review findings on a candidate branch without the commit-time invalidation check refusing the repair on the strength of an audit that the same candidate introduced.

## Scope
- SB-001: The check-invalidation command's audit-index subject and its --base/--head and --changed forms, the producer library behind them, the skill wording that documents them, and the generated skill-manifest digest bound to that wording.

## Non-Goals
- SB-002: Do not implement later lifecycle commands or Governance enforcement in this specification.
- SB-003: Do not change the report or audit JSON schema, the `validate` command's
  digest binding, or the semantics of the `audit-binding-exceptions.json` excuse
  ledger. That ledger's declared-but-never-consumed defect is a distinct root cause
  tracked as `FS-GG/.github#2659`; the two stories stay separate.
- SB-004: Do not rewrite, rename or relax any existing audit record. The evidence
  history is the thing being protected, not an obstacle to route around.

## User Stories
- US-001 (P1): As a worker repairing review findings, I can change evidence that
  only my own not-yet-merged audit cites and still get a green commit-time
  invalidation check, so an authorized repair round is not a fixed point.
- US-002 (P1): As a maintainer, I can rely on the check to keep refusing a
  candidate that touches evidence cited by a genuinely merged audit — including a
  candidate that tries to clear the refusal by deleting or rewriting that audit.
- US-003 (P2): As a reader of the check's output, I can see which tree it indexed,
  so a green verdict states what it examined rather than leaving it to be inferred.

## Acceptance Scenarios
- AC-001 [US-001] [FR-001] [FR-002]: Given an audit that exists at HEAD but not in
  the tree of the ref supplied as `--base`, and a candidate that changes a path
  that audit cites with a digest, when `check-invalidation --base REF --head REF`
  runs, then it passes and reports no invalidated binding.
- AC-002 [US-002] [FR-001] [FR-003]: Given an audit that exists in the base tree
  and cites a path with a digest, and a candidate that changes that path, when
  `check-invalidation --base REF --head REF` runs, then it fails and names the
  audit, report, finding id and locator.
- AC-003 [US-002] [FR-003]: Given the same base-present audit, and a candidate
  that deletes or rewrites that audit file itself while changing the cited path,
  when the check runs, then it still fails — the index is the base tree, so the
  candidate cannot edit its way out of the refusal.
- AC-004 [US-002] [FR-001]: Given a candidate that renames, copies or deletes a
  path cited by a base-present audit, when the check runs, then both sides of a
  rename or copy and the removed source of a deletion are still in the changed set
  and the citation is still detected.
- AC-005 [US-002] [FR-004]: Given a ref that does not resolve, when the check runs,
  then it fails with a diagnostic that names the unresolvable ref, and reports no
  verdict about bindings.
- AC-006 [US-002] [FR-004]: Given an audit in the base tree whose JSON is malformed
  or whose schema is invalid, when the check runs, then it fails with a diagnostic
  naming that audit path, and a broken index cannot render a safe empty verdict.
- AC-007 [US-003] [FR-005]: Given any completed run in either input form, when the
  check reports its verdict, then the verdict names the tree it indexed — the base
  ref for `--base/--head`, and the working tree for `--changed`.

## Functional Requirements
- FR-001: In `--base REF --head REF` mode the audit index MUST be exactly the `feedback/audits/*.audit.json` documents present in the tree of the REF supplied as `--base`, while the changed-path set is still derived from base to head with rename, copy and delete detection. (Stories: US-001, US-002; Acceptance: AC-001, AC-002, AC-004)
- FR-002: A candidate-only audit MUST NOT invalidate the candidate that introduces it. (Stories: US-001; Acceptance: AC-001)
- FR-003: An audit present in the base tree MUST still invalidate a candidate that changes one of its digest-bound cited paths, including when the candidate deletes or rewrites that audit. (Stories: US-002; Acceptance: AC-002, AC-003)
- FR-004: An unreadable ref, an unreadable audit blob, and a malformed base audit MUST each fail closed with its own explicit diagnostic. (Stories: US-002; Acceptance: AC-005, AC-006)
- FR-005: Every verdict MUST name the tree it indexed, and the `--changed` form MUST state that its subject is the working tree. (Stories: US-003; Acceptance: AC-007)

## Ambiguities
- AMB-001: The issue title says the index must come from "the merge base", while
  its acceptance criteria say "audits that exist in the supplied base/merged tree"
  and its reproduction reads `origin/main:` directly. `git merge-base origin/main
  HEAD` and `origin/main` are different trees whenever main has advanced since the
  branch point, and they select different audit sets.
- AMB-002: The `--changed` form has no base ref to index from, so FR-001 cannot
  apply to it. Whether it should be removed, made to require an explicit index
  subject, or documented as a working-tree query is not settled by the issue text.

## Public Or Tool-Facing Impact
- `feedback-tool.fsx check-invalidation --base/--head` changes which audits it
  indexes. A candidate-only audit stops producing a failure; nothing that failed
  for a base-present audit starts passing.
- Both forms gain an explicit audit-index subject in their pass and fail output.
- `FeedbackReportTool.fs` gains the index types and the base-ref entry point. The
  existing `findInvalidatedAuditBindings` signature and behavior are preserved.
- `template/feedback-report/skill/SKILL.md` changes, so the SHA256 recorded for
  `fs-gg-feedback-report` in `template/skill-manifest/skill-manifest.json` changes
  with it.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd clarify --work feedback-invalidation-base-audits`.
