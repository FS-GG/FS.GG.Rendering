# Contract: Readiness Package

## Entry Point

`specs/158-separate-proof-timing/readiness/validation-summary.md` is the reviewer entry point. It
must link every included timing sample, excluded sample group, proof/probe artifact, unsupported-host
result, compatibility note, package validation record, and regression record needed to determine
Feature 158 status in under 5 minutes.

## Required Files

```text
readiness/
|-- timing/
|   |-- summary.md
|   |-- summary.json
|   |-- scenarios/
|   |   |-- timing-localized-update.md
|   |   |-- timing-no-change.md
|   |   |-- timing-movement-old-new.md
|   |   |-- timing-overlap.md
|   |   `-- timing-edge-clipping.md
|   |-- raw/
|   |   |-- *.csv
|   |   `-- *.json
|   |-- excluded/
|   |   `-- *.md
|   `-- unsupported/
|       `-- README.md
|-- proof-probes/
|-- fsi/
|-- compatibility-ledger.md
|-- package-validation.md
|-- regression-validation.md
`-- validation-summary.md
```

## Validation Summary Fields

- Final status: `accepted`, `rejected`, `fallback-only`, or `environment-limited`.
- Host profile and run identity.
- Measurement policy id.
- Required scenario coverage.
- Included timing samples by path and scenario.
- Excluded samples by reason.
- Proof/probe evidence links.
- Unsupported-host result.
- Feature 156 comparison: supersedes, confirms, or contextualizes the previous noisy timing result.
- Compatibility impact.
- Package validation result.
- Regression validation result.
- Shipped performance claim status.
- Remaining performance gates.

## Acceptance Rules

- `accepted` requires all required scenarios to have enough included samples under
  `readback-free-timing-v1`.
- Every accepted sample must declare `readback-free` or `readback-outside-measurement`.
- Every excluded sample must include one primary exclusion reason.
- Proof/probe samples must appear only as linked proof/probe or excluded evidence.
- Unsupported-host readiness records zero accepted proof and zero accepted performance artifacts.
- The summary must state `performance-not-accepted` unless all later report-defined performance
  gates are complete and positive.

## Compatibility Ledger

The compatibility ledger must state one of:

- No public API surface changed.
- Public surface changed for measurement policy, exclusion reasons, probe evidence, or readiness
  validation; include affected `.fsi`, surface-baseline files, semantic tests, FSI transcripts, and
  migration notes.

## Package Validation

Package validation records:

- Focused Rendering.Harness Feature 158 tests.
- `Perf` measurement-policy tests.
- Package.Tests compatibility checks.
- Testing helper checks if public Testing helpers are added.
- Surface-baseline command outcome when `.fsi` changes.
- Pack or package-readiness command outcome.

## Regression Validation

Regression validation records focused Feature 155 proof, Feature 156 timing, and Feature 157
damage-readiness checks plus broader rendering checks needed to show existing correctness,
fallback, unsupported-host, package, and public-surface boundaries remain valid.
