---
schemaVersion: 1
workId: 1250-audit-binding-exceptions
title: Audit Binding Exceptions
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

# Audit Binding Exceptions Charter

## Identity
- Work id: `1250-audit-binding-exceptions`
- Lifecycle stage: charter
- Status: chartered

## Principles
- Fail closed: an exception is usable only when every declared binding matches current evidence exactly.
- Keep exceptions narrow, versioned, deterministic, and observable; malformed or unused entries are errors.
- Preserve the existing no-ledger behavior and the immutable audit record.

## Scope Boundaries
- Change the canonical feedback-report producer, its contract, and focused tests.
- Materialize the producer into a clean receiver and prove byte-identical behavior there.
- Do not weaken audit digest validation, invent wildcard matching, or change unrelated feedback-report output.
- Keep SDD lifecycle ownership separate from optional Governance enforcement.

## Policy Pointers
- SDD policy comes from `.fsgg/sdd.yml` and `.fsgg/agents.yml`.
- Governance files are optional compatibility pointers and are not evaluated by this command.

## Lifecycle Notes
- Governing issue: `FS-GG/FS.GG.Rendering#1250`.
- Source finding: `FS-GG/.github#2659`.
- Next lifecycle action: `fsgg-sdd specify --work 1250-audit-binding-exceptions`.
