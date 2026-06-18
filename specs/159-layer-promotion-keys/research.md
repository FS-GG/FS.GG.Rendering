# Research: Layer Promotion and Content/Transform Key Split

## Decision: Split content identity from placement identity

**Rationale**: The current replay/picture key is safe because it folds placement into the cache
key, but that also forces re-recording when only position, scroll offset, or transform changes.
Feature 159 needs two comparable identities: a content identity over render-affecting local content
and a placement identity over where that content is drawn. Content identity equality permits reuse
only when the retained representation can be replayed in local boundary coordinates and the old and
new covered regions are damaged. If the implementation cannot prove local-coordinate replay, the
attempt re-records content and records a safe fallback reason.

**Alternatives considered**:

- Keep the current combined key: rejected because placement-only movement remains a miss and cannot
  satisfy the feature goal.
- Ignore placement changes for the replay key: rejected because old and new covered regions would
  not be damaged or reviewed.
- Treat transform or clip changes as content changes unconditionally: retained as a fallback for
  unsupported cases, rejected as the default because it would block the intended movement and scroll
  reuse.

## Decision: Use existing promotion thresholds and add explicit benefit evidence

**Rationale**: The repository already exposes `thresholds.PromotionReductionPercent = 30.0`,
`thresholds.SimpleSceneOverheadPercent = 5.0`, and a pure
`RetainedRender.promotionDecision` policy that observes stability, expected saved work, overhead,
and parity. Feature 159 should make those rules the accepted evidence contract rather than adding a
second heuristic. A boundary promotes only after three stable frames, parity success, at least 30%
repeated-work reduction for the candidate class, and expected saved work greater than measured
overhead.

**Alternatives considered**:

- Promote every stable boundary: rejected because cheap stable content can regress.
- Use a fixed node-count threshold only: rejected because existing counters already distinguish
  saved work, overhead, replay hits, and demotion cost more directly.
- Make thresholds configurable from public API in this feature: rejected because the current need
  is reviewer-visible evidence and safe defaults, not a consumer tuning surface.

## Decision: Demote churn and non-beneficial promotion with first-class reason tokens

**Rationale**: Over-promotion is the named R6 risk. A promoted boundary that starts changing
content every frame, misses parity, loses retained content, exceeds resource limits, or costs more
bookkeeping than it saves must demote or bypass promotion with a stable reviewer-visible reason.
Reason tokens make readiness, tests, and package compatibility checks deterministic.

**Alternatives considered**:

- Leave demotion as free-form diagnostics: rejected because reviewers could not aggregate churn,
  low-cost, stale identity, and parity failures consistently.
- Keep promoted state until eviction: rejected because churning promoted boundaries can keep paying
  promotion overhead.
- Hide demotion from readiness: rejected because the feature's safety case depends on visible
  fail-closed behavior.

## Decision: Validate seven Feature 159 scenario classes

**Rationale**: The spec requires representative coverage for static retained content,
placement-only movement, scrolling or shifted content, nested retained content, content churn, and
fallback. The planned required set is:

- `promotion/static-retained`
- `promotion/placement-only-move`
- `promotion/scroll-shifted`
- `promotion/nested-retained`
- `promotion/content-change`
- `promotion/churn-demotion`
- `promotion/fallback-safe`

This set separates accepted reuse from rejection/demotion/fallback evidence while keeping the
harness small enough for focused validation.

**Alternatives considered**:

- Reuse only the Feature 157 damage scenarios: rejected because they do not prove content identity
  and placement identity are distinct.
- Require timing scenarios only: rejected because Feature 159 acceptance is about reuse and
  promotion counters; timing remains a later final-claim gate.
- Collapse churn and fallback into one scenario: rejected because churning promoted content and
  missing/unsafe retained content fail for different reasons.

## Decision: Keep accepted evidence scoped to `probe-08a47c01`

**Rationale**: Features 155-158 already scope accepted proof, damage readiness, and measurement
separation to stable host profile `probe-08a47c01`. Feature 159 reuse evidence is only comparable
when host profile, renderer identity, present mode, run identity, package version, scenario
definition, retained content state, and parity evidence match. Unsupported hosts and cross-profile
evidence publish zero accepted reuse or promotion artifacts.

**Alternatives considered**:

- Accept any OpenGL host as comparable: rejected because compositor behavior can vary by display,
  present mode, scale, and driver.
- Accept cross-profile reuse counters with a warning: rejected because it would mix incomparable
  proof, damage, and reuse evidence.
- Block all unsupported-host output: rejected because fail-closed unsupported-host evidence remains
  required.

## Decision: Publish counters without accepting final performance

**Rationale**: Feature 159 can satisfy the reuse/promotion counter gate from the report, but the
final shipped compositor performance claim also needs non-noisy same-profile timing and the later
host-lane ledger. Readiness therefore records accepted, non-beneficial, fallback-only, rejected, or
environment-limited Feature 159 status, while the shipped performance claim stays
`performance-not-accepted` unless all report-defined gates are present.

**Alternatives considered**:

- Accept final performance from reuse counters alone: rejected because counters are not timing.
- Require Feature 161 before Feature 159 can close: rejected because the report names Feature 159
  as a separate reuse/promotion counter gate.
- Hide non-beneficial counters: rejected because reviewers need to know when promotion was safe but
  not worth claiming.
