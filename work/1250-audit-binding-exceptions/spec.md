---
schemaVersion: 1
workId: 1250-audit-binding-exceptions
title: Audit Binding Exceptions
stage: specify
changeTier: tier1
status: specified
publicOrToolFacingImpact: true
---

# Audit Binding Exceptions Specification

Prose status: specified

## User Value
Feedback-report maintainers can accept bounded evidence evolution without rewriting immutable audit records.

## Scope
- SB-001: Consume and validate the canonical scripts/audit-binding-exceptions.json ledger in the feedback-report producer, contract, and focused materialization tests.

## Non-Goals
- SB-002: Do not implement later lifecycle commands or Governance enforcement in this specification.

## User Stories
- US-001 (P1): As a feedback-report maintainer, I can disposition one superseded immutable audit binding with current replacement evidence without rewriting the audit.
- US-002 (P1): As a reviewer, I receive a fail-closed diagnostic when any exception is malformed, stale, ambiguous, duplicate, or unused.

## Acceptance Scenarios
- AC-001 [US-001] [FR-001] [FR-002]: Given no exception-ledger file, when invalidation runs, then its verdict and diagnostics are unchanged.
- AC-002 [US-001] [FR-002] [FR-003]: Given one exact schema-v1 entry matching an audit, finding, locator, cited path, and prior digest, and its replacement path has the declared current digest and a bounded executable evidence locator, when invalidation runs, then only that binding is dispositioned and the disposition is reported.
- AC-003 [US-002] [FR-001] [FR-004]: Given malformed JSON, an unsupported schema, a missing or extra field, or an invalid path/digest/locator, when invalidation runs, then it fails closed.
- AC-004 [US-002] [FR-003] [FR-004]: Given a stale replacement digest, mismatched prior digest, mismatched binding, duplicate binding, duplicate entry id, or unused entry, when invalidation runs, then it fails closed.
- AC-005 [US-002] [FR-004]: Given an entry that attempts wildcard or directory-wide matching, when invalidation runs, then it fails as overbroad rather than excusing multiple bindings.
- AC-006 [US-001] [FR-005]: Given the producer template is materialized into a clean receiver, when the same positive and inversion matrix runs there, then the receiver has the producer behavior without a local fork.

## Functional Requirements
- FR-001: The invalidation checker MUST consume `scripts/audit-binding-exceptions.json` only as a strict schema-version-1 JSON object and MUST preserve existing behavior when that file is absent. (covers AC-001, AC-003)
- FR-002: An exception MUST bind one exact audit path, finding id, original locator, cited workspace path, and lowercase SHA-256 digest from the immutable audit. (covers AC-001, AC-002)
- FR-003: An applicable exception MUST name one exact replacement workspace path, its current lowercase SHA-256 digest, and a non-private `command:` evidence locator that explicitly inspects that replacement path. (covers AC-002, AC-004)
- FR-004: The checker MUST fail closed on malformed, stale, mismatched, duplicate, unused, or wildcard/directory-overbroad entries and MUST report applied dispositions deterministically. (covers AC-003, AC-004, AC-005)
- FR-005: The producer contract and focused tests MUST document and prove the immutable-audit boundary and MUST verify materialized receiver bytes with the same positive and negative cases. (covers AC-006)

## Ambiguities
- AMB-001: Whether "executable evidence locator" means the checker should execute arbitrary ledger text. Arbitrary execution would make an exception ledger a code-execution surface.
- AMB-002: Which tree supplies the ledger and replacement bytes for the ref-aware `--base/--head` form.

## Public Or Tool-Facing Impact
- `feedback-tool.fsx check-invalidation` gains strict exception-ledger validation and observable applied-disposition output. Existing invocations and the absent-ledger verdict remain compatible.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd clarify --work 1250-audit-binding-exceptions`.
