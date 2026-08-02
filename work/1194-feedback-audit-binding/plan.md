---
schemaVersion: 1
workId: 1194-feedback-audit-binding
title: Feedback Audit Binding
stage: plan
changeTier: tier1
status: planned
sourceSpec: work/1194-feedback-audit-binding/spec.md
sourceClarifications: work/1194-feedback-audit-binding/clarifications.md
sourceChecklist: work/1194-feedback-audit-binding/checklist.md
publicOrToolFacingImpact: true
---

# Feedback Audit Binding Plan

Prose status: planned

## Source Snapshot
- spec: work/1194-feedback-audit-binding/spec.md sha256:4c0b5286864b032a315c8d65cbb6ea3858e9526ccd689bc0a67c984a3640a939 schemaVersion:1
- clarifications: work/1194-feedback-audit-binding/clarifications.md sha256:d044621df3f670f6c3bd00092739833810a08a1bc8ccce141f95e72421081f18 schemaVersion:1
- checklist: work/1194-feedback-audit-binding/checklist.md sha256:d1f9277ff2bf52225d4018d43859d99a2637f04fc9af5171534f1a5cdadb8e1c schemaVersion:1

## Plan Scope
- Change only `FeedbackReportTool.fs` and `feedback-tool.fsx` plus their SDD
  lifecycle records. Preserve the existing audit/report JSON schema.

## Plan Decisions
- PD-001 [AC-001] [FR-001] complete: Resolve both the configured excuse-ledger
  location and each citation locator before comparison; skip digest validation only
  when those resolved paths are equal, while collecting explicit unchecked output.
- PD-002 [AC-002] [FR-002] complete: Leave the existing digest comparison and
  failure aggregation intact for every locator that is not the resolved ledger.
- PD-003 [AC-003] [FR-003] complete: Add focused F# script-level regression cases
  covering direct and symlinked ledger paths, changed non-ledger evidence, and the
  emitted unchecked-citation summary.

## Contract Impact
- PC-001 [PD-001] validate output: `feedback-tool.fsx validate` keeps its
  fail-closed exit status for stale evidence and adds observable text for each
  intentionally unchecked ledger citation.

## Verification Obligations
- VO-001 [PD-001] [PC-001] semanticTest: Run focused feedback-report validation
  tests or an equivalent real script fixture proving the direct-path, symlink-path,
  and non-ledger stale cases.

## Performance Intent
No performance intent is declared for this work item.

## Migration Posture
- PM-001 [PC-001] compatible: Existing audits and reports need no rewrite. A
  historical ledger citation becomes explicitly unchecked at validation time;
  all pre-existing non-ledger digest mismatches remain actionable failures.

## Generated View Impact
- GV-001 [PD-001] workModel: The SDD readiness view records the source digests for
  this plan and evidence; regenerate it after implementation so the PR proves the
  authoring records and generated view are current.

## Accepted Deferrals
No accepted plan deferrals recorded.

## Planning Findings
No blocking planning findings recorded.

## Advisory Notes
- Optional Governance pointers remain compatibility facts only.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd tasks --work 1194-feedback-audit-binding`.
