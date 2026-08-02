---
schemaVersion: 1
workId: 1178-selective-feedback-invalidation
title: Detect feedback-audit invalidation at commit time
stage: specify
changeTier: tier1
status: specified
publicOrToolFacingImpact: true
---

# Detect feedback-audit invalidation at commit time Specification

Prose status: specified

## User Value
Maintainers see exactly which merged feedback finding becomes stale before a touching commit lands.

## Scope
- SB-001: The feedback-report helper, its command wrapper, skill instructions, focused tests, and SDD artifacts.

## Non-Goals
- SB-002: Do not implement later lifecycle commands or Governance enforcement in this specification.

## User Stories
- US-001 (P1): As a user, I can maintainers see exactly which merged feedback finding becomes stale before a touching commit lands.

## Acceptance Scenarios
- AC-001 [US-001] [FR-001]: Given Detect feedback-audit invalidation at commit time is available, when the user exercises it, then they can maintainers see exactly which merged feedback finding becomes stale before a touching commit lands.

## Functional Requirements
- FR-001: Given changed paths and audit JSON files, the checker deterministically reports each digest-bound file citation whose normalized path is changed, without invoking full report validation. (Stories: US-001; Acceptance: AC-001)

## Ambiguities
No material ambiguities recorded.

## Public Or Tool-Facing Impact
- This specification is an SDD lifecycle artifact and command-report contract input.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd clarify --work 1178-selective-feedback-invalidation`.
