---
schemaVersion: 1
workId: 1261-rendering-skills-v2-manifest
title: Schema-v2 manifest for Rendering Skills sidecars
stage: specify
changeTier: tier1
status: specified
publicOrToolFacingImpact: true
---

# Schema-v2 manifest for Rendering Skills sidecars Specification

Prose status: specified

## User Value
Every product skill is delivered with all of its verified sidecars through a non-rendering SDD scaffold.

## Scope
- SB-001: Publish FS.GG.Rendering.Skills 0.1.1 with a schema-v2 complete per-file manifest and prove the PR #899 consumer materializes it.

## Non-Goals
- SB-002: Do not implement later lifecycle commands or Governance enforcement in this specification.

## User Stories
- US-001 (P1): As a user, I can every product skill is delivered with all of its verified sidecars through a non-rendering SDD scaffold.

## Acceptance Scenarios
- AC-001 [US-001] [FR-001]: Given Schema-v2 manifest for Rendering Skills sidecars is available, when the user exercises it, then they can every product skill is delivered with all of its verified sidecars through a non-rendering SDD scaffold.

## Functional Requirements
- FR-001: All 18 product rows declare an exact files digest set, producer mutations fail closed, and the built PR #899 CLI materializes feedback-report sidecars into both roots without the undeclared-sidecar diagnostic. (Stories: US-001; Acceptance: AC-001)

## Ambiguities
No material ambiguities recorded.

## Public Or Tool-Facing Impact
- The published manifest is a cross-repository content contract consumed by FS.GG.SDD.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd clarify --work 1261-rendering-skills-v2-manifest`.
