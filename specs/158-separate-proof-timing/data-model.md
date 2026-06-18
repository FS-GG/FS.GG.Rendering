# Data Model: Separate Proof Readback From Timing

## Measurement Policy

**Purpose**: Declares whether validation readback is absent from, outside, or inside the measured
timing interval.

**Values**:

- `readback-free`
- `readback-outside-measurement`
- `probe-readback-included`
- `unverified`
- `missing`

**Validation rules**:

- Accepted timing samples must be `readback-free` or `readback-outside-measurement`.
- `probe-readback-included` is valid only for explicit probes and is always excluded from
  performance acceptance.
- `unverified` and `missing` samples are excluded with a reviewer-visible reason.

## Timing Run

**Purpose**: One Feature 158 evidence collection session.

**Fields**:

- Run id.
- Host profile.
- Policy id: `readback-free-timing-v1`.
- Scenario set id.
- Warmup count.
- Measured repetitions per path.
- Timing samples.
- Excluded samples.
- Linked proof/probe evidence.
- Measurement separation status.
- Shipped performance claim status.
- Diagnostics.

**Validation rules**:

- All accepted samples must share run id, host profile, renderer identity, display environment,
  package version, scenario definition, and policy id.
- A run with no accepted samples is `rejected`, `fallback-only`, or `environment-limited`.
- The shipped performance claim remains `performance-not-accepted` unless later report-defined
  gates are also present and positive.

## Timing Scenario

**Purpose**: Representative workload measured by the readback-free timing lane.

**Fields**:

- Scenario id.
- Scenario definition hash or version.
- Expected damage behavior.
- Frame size.
- Path definitions.
- Required sample count.
- Artifact paths.
- Scenario inclusion status.

**Required scenarios**:

- `timing/localized-update`
- `timing/no-change`
- `timing/movement-old-new`
- `timing/overlap`
- `timing/edge-clipping`

**Validation rules**:

- Required scenarios must match the Feature 156 scenario identities unless readiness explicitly
  documents a difference.
- Missing required scenarios prevent accepted measurement separation.
- Additional scenarios may be published but cannot replace required scenarios.

## Timing Sample

**Purpose**: One measured repetition intended for performance evaluation.

**Fields**:

- Sample id and sample index.
- Scenario id.
- Path: `full-redraw` or `damage-scoped`.
- Run id.
- Host profile id.
- Duration milliseconds.
- Measurement policy.
- Measurement-window metadata.
- Inclusion status.
- Exclusion reason when excluded.
- Artifact path.

**Validation rules**:

- Duration must be finite and non-negative.
- Sample identity must be unique within path and scenario.
- Readback inside the measured interval excludes the sample.
- Samples from probes, proof runs, cross-profile runs, unsupported hosts, mismatched packages, or
  ambiguous policy metadata are excluded.

## Accepted Timing Set

**Purpose**: The subset of timing samples that may be summarized for performance evaluation.

**Fields**:

- Run id.
- Host profile id.
- Policy id.
- Included samples.
- Required scenario coverage.
- Per-path distributions.
- Distribution verdicts.
- Artifact paths.

**Validation rules**:

- Every included sample must pass measurement-policy, same-profile, scenario, package, and
  artifact validation.
- Proof/probe samples are never included.
- Accepted set construction must be deterministic and reproducible from published artifacts.

## Proof Readback Evidence

**Purpose**: Screenshot or pixel readback evidence used to preserve correctness proof.

**Fields**:

- Proof id.
- Host profile.
- Scenario id.
- Proof method.
- Readback artifact paths.
- Verdict.
- Created timestamp.
- Diagnostics.

**Validation rules**:

- Proof evidence can support correctness or probe diagnostics, but not accepted performance timing.
- Failed proof readback keeps proof fail-closed and records zero accepted performance artifacts.
- Cross-profile proof cannot support same-profile timing acceptance.

## Explicit Probe Run

**Purpose**: Deliberately requested run that may include readback for measurement inspection.

**Fields**:

- Probe id.
- Triggering command and options.
- Host profile.
- Scenario ids.
- Probe samples.
- Readback artifacts.
- Exclusion reason.
- Diagnostics.

**Validation rules**:

- Probe samples use `probe-readback-included`.
- Probe samples are excluded from the accepted timing set with reason `probe-run-excluded`.
- Probe artifacts may be linked from readiness as proof/probe evidence.

## Excluded Sample

**Purpose**: Sample removed from performance acceptance.

**Reason tokens**:

- `probe-run-excluded`
- `proof-readback-in-measured-interval`
- `missing-measurement-policy`
- `unverifiable-measurement-policy`
- `cross-profile-evidence`
- `scenario-definition-mismatch`
- `package-version-mismatch`
- `run-identity-mismatch`
- `unsupported-host`
- `environment-limited`
- `failed-proof-readback`

**Validation rules**:

- Every excluded sample must have exactly one primary reason and may include secondary diagnostics.
- Excluded samples remain listed in readiness and raw artifact indexes.

## Host Profile

**Purpose**: Stable identity used to decide whether timing, proof, and probe evidence are
comparable.

**Fields**:

- Profile id.
- Backend and renderer identity.
- Display environment.
- Present mode.
- Framebuffer size and scale.
- Direct rendering flag.
- Package and harness version.
- Proof algorithm version.

**Validation rules**:

- Accepted Feature 158 timing targets Feature 155 stable profile `probe-08a47c01`.
- Evidence from different host profiles or display environments cannot be combined.

## Readiness Summary

**Purpose**: Reviewer-facing entry point for Feature 158.

**Fields**:

- Final status: `accepted`, `rejected`, `fallback-only`, or `environment-limited`.
- Host profile and run identity.
- Measurement policy id.
- Included timing samples.
- Excluded samples and reasons.
- Scenario coverage.
- Proof/probe links.
- Unsupported-host result.
- Feature 156 comparison status.
- Compatibility impact.
- Package and regression validation.
- Shipped performance claim status.
- Remaining gates.

**Validation rules**:

- Accepted measurement separation requires all required scenarios to have only accepted
  readback-free or outside-measurement timing samples.
- Unsupported-host readiness records zero accepted proof or performance artifacts.
- The summary must let a reviewer determine the policy, included/excluded sample sets, proof/probe
  evidence, and claim status from one entry point.

## Measurement Workflow State

**Purpose**: State for collecting, classifying, and publishing Feature 158 evidence.

**States**:

```text
initialized -> profile-bound -> policy-declared -> scenarios-prepared
            -> timing-collected -> samples-classified -> probes-linked
            -> summary-published -> accepted | rejected | fallback-only | environment-limited
```

**Validation rules**:

- State transitions occur only through workflow messages.
- Native I/O is represented as effects and interpreted at the edge.
- Any failure records a reviewer-visible reason.
