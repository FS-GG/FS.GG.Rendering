# Data Model: Same-Profile Timing Evidence

## Accepted Host Profile

**Purpose**: The stable host identity that binds Feature 156 timing to Feature 155 accepted P7
correctness evidence.

**Fields**:

- Profile id.
- Display environment and display token.
- Renderer identity and GL version.
- Direct rendering flag.
- Present mode and refresh source if available.
- Framebuffer size and scale.
- Package version and harness version.
- Proof algorithm version.

**Validation rules**:

- Positive timing evidence must use the Feature 155 accepted profile `probe-08a47c01`.
- Evidence from another profile, missing profile, changed renderer, changed display environment,
  changed package version, changed scenario definition, or changed run identity cannot support a
  positive decision.

## Timing Run

**Purpose**: One evidence collection session for Feature 156.

**Fields**:

- Run identity.
- Started and completed timestamps.
- Accepted host profile.
- Scenario set identity.
- Noise policy id.
- Warmup count.
- Measured repetitions per path.
- Output directories.
- Proof/parity readiness references.
- Overall timing verdict.
- Shipped performance claim status.
- Diagnostics and rejection reasons.

**Validation rules**:

- Run identity must be stable across all scenarios in the package.
- A positive Feature 156 timing verdict requires at least five complete positive scenarios.
- The shipped P7 performance claim remains `performance-not-accepted` until later report-defined
  gates are also satisfied.
- Unsupported or unavailable presentation environments produce an `environment-limited` run with
  zero accepted performance artifacts.

## Timing Scenario

**Purpose**: A representative compositor workload measured through both paths.

**Fields**:

- Scenario id.
- Scenario definition hash or version.
- Expected damage behavior.
- Frame size.
- Full-redraw path definition.
- Damage-scoped path definition.
- Artifact paths.
- Scenario verdict.

**Required positive-decision scenarios**:

- `timing/localized-update`
- `timing/no-change`
- `timing/movement-old-new`
- `timing/overlap`
- `timing/edge-clipping`

**Validation rules**:

- Required scenarios must be measured on the same run identity and host profile.
- Scenario definitions must not drift between full-redraw and damage-scoped measurements.
- Additional fallback or stress scenarios may be reported, but they cannot replace the required
  positive-decision scenarios.

## Measured Path

**Purpose**: One side of the comparison for a scenario.

**Values**:

- `full-redraw`
- `damage-scoped`

**Fields**:

- Path id.
- Warmup record.
- Sample distribution.
- Raw sample artifact.
- Readback/validation overhead disclosure.
- Path diagnostics.

**Validation rules**:

- Both paths must have at least five measured repetitions after warmup.
- If proof readback or validation overhead is included and cannot be separated, the scenario is
  limited and cannot support a shipped performance claim.

## Warmup Record

**Purpose**: Frames or repetitions excluded from measurement.

**Fields**:

- Warmup count.
- Warmup sample artifact if recorded.
- Exclusion reason.
- Path id.

**Validation rules**:

- Warmup count must be recorded for each path.
- Warmup samples are never included in p50, p95, or p99.

## Timing Sample

**Purpose**: A single measured repetition.

**Fields**:

- Sample index.
- Path id.
- Scenario id.
- Run identity.
- Host profile id.
- Duration in milliseconds.
- Artifact path.
- Diagnostics.

**Validation rules**:

- Sample index must be unique within path and scenario.
- Duration must be finite and non-negative.
- Samples from different run identities, host profiles, scenario definitions, or package versions
  cannot be combined.

## Sample Distribution

**Purpose**: The summarized measured samples for one path in one scenario.

**Fields**:

- Measured sample count.
- p50 milliseconds.
- p95 milliseconds.
- p99 milliseconds.
- Minimum and maximum milliseconds.
- Raw sample artifact path.
- Distribution diagnostics.

**Validation rules**:

- Distribution metrics are computed only from measured samples after warmup.
- Missing or unreadable raw samples mark the distribution incomplete.

## Noise Policy

**Purpose**: Predeclared rule for classifying measured differences.

**Fields**:

- Policy id: `same-profile-live-threshold-v2`.
- Noise band formula: `max(0.25 ms, 5% of full-redraw p50)`.
- Positive p50 rule.
- Positive p95 rule.
- Tail p99 guardrail.
- No-mixing rules.

**Validation rules**:

- The policy must be recorded before scenario verdicts are evaluated.
- A scenario is positive only when damage-scoped p50 and p95 are faster than full redraw by at
  least the noise band and damage-scoped p99 is not worse than full-redraw p99 by more than the
  noise band.
- Inside-band results are `noisy`; slower or equivalent damage-scoped results are
  `non-beneficial`.

## Scenario Verdict

**Purpose**: The decision for one scenario.

**Values**:

- `positive`
- `noisy`
- `non-beneficial`
- `incomplete`
- `rejected`
- `environment-limited`
- `limited`

**Validation rules**:

- Missing samples, missing artifacts, stale artifacts, fewer than five measured repetitions,
  unreadable samples, duplicated artifacts, or mixed profiles produce `incomplete` or `rejected`.
- Unsupported host evidence produces `environment-limited`.
- Readback-dominated or unseparated validation overhead produces `limited`.

## Timing Evidence Summary

**Purpose**: The single reviewer-facing entry point for Feature 156.

**Fields**:

- Run identity.
- Accepted host profile.
- Proof/parity baseline references.
- Noise policy.
- Scenario table.
- Per-path distributions.
- Scenario verdicts and rejection reasons.
- Artifact paths.
- Overhead disclosure.
- Feature 156 timing verdict.
- Shipped P7 performance claim status.
- Remaining performance gates.

**Validation rules**:

- A reviewer must be able to determine measured profile, scenario verdicts, distribution metrics,
  policy, artifact paths, limitations, and final claim status from the summary.
- The summary must state `performance-not-accepted` for the shipped P7 performance claim unless
  all later report-defined gates are present and positive.

## Timing Workflow State

**Purpose**: State for the timing collection and publication workflow.

**States**:

```text
initialized -> profile-bound -> policy-declared -> scenario-prepared
            -> full-redraw-measured -> damage-scoped-measured
            -> scenario-evaluated -> summary-published
            -> positive | rejected | environment-limited | limited
```

**Validation rules**:

- State transitions occur only through workflow messages.
- Native I/O is represented as effects and interpreted at the edge.
- Any failure state records a reviewer-visible reason.
