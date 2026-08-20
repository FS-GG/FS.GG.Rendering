---
schemaVersion: 1
workId: 1250-audit-binding-exceptions
title: Audit Binding Exceptions
stage: clarify
changeTier: tier1
status: clarified
sourceSpec: work/1250-audit-binding-exceptions/spec.md
publicOrToolFacingImpact: true
---

# Audit Binding Exceptions Clarifications

## Source Specification
- work/1250-audit-binding-exceptions/spec.md

## Clarification Questions
- CQ-001 [AMB:AMB-001] blocking open: Resolve source ambiguity AMB-001 before checklist.
- CQ-002 [AMB:AMB-002] blocking open: Resolve source ambiguity AMB-002 before checklist.

## Answers
- CQ-001 [AMB:AMB-001] decision: answer: Do not execute ledger text. Require a non-private command: locator that explicitly names the exact replacement path; execution remains external evidence.
- CQ-002 [AMB:AMB-002] decision: answer: Use the candidate head ref for the ledger and replacement bytes while retaining the immutable audit index from the base ref.

## Decisions
- DEC-001 [CQ-001] [AMB:AMB-001]: answer: Do not execute ledger text. Require a non-private command: locator that explicitly names the exact replacement path; execution remains external evidence.
- DEC-002 [CQ-002] [AMB:AMB-002]: answer: Use the candidate head ref for the ledger and replacement bytes while retaining the immutable audit index from the base ref.

## Accepted Deferrals
No accepted deferrals recorded.

## Remaining Ambiguity
No blocking ambiguity remains.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd checklist --work 1250-audit-binding-exceptions`.
