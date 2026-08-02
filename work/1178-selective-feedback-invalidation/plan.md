---
schemaVersion: 1
workId: 1178-selective-feedback-invalidation
title: Detect feedback-audit invalidation at commit time
stage: plan
changeTier: tier1
status: planned
sourceSpec: work/1178-selective-feedback-invalidation/spec.md
sourceClarifications: work/1178-selective-feedback-invalidation/clarifications.md
sourceChecklist: work/1178-selective-feedback-invalidation/checklist.md
publicOrToolFacingImpact: true
---

# Detect feedback-audit invalidation at commit time Plan

Prose status: planned

## Source Snapshot
- spec: work/1178-selective-feedback-invalidation/spec.md sha256:5ffaff2464829777a8aea3a37c28b88f63a7727750898cb1f34e66894cfebb17 schemaVersion:1
- clarifications: work/1178-selective-feedback-invalidation/clarifications.md sha256:8e6822c708af9b5d8b52abc8083798be4b2daf86189a98947f793fd0b3d9f19c schemaVersion:1
- checklist: work/1178-selective-feedback-invalidation/checklist.md sha256:ca7c6b7f6224b4cb62c7239626eec70b45db4c67c613ffd6c4bb72b869d9f4e9 schemaVersion:1

## Plan Scope
- Work item 1178-selective-feedback-invalidation is planned from the current specification, clarification, and checklist facts.
- Requirement count: 1.
- Clarification decision count: 0.
- Checklist result count: 1.

## Plan Decisions
- PD-001 [AC-001] [FR-001] complete: Build a deterministic index from each `feedback/audits/*.audit.json` finding's digest-bearing `file:` evidence to its normalized workspace-relative path; intersect that index with an explicit commit changed-path list, sort results, and never call full report/evidence validation.

## Contract Impact
- PC-001 [PD-001] command report: `feedback-tool.fsx check-invalidation --changed "path;path" [--root PATH]` exits zero only when no cited binding is touched, names audit/report/finding/locator for each invalidation, and fails closed for malformed audit JSON.

## Verification Obligations
- VO-001 [PD-001] [PC-001] semanticTest: Focused tests cover touched and untouched citations, malformed audit metadata, deterministic ordering, and 200-audit scale; an FSI smoke command proves the clean zero-hit result.

## Performance Intent
No performance intent is declared for this work item.

## Migration Posture
- PM-001 [PC-001] diagnoseOnly: Existing audit schema v1 is read without migration. Audits that cannot deserialize or omit required collection structure produce a diagnostic and block the check rather than being silently skipped.

## Generated View Impact
- GV-001 [PD-001] workModel: Refresh the SDD work model, analysis, evidence, verification, and ship verdict from the final authored plan and observed focused-test receipt.

## Accepted Deferrals
No accepted plan deferrals recorded.

## Planning Findings
No blocking planning findings recorded.

## Advisory Notes
- Optional Governance pointers remain compatibility facts only.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd tasks --work 1178-selective-feedback-invalidation`.
