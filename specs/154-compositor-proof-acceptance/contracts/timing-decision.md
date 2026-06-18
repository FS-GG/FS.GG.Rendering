# Contract: Timing Decision

## Scope

This contract defines how Feature 154 accepts, rejects, or marks inconclusive any live performance
claim for damage-scoped redraw. Timing is a claim gate, not a substitute for proof or parity.

## Public or Observable Surface

Any public timing policy, result token, harness command argument, readiness field, or package
helper must be declared in `.fsi` before implementation and covered by semantic tests. The
observable surface must support:

- Declaring the threshold and noise policy before evaluating timing.
- Recording same-profile full-redraw and damage-scoped measurements.
- Rejecting missing, noisy, incomplete, cross-profile, environment-limited, or non-beneficial
  timing evidence.
- Publishing an accepted, rejected, or inconclusive timing decision.
- Marking reuse, snapshot, or deterministic evidence as context-only when same-profile live timing
  is absent.

## Required Timing Evidence

An accepted performance benefit requires:

- Accepted proof set for the host profile.
- Accepted same-profile parity gate.
- Declared threshold and noise policy.
- At least five representative live scenarios.
- At least five comparable repetitions per scenario.
- Comparable full-redraw and damage-scoped measurements on the same host profile.

## Verdict Rules

- `accepted`: measurements satisfy the policy and support the declared performance benefit.
- `rejected`: measurements are complete but do not satisfy the declared policy or show no benefit.
- `inconclusive`: measurements are missing, noisy, incomplete, cross-profile, environment-limited,
  or otherwise insufficient.

Only `accepted` may support a performance claim. `rejected` and `inconclusive` must record no
accepted performance benefit.

## Required Output

The timing package under `readiness/timing/` records:

- Timing policy id, threshold, and noise policy.
- Host profile id and proof-set id.
- Scenario list and repetition counts.
- Measurement summary for full-redraw and damage-scoped modes.
- Decision status and reason.
- Context-only evidence labels for reuse, snapshot, or deterministic counters.

## Acceptance Tests

- Same-profile timing with declared policy, at least five scenarios, at least five repetitions per
  scenario, and sufficient benefit accepts the performance claim.
- Complete but non-beneficial timing rejects the claim.
- Missing policy, missing repetitions, noisy measurements, cross-profile data, unsupported host
  data, or absent parity produces an inconclusive or rejected decision with no accepted claim.
- Snapshot or reuse evidence without same-profile live timing is recorded as context-only.
