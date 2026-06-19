# Contract: Visual Inspection Validation Rules

## Rule Vocabulary

Initial validators must support these rule ids:

- `required-region-present`
- `required-region-painted`
- `ordinary-regions-disjoint`
- `text-contained-in-owner`
- `clip-intent-classified`
- `overlay-overlap-classified`
- `visual-order-stable`
- `unsupported-required-fact`
- `identity-stable`

Rule ids are stable contract tokens and appear in findings, exceptions, summaries, and tests.

## Severity Vocabulary

- `pass`: rule passed.
- `info`: advisory finding.
- `warning`: non-blocking defect or caveat.
- `blocking`: required readiness blocker.
- `unsupported`: required fact cannot be inspected.
- `environment-limited`: host/environment prevents inspection and the limitation is explicitly recorded.

## Required Region Rules

`required-region-present` passes only when every required region is present with finite non-negative bounds.

`required-region-painted` passes only when every required root or section region has complete intentional coverage or a valid exception.

Failure requirements:

- Missing bounds produce a blocking finding.
- Missing required paint produces a blocking finding.
- Unsupported paint facts for required regions produce unsupported status.

## Overlap Rules

`ordinary-regions-disjoint` passes only when ordinary non-overlay regions do not overlap.

`overlay-overlap-classified` passes only when overlap involving overlays, popups, floating panels, or tooltips has explicit owner and reason.

Failure requirements:

- Unclassified ordinary overlap is blocking.
- Unclassified overlay overlap is blocking until a valid exception is supplied.
- Exceptions apply only to matching rule id and affected ids.

## Text Rules

`text-contained-in-owner` passes only when text is inside its owner bounds or classified as intentionally wrapped/truncated.

Failure requirements:

- Overflowing required text is blocking.
- Accidental clipped text is blocking.
- Unsupported required text facts are unsupported and cannot be accepted as deterministic evidence.
- Approximate measurement may pass with a warning only when no exact measurement is available and the text still fits with the configured tolerance.

## Clip Rules

`clip-intent-classified` passes only when clipping is absent, owned by a scroll/bounded-content role, or covered by a valid exception.

Failure requirements:

- Accidental clipping is blocking.
- Unsupported clipping on required content is unsupported.
- Hidden, virtualized, or off-screen content must be classified separately from accidental clipping.

## Identity and Ordering Rules

`identity-stable` passes when unchanged static nodes keep the same ids across repeated inspection runs.

`visual-order-stable` passes when repeated unchanged runs keep the same relative order for inspected visible nodes.

Failure requirements:

- Static node id churn creates a blocking finding for deterministic evidence.
- Dynamic nodes may be exempt only when marked dynamic with a reason.

## Intentional Exceptions

An exception is valid only when it includes:

- `exceptionId`
- `ruleId`
- `ownerId`
- non-empty `affectedIds`
- non-empty `reason`

Invalid exceptions are diagnostics and do not affect findings.

Unused exceptions are warnings so stale allowances do not remain hidden.

## Readiness Status Rules

Accepted:

- all required scopes inspected
- no blocking findings
- no unsupported required facts
- no invalid required exceptions

Blocked:

- any required scope has blocking findings

Incomplete:

- any required scope is not run or not inspected

Unsupported:

- required facts cannot be produced and no environment limitation is recorded

Environment-limited:

- required facts cannot be produced because of an explicitly recorded host/environment limitation

Not-run and not-inspected:

- remain visible in summaries and cannot be represented as accepted
