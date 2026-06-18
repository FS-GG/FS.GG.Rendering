# Contract: Promotion Workflow Effects

## Scope

Feature 159 evidence collection is stateful and I/O-bearing. This contract defines the MVU-shaped
boundary used by the harness and package-facing readiness helpers.

## Model

The workflow model owns:

- Run id.
- Expected host profile.
- Active host profile.
- Policy id.
- Scenario set.
- Candidate observations.
- Promotion decisions.
- Reuse decisions.
- Parity results.
- Counter totals.
- Published artifacts.
- Final status.
- Diagnostics.

## Messages

Workflow messages include:

- Host profile detected.
- Host profile rejected.
- Policy declared.
- Scenario prepared.
- Candidate observed.
- Content identity classified.
- Placement identity classified.
- Promotion decision recorded.
- Reuse decision recorded.
- Parity result recorded.
- Counter totals recorded.
- Unsupported host recorded.
- Artifact published.
- Summary published.
- Diagnostic recorded.

## Effects

Workflow effects include:

- Detect host profile.
- Declare policy.
- Prepare scenario.
- Render safe output.
- Render promoted or reused output.
- Capture parity evidence.
- Read retained layer state.
- Record content identity.
- Record placement identity.
- Write attempt artifact.
- Write counter artifact.
- Write readiness summary.
- Write compatibility ledger.
- Write package validation.
- Write regression validation.

Effects are interpreted only at the harness, viewer, or filesystem edge. The update function stays
pure and deterministic.

## Status Transitions

```text
initialized -> profile-bound -> scenarios-prepared -> candidates-observed
            -> identities-classified -> promotion-evaluated -> reuse-evaluated
            -> parity-checked -> counters-aggregated -> summary-published
            -> accepted | non-beneficial | fallback-only | rejected | environment-limited
```

Invalid transitions record diagnostics and leave accepted counters unchanged.

## Failure Rules

- Host-profile rejection produces `environment-limited` or `rejected` with zero accepted reuse.
- Missing policy rejects accepted evidence.
- Missing identity, stale identity, missing retained content, resource limitation, or parity
  mismatch records a primary reason and prevents accepted reuse for that attempt.
- Unsupported-host output remains successful command output only when it publishes zero accepted
  reuse or promotion artifacts.

## Acceptance Tests

- Pure update transitions from candidate observation to promoted accepted state when all gates pass.
- Pure update records demotion and zero accepted counters when churn is observed.
- Unsupported host transitions to `environment-limited` and emits write effects for limitation
  artifacts.
- Parity mismatch transitions to `rejected` for the attempt and keeps final performance claim
  `performance-not-accepted`.
