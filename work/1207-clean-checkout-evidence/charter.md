---
schemaVersion: 1
workId: 1207-clean-checkout-evidence
title: Clean-checkout-safe feedback evidence locators
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

# Clean-checkout-safe feedback evidence locators Charter

## Identity
Make feedback evidence reviewable from the immutable Git snapshot that a report
names, rather than accidentally accepting a generated artifact from a dirty
worker checkout.

## Principles
- `file:` means a regular file in the report's declared commit.
- Unknown Git state is a validation failure, never an implicit clean checkout.
- Generated render and performance output remains useful evidence through a
  specific reproducible `command:` locator when committing the output is wrong.

## Scope Boundaries
- In: feedback-tool validation, packaged-skill fixtures, and author guidance.
- Out: feedback/audit schema changes and executing arbitrary command locators.

## Policy Pointers
- SDD policy comes from `.fsgg/sdd.yml` and `.fsgg/agents.yml`.
- Governance files are optional compatibility pointers and are not evaluated by this command.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd specify --work 1207-clean-checkout-evidence`.
