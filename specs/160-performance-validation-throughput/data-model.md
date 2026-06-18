# Data Model: Performance Validation Throughput

## Host Profile

**Purpose**: Stable identity used to decide whether performance evidence is comparable.

**Fields**:

- Profile id.
- Backend and renderer identity.
- Presentation environment.
- Framebuffer size and scale.
- Package and harness version.
- Proof and measurement policy versions.

**Validation rules**:

- Accepted Feature 160 throughput targets profile `probe-08a47c01` unless a later accepted proof
  explicitly replaces it.
- Evidence from different profiles, display environments, renderer identities, package versions,
  or policy versions cannot be combined.
- Unsupported or unavailable hosts publish zero accepted same-profile performance artifacts.

## Focused Performance Lane

**Purpose**: Bounded validation path for repeated P7 timing iterations without invoking broad
release validation.

**Fields**:

- Lane id.
- Feature id.
- Policy id.
- Declared bound.
- Scenario set.
- Warmup count.
- Measured repetitions.
- Expected host profile.
- Output directory.

**Validation rules**:

- Accepted lane id is `focused`.
- Accepted policy id is `focused-throughput-v1`.
- The bound must be declared before timing samples are collected.
- The lane must not execute broad release validation as part of each iteration.

## Validation Bound

**Purpose**: Maximum allowed duration for a focused iteration before it is excluded.

**Fields**:

- Bound id.
- Duration in minutes.
- Started timestamp.
- Completed timestamp.
- Enforcement result.
- Timeout or cancellation diagnostics.

**Validation rules**:

- Accepted focused iterations must complete within 10 minutes.
- Unsupported-host validation must complete within 2 minutes.
- Timed-out, canceled, interrupted, or partially written iterations are excluded with a primary
  reason and zero accepted throughput contribution.

## Focused Iteration

**Purpose**: One same-profile run of the focused performance lane.

**Fields**:

- Iteration id.
- Run id.
- Host profile.
- Policy id.
- Declared bound.
- Actual duration.
- Scenario coverage.
- Sample count.
- Inclusion status.
- Exclusion reason when not accepted.
- Artifact paths.
- Diagnostics.

**Validation rules**:

- Accepted throughput requires at least three fresh focused iterations.
- Fresh means the iteration was produced for the current implementation state, package/surface
  baseline, policy id, scenario definition id, and readiness run.
- Accepted iterations must be same-profile, same-policy, same-scenario-definition, complete, and
  metadata-complete.
- Cross-profile, stale, mixed-policy, missing-metadata, unsupported-host, environment-limited,
  timed-out, canceled, partial, scenario-coverage-missing, sample-policy-mismatch,
  run-identity-mismatch, artifact-unreadable, or readback-contaminated iterations are excluded.

## Scenario Coverage

**Purpose**: Evidence that the focused lane exercises the required P7 timing scenario categories.

**Required scenario ids**:

- `timing/localized-update`
- `timing/no-change`
- `timing/movement-old-new`
- `timing/overlap`
- `timing/edge-clipping`

**Fields**:

- Scenario id.
- Scenario definition id.
- Full-redraw sample count.
- Damage-scoped sample count.
- Warmup count.
- Measured repetitions.
- Artifact paths.
- Coverage status.

**Validation rules**:

- Every accepted iteration covers every required scenario.
- Missing or explicitly excluded required scenarios prevent iteration acceptance.
- Scenario definition, sample policy, or warmup policy changes must be reported as superseding,
  confirming, or not comparable to prior evidence.

## Sample Policy

**Purpose**: Measurement policy preserved from Feature 158 for comparable timing evidence.

**Fields**:

- Warmup count.
- Measured repetitions.
- Measurement policy.
- Readback classification.
- Included sample count.
- Excluded sample count.

**Validation rules**:

- Accepted iterations use warmup `3` and measured repetitions `5` per path per scenario.
- Accepted samples must be `readback-free` or `readback-outside-measurement`.
- Probe/readback samples, readback-contaminated samples, and missing-policy samples are excluded.

## Excluded Evidence

**Purpose**: Reviewer-visible record for iterations or samples that cannot contribute to accepted
throughput.

**Reason tokens**:

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

**Validation rules**:

- Every excluded iteration or sample has exactly one primary reason.
- Excluded evidence remains linked from readiness but contributes zero accepted throughput samples
  or iterations.

## Broad Release Gate

**Purpose**: Full solution validation required before release-ready status.

**Fields**:

- Command.
- Started timestamp.
- Completed timestamp.
- Status.
- Artifact paths.
- Staleness marker.
- Diagnostics.

**Validation rules**:

- Missing, failing, interrupted, stale, or undocumented full validation blocks release-ready status.
- Stale means the full-validation record was produced for a different implementation commit,
  validation command, package/surface baseline, or readiness artifact set than the implementation
  state being marked ready.
- The focused lane may pass while release readiness remains blocked.
- Full validation is recorded separately from focused iteration artifacts.

## Throughput Readiness Result

**Purpose**: Feature 160 validation-throughput status.

**Values**:

- `accepted`
- `rejected`
- `fallback-only`
- `environment-limited`
- `blocked`

**Fields**:

- Status.
- Accepted iteration count.
- Required iteration count.
- Missing scenarios.
- Excluded evidence count.
- Unsupported-host result.
- Full-validation status.
- Performance claim status.
- Artifact paths.
- Limitations.

**Validation rules**:

- `accepted` requires at least three fresh same-profile focused iterations, complete required
  scenario coverage, complete metadata, no accepted unsupported-host artifacts, and declared-bound
  compliance.
- `environment-limited` records zero accepted performance artifacts.
- `blocked` is used when focused throughput evidence passes but full validation is missing,
  failing, interrupted, stale, or undocumented.

## Performance Claim Status

**Purpose**: Reviewer-visible shipped compositor performance claim boundary.

**Values**:

- `performance-not-accepted`
- `performance-accepted`

**Validation rules**:

- Feature 160 alone does not accept the shipped performance claim.
- `performance-accepted` is valid only when same-profile timing is not noisy, Feature 159
  reuse/promotion counters are net-positive, Feature 160 throughput is accepted, and Feature 161
  host-lane scoping is accepted.

## Workflow State Transitions

```text
initialized -> profile-detected -> policy-declared -> scenarios-prepared
            -> iteration-running -> samples-classified -> iteration-published
            -> iterations-aggregated -> full-validation-recorded
            -> summary-published
            -> accepted | rejected | fallback-only | environment-limited | blocked
```

Invalid transitions record diagnostics and leave accepted iteration counts unchanged.
