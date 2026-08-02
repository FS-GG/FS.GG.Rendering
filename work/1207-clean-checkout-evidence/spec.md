---
schemaVersion: 1
workId: 1207-clean-checkout-evidence
title: Clean-checkout-safe feedback evidence locators
stage: specify
changeTier: tier1
status: specified
publicOrToolFacingImpact: true
---

# Clean-checkout-safe feedback evidence locators Specification

Prose status: specified

## User Value
Feedback audits are reproducible from a clean checkout.

## Scope
- SB-001: Validate every file locator against a Git committed tree at the report head; fail closed when Git state cannot be established; preserve command locators.

## Non-Goals
- SB-002: Do not change feedback report or audit schema.

## User Stories
- US-001 (P1): As a user, I can feedback audits are reproducible from a clean checkout.

## Acceptance Scenarios
- AC-001 [US-001] [FR-001]: Given Clean-checkout-safe feedback evidence locators is available, when the user exercises it, then they can feedback audits are reproducible from a clean checkout.

## Functional Requirements
- FR-001: A file locator succeeds only when it resolves to a regular tracked file in the committed tree at the report head. (Stories: US-001; Acceptance: AC-001)
- FR-002: An untracked, ignored, or absent file locator fails with its locator, classification, and bounded remediation. (Stories: US-001; Acceptance: AC-001)
- FR-003: An unavailable Git repository or unresolvable report head fails validation closed. (Stories: US-001; Acceptance: AC-001)
- FR-004: A command locator remains valid for generated render or performance evidence without committing the generated artifact. (Stories: US-001; Acceptance: AC-001)
- FR-005: Packaged feedback skill fixtures exercise committed, untracked, ignored, absent, unavailable-Git, and command-locator cases. (Stories: US-001; Acceptance: AC-001)

## Ambiguities
No material ambiguities recorded.

## Public Or Tool-Facing Impact
- This specification is an SDD lifecycle artifact and command-report contract input.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd clarify --work 1207-clean-checkout-evidence`.
