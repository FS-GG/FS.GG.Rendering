---
schemaVersion: 1
workId: 1196-windowed-fullscreen
title: Windowed Fullscreen
stage: specify
changeTier: tier1
status: specified
publicOrToolFacingImpact: true
---

# Windowed Fullscreen Specification

Prose status: specified

## User Value
A borderless multi-output game window remains visible and its on-screen controls remain clickable after a display-mode change.

## Scope
- SB-001: Native windowed-fullscreen work-area selection and post-change logical-canvas/pointer fitting; safe game-shell configuration guidance and executable option-overlay examples.

## Non-Goals
- SB-002: Do not implement later lifecycle commands or Governance enforcement in this specification.

## User Stories
- US-001 (P1): As a user, I can A borderless multi-output game window remains visible and its on-screen controls remain clickable after a display-mode change.

## Acceptance Scenarios
- AC-001 [US-001] [FR-001]: Given Windowed Fullscreen is available, when the user exercises it, then they can A borderless multi-output game window remains visible and its on-screen controls remain clickable after a display-mode change.

## Functional Requirements
- FR-001: Switching or opening WindowedFullscreen resolves one monitor work area, observes the resulting surface before fitting presentation and inverse pointer coordinates, and tests the post-change inverse mapping; the game-shell teaches Fullscreen as its safe default and verifies no flags plus every individual option preserves unspecified behavior fields. (Stories: US-001; Acceptance: AC-001)

## Ambiguities
No material ambiguities recorded.

## Public Or Tool-Facing Impact
- This specification is an SDD lifecycle artifact and command-report contract input.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd clarify --work 1196-windowed-fullscreen`.
