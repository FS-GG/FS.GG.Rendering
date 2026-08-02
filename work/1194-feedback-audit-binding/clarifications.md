---
schemaVersion: 1
workId: 1194-feedback-audit-binding
title: Feedback Audit Binding
stage: clarify
changeTier: tier1
status: clarified
sourceSpec: work/1194-feedback-audit-binding/spec.md
publicOrToolFacingImpact: true
---

# Feedback Audit Binding Clarifications

## Source Specification
- work/1194-feedback-audit-binding/spec.md

## Clarification Questions
- CQ-001 [AMB:AMB-001]: Should ordinary changes to every evidence locator be
  tolerated, or is the exception limited to the mutable excuse ledger?

## Answers
- CQ-001: The exception is limited to the one resolved excuse-ledger path. Every
  other citation remains digest-bound and fails closed when stale.

## Decisions
- DEC-001 [CQ-001] [AMB:AMB-001] [FR-001] [FR-002] [FR-003]: Copy the sibling
  binding check's narrow fixed-point rule: recognize only the resolved ledger,
  expose skipped checks in output, and retain stale-digest failures elsewhere.

## Accepted Deferrals
No accepted deferrals recorded.

## Remaining Ambiguity
- None. AMB-001 is resolved by DEC-001.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd checklist --work 1194-feedback-audit-binding`.
