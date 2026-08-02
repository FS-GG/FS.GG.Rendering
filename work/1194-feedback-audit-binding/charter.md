---
schemaVersion: 1
workId: 1194-feedback-audit-binding
title: Make feedback audit bindings converge after excuse-ledger updates
stage: charter
changeTier: tier1
status: chartered
policyPointers:
  - .fsgg/sdd.yml
  - .fsgg/agents.yml
  - .fsgg/policy.yml
  - .fsgg/capabilities.yml
  - .fsgg/tooling.yml
---

# Make feedback audit bindings converge after excuse-ledger updates Charter

## Identity
Repair the feedback-report validator's evidence-binding contract so accepted
feedback cycles remain verifiable after the one supported mutable artifact—the
excuse ledger—changes.

## Principles
- Preserve fail-closed digest validation for every non-ledger citation.
- Exempt by resolved filesystem identity, not a textual spelling that breaks
  under a symlinked skill root.
- Make skipped validation observable in command output.

## Scope Boundaries
- In: validator behavior and its tests within `template/feedback-report`.
- Out: report schema changes, bulk audit rewrites, and exemptions for other
  audit/report files.

## Policy Pointers
- Constitution II, VI and VIII: keep the validator's structured contract,
  automated proof, and explicit diagnostics coherent.
- Issue #1194 acceptance criteria are the product-specific delivery contract.

## Lifecycle Notes
- Tier 1: the validator has a durable tool-facing validation contract.
- The mandatory delivery-route decision is `sdd-required`; bind the completed
  work and readiness evidence to issue #1194 and its PR.
