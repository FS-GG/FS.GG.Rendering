# Contract: Readiness Package

## Entry Point

`specs/160-performance-validation-throughput/readiness/validation-summary.md` is the reviewer entry
point. It must link every accepted iteration, excluded iteration, raw timing sample,
unsupported-host result, full-validation record, compatibility note, package validation record, and
regression record needed to determine Feature 160 status in under 5 minutes.

## Required Files

```text
readiness/
|-- throughput/
|   |-- summary.md
|   |-- iterations/
|   |   `-- iteration-*.md
|   |-- raw/
|   |   |-- *.csv
|   |   `-- *.json
|   |-- excluded/
|   |   `-- *.md
|   `-- unsupported/
|       `-- README.md
|-- full-validation/
|-- fsi/
|-- compatibility-ledger.md
|-- package-validation.md
|-- regression-validation.md
`-- validation-summary.md
```

## Validation Summary Fields

- Final throughput status: `accepted`, `rejected`, `fallback-only`, `environment-limited`, or
  `blocked`.
- Release-ready status.
- Host profile and run identity.
- Focused lane id.
- Policy id.
- Declared per-iteration bound.
- Actual iteration durations.
- Required scenario coverage.
- Sample counts by scenario and path.
- Accepted iterations and count.
- Excluded iterations and primary reasons.
- Unsupported-host result.
- Full validation status.
- Compatibility impact.
- Package validation result.
- Regression validation result.
- Artifact locations.
- Shipped performance claim status.
- Remaining performance gates.
- Reviewer checklist result showing that the summary exposes the required decision fields within
  the 5 minute review target.

## Acceptance Rules

- `accepted` throughput requires at least three fresh same-profile focused iterations, each under
  the declared 10 minute bound, all required scenarios, matching sample policy, complete metadata,
  and readable artifact paths.
- `blocked` is used when focused throughput is accepted but full validation is missing, failing,
  interrupted, stale, or undocumented.
- `rejected` is used when focused evidence exists but cannot satisfy throughput acceptance.
- `fallback-only` is used when validated paths safely fall back without accepted performance
  evidence.
- `environment-limited` is used when the host cannot produce comparable evidence and accepted
  same-profile performance artifacts are zero.
- Unsupported-host validation must report zero accepted same-profile performance artifacts.
- The shipped performance claim remains `performance-not-accepted` unless same-profile timing is
  not noisy, Feature 159 reuse/promotion counters are net-positive, Feature 160 throughput is
  accepted, and Feature 161 host-lane scoping is accepted.
- Noisy same-profile timing is reported as a remaining performance-claim gate, not as a focused
  throughput exclusion reason by itself.

## Compatibility Ledger

The compatibility ledger must state one of:

- No public API surface changed.
- Public surface changed for throughput diagnostics, focused-lane status, exclusion reasons,
  readiness validation, or artifact inspection; include affected `.fsi`, surface-baseline files,
  semantic tests, FSI transcripts, and migration notes.

## Package Validation

Package validation records:

- Rendering.Harness Feature 160 focused-lane tests.
- Testing helper checks if public Testing helpers are added.
- Package.Tests compatibility checks.
- Surface-baseline command outcome when `.fsi` changes.
- Pack or package-readiness command outcome when package output changes.

## Regression Validation

Regression validation records focused preservation checks for Feature 155 proof correctness,
Feature 157 no-clear damage-scissored readiness, Feature 158 readback-free timing separation,
Feature 159 reuse/promotion readiness, unsupported-host fail-closed behavior, package validation,
and public-surface drift.

## Full Validation

Full validation records broad release-gate output separately from focused throughput iterations.
Missing, failing, interrupted, stale, or undocumented full validation blocks release-ready status
even when focused throughput is accepted.
