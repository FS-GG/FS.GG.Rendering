---
schemaVersion: 1
workId: feedback-invalidation-base-audits
title: Feedback Invalidation Base Audits
stage: clarify
changeTier: tier1
status: clarified
sourceSpec: work/feedback-invalidation-base-audits/spec.md
publicOrToolFacingImpact: true
---

# Feedback Invalidation Base Audits Clarifications

## Source Specification
- work/feedback-invalidation-base-audits/spec.md

## Clarification Questions
- CQ-001 [AMB:AMB-001] blocking answered: Should the audit index be the tree of
  `git merge-base <base> <head>`, or the tree of the ref supplied as `--base`?
- CQ-002 [AMB:AMB-002] blocking answered: What is the `--changed` form's audit-index
  subject, given it has no base ref to index from?

## Answers
- CQ-001: The tree of the ref supplied as `--base`. Two independent reasons agree.
  First, the issue's own normative text does: its acceptance criteria say "audits
  that exist in the supplied base/merged tree", and its reproduction reads
  `git cat-file -e origin/main:feedback/audits/...` — the base ref's tree, not a
  merge base. "Merge base" in the title is loose wording for "the merged state".
  Second, `git merge-base` would be strictly weaker for the property being
  protected: an audit merged into `origin/main` after the branch point IS merged
  and MUST keep guarding its evidence, and a merge-base index would silently drop
  it. Indexing the `--base` ref also makes the index and the changed-path set share
  one left-hand side, since `git diff <base> <head>` already diffs from that same
  tree; using a merge base for one and the base tip for the other would leave the
  two halves of the answer describing different states.
- CQ-002: Documented as a working-tree query, and named as one in the output. The
  `--changed` form is an advanced input in which the caller supplies the changed
  set directly; it carries no ref, so FR-001 has nothing to bind to and no
  fail-closed derivation is available to invent one. Removing it would break the
  documented advanced input for no gain, and requiring a ref would make it a second
  spelling of `--base/--head`. FR-005 is what makes the retained form honest: the
  verdict says `audit index: the working tree`, so a reader can never mistake a
  `--changed` verdict for a merged-state one.

## Decisions
- DEC-001 [CQ-001] [AMB:AMB-001] [FR-001] [FR-002] [FR-003]: Build the audit index
  from `git ls-tree` / `git cat-file blob` at the ref supplied as `--base`. Derive
  changed paths from `git diff --name-status --find-renames --find-copies <base>
  <head>` as today. Do not consult the working tree in this mode at all, so an
  audit's presence, absence or content on disk cannot affect the verdict.
- DEC-002 [CQ-002] [AMB:AMB-002] [FR-005]: Keep `--changed` with the working tree
  as its index, and make the indexed subject part of every verdict line in both
  forms rather than a documentation claim about them.
- DEC-003 [CQ-001] [FR-004]: An index that could not be enumerated or read never
  degrades to an empty index. An unresolvable ref, an unreadable blob and a
  malformed or schema-invalid audit each produce their own diagnostic, and the
  command exits non-zero on any of them. A base tree that genuinely contains no
  `feedback/audits` entries is a different fact from one that could not be read,
  and only the former is an empty index.

## Accepted Deferrals
No accepted deferrals recorded.

## Remaining Ambiguity
- None. AMB-001 is resolved by DEC-001 and AMB-002 by DEC-002.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd checklist --work feedback-invalidation-base-audits`.
