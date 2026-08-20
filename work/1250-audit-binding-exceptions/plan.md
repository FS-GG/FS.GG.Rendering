---
schemaVersion: 1
workId: 1250-audit-binding-exceptions
title: Audit Binding Exceptions
stage: plan
changeTier: tier1
status: planned
sourceSpec: work/1250-audit-binding-exceptions/spec.md
sourceClarifications: work/1250-audit-binding-exceptions/clarifications.md
sourceChecklist: work/1250-audit-binding-exceptions/checklist.md
publicOrToolFacingImpact: true
---

# Audit Binding Exceptions Plan

Prose status: planned

## Source Snapshot
- spec: work/1250-audit-binding-exceptions/spec.md sha256:7bee0ec5ab81d7e8322939ee0560907215a10283811f21d1420891d63dd509ec schemaVersion:1
- clarifications: work/1250-audit-binding-exceptions/clarifications.md sha256:a4dc884d32d14ae24726546823357e6b18dc33ed111ec9b2a1195ecedf26efb0 schemaVersion:1
- checklist: work/1250-audit-binding-exceptions/checklist.md sha256:cf2760d313530a1ed505a0e34f3b84cc423e7ccdfc42e3f5cbb6a35bdb615a4e schemaVersion:1

## Plan Scope
- Add strict ledger types and parsing beside the existing invalidation index in `FeedbackReportTool.fs`.
- Resolve the ledger and replacement evidence from the working tree for `--changed` and from the candidate head tree for `--base/--head`.
- Reconcile exact entries against invalidated bindings once, rejecting any duplicate or unused entry.
- Extend the canonical skill contract and focused package tests, including a copied-skill receiver fixture.

## Plan Decisions
- PD-001 [AC-001] [AC-003] [FR-001] complete: Use `schemaVersion: 1` with a required `exceptions` array and reject unknown JSON properties so typos cannot silently weaken a disposition.
- PD-002 [AC-001] [AC-002] [FR-002] complete: Extend each invalidated binding with its prior digest and key ledger entries by exact normalized scalar equality; no regex, prefix, or wildcard matching is accepted.
- PD-003 [AC-002] [AC-004] [FR-003] complete: Require one regular working-tree file or candidate-head mode-100644/mode-100755 blob, rejecting symlinks, trees, gitlinks, missing paths, and unreadable evidence before decoding and LF-normalizing replacement text with the feedback tool's existing digest convention; require a non-private `command:` locator whose tokens name the exact replacement path and never execute ledger-controlled text.
- PD-004 [AC-003] [AC-004] [AC-005] [FR-004] complete: Accumulate deterministic errors for malformed, duplicate, stale, mismatched, and unused entries; return remaining invalidations and separately report applied dispositions.
- PD-005 [AC-006] [FR-005] complete: Prove no-ledger compatibility, one-positive-only behavior, every negative class, and copied-skill execution in temporary Git repositories.

## Contract Impact
- PC-001 [PD-001] check-invalidation output: successful exact exceptions are printed as applied dispositions; absent ledgers preserve existing output and exit behavior; invalid ledgers fail through the existing error channel.

## Verification Obligations
- VO-001 [PD-001] [PD-002] [PD-003] [PD-004] [PD-005] [PC-001] semanticTest: Run `dotnet test tests/Package.Tests --filter FullyQualifiedName~FeatureFeedbackReportSkillTests`, invert an exact-binding field to observe red, and run the packaged FSI command in a clean receiver fixture.

## Performance Intent
No performance intent is declared for this work item.

## Migration Posture
- PM-001 [PC-001] compatible: Repositories without the ledger behave exactly as before; repositories adding it must use schema version 1 and exact entries.

## Generated View Impact
- GV-001 [PD-001] [PD-005] workModel: Regenerate the work model and agent guidance after implementation so source digests, task state, and receiver verification evidence remain current in the PR.

## Accepted Deferrals
No accepted plan deferrals recorded.

## Planning Findings
No blocking planning findings recorded.

## Advisory Notes
- Optional Governance pointers remain compatibility facts only.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd tasks --work 1250-audit-binding-exceptions`.
