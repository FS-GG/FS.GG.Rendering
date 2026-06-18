# P8 Readiness Contract

## Purpose

Define the final readiness package shape and acceptance rules for closing P8/R3b layout.

## Required Readiness Artifacts

`specs/151-complete-p8-layout/readiness/` must contain:

- `validation-summary.md`: single entry point and final P8 status;
- `corpus-validation.md`: representative layout corpus coverage and verdicts;
- `scrollviewer-validation.md`: ScrollViewer corpus coverage and verdicts;
- `reuse-validation.md`: measured/intrinsic reuse, miss, and stale-rejection evidence;
- `full-incremental-parity.md`: full/cold/warm/changed incremental comparisons;
- `regression-evidence.md`: broad regression evidence and classification;
- `compatibility-ledger.md`: public behavior/API deltas and migration notes;
- `package-validation.md`: full solution, surface, pack, and local feed evidence;
- `limitations.md`: environment limits and follow-up scope.

## Status Vocabulary

Use only these readiness statuses:

- accepted;
- incomplete;
- failed;
- skipped;
- environment-limited;
- synthetic-only;
- compatibility-blocked;
- missing-evidence.

These statuses map to `FS.GG.UI.Testing.LayoutReadinessStatus`.

## Acceptance Rules

- `validation-summary.md` is accepted only when all required evidence categories are accepted or
  explicitly non-blocking.
- Failed, incomplete, skipped, synthetic-only, compatibility-blocked, missing-evidence, or
  unclassified required evidence prevents final accepted P8 status.
- Environment-limited evidence cannot claim accepted behavior for the limited path.
- Compatibility deltas must be intentional, documented, and covered by surface/package evidence.
- The radical rendering report may be updated only after the feature-scoped readiness summary
  records the final status.

## Reviewability Rule

A maintainer must be able to open `validation-summary.md` and identify P8 status, blockers,
limitations, compatibility impact, package readiness, and links to supporting evidence in under
10 minutes.
