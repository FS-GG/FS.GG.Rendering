# Contract: Console Summary

## Default Output

Default sample and readiness output prints a compact grouped summary. For the
representative mixed fixture, the default output must stay within 12 lines.

Required content:

- diagnostic status token
- counts by severity
- counts by category
- blocker count
- unclassified/review-required count
- artifact path when available
- first blocker or review-required source when present
- artifact write warning when present

Example shape:

```text
Diagnostics: blocked
Severity: informational=2 warning=2 error=1
Category: environment=1 backend-cost=1 rendering-limitation=1 developer-action=1 readiness-blocker=1
Blockers: 1 (first: validation-lanes/package-feed)
Review required: 0
Artifacts: specs/169-runtime-diagnostics-taxonomy/readiness/diagnostics-summary.json
```

## Grouped Entries

Default output shows grouped entries only for:

- blockers
- review-required diagnostics
- artifact write failures
- optional top non-blocking group when no blockers/review-required diagnostics
  exist

Repeated non-blocking diagnostics must not print one line per occurrence.

## Verbose Output

Verbose output may print all groups and individual records, but classification
and readiness status must not change when verbose mode is enabled.

Verbose output includes:

- source
- code
- category
- severity
- occurrence count
- first and last context
- action guidance
- matched exception id when applicable

## Artifact Failure Output

If a diagnostic artifact cannot be written, console output includes a
developer-action warning naming:

- failed artifact path
- error summary
- whether in-memory classification completed

The warning must be visible in both default and verbose output.
