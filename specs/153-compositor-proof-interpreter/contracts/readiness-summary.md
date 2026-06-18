# Contract: Readiness Summary

## Scope

This contract defines the review entry point for Feature 153. Reviewers must be able to determine
what the live proof interpreter proved, what remains fallback-gated, and what later gates are
required before partial redraw or performance claims are accepted.

## Required Readiness Files

The readiness package lives under `specs/153-compositor-proof-interpreter/readiness/` and contains:

- `validation-summary.md`: final live proof status, fallback status, remaining gates, and evidence
  links.
- `compatibility-ledger.md`: public API, diagnostics, fallback, readiness, docs, and package-impact
  record when observable changes occur.
- `live-proof/attempts/`: one directory or summary per proof attempt.
- `live-proof/unsupported/`: unsupported-host evidence and zero-accepted-artifact statement.
- `proof-set.md`: aggregate proof-set decision.
- `package-validation.md`: surface/package validation evidence when public drift occurs.
- `regression-validation.md`: adjacent readiness and regression evidence.

Machine-readable records may be added beside Markdown summaries when the implementation chooses a
stable format.

## Status Rules

- `accepted-proof-set`: exactly three fresh matching capable-host attempts are accepted and linked.
- `environment-limited`: the environment cannot produce honest capable-host proof evidence; no
  partial-redraw artifacts are accepted.
- `failed`: proof execution or artifact validation completed and found a defect.
- `fallback-gated`: evidence is missing, incomplete, stale, host-mismatched,
  proof-method-mismatched, synthetic-only, or otherwise non-accepting.

Regardless of proof status, the summary must state that partial redraw remains fallback-gated until
same-profile parity requirements also pass, and that performance claims remain unaccepted until a
later same-profile timing gate passes.

## Compatibility Rules

The compatibility ledger is required when the feature changes:

- Public API or `.fsi` surfaces.
- Public diagnostics or metrics.
- Surface baselines.
- Observable fallback behavior.
- Readiness vocabulary.
- Release notes or migration guidance.

Accepted readiness is blocked by undocumented public drift.

## Regression Rules

Feature 153 must preserve:

- Feature 152 proof-set vocabulary and fail-closed rules.
- Feature 149 deterministic compositor readiness evidence.
- Feature 151 P8 layout acceptance and compositor non-claim regression.
- Existing render-anywhere, overlay, text-shaping, full-redraw, package-readiness, and surface
  baseline guarantees unless a compatibility note explicitly documents a change.

## Acceptance Tests

- Accepted proof set summary links all three attempts and required artifacts.
- Environment-limited proof writes unsupported-host evidence and zero accepted artifacts.
- Failed proof writes a reviewer-visible reason.
- Missing, stale, synthetic-only, host-mismatched, or proof-method-mismatched evidence writes
  fallback-gated readiness.
- The summary explicitly states partial redraw and performance claims remain unaccepted pending
  later gates.
- Public drift without compatibility notes blocks readiness closeout.
