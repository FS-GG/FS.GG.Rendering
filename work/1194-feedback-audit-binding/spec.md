---
schemaVersion: 1
workId: 1194-feedback-audit-binding
title: Feedback Audit Binding
stage: specify
changeTier: tier1
status: specified
publicOrToolFacingImpact: true
---

# Feedback Audit Binding Specification

Prose status: specified

## User Value
Maintainers can validate accepted feedback cycles after normal evidence evolution without rewriting an audit record.

## Scope
- SB-001: The feedback-report validator’s handling of checked evidence and the sole mutable excuse ledger; no changes to report format or broader audit exemptions.

## Non-Goals
- SB-002: Do not implement later lifecycle commands or Governance enforcement in this specification.

## User Stories
- US-001 (P1): As a maintainer, I can revalidate an accepted feedback cycle after
  the excuse ledger changes without rewriting the critic's audit record.

## Acceptance Scenarios
- AC-001 [US-001] [FR-001]: Given an audit cites the resolved excuse ledger and
  the ledger has changed, when validation runs, then validation succeeds and
  reports that citation as unchecked.
- AC-002 [US-001] [FR-002]: Given a citation resolves to any non-ledger file whose
  digest changed, when validation runs, then validation fails with stale-binding
  evidence.
- AC-003 [US-001] [FR-003]: Given the ledger is reached through a symlinked skill
  root, when validation runs, then the exemption is still recognized by resolved path.

## Functional Requirements
- FR-001: Validation MUST exempt only a citation whose resolved path is the feedback excuse ledger and MUST print every citation it leaves unchecked. (covers AC-001)
- FR-002: Validation MUST continue to reject a digest mismatch for every non-ledger citation. (covers AC-002)
- FR-003: Validation MUST decide the ledger exemption using resolved paths so equivalent symlinked paths have identical behavior. (covers AC-003)

## Ambiguities
- AMB-001: The acceptance criterion's phrase "unrelated file changed" could mean
  all audit citations should become history-insensitive. This work treats the
  issue's sibling-tool precedent as authoritative: only the mutable excuse ledger
  is exempt; all other stale citations remain fail-closed.

## Public Or Tool-Facing Impact
- `feedback-tool.fsx validate` gains explicit output for a citation intentionally
  not digest-checked; its exit behavior for all other stale citations is preserved.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd clarify --work 1194-feedback-audit-binding`.
