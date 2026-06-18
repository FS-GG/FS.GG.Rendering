# Contract: Promotion and Reuse Evidence

## Scope

This contract defines the reviewer-visible evidence for promoted retained layers, placement-only
reuse, content re-recording, demotion, fallback, counters, and parity.

## Attempt Record

Each attempt records:

- Attempt id and run id.
- Scenario id and scenario definition id.
- Host profile and package version.
- Policy id.
- Promotion candidates.
- Promotion decisions.
- Reuse decisions.
- Content identities.
- Placement identities.
- Reuse counters.
- Demotion and fallback reasons.
- Parity results.
- Artifact paths.
- Diagnostics.

Accepted attempts must link every field required by the feature spec: promotion decision, content
identity, placement identity, reuse decision, reuse counters, demotion status, parity status, host
profile, run identity, scenario identity, and artifact locations.

## Promotion Decisions

Promotion decisions use stable status tokens:

- `promoted`
- `observing`
- `kept`
- `demoted`
- `rejected`
- `bypassed`
- `non-beneficial`
- `fallback-only`
- `environment-limited`

`promoted` requires the three-frame stability window, parity success, current same-profile
evidence, and net-positive counters. All other non-accepting decisions require exactly one primary
reason.

## Reuse Decisions

Reuse decisions use stable status tokens:

- `content-reused-placement-updated`
- `content-recorded`
- `content-re-recorded`
- `fallback-full-redraw`
- `reuse-rejected`
- `environment-limited`

Placement-only reuse must include prior and current placement identity. Content re-recording must
include the prior and current content identity so reviewers can verify stale content was not used.

## Counter Requirements

Counter records must distinguish:

- Avoided content work.
- Placement-only reuse.
- Content recording.
- Content re-recording.
- Promotions.
- Demotions.
- Fallback decisions.
- Replay hits, misses, and records.
- Promotion overhead.
- Net saved work.

Counters from rejected, unsupported-host, cross-profile, stale, missing-policy, incomplete,
resource-limited, or parity-failing attempts cannot contribute to accepted Feature 159 status.

## Parity Requirements

Every accepted promoted or reused output must be compared to the equivalent safe output for the
same scenario. Parity records include full-redraw or lower-safe-tier artifact paths, promoted/reuse
artifact paths, damage and placement coverage, outside-damage drift count, verdict, and
diagnostics.

Parity mismatch rejects the attempt and blocks reuse for that evidence set. Future attempts may be
accepted only after fresh comparable evidence is collected.

## Reason Tokens

Non-accepting records use these primary reason tokens:

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
- `missing-policy`
- `environment-limited`

## Acceptance Tests

- Stable expensive content promotes after observation and records net-positive counters.
- Stable cheap content is not promoted unless evidence records a net-positive reason.
- Placement-only movement reuses content and records placement identities separately.
- Content churn demotes or bypasses promotion with `instability`.
- Missing retained content falls back with `missing-retained-content`.
- Parity mismatch rejects reuse and records zero accepted reuse artifacts.
