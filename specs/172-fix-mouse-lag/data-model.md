# Data Model: Fix Mouse Interaction Lag

## Interactive Review Session

Represents one visible desktop responsiveness review run.

**Fields**

- `runId`: stable run identifier with `resp-` prefix.
- `startedUtc`, `completedUtc`: run boundaries.
- `theme`: `light` or `dark`.
- `scope`: sample/page/family scope, for example `antshowcase/all-interactive/light`.
- `desktopSessionStatus`: `measured`, `no-visible-surface`, `missing-boundary`,
  `headless-substitute`, `failed`, or `write-failed`.
- `artifactsRoot`: feature-local output directory.
- `environmentLimitations`: list of blocking or caveat tokens.

**Validation Rules**

- Accepted runs require `desktopSessionStatus = measured`.
- Runs with no visible surface, missing presentation boundary, low precision timestamps, or
  write failures cannot be accepted.

**State Transitions**

`not-run -> running -> accepted | blocked | failed`

## Pointer Interaction Review Action

One representative pointer action that must be exercised.

**Fields**

- `actionId`: stable id, unique inside the run.
- `pageId`: page from `PageRegistry.all`.
- `controlFamily`: interaction contract id such as `button-click`, `slider-rating`, or
  `navigation`.
- `controlIds`: catalog control ids covered by the action.
- `actionType`: `click`, `press-release`, `drag`, `open-close`, `select`, `navigate`, or
  `value-change`.
- `inputKind`: viewer input kind, usually `pointer-discrete`, `pointer-move`, or `wheel`.
- `expectedVisibleResult`: reviewer-readable expected feedback.
- `targetBudgetMs`: action budget, normally 100 ms target and 150 ms max.
- `displayOnlyReason`: present only for explicit exclusions.

**Validation Rules**

- `pageId` must be known.
- Interactive actions must have at least one `controlId`, an `expectedVisibleResult`, and no
  `displayOnlyReason`.
- Display-only exclusions must not be timed as failures, but must carry a reason.

## Responsiveness Evidence Record

Measured output for one review action.

**Fields**

- `recordId`, `runId`, `inputSequenceId`: stable identifiers.
- `page`, `controlFamily`, `controlIds`, `actionType`: action coverage context.
- `inputKind`, `inputName`: normalized input classification.
- `expectedVisibleResult`, `observedVisibleResult`: reviewer-facing expectation and result.
- `receiptTimestamp`, `queueDepthAtReceipt`, `queueDepthAtDrain`,
  `coalescedMovementCount`: scheduling context.
- `productMessageCount`, `productStateChanged`, `runtimeStateChanged`: state-change facts.
- `visibleResponse`: `presented-frame`, `no-visible-response`, `failed`,
  `environment-limited`, or `not-run`.
- `environmentStatus`: `measured`, `missing-boundary`, `no-visible-surface`,
  `headless-substitute`, `failed`, or `write-failed`.
- `phaseTiming`: receipt, queue, routing, update, view, retained step, layout, text, paint,
  present, and total input-to-visible durations where available.
- `dirtyRegion`: retained dirty-region summary when available.
- `acceptanceStatus`: `accepted`, `blocked`, `rejected`, or `excluded`.
- `diagnostics`: explanatory tokens and messages.

**Validation Rules**

- `accepted` requires `environmentStatus = measured`, `visibleResponse = presented-frame`,
  a present `totalInputToVisibleMs`, and timing inside the run acceptance policy.
- Records over 150 ms are rejected, not accepted.
- Records without a live boundary are blocked or environment-limited, not accepted.

## Responsiveness Summary

Run-level aggregate used by maintainers.

**Fields**

- `runId`, `scope`, `startedUtc`, `completedUtc`.
- `overallReadiness`: `accepted`, `blocked`, `incomplete`, `environment-limited`, or `failed`.
- `recordsPath`: relative path to JSONL records.
- `budgets`: input receipt, input-to-visible, and long-frame thresholds.
- `groups`: per page/input/family counts and p50/p95/max durations.
- `firstFailedBudget`: first failed timing budget, if any.
- `coverage`: interactive family count, accepted family count, excluded display-only count, and
  missing family ids.
- `slowestInteractions`: highest latency records.
- `environmentLimitations`, `diagnostics`.

**Validation Rules**

- Accepted summary requires every interactive family to have accepted evidence or an explicit
  display-only exclusion and no failed budget.
- A single live-environment limitation keeps the summary non-accepted.

## Visual Regression Evidence

Evidence that prior showcase fixes stayed intact.

**Fields**

- `preferredContactSheet`, `minimumContactSheet`: light/dark contact sheet paths.
- `alphaStatus`: opaque or failed.
- `navigationStatus`: Ant-like ghost/selected navigation status.
- `coverageStatus`: mapped-control coverage status.
- `sliderStatus`: click/drag behavior status.
- `findingStatus`: unresolved visual findings count.

**Validation Rules**

- Accepted visual regression requires opaque screenshots, no primary-filled navigation rail
  regression, clean coverage, and no slider click/drag regression.
