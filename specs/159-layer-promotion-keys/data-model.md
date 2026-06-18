# Data Model: Layer Promotion and Content/Transform Key Split

## Host Profile

**Purpose**: Stable identity used to decide whether proof, damage, reuse, parity, and readiness
evidence are comparable.

**Fields**:

- Profile id.
- Backend and renderer identity.
- Present mode.
- Framebuffer size and scale.
- Display environment.
- Package and harness version.
- Proof algorithm version.

**Validation rules**:

- Accepted Feature 159 evidence targets profile `probe-08a47c01` unless a later accepted proof
  explicitly replaces it.
- Evidence from different profiles, display environments, renderer identities, package versions,
  or proof algorithms cannot be combined.

## Content Identity

**Purpose**: Fingerprint or equivalent evidence that proves retained visual content is unchanged.

**Fields**:

- Content identity id.
- Boundary id.
- Local-content fingerprint.
- Content identity algorithm version.
- Source scene or retained subtree identity.
- Render-affecting input summary.
- Created frame id and run id.
- Artifact path when published.

**Validation rules**:

- Equality must cover render-affecting content inputs such as geometry local to the boundary,
  paint, text, font, visual state, clip content, opacity, and local scene node shape.
- Content changes invalidate reuse even when placement identity is unchanged.
- Missing, stale, ambiguous, cross-run, cross-profile, or algorithm-mismatched content identity
  rejects reuse.

## Placement Identity

**Purpose**: Evidence that determines where unchanged content is drawn for the current frame.

**Fields**:

- Placement identity id.
- Boundary id.
- Absolute box.
- Transform or scroll/offset evidence.
- Scale and framebuffer mapping.
- Coverage region.
- Previous placement identity when movement occurs.
- Placement identity algorithm version.

**Validation rules**:

- Placement-only changes may reuse content only when content identity is unchanged and both old
  and new covered regions are included in damage evidence.
- Placement evidence must be current-run and current-profile.
- Ambiguous transforms, unsupported clipping, scale mismatch, or disconnected previous placement
  force re-record or full redraw.

## Promotion Candidate

**Purpose**: A retained subtree being evaluated for promotion.

**Fields**:

- Boundary id.
- Scenario id.
- Host profile.
- Observation window.
- Observed stability frames.
- Content identity history.
- Placement identity history.
- Expected saved work.
- Measured overhead.
- Repeated-work reduction percent.
- Current compositor tier.
- Eligibility diagnostics.

**Validation rules**:

- Stable candidates require at least three consecutive comparable frames with unchanged content
  identity.
- Promotion requires parity success, net-positive saved work, and at least the 30% repeated-work
  reduction threshold.
- Cheap, unstable, missing-metadata, resource-limited, stale, cross-profile, or parity-failing
  candidates are not accepted as promoted reuse evidence.

## Retained Layer

**Purpose**: Reusable recorded content that may be replayed at a new placement.

**Fields**:

- Retained layer id.
- Boundary id.
- Content identity.
- Last placement identity.
- Host profile.
- Run id.
- State: recorded, reused, refreshed, bypassed, evicted, demoted, invalid, or unavailable.
- Resource estimate.
- Artifact paths.
- Diagnostics.

**Validation rules**:

- A retained layer can be reused only when content identity matches and retained content is
  resident, current-run, current-profile, and parity-eligible.
- Evicted, disposed, unavailable, stale, or resource-limited retained content forces refresh,
  demotion, bypass, or full redraw before use.

## Promotion Decision

**Purpose**: Reviewer-visible decision for one candidate.

**Values**:

- `promoted`
- `observing`
- `kept`
- `demoted`
- `rejected`
- `bypassed`
- `non-beneficial`
- `fallback-only`
- `environment-limited`

**Fields**:

- Boundary id.
- Decision value.
- Primary reason.
- Observed stability frames.
- Expected saved work.
- Measured overhead.
- Reduction percent.
- Target tier.
- Linked parity result.
- Artifact paths.

**Validation rules**:

- `promoted` requires stable content, passing parity, current host/profile/run, and net-positive
  counters.
- `demoted`, `rejected`, `bypassed`, `non-beneficial`, `fallback-only`, and
  `environment-limited` require exactly one primary reason token.

## Reuse Decision

**Purpose**: Per-attempt decision to reuse content, re-record content, or fall back.

**Values**:

- `content-reused-placement-updated`
- `content-recorded`
- `content-re-recorded`
- `fallback-full-redraw`
- `reuse-rejected`
- `environment-limited`

**Fields**:

- Attempt id.
- Boundary id.
- Prior content identity.
- Current content identity.
- Prior placement identity.
- Current placement identity.
- Decision value.
- Primary reason.
- Counter deltas.
- Artifact paths.

