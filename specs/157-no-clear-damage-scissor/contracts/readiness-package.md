# Contract: Readiness Package

## Entry Point

`specs/157-no-clear-damage-scissor/readiness/validation-summary.md` is the reviewer entry point.
It must link every accepted, rejected, fallback, unsupported-host, compatibility, package, and
regression artifact needed to determine the final status in under 5 minutes.

## Required Files

```text
readiness/
|-- damage/
|   |-- attempts/
|   |-- fallbacks/
|   |-- parity/
|   |-- unsupported/
|   |-- summary.md
|   `-- summary.json
|-- fsi/
|-- compatibility-ledger.md
|-- package-validation.md
|-- regression-validation.md
`-- validation-summary.md
```

## Validation Summary Fields

- Final status: `accepted`, `fallback-only`, `rejected`, or `environment-limited`.
- Host profile and run identity.
- Feature 155 proof gate reference.
- Accepted attempts and scenario coverage.
- Fallback attempts and primary reasons.
- Damage validation status counts.
- Preserved-pixel evidence links.
- Damaged-pixel evidence links.
- Parity evidence links.
- Unsupported-host result.
- Compatibility impact.
- Package validation result.
- Regression validation result.
- Shipped performance claim status.
- Remaining performance gates.

## Acceptance Rules

- `accepted` requires at least three fresh accepted current-host attempts and at least five
  representative scenarios.
- Every accepted attempt must include proof gate, retained backing, damage validation, preserved
  pixels, damaged pixels, parity, host profile, run id, and artifact paths.
- Every fallback attempt must include one primary reason category.
- Unsupported-host validation must report zero accepted partial-redraw artifacts.
- The summary must state `performance-not-accepted` unless later report-defined gates are also
  complete and positive.

## Compatibility Ledger

The compatibility ledger must state one of:

- No public API surface changed.
- Public surface changed for damage-scoped diagnostics/readiness; include affected `.fsi`,
  surface-baseline files, semantic tests, FSI transcripts, and migration notes.

## Package Validation

Package validation records:

- Focused SkiaViewer tests.
- Rendering.Harness Feature 157 tests.
- Package.Tests compatibility checks.
- Testing helper checks if public Testing helpers are added.
- Surface-baseline command outcome when `.fsi` changes.
- Pack or package-readiness command outcome.

## Regression Validation

Regression validation records Feature 155 and Feature 156 focused checks plus broader rendering
checks needed to show existing P7 correctness, unsupported-host behavior, package validation, and
public-surface drift remain valid.
