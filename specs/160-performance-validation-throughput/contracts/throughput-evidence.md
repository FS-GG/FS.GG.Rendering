# Contract: Throughput Evidence

## Scope

This contract defines reviewer-visible evidence for focused performance iterations, sample policy,
scenario coverage, exclusions, host identity, artifact locations, and throughput status.

## Iteration Record

Each focused iteration records:

- Iteration id and run id.
- Command and options.
- Host profile.
- Package and harness version.
- Lane id.
- Policy id.
- Declared bound.
- Actual duration.
- Required scenario coverage.
- Warmup count.
- Measured repetitions.
- Sample count by scenario and path.
- Inclusion status.
- Exclusion reason when not accepted.
- Artifact paths.
- Diagnostics.

Accepted iteration records must link every field required by the feature spec: duration, declared
bound, scenario coverage, sample count, inclusion status, exclusion reason when applicable, host
profile, run identity, scenario identity, and artifact locations.

## Scenario Coverage

Accepted throughput iterations cover these scenario ids:

- `timing/localized-update`
- `timing/no-change`
- `timing/movement-old-new`
- `timing/overlap`
- `timing/edge-clipping`

Each scenario report records full-redraw and damage-scoped sample counts, warmup count, measured
repetition count, measurement policy, raw sample paths, included sample count, excluded sample
count, and diagnostics.

## Inclusion Rules

An iteration is accepted only when all are true:

- Lane is `focused`.
- Policy is `focused-throughput-v1`.
- Host profile matches `probe-08a47c01` or a later accepted same-profile proof.
- The iteration completes under the declared 10 minute bound.
- Every required scenario is covered.
- Warmup and measured repetitions match the declared policy.
- Every accepted sample is readback-free or readback-outside-measurement.
- Artifact paths are present, readable, and inside the requested readiness tree.
- Iteration, scenario, package, harness, and run identity metadata are complete.

## Exclusion Rules

An iteration or sample is excluded from throughput acceptance when any of these are present:

- Timed out, canceled, interrupted, or partial output.
- Unsupported or unavailable presentation environment.
- Different host profile, display environment, renderer identity, package version, run id, or
  scenario definition.
- Missing, stale, unreadable, duplicated, or outside-readiness artifact paths.
- Missing policy, mixed policy, sample-policy mismatch, or readback contamination.
- Missing required scenario category.
- Missing duration, bound, sample count, inclusion status, or exclusion metadata.

Excluded records must use exactly one primary reason token and may include secondary diagnostics.

## Reason Tokens

- `timed-out`
- `canceled`
- `partial-evidence`
- `cross-profile-evidence`
- `stale-evidence`
- `mixed-policy`
- `missing-metadata`
- `unsupported-host`
- `environment-limited`
- `scenario-coverage-missing`
- `sample-policy-mismatch`
- `run-identity-mismatch`
- `artifact-unreadable`
- `readback-contaminated`

## Throughput Status

Readiness uses these throughput status tokens:

- `accepted`
- `rejected`
- `fallback-only`
- `environment-limited`
- `blocked`

`accepted` requires at least three fresh accepted focused iterations. `blocked` is used when
focused throughput is accepted but full solution validation blocks release-ready status.

## Performance Claim Boundary

Feature 160 throughput evidence never accepts the shipped compositor performance claim by itself.
The readiness summary must state `performance-not-accepted` unless same-profile timing is not
noisy, Feature 159 reuse/promotion counters are net-positive, Feature 160 throughput is accepted,
and Feature 161 host-lane scoping is accepted.

Noisy same-profile timing is reported as a remaining performance-claim gate; it is not an exclusion
reason by itself for focused throughput.
