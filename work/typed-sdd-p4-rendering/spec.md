---
schemaVersion: 1
workId: typed-sdd-p4-rendering
title: Typed Sdd P4 Rendering
stage: specify
changeTier: tier1
status: specified
publicOrToolFacingImpact: true
---

# Typed Sdd P4 Rendering Specification

Prose status: specified

## User Value
Consumers can explicitly select Typed SDD from the published raw Rendering template without losing Standard SDD or Freeform behavior.

## Scope
- SB-001: Additive lifecycle choice, template conditioning, guard semantics, documentation, package validation, installed-template proof, and publication handoff for fs-gg-ui.

## Non-Goals
- SB-002: Do not implement later lifecycle commands or Governance enforcement in this specification.

## User Stories
- US-001 (P1): As a user, I can consumers can explicitly select Typed SDD from the published raw Rendering template without losing Standard SDD or Freeform behavior.

## Acceptance Scenarios
- AC-001 [US-001] [FR-001]: Given Typed Sdd P4 Rendering is available, when the user exercises it, then they can consumers can explicitly select Typed SDD from the published raw Rendering template without losing Standard SDD or Freeform behavior.

## Functional Requirements
- FR-001: Explicit typed-sdd, sdd, and none installations succeed with distinct expected trees; omitted selection remains byte-equivalent to explicit sdd; unknown lifecycle and wrong-default mutations fail. (Stories: US-001; Acceptance: AC-001)

## Ambiguities
No material ambiguities recorded.

## Public Or Tool-Facing Impact
- This specification is an SDD lifecycle artifact and command-report contract input.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd clarify --work typed-sdd-p4-rendering`.
