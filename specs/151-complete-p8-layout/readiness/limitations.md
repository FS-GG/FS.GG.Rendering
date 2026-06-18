# Feature151 Limitations and Follow-Up Scope

Status: `accepted`

## Environment Limits

- P7 live compositor partial-redraw proof remains environment-limited and outside Feature151 acceptance. Feature151 does not turn environment-limited compositor evidence into an accepted performance or partial-redraw claim. Live partial-redraw acceptance is not claimed by P8.

## Synthetic-Only Disclosures

- Feature151 uses deterministic fixtures for failure-path and classification
  tests. These do not replace required accepted corpus, layout, ScrollViewer,
  package, or readiness evidence.

## Pre-Existing Unrelated Failures

- None recorded for the final local validation pass.

## Non-Accepted Checks

- Browser backend acceptance is not claimed.
- A general constraint solver is not introduced.
- Text shaping behavior is regression-classified only; no new text shaping
  behavior is claimed by this feature.

## Follow-Up Scope

- Restore or replace the absent root `fake.sh` wrapper if governed FAKE targets
  are required again. Feature151 used the runnable `dotnet` validation path from
  `quickstart.md`.
