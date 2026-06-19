# Contract: Readiness Rules

## Readiness Tokens

Run-level `overallReadiness` values:

- `accepted`
- `rejected`
- `blocked`
- `failed`
- `environment-limited`

Record-level `acceptanceStatus` values:

- `accepted`
- `rejected`
- `blocked`
- `environment-limited`
- `excluded`
- `failed`

## Accepted Run Requirements

A run is `accepted` only when all of the following are true:

- A visible, focusable desktop surface is available.
- A reliable presentation boundary is measured for every accepted action.
- Timestamps are monotonic and precise enough for the budget calculation.
- Artifact write status is `complete`.
- Every required interactive family has accepted measured evidence.
- Display-only exclusions have reasons and are not counted as timed failures.
- At least 95% of representative actions are at or below 100 ms input-to-visible latency.
- No accepted representative action exceeds 150 ms.
- Every value-changing drag action is classified as `continuous`.
- No required validation check is timed-out, blocked, substitute-only, skipped without rationale, degraded, or manual-review-pending in the final readiness package.

## Rejected Run Rules

A measured run is `rejected` when live evidence exists but fails an acceptance budget or behavior rule:

- fewer than 95% of representative actions are at or below 100 ms
- any accepted candidate action exceeds 150 ms
- drag continuity is `delayed-catch-up`, `insufficient-samples`, or `failed`
- the expected visible result is not observed
- long-frame policy marks a required action non-accepted

Rejected summaries include `firstFailedBudget` and the five slowest interactions when timing data exists.

## Blocked and Environment-Limited Rules

A run is `environment-limited` or `blocked` when accepted live evidence cannot be collected:

- no visible desktop session
- window hidden, minimized, or not focusable
- no reliable presentation boundary
- low-precision or non-monotonic timestamps
- target control cannot be resolved
- live run timeout before complete coverage
- substitute/headless evidence only

The summary must say which boundary failed and which families are missing or blocked.

## Failed Rules

A run is `failed` when infrastructure prevents trustworthy classification:

- output root cannot be created
- `records.jsonl`, `summary.json`, `summary.md`, or `environment.md` cannot be completely written
- artifacts disagree with each other
- unhandled runner exception
- invalid summary schema

Failed runs do not claim accepted, rejected, or blocked responsiveness.

## Budget Calculation

- Percentiles use only measured `presented-frame` records with numeric `totalInputToVisibleMs`.
- Missing or environment-limited timing values are never treated as zero.
- Display-only exclusions are not included in timing percentiles.
- Rejected/blocked/failed records remain visible in counts and diagnostics.
- `firstFailedBudget` is the earliest budget in this order: coverage, environment boundary, write status, p95, max, drag continuity, long-frame policy.

## Final Validation Disclosure

The final validation package must explicitly list:

- timed-out checks
- blocked checks
- environment-limited checks
- substitute/headless checks
- skipped checks
- degraded checks
- manual-review-pending checks

None of those states may be summarized as green.