**Validation rules**:

- Placement-only reuse requires matching content identity and changed placement identity.
- Content identity changes require `content-re-recorded` or safe fallback.
- Reuse rejection must distinguish stale identity, cross-profile evidence, missing retained
  content, unsupported host, parity mismatch, resource limitation, and non-beneficial counters.

## Demotion Reason

**Purpose**: Stable reason token for removing or bypassing promotion.

**Reason tokens**:

- `instability`
- `low-cost`
- `overhead-exceeds-saved-work`
- `stale-content-identity`
- `stale-placement-identity`
- `cross-profile-evidence`
- `missing-retained-content`
- `resource-limited`
- `unsupported-host`
- `parity-mismatch`
- `non-beneficial-counters`
- `run-identity-mismatch`
- `scenario-definition-mismatch`

**Validation rules**:

- Every demotion or bypass uses exactly one primary token and may add secondary diagnostics.
- Churn across the observation window maps to `instability`.

## Reuse Counters

**Purpose**: Aggregated evidence totals used to decide net-positive reuse.

**Fields**:

- Avoided content work.
- Placement-only reuse count.
- Content record count.
- Content re-record count.
- Promotion count.
- Demotion count.
- Fallback count.
- Replay hit/miss/record count.
- Promotion overhead.
- Net saved work.
- Counter artifact paths.

**Validation rules**:

- Net-positive acceptance requires avoided content work and placement-only reuse to exceed
  promotion overhead for the accepted scenario set.
- Counters from unsupported, cross-profile, stale, noisy, incomplete, or rejected attempts cannot
  contribute to accepted Feature 159 status.

## Parity Result

**Purpose**: Comparison between promoted/reused output and equivalent safe output.

**Fields**:

- Scenario id.
- Attempt id.
- Full-redraw or lower-safe-tier artifact.
- Promoted/reused artifact.
- Damage and placement coverage.
- Outside-damage drift count.
- Verdict.
- Diagnostics.

**Validation rules**:

- Accepted parity requires zero unexplained drift and expected changes inside damaged coverage.
- Parity mismatch rejects the attempt and blocks reuse for that evidence set.

## Promotion Attempt

**Purpose**: One same-profile Feature 159 evidence attempt.

**Fields**:

- Attempt id and run id.
- Scenario id.
- Host profile.
- Promotion candidates.
- Promotion decisions.
- Reuse decisions.
- Reuse counters.
- Demotion reasons.
- Parity results.
- Unsupported-host reason when applicable.
- Performance claim status.
- Artifact paths.
- Diagnostics.

**Validation rules**:

- Accepted readiness requires at least three fresh same-profile attempts.
- Every accepted attempt links content identity, placement identity, reuse decision, counters,
  demotion status, parity, host profile, run identity, scenario identity, and artifacts.
- Attempts with unsupported hosts or failed safety gates record zero accepted reuse artifacts.

## Readiness Summary

**Purpose**: Reviewer-facing entry point for Feature 159.

**Fields**:

- Final status: `accepted`, `non-beneficial`, `fallback-only`, `rejected`, or
  `environment-limited`.
- Host profile and run identity.
- Policy id: `layer-promotion-v1`.
- Scenario coverage.
- Accepted attempts.
- Rejected, demoted, bypassed, and fallback attempts.
- Promotion decisions.
- Reuse decisions.
- Counter totals.
- Parity status.
- Unsupported-host result.
- Compatibility impact.
- Package validation result.
- Regression validation result.
- Shipped performance claim status.
- Remaining gates.

**Validation rules**:

- Accepted status requires required scenario coverage, at least three fresh same-profile attempts,
  passing parity, and net-positive counters.
- Unsupported-host readiness records zero accepted reuse or promotion artifacts.
- The summary must allow a reviewer to determine promoted scenarios, reused scenarios, demoted
  scenarios, counter totals, parity, host scope, compatibility impact, artifact paths, and final
  claim status from one file.

## Promotion Workflow State

**Purpose**: State for collecting, classifying, and publishing Feature 159 evidence.

**States**:

```text
initialized -> profile-bound -> scenarios-prepared -> candidates-observed
            -> identities-classified -> promotion-evaluated -> reuse-evaluated
            -> parity-checked -> counters-aggregated -> summary-published
            -> accepted | non-beneficial | fallback-only | rejected | environment-limited
```

**Validation rules**:

- State transitions occur only through workflow messages.
- Native I/O, rendering, parity capture, and filesystem writes are represented as effects and
  interpreted at the edge.
- Any failure state records a reviewer-visible reason.
