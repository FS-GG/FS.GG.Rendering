# Contract: P7 Readiness Summary

## Scope

This contract defines the single reviewer entry point for Feature 154. The summary must state
whether P7 live partial redraw is accepted, failed, environment-limited, or fallback-gated, and it
must connect each accepted or rejected claim to its evidence.

## Required Readiness Files

The readiness package lives under `specs/154-compositor-proof-acceptance/readiness/` and contains:

- `validation-summary.md`: final P7 readiness status, fallback status, host profile, proof,
  parity, timing, compatibility, limitations, and evidence links.
- `proof-set.md`: selected proof attempts and aggregate proof-set status.
- `compatibility-ledger.md`: public API, diagnostics, fallback, readiness, docs, package-impact,
  and migration record when observable changes occur.
- `package-validation.md`: package and public-surface validation evidence.
- `regression-validation.md`: focused adjacent regression evidence.
- `live-proof/attempts/`: selected capable-host attempts and artifacts.
- `live-proof/unsupported/`: unsupported-host evidence and zero accepted artifacts.
- `parity/`: same-profile parity corpus verdicts and artifacts.
- `timing/`: timing policy, measurements, and claim decision.

Machine-readable records may be added beside Markdown summaries when the implementation chooses a
stable format.

## Status Rules

- `accepted`: proof-set acceptance and same-profile parity acceptance are both current. Timing may
  be accepted, rejected, or inconclusive, but the summary must separate safety readiness from the
  performance claim.
- `failed`: proof, parity, package validation, compatibility validation, or adjacent regression
  evidence completed and found a blocking defect.
- `environment-limited`: the environment cannot produce honest capable-host proof or parity
  evidence, and no partial-redraw artifacts are accepted.
- `fallback-gated`: required proof, parity, timing, compatibility, package, or regression evidence
  is missing, stale, cross-profile, incomplete, synthetic-only, or otherwise non-accepting.

Partial redraw remains full-redraw fallback-gated unless proof-set acceptance and same-profile
parity acceptance are both present and current.

## Compatibility Rules

The compatibility ledger is required when the feature changes:

- Public API or `.fsi` surfaces.
- Public diagnostics or metrics.
- Surface baselines.
- Observable fallback behavior.
- Readiness vocabulary or final status.
- Release notes, docs, or migration guidance.

Accepted readiness is blocked by undocumented public drift.

## Regression Rules

Feature 154 must preserve:

- Feature 153 proof interpreter behavior and fail-closed proof-set rules.
- Feature 152/153 readiness vocabulary for accepted, fallback-gated, failed, and
  environment-limited decisions.
- Existing layout acceptance, render-anywhere behavior, text-shaping behavior, overlay behavior,
  full-redraw fallback behavior, package-readiness checks, and surface baseline guarantees unless
  a compatibility note explicitly documents a change.

## Acceptance Tests

- Accepted readiness summary links proof set, selected attempts, parity corpus, timing decision,
  fallback status, package validation, compatibility ledger, and regression validation.
- Failed or environment-limited proof keeps partial redraw fallback-gated and names the blocking
  evidence.
- Failed parity keeps partial redraw fallback-gated even when proof is accepted.
- Rejected or inconclusive timing records no accepted performance claim while preserving the
  proof/parity safety verdict.
- Public drift without compatibility notes blocks readiness closeout.
