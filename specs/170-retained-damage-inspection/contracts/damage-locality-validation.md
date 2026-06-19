# Contract: Damage Locality Validation

## Rule Vocabulary

Initial validators must support these rule ids:

- `retained-node-status-classified`
- `retained-identity-stable`
- `dirty-region-unioned`
- `empty-damage-explicit`
- `damage-localized-to-expected-region`
- `full-surface-localized-change-blocked`
- `broad-damage-requires-exception`
- `shifted-nodes-separated`
- `unsupported-damage-explicit`
- `not-inspected-damage-explicit`
- `antshowcase-structured-evidence-present`
- `screenshot-readiness-counts-preserved`

Rule ids are stable contract tokens and appear in findings, exceptions, summaries, tests, and readiness artifacts.

## Severity Vocabulary

- `info`: advisory retained/damage fact.
- `warning`: non-blocking caveat or review note.
- `blocking`: required readiness blocker.
- `unsupported`: required fact cannot be inspected.
- `environment-limited`: host/environment prevents inspection and the limitation is explicitly recorded.

## Retained Node Rules

`retained-node-status-classified` passes only when every required retained node has one of these explicit statuses:

- retained/reused
- repainted
- shifted
- shifted-and-repainted
- added
- removed
- unaffected
- unsupported

Failure requirements:

- Missing status for a required node is blocking.
- Unsupported required node facts are unsupported and cannot count as accepted evidence.
- Added/removed/shifted facts must include the relevant prior/current bounds or an unsupported fact explaining why bounds are unavailable.

`retained-identity-stable` passes when repeated unchanged inputs keep stable public node ids and finding ids.

Failure requirements:

- Static node id churn creates a blocking finding.
- Opaque retained correlation tokens may change only when the artifact marks them dynamic or implementation-scoped.

## Dirty Region Rules

`dirty-region-unioned` passes when dirty area is the true union of clipped visible dirty rectangles.

Failure requirements:

- Overlapping rectangles must not be double-counted.
- Dirty area must never exceed the visible frame area.
- Missing union area for required damage evidence is unsupported.

`empty-damage-explicit` passes when a no-change transition reports `empty` damage with zero dirty rectangles and zero dirty area.

Failure requirements:

- Omitted damage evidence for a no-change transition is unsupported.
- Empty damage must not be reported as not-inspected.

## Locality Rules

`damage-localized-to-expected-region` passes when dirty rectangles and affected nodes/regions are contained within the transition's declared expected affected regions or accepted tolerance.

Failure requirements:

- Dirty regions outside expected affected regions are blocking for required localized interactions unless a matching exception exists.
- Dirty percentage above the transition's maximum dirty percentage is broad damage and requires review or exception.

`full-surface-localized-change-blocked` passes only when a localized interaction does not dirty the full visible surface.

Failure requirements:

- Full-surface dirty area for a localized interaction is blocking unless a scoped intentional exception exists.
- A root-level theme, density, or size change can be accepted only when the transition is not declared localized or an intentional exception names the root-level cause.

`broad-damage-requires-exception` passes when broad damage is either absent or tied to a valid intentional exception.

Failure requirements:

- Broad damage without a matching exception is review-required or blocking according to the transition rule.
- Invalid exceptions do not affect findings.

## Shift Rules

`shifted-nodes-separated` passes when shifted nodes are reported separately from repainted nodes.

Failure requirements:

- A shifted-only node counted only as repainted creates a blocking finding.
- A repainted-only node counted as shifted creates a blocking finding.
- Nodes that are both shifted and repainted must be visible in both counts or through an explicit combined status.

## Unsupported and Not-Inspected Rules

`unsupported-damage-explicit` passes when missing retained/damage facts include fact name, owner, reason, diagnostic, and environment-limited flag.

`not-inspected-damage-explicit` passes when declared but intentionally skipped scopes appear in the summary as not-inspected.

Failure requirements:

- Unsupported required facts cannot produce accepted readiness.
- Not-inspected required scopes produce incomplete or blocked readiness according to the check.
- Environment-limited evidence can be environment-limited only with an explicit host/environment reason.

## AntShowcase Adoption Rules

`antshowcase-structured-evidence-present` passes when the migrated `charts-statistical` visual-shell assertion validates against retained inspection evidence for preferred size in light and dark themes.

`screenshot-readiness-counts-preserved` passes when existing AntShowcase screenshot target counts remain unchanged unless a deliberate change is documented.

Failure requirements:

- Missing structured evidence for the selected page/theme/size is blocking for sample adoption.
- Changed screenshot target counts require an explicit compatibility note; an unexplained change is blocking.

## Readiness Status Rules

Accepted:

- required retained/damage scopes inspected
- no blocking findings
- no required unsupported facts
- no invalid required exceptions
- command evidence recorded

Blocked:

- any required localized interaction has full-surface or broad unexcepted damage
- required shifted/repainted separation is missing
- required sample adoption evidence is missing

Review-required:

- broad damage exceeds scenario tolerance but is not marked blocking by the rule
- unused or stale exceptions exist
- optional damage evidence is incomplete

Unsupported:

- required retained/damage facts cannot be produced and no environment limitation is recorded

Environment-limited:

- required facts cannot be produced because of an explicitly recorded host/environment limitation

Not-run and not-inspected:

- remain visible in summaries and cannot be represented as accepted
