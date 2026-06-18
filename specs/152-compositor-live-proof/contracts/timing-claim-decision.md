# Contract: Timing Claim Decision

## Scope

This contract defines how Feature 152 accepts, rejects, or marks inconclusive any live compositor
performance claim. Correctness acceptance and performance acceptance are separate decisions.

## Measurement Preconditions

Timing evidence can support a performance claim only when:

- A fresh accepted proof set exists for the active host profile.
- Live parity passed for the measured scenarios on the same host profile.
- Full redraw and damage-scoped redraw are measured under comparable conditions.
- At least 5 representative live scenarios are covered.
- At least 5 comparable repetitions per scenario are recorded.
- Warmup frames and measured frames are distinguished.
- Environment facts and threshold policy are recorded.

If any precondition fails, the timing decision is `inconclusive`, `environment-limited`, or
`rejected`, and no performance benefit is accepted.

## Comparison Rules

- Damage-scoped redraw compares against full redraw.
- Snapshot or reuse evidence may be linked only when it uses the same accepted host profile and
  passed parity evidence.
- Beneficial and non-beneficial scenario classes are both represented.
- Noise, incomplete samples, environment limits, missing proof, failed parity, or non-beneficial
  results reject or mark the claim inconclusive.
- Work-reduction counters and deterministic timing summaries may explain the result but cannot
  replace live measurements.

## Decision Output

The timing decision records:

- Host profile and proof set identity.
- Scenario ids and repetition counts.
- Baseline full-redraw metrics.
- Damage-scoped metrics.
- Thresholds and noise policy.
- Verdict: `accepted`, `rejected`, `inconclusive`, or `environment-limited`.
- Public claim text when accepted, or explicit no-claim text otherwise.
- Links to proof, parity, and timing artifacts for the same host profile.

## Acceptance Tests

- Complete same-profile timing with accepted proof and passed parity can accept a benefit only
  when thresholds are met.
- Incomplete, noisy, environment-limited, or non-beneficial timing records no accepted performance
  claim.
- Timing from a host profile different from proof/parity is rejected.
- Snapshot or reuse timing without same-profile live parity cannot support a performance claim.
- Readiness output distinguishes correctness acceptance from performance-claim rejection.
