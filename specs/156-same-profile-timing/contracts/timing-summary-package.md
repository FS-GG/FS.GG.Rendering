# Contract: Timing Summary Package

## Scope

The timing summary package is the reviewer-facing entry point for Feature 156. It must let a
reviewer determine what was measured, where, with which policy, what passed, what failed, what was
inconclusive, and why the shipped performance claim is or is not accepted.

## Required Files

```text
readiness/
|-- timing/
|   |-- summary.md
|   |-- scenarios/
|   |   |-- timing-localized-update.md
|   |   |-- timing-no-change.md
|   |   |-- timing-movement-old-new.md
|   |   |-- timing-overlap.md
|   |   `-- timing-edge-clipping.md
|   |-- raw/
|   |   |-- *.csv
|   |   `-- *.json
|   `-- unsupported/
|       `-- README.md
|-- fsi/
|-- compatibility-ledger.md
|-- package-validation.md
|-- regression-validation.md
`-- validation-summary.md
```

## `timing/summary.md` Content

The summary must include:

- Feature id and run identity.
- Accepted host profile id.
- Display environment, renderer identity, direct-rendering flag, refresh fact if available, and
  package version.
- Feature 155 proof-set and parity references.
- Policy id and noise-band formula.
- Warmup count and measured repetitions per path.
- Scenario table with full-redraw p50/p95/p99, damage-scoped p50/p95/p99, noise band, verdict,
  confidence decision, and artifact links.
- Rejection reasons for every noisy, incomplete, non-beneficial, rejected, limited, or
  environment-limited scenario.
- Readback/validation overhead disclosure.
- Feature 156 timing verdict for the measured profile.
- Shipped P7 performance claim status.
- Remaining gates: Feature 157, Feature 158, Feature 159, and Feature 161.

## Validation Rules

- The summary must not require reviewers to reconstruct verdicts from raw samples.
- The summary must state `performance-not-accepted` for the shipped P7 performance claim unless
  all later report-defined gates are present and positive.
- Missing scenario files, raw samples, or artifact links make the package incomplete.
- Unsupported-host timing output is recorded under `timing/unsupported/` and records zero accepted
  performance artifacts.

## Compatibility Notes

Any new public timing token, command argument, result field, helper, or readiness output consumed
by package users must be documented in `compatibility-ledger.md`, exercised through FSI/package
tests, and reflected in surface baselines when the public `.fsi` surface changes.
