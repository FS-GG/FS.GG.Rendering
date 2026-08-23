---
schemaVersion: 1
workId: 1261-rendering-skills-v2-manifest
title: Schema-v2 manifest for Rendering Skills sidecars
stage: plan
changeTier: tier1
status: planned
sourceSpec: work/1261-rendering-skills-v2-manifest/spec.md
sourceClarifications: work/1261-rendering-skills-v2-manifest/clarifications.md
sourceChecklist: work/1261-rendering-skills-v2-manifest/checklist.md
publicOrToolFacingImpact: true
---

# Schema-v2 manifest for Rendering Skills sidecars Plan

Prose status: planned

## Source Snapshot
- spec: work/1261-rendering-skills-v2-manifest/spec.md sha256:d32725c81a706d92cec74afb5ba3ea5ad1b39b6db0f35b38a665fcee2581adc3 schemaVersion:1
- clarifications: work/1261-rendering-skills-v2-manifest/clarifications.md sha256:5b4d59fb5f2ab0d35d6ff6cebb05537c04127d5471bc29d064f78158ca70e26f schemaVersion:1
- checklist: work/1261-rendering-skills-v2-manifest/checklist.md sha256:21671187802ad010d077a1a4e10c562c0054fd0a3ab61594e836813957853e63 schemaVersion:1

## Plan Scope
- Work item 1261-rendering-skills-v2-manifest is planned from the current specification, clarification, and checklist facts.
- Requirement count: 1.
- Clarification decision count: 0.
- Checklist result count: 1.

## Plan Decisions
- PD-001 [AC-001] [FR-001] complete: Generate schemaVersion 2 rows from each supplied skill directory, retain each canonical SKILL.md digest in sha256, and reject a directory whose declared file paths or canonical digests do not close its bytes before staging.

## Contract Impact
- PC-001 [PD-001] manifest schema: FS.GG.Rendering.Skills 0.1.1 publishes a compatible top-level SKILL.md digest plus a complete ordered files array of path and canonical digest records for every selected product skill; FS.GG.SDD PR #899 consumes that exact schema at scaffold time.

## Verification Obligations
- VO-001 [PD-001] [PC-001] verification: Run verify-package and its JUnit receipt, mutate a feedback-report sidecar to observe the verifier red, run focused manifest tests, pack 0.1.1, and use the direct built PR #899 CLI with the local candidate package to prove both roots receive every feedback-report file without the undeclared-sidecar diagnostic.

## Performance Intent
No performance intent is declared for this work item.

## Migration Posture
- PM-001 [PC-001] additive-compatible: schema v2 retains the existing per-row SKILL.md sha256 field so schema-v1 readers retain their canonical body verification; v2-aware consumers additionally require the closed files set and fail rather than materialize partial sidecars.

## Generated View Impact
- GV-001 [PD-001] workModel: readiness/1261-rendering-skills-v2-manifest/work-model.json and all lifecycle views are regenerated from this current contract plan before review.

## Accepted Deferrals
No accepted plan deferrals recorded.

## Planning Findings
No blocking planning findings recorded.

## Advisory Notes
- Optional Governance pointers remain compatibility facts only.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd tasks --work 1261-rendering-skills-v2-manifest`.
