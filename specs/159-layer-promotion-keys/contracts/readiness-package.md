# Contract: Readiness Package

## Entry Point

`specs/159-layer-promotion-keys/readiness/validation-summary.md` is the reviewer entry point. It
must link every accepted, rejected, demoted, fallback, unsupported-host, counter, parity,
compatibility, package, and regression artifact needed to determine the final status in under
5 minutes.

## Required Files

```text
readiness/
|-- promotion/
|   |-- attempts/
|   |-- reuse/
|   |-- demotions/
|   |-- fallbacks/
|   |-- parity/
|   |-- unsupported/
|   `-- summary.md
|-- counters/
|-- fsi/
|-- compatibility-ledger.md
|-- package-validation.md
|-- regression-validation.md
`-- validation-summary.md
```

## Validation Summary Fields

- Final status: `accepted`, `non-beneficial`, `fallback-only`, `rejected`, or
  `environment-limited`.
- Host profile and run identity.
- Policy id.
- Required scenario coverage.
- Accepted attempts and count.
- Promotion decisions.
- Reuse decisions.
- Demotion and fallback reasons.
- Counter totals.
- Parity status.
- Unsupported-host result.
- Compatibility impact.
- Package validation result.
- Regression validation result.
- Shipped performance claim status.
- Remaining performance gates.

## Acceptance Rules

- `accepted` requires at least three fresh same-profile attempts, all required scenario classes,
  passing parity, and net-positive reuse/promotion counters.
- `non-beneficial` is used when safety and parity pass but counters do not justify promotion.
- `fallback-only` is used when every validated path safely falls back with reviewer-visible
  reasons.
- `environment-limited` is used when the host cannot produce comparable evidence and accepted
  reuse artifacts are zero.
- Unsupported-host validation must report zero accepted Feature 159 reuse or promotion artifacts.
- The shipped performance claim remains `performance-not-accepted` unless same-profile timing is
  not noisy and the later host-lane gate is satisfied.

## Compatibility Ledger

The compatibility ledger must state one of:

- No public API surface changed.
- Public surface changed for promotion/reuse diagnostics or readiness; include affected `.fsi`,
  surface-baseline files, semantic tests, FSI transcripts, and migration notes.

## Package Validation

Package validation records:

- Focused Controls tests.
- Focused SkiaViewer tests.
- Rendering.Harness Feature 159 tests.
- Testing helper checks if public Testing helpers are added.
- Package.Tests compatibility checks.
- Surface-baseline command outcome when `.fsi` changes.
- Pack or package-readiness command outcome.

## Regression Validation

Regression validation records Feature 155, Feature 157, and Feature 158 focused checks plus the
broader rendering checks needed to show proof correctness, no-clear damage-scissored readiness,
readback/timing separation, full-redraw fallback, unsupported-host behavior, package validation,
and public-surface drift remain valid.
